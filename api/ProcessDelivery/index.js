const https = require('https');
const { generateHttpSignature } = require('../utils/signatures');
const tableStorage = require('../utils/tableStorage');

/**
 * ProcessDelivery Azure Function
 * 
 * Trigger: Azure Queue 'activitypub-delivery'
 * Purpose: Deliver Create activities to follower inboxes
 * 
 * Queue message format:
 * {
 *   "activityId": "https://lqdev.me/api/activitypub/notes/abc123",
 *   "activityJson": "{...}",
 *   "targetInbox": "https://mastodon.social/inbox",
 *   "followerActor": "https://mastodon.social/users/alice",
 *   "attemptCount": 0,
 *   "queuedAt": "2026-01-19T12:00:00Z"
 * }
 */

/**
 * Deliver activity to remote inbox with HTTP signature
 * @param {string} inboxUrl - Target inbox URL
 * @param {Object} activity - Activity object to deliver
 * @param {Object} context - Azure Function context for logging
 * @returns {Promise<Object>} Result with success status and HTTP status code
 */
async function deliverActivityToInbox(inboxUrl, activity, context) {
    const { URL } = require('url');
    const url = new URL(inboxUrl);
    
    const body = JSON.stringify(activity);
    
    // Build request headers (keys must be lowercase for generateHttpSignature)
    const headers = {
        'host': url.hostname,
        'date': new Date().toUTCString(),
        'content-type': 'application/activity+json',
        'content-length': Buffer.byteLength(body),
        'user-agent': 'lqdev.me ActivityPub/1.0'
    };
    
    // Generate HTTP signature using Key Vault (also sets digest header)
    const signatureHeader = await generateHttpSignature('POST', inboxUrl, headers, body);
    headers['signature'] = signatureHeader;
    
    // Send the signed request (HTTP header names are case-insensitive in the request)
    const options = {
        hostname: url.hostname,
        port: url.port || 443,
        path: url.pathname + url.search,
        method: 'POST',
        headers: {
            'Host': url.hostname,
            'Date': headers['date'],
            'Content-Type': headers['content-type'],
            'Content-Length': headers['content-length'],
            'User-Agent': headers['user-agent'],
            'Digest': headers['digest'],
            'Signature': headers['signature']
        }
    };
    
    return new Promise((resolve, reject) => {
        const req = https.request(options, (res) => {
            let data = '';
            res.on('data', (chunk) => { data += chunk; });
            res.on('end', () => {
                const result = {
                    success: res.statusCode >= 200 && res.statusCode < 300,
                    statusCode: res.statusCode,
                    responseBody: data
                };
                
                if (result.success) {
                    context.log(`✅ Delivery successful: ${res.statusCode}`);
                } else {
                    context.log.warn(`⚠️ Delivery returned ${res.statusCode}: ${data}`);
                }
                
                resolve(result);
            });
        });
        
        req.on('error', (err) => {
            context.log.error(`❌ Network error delivering activity: ${err.message}`);
            reject(err);
        });
        
        req.setTimeout(30000, () => {
            req.destroy();
            const timeoutError = new Error('Timeout delivering activity (30s)');
            context.log.error(timeoutError.message);
            reject(timeoutError);
        });
        
        req.write(body);
        req.end();
    });
}

/**
 * Determine if error is permanent (don't retry) or temporary (retry)
 * @param {number} statusCode - HTTP status code
 * @returns {boolean} True if error is permanent
 */
function isPermanentFailure(statusCode) {
    // 4xx errors (except 429 Too Many Requests) are permanent
    if (statusCode >= 400 && statusCode < 500 && statusCode !== 429) {
        return true;
    }
    return false;
}

