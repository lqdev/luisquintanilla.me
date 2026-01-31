const { TableClient, AzureNamedKeyCredential } = require('@azure/data-tables');
const { DefaultAzureCredential } = require('@azure/identity');
const crypto = require('crypto');

// Constants for delivery queue status tracking
const DELIVERY_STATUS = {
    PENDING: 'pending',
    COMPLETED: 'completed',
    FAILED: 'failed'
};

/**
 * Table Storage utility for ActivityPub follower management
 * 
 * Tables:
 * - followers: Stores follower actor information
 * - pendingaccepts: Stores Accept activities waiting for GitHub Actions delivery
 * - deliverystatus: Stores delivery status for post distribution
 * 
 * Configuration via environment variables:
 * - ACTIVITYPUB_STORAGE_CONNECTION: Connection string for Table Storage
 * - AZURE_STORAGE_ACCOUNT_NAME: Storage account name (alternative auth)
 */

let followersClient = null;
let pendingAcceptsClient = null;

/**
 * Convert string to URL-safe base64 encoding for use as Table Storage RowKey
 * Implements RFC 4648 base64url encoding
 * @param {string} str - String to encode
 * @returns {string} URL-safe base64 encoded string
 */
/**
 * Convert string to URL-safe hash for use as table storage key
 * Azure Table Storage RowKey restrictions: no /, \, #, or ? characters
 * Using SHA-256 hash ensures consistent, valid keys
 */
function toUrlSafeBase64(str) {
    const hash = crypto.createHash('sha256').update(str).digest('hex');
    return hash; // Hex is always safe for Azure Table Storage
}

/**
 * Initialize Table Storage clients
 * Supports both connection string (production) and managed identity (future)
 */
function initializeClients() {
    if (followersClient) {
        return; // Already initialized
    }

    const connectionString = process.env.ACTIVITYPUB_STORAGE_CONNECTION;
    
    if (!connectionString) {
        throw new Error('ACTIVITYPUB_STORAGE_CONNECTION environment variable not set');
    }

    // Initialize followers table client
    followersClient = TableClient.fromConnectionString(
        connectionString,
        'followers'
    );

    // Initialize pending accepts table client
    pendingAcceptsClient = TableClient.fromConnectionString(
        connectionString,
        'pendingaccepts'
    );
}

/**
 * Ensure table exists (create if not)
 * @param {TableClient} client - Table client to check
 */
async function ensureTableExists(client) {
    try {
        await client.createTable();
    } catch (error) {
        // Ignore if table already exists
        if (error.statusCode !== 409) {
            throw error;
        }
    }
}

/**
 * Add a follower to Table Storage
 * @param {string} actorUrl - Follower's actor URL
 * @param {string} inbox - Follower's inbox URL
 * @param {string} followActivityId - Original Follow activity ID
 * @param {string} displayName - Optional display name
 * @param {string} sharedInbox - Optional shared inbox URL (for delivery optimization)
 * @returns {Promise<void>}
 */
async function addFollower(actorUrl, inbox, followActivityId, displayName = null, sharedInbox = null) {
    initializeClients();
    await ensureTableExists(followersClient);

    const entity = {
        partitionKey: 'follower',
        rowKey: toUrlSafeBase64(actorUrl),
        actorUrl: actorUrl,
        inbox: inbox || '',
        sharedInbox: sharedInbox || '',  // Phase 4D: Shared inbox for delivery optimization
        followedAt: new Date().toISOString(),
        displayName: displayName || '',
        followActivityId: followActivityId
    };

    try {
        // Use upsert to handle both new followers and updates
        await followersClient.upsertEntity(entity, 'Merge');
    } catch (error) {
        throw new Error(`Failed to add follower: ${error.message}`);
    }
}

/**
 * Remove a follower from Table Storage
 * @param {string} actorUrl - Follower's actor URL
 * @returns {Promise<boolean>} True if follower was removed, false if not found
 */
async function removeFollower(actorUrl) {
    initializeClients();

    const rowKey = toUrlSafeBase64(actorUrl);
    
    try {
        await followersClient.deleteEntity('follower', rowKey);
        return true;
    } catch (error) {
        if (error.statusCode === 404) {
            return false; // Follower not found
        }
        throw new Error(`Failed to remove follower: ${error.message}`);
    }
}

/**
 * Check if an actor is following
 * @param {string} actorUrl - Actor URL to check
 * @returns {Promise<boolean>}
 */
