const crypto = require('crypto');
const { KeyVaultSigner } = require('./keyvault');

/**
 * HTTP Signatures for ActivityPub
 * Implementation following: https://docs.joinmastodon.org/spec/security/
 */

/**
 * Parse HTTP Signature header
 * @param {string} signatureHeader - Value of Signature header
 * @returns {Object} Parsed signature components
 */
function parseSignatureHeader(signatureHeader) {
  const parts = {};
  const regex = /(\w+)="([^"]*)"/g;
  let match;
  
  while ((match = regex.exec(signatureHeader)) !== null) {
    parts[match[1]] = match[2];
  }
  
  return parts;
}

/**
 * Fetch actor's public key from their profile
 * @param {string} actorUrl - Actor ID URL
 * @returns {Promise<string>} PEM-encoded public key
 */
async function fetchActorPublicKey(actorUrl) {
  try {
    const https = require('https');
    const { URL } = require('url');
    const url = new URL(actorUrl);
    
    return new Promise((resolve, reject) => {
      const options = {
        hostname: url.hostname,
        port: url.port || 443,
        path: url.pathname + url.search,
        method: 'GET',
        headers: {
          'Accept': 'application/activity+json, application/ld+json'
        }
      };
      
      const req = https.request(options, (res) => {
        let data = '';
        res.on('data', (chunk) => { data += chunk; });
        res.on('end', () => {
          try {
            const actor = JSON.parse(data);
            if (actor.publicKey && actor.publicKey.publicKeyPem) {
              resolve(actor.publicKey.publicKeyPem);
            } else {
              reject(new Error('No public key found in actor profile'));
            }
          } catch (parseError) {
            reject(new Error(`Failed to parse actor JSON: ${parseError.message}`));
          }
        });
      });
      
      req.on('error', reject);
      req.setTimeout(5000, () => {
        req.destroy();
        reject(new Error('Timeout fetching actor public key'));
      });
      req.end();
    });
  } catch (error) {
    throw new Error(`Failed to fetch actor public key: ${error.message}`);
  }
}

/**
 * Verify HTTP signature on incoming ActivityPub request
 * @param {Object} req - Azure Function request object
 * @param {Object} context - Azure Function context for logging
 * @returns {Promise<boolean>} True if signature is valid
 */
async function verifyHttpSignature(req, context) {
  try {
    const signatureHeader = req.headers['signature'];
    if (!signatureHeader) {
      context.log.warn('No Signature header present');
      return false;
    }
    
    // Parse signature header
    const sigParts = parseSignatureHeader(signatureHeader);
    if (!sigParts.keyId || !sigParts.signature || !sigParts.headers) {
      context.log.warn('Incomplete signature header');
      return false;
    }
    
    // Extract actor ID from keyId (usually ends with #main-key)
    const actorUrl = sigParts.keyId.split('#')[0];
    
    // Fetch actor's public key
    const publicKeyPem = await fetchActorPublicKey(actorUrl);
    
    // Reconstruct signing string from headers
    const headersToSign = sigParts.headers.split(' ');
    const signingString = headersToSign.map(headerName => {
      if (headerName === '(request-target)') {
        const method = req.method.toLowerCase();
        
        // PHASE 2 FIX: Use x-ms-original-url header for Azure Static Web Apps
        // Azure SWA routing modifies req.url, but preserves original URL in this header
        // This ensures we reconstruct the same path that Mastodon signed
        let path = req.url || '/';
        if (req.headers['x-ms-original-url']) {
          try {
            const { URL } = require('url');
            const originalUrl = new URL(req.headers['x-ms-original-url']);
            path = originalUrl.pathname + originalUrl.search;
            if (context) {
              context.log(`[Phase 2] Using x-ms-original-url path: ${path} (req.url was: ${req.url})`);
            }
          } catch (parseError) {
            if (context) {
              context.log.warn(`[Phase 2] Failed to parse x-ms-original-url: ${parseError.message}, falling back to req.url`);
            }
          }
        } else if (req.url === '/inbox') {
          // Fallback: If no x-ms-original-url and path is just /inbox, 
          // construct full path (may be needed for local testing)
          path = '/api/inbox';
          if (context) {
            context.log(`[Phase 2] No x-ms-original-url, using known path: ${path}`);
          }
        }
        
        return `(request-target): ${method} ${path}`;
      } else {
        const headerValue = req.headers[headerName.toLowerCase()];
        return `${headerName}: ${headerValue}`;
      }
    }).join('\n');
    
    // Verify signature
    const signer = new KeyVaultSigner();
    const signatureBuffer = Buffer.from(sigParts.signature, 'base64');
    const dataBuffer = Buffer.from(signingString, 'utf8');
    
    const isValid = await signer.verify(dataBuffer, signatureBuffer, publicKeyPem);
    
    if (!isValid) {
      context.log.warn(`Invalid signature from ${actorUrl}`);
    }
    
    return isValid;
  } catch (error) {
    context.log.error(`Signature verification error: ${error.message}`);
    return false;
  }
}

/**
 * Generate HTTP signature for outgoing ActivityPub request
 * @param {string} method - HTTP method (GET, POST, etc.)
 * @param {string} url - Target URL
 * @param {Object} headers - Request headers
 * @param {string} body - Request body (for POST)
 * @returns {Promise<string>} Signature header value
 */
async function generateHttpSignature(method, url, headers, body = null) {
  const signer = new KeyVaultSigner();
  if (!signer.isConfigured()) {
    throw new Error('Signing key not configured');
  }
  
  const { URL } = require('url');
  const parsedUrl = new URL(url);
  const path = parsedUrl.pathname + parsedUrl.search;
  
  // Build signing string
  const headersToSign = ['(request-target)', 'host', 'date'];
  if (body) {
    headersToSign.push('digest');
  }
  
  const signingParts = [
    `(request-target): ${method.toLowerCase()} ${path}`,
    `host: ${parsedUrl.hostname}`,
    `date: ${headers['date'] || new Date().toUTCString()}`
  ];
  
  if (body) {
    const hash = crypto.createHash('sha256').update(body).digest('base64');
    headers['digest'] = `SHA-256=${hash}`;
    signingParts.push(`digest: ${headers['digest']}`);
  }
  
  const signingString = signingParts.join('\n');
  
  // Hash the signing string first (Key Vault requires digest input for RS256)
  const hash = crypto.createHash('sha256').update(signingString, 'utf8').digest();
  
  // Sign the hash
  const signature = await signer.sign(hash);
  const signatureB64 = signature.toString('base64');
  
  // Build signature header
  const keyId = 'https://lqdev.me/api/activitypub/actor#main-key';
  return `keyId="${keyId}",headers="${headersToSign.join(' ')}",signature="${signatureB64}",algorithm="rsa-sha256"`;
}

module.exports = {
  verifyHttpSignature,
  generateHttpSignature,
  parseSignatureHeader,
  fetchActorPublicKey
};
