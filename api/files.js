const { app } = require('@azure/functions');

/**
 * Azure Functions HTTP trigger for wildcard file redirects
 * Handles requests to /api/files/* and redirects to blob storage
 * 
 * This function implements wildcard redirect functionality that Azure Static Web Apps
 * cannot handle natively. It captures the file path from the URL and constructs
 * a redirect to the corresponding blob storage location.
 */
app.http('filesRedirect', {
    methods: ['GET'],
    route: 'files/{*filePath}',
    authLevel: 'anonymous',
    handler: async (request, context) => {
        try {
            // Extract the file path from the route parameter
            const filePath = request.params.filePath || '';
            
            // Validate the file path to prevent potential security issues
            if (!filePath || filePath.includes('..') || filePath.startsWith('/')) {
                context.log.warn(`Invalid file path attempted: ${filePath}`);
                return {
                    status: 400,
                    body: 'Invalid file path'
                };
            }
            
            // Construct the blob storage URL
            const baseUrl = 'https://luisquintanillame.blob.core.windows.net/files/';
            const redirectUrl = baseUrl + filePath;
            
            // Preserve query parameters if they exist
            const url = new URL(request.url);
            if (url.search) {
                const redirectUrlWithQuery = redirectUrl + url.search;
                context.log.info(`Redirecting ${request.url} to ${redirectUrlWithQuery}`);
                
                return {
                    status: 301,
                    headers: {
                        'Location': redirectUrlWithQuery,
                        'Cache-Control': 'public, max-age=3600', // Cache for 1 hour
                        'X-Robots-Tag': 'noindex' // Prevent indexing of redirect URLs
                    }
                };
            }
            
            context.log.info(`Redirecting ${request.url} to ${redirectUrl}`);
            
            // Return 301 permanent redirect response
            return {
                status: 301,
                headers: {
                    'Location': redirectUrl,
                    'Cache-Control': 'public, max-age=3600', // Cache for 1 hour
                    'X-Robots-Tag': 'noindex' // Prevent indexing of redirect URLs
                }
            };
            
        } catch (error) {
            context.log.error(`Error processing redirect: ${error.message}`);
            
            return {
                status: 500,
                body: 'Internal server error'
            };
        }
    }
});
