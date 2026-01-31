# Phase 5: Fediverse-Native Content Expansion Plan

**Date**: January 28, 2026  
**Status**: Phase 5A ✅ | Phase 5B ✅ | Phase 5C ✅ | Phase 5D 🔄 Revision | Phase 5F ✅ | Phase 5E In Progress  
**Author**: AI Development Partner (based on PR #1990 analysis)  
**Scope**: Expand ActivityPub to express rich content types natively in the Fediverse

---

## Implementation Progress

### Phase 5A: Response Semantics ✅ COMPLETE (January 30, 2026)

**Deployed & Validated** - Federation confirmed working in production.

| Task | Status | Details |
|------|--------|---------|
| 5A.1: Extend UnifiedFeedItem | ✅ | Added `ResponseType`, `TargetUrl`, `UpdatedDate` fields |
| 5A.2: Response Conversion | ✅ | `convertResponsesToUnified` populates response semantics |
| 5A.3: Activity Types | ✅ | `ActivityPubLike`, `ActivityPubAnnounce` types created |
| 5A.4: Conversion Router | ✅ | `convertToActivity` routes by response type |
| 5A.5: Outbox Generation | ✅ | Mixed activity types in outbox |
| 5A.6: Path Migration | ✅ | `/activitypub/activities/` with 301 redirects |
| 5A.7: Azure Function | ✅ | Updated with object unwrapping for Mastodon compatibility |

**Production Metrics (January 30, 2026)**:
- 502 Announce activities (reshares)
- 946 Create activities (posts, notes, replies, bookmarks)
- 146 Like activities (stars)
- Total: 1,594 activities

**Critical Fixes Applied During 5A**:
1. **Fragment Pattern Fix**: Create activity IDs use base URL (fetchable), Notes use `#object` fragment
2. **Object Unwrapping**: Azure Function returns Note/Article objects for Mastodon URL search compatibility

**Files Modified**:
- `GenericBuilder.fs` - UnifiedFeedItem type extension, response conversion
- `ActivityPubBuilder.fs` - Activity types, conversion router, build functions
- `staticwebapp.config.json` - 301 redirects for path migration
- `api/activitypub-activities/index.js` - Object unwrapping logic

---

### Phase 5B: Bookmark Link Attachments ✅ COMPLETE (January 31, 2026)

**Implemented & Verified** - Bookmarks now include FEP-8967 compliant Link attachments.

| Task | Status | Details |
|------|--------|---------|
| 5B.1: ActivityPubLink Type | ✅ | `{ Type: "Link"; Href: string; Name: string option }` |
| 5B.2: Polymorphic Attachments | ✅ | Changed `Attachment` field to `obj array option` |
| 5B.3: Link Attachment Logic | ✅ | Bookmarks include Link attachment with target URL |
| 5B.4: Type Annotation Fixes | ✅ | Explicit F# record type annotations for ActivityPubHashtag, ActivityPubImage, ActivityPubLink |

**Verified Output**:
```json
{
  "type": "Create",
  "object": {
    "type": "Note",
    "attachment": [
      {
        "type": "Link",
        "href": "https://example.com/bookmarked-page",
        "name": "Bookmark Title"
      }
    ]
  }
}
```

**Research Basis**: FEP-8967 standardizes Link attachments in Mastodon 4.5+. Mastodon generates its own link previews but preserves the semantic reference.

**Files Modified**:
- `ActivityPubBuilder.fs` - ActivityPubLink type, polymorphic attachment handling, convertToNote Link logic

---

### Phase 5F: Outbox Pagination ✅ COMPLETE (January 31, 2026)

**Implemented & Verified** - Outbox now uses proper OrderedCollection/OrderedCollectionPage pattern.

| Task | Status | Details |
|------|--------|---------|
| 5F.1: Root Collection Type | ✅ | `ActivityPubOutbox` with `first`/`last` links, no `orderedItems` |
| 5F.2: Page Collection Type | ✅ | `ActivityPubOutboxPage` with `orderedItems`, `partOf`, `next`, `prev` |
| 5F.3: Paginated Generation | ✅ | 50 items per page, generates separate page files |
| 5F.4: Azure Function Update | ✅ | Handles `?page=N` query parameter |
| 5F.5: Workflow Sync | ✅ | Copies all page files to API directory |

**Verified Output**:
```json
// Root collection: /api/activitypub/outbox
{
  "type": "OrderedCollection",
  "totalItems": 1594,
  "first": "https://lqdev.me/api/activitypub/outbox?page=1",
  "last": "https://lqdev.me/api/activitypub/outbox?page=32"
}

// Page 1: /api/activitypub/outbox?page=1
{
  "type": "OrderedCollectionPage",
  "partOf": "https://lqdev.me/api/activitypub/outbox",
  "next": "https://lqdev.me/api/activitypub/outbox?page=2",
  "prev": null,
  "orderedItems": [/* 50 activities */]
}
```

**Research Basis**: Per ActivityStreams spec, root OrderedCollection should NOT contain inline items for large collections. 50 items per page is the Mastodon standard.

**Files Modified**:
- `ActivityPubBuilder.fs` - Root/page types, paginated generation logic
- `api/outbox/index.js` - Query parameter handling for page requests
- `.github/workflows/publish-azure-static-web-apps.yml` - Copy all page files to API

---

## Executive Summary

This document provides a comprehensive, research-validated plan to expand the ActivityPub implementation beyond generic `Create` + `Note/Article` activities. The goal is to make the Fediverse understand what is actually published—replies, reshares, likes, bookmarks, reviews, media, albums, playlists, and locations—while maintaining the existing static-first architecture and ensuring backward compatibility.

### Your Vision (From lqdev.me/posts/activitypub-implementation-progress-2026-01-23)

> "I want my website to be my digital hub, with protocols like ActivityPub, Nostr, and AT Protocol serving as spokes to reach different networks. My content and identity remain on my site, independent of any single platform. If any protocol or network disappears, my content and identity remain intact."

This expansion aligns perfectly with that vision by:
1. **Maximizing Fediverse expressiveness** - Your content appears as it should in Mastodon, Pixelfed, etc.
2. **Preserving your website as source of truth** - All metadata originates from your markdown files
3. **Leveraging existing IndieWeb integration** - Microformats already define response semantics; ActivityPub mirrors them
4. **Maintaining static-first architecture** - Build-time generation with minimal dynamic components
5. **Linking to the open web** - Responses target any URL on the web, not just Fediverse endpoints

### Key Architectural Principle: Web-Wide Responses

Your website responds to the **open web**, not just ActivityPub content. This is intentional and aligns with IndieWeb principles:

- **Stars** (`u-like-of`) → may reference any URL (blog posts, articles, GitHub issues)
- **Reshares** (`u-repost-of`) → may boost any URL (news articles, project pages)
- **Replies** (`u-in-reply-to`) → may respond to any URL (forum posts, newsletters)
- **Bookmarks** (`u-bookmark-of`) → may save any URL for later reference

**ActivityPub Behavior**:
- When `inReplyTo`/`object` references an ActivityPub URI → Mastodon threads/notifies correctly
- When it references a regular web URL → Mastodon displays as a Note with reference link
- The semantic intent is preserved regardless—your response is linked to the original content

**Implementation Implication**: We generate Like/Announce activities referencing any URL. Fediverse clients will handle them appropriately based on whether the target is discoverable via ActivityPub.

### Scope Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **Like/Announce Delivery** | Outbox discovery only | No active delivery to original authors—activities appear in outbox for discovery |
| **Pagination** | 50 items per page | Standard recommendation, balances performance and usability |
| **Review Format** | Schema.org (spec-compliant) | Not BookWyrm-specific; any compliant client should understand |
| **Playlists** | Out of scope | Not in UnifiedFeed pipeline |
| **Activity Path** | `/activitypub/activities/` | Generic path for all activity types (notes, likes, announces, etc.) |

---

## Current State Analysis

### What Works Well ✅

| Component | Status | Details |
|-----------|--------|---------|
| **Discovery** | ✅ Complete | WebFinger, Actor profile, domain standardization |
| **Follow/Accept** | ✅ Complete | HTTP signature verification, Table Storage followers |
| **Outbox Generation** | ✅ Complete | 1,547+ items from UnifiedFeedItem pipeline (496 reshares, 145 stars, 103 replies) |
| **Post Delivery** | ✅ Complete | Queue-based delivery with exponential backoff |
| **Microformats** | ✅ Rich | h-entry, u-like-of, u-repost-of, u-bookmark-of, p-rating |
| **Response Types** | ✅ In Content | reply, reshare, star, bookmark with targeturl |
| **Review Metadata** | ✅ Parsed | :::review blocks with rating, item, ISBN, etc. |
| **Media Extraction** | ✅ Working | :::media blocks → attachment array |
| **Location Data** | ✅ Available | Albums have lat/lon coordinates |

### Current Limitations ❌

| Gap | Impact | Root Cause |
|-----|--------|------------|
| Responses exported as generic Note | Replies don't thread, reshares aren't boosts | UnifiedFeedItem loses ResponseType/TargetUrl |
| No Like/Announce activities | Stars/reshares invisible to Fediverse | All wrapped in Create + Note |
| Bookmarks lack Link emphasis | Bookmarked URL buried in content | No attachment or specialized structure |
| Reviews as plain text | Rating/metadata invisible to BookWyrm | No schema.org or Review type extension |
| Albums as single Note | Multi-image sets not federated properly | Not using Collection pattern |
| No outbox pagination | Large outbox may fail on some clients | Single JSON file for all 1,547 items |
| Actor missing collections | featured/bookmarks/reviews not discoverable | Actor JSON incomplete |

---

## Research-Validated Mappings

### Content Type → ActivityStreams Mapping

Based on W3C ActivityStreams 2.0 specification, Mastodon/Pixelfed/BookWyrm implementation analysis, and IndieWeb microformat alignment:

| Site Content | Microformat | Current AP | Proposed AP | Mastodon Display |
|--------------|-------------|------------|-------------|------------------|
| **Posts** | h-entry | `Create` + `Article` | Keep unchanged | Long-form article |
| **Notes** | h-entry | `Create` + `Note` | Keep unchanged | Standard toot |
| **Reply (response)** | u-in-reply-to | `Create` + `Note` | `Create` + `Note` w/ `inReplyTo` | Threaded reply |
| **Reshare (response)** | u-repost-of | `Create` + `Note` | `Announce` | Boost/reblog |
| **Star (response)** | u-like-of | `Create` + `Note` | `Like` | Favorite (inbox only) |
| **Bookmark** | u-bookmark-of | `Create` + `Note` | `Create` + `Note` w/ `Link` attachment | Note with link card |
| **Review** | p-rating, p-item | `Create` + `Note` | `Create` + `Article` w/ schema:Review context | Article with rating metadata |
| **Media (single)** | - | `Create` + `Note` + attachment | `Create` + `Note` + `Document` attachment¹ | Native media rendering |
| **Album** | - | `Create` + `Note` | `Create` + `Collection` + `Place` | Gallery (Pixelfed-style) |

¹ **Media Compatibility Note (Phase 5D Revision)**: Research revealed that standalone `Image/Video/Audio` objects are NOT rendered as media players by Mastodon or Pixelfed—they only show a link. The revised approach uses `Note` with `Document` attachments as the primary representation (Mastodon-compatible), with semantic `Video/Image/Audio` objects available at alternate URLs for PeerTube and other media-native platforms. See [phase5d-media-research.md](phase5d-media-research.md) for details.


### Key Research Findings

#### 1. Like and Announce Are Activities, Not Objects

```
✅ Correct:
{
  "@context": "https://www.w3.org/ns/activitystreams",
  "type": "Like",                    // Activity type, not wrapped in Create
  "actor": "https://lqdev.me/api/activitypub/actor",
  "object": "https://remote.server/posts/12345"
}

❌ Current (Incorrect for stars):
{
  "type": "Create",
  "object": {
    "type": "Note",
    "content": "⭐ Starred: ..."
  }
}
```

**Impact**: Mastodon will display your stars as favorites in the notification timeline. Reshares appear as boosts. This is how Fediverse users expect to see engagement.

#### 2. inReplyTo Works for Any URL (Not Just ActivityPub)

```json
{
  "type": "Create",
  "object": {
    "type": "Note",
    "inReplyTo": "https://example.com/blog/interesting-post",
    "content": "Your actual reply content..."
  }
}
```

**Important**: Your website responds to the **open web**, not just Fediverse content. This means:
- `inReplyTo` may point to any URL (blog posts, news articles, GitHub issues, etc.)
- `Like` objects may reference any URL you've starred
- `Announce` objects may reference any URL you've reshared

**Mastodon Behavior**: 
- If `inReplyTo` points to an ActivityPub object, reply threads correctly
- If `inReplyTo` points to a regular web URL, Mastodon displays it as a Note with a reference link
- The semantic intent is preserved regardless—your response is linked to the original content

**IndieWeb Alignment**: This matches how `u-in-reply-to`, `u-like-of`, and `u-repost-of` microformats work—they reference any URL on the web, not just IndieWeb-compatible sites.

#### 3. Review Objects Need Schema.org Extension

ActivityStreams 2.0 has no native Review type. Options:

**Option A (Recommended): Article with Schema.org context**
```json
{
  "@context": [
    "https://www.w3.org/ns/activitystreams",
    { "schema": "https://schema.org/" }
  ],
  "type": "Article",
  "name": "The Creative Act: A Way of Being Review",
  "content": "<div class=\"review\">...",
  "schema:reviewRating": {
    "@type": "schema:Rating",
    "schema:ratingValue": 4.8,
    "schema:bestRating": 5.0
  },
  "schema:itemReviewed": {
    "@type": "schema:Book",
    "schema:name": "The Creative Act: A Way of Being",
    "schema:author": "Rick Rubin",
    "schema:isbn": "9780593653425"
  }
}
```

**Option B: BookWyrm-style custom type (for BookWyrm federation)**
```json
{
  "type": ["Article", "Review"],
  "rating": 4.8,
  "scale": 5.0
}
```

**Recommendation**: Use Option A with schema.org for broad compatibility; Option B only if targeting BookWyrm specifically.

#### 4. OrderedCollection Pagination Is Critical

Current outbox: 1,547 items in single file (~large JSON).

**Mastodon behavior**: Fetches first page, follows `next` links as needed.

**Recommended structure**:
```
/api/activitypub/outbox           → OrderedCollection with first/last/totalItems
/api/activitypub/outbox/page/1    → OrderedCollectionPage (newest 50 items)
/api/activitypub/outbox/page/2    → OrderedCollectionPage (next 50 items)
...
```

Each page includes:
```json
{
  "type": "OrderedCollectionPage",
  "partOf": "https://lqdev.me/api/activitypub/outbox",
  "next": "https://lqdev.me/api/activitypub/outbox/page/2",
  "prev": null,
  "orderedItems": [ ... 50 activities ... ]
}
```

---

## Critical Fixes Applied

### Activity ID Fragment Pattern Fix (January 30, 2026)

**Problem**: Activities were not discoverable via Mastodon search. Pasting activity URLs in Mastodon returned no results.

**Root Cause**: Create activity IDs used fragment identifiers (`#create`) that violate ActivityPub dereferenceability requirements.

```
❌ Before:
- Create activity ID: https://lqdev.me/api/activitypub/activities/{hash}#create
- Note ID: https://lqdev.me/api/activitypub/activities/{hash}
- Mastodon fetches: https://lqdev.me/api/activitypub/activities/{hash}
- JSON returns: id with #create fragment
- RESULT: ID mismatch → activity not found
```

**Technical Details**:
- Per RFC 3986, fragment identifiers (`#...`) are **never sent to servers** in HTTP requests
- When Mastodon fetches a URL, it validates that the returned JSON's `id` field **exactly matches** the fetched URL
- The `#create` fragment caused a mismatch: fetched URL didn't include the fragment, but the JSON `id` did
- This is spec-compliant behavior by Mastodon—the W3C ActivityPub Primer recommends avoiding fragments in object identifiers

**Solution**: Inverted the fragment pattern so Create activity IDs match fetchable URLs:

```
✅ After:
- Create activity ID: https://lqdev.me/api/activitypub/activities/{hash}  (fetchable URL)
- Note object ID: https://lqdev.me/api/activitypub/activities/{hash}#object  (uses fragment)
- Mastodon fetches: https://lqdev.me/api/activitypub/activities/{hash}
- JSON returns: id matches exactly
- RESULT: Activity found ✅
```

**Implementation**:
- `generateObjectId`: Creates Note ID with `#object` fragment
- `generateCreateActivityId`: Strips fragments to get base fetchable URL
- `convertToNote`: Uses `generateObjectId` for Note, Create wrapper uses base URL

**Spec Compliance**:
- ✅ W3C ActivityPub: Objects must have "publicly dereferenceable URIs"
- ✅ W3C ActivityPub Primer: "Publishers should avoid using fragments in object identifiers"
- ✅ RFC 3986: Fragment handling aligned with URI specification
- ✅ Mastodon validation: IDs match fetchable URLs

**Files Modified**: `ActivityPubBuilder.fs`

### Azure Function Object Unwrapping Pattern (January 30, 2026)

**Problem**: Even after fixing the fragment pattern, activities were still not discoverable via Mastodon search. Pasting activity URLs in Mastodon returned no results.

**Root Cause Discovery**: Through analysis of Mastodon's `fetch_resource_service.rb` and `resolve_url_service.rb`, we discovered that Mastodon's URL search only accepts **object types** (Note, Article, Image, etc.), not **activity types** (Create, Like, Announce).

```ruby
# From Mastodon's fetch_resource_service.rb
SUPPORTED_TYPES = %w(Note Question).freeze
CONVERTED_TYPES = %w(Image Audio Video Article Page Event).freeze

# Mastodon rejects Create/Like/Announce when searching URLs!
def expected_type?(json)
  equals_or_includes_any?(json['type'], SUPPORTED_TYPES + CONVERTED_TYPES)
end
```

**Key Insight**: The distinction between objects and activities:

| Category | Types | Searchable? | Purpose |
|----------|-------|-------------|--------|
| **Objects** | Note, Article, Image, Video, etc. | ✅ Yes | Content people create and search for |
| **Activities** | Create, Like, Announce | ❌ No | Verbs that wrap or reference objects |

**Architectural Decision**: Rather than maintaining separate endpoints for notes vs activities (Option A), we chose to make the existing activities endpoint **dynamically unwrap Create activities** (Option B).

**Solution**: Modified `api/activitypub-activities/index.js` to:
1. Fetch the activity JSON from CDN
2. If `type === 'Create'`, extract and return the embedded `object` (Note/Article)
3. Update the object's `id` to match the fetchable URL
4. If Like/Announce, return as-is (these live in outbox, not meant to be searched)

```javascript
// CRITICAL FIX: For Create activities, return the embedded object
if (activity.type === 'Create' && activity.object) {
    const obj = activity.object;
    obj['@context'] = 'https://www.w3.org/ns/activitystreams';
    obj.id = `https://lqdev.me/api/activitypub/activities/${activityId}`;
    responseContent = JSON.stringify(obj);
} else {
    responseContent = activityContent;  // Like/Announce unchanged
}
```

**Why This Works**:
- Mastodon fetches URL → gets `type: "Note"` → **accepts it!**
- The Note's `id` matches the fetched URL (no mismatch)
- Like/Announce activities pass through unchanged (for outbox discovery)
- Single endpoint handles all activity types

**Benefits of Option B over Option A (separate notes endpoint)**:
- Simpler architecture: one endpoint, not two
- No path migration needed
- Works for all object types (Note, Article, Image, etc.)
- Preserves existing `/activitypub/activities/` path structure

**Spec Compliance**:
- ✅ W3C ActivityPub: Objects dereferenceable at their ID URLs
- ✅ Mastodon compatibility: Returns searchable object types
- ✅ Outbox integrity: Like/Announce activities unchanged for federation

**Files Modified**: `api/activitypub-activities/index.js`

---

## Architecture Design

### Core Principle: Preserve UnifiedFeedItem Pipeline

The current system flows:
```
Markdown Files → Domain Types → FeedData → UnifiedFeedItem → ActivityPubBuilder → JSON
```

**Extension Strategy**: Add optional metadata fields to UnifiedFeedItem, not replace the pipeline.

### UnifiedFeedItem Extension

Current structure ([GenericBuilder.fs](../../GenericBuilder.fs#L1264-L1275)):
```fsharp
type UnifiedFeedItem = {
    Title: string
    Content: string
    Url: string
    Date: string
    ContentType: string   // "posts", "notes", "star", "reply", "reshare", "bookmarks"
    Tags: string array
    RssXml: XElement
}
```

**Proposed Extension**:
```fsharp
type UnifiedFeedItem = {
    Title: string
    Content: string
    Url: string
    Date: string
    ContentType: string
    Tags: string array
    RssXml: XElement
    // NEW: Optional ActivityPub-specific metadata
    TargetUrl: string option           // For responses (inReplyTo/Like/Announce object)
    ResponseType: string option        // "reply", "reshare", "star", "bookmark"
    UpdatedDate: string option         // For edits (as:updated)
    ReviewMetadata: ReviewAPData option // For reviews
    MediaItems: MediaAPData list option // For albums/media posts
    Location: LocationAPData option     // For geo-tagged content
}

