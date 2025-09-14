const fs = require('fs').promises;
const path = require('path');

module.exports = async function (context, req) {
    try {
        // Extract note ID from route parameter
        const noteId = context.bindingData.noteId;
        
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
        context.log.error(`Notes error: ${error.message}`);
        context.res = {
            status: 500,
            headers: {
                'Content-Type': 'application/json'
            },
            body: { error: 'Internal server error' }
        };
    }
};