async function isFollower(actorUrl) {
    initializeClients();

    const rowKey = toUrlSafeBase64(actorUrl);
    
    try {
        await followersClient.getEntity('follower', rowKey);
        return true;
    } catch (error) {
        if (error.statusCode === 404) {
            return false;
        }
        throw new Error(`Failed to check follower: ${error.message}`);
    }
}

/**
 * Get follower details
 * @param {string} actorUrl - Actor URL
 * @returns {Promise<Object|null>} Follower entity or null if not found
 */
async function getFollower(actorUrl) {
    initializeClients();

    const rowKey = toUrlSafeBase64(actorUrl);
    
    try {
        const entity = await followersClient.getEntity('follower', rowKey);
        return {
            actorUrl: entity.actorUrl,
            inbox: entity.inbox,
            sharedInbox: entity.sharedInbox || null,  // Phase 4D: Shared inbox for delivery optimization
            followedAt: entity.followedAt,
            displayName: entity.displayName,
            followActivityId: entity.followActivityId
        };
    } catch (error) {
        if (error.statusCode === 404) {
            return null;
        }
        throw new Error(`Failed to get follower: ${error.message}`);
    }
}

/**
 * Update a follower's details in Table Storage
 * Used for migration scripts to backfill sharedInbox and other fields
 * @param {string} actorUrl - Follower's actor URL
 * @param {Object} updates - Object containing fields to update
 * @returns {Promise<boolean>} True if follower was updated, false if not found
 */
async function updateFollower(actorUrl, updates) {
    initializeClients();

    const rowKey = toUrlSafeBase64(actorUrl);
    
    try {
        // Get existing entity
        const entity = await followersClient.getEntity('follower', rowKey);
        
        // Merge updates into entity
        if (updates.inbox !== undefined) entity.inbox = updates.inbox;
        if (updates.sharedInbox !== undefined) entity.sharedInbox = updates.sharedInbox || '';
        if (updates.displayName !== undefined) entity.displayName = updates.displayName || '';
        
        // Use merge update to preserve other fields
        await followersClient.updateEntity(entity, 'Merge');
        return true;
    } catch (error) {
        if (error.statusCode === 404) {
            return false; // Follower not found
        }
        throw new Error(`Failed to update follower: ${error.message}`);
    }
}

/**
 * Get all followers from Table Storage
 * @returns {Promise<Array>} Array of follower entities
 */
async function getAllFollowers() {
    initializeClients();

    const followers = [];
    const entities = followersClient.listEntities({
        queryOptions: { filter: "PartitionKey eq 'follower'" }
    });

    for await (const entity of entities) {
        followers.push({
            actorUrl: entity.actorUrl,
            inbox: entity.inbox,
            sharedInbox: entity.sharedInbox || null,  // Phase 4D: Shared inbox for delivery optimization
            followedAt: entity.followedAt,
            displayName: entity.displayName,
            followActivityId: entity.followActivityId
        });
    }

    return followers;
}

/**
 * Get follower count
 * @returns {Promise<number>}
 */
async function getFollowerCount() {
    const followers = await getAllFollowers();
    return followers.length;
}

/**
 * Build ActivityPub followers collection from Table Storage
 * Used for /api/activitypub/followers endpoint
 * @returns {Promise<Object>} ActivityPub OrderedCollection
 */
async function buildFollowersCollection() {
    const followers = await getAllFollowers();
    
    return {
        "@context": "https://www.w3.org/ns/activitystreams",
        "id": "https://lqdev.me/api/activitypub/followers",
        "type": "OrderedCollection",
        "totalItems": followers.length,
        "orderedItems": followers.map(f => f.actorUrl)
    };
}

/**
 * Queue an Accept activity for delivery by GitHub Actions
 * @param {Object} followActivity - Original Follow activity
 * @param {string} actorUrl - Follower's actor URL
 * @param {string} inbox - Follower's inbox URL
 * @returns {Promise<string>} Accept activity ID
 */
