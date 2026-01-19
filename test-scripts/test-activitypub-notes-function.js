#!/usr/bin/env node

/**
 * Test script for ActivityPub notes Azure Function
 * 
 * Tests the function logic directly without needing Azure Functions Core Tools
 */

const fs = require('fs').promises;
const path = require('path');

// Mock context object for testing
const createMockContext = () => {
    const log = (...args) => console.log('[LOG]', ...args);
    log.warn = (...args) => console.log('[WARN]', ...args);
    log.error = (...args) => console.error('[ERROR]', ...args);
    log.info = (...args) => console.log('[INFO]', ...args);
    
    return {
        log: log,
        res: null
    };
};

// Mock request objects
const createMockRequest = (noteId, method = 'GET') => {
    return {
        method: method,
        params: {
            noteId: noteId
        }
    };
};

// Load the function
const functionHandler = require('../api/activitypub-notes/index.js');

// Test cases
async function runTests() {
    console.log('========================================');
    console.log('ActivityPub Notes Function Tests');
    console.log('========================================\n');

    let passed = 0;
    let failed = 0;

    // Test 1: Valid note ID
    console.log('Test 1: Valid note ID');
    try {
        const context = createMockContext();
        const noteId = '00029d200a98327f59a82821a27b959d';
        const req = createMockRequest(noteId);
        
        await functionHandler(context, req);
        
        if (context.res && context.res.status === 200) {
            const contentType = context.res.headers['Content-Type'];
            if (contentType === 'application/activity+json; charset=utf-8') {
                console.log('✅ PASS: Valid note returned with correct Content-Type');
                console.log(`   Content-Type: ${contentType}`);
                console.log(`   Cache-Control: ${context.res.headers['Cache-Control']}`);
                console.log(`   X-Content-Source: ${context.res.headers['X-Content-Source']}`);
                
                // Verify JSON is valid
                const parsed = JSON.parse(context.res.body);
                if (parsed.id && parsed.type && parsed['@context']) {
                    console.log(`   Note ID: ${parsed.id}`);
                    console.log(`   Note Type: ${parsed.type}`);
                    passed++;
                } else {
                    console.log('❌ FAIL: Note JSON missing required fields');
                    failed++;
                }
            } else {
                console.log(`❌ FAIL: Wrong Content-Type: ${contentType}`);
                failed++;
            }
        } else {
            console.log(`❌ FAIL: Expected 200 but got ${context.res ? context.res.status : 'undefined'}`);
            failed++;
        }
    } catch (err) {
        console.log(`❌ FAIL: ${err.message}`);
        failed++;
    }
    console.log('');

    // Test 2: Non-existent note ID
    console.log('Test 2: Non-existent note ID');
    try {
        const context = createMockContext();
        const noteId = 'ffffffffffffffffffffffffffffffff';
        const req = createMockRequest(noteId);
        
        await functionHandler(context, req);
        
        if (context.res && context.res.status === 404) {
            console.log('✅ PASS: Returns 404 for non-existent note');
            const body = JSON.parse(context.res.body);
            console.log(`   Error: ${body.error}`);
            passed++;
        } else {
            console.log(`❌ FAIL: Expected 404 but got ${context.res ? context.res.status : 'undefined'}`);
            failed++;
        }
    } catch (err) {
        console.log(`❌ FAIL: ${err.message}`);
        failed++;
    }
    console.log('');

    // Test 3: Invalid note ID format
    console.log('Test 3: Invalid note ID format');
    try {
        const context = createMockContext();
        const noteId = 'invalid-id-format!@#';
        const req = createMockRequest(noteId);
        
        await functionHandler(context, req);
        
        if (context.res && context.res.status === 400) {
            console.log('✅ PASS: Returns 400 for invalid note ID format');
            const body = JSON.parse(context.res.body);
            console.log(`   Error: ${body.error}`);
            passed++;
        } else {
            console.log(`❌ FAIL: Expected 400 but got ${context.res ? context.res.status : 'undefined'}`);
            failed++;
        }
    } catch (err) {
        console.log(`❌ FAIL: ${err.message}`);
        failed++;
    }
    console.log('');

    // Test 4: OPTIONS request (CORS preflight)
    console.log('Test 4: OPTIONS request (CORS preflight)');
    try {
        const context = createMockContext();
        const noteId = '00029d200a98327f59a82821a27b959d';
        const req = createMockRequest(noteId, 'OPTIONS');
        
        await functionHandler(context, req);
        
        if (context.res && context.res.status === 200) {
            const headers = context.res.headers;
            if (headers['Access-Control-Allow-Origin'] === '*' &&
                headers['Access-Control-Allow-Methods'] &&
                headers['Access-Control-Allow-Headers']) {
                console.log('✅ PASS: OPTIONS request returns proper CORS headers');
                console.log(`   Access-Control-Allow-Origin: ${headers['Access-Control-Allow-Origin']}`);
                console.log(`   Access-Control-Allow-Methods: ${headers['Access-Control-Allow-Methods']}`);
                passed++;
            } else {
                console.log('❌ FAIL: Missing or incorrect CORS headers');
                failed++;
            }
        } else {
            console.log(`❌ FAIL: Expected 200 but got ${context.res ? context.res.status : 'undefined'}`);
            failed++;
        }
    } catch (err) {
        console.log(`❌ FAIL: ${err.message}`);
        failed++;
    }
    console.log('');

    // Test 5: Overly long note ID (DoS prevention)
    console.log('Test 5: Overly long note ID (DoS prevention)');
    try {
        const context = createMockContext();
        const noteId = 'a'.repeat(100); // 100 character hex string
        const req = createMockRequest(noteId);
        
        await functionHandler(context, req);
        
        if (context.res && context.res.status === 400) {
            console.log('✅ PASS: Returns 400 for overly long note ID');
            const body = JSON.parse(context.res.body);
            console.log(`   Error: ${body.error}`);
            passed++;
        } else {
            console.log(`❌ FAIL: Expected 400 but got ${context.res ? context.res.status : 'undefined'}`);
            failed++;
        }
    } catch (err) {
        console.log(`❌ FAIL: ${err.message}`);
        failed++;
    }
    console.log('');

    // Test 6: Too short note ID
    console.log('Test 6: Too short note ID');
    try {
        const context = createMockContext();
        const noteId = 'abc123'; // Only 6 characters, should be 32
        const req = createMockRequest(noteId);
        
        await functionHandler(context, req);
        
        if (context.res && context.res.status === 400) {
            console.log('✅ PASS: Returns 400 for too short note ID');
            const body = JSON.parse(context.res.body);
            console.log(`   Error: ${body.error}`);
            passed++;
        } else {
            console.log(`❌ FAIL: Expected 400 but got ${context.res ? context.res.status : 'undefined'}`);
            failed++;
        }
    } catch (err) {
        console.log(`❌ FAIL: ${err.message}`);
        failed++;
    }
    console.log('');

    // Summary
    console.log('========================================');
    console.log('Test Summary');
    console.log('========================================');
    console.log(`Passed: ${passed}`);
    console.log(`Failed: ${failed}`);
    console.log('');

    if (failed === 0) {
        console.log('✅ All tests passed!');
        process.exit(0);
    } else {
        console.log('❌ Some tests failed.');
        process.exit(1);
    }
}

// Run tests
runTests().catch(err => {
    console.error('Test execution failed:', err);
    process.exit(1);
});
