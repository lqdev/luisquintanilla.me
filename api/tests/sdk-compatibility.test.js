const assert = require('node:assert/strict');
const crypto = require('node:crypto');
const { test, mock } = require('node:test');
const { createHttpHeaders } = require('@azure/core-rest-pipeline');
const { toHttpHeadersLike } = require('@azure/core-http-compat');
const { DefaultAzureCredential } = require('@azure/identity');
const { CryptographyClient } = require('@azure/keyvault-keys');
const { TableClient } = require('@azure/data-tables');
const { QueueClient } = require('@azure/storage-queue');
const { KeyVaultSigner } = require('../utils/keyvault');

// Fail closed if a client accidentally uses a real transport or identity endpoint.
const denyNetwork = () => { throw new Error('Network access is forbidden in SDK smoke tests'); };
mock.method(require('node:http'), 'request', denyNetwork);
mock.method(require('node:https'), 'request', denyNetwork);
mock.method(globalThis, 'fetch', denyNetwork);

function response(request, status, body, headers = {}) {
  return {
    request,
    status,
    headers: createHttpHeaders({ 'content-type': 'application/json', ...headers }),
    bodyAsText: typeof body === 'string' ? body : JSON.stringify(body)
  };
}

const connectionString =
  'DefaultEndpointsProtocol=https;AccountName=smoketest;' +
  `AccountKey=${Buffer.alloc(32, 1).toString('base64')};EndpointSuffix=core.windows.net`;

test('Node runtime matches the deployment and API engine requirement', () => {
  assert.equal(process.versions.node.split('.')[0], '22');
  assert.equal(require('../../staticwebapp.config.json').platform.apiRuntime, 'node:22');
  assert.equal(require('../package.json').engines.node, '>=22.0.0 <23.0.0');
  assert.ok(new DefaultAzureCredential());
});

test('Key Vault RS256 request and signer result remain compatible', async (t) => {
  const keyId = 'https://smoketest.vault.azure.net/keys/signing/version';
  const digest = crypto.createHash('sha256').update('offline signing fixture').digest();
  const signature = Buffer.from('offline-signature');
  const requests = [];
  const credential = { getToken: async () => ({ token: 'offline-token', expiresOnTimestamp: Date.now() + 60000 }) };
  const httpClient = {
    async sendRequest(request) {
      assert.equal(new URL(request.url).hostname, 'smoketest.vault.azure.net');
      if (!request.headers.get('authorization')) {
        return response(request, 401, { error: { code: 'Unauthorized' } }, {
          'www-authenticate': 'Bearer authorization="https://login.windows.net/offline-tenant", resource="https://vault.azure.net"'
        });
      }
      assert.equal(request.headers.get('authorization'), 'Bearer offline-token');
      requests.push(request);
      if (request.method === 'GET' && new URL(request.url).pathname === '/keys/signing/version') {
        return response(request, 200, {
          key: { kid: keyId, kty: 'RSA', key_ops: ['sign'] },
          attributes: { enabled: true }
        });
      }
      assert.equal(request.method, 'POST');
      assert.equal(new URL(request.url).pathname, '/keys/signing/version/sign');
      const body = JSON.parse(request.body);
      assert.equal(body.alg, 'RS256');
      assert.equal(body.value, digest.toString('base64url'));
      return response(request, 200, { kid: keyId, value: signature.toString('base64url') });
    }
  };
  const previousKeyId = process.env.KEY_VAULT_KEY_ID;
  process.env.KEY_VAULT_KEY_ID = keyId;
  t.after(() => {
    if (previousKeyId === undefined) delete process.env.KEY_VAULT_KEY_ID;
    else process.env.KEY_VAULT_KEY_ID = previousKeyId;
  });
  const signer = new KeyVaultSigner();
  signer.client = new CryptographyClient(keyId, credential, { httpClient });
  assert.equal(signer.isConfigured(), true);
  assert.deepEqual(await signer.sign(digest), signature);
  assert.deepEqual(requests.map(request => request.method), ['GET', 'POST']);
});

test('local RSA signing and public-key verification remain compatible', async (t) => {
  const keys = crypto.generateKeyPairSync('rsa', {
    modulusLength: 2048,
    publicKeyEncoding: { type: 'spki', format: 'pem' },
    privateKeyEncoding: { type: 'pkcs8', format: 'pem' }
  });
  const previousKeyId = process.env.KEY_VAULT_KEY_ID;
  const previousPrivateKey = process.env.ACTIVITYPUB_PRIVATE_KEY;
  delete process.env.KEY_VAULT_KEY_ID;
  process.env.ACTIVITYPUB_PRIVATE_KEY = keys.privateKey;
  t.after(() => {
    if (previousKeyId === undefined) delete process.env.KEY_VAULT_KEY_ID;
    else process.env.KEY_VAULT_KEY_ID = previousKeyId;
    if (previousPrivateKey === undefined) delete process.env.ACTIVITYPUB_PRIVATE_KEY;
    else process.env.ACTIVITYPUB_PRIVATE_KEY = previousPrivateKey;
  });
  const signer = new KeyVaultSigner();
  const data = Buffer.from('offline HTTP signature fixture');
  const signature = await signer.sign(data);
  assert.equal(await signer.verify(data, signature, keys.publicKey), true);
  assert.equal(await signer.verify(Buffer.from('modified'), signature, keys.publicKey), false);
});

