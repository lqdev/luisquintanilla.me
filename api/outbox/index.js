const fs = require('fs').promises;
const path = require('path');

/**
 * ActivityPub Outbox handler with pagination support
 * Phase 5F: Handles both root collection and paginated page requests
 * 
 * Routes:
 *   GET /api/activitypub/outbox         → Returns root OrderedCollection with first/last links
 *   GET /api/activitypub/outbox?page=1  → Returns OrderedCollectionPage with items
 */
module.exports = async function (context, req) {
    try {
        // Check for page query parameter
        const pageParam = req.query.page;
        
        let filePath;
        if (pageParam) {
            // Validate page number
            const pageNum = parseInt(pageParam, 10);
            if (isNaN(pageNum) || pageNum < 1) {
                context.res = {
                    status: 400,
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: { error: 'Invalid page parameter' }
                };
                return;
            }
            filePath = path.join(__dirname, `../data/outbox/page-${pageNum}.json`);
        } else {
            // No page param - return root collection
            filePath = path.join(__dirname, '../data/outbox/index.json');
        }
        
        const outboxData = await fs.readFile(filePath, 'utf8');
        
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
        // Check if file not found (page out of range)
        if (error.code === 'ENOENT') {
            context.res = {
                status: 404,
                headers: {
                    'Content-Type': 'application/json'
                },
                body: { error: 'Page not found' }
            };
            return;
        }
        
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