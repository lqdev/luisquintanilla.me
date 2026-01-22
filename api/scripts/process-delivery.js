#!/usr/bin/env node

/**
 * Process ActivityPub Post Delivery Queue
 * 
 * This script runs in GitHub Actions to process the delivery queue
 * and deliver Create activities to follower inboxes with proper HTTP signatures.
 * 
 * Free tier architecture: Functions queue, GitHub Actions delivers.
 */

const https = require('https');
const { generateHttpSignature } = require('../utils/signatures');
const tableStorage = require('../utils/tableStorage');
const queueStorage = require('../utils/queueStorage');

/**
 * Validate inbox URL to prevent SSRF attacks
 */
function isValidInboxUrl(urlString) {
    try {
        const url = new URL(urlString);
        
        // Only allow HTTPS for ActivityPub
        if (url.protocol !== 'https:') {
            return false;
        }
        
        // Block private IP ranges and localhost
        const hostname = url.hostname.toLowerCase();
        
        // Block localhost and loopback
        if (hostname === 'localhost' || hostname === '127.0.0.1' || hostname === '::1') {
            return false;
        }
        
        // Block private IP ranges (10.x.x.x, 172.16-31.x.x, 192.168.x.x)
        if (hostname.match(/^10\./) || 
            hostname.match(/^172\.(1[6-9]|2[0-9]|3[0-1])\./) ||
            hostname.match(/^192\.168\./)) {
            return false;
        }
        
        // Block link-local addresses
        if (hostname.match(/^169\.254\./) || hostname.match(/^fe80:/)) {
            return false;
        }
        
        return true;
    } catch {
        return false;
    }
}

/**
 * Deliver activity to remote inbox with HTTP signature
 */
async function deliverActivityToInbox(inboxUrl, activity) {
    const url = new URL(inboxUrl);
    const body = JSON.stringify(activity);
    
    // Build request headers (keys must be lowercase for generateHttpSignature)
    const headers = {
        'host': url.hostname,
        'date': new Date().toUTCString(),
        'content-type': 'application/activity+json',
        'content-length': Buffer.byteLength(body),
        'user-agent': 'lqdev.me ActivityPub/1.0 (GitHub Actions)'
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
        const maxResponseSize = 1024 * 1024; // 1MB limit
        let dataSize = 0;
        
        const req = https.request(options, (res) => {
            let data = '';
            res.on('data', (chunk) => {
                dataSize += chunk.length;
                if (dataSize > maxResponseSize) {
                    req.destroy();
                    reject(new Error('Response size exceeds limit'));
                    return;
                }
                data += chunk;
            });
            res.on('end', () => {
                const result = {
                    success: res.statusCode >= 200 && res.statusCode < 300,
                    statusCode: res.statusCode,
                    responseBody: data
                };
                
                if (result.success) {
                    console.log(`✓ Delivery successful: ${res.statusCode}`);
                } else {
                    console.warn(`⚠ Delivery returned ${res.statusCode}: ${data}`);
                }
                
                resolve(result);
            });
        });
        
        req.on('error', (err) => {
            console.error(`✗ Network error delivering activity: ${err.message}`);
            reject(err);
        });
        
        req.setTimeout(30000, () => {
            req.destroy();
            const timeoutError = new Error('Timeout delivering activity (30s)');
            console.error(timeoutError.message);
            reject(timeoutError);
        });
        
        req.write(body);
        req.end();
    });
}

/**
 * Determine if error is permanent (don't retry) or temporary (retry)
 */
function isPermanentFailure(statusCode) {
    // 4xx errors (except 429 Too Many Requests) are permanent
    if (statusCode >= 400 && statusCode < 500 && statusCode !== 429) {
        return true;
    }
    return false;
}

/**
 * Process a single delivery task
 */
