# Phase 4 Research Summary: ActivityPub Activity Delivery

**Date**: January 18, 2026  
**Phase**: 4 - Activity Delivery to Follower Inboxes  
**Status**: Research Complete, Ready for Planning

## Executive Summary

Phase 4 involves implementing server-to-server ActivityPub delivery - the mechanism by which our activities are pushed to follower inboxes across the fediverse. This is the most complex phase requiring:

1. **HTTP Signature Authentication** (cryptographic signing using RSA keys)
2. **Recipient Discovery** (finding follower inboxes)
3. **Asynchronous Delivery** (queue-based processing with retries)
4. **Error Handling** (robust retry logic and failure tracking)
5. **Azure Functions Integration** (serverless delivery infrastructure)

## Key Research Findings

### 1. HTTP Signature Requirements (Critical)

**Discovery**: ActivityPub delivery requires cryptographically signed HTTP requests using HTTP Signatures (RFC 9421/cavage-12). Nearly 99% of fediverse software requires this for authentication.

**Required Components**:
- **Signature Header**: Contains keyId, algorithm, headers list, and base64-encoded signature
- **Digest Header**: SHA-256 hash of request body (prevents tampering)
- **Date Header**: RFC 7231 formatted UTC timestamp (prevents replay attacks)
- **Host Header**: Target server hostname
- **Content-Type**: `application/activity+json` or `application/ld+json; profile="https://www.w3.org/ns/activitystreams"`

**Signature String Format** (cavage-12 - most compatible):
```
(request-target): post /inbox
host: mastodon.social
date: Wed, 21 Jun 2023 22:38:44 GMT
digest: SHA-256=IuHD2Uc886zZlkrLZj02yFu1hSI9ZRC9DSkdb2Hmt8o=
content-type: application/activity+json
```

**Cryptographic Process**:
1. Construct signature string from required headers
2. Compute SHA-256 hash of signature string
3. Sign hash using RSA-SHA256 with actor's private key
4. Base64-encode signature
5. Include in `Signature` header with metadata

**Header Format**:
```
Signature: keyId="https://www.luisquintanilla.me/actor#main-key",headers="(request-target) host date digest content-type",signature="..."
```

### 2. Mastodon-Specific Requirements

**Discovery**: Mastodon is the dominant implementation and effectively the reference for compatibility.

**Validation Checks** (from Mastodon source):
- **Signature Header Presence**: Must exist on all POST requests
- **Actor Resolution**: KeyId must resolve to valid actor with public key
- **Signature Verification**: Cryptographic verification against actor's public key
- **Required Signed Headers**:
  - Date or (created) pseudo-header
  - Digest header (for POST)
  - Host header (for GET)
- **Body Digest Verification**: SHA-256 digest must match request body
- **Timestamp Validation**: Within 12-hour window (prevents replay attacks)
- **Domain Control**: KeyId domain must match actor domain

**Retry Logic**: Exponential backoff with Sidekiq, custom jitter added
**Circuit Breakers**: Stoplight gem prevents repeated attempts to unresponsive servers
**Delivery Failure Tracking**: Marks inboxes unavailable after failures

### 3. Delivery Architecture Patterns

**Asynchronous Delivery is Essential**:
- Synchronous delivery would block build process
- Remote servers may be slow or unavailable
- Queue-based processing allows fast content publication

**Recommended Architecture for Static Sites**:
```
Content Update → Build Process → Queue Delivery Tasks → Azure Function Workers → Remote Inboxes
```

**Delivery Sequence**:
1. **Activity Creation**: New content published during build
2. **Recipient Discovery**: Find follower inboxes from followers.json
3. **Queue Tasks**: Azure Queue Storage for delivery tasks
4. **Background Processing**: Azure Functions process queue
5. **HTTP Signature Generation**: Sign each request
6. **Delivery Execution**: POST to remote inboxes
7. **Error Handling**: Retry with exponential backoff

### 4. Error Handling & Retry Strategy

**HTTP Status Code Interpretation**:
- **2xx (Success)**: 200 OK, 201 Created, 202 Accepted, 204 No Content - Delivery successful
- **4xx (Client Error)**: 
  - 400 Bad Request - Invalid signature format
  - 401 Unauthorized - Signature verification failed
  - 403 Forbidden - Blocked by remote server
  - 404 Not Found - Inbox no longer exists
  - 410 Gone - Resource permanently deleted
  - **Do NOT retry 4xx errors** (except 429 with Retry-After)
- **5xx (Server Error)**: 
  - 500-599 - Temporary server issues
  - **Retry with exponential backoff**
