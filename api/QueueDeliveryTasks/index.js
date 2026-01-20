const fs = require('fs').promises;
const path = require('path');
const tableStorage = require('../utils/tableStorage');
const queueStorage = require('../utils/queueStorage');

/**
 * QueueDeliveryTasks Azure Function
 * 
 * Trigger: HTTP POST /api/activitypub/trigger-delivery
 * Purpose: Queue delivery tasks for new content to all followers
 * 
 * Request body:
 * {
 *   "activityIds": ["https://lqdev.me/api/activitypub/notes/abc123", ...]
 * }
 * 
 * Response:
 * {
 *   "success": true,
 *   "totalFollowers": 10,
 *   "tasksQueued": 10,
 *   "activitiesProcessed": 1
 * }
 */

/**
 * Load activity from outbox by ID
 * @param {string} activityId - Activity ID URL
 * @returns {Promise<Object|null>} Activity object or null if not found
 */
async function loadActivityFromOutbox(activityId) {
    try {
        // Load outbox index
        const outboxPath = path.join(__dirname, '../data/outbox/index.json');
        const outboxData = await fs.readFile(outboxPath, 'utf8');
        const outbox = JSON.parse(outboxData);

        // Find activity by ID
        const activity = outbox.orderedItems.find(item => {
            return item.id === activityId || 
                   (item.object && item.object.id === activityId);
        });

        return activity || null;
    } catch (error) {
        console.error(`Failed to load activity ${activityId}: ${error.message}`);
        return null;
    }
}

/**
 * Validate inbox URL for SSRF protection
 * @param {string} inboxUrl - Inbox URL to validate
 * @returns {boolean} True if URL is valid
 */
function isValidInboxUrl(inboxUrl) {
    try {
        const url = new URL(inboxUrl);
        
        // Only allow HTTPS
        if (url.protocol !== 'https:') {
            return false;
        }
        
        // Block private IP ranges
        const hostname = url.hostname;
        if (
            hostname === 'localhost' ||
            hostname === '127.0.0.1' ||
            hostname.startsWith('192.168.') ||
            hostname.startsWith('10.') ||
            hostname.startsWith('172.16.') ||
            hostname.startsWith('172.17.') ||
            hostname.startsWith('172.18.') ||
            hostname.startsWith('172.19.') ||
            hostname.startsWith('172.2') ||
            hostname.startsWith('172.3') ||
            hostname === '::1'
        ) {
            return false;
        }
        
        return true;
    } catch (error) {
        return false;
    }
}

module.exports = async function (context, req) {
    try {
        context.log('QueueDeliveryTasks function triggered');

        // Parse request body
        const requestBody = req.body;
        const activityIds = requestBody.activityIds || [];

        if (!Array.isArray(activityIds) || activityIds.length === 0) {
            context.res = {
                status: 400,
                headers: { 'Content-Type': 'application/json' },
                body: { 
                    error: 'Invalid request',
                    message: 'activityIds array is required and must not be empty'
                }
            };
            return;
        }

        context.log(`Processing ${activityIds.length} activity IDs`);

        // Get all followers from Table Storage
        const followers = await tableStorage.getAllFollowers();
        context.log(`Found ${followers.length} followers`);

        if (followers.length === 0) {
            context.res = {
                status: 200,
                headers: { 'Content-Type': 'application/json' },
                body: {
                    success: true,
                    message: 'No followers to deliver to',
                    totalFollowers: 0,
                    tasksQueued: 0,
                    activitiesProcessed: activityIds.length
                }
            };
            return;
        }

        let totalTasksQueued = 0;
        let activitiesProcessed = 0;

        // Process each activity ID
        for (const activityId of activityIds) {
            context.log(`Processing activity: ${activityId}`);

            // Load activity from outbox
            const activity = await loadActivityFromOutbox(activityId);
            
            if (!activity) {
                context.log.warn(`Activity not found in outbox: ${activityId}`);
                continue;
            }

            const activityJson = JSON.stringify(activity);
            activitiesProcessed++;

            // Queue delivery task for each follower
            const tasks = [];
            for (const follower of followers) {
                const inbox = follower.inbox;
                
                // Validate inbox URL (SSRF protection)
                if (!isValidInboxUrl(inbox)) {
                    context.log.warn(`Invalid inbox URL for ${follower.actorUrl}: ${inbox}`);
                    continue;
                }

                tasks.push({
                    activityId: activityId,
                    activityJson: activityJson,
                    targetInbox: inbox,
                    followerActor: follower.actorUrl
                });
            }

            // Queue all tasks for this activity
            const queued = await queueStorage.queueDeliveryTasks(tasks);
            totalTasksQueued += queued;
            
            context.log(`Queued ${queued} delivery tasks for activity ${activityId}`);
        }

        // Return success response
        context.res = {
            status: 200,
            headers: { 'Content-Type': 'application/json' },
            body: {
                success: true,
                totalFollowers: followers.length,
                tasksQueued: totalTasksQueued,
                activitiesProcessed: activitiesProcessed,
                message: `Successfully queued ${totalTasksQueued} delivery tasks for ${activitiesProcessed} activities`
            }
        };

    } catch (error) {
        context.log.error(`QueueDeliveryTasks error: ${error.message}`);
        context.log.error(error.stack);
        
        context.res = {
            status: 500,
            headers: { 'Content-Type': 'application/json' },
            body: {
                error: 'Internal server error',
                message: error.message
            }
        };
    }
};
