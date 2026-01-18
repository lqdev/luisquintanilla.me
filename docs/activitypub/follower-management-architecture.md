# Follower Management Architecture for Static Sites

**Date**: January 18, 2026  
**Status**: Architecture Documentation  
**Context**: How followers are managed in static site ActivityPub implementations

---

## The Critical Question

**Q**: "Given my static site and static resources, how are followers managed and updated?"

**A**: **Followers CANNOT be managed purely statically**. Your static site needs Azure Functions (or similar serverless backend) to handle incoming Follow activities and maintain follower state.

This is a **fundamental requirement** for ActivityPub federation—there's no way around it.

---

## The Fundamental Architecture Challenge

### Why Static Sites Need Dynamic Backends

ActivityPub is a **push-based, real-time protocol** that requires:

1. **Server-side processing** to receive POST requests at `/inbox`
2. **Immediate responses** to Follow activities with Accept activities
3. **Dynamic state management** for follower lists
4. **Cryptographic signing** of outbound Accept activities

**Key Insight**: Even though you call it a "static site," the ActivityPub inbox MUST be dynamic. However, the good news is:
- **99% of your site remains static** (HTML, CSS, posts, images)
- **Only the inbox endpoint needs serverless processing**
- **Follower data lives separately** from static content

---

## Your Current Implementation Status

### ✅ What You Have (Phases 1-3)

| Phase | Component | Status | What It Does |
|-------|-----------|--------|--------------|
| **Phase 1** | Discovery | ✅ Complete | `.well-known/webfinger` + actor endpoint |
| **Phase 2** | Keys & Structure | ✅ Complete | Private key in Azure Key Vault, actor JSON |
| **Phase 3** | Outbox | ✅ Complete | 1,547 activities automatically generated |

### ❌ What You're Missing (Critical for Followers)

| Component | Status | What It Does | Priority |
|-----------|--------|--------------|----------|
| **Inbox Handler** | ⚠️ Not Implemented | Receives Follow activities via POST | **CRITICAL** |
| **Follower Storage** | ⚠️ Not Implemented | Stores follower list (Azure Table Storage) | **CRITICAL** |
| **Accept Generator** | ⚠️ Not Implemented | Sends Accept activities to followers | **CRITICAL** |
| **Signature Validator** | ⚠️ Not Implemented | Verifies incoming HTTP signatures | **HIGH** |

**Without these components, your site cannot accept followers.**

---

## How Follower Management Actually Works

### The Complete Follower Lifecycle

