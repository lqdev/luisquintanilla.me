# Phase 5D: Media-First ActivityPub Objects

**Started**: January 31, 2026  
**Completed**: January 31, 2026  
**Status**: ✅ COMPLETE (Note+Attachment Pattern)  
**Branch**: `feature/phase5d-dual-media-objects`  
**Outcome**: Implemented Note+attachment pattern; deferred dual-object pattern as premature optimization

---

## Problem Statement

Phase 5D initially implemented standalone `Video`, `Image`, and `Audio` ActivityPub objects per the W3C ActivityStreams 2.0 specification. Production testing revealed that **Mastodon and Pixelfed do not render these objects as media players** - they only display a text summary with a link.

### Root Cause

From Mastodon's source code (`fetch_resource_service.rb`):

```ruby
SUPPORTED_TYPES = %w(Note Question).freeze
CONVERTED_TYPES = %w(Image Audio Video Article Page Event).freeze

# Only Note and Question are first-class types
# All others are "converted as best as possible"
```

This means standalone `Create → Video` activities appear as text links, not embedded video players.

### Research Summary

| Platform | Standalone Video/Image/Audio | Note + Attachment |
|----------|------------------------------|-------------------|
| **Mastodon** | ❌ Shows link only | ✅ Renders player |
| **Pixelfed** | ❌ Not displayed | ✅ Renders image |
| **PeerTube** | ✅ Native support (Video) | N/A |
| **Castopod** | Uses dual pattern | ✅ Note fallback |

**Detailed Research**: [docs/activitypub/phase5d-media-research.md](../../docs/activitypub/phase5d-media-research.md)

---

## Final Decision: Note+Attachment Only

### What We Implemented

Set `useNativeMediaObjects = false` to route all media content to the existing Note+attachment pattern.

**Commit**: `f716bc59 - fix(activitypub): disable native media objects, restore Note+attachment pattern`

### What We Did NOT Implement (Deferred)

**Dual-object pattern** was considered but identified as **premature optimization**:
- Generate semantic Video/Image/Audio objects at `/api/activitypub/objects/{type}-{hash}`
- Link primary Note to secondary object via `tag` with `rel: alternate`

**Why Deferred**: No evidence any Fediverse platform would consume these objects:
- PeerTube expects its own format (multi-resolution URLs, HLS, chapters)
- Funkwhale expects platform-specific extensions (funkwhale:Track, funkwhale:Artist)
- Castopod is a host, not an aggregator—doesn't consume external audio
- No platform follows `rel: alternate` links to fetch semantic objects

### Implementation Details

**File**: `ActivityPubBuilder.fs` (line 778-782)

**Before**:
```fsharp
/// Phase 5D: Feature flag for enabling native media objects
/// Set to true to use Image/Video/Audio, false to use Note+attachment for media
let useNativeMediaObjects = true
```

**After**:
```fsharp
/// Phase 5D: Feature flag for enabling native media objects
/// Set to false: Use Note+attachment (Mastodon/Pixelfed compatible) as primary
/// Set to true: Use standalone Image/Video/Audio objects (breaks rendering in Mastodon)
/// Research: Mastodon only renders attachments on Note objects, not standalone media objects
let useNativeMediaObjects = false
```

### Verification

**Video Activity Output** (hash: `3e799d6c8979bccd7861ec3594f4e726`):
```json
{
  "type": "Create",
  "object": {
    "type": "Note",  ← ✅ Now Note instead of Video
    "content": "This is a video recording I made",
    "attachment": [{
      "type": "Video",  ← ✅ Video as attachment, not standalone object
      "mediaType": "video/mp4",
      "url": "https://cdn.lqdev.tech/files/videos/20251027_054251_49a8f53a-2345-4dc4-a81c-3b766a0bbc73.mp4",
      "name": "media"
    }]
  }
}
```

**Image Activity Output** (hash: `2f9ec45187bd7eb0cffd5195569269c4`):
```json
{
  "type": "Create",
  "object": {
    "type": "Note",  ← ✅ Now Note instead of Image
    "content": "It's here! Looks like again...",
    "attachment": [
      {"type": "Image", "mediaType": "image/png", "url": "..."},
      {"type": "Image", "mediaType": "image/png", "url": "..."},
      {"type": "Image", "mediaType": "image/png", "url": "..."},
      {"type": "Image", "mediaType": "image/png", "url": "..."},
      {"type": "Image", "mediaType": "image/png", "url": "..."},
      {"type": "Image", "mediaType": "image/png", "url": "..."},
      {"type": "Image", "mediaType": "image/png", "url": "..."}
    ]  ← ✅ 7 images as attachments, not standalone objects
  }
}
```

