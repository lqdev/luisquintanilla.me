# ActivityPub Implementation Status

**Last Updated**: January 18, 2026  
**Current Phase**: Phase 2 Complete (Discovery & Follow/Accept Workflow)  
**Primary Reference**: `/api/ACTIVITYPUB.md`

---

## Quick Status Overview

| Phase | Status | Description |
|-------|--------|-------------|
| **Phase 1** | ✅ **COMPLETE** | Discovery & URL Standardization |
| **Phase 2** | ✅ **COMPLETE** | Follow/Accept Workflow & Security (Key Vault) |
| **Phase 3** | 📋 **PLANNED** | Outbox Automation from F# Build |
| **Phase 4** | 📋 **FUTURE** | Activity Delivery to Followers |

---

## Current Implementation Details

### URL Structure (Current)

All ActivityPub endpoints currently follow the `/api/*` pattern:

```
https://lqdev.me/.well-known/webfinger  → /api/webfinger
https://lqdev.me/api/actor               → Actor profile
https://lqdev.me/api/inbox               → Receive activities
https://lqdev.me/api/outbox              → Public activities
https://lqdev.me/api/followers           → Followers collection
https://lqdev.me/api/following           → Following collection
```

### URL Structure (Planned Migration)

**Decision by @lqdev**: Move to `/api/activitypub/` top-level structure:

```
https://lqdev.me/.well-known/webfinger      → /api/webfinger (unchanged)
https://lqdev.me/api/activitypub/actor      → Actor profile
https://lqdev.me/api/activitypub/inbox      → Receive activities
https://lqdev.me/api/activitypub/outbox     → Public activities
https://lqdev.me/api/activitypub/followers  → Followers collection
https://lqdev.me/api/activitypub/following  → Following collection
```

**Rationale**: Enables other `/api/*` functionality for non-ActivityPub features while keeping ActivityPub endpoints logically grouped.

**Implementation Status**: Not yet implemented. Will require:
- Azure Functions endpoint path updates
- `api/data/actor.json` URL updates
- `api/data/webfinger.json` link updates
- `staticwebapp.config.json` CORS header updates
- Testing and validation

---

## Phase 1: Discovery & URL Standardization ✅

**Completion Date**: January 18, 2026

### What Was Implemented

1. **Domain Standardization**
   - All URLs use `lqdev.me` (without www)
   - WebFinger accepts both `@lqdev.me` and `@www.lqdev.me` for backward compatibility
   - Actor ID: `https://lqdev.me/api/actor`

2. **URL Pattern Consistency**
   - Standardized all endpoints to `/api/*` pattern
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
curl -H "Accept: application/activity+json" "https://lqdev.me/api/actor"

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

curl -H "Accept: application/activity+json" "https://lqdev.me/api/followers"
```

---

## Phase 3: Outbox Automation 📋

**Status**: PLANNED (Not Yet Implemented)  
**Estimated Effort**: 1-2 weeks  
**Infrastructure Ready**: ✅ Key Vault setup complete (signing not needed for this phase)

### Goal

Automatically generate ActivityPub objects from website content during build process.

**Important**: This phase generates **unsigned** static JSON files. ActivityPub signing happens in Phase 4 (delivery), not during file generation.

### What Will Be Implemented

1. **F# Integration**
   - Create ActivityPub module in F# codebase
   - Convert `UnifiedFeedItems` to ActivityPub `Create` + `Note` activities
   - Integrate with existing `Program.fs` build pipeline
   - Leverage `GenericBuilder.fs` pattern for consistency

2. **Build-Time Generation**
   - Generate **unsigned** `api/data/outbox/index.json` during site build (correct approach)
   - Create individual note JSON files for content discovery
   - Use actual content dates (not placeholder future dates)
   - Maintain JSON format for Azure Functions to serve

3. **RSS Script Role**
   - `Scripts/rss-to-activitypub.fsx` is a **prototype** for F# integration
   - Contributes to understanding outbox generation patterns
   - Remains standalone script for now
   - May inform final Phase 3 implementation

### Current Outbox Status

- ✅ Endpoint functional: `https://lqdev.me/api/outbox`
- ⚠️ Contains 20 manually created entries with placeholder future dates
- ⚠️ Not automatically updated on content publish
- ⚠️ Needs F# build integration for automation