// Supporting types
type ReviewAPData = {
    Rating: float
    Scale: float
    ItemReviewed: string
    ItemType: string        // "book", "movie", "music", etc.
    Author: string option
    Isbn: string option
    ItemUrl: string option
}

type MediaAPData = {
    Url: string
    MediaType: string       // "image/jpeg", "video/mp4", etc.
    AltText: string option
    Caption: string option
}

type LocationAPData = {
    Latitude: float
    Longitude: float
    Name: string option
}
```

### ActivityPubBuilder Conversion Router

New conversion logic in [ActivityPubBuilder.fs](../../ActivityPubBuilder.fs):

```fsharp
/// Activity type determination based on content semantics
let determineActivityType (item: UnifiedFeedItem) : ActivityType =
    match item.ResponseType, item.ContentType with
    | Some "star", _        -> ActivityType.Like
    | Some "reshare", _     -> ActivityType.Announce  
    | Some "reply", _       -> ActivityType.CreateNote { InReplyTo = item.TargetUrl }
    | Some "bookmark", _    -> ActivityType.CreateNote { LinkAttachment = item.TargetUrl }
    | _, "posts"            -> ActivityType.CreateArticle
    | _, "notes"            -> ActivityType.CreateNote { InReplyTo = None }
    | _, "reviews"          -> ActivityType.CreateReview
    | _, "media"            -> ActivityType.CreateMedia
    | _, "album-collection" -> ActivityType.CreateCollection
    | _                     -> ActivityType.CreateNote { InReplyTo = None }  // Fallback