### Result

- ✅ Build compiles successfully
- ✅ Video activities now use Note+attachment pattern
- ✅ Image activities now use Note+attachment pattern  
- ✅ Media attachments preserve type (Image/Video/Audio) for rendering hints
- ✅ All existing tests pass (no regressions)

---

## Deferred: Dual-Object Pattern (Premature Optimization)

The following steps were **planned but not implemented** because research revealed no practical benefit with current Fediverse platforms. This section documents the original plan for future reference.

### Why Deferred

After comprehensive research, we determined that:

1. **No platform consumes external semantic media objects**
   - PeerTube expects its own format (multi-resolution URLs, HLS, chapters, thumbnails)
   - Funkwhale expects platform-specific extensions (`funkwhale:Track`, `funkwhale:Artist`)
   - Castopod is a podcast host, not an aggregator—doesn't consume external Audio
   - Mastodon/Pixelfed only render Note+attachment

2. **No evidence platforms follow `rel: alternate` links**
   - The proposed linking mechanism (`tag` with `rel: alternate`) has no documented support
   - PeerTube federation documentation focuses on PeerTube-to-PeerTube federation

3. **Added complexity without measurable value**
   - Two objects per media item instead of one
   - New URL path (`/api/activitypub/objects/`)
   - New Azure Function for serving objects
   - More code to maintain

### Step 2: Dual Object Generation ⏸️ DEFERRED

**Original Plan**: Generate semantic Video/Image/Audio objects at alternate URLs alongside primary Note+attachment.

**Proposed URL Structure**:
| Content Type | Primary Activity | Secondary Object |
|--------------|------------------|------------------|
| Video media | `/api/activitypub/activities/{hash}` | `/api/activitypub/objects/video-{hash}.json` |
| Image media | `/api/activitypub/activities/{hash}` | `/api/activitypub/objects/image-{hash}.json` |
| Audio media | `/api/activitypub/activities/{hash}` | `/api/activitypub/objects/audio-{hash}.json` |

**Why Not Implemented**: No Fediverse platform would consume these objects. PeerTube expects its own multi-resolution format; Funkwhale expects music metadata; Castopod is a host, not aggregator.

### Step 3: Mastodon Extensions ⏸️ DEFERRED

**Original Plan**: Add `blurhash`, `width`, `height`, `focalPoint` to attachments.

| Property | Purpose | Example |
|----------|---------|---------|
| `blurhash` | Color placeholder while loading | `"UBL_:rOpGG..."` |
| `width` | Media dimensions | `1920` |
| `height` | Media dimensions | `1080` |
| `focalPoint` | Crop centering coordinates | `[0.0, 0.5]` |

**Why Not Implemented**: These are nice-to-have enhancements. The basic Note+attachment already renders correctly. These can be added later if needed without architectural changes.

**Future Implementation Path**:
- `blurhash`: Requires image processing library (complex)
- `width`/`height`: Could be added to frontmatter metadata or extracted at build time
- `focalPoint`: Manual specification in frontmatter

### Step 4: Alternate Link Tag ⏸️ DEFERRED

**Original Plan**: Link primary Note to secondary semantic object via `tag` array.

```fsharp
let alternateLink = 
    if hasVideoAttachment then
        Some {| Type = "Link"
                Href = $"https://lqdev.me/api/activitypub/objects/video-{hash}"
                Rel = "alternate"
                MediaType = "application/activity+json" |}
    else None
```

**Why Not Implemented**: Without dual objects (Step 2), this has no purpose. No evidence platforms follow `rel: alternate` links.

---

## When to Reconsider Dual-Object Pattern

Re-evaluate this decision if:

1. **A platform emerges that consumes generic Video/Image/Audio objects** from external sources (not just its own format)
2. **PeerTube adds documentation** for importing external Video objects via `rel: alternate` links
3. **A media-aggregation platform becomes popular** (Fediverse-native video/audio aggregator)
4. **ActivityPub standardizes** a cross-platform media consumption pattern