```
┌─────────────────────────────────────────────────────────────────────┐
│ 1. DISCOVERY (✅ Working)                                           │
│    User searches for "lqdev@lqdev.me" on Mastodon                  │
│    → Mastodon queries .well-known/webfinger (Azure Function)       │
│    → Returns actor URL: https://lqdev.me/api/activitypub/actor     │
└─────────────────────────────────────────────────────────────────────┘
                                ↓
┌─────────────────────────────────────────────────────────────────────┐
│ 2. FOLLOW REQUEST (⚠️ Not Implemented Yet)                          │
│    User clicks "Follow" button on Mastodon                         │
│    → Mastodon POSTs Follow activity to your inbox                  │
│    → https://lqdev.me/api/activitypub/inbox (Azure Function needed)│
│                                                                     │
│    Follow Activity Structure:                                      │
│    {                                                                │
│      "@context": "https://www.w3.org/ns/activitystreams",         │
│      "id": "https://mastodon.social/abc-123",                     │
│      "type": "Follow",                                             │
│      "actor": "https://mastodon.social/users/alice",              │
│      "object": "https://lqdev.me/api/activitypub/actor"           │
│    }                                                                │
└─────────────────────────────────────────────────────────────────────┘
                                ↓
┌─────────────────────────────────────────────────────────────────────┐
│ 3. SIGNATURE VALIDATION (⚠️ Not Implemented Yet)                    │
│    Azure Function receives Follow activity                          │
│    → Extracts Signature header from HTTP request                   │
│    → Fetches follower's public key from their actor URL            │
│    → Verifies cryptographic signature                              │
│    → Rejects if invalid (prevents impersonation)                   │
└─────────────────────────────────────────────────────────────────────┘
                                ↓
┌─────────────────────────────────────────────────────────────────────┐
│ 4. FOLLOWER STORAGE (⚠️ Not Implemented Yet)                        │
│    Azure Function stores follower in Azure Table Storage:          │
│                                                                     │
│    PartitionKey: "follower"                                        │
│    RowKey: "https://mastodon.social/users/alice"                  │
│    InboxUrl: "https://mastodon.social/users/alice/inbox"          │
│    SharedInbox: "https://mastodon.social/inbox"                   │
│    FollowedAt: "2026-01-18T15:30:00Z"                             │
│    Domain: "mastodon.social"                                       │
└─────────────────────────────────────────────────────────────────────┘
                                ↓
┌─────────────────────────────────────────────────────────────────────┐
│ 5. ACCEPT ACTIVITY (⚠️ Not Implemented Yet)                         │
│    Azure Function generates Accept activity:                        │
│                                                                     │
│    {                                                                │
│      "@context": "https://www.w3.org/ns/activitystreams",         │
│      "id": "https://lqdev.me/api/activitypub/actor#accept-abc-123",│
│      "type": "Accept",                                             │
│      "actor": "https://lqdev.me/api/activitypub/actor",           │
│      "object": { /* original Follow activity */ }                  │
│    }                                                                │
│                                                                     │
│    → Signs with HTTP signature using private key from Key Vault    │
│    → POSTs to follower's inbox                                     │
│    → Follower sees "Following" status change on Mastodon           │
└─────────────────────────────────────────────────────────────────────┘
                                ↓
┌─────────────────────────────────────────────────────────────────────┐
│ 6. DELIVERY TO FOLLOWERS (Phase 4 - Not Yet Implemented)           │
│    When you publish new content during build:                      │
│    → Build generates outbox activities (✅ already working)         │
│    → Azure Function retrieves followers from Table Storage         │
│    → Groups followers by shared inbox for efficiency               │
│    → Signs Create activities with HTTP signatures                  │
│    → POSTs to each follower inbox/shared inbox                     │
│    → Followers see your new post in their timeline                 │
└─────────────────────────────────────────────────────────────────────┘
```

---

## The Missing Infrastructure: Inbox Handler

### What Needs to Be Built

#### Azure Function: `InboxHandler`

**Trigger**: HTTP POST to `/api/activitypub/inbox`  
**Purpose**: Process incoming ActivityPub activities  
**Authentication**: HTTP Signature validation

**Core Responsibilities**:

1. **Receive Follow Activities**
   - Parse incoming JSON activity
   - Validate activity structure
   - Extract actor URI

2. **Validate HTTP Signatures**
   - Extract Signature header from request
   - Fetch remote actor's public key
   - Compute signature verification
   - Reject invalid signatures (401 Unauthorized)

3. **Fetch Remote Actor Information**
   - GET request to actor URI
   - Extract inbox URL (critical for Accept response)
   - Extract public key (for future signature validation)
   - Optional: Cache actor information

4. **Store Follower Record**
   - Add to Azure Table Storage
   - Store: actor URI, inbox URL, shared inbox, timestamp, domain
   - Idempotent: Handle duplicate Follow requests

5. **Send Accept Activity**
   - Construct Accept activity wrapping original Follow
   - Sign with HTTP signature (using private key from Key Vault)
   - POST to follower's inbox URL
   - Handle delivery errors with retry logic

6. **Handle Unfollow Activities**
   - Process Undo(Follow) activities
   - Remove from Azure Table Storage
   - Send Accept activity confirming removal
   - Update follower count

---

## Follower Storage Architecture

### Azure Table Storage Schema

**Table Name**: `followers`

