# Phase 5D: Dual-Object Media Implementation

**Started**: January 31, 2026  
**Status**: 🔄 In Progress  
**Branch**: `feature/phase5d-dual-media-objects`  
**Goal**: Implement dual-object pattern for maximum Fediverse compatibility

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

**Solution**: Implement dual-object pattern inspired by Castopod:
1. **Primary**: Note + Document attachment (Mastodon/Pixelfed compatible)
2. **Secondary**: Semantic Video/Image/Audio at alternate URLs (PeerTube compatible)

**Detailed Research**: [docs/activitypub/phase5d-media-research.md](../../docs/activitypub/phase5d-media-research.md)

---

## Implementation Progress

### Commits

| Commit | Description | Status |
|--------|-------------|--------|
| `f716bc59` | Disable native media objects, restore Note+attachment pattern | ✅ Complete |
| - | Add dual-object generation (semantic objects at alternate URLs) | 🔲 Pending |
| - | Add Mastodon extensions (blurhash, width, height) | 🔲 Pending |
| - | Add alternate link in tag array | 🔲 Pending |

---

## Step 1: Restore Working Behavior ✅ COMPLETE

**Commit**: `f716bc59 - fix(activitypub): disable native media objects, restore Note+attachment pattern`

### What Changed

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

## Step 2: Dual Object Generation 🔲 PENDING

Generate semantic Video/Image/Audio objects at alternate URLs in addition to the primary Note+attachment.

### Planned Approach

1. **Primary Path** (existing `convertToNote`): Creates `Note` with media attachments
2. **Secondary Path** (new): Also generates semantic `Video`/`Image`/`Audio` objects at `/api/activitypub/objects/{type}-{hash}.json`
3. **Link Primary to Secondary**: Add `tag` array entry with `rel: "alternate"` pointing to semantic object

### Planned URL Structure

| Content Type | Primary Activity | Secondary Object |
|--------------|------------------|------------------|
| Video media | `/api/activitypub/activities/{hash}` | `/api/activitypub/objects/video-{hash}.json` |
| Image media | `/api/activitypub/activities/{hash}` | `/api/activitypub/objects/image-{hash}.json` |
| Audio media | `/api/activitypub/activities/{hash}` | `/api/activitypub/objects/audio-{hash}.json` |

### Expected Output

**Primary Activity** (Mastodon sees this):
```json
{
  "type": "Create",
  "object": {
    "type": "Note",
    "attachment": [{"type": "Document", "mediaType": "video/mp4", "url": "..."}],
    "tag": [{
      "type": "Link",
      "href": "https://lqdev.me/api/activitypub/objects/video-{hash}",
      "rel": "alternate",
      "mediaType": "application/activity+json"
    }]
  }
}
```

**Secondary Object** (PeerTube can follow the link):
```json
{
  "type": "Video",
  "id": "https://lqdev.me/api/activitypub/objects/video-{hash}",
  "name": "Video Upload Test",
  "url": [{"type": "Link", "mediaType": "video/mp4", "href": "..."}],
  "attributedTo": "https://lqdev.me/api/activitypub/actor"
}
```

---

## Step 3: Mastodon Extensions 🔲 PENDING

Add Mastodon-specific properties to attachments for better rendering.

### Planned Enhancements

| Property | Purpose | Example |
|----------|---------|---------|
| `blurhash` | Color placeholder while loading | `"UBL_:rOpGG..."` |
| `width` | Media dimensions | `1920` |
| `height` | Media dimensions | `1080` |
| `focalPoint` | Crop centering coordinates | `[0.0, 0.5]` |

### Implementation Notes

- `blurhash` generation requires image processing (may defer to future enhancement)
- `width`/`height` require either:
  - Extraction from source files (complex)
  - Storage in frontmatter metadata (simpler, requires manual input)
  - Default values (simplest, less accurate)

**Decision**: Start with optional frontmatter metadata, fall back to defaults.

---

## Step 4: Alternate Link Tag 🔲 PENDING

Link the primary Note to the secondary semantic object via `tag` array.

### Implementation

Add to `convertToNote` when media is present:
```fsharp
let alternateLink = 
    if hasVideoAttachment then
        Some {| Type = "Link"
                Href = $"https://lqdev.me/api/activitypub/objects/video-{hash}"
                Rel = "alternate"
                MediaType = "application/activity+json" |}
    else None

// Add to tag array
```

---

## Technical Details

### Key Files

| File | Purpose | Changes Needed |
|------|---------|----------------|
| `ActivityPubBuilder.fs` | Activity generation | ✅ Step 1 complete, Steps 2-4 pending |
| `GenericBuilder.fs` | MediaAPData, MediaExtractor | No changes needed |
| `api/activitypub-activities/index.js` | Activity serving | May need update for object URLs |
| `Program.fs` | Build orchestration | May need to call new object generator |

### Existing Infrastructure (Phase 5D.1-5D.5)

| Component | Status | Details |
|-----------|--------|---------|
| `MediaAPData` type | ✅ Available | MediaUrl, MediaType, ObjectType, AltText, Caption |
| `MediaExtractor` module | ✅ Available | Extracts from :::media blocks |
| `ActivityPubMediaObject` type | ✅ Available | Can be repurposed for secondary objects |
| `convertToCreateMediaActivity` | ✅ Available | Can generate semantic objects |
| `useNativeMediaObjects` flag | ✅ Disabled | Routes all media to Note+attachment |

### Current Routing Logic

```fsharp
// ActivityPubBuilder.fs line 784+
let convertToActivity (item: UnifiedFeedItem) =
    if not useNativeActivityTypes then
        convertToNote item |> box
    else
        match item.ResponseType with
        | Some "star" -> convertToLike item |> box
        | Some "reshare" -> convertToAnnounce item |> box
        | _ ->
            // Phase 5D: Media-primary content (NOW DISABLED)
            if useNativeMediaObjects && item.MediaData.IsSome then
                convertToCreateMediaActivity item |> box
            else
                convertToNote item |> box
```

With `useNativeMediaObjects = false`, all media content goes through `convertToNote`, which calls `extractMediaAttachments` to create proper attachment arrays.

---

## Success Criteria

### Step 1 ✅
- [x] Video activities render as video players in Mastodon
- [x] Image activities render as image galleries in Mastodon
- [x] No regressions in existing functionality
- [x] Build compiles and passes

### Step 2 (Pending)
- [ ] Semantic Video/Image/Audio objects generated at `/api/activitypub/objects/`
- [ ] Objects are valid ActivityStreams
- [ ] PeerTube can process Video objects

### Step 3 (Pending)
- [ ] Attachments include `width`/`height` when available
- [ ] Optional `blurhash` support for future enhancement

### Step 4 (Pending)
- [ ] Primary Note includes `tag` with `rel: "alternate"` to semantic object
- [ ] Link uses correct `mediaType: "application/activity+json"`

---

## References

- [Phase 5D Research](../../docs/activitypub/phase5d-media-research.md) - Comprehensive platform analysis
- [Phase 5 Implementation Plan](../../docs/activitypub/phase5-fediverse-native-expansion-plan.md) - Overall roadmap
- [Mastodon ActivityPub Docs](https://docs.joinmastodon.org/spec/activitypub/) - Compatibility requirements
- [PeerTube ActivityPub Docs](https://docs.joinpeertube.org/api/activitypub) - Video object format
- [Castopod Discussion](https://github.com/Podcastindex-org/podcast-namespace/discussions/623) - Dual-object pattern inspiration
