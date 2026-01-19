const tableStorage = require('../utils/tableStorage');

module.exports = async function (context, req) {
    try {
        // Get current followers from Table Storage
        const followers = await tableStorage.buildFollowersCollection();
        
        context.log(`Followers endpoint: returning ${followers.totalItems} followers from Table Storage`);
        
        context.res = {
            status: 200,
            headers: {
                'Content-Type': 'application/activity+json',
                'Access-Control-Allow-Origin': '*',
                'Cache-Control': 'public, max-age=60' // Cache for 1 minute
            },
            body: followers
        };
    } catch (error) {
        context.log.error(`Followers error: ${error.message}`);
        
        // Fallback to empty collection
        const emptyCollection = {
            "@context": "https://www.w3.org/ns/activitystreams",
            "id": "https://lqdev.me/api/activitypub/followers",
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
            body: emptyCollection
        };
    }
};