- **Network Issues**: Timeouts, connection refused, DNS failures - **Retry with backoff**

**Recommended Retry Strategy** (Azure Functions best practices):
```
Retry Strategy: Exponential Backoff
Initial Delay: 2 seconds
Maximum Delay: 1 hour
Maximum Retries: 10 attempts
Maximum Retry Period: 24 hours
Timeout per Request: 30 seconds
```

**Exponential Backoff Formula**:
```
Delay = min(initial_delay * 2^(attempt - 1), max_delay)
Attempts: 2s, 4s, 8s, 16s, 32s, 64s, 128s, 256s, 512s, 1024s (capped at 3600s)
```

### 5. Shared Inbox Optimization

**Discovery**: Shared inboxes reduce delivery overhead for multiple followers on same server.

**Pattern**:
- Instead of posting to each follower's individual inbox: `/users/alice/inbox`, `/users/bob/inbox`
- Post once to server's shared inbox: `/inbox`
- Receiving server routes activity to local followers

**Implementation**:
- Discover `sharedInbox` property from actor objects
- Group followers by server domain
- Use shared inbox when available, fall back to individual inboxes

**Benefits**:
- Reduces HTTP requests (1 request vs N requests per server)
- Improves performance for large follower counts
- Reduces server load on both ends

### 6. Azure Functions Integration Strategy

**Serverless Considerations**:
- **Cold Start Mitigation**: Queue-based processing defers work
- **Execution Time Limits**: 15-minute max per function (configurable)
- **Stateless Nature**: Store delivery state in Azure Queue/Table Storage
- **Cost Optimization**: Only pay for actual delivery execution time

**Recommended Azure Services**:
- **Azure Queue Storage**: Delivery task queue
- **Azure Functions (HTTP Trigger)**: Delivery worker
- **Azure Table Storage**: Delivery status tracking
- **Azure Key Vault**: Secure private key storage (already implemented)
- **Application Insights**: Error tracking and monitoring

**Function Architecture**:
```
Function 1: QueueDeliveryTasks (Timer Trigger - runs on new content)
  - Reads outbox index.json
  - Reads followers.json
  - Discovers recipient inboxes
  - Queues delivery tasks to Azure Queue Storage

Function 2: ProcessDelivery (Queue Trigger)
  - Reads delivery task from queue
  - Retrieves private key from Key Vault
  - Generates HTTP signature
  - POSTs activity to remote inbox
  - Handles errors and retries
  - Updates delivery status in Table Storage
```

## Common Implementation Pitfalls to Avoid

### Pitfall 1: Incorrect Digest Computation
**Problem**: Computing hash of wrong representation (pretty-printed vs minified JSON)  
**Solution**: Compute digest over exact bytes that will be transmitted

### Pitfall 2: Header Ordering Issues
**Problem**: Headers in Signature header don't match signature string order  
**Solution**: Maintain consistent header order throughout signing process

### Pitfall 3: Missing Timestamp Validation
**Problem**: Not validating Date header, allowing replay attacks  
**Solution**: Verify timestamps within acceptable window (12 hours)

### Pitfall 4: Private Addressing Leakage
**Problem**: Including `bto`/`bcc` fields in delivered activities  
**Solution**: Strip private addressing fields before delivery

### Pitfall 5: Synchronous Delivery
**Problem**: Blocking build process waiting for remote servers  
**Solution**: Queue-based asynchronous delivery

### Pitfall 6: Insufficient Error Handling
**Problem**: Not distinguishing between retriable and non-retriable errors  
**Solution**: Implement proper status code handling and retry logic

## Technical Implementation Requirements

### 1. Private Key Management (Already Have)
✅ Phase 2 implemented Azure Key Vault integration
✅ Private key securely stored and retrievable
✅ Key Vault access configured for Azure Functions

### 2. HTTP Client Requirements
**Need**: F# HTTP client library with:
- Full control over headers
- Request body as raw bytes
- SHA-256 digest computation
- RSA-SHA256 signature generation
- Timeout handling
- Retry policy support

**Recommended**: .NET `HttpClient` with custom signature handler

### 3. Recipient Discovery
**Need**: Function to discover follower inboxes
**Input**: `followers.json` (from Phase 2)
**Process**: 
- Fetch actor objects from follower URIs
- Extract `inbox` or `sharedInbox` properties
- Cache inbox URLs to reduce fetches
- Group by server for shared inbox optimization

