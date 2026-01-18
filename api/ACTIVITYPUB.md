# ActivityPub Implementation

This directory contains the Azure Functions implementation for ActivityPub federation support, enabling the static website to participate in the Fediverse (Mastodon, Pleroma, etc.).

> **📋 Documentation Home**: [`/docs/activitypub/`](../docs/activitypub/) - Complete ActivityPub documentation
>
> **Implementation Status**: See [`implementation-status.md`](../docs/activitypub/implementation-status.md) for current state  
> **Current State**: Phase 2 Complete (Discovery + Follow/Accept Workflow + Key Vault Security)  
> **Next Phase**: Phase 3 (Outbox Automation from F# Build) - Planned

## Architecture

### Hybrid Static + Dynamic Approach

- **Static Files**: Actor profile, webfinger configuration, outbox collection (stored in `/api/data/`)
- **Dynamic Endpoints**: Azure Functions that serve these static files with proper headers
- **Inbox Processing**: Dynamic endpoint that receives and logs federation activities

### URL Structure

**Current Implementation**: All ActivityPub endpoints follow the `/api/*` pattern:

- **WebFinger Discovery**: `/.well-known/webfinger` → `/api/webfinger`
- **Actor Profile**: `/api/actor`
- **Outbox**: `/api/outbox`
- **Inbox**: `/api/inbox`
- **Followers**: `/api/followers`
- **Following**: `/api/following`

**Future Migration** (Planned): Move to `/api/activitypub/*` top-level structure to enable other `/api/*` functionality:

- `/api/activitypub/actor`
- `/api/activitypub/inbox`
- `/api/activitypub/outbox`
- `/api/activitypub/followers`
- `/api/activitypub/following`

This migration will require coordinated updates to data files, Azure Functions, and routing configuration. See [`/docs/activitypub/implementation-status.md`](../docs/activitypub/implementation-status.md) for migration details.

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

### 2. Actor Profile (`/api/actor`)

**Purpose**: Returns the ActivityPub actor document with profile information, public keys, and endpoint URLs.

**Response**: `application/activity+json`

```bash
curl -H "Accept: application/activity+json" \
  "https://lqdev.me/api/actor"
```

**Key Fields**:
- `id`: Actor ID (`https://lqdev.me/api/actor`)
- `type`: "Person"
- `preferredUsername`: "lqdev"
- `inbox`, `outbox`, `followers`, `following`: Collection endpoints
- `publicKey`: RSA public key for HTTP signature verification

### 3. Outbox (`/api/outbox`)

**Purpose**: Public collection of activities (posts) from this actor.

**Response**: `application/activity+json`

```bash
curl -H "Accept: application/activity+json" \
  "https://lqdev.me/api/outbox"
```

**Structure**: OrderedCollection with Create activities wrapping Note objects.

**Current Status**: Contains manually created entries. Future enhancement will auto-generate from website content.

### 4. Inbox (`/api/inbox`)

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

### 5. Followers & Following (`/api/followers`, `/api/following`)

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
curl -H "Accept: application/activity+json" "https://lqdev.me/api/actor"

# Test Outbox
curl -H "Accept: application/activity+json" "https://lqdev.me/api/outbox"

# Test Inbox (GET)
curl -H "Accept: application/activity+json" "https://lqdev.me/api/inbox"
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

### Phase 3: Outbox Automation

**Goal**: Generate ActivityPub objects from website's UnifiedFeedItems  
**Status**: Planned (not yet implemented)  
**Estimated Effort**: 1-2 weeks

**Planned Approach**:
- Generate ActivityPub objects from website's UnifiedFeedItems during F# build
- Auto-update outbox during build process with actual content
- Create individual Note JSON files for recent content
- Use build-time generation (file-based storage)
- RSS feed as primary content source

**RSS Conversion Script**: The F# script [`Scripts/rss-to-activitypub.fsx`](../Scripts/rss-to-activitypub.fsx) serves as a **prototype** for Phase 3 implementation:
- Demonstrates RSS → ActivityPub conversion patterns
- Generates Note objects and Create activities from RSS items
- Outputs to `api/data/` structure
- Currently standalone, not integrated with main build
- Will inform final F# module design for build pipeline integration

For detailed Phase 3 planning and script documentation, see [`/docs/activitypub/implementation-status.md`](../docs/activitypub/implementation-status.md).

**Current Outbox Status**: Contains 20 manually created entries with placeholder dates. Will be replaced with auto-generated content in Phase 3.

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
