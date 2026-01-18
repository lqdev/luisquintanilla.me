const https = require('https');
const { signRequest } = require('../utils/signatures');

/**
 * ProcessAccept Function
 * Queue-triggered function that delivers Accept activities to follower inboxes
 * with HTTP signature authentication using Azure Key Vault
 */

/**
 * Fetch actor profile to get public key and other details
 */
async function fetchActorProfile(actorUrl) {
    return new Promise((resolve, reject) => {
        const { URL } = require('url');
        const url = new URL(actorUrl);
        
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
                    resolve(JSON.parse(data));
                } catch (parseError) {
                    reject(new Error(`Failed to parse actor JSON: ${parseError.message}`));
                }
            });
        });
        
        req.on('error', reject);
        req.setTimeout(5000, () => {
            req.destroy();
            reject(new Error('Timeout fetching actor profile'));
        });
        req.end();
    });
}

/**
 * Send signed Accept activity to follower inbox
 */
async function deliverAcceptActivity(targetInbox, acceptActivity, context) {
    const { URL } = require('url');
    const url = new URL(targetInbox);
    
    const body = JSON.stringify(acceptActivity);
    const bodyDigest = `SHA-256=${require('crypto').createHash('sha256').update(body).digest('base64')}`;
    
    // Build request object for signing
    const requestToSign = {
        method: 'POST',
        url: targetInbox,
        headers: {
            'Host': url.hostname,
            'Date': new Date().toUTCString(),
            'Digest': bodyDigest,
            'Content-Type': 'application/activity+json',
            'Content-Length': Buffer.byteLength(body),
            'User-Agent': 'lqdev.me ActivityPub/1.0'
        }
    };
    
    // Sign the request using Key Vault
    try {
        await signRequest(requestToSign, context);
    } catch (signError) {
        throw new Error(`Failed to sign request: ${signError.message}`);
    }
    
    // Send the signed request
    return new Promise((resolve, reject) => {
        const options = {
            hostname: url.hostname,
            port: url.port || 443,
            path: url.pathname + url.search,
            method: 'POST',
            headers: requestToSign.headers
        };
        
        const req = https.request(options, (res) => {
            let data = '';
            res.on('data', (chunk) => { data += chunk; });
            res.on('end', () => {
                if (res.statusCode >= 200 && res.statusCode < 300) {
                    context.log(`Accept delivered successfully: ${res.statusCode}`);
                    resolve({ success: true, statusCode: res.statusCode, body: data });
                } else {
                    context.log.warn(`Accept delivery returned ${res.statusCode}: ${data}`);
                    reject(new Error(`HTTP ${res.statusCode}: ${data}`));
                }
            });
        });
        
        req.on('error', reject);
        req.setTimeout(10000, () => {
            req.destroy();
            reject(new Error('Timeout delivering Accept activity'));
        });
        req.write(body);
        req.end();
    });
}

module.exports = async function (context, queueItem) {
    try {
        // Parse queue message
        const message = JSON.parse(Buffer.from(queueItem, 'base64').toString());
        const { acceptActivity, targetInbox, followerActorUrl } = message;
        
        context.log(`Processing Accept delivery to ${targetInbox}`);
        context.log(`Follower: ${followerActorUrl}`);
        
        // Deliver Accept activity with HTTP signature
        const result = await deliverAcceptActivity(targetInbox, acceptActivity, context);
        
        context.log(`✓ Accept activity delivered successfully to ${followerActorUrl}`);
        context.log(`  Target inbox: ${targetInbox}`);
        context.log(`  Status code: ${result.statusCode}`);
        
    } catch (error) {
        context.log.error(`✗ Failed to deliver Accept activity: ${error.message}`);
        context.log.error(error.stack);
        
        // Azure Queue will automatically retry failed messages based on queue configuration
        // After max retries (default 5), message moves to poison queue
        throw error; // Rethrow to trigger retry
    }
};
