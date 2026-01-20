/**
 * Test script for ActivityPub Post Delivery System
 * 
 * Tests the QueueDeliveryTasks endpoint and verifies queue/table storage integration
 * 
 * Usage:
 *   node test-post-delivery.js
 * 
 * Requirements:
 *   - ACTIVITYPUB_STORAGE_CONNECTION environment variable set
 *   - Outbox data available at api/data/outbox/index.json
 */

const https = require('https');
const tableStorage = require('./utils/tableStorage');
const queueStorage = require('./utils/queueStorage');

// Configuration
const FUNCTION_URL = process.env.FUNCTION_URL || 'https://luisquintanillame-static.azurestaticapps.net';
const API_ENDPOINT = `${FUNCTION_URL}/api/activitypub/trigger-delivery`;

/**
 * Test 1: Verify Table Storage connection
 */
async function testTableStorage() {
    console.log('\n📊 Test 1: Table Storage Connection');
    console.log('=====================================');
    
    try {
        const followers = await tableStorage.getAllFollowers();
        console.log(`✅ Connected to Table Storage`);
        console.log(`   Followers count: ${followers.length}`);
        
        if (followers.length > 0) {
            console.log(`   Sample follower: ${followers[0].actorUrl}`);
        }
        
        return true;
    } catch (error) {
        console.error(`❌ Table Storage connection failed: ${error.message}`);
        return false;
    }
}

/**
 * Test 2: Verify Queue Storage connection
 */
async function testQueueStorage() {
    console.log('\n📬 Test 2: Queue Storage Connection');
    console.log('====================================');
    
    try {
        const queueLength = await queueStorage.getQueueLength('activitypub-delivery');
        console.log(`✅ Connected to Queue Storage`);
        console.log(`   Current queue length: ${queueLength}`);
        
        return true;
    } catch (error) {
        console.error(`❌ Queue Storage connection failed: ${error.message}`);
        return false;
    }
}

/**
 * Test 3: Test QueueDeliveryTasks endpoint (dry run with empty array)
 */
async function testQueueDeliveryEndpoint() {
    console.log('\n🎯 Test 3: QueueDeliveryTasks Endpoint');
    console.log('======================================');
    
    return new Promise((resolve) => {
        const url = new URL(API_ENDPOINT);
        const requestBody = JSON.stringify({ activityIds: [] });
        
        const options = {
            hostname: url.hostname,
            port: url.port || 443,
            path: url.pathname,
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Content-Length': Buffer.byteLength(requestBody)
            }
        };
        
        console.log(`   Endpoint: ${API_ENDPOINT}`);
        console.log(`   Request: POST with empty activityIds array`);
        
        const req = https.request(options, (res) => {
            let data = '';
            res.on('data', (chunk) => { data += chunk; });
            res.on('end', () => {
                if (res.statusCode === 200) {
                    console.log(`✅ Endpoint reachable and working (HTTP 200)`);
                    try {
                        const response = JSON.parse(data);
                        console.log(`   Response:`, JSON.stringify(response, null, 2));
                    } catch (e) {
                        console.log(`   Response: ${data}`);
                    }
                    resolve(true);
                } else if (res.statusCode === 400) {
                    console.log(`⚠️ Endpoint returned 400 Bad Request (expected with empty array)`);
                    try {
                        const response = JSON.parse(data);
                        console.log(`   Response:`, JSON.stringify(response, null, 2));
                    } catch (e) {
                        console.log(`   Response: ${data}`);
                    }
                    resolve(true); // Still consider this a pass since endpoint is reachable
                } else {
                    console.error(`❌ Unexpected status code: ${res.statusCode}`);
                    console.log(`   Response: ${data}`);
                    resolve(false);
                }
            });
        });
        
        req.on('error', (err) => {
            console.error(`❌ Request failed: ${err.message}`);
            resolve(false);
        });
        
        req.setTimeout(10000, () => {
            req.destroy();
            console.error('❌ Request timeout (10s)');
            resolve(false);
        });
        
        req.write(requestBody);
        req.end();
    });
}

/**
 * Test 4: Check outbox data availability
 */
async function testOutboxData() {
    console.log('\n📦 Test 4: Outbox Data Availability');
    console.log('====================================');
    
    try {
        const fs = require('fs').promises;
        const path = require('path');
        const outboxPath = path.join(__dirname, 'data/outbox/index.json');
        
        const outboxData = await fs.readFile(outboxPath, 'utf8');
        const outbox = JSON.parse(outboxData);
        
        console.log(`✅ Outbox data loaded`);
        console.log(`   Total items: ${outbox.orderedItems ? outbox.orderedItems.length : 0}`);
        
        if (outbox.orderedItems && outbox.orderedItems.length > 0) {
            const firstItem = outbox.orderedItems[0];
            const activityId = firstItem.id || (firstItem.object && firstItem.object.id);
            console.log(`   First activity ID: ${activityId}`);
        }
        
        return true;
    } catch (error) {
        console.error(`❌ Failed to load outbox data: ${error.message}`);
        console.log(`   This is expected if the site hasn't been built yet`);
        return false;
    }
}

/**
 * Run all tests
 */
async function runTests() {
    console.log('🧪 ActivityPub Post Delivery System Tests');
    console.log('==========================================\n');
    
    // Check environment
    if (!process.env.ACTIVITYPUB_STORAGE_CONNECTION) {
        console.error('❌ ACTIVITYPUB_STORAGE_CONNECTION environment variable not set');
        console.log('   Set it with: export ACTIVITYPUB_STORAGE_CONNECTION="..."');
        process.exit(1);
    }
    
    console.log('✅ Environment variable ACTIVITYPUB_STORAGE_CONNECTION is set');
    
    // Run tests
    const results = {
        tableStorage: await testTableStorage(),
        queueStorage: await testQueueStorage(),
        outboxData: await testOutboxData(),
        endpoint: await testQueueDeliveryEndpoint()
    };
    
    // Summary
    console.log('\n📊 Test Summary');
    console.log('===============');
    console.log(`Table Storage:    ${results.tableStorage ? '✅ PASS' : '❌ FAIL'}`);
    console.log(`Queue Storage:    ${results.queueStorage ? '✅ PASS' : '❌ FAIL'}`);
    console.log(`Outbox Data:      ${results.outboxData ? '✅ PASS' : '⚠️ WARNING'}`);
    console.log(`QueueDelivery EP: ${results.endpoint ? '✅ PASS' : '❌ FAIL'}`);
    
    const allPassed = results.tableStorage && results.queueStorage && results.endpoint;
    
    if (allPassed) {
        console.log('\n✅ All critical tests passed! System is ready for post delivery.');
    } else {
        console.log('\n❌ Some tests failed. Review the errors above.');
        process.exit(1);
    }
}

// Run tests
runTests().catch(error => {
    console.error('\n💥 Test runner error:', error);
    process.exit(1);
});