### Technical Approach

```fsharp
// Planned F# module structure
module ActivityPubGenerator =
    
    // Convert unified content to ActivityPub Note
    let convertToNote (item: UnifiedFeedItem) : ActivityPubNote = ...
    
    // Wrap Note in Create activity
    let convertToCreateActivity (note: ActivityPubNote) : ActivityPubActivity = ...
    
    // Generate outbox collection
    let generateOutbox (activities: ActivityPubActivity list) : ActivityPubOutbox = ...
    
    // Save to api/data/outbox/
    let saveOutboxFiles (outbox: ActivityPubOutbox) : unit = ...
```

### Implementation Resources

- **Reference Plan**: `activitypub/implementation-plan.md` (Sections Phase 1-3)
- **Prototype Script**: `Scripts/rss-to-activitypub.fsx`
- **Current Outbox**: `api/data/outbox/index.json` (manual entries for reference)
- **Build Integration**: Will modify `Program.fs` main build pipeline

---

## Phase 4: Activity Delivery 📋

**Status**: FUTURE (Post-Phase 3)  
**Estimated Effort**: 1-2 weeks  
**Infrastructure Ready**: ✅ Key Vault setup complete and verified

### Goal

Deliver new content activities to follower inboxes when published.

**This is where ActivityPub signing is implemented.** HTTP Signatures are computed per-request at delivery time.

### What Will Be Implemented

1. **Activity Delivery System**
   - Load follower list from `api/data/followers.json`
   - Generate `Create` activities for new posts
   - **Sign each HTTP request** with Azure Key Vault (fresh signature per POST)
   - Include `Signature` HTTP header with request metadata (date, host, digest)
   - POST to each follower's inbox URL with signed request
   - Handle delivery failures and retries

2. **CI/CD Integration**
   - Trigger delivery on successful deployment
   - Integrate with GitHub Actions publish workflow
   - Delivery logs for monitoring and debugging

3. **Advanced Features**
   - Like/Boost activity processing
   - Reply/Mention handling
   - Webmention bridge integration
   - Collections pagination for large follower counts

### Impact

When Phase 4 is complete:
- ✅ New blog posts automatically appear in follower timelines
- ✅ Followers receive notifications when new content is published
- ✅ Full ActivityPub federation experience

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

**`/api/ACTIVITYPUB.md`** - Complete endpoint reference, current implementation status, testing guide

### Implementation Plans (Historical Context + Future Roadmap)

**`/docs/activitypub/implementation-plan.md`** - Original 8-week phased plan with technical details  
**`/docs/activitypub/az-fn-implementation-plan.md`** - Azure Functions-specific implementation strategy

### Status & Completion Summary

**`/docs/activitypub/fix-summary.md`** - Phase 1 & 2 completion details, learnings, validation  
**`/docs/activitypub/implementation-status.md`** (this file) - Current state, phase breakdown, next steps

### Deployment & Operations

**`/docs/activitypub/deployment-guide.md`** - Post-merge Azure setup, Key Vault configuration, testing  
**`/docs/activitypub/keyvault-setup.md`** - Detailed Azure Key Vault setup instructions

### Testing

**`Scripts/test-activitypub.sh`** - Automated endpoint validation suite

### Code & Scripts

**`api/`** - All Azure Functions implementation  
**`Scripts/rss-to-activitypub.fsx`** - Prototype RSS → ActivityPub conversion (Phase 3 reference)

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
3. Check follower collection: `curl https://lqdev.me/api/followers`
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
