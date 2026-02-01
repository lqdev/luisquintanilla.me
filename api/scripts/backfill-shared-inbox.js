#!/usr/bin/env node
/**
 * Phase 4D: Backfill SharedInbox for Existing Followers
 * 
 * This script fetches actor profiles for all existing followers and updates
 * their Table Storage entries with the sharedInbox endpoint (if available).
 * 
 * Usage:
 *   node backfill-shared-inbox.js [--dry-run]
 * 
 * Environment Variables Required:
 *   ACTIVITYPUB_STORAGE_CONNECTION - Azure Table Storage connection string
 * 
 * Features:
 *   - Idempotent: Safe to run multiple times
 *   - Rate limiting: 1 second delay between requests to be a good citizen
 *   - Error handling: Logs failures but continues processing
 *   - Dry run mode: Preview changes without writing to Table Storage
 */

const https = require('https');
const tableStorage = require('../utils/tableStorage');

// Configuration
const DELAY_BETWEEN_REQUESTS_MS = 1000;  // Be a good citizen, don't hammer servers
const REQUEST_TIMEOUT_MS = 10000;

/**
 * Fetch actor profile from remote server
 * @param {string} actorUrl - Actor URL to fetch
 * @returns {Promise<Object>} Actor profile JSON
 */
async function fetchActorProfile(actorUrl) {
    return new Promise((resolve, reject) => {
        const { URL } = require('url');
        const url = new URL(actorUrl);
        
        const options = {
            hostname: url.hostname,
            port: url.port || 443,
            path: url.pathname + url.search,
            method: 'GET',
            headers: {
                'Accept': 'application/activity+json, application/ld+json; profile="https://www.w3.org/ns/activitystreams"',
                'User-Agent': 'lqdev.me ActivityPub/1.0 (Backfill Script)'
            }
        };
        
        const req = https.request(options, (res) => {
            let data = '';
            res.on('data', (chunk) => { data += chunk; });
            res.on('end', () => {
                try {
                    if (res.statusCode >= 200 && res.statusCode < 300) {
                        resolve(JSON.parse(data));
                    } else if (res.statusCode === 410) {
                        // Gone - actor no longer exists
                        reject(new Error(`Actor gone (410): ${actorUrl}`));
                    } else if (res.statusCode === 404) {
                        reject(new Error(`Actor not found (404): ${actorUrl}`));
                    } else {
                        reject(new Error(`HTTP ${res.statusCode}: ${data.substring(0, 200)}`));
                    }
                } catch (parseError) {
                    reject(new Error(`Failed to parse actor JSON: ${parseError.message}`));
                }
            });
        });
        
        req.on('error', reject);
        req.setTimeout(REQUEST_TIMEOUT_MS, () => {
            req.destroy();
            reject(new Error('Timeout fetching actor profile'));
        });
        req.end();
    });
}

/**
 * Sleep for specified milliseconds
 * @param {number} ms - Milliseconds to sleep
 */
function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

/**
 * Main backfill function
 */
async function backfillSharedInboxes() {
    const args = process.argv.slice(2);
    const dryRun = args.includes('--dry-run');
    
    console.log('=================================================');
    console.log('Phase 4D: Backfill SharedInbox for Existing Followers');
    console.log('=================================================');
    console.log(`Mode: ${dryRun ? 'DRY RUN (no changes will be written)' : 'LIVE'}`);
    console.log('');

    // Get all followers
    console.log('Fetching all followers from Table Storage...');
    let followers;
    try {
        followers = await tableStorage.getAllFollowers();
    } catch (error) {
        console.error(`Failed to fetch followers: ${error.message}`);
        process.exit(1);
    }
    
    console.log(`Found ${followers.length} followers to process`);
    console.log('');

    // Statistics
    const stats = {
        total: followers.length,
        updated: 0,
        alreadyHasSharedInbox: 0,
        noSharedInboxAvailable: 0,
        errors: 0,
        gone: 0
    };

    // Process each follower
    for (let i = 0; i < followers.length; i++) {
        const follower = followers[i];
        const progress = `[${i + 1}/${followers.length}]`;
        
        // Check if already has sharedInbox
        if (follower.sharedInbox) {
            console.log(`${progress} ✓ ${follower.actorUrl} - already has sharedInbox: ${follower.sharedInbox}`);
            stats.alreadyHasSharedInbox++;
            continue;
        }
        
        console.log(`${progress} Fetching: ${follower.actorUrl}`);
        
        try {
            // Fetch actor profile
            const actorProfile = await fetchActorProfile(follower.actorUrl);
            
            // Extract sharedInbox from endpoints object
            const sharedInbox = actorProfile.endpoints?.sharedInbox || null;
            
            if (sharedInbox) {
                console.log(`${progress} → Found sharedInbox: ${sharedInbox}`);
                
                if (!dryRun) {
                    // Update Table Storage
                    await tableStorage.updateFollower(follower.actorUrl, { 
                        sharedInbox: sharedInbox,
                        // Also update inbox if it was missing
                        inbox: follower.inbox || actorProfile.inbox
                    });
                    console.log(`${progress} ✓ Updated in Table Storage`);
                } else {
                    console.log(`${progress} [DRY RUN] Would update sharedInbox`);
                }
                
                stats.updated++;
            } else {
                console.log(`${progress} ○ No sharedInbox endpoint found (will use personal inbox)`);
                stats.noSharedInboxAvailable++;
            }
        } catch (error) {
            if (error.message.includes('410') || error.message.includes('404')) {
                console.log(`${progress} ✗ Actor no longer exists: ${error.message}`);
                stats.gone++;
            } else {
                console.error(`${progress} ✗ Error: ${error.message}`);
                stats.errors++;
            }
        }
        
        // Rate limiting - be a good citizen
        if (i < followers.length - 1) {
            await sleep(DELAY_BETWEEN_REQUESTS_MS);
        }
    }
    
    // Print summary
    console.log('');
    console.log('=================================================');
    console.log('                     SUMMARY                      ');
    console.log('=================================================');
    console.log(`Total followers processed:     ${stats.total}`);
    console.log(`Already had sharedInbox:       ${stats.alreadyHasSharedInbox}`);
    console.log(`Updated with sharedInbox:      ${stats.updated}`);
    console.log(`No sharedInbox available:      ${stats.noSharedInboxAvailable}`);
    console.log(`Gone (410/404):                ${stats.gone}`);
    console.log(`Errors:                        ${stats.errors}`);
    console.log('=================================================');
    
    if (dryRun) {
        console.log('');
        console.log('This was a DRY RUN. No changes were written to Table Storage.');
        console.log('Run without --dry-run to apply changes.');
    }
}

// Run the script
backfillSharedInboxes()
    .then(() => {
        console.log('');
        console.log('Backfill complete!');
        process.exit(0);
    })
    .catch((error) => {
        console.error('Fatal error:', error);
        process.exit(1);
    });
