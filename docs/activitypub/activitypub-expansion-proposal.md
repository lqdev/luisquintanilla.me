# ActivityPub Expansion Proposal

**Goal**: Expand ActivityPub support beyond basic `Create` + `Note/Article` so the Fediverse can understand *what you actually publish* (responses, bookmarks, reviews, media, albums, playlists, locations, and collections), while keeping the implementation aligned with your existing content pipeline. This proposal is a detailed design document intended for incremental execution and low-risk rollout.

---

## 1. Current Implementation Snapshot

### 1.1 ActivityPub generation today
- ActivityPub objects are generated from **Unified Feed** items during the F# build and serialized to static JSON (outbox + note files). The conversion currently creates `Note` or `Article` objects based on content type, wraps them in `Create`, and emits a single `OrderedCollection` outbox. The conversion does not vary by response/bookmark/review/media semantics yet.【F:ActivityPubBuilder.fs†L298-L435】
- The outbox endpoint (`/api/activitypub/outbox`) serves a static JSON file with `application/activity+json` headers via an Azure Function proxy.【F:api/outbox/index.js†L1-L31】
- Individual note dereferencing uses an HTTP proxy function for correct headers and CDN caching, with a strict ID format for the note hash.【F:api/activitypub-notes/index.js†L1-L152】

### 1.2 Content types and reality of the site
Your site spans a broader set of content types than the ActivityPub export currently reflects:
- **Posts** (long-form), **notes** (microblog), **responses** (reply/reshare/star), **bookmarks**, **reviews**, **media**, **albums**, **playlists**, **streams**, and more are all part of the content architecture.【F:README.md†L95-L161】
- **Responses** carry `response_type` and `targeturl` metadata (reply, reshare, star) that should map to ActivityStreams activities but are not exposed in the ActivityPub output today.【F:_src/responses/implement-activitypub-static-site-series-maho.md†L1-L12】
- **Bookmarks** are link-centric posts with `response_type: bookmark` and `targeturl`, also not represented as first-class ActivityPub objects yet.【F:_src/bookmarks/indieweb-bookmark-post.md†L1-L7】
- **Reviews** include structured review metadata in custom `:::review` blocks (item type, rating, ISBN, etc.).【F:_src/reviews/library/creative-act-way-of-being-rubin.md†L1-L20】
- **Media** posts use `:::media` blocks for image/video attachments and captions, which currently only get extracted to attachments for `Note` objects rather than emitting media-first ActivityStreams objects.【F:_src/media/video-upload-test.md†L1-L13】【F:ActivityPubBuilder.fs†L236-L347】
- **Albums** contain multiple media items and optional geo coordinates that could map to `Collection`/`Place` objects but currently do not.【F:_src/albums/winter-wonderland-2024.md†L1-L20】
- **Playlists** are structured track lists with YouTube/Spotify links, currently exported as generic notes if at all.【F:_src/playlists/crate-finds-march-2024.md†L1-L40】

### 1.3 Unified feed limits for ActivityPub mapping
The `UnifiedFeedItem` model is intentionally minimal: title/content/url/date/contentType/tags/rssXml. It does not include response-specific or structured metadata (e.g., `response_type`, `targeturl`, review ratings, media metadata, location).【F:GenericBuilder.fs†L1264-L1303】

**Result:** ActivityPub output cannot express the Fediverse semantics of your content because the feed pipeline is too lossy for specialized fields.

---

## 2. Expansion Principles

1. **Preserve the single source of truth**: Keep the Unified Feed pipeline as the canonical source of data, but extend it to carry more structured metadata where required.
2. **Prefer ActivityStreams-native types**: Map content to `Article`, `Note`, `Image`, `Video`, `Audio`, `Collection`, `Review`, and interaction activities (`Announce`, `Like`), instead of forcing everything into `Create` + `Note`.
3. **Keep static-friendly architecture**: Continue generating static JSON where possible; use Azure Functions only for headers, signing, and inbox/outbox behavior.【F:api/outbox/index.js†L1-L31】【F:api/activitypub-notes/index.js†L1-L152】
4. **Minimize breaking changes**: Add new fields and endpoints without changing existing URLs or behavior.
5. **Be additive and phased**: Each phase should be deliverable, testable, and reversible.