module.exports = async function (context, queueItem) {
    try {
        context.log('ProcessDelivery function triggered');
        
        // Parse queue message
        let task;
        try {
            // Queue messages are base64 encoded
            const messageText = Buffer.from(queueItem, 'base64').toString('utf8');
            task = JSON.parse(messageText);
        } catch (parseError) {
            context.log.error(`Failed to parse queue message: ${parseError.message}`);
            // Don't retry - message is malformed
            return;
        }
        
        const { activityId, activityJson, targetInbox, followerActor, attemptCount } = task;
        
        context.log(`Delivering activity ${activityId} to ${followerActor}`);
        context.log(`Target inbox: ${targetInbox}`);
        context.log(`Attempt: ${attemptCount + 1}`);
        
        // Parse activity JSON
        let activity;
        try {
            activity = JSON.parse(activityJson);
        } catch (parseError) {
            context.log.error(`Failed to parse activity JSON: ${parseError.message}`);
            // Don't retry - activity is malformed
            await tableStorage.addDeliveryStatus(
                activityId,
                targetInbox,
                followerActor,
                'failed',
                0,
                `Malformed activity JSON: ${parseError.message}`
            );
            return;
        }
        
        // Attempt delivery
        let result;
        try {
            result = await deliverActivityToInbox(targetInbox, activity, context);
            
            if (result.success) {
                // Delivery successful (2xx response)
                context.log(`✅ Successfully delivered to ${followerActor}`);
                
                await tableStorage.addDeliveryStatus(
                    activityId,
                    targetInbox,
                    followerActor,
                    'delivered',
                    result.statusCode,
                    null
                );
                
                // Success - message will be deleted from queue
                return;
            } else {
                // Non-2xx response
                const errorMsg = `HTTP ${result.statusCode}: ${result.responseBody}`;
                
                if (isPermanentFailure(result.statusCode)) {
                    // Permanent failure - don't retry
                    context.log.error(`❌ Permanent failure for ${followerActor}: ${errorMsg}`);
                    
                    await tableStorage.addDeliveryStatus(
                        activityId,
                        targetInbox,
                        followerActor,
                        'failed',
                        result.statusCode,
                        errorMsg
                    );
                    
                    // Don't throw - message will be deleted
                    return;
                } else {
                    // Temporary failure - retry (5xx, 429, etc.)
                    context.log.warn(`⚠️ Temporary failure for ${followerActor}: ${errorMsg}`);
                    
                    // Try to get existing status to update attempt count
                    let existingStatus = null;
                    try {
                        existingStatus = await tableStorage.getDeliveryStatus(activityId, targetInbox);
                    } catch (error) {
                        // Status doesn't exist yet, will create a new one
                    }
                    
                    if (existingStatus) {
                        // Update existing status
                        await tableStorage.updateDeliveryStatus(
                            activityId,
                            targetInbox,
                            'pending',
                            existingStatus.attemptCount + 1,
                            result.statusCode,
                            errorMsg
                        );
                    } else {
                        // Create new status entry (first attempt)
                        await tableStorage.addDeliveryStatus(
                            activityId,
                            targetInbox,
                            followerActor,
                            'pending',
                            result.statusCode,
                            errorMsg
                        );
                    }
                    
                    // Throw error to trigger automatic retry
                    throw new Error(`Temporary failure: ${errorMsg}`);
                }
            }
        } catch (error) {
            // Network error or timeout - retry
            context.log.error(`❌ Error delivering to ${followerActor}: ${error.message}`);
            
            // Try to get existing status to update attempt count
            let existingStatus = null;
            try {
                existingStatus = await tableStorage.getDeliveryStatus(activityId, targetInbox);
            } catch (statusError) {
                // Status doesn't exist yet, will create a new one
            }
            
            if (existingStatus) {
                // Update existing status
                await tableStorage.updateDeliveryStatus(
                    activityId,
                    targetInbox,
                    'pending',
                    existingStatus.attemptCount + 1,
                    0,
                    error.message
                );
            } else {
                // Create new status entry (first attempt)
                await tableStorage.addDeliveryStatus(
                    activityId,
                    targetInbox,
                    followerActor,
                    'pending',
                    0,
                    error.message
                );
            }
            
            // Throw error to trigger automatic retry
            throw error;
        }
        
    } catch (error) {
        context.log.error(`ProcessDelivery error: ${error.message}`);
        context.log.error(error.stack);
        
        // Re-throw to trigger automatic retry by Azure Queue
        throw error;
    }
};