### 4. Delivery Queue Schema
**Queue Message Format**:
```json
{
  "activityId": "https://www.luisquintanilla.me/api/activitypub/activities/abc123",
  "activityContent": "{...}", // Full activity JSON
  "targetInbox": "https://mastodon.social/inbox",
  "attemptCount": 0,
  "createdAt": "2026-01-18T15:30:00Z"
}
```

### 5. Delivery Status Tracking
**Table Storage Schema**:
```
PartitionKey: activityId
RowKey: targetInbox
Status: pending/delivered/failed
LastAttempt: timestamp
NextRetry: timestamp
ErrorMessage: string
```

## Implementation Phases

### Phase 4A: HTTP Signature Infrastructure
**Objective**: Implement cryptographic signing capability

**Tasks**:
1. Create SignatureBuilder.fs module
2. Implement signature string construction
3. Implement RSA-SHA256 signing with Key Vault
4. Implement digest computation (SHA-256)
5. Implement header formatting
6. Create test scripts for signature verification

**Success Criteria**:
- Generate valid HTTP signatures
- Signatures verify against test vectors
- Test against Mastodon test instance

### Phase 4B: Inbox Discovery & Caching
**Objective**: Discover follower inboxes efficiently

**Tasks**:
1. Create InboxDiscovery.fs module
2. Implement actor fetching (with HTTP signatures for authorized fetch)
3. Implement inbox extraction (shared vs individual)
4. Implement inbox caching (Azure Table Storage)
5. Implement shared inbox grouping

**Success Criteria**:
- Discover inboxes from follower URIs
- Cache discovered inboxes
- Group followers by shared inbox

### Phase 4C: Delivery Queue System
**Objective**: Queue delivery tasks for async processing

**Tasks**:
1. Create Azure Queue Storage queue (activitypub-delivery)
2. Create QueueDeliveryTasks Azure Function (Timer trigger)
3. Implement delivery task creation
4. Implement queue message serialization
5. Configure queue visibility timeout

**Success Criteria**:
- New content triggers delivery queueing
- Delivery tasks queued with proper format
- Queue accessible from Azure Functions

### Phase 4D: Delivery Worker Implementation
**Objective**: Process queued deliveries with retries

**Tasks**:
1. Create ProcessDelivery Azure Function (Queue trigger)
2. Implement HTTP POST with signatures
3. Implement status code handling
4. Implement exponential backoff retry
5. Implement delivery status tracking
6. Configure Application Insights logging

**Success Criteria**:
- Activities delivered to remote inboxes
- HTTP signatures validate correctly
- Errors handled with proper retries
- Delivery status tracked in Table Storage

### Phase 4E: Testing & Validation
**Objective**: Comprehensive delivery testing

**Tasks**:
1. Test delivery to Mastodon test instance
2. Validate HTTP signature acceptance
3. Test error handling and retries
4. Monitor delivery performance
5. Validate against ActivityPub test suite

**Success Criteria**:
- Activities appear on remote servers
- Zero signature verification failures
- Proper error recovery
- Delivery monitoring dashboard

## Success Metrics

**Phase 4 Complete When**:
- ✅ HTTP signatures generate correctly
- ✅ Activities delivered to follower inboxes
- ✅ Deliveries verified on remote servers (Mastodon)
- ✅ Error handling covers all status codes
- ✅ Retry logic implements exponential backoff
- ✅ Delivery status tracked and queryable
- ✅ No blocking of build process
- ✅ Application Insights monitoring configured

## References

**ActivityPub Specification**: https://www.w3.org/TR/activitypub/  
**HTTP Signatures**: https://swicg.github.io/activitypub-http-signature/  
**Mastodon Documentation**: https://docs.joinmastodon.org/spec/security/  
**Azure Functions Error Handling**: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-error-pages  
**Mastodon Source (Delivery)**: mastodon/mastodon GitHub repository  
**Static Site ActivityPub Guide**: https://maho.dev/2024/04/a-guide-to-implementing-activitypub-in-a-static-site-or-any-website-part-6/

## Next Steps

Ready to proceed with Phase 4A: HTTP Signature Infrastructure implementation.

**Estimated Timeline**:
- Phase 4A: HTTP Signature Infrastructure - 2-3 days
- Phase 4B: Inbox Discovery & Caching - 1-2 days
- Phase 4C: Delivery Queue System - 1 day
- Phase 4D: Delivery Worker Implementation - 2-3 days
- Phase 4E: Testing & Validation - 2-3 days

**Total Estimated Duration**: 8-12 days for complete implementation

---

*Research conducted using Perplexity (comprehensive ActivityPub delivery analysis), DeepWiki (Mastodon implementation patterns), and Microsoft Documentation (Azure Functions best practices)*