---

## 3. Expansion Map (Content → ActivityStreams)

| Site Content | Current AP Type | Proposed AP Type(s) | Why It Matters |
| --- | --- | --- | --- |
| Posts | `Create` + `Article` | Keep | Already correct for long-form content. |
| Notes | `Create` + `Note` | Keep | Appropriate for microblog notes. |
| Responses (`reply`) | `Create` + `Note` | `Create` + `Note` with `inReplyTo` | Enables true reply threading. |
| Responses (`reshare`) | `Create` + `Note` | `Announce` | Shows as boost/reblog in clients. |
| Responses (`star`) | `Create` + `Note` | `Like` | Shows as likes in inbox/outbox. |
| Bookmarks | `Create` + `Note` | `Create` + `Note` w/ `Link` attachment OR `Add` + `Collection` | Highlights the bookmarked URL. |
| Reviews | `Create` + `Note` | `Review` (schema-aligned) | Preserves ratings and item metadata. |
| Media | `Create` + `Note` + attachments | `Create` + `Image/Video/Audio` | More native rendering in media-first apps. |
| Albums | `Create` + `Note` | `Collection`/`OrderedCollection` + `Image` items | Represents multi-item media sets. |
| Playlists | `Create` + `Note` | `OrderedCollection` + `Audio/Link` items | Preserves track structure and links. |
| Location-aware content | Not represented | `Place` in `location` | Enables geo-aware clients to display location context. |

---

## 4. Detailed Proposals (Each Workstream)

### 4.1 Responses → Reply / Announce / Like
**Problem:** Response content uses `response_type` and `targeturl`, but ActivityPub output ignores these semantics and renders them as generic `Create` + `Note` objects.【F:_src/responses/implement-activitypub-static-site-series-maho.md†L1-L12】【F:ActivityPubBuilder.fs†L298-L367】

**Design:**
1. **Extend `UnifiedFeedItem` with response metadata**:
   - Add optional fields: `ResponseType`, `TargetUrl`, `UpdatedDate`.
   - Populate these fields when converting response RSS items or when reading response frontmatter.
2. **Activity mapping**:
   - `reply` → `Create` + `Note` with `inReplyTo = TargetUrl`.
   - `reshare` → `Announce` activity, `object = TargetUrl` (optionally `Link` object).
   - `star` → `Like` activity, `object = TargetUrl`.
3. **Outbox ordering**: Keep ordering by date regardless of activity type.

**Data extraction sources:**
- Response frontmatter and current response publishing workflow (e.g., `response_type`, `targeturl`).【F:_src/responses/implement-activitypub-static-site-series-maho.md†L1-L12】

**Acceptance criteria:**
- Replies appear as reply threads in Mastodon-style clients.
- Reshares appear as boosts.
- Stars appear as likes.

---

### 4.2 Bookmarks → Link-centric ActivityStreams objects
**Problem:** Bookmark posts are link-centric but ActivityPub currently treats them as generic notes without emphasizing the bookmarked URL.【F:_src/bookmarks/indieweb-bookmark-post.md†L1-L7】【F:ActivityPubBuilder.fs†L298-L347】

**Design Options:**
- **Option A (minimal):** `Create` of `Note` with a `Link` in `attachment` or `url` fields.
- **Option B (semantic):** `Add` activity where `object` is a `Link` and `target` is a `bookmarks` collection.

**Recommendation:** Start with Option A for compatibility, then add a `bookmarks` collection for Option B (or both).

**Acceptance criteria:**
- Bookmark posts show the original URL prominently in clients.

---

