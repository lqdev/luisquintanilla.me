const fs = require('fs').promises;
const path = require('path');

module.exports = async function (context, req) {
    try {
        // Get resource parameter
        const resource = req.query.resource;
        
        if (!resource || resource !== 'acct:lqdev@www.lqdev.me') {
            context.res = {
                status: 404,
                headers: {
                    'Content-Type': 'application/json'
                },
                body: { error: 'Resource not found' }
            };
            return;
        }

        // Read webfinger data
        const webfingerPath = path.join(__dirname, '../data/webfinger.json');
        const webfingerData = await fs.readFile(webfingerPath, 'utf8');
        
        context.res = {
            status: 200,
            headers: {
                'Content-Type': 'application/jrd+json',
                'Access-Control-Allow-Origin': '*',
                'Cache-Control': 'public, max-age=3600'
            },
            body: webfingerData
        };
    } catch (error) {
        context.log.error(`WebFinger error: ${error.message}`);
        context.res = {
            status: 500,
            headers: {
                'Content-Type': 'application/json'
            },
            body: { error: 'Internal server error' }
        };
    }
};