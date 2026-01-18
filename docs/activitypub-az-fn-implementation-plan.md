# ActivityPub Implementation Plan for Azure Static Web Apps

> **📋 NOTE**: This is an **Azure-specific implementation guide** for historical reference.  
> **For current implementation status**, see [`activitypub-implementation-status.md`](activitypub-implementation-status.md)  
> **For API reference**, see [`/api/ACTIVITYPUB.md`](../api/ACTIVITYPUB.md)

## Overview

**Status**: **Phases 1-2 Complete** - Discovery, Follow/Accept workflow, and Key Vault integration operational

Implement a complete ActivityPub federation system for a static blog using Azure Static Web Apps with Azure Functions for dynamic endpoints. The system will automatically convert RSS feed content to ActivityPub posts during deployment and handle federation activities like follows/unfollows.

## Current State
- Static site hosted on Azure Static Web Apps at `www.lqdev.me`
- Blog generates RSS feed at `https://www.lqdev.me/feed/feed.xml`
- Basic WebFinger discovery working with wildcard routing
- Actor profile accessible but needs Azure Functions for full federation

## Architecture Decision
Use hybrid approach: Azure Functions for dynamic ActivityPub endpoints + static JSON files for data persistence + build-time RSS processing for post generation.

## Implementation Steps

### Phase 1: Project Structure Setup

#### 1.1 Create Azure Functions Directory Structure
```
/api/
├── host.json
├── package.json
├── data/
│   ├── posts.json (generated at build)
│   ├── followers.json
│   ├── following.json
│   └── activities/
├── webfinger/
│   ├── function.json
│   └── index.js
├── actor/
│   ├── function.json
│   └── index.js
├── outbox/
│   ├── function.json
│   └── index.js
├── inbox/
│   ├── function.json
│   └── index.js
├── followers/
│   ├── function.json
│   └── index.js
└── following/
    ├── function.json
    └── index.js
```

#### 1.2 Configuration Files

Create `/api/host.json`:
```json
{
  "version": "2.0",
  "extensionBundle": {
    "id": "Microsoft.Azure.Functions.ExtensionBundle",
    "version": "[2.*, 3.0.0)"
  }
}
```

Create `/api/package.json`:
```json
{
  "name": "activitypub-functions",
  "version": "1.0.0",
  "dependencies": {
    "rss-parser": "^3.13.0"
  }
}
```

### Phase 2: RSS Processing Pipeline

#### 2.1 RSS Processing Integration
- Use existing RSS generation script that already converts blog posts to RSS format
- Ensure RSS script outputs ActivityPub-compatible data to `/api/data/posts.json`
- RSS processing should generate activities in this format:

Expected output structure for `/api/data/posts.json`:
```json
[
  {
    "@context": "https://www.w3.org/ns/activitystreams",
    "id": "https://www.lqdev.me/api/posts/{slug}",
    "type": "Create",
    "published": "2025-01-15T10:00:00Z",
    "actor": "https://www.lqdev.me/api/actor",
    "object": {
      "id": "https://www.lqdev.me/notes/{slug}",
      "type": "Note",
      "content": "Post content or snippet",
      "url": "https://www.lqdev.me/posts/original-post-url",
      "published": "2025-01-15T10:00:00Z",
      "name": "Post Title"
    }
  }
]
```

#### 2.2 Integration with Existing Build Process
- Modify existing RSS script to also output ActivityPub activities
- Ensure `/api/data/posts.json` is generated during current build process
- No changes needed to existing build pipeline

### Phase 3: Azure Functions Implementation

#### 3.1 WebFinger Function (`/api/webfinger/`)
**Purpose**: Handle discovery queries with resource parameters

`function.json`:
```json
{
  "bindings": [
    {
      "authLevel": "anonymous",
      "type": "httpTrigger",
      "direction": "in",
      "name": "req",
      "methods": ["get"]
    },
    {
      "type": "http",
      "direction": "out",
      "name": "res"
    }
  ]
}
```

`index.js`: Return WebFinger JSON with links to actor profile for `acct:lqdev@www.lqdev.me` resource queries.

#### 3.2 Actor Profile Function (`/api/actor/`)
**Purpose**: Serve complete ActivityPub actor profile

Return full actor object with:
- Personal information (name, summary, avatar)
- Public key for signature verification
- Links to inbox, outbox, followers, following
- Proper ActivityPub context and content-type headers

#### 3.3 Outbox Function (`/api/outbox/`)
**Purpose**: Serve blog posts as ActivityPub activities

- Read posts from `/api/data/posts.json` (generated at build time)
- Return as OrderedCollection of Create activities
- Each activity wraps a blog post as a Note object
- Cache headers: `public, max-age=300`

#### 3.4 Inbox Function (`/api/inbox/`)
**Purpose**: Receive federation activities (follows, replies, etc.)

Handle POST requests for:
- **Follow activities**: Add to followers.json, log activity
- **Undo/Follow activities**: Remove from followers.json
- **Other activities**: Log to activities directory for review
- GET requests: Return empty collection

Store activities as timestamped JSON files in `/api/data/activities/`

#### 3.5 Followers Function (`/api/followers/`)
**Purpose**: List current followers

- Read from `/api/data/followers.json`
- Return as OrderedCollection
- Handle missing file gracefully (return empty collection)

#### 3.6 Following Function (`/api/following/`)
**Purpose**: List accounts being followed

- Read from `/api/data/following.json` 
- Return as OrderedCollection
- Handle missing file gracefully (return empty collection)

