const fs = require('fs').promises;
const path = require('path');

module.exports = async function (context, req) {
    try {
        // Get resource parameter
        const resource = req.query.resource;
        
        // Accept both lqdev@lqdev.me (standard) and lqdev@www.lqdev.me (backward compatibility)
        const validResources = [
            'acct:lqdev@lqdev.me',
            'acct:lqdev@www.lqdev.me'  // Backward compatibility
        ];
        
        if (!resource || !validResources.includes(resource)) {
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
        const parsedWebfinger = JSON.parse(webfingerData);
        
        context.res = {
            status: 200,
            headers: {
                'Content-Type': 'application/jrd+json',
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Methods': 'GET, POST, OPTIONS',
                'Access-Control-Allow-Headers': 'Accept, Content-Type',
                'Cache-Control': 'public, max-age=3600'
            },
            body: parsedWebfinger
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