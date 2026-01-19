const { QueueClient } = require('@azure/storage-queue');

/**
 * Queue Storage utility for ActivityPub async activity delivery
 * 
 * Queues:
 * - accept-delivery: Accept activities to be delivered to new followers
 * 
 * Configuration via environment variables:
 * - ACTIVITYPUB_STORAGE_CONNECTION: Connection string for Queue Storage
 */

let acceptQueueClient = null;

/**
 * Initialize Queue Storage clients
 */
function initializeClients() {
    if (acceptQueueClient) {
        return; // Already initialized
    }

    const connectionString = process.env.ACTIVITYPUB_STORAGE_CONNECTION;
    
    if (!connectionString) {
        throw new Error('ACTIVITYPUB_STORAGE_CONNECTION environment variable not set');
    }

    // Initialize accept-delivery queue client
    acceptQueueClient = new QueueClient(
        connectionString,
        'accept-delivery'
    );
}

/**
 * Ensure queue exists (create if not)
 */
async function ensureQueueExists() {
    initializeClients();
    
    try {
        await acceptQueueClient.create();
    } catch (error) {
        // Ignore if queue already exists
        if (error.statusCode !== 409) {
            throw error;
        }
    }
}

/**
 * Queue an Accept activity for delivery
 * @param {Object} acceptActivity - Accept activity to deliver
 * @param {string} targetInbox - Follower's inbox URL
 * @param {string} followerActorUrl - Follower's actor URL
 * @returns {Promise<void>}
 */
async function queueAcceptDelivery(acceptActivity, targetInbox, followerActorUrl) {
    await ensureQueueExists();

    const message = {
        acceptActivity: acceptActivity,
        targetInbox: targetInbox,
        followerActorUrl: followerActorUrl,
        queuedAt: new Date().toISOString()
    };

    const messageText = Buffer.from(JSON.stringify(message)).toString('base64');
    
    try {
        await acceptQueueClient.sendMessage(messageText);
    } catch (error) {
        throw new Error(`Failed to queue Accept delivery: ${error.message}`);
    }
}

module.exports = {
    queueAcceptDelivery
};