```

### Microformat ↔ ActivityPub Alignment

Your existing microformats provide the semantic foundation:

| Microformat | HTML Class | ActivityPub Property |
|-------------|------------|---------------------|
| Reply | `u-in-reply-to` | `inReplyTo` on Note |
| Repost | `u-repost-of` | `object` on Announce |
| Like | `u-like-of` | `object` on Like |
| Bookmark | `u-bookmark-of` | `attachment` with Link type |
| Rating | `p-rating` | `schema:ratingValue` |
| Entry | `h-entry` | Note/Article object |
| Author | `h-card` | `attributedTo` actor |

This means: **The HTML already has the correct semantics. ActivityPub just needs to mirror them.**

---

## Implementation Phases

### Phase 5A: Response Semantics (Highest ROI) 
**Estimated Effort**: 2-3 days  
**Risk**: Low (additive changes only)

#### Objectives
1. Extend UnifiedFeedItem with ResponseType and TargetUrl
2. Generate Like activities for stars
3. Generate Announce activities for reshares  
4. Generate Note with inReplyTo for replies
5. Maintain backward compatibility for generic clients

#### Tasks

**Task 5A.1: Extend UnifiedFeedItem Type**
- Location: `GenericBuilder.fs`
- Add optional fields: `TargetUrl`, `ResponseType`, `UpdatedDate`
- Update all `convert*ToUnified` functions to populate new fields
- Ensure backward compatibility (all new fields optional)

**Task 5A.2: Extend Response Conversion Function**
- Location: `GenericBuilder.fs` → `convertResponsesToUnified`
- Extract `TargetUrl` from `feedData.Content.Metadata.TargetUrl`
- Map `response_type` to `ResponseType` field
- Preserve existing ContentType behavior for timeline filtering

**Task 5A.3: Create ActivityPub Activity Types**
- Location: `ActivityPubBuilder.fs`
- Add new domain types:
  ```fsharp
  type ActivityPubLike = {
      Context: string
      Id: string
      Type: string  // "Like"
      Actor: string
      Object: string  // TargetUrl
      Published: string
  }
  
  type ActivityPubAnnounce = {
      Context: string
      Id: string
      Type: string  // "Announce"
      Actor: string
      Object: string  // TargetUrl
      Published: string
      To: string array
      Cc: string array option
  }
  ```

**Task 5A.4: Implement Conversion Router with Feature Flag**
- Location: `ActivityPubBuilder.fs`
- Create `convertToActivity` function with pattern matching on ResponseType
- Stars → Like activity
- Reshares → Announce activity
- Replies → Create + Note with inReplyTo
- All others → existing Create + Note/Article

**Feature Flag Implementation**:

Add a feature flag to toggle between old (all Create+Note) and new (mixed activity types) behavior:

```fsharp
// In ActivityPubBuilder.fs or Config
let useNativeActivityTypes = true  // Feature flag

/// Convert UnifiedFeedItem to appropriate ActivityPub activity
let convertToActivity (item: UnifiedFeedItem) =
    if not useNativeActivityTypes then
        // Legacy behavior: everything is Create + Note/Article
        convertToCreateActivity item
    else
        // New behavior: route to appropriate activity type
        match item.ResponseType with
        | Some "star" -> convertToLike item
        | Some "reshare" -> convertToAnnounce item
        | Some "reply" -> convertToCreateNoteWithReplyTo item
        | Some "bookmark" -> convertToCreateNoteWithLink item
        | _ -> convertToCreateActivity item  // Fallback to existing behavior
