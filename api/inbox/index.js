const fs = require('fs').promises;
const path = require('path');
const https = require('https');
const { verifyHttpSignature } = require('../utils/signatures');
const { addFollower, removeFollower, isFollower } = require('../utils/followers');

/**
 * Send Accept activity in response to Follow
 */
async function sendAcceptActivity(followActivity, context) {
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
        
        // Send Accept activity to follower's inbox
        await sendActivityToInbox(inboxUrl, acceptActivity, context);
        context.log(`Sent Accept activity to ${inboxUrl}`);
        return true;
    } catch (error) {
        context.log.error(`Failed to send Accept activity: ${error.message}`);
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
            const activityData = req.body;
            const activityType = activityData.type;
            const timestamp = new Date().toISOString();
            
            context.log(`Received activity: ${activityType} from ${activityData.actor}`);
            
            // Verify HTTP signature (optional for now, will be enforced in Phase 2.2)
            const hasSignature = req.headers['signature'];
            if (hasSignature) {
                const isValidSignature = await verifyHttpSignature(req, context);
                if (!isValidSignature) {
                    context.log.warn('Invalid signature - rejecting activity');
                    context.res = {
                        status: 401,
                        headers: { 'Content-Type': 'application/json' },
                        body: { error: 'Invalid signature' }
                    };
                    return;
                }
                context.log('Signature verified successfully');
            } else {
                context.log.warn('No signature present - accepting anyway (development mode)');
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
                
                // Add to followers list
                await addFollower(followerActor);
                context.log(`Added follower: ${followerActor}`);
                
                // Send Accept activity
                const acceptSent = await sendAcceptActivity(activityData, context);
                
                if (acceptSent) {
                    context.res = {
                        status: 202,
                        headers: { 'Content-Type': 'application/json' },
                        body: { message: 'Follow accepted' }
                    };
                } else {
                    context.res = {
                        status: 202,
                        headers: { 'Content-Type': 'application/json' },
                        body: { message: 'Follow accepted but failed to send Accept activity' }
                    };
                }
                return;
            }
            
            if (activityType === 'Undo') {
                // Handle Undo activity (typically Undo Follow = Unfollow)
                const object = activityData.object;
                
                if (object && object.type === 'Follow') {
                    const followerActor = activityData.actor;
                    
                    // Remove from followers list
                    await removeFollower(followerActor);
                    context.log(`Removed follower: ${followerActor}`);
                    
                    context.res = {
                        status: 202,
                        headers: { 'Content-Type': 'application/json' },
                        body: { message: 'Unfollow processed' }
                    };
                    return;
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