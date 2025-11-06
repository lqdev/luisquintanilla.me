import { promises as fs } from 'fs';
import * as path from 'path';
import { AzureFunction, Context, HttpRequest } from '../types';

const httpTrigger: AzureFunction = async function (context: Context, _req?: HttpRequest): Promise<void> {
    try {
        // Extract note ID from route parameter
        const noteId = context.bindingData.noteId as string | undefined;
        
        if (!noteId || !noteId.match(/^[a-f0-9]{32}$/)) {
            context.res = {
                status: 404,
                headers: {
                    'Content-Type': 'application/json'
                },
                body: { error: 'Note not found' }
            };
            return;
        }

        const notePath = path.join(__dirname, `../data/notes/${noteId}.json`);
        
        try {
            const noteData = await fs.readFile(notePath, 'utf8');
            const parsedNote = JSON.parse(noteData);
            
            context.res = {
                status: 200,
                headers: {
                    'Content-Type': 'application/activity+json',
                    'Access-Control-Allow-Origin': '*',
                    'Cache-Control': 'public, max-age=3600'
                },
                body: parsedNote
            };
        } catch (fileError) {
            context.res = {
                status: 404,
                headers: {
                    'Content-Type': 'application/json'
                },
                body: { error: 'Note not found' }
            };
        }
    } catch (error) {
        const errorMessage = error instanceof Error ? error.message : 'Unknown error';
        context.log.error(`Notes error: ${errorMessage}`);
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
