const { getFollowers } = require('../utils/followers');

module.exports = async function (context, req) {
    try {
        // Get current followers from file
        const followers = await getFollowers();
        
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
        context.res = {
            status: 500,
            headers: {
                'Content-Type': 'application/json'
            },
            body: { error: 'Internal server error' }
        };
    }
};