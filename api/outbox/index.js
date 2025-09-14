const fs = require('fs').promises;
const path = require('path');

module.exports = async function (context, req) {
    try {
        const outboxPath = path.join(__dirname, '../data/outbox/index.json');
        const outboxData = await fs.readFile(outboxPath, 'utf8');
        
        context.res = {
            status: 200,
            headers: {
                'Content-Type': 'application/activity+json',
                'Access-Control-Allow-Origin': '*',
                'Cache-Control': 'public, max-age=300'
            },
            body: outboxData
        };
    } catch (error) {
        context.log.error(`Outbox error: ${error.message}`);
        context.res = {
            status: 500,
            headers: {
                'Content-Type': 'application/json'
            },
            body: { error: 'Internal server error' }
        };
    }
};