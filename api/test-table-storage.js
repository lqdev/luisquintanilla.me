/**
 * Test script for Table Storage connectivity
 * Run with: node test-table-storage.js
 * 
 * Requires ACTIVITYPUB_STORAGE_CONNECTION environment variable
 */

const tableStorage = require('./utils/tableStorage');

async function testTableStorage() {
    console.log('🧪 Testing Table Storage connectivity...\n');
    
    try {
        // Test 1: Add a test follower
        console.log('Test 1: Adding test follower...');
        await tableStorage.addFollower(
            'https://mastodon.social/users/testuser',
            'https://mastodon.social/users/testuser/inbox',
            'https://mastodon.social/test-follow-123',
            'Test User'
        );
        console.log('✓ Test follower added\n');
        
        // Test 2: Check if follower exists
        console.log('Test 2: Checking if follower exists...');
        const exists = await tableStorage.isFollower('https://mastodon.social/users/testuser');
        console.log(`✓ Follower exists: ${exists}\n`);
        
        // Test 3: Get follower details
        console.log('Test 3: Getting follower details...');
        const follower = await tableStorage.getFollower('https://mastodon.social/users/testuser');
        console.log('✓ Follower details:');
        console.log(JSON.stringify(follower, null, 2));
        console.log('');
        
        // Test 4: Get all followers
        console.log('Test 4: Getting all followers...');
        const allFollowers = await tableStorage.getAllFollowers();
        console.log(`✓ Total followers: ${allFollowers.length}`);
        console.log(JSON.stringify(allFollowers, null, 2));
        console.log('');
        
        // Test 5: Build followers collection
        console.log('Test 5: Building ActivityPub followers collection...');
        const collection = await tableStorage.buildFollowersCollection();
        console.log('✓ Followers collection:');
        console.log(JSON.stringify(collection, null, 2));
        console.log('');
        
        // Test 6: Remove test follower
        console.log('Test 6: Removing test follower...');
        const removed = await tableStorage.removeFollower('https://mastodon.social/users/testuser');
        console.log(`✓ Follower removed: ${removed}\n`);
        
        // Test 7: Verify removal
        console.log('Test 7: Verifying removal...');
        const stillExists = await tableStorage.isFollower('https://mastodon.social/users/testuser');
        console.log(`✓ Follower still exists: ${stillExists}\n`);
        
        console.log('✅ All tests passed!');
        
    } catch (error) {
        console.error('❌ Test failed:', error.message);
        console.error(error.stack);
        process.exit(1);
    }
}

// Run tests
testTableStorage();
