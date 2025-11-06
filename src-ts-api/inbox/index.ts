import { promises as fs } from 'fs';
import * as path from 'path';
import { AzureFunction, Context, HttpRequest } from '../types';

interface ActivityPubActivity {
    type?: string;
    [key: string]: any;
}

const httpTrigger: AzureFunction = async function (context: Context, req?: HttpRequest): Promise<void> {
    try {
        if (!req) {
            context.res = {
                status: 400,
                headers: { 'Content-Type': 'application/json' },
                body: { error: 'No request provided' }
            };
            return;
        }

        if (req.method === 'GET') {
            // Return inbox as empty collection for now
            const emptyInbox = {
                "@context": "https://www.w3.org/ns/activitystreams",
                "id": "https://www.lqdev.me/activitypub/inbox",
                "type": "OrderedCollection",
                "totalItems": 0,
                "orderedItems": []
            };
            
            context.res = {
                status: 200,
                headers: {
                    'Content-Type': 'application/activity+json',
                    'Access-Control-Allow-Origin': '*'
                },
                body: JSON.stringify(emptyInbox, null, 2)
            };
            return;
        }

        if (req.method === 'POST') {
            // Log incoming activities for Phase 3 implementation
            const activityData: ActivityPubActivity = req.body as ActivityPubActivity;
            const timestamp = new Date().toISOString();
            
            // Ensure activities directory exists
            const activitiesDir = path.join(__dirname, '../data/activities');
            try {
                await fs.mkdir(activitiesDir, { recursive: true });
            } catch (mkdirError) {
                // Directory might already exist
            }
            
            // Log activity to file
            const logFile = path.join(activitiesDir, `${timestamp.replace(/[:.]/g, '-')}.json`);
            await fs.writeFile(logFile, JSON.stringify(activityData, null, 2));
            
            context.log(`Received activity: ${activityData.type || 'Unknown'}`);
            
            // Return 202 Accepted for now (Phase 3 will implement processing)
            context.res = {
                status: 202,
                headers: {
                    'Content-Type': 'application/json'
                },
                body: { message: 'Activity received and logged' }
            };
            return;
        }

        // Method not allowed
        context.res = {
            status: 405,
            headers: {
                'Content-Type': 'application/json'
            },
            body: { error: 'Method not allowed' }
        };
    } catch (error) {
        const errorMessage = error instanceof Error ? error.message : 'Unknown error';
        context.log.error(`Inbox error: ${errorMessage}`);
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