```

**Flag Lifecycle**:
1. **Initial deployment**: `useNativeActivityTypes = false` (safe, no behavior change)
2. **Testing phase**: `useNativeActivityTypes = true` (verify in staging/local)
3. **Production validation**: Toggle true, verify federation works
4. **Cleanup**: Once confirmed working, remove flag and `convertToCreateActivity` fallback path

**Cleanup Criteria** (when to remove flag):
- [ ] Like activities appear as favorites in Mastodon
- [ ] Announce activities appear as boosts in Mastodon
- [ ] Replies thread correctly
- [ ] No federation errors in 48 hours
- [ ] At least 5 new activities of each type federated successfully

**Task 5A.5: Update Outbox Generation**
- Location: `ActivityPubBuilder.fs` → `buildOutbox`
- Replace uniform `convertToCreateActivity` with router
- Ensure outbox contains mixed activity types
- Maintain reverse-chronological ordering

**Task 5A.6: Migrate to Generic Activities Path**

This task migrates from `/activitypub/notes/` to `/activitypub/activities/` with 301 redirects for backward compatibility.

**Why Migration is Required**:
- Current path: `/activitypub/notes/{hash}.json` — only supports Note objects
- New path: `/activitypub/activities/{hash}.json` — supports Notes, Likes, Announces, Articles, Reviews, etc.
- Remote Mastodon servers have cached URLs like `https://lqdev.me/api/activitypub/notes/abc123`
- These cached URLs must continue working or federation breaks

**Step 1: Add Redirect Rules to `staticwebapp.config.json`**

Open `staticwebapp.config.json` and add this redirect rule in the `routes` array (add near the top, before other activitypub routes):

```json
{
  "route": "/api/activitypub/notes/*",
  "redirect": "/api/activitypub/activities/:splat",
  "statusCode": 301
}
```

Also add a rule for the static file path (non-API):
```json
{
  "route": "/activitypub/notes/*",
  "redirect": "/activitypub/activities/:splat",
  "statusCode": 301
}
```

**Full context** — add these two rules after line 6 in `staticwebapp.config.json`:
```json
{
  "trailingSlash": "auto",
  "mimeTypes": {
    ".activitypub": "application/activity+json",
    ".json": "application/json"
  },
  "routes": [
    {
      "route": "/api/activitypub/notes/*",
      "redirect": "/api/activitypub/activities/*",
      "statusCode": 301
    },
    {
      "route": "/activitypub/notes/*",
      "redirect": "/activitypub/activities/*",
      "statusCode": 301
    },
    {
      "route": "/.well-known/webfinger",
      ...
```

**Step 2: Update Config in `ActivityPubBuilder.fs`**

Find the config section (around line 30-50) and update:

```fsharp
// BEFORE:
let notesPath = "/api/activitypub/notes/"

// AFTER:
let activitiesPath = "/api/activitypub/activities/"
```

**Step 3: Rename `buildNotes` Function to `buildActivities`**

In `ActivityPubBuilder.fs`, rename the function:

```fsharp
// BEFORE:
let buildNotes (items: UnifiedFeedItem list) (outputDir: string) =
    let notesDir = Path.Combine(outputDir, "activitypub", "notes")
    ...

// AFTER:
let buildActivities (items: UnifiedFeedItem list) (outputDir: string) =
    let activitiesDir = Path.Combine(outputDir, "activitypub", "activities")
    ...
```

**Step 4: Update All ID Generation**

Every place that generates an activity ID must use the new path:

```fsharp
// BEFORE:
let noteId = sprintf "%s/api/activitypub/notes/%s" Config.baseUrl hash

// AFTER:
let activityId = sprintf "%s/api/activitypub/activities/%s" Config.baseUrl hash
```

Search for all occurrences of `/activitypub/notes/` in these files:
- `ActivityPubBuilder.fs`
- `Scripts/rss-to-activitypub.fsx`
- Any test scripts

**Step 5: Update Output Directory Creation**

```fsharp
// BEFORE:
let notesDir = Path.Combine(outputDir, "activitypub", "notes")
Directory.CreateDirectory(notesDir) |> ignore

// AFTER:
let activitiesDir = Path.Combine(outputDir, "activitypub", "activities")
Directory.CreateDirectory(activitiesDir) |> ignore
```

**Step 6: Update Program.fs Build Call**

```fsharp
// BEFORE:
ActivityPubBuilder.buildNotes unifiedContent outputDir

// AFTER:
ActivityPubBuilder.buildActivities unifiedContent outputDir
```

**Step 7: Stop Generating to Old Directory**

After the redirect is in place, the build should ONLY generate to `_public/activitypub/activities/`. 

Do NOT generate to `_public/activitypub/notes/` anymore.

**Step 8: Verify Redirect Works**

After deployment, test that old URLs redirect correctly:

```bash
# Test redirect (should return 301 with Location header)
curl -I https://lqdev.me/api/activitypub/notes/d97d66cdd6d8caad5219756036458d70

# Expected response:
# HTTP/2 301
# location: /api/activitypub/activities/d97d66cdd6d8caad5219756036458d70

# Test that new URL works
curl -H "Accept: application/activity+json" \
  https://lqdev.me/api/activitypub/activities/d97d66cdd6d8caad5219756036458d70

# Should return the activity JSON
```

**Files Modified in This Task**:
| File | Change |
|------|--------|
| `staticwebapp.config.json` | Add 2 redirect rules |
| `ActivityPubBuilder.fs` | Rename path config, rename function, update all ID generation |
| `Program.fs` | Update function call from `buildNotes` to `buildActivities` |
| `Scripts/rss-to-activitypub.fsx` | Update path references |

**Validation Checklist**:
- [ ] Build succeeds with no errors
- [ ] `_public/activitypub/activities/` directory created
- [ ] `_public/activitypub/notes/` directory NOT created (or empty)
- [ ] Activity JSON files have `"id": "https://lqdev.me/api/activitypub/activities/{hash}"`
- [ ] Outbox `orderedItems` reference `/api/activitypub/activities/` URLs
- [ ] Old URLs return 301 redirect to new URLs
- [ ] Remote Mastodon servers can still fetch previously-federated content

**Task 5A.7: Update Azure Function Proxy**

There's an Azure Function at `api/activitypub-notes/` that proxies requests to static files. This must be updated alongside the path migration.

**Step 1: Rename the Function Directory**

```bash
# In the api/ directory
mv activitypub-notes activitypub-activities
```

**Step 2: Update `api/activitypub-activities/function.json`**

```json
// BEFORE:
{
  "bindings": [
    {
      "route": "activitypub/notes/{noteId}"
    }
  ]
}

// AFTER:
{
  "bindings": [
    {
      "authLevel": "anonymous",
      "type": "httpTrigger",
      "direction": "in",
      "name": "req",
      "methods": ["get", "options"],
      "route": "activitypub/activities/{activityId}"
    },
    {
      "type": "http",
      "direction": "out",
      "name": "res"
    }
  ]
}
```

**Step 3: Update `api/activitypub-activities/index.js`**

Update the parameter name and CDN URL:

```javascript
// BEFORE:
const noteId = req.params.noteId;
// ... 
const cdnUrl = `https://lqdev.me/activitypub/notes/${noteId}.json`;

