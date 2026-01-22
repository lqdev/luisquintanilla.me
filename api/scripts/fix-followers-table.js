/**
 * Fix followers table by migrating data from pendingaccepts
 * 
 * This script reconstructs the followers table from the pendingaccepts history
 * since followers were being queued for Accepts but not added to followers table.
 * 
 * Usage:
 *   cd api
 *   node scripts/fix-followers-table.js
 */

const { TableClient } = require('@azure/data-tables');
const https = require('https');

/**
 * Fetch actor profile
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
                'Accept': 'application/activity+json, application/ld+json'
            }
        };
        
        const req = https.request(options, (res) => {
            let data = '';
            res.on('data', (chunk) => { data += chunk; });
            res.on('end', () => {
                try {
                    resolve(JSON.parse(data));
                } catch (parseError) {
                    reject(new Error(`Failed to parse actor JSON: ${parseError.message}`));
                }
            });
        });
        
        req.on('error', reject);
        req.setTimeout(5000, () => {
            req.destroy();
            reject(new Error('Timeout fetching actor profile'));
        });
        req.end();
    });
}

/**
 * Convert string to URL-safe base64
 */
function toUrlSafeBase64(str) {
    return Buffer.from(str)
        .toString('base64')
        .replace(/\+/g, '-')
        .replace(/\//g, '_')
        .replace(/=/g, '');
}

async function fixFollowersTable() {
    console.log('🔧 Fixing Followers Table from PendingAccepts Data\n');
    console.log('====================================================\n');
    
    const connectionString = process.env.ACTIVITYPUB_STORAGE_CONNECTION;
    
    if (!connectionString) {
        console.error('❌ ACTIVITYPUB_STORAGE_CONNECTION environment variable not set');
        process.exit(1);
    }
    
    const pendingAcceptsClient = TableClient.fromConnectionString(connectionString, 'pendingaccepts');
    const followersClient = TableClient.fromConnectionString(connectionString, 'followers');
    
    // Get all pending accepts (both delivered and failed)
    console.log('📊 Fetching pending accepts...');
    const pendingAccepts = [];
    const entities = pendingAcceptsClient.listEntities();
    
    for await (const entity of entities) {
        pendingAccepts.push({
            rowKey: entity.rowKey,
            actorUrl: entity.actorUrl,
            inbox: entity.inbox,
            status: entity.status,
            queuedAt: entity.queuedAt,
            followActivityId: entity.followActivityId
        });
    }
    
    console.log(`✅ Found ${pendingAccepts.length} pending accept records\n`);
    
    // Group by actorUrl to get unique followers
    const uniqueFollowers = new Map();
    
    for (const accept of pendingAccepts) {
        if (!uniqueFollowers.has(accept.actorUrl)) {
            uniqueFollowers.set(accept.actorUrl, accept);
        } else {
            // Keep the most recent one (based on queuedAt)
            const existing = uniqueFollowers.get(accept.actorUrl);
            if (new Date(accept.queuedAt) > new Date(existing.queuedAt)) {
                uniqueFollowers.set(accept.actorUrl, accept);
            }
        }
    }
    
    console.log(`📋 Found ${uniqueFollowers.size} unique followers\n`);
    
    // Add each follower to followers table
    let successCount = 0;
    let failCount = 0;
    
    for (const [actorUrl, acceptData] of uniqueFollowers) {
        console.log(`\n👤 Processing: ${actorUrl}`);
        console.log(`   Status: ${acceptData.status}`);
        console.log(`   Queued: ${acceptData.queuedAt}`);
        
        try {
            // Fetch current profile for display name
            let displayName = '';
            try {
                console.log('   Fetching profile...');
                const profile = await fetchActorProfile(actorUrl);
                displayName = profile.name || profile.preferredUsername || '';
                console.log(`   Display name: ${displayName}`);
            } catch (profileError) {
                console.log(`   ⚠️  Could not fetch profile: ${profileError.message}`);
            }
            
            // Add to followers table
            const entity = {
                partitionKey: 'follower',
                rowKey: toUrlSafeBase64(actorUrl),
                actorUrl: actorUrl,
                inbox: acceptData.inbox || '',
                followedAt: acceptData.queuedAt,
                displayName: displayName,
                followActivityId: acceptData.followActivityId || ''
            };
            
            await followersClient.upsertEntity(entity, 'Merge');
            console.log('   ✅ Added to followers table');
            successCount++;
            
        } catch (error) {
            console.log(`   ❌ Failed: ${error.message}`);
            failCount++;
        }
        
        // Small delay to avoid rate limiting
        await new Promise(resolve => setTimeout(resolve, 500));
    }
    
    console.log('\n====================================================');
    console.log('📊 Results:\n');
    console.log(`   ✅ Successfully added: ${successCount}`);
    console.log(`   ❌ Failed: ${failCount}`);
    console.log(`   📈 Total unique followers: ${uniqueFollowers.size}\n`);
    
    // Verify final state
    console.log('🔍 Verifying followers table...');
    let finalCount = 0;
    const verifyEntities = followersClient.listEntities();
    
    for await (const entity of verifyEntities) {
        finalCount++;
    }
    
    console.log(`✅ Followers table now has ${finalCount} entries\n`);
}

fixFollowersTable()
    .then(() => {
        console.log('✅ Fix complete\n');
        process.exit(0);
    })
    .catch(error => {
        console.error(`\n❌ Fatal error: ${error.message}`);
        console.error(error.stack);
        process.exit(1);
    });
