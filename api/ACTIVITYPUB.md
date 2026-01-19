# ActivityPub Implementation

This directory contains the Azure Functions implementation for ActivityPub federation support, enabling the static website to participate in the Fediverse (Mastodon, Pleroma, etc.).

> **📋 Documentation Home**: [`/docs/activitypub/`](../docs/activitypub/) - Complete ActivityPub documentation
>
> **Implementation Status**: See [`implementation-status.md`](../docs/activitypub/implementation-status.md) for current state  
> **Current State**: Phase 4 In Progress (Activity Delivery + Production Infrastructure)  
> **Completed**: Phase 1 (Discovery), Phase 2 (Follow/Accept + Key Vault), Phase 3 (Outbox Automation with 1,547 items)

## Architecture

### Hybrid Static + Dynamic Approach

- **Static Files**: Actor profile, webfinger configuration, outbox collection (stored in `/api/data/`)
- **Dynamic Endpoints**: Azure Functions that serve static files + process inbox activities
- **Inbox Processing**: Dynamic endpoint that receives Follow/Unfollow activities, stores followers in Table Storage
- **Activity Delivery**: Queue-based async delivery system with HTTP signature authentication

### Phase 4 Architecture (In Progress)

**Production-Ready Approach**:
- **Azure Table Storage**: Source of truth for follower state (`followers` table) and delivery tracking (`deliverystatus` table)
- **Azure Queue Storage**: Async processing queues (`accept-delivery`, `activitypub-delivery`)
- **Azure Functions**: Serverless compute for inbox handler, Accept delivery, and post delivery
- **Application Insights**: Monitoring, logging, and performance tracking
- **Static followers.json**: Regenerated from Table Storage during builds for public discoverability

**Data Flow**:
1. Remote server POSTs Follow activity to `/api/activitypub/inbox`
2. InboxHandler validates signature → stores follower in Table Storage → queues Accept activity
3. ProcessAccept function delivers Accept activity with HTTP signature to follower's inbox
4. On new post publish: QueueDeliveryTasks queries Table Storage → queues delivery tasks
5. ProcessDelivery function delivers Create activity to each follower inbox with retry logic
6. GitHub Actions regenerates `followers.json` from Table Storage on next build

**Cost**: ~$0.02/month for typical usage (100-1000 followers, mostly covered by Azure free tiers)

### URL Structure

**Current Pattern**: All ActivityPub endpoints follow the `/api/activitypub/*` pattern (decided January 2026):

- **WebFinger Discovery**: `/.well-known/webfinger` → `/api/webfinger` (legacy path for compatibility)
- **Actor Profile**: `/api/activitypub/actor`
- **Outbox**: `/api/activitypub/outbox`
- **Inbox**: `/api/activitypub/inbox` (Phase 4 - receives Follow/Unfollow activities)
- **Followers**: `/api/activitypub/followers`
- **Following**: `/api/activitypub/following`
- **Individual Notes**: `/api/activitypub/notes/{hash}` (Azure Function proxy serving static files with correct Content-Type)

**Rationale**: The `/api/activitypub/*` structure enables other `/api/*` functionality for non-ActivityPub features while keeping ActivityPub endpoints logically grouped. See [`/docs/activitypub/implementation-status.md`](../docs/activitypub/implementation-status.md) lines 37-45 for full architectural decision documentation.

**Individual Notes**: Notes are served through an Azure Function proxy (see [`/docs/activitypub/notes-function-proxy.md`](../docs/activitypub/notes-function-proxy.md)) to ensure proper `application/activity+json` Content-Type headers. Static files are generated at build time and cached by CDN for performance.

**Phase 4 Additional Endpoints** (In Development):
- **Delivery Trigger**: `/api/activitypub/trigger-delivery` (HTTP POST to queue delivery tasks)
- **Health Check**: `/api/activitypub/health` (monitoring endpoint for Application Insights)

## Endpoints

### 1. WebFinger (`/.well-known/webfinger`)

**Purpose**: Enables discovery of the ActivityPub actor from a Fediverse handle.

**Query Parameters**:
- `resource`: The account to look up (e.g., `acct:lqdev@lqdev.me`)

**Accepted Resources**:
- `acct:lqdev@lqdev.me` (standard)
- `acct:lqdev@www.lqdev.me` (backward compatibility)

**Response**: `application/jrd+json`