// AFTER:
const activityId = req.params.activityId;
// ...
const cdnUrl = `https://lqdev.me/activitypub/activities/${activityId}.json`;
```

Also update:
- All log messages referencing "note" → "activity"
- Cache key prefix: `note_${noteId}` → `activity_${activityId}`
- Error messages mentioning "Note" → "Activity"

**Step 4: Search and Replace in index.js**

Search for all occurrences and update:
| Find | Replace With |
|------|-------------|
| `noteId` | `activityId` |
| `noteCache` | `activityCache` |
| `/activitypub/notes/` | `/activitypub/activities/` |
| `"Note not found"` | `"Activity not found"` |
| `note_${` | `activity_${` |

**Validation Checklist for Task 5A.7**:
- [ ] Function directory renamed from `activitypub-notes` to `activitypub-activities`
- [ ] `function.json` route updated to `activitypub/activities/{activityId}`
- [ ] `index.js` parameter renamed from `noteId` to `activityId`
- [ ] CDN URL path updated to `/activitypub/activities/`
- [ ] Cache keys updated
- [ ] Local function testing passes
- [ ] Deployed function responds at `/api/activitypub/activities/{hash}`

#### Acceptance Criteria
- [ ] Star on lqdev.me → appears as Like in Mastodon notifications
- [ ] Reshare on lqdev.me → appears as boost in Mastodon timeline
- [ ] Reply on lqdev.me → threads correctly under original post
- [ ] Existing posts continue to federate normally
- [ ] Build process completes without errors
- [ ] All 1,547+ items convert successfully

#### Validation Plan
```bash
# Verify activity types in outbox
curl -H "Accept: application/activity+json" https://lqdev.me/api/activitypub/outbox | jq '.orderedItems | group_by(.type) | map({type: .[0].type, count: length})'

# Expected output (based on actual content analysis):
# [
#   { "type": "Create", "count": ~800 },
#   { "type": "Announce", "count": ~496 },  # reshares
#   { "type": "Like", "count": ~145 },       # stars
# ]
# Note: ~103 replies will be Create+Note with inReplyTo

# Verify inReplyTo for reply posts (using NEW /activities/ path)
curl -H "Accept: application/activity+json" https://lqdev.me/api/activitypub/activities/{reply-hash} | jq '.inReplyTo'

# Should return the remote post URL, not null

# Verify old /notes/ URLs redirect to new /activities/ URLs
curl -I https://lqdev.me/api/activitypub/notes/{hash}

# Expected: HTTP 301 with Location: /api/activitypub/activities/{hash}
```

---

### Phase 5B: Bookmark & Link-Centric Content
**Estimated Effort**: 1-2 days  
**Risk**: Low

#### Objectives
1. Bookmarks include Link attachment with bookmarked URL
2. Link renders as preview card in Mastodon
3. Preserve IndieWeb u-bookmark-of semantics

#### Tasks

**Task 5B.1: Extend ActivityPubNote with Link Attachment**
- Add Link type to attachment handling
- Create link attachment when ContentType is "bookmarks"

**Task 5B.2: Update Conversion for Bookmarks**
```fsharp
let convertBookmarkToNote (item: UnifiedFeedItem) : ActivityPubNote =
    let linkAttachment = 
        item.TargetUrl 
        |> Option.map (fun url -> {
            Type = "Link"
            Href = url
            Name = Some item.Title
        })
    // ... rest of Note creation with attachment
```

**Task 5B.3: Test Link Card Rendering**
- Verify Mastodon displays bookmarked URL as link card
- Ensure alt text/title preserved

#### Acceptance Criteria
- [ ] Bookmark posts show link preview in Mastodon
- [ ] Bookmarked URL clearly visible in federated view
- [ ] Content description preserved

---

### Phase 5C: Review Objects with Schema.org ✅ COMPLETE (January 31, 2026)

**Implemented & Verified** - Reviews now include Schema.org vocabulary for rating and item metadata.

| Task | Status | Details |
|------|--------|---------|
| 5C.1: ReviewMetadata Type | ✅ | Created at GenericBuilder module level with all review fields |
| 5C.2: UnifiedFeedItem Extension | ✅ | Added `ReviewData: ReviewMetadata option` field |
| 5C.3: SchemaRating Type | ✅ | `{ Type; RatingValue; BestRating; WorstRating }` |
| 5C.4: SchemaItemReviewed Type | ✅ | `{ Type; Name; Author; Isbn; Url; Image }` |
| 5C.5: Review Conversion | ✅ | `convertBooksToUnified` populates ReviewData from cache |
| 5C.6: Schema.org Properties | ✅ | `convertToNote` generates `schema:reviewRating` and `schema:itemReviewed` |
| 5C.7: Item Type Mapping | ✅ | Maps book→schema:Book, movie→schema:Movie, etc. |

**Verified Output**:
```json
{
  "type": "Note",
  "name": "The Serviceberry Review",
  "schema:reviewRating": {
    "@type": "schema:Rating",
    "schema:ratingValue": 4.8,
    "schema:bestRating": 5,
    "schema:worstRating": 1
  },
  "schema:itemReviewed": {
    "@type": "schema:Book",
    "schema:name": "The Serviceberry",
    "schema:author": "Robin Wall Kimerrer",
    "schema:isbn": "9781668072240",
    "schema:url": "https://www.simonandschuster.com/books/...",
    "schema:image": "https://..."
  }
}
```

**Research Basis**: Per W3C ActivityStreams and Schema.org Review vocabulary specification, using `schema:` prefixed properties enables Fediverse clients to understand review semantics for enhanced display and aggregation.

**Files Modified**:
- `GenericBuilder.fs` - ReviewMetadata type, UnifiedFeedItem extension, BookProcessor cache integration
- `ActivityPubBuilder.fs` - SchemaRating/SchemaItemReviewed types, convertToNote Schema.org property generation

---

### Phase 5D: Media-First Objects 🔄 REVISION IN PROGRESS (January 31, 2026)

**Initial Implementation Complete → Discovered Compatibility Issue → Architectural Revision**

#### Background

The initial Phase 5D implementation created standalone `Video`, `Image`, and `Audio` object types per the ActivityStreams specification. While spec-compliant, testing revealed that **Mastodon and Pixelfed do not render these objects as media players**—they only display a link.

**Detailed Research**: See [phase5d-media-research.md](phase5d-media-research.md) for comprehensive platform analysis.

#### Discovery Summary

| Platform | Standalone Video/Image/Audio | Note + Attachment |
|----------|------------------------------|-------------------|
| **Mastodon** | ❌ Shows link only | ✅ Renders player |
| **Pixelfed** | ❌ Not displayed | ✅ Renders image |
| **PeerTube** | ✅ Native support (Video) | N/A |
| **Funkwhale** | ⚠️ Custom format | ⚠️ Partial |
| **Castopod** | ⚠️ Uses dual pattern | ✅ Note fallback |

**Root Cause**: Mastodon's source code (`fetch_resource_service.rb`) explicitly treats only `Note` and `Question` as "first-class" types. All others are "converted as best as possible."

#### Revised Approach: Dual-Object Pattern

Inspired by Castopod's solution, we will generate **both**:

1. **Primary**: Note object with Document attachment (Mastodon/Pixelfed compatible)
2. **Secondary**: Semantic Video/Image/Audio object at alternate URL (PeerTube compatible)

#### Task Status

| Task | Status | Details |
|------|--------|--------|
| 5D.1: MediaAPData Type | ✅ | Created with MediaUrl, MediaType, ObjectType, AltText, Caption fields |
| 5D.2: MediaExtractor Module | ✅ | Extracts media data from :::media blocks with MIME detection |
| 5D.3: ActivityPubMediaObject Type | ✅ | Native Image/Video/Audio object type (now secondary) |
| 5D.4: Conversion Router (v1) | ✅ | Routes to native objects (needs revision) |
| 5D.5: Feature Flag | ✅ | `useNativeMediaObjects` for safe rollout |
| 5D.6: Note+Attachment Primary | 🆕 | Generate Note with Document attachment as primary |
| 5D.7: Dual Object Generation | 🆕 | Both Note and semantic object at separate URLs |
| 5D.8: Mastodon Extensions | 🆕 | Add blurhash, focalPoint, width, height |
| 5D.9: Alternate Link Tag | 🆕 | Link semantic object via `tag` array |

**Initial Production Metrics (January 31, 2026)** - Before revision:
- 13 Image activities generated
- 1 Video activity generated
- 0 Audio activities (no audio content yet)
- Total: 14 native media activities

**Issue Discovered**: Video activity accessible but Mastodon displays as text+link, not video player.

#### Revised Output Structure

**Primary Activity (Mastodon-compatible)**:
```json
{
  "type": "Create",
  "id": "https://lqdev.me/api/activitypub/activities/{hash}",
  "object": {
    "type": "Note",
    "id": "https://lqdev.me/api/activitypub/activities/{hash}#object",
    "content": "<p>My Rome Travel Video</p>",
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
      "href": "https://lqdev.me/api/activitypub/objects/video-{hash}",
      "rel": "alternate",
      "mediaType": "application/activity+json"
    }]
  }
}
```

**Secondary Object (PeerTube-compatible)**:
```json
{
  "type": "Video",
  "id": "https://lqdev.me/api/activitypub/objects/video-{hash}",
  "name": "My Rome Travel Video",
  "url": [{
    "type": "Link",
    "mediaType": "video/mp4",
    "href": "https://cdn.lqdev.tech/files/videos/rome.mp4"
  }],
  "icon": [{
    "type": "Image",
    "url": "https://cdn.lqdev.tech/files/images/rome-thumbnail.jpg"
  }],
  "duration": "PT2M30S",
  "attributedTo": "https://lqdev.me/api/activitypub/actor"
}
```

#### Key Research Citations

1. **Mastodon Docs**: "Other Object types are converted as best as possible" - [docs.joinmastodon.org/spec/activitypub](https://docs.joinmastodon.org/spec/activitypub/)
2. **GitHub #19357**: "Federated Create->Image activities do not render the image inline" - [github.com/mastodon/mastodon/issues/19357](https://github.com/mastodon/mastodon/issues/19357)
3. **Pixelfed Docs**: "Currently only accepts Create.Note objects" - [pixelfed.github.io/docs-next/spec/ActivityPub.html](https://pixelfed.github.io/docs-next/spec/ActivityPub.html)
4. **Castopod Pattern**: Dual PodcastEpisode + Note approach - [github.com/Podcastindex-org/podcast-namespace/discussions/623](https://github.com/Podcastindex-org/podcast-namespace/discussions/623)

**Files Modified** (Initial):
- `GenericBuilder.fs` - MediaAPData type, MediaExtractor module, UnifiedFeedItem extension
- `ActivityPubBuilder.fs` - ActivityPubMediaObject type, conversion functions, router update

**Files To Modify** (Revision):
- `ActivityPubBuilder.fs` - Dual object generation, Mastodon extensions, Note+attachment primary pattern

---

### Phase 5E: Collections (Albums)
**Estimated Effort**: 2-3 days  
**Risk**: Medium

#### Objectives
1. Albums as OrderedCollection of Image objects
2. Location data as Place object
3. Actor links to album collection

#### Tasks

**Task 5E.1: Album → Collection**
```fsharp
type ActivityPubCollection = {
    Context: string
    Id: string
    Type: string  // "OrderedCollection"
    AttributedTo: string
    Name: string
    Published: string
    TotalItems: int
    OrderedItems: ActivityPubImage array
    Location: ActivityPubPlace option  // If geo-tagged
}

