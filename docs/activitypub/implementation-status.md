# ActivityPub Implementation Status

**Last Updated**: January 23, 2026  
**Current Phase**: Phase 4 Complete (HTTP Signature Verification) 🔒  
**Primary Reference**: `docs/activitypub/http-signature-verification-plan.md`

---

## Quick Status Overview

| Phase | Status | Description |
|-------|--------|-------------|
| **Phase 1** | ✅ **COMPLETE** | Discovery & URL Standardization |
| **Phase 2** | ✅ **COMPLETE** | Follow/Accept Workflow & Security (Key Vault) |
| **Phase 3** | ✅ **COMPLETE** | Outbox Automation from F# Build (1,547 items) |
| **Phase 4** | ✅ **COMPLETE** 🔒 | HTTP Signature Verification (LIVE IN PRODUCTION) |
| **Phase 5** | 📋 **FUTURE** | Enhanced Activity Delivery & Analytics |

---

## Current Implementation Details

### URL Structure (Current - Migrated January 2026)

All ActivityPub endpoints now follow the `/api/activitypub/*` pattern:

```
https://lqdev.me/.well-known/webfinger          → /api/webfinger
https://lqdev.me/api/activitypub/actor          → Actor profile
https://lqdev.me/api/activitypub/inbox          → Receive activities
https://lqdev.me/api/activitypub/outbox         → Public activities
https://lqdev.me/api/activitypub/followers      → Followers collection
https://lqdev.me/api/activitypub/following      → Following collection
```

**Rationale**: Enables other `/api/*` functionality for non-ActivityPub features while keeping ActivityPub endpoints logically grouped.

**Implementation Status**: ✅ **COMPLETE** - Migration completed with:
- Azure Functions endpoint routes updated in all `function.json` files
- `api/data/actor.json` URLs updated to use `/api/activitypub/*` pattern
- `api/data/webfinger.json` links updated to point to `/api/activitypub/actor`
- `staticwebapp.config.json` CORS headers configured for all `/api/activitypub/*` endpoints
- All code references updated to use new endpoint structure

---

## Phase 1: Discovery & URL Standardization ✅

**Completion Date**: January 18, 2026

### What Was Implemented

1. **Domain Standardization**
   - All URLs use `lqdev.me` (without www)
   - WebFinger accepts both `@lqdev.me` and `@www.lqdev.me` for backward compatibility
   - Actor ID: `https://lqdev.me/api/activitypub/actor`

2. **URL Pattern Consistency**
   - Migrated all endpoints to `/api/activitypub/*` pattern
   - Removed `/@lqdev` routing indirection
   - Updated `staticwebapp.config.json` with proper CORS headers

3. **Data Files**
   - `api/data/webfinger.json` - WebFinger discovery response
   - `api/data/actor.json` - Actor profile with public key
   - `api/data/outbox/index.json` - Activity collection (manual entries)

4. **Testing & Documentation**
   - `Scripts/test-activitypub.sh` - Automated test suite
   - `api/ACTIVITYPUB.md` - Comprehensive endpoint documentation

### Discovery Testing

```bash
# WebFinger (both formats work)
curl "https://lqdev.me/.well-known/webfinger?resource=acct:lqdev@lqdev.me"
curl "https://lqdev.me/.well-known/webfinger?resource=acct:lqdev@www.lqdev.me"

# Actor profile
curl -H "Accept: application/activity+json" "https://lqdev.me/api/activitypub/actor"

# Mastodon search
Search for: @lqdev@lqdev.me
```

---

## Phase 2: Follow/Accept Workflow & Security ✅

**Completion Date**: January 18, 2026  
**Infrastructure Status**: ✅ **VERIFIED** (AZURE_CREDENTIALS secret confirmed in GitHub)

### What Was Implemented

1. **Azure Key Vault Integration**
   - NPM dependencies: `@azure/identity`, `@azure/keyvault-keys`
   - `api/utils/keyvault.js` - Key Vault + crypto wrapper
   - Supports production (Key Vault + managed identity) and development (env vars)
   - Secure signing key management with RBAC access control
   - Service principal configured with Key Vault Crypto User role
   - GitHub Actions authentication verified (AZURE_CREDENTIALS secret set)

2. **HTTP Signature Verification**
   - `api/utils/signatures.js` - Complete HTTP Signatures implementation
   - Verifies all incoming POST requests to inbox
   - Fetches remote actor public keys automatically
   - Generates signatures for outbound Accept activities
   - Prevents activity spoofing attacks