```bash
curl -H "Accept: application/jrd+json" \
  "https://lqdev.me/.well-known/webfinger?resource=acct:lqdev@lqdev.me"
```

### 2. Actor Profile (`/api/activitypub/actor`)

**Purpose**: Returns the ActivityPub actor document with profile information, public keys, and endpoint URLs.

**Response**: `application/activity+json`

```bash
curl -H "Accept: application/activity+json" \
  "https://lqdev.me/api/activitypub/actor"
```

**Key Fields**:
- `id`: Actor ID (`https://lqdev.me/api/activitypub/actor`)
- `type`: "Person"
- `preferredUsername`: "lqdev"
- `inbox`, `outbox`, `followers`, `following`: Collection endpoints
- `publicKey`: RSA public key for HTTP signature verification

### 3. Outbox (`/api/activitypub/outbox`)

**Purpose**: Public collection of activities (posts) from this actor.

**Response**: `application/activity+json`

```bash
curl -H "Accept: application/activity+json" \
  "https://lqdev.me/api/activitypub/outbox"
```

**Structure**: OrderedCollection with Create activities wrapping Note objects.

**Current Status**: Contains 1,551 automatically generated entries from website content (posts, notes, responses, etc.).

**Note References**: Each activity's `object.id` points to an individual note at `/api/activitypub/notes/{hash}`.

### 4. Individual Notes (`/api/activitypub/notes/{hash}`)

**Purpose**: Dereferenceable URIs for individual ActivityPub Note objects. Each note represents a piece of content (post, note, response, etc.) from the website.

**Response**: `application/activity+json; charset=utf-8`

**Implementation**: Azure Function proxy serving static JSON files with correct Content-Type headers (see [`/docs/activitypub/notes-function-proxy.md`](../docs/activitypub/notes-function-proxy.md) for details).

```bash
# Example: Fetch a specific note
curl -H "Accept: application/activity+json" \
  "https://lqdev.me/api/activitypub/notes/00029d200a98327f59a82821a27b959d"
```

**Structure**: ActivityPub Note object with:
- `id`: Note URI (e.g., `https://lqdev.me/api/activitypub/notes/{hash}`)
- `type`: "Note" or "Article" (based on content type)
- `attributedTo`: Actor URI
- `published`: RFC 3339 timestamp
- `content`: HTML content
- `url`: Link to original webpage
- `to`, `cc`: Addressing (Public collection and followers)

**Key Features**:
- **Stable IDs**: Content hash-based for consistency across rebuilds
- **CDN Cached**: 24-hour cache for performance
- **CORS Enabled**: Full federation support
- **Error Handling**: Returns 404 for non-existent notes, 400 for invalid IDs

**Note Generation**: 
- Static files generated during site build at `_public/activitypub/notes/{hash}.json`
- Function reads and serves with proper ActivityPub headers
- Total notes: 1,551 (as of build)

### 5. Inbox (`/api/activitypub/inbox`)

**Purpose**: Receives federation activities from other Fediverse servers.

**Methods**:
- `GET`: Returns empty OrderedCollection
- `POST`: Accepts incoming activities (currently logs to files)

**Incoming Activities Logged**:
- Follow requests
- Like/Favorite notifications
- Reply/Mention notifications
- Announce/Boost notifications

**Current Status**: ✅ **IMPLEMENTED** - Full Follow/Accept workflow with HTTP signature verification using Azure Key Vault. Handles Follow, Accept, and Undo activities with persistent follower storage.

### 6. Followers & Following (`/api/activitypub/followers`, `/api/activitypub/following`)

**Purpose**: Collections of followers and accounts being followed.

**Response**: `application/activity+json`

**Current Status**: ✅ **IMPLEMENTED** - Returns actual followers from managed collection stored in `api/data/followers.json`. Updates dynamically as users follow/unfollow.

## Data Files

### `/api/data/actor.json`

Static actor profile containing:
- Profile metadata (name, summary, avatar)
- Public key for signature verification
- Endpoint URLs (inbox, outbox, followers, following)
- Profile attachments (website, GitHub links)

### `/api/data/webfinger.json`

WebFinger response containing:
- Subject (acct:lqdev@lqdev.me)
- Actor aliases
- Links to actor profile and homepage

### `/api/data/outbox/index.json`