### 4.3 Reviews → ActivityStreams Review objects
**Problem:** Reviews contain detailed metadata (rating, item type, ISBN, etc.) but are exported as generic notes today.【F:_src/reviews/library/creative-act-way-of-being-rubin.md†L1-L20】

**Design:**
1. Parse `:::review` block metadata into a structured model.
2. Emit `Review` objects (or `Article` with `schema:Review` context) that include:
   - `rating` (with scale),
   - `item` (Book/Movie/Music/Product/Business),
   - `url` to the reviewed item.
3. Extend `@context` to include `schema` namespace when review objects are present.

**Why this aligns with your site:** Reviews are a first-class content type (books, movies, music, business, products).【F:README.md†L12-L21】【F:_src/reviews/library/creative-act-way-of-being-rubin.md†L1-L20】

**Acceptance criteria:**
- Review objects preserve rating and item metadata in JSON.

---

### 4.4 Media posts → Image/Video/Audio objects
**Problem:** Media posts are driven by `:::media` blocks but are emitted as `Note` with attachments instead of media-first objects, which limits native rendering on media-first Fediverse platforms.【F:_src/media/video-upload-test.md†L1-L13】【F:ActivityPubBuilder.fs†L236-L347】

**Design:**
1. Detect `post_type: media` or presence of media blocks.
2. Emit `Image`, `Video`, or `Audio` objects directly, with `url`, `mediaType`, `name` (caption), and `summary` (alt text).
3. Use `Create` activity for each media object, or group into a `Collection` for multiple media items.

**Acceptance criteria:**
- Media posts render natively on Pixelfed-style clients.

---

### 4.5 Albums → Collection + Place
**Problem:** Albums contain multiple media items and optional geo coordinates, but are exported as generic notes, losing the collection and location semantics.【F:_src/albums/winter-wonderland-2024.md†L1-L20】

**Design:**
1. Emit a `Collection`/`OrderedCollection` object for each album.
2. Each item is an `Image` (or `Video`) with captions and alt text.
3. If geo coordinates are present, add `location` with a `Place` object (`latitude`, `longitude`, `name`).

**Acceptance criteria:**
- Album JSON contains `orderedItems` with media items and optional `location` object.

---

### 4.6 Playlists → OrderedCollection of Audio/Link
**Problem:** Playlists are structured track lists with external links, but export as plain notes without structure.【F:_src/playlists/crate-finds-march-2024.md†L1-L40】

**Design:**
1. Parse track entries into a structured list: `name`, `artist`, `duration`, `url` (YouTube/Spotify).
2. Emit an `OrderedCollection` object for the playlist.
3. Each item is an `Audio` or `Link` object with `name` and `url`.

**Acceptance criteria:**
- Playlist JSON contains ordered tracks as ActivityStreams items.

---

### 4.7 Outbox paging
**Problem:** The outbox is a single `OrderedCollection` file. Pagination improves compatibility and performance for large timelines.【F:ActivityPubBuilder.fs†L367-L435】【F:api/outbox/index.js†L1-L31】

**Design:**
1. Generate outbox pages (e.g., `page-1.json`, `page-2.json`), each a `OrderedCollectionPage`.
2. Root outbox includes `first`, `last`, and `totalItems`.
3. Serve pages either via query params or dedicated paths.

**Acceptance criteria:**
- Root outbox exposes `first` and `last` with valid `OrderedCollectionPage` links.

---

### 4.8 Enrich ActivityPub objects with summary, source, updated
**Problem:** Many ActivityPub objects omit `summary`, `source`, and `updated`, which can improve display and editing semantics in clients.【F:ActivityPubBuilder.fs†L298-L347】

**Design:**
1. Add `summary` extraction from existing description/excerpt fields.
2. Add `source` with `mediaType: text/markdown` to preserve original markdown.
3. Add `updated` using `dt_updated` where available in frontmatter (especially for responses).

**Acceptance criteria:**
- Objects include `summary` and `updated` where available; `source` present for markdown-backed items.

---