3. **Followers Management**
   - `api/utils/followers.js` - File-based followers storage
   - Add/remove/check follower operations
   - Persistent storage in `api/data/followers.json`
   - Simple, maintainable file-based approach

4. **Complete Follow/Accept Workflow**
   - Updated `api/inbox/index.js` with full federation workflow
   - Receives and validates Follow activities
   - Adds followers to persistent collection
   - Sends Accept activities to follower inboxes
   - Handles Undo (unfollow) activities
   - Comprehensive activity logging in `api/data/activities/`

5. **Updated Followers Endpoint**
   - `api/followers/index.js` returns actual followers from managed collection
   - 60-second cache for performance
   - Proper error handling and fallback

### Security Features

- ✅ HTTP signature verification for all incoming POST requests
- ✅ Azure Key Vault for signing key management
- ✅ Managed identity authentication (no secrets in code)
- ✅ Flexible dev/prod configuration
- ✅ Actor verification with remote public key fetching
- ✅ Activity structure validation
- ✅ Comprehensive audit logging

### Testing Follow Workflow

```bash
# From any Mastodon instance:
1. Search for: @lqdev@lqdev.me
2. Click Follow
3. Verify follow appears in followers collection:

curl -H "Accept: application/activity+json" "https://lqdev.me/api/activitypub/followers"
```

---

## Phase 3: Outbox Automation ✅

**Status**: ✅ **COMPLETE**  
**Completion Date**: January 18, 2026  
**Estimated Effort**: 1-2 weeks → **Actual: 2 hours**  
**Infrastructure Ready**: ✅ Key Vault setup complete (signing not needed for this phase)

### Goal

Automatically generate ActivityPub objects from website content during build process.

**Important**: This phase generates **unsigned** static JSON files. ActivityPub signing happens in Phase 4 (delivery), not during file generation.

### What Was Implemented ✅

1. **F# Integration**
   - Created `ActivityPubBuilder.fs` module (286 lines)
   - Converts `UnifiedFeedItems` to ActivityPub `Create` + `Note` activities
   - Integrated with `Program.fs` build pipeline
   - Leverages `GenericBuilder.fs` pattern for consistency

2. **Build-Time Generation**
   - Generates **unsigned** `api/data/outbox/index.json` during site build ✅
   - Creates OrderedCollection with 1,547 content items (vs 20 manual entries)
   - Uses actual content dates with RFC 3339 format
   - Maintains proper JSON structure for federation compatibility

3. **Research-Validated Implementation**
   - All domain types match W3C ActivityPub specification
   - Mastodon federation requirements validated via DeepWiki research
   - Content-based stable ID generation using MD5 hashing
   - Proper HTML content formatting with hashtag conversion

### Implementation Details

**Domain Types Created**:
```fsharp
type ActivityPubNote = {
    Context: string              // ActivityStreams namespace
    Id: string                   // Stable, dereferenceable URI
    Type: string                 // "Note" or "Article"
    AttributedTo: string         // Actor URI
    Published: string            // RFC 3339 format
    Content: string              // HTML content
    Name: string option          // Plain text title
    Url: string option           // HTML permalink
    To: string array             // Primary addressing
    Cc: string array option      // Secondary addressing (followers)
    Tag: ActivityPubHashtag array option  // Hashtags
}

type ActivityPubCreate = {
    // Wraps Note in Create activity
    Actor: string
    Object: ActivityPubNote
    // ... other required fields
}

type ActivityPubOutbox = {
    Type: string                 // "OrderedCollection" (required by spec)
    TotalItems: int              // 1,547 items
    OrderedItems: ActivityPubCreate array  // Reverse chronological
}
```

**Conversion Pipeline**:
```
UnifiedFeedItem → convertToNote → ActivityPubNote
                → convertToCreateActivity → ActivityPubCreate
                → generateOutbox → ActivityPubOutbox
                → serialize to JSON
```

**Build Integration** (Program.fs):
```fsharp
printfn "🎭 Building ActivityPub outbox..."
ActivityPubBuilder.buildOutbox allUnifiedContent "_public"
```

### Current Outbox Status

- ✅ Endpoint functional: `https://lqdev.me/api/activitypub/outbox`
- ✅ Contains 1,547 automatically generated entries (all website content)
- ✅ Automatically updated on every content publish
- ✅ Full F# build integration complete
- ✅ RFC 3339 compliant dates: `"2025-09-27T18:36:00-05:00"`
- ✅ Research-validated structure matching W3C spec and Mastodon requirements