Outbox collection containing recent activities:
- OrderedCollection of Create activities
- Each activity wraps a Note object
- Includes content, hashtags, mentions, publication dates

**⚠️ Important - Deployment Synchronization**:
- This file is **generated** during the build process (via `ActivityPubBuilder.buildOutbox`)
- Generated to: `_public/api/data/outbox/index.json` (contains all 1548+ items)
- **Must be synced** to `api/data/outbox/index.json` before Azure Functions deployment
- The GitHub Actions workflow (`.github/workflows/publish-azure-static-web-apps.yml`) includes a "Sync ActivityPub data" step that copies the generated file to the API directory
- This file should **not** be tracked in Git (excluded via `api/.gitignore`)
- Static files like `actor.json` and `webfinger.json` remain tracked as they are configuration files

## URL Consistency & Domain Strategy

### Decision: Use `lqdev.me` without `www`

**Rationale**:
- ActivityPub/WebFinger best practice: Use root domain without subdomain
- Maximizes compatibility with Fediverse implementations
- Standard followed by Mastodon and other major ActivityPub servers

**Implementation**:
- All ActivityPub URLs use `https://lqdev.me`
- WebFinger accepts both `@lqdev.me` and `@www.lqdev.me` for backward compatibility
- Consistent `/api/*` endpoint pattern throughout

## Testing

### Local Testing with Azure Functions Core Tools

```bash
# Start Azure Functions locally
cd api
func start

# In another terminal, run tests
cd /home/runner/work/luisquintanilla.me/luisquintanilla.me
./Scripts/test-activitypub.sh
```

### Manual Testing

```bash
# Test WebFinger discovery
curl "https://lqdev.me/.well-known/webfinger?resource=acct:lqdev@lqdev.me"

# Test Actor endpoint
curl -H "Accept: application/activity+json" "https://lqdev.me/api/activitypub/actor"

# Test Outbox
curl -H "Accept: application/activity+json" "https://lqdev.me/api/activitypub/outbox"

# Test Inbox (GET)
curl -H "Accept: application/activity+json" "https://lqdev.me/api/activitypub/inbox"
```

### Testing from Mastodon

1. Log into any Mastodon instance
2. Use the search box to look up: `@lqdev@lqdev.me`
3. If discovery works, you should see the profile
4. Click "Follow" to test the Follow workflow

## Implementation Status

### Phase 1: Discovery & URL Standardization ✅ COMPLETE
- Root domain usage (`lqdev.me` without www)
- Consistent `/api/*` endpoint pattern
- Backward compatibility for legacy formats
- Comprehensive testing and documentation

### Phase 2: Follow/Accept Workflow & Security ✅ COMPLETE
- Azure Key Vault integration for signing keys
- HTTP signature verification for incoming activities
- Follow/Accept/Undo workflow implementation
- Persistent follower storage in `api/data/followers.json`
- Activity logging for debugging
- Flexible configuration (Key Vault or environment variables)

## Future Enhancements

### Phase 3: Outbox Automation ✅ COMPLETE

**Goal**: Generate ActivityPub objects from website's UnifiedFeedItems  
**Status**: ✅ **IMPLEMENTED** - Outbox auto-generates from unified feed during build  
**Completion Date**: January 2026

**Implemented Approach**:
- ActivityPub objects generated from UnifiedFeedItems during F# build process
- Auto-updates outbox during build with all 1548+ content items
- Creates comprehensive outbox collection at build time
- Build-time generation integrated into `Program.fs` workflow
- Deployment synchronization handled via GitHub Actions workflow

**Deployment Architecture**:
- F# build generates: `_public/api/data/outbox/index.json`
- GitHub Actions syncs to: `api/data/outbox/index.json` (for Azure Functions)
- Azure Functions serve the synced file via `/api/activitypub/outbox` endpoint
- File is excluded from Git tracking (generated fresh on each deployment)

**Known Issue Fixed** (January 2026):
- **Problem**: Outbox endpoint returned only 20 stale items despite Phase 3 implementation
- **Root Cause**: Generated outbox not being synced to Azure Functions directory during deployment
- **Solution**: Added workflow step to copy generated data from `_public` to `api` directory
- **Result**: Endpoint now returns all 1548+ fresh items correctly

For detailed Phase 3 implementation, see [`/docs/activitypub/phase3-implementation-complete.md`](../docs/activitypub/phase3-implementation-complete.md).

