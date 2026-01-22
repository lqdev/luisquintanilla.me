#!/usr/bin/env node

/**
 * Queue ActivityPub Post for Delivery
 * 
 * This script is called from the F# build process to queue a newly published
 * post for delivery to followers.
 * 
 * Usage: node queue-post-delivery.js <noteId> <createActivityJsonPath>
 */

const fs = require('fs');
const path = require('path');
const tableStorage = require('../utils/tableStorage');

async function main() {
    if (process.argv.length < 4) {
        console.error('Usage: node queue-post-delivery.js <noteId> <createActivityJsonPath>');
        process.exit(1);
    }

    const noteId = process.argv[2];
    const createActivityPath = process.argv[3];

    // Verify environment
    if (!process.env.ACTIVITYPUB_STORAGE_CONNECTION) {
        console.error('❌ ACTIVITYPUB_STORAGE_CONNECTION not configured');
        process.exit(1);
    }

    try {
        // Read Create activity JSON
        const createActivityJson = fs.readFileSync(createActivityPath, 'utf8');
        const createActivity = JSON.parse(createActivityJson);

        // Queue for delivery
        const queueId = await tableStorage.queuePostDelivery(noteId, createActivity);

        console.log(`✅ Queued post ${noteId} for delivery (queue ID: ${queueId})`);
        process.exit(0);
    } catch (error) {
        console.error(`❌ Failed to queue post delivery: ${error.message}`);
        process.exit(1);
    }
}

main();
