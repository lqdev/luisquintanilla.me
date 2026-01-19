/**
 * Azure Function HTTP proxy for ActivityPub notes
 * 
 * Purpose: Fetch static note JSON from CDN and serve with correct Content-Type header
 * 
 * Architecture: HTTP Proxy Pattern
 * - Static files remain in _public/activitypub/notes/ (deployed to Azure CDN)
 * - Function fetches from CDN URLs and rewrites headers
 * - In-memory caching (1 hour) for frequently accessed notes
 * - CDN caching (24 hours) for global distribution
 * 
 * Why HTTP Proxy vs File System?
 * Azure Static Web Apps architecture separates static content (CDN) from Functions
 * (separate runtime). File system access to _public/ does not work in production.
 * 
 * Benefits:
 * - Ensures ActivityPub spec compliance
 * - Leverages Azure CDN for global performance
 * - Smaller API bundle (no file duplication)
 * - Simpler deployment (no file sync step)
 * - In-memory caching provides near-instant responses for popular notes
 * 
 * @param {Object} context - Azure Functions context
 * @param {Object} req - HTTP request object
 */

// In-memory cache for frequently accessed notes
// Cache survives across function invocations within same instance
const noteCache = new Map();
const CACHE_TTL = 3600000; // 1 hour in milliseconds

module.exports = async function (context, req) {
    // Handle CORS preflight requests
    if (req.method === 'OPTIONS') {
        context.res = {
            status: 200,
            headers: {
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Methods': 'GET, OPTIONS',
                'Access-Control-Allow-Headers': 'Accept, Content-Type',
                'Access-Control-Max-Age': '86400'
            },
            body: null
        };
        return;
    }

    const noteId = req.params.noteId;
    
    // Validate noteId format (MD5 hash = 32 hex characters)
    // Length constraint prevents DoS from extremely long IDs
    if (!noteId || !/^[a-f0-9]{32}$/i.test(noteId)) {
        context.log.warn(`Invalid noteId format: ${noteId}`);
        context.res = {
            status: 400,
            headers: {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*'
            },
            body: JSON.stringify({
                error: 'Bad Request',
                message: 'Invalid note ID format (expected 32-character hex hash)'
            })
        };
        return;
    }

    // Check in-memory cache first
    const cached = noteCache.get(noteId);
    if (cached && Date.now() - cached.timestamp < CACHE_TTL) {
        context.log(`Cache HIT for note: ${noteId}`);
        context.res = {
            status: 200,
            headers: {
                'Content-Type': 'application/activity+json; charset=utf-8',
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Methods': 'GET, OPTIONS',
                'Access-Control-Allow-Headers': 'Accept, Content-Type',
                'Cache-Control': 'public, max-age=86400',
                'X-Cache': 'HIT',
                'X-Content-Source': 'http-proxy'
            },
            body: cached.content
        };
        return;
    }

    // Cache MISS - fetch from CDN
    // Use environment variable for base URL (allows local dev override)
    const baseUrl = process.env.STATIC_BASE_URL || 'https://lqdev.me';
    const staticUrl = `${baseUrl}/activitypub/notes/${noteId}.json`;
    
    try {
        context.log(`Cache MISS - Fetching note from CDN: ${staticUrl}`);
        
        // Fetch from static CDN URL
        const response = await fetch(staticUrl);
        
        if (!response.ok) {
            if (response.status === 404) {
                context.log.warn(`Note not found: ${noteId}`);
                context.res = {
                    status: 404,
                    headers: {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    body: JSON.stringify({
                        error: 'Not Found',
                        message: `Note with ID '${noteId}' does not exist`
                    })
                };
                return;
            }
            
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }
        
        // Get note content
        const noteContent = await response.text();
        
        // Validate JSON before caching and returning
        try {
            JSON.parse(noteContent);
        } catch (parseError) {
            context.log.error(`Invalid JSON in note ${noteId}: ${parseError.message}`);
            context.res = {
                status: 500,
                headers: {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*'
                },
                body: JSON.stringify({
                    error: 'Internal Server Error',
                    message: 'Note contains invalid JSON'
                })
            };
            return;
        }
        
        // Cache the note content
        noteCache.set(noteId, {
            content: noteContent,
            timestamp: Date.now()
        });
        
        // Return note with proper ActivityPub headers
        context.res = {
            status: 200,
            headers: {
                // Required: ActivityPub Content-Type with charset
                'Content-Type': 'application/activity+json; charset=utf-8',
                // CORS headers for federation
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Methods': 'GET, OPTIONS',
                'Access-Control-Allow-Headers': 'Accept, Content-Type',
                // Aggressive caching for CDN efficiency (24 hours)
                'Cache-Control': 'public, max-age=86400',
                // Diagnostic headers
                'X-Cache': 'MISS',
                'X-Content-Source': 'http-proxy'
            },
            body: noteContent
        };
        
        context.log(`Successfully proxied note: ${noteId}`);
        
    } catch (err) {
        // Handle HTTP fetch errors
        context.log.error(`Error fetching note ${noteId}: ${err.message}`);
        context.res = {
            status: 500,
            headers: {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*'
            },
            body: JSON.stringify({
                error: 'Internal Server Error',
                message: 'Failed to fetch note from CDN'
            })
        };
    }
};