**Effort to Implement Later**: Medium (2-3 days)
- Add `objectsPath` to Config
- Create `generateMediaObjectId` function
- Create `buildMediaObjects` function
- Modify `convertToNote` to add alternate link
- Create Azure Function at `/api/activitypub/objects/`
- Match target platform's expected format

---

## Technical Details

### Key Files

| File | Purpose | Changes Made |
|------|---------|--------------|
| `ActivityPubBuilder.fs` | Activity generation | ✅ `useNativeMediaObjects = false` |
| `GenericBuilder.fs` | MediaAPData, MediaExtractor | No changes (infrastructure remains available) |

### Existing Infrastructure (Available for Future Use)

| Component | Status | Details |
|-----------|--------|---------|
| `MediaAPData` type | ✅ Available | MediaUrl, MediaType, ObjectType, AltText, Caption |
| `MediaExtractor` module | ✅ Available | Extracts from :::media blocks |
| `ActivityPubMediaObject` type | ✅ Available | Can generate semantic objects if needed |
| `convertToMediaObject` | ✅ Available | Creates Video/Image/Audio objects |
| `convertToCreateMediaActivity` | ✅ Available | Wraps media objects in Create activities |
| `useNativeMediaObjects` flag | ✅ Set to false | Routes all media to Note+attachment |

This infrastructure was built during the initial Phase 5D implementation and remains available if the dual-object pattern becomes valuable in the future.

---

## Success Criteria ✅ COMPLETE

- [x] Video activities render as video players in Mastodon
- [x] Image activities render as image galleries in Mastodon
- [x] No regressions in existing functionality
- [x] Build compiles and passes
- [x] Decision documented with citations
- [x] Future implementation path documented

---

## References

### Primary Sources (Used in Decision)

1. **Mastodon ActivityPub Documentation**
   - URL: [docs.joinmastodon.org/spec/activitypub](https://docs.joinmastodon.org/spec/activitypub/)
   - Quote: "Other Object types—including Article, Page, Image, Audio, Video, and Event—are 'converted as best as possible.'"
   - Implication: Standalone media objects don't render as media players

2. **Mastodon GitHub Issue #19357**
   - URL: [github.com/mastodon/mastodon/issues/19357](https://github.com/mastodon/mastodon/issues/19357)
   - Title: "Federated Create->Image activities do not render the image inline"
   - Status: Open (as of January 2026)
   - Implication: Known limitation, not a bug they intend to fix

3. **Mastodon Source Code - fetch_resource_service.rb**
   - URL: [github.com/mastodon/mastodon/blob/main/app/services/fetch_resource_service.rb](https://github.com/mastodon/mastodon/blob/main/app/services/fetch_resource_service.rb)
   - Code: `SUPPORTED_TYPES = %w(Note Question).freeze`
   - Implication: Only Note and Question are first-class content types

4. **Pixelfed ActivityPub Documentation**
   - URL: [pixelfed.github.io/docs-next/spec/ActivityPub.html](https://pixelfed.github.io/docs-next/spec/ActivityPub.html)
   - Quote: "Pixelfed currently only accepts Create.Note objects"
   - Implication: Image objects must be attachments on Notes

5. **PeerTube ActivityPub Documentation**
   - URL: [docs.joinpeertube.org/api/activitypub](https://docs.joinpeertube.org/api/activitypub)
   - Note: Documents Video object structure for PeerTube-originated content
   - Note: No documentation on consuming external Video objects
   - Format: Multi-resolution URLs, HLS playlists, chapters, thumbnails

6. **Funkwhale ActivityPub Specification**
   - URL: [docs.funkwhale.audio/specs/](https://docs.funkwhale.audio/specs/)
   - Note: Uses platform-specific extensions (`funkwhale:Track`, `funkwhale:Artist`, `funkwhale:Album`)
   - Implication: Not compatible with generic Audio objects

7. **Castopod Podcast Namespace Discussion**
   - URL: [github.com/Podcastindex-org/podcast-namespace/discussions/623](https://github.com/Podcastindex-org/podcast-namespace/discussions/623)
   - Pattern: Dual PodcastEpisode + Note approach
   - Note: Castopod is a host, not an aggregator—doesn't consume external audio

### Related Documentation

- [Phase 5D Research](../../docs/activitypub/phase5d-media-research.md) - Comprehensive platform analysis
- [Phase 5 Implementation Plan](../../docs/activitypub/phase5-fediverse-native-expansion-plan.md) - Overall roadmap
