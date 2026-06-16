---
title: "Pattern: One Surface, Two Renderers — Drift Across the Server/Client Progressive-Loading Boundary"
description: "When a single view is drawn by a server-side renderer for its initial window and a client-side renderer for progressively-loaded items, every derived value (badge, URL) gets computed twice and silently drifts — and recency decides which path draws an item, so a server-only fix never fires for older content."
entry_type: pattern
published_date: "2026-06-16 10:37 -05:00"
last_updated_date: "2026-06-16 10:37 -05:00"
tags: "fsharp, javascript, web, architecture, performance, patterns, lqdev-me"
related_skill: "write-ai-memex"
source_project: "lqdev-me"
related_entries: pattern-progressive-loading, pattern-content-type-taxonomy-mismatch, pattern-closed-du-identity-vs-wire-boundary, pattern-response-type-badge-specificity
---

## Discovery

RSVP cards on the homepage timeline showed a generic `rsvp` badge with no attendance
status — you couldn't tell *yes* from *maybe* from a card, only by opening the detail page
(which correctly shows `✅ yes to {url}`). While fixing it, two things were surprising:

1. **A server-side fix looked correct in review but never fired.** I first enriched the
   initial-card badge in `Views/TimelineViews.fs` to render `RSVP · {Status}`. The code was
   right, the build was clean — and the actual RSVP cards on the live homepage still showed a
   raw lowercase `rsvp`. The existing RSVPs are from January 2026; they fall *outside* the
   server-rendered initial window, so that code path is never exercised for them.

2. **The same value was being derived in two places that disagreed.** The badge label and the
   permalink for a timeline card are each computed *once in F#* (for the initial cards) and
   *again in JavaScript* (for progressively-loaded cards). The two derivations had drifted: the
   JS badge map had no `rsvp` case (so it printed the raw key), and a separate progressive-link
   URL shim routed response subtypes to `/rsvp/{file}/` instead of `/responses/{file}/`.

## Root Cause

The homepage timeline is **one logical surface drawn by two physically separate renderers**, a
direct consequence of the [[pattern-progressive-loading|progressive-loading]] architecture:

- **Server path** — `Views/TimelineViews.fs` renders roughly the first ~50 cards as static HTML
  at build time (badge match + `getProperPermalink`, `TimelineViews.fs:201`).
- **Client path** — the remaining items are emitted as `remainingContentData-{key}` JSON
  `<script>` blocks and rendered in the browser by `_src/js/timeline.js` (`createCard`, with its
  own `contentTypeBadge` map and its own URL construction).

Three independent traps fall out of this split:

1. **Recency decides the dispatch.** Whether an item is drawn by F# or by JS depends purely on
   how recent it is. Old content (Jan-2026 RSVPs) renders *only* via the JS path. So a fix to the
   server renderer is invisible for exactly the content that motivated the bug report — and a
   quick local check on a freshly-authored item would render server-side and *mask* the defect.

2. **Duplicated derivation drifts.** The badge label and the permalink are each derived twice —
   once server-side, once client-side — with no shared source of truth. Response subtypes
   (`reply`/`reshare`/`star`/`rsvp`) flow through both as the wire `contentType`
   (see [[pattern-closed-du-identity-vs-wire-boundary]]), and the two derivations had silently
   diverged:
   - **Badge:** `TimelineViews.fs` had a match arm; `timeline.js`'s badge object had no `rsvp`
     key → fell through to the raw string.
   - **URL:** `getProperPermalink` (`TimelineViews.fs:214`) correctly mapped
     `star|reply|reshare|rsvp → /responses/`, but the progressive serializer
     (`TimelineViews.fs:310`) built links via `ContentTypes.urlPrefixForKey`, whose `parse`
     returns `None` for subtypes, leaving the `| other -> sprintf "/%s/" other` fallback to emit
     `/rsvp/`, `/star/`, etc. Two functions answering "what's the URL prefix for this key?"
     gave different answers.

3. **The client can only render what the JSON carries.** Even with a correct JS badge, the
   browser cannot show RSVP status unless that status crosses the boundary. The serialized record
   `ProgressiveContentItem` (`TimelineViews.fs:28`) carried `title/contentType/date/url/content/
   tags` but **not** `rsvpStatus` — so the data needed for the fix simply wasn't on the wire.

## Solution

Treat the **JSON record as the contract** between the two renderers, and make the client path
mirror the server path for every value it must show.

1. **Thread the missing field through the wire record** (plain `string`, empty = none — STJ
   serializes F# `option` poorly without FSharp.SystemTextJson; see
   [[pattern-stj-private-record-empty-object]]):
   ```fsharp
   type ProgressiveContentItem =
       { title: string; contentType: string; date: string
         url: string; content: string; rsvpStatus: string; tags: string[] }
   // ...
   rsvpStatus = (match item.RsvpStatus with Some s -> s | None -> "")
   ```

2. **Give the JS renderer the same case the F# renderer has:**
   ```javascript
   if (item.contentType === 'rsvp') {
       const s = item.rsvpStatus;
       return s ? `RSVP · ${s.charAt(0).toUpperCase()}${s.slice(1)}` : 'RSVP';
   }
   ```

3. **Reconcile the two URL derivations.** Make `urlPrefixForKey` agree with `getProperPermalink`
   for subtypes instead of relying on the `/%s/` fallback (`ContentTypes.fs:163`):
   ```fsharp
   | None ->
       match contentType with
       | Reply | Reshare | Star | Rsvp -> "/responses/"
       | Bookmark -> "/bookmarks/"
       | other -> sprintf "/%s/" other
   ```

4. **Verify against the generated artifact *and* old content**, not a fresh item: confirm
   `"contentType":"rsvp"..."rsvpStatus":"maybe"` is present in `_public/index.html`, that the
   deployed `_public/assets/js/timeline.js` is byte-identical to source (MD5), and that the
   progressive RSVP links resolve to `/responses/...`.

## Prevention

- **A "single view" with progressive loading is two renderers; enumerate every derived value and
  confirm both paths compute it identically.** Badges, permalinks, date formatting, icons — each
  is a drift candidate. The durable fix is a *shared source of truth* (derive server-side, ship
  the rendered value or a small enum in the JSON, let the client only display); short of that,
  every server-side branch needs a mirrored client-side branch.
- **Recency-dependent dispatch hides bugs from the obvious test.** When the renderer is chosen by
  position-in-feed, always reproduce on the actual *old* item that triggered the report. A
  freshly-authored test item renders down the server path and will pass while the real content
  stays broken.
- **The client can only show what the wire record carries.** Before touching client rendering,
  check the serialized contract first — a missing field is an upstream fix, not a JS fix.
- **Two functions named "the URL/badge/prefix for this key" are a coupling hazard.** Here
  `getProperPermalink` and `urlPrefixForKey` independently encoded the same subtype→route table;
  one was right and one had a latent `/%s/` fallback that only became a *live* bug once subtypes
  flowed through it into emitted links. Collapse such pairs to one function, or assert they agree.
- **Diagnose generator bugs from `_public/`, not the source.** As with the sibling
  [[pattern-content-type-taxonomy-mismatch]], the F# compiled cleanly and the source looked
  fine; the defect was only visible in the emitted HTML/JSON and the deployed JS asset.