**Current Outbox Status**: Contains all website content (1548+ items) auto-generated from unified feed system.

### Phase 4: Activity Delivery
- Activity delivery to follower inboxes when new content is published
- Like/Boost activity processing
- Reply/Mention handling and webmention bridge
- Collections pagination for large follower counts

## Security Implementation

### Current Security Features ✅
- **HTTP Signature Verification**: All incoming POST requests to inbox are verified using ActivityPub HTTP Signatures spec
- **Azure Key Vault Integration**: Signing keys managed securely in Azure Key Vault with RBAC access control
- **Managed Identity**: Azure Functions access Key Vault via system-assigned managed identity (no secrets in code)
- **Flexible Configuration**: Development mode supports environment variables, production uses Key Vault
- **Actor Verification**: Fetches and validates remote actor public keys automatically
- **Activity Validation**: Verifies activity structure and required fields before processing
- **Comprehensive Logging**: All activities logged to `api/data/activities/` for audit trail

### Infrastructure Security
- CORS headers properly configured for ActivityPub federation
- Rate limiting relies on Azure infrastructure
- Private keys never exposed (stored securely in Key Vault)
- Public keys published in actor profile for signature verification

### Future Security Enhancements
- Enhanced rate limiting on inbox endpoint
- Additional content validation rules
- Key rotation automation

## Troubleshooting

### Discovery Not Working

1. **Check WebFinger**: Ensure `/.well-known/webfinger` returns proper JSON
2. **Verify Domain**: Confirm using `lqdev.me` without `www`
3. **Check Actor ID**: Should match webfinger `href` exactly
4. **Headers**: Ensure `Content-Type: application/jrd+json` for webfinger

### Actor Not Loading

1. **Check Actor JSON**: Verify file exists at `/api/data/actor.json`
2. **Check Headers**: Should return `Content-Type: application/activity+json`
3. **Validate JSON**: Ensure proper ActivityPub context and required fields
4. **Check Public Key**: Verify publicKeyPem format is correct

### Inbox Not Receiving Activities

1. **Check POST Handler**: Verify inbox function processes POST requests
2. **Check Logging**: Look for activity files in `/api/data/activities/`
3. **Network Issues**: Verify sending server can reach your inbox URL
4. **CORS Issues**: Check CORS headers are properly set

## References

### ActivityPub Specification
- [W3C ActivityPub Recommendation](https://www.w3.org/TR/activitypub/)
- [ActivityStreams 2.0](https://www.w3.org/TR/activitystreams-core/)

### Implementation Guides
- [Maho.dev: Implementing ActivityPub in a Static Site](https://maho.dev/2024/02/a-guide-to-implement-activitypub-in-a-static-site-or-any-website/)
- [Elvery.net: ActivityPub on a Mostly Static Website](https://elvery.net/drzax/activitypub-on-a-mostly-static-website/)
- [Mastodon Documentation: WebFinger](https://docs.joinmastodon.org/spec/webfinger/)

### Related Documentation
- [ActivityPub Implementation Status](../docs/activitypub/implementation-status.md) - **Current phase breakdown, roadmap, and decisions**
- [ActivityPub Implementation Plan](../docs/activitypub/implementation-plan.md) - Original 8-week phased plan with technical details
- [ActivityPub Azure Functions Plan](../docs/activitypub/az-fn-implementation-plan.md) - Azure-specific implementation strategy
- [ActivityPub Fix Summary](../docs/activitypub/fix-summary.md) - Phase 1 & 2 completion summary
- [ActivityPub Deployment Guide](../docs/activitypub/deployment-guide.md) - Post-merge Azure setup and testing
- [ActivityPub Key Vault Setup](../docs/activitypub/keyvault-setup.md) - Detailed Key Vault configuration
- [RSS to ActivityPub Script](../Scripts/rss-to-activitypub.fsx) - Prototype conversion script for Phase 3 reference

## Contributing

When making changes to ActivityPub endpoints:

1. **Maintain URL consistency**: Always use `lqdev.me` without `www`
2. **Test locally**: Run `Scripts/test-activitypub.sh` before committing
3. **Update documentation**: Keep this README and related docs in sync
4. **Preserve backward compatibility**: Webfinger should accept both domain formats
5. **Follow ActivityPub spec**: Validate JSON structures against W3C specification

## License

This implementation follows the same license as the main website repository.