async function queueAcceptActivity(followActivity, actorUrl, inbox) {
    initializeClients();
    await ensureTableExists(pendingAcceptsClient);

    const acceptId = `accept-${Date.now()}-${crypto.randomBytes(4).toString('hex')}`;
    const acceptActivity = {
        "@context": "https://www.w3.org/ns/activitystreams",
        "id": `https://lqdev.me/api/activitypub/activities/${acceptId}`,
        "type": "Accept",
        "actor": "https://lqdev.me/api/activitypub/actor",
        "object": followActivity
    };

    const entity = {
        partitionKey: 'pending',
        rowKey: acceptId,
        actorUrl: actorUrl,
        inbox: inbox,
        followActivityId: followActivity.id || '',
        acceptActivity: JSON.stringify(acceptActivity),
        queuedAt: new Date().toISOString(),
        status: 'pending', // pending, delivered, failed
        retryCount: 0
    };

    try {
        await pendingAcceptsClient.createEntity(entity);
        return acceptId;
    } catch (error) {
        throw new Error(`Failed to queue Accept activity: ${error.message}`);
    }
}

/**
 * Get all pending Accept activities
 * @returns {Promise<Array>} Array of pending Accept entities
 */
async function getPendingAccepts() {
    initializeClients();

    const pending = [];
    const entities = pendingAcceptsClient.listEntities({
        queryOptions: { filter: "PartitionKey eq 'pending' and status eq 'pending'" }
    });

    for await (const entity of entities) {
        pending.push({
            acceptId: entity.rowKey,
            actorUrl: entity.actorUrl,
            inbox: entity.inbox,
            followActivityId: entity.followActivityId,
            acceptActivity: JSON.parse(entity.acceptActivity),
            queuedAt: entity.queuedAt,
            retryCount: entity.retryCount || 0
        });
    }

    return pending;
}

/**
 * Mark Accept activity as delivered
 * @param {string} acceptId - Accept activity ID
 * @returns {Promise<void>}
 */
async function markAcceptDelivered(acceptId) {
    initializeClients();

    try {
        const entity = await pendingAcceptsClient.getEntity('pending', acceptId);
        entity.status = 'delivered';
        entity.deliveredAt = new Date().toISOString();
        await pendingAcceptsClient.updateEntity(entity, 'Replace');
    } catch (error) {
        throw new Error(`Failed to mark Accept as delivered: ${error.message}`);
    }
}

/**
 * Mark Accept activity as failed
 * @param {string} acceptId - Accept activity ID
 * @param {string} errorMessage - Error message
 * @returns {Promise<void>}
 */
async function markAcceptFailed(acceptId, errorMessage) {
    initializeClients();

    try {
        const entity = await pendingAcceptsClient.getEntity('pending', acceptId);
        entity.status = 'failed';
        entity.errorMessage = errorMessage;
        entity.failedAt = new Date().toISOString();
        entity.retryCount = (entity.retryCount || 0) + 1;
        await pendingAcceptsClient.updateEntity(entity, 'Replace');
    } catch (error) {
        throw new Error(`Failed to mark Accept as failed: ${error.message}`);
    }
}

/**
 * Delivery status tracking
 */

let deliveryStatusClient = null;

/**
 * Initialize delivery status table client
 */
function initializeDeliveryStatusClient() {
    if (deliveryStatusClient) {
        return;
    }

    const connectionString = process.env.ACTIVITYPUB_STORAGE_CONNECTION;
    
    if (!connectionString) {
        throw new Error('ACTIVITYPUB_STORAGE_CONNECTION environment variable not set');
    }

    deliveryStatusClient = TableClient.fromConnectionString(
        connectionString,
        'deliverystatus'
    );
}

/**
 * Add delivery status entry
 * @param {string} activityId - Activity ID
 * @param {string} targetInbox - Target inbox URL
 * @param {string} followerActor - Follower actor URL
 * @param {string} status - Status (pending/delivered/failed)
 * @param {number} httpStatusCode - HTTP status code (optional)
 * @param {string} errorMessage - Error message (optional)
 * @returns {Promise<void>}
 */
async function addDeliveryStatus(activityId, targetInbox, followerActor, status, httpStatusCode = null, errorMessage = null) {
    initializeDeliveryStatusClient();
    await ensureTableExists(deliveryStatusClient);

    // Use URL-safe encoding for rowKey
    const rowKey = toUrlSafeBase64(targetInbox);
    
    // Sanitize error message - Azure Table Storage doesn't like certain characters
    let sanitizedError = errorMessage || '';
    if (sanitizedError) {
        // Remove control characters and limit length
        sanitizedError = sanitizedError
            .replace(/[\x00-\x1F\x7F]/g, ' ') // Remove control chars
            .substring(0, 1000); // Limit length
    }

    const entity = {
        partitionKey: activityId,
        rowKey: rowKey,
        activityId: activityId,
        targetInbox: targetInbox,
        followerActor: followerActor,
        status: status,
        attemptCount: 1,
        lastAttempt: new Date().toISOString(),
        httpStatusCode: httpStatusCode || 0,
        errorMessage: sanitizedError,
        deliveredAt: status === 'delivered' ? new Date().toISOString() : ''
    };

    try {
        await deliveryStatusClient.upsertEntity(entity, 'Merge');
    } catch (error) {
        throw new Error(`Failed to add delivery status: ${error.message}`);
    }
}

