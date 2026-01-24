# Phase 4A: ActivityPub Inbox Handler Implementation

**Project**: ActivityPub Federation Infrastructure - Phase 4A  
**Phase**: 4A - Inbox Handler & Follower Management  
**Started**: 2026-01-18  
**Status**: 🟢 Active Development  
**Aligned With**: phase4-implementation-plan.md, ACTIVITYPUB.md, implementation-status.md

## 📋 Project Overview

Implement production-ready ActivityPub inbox handler accepting Follow/Unfollow requests with persistent follower state management in Azure Table Storage. This enables the static site to accept followers from Mastodon and other Fediverse servers while maintaining static followers.json for public accessibility.

**Note**: Original plan included queue-based async delivery using ProcessAccept function, but this was simplified due to Azure Static Web Apps deployment constraints (only HTTP triggers supported). Accept delivery now happens synchronously within inbox handler.

### Success Criteria (Phase 4A Specific)
- ✅ Accept Follow requests via `/api/activitypub/inbox` POST endpoint
- ✅ HTTP signature verification using existing Key Vault integration
- ✅ Store follower state in Azure Table Storage (`followers` table)
- ✅ Deliver Accept activities with HTTP signatures to follower inboxes (synchronous)
- ✅ Handle Undo (unfollow) activities and remove from Table Storage
- ✅ Maintain static `api/data/followers.json` regenerated from Table Storage during builds
- ✅ Idempotent activity processing with de-duplication

### Deployment Constraints

**Azure Static Web Apps Limitation**: Only HTTP-triggered functions are supported in the `/api` folder. Queue-triggered, timer-triggered, and other trigger types require a standalone Azure Function App.

**Impact on Design**:
- Accept activities are delivered synchronously (not queued)
- No automatic retry for failed deliveries
- Suitable for current scale (personal site with limited followers)
- Future migration to standalone Function App may be needed for high-volume scenarios

### Architectural Alignment

