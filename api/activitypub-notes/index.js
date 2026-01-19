const fs = require('fs').promises;
const path = require('path');

/**
 * Azure Function proxy for ActivityPub notes
 * 
 * Purpose: Serve static note JSON files with correct Content-Type header
 * 
 * Context: Azure Static Web Apps cannot reliably override Content-Type for
 * .json files via staticwebapp.config.json. ActivityPub spec requires
 * 'application/activity+json' Content-Type for proper federation.
 * 
 * Solution: This function proxies static files from _public/activitypub/notes/
 * and serves them with the correct headers for ActivityPub compliance.
 * 
 * Benefits:
 * - Ensures ActivityPub spec compliance
 * - Maintains CDN caching for performance (24-hour cache)
 * - Minimal compute costs (function only invoked on cache misses)
 * - Static files remain for easy development/testing
 * 
 * @param {Object} context - Azure Functions context
 * @param {Object} req - HTTP request object
 */
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
    
    // Validate noteId format (should be hex string)
    if (!noteId || !/^[a-f0-9]+$/i.test(noteId)) {
        context.log.warn(`Invalid noteId format: ${noteId}`);
        context.res = {
            status: 400,
            headers: {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*'
            },
            body: JSON.stringify({
                error: 'Bad Request',
                message: 'Invalid note ID format'
            })
        };
        return;
    }

    // Construct path to static note file
    // In production, this resolves to: /home/site/wwwroot/_public/activitypub/notes/{noteId}.json
    // In local dev, this resolves to: ../../../_public/activitypub/notes/{noteId}.json
    const notePath = path.join(__dirname, '../../_public/activitypub/notes', `${noteId}.json`);
    
    try {
        context.log(`Reading note file: ${notePath}`);
        const noteContent = await fs.readFile(notePath, 'utf8');
        
        // Validate JSON before returning
        try {
            JSON.parse(noteContent);
        } catch (parseError) {
            context.log.error(`Invalid JSON in note file ${noteId}: ${parseError.message}`);
            context.res = {
                status: 500,
                headers: {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*'
                },
                body: JSON.stringify({
                    error: 'Internal Server Error',
                    message: 'Note file contains invalid JSON'
                })
            };
            return;
        }

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
                // Diagnostic header to identify proxy vs static serving
                'X-Content-Source': 'static-proxy'
            },
            body: noteContent
        };
        
        context.log(`Successfully served note: ${noteId}`);
        
    } catch (err) {
        // Handle file not found
        if (err.code === 'ENOENT') {
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
        } else {
            // Handle other errors
            context.log.error(`Error reading note ${noteId}: ${err.message}`);
            context.res = {
                status: 500,
                headers: {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*'
                },
                body: JSON.stringify({
                    error: 'Internal Server Error',
                    message: 'Failed to read note file'
                })
            };
        }
    }
};