type ActivityPubPlace = {
    Type: string  // "Place"
    Name: string option
    Latitude: float
    Longitude: float
}
```

**Task 5E.2: Update Actor with Collection Links**
```json
{
  "id": "https://lqdev.me/api/activitypub/actor",
  // ... existing fields ...
  "featured": "https://lqdev.me/api/activitypub/featured",
  "albums": "https://lqdev.me/api/activitypub/albums",

  "bookmarks": "https://lqdev.me/api/activitypub/bookmarks",
  "reviews": "https://lqdev.me/api/activitypub/reviews"
}
```

**Task 5E.3: Generate Collection Endpoints**
- During build, generate static JSON for album collection
- Pagination if collection grows large

#### Acceptance Criteria
- [ ] Albums display as galleries in Pixelfed-style clients
- [ ] Location appears on geo-aware clients
- [ ] Actor profile links to album collection

---

### Phase 5F: Outbox Pagination & Actor Enhancements
**Estimated Effort**: 2-3 days  
**Risk**: Low

#### Objectives
1. Paginated outbox (50 items per page)
2. Featured posts collection (pinned)
3. Shared inbox endpoint declaration
4. Summary, source, updated fields

#### Tasks

**Task 5F.1: Implement Outbox Pagination**
```fsharp
let buildPaginatedOutbox (activities: ActivityPubActivity list) (outputDir: string) =
    let pageSize = 50
    let pages = activities |> List.chunkBySize pageSize
    let totalPages = pages.Length
    
    // Generate root collection
    let rootOutbox = {
        Context = Config.activityStreamsContext
        Id = Config.outboxUri
        Type = "OrderedCollection"
        TotalItems = activities.Length
        First = sprintf "%s/page/1" Config.outboxUri
        Last = sprintf "%s/page/%d" Config.outboxUri totalPages
    }
    
    // Generate each page
    pages |> List.iteri (fun i pageItems ->
        let pageNum = i + 1
        let page = {
            Context = Config.activityStreamsContext
            Id = sprintf "%s/page/%d" Config.outboxUri pageNum
            Type = "OrderedCollectionPage"
            PartOf = Config.outboxUri
            Next = if pageNum < totalPages then Some (sprintf "%s/page/%d" Config.outboxUri (pageNum + 1)) else None
            Prev = if pageNum > 1 then Some (sprintf "%s/page/%d" Config.outboxUri (pageNum - 1)) else None
            OrderedItems = pageItems |> List.toArray
        }
        // Write to file
    )