**Entity Structure**:
```json
{
  "PartitionKey": "follower",           // All followers in same partition
  "RowKey": "https://mastodon.social/users/alice",  // Unique actor URI
  "InboxUrl": "https://mastodon.social/users/alice/inbox",
  "SharedInbox": "https://mastodon.social/inbox",  // For efficiency
  "DisplayName": "Alice Smith",
  "Domain": "mastodon.social",          // For grouping/analytics
  "FollowedAt": "2026-01-18T15:30:00Z",
  "PublicKeyPem": "-----BEGIN PUBLIC KEY-----...",  // Cached for signature validation
  "AvatarUrl": "https://mastodon.social/avatars/alice.jpg",  // Optional
  "LastSeenAt": "2026-01-18T16:00:00Z"  // Optional: track engagement
}
```

**Query Patterns**:
- **Get all followers**: Query by PartitionKey = "follower"
- **Check if user is follower**: Get entity by RowKey = actor URI
- **Group by domain**: Filter or group by Domain property
- **Get shared inbox targets**: Group by SharedInbox for delivery optimization

---

## Critical Implementation Details

### 1. HTTP Signature Validation (Security Critical)

**Why It's Required**:
- Prevents impersonation attacks
- Mastodon/fediverse servers REQUIRE signatures
- Without validation, anyone can fake Follow requests

**Process**:
```fsharp
// Pseudo-code for signature validation
let validateHttpSignature (request: HttpRequest) : Async<ValidationResult> = async {
    // 1. Extract Signature header
    let signatureHeader = request.Headers.["Signature"]
    
    // 2. Parse signature components
    let keyId = extractKeyId signatureHeader
    let signature = extractSignature signatureHeader
    let signedHeaders = extractHeadersList signatureHeader
    
    // 3. Fetch remote actor's public key
    let! actor = fetchActor keyId  // HTTP GET to remote server
    let publicKey = actor.PublicKey.PublicKeyPem
    
    // 4. Construct signature string from request headers
    let signatureString = constructSignatureString request signedHeaders
    
    // 5. Verify signature cryptographically
    return verifyRsaSignature publicKey signature signatureString
}
```

### 2. Accept Activity Generation

**Critical Detail**: The `object` field MUST contain the **complete original Follow activity**, not just a reference.

**Correct Format**:
```json
{
  "@context": "https://www.w3.org/ns/activitystreams",
  "id": "https://lqdev.me/api/activitypub/actor#accept-abc-123",
  "type": "Accept",
  "actor": "https://lqdev.me/api/activitypub/actor",
  "object": {
    "@context": "https://www.w3.org/ns/activitystreams",
    "id": "https://mastodon.social/abc-123",
    "type": "Follow",
    "actor": "https://mastodon.social/users/alice",
    "object": "https://lqdev.me/api/activitypub/actor"
  }
}
```

**Common Mistake**:
```json
{
  "object": "https://mastodon.social/abc-123"  // ❌ Wrong! Must be full activity
}
```

### 3. Shared Inbox Optimization

**Problem**: If you have 100 followers on mastodon.social, sending 100 individual POST requests is inefficient.

**Solution**: Use shared inbox when available.

**Implementation**:
```fsharp
// Group followers by shared inbox
let groupedFollowers = 
    followers
    |> List.groupBy (fun f -> f.SharedInbox |> Option.defaultValue f.InboxUrl)
    |> List.map (fun (inbox, followers) -> 
        (inbox, followers |> List.map (fun f -> f.ActorUri)))

// Deliver once to each unique inbox
for (inboxUrl, followerActors) in groupedFollowers do
    let! _ = deliverActivity inboxUrl activity
    printfn $"Delivered to {inboxUrl} for {followerActors.Length} followers"
```

**Result**: 100 followers → 1 POST request instead of 100!

---

## Integration with Existing Architecture

### How Inbox Handler Connects to Your Current Setup

