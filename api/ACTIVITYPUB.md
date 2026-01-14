# ActivityPub Implementation

This directory contains the Azure Functions implementation for ActivityPub federation support, enabling the static website to participate in the Fediverse (Mastodon, Pleroma, etc.).

## Architecture

### Hybrid Static + Dynamic Approach

- **Static Files**: Actor profile, webfinger configuration, outbox collection (stored in `/api/data/`)
- **Dynamic Endpoints**: Azure Functions that serve these static files with proper headers
- **Inbox Processing**: Dynamic endpoint that receives and logs federation activities

### URL Structure

All ActivityPub endpoints follow the `/api/*` pattern for consistency:

- **WebFinger Discovery**: `/.well-known/webfinger` → `/api/webfinger`
- **Actor Profile**: `/api/actor`
- **Outbox**: `/api/outbox`
- **Inbox**: `/api/inbox`
- **Followers**: `/api/followers`
- **Following**: `/api/following`

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

**Current Status**: Basic logging. Future enhancement will add HTTP signature verification and Follow/Accept workflow.

### 5. Followers & Following (`/api/followers`, `/api/following`)

**Purpose**: Collections of followers and accounts being followed.

**Response**: `application/activity+json`

**Current Status**: Returns empty OrderedCollections. Future enhancement will track actual followers.

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

## Future Enhancements

### Phase 2: Outbox Automation
- Generate ActivityPub objects from website's UnifiedFeedItems
- Auto-update outbox during build process
- Create individual Note JSON files for recent content
- Fix date issues (currently has placeholder future dates)

### Phase 3: Enhanced Inbox
- Implement HTTP signature verification
- Add Follow/Accept workflow
- Track followers in persistent storage
- Send Accept activities in response to Follow requests
- Implement Undo/Unfollow handling

### Phase 4: Advanced Features
- Activity delivery to follower inboxes when new content is published
- Like/Boost activity processing
- Reply/Mention handling and webmention bridge
- Collections pagination for large follower counts

## Security Considerations

### Current Implementation
- CORS headers allow cross-origin requests
- No rate limiting (relies on Azure infrastructure)
- No HTTP signature verification (logs all incoming activities)

### Planned Security Enhancements
- HTTP signature verification for all POST requests
- Actor domain verification
- Rate limiting on inbox endpoint
- Content validation for incoming activities
- Private key storage in Azure Key Vault

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
- [ActivityPub Implementation Plan](../docs/activitypub-implementation-plan.md)
- [ActivityPub Azure Functions Plan](../docs/activitypub-az-fn-implementation-plan.md)

## Contributing

When making changes to ActivityPub endpoints:

1. **Maintain URL consistency**: Always use `lqdev.me` without `www`
2. **Test locally**: Run `Scripts/test-activitypub.sh` before committing
3. **Update documentation**: Keep this README and related docs in sync
4. **Preserve backward compatibility**: Webfinger should accept both domain formats
5. **Follow ActivityPub spec**: Validate JSON structures against W3C specification

## License

This implementation follows the same license as the main website repository.
