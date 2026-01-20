#!/usr/bin/env node

/**
 * Deliver Pending ActivityPub Accept Activities
 * 
 * This script runs in GitHub Actions to deliver queued Accept activities
 * with proper HTTP signatures using Azure Key Vault signing.
 * 
 * Free tier architecture: Functions queue, GitHub Actions delivers.
 */

const https = require('https');
const { DefaultAzureCredential } = require('@azure/identity');
const { CryptographyClient } = require('@azure/keyvault-keys');
const crypto = require('crypto');
const tableStorage = require('../utils/tableStorage');

/**
 * Generate HTTP Signature using Azure Key Vault
 */
async function generateHttpSignature(method, targetUrl, headers, body) {
    const keyVaultKeyId = process.env.KEY_VAULT_KEY_ID;
    
    if (!keyVaultKeyId) {
        throw new Error('KEY_VAULT_KEY_ID environment variable not set');
    }

    // Initialize Key Vault client with managed identity
    const credential = new DefaultAzureCredential();
    const cryptoClient = new CryptographyClient(keyVaultKeyId, credential);

    // Calculate SHA-256 digest of body
    const bodyHash = crypto.createHash('sha256').update(body).digest('base64');
    headers['Digest'] = `SHA-256=${bodyHash}`;

    // Parse URL for signing
    const url = new URL(targetUrl);
    const requestTarget = `${method.toLowerCase()} ${url.pathname}${url.search}`;

    // Build string to sign
    const headersToSign = ['(request-target)', 'host', 'date', 'digest'];
    const signatureString = headersToSign.map(header => {
        if (header === '(request-target)') {
            return `(request-target): ${requestTarget}`;
        }
        const headerValue = headers[header.split(':')[0].toLowerCase().replace(/^./, str => str.toUpperCase())];
        return `${header}: ${headerValue}`;
    }).join('\n');

    // Sign with Key Vault
    const signatureBuffer = Buffer.from(signatureString, 'utf-8');
    const hashAlgorithm = 'SHA-256';
    const hash = crypto.createHash('sha256').update(signatureBuffer).digest();
    
    const signResult = await cryptoClient.sign('RS256', hash);
    const signature = Buffer.from(signResult.result).toString('base64');

    // Build Signature header
    const keyId = 'https://lqdev.me/api/activitypub/actor#main-key';
    const signatureHeader = `keyId="${keyId}",algorithm="rsa-sha256",headers="${headersToSign.join(' ')}",signature="${signature}"`;

    return signatureHeader;
}

/**
 * Deliver Accept activity to remote inbox
 */
async function deliverAccept(pendingAccept) {
    const { acceptId, inbox, acceptActivity } = pendingAccept;
    
    console.log(`Delivering Accept ${acceptId} to ${inbox}`);

    const url = new URL(inbox);
    const body = JSON.stringify(acceptActivity);
    
    const headers = {
        'Host': url.hostname,
        'Date': new Date().toUTCString(),
        'Content-Type': 'application/activity+json',
        'Content-Length': Buffer.byteLength(body),
        'User-Agent': 'lqdev.me ActivityPub/1.0 (GitHub Actions)'
    };

    // Generate HTTP signature
    try {
        const signatureHeader = await generateHttpSignature('POST', inbox, headers, body);
        headers['Signature'] = signatureHeader;
    } catch (signError) {
        console.error(`Failed to generate signature: ${signError.message}`);
        throw signError;
    }

    // Send signed request
    return new Promise((resolve, reject) => {
        const options = {
            hostname: url.hostname,
            port: url.port || 443,
            path: url.pathname + url.search,
            method: 'POST',
            headers: headers
        };

        const req = https.request(options, (res) => {
            let data = '';
            res.on('data', (chunk) => { data += chunk; });
            res.on('end', () => {
                if (res.statusCode >= 200 && res.statusCode < 300) {
                    console.log(`✓ Accept delivered successfully: HTTP ${res.statusCode}`);
                    resolve({ success: true, statusCode: res.statusCode, body: data });
                } else {
                    const errorMsg = `HTTP ${res.statusCode}: ${data}`;
                    console.error(`✗ Accept delivery failed: ${errorMsg}`);
                    reject(new Error(errorMsg));
                }
            });
        });

        req.on('error', (err) => {
            console.error(`✗ Network error: ${err.message}`);
            reject(err);
        });

        req.setTimeout(15000, () => {
            req.destroy();
            reject(new Error('Timeout delivering Accept activity'));
        });

        req.write(body);
        req.end();
    });
}

/**
 * Main delivery function
 */
async function main() {
    console.log('🚀 Starting Accept activity delivery');
    console.log(`Timestamp: ${new Date().toISOString()}`);
    
    // Verify environment configuration
    if (!process.env.KEY_VAULT_KEY_ID) {
        console.error('❌ KEY_VAULT_KEY_ID not configured');
        process.exit(1);
    }
    if (!process.env.ACTIVITYPUB_STORAGE_CONNECTION) {
        console.error('❌ ACTIVITYPUB_STORAGE_CONNECTION not configured');
        process.exit(1);
    }

    try {
        // Get pending Accepts from table storage
        const pendingAccepts = await tableStorage.getPendingAccepts();
        console.log(`Found ${pendingAccepts.length} pending Accept activities`);

        if (pendingAccepts.length === 0) {
            console.log('✓ No pending activities to deliver');
            return;
        }

        let successCount = 0;
        let failureCount = 0;

        // Process each pending Accept
        for (const pendingAccept of pendingAccepts) {
            try {
                await deliverAccept(pendingAccept);
                await tableStorage.markAcceptDelivered(pendingAccept.acceptId);
                successCount++;
            } catch (error) {
                console.error(`Failed to deliver Accept ${pendingAccept.acceptId}: ${error.message}`);
                await tableStorage.markAcceptFailed(pendingAccept.acceptId, error.message);
                failureCount++;
            }
        }

        console.log('\n📊 Delivery Summary:');
        console.log(`   ✓ Successful: ${successCount}`);
        console.log(`   ✗ Failed: ${failureCount}`);
        console.log(`   📋 Total: ${pendingAccepts.length}`);

    } catch (error) {
        console.error('❌ Fatal error:', error);
        process.exit(1);
    }
}

// Run main function
main().catch(error => {
    console.error('❌ Unhandled error:', error);
    process.exit(1);
});
