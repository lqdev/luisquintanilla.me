/**
 * Azure Function HTTP proxy for ActivityPub activities
 * Phase 5A: Updated to support mixed activity types (Create, Like, Announce)
 * 
 * Purpose: Fetch static activity JSON from CDN and serve with correct Content-Type header
 * 
 * Architecture: HTTP Proxy Pattern
 * - Static files remain in _public/activitypub/activities/ (deployed to Azure CDN)
 * - Function fetches from CDN URLs and rewrites headers
 * - In-memory caching (1 hour) for frequently accessed activities
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
 * - In-memory caching provides near-instant responses for popular activities
 * 
 * @param {Object} context - Azure Functions context
 * @param {Object} req - HTTP request object
 */

// In-memory cache for frequently accessed activities
// Cache survives across function invocations within same instance
const activityCache = new Map();
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

    let activityId = req.params.activityId;
    
    // Strip fragment identifier if present (e.g., "hash#create" -> "hash")
    // ActivityPub Create activities use fragment pattern for wrapper IDs
    if (activityId && activityId.includes('#')) {
        activityId = activityId.split('#')[0];
    }
    
    // Format validation (32-character MD5 hex) prevents both path traversal and DoS from malformed/long IDs
    if (!activityId || !/^[a-f0-9]{32}$/i.test(activityId)) {
        context.log.warn(`Invalid activityId format: ${activityId}`);
        context.res = {
            status: 400,
            headers: {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*'
            },
            body: JSON.stringify({
                error: 'Bad Request',
                message: 'Invalid activity ID format (expected 32-character hex hash)'
            })
        };
        return;
    }

    // Check in-memory cache first
    const cached = activityCache.get(activityId);
    if (cached && Date.now() - cached.timestamp < CACHE_TTL) {
        context.log(`Cache HIT for activity: ${activityId}`);
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
    const staticUrl = `${baseUrl}/activitypub/activities/${activityId}.json`;
    
    try {
        context.log(`Cache MISS - Fetching activity from CDN: ${staticUrl}`);
        
        // Fetch from static CDN URL
        const response = await fetch(staticUrl);
        
        if (!response.ok) {
            if (response.status === 404) {
                context.log.warn(`Activity not found: ${activityId}`);
                context.res = {
                    status: 404,
                    headers: {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    body: JSON.stringify({
                        error: 'Not Found',
                        message: `Activity with ID '${activityId}' does not exist`
                    })
                };
                return;
            }
            
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }
        
        // Get activity content
        const activityContent = await response.text();
        
        // Parse and validate JSON
        let activity;
        try {
            activity = JSON.parse(activityContent);
        } catch (parseError) {
            context.log.error(`Invalid JSON in activity ${activityId}: ${parseError.message}`);
            context.res = {
                status: 500,
                headers: {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*'
                },
                body: JSON.stringify({
                    error: 'Internal Server Error',
                    message: 'Activity contains invalid JSON'
                })
            };
            return;
        }
        
        // CRITICAL FIX: For Create activities, return the embedded object (Note/Article)
        // Mastodon's URL search only accepts object types (Note, Article, etc.), not activity types (Create)
        // Like/Announce activities are returned as-is (they reference external objects)
        let responseContent;
        if (activity.type === 'Create' && activity.object) {
            // Extract the Note/Article object and ensure it has proper context
            const obj = activity.object;
            if (!obj['@context']) {
                obj['@context'] = 'https://www.w3.org/ns/activitystreams';
            }
            // Update the object's ID to match the fetchable URL (no fragment)
            // This ensures Mastodon's ID validation passes
            obj.id = `https://lqdev.me/api/activitypub/activities/${activityId}`;
            responseContent = JSON.stringify(obj);
            context.log(`Unwrapped Create activity to ${obj.type}: ${activityId}`);
        } else {
            // Like, Announce, or other activities returned as-is
            responseContent = activityContent;
        }
        
        // Cache the response content
        activityCache.set(activityId, {
            content: responseContent,
            timestamp: Date.now()
        });
        
        // Return with proper ActivityPub headers
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
            body: responseContent
        };
        
        context.log(`Successfully proxied activity: ${activityId}`);
        
    } catch (err) {
        // Handle HTTP fetch errors
        context.log.error(`Error fetching activity ${activityId}: ${err.message}`);
        context.res = {
            status: 500,
            headers: {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*'
            },
            body: JSON.stringify({
                error: 'Internal Server Error',
                message: 'Failed to fetch activity from CDN'
            })
        };
    }
};
