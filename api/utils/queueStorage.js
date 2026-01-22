const { QueueClient } = require('@azure/storage-queue');

/**
 * Queue Storage utility for ActivityPub delivery queue management
 * 
 * Queues:
 * - activitypub-delivery: Queue for delivering Create activities to follower inboxes
 * 
 * Configuration via environment variables:
 * - ACTIVITYPUB_STORAGE_CONNECTION: Connection string for Queue Storage
 */

let queueClients = {};

/**
 * Get or create a queue client for the specified queue
 * @param {string} queueName - Name of the queue
 * @returns {QueueClient} Queue client instance
 */
function getQueueClient(queueName) {
    if (queueClients[queueName]) {
        return queueClients[queueName];
    }

    const connectionString = process.env.ACTIVITYPUB_STORAGE_CONNECTION;
    
    if (!connectionString) {
        throw new Error('ACTIVITYPUB_STORAGE_CONNECTION environment variable not set');
    }

    const client = new QueueClient(connectionString, queueName);
    queueClients[queueName] = client;
    
    return client;
}

/**
 * Ensure queue exists (create if not)
 * @param {QueueClient} client - Queue client
 */
async function ensureQueueExists(client) {
    try {
        await client.createIfNotExists();
    } catch (error) {
        // Ignore if queue already exists
        if (error.statusCode !== 409) {
            throw error;
        }
    }
}

/**
 * Queue a delivery task for an activity to a follower inbox
 * @param {string} activityId - Activity ID (e.g., URL of the activity)
 * @param {string} activityJson - JSON string of the complete Create activity
 * @param {string} targetInbox - Follower's inbox URL
 * @param {string} followerActor - Follower's actor URL
 * @returns {Promise<string>} Message ID
 */
async function queueDeliveryTask(activityId, activityJson, targetInbox, followerActor) {
    const client = getQueueClient('activitypub-delivery');
    await ensureQueueExists(client);

    const message = {
        activityId: activityId,
        activityJson: activityJson,
        targetInbox: targetInbox,
        followerActor: followerActor,
        attemptCount: 0,
        queuedAt: new Date().toISOString()
    };

    const messageText = JSON.stringify(message);
    
    try {
        const response = await client.sendMessage(Buffer.from(messageText).toString('base64'));
        return response.messageId;
    } catch (error) {
        throw new Error(`Failed to queue delivery task: ${error.message}`);
    }
}

/**
 * Queue multiple delivery tasks (bulk operation)
 * @param {Array<Object>} tasks - Array of task objects with activityId, activityJson, targetInbox, followerActor
 * @returns {Promise<Object>} Object with successCount and failedTasks array
 */
async function queueDeliveryTasks(tasks) {
    const client = getQueueClient('activitypub-delivery');
    await ensureQueueExists(client);

    let successCount = 0;
    const failedTasks = [];
    
    for (const task of tasks) {
        try {
            await queueDeliveryTask(
                task.activityId,
                task.activityJson,
                task.targetInbox,
                task.followerActor
            );
            successCount++;
        } catch (error) {
            console.error(`Failed to queue task for ${task.followerActor}: ${error.message}`);
            failedTasks.push({
                followerActor: task.followerActor,
                targetInbox: task.targetInbox,
                error: error.message
            });
        }
    }

    return {
        successCount,
        failedTasks
    };
}

/**
 * Get approximate queue length
 * @param {string} queueName - Name of the queue
 * @returns {Promise<number>} Approximate number of messages in queue
 */
async function getQueueLength(queueName) {
    const client = getQueueClient(queueName);
    
    try {
        const properties = await client.getProperties();
        return properties.approximateMessagesCount || 0;
    } catch (error) {
        throw new Error(`Failed to get queue length: ${error.message}`);
    }
}

/**
 * Receive messages from queue for processing (GitHub Actions worker pattern)
 * @param {string} queueName - Name of the queue
 * @param {number} maxMessages - Maximum number of messages to receive (1-32)
 * @param {number} visibilityTimeout - How long messages are hidden after retrieval (in seconds, default 300)
 * @returns {Promise<Array>} Array of message objects with messageId, messageText, popReceipt
 */
async function receiveMessages(queueName, maxMessages = 32, visibilityTimeout = 300) {
    const client = getQueueClient(queueName);
    
    try {
        const response = await client.receiveMessages({
            numberOfMessages: Math.min(Math.max(1, maxMessages), 32),
            visibilityTimeout: visibilityTimeout
        });
        
        return response.receivedMessageItems.map(msg => ({
            messageId: msg.messageId,
            messageText: Buffer.from(msg.messageText, 'base64').toString('utf8'),
            popReceipt: msg.popReceipt,
            dequeueCount: msg.dequeueCount,
            insertedOn: msg.insertedOn,
            expiresOn: msg.expiresOn,
            nextVisibleOn: msg.nextVisibleOn
        }));
    } catch (error) {
        throw new Error(`Failed to receive messages: ${error.message}`);
    }
}

/**
 * Delete a message from the queue after successful processing
 * @param {string} queueName - Name of the queue
 * @param {string} messageId - Message ID
 * @param {string} popReceipt - Pop receipt from receiveMessages
 * @returns {Promise<void>}
 */
async function deleteMessage(queueName, messageId, popReceipt) {
    const client = getQueueClient(queueName);
    
    try {
        await client.deleteMessage(messageId, popReceipt);
    } catch (error) {
        throw new Error(`Failed to delete message: ${error.message}`);
    }
}

module.exports = {
    queueDeliveryTask,
    queueDeliveryTasks,
    getQueueLength,
    receiveMessages,
    deleteMessage,
    getQueueClient,
    ensureQueueExists
};