async function processDeliveryTask(task) {
    const { activityId, activityJson, targetInbox, followerActor, attemptCount } = task;
    
    console.log(`\nDelivering activity ${activityId} to ${followerActor}`);
    console.log(`Target inbox: ${targetInbox}`);
    console.log(`Attempt: ${attemptCount + 1}`);
    
    // Validate inbox URL to prevent SSRF
    if (!isValidInboxUrl(targetInbox)) {
        console.error(`✗ Invalid or unsafe inbox URL: ${targetInbox}`);
        await tableStorage.addDeliveryStatus(
            activityId,
            targetInbox,
            followerActor,
            'failed',
            0,
            'Invalid or unsafe inbox URL'
        );
        return { success: false, permanent: true };
    }
    
    // Parse activity JSON
    let activity;
    try {
        activity = JSON.parse(activityJson);
    } catch (parseError) {
        console.error(`✗ Failed to parse activity JSON: ${parseError.message}`);
        await tableStorage.addDeliveryStatus(
            activityId,
            targetInbox,
            followerActor,
            'failed',
            0,
            `Malformed activity JSON: ${parseError.message}`
        );
        return { success: false, permanent: true };
    }
    
    // Attempt delivery
    try {
        const result = await deliverActivityToInbox(targetInbox, activity);
        
        if (result.success) {
            // Delivery successful (2xx response)
            console.log(`✓ Successfully delivered to ${followerActor}`);
            
            await tableStorage.addDeliveryStatus(
                activityId,
                targetInbox,
                followerActor,
                'delivered',
                result.statusCode,
                null
            );
            
            return { success: true, permanent: false };
        } else {
            // Non-2xx response
            const errorMsg = `HTTP ${result.statusCode}: ${result.responseBody}`;
            
            if (isPermanentFailure(result.statusCode)) {
                // Permanent failure - don't retry
                console.error(`✗ Permanent failure for ${followerActor}: ${errorMsg}`);
                
                await tableStorage.addDeliveryStatus(
                    activityId,
                    targetInbox,
                    followerActor,
                    'failed',
                    result.statusCode,
                    errorMsg
                );
                
                return { success: false, permanent: true };
            } else {
                // Temporary failure - will be retried
                console.warn(`⚠ Temporary failure for ${followerActor}: ${errorMsg}`);
                
                // Try to get existing status to update attempt count
                let existingStatus = null;
                try {
                    existingStatus = await tableStorage.getDeliveryStatus(activityId, targetInbox);
                } catch (error) {
                    // Status doesn't exist yet
                }
                
                if (existingStatus) {
                    await tableStorage.updateDeliveryStatus(
                        activityId,
                        targetInbox,
                        'pending',
                        existingStatus.attemptCount + 1,
                        result.statusCode,
                        errorMsg
                    );
                } else {
                    await tableStorage.addDeliveryStatus(
                        activityId,
                        targetInbox,
                        followerActor,
                        'pending',
                        result.statusCode,
                        errorMsg
                    );
                }
                
                return { success: false, permanent: false };
            }
        }
    } catch (error) {
        // Network error or timeout - will be retried
        console.error(`✗ Error delivering to ${followerActor}: ${error.message}`);
        
        // Try to get existing status to update attempt count
        let existingStatus = null;
        try {
            existingStatus = await tableStorage.getDeliveryStatus(activityId, targetInbox);
        } catch (statusError) {
            // Status doesn't exist yet
        }
        
        if (existingStatus) {
            await tableStorage.updateDeliveryStatus(
                activityId,
                targetInbox,
                'pending',
                existingStatus.attemptCount + 1,
                0,
                error.message
            );
        } else {
            await tableStorage.addDeliveryStatus(
                activityId,
                targetInbox,
                followerActor,
                'pending',
                0,
                error.message
            );
        }
        
        return { success: false, permanent: false };
    }
}

/**
 * Main processing function
 */
async function main() {
    console.log('🚀 Starting ActivityPub post delivery processing');
    console.log(`Timestamp: ${new Date().toISOString()}`);
    
    // Verify environment configuration
    if (!process.env.KEY_VAULT_KEY_ID) {
        console.error('❌ KEY_VAULT_KEY_ID not configured');
        process.exit(1);
    }
    if (!process.env.ACTIVITYPUB_STORAGE_CONNECTION) {
        console.error('❌ ACTIVITYPUB_STORAGE_CONNECTION not configured');
        process.exit(1);
    }
    
    try {
        // Check queue length
        const queueLength = await queueStorage.getQueueLength('activitypub-delivery');
        console.log(`Found ${queueLength} messages in delivery queue`);
        
        if (queueLength === 0) {
            console.log('✓ No pending deliveries to process');
            return;
        }
        
        // Process messages (batch of 32 max per run to avoid timeout)
        const maxMessages = Math.min(queueLength, 32);
        console.log(`Processing up to ${maxMessages} messages\n`);
        
        const messages = await queueStorage.receiveMessages('activitypub-delivery', maxMessages);
        console.log(`Retrieved ${messages.length} messages from queue`);
        
        let successCount = 0;
        let permanentFailCount = 0;
        let temporaryFailCount = 0;
        
        // Process each message
        for (const message of messages) {
            try {
                // Parse message
                const task = JSON.parse(message.messageText);
                
                // Process delivery
                const result = await processDeliveryTask(task);
                
                if (result.success) {
                    successCount++;
                    // Delete message from queue on success
                    await queueStorage.deleteMessage('activitypub-delivery', message.messageId, message.popReceipt);
                } else if (result.permanent) {
                    permanentFailCount++;
                    // Delete message from queue on permanent failure
                    await queueStorage.deleteMessage('activitypub-delivery', message.messageId, message.popReceipt);
                } else {
                    temporaryFailCount++;
                    // Leave message in queue for retry (visibility timeout will reset)
                }
            } catch (error) {
                console.error(`✗ Error processing message: ${error.message}`);
                temporaryFailCount++;
                // Leave message in queue for retry
            }
        }
        
        console.log('\n📊 Delivery Summary:');
        console.log(`   ✓ Successful: ${successCount}`);
        console.log(`   ✗ Permanent Failures: ${permanentFailCount}`);
        console.log(`   ⚠ Temporary Failures (will retry): ${temporaryFailCount}`);
        console.log(`   📋 Total Processed: ${messages.length}`);
        console.log(`   📦 Remaining in Queue: ~${Math.max(0, queueLength - successCount - permanentFailCount)}`);
        
    } catch (error) {
        console.error('❌ Fatal error:', error);
        process.exit(1);
    }
}

// Run main function
main().catch(error => {
    console.error('❌ Unhandled error:', error);
    process.exit(1);
});
