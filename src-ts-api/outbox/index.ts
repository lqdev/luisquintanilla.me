import { promises as fs } from 'fs';
import * as path from 'path';
import { AzureFunction, Context, HttpRequest } from '../types';

const httpTrigger: AzureFunction = async function (context: Context, _req?: HttpRequest): Promise<void> {
    try {
        const outboxPath = path.join(__dirname, '../data/outbox/index.json');
        const outboxData = await fs.readFile(outboxPath, 'utf8');
        
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
        const errorMessage = error instanceof Error ? error.message : 'Unknown error';
        context.log.error(`Outbox error: ${errorMessage}`);
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