**Existing Implementation (Phases 1-3 Complete)**:
- ✅ Phase 1: Discovery & URL Standardization (`/.well-known/webfinger`, actor endpoint)
- ✅ Phase 2: Follow/Accept Workflow & Key Vault Security (HTTP signatures with Key Vault)
- ✅ Phase 3: Outbox Automation (1,547 activities from F# build process)

**Current JavaScript Infrastructure**:
- ✅ Existing inbox handler at `api/inbox/index.js` with Follow/Accept workflow
- ✅ HTTP signature verification in `api/utils/signatures.js`
- ✅ Key Vault integration in `api/utils/keyvault.js`
- ✅ File-based followers management in `api/utils/followers.js`

**Phase 4A Enhancement Strategy**:
- **Keep**: Existing JavaScript Azure Functions for inbox/outbox endpoints
- **Add**: Azure Table Storage for persistent follower state (replacing file-based)
- **Add**: Azure Queue Storage for async Accept activity delivery
- **Add**: F# build-time static file generation from Table Storage
- **Enhance**: Existing inbox handler with Table Storage integration
- **Maintain**: Static `followers.json` for public discoverability and spec compliance

## 🔬 Research Insights

### Recent HTTP Signature Verification Fixes (January 2026)

**Status**: ✅ Phases 1-5 Complete (PR #1924 ready to merge)

HTTP signature verification was temporarily disabled in PR #1855 due to path mismatch issues. This has been comprehensively fixed with research-backed implementation:

**Completed Enhancements**:
1. **Path Reconstruction Fix** - Uses `x-ms-original-url` to handle Azure SWA routing
2. **Multi-Algorithm Digest** - SHA-256 and SHA-512 support with validation
3. **Timestamp Validation** - 5-minute window prevents replay attacks
4. **Feature Flag Control** - Safe rollout with `ACTIVITYPUB_VERIFY_SIGNATURES` env var
5. **Security Hardening** - Invalid date detection, malformed URL rejection, case-insensitive algorithm matching

**Documentation**: `docs/activitypub/http-signature-verification-plan.md`  
**Branch**: `feature/http-signature-verification`  
**Next**: Merge PR #1924 and enable feature flag for production testing

This ensures Phase 4A has robust, production-ready signature verification for incoming Follow activities.

---

### Critical Implementation Requirements

**1. HTTP Signature Verification (RFC 9421)**
- **MUST verify**: `@target-uri`, `Date`, `Host` headers, plus `Content-Type` and `Digest` for POST
- **Algorithm**: Use `hs2019` with algorithm detection from public key metadata (ed25519 or RSA-SHA256)
- **Digest Validation**: SHA-256 hash of request body MUST match Digest header for POST requests
- **Origin Verification**: Verify keyId → actor → public key chain to prevent spoofing
- **Double-Knocking**: Attempt alternative algorithm interpretations if verification fails (interoperability)

**2. Activity Validation**
- **Required Properties**: `type`, `actor`, `id`, plus type-specific requirements
- **Content Spoofing Prevention**: Fetch objects from origin servers to verify authenticity
- **Actor Ownership**: Verify actor owns the objects they're creating/updating/deleting
- **HTML Sanitization**: Parse and sanitize `content` fields to prevent injection attacks

**3. Azure Storage Architecture**
- **Cosmos DB Document Structure**:
  - Partition key: `/receivingActor` for efficient inbox queries
  - Document includes: activity JSON-LD, processing status, timestamps, validation results
  - Indexes on: `activityId`, `receivedAt`, `processingStatus`
- **Idempotency**: Check activity `id` before processing, store processing state atomically
- **TTL**: Configure 90-day retention for completed activities, indefinite for failed

**4. Error Handling & Reliability**
- **Exponential Backoff**: Base 1s delay, retry up to 5 times (1s, 2s, 4s, 8s, 16s) with jitter
- **Status Code Handling**: Retry 5xx and 429, don't retry most 4xx (bad requests)
- **Rate Limiting**: Respect Retry-After headers, implement per-server backoff state
- **Permanent Failures**: Log but don't retry 404, 410, 401, 403

**5. Security Best Practices**
- **Authorized Fetch**: Require HTTP signatures on GET requests for private content
- **Instance Actor**: Server-level actor for signing GET requests without circular dependencies
- **Private Addressing**: Strip `bto`/`bcc` properties before forwarding
- **Rate Limiting**: Multi-level protection (network layer + function app tracking)
- **HTTPS Enforcement**: Verify SSL certificates on all outbound federation requests

## 📁 Architecture Design

### Hybrid JavaScript + F# Approach

**Strategy**: Enhance existing JavaScript Azure Functions with Azure Storage backend, use F# for build-time static file generation.

```
api/
  ├── inbox/
  │   ├── function.json          # ✅ Existing HTTP trigger
  │   └── index.js               # ✅ Enhanced with Table Storage + synchronous Accept delivery
  ├── utils/
  │   ├── signatures.js          # ✅ Existing HTTP signature verification
  │   ├── keyvault.js            # ✅ Existing Key Vault integration
  │   ├── followers.js           # 🔄 Enhanced for Table Storage
  │   └── tableStorage.js        # ➕ NEW: Table Storage operations
  └── data/
      └── followers.json         # 🔄 Generated from Table Storage

Services/
  └── ActivityPub/
      └── FollowersSync.fs       # ➕ NEW: Build-time Table Storage → static file
```

**Note**: Originally planned to use ProcessAccept queue-triggered function for async delivery, but removed due to Azure Static Web Apps constraint (only HTTP triggers supported). Accept delivery now happens synchronously in inbox handler.

### Data Flow (Phase 4A)

**Follow Request Flow**:
1. **Incoming Follow** → Azure Functions HTTP POST `/api/activitypub/inbox`
2. **Signature Verification** → Existing `signatures.js` validates HTTP signature
3. **Activity Validation** → Validate Follow activity structure and actor
4. **Table Storage** → Store follower in `followers` table
5. **Sign & Deliver Accept** → Generate HTTP signature using Key Vault and deliver to follower inbox
6. **Immediate Response** → Return 202 Accepted (follower accepted, delivery attempted)

**Build-Time Sync** (F#):
7. **F# Build** → `FollowersSync.fs` queries Table Storage during site build
8. **Generate Static** → Write `api/data/followers.json` for public accessibility

### Azure Table Storage Schema

**Table: `followers`**
```typescript
{
  PartitionKey: string,           // "follower" (single partition for simplicity)
  RowKey: string,                 // Follower actor URL (e.g., "https://mastodon.social/users/alice")
  actorUrl: string,               // Same as RowKey
  inbox: string,                  // Follower's inbox URL
  followedAt: string,             // ISO 8601 timestamp
  displayName: string,            // Optional: Actor's display name
  followActivityId: string,       // Original Follow activity ID
  Timestamp: DateTime             // Azure Table Storage automatic field
}
```

**Table: `deliverystatus`** (Future - Phase 4B/4C)
```typescript
{
  PartitionKey: string,           // Activity ID
  RowKey: string,                 // Target inbox URL
  status: string,                 // "pending" | "delivered" | "failed"
  attemptCount: number,           // Retry attempts
  lastAttempt: string,            // ISO 8601 timestamp
  lastError: string               // Error message if failed
}
```

## 🔧 Implementation Phases

### Phase 4A-1: Azure Resources & Table Storage (Day 1)

**Status**: ✅ COMPLETE

- [x] Create Azure Storage Account (`lqdevactivitypub`)
- [x] Create `followers` table in Table Storage
- [x] Create `accept-delivery` queue in Queue Storage
- [x] Configure GitHub Secrets (ACTIVITYPUB_STORAGE_CONNECTION)
- [x] Configure Azure Static Web App settings
- [x] Create `api/utils/tableStorage.js` with follower operations
- [x] Add @azure/data-tables NPM dependency to api/package.json
- [x] Test Table Storage connectivity from Azure Functions (7/7 tests passed)

### Phase 4A-2: Enhanced Inbox Handler (Day 1-2)

**Status**: ✅ COMPLETE

- [x] Review existing `api/inbox/index.js` implementation
- [x] Created `api/utils/tableStorage.js` with Table Storage operations (CRUD, collection builder)
- [x] Created `api/utils/queueStorage.js` for Accept delivery queueing
- [x] Enhanced inbox handler with idempotent Follow processing (duplicate activity ID checks)
- [x] Queue Accept activities to `accept-delivery` queue instead of immediate delivery
- [x] Add comprehensive logging to Application Insights
- [x] Added @azure/data-tables and @azure/storage-queue NPM dependencies
- [x] Test Table Storage connectivity (7/7 tests passed)

### Phase 4A-3: Accept Delivery Function (Day 2)

**Status**: ✅ COMPLETE

- [x] Create `api/ProcessAccept/function.json` (queue trigger)
- [x] Create `api/ProcessAccept/index.js` with Accept delivery logic
- [x] Reuse existing `signatures.js` for HTTP signature generation
- [x] Reuse existing `keyvault.js` for signing with Azure Key Vault
- [x] Implement exponential backoff retry (automatic via queue visibility timeout)
- [x] Add delivery status logging to Application Insights
- [x] Updated `api/followers/index.js` to read from Table Storage
- [ ] Test Accept delivery to Mastodon test account (pending Phase 4A-5)

### Phase 4A-4: F# Static File Generation (Day 2-3)

**Status**: ✅ COMPLETE

- [x] Create `Services/FollowersSync.fs` module
- [x] Add Table Storage SDK reference to PersonalSite.fsproj
- [x] Implement `getFollowersFromTableStorage()` function
- [x] Implement `buildFollowersCollection()` function
- [x] Integrate with Program.fs build pipeline
- [x] Fix F# compilation errors (resolved by restructuring JSON generation)
- [x] Test build-time followers.json generation
- [x] Validate followers endpoint returns correct data

### Phase 4A-5: Testing & Validation (Day 3)

- [ ] Test complete Follow workflow with real Mastodon account
- [ ] Verify follower appears in Table Storage
- [ ] Verify Accept activity delivered to follower inbox
- [ ] Verify followers.json regenerated during build
- [ ] Test Undo (unfollow) workflow
- [ ] Verify idempotent processing (duplicate Follow requests)
- [ ] Load testing with multiple simultaneous Follow requests
- [ ] Document Phase 4A completion and lessons learned

## 🎯 Current Focus: Phase 4A-5 - Testing & Validation

### Implementation Status Summary

**✅ COMPLETE: Phases 4A-1, 4A-2, 4A-3, 4A-4**
- ✅ Azure resources: Storage Account, Table Storage, Queue Storage, App Insights
- ✅ GitHub secrets and Azure Static Web App settings configured
- ✅ Table Storage utility module (`api/utils/tableStorage.js`) with full CRUD operations
- ✅ Queue Storage utility module (`api/utils/queueStorage.js`)
- ✅ Enhanced inbox handler with idempotent Follow processing
- ✅ ProcessAccept queue-triggered function for async delivery
- ✅ Updated followers endpoint to read from Table Storage
- ✅ Table Storage connectivity validated (7/7 tests passed)
- ✅ Created `Services/FollowersSync.fs` module for build-time static file generation
- ✅ Integrated with Program.fs build pipeline
- ✅ F# compilation successful (resolved JSON generation issues)
- ✅ Build process generates api/data/followers.json correctly
- ✅ Git commits: 
  - "feat(activitypub): Phase 4A implementation - Table Storage integration"
  - "wip(activitypub): Phase 4A-4 F# static file generation (in progress)"

**🟢 READY FOR: Phase 4A-5**
- All technical components implemented and validated
- Build process tested and working correctly
- followers.json generation confirmed
- Ready for end-to-end testing with real Mastodon account

### Next Immediate Steps

1. **Phase 4A-5**: End-to-end testing with real Mastodon account
2. **Verify complete workflow**: Follow → Accept → followers.json
3. **Test Undo workflow**: Unfollow removes from Table Storage
4. **Document completion**: Phase 4A lessons learned

## 📚 References

### Existing Documentation Alignment

- **Phase 4 Overall Plan**: [`docs/activitypub/phase4-implementation-plan.md`](../../docs/activitypub/phase4-implementation-plan.md)
- **Architecture Reference**: [`docs/activitypub/follower-management-architecture.md`](../../docs/activitypub/follower-management-architecture.md)
- **API Documentation**: [`api/ACTIVITYPUB.md`](../../api/ACTIVITYPUB.md)
- **Implementation Status**: [`docs/activitypub/implementation-status.md`](../../docs/activitypub/implementation-status.md)

### Technical Specifications

- **RFC 9421**: HTTP Message Signatures - https://www.rfc-editor.org/rfc/rfc9421
- **W3C ActivityPub**: https://www.w3.org/TR/activitypub/
- **HTTP Signatures Profile**: https://swicg.github.io/activitypub-http-signature/
- **Azure Table Storage SDK**: https://learn.microsoft.com/en-us/javascript/api/@azure/data-tables
- **Azure Queue Storage SDK**: https://learn.microsoft.com/en-us/javascript/api/@azure/storage-queue
- **Azure Functions Best Practices**: https://learn.microsoft.com/en-us/azure/azure-functions/functions-best-practices

### Research Summary

**Key Findings from Research**:
- HTTP signatures MUST verify `@target-uri`, `Date`, `Host`, `Content-Type`, and `Digest` headers
- Activity validation must verify actor ownership to prevent spoofing
- Table Storage provides excellent persistence for follower state (<$0.02/month)
- Queue-based async delivery prevents blocking on slow remote servers
- Idempotent processing critical for handling duplicate activities
- Static followers.json maintains spec compliance and public accessibility

## 🔄 Phase Completion Criteria

**Phase 4A considered complete when**:
- [ ] Follow requests successfully stored in Table Storage
- [ ] Accept activities delivered to follower inboxes with HTTP signatures
- [ ] Unfollow (Undo) activities properly remove followers
- [ ] Static followers.json regenerated during F# build process
- [ ] End-to-end testing with real Mastodon account successful
- [ ] Comprehensive documentation and lessons learned captured
- [ ] Ready to proceed to Phase 4B (Post Delivery)
