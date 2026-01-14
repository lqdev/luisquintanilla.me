const fs = require('fs').promises;
const path = require('path');

module.exports = async function (context, req) {
    try {
        if (req.method === 'GET') {
            // Return inbox as empty collection for now
            const emptyInbox = {
                "@context": "https://www.w3.org/ns/activitystreams",
                "id": "https://lqdev.me/api/inbox",
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
            // Log incoming activities for Phase 3 implementation
            const activityData = req.body;
            const timestamp = new Date().toISOString();
            
            // Ensure activities directory exists
            const activitiesDir = path.join(__dirname, '../data/activities');
            try {
                await fs.mkdir(activitiesDir, { recursive: true });
            } catch (mkdirError) {
                // Directory might already exist
            }
            
            // Log activity to file
            const logFile = path.join(activitiesDir, `${timestamp.replace(/[:.]/g, '-')}.json`);
            await fs.writeFile(logFile, JSON.stringify(activityData, null, 2));
            
            context.log(`Received activity: ${activityData.type || 'Unknown'}`);
            
            // Return 202 Accepted for now (Phase 3 will implement processing)
            context.res = {
                status: 202,
                headers: {
                    'Content-Type': 'application/json'
                },
                body: { message: 'Activity received and logged' }
            };
            return;
        }

        // Method not allowed
        context.res = {
            status: 405,
            headers: {
                'Content-Type': 'application/json'
            },
            body: { error: 'Method not allowed' }
        };
    } catch (error) {
        context.log.error(`Inbox error: ${error.message}`);
        context.res = {
            status: 500,
            headers: {
                'Content-Type': 'application/json'
            },
            body: { error: 'Internal server error' }
        };
    }
};