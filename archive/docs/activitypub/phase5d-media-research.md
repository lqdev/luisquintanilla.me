# Phase 5D: Fediverse Media Object Research & Architecture Decision

**Date**: January 31, 2026  
**Status**: Research Complete → Architecture Revised  
**Author**: AI Development Partner  
**Scope**: How Fediverse platforms handle ActivityPub Image, Video, and Audio objects

---

## Executive Summary

This document captures comprehensive research into how major Fediverse platforms handle ActivityPub media objects. The research was conducted to understand why the initial Phase 5D implementation—which used standalone `Video`, `Image`, and `Audio` object types—did not render properly in Mastodon despite being spec-compliant ActivityPub.

### Key Finding

**Mastodon only renders media players for attachments on Note objects, not standalone Video/Image/Audio objects.**

This is not a bug—it's an architectural decision by Mastodon that treats Note and Question as "first-class" object types, with all other types "converted as best as possible" (often to just a link).

### Implemented Solution: Note+Attachment Pattern

After research revealed that **no Fediverse platform consumes external semantic media objects** (PeerTube expects its own Video format, Funkwhale expects platform-specific extensions), we implemented the simpler Note+attachment pattern only.

**Dual-object generation was identified as premature optimization and deferred** to a potential future phase when there's evidence of platforms that would consume external semantic media objects.

---

## Research Methodology

Research was conducted using:
- **Perplexity Research API** for comprehensive platform analysis
- **Official documentation** from Mastodon, PeerTube, Pixelfed, Castopod, Funkwhale
- **ActivityPub/ActivityStreams W3C specifications**
- **GitHub issues** documenting known interoperability problems
- **Fediverse Enhancement Proposals (FEPs)** for emerging standards

---

## Platform-by-Platform Analysis

### Mastodon (Microblogging)

**Documentation**: https://docs.joinmastodon.org/spec/activitypub/

#### First-Class Object Types

Mastodon explicitly states that only **two object types** are first-class:

> "Mastodon recognizes only two first-class Object types: Note and Question. Notes are transformed directly into regular statuses, while Questions are converted into poll statuses."

