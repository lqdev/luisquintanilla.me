const fs = require('fs').promises;
const path = require('path');

// Every identifier that resolves to this account. URL forms (RFC 7033 §4.5)
// are accepted alongside the acct: form so clients can look up the actor or
// homepage directly.
const VALID_RESOURCES = new Set([
    'acct:lqdev@lqdev.me',
    'acct:lqdev@www.lqdev.me',  // Backward compatibility
    'https://lqdev.me',
    'https://lqdev.me/',
    'https://www.lqdev.me',
    'https://www.lqdev.me/',
    'https://lqdev.me/api/activitypub/actor'
]);

module.exports = async function (context, req) {
    try {
        const resource = req.query.resource;

        if (!resource || !VALID_RESOURCES.has(resource)) {
            context.res = {
                status: 404,
                headers: {
                    'Content-Type': 'application/json'
                },
                body: { error: 'Resource not found' }
            };
            return;
        }

        const webfingerPath = path.join(__dirname, '../data/webfinger.json');
        const webfingerData = await fs.readFile(webfingerPath, 'utf8');
        const jrd = JSON.parse(webfingerData);

        // RFC 7033 §4.3: optional `rel` filter (may repeat). Filters links only;
        // subject/aliases/properties are always returned.
        const requestUrl = new URL(req.url, 'https://lqdev.me');
        const rels = requestUrl.searchParams.getAll('rel');
        const body = rels.length === 0
            ? jrd
            : { ...jrd, links: jrd.links.filter(link => rels.includes(link.rel)) };

        context.res = {
            status: 200,
            headers: {
                'Content-Type': 'application/jrd+json',
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Methods': 'GET, POST, OPTIONS',
                'Access-Control-Allow-Headers': 'Accept, Content-Type',
                'Cache-Control': 'public, max-age=3600'
            },
            body
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
