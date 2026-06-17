---
title: "Pattern: God-Module Decomposition at Scale — Slicing Contamination & the Composition-Wiring Boundary"
description: "Two lessons from finishing the lqdev.me god-module split: batch line-range slicing silently contaminates output when top-level types are interleaved between nested modules, and 'make it pure glue' has a hard floor at composition wiring."
entry_type: pattern
published_date: "2026-06-16 19:05 -05:00"
last_updated_date: "2026-06-16 19:05 -05:00"
tags: fsharp, dotnet, refactoring, modules, architecture, compile-order, gotcha, patterns
related_entries: pattern-fsharp-module-extraction-inference-flip, pattern-content-type-taxonomy-mismatch, pattern-advice-based-module-wiring, pattern-long-lived-umbrella-branch-merge-strategy
related_skill: write-ai-memex
source_project: lqdev-me
---

## Context

This finishes assessment finding **F9 ("god modules")**, the unfinished half left after the
2026 refactor's step 2.5 (which already lifted `UnifiedFeeds` and `TimelineViews` — see
[[pattern-fsharp-module-extraction-inference-flip]]). The remaining surface was large:

- `Builder.fs` (1,489 lines, ~14 unrelated concerns) → decomposed into 13 cohesive
  `Builders/*.fs` modules, then **deleted entirely** (no re-export facade).
- `GenericBuilder.fs` (1,531 lines) → reduced to a **152-line core** (the `ContentProcessor<'T>` /
  `FeedData<'T>` abstraction, `buildContentWithFeeds`, `ContentPipeline`); its 13 per-type
  processors + 3 data extractors lifted to 15 `Processors/*.fs` modules.
- `TextOnlyViews.fs` (1,094) → `Views/TextOnly/Content.fs` (processor + helpers) + `Pages.fs`.
- `Program.fs` confirmed already a composition root; only the inline dir-prep block moved out.

Same safety model as before: **byte-identical `_public/` per move** (SHA-256 hash-manifest diff,
`verify-baseline.ps1`), one concern per commit, URL permanence as a hard invariant. Two new lessons
surfaced that the earlier entry didn't cover.

## Gotcha — batch line-range slicing assumes contiguous modules; interleaved top-level types break it

To lift 14 processors in one pass I wrote a slicer: grep the column-0 `module X` declarations, then
for each, copy lines `[start+1 .. nextModuleStart-1]` into `Processors/X.fs` under a fresh header.
The assumption baked into `nextModuleStart-1` is **"module N's body ends where module N+1 begins."**

That assumption was false. Three **top-level `type` records were interleaved between processor
modules**, deliberately placed at module level so a downstream file could consume them unqualified:

```fsharp
module ReviewDataExtractor = ...        // ends ~line 518

/// Defined at module level so it can be used by both BookProcessor and UnifiedFeeds
type ReviewMetadata = { ... }           // <-- top-level, BETWEEN two modules
type MediaAPData    = { ... }

module MediaExtractor = ...             // next module
```

`UnifiedFeeds.fs` references `ReviewMetadata` / `MediaAPData` / `AlbumMediaData` **bare** (via
`open GenericBuilder`). A naive `[moduleStart .. nextModuleStart-1]` slice swept each trailing type
into the *preceding* processor's new file — duplicating a type that must be singular. Had it shipped,
`UnifiedFeeds` would have type-mismatched (`GenericBuilder.ReviewMetadata` vs the stray
`ReviewDataExtractor.ReviewMetadata`), structurally identical but nominally distinct.

### The tell

Two generated files ended with a **column-0 `}`**. In a promoted module whose body is uniformly
4-space-indented (`module X` with no `=`, body pasted verbatim — see the sibling entry), a closing
token at **column 0 is impossible for real body content**. That single anomaly — "the last line of
this slice is dedented to the margin" — was the signal that the slice had captured a top-level
declaration that didn't belong to the module.

## Root Cause