/**
 * Get delivery status
 * @param {string} activityId - Activity ID
 * @param {string} targetInbox - Target inbox URL
 * @returns {Promise<Object|null>} Delivery status entity or null
 */
async function getDeliveryStatus(activityId, targetInbox) {
    initializeDeliveryStatusClient();

    const rowKey = toUrlSafeBase64(targetInbox);

    try {
        const entity = await deliveryStatusClient.getEntity(activityId, rowKey);
        return {
            activityId: entity.activityId,
            targetInbox: entity.targetInbox,
            followerActor: entity.followerActor,
            status: entity.status,
            attemptCount: entity.attemptCount || 0,
            lastAttempt: entity.lastAttempt,
            httpStatusCode: entity.httpStatusCode || 0,
            errorMessage: entity.errorMessage || '',
            deliveredAt: entity.deliveredAt || null
        };
    } catch (error) {
        if (error.statusCode === 404) {
            return null;
        }
        throw new Error(`Failed to get delivery status: ${error.message}`);
    }
}

/**
 * Update delivery status
 * @param {string} activityId - Activity ID
 * @param {string} targetInbox - Target inbox URL
 * @param {string} status - Status (pending/delivered/failed)
 * @param {number} attemptCount - Attempt count
 * @param {number} httpStatusCode - HTTP status code (optional)
 * @param {string} errorMessage - Error message (optional)
 * @returns {Promise<void>}
 */
async function updateDeliveryStatus(activityId, targetInbox, status, attemptCount, httpStatusCode = null, errorMessage = null) {
    initializeDeliveryStatusClient();

    const rowKey = toUrlSafeBase64(targetInbox);

    try {
        const entity = await deliveryStatusClient.getEntity(activityId, rowKey);
        entity.status = status;
        entity.attemptCount = attemptCount;
        entity.lastAttempt = new Date().toISOString();
        if (httpStatusCode !== null) {
            entity.httpStatusCode = httpStatusCode;
        }
        if (errorMessage !== null) {
            entity.errorMessage = errorMessage;
        }
        if (status === 'delivered') {
            entity.deliveredAt = new Date().toISOString();
        }
        
        // Use optimistic concurrency control with ETag to prevent race conditions
        await deliveryStatusClient.updateEntity(entity, 'Merge', {
            etag: entity.etag
        });
    } catch (error) {
        throw new Error(`Failed to update delivery status: ${error.message}`);
    }
}

/**
 * Get all delivery statuses for an activity
 * @param {string} activityId - Activity ID
 * @returns {Promise<Array>} Array of delivery status entities
 */
async function getDeliveryStatusesForActivity(activityId) {
    initializeDeliveryStatusClient();

    const statuses = [];
    const entities = deliveryStatusClient.listEntities({
        queryOptions: { filter: `PartitionKey eq '${activityId}'` }
    });

    for await (const entity of entities) {
        statuses.push({
            activityId: entity.activityId,
            targetInbox: entity.targetInbox,
            followerActor: entity.followerActor,
            status: entity.status,
            attemptCount: entity.attemptCount || 0,
            lastAttempt: entity.lastAttempt,
            httpStatusCode: entity.httpStatusCode || 0,
            errorMessage: entity.errorMessage || '',
            deliveredAt: entity.deliveredAt || null
        });
    }

    return statuses;
}

/**
 * Post delivery queue management
 */

let deliveryQueueClient = null;

/**
 * Initialize delivery queue table client
 */
function initializeDeliveryQueueClient() {
    if (deliveryQueueClient) {
        return;
    }

    const connectionString = process.env.ACTIVITYPUB_STORAGE_CONNECTION;
    
    if (!connectionString) {
        throw new Error('ACTIVITYPUB_STORAGE_CONNECTION environment variable not set');
    }

    deliveryQueueClient = TableClient.fromConnectionString(
        connectionString,
        'deliveryqueue'
    );
}

