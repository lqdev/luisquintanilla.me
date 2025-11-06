import { promises as fs } from 'fs';
import * as path from 'path';
import { AzureFunction, Context, HttpRequest } from '../types';

const httpTrigger: AzureFunction = async function (context: Context, _req?: HttpRequest): Promise<void> {
    try {
        const actorPath = path.join(__dirname, '../data/actor.json');
        const actorData = await fs.readFile(actorPath, 'utf8');
        const parsedActor = JSON.parse(actorData);
        
        context.res = {
            status: 200,
            headers: {
                'Content-Type': 'application/activity+json',
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Methods': 'GET, POST, OPTIONS',
                'Access-Control-Allow-Headers': 'Accept, Content-Type',
                'Cache-Control': 'public, max-age=3600'
            },
            body: parsedActor
        };
    } catch (error) {
        const errorMessage = error instanceof Error ? error.message : 'Unknown error';
        context.log.error(`Actor error: ${errorMessage}`);
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