### Phase 4: Routing Configuration

#### 4.1 Update staticwebapp.config.json
```json
{
  "routes": [
    {
      "route": "/.well-known/webfinger",
      "rewrite": "/api/webfinger"
    },
    {
      "route": "/@lqdev",
      "rewrite": "/api/actor"
    }
  ],
  "globalHeaders": {
    "Access-Control-Allow-Origin": "*"
  }
}
```

#### 4.2 Content-Type Strategy
- Azure Functions set headers explicitly in code
- Remove static file MIME type configurations
- All ActivityPub responses use `application/activity+json`
- WebFinger responses use `application/jrd+json`

### Phase 5: Data Management

#### 5.1 Initial Data Files
Create empty initial files:

`/api/data/followers.json`:
```json
[]
```

`/api/data/following.json`:
```json
[]
```

#### 5.2 Activity Logging
- All inbox activities logged to `/api/data/activities/activity-{timestamp}.json`
- Follows automatically update followers.json
- Manual review of activities possible via file inspection

### Phase 6: Cache Control Strategy

#### 6.1 Endpoint-Specific Caching
- **WebFinger**: `no-cache, must-revalidate` (always fresh for discovery)
- **Actor**: `public, max-age=3600, s-maxage=1800` (changes infrequently)
- **Outbox**: `public, max-age=300` (new posts every few days/weeks)
- **Inbox**: `no-store` (POST endpoint, never cache)
- **Followers/Following**: `public, max-age=300` (updated on follows)

#### 6.2 CORS Configuration
- All endpoints: `Access-Control-Allow-Origin: *`
- Safe for ActivityPub federation (public discovery endpoints)
- Required for cross-domain federation requests

### Phase 7: Migration from Current Setup

#### 7.1 Account Migration Strategy
If migrating from existing Mastodon instance:
1. Keep old domain WebFinger redirecting to new static implementation
2. Add old account to aliases in new WebFinger response
3. Use Mastodon's official account migration before switching
4. Maintain old domain for transition period (3-6 months)

#### 7.2 Public Key Management
- Generate RSA key pair with OpenSSL
- Store private key securely (not in repository)
- Include public key in actor profile
- Use for activity signature verification

### Phase 8: Testing and Validation

#### 8.1 Endpoint Testing
Test each endpoint with curl:
```bash
# WebFinger
curl -H "Accept: application/jrd+json" "https://www.lqdev.me/.well-known/webfinger?resource=acct:lqdev@www.lqdev.me"

# Actor
curl -H "Accept: application/activity+json" "https://www.lqdev.me/@lqdev"

# Outbox
curl -H "Accept: application/activity+json" "https://www.lqdev.me/api/outbox"
```

#### 8.2 Federation Testing
1. Search for account from multiple Mastodon instances
2. Attempt follows from different instances
3. Verify follower count updates correctly
4. Test that blog posts appear in outbox after deployment

#### 8.3 Content-Type Verification
- Confirm all endpoints return correct MIME types
- Verify CORS headers present on all responses
- Test both browser and ActivityPub client requests

### Phase 9: Deployment and Monitoring

#### 9.1 Deployment Process
1. Azure Static Web Apps automatically detects `/api` folder
2. Provisions Azure Functions runtime
3. Deploys both static files and functions together
4. No additional Azure Functions app needed

#### 9.2 Monitoring Strategy
- Check `/api/data/activities/` for incoming federation activities
- Monitor followers.json for new follows
- Review function logs in Azure portal for errors
- Test discovery periodically from external Mastodon instances

## Success Criteria

### Functional Requirements
- [ ] WebFinger discovery works with query parameters
- [ ] Actor profile accessible via ActivityPub and web browser
- [ ] Blog posts automatically appear in outbox after deployment
- [ ] Can receive and log follow/unfollow activities
- [ ] Follower count updates correctly
- [ ] Account discoverable from Mastodon search

### Technical Requirements
- [ ] All endpoints return correct content-type headers
- [ ] CORS headers enable cross-domain federation
- [ ] RSS processing runs successfully during build
- [ ] Functions handle errors gracefully (return appropriate HTTP status codes)
- [ ] Data files created and updated correctly
- [ ] No runtime errors in Azure Functions logs

### Federation Requirements
- [ ] Account appears in Mastodon search results
- [ ] Can be followed from multiple Mastodon instances
- [ ] Followers receive notifications when new blog posts published
- [ ] Account migration from old instance (if applicable) works smoothly

## Technical Considerations

### Performance
- Static JSON files provide fast response times
- Azure Functions cold start minimal impact for discovery endpoints
- CDN caching reduces load on outbox/actor endpoints
- RSS processing at build time eliminates runtime overhead

### Security
- Public key cryptography for activity verification
- Rate limiting handled by Azure platform
- No sensitive data exposure (all endpoints serve public information)
- Activity logging for audit trail

### Maintainability
- Minimal code complexity in Azure Functions
- Standard ActivityPub implementation patterns
- Version controlled data files
- Clear separation between static content and dynamic endpoints

## Future Enhancements

### Optional Features (Post-MVP)
- Individual post endpoints (`/api/posts/{slug}`)
- Rich media support (images, videos in posts)
- Reply/mention handling in inbox
- Activity signature verification
- Automated follow-back functionality
- Web interface for managing followers/following

### Scaling Considerations
- Move to database if follower count grows significantly
- Implement pagination for large collections
- Add caching layer for frequently accessed data
- Consider CDN for media assets