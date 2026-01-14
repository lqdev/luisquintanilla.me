const fs = require('fs').promises;
const path = require('path');

module.exports = async function (context, req) {
    try {
        const followersPath = path.join(__dirname, '../data/followers.json');
        let followersData;
        
        try {
            followersData = await fs.readFile(followersPath, 'utf8');
        } catch (fileError) {
            // Return empty collection if file doesn't exist
            const emptyCollection = {
                "@context": "https://www.w3.org/ns/activitystreams",
                "id": "https://lqdev.me/api/followers",
                "type": "OrderedCollection",
                "totalItems": 0,
                "orderedItems": []
            };
            followersData = JSON.stringify(emptyCollection, null, 2);
        }
        
        context.res = {
            status: 200,
            headers: {
                'Content-Type': 'application/activity+json',
                'Access-Control-Allow-Origin': '*',
                'Cache-Control': 'public, max-age=300'
            },
            body: followersData
        };
    } catch (error) {
        context.log.error(`Followers error: ${error.message}`);
        context.res = {
            status: 500,
            headers: {
                'Content-Type': 'application/json'
            },
            body: { error: 'Internal server error' }
        };
    }
};