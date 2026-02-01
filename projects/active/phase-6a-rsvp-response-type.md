# Phase 6A: RSVP Response Type Implementation

## Project Metadata
- **Issue**: [#2039 - Phase 6A: Robust RSVP & RSVP Responses](https://github.com/lqdev/luisquintanilla.me/issues/2039)
- **Branch**: `feature/rsvp-response-type`
- **PR**: [#2041](https://github.com/lqdev/luisquintanilla.me/pull/2041)
- **Started**: 2026-01-31
- **Status**: ✅ Implementation Complete - Copilot Review Addressed - Ready to Merge
- **Research**: [docs/activitypub/phase6a-rsvp-research.md](../../docs/activitypub/phase6a-rsvp-research.md)

---

## Research Summary

> **Full research documentation with citations**: See [docs/activitypub/phase6a-rsvp-research.md](../../docs/activitypub/phase6a-rsvp-research.md)

## Research Findings

### 1. IndieWeb RSVP Specification
**Source**: [indieweb.org/rsvp](https://indieweb.org/rsvp)

**Key Requirements**:
- RSVP is a specialized **reply** to an **event** that indicates attendance intent
- Valid `p-rsvp` values: `yes`, `no`, `maybe`, `interested`
- Required microformats markup:
  - `h-entry` on the container
  - `p-rsvp` with the attendance value
  - `u-in-reply-to` linking to the event URL
  - `p-author h-card` for author information
  - `dt-published` for publication timestamp

**Example Minimal Markup**:
```html
<div class="h-entry">
  <p class="p-summary">
    <a href="https://example.com" class="p-author h-card">Your Name</a>
    RSVPs <span class="p-rsvp">yes</span>
    to <a href="https://events.example.com/event" class="u-in-reply-to">Event Name</a>
  </p>
</div>
```

**Webmention**: RSVP responses should send a webmention to the target event URL upon publish/update.

### 2. ActivityPub RSVP Implementation
**Sources**: W3C ActivityStreams Vocabulary, Mobilizon Federation Docs, Perplexity Research

**Activity Type Mapping**:
| RSVP Status | ActivityPub Activity Type |
|-------------|---------------------------|
| `yes` | `Accept` |
| `maybe` | `TentativeAccept` |
| `interested` | `TentativeAccept` |
| `no` | `Reject` |

**Key Insights from Research**:
1. **Mobilizon Pattern**: Uses `Join` activity for event participation, with `Accept`/`Reject` as responses. However, for RSVP-as-response (not event hosting), using `Accept`/`TentativeAccept`/`Reject` directly is appropriate.

2. **Mastodon Limitation**: Mastodon currently doesn't support proper event RSVP activities - it treats events as Notes. This means our RSVP activities will be visible in our outbox but may not render specially in Mastodon.

3. **Static Site Approach**: Our outbox-only approach is correct - we generate the activities but don't attempt inbox delivery unless the target is a known ActivityPub actor.

4. **Activity Structure** (per W3C spec):
```json
{
  "@context": "https://www.w3.org/ns/activitystreams",
  "id": "https://lqdev.me/api/activitypub/activities/[hash]",
  "type": "Accept",
  "actor": "https://lqdev.me/api/activitypub/actor",
  "object": "https://mobilizon.fr/events/some-event",
  "inReplyTo": "https://mobilizon.fr/events/some-event",
  "published": "2026-02-15T10:00:00-05:00"
}
```

### 3. Existing Codebase Infrastructure

**Already Implemented (:::rsvp custom blocks)**:
- `RsvpData` type in `CustomBlocks.fs` with fields: `event_name`, `event_url`, `event_date`, `rsvp_status`, `event_location`, `notes`
- `RsvpBlock` parser for `:::rsvp` markdown syntax
- `RsvpRenderer` in `BlockRenderers.fs` with proper badge rendering (✅ Attending, ❌ Not Attending, etc.)
- This is for **inline RSVP blocks within content** - different from RSVP as a response_type

**Response Infrastructure**:
- `ResponseDetails` type with: `Title`, `TargetUrl`, `ResponseType`, `DatePublished`, `DateUpdated`, `Tags`, `ReadingTimeMinutes`
- `ResponseType` union: `Reply | Star | Share | Bookmark`
- `process-response-issue.fsx` script handles: `reply`, `reshare`, `star`
- `post-response.yml` issue template with dropdown for response types
- `ContentViews.fs` has body views: `replyBodyView`, `reshareBodyView`, `starBodyView`, `bookmarkBodyView`
- `ActivityPubBuilder.fs` routes: `star` → `Like`, `reshare` → `Announce`, `reply` → `Create+Note with inReplyTo`

**Webmention Infrastructure**:
- `identify-webmentions.fsx` script handles response types for webmention discovery
- Webmention sending is already integrated for responses

---

## Architectural Decisions

### Decision 1: RSVP as Response Type (Not Separate Content Type)
**Decision**: Add `rsvp` as a `response_type` value alongside `reply`, `reshare`, `star`, `bookmark`

**Rationale**:
- RSVPs are semantically responses to external content (events)
- Fits the existing response architecture perfectly
- No new folders, workflows, or content type infrastructure needed
- Consistent with how other response types work

**Alternative Considered**: Create `_src/rsvp/` directory with separate workflow
**Rejected Because**: Adds unnecessary complexity; RSVPs are just a specialized response

### Decision 2: Add `rsvp_status` Field to ResponseDetails
**Decision**: Add optional `RsvpStatus: string` field to `ResponseDetails` type

**Rationale**:
- Only RSVP responses need this field; null/empty for other response types
- Values: `yes`, `no`, `maybe`, `interested` (per IndieWeb spec)
- Minimal change to existing type structure
- YAML alias: `rsvp_status`

### Decision 3: ActivityPub Activity Type Routing
**Decision**: Route RSVP responses to Accept/TentativeAccept/Reject activities in `ActivityPubBuilder.fs`

**Implementation Pattern**:
```fsharp
match item.ResponseType, item.RsvpStatus with
| Some "rsvp", Some "yes" -> convertToAcceptActivity item
| Some "rsvp", Some "maybe" | Some "rsvp", Some "interested" -> convertToTentativeAcceptActivity item
| Some "rsvp", Some "no" -> convertToRejectActivity item
| Some "star", _ -> convertToLikeActivity item
| Some "reshare", _ -> convertToAnnounceActivity item
// ... existing routing
```

**Rationale**:
- Follows existing pattern for response type routing
- Maps correctly to W3C ActivityStreams vocabulary
- Consistent with Mobilizon and other ActivityPub implementations

### Decision 4: Microformats Markup for RSVP View
**Decision**: Create `rsvpBodyView` function in `ContentViews.fs` with proper IndieWeb microformats

**Required Markup**:
- Container: `h-entry response response-rsvp`
- RSVP value: `<data class="p-rsvp" value="yes">✅ Attending</data>`
- Target link: `<a class="u-in-reply-to" href="...">`
- Content: `e-content` class

**Badge Display** (reusing existing pattern from RsvpRenderer):
- `yes` → ✅ Attending
- `no` → ❌ Not Attending
- `maybe` → ❓ Maybe
- `interested` → ⭐ Interested

### Decision 5: GitHub Issue Template Updates
**Decision**: Extend `post-response.yml` with conditional `rsvp_status` field

**Implementation**:
- Add `rsvp` to response_type dropdown options
- Add new `rsvp_status` dropdown (yes/no/maybe/interested)
- Note in template that rsvp_status is only used when response_type is rsvp

### Decision 6: UnifiedFeedItem Enhancement
**Decision**: Add `RsvpStatus: string option` to `UnifiedFeedItem` in `GenericBuilder.fs`

**Rationale**:
- Needed for ActivityPub conversion to determine correct activity type
- Consistent with existing `ResponseType` and `TargetUrl` optional fields
- Flows through the unified content pipeline

---

## Implementation Plan

### Phase 1: Domain Model Updates
1. Add `Rsvp` to `ResponseType` union in `Domain.fs`
2. Add `RsvpStatus: string` to `ResponseDetails` in `Domain.fs`
3. Build and verify no regressions

### Phase 2: GenericBuilder Updates
1. Add `RsvpStatus: string option` to `UnifiedFeedItem`
2. Update `convertResponsesToUnified` to extract and pass `rsvp_status`
3. Build and verify

### Phase 3: ActivityPub Integration
1. Add `convertToAcceptActivity` function
2. Add `convertToTentativeAcceptActivity` function
3. Add `convertToRejectActivity` function
4. Update `convertToActivity` routing logic
5. Build and verify ActivityPub output

### Phase 4: View Layer Updates
1. Add `rsvpBodyView` function in `ContentViews.fs`
2. Update `responsePostView` switch to handle `"rsvp"` case
3. Update `ResponseProcessor.RenderCard` for RSVP rendering
4. Build and verify HTML output

### Phase 5: GitHub Integration
1. Update `post-response.yml` issue template
2. Update `process-response-issue.fsx` script to handle `rsvp` type and `rsvp_status`
3. Update `process-content-issue.yml` workflow extraction

### Phase 6: Testing & Validation
1. Create test RSVP response file
2. Build site and verify:
   - HTML rendering with correct microformats
   - ActivityPub outbox includes Accept/TentativeAccept/Reject
   - RSS feed includes RSVP
   - Timeline displays RSVP with correct badge
3. Validate webmention discovery works for RSVP

---

## Files to Modify

| File | Change Description |
|------|-------------------|
| `Domain.fs` | Add `Rsvp` to union, add `RsvpStatus` field |
| `GenericBuilder.fs` | Add `RsvpStatus` to UnifiedFeedItem, update conversion |
| `ActivityPubBuilder.fs` | Add Accept/TentativeAccept/Reject activity types and routing |
| `Views/ContentViews.fs` | Add `rsvpBodyView` function |
| `.github/ISSUE_TEMPLATE/post-response.yml` | Add rsvp option and rsvp_status dropdown |
| `Scripts/process-response-issue.fsx` | Handle rsvp type and rsvp_status parameter |
| `.github/workflows/process-content-issue.yml` | Extract rsvp_status from issue |

---

## Architectural Decisions

### AD-1: GitHub Issue Template Form Limitation

**Decision**: Use always-visible `rsvp_status` dropdown with "not applicable" default.

**Context**: GitHub issue template forms do not support conditional fields. We cannot show/hide the `rsvp_status` field based on `response_type` selection.

**Alternatives Considered**:
1. **Separate `post-rsvp.yml` template** - Rejected due to maintenance overhead of multiple templates
2. **Encode status in content field** - Rejected as hacky and error-prone

**Consequence**: Users see `rsvp_status` dropdown for all response types. Description clearly explains to leave as "not applicable" for non-RSVP responses. Processing script ignores the field unless response_type is "rsvp".

**Documentation**: See [docs/activitypub/phase6a-rsvp-research.md](../../docs/activitypub/phase6a-rsvp-research.md#implementation-constraints) for full details.

### AD-2: ActivityPub inReplyTo Field for RSVP Activities

**Decision**: Include optional `inReplyTo` field in `ActivityPubRsvp` type, set to same value as `object`.

**Context**: During PR review, Copilot suggested adding `inReplyTo` for platforms that use reply-threading for RSVPs.

**Research Validation** (Perplexity + W3C specs):
- W3C ActivityPub spec is **silent** on `inReplyTo` for Accept activities
- W3C spec examples for Accept/TentativeAccept **omit** inReplyTo entirely
- `object` property provides the definitive semantic relationship
- `inReplyTo` provides supplementary threading context
- Platform behavior varies:
  - **Mobilizon**: Relies on `object` alone, docs don't mention inReplyTo for RSVP
  - **Gathio**: Checks `inReplyTo` for poll-based RSVPs
  - **Mastodon**: Limited event support, doesn't implement specialized RSVP

**Consequence**: Applied the change following "be liberal in what you send" principle. The field is optional (`string option`) and set to the same value as `Object` (the event URL). Platforms that don't use it are unaffected.

**Commit**: `1e35c0a6` - fix(activitypub): Add optional InReplyTo field to ActivityPubRsvp

---

## Success Criteria

1. ✅ RSVP responses can be created via GitHub issue template
2. ✅ RSVP responses render with correct IndieWeb microformats (h-entry, p-rsvp, u-in-reply-to)
3. ✅ RSVP responses appear in ActivityPub outbox as Accept/TentativeAccept/Reject activities
4. ✅ RSVP responses appear in timeline with appropriate badges
5. ✅ RSVP responses appear in RSS feeds
6. ✅ Webmentions are discoverable for RSVP responses
7. ✅ No regressions to existing response types (reply, reshare, star, bookmark)

---

## Out of Scope (per issue #2039)

- Full event hosting or management
- Inbox processing for inbound Accepts from others
- Dedicated `_src/rsvp/` directory
- New response workflow/template (using existing response infrastructure)

---

## References

- [IndieWeb RSVP Spec](https://indieweb.org/rsvp)
- [Microformats2 RSVP Patterns](https://microformats.org/wiki/h-entry#RSVP)
- [W3C ActivityStreams Accept](https://www.w3.org/TR/activitystreams-vocabulary/#dfn-accept)
- [Mobilizon Federation RSVP](https://docs.mobilizon.org/5.%20Interoperability/1.activity_pub/)
- [Issue #2039](https://github.com/lqdev/luisquintanilla.me/issues/2039)
