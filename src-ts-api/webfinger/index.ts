import { promises as fs } from 'fs';
import * as path from 'path';
import { AzureFunction, Context, HttpRequest } from '../types';

const httpTrigger: AzureFunction = async function (context: Context, req?: HttpRequest): Promise<void> {
    try {
        // Get resource parameter
        const resource = req?.query?.['resource'];
        
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
        const errorMessage = error instanceof Error ? error.message : 'Unknown error';
        context.log.error(`WebFinger error: ${errorMessage}`);
        context.res = {
            status: 500,
            headers: {
                'Content-Type': 'application/json'
            },
            body: { error: 'Internal server error' }
        };
    }
};

export default httpTrigger;