A god module isn't a clean stack of modules; it accretes **shared top-level types wedged next to
their first consumer**. Tooling that keys off `module`/`let` boundaries alone is blind to interleaved
`type` declarations. The real structure only shows up when you enumerate **every** column-0
`^(type|module|let)` line, not just the modules.

## Solution

1. **Get the true skeleton first.** Before slicing, grep all top-level declarations
   (`^(type|module|let|open)`), not just `module`. Treat the file as a flat sequence of top-level
   members, some of which are types that may sit between modules.
2. **Shared types stay in the core.** A type consumed *unqualified* downstream belongs where those
   consumers resolve it. Here the 3 records were **relocated up into the `GenericBuilder` core block**
   (right after `buildContentWithFeeds`), making the processor region a single contiguous,
   safely-deletable range — and keeping `open GenericBuilder` consumers working untouched.
3. **Reconstruct from explicit part-ranges, not fragmented deletions.** Rather than deleting around
   the interleaved types, rebuild the core file as `core[1..96] + relocatedTypes + ContentPipeline`.
   Cleaner result, one write, no fiddly multi-range masks.
4. **Verify byte-identity after the batch.** Type-declaration order is compile-time only, so
   relocating records is invisible to `_public/` — `verify-baseline.ps1` confirmed 0 diffs.

## The other lesson — "make it pure glue" has a floor at composition wiring

The user's thesis was "Builder and Program should only be glue." Half right: `Builder.fs` *was* a
god module (now gone), but `Program.fs` was **already a composition root** — and the instinct to
"make Program pure glue" must **stop at composition wiring**.

`Program.main` holds a 12-row `contentRoster` table and an `allTaggableContent` list that reference
the just-built typed feeds. Extracting them into a `buildRoster(...)` would need a 12-parameter
function — *worse*, not cleaner. And the codebase's own `ContentRegistry.fs` already documents this:

> Deliberately NOT modelled here (would be false unification)… `allTaggableContent`… stays explicit
> in Program.fs.

So the discipline is: **extract implementation, leave composition.** The genuinely misplaced thing
in `main` was the directory-prep block (filesystem side-effects) → lifted to
`AssetsBuilder.prepareDirectories`. The wiring tables — the declarative statement of *which content
types join which feeds* — are the composition root's job and stay visible there. Pulling them into a
module is the same **false-unification / B2 anti-pattern** the assessment warns against (cf.
[[pattern-content-type-taxonomy-mismatch]] for why the roster exists as data, not duplicated lists).

## Prevention

- **Never batch-slice nested modules on the assumption that they're contiguous.** Enumerate the real
  top-level skeleton (`type` + `module` + `let`) first; interleaved types are common in god modules.
- **A column-0 closing token at the end of an indented-body slice = contamination.** Cheap canary;
  diff the last line's indentation against the body's.
- **A shared type lives where its unqualified consumers resolve it** (the core), not travelling with
  whichever processor happens to precede it.
- **"Make X pure glue" is bounded by composition.** Extract side-effecting implementation; keep the
  wiring/registry tables in the composition root. If extraction would produce a many-parameter
  function fed entirely by locals, that's the signal you're about to over-engineer the root.
- **Tooling footnote (PowerShell):** mask-based `.NET` line deletion with a *single* range must use
  `@(,@(S,E))` — `@(@(S,E))` is flattened and silently no-ops. Multi-range `@(@(a,b),@(c,d))` is fine.

## Outcome

F9 closed. `Builder.fs` 1,489 → 0 (deleted; 13 `Builders/*.fs`). `GenericBuilder.fs` 1,531 → 152
core + 15 `Processors/*.fs` (42–218 lines each). `TextOnlyViews.fs` 1,094 → 122 + 983.
Every one of the ~30 resulting files landed via a **byte-identical, hash-verified** move; the only
real thinking beyond the mechanical paste was (a) catching the slicing contamination and (b) drawing
the implementation-vs-composition line in `Program.fs`.
