/**
 * Diagnostic script to check Azure Table Storage state
 * 
 * Usage:
 *   cd api
 *   node scripts/diagnose-table-storage.js
 * 
 * Environment variables required:
 *   ACTIVITYPUB_STORAGE_CONNECTION - Azure Storage connection string
 */

const { TableClient } = require('@azure/data-tables');

async function diagnoseTableStorage() {
    console.log('🔍 ActivityPub Table Storage Diagnostic\n');
    console.log('========================================\n');
    
    const connectionString = process.env.ACTIVITYPUB_STORAGE_CONNECTION;
    
    if (!connectionString) {
        console.error('❌ ACTIVITYPUB_STORAGE_CONNECTION environment variable not set');
        console.log('\n💡 Set it with your Azure Storage connection string:');
        console.log('   export ACTIVITYPUB_STORAGE_CONNECTION="DefaultEndpointsProtocol=https;..."');
        process.exit(1);
    }
    
    console.log('✅ Connection string found\n');
    
    // Table names to check
    const tables = ['followers', 'pendingaccepts', 'deliverystatus'];
    
    for (const tableName of tables) {
        console.log(`\n📊 Table: ${tableName}`);
        console.log('─'.repeat(50));
        
        try {
            const client = TableClient.fromConnectionString(connectionString, tableName);
            
            // Check if table exists by trying to query it
            let count = 0;
            let entities = [];
            
            try {
                const listResults = client.listEntities();
                
                for await (const entity of listResults) {
                    count++;
                    entities.push(entity);
                    
                    // Only show first 5 entities
                    if (count <= 5) {
                        console.log(`\n  Entity #${count}:`);
                        console.log(`    PartitionKey: ${entity.partitionKey}`);
                        console.log(`    RowKey: ${entity.rowKey}`);
                        
                        // Show specific fields based on table
                        if (tableName === 'followers') {
                            console.log(`    ActorURL: ${entity.actorUrl || 'N/A'}`);
                            console.log(`    Inbox: ${entity.inbox || 'N/A'}`);
                            console.log(`    FollowedAt: ${entity.followedAt || 'N/A'}`);
                            console.log(`    DisplayName: ${entity.displayName || 'N/A'}`);
                        } else if (tableName === 'pendingaccepts') {
                            console.log(`    ActorURL: ${entity.actorUrl || 'N/A'}`);
                            console.log(`    Status: ${entity.status || 'N/A'}`);
                            console.log(`    QueuedAt: ${entity.queuedAt || 'N/A'}`);
                            console.log(`    RetryCount: ${entity.retryCount || 0}`);
                        } else if (tableName === 'deliverystatus') {
                            console.log(`    ActivityId: ${entity.activityId || 'N/A'}`);
                            console.log(`    Status: ${entity.status || 'N/A'}`);
                            console.log(`    TargetInbox: ${entity.targetInbox || 'N/A'}`);
                            console.log(`    LastAttempt: ${entity.lastAttempt || 'N/A'}`);
                        }
                    }
                }
                
                console.log(`\n  ✅ Total entities: ${count}`);
                
                if (count === 0) {
                    console.log('  ⚠️  Table is EMPTY');
                } else if (count > 5) {
                    console.log(`  ℹ️  Showing first 5 of ${count} entities`);
                }
                
            } catch (queryError) {
                if (queryError.statusCode === 404) {
                    console.log('  ❌ Table does NOT exist');
                } else {
                    console.log(`  ❌ Error querying table: ${queryError.message}`);
                }
            }
            
        } catch (error) {
            console.log(`  ❌ Error accessing table: ${error.message}`);
        }
    }
    
    console.log('\n========================================');
    console.log('📋 Diagnosis Summary\n');
    
    console.log('Expected state for working ActivityPub:');
    console.log('  • followers table: Should have entries for each follower');
    console.log('  • pendingaccepts table: May have pending accepts OR be empty if all delivered');
    console.log('  • deliverystatus table: Should have entries for delivered posts\n');
    
    console.log('Common issues:');
    console.log('  • All tables empty: Connection string might be wrong or pointing to wrong storage');
    console.log('  • Tables don\'t exist: First-time setup needed or tables were deleted');
    console.log('  • pendingaccepts has old entries: Accept delivery worker might be failing\n');
}

// Run diagnostic
diagnoseTableStorage()
    .then(() => {
        console.log('✅ Diagnostic complete\n');
        process.exit(0);
    })
    .catch(error => {
        console.error(`\n❌ Fatal error: ${error.message}`);
        console.error(error.stack);
        process.exit(1);
    });
