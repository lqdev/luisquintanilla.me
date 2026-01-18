const { TableClient, AzureNamedKeyCredential } = require('@azure/data-tables');
const { DefaultAzureCredential } = require('@azure/identity');

/**
 * Table Storage utility for ActivityPub follower management
 * 
 * Tables:
 * - followers: Stores follower actor information
 * 
 * Configuration via environment variables:
 * - ACTIVITYPUB_STORAGE_CONNECTION: Connection string for Table Storage
 * - AZURE_STORAGE_ACCOUNT_NAME: Storage account name (alternative auth)
 */

let followersClient = null;

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
 * @returns {Promise<void>}
 */
async function addFollower(actorUrl, inbox, followActivityId, displayName = null) {
    initializeClients();
    await ensureTableExists(followersClient);

    const entity = {
        partitionKey: 'follower',
        rowKey: Buffer.from(actorUrl).toString('base64').replace(/[\/\+\=]/g, '_'), // URL-safe encoding
        actorUrl: actorUrl,
        inbox: inbox || '',
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

    const rowKey = Buffer.from(actorUrl).toString('base64').replace(/[\/\+\=]/g, '_');
    
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

    const rowKey = Buffer.from(actorUrl).toString('base64').replace(/[\/\+\=]/g, '_');
    
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

    const rowKey = Buffer.from(actorUrl).toString('base64').replace(/[\/\+\=]/g, '_');
    
    try {
        const entity = await followersClient.getEntity('follower', rowKey);
        return {
            actorUrl: entity.actorUrl,
            inbox: entity.inbox,
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

module.exports = {
    addFollower,
    removeFollower,
    isFollower,
    getFollower,
    getAllFollowers,
    getFollowerCount,
    buildFollowersCollection
};