Source: [Mastodon ActivityPub Documentation](https://docs.joinmastodon.org/spec/activitypub/)

#### Type Conversion Behavior

For all other object types (Image, Video, Audio, Article, Page, Event):

> "Other Object types—including Article, Page, Image, Audio, Video, and Event—are 'converted as best as possible.'"

**In Practice**: This "conversion" typically results in:
- A text-only representation with a link to the original content
- No embedded media player or image display
- Losing the visual/audio experience entirely

#### GitHub Issue Evidence

[GitHub Issue #19357](https://github.com/mastodon/mastodon/issues/19357) documents this exact problem:

> "Federated Create->Image activities do not render the image inline."

This confirms that standalone Image objects arriving from federated servers don't display properly.

#### Attachment Rendering

Mastodon **does** render media when included as attachments to Note objects:

```json
{
  "type": "Note",
  "content": "Check out this video!",
  "attachment": [{
    "type": "Video",
    "mediaType": "video/mp4",
    "url": "https://example.com/video.mp4"
  }]
}
```

Key requirements for proper rendering:
- Use type matching the media: `Image`, `Video`, or `Audio` (per Mastodon's MediaAttachment entity)
- Include `mediaType` with proper MIME type
- Include `blurhash` for preview placeholders (Mastodon extension, optional)
- Include `width` and `height` for aspect ratio (optional enhancement)
- Note: Mastodon's **posting UI** limits to 4 images or 1 video/audio, but federated content may display more

#### Mastodon-Specific Extensions

Mastodon has introduced several non-standard properties that have become de facto standards:

| Property | Namespace | Purpose |
|----------|-----------|---------|
| `blurhash` | `toot:blurhash` | Compact color preview while loading |
| `focalPoint` | `toot:focalPoint` | Coordinates for intelligent cropping |
| `sensitive` | `as:sensitive` | Content warning flag |

These extensions are defined in Mastodon's namespace: `http://joinmastodon.org/ns#`

---

### PeerTube (Video Platform)

**Documentation**: https://docs.joinpeertube.org/api/activitypub

#### Native Video Object Support

PeerTube is the **only major Fediverse platform** that natively understands and renders standalone `Video` objects. This makes sense—PeerTube is a video platform.

#### Video Object Structure

PeerTube Video objects are comprehensive:

```json
{
  "@context": [
    "https://www.w3.org/ns/activitystreams",
    "https://w3id.org/security/v1",
    {
      "pt": "https://joinpeertube.org/ns#",
      "sc": "http://schema.org/"
    }
  ],
  "type": "Video",
  "id": "https://peertube.example/videos/watch/uuid",
  "name": "Video Title",
  "duration": "PT5M30S",
  "url": [
    {
      "type": "Link",
      "mediaType": "video/mp4",
      "href": "https://peertube.example/static/webseed/uuid-1080.mp4",
      "height": 1080,
      "width": 1920,
      "fps": 30
    },
    {
      "type": "Link",
      "mediaType": "video/mp4",
      "href": "https://peertube.example/static/webseed/uuid-720.mp4",
      "height": 720,
      "width": 1280,
      "fps": 30
    }
  ],
  "icon": [{
    "type": "Image",
    "url": "https://peertube.example/thumbnails/uuid.jpg",
    "mediaType": "image/jpeg"
  }],
  "attributedTo": [
    { "type": "Person", "id": "https://peertube.example/accounts/user" },
    { "type": "Group", "id": "https://peertube.example/video-channels/channel" }
  ]
}
```

#### Key PeerTube Requirements

1. **Multi-resolution URL array**: Multiple Link objects with different quality/format options
2. **Icon for thumbnail**: Uses `icon` property (not `preview` or `image`)
3. **Hierarchical attribution**: Both Person (user) and Group (channel) in `attributedTo`
4. **PeerTube namespace extensions**: Custom `pt:` namespace for podcast-specific properties
5. **Duration in ISO 8601**: `PT5M30S` format

#### PeerTube-Specific Extensions

| Property | Namespace | Purpose |
|----------|-----------|---------|
| `uuid` | `pt:uuid` | Unique video identifier |
| `views` | `pt:views` | View count |
| `commentsPolicy` | `pt:commentsPolicy` | Comment settings |
| `liveSaveReplay` | `pt:liveSaveReplay` | Live stream replay flag |

---

### Pixelfed (Image Platform)

**Documentation**: https://pixelfed.github.io/docs-next/spec/ActivityPub.html

#### Note-Only Architecture

Despite being an image-focused platform, Pixelfed:

> "Currently only accepts Create.Note objects (with or without attachments) for posts and comments."

Source: [Pixelfed ActivityPub Spec](https://pixelfed.github.io/docs-next/spec/ActivityPub.html)

This means standalone `Image` objects will **not** be displayed by Pixelfed.

#### Attachment Type: Document

Pixelfed uses `Document` type for image attachments (not `Image`):

```json
{
  "type": "Note",
  "content": "Beautiful sunset! #photography",
  "attachment": [{
    "type": "Document",
    "mediaType": "image/jpeg",
    "url": "https://pixelfed.example/storage/image.jpg",
    "width": 5120,
    "height": 2880,
    "blurhash": "UBL_:rOpGG-oBUNG..."
  }]
}
```

This was a deliberate standardization decision documented in Pixelfed's changelog:
> "Update to ActivityPub attachments to use Document type by default"

#### Pixelfed-Specific Extensions

| Property | Namespace | Purpose |
|----------|-----------|---------|
| `commentsEnabled` | `pixelfed:commentsEnabled` | Allow comments |
| `capabilities` | `pixelfed:capabilities` | announce/like/reply permissions |
| Location support | Standard `Place` object | Geo-tagging |

---

### Castopod (Podcast Platform)

**Documentation**: https://blog.castopod.org/castopod-host-alpha-42-fediverse/

#### Dual-Object Pattern (Model for Our Implementation)

Castopod faces the same challenge we do: their native content (podcast episodes) doesn't map to Mastodon's expected types. Their solution:

> "Episodes are represented as PodcastEpisode objects, which constitute a specialized ActivityPub object type... However, the implementation maintains associated Note objects alongside PodcastEpisode objects to ensure compatibility with existing Fediverse applications."

Source: [GitHub Discussion on Podcast Namespace](https://github.com/Podcastindex-org/podcast-namespace/discussions/623)

#### Castopod's Implementation

1. **Primary Object**: Custom `PodcastEpisode` type with full podcast metadata
2. **Fallback Note**: Associated `Note` object for Mastodon/generic platforms

```json
// Primary: PodcastEpisode (for podcast-aware apps)
{
  "type": "PodcastEpisode",
  "name": "Episode Title",
  "podcast:guid": "episode-guid",
  "audio": {
    "type": "Audio",
    "url": "https://castopod.example/episode.mp3"
  }
}

// Fallback: Note (for Mastodon)
{
  "type": "Note",
  "content": "<p><b>New Episode:</b> Episode Title</p>",
  "attachment": [{
    "type": "Document",
    "mediaType": "audio/mpeg",
    "url": "https://castopod.example/episode.mp3"
  }]
}
```

This dual-object pattern is exactly what we should adopt.

---

### Funkwhale (Audio/Music Platform)

**Documentation**: https://docs.funkwhale.audio/developer/federation/index.html

#### Custom Audio Types

Funkwhale extends ActivityPub with specialized music types:

- `Audio` object with additional properties
- `Album` collection type
- `Track` object type
- Access control for copyrighted content

#### Audio Object Structure

```json
{
  "type": "Audio",
  "url": "https://funkwhale.example/track.mp3",
  "mediaType": "audio/mpeg",
  "name": "Track - Album - Artist",
  "size": 52428800,
  "attributedTo": "https://funkwhale.example/users/artist"
}
```

Funkwhale is more likely to understand standalone Audio objects than Mastodon, but still may have limitations.

---

## Interoperability Matrix

Based on research, here's how each platform handles incoming media objects:

### Incoming Standalone Objects

| Platform | Image Object | Video Object | Audio Object |
|----------|-------------|--------------|--------------|
| **Mastodon** | ❌ Link only | ❌ Link only | ❌ Link only |
| **Pixelfed** | ❌ Not displayed | ❌ Not displayed | ❌ Not displayed |
| **PeerTube** | ❓ Partial | ✅ Native | ❓ Partial |
| **Funkwhale** | ❓ Unknown | ❓ Unknown | ⚠️ Custom format |
| **Castopod** | ❓ Unknown | ❓ Unknown | ⚠️ Uses Note fallback |

### Incoming Note with Attachments

| Platform | Image Attachment | Video Attachment | Audio Attachment |
|----------|-----------------|------------------|------------------|
| **Mastodon** | ✅ Renders (max 4) | ✅ Renders player | ✅ Renders player |
| **Pixelfed** | ✅ Renders | ✅ Renders | ⚠️ Unknown |
| **PeerTube** | N/A | ⚠️ Partial | N/A |
| **Funkwhale** | N/A | N/A | ⚠️ Partial |

### Key Limitations Discovered

1. **Mastodon 4-image limit**: Posts with >4 images are completely broken (all images discarded, not just extras)
   - Source: [GitHub Issue #14336](https://github.com/tootsuite/mastodon/issues/14336)

2. **Attachment type matters**: Use `Document` not `Image`/`Video` for attachments
   - Mastodon and Pixelfed expect `Document` type

3. **Blurhash expected**: Mastodon clients expect `blurhash` for loading placeholders

---

## Architectural Decision: Dual-Object Pattern

### Decision

Adopt the **Castopod-style dual-object pattern**:

1. **Generate semantic objects** (Video/Image/Audio) at dedicated URLs for platforms that understand them
2. **Generate Note objects with attachments** as the primary discoverable content for maximum compatibility
3. **Link both representations** so platforms can access either

### Implementation Design

#### File Structure

```
_public/activitypub/activities/
  ├── {hash}.json              # Note version (primary, Mastodon-compatible)
  └── objects/
      ├── video-{hash}.json    # Semantic Video object
      ├── image-{hash}.json    # Semantic Image object
      └── audio-{hash}.json    # Semantic Audio object
```

#### Primary Activity (Note-based)

```json
{
  "@context": [
    "https://www.w3.org/ns/activitystreams",
    {
      "toot": "http://joinmastodon.org/ns#",
      "blurhash": "toot:blurhash",
      "focalPoint": { "@id": "toot:focalPoint", "@container": "@list" }
    }
  ],
  "type": "Create",
  "id": "https://lqdev.me/api/activitypub/activities/{hash}",
  "actor": "https://lqdev.me/api/activitypub/actor",
  "published": "2026-01-31T10:00:00Z",
  "to": ["https://www.w3.org/ns/activitystreams#Public"],
  "cc": ["https://lqdev.me/api/activitypub/actor/followers"],
  "object": {
    "type": "Note",
    "id": "https://lqdev.me/api/activitypub/activities/{hash}#object",
    "content": "<p>My Rome Travel Video</p>",
    "url": "https://lqdev.me/notes/rome-travel/",
    "attributedTo": "https://lqdev.me/api/activitypub/actor",
    "attachment": [{
      "type": "Document",
      "mediaType": "video/mp4",
      "url": "https://cdn.lqdev.tech/files/videos/rome.mp4",
      "blurhash": "UBL_:rOpGG-oBUNG...",
      "width": 1920,
      "height": 1080
    }],
    "tag": [{
      "type": "Link",
      "href": "https://lqdev.me/api/activitypub/activities/objects/video-{hash}",
      "rel": "alternate",
      "mediaType": "application/activity+json"
    }]
  }
}
```

#### Semantic Object (Video)

```json
{
  "@context": "https://www.w3.org/ns/activitystreams",
  "type": "Video",
  "id": "https://lqdev.me/api/activitypub/activities/objects/video-{hash}",
  "name": "My Rome Travel Video",
  "url": [{
    "type": "Link",
    "mediaType": "video/mp4",
    "href": "https://cdn.lqdev.tech/files/videos/rome.mp4"
  }],
  "icon": [{
    "type": "Image",
    "url": "https://cdn.lqdev.tech/files/images/rome-thumbnail.jpg",
    "mediaType": "image/jpeg"
  }],
  "duration": "PT2M30S",
  "published": "2026-01-31T10:00:00Z",
  "attributedTo": "https://lqdev.me/api/activitypub/actor"
}
```

### Benefits

1. **Maximum Compatibility**: Note+attachment works everywhere
2. **Semantic Preservation**: Video/Image/Audio objects available for capable platforms
3. **Future-Proof**: As Mastodon/Pixelfed add support, they can discover semantic objects
4. **IndieWeb Alignment**: Your content remains the source of truth

### Trade-offs

1. **Complexity**: Two objects to generate and maintain
2. **Storage**: More JSON files (but they're small)
3. **Discoverability**: Semantic objects are secondary, linked via tag

---

## References

### Official Documentation

1. **W3C ActivityPub Specification**: https://www.w3.org/TR/activitypub/
2. **W3C ActivityStreams 2.0**: https://www.w3.org/TR/activitystreams-core/
3. **Mastodon ActivityPub Docs**: https://docs.joinmastodon.org/spec/activitypub/
4. **PeerTube ActivityPub API**: https://docs.joinpeertube.org/api/activitypub
5. **Pixelfed ActivityPub Spec**: https://pixelfed.github.io/docs-next/spec/ActivityPub.html
6. **Funkwhale Federation Docs**: https://docs.funkwhale.audio/developer/federation/index.html
7. **Castopod Fediverse Blog**: https://blog.castopod.org/castopod-host-alpha-42-fediverse/

### GitHub Issues & Discussions

8. **Mastodon #19357**: Federated Create->Image activities do not render inline
   - https://github.com/mastodon/mastodon/issues/19357

9. **Mastodon #14336**: Posts with >4 images from Pixelfed broken
   - https://github.com/tootsuite/mastodon/issues/14336

10. **Podcast Namespace Discussion #623**: ActivityPub for podcasts
    - https://github.com/Podcastindex-org/podcast-namespace/discussions/623

11. **SocialHub Discussion**: Representing images in ActivityPub
    - https://socialhub.activitypub.rocks/t/representing-images/624

### Community Research

12. **FunFedi Input Testing**: Image attachment format testing across platforms
    - https://inputs.funfedi.dev/inputs/image_attachments/
    - https://funfedi.dev/support_tables/image_attachments/

13. **SocialHub**: Guide for new ActivityPub implementers
    - https://socialhub.activitypub.rocks/t/guide-for-new-activitypub-implementers/479

---

## Appendix A: Mastodon Source Code Analysis

From `fetch_resource_service.rb`:

```ruby
# Mastodon only accepts these types when searching URLs
SUPPORTED_TYPES = %w(Note Question).freeze
CONVERTED_TYPES = %w(Image Audio Video Article Page Event).freeze

def expected_type?(json)
  equals_or_includes_any?(json['type'], SUPPORTED_TYPES + CONVERTED_TYPES)
end
```

This confirms that while Mastodon will *accept* Image/Video/Audio objects, they are "converted" rather than rendered natively.

---

## Appendix B: Phase 5D Original vs. Revised Scope

### Original Phase 5D (January 31, 2026)

- Create standalone Video/Image/Audio objects
- Direct `type: "Video"` in Create activities
- Assumed Fediverse would render natively

### Revised Phase 5D (Post-Research)

- Primary: Note objects with Document attachments (Mastodon-compatible)
- Secondary: Semantic Video/Image/Audio objects at alternate URLs
- Link semantic objects via `tag` array for discoverability
- Include Mastodon extensions (blurhash, focalPoint)

### Status Update

| Task | Original Status | Revised Status |
|------|-----------------|----------------|
| 5D.1: MediaAPData Type | ✅ Complete | ✅ Keep |
| 5D.2: MediaExtractor Module | ✅ Complete | ✅ Keep |
| 5D.3: ActivityPubMediaObject Type | ✅ Complete | ⚠️ Now secondary |
| 5D.4: Conversion Router | ✅ Complete | 🔄 Needs revision |
| 5D.5: Note+Attachment Pattern | ❌ Not implemented | 🆕 New primary approach |
| 5D.6: Dual Object Generation | ❌ Not implemented | 🆕 New |
| 5D.7: Mastodon Extensions | ❌ Not implemented | 🆕 New (blurhash, etc.) |
