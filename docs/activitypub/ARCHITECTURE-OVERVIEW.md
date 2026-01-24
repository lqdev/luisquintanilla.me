# ActivityPub Implementation: Architecture & Implementation Overview

**Last Updated**: January 23, 2026  
**Status**: Production - Phases 1-4 Complete (Phase 4: HTTP Signature Verification LIVE 🔒)  
**Primary Maintainer**: See commit history

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [High-Level Architecture](#high-level-architecture)
3. [Implementation Phases & Current Status](#implementation-phases--current-status)
4. [Azure Infrastructure](#azure-infrastructure)
5. [URL Structure & Endpoints](#url-structure--endpoints)
6. [Security Architecture](#security-architecture)
7. [Data Flow & Processing](#data-flow--processing)
8. [Cost Analysis](#cost-analysis)
9. [Testing & Validation](#testing--validation)
10. [Reference Documentation](#reference-documentation)

---

## Executive Summary

This repository implements ActivityPub federation for a static website built with F#, enabling participation in the Fediverse (Mastodon, Pleroma, etc.) through a hybrid static+dynamic architecture.

### What This Means

**For Users**: Follow `@lqdev@lqdev.me` from any Mastodon instance to receive new post notifications in your timeline.

**For Developers**: A production-ready reference implementation demonstrating ActivityPub integration with static sites using F# and Azure serverless infrastructure.

### Current Capabilities

✅ **Discoverable** - WebFinger protocol enables account discovery from any Fediverse instance  
✅ **Follow/Accept** - Automatic acceptance of follow requests with HTTP signature verification  
✅ **Persistent Followers** - Azure Table Storage maintains follower state across deployments  
✅ **Automatic Outbox** - F# build process generates 1,547+ ActivityPub activities from website content  
✅ **Post Delivery** - Queue-based asynchronous delivery to all follower inboxes  
✅ **Production Security** - Azure Key Vault for signing keys, HTTP signature validation, managed identity authentication

### Key Architectural Decisions

1. **Hybrid Static+Dynamic**: 99% static files for performance, 1% Azure Functions for protocol requirements
2. **Azure Serverless**: Functions, Table Storage, Queue Storage, Key Vault - no dedicated servers
3. **Queue-Based Delivery**: Async processing with exponential backoff retry for reliability
4. **Table Storage as Truth**: Persistent follower state with static `followers.json` regenerated during builds
5. **Cost-Optimized**: ~$0.02/month operational cost using Azure free tiers

---

## High-Level Architecture

### Static + Dynamic Hybrid Approach

```
┌─────────────────────────────────────────────────────────────────┐
│ STATIC SITE (Azure Blob Storage + CDN)                          │
│                                                                  │
│  • HTML/CSS/JavaScript - Website content                        │
│  • Post content (Markdown → HTML)                               │
│  • Images and media assets                                      │
│  • RSS feeds (all.rss, posts.rss, etc.)                         │
│  • ActivityPub outbox collection (1,547+ activities)            │
│  • Static followers.json (regenerated from Table Storage)       │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ AZURE FUNCTIONS (Serverless Dynamic Backend)                    │
│                                                                  │
│  1. WebFinger /.well-known/webfinger                            │
│     → Returns actor discovery information                        │
│                                                                  │
│  2. Actor /api/activitypub/actor                                │
│     → Serves actor profile with public keys                     │
│                                                                  │
│  3. InboxHandler /api/activitypub/inbox                         │
│     → Receives Follow/Unfollow activities (POST)                │
│     → Validates HTTP signatures                                 │
│     → Stores followers in Table Storage                         │
│     → Queues Accept activities for async delivery               │
│                                                                  │
│  4. ProcessAccept (Queue trigger: accept-delivery)              │
│     → Delivers Accept activities to follower inboxes            │
│     → Signs with HTTP signatures using Key Vault                │
│     → Implements retry with exponential backoff                 │
│                                                                  │
│  5. QueueDeliveryTasks (HTTP trigger: /trigger-delivery)        │
│     → Loads recent activities from outbox                       │
│     → Retrieves all followers from Table Storage                │
│     → Queues delivery tasks for each follower                   │
│                                                                  │
│  6. ProcessDelivery (Queue trigger: activitypub-delivery)       │
│     → Delivers Create activities to follower inboxes            │
│     → Signs with HTTP signatures using Key Vault                │
│     → Tracks delivery status in Table Storage                   │
│     → Implements retry with exponential backoff                 │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ AZURE KEY VAULT (Secure Key Management)                         │
│                                                                  │
│  • RSA private key for signing (2048-bit)                       │
│  • Managed identity authentication (no secrets in code)         │
│  • RBAC access control (Key Vault Crypto User role)             │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ AZURE TABLE STORAGE (Persistent State)                          │
│                                                                  │
│  Table: followers                                               │
│  • PartitionKey: "follower"                                     │
│  • RowKey: Actor URI (unique follower identifier)               │
│  • Columns: ActorUri, InboxUrl, SharedInboxUrl, Domain,         │
│             FollowedAt, LastDeliveryAttempt, LastDeliveryStatus │
│                                                                  │
│  Table: deliverystatus                                          │
│  • PartitionKey: Activity ID (e.g., "post-12345")               │
│  • RowKey: Follower actor URI                                   │
│  • Columns: ActivityId, Status, AttemptCount, LastAttempt,      │
│             NextRetry, ErrorMessage                             │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ AZURE QUEUE STORAGE (Async Processing)                          │
│                                                                  │
│  Queue: accept-delivery                                         │
│  • Accept activity delivery tasks                               │
│  • Processed by ProcessAccept function                          │
│                                                                  │
│  Queue: activitypub-delivery                                    │
│  • Post delivery tasks (Create activities)                      │
│  • Processed by ProcessDelivery function                        │
│  • Exponential backoff: 2s → 4s → 8s → 16s → 32s → 1 hour      │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ APPLICATION INSIGHTS (Monitoring & Logging)                      │
│                                                                  │
│  • Delivery success/failure rates                               │
│  • HTTP signature validation metrics                            │
│  • Queue processing times                                       │
│  • Error tracking and debugging                                 │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ GITHUB ACTIONS (CI/CD Integration)                              │
│                                                                  │
│  1. Build site (F# → HTML)                                      │
│  2. Generate ActivityPub outbox from UnifiedFeedItems           │
│  3. Deploy to Azure Static Web Apps                             │
│  4. Trigger delivery of new activities to all followers         │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Why This Architecture?

**Static Where Possible**: 99% of website content is static HTML/CSS/JS for exceptional performance, low cost, and CDN caching.

**Dynamic Where Necessary**: ActivityPub is a real-time push-based protocol requiring server-side processing for:
- Receiving POST requests (Follow/Unfollow activities)
- Cryptographic signature validation
- Persistent state management (follower lists)
- Asynchronous activity delivery

**Serverless Functions**: Azure Functions provide the minimal dynamic backend needed without maintaining dedicated servers, resulting in ~$0.02/month operational cost.

---

## Implementation Phases & Current Status

### Phase 1: Discovery & URL Standardization ✅ COMPLETE

**Completion Date**: January 18, 2026

**Implemented**:
- WebFinger discovery endpoint (`/.well-known/webfinger`)
- Actor profile endpoint (`/api/activitypub/actor`)
- Domain standardization (use `lqdev.me` without www)
- URL pattern consistency (`/api/activitypub/*` structure)
- Backward compatibility (WebFinger accepts both `@lqdev.me` and `@www.lqdev.me`)

**Testing**: Automated test suite (`Scripts/test-activitypub.sh`)

**Key Decision**: Use root domain without subdomain for maximum Fediverse compatibility

---

### Phase 2: Follow/Accept Workflow & Security ✅ COMPLETE

**Completion Date**: January 18, 2026

**Implemented**:
- Azure Key Vault integration for signing keys
- HTTP signature verification for incoming activities
- Follow/Accept/Undo activity processing
- Persistent follower storage
- Activity audit logging
- Managed identity authentication (no secrets in code)

**Security Features**:
- RSA 2048-bit key pairs
- HTTP Signatures (RFC 9421/cavage-12)
- Actor verification with remote public key fetching
- Activity structure validation
- Comprehensive audit trail

**Key Decision**: Azure Key Vault for production-grade key management with RBAC

---

### Phase 3: Outbox Automation ✅ COMPLETE

**Completion Date**: January 18, 2026

**Implemented**:
- F# module `ActivityPubBuilder.fs` (286 lines)
- Automatic conversion of `UnifiedFeedItems` to ActivityPub activities
- Build-time generation integrated with `Program.fs`
- 1,547 content items automatically converted
- RFC 3339 compliant dates
- Stable content-hash-based IDs

**Domain Types Created**:
- `ActivityPubNote` - Individual content object
- `ActivityPubCreate` - Wrapper for Note in Create activity
- `ActivityPubOutbox` - OrderedCollection container

**Key Decision**: Generate outbox during F# build process, not runtime

---

### Phase 4: HTTP Signature Verification ✅ COMPLETE 🔒

**Completion Date**: January 23, 2026  
**Duration**: 7.5 hours (including production rollout with 4 hotfixes)  
**Production Status**: LIVE and ENFORCED

**What Was Implemented**:

**Sub-Phases 1-5: Development** (PR #1924)
- Phase 1: Comprehensive diagnostic logging for signature debugging
- Phase 2: Path reconstruction using `x-ms-original-url` for Azure Static Web Apps
- Phase 3: Multi-algorithm digest verification (SHA-256, SHA-512)
- Phase 4: Timestamp validation with 5-minute window (replay attack prevention)
- Phase 5: Feature flag implementation for safe production rollout

**Phase 6: Production Rollout** (January 23, 2026)
- PR #1924 merged (Phases 2-5)
- HOTFIX 1 (763a7981): Added missing function export
- HOTFIX 2 (889e017f): Added missing function definition
- HOTFIX 3 (e5ed53cd): Fixed `logBoth()` declaration order
- Final Fix (84f58c88): Enforced signature requirement when enabled
- Feature flag enabled: `ACTIVITYPUB_VERIFY_SIGNATURES=true`

**Production Behavior**:
- ✅ Valid signatures → Accepted
- ❌ Missing signatures → 401 "HTTP signature required"
- ❌ Invalid signatures → 401 "Invalid HTTP signature"

**Security Enhancements**:
1. Cryptographic request authentication using RSA public keys
2. Replay attack prevention via timestamp validation
3. Body integrity verification with multi-algorithm digest support
4. Correct path reconstruction for Azure Static Web Apps routing
5. Safe rollout with instant rollback capability via feature flag

**Key Learnings**:
- Multi-hotfix debugging required without Application Insights (Azure SWA Free tier)
- JavaScript function hoisting issues with const/let expressions
- Feature flag security logic must ENFORCE requirements, not just verify when present
- Azure Static Web Apps `x-ms-original-url` header critical for path reconstruction

**Documentation**: See `docs/activitypub/phase4-http-signature-verification-complete.md` for complete implementation details, production testing results, and technical specifications.

---

## Azure Infrastructure

### Resource Group: `rg-activitypub` (or integrated with existing)

#### 1. Azure Storage Account: `lqdevactivitypub`

**Tables**:
- `followers` - Follower state management
- `deliverystatus` - Activity delivery tracking

**Queues**:
- `accept-delivery` - Accept activity tasks
- `activitypub-delivery` - Post delivery tasks

**Cost**: ~$0.01-0.02/month (mostly free tier)

#### 2. Azure Key Vault: `lqdev-keyvault` (existing)

**Keys**:
- `activitypub-signing-key` - RSA 2048-bit private key

**Access**:
- Managed identity for Azure Functions
- Key Vault Crypto User RBAC role

**Cost**: ~$0.03/month (10,000 operations free tier)

#### 3. Azure Functions: `lqdev-activitypub-functions`

**Hosting Plan**: Consumption (serverless)

**Functions**:
- WebFinger (HTTP trigger)
- Actor (HTTP trigger)
- InboxHandler (HTTP trigger)
- ProcessAccept (Queue trigger)
- QueueDeliveryTasks (HTTP trigger)
- ProcessDelivery (Queue trigger)

**Cost**: $0-5/month (1 million executions free, typical usage well within)

#### 4. Application Insights: `lqdev-activitypub-insights`

**Purpose**: Monitoring, logging, performance tracking

**Metrics**:
- Delivery success rates
- Signature validation success rates
- Queue processing times
- Error tracking

**Cost**: Included in free tier for typical usage

### Total Infrastructure Cost

**Monthly Estimate**: ~$0.02-5/month (depending on follower count and activity)

**Cost Breakdown**:
- Table Storage: ~$0.01/month
- Queue Storage: ~$0.01/month
- Functions: $0-5/month (likely $0 within free tier)
- Key Vault: ~$0.03/month
- Application Insights: $0 (free tier)

**Comparison**: Traditional Mastodon server costs $100+/month for hosting, maintenance, and storage.

---

## URL Structure & Endpoints

### Current Production Pattern: `/api/activitypub/*`

All ActivityPub endpoints follow a consistent pattern under `/api/activitypub/` for logical grouping and future extensibility.

#### Discovery Endpoint

```
/.well-known/webfinger?resource=acct:lqdev@lqdev.me
```

**Purpose**: Account discovery via WebFinger protocol  
**Implementation**: Azure Function proxy  
**Response**: `application/jrd+json`

#### Core ActivityPub Endpoints

```
/api/activitypub/actor           - Actor profile with public keys
/api/activitypub/inbox           - Receive activities (POST)
/api/activitypub/outbox          - Public activities collection
/api/activitypub/followers       - Followers collection
/api/activitypub/following       - Following collection
/api/activitypub/notes/{hash}    - Individual note objects
```

#### Management Endpoints

```
/api/activitypub/trigger-delivery  - Trigger post delivery (internal)
/api/activitypub/health            - Health check endpoint
```

### URL Design Rationale

**Consistent Prefix**: `/api/activitypub/*` groups all federation endpoints logically

**Future-Proof**: Enables other `/api/*` functionality for non-ActivityPub features

**Spec Compliant**: Meets ActivityPub specification requirements for collections and endpoints

**CDN-Friendly**: Static endpoints (`outbox`, `followers`, `following`) cached by CDN for performance

---

## Security Architecture

### Defense in Depth Strategy

```
┌─────────────────────────────────────────────────────────────┐
│ Layer 1: Azure Infrastructure Security                      │
│  • Managed Identity (no credentials in code)                │
│  • RBAC for Key Vault access                                │
│  • Network isolation with Azure Static Web Apps              │
│  • HTTPS-only endpoints                                      │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│ Layer 2: ActivityPub Protocol Security                      │
│  • HTTP Signatures (RFC 9421/cavage-12)                     │
│  • Public key verification from remote actors                │
│  • Activity structure validation                            │
│  • Actor domain validation                                   │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│ Layer 3: Application Security                               │
│  • Idempotent activity processing (duplicate checking)       │
│  • Input validation and sanitization                         │
│  • Rate limiting via Azure infrastructure                    │
│  • Comprehensive error handling                              │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│ Layer 4: Monitoring & Audit                                 │
│  • Activity audit logging                                    │
│  • Application Insights tracking                             │
│  • Failed signature validation alerts                        │
│  • Anomaly detection for unusual activity                    │
└─────────────────────────────────────────────────────────────┘
```

### HTTP Signatures Implementation

**Signing (Outbound)**:
1. Generate signature string from HTTP headers (date, host, digest)
2. Sign with RSA private key from Azure Key Vault
3. Include `Signature` header with request
4. Remote server validates using our public key from actor profile

**Verification (Inbound)**:
1. Extract `Signature` header from incoming request
2. Fetch remote actor's public key
3. Reconstruct signature string from request headers
4. Verify cryptographic signature
5. Reject requests with invalid signatures (401 Unauthorized)

**Key Management**:
- Private key stored in Azure Key Vault (never in code or configuration)
- Public key published in actor profile (`/api/activitypub/actor`)
- Managed identity for Functions to access Key Vault (no secrets)
- Key rotation supported through Azure Key Vault versioning

---

## Data Flow & Processing

### Follow Request Flow

```
1. User clicks "Follow @lqdev@lqdev.me" on Mastodon
   ↓
2. Mastodon queries /.well-known/webfinger
   ↓
3. WebFinger returns actor URI: /api/activitypub/actor
   ↓
4. Mastodon fetches actor profile and public key
   ↓
5. Mastodon POSTs Follow activity to /api/activitypub/inbox
   - Includes Signature header with cryptographic proof
   ↓
6. InboxHandler Function receives POST request
   - Validates HTTP signature using remote actor's public key
   - Checks for duplicate activity ID (idempotency)
   - Stores follower in Table Storage
   - Queues Accept activity for async delivery
   - Returns 200 OK immediately (non-blocking)
   ↓
7. ProcessAccept Function (queue trigger)
   - Dequeues Accept activity task
   - Constructs Accept activity wrapping original Follow
   - Signs with HTTP signature using Key Vault
   - POSTs Accept to follower's inbox
   - Implements exponential backoff retry on failure
   ↓
8. Mastodon receives Accept and updates UI to "Following"
   ↓
9. Next build: F# generates followers.json from Table Storage
   - Public discoverability maintained
   - Static file compliance with ActivityPub spec
```

### Post Delivery Flow

```
1. Developer pushes new content to main branch
   ↓
2. GitHub Actions builds site
   - F# processes UnifiedFeedItems
   - ActivityPubBuilder generates outbox activities
   - Deploys to Azure Static Web Apps
   ↓
3. GitHub Actions calls /api/activitypub/trigger-delivery
   ↓
4. QueueDeliveryTasks Function
   - Loads recent activities from outbox
   - Queries all followers from Table Storage
   - Groups by shared inbox for efficiency
   - Queues delivery task per unique inbox
   ↓
5. ProcessDelivery Function (queue trigger)
   - Dequeues delivery task
   - Constructs Create activity for post
   - Signs with HTTP signature using Key Vault
   - POSTs to follower inbox (or shared inbox)
   - Records delivery status in Table Storage
   - Implements exponential backoff retry on failure
   ↓
6. Follower's server receives Create activity
   - Validates signature
   - Adds post to user's timeline
   ↓
7. User sees new post in their Mastodon feed
```

### Shared Inbox Optimization

**Problem**: 100 followers on mastodon.social = 100 POST requests

**Solution**: Use shared inbox when available

**Implementation**:
```fsharp
// Group followers by shared inbox
let groupedFollowers = 
    followers
    |> List.groupBy (fun f -> 
        f.SharedInbox |> Option.defaultValue f.InboxUrl)
    |> List.map (fun (inbox, followers) -> 
        (inbox, followers |> List.map (fun f -> f.ActorUri)))

// Deliver once to each unique inbox
for (inboxUrl, followerActors) in groupedFollowers do
    let! _ = deliverActivity inboxUrl activity
    printfn $"Delivered to {inboxUrl} for {followerActors.Length} followers"
```

**Result**: 100 followers → 1 POST request instead of 100

---

## Cost Analysis

### Operational Costs

| Component | Usage Pattern | Monthly Cost |
|-----------|---------------|--------------|
| **Table Storage** | ~100-1000 followers, 1-10 posts/week | ~$0.01 |
| **Queue Storage** | ~1000-10000 messages/month | ~$0.01 |
| **Azure Functions** | ~10000-100000 executions/month | $0 (free tier) |
| **Key Vault** | ~1000-10000 operations/month | ~$0.03 |
| **Application Insights** | Standard monitoring | $0 (free tier) |
| **Blob Storage** | Static site hosting (existing) | ~$0.02 |

**Total**: ~$0.02-0.10/month for typical usage patterns

### Scaling Costs

| Follower Count | Posts/Week | Est. Monthly Cost |
|----------------|------------|-------------------|
| 100 | 7 | ~$0.02 |
| 1,000 | 7 | ~$0.05 |
| 10,000 | 7 | ~$0.50 |
| 100,000 | 7 | ~$5.00 |

### Comparison with Traditional Hosting

| Option | Monthly Cost | Maintenance |
|--------|--------------|-------------|
| **Hybrid Static+Serverless** | $0.02-5 | Minimal |
| **Managed Mastodon (masto.host)** | $6-29 | None |
| **Self-Hosted Mastodon (VPS)** | $20-100+ | High |
| **Dedicated Mastodon Server** | $100-500+ | Very High |

---

## Testing & Validation

### Automated Testing

**Test Suite**: `Scripts/test-activitypub.sh`

```bash
# Run all ActivityPub endpoint tests
./Scripts/test-activitypub.sh

# Tests include:
# - WebFinger discovery (both domain formats)
# - Actor profile validation
# - Outbox collection structure
# - Followers/Following collections
# - HTTP status codes and headers
# - JSON structure validation
# - Content-Type header verification
```

### Manual Testing from Mastodon

**Discovery Test**:
1. Log into any Mastodon instance
2. Search for: `@lqdev@lqdev.me`
3. Verify profile loads correctly
4. Check avatar, display name, bio, website link

**Follow Workflow Test**:
1. Click "Follow" button
2. Verify "Following" status change
3. Check follower appears in collection:
   ```bash
   curl -H "Accept: application/activity+json" \
     "https://lqdev.me/api/activitypub/followers"
   ```

**Post Delivery Test**:
1. Publish new content to website
2. Wait 5-10 minutes for delivery processing
3. Check Mastodon timeline for new post
4. Verify post content, title, and link

**Unfollow Test**:
1. Click "Unfollow" button from Mastodon
2. Verify follower removed from collection
3. Verify no further post deliveries received

### Monitoring & Debugging

**Application Insights Queries**:

```kusto
// Delivery success rate (last 24 hours)
traces
| where timestamp > ago(24h)
| where message contains "Delivery"
| summarize 
    Total = count(),
    Success = countif(message contains "Success"),
    Failed = countif(message contains "Failed")
| extend SuccessRate = round(100.0 * Success / Total, 2)

// HTTP signature validation failures
traces
| where timestamp > ago(24h)
| where message contains "Signature validation failed"
| project timestamp, customDimensions.actorUri, customDimensions.reason

// Queue processing times
dependencies
| where timestamp > ago(24h)
| where type == "Queue"
| summarize avg(duration), max(duration), p95=percentile(duration, 95)
    by name
```

**Azure Table Storage Queries**:

```bash
# List all followers
az storage entity query \
  --table-name followers \
  --account-name lqdevactivitypub

# Check delivery status for specific activity
az storage entity query \
  --table-name deliverystatus \
  --filter "PartitionKey eq 'activity-12345'" \
  --account-name lqdevactivitypub
```

---

## Reference Documentation

### Primary Documentation (Current)

**Start Here**:
- [`implementation-status.md`](./implementation-status.md) - Complete phase breakdown, decisions log, roadmap
- [`/api/ACTIVITYPUB.md`](../../api/ACTIVITYPUB.md) - Endpoint reference, testing procedures, troubleshooting

**Detailed Technical Docs**:
- [`follower-management-architecture.md`](./follower-management-architecture.md) - Why static sites need dynamic backends
- [`phase4-kickoff-summary.md`](./phase4-kickoff-summary.md) - Phase 4 preparation and decisions
- [`phase4-implementation-plan.md`](./phase4-implementation-plan.md) - Detailed Phase 4 implementation guide

**Operational Guides**:
- [`deployment-guide.md`](./deployment-guide.md) - Azure setup and deployment procedures
- [`keyvault-setup.md`](./keyvault-setup.md) - Azure Key Vault configuration details

**Phase Completion Summaries**:
- [`phase3-implementation-complete.md`](./phase3-implementation-complete.md) - Outbox automation
- [`phase4a-complete-summary.md`](./phase4a-complete-summary.md) - Inbox handler & Table Storage
- [`phase4b-4c-complete-summary.md`](./phase4b-4c-complete-summary.md) - Delivery infrastructure & GitHub Actions

### Historical Documentation (Reference)

**Archived for Context**:
- [`historical/implementation-plan.md`](./historical/implementation-plan.md) - Original 8-week phased plan
- [`historical/az-fn-implementation-plan.md`](./historical/az-fn-implementation-plan.md) - Azure Functions strategy
- [`historical/fix-summary.md`](./historical/fix-summary.md) - Phase 1-2 completion (superseded by phase summaries)
- [`historical/reconciliation-summary.md`](./historical/reconciliation-summary.md) - Documentation reconciliation

**Purpose**: Historical documents provide valuable context about decision-making process, alternative approaches considered, and evolution of the implementation.

### Testing & Scripts

**Automated Testing**:
- [`Scripts/test-activitypub.sh`](../../Scripts/test-activitypub.sh) - Comprehensive endpoint validation
- [`Scripts/ACTIVITYPUB-SCRIPTS.md`](../../Scripts/ACTIVITYPUB-SCRIPTS.md) - Script documentation

**Development Tools**:
- [`Scripts/rss-to-activitypub.fsx`](../../Scripts/rss-to-activitypub.fsx) - Phase 3 prototype (historical reference)

### External Specifications

**W3C Standards**:
- [ActivityPub Recommendation](https://www.w3.org/TR/activitypub/)
- [ActivityStreams 2.0](https://www.w3.org/TR/activitystreams-core/)
- [WebFinger RFC 7033](https://tools.ietf.org/html/rfc7033)

**HTTP Signatures**:
- [RFC 9421 - HTTP Message Signatures](https://datatracker.ietf.org/doc/html/rfc9421)
- [Cavage HTTP Signatures (draft-12)](https://datatracker.ietf.org/doc/html/draft-cavage-http-signatures-12)

**Implementation Guides**:
- [Maho.dev: ActivityPub in Static Sites](https://maho.dev/2024/02/a-guide-to-implement-activitypub-in-a-static-site-or-any-website/)
- [Mastodon ActivityPub Docs](https://docs.joinmastodon.org/spec/activitypub/)

---

## Key Architectural Insights

### Why This Approach Works

1. **Leverages Static Site Strengths**: 99% of content benefits from static hosting (performance, cost, CDN)
2. **Minimizes Dynamic Footprint**: Only protocol-required components are dynamic
3. **Production-Ready from Start**: Queue-based processing, retry logic, monitoring built-in
4. **Cost-Optimized**: Azure serverless scales to zero, pay only for actual usage
5. **Security-First**: Key Vault, HTTP signatures, managed identity, RBAC
6. **F# Integration**: Type-safe domain modeling, unified content pipeline, maintainable codebase

### Lessons Learned

1. **Queue-Based is Essential**: Synchronous delivery blocks on slow remote servers
2. **Idempotency Matters**: ActivityPub servers often retry, duplicate checking is critical
3. **Shared Inbox Optimization**: Reduces delivery from O(n) to O(domains)
4. **Table Storage is Perfect**: Cheap, reliable, and sufficient for follower scale
5. **Monitoring is Non-Negotiable**: Application Insights enables debugging in production
6. **Phased Implementation**: Testing each phase independently reduces risk

### Future Enhancements (Optional)

**Potential Additions**:
- Reply/Mention handling for two-way conversations
- Like/Boost activity processing for engagement metrics
- Collections pagination for large follower counts
- Webmention bridge for IndieWeb integration
- Media attachment support (images, videos)
- Custom emoji support

**Not Currently Planned**: These features are optional enhancements that don't impact core federation functionality.

---

## Contributing

When working with the ActivityPub implementation:

1. **Read This Document First**: Understand the architecture and design decisions
2. **Check Current Status**: Review [`implementation-status.md`](./implementation-status.md) for latest state
3. **Test Locally**: Run `./Scripts/test-activitypub.sh` before committing
4. **Maintain Documentation**: Update this overview when making architectural changes
5. **Monitor in Production**: Use Application Insights to validate changes

---

## Support & Questions

**Documentation Issues**: File an issue in the repository  
**Implementation Questions**: Reference relevant docs or file an issue  
**Production Issues**: Check Application Insights, review delivery status in Table Storage

---

**Document Version**: 1.0  
**Last Reviewed**: January 22, 2026  
**Next Review**: After significant architectural changes