test('table connection-string authentication, CRUD and list serialization', async () => {
  const requests = [];
  let storedEntity;
  const client = TableClient.fromConnectionString(connectionString, 'followers', {
    httpClient: {
      async sendRequest(request) {
        assert.equal(new URL(request.url).hostname, 'smoketest.table.core.windows.net');
        assert.match(request.headers.get('authorization'), /^SharedKey/);
        requests.push(request.method);
        const url = new URL(request.url);
        if (url.pathname === '/Tables') {
          assert.equal(request.method, 'POST');
          assert.equal(JSON.parse(request.body).TableName, 'followers');
          return response(request, 201, { TableName: 'followers' });
        }
        if (request.method === 'PATCH') {
          storedEntity = { ...storedEntity, ...JSON.parse(request.body) };
          return response(request, 204, '', { etag: '"offline-etag"' });
        }
        if (request.method === 'GET') {
          return response(request, 200,
            url.pathname.endsWith('()') ? { value: [storedEntity] } : storedEntity,
            { etag: '"offline-etag"' });
        }
        assert.equal(request.method, 'DELETE');
        return response(request, 204, '');
      }
    }
  });
  await client.createTable();
  const entity = { partitionKey: 'follower', rowKey: 'offline-row', actorUrl: 'https://example.invalid/actor' };
  await client.upsertEntity(entity, 'Merge');
  const retrieved = await client.getEntity(entity.partitionKey, entity.rowKey);
  assert.equal(retrieved.actorUrl, entity.actorUrl);
  assert.equal(retrieved.partitionKey, entity.partitionKey);
  await client.updateEntity({ ...retrieved, displayName: 'Offline actor' }, 'Merge');
  const listed = [];
  for await (const item of client.listEntities({ queryOptions: { filter: "PartitionKey eq 'follower'" } })) {
    listed.push(item);
  }
  assert.equal(listed.length, 1);
  assert.equal(listed[0].displayName, 'Offline actor');
  await client.deleteEntity(entity.partitionKey, entity.rowKey);
  assert.deepEqual(requests, ['POST', 'PATCH', 'GET', 'PATCH', 'GET', 'DELETE']);
});

test('queue connection-string authentication and base64 message serialization', async () => {
  const message = JSON.stringify({ activityId: 'offline-id', targetInbox: 'https://example.invalid/inbox' });
  const encoded = Buffer.from(message).toString('base64');
  const requests = [];
  const client = new QueueClient(connectionString, 'activitypub-delivery', {
    httpClient: {
      async sendRequest(request) {
        assert.equal(new URL(request.url).hostname, 'smoketest.queue.core.windows.net');
        assert.match(request.headers.get('authorization'), /^SharedKey/);
        requests.push(request.method);
        let result;
        if (request.method === 'PUT') {
          assert.equal(new URL(request.url).pathname, '/activitypub-delivery');
          result = response(request, 201, '');
        } else {
          assert.equal(request.method, 'POST');
          assert.equal(new URL(request.url).pathname, '/activitypub-delivery/messages');
          assert.ok(request.body.includes(`<MessageText>${encoded}</MessageText>`));
          result = response(request, 201,
            '<QueueMessagesList><QueueMessage><MessageId>offline-message</MessageId>' +
            '<InsertionTime>Sat, 14 Nov 2026 12:00:00 GMT</InsertionTime>' +
            '<ExpirationTime>Sat, 21 Nov 2026 12:00:00 GMT</ExpirationTime>' +
            '<PopReceipt>offline-receipt</PopReceipt>' +
            '<TimeNextVisible>Sat, 14 Nov 2026 12:00:00 GMT</TimeNextVisible>' +
            '</QueueMessage></QueueMessagesList>', { 'content-type': 'application/xml' });
        }
        result.headers = toHttpHeadersLike(result.headers);
        return result;
      }
    }
  });
  await client.createIfNotExists();
  const result = await client.sendMessage(encoded);
  assert.equal(result.messageId, 'offline-message');
  assert.deepEqual(requests, ['PUT', 'POST']);
});
