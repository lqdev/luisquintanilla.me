# Phase 6A: RSVP ActivityPub Research Summary

**Last Updated**: 2026-01-31  
**Author**: GitHub Copilot (Claude Opus 4.5)  
**Related Issue**: [#2039](https://github.com/lqdev/luisquintanilla.me/issues/2039)  
**Status**: Research Complete, Implementation Pending

---

## Executive Summary

This document captures research findings for implementing RSVP responses that integrate with IndieWeb conventions, Microformats2, and ActivityPub federation. The goal is to enable RSVP responses to external events (e.g., Mobilizon, Meetup, conference pages) from the static site, with proper federation visibility.

**Key Finding**: RSVP responses map to ActivityPub `Accept`, `TentativeAccept`, and `Reject` activity types, following W3C ActivityStreams vocabulary. This is well-supported by event-focused platforms like Mobilizon but has limited support in Mastodon.

---

## Research Sources

### Primary Sources (Specifications)

| Source | URL | Accessed |
|--------|-----|----------|
| W3C ActivityStreams 2.0 Vocabulary | https://www.w3.org/TR/activitystreams-vocabulary/ | 2026-01-31 |
| W3C ActivityPub Specification | https://www.w3.org/TR/activitypub/ | 2026-01-31 |
| IndieWeb RSVP Specification | https://indieweb.org/rsvp | 2026-01-31 |
| Microformats2 h-entry RSVP | https://microformats.org/wiki/h-entry#p-rsvp | 2026-01-31 |

### Implementation References

| Source | URL | Accessed |
|--------|-----|----------|
| Mobilizon Federation Docs | https://docs.mobilizon.org/5.%20Interoperability/1.activity_pub/ | 2026-01-31 |
| Mobilizon Participation Management | https://docs.mobilizon.org/1.%20Use/Events%20and%20Activities/6.manage-participations/ | 2026-01-31 |
| Gathio Fediverse Integration | https://docs.gath.io/using-gathio/fediverse/ | 2026-01-31 |
| Mastodon ActivityPub Spec | https://docs.joinmastodon.org/spec/activitypub/ | 2026-01-31 |
| Mastodon Event Issue #20407 | https://github.com/mastodon/mastodon/issues/20407 | 2026-01-31 |

### Community Discussions

| Source | URL | Accessed |
|--------|-----|----------|
| ActivityPub Primer: Accept Activity | https://www.w3.org/wiki/ActivityPub/Primer/Accept_activity | 2026-01-31 |
| ActivityPub Primer: Reject Activity | https://www.w3.org/wiki/ActivityPub/Primer/Reject_activity | 2026-01-31 |
| SocialHub: Federated Events | https://socialhub.activitypub.rocks/t/federated-events/305 | 2026-01-31 |
| Event Federation Project | https://event-federation.eu/wordpress-activitypub-event-extensions/ | 2026-01-31 |

---

## W3C ActivityStreams RSVP Activity Types

### Accept Activity
**Specification**: https://www.w3.org/TR/activitystreams-vocabulary/#dfn-accept

> "Indicates that the actor accepts the object. The target property can be used in certain circumstances to indicate the context into which the object has been accepted."

**Use Case**: RSVP "yes" - confirming attendance to an event.

**Example from W3C Spec**:
```json
{
  "@context": "https://www.w3.org/ns/activitystreams",
  "summary": "Sally accepted an invitation to a party",
  "type": "Accept",
  "actor": {
    "type": "Person",
    "name": "Sally"
  },
  "object": {
    "type": "Invite",
    "actor": "http://john.example.org",
    "object": {
      "type": "Event",
      "name": "Going-Away Party for Jim"
    }
  }
}
```

### TentativeAccept Activity
**Specification**: https://www.w3.org/TR/activitystreams-vocabulary/#dfn-tentativeaccept

> "A specialization of Accept indicating that the acceptance is tentative."

**Use Case**: RSVP "maybe" or "interested" - expressing tentative interest.

**Example from W3C Spec**:
```json
{
  "@context": "https://www.w3.org/ns/activitystreams",
  "summary": "Sally tentatively accepted an invitation to a party",
  "type": "TentativeAccept",
  "actor": {
    "type": "Person",
    "name": "Sally"
  },
  "object": {
    "type": "Invite",
    "actor": "http://john.example.org",
    "object": {
      "type": "Event",
      "name": "Going-Away Party for Jim"
    }
  }
}
```

### Reject Activity
**Specification**: https://www.w3.org/TR/activitystreams-vocabulary/#dfn-reject

> "Indicates that the actor is rejecting the object. The target and origin typically have no defined meaning."

**Use Case**: RSVP "no" - declining attendance.

**Example from W3C Spec**:
```json
{
  "@context": "https://www.w3.org/ns/activitystreams",
  "summary": "Sally rejected an invitation to a party",
  "type": "Reject",
  "actor": {
    "type": "Person",
    "name": "Sally"
  },
  "object": {
    "type": "Invite",
    "actor": "http://john.example.org",
    "object": {
      "type": "Event",
      "name": "Going-Away Party for Jim"
    }
  }
}
```

---

## IndieWeb RSVP Specification

### Valid RSVP Values
From https://indieweb.org/rsvp (accessed 2026-01-31):

| Value | Meaning | Common Display |
|-------|---------|----------------|
| `yes` | Attending | ✅ Attending |
| `no` | Not attending | ❌ Not Attending |
| `maybe` | Might attend | ❓ Maybe |
| `interested` | Interested but not committed | ⭐ Interested |

**Note**: Values are case-insensitive per the h-entry specification.

### Required Microformats Markup
From https://microformats.org/wiki/h-entry#p-rsvp (accessed 2026-01-31):

```html
<div class="h-entry">
  <p class="p-summary">
    <a href="https://example.com" class="p-author h-card">Your Name</a>
    RSVPs <span class="p-rsvp">yes</span>
    to <a href="https://events.indieweb.org/example" class="u-in-reply-to">Event Name</a>
  </p>
  <time class="dt-published" datetime="2026-01-31T10:00:00-05:00">Jan 31, 2026</time>
</div>
```

**Key Classes**:
- `h-entry`: Root microformat for the RSVP post
- `p-rsvp`: The RSVP value (yes/no/maybe/interested)
- `u-in-reply-to`: Link to the event being responded to
- `p-author h-card`: Author information
- `dt-published`: Publication timestamp

### Using `<data>` Element for Custom Display
The IndieWeb spec allows using the `<data>` element to separate the machine-readable value from the human-readable display:

```html
<data class="p-rsvp" value="yes">I'll be there!</data>
```

This allows displaying "✅ Attending" while the microformat parser reads "yes".

### Webmention Requirement
From IndieWeb RSVP spec:
> "RSVP responses should trigger a webmention to the target event URL on publish/update."

The target event page should:
1. Recognize the incoming webmention as an RSVP (check for `p-rsvp` property)
2. Display the RSVP in an attendee list or counter
3. Handle updates (changed RSVP status) and deletes (410 Gone)

---

## Platform Implementation Analysis

### Mobilizon (Full Support)
**Source**: https://docs.mobilizon.org/5.%20Interoperability/1.activity_pub/ (accessed 2026-01-31)

Mobilizon is the reference implementation for federated event RSVPs:

**Supported Activities**:
- `Join` → Request to participate in an event
- `Accept` → Approve participation (from organizer)
- `Reject` → Decline participation (from organizer)
- `Leave` → Withdraw from event

**Custom Extensions**:
- `mz:participationMessage`: Allows participants to include a note when joining
- Events are represented as proper `Event` objects, not Notes

**Participation Workflow**:
1. User sends `Join` activity to event
2. If auto-approve: Server immediately sends `Accept` back
3. If manual-approve: Organizer reviews and sends `Accept` or `Reject`

**Key Insight**: For our use case (RSVPing to external events, not hosting), we generate `Accept`/`TentativeAccept`/`Reject` activities that reference the event URL in the `object` field.

### Mastodon (Limited Support)
**Source**: https://github.com/mastodon/mastodon/issues/20407 (accessed 2026-01-31)

**Current Limitations**:
- Mastodon transforms `Event` objects into `Note` objects for display
- No native support for `Join` or `Leave` activities on events
- Users can only "Like" events (using `Like` activity), not properly RSVP
- RSVPs from Mastodon users don't register as participation on event platforms

**Quote from Issue #20407**:
> "Mastodon doesn't support Join or Leave activities for event objects. Users on instances don't get likes and since mastodon users don't join or leave events, remote users also do not get an estimate of users interested in participating at those events."

**Implication for Our Implementation**:
- Our `Accept`/`TentativeAccept`/`Reject` activities will appear in our outbox
- Mastodon instances may not render these specially
- Event-focused platforms like Mobilizon should recognize them properly

### Gathio (Creative Workaround)
**Source**: https://docs.gath.io/using-gathio/fediverse/ (accessed 2026-01-31)

Gathio works around Mastodon's limitations:
- Events get unique ActivityPub actor handles (e.g., `@B2Ee4Rpa1@gath.io`)
- Users follow the event actor
- RSVP collected via DM poll mechanism
- Works within Mastodon's constraints

**Not applicable to our use case** (we're RSVPing to external events, not hosting).

---

## Proposed Activity Structure for Our Implementation

### Accept Activity (RSVP Yes)
```json
{
  "@context": "https://www.w3.org/ns/activitystreams",
  "id": "https://lqdev.me/api/activitypub/activities/[hash]",
  "type": "Accept",
  "actor": "https://lqdev.me/api/activitypub/actor",
  "published": "2026-01-31T10:00:00-05:00",
  "to": ["https://www.w3.org/ns/activitystreams#Public"],
  "cc": ["https://lqdev.me/api/activitypub/followers"],
  "object": "https://mobilizon.fr/events/some-event",
  "inReplyTo": "https://mobilizon.fr/events/some-event"
}
```

### TentativeAccept Activity (RSVP Maybe/Interested)
```json
{
  "@context": "https://www.w3.org/ns/activitystreams",
  "id": "https://lqdev.me/api/activitypub/activities/[hash]",
  "type": "TentativeAccept",
  "actor": "https://lqdev.me/api/activitypub/actor",
  "published": "2026-01-31T10:00:00-05:00",
  "to": ["https://www.w3.org/ns/activitystreams#Public"],
  "cc": ["https://lqdev.me/api/activitypub/followers"],
  "object": "https://meetup.com/event/12345",
  "inReplyTo": "https://meetup.com/event/12345"
}
```

### Reject Activity (RSVP No)
```json
{
  "@context": "https://www.w3.org/ns/activitystreams",
  "id": "https://lqdev.me/api/activitypub/activities/[hash]",
  "type": "Reject",
  "actor": "https://lqdev.me/api/activitypub/actor",
  "published": "2026-01-31T10:00:00-05:00",
  "to": ["https://www.w3.org/ns/activitystreams#Public"],
  "cc": ["https://lqdev.me/api/activitypub/followers"],
  "object": "https://events.indieweb.org/2026/01/homebrew-website-club",
  "inReplyTo": "https://events.indieweb.org/2026/01/homebrew-website-club"
}
```

### Design Decisions

1. **`object` field**: Contains the event URL directly (string), not a nested Event object. This is simpler and compatible with non-ActivityPub event sources.

2. **`inReplyTo` field**: Included for compatibility with platforms that use reply threading for RSVPs.

3. **Addressing**: Public (`to: Public`, `cc: followers`) to ensure visibility in outbox and follower feeds.

4. **No `content` field**: Unlike our `Like` and `Announce` activities, RSVP activities don't require content. The activity type itself conveys the meaning.

---

## Relationship to Existing Infrastructure

### Existing :::rsvp Custom Blocks
The codebase already has RSVP infrastructure for **inline custom blocks**:

**Location**: `CustomBlocks.fs`, `BlockRenderers.fs`, `ASTParsing.fs`

**Purpose**: Render RSVP information within markdown content using `:::rsvp` syntax:
```markdown
:::rsvp
event_name: "IndieWebCamp 2026"
event_url: "https://indieweb.org/2026"
event_date: "2026-06-14"
rsvp_status: "yes"
:::
```

**This is different from RSVP as a response_type**:
- Custom blocks: Inline RSVP display within any content
- Response type: Standalone RSVP post responding to an external event

Both can coexist and serve different purposes.

### Existing Response Type Infrastructure

**Current response types**: `reply`, `reshare`, `star`, `bookmark`

**ActivityPub mapping** (from `ActivityPubBuilder.fs`):
- `star` → `Like` activity
- `reshare` → `Announce` activity
- `reply` → `Create` + `Note` with `inReplyTo`
- `bookmark` → `Create` + `Note` with `Link` attachment

**Proposed addition**:
- `rsvp` + `yes` → `Accept` activity
- `rsvp` + `maybe`/`interested` → `TentativeAccept` activity
- `rsvp` + `no` → `Reject` activity

---

## Federation Considerations

### Outbox-Only Approach (Static Site Constraint)
Our static site generates ActivityPub content at build time:
- Activities are placed in the outbox for discovery
- No real-time inbox delivery to event servers
- Followers see activities via outbox polling

### Compatibility Matrix

| Platform | Will See RSVP? | Notes |
|----------|---------------|-------|
| Mobilizon | ✅ Yes | Full support for Accept/TentativeAccept/Reject |
| Mastodon | ⚠️ Partial | May appear in timeline but not as special RSVP UI |
| Pleroma/Akkoma | ⚠️ Unknown | Likely similar to Mastodon |
| Friendica | ✅ Likely | Better event support than Mastodon |
| Pixelfed | ❌ No | Photo-focused, minimal event support |

### Webmention Fallback
For non-ActivityPub event platforms (IndieWeb sites, Meetup, Eventbrite):
- Webmention provides the notification mechanism
- Target site can parse our h-entry with p-rsvp
- This is the primary integration path for IndieWeb events

---

## Future Considerations

### Event Federation Project
**Source**: https://event-federation.eu/ (accessed 2026-01-31)

The Event Federation project (NGI0 funded) is working on:
- WordPress ActivityPub event extensions
- Standardizing event-related ActivityPub patterns
- Improving interoperability between platforms

**Recommendation**: Monitor this project for emerging standards that could enhance our implementation.

### Potential Enhancements (Out of Scope for Phase 6A)
1. **Remote participation RSVP**: `remote-yes`, `remote-maybe` values (per IndieWeb spec)
2. **RSVP with guests**: "+1" or named guests
3. **Calendar integration**: Generate iCal from RSVP yes posts
4. **Backfeed**: Accept RSVPs from others to our events (requires inbox processing)

---

## Implementation Constraints

### GitHub Issue Template Forms Limitation

**Constraint**: GitHub issue template forms do **not support conditional fields**. There is no way to show/hide fields based on other field values.

**Impact**: The `rsvp_status` dropdown cannot be conditionally shown only when `response_type` is "rsvp".

**Workaround Implemented**: The template always shows the `rsvp_status` dropdown with a "not applicable" default option. The description clearly explains it should only be changed for RSVP responses.

**Alternative Approaches Considered**:
1. **Separate RSVP template** (`post-rsvp.yml`) - Cleaner UX but adds maintenance overhead with multiple templates
2. **Encode status in content field** - Hacky, error-prone

**Processing Logic**: The F# processing script (`Scripts/process-response-issue.fsx`) only includes `rsvp_status` in the frontmatter when:
- `response_type` is "rsvp"
- `rsvp_status` is not "not applicable"

This ensures non-RSVP responses are not polluted with irrelevant fields.

**Reference**: GitHub Issue Template Forms documentation does not mention conditional field support as of 2026-01-31.

---

## Conclusion

The research validates the approach proposed in Issue #2039:
1. RSVP as a `response_type` is the correct architectural fit
2. `Accept`/`TentativeAccept`/`Reject` are the proper ActivityPub activity types
3. IndieWeb microformats (`p-rsvp`, `u-in-reply-to`) ensure interoperability
4. Existing infrastructure (response processing, ActivityPub builder) extends naturally

The implementation should proceed as planned in [projects/active/phase-6a-rsvp-response-type.md](../../projects/active/phase-6a-rsvp-response-type.md).