### 4.9 Actor enhancements: featured posts, collections, shared inbox
**Problem:** Actor metadata does not expose `featured`, `bookmarks`, `playlists`, `albums`, or `sharedInbox`, limiting discoverability of your richer content sets.【F:api/data/actor.json†L1-L57】

**Design:**
1. Generate `featured` collection for pinned posts (align with existing pinned posts workflows).
2. Add new collections for `bookmarks`, `playlists`, `albums`, `reviews`.
3. Add `endpoints.sharedInbox` if adopting shared inbox patterns later.

**Acceptance criteria:**
- Actor JSON links to new collections without changing existing fields.

---

## 5. Schema & Pipeline Impact

### 5.1 UnifiedFeedItem expansion (core change)
- Add optional fields for response data, review metadata, media/album details, and location.
- Maintain backward compatibility by defaulting missing fields.

### 5.2 ActivityPubBuilder updates
- Introduce a conversion router that selects ActivityStreams object/activity types based on `ContentType` + new metadata fields.
- Keep current `Note`/`Article` generation as fallback.

---

## 6. Phased Roadmap (Low-Risk)

### Phase 1: Response semantics (highest ROI)
- Extend `UnifiedFeedItem` for `ResponseType` and `TargetUrl`.
- Map `reply`, `reshare`, `star` to `Create`/`Announce`/`Like`.

### Phase 2: Media-first objects
- Emit `Image`/`Video`/`Audio` objects for media posts.
- Preserve attachments for compatibility.

### Phase 3: Reviews
- Parse `:::review` metadata and emit `Review` objects with schema context.

### Phase 4: Collections & playlists
- Albums and playlists as `OrderedCollection` objects.
- Add collection endpoints and actor links.

### Phase 5: Outbox paging + actor enhancements
- Add `OrderedCollectionPage` support.
- Add `featured` and related collection links in actor.

---

## 7. Validation Plan

- **Static JSON validation**: ensure serialized JSON matches the object types declared (e.g., `Review`, `Collection`, `Like`).
- **Client smoke tests**: verify a small set of items in Mastodon/Pixelfed/BookWyrm clients render correctly.
- **Backwards compatibility**: compare outbox pre/post migration to ensure baseline behavior is preserved for generic clients.

---

## 8. Alignment With Your Content Strategy

- Your site is intended to be the authoritative source of your identity and content, with ActivityPub as the distribution layer. That requires higher-fidelity object mapping so content types (reviews, media, playlists) are represented natively in the Fediverse instead of flattened into generic notes.【F:_src/responses/implement-activitypub-static-site-series-maho.md†L13-L34】
- You already invested in rich structured content (reviews, media blocks, playlists, collections). The ActivityPub export should surface those strengths rather than reduce them.【F:README.md†L12-L21】【F:_src/reviews/library/creative-act-way-of-being-rubin.md†L1-L20】【F:_src/media/video-upload-test.md†L1-L13】

---

## 9. Internal References (Implementation Anchors)

- ActivityPub builder implementation and current ActivityPub output mapping.【F:ActivityPubBuilder.fs†L298-L435】
- Unified feed model and current limits on structured metadata.【F:GenericBuilder.fs†L1264-L1303】
- Actor JSON profile (current fields).【F:api/data/actor.json†L1-L57】
- Example response with `response_type` metadata.【F:_src/responses/implement-activitypub-static-site-series-maho.md†L1-L12】
- Example bookmark post metadata.【F:_src/bookmarks/indieweb-bookmark-post.md†L1-L7】
- Example review with `:::review` metadata block.【F:_src/reviews/library/creative-act-way-of-being-rubin.md†L1-L20】
- Example media post with `:::media` block.【F:_src/media/video-upload-test.md†L1-L13】
- Example album with location metadata and media list.【F:_src/albums/winter-wonderland-2024.md†L1-L20】
- Example playlist with structured track list.【F:_src/playlists/crate-finds-march-2024.md†L1-L40】