### Success Metrics

**Quantitative**:
- Content Coverage: 1,547 items (100% of unified feed)
- Automation: 100% (zero manual intervention)
- Build Time: ~2-3 seconds (minimal overhead)
- Output Size: 79,346 lines of valid JSON

**Qualitative**:
- ✅ W3C ActivityPub specification compliance
- ✅ Mastodon federation requirements met
- ✅ Research-backed design decisions
- ✅ Production-ready code quality
- ✅ Stable ID generation for future updates

### Documentation

- **Implementation Details**: `docs/activitypub/phase3-implementation-complete.md`
- **Research Summary**: `docs/activitypub/phase3-research-summary.md`
- **Source Code**: `ActivityPubBuilder.fs`

---

## Phase 4: Activity Delivery �

**Status**: 🟡 IN PROGRESS  
**Completion Date**: TBD (January 2026)  
**Estimated Effort**: 4-6 days (Phased: 4A → 4B → 4C)  
**Infrastructure Ready**: ✅ Key Vault setup complete and verified

### Goal

Deliver new content activities to follower inboxes when published, with production-ready reliability.

**This is where ActivityPub signing is implemented.** HTTP Signatures are computed per-request at delivery time.

### Architecture Decisions

**Decision 1**: **Production-Ready Architecture**  
- Rationale: Ensures reliable delivery at scale, comprehensive error tracking, professional production quality
- Components: Azure Functions, Queue Storage, Table Storage, Application Insights monitoring

**Decision 2**: **Static followers.json + Azure Table Storage (Option A)**  
- Rationale: Table Storage handles Follow POST requests (source of truth), static file regenerated during builds for public discoverability
- Implementation: Inbox handler stores followers in Table Storage → GitHub Actions regenerates followers.json on next build

**Decision 3**: **Phased Implementation Approach**  
- Rationale: Systematic rollout enables testing at each stage, reduces risk of compound issues
- Phases: 4A (Inbox handler + follower management, 1-2 days), 4B (Delivery infrastructure, 1-2 days), 4C (Full integration + monitoring, 1-2 days)