```
┌──────────────────────────────────────────────────────────────────────┐
│ STATIC SITE (Azure Blob Storage)                                     │
│                                                                       │
│  • HTML/CSS/JavaScript                           ✅ Already Working  │
│  • Post content (markdown → HTML)                                    │
│  • Images and media                                                  │
│  • RSS feeds                                                         │
│                                                                       │
└──────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────┐
│ ACTIVITYPUB STATIC RESOURCES (Azure Blob Storage)                    │
│                                                                       │
│  • /api/activitypub/outbox/index.json (1,547)   ✅ Already Working  │
│  • /api/activitypub/actor (actor JSON)          ✅ Already Working  │
│  • /.well-known/webfinger                       ✅ Already Working  │
│                                                                       │
└──────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────┐
│ AZURE FUNCTIONS (Serverless Backend) - NEW COMPONENTS NEEDED         │
│                                                                       │
│  ⚠️ /api/activitypub/inbox (HTTP POST handler)  NOT YET IMPLEMENTED │
│     • Receives Follow/Unfollow activities                            │
│     • Validates HTTP signatures                                      │
│     • Stores followers in Table Storage                              │
│     • Sends Accept activities                                        │
│                                                                       │
│  ⚠️ QueueDeliveryTasks (Timer trigger)           NOT YET IMPLEMENTED │
│     • Runs when new content published                                │
│     • Retrieves followers from Table Storage                         │
│     • Queues delivery tasks                                          │
│                                                                       │
│  ⚠️ ProcessDelivery (Queue trigger)              NOT YET IMPLEMENTED │
│     • Processes delivery queue                                       │
│     • Signs activities with HTTP signatures                          │
│     • POSTs to follower inboxes                                      │
│     • Handles retries and errors                                     │
│                                                                       │
└──────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────┐
│ AZURE KEY VAULT (Secure Key Storage)                                 │
│                                                                       │
│  • Private key for signing                       ✅ Already Working  │
│  • Public key published in actor JSON            ✅ Already Working  │
│                                                                       │
└──────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────┐
│ AZURE TABLE STORAGE (Follower State) - NEW COMPONENT NEEDED          │
│                                                                       │
│  ⚠️ Table: followers                             NOT YET IMPLEMENTED │
│     • Actor URI (RowKey)                                             │
│     • Inbox URL                                                      │
│     • Shared inbox                                                   │
│     • Follow timestamp                                               │
│     • Domain                                                         │
│                                                                       │
└──────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────┐
│ AZURE QUEUE STORAGE (Delivery Tasks) - NEW COMPONENT NEEDED          │
│                                                                       │
│  ⚠️ Queue: activitypub-delivery                  NOT YET IMPLEMENTED │
│     • Delivery task messages                                         │
│     • Retry logic                                                    │
│     • Exponential backoff                                            │
│                                                                       │
└──────────────────────────────────────────────────────────────────────┘
```

---

## Why This Architecture is Necessary

### Attempted "Pure Static" Approaches That Don't Work

❌ **Storing followers.json as static file**  
**Problem**: Who updates it? Can't receive POST requests to static files.

❌ **Using only outbox feed without inbox**  
**Problem**: Mastodon won't pull from your outbox unless users explicitly follow you. Following requires the Follow/Accept handshake via inbox.

❌ **Manual follower management**  
**Problem**: You'd have to manually add every follower to a JSON file. Not scalable and breaks federation protocol.

❌ **Using third-party service for inbox**  
**Problem**: Trust issues, data privacy, cost, and dependency on external service.

### ✅ The Correct Hybrid Architecture

**Philosophy**: "Static where possible, dynamic where necessary"

- **Static content**: 99% of your site (HTML, CSS, posts, images, outbox)
- **Dynamic backend**: Only for real-time ActivityPub protocol requirements
- **Serverless functions**: Pay only for actual usage (very low cost)
- **Azure integration**: Everything in one ecosystem (Key Vault, Table Storage, Functions)

---

## Cost Implications

