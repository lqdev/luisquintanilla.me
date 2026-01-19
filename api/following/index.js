const fs = require('fs').promises;
const path = require('path');

module.exports = async function (context, req) {
    try {
        const followingPath = path.join(__dirname, '../data/following.json');
        let followingData;
        
        try {
            followingData = await fs.readFile(followingPath, 'utf8');
        } catch (fileError) {
            // Return empty collection if file doesn't exist
            const emptyCollection = {
                "@context": "https://www.w3.org/ns/activitystreams",
                "id": "https://lqdev.me/api/activitypub/following",
                "type": "OrderedCollection", 
                "totalItems": 0,
                "orderedItems": []
            };
            followingData = JSON.stringify(emptyCollection, null, 2);
        }
        
        context.res = {
            status: 200,
            headers: {
                'Content-Type': 'application/activity+json',
                'Access-Control-Allow-Origin': '*',
                'Cache-Control': 'public, max-age=300'
            },
            body: followingData
        };
    } catch (error) {
        context.log.error(`Following error: ${error.message}`);
        context.res = {
            status: 500,
            headers: {
                'Content-Type': 'application/json'
            },
            body: { error: 'Internal server error' }
        };
    }
};