**Decision 4**: **URL Pattern - /api/activitypub/***  
- Rationale: Maintains consistency with existing architectural decisions (see line 37-45)
- All endpoints follow: `/api/activitypub/inbox`, `/api/activitypub/outbox`, `/api/activitypub/actor`, etc.

### Implementation Plan

📄 **Complete Documentation**: [Phase 4 Implementation Plan](./phase4-implementation-plan.md)

**Azure Resources Required**:
- Storage Account: `lqdevactivitypub`
  - Table Storage: `followers` (follower state), `deliverystatus` (delivery tracking)
  - Queue Storage: `accept-delivery` (Accept activities), `activitypub-delivery` (post delivery)
- Application Insights: `lqdev-activitypub-insights` (monitoring, logging, performance)
- Estimated Cost: ~$0.02/month for typical usage (100-1000 followers)

**Phase 4A - Inbox Handler + Follower Management** (1-2 days):
- [x] Phase 4 comprehensive research (HTTP signatures, Mastodon validation, delivery patterns)
- [x] Follower management architecture documentation
- [x] Phase 4 implementation plan (production-ready, Option A, phased approach)
- [x] Azure resource provisioning script (`scripts/setup-activitypub-azure-resources.ps1`)
- [ ] Execute Azure resource creation (Storage Account, Tables, Queues, App Insights)
- [ ] Create F# service modules (HttpSignature.fs, FollowerStore.fs, ActivityQueue.fs)
- [ ] Implement InboxHandler Azure Function (POST /api/activitypub/inbox)
- [ ] Implement ProcessAccept Azure Function (Queue trigger for accept-delivery)
- [ ] Test Follow/Accept workflow with real Mastodon instance

**Phase 4B - Delivery Infrastructure** (1-2 days):
- [ ] Implement QueueDeliveryTasks Azure Function (HTTP trigger to queue all deliveries)
- [ ] Implement ProcessDelivery Azure Function (Queue trigger for activitypub-delivery)
- [ ] Implement delivery status tracking in Table Storage
- [ ] Test delivery to follower inboxes with real ActivityPub servers
- [ ] Validate HTTP signature generation and Mastodon compatibility

**Phase 4C - Full Integration + Monitoring** (1-2 days):
- [ ] Update GitHub Actions workflow for followers.json regeneration
- [ ] Add delivery trigger step to publish workflow
- [ ] Create Application Insights dashboard queries
- [ ] Document monitoring and troubleshooting procedures
- [ ] End-to-end testing of complete federation workflow

### Technical Components

1. **Inbox Handler** (`/api/activitypub/inbox`)
   - Receive Follow/Unfollow activities via HTTP POST
   - Validate HTTP signatures (RFC 9421/cavage-12)
   - Store followers in Azure Table Storage
   - Queue Accept activities for async delivery

2. **Activity Delivery System**
   - Load followers from Azure Table Storage
   - Generate `Create` activities for new posts from existing outbox
   - **Sign each HTTP request** with Azure Key Vault (fresh signature per POST)
   - Include `Signature` HTTP header with request metadata (date, host, digest)
   - POST to each follower's inbox URL (or shared inbox for optimization)
   - Handle delivery failures with exponential backoff retry (2s → 4s → 8s...up to 1 hour)
   - Track delivery status in Table Storage (pending → delivered → failed)

3. **CI/CD Integration**
   - Regenerate `api/data/followers.json` from Table Storage on each build
   - Trigger delivery on successful deployment
   - Integrate with GitHub Actions publish workflow
   - Delivery logs sent to Application Insights for monitoring

### Impact

When Phase 4 is complete:
- ✅ New blog posts automatically appear in follower timelines
- ✅ Followers receive notifications when new content is published
- ✅ Full ActivityPub federation experience with production-ready reliability
- ✅ Comprehensive monitoring and error tracking
- ✅ Automatic retry logic for failed deliveries

---

## Data Files & Storage

### Current Data Files

```
api/data/
├── actor.json              # Actor profile with public key
├── webfinger.json          # WebFinger discovery response
├── followers.json          # Persistent follower list (managed dynamically)
├── activities/             # Activity logs (gitignored, for audit)
├── notes/                  # Individual ActivityPub notes (empty, for Phase 3)
└── outbox/
    └── index.json          # Outbox collection (manual entries, Phase 3 will automate)
```

### Storage Strategy

**Current**: File-based storage for all ActivityPub data
**Rationale**: Simple, maintainable, version-controlled, adequate for static site scale
**Future**: Database migration only if follower count grows significantly

### Build-Time vs Runtime

| Data File | Generated At | Updated At |
|-----------|--------------|------------|
| `actor.json` | Manual setup | Rarely (profile changes) |
| `webfinger.json` | Manual setup | Rarely (domain changes) |
| `followers.json` | Runtime | On Follow/Undo activities |
| `activities/` | Runtime | On inbox POST requests |
| `outbox/index.json` | **Phase 3: Build-time** | Every site build |
| `notes/*.json` | **Phase 3: Build-time** | Every site build |

---

## RSS Script Analysis

### Script: `Scripts/rss-to-activitypub.fsx`

**Role**: Prototype for future F# integration (Phase 3)  
**Status**: Standalone script, not integrated with build process  
**Purpose**: Demonstrates RSS → ActivityPub conversion patterns

### What the Script Does

1. Parses RSS feed XML (`_public/feed/feed.xml`)
2. Converts RSS items to ActivityPub Note objects
3. Wraps Notes in Create activities
4. Generates outbox collection JSON
5. Outputs to `api/data/` directory structure

### URL Pattern in Script

⚠️ **Note**: Script currently generates URLs with pattern:
- `/api/activitypub/inbox`
- `/api/activitypub/outbox`
- `/api/activitypub/notes/{hash}`

This matches @lqdev's **preferred future pattern** but differs from current implementation (`/api/inbox`, `/api/outbox`).

### Integration Timeline

**Current**: Script is standalone, not called by main build  
**Phase 3**: Script concepts will inform F# module implementation  
**Decision**: Remain standalone for now, contribute to Phase 3 design

### Running the Script

```bash
# Manual execution (generates ActivityPub data)
dotnet fsi Scripts/rss-to-activitypub.fsx

# With custom paths
dotnet fsi Scripts/rss-to-activitypub.fsx \
  --rss-path ./_public/feed/feed.xml \
  --static-path ./api/data
```

---

## Testing & Validation

### Automated Testing

**Test Suite**: `Scripts/test-activitypub.sh`

```bash
# Run all ActivityPub endpoint tests
./Scripts/test-activitypub.sh

# Tests include:
# - WebFinger discovery (both domain formats)
# - Actor endpoint validation
# - Collections (outbox, followers, following)
# - HTTP status codes and content-type headers
# - JSON structure validation
```

### Manual Testing from Mastodon

1. **Discovery Test**: Search for `@lqdev@lqdev.me` from any Mastodon instance
2. **Follow Test**: Click Follow button and verify follow acceptance
3. **Followers Test**: Check `https://lqdev.me/api/followers` for your account
4. **Unfollow Test**: Unfollow and verify removal from followers collection

### Expected Behavior (Phases 1-2)

✅ **Working**:
- Account discoverable from Mastodon search
- Follow requests are accepted automatically
- Followers appear in `/api/followers` collection
- Unfollow removes followers properly
- HTTP signature verification prevents spoofing

⏳ **Not Yet Working** (Phases 3-4):
- New posts don't appear in follower timelines (requires Phase 3+4)
- Outbox contains manual entries with placeholder dates (requires Phase 3)

---

## Migration Notes: URL Pattern Change

### Current Pattern → Planned Pattern

When migrating from `/api/*` to `/api/activitypub/*`:

**Files Requiring Updates**:
1. `api/data/actor.json` - All endpoint URLs
2. `api/data/webfinger.json` - Actor href link
3. `api/data/outbox/index.json` - Activity IDs
4. `api/actor/index.js` - Endpoint paths in code
5. `api/inbox/index.js` - Endpoint paths in code
6. `api/outbox/index.js` - Endpoint paths in code
7. `api/followers/index.js` - Collection ID
8. `api/following/index.js` - Collection ID
9. `staticwebapp.config.json` - CORS header routes

**Azure Functions Changes**:
- Folder structure OR routing configuration
- Function bindings may need route parameter updates

**Testing Requirements**:
- Re-run full test suite after migration
- Verify existing followers remain functional
- Test from fresh Mastodon instance
- Confirm backward compatibility doesn't break

**Deployment Strategy**:
- Deploy all changes atomically
- Brief federation downtime acceptable
- Monitor activity logs after deployment
- Have rollback plan ready

---

## Documentation Hierarchy

### Primary Reference (Most Current)

**`/docs/activitypub/ARCHITECTURE-OVERVIEW.md`** - **START HERE** - Comprehensive architecture and implementation guide

**`/docs/activitypub/implementation-status.md`** (this file) - Complete phase breakdown, decisions log, roadmap

**`/api/ACTIVITYPUB.md`** - Complete endpoint reference, current implementation status, testing guide

### Implementation Plans (Detailed Context)

**`/docs/activitypub/follower-management-architecture.md`** - Why static sites need dynamic backends  
**`/docs/activitypub/phase4-implementation-plan.md`** - Phase 4 detailed implementation guide  
**`/docs/activitypub/phase4-kickoff-summary.md`** - Phase 4 preparation and decisions

### Status & Completion Summary

**`/docs/activitypub/phase3-implementation-complete.md`** - Phase 3 outbox automation completion  
**`/docs/activitypub/phase4a-complete-summary.md`** - Phase 4A inbox handler completion  
**`/docs/activitypub/phase4b-4c-complete-summary.md`** - Phase 4B/C delivery infrastructure completion

### Deployment & Operations

**`/docs/activitypub/deployment-guide.md`** - Post-merge Azure setup, Key Vault configuration, testing  
**`/docs/activitypub/keyvault-setup.md`** - Detailed Azure Key Vault setup instructions

### Testing

**`Scripts/test-activitypub.sh`** - Automated endpoint validation suite

### Code & Scripts

**`api/`** - All Azure Functions implementation  
**`Scripts/rss-to-activitypub.fsx`** - Prototype RSS → ActivityPub conversion (Phase 3 reference)

### Historical Documentation (Archived)

**`/docs/activitypub/historical/`** - Archived planning documents and early summaries for historical reference

---

## Quick Reference for Contributors

### I Want To...

**Add a new ActivityPub endpoint**
1. Read: `/api/ACTIVITYPUB.md` for architecture patterns
2. Create Azure Function in `/api/[endpoint-name]/`
3. Update `staticwebapp.config.json` with CORS headers
4. Add tests to `Scripts/test-activitypub.sh`
5. Update `/api/ACTIVITYPUB.md` documentation

**Fix a bug in existing endpoint**
1. Check `/api/ACTIVITYPUB.md` for endpoint specification
2. Review function code in `/api/[endpoint-name]/index.js`
3. Test locally with `func start`
4. Run `Scripts/test-activitypub.sh` for validation
5. Check logs in `api/data/activities/` for debugging

**Implement Phase 3 (Outbox Automation)**
1. Read: `/docs/activitypub/implementation-plan.md` (Phase 3 section)
2. Reference: `Scripts/rss-to-activitypub.fsx` for conversion patterns
3. Create F# module for ActivityPub generation
4. Integrate with `Program.fs` build pipeline
5. Update `/docs/activitypub/fix-summary.md` when complete

**Update ActivityPub documentation**
1. Primary updates go in `/api/ACTIVITYPUB.md`
2. Mark outdated sections in implementation plans
3. Update this status doc (`activitypub-implementation-status.md`)
4. Ensure cross-references remain accurate

**Test ActivityPub functionality**
1. Run automated suite: `./Scripts/test-activitypub.sh`
2. Test from Mastodon: Search and Follow `@lqdev@lqdev.me`
3. Check follower collection: `curl https://lqdev.me/api/activitypub/followers`
4. Review activity logs: `api/data/activities/`

---

## Next Steps (Recommended Priority)

### Immediate (Current State Stable)
1. ✅ Complete this documentation reconciliation
2. ✅ Ensure all docs cross-reference correctly
3. ✅ Validate testing procedures are documented

### Short-Term (Phase 3 Preparation)
1. 📋 Plan F# ActivityPub module architecture
2. 📋 Design UnifiedFeedItem → ActivityPub conversion
3. 📋 Decide on RSS script integration vs. rewrite
4. 📋 Prototype build-time outbox generation

### Medium-Term (Phase 3 Implementation)
1. 📋 Implement F# ActivityPub generation module
2. 📋 Integrate with Program.fs build pipeline
3. 📋 Replace manual outbox entries with generated content
4. 📋 Test outbox with real content
5. 📋 Update documentation with Phase 3 completion

### Long-Term (Phase 4 & URL Migration)
1. 📋 Implement activity delivery system
2. 📋 Integrate with CI/CD publish workflow
3. 📋 Execute URL pattern migration (`/api/activitypub/*`)
4. 📋 Add advanced features (replies, mentions, webmention bridge)

---

## Appendix: Key Decisions Log

### Decision: Domain Without Subdomain
**Date**: January 18, 2026  
**Decision**: Use `lqdev.me` (without www) for all ActivityPub URLs  
**Rationale**: ActivityPub/WebFinger best practice, maximum Fediverse compatibility  
**Impact**: Backward compatibility maintained via WebFinger accepting both formats

### Decision: `/api/activitypub/` Top-Level Path
**Date**: January 18, 2026 (pending implementation)  
**Decision**: Migrate from `/api/*` to `/api/activitypub/*` structure  
**Rationale**: Enable other `/api/*` functionality, logical grouping of ActivityPub endpoints  
**Impact**: Requires coordinated update across data files, functions, and routing config

### Decision: File-Based Storage
**Date**: January 18, 2026  
**Decision**: Use file-based storage for followers, activities, and outbox data  
**Rationale**: Simple, maintainable, version-controlled, adequate for static site scale  
**Impact**: No database infrastructure needed, easier development and debugging

### Decision: Build-Time Outbox Generation
**Date**: January 18, 2026  
**Decision**: Generate ActivityPub outbox during F# site build (Phase 3)  
**Rationale**: Aligns with static site architecture, RSS as source of truth  
**Impact**: Outbox updated on every build, no runtime generation overhead

### Decision: RSS as Source of Truth (Tentative)
**Date**: January 18, 2026  
**Decision**: Use RSS feed as primary content source for ActivityPub conversion  
**Rationale**: RSS already contains all content, well-structured, battle-tested  
**Impact**: ActivityPub content quality matches RSS quality, single source of truth

### Decision: Azure Key Vault for Signing Keys
**Date**: January 18, 2026  
**Decision**: Use Azure Key Vault for ActivityPub signing key management  
**Rationale**: Production-ready security, managed identity authentication, RBAC access control  
**Impact**: Secure key storage, no secrets in code, flexible dev/prod configuration

---

**Document Maintainer**: This document should be updated after each phase completion and when architectural decisions are made.  
**Last Reviewed**: January 18, 2026