### Azure Services Required for Follower Management

| Service | Purpose | Estimated Cost (100 followers, 1 post/week) |
|---------|---------|---------------------------------------------|
| **Azure Functions** | Inbox handler + delivery | $0-5/month (Consumption plan) |
| **Azure Table Storage** | Follower list | $0.01/month (negligible) |
| **Azure Queue Storage** | Delivery queue | $0.01/month (negligible) |
| **Azure Key Vault** | Private key storage | ✅ Already paying (~$0.03/month) |
| **Azure Blob Storage** | Static site hosting | ✅ Already paying (~$0.02/month) |

**Total New Cost**: ~$0-5/month for full ActivityPub federation capability

**Note**: Functions on Consumption plan include 1 million free executions/month. With typical usage (few Follow requests, weekly posts), you'll likely stay in free tier.

---

## Implementation Priority

### What You Need to Build (In Order)

**Phase 4A: Inbox Handler (CRITICAL - Blocks all follower management)**
1. Create Azure Function for `/api/activitypub/inbox` POST endpoint
2. Implement HTTP signature validation
3. Implement Follow activity processing
4. Implement Accept activity generation and delivery
5. Implement Unfollow (Undo) activity processing

**Phase 4B: Follower Storage (CRITICAL - Required by inbox handler)**
1. Create Azure Table Storage table
2. Implement follower CRUD operations
3. Implement follower querying and grouping
4. Implement domain-based analytics (optional)

**Phase 4C: Delivery Queue (Important - For broadcasting posts)**
1. Create Azure Queue Storage queue
2. Implement queue message format
3. Implement queueing logic when new content published
4. Configure visibility timeout and retry policy

**Phase 4D: Delivery Worker (Important - For broadcasting posts)**
1. Create ProcessDelivery Azure Function
2. Implement HTTP signature generation for outbound posts
3. Implement delivery to follower inboxes
4. Implement exponential backoff retry logic
5. Implement delivery status tracking

---

## Key Takeaways

### The Bottom Line

**Your static site CANNOT manage followers without serverless backend functions.**

This is not a limitation of your implementation—it's a fundamental requirement of the ActivityPub protocol. Every ActivityPub server (including Mastodon, Pleroma, PeerTube) has a dynamic inbox endpoint because the protocol is built on real-time, push-based communication.

### What Makes This "Hybrid" Architecture Still Valuable

✅ **99% static**: All content, assets, and read operations remain static  
✅ **Minimal dynamic footprint**: Only inbox and delivery logic require serverless functions  
✅ **Pay-per-use costs**: Serverless functions cost pennies or nothing for typical usage  
✅ **No dedicated servers**: No 24/7 running VPS or maintenance burden  
✅ **Azure integration**: Everything in one ecosystem with unified auth and monitoring  
✅ **Scalable**: Automatically handles traffic spikes without configuration  

### Current Blockers

🚫 **Cannot accept followers** until inbox handler implemented  
🚫 **Cannot deliver posts to followers** until delivery system implemented  
✅ **Can be discovered** via webfinger (Phase 1 complete)  
✅ **Has valid actor** with public key (Phase 2 complete)  
✅ **Has outbox content** with 1,547 activities (Phase 3 complete)  

---

## Next Steps

**Ready to implement Phase 4A (Inbox Handler)?**

This will enable:
- Accepting followers from Mastodon and other fediverse servers
- Building your follower list in Azure Table Storage
- Seeing actual follower counts
- Completing the Follow/Accept handshake

Once the inbox handler is complete, you can proceed with Phase 4B-D to enable broadcasting your new posts to all followers automatically during the build process.

---

**Documentation**: This architecture document should be referenced when planning Phase 4 implementation.  
**Research Validation**: Based on comprehensive research of Maho's implementation, Paul Kinlan's approach, and ActivityPub specification requirements.  
**F# Integration**: All components can be implemented in F# using Azure Functions SDK, matching your existing codebase patterns.
