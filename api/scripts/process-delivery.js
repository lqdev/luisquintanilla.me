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
                    res.destroy();
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
                    const maxLogChars = 500;
                    const truncatedData = typeof data === 'string'
                        ? (data.length > maxLogChars ? data.slice(0, maxLogChars) + '...[truncated]' : data)
                        : data;
                    console.warn(`⚠ Delivery returned ${res.statusCode}: ${truncatedData}`);
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
 * Phase 4D: Now supports coveredActors for shared inbox optimization
 */
async function processDeliveryTask(task) {
    const { activityId, activityJson, targetInbox, followerActor, attemptCount, coveredActors } = task;
    
    // Phase 4D: Determine all actors covered by this delivery
    const allCoveredActors = coveredActors && coveredActors.length > 0 
        ? coveredActors 
        : [followerActor];  // Fallback for backward compatibility
    
    console.log(`\nDelivering activity ${activityId} to inbox: ${targetInbox}`);
    console.log(`Covers ${allCoveredActors.length} follower(s)${allCoveredActors.length > 1 ? ' (shared inbox)' : ''}`);
    console.log(`Attempt: ${(attemptCount || 0) + 1}`);
    
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
            console.log(`✓ Successfully delivered to inbox: ${targetInbox}`);
            
            // Phase 4D: Track delivery status for ALL covered actors
            for (const actor of allCoveredActors) {
                await tableStorage.addDeliveryStatus(
                    activityId,
                    targetInbox,
                    actor,
                    'delivered',
                    result.statusCode,
                    null
                );
            }
            
            if (allCoveredActors.length > 1) {
                console.log(`✓ Marked delivery complete for ${allCoveredActors.length} followers via shared inbox`);
            }
            
            return { success: true, permanent: false, coveredCount: allCoveredActors.length };
        } else {
            // Non-2xx response
            const errorMsg = `HTTP ${result.statusCode}: ${result.responseBody}`;
            
            if (isPermanentFailure(result.statusCode)) {
                // Permanent failure - don't retry
                console.error(`✗ Permanent failure for inbox ${targetInbox}: ${errorMsg}`);
                
                // Truncate error message to prevent Azure Table Storage entity size limits (1MB)
                const maxErrorChars = 1000;
                const truncatedError = errorMsg.length > maxErrorChars 
                    ? errorMsg.slice(0, maxErrorChars) + '...[truncated]' 
                    : errorMsg;
                
                // Phase 4D: Track failure for ALL covered actors
                for (const actor of allCoveredActors) {
                    await tableStorage.addDeliveryStatus(
                        activityId,
                        targetInbox,
                        actor,
                        'failed',
                        result.statusCode,
                        truncatedError
                    );
                }
                
                if (allCoveredActors.length > 1) {
                    console.error(`✗ Marked delivery failed for ${allCoveredActors.length} followers via shared inbox`);
                }
                
                return { success: false, permanent: true, coveredCount: allCoveredActors.length };
            } else {
                // Temporary failure - will be retried
                console.warn(`⚠ Temporary failure for inbox ${targetInbox}: ${errorMsg}`);
                
                // Phase 4D: Track pending status for ALL covered actors
                // For shared inboxes, we track all actors as pending for retry
                for (const actor of allCoveredActors) {
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
                        break;  // Only need to update once for shared inbox
                    } else {
                        await tableStorage.addDeliveryStatus(
                            activityId,
                            targetInbox,
                            actor,
                            'pending',
                            result.statusCode,
                            errorMsg
                        );
                    }
                }
                
                return { success: false, permanent: false, coveredCount: allCoveredActors.length };
            }
        }
    } catch (error) {
        // Network error or timeout - will be retried
        console.error(`✗ Network error for inbox ${targetInbox}: ${error.message}`);
        
        // Phase 4D: Track error for first actor (shared inbox delivery is atomic)
        const primaryActor = allCoveredActors[0];
        
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
                primaryActor,
                'pending',
                0,
                error.message
            );
        }
        
        return { success: false, permanent: false, coveredCount: allCoveredActors.length };
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
        // Get pending deliveries from table storage
        const pendingDeliveries = await tableStorage.getPendingDeliveries();
        console.log(`Found ${pendingDeliveries.length} pending deliveries in queue`);
        
        if (pendingDeliveries.length === 0) {
            console.log('✓ No pending deliveries to process');
            return;
        }
        
        // Get all followers to deliver to
        const followers = await tableStorage.getAllFollowers();
        console.log(`Found ${followers.length} followers to deliver to\n`);
        
        if (followers.length === 0) {
            console.log('⚠ No followers to deliver to - marking deliveries as completed');
            for (const delivery of pendingDeliveries) {
                await tableStorage.markDeliveryCompleted(delivery.queueId);
            }
            return;
        }
        
        let successCount = 0;
        let permanentFailCount = 0;
        let temporaryFailCount = 0;
        
        // Process each pending delivery
        for (const delivery of pendingDeliveries) {
            console.log(`\n📨 Processing delivery: ${delivery.noteId}`);
            
            let deliverySuccessCount = 0;
            let deliveryFailCount = 0;
            
            // Deliver to each follower
            for (const follower of followers) {
                if (!follower.inbox) {
                    console.warn(`⚠ Skipping follower ${follower.actorUrl} - no inbox URL`);
                    continue;
                }
                
                const task = {
                    activityId: delivery.createActivity.id,
                    activityJson: JSON.stringify(delivery.createActivity),
                    targetInbox: follower.inbox,
                    followerActor: follower.actorUrl,
                    attemptCount: delivery.retryCount || 0
                };
                
                try {
                    const result = await processDeliveryTask(task);
                    
                    if (result.success) {
                        deliverySuccessCount++;
                    } else {
                        deliveryFailCount++;
                        if (result.permanent) {
                            console.warn(`⚠ Permanent failure for ${follower.actorUrl}`);
                        }
                    }
                } catch (error) {
                    console.error(`✗ Error delivering to ${follower.actorUrl}: ${error.message}`);
                    deliveryFailCount++;
                }
            }
            
            // Mark delivery as completed if all or most succeeded
            if (deliverySuccessCount > 0 || deliveryFailCount === followers.length) {
                await tableStorage.markDeliveryCompleted(delivery.queueId);
                successCount++;
                console.log(`✓ Delivery ${delivery.noteId} completed (${deliverySuccessCount}/${followers.length} successful)`);
            } else {
                // Mark as failed if all failed
                await tableStorage.markDeliveryFailed(
                    delivery.queueId,
                    `All deliveries failed (0/${followers.length} successful)`
                );
                permanentFailCount++;
            }
        }
        
        console.log('\n📊 Delivery Summary:');
        console.log(`   ✓ Successful: ${successCount}`);
        console.log(`   ✗ Failed: ${permanentFailCount}`);
        console.log(`   📋 Total Deliveries Processed: ${pendingDeliveries.length}`);
        
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
