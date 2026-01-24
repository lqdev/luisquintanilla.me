const fs = require('fs').promises;
const path = require('path');
const https = require('https');
const crypto = require('crypto');
const { verifyHttpSignatureWithFeatureFlag, generateHttpSignature } = require('../utils/signatures');

// Enhanced error handling for module loading
let tableStorage;
try {
    tableStorage = require('../utils/tableStorage');
    console.log('✅ tableStorage module loaded successfully');
} catch (error) {
    console.error('❌ CRITICAL: Failed to load tableStorage module:', error.message);
    console.error('Stack:', error.stack);
    throw error; // Re-throw to prevent function from loading with broken dependency
}

/**
 * Deliver Accept activity directly to follower inbox
 * Note: Delivery is done synchronously in this version to avoid Azure Static Web Apps queue trigger limitations
 */
async function deliverAcceptActivity(followActivity, context) {
    const acceptActivity = {
        "@context": "https://www.w3.org/ns/activitystreams",
        "id": `https://lqdev.me/api/activitypub/activities/accept/${Date.now()}`,
        "type": "Accept",
        "actor": "https://lqdev.me/api/activitypub/actor",
        "object": followActivity
    };
    
    // Extract inbox URL from follower actor
    const followerActor = followActivity.actor;
    
    try {
        // Fetch follower's actor profile to get inbox URL
        const actorProfile = await fetchActorProfile(followerActor);
        const inboxUrl = actorProfile.inbox;
        
        if (!inboxUrl) {
            context.log.error(`No inbox URL found for ${followerActor}`);
            return false;
        }
        
        // Deliver Accept activity with HTTP signature
        const { URL } = require('url');
        const url = new URL(inboxUrl);
        
        const body = JSON.stringify(acceptActivity);
        
        // Build request headers
        const headers = {
            'Host': url.hostname,
            'Date': new Date().toUTCString(),
            'Content-Type': 'application/activity+json',
            'Content-Length': Buffer.byteLength(body),
            'User-Agent': 'lqdev.me ActivityPub/1.0'
        };
        
        // Generate HTTP signature using Key Vault (also sets Digest header)
        let signatureHeader;
        try {
            signatureHeader = await generateHttpSignature('POST', inboxUrl, headers, body);
            headers['Signature'] = signatureHeader;
        } catch (signError) {
            context.log.error(`Failed to generate signature: ${signError.message}`);
            return false;
        }
        
        // Send the signed request
        await new Promise((resolve, reject) => {
            const options = {
                hostname: url.hostname,
                port: url.port || 443,
                path: url.pathname + url.search,
                method: 'POST',
                headers: headers
            };
            
            const req = https.request(options, (res) => {
                let data = '';
                res.on('data', (chunk) => { data += chunk; });
                res.on('end', () => {
                    if (res.statusCode >= 200 && res.statusCode < 300) {
                        context.log(`Accept delivered successfully: ${res.statusCode}`);
                        resolve({ success: true, statusCode: res.statusCode, body: data });
                    } else {
                        const errorMsg = `HTTP ${res.statusCode}: ${data}`;
                        context.log.error(`Accept delivery failed - ${errorMsg}`);
                        reject(new Error(errorMsg));
                    }
                });
            });
            
            req.on('error', (err) => {
                context.log.error(`Network error delivering Accept: ${err.message}`);
                reject(err);
            });
            req.setTimeout(10000, () => {
                req.destroy();
                const timeoutError = new Error('Timeout delivering Accept activity');
                context.log.error(timeoutError.message);
                reject(timeoutError);
            });
            req.write(body);
            req.end();
        });
        
        context.log(`Accept activity delivered successfully to ${followerActor}`);
        return true;
    } catch (error) {
        context.log.error(`Failed to deliver Accept activity: ${error.message}`);
        return false;
    }
}

/**
 * Fetch actor profile
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
 * Send activity to remote inbox
 * Note: Signing is optional for now (Phase 2.1), will be added in Phase 2.2
 */
async function sendActivityToInbox(inboxUrl, activity, context) {
    return new Promise((resolve, reject) => {
        const { URL } = require('url');
        const url = new URL(inboxUrl);
        
        const body = JSON.stringify(activity);
        
        const options = {
            hostname: url.hostname,
            port: url.port || 443,
            path: url.pathname + url.search,
            method: 'POST',
            headers: {
                'Content-Type': 'application/activity+json',
                'Content-Length': Buffer.byteLength(body),
                'User-Agent': 'lqdev.me ActivityPub/1.0'
            }
        };
        
        const req = https.request(options, (res) => {
            let data = '';
            res.on('data', (chunk) => { data += chunk; });
            res.on('end', () => {
                if (res.statusCode >= 200 && res.statusCode < 300) {
                    resolve(data);
                } else {
                    reject(new Error(`HTTP ${res.statusCode}: ${data}`));
                }
            });
        });
        
        req.on('error', reject);
        req.setTimeout(10000, () => {
            req.destroy();
            reject(new Error('Timeout sending activity'));
        });
        req.write(body);
        req.end();
    });
}

