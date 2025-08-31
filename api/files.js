/**
 * Azure Functions HTTP trigger for wildcard file redirects
 * Handles requests to /api/files/* and redirects to blob storage
 * 
 * This function implements wildcard redirect functionality that Azure Static Web Apps
 * cannot handle natively. It captures the file path from the URL and constructs
 * a redirect to the corresponding blob storage location.
 */
module.exports = async function (context, req) {
    try {
        // Extract the file path from the route parameter
        // For route 'files/{*filePath}', the parameter comes from context.bindingData
        const filePath = context.bindingData.filePath || '';
        
        context.log.info(`Processing file redirect request for: ${filePath}`);
        
        // Validate the file path to prevent potential security issues
        if (!filePath || filePath.includes('..') || filePath.startsWith('/')) {
            context.log.warn(`Invalid file path attempted: ${filePath}`);
            context.res = {
                status: 400,
                body: 'Invalid file path'
            };
            return;
        }
        
        // Construct the blob storage URL
        const baseUrl = 'https://luisquintanillame.blob.core.windows.net/files/';
        const redirectUrl = baseUrl + filePath;
        
        // Preserve query parameters if they exist
        const queryString = req.url.includes('?') ? req.url.split('?')[1] : '';
        const finalRedirectUrl = queryString ? `${redirectUrl}?${queryString}` : redirectUrl;
        
        context.log.info(`Redirecting to: ${finalRedirectUrl}`);
        
        // Return 301 permanent redirect response
        context.res = {
            status: 301,
            headers: {
                'Location': finalRedirectUrl,
                'Cache-Control': 'public, max-age=3600', // Cache for 1 hour
                'X-Robots-Tag': 'noindex' // Prevent indexing of redirect URLs
            }
        };
        
    } catch (error) {
        context.log.error(`Error processing redirect: ${error.message}`);
        
        context.res = {
            status: 500,
            body: 'Internal server error'
        };
    }
};
