import { promises as fs } from 'fs';
import * as path from 'path';
import { AzureFunction, Context, HttpRequest } from '../types';

const httpTrigger: AzureFunction = async function (context: Context, _req?: HttpRequest): Promise<void> {
    try {
        const followingPath = path.join(__dirname, '../data/following.json');
        let followingData: string;
        
        try {
            followingData = await fs.readFile(followingPath, 'utf8');
        } catch (fileError) {
            // Return empty collection if file doesn't exist
            const emptyCollection = {
                "@context": "https://www.w3.org/ns/activitystreams",
                "id": "https://www.lqdev.me/activitypub/following",
                "type": "OrderedCollection", 
                "totalItems": 0,
                "orderedItems": []
            };
            followingData = JSON.stringify(emptyCollection, null, 2);
        }
        
        context.res = {
            status: 200,
            headers: {
                'Content-Type': 'application/activity+json',
                'Access-Control-Allow-Origin': '*',
                'Cache-Control': 'public, max-age=300'
            },
            body: followingData
        };
    } catch (error) {
        const errorMessage = error instanceof Error ? error.message : 'Unknown error';
        context.log.error(`Following error: ${errorMessage}`);
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
