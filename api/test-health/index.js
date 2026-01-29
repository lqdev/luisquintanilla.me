// Minimal health check - no dependencies, no file reads
module.exports = async function (context, req) {
    context.res = {
        status: 200,
        headers: {
            'Content-Type': 'application/json'
        },
        body: { 
            status: 'ok', 
            timestamp: new Date().toISOString(),
            nodeVersion: process.version
        }
    };
};