```

**Task 5F.2: Featured Posts Collection**
- Generate from pinned posts (existing feature)
- Static JSON at `/api/activitypub/featured`

**Task 5F.3: Add Summary, Source, Updated**
- Extract summary from description/excerpt
- Include markdown source with `mediaType: text/markdown`
- Use `dt_updated` for updated field

**Task 5F.4: Update Actor JSON**
- Add `endpoints.sharedInbox` (for future)
- Add collection links

#### Acceptance Criteria
- [ ] Outbox returns paginated collection
- [ ] Each page has correct next/prev links
- [ ] Featured posts accessible
- [ ] Large outbox performs well on all clients
- [ ] Updated field reflects edit timestamps

---

## Deployment Sequence

**Critical**: Deploy in this order to avoid broken federation.

### Phase 5A Deployment Steps

```
┌─────────────────────────────────────────────────────────────────┐
│  STEP 1: Deploy Redirects First                                │
│  ─────────────────────────────────────────────────────────────  │
│  File: staticwebapp.config.json                                 │
│  Action: Add /notes/* → /activities/* redirect rules            │
│  Why: Ensures old URLs work during transition                   │
│  Validation: curl -I https://lqdev.me/api/activitypub/notes/... │
│              → Should return 301                                │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│  STEP 2: Deploy Azure Function Changes                         │
│  ─────────────────────────────────────────────────────────────  │
│  Files: api/activitypub-activities/*                            │
│  Action: Rename folder, update function.json and index.js       │
│  Why: API endpoint must exist before static files reference it  │
│  Validation: Function responds at /api/activitypub/activities/  │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│  STEP 3: Deploy F# Build Changes                                │
│  ─────────────────────────────────────────────────────────────  │
│  Files: GenericBuilder.fs, ActivityPubBuilder.fs, Program.fs    │
│  Action: New activity types, conversion router, path updates    │
│  Why: Now generates to /activities/ which function serves       │
│  Validation: Build succeeds, outbox contains mixed types        │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│  STEP 4: Federation Verification                                │
│  ─────────────────────────────────────────────────────────────  │
│  Action: Test from Mastodon instance                            │
│  - Follow @lqdev@lqdev.me                                       │
│  - Verify new posts appear                                      │
│  - Check stars appear as favorites                              │
│  - Check reshares appear as boosts                              │
└─────────────────────────────────────────────────────────────────┘
```

### Rollback Plan

If issues occur:

1. **Revert F# changes** → Old build generates to `/notes/`
2. **Redirects remain** → Old URLs still redirect (harmless)
3. **Function can stay** → Both paths can coexist temporarily

### Hash Consistency Note

**Accepted Risk**: When a star/reshare converts from `Create+Note` to `Like`/`Announce`, the activity ID hash may change because the content structure changes.

**Impact**: Previously-federated URLs for these activities may 404 even with redirects.

**Decision**: This is acceptable. The implementation is still early, follower count is low, and correct information architecture takes priority over preserving potentially-incorrect historical links. New activities will federate correctly going forward.

---

## Risk Assessment

### Low Risk ✅
- **Phase 5A (Responses)**: Additive changes, existing tests validate
- **Phase 5B (Bookmarks)**: Simple attachment addition
- **Phase 5F (Pagination)**: Improves performance, backward compatible

### Medium Risk ⚠️
- **Phase 5C (Reviews)**: Schema.org extension may not be understood by all clients
  - *Mitigation*: Fallback to Article display
- **Phase 5D (Media)**: Pixelfed-specific features may not work everywhere
  - *Mitigation*: Maintain attachment fallback
- **Phase 5E (Collections)**: Complex type, less widely implemented
  - *Mitigation*: Test with Pixelfed, use Note fallback

### Breaking Change Risk
- **None**: All changes are additive. Existing Create+Note behavior preserved as fallback.

---

## Testing Strategy

### Unit Testing
- Test each conversion function in isolation
- Validate JSON structure against ActivityStreams spec
- Use F# script files for quick validation

### Integration Testing
```bash
# Build and validate locally
dotnet build
dotnet run

# Check outbox structure
curl -H "Accept: application/activity+json" http://localhost:5000/api/activitypub/outbox | jq '.orderedItems[:5]'

# Validate activity types
jq '.orderedItems | group_by(.type)' < _public/api/data/outbox/index.json
```

### Federation Testing
1. **Mastodon**: Follow @lqdev@lqdev.me, verify posts appear correctly
2. **Pixelfed**: Verify media posts render as galleries
3. **BookWyrm**: Test review federation (if access available)
4. **Pleroma/Akkoma**: Verify compatibility with alternative implementations

### Validation Checklist Per Phase
- [ ] Build completes without errors
- [ ] All existing content converts successfully
- [ ] New activity types present in outbox
- [ ] JSON validates against ActivityStreams schema
- [ ] Mastodon displays content correctly
- [ ] No regression in existing federation

---

## Success Metrics

### Quantitative
| Metric | Current | Target | How to Measure |
|--------|---------|--------|----------------|
| Activity types in outbox | 1 (Create) | 3+ (Create, Like, Announce) | jq analysis |
| Replies with inReplyTo | 0 | 100% of reply responses | JSON validation |
| Reviews with rating | 0 | 100% of reviews | JSON validation |
| Outbox page load time | Varies | <1s per page | Lighthouse |
| Federation success rate | 95%+ | 98%+ | Delivery monitoring |

### Qualitative
- [ ] Stars appear as favorites in Mastodon
- [ ] Reshares appear as boosts in Mastodon
- [ ] Replies thread correctly
- [ ] Reviews show rating in ActivityPub-aware clients
- [ ] Albums display as galleries
- [ ] Website remains authoritative source

---

## Implementation Timeline

**Recommended Order**: 5A → 5B → 5F → 5D → 5C → 5E

This order prioritizes:
1. **Highest impact** (response semantics) - Like/Announce/inReplyTo
2. **Quick wins** (bookmarks, pagination) - Low effort, high compatibility improvement  
3. **Media enhancement** before reviews - More visual impact
4. **Complex features last** (reviews, collections) - Schema extensions and collection types

| Phase | Duration | Dependencies | Milestone |
|-------|----------|--------------|-----------|
| **5A: Responses** | 2-3 days | None | Like/Announce/inReplyTo working |
| **5B: Bookmarks** | 1-2 days | 5A | Link attachments |
| **5F: Pagination** | 2-3 days | 5A | Paginated outbox |
| **5D: Media** | 2-3 days | 5A | Native media objects |
| **5C: Reviews** | 2-3 days | 5A | Schema.org reviews |
| **5E: Albums** | 2-3 days | 5D | Album collections |

**Total Estimated Effort**: 12-16 days
| **5A: Responses** | 2-3 days | None | Like/Announce/inReplyTo working |
| **5B: Bookmarks** | 1-2 days | 5A | Link attachments |
| **5C: Reviews** | 2-3 days | 5A | Schema.org reviews |
| **5D: Media** | 2-3 days | 5A | Native media objects |
| **5E: Collections** | 3-4 days | 5D | Albums/Playlists |
| **5F: Pagination** | 2-3 days | 5A | Paginated outbox |

**Total Estimated Effort**: 12-18 days

**Recommended Order**: 5A → 5B → 5F → 5C → 5D → 5E

This order prioritizes:
1. Highest impact (response semantics)
2. Quick wins (bookmarks, pagination)
3. Complex features last (collections)

---

## Files to Modify

### Core Implementation Files
| File | Changes |
|------|---------|
| `GenericBuilder.fs` | Extend UnifiedFeedItem, update conversion functions |
| `ActivityPubBuilder.fs` | Add activity types, conversion router, pagination |
| `Domain.fs` | No changes needed (metadata already exists) |
| `Program.fs` | Update build orchestration for new collections |
| `api/data/actor.json` | Add collection links, endpoints |

### New Files to Create
| File | Purpose |
|------|---------|
| `Services/ActivityPub.fs` | ActivityPub conversion utilities |
| `_public/activitypub/activities/*.json` | All activity objects (notes, likes, announces, articles) |
| `_public/api/activitypub/outbox/page/*.json` | Paginated outbox pages |
| `_public/api/activitypub/featured/index.json` | Featured posts collection |
| `_public/api/activitypub/albums/index.json` | Albums collection |
| `_public/api/activitypub/reviews/index.json` | Reviews collection |

### Path Structure Migration
```
Current Structure:
/activitypub/notes/{hash}.json        → Note objects only

New Structure:
/activitypub/activities/{hash}.json   → All activity objects
                                        - Notes (Create + Note)
                                        - Articles (Create + Article)
                                        - Likes (Like activity)
                                        - Announces (Announce activity)
                                        - Reviews (Create + Article w/ schema.org)
                                        - Media (Create + Image/Video/Audio)
```

---

## References

### Specifications
- [W3C ActivityPub](https://www.w3.org/TR/activitypub/)
- [W3C ActivityStreams 2.0 Vocabulary](https://www.w3.org/TR/activitystreams-vocabulary/)
- [W3C ActivityStreams 2.0 Core](https://www.w3.org/TR/activitystreams-core/)
- [Schema.org Review](https://schema.org/Review)

### Implementation Guides
- [Mastodon ActivityPub Spec](https://docs.joinmastodon.org/spec/activitypub/)
- [Pixelfed ActivityPub Spec](https://pixelfed.github.io/docs-next/spec/ActivityPub.html)
- [BookWyrm ActivityPub](https://docs.joinbookwyrm.com/activitypub.html)
- [ActivityPub Primer: Threading](https://www.w3.org/wiki/ActivityPub/Primer/Threading)
- [ActivityPub Primer: Announce](https://www.w3.org/wiki/ActivityPub/Primer/Announce_activity)

### Existing Documentation
- [docs/activitypub/ARCHITECTURE-OVERVIEW.md](./ARCHITECTURE-OVERVIEW.md)
- [docs/activitypub/implementation-status.md](./implementation-status.md)
- [docs/activitypub/phase4-implementation-plan.md](./phase4-implementation-plan.md)

---

## Appendix A: Activity Examples

### Like Activity (for star responses)
Stars can reference any URL on the web—ActivityPub endpoints, blog posts, GitHub pages, etc.

```json
{
  "@context": "https://www.w3.org/ns/activitystreams",
  "id": "https://lqdev.me/api/activitypub/activities/abc123",
  "type": "Like",
  "actor": "https://lqdev.me/api/activitypub/actor",
  "object": "https://maho.dev/2024/02/a-guide-to-implement-activitypub-in-a-static-site-or-any-website/",
  "published": "2024-11-19T10:03:00-05:00"
}
```

### Announce Activity (for reshares)
Reshares work the same way—the object can be any URL you want to boost.

```json
{
  "@context": "https://www.w3.org/ns/activitystreams",
  "id": "https://lqdev.me/api/activitypub/activities/def456",
  "type": "Announce",
  "actor": "https://lqdev.me/api/activitypub/actor",
  "object": "https://indieweb.org/bookmark",
  "published": "2022-10-10T20:39:00-05:00",
  "to": ["https://www.w3.org/ns/activitystreams#Public"],
  "cc": ["https://lqdev.me/api/activitypub/followers"]
}
```

### Reply Note (for reply responses)
Replies reference the original content URL, whether it's an ActivityPub object or a regular web page.

```json
{
  "@context": "https://www.w3.org/ns/activitystreams",
  "id": "https://lqdev.me/api/activitypub/activities/ghi789",
  "type": "Note",
  "attributedTo": "https://lqdev.me/api/activitypub/actor",
  "inReplyTo": "https://maho.dev/2024/02/a-guide-to-implement-activitypub-in-a-static-site-or-any-website/",
  "content": "<p>I've been following along with this amazing series from Maho on implementing ActivityPub on static websites...</p>",
  "published": "2024-11-19T10:03:00-05:00",
  "url": "https://lqdev.me/responses/implement-activitypub-static-site-series-maho/",
  "to": ["https://www.w3.org/ns/activitystreams#Public"],
  "cc": ["https://lqdev.me/api/activitypub/followers"]
}
```

### Review Article
```json
{
  "@context": [
    "https://www.w3.org/ns/activitystreams",
    { "schema": "https://schema.org/" }
  ],
  "id": "https://lqdev.me/api/activitypub/notes/review123",
  "type": "Article",
  "attributedTo": "https://lqdev.me/api/activitypub/actor",
  "name": "The Creative Act: A Way of Being Review",
  "content": "<div class=\"review\"><h3>The Creative Act: A Way of Being</h3><p>Rating: 4.8/5.0</p>...</div>",
  "published": "2024-09-27T07:40:00-05:00",
  "url": "https://lqdev.me/reviews/library/creative-act-way-of-being-rubin/",
  "schema:reviewRating": {
    "@type": "schema:Rating",
    "schema:ratingValue": 4.8,
    "schema:bestRating": 5.0
  },
  "schema:itemReviewed": {
    "@type": "schema:Book",
    "schema:name": "The Creative Act: A Way of Being",
    "schema:author": { "@type": "schema:Person", "schema:name": "Rick Rubin" },
    "schema:isbn": "9780593653425"
  }
}
```

### Album Collection
```json
{
  "@context": "https://www.w3.org/ns/activitystreams",
  "id": "https://lqdev.me/api/activitypub/albums/winter-wonderland-2024",
  "type": "OrderedCollection",
  "attributedTo": "https://lqdev.me/api/activitypub/actor",
  "name": "Winter Wonderland 2024",
  "summary": "Beautiful snowy scenes from our winter adventures in the mountains",
  "published": "2024-01-20T09:00:00-05:00",
  "totalItems": 2,
  "orderedItems": [
    {
      "type": "Image",
      "url": "http://cdn.lqdev.tech/files/images/sunrise.jpg",
      "mediaType": "image/jpeg",
      "name": "First light hitting the mountain peaks"
    },
    {
      "type": "Image",
      "url": "http://cdn.lqdev.tech/files/images/sunset.jpg",
      "mediaType": "image/jpeg",
      "name": "Golden hour in the winter forest"
    }
  ],
  "location": {
    "type": "Place",
    "latitude": 39.1911,
    "longitude": -106.8175
  }
}
```

---

## Appendix B: Delivery Considerations

### Outbox Discovery Model (Current Scope)

For this implementation, Like and Announce activities are generated in the outbox for **discovery only**:

1. Activities are generated during site build
2. Activities appear in the outbox OrderedCollection
3. Individual activity files are dereferenceable at `/activitypub/activities/{hash}.json`
4. Followers receive activities through normal post delivery mechanisms
5. **No active delivery** to original content authors (this could be a future enhancement)

**Rationale**: 
- Keeps implementation simple and static-first
- Original content may not be ActivityPub-aware (regular web pages)
- Avoids complexity of discovering remote inboxes for arbitrary URLs
- Activities are still semantically correct and visible in your outbox

### Future Enhancement: Active Delivery

A future phase could add active delivery for Like/Announce activities when the target IS an ActivityPub object:

1. Check if `targetUrl` resolves to an ActivityPub object (via Accept: application/activity+json)
2. If yes, extract the author's inbox from the object's `attributedTo` actor
3. Queue delivery of the Like/Announce to that inbox
4. This would cause the original author to see your like/reshare in their notifications

This enhancement would require:
- URL probing during build to detect ActivityPub endpoints
- Caching of discovered inboxes
- Integration with existing delivery queue infrastructure

**Not in current scope** but architecturally compatible with the current approach.

---

## Appendix C: Research Sources & References

This plan was developed through comprehensive research using authoritative sources. AI assistants implementing this plan can reference these sources for deeper context, validation, or edge case handling.

### Primary Specifications

| Source | URL | Used For |
|--------|-----|----------|
| **W3C ActivityStreams 2.0** | https://www.w3.org/TR/activitystreams-core/ | Core vocabulary, activity types, object model |
| **W3C ActivityStreams Vocabulary** | https://www.w3.org/TR/activitystreams-vocabulary/ | Type definitions (Like, Announce, Create, Note, Article, Collection) |
| **W3C ActivityPub** | https://www.w3.org/TR/activitypub/ | Client-to-server, server-to-server federation protocol |
| **Schema.org** | https://schema.org/Review | Review, Rating, Book, itemReviewed extension context |

### Implementation References

| Source | Repository/URL | Used For |
|--------|----------------|----------|
| **Mastodon Source** | https://github.com/mastodon/mastodon | Activity processing, Like/Announce handling, inReplyTo threading |
| **Mastodon Documentation** | https://docs.joinmastodon.org/ | API behavior, federation expectations |
| **Pixelfed Source** | https://github.com/pixelfed/pixelfed | Collection/gallery handling, Image object processing |
| **BookWyrm Source** | https://github.com/bookwyrm-social/bookwyrm | Review type extensions, rating federation |

### Key Research Findings & Sources

#### 1. Like and Announce Are Top-Level Activities (Not Wrapped in Create)

**Source**: W3C ActivityStreams 2.0 Vocabulary - Activity Types
- **URL**: https://www.w3.org/TR/activitystreams-vocabulary/#activity-types
- **Key Quote**: "The Like activity indicates the actor likes the object... The Announce activity indicates that the actor is calling the target's attention to the object."
- **Validation**: Mastodon source code `app/lib/activitypub/activity/` shows Like and Announce processed as direct activity types, not unwrapped from Create.

**DeepWiki Analysis** (Mastodon repository):
- Mastodon's `ActivityPub::Activity::Like` handler expects `type: "Like"` directly
- Mastodon's `ActivityPub::Activity::Announce` handler processes boosts/reblogs
- Both handlers extract `object` property to find the liked/announced content

#### 2. inReplyTo Works With Any URL

**Source**: W3C ActivityStreams 2.0 Core - Object Properties
- **URL**: https://www.w3.org/TR/activitystreams-core/#object
- **Key Quote**: "The inReplyTo property... indicates one or more entities for which this object is considered a response."
- **Note**: Spec does not restrict inReplyTo to ActivityPub objects—any IRI is valid.

**Mastodon Behavior** (verified via DeepWiki analysis):
- When `inReplyTo` points to known ActivityPub object → reply threads under original
- When `inReplyTo` points to unknown URL → Mastodon displays as standalone Note with reference
- Semantic intent preserved regardless of target discoverability

**IndieWeb Alignment**:
- `u-in-reply-to`, `u-like-of`, `u-repost-of` microformats work identically—any URL is valid
- This plan mirrors existing microformat semantics in the HTML

#### 3. OrderedCollection Pagination Pattern

**Source**: W3C ActivityStreams 2.0 - Paging
- **URL**: https://www.w3.org/TR/activitystreams-core/#paging
- **Key Properties**: `first`, `last`, `next`, `prev`, `partOf`, `totalItems`
- **Page Type**: `OrderedCollectionPage` for paginated results

**Mastodon Implementation**:
- Mastodon fetches outbox `first` page, follows `next` links
- Standard page size: 20-50 items
- This plan uses 50 items per page (user-specified)

#### 4. Schema.org Extension for Reviews

**Source**: ActivityStreams 2.0 Extensibility
- **URL**: https://www.w3.org/TR/activitystreams-core/#extensibility
- **Pattern**: Add schema.org context to extend vocabulary

**Schema.org Review**:
- **URL**: https://schema.org/Review
- **Properties Used**: `reviewRating`, `itemReviewed`, `ratingValue`, `bestRating`

**BookWyrm Reference** (alternative approach):
- Uses `type: ["Article", "Review"]` multi-type pattern
- Adds `rating` and `scale` properties directly
- This plan uses Schema.org for broader compatibility

#### 5. Media Object Types

**Source**: W3C ActivityStreams Vocabulary - Object Types
- **URL**: https://www.w3.org/TR/activitystreams-vocabulary/#object-types
- **Types**: `Image`, `Video`, `Audio`, `Document`
- **Properties**: `url`, `mediaType`, `name` (caption), `summary` (alt text)

**Pixelfed Behavior**:
- Processes `Image` objects as native media
- Supports `Collection` of `Image` objects for galleries
- `Place` object for location data

### Research Tools Used

| Tool | Purpose | Key Queries |
|------|---------|-------------|
| **Perplexity Research** | ActivityStreams 2.0 spec analysis | "ActivityStreams 2.0 Like vs Create activity", "inReplyTo non-ActivityPub URLs" |
| **DeepWiki** | Mastodon source code analysis | "How does Mastodon process Like activities", "Announce activity handling" |
| **Context7** | Library documentation | F# JSON serialization patterns |
| **Microsoft Docs** | F# best practices | Type design, pattern matching |

### Codebase References

These files in the luisquintanilla.me repository were analyzed:

| File | Lines | Purpose |
|------|-------|---------|
| `GenericBuilder.fs` | L1264-L1303 | UnifiedFeedItem type definition |
| `GenericBuilder.fs` | L1585-L1600 | convertResponsesToUnified function |
| `ActivityPubBuilder.fs` | L298-L435 | Current activity conversion logic |
| `Domain.fs` | L381-L395 | ResponseDetails type with TargetUrl |
| `CustomBlocks.fs` | L31-L130 | ReviewData type definitions |
| `Views/ContentViews.fs` | L57-L77 | Microformat rendering (u-like-of, u-repost-of) |

### Existing Architecture Documentation

| Document | Location | Purpose |
|----------|----------|---------|
| **ARCHITECTURE-OVERVIEW.md** | `docs/activitypub/` | Current 4-phase implementation status |
| **implementation-status.md** | `docs/activitypub/` | Phase completion tracking |
| **notes-function-proxy.md** | `docs/activitypub/` | Notes proxy architecture |
| **PR #1990 Proposal** | `activitypub-expansion-proposal.md` | Original expansion proposal |

### How to Use These Sources

**For validation**: If uncertain about an ActivityPub behavior, check W3C specs first, then Mastodon source for implementation reality.

**For edge cases**: DeepWiki can analyze how Mastodon handles specific scenarios not covered in specs.

**For F# patterns**: Microsoft Docs and existing codebase provide proven implementation patterns.

**For testing**: Mastodon documentation describes expected federation behavior for verification.

---

*Document generated based on PR #1990 analysis, ActivityPub specification research, and existing luisquintanilla.me architecture documentation.*
