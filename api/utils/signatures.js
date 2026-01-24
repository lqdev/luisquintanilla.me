const crypto = require('crypto');
const { URL } = require('url');
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
 * Verify Digest header matches request body
 * @param {Object} req - Azure Function request object
 * @param {Object} context - Azure Function context for logging
 * @returns {boolean} True if digest matches or no digest present
 */
function verifyDigest(req, context) {
  const digestHeader = req.headers['digest'];
  
  // If no Digest header, verification passes (digest is optional per spec)
  if (!digestHeader) {
    context.log('[Phase 3] No Digest header present - skipping digest verification');
    return true;
  }
  
  try {
    // Get request body - prefer rawBody for byte-accurate verification
    let bodyForDigest;
    if (req.rawBody && (typeof req.rawBody === 'string' || Buffer.isBuffer(req.rawBody))) {
      bodyForDigest = req.rawBody;
      context.log('[Phase 3] Using req.rawBody for digest verification');
    } else if (typeof req.body === 'string') {
      bodyForDigest = req.body;
      context.log('[Phase 3] Using string req.body for digest verification');
    } else {
      // JSON.stringify fallback may not match sender's representation
      bodyForDigest = JSON.stringify(req.body);
      context.log.warn('[Phase 3] ⚠️  Using JSON.stringify(req.body) - may not match sender representation');
    }
    
    // Convert to buffer if needed
    const bodyBuffer = Buffer.isBuffer(bodyForDigest)
      ? bodyForDigest
      : Buffer.from(String(bodyForDigest || ''), 'utf8');
    
    // Parse algorithm from Digest header (e.g., "SHA-256=..." or "SHA-512=...")
    const digestParts = typeof digestHeader === 'string' ? digestHeader.split('=', 2) : [];
    const digestAlgorithm = digestParts[0];
    const digestValue = digestParts[1];
    
    if (!digestAlgorithm || !digestValue) {
      context.log('[Phase 3] ❌ Digest verification FAILED - malformed Digest header');
      context.log(`[Phase 3]   Received: ${digestHeader}`);
      return false;
    }
    
    // Map HTTP Digest algorithm names to Node.js crypto algorithms
    const algoLower = digestAlgorithm.toLowerCase();
    const hashAlgorithmMap = {
      'sha-256': 'sha256',
      'sha256': 'sha256',
      'sha-512': 'sha512',
      'sha512': 'sha512'
    };
    const hashAlgorithm = hashAlgorithmMap[algoLower];
    
    if (!hashAlgorithm) {
      context.log('[Phase 3] ❌ Digest verification FAILED - unsupported digest algorithm');
      context.log(`[Phase 3]   Algorithm: ${digestAlgorithm}`);
      context.log(`[Phase 3]   Received: ${digestHeader}`);
      return false;
    }
    
    // Compute digest using the algorithm from the header
    const computedDigest = crypto.createHash(hashAlgorithm).update(bodyBuffer).digest('base64');
    
    // Compare using normalized algorithm name to handle case variations (SHA-256, sha-256, Sha-256)
    const normalizedAlgorithm = digestAlgorithm.toUpperCase().replace('SHA', 'SHA-');
    const expectedDigest = `${normalizedAlgorithm}=${computedDigest}`;
    const normalizedHeader = digestHeader.split('=')[0].toUpperCase().replace('SHA', 'SHA-') + '=' + digestHeader.split('=')[1];
    
    const digestMatch = normalizedHeader === expectedDigest;
    
    if (digestMatch) {
      context.log(`[Phase 3] ✅ Digest verification PASSED (${digestAlgorithm})`);
    } else {
      context.log('[Phase 3] ❌ Digest verification FAILED');
      context.log(`[Phase 3]   Expected: ${expectedDigest}`);
      context.log(`[Phase 3]   Received: ${digestHeader}`);
    }
    
    return digestMatch;
  } catch (error) {
    if (context) {
      context.log.error(`[Phase 3] Digest verification error: ${error.message}`);
    }
    return false;
  }
}

/**
 * Validate Date header timestamp to prevent replay attacks
 * @param {Object} req - Azure Function request object
 * @param {Object} context - Azure Function context for logging
 * @param {number} maxAgeSeconds - Maximum age of request in seconds (default: 300 = 5 minutes)
 * @returns {boolean} True if timestamp is within acceptable window
 */
function validateTimestamp(req, context, maxAgeSeconds = 300) {
  const dateHeader = req.headers['date'];
  
  // If no Date header, fail validation
  if (!dateHeader) {
    context.log('[Phase 4] ❌ Timestamp validation FAILED - no Date header present');
    return false;
  }
  
  try {
    const requestDate = new Date(dateHeader);
    
    // Validate that the date string parsed successfully
    if (isNaN(requestDate.getTime())) {
      context.log('[Phase 4] ❌ Timestamp validation FAILED - invalid Date header');
      return false;
    }
    
    const now = new Date();
    const ageSeconds = Math.abs((now - requestDate) / 1000);
    
    if (ageSeconds > maxAgeSeconds) {
      context.log('[Phase 4] ❌ Timestamp validation FAILED - request too old');
      context.log(`[Phase 4]   Request date: ${requestDate.toISOString()}`);
      context.log(`[Phase 4]   Current time: ${now.toISOString()}`);
      context.log(`[Phase 4]   Age: ${Math.round(ageSeconds)} seconds (max: ${maxAgeSeconds})`);
      return false;
    }
    
    context.log(`[Phase 4] ✅ Timestamp validation PASSED (age: ${Math.round(ageSeconds)}s)`);
    return true;
  } catch (error) {
    context.log.error(`[Phase 4] Timestamp validation error: ${error.message}`);
    return false;
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
    // PHASE 3: Verify Digest header first (if present)
    const digestValid = verifyDigest(req, context);
    if (!digestValid) {
      context.log.warn('[Phase 3] Digest verification failed - rejecting request');
      return false;
    }
    
    // PHASE 4: Validate timestamp to prevent replay attacks
    const timestampValid = validateTimestamp(req, context);
    if (!timestampValid) {
      context.log.warn('[Phase 4] Timestamp validation failed - rejecting request');
      return false;
    }
    
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
            const originalUrl = new URL(req.headers['x-ms-original-url']);
            // Only append search if not empty to avoid trailing '?'
            path = originalUrl.pathname + (originalUrl.search || '');
            if (context) {
              context.log(`[Phase 2] Using x-ms-original-url path: ${path} (req.url was: ${req.url})`);
            }
          } catch (parseError) {
            // If x-ms-original-url is present but malformed, fail verification
            // to avoid accidentally verifying against an incorrect req.url path
            context.log.error(`[Phase 2] Failed to parse x-ms-original-url: ${parseError.message}`);
            context.log.error('[Phase 2] Signature verification FAILED due to malformed x-ms-original-url');
            throw parseError;
          }
        } else if (req.url === '/inbox') {
          // Fallback: If no x-ms-original-url and path is just /inbox, 
          // construct full path (matches actual route: /api/activitypub/inbox)
          path = '/api/activitypub/inbox';
          context.log.warn(`[Phase 2] No x-ms-original-url, using fallback path: ${path}`);
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
  verifyDigest,
  generateHttpSignature,
  parseSignatureHeader,
  fetchActorPublicKey
};
