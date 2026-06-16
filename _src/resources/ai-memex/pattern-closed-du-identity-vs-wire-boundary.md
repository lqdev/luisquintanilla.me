---
title: "Closed DU for Identity & Dispatch; Strings at the Wire Boundary"
description: "When upgrading scattered string literals to a closed discriminated union, the DU should govern identity and dispatch — but strings must survive at serialization boundaries that carry out-of-taxonomy values."
entry_type: pattern
published_date: "2026-06-11 14:40 -05:00"
last_updated_date: "2026-06-11 14:40 -05:00"
tags: "fsharp, dotnet, architecture, patterns, refactoring"
related_skill: write-ai-memex
source_project: lqdev-me
related_entries: pattern-content-type-taxonomy-mismatch, pattern-false-unification, pattern-fsharp-module-extraction-inference-flip
---

## Discovery

Phase 2.7 of the architecture refactor upgraded `ContentTypes.fs` from a module of
`[<Literal>]` strings to a closed `[<RequireQualifiedAccess>] type ContentType` DU. The
assessment's stated goal (F5) was *exhaustiveness*: adding `| Recipes` later should make the
compiler enumerate every site that must handle it, converting an 8-file human checklist into
compiler-guided errors.

The naive reading was "replace all consumers of the literals with the DU." That reading is
wrong, and following it would have **broken byte-identical output**.

## Root Cause

Two facts collided:

1. **Not every literal use is a dispatch.** A grep for actual case-pattern matches
   (`^\s*\|\s*ContentTypes\.`) found the literals are matched-on in exactly **two** places
   (`urlPrefix` URL routing, `toJsonFeedContent` JSON-feed body decision). The other ~90 uses
   are feed/tag **keys** (`(ContentTypes.Posts, …)`), **equality comparisons**
   (`item.ContentType = ContentTypes.Posts`), and **setters**
   (`ContentType = Some ContentTypes.Notes`). Those are *serialization* — producing or comparing
   the wire string — not dispatch. A DU buys them nothing; they are exhaustive-by-construction
   (one converter per type).

2. **The wire field carries out-of-taxonomy values.** `UnifiedFeedItem.ContentType` is a
   `string` that, for responses, holds the **subtype** (`reply`/`reshare`/`star`/`rsvp`/
   `bookmark`) rather than the canonical `responses`. That string is serialized verbatim into the
   timeline's client JSON (`"contentType":"reply"`). The subtypes are **not** `ContentType` cases
   — they are `Domain.ResponseType` values. Forcing the DU onto that field is impossible without
   either (a) widening the DU with non-content-type cases (polluting the taxonomy) or (b) changing
   byte-visible output.

So the closed DU's enforcement value lands **only at the dispatch sites**, and strings must
remain at the serialization boundary.

## Solution

Model **identity** with the closed DU; keep **strings at the wire boundary**; connect them with a
single `parse`/`serialize` pair.

```fsharp
[<RequireQualifiedAccess>]
type ContentType =
    | Posts | Notes | Responses | Bookmarks | Snippets | Wiki | Presentations
    | Reviews | Media | Streams | AiMemex | AlbumCollection | PlaylistCollection

let serialize (ct: ContentType) =          // identity -> wire, exhaustive, NO wildcard
    match ct with
    | ContentType.Posts -> Posts | ContentType.Notes -> Notes (* … all 13 … *)

let parse (s: string) : ContentType option = // wire -> identity (intake boundary)
    match s with
    | Posts -> Some ContentType.Posts (* … *) | _ -> None   // wildcard OK: matching a string
```

Make the genuine dispatch sites exhaustive over the DU, with **no wildcard**:

```fsharp
let urlPrefix (ct: ContentType) =           // adding a case won't compile until handled here
    match ct with
    | ContentType.Posts -> "/posts/" (* … all 13 explicit … *)

// String-boundary shim: preserves the old `| other -> sprintf "/%s/" other` verbatim,
// so response subtypes and unmapped values stay byte-for-byte identical.
let urlPrefixForKey (s: string) =
    match parse s with Some ct -> urlPrefix ct | None -> sprintf "/%s/" s
```

For a dispatch that consumes a wire string, route it through `parse` and match
`Some <case> | None` exhaustively — still no wildcard, so a new case forces a decision:

```fsharp
match ContentTypes.parse item.ContentType with
| Some ContentType.Posts | Some ContentType.Notes | … -> convertMd item.Content
| Some ContentType.Responses | … | None -> item.Content
```

Result: the F5 exhaustiveness payoff at the two sites that matter, the taxonomy decision made
explicit (subtypes live on `ResponseType`, excluded from `ContentType`), and **0 diffs** across
13,510 generated files.

## Prevention

- **Before "replace all literals with the DU," grep for actual case-pattern matches.** The
  enforcement value of a closed DU is at *dispatch* sites (matches that branch on identity), not
  at *serialization* sites (keys, equality, setters). Count them first; the migration is usually
  far smaller than it looks.
- **Distinguish identity from wire.** A DU is the right model for identity. A serialization
  boundary that must round-trip values *outside* the taxonomy (here, response subtypes flowing
  through a shared field) stays a string by design. Don't widen the DU to swallow wire-only
  values — that destroys the taxonomy the DU exists to enforce.
- **Keep a string shim at the boundary** so callers holding a wire string preserve any pre-existing
  fallback (`| other -> …`) byte-for-byte. The no-wildcard rule applies to matches *on the DU*;
  a wildcard when matching an *arbitrary string* (in `parse`) is correct and necessary.
- **The wildcard ban is the whole point.** A single `| _ ->` in a content-type match silently
  disables the exhaustiveness you just paid for. Enforce it in review.
