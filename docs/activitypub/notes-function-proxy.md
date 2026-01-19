# ActivityPub Notes Azure Function Proxy

## Overview

This document describes the Azure Function proxy implementation for ActivityPub note files, which solves a critical Content-Type header limitation in Azure Static Web Apps.

## Problem Statement

### The Issue
ActivityPub notes must be served with the Content-Type header:
```
Content-Type: application/activity+json; charset=utf-8
```

However, Azure Static Web Apps (SWA) applies global MIME types based on file extensions. For `.json` files, SWA uses `application/json` regardless of route-specific header overrides in `staticwebapp.config.json`.

### Why This Matters
- **ActivityPub Spec Compliance**: The ActivityPub specification requires `application/activity+json` for proper federation
- **Federation Failures**: Mastodon, Pleroma, and other Fediverse servers reject or misinterpret notes with incorrect Content-Type
- **Static Config Limitations**: Azure SWA's route-level headers cannot reliably override global MIME types for file extensions

### Prior Architecture
The original implementation used Azure Functions for note endpoints, which provided full header control. The migration to static files (PR #1834) broke this guarantee for performance benefits but introduced the Content-Type issue.

## Solution Architecture

### Hybrid Approach: Static Files + Function Proxy

The solution maintains the best of both approaches:

1. **Static File Generation**: F# build continues generating note files at `_public/activitypub/notes/{hash}.json`
2. **Function Proxy**: Azure Function serves these static files with correct headers
3. **API Endpoint URLs**: Note IDs reference the API endpoint (`/api/activitypub/notes/{hash}`)
4. **CDN Caching**: Function responses are cached for 24 hours at the CDN edge

```
┌─────────────┐     ┌──────────────┐     ┌─────────────┐
│   Fediverse │────▶│Azure Function│────▶│Static .json │
│   Server    │     │    Proxy     │     │   Files     │
└─────────────┘     └──────────────┘     └─────────────┘
                           │
                           ▼
                    ┌──────────────┐
                    │  CDN Cache   │
                    │  (24 hours)  │
                    └──────────────┘
```

## Implementation Details

### 1. Azure Function Configuration

**File**: `api/activitypub-notes/function.json`

```json
{
  "bindings": [
    {
      "authLevel": "anonymous",
      "type": "httpTrigger",
      "direction": "in",
      "name": "req",
      "methods": ["get", "options"],
      "route": "activitypub/notes/{noteId}"
    },
    {
      "type": "http",
      "direction": "out",
      "name": "res"
    }
  ]
}
```

**Route**: `/api/activitypub/notes/{noteId}`

### 2. Function Logic

**File**: `api/activitypub-notes/index.js`

**Key Features**:
- **File Reading**: Reads static JSON from `_public/activitypub/notes/{noteId}.json`
- **Header Control**: Sets proper ActivityPub Content-Type
- **Validation**: 
  - Validates noteId format (must be hex string)
  - Validates JSON content before serving
- **Error Handling**:
  - 400 for invalid noteId format
  - 404 for non-existent notes
  - 500 for malformed JSON or read errors
- **CORS**: Full CORS support for federation
- **Caching**: 24-hour cache directive for CDN

**Headers Set**:
```javascript
{
  'Content-Type': 'application/activity+json; charset=utf-8',
  'Access-Control-Allow-Origin': '*',
  'Access-Control-Allow-Methods': 'GET, OPTIONS',
  'Access-Control-Allow-Headers': 'Accept, Content-Type',
  'Cache-Control': 'public, max-age=86400',
  'X-Content-Source': 'static-proxy'
}
```

### 3. F# Build Integration

**File**: `ActivityPubBuilder.fs`

**Changes**:
```fsharp
module Config =
    // ... other config ...
    let notesPath = "/api/activitypub/notes/"  // Changed from "/activitypub/notes/"
```

**Impact**:
- Note IDs now use API endpoint: `https://lqdev.me/api/activitypub/notes/{hash}`
- Static file generation continues unchanged (no code changes needed)
- Outbox references automatically use API endpoints

## Performance Characteristics

### CDN Caching Strategy

**Cache Duration**: 24 hours (`max-age=86400`)

**Rationale**:
- Notes are immutable (content hash-based IDs ensure stability)
- Long cache duration minimizes function invocations
- CDN serves most requests without hitting the function

**Expected Behavior**:
- First request: Function reads file → CDN caches response
- Subsequent requests: CDN serves cached response (no function invocation)
- Cache miss after 24h: Function re-reads file → CDN re-caches

### Cost Analysis

**Azure Functions Consumption Plan**:
- First 1,000,000 executions/month: Free
- Storage: Static files included in SWA deployment

**Expected Usage** (1000 followers, 10 posts/day):
- Initial cache fills: ~1,551 invocations (one per note)
- Daily cache refreshes: ~65 invocations (1,551 notes ÷ 24 hours)
- Follow activity spikes: Negligible (CDN absorbs traffic)

**Estimated Cost**: $0.00/month (well within free tier)

## Testing

### Test Suite

**File**: `test-scripts/test-activitypub-notes-function.js`

**Test Cases**:
1. ✅ Valid note ID returns 200 with correct Content-Type
2. ✅ Non-existent note returns 404
3. ✅ Invalid note ID format returns 400
4. ✅ OPTIONS request returns proper CORS headers

**Running Tests**:
```bash
cd /home/runner/work/luisquintanilla.me/luisquintanilla.me
node test-scripts/test-activitypub-notes-function.js
```

### Manual Testing

**Local Development**:
```bash
# Build site to generate static notes
dotnet run

# Test function directly
node test-scripts/test-activitypub-notes-function.js
```

**Production Validation**:
```bash
# Test note endpoint
curl -H "Accept: application/activity+json" \
  https://lqdev.me/api/activitypub/notes/00029d200a98327f59a82821a27b959d

# Verify Content-Type header
curl -I -H "Accept: application/activity+json" \
  https://lqdev.me/api/activitypub/notes/00029d200a98327f59a82821a27b959d
```

## Migration Impact

### Backward Compatibility

**Note**: Existing note URLs change from static paths to API endpoints.

**Before**: `https://lqdev.me/activitypub/notes/{hash}`  
**After**: `https://lqdev.me/api/activitypub/notes/{hash}`

**Mitigation**:
- Note IDs are regenerated on every build (content hash-based)
- Outbox contains all note references (acts as source of truth)
- No external caching of individual note URLs expected

### Deployment Steps

1. **Code Deployment**: PR merged → GitHub Actions deploys to Azure
2. **Function Activation**: Azure SWA automatically provisions function
3. **Static Files**: Build generates notes at same location
4. **CDN Warm-up**: First requests populate CDN cache
5. **Verification**: Run test scripts to confirm headers

### Rollback Plan

If issues arise, rollback involves:
1. Revert F# config change (`notesPath` back to `/activitypub/notes/`)
2. Redeploy site (regenerates outbox with static URLs)
3. Remove function proxy (optional, won't be called)

## Future Considerations

### If Azure SWA Improves Header Control

Microsoft may eventually support reliable Content-Type overrides for file extensions.

**Reevaluation Triggers**:
- Azure SWA announces support for extension-specific route headers
- Documented limitation is lifted in official documentation
- Community reports successful override implementations

**Migration Back to Static**:
1. Test override configuration in staging environment
2. Update `staticwebapp.config.json` with route-level Content-Type
3. Change F# config back to static path
4. Deploy and validate headers
5. Remove function proxy
6. Update documentation

### Monitoring Recommendations

**Metrics to Track**:
- Function invocation count (should be low due to CDN)
- Function error rate (should be near zero)
- CDN cache hit ratio (should be >95%)
- Response time (should be <100ms)

**Alerts**:
- Function error rate >5%
- Function invocation spike (potential CDN issue)
- 404 rate >1% (potential note generation issue)

## Security Considerations

### Input Validation

**Note ID Format**: Function validates `noteId` matches hex pattern `[a-f0-9]+`

**Benefits**:
- Prevents path traversal attacks
- Ensures predictable file system access
- Rejects malformed requests early

### File System Access

**Path Construction**: `path.join(__dirname, '../../_public/activitypub/notes', ...)` 

**Safety**:
- Validated noteId prevents directory traversal
- Only accesses files in designated notes directory
- No user-controlled path segments

### CORS Policy

**Current**: Permissive (`Access-Control-Allow-Origin: *`)

**Rationale**: ActivityPub federation requires open access for remote servers

**Future**: Could restrict to known Fediverse servers if needed

## Documentation References

**Related Documents**:
- [`api/ACTIVITYPUB.md`](../../api/ACTIVITYPUB.md) - Main ActivityPub API documentation
- [`docs/activitypub/implementation-status.md`](./implementation-status.md) - Current implementation status
- [`docs/activitypub/README.md`](./README.md) - ActivityPub documentation home

**External Specifications**:
- [ActivityPub Specification](https://www.w3.org/TR/activitypub/)
- [ActivityStreams 2.0](https://www.w3.org/TR/activitystreams-core/)

---

**Last Updated**: January 19, 2026  
**Implementation PR**: [PR #XXXX](https://github.com/lqdev/luisquintanilla.me/pull/XXXX)  
**Status**: ✅ Implemented and Tested
