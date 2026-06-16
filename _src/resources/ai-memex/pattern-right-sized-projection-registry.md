---
title: "Right-Sized Projection Registry — Collapse Scattered Lists Without Erasing the Type"
description: "When duplicated membership lists beg for a registry, project already-typed results into one homogeneous table instead of erasing 'T into an existential/interface registry — you get the single source of truth without a doctrine regression or byte-identity risk."
entry_type: pattern
published_date: "2026-06-11 17:30 -05:00"
last_updated_date: "2026-06-11 17:30 -05:00"
tags: "fsharp, dotnet, architecture, patterns, type-systems, composition"
related_skill: write-ai-memex
source_project: lqdev-me
related_entries: pattern-false-unification, pattern-closed-du-identity-vs-wire-boundary, pattern-generators-as-data-build-driver
---

## Discovery

Three hand-maintained lists lived inline in `Program.fs`, each declaring which content types
belonged to a feed surface: the homepage `timelineFeedItems` (7 of 12 types), the
`allUnifiedItems` fire-hose (all 12), and `blogArchiveFeedItems` (posts + notes + responses).
Adding a content type meant editing two or three of these *and remembering which*. Forgetting
one silently dropped the type from that surface — the exact bug class already captured in
`[[pattern-content-type-taxonomy-mismatch]]`. The obvious fix: "make a registry."

The plan's literal sketch asked for **one record per content type holding everything** — identity,
URL prefix, source dir, **processor, and views**. Taken at face value, that record is the trap.

## Root Cause

Putting *processor and views for every type* in one table means holding `ContentProcessor<Post>`
and `ContentProcessor<Note>` side by side — F# can only do that by **erasing `'T`**. The natural
way to erase is an existential interface:

```fsharp
type IContentType =
    abstract member BuildAndProject : srcDir:string -> outDir:string -> ProducedContent
let registry : IContentType list = [ { new IContentType with ... }; ... ]
```

Two things make that a net loss here:

1. **It reintroduces the hierarchy the build driver deliberately rejected.** The generic build
   driver ([[pattern-generators-as-data-build-driver]]) is a *record of functions* consumed by one
   function — composition, not an `IBuilder` base class. An existential registry walks that back to
   `{ new IContentType with … }` object expressions — a regression against composition-over-inheritance.
2. **Erasure doesn't even pay off.** Four of the twelve types have *type-specific* downstream
   consumers that need the concrete `'T`: a bookmarks landing page (`FeedData<Response>`), AI Memex
   pages (`FeedData<AiMemex>`), the text-only site (`FeedData<Presentation>`), and an intentional
   second build pass that is load-bearing for byte-identity (a global gradient-ID counter). Those
   keep their typed handles no matter what — so the erased registry becomes a redundant *parallel*
   structure. Worse, iterating it as a build *loop* reorders those global counters and breaks
   byte-identical output.

## Solution

Build a **projection registry**, not a driver. Keep the typed `buildX()` / `convertX()` calls
exactly as they are; then project their *already-computed results* into one homogeneous table whose
rows differ only by data — no `'T`, no interface:

```fsharp
type ContentTypeRoster =
    { Identity: ContentTypes.ContentType            // closed DU (the identity authority)
      Unified: UnifiedFeeds.UnifiedFeedItem list    // the type's already-projected items
      InTimeline: bool
      InAllFeeds: bool
      InBlogArchive: bool }

let private project pred roster =
    roster |> List.filter pred |> List.map (fun r -> ContentTypes.serialize r.Identity, r.Unified)
let timeline    = project (fun r -> r.InTimeline)
let allFeeds    = project (fun r -> r.InAllFeeds)
let blogArchive = project (fun r -> r.InBlogArchive)
```

`Program.fs` builds **one ordered roster** from the typed results, and the three lists become
one-line derivations. Participation is now a per-row boolean set in one place; the
"added to one list, forgot another" bug is structurally impossible.

Because the roster *holds results produced in the existing build order* (it never drives the
build), the global counters never move — output is **byte-identical (0 diffs / 13,518 files)**.

## Prevention

- **A registry that holds generic behavior forces erasure; a registry that holds *results* doesn't.**
  Before reaching for an existential/interface registry, ask whether you can project the typed
  outputs into a homogeneous row instead. Usually you can, and it stays composition-clean.
- **Order matters: project *after* the typed work, never drive it.** If a registry becomes the build
  loop, it reorders side effects (global counters, file-write order) and silently breaks any
  byte-identity contract. Keep it a read-model over results.
- **Right-size to the genuine scatter.** Only the homogeneous lists (same shape, differ by
  membership) belong in the table. Things that *look* unifiable but aren't — editorial nav grouping,
  a list that merges two types under one key — stay explicit; folding them in is
  [[pattern-false-unification]]. Document the exclusions so they read as decisions, not omissions.
- **Let the closed DU be the row key.** `serialize Identity` ([[pattern-closed-du-identity-vs-wire-boundary]])
  gives the wire string for free and keeps the single identity authority intact.
