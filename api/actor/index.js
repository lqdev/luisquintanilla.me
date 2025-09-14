const fs = require('fs').promises;
const path = require('path');

module.exports = async function (context, req) {
    try {
        const actorPath = path.join(__dirname, '../data/actor.json');
        const actorData = await fs.readFile(actorPath, 'utf8');
        const parsedActor = JSON.parse(actorData);
        
        context.res = {
            status: 200,
            headers: {
                'Content-Type': 'application/activity+json',
                'Access-Control-Allow-Origin': '*',
                'Cache-Control': 'public, max-age=3600'
            },
            body: parsedActor
        };
    } catch (error) {
        context.log.error(`Actor error: ${error.message}`);
        context.res = {
            status: 500,
            headers: {
                'Content-Type': 'application/json'
            },
            body: { error: 'Internal server error' }
        };
    }
};