module.exports = async function (context, req) {
    try {
        if (req.method === 'GET') {
            // Return inbox as empty collection (inbox contents are not publicly visible)
            const emptyInbox = {
                "@context": "https://www.w3.org/ns/activitystreams",
                "id": "https://lqdev.me/api/activitypub/inbox",
                "type": "OrderedCollection",
                "totalItems": 0,
                "orderedItems": []
            };
            
            context.res = {
                status: 200,
                headers: {
                    'Content-Type': 'application/activity+json',
                    'Access-Control-Allow-Origin': '*'
                },
                body: JSON.stringify(emptyInbox, null, 2)
            };
            return;
        }

        if (req.method === 'POST') {
            // Define logging helper first (before any usage)
            const logBoth = (message) => {
                console.log(message);
                context.log(message);
            };
            
            const activityData = req.body;
            const activityType = activityData.type;
            const timestamp = new Date().toISOString();
            
            logBoth(`=== Received Activity ===`);
            logBoth(`[Activity] Type: ${activityType}`);
            logBoth(`[Activity] From: ${activityData.actor}`);
            logBoth(`[Activity] ID: ${activityData.id || 'NO ID'}`);
            
            // ====================================================================
            // PHASE 1: HTTP SIGNATURE DIAGNOSTIC LOGGING
            // ====================================================================
            // Capture exact request values to diagnose signature verification
            // Related: feature/http-signature-verification
            // Using both console.log and context.log for Azure Static Web Apps compatibility
            
            logBoth('=== HTTP Signature Debug Info ===');
            logBoth(`[HTTP Sig Debug] req.url: ${req.url}`);
            logBoth(`[HTTP Sig Debug] req.method: ${req.method}`);
            logBoth(`[HTTP Sig Debug] x-ms-original-url: ${req.headers['x-ms-original-url'] || 'NOT PRESENT'}`);
            logBoth(`[HTTP Sig Debug] Host header: ${req.headers['host']}`);
            logBoth(`[HTTP Sig Debug] Date header: ${req.headers['date']}`);
            logBoth(`[HTTP Sig Debug] Digest header: ${req.headers['digest']}`);
            logBoth(`[HTTP Sig Debug] Signature header: ${req.headers['signature'] ? req.headers['signature'].substring(0, 100) + '...' : 'NOT PRESENT'}`);
            
            // Parse and log request body details with digest verification
            // Wrapped in try-catch to prevent diagnostic code from breaking production
            try {
                // Prefer rawBody to avoid JSON re-serialization differences
                // Azure Functions may parse JSON, causing byte-level differences
                let bodyForDigest;
                if (req.rawBody && (typeof req.rawBody === 'string' || Buffer.isBuffer(req.rawBody))) {
                    bodyForDigest = req.rawBody;
                    logBoth('[HTTP Sig Debug] Using req.rawBody for digest verification');
                } else if (typeof req.body === 'string') {
                    bodyForDigest = req.body;
                    logBoth('[HTTP Sig Debug] Using string req.body for digest verification');
                } else {
                    bodyForDigest = JSON.stringify(req.body);
                    logBoth('[HTTP Sig Debug] Using JSON.stringify(req.body) for digest verification (rawBody not available)');
                }
                
                const bodyLength = Buffer.isBuffer(bodyForDigest)
                    ? bodyForDigest.length
                    : String(bodyForDigest || '').length;
                logBoth(`[HTTP Sig Debug] Request body length: ${bodyLength} bytes`);
                
                // If Digest header present, verify it matches body
                if (req.headers['digest']) {
                    const bodyBuffer = Buffer.isBuffer(bodyForDigest)
                        ? bodyForDigest
                        : Buffer.from(String(bodyForDigest || ''), 'utf8');
                    const computedDigest = crypto.createHash('sha256').update(bodyBuffer).digest('base64');
                    const expectedDigest = `SHA-256=${computedDigest}`;
                    const digestMatch = req.headers['digest'] === expectedDigest;
                    logBoth(`[HTTP Sig Debug] Digest verification: ${digestMatch ? 'MATCH ✅' : 'MISMATCH ❌'}`);
                    if (!digestMatch) {
                        logBoth(`[HTTP Sig Debug]   Expected: ${expectedDigest}`);
                        logBoth(`[HTTP Sig Debug]   Received: ${req.headers['digest']}`);
                    }
                }
            } catch (error) {
                logBoth(`[HTTP Sig Debug] ⚠️  Error during diagnostic digest computation: ${error.message}`);
            }
            logBoth('=== End HTTP Signature Debug Info ===');
            
            // PHASE 5: Verify HTTP signature with feature flag
            const hasSignature = req.headers['signature'];
            if (hasSignature) {
                logBoth('[Phase 5] Signature header present - calling verifyHttpSignatureWithFeatureFlag...');
                const isValidSignature = await verifyHttpSignatureWithFeatureFlag(req, context);
                if (!isValidSignature) {
                    logBoth('[Phase 5] ❌ Signature verification FAILED - rejecting activity');
                    context.res = {
                        status: 401,
                        headers: { 'Content-Type': 'application/json' },
                        body: { error: 'Invalid HTTP signature' }
                    };
                    return;
                }
                logBoth('[Phase 5] ✅ Signature verification PASSED');
            } else {
                logBoth('[Phase 5] ⚠️  No Signature header present - accepting unsigned request (for backward compatibility)');
            }
            
            // Log activity to file for debugging
            const activitiesDir = path.join(__dirname, '../data/activities');
            try {
                await fs.mkdir(activitiesDir, { recursive: true });
                const logFile = path.join(activitiesDir, `${timestamp.replace(/[:.]/g, '-')}.json`);
                await fs.writeFile(logFile, JSON.stringify(activityData, null, 2));
            } catch (logError) {
                context.log.warn(`Failed to log activity: ${logError.message}`);
            }
            
            // Process activity based on type
            if (activityType === 'Follow') {
                // Handle Follow activity
                const followerActor = activityData.actor;
                const followActivityId = activityData.id;
                
                // Check for duplicate Follow (idempotency)
                const isAlreadyFollower = await tableStorage.isFollower(followerActor);
                
                if (isAlreadyFollower) {
                    context.log(`Already following: ${followerActor} - idempotent request`);
                    // Still return success for idempotency
                    context.res = {
                        status: 202,
                        headers: { 'Content-Type': 'application/json' },
                        body: { message: 'Already following' }
                    };
                    return;
                }
                
                // Fetch follower profile to get inbox and display name
                let inbox = null;
                let displayName = null;
                try {
                    const actorProfile = await fetchActorProfile(followerActor);
                    inbox = actorProfile.inbox;
                    displayName = actorProfile.name || actorProfile.preferredUsername;
                } catch (error) {
                    context.log.error(`Failed to fetch actor profile: ${error.message}`);
                    // Continue anyway, we'll try to get inbox later during Accept delivery
                }
                
                // Add to Table Storage
                await tableStorage.addFollower(followerActor, inbox, followActivityId, displayName);
                context.log(`Added follower to Table Storage: ${followerActor}`);
                
                // Queue Accept activity for GitHub Actions delivery (Free tier architecture)
                // Note: Free tier doesn't support managed identities for Key Vault signing
                // Accept activities are signed and delivered by GitHub Actions workflow
                try {
                    const acceptId = await tableStorage.queueAcceptActivity(activityData, followerActor, inbox);
                    context.log(`Queued Accept activity for delivery: ${acceptId}`);
                    
                    context.res = {
                        status: 202,
                        headers: { 'Content-Type': 'application/json' },
                        body: { 
                            message: 'Follow accepted, Accept queued for delivery',
                            acceptId: acceptId
                        }
                    };
                } catch (queueError) {
                    context.log.error(`Failed to queue Accept activity: ${queueError.message}`);
                    context.res = {
                        status: 202,
                        headers: { 'Content-Type': 'application/json' },
                        body: { message: 'Follow accepted but failed to queue Accept activity' }
                    };
                }
                return;
            }
            
            if (activityType === 'Undo') {
                // Handle Undo activity (typically Undo Follow = Unfollow)
                logBoth(`[Undo Debug] Processing Undo activity from ${activityData.actor}`);
                const object = activityData.object;
                logBoth(`[Undo Debug] Object type: ${object ? object.type : 'null'}`);
                logBoth(`[Undo Debug] Object actor: ${object && object.actor ? object.actor : 'null'}`);
                
                if (object && object.type === 'Follow') {
                    const followerActor = activityData.actor;
                    logBoth(`[Undo Debug] Attempting to remove follower: ${followerActor}`);
                    
                    // Remove from Table Storage
                    const removed = await tableStorage.removeFollower(followerActor);
                    
                    if (removed) {
                        logBoth(`[Undo Debug] ✅ Successfully removed follower from Table Storage: ${followerActor}`);
                    } else {
                        logBoth(`[Undo Debug] ⚠️  Follower not found in Table Storage: ${followerActor}`);
                    }
                    
                    context.res = {
                        status: 202,
                        headers: { 'Content-Type': 'application/json' },
                        body: { message: 'Unfollow processed' }
                    };
                    return;
                } else {
                    logBoth(`[Undo Debug] ⚠️  Undo activity received but object type is not Follow (${object ? object.type : 'null'})`);
                }
            }
            
            // Other activity types are logged but not processed yet
            context.log(`Activity type ${activityType} not implemented yet`);
            context.res = {
                status: 202,
                headers: { 'Content-Type': 'application/json' },
                body: { message: 'Activity received' }
            };
            return;
        }

        // Method not allowed
        context.res = {
            status: 405,
            headers: { 'Content-Type': 'application/json' },
            body: { error: 'Method not allowed' }
        };
    } catch (error) {
        context.log.error(`Inbox error: ${error.message}`);
        context.log.error(error.stack);
        context.res = {
            status: 500,
            headers: { 'Content-Type': 'application/json' },
            body: { error: 'Internal server error' }
        };
    }
};