/**
 * Queue a post (Create activity) for delivery to followers
 * @param {string} noteId - Note/post ID
 * @param {Object} createActivity - Create activity object
 * @returns {Promise<string>} Queue entry ID
 */
async function queuePostDelivery(noteId, createActivity) {
    initializeDeliveryQueueClient();
    await ensureTableExists(deliveryQueueClient);

    // Generate unique queue entry ID
    const queueId = `post-${Date.now()}-${crypto.randomBytes(4).toString('hex')}`;

    const entity = {
        partitionKey: DELIVERY_STATUS.PENDING,
        rowKey: queueId,
        noteId: noteId,
        createActivity: JSON.stringify(createActivity),
        queuedAt: new Date().toISOString(),
        status: DELIVERY_STATUS.PENDING, // pending, processing, completed, failed
        retryCount: 0
    };

    try {
        await deliveryQueueClient.createEntity(entity);
        return queueId;
    } catch (error) {
        console.error('Failed to queue post delivery', { error, entity });
        throw error;
    }
}

/**
 * Get all pending post deliveries
 * @returns {Promise<Array>} Array of pending delivery entities
 */
async function getPendingDeliveries() {
    initializeDeliveryQueueClient();
    await ensureTableExists(deliveryQueueClient);

    console.log(`🔍 Querying for: PartitionKey eq '${DELIVERY_STATUS.PENDING}' and status eq '${DELIVERY_STATUS.PENDING}'`);
    
    const pending = [];
    const entities = deliveryQueueClient.listEntities({
        queryOptions: { filter: `PartitionKey eq '${DELIVERY_STATUS.PENDING}' and status eq '${DELIVERY_STATUS.PENDING}'` }
    });

    for await (const entity of entities) {
        console.log('📦 Found entity:', { 
            partitionKey: entity.partitionKey, 
            rowKey: entity.rowKey, 
            noteId: entity.noteId,
            status: entity.status 
        });
        pending.push({
            queueId: entity.rowKey,
            noteId: entity.noteId,
            createActivity: JSON.parse(entity.createActivity),
            queuedAt: entity.queuedAt,
            retryCount: entity.retryCount || 0
        });
    }
    
    console.log(`📊 Total pending deliveries found: ${pending.length}`);

    return pending;
}

/**
 * Mark delivery as completed
 * @param {string} queueId - Queue entry ID
 * @returns {Promise<void>}
 */
async function markDeliveryCompleted(queueId) {
    initializeDeliveryQueueClient();
    await ensureTableExists(deliveryQueueClient);

    try {
        const entity = await deliveryQueueClient.getEntity(DELIVERY_STATUS.PENDING, queueId);
        entity.status = DELIVERY_STATUS.COMPLETED;
        entity.completedAt = new Date().toISOString();
        await deliveryQueueClient.updateEntity(entity, 'Replace');
    } catch (error) {
        throw new Error(`Failed to mark delivery completed: ${error.message}`);
    }
}

/**
 * Mark delivery as failed
 * @param {string} queueId - Queue entry ID
 * @param {string} errorMessage - Error message
 * @returns {Promise<void>}
 */
async function markDeliveryFailed(queueId, errorMessage) {
    initializeDeliveryQueueClient();
    await ensureTableExists(deliveryQueueClient);

    try {
        const entity = await deliveryQueueClient.getEntity(DELIVERY_STATUS.PENDING, queueId);
        entity.status = DELIVERY_STATUS.FAILED;
        entity.errorMessage = errorMessage;
        entity.failedAt = new Date().toISOString();
        entity.retryCount = (entity.retryCount || 0) + 1;
        await deliveryQueueClient.updateEntity(entity, 'Replace');
    } catch (error) {
        throw new Error(`Failed to mark delivery failed: ${error.message}`);
    }
}

module.exports = {
    addFollower,
    removeFollower,
    isFollower,
    getFollower,
    updateFollower,  // Phase 4D: For migration script to backfill sharedInbox
    getAllFollowers,
    getFollowerCount,
    buildFollowersCollection,
    queueAcceptActivity,
    getPendingAccepts,
    markAcceptDelivered,
    markAcceptFailed,
    addDeliveryStatus,
    getDeliveryStatus,
    updateDeliveryStatus,
    getDeliveryStatusesForActivity,
    queuePostDelivery,
    getPendingDeliveries,
    markDeliveryCompleted,
    markDeliveryFailed
};
