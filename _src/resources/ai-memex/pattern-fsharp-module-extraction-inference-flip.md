---
title: "Pattern: F# Module Extraction — Promotion Without Reindent + the open-Path Inference Flip"
description: "Two linked gotchas when mechanically splitting a god module in F#: promoting a nested module to top-level without reindenting its body, and the silent record-field inference flip that a moved open path causes."
entry_type: pattern
published_date: "2026-06-11 10:12 -05:00"
last_updated_date: "2026-06-11 10:12 -05:00"
tags: fsharp, dotnet, refactoring, modules, type-inference, compile-order, gotcha, architecture
related_entries: pattern-content-type-taxonomy-mismatch, pattern-long-lived-umbrella-branch-merge-strategy, pattern-advice-based-module-wiring
related_skill: write-ai-memex
source_project: lqdev-me
---

## Context

Phase 2.5 of the lqdev.me architecture refactor (F9 — "god modules") called for two purely
mechanical module extractions, each required to leave `_public/` **byte-identical**:

- Lift the `UnifiedFeeds` inner module out of `GenericBuilder.fs` (2123 → 1476 lines) into its own
  top-level `UnifiedFeeds.fs`.
- Lift the timeline-view cluster out of `LayoutViews.fs` (1402 → 767 lines) into
  `Views/TimelineViews.fs`.

Both are "just move code." Both hid a trap that only surfaces because F# resolves names by
**compile order** and by **what is currently `open`**. Capturing them because module-per-concern
splits will keep happening as the codebase grows, and these two bite every time.

## Gotcha 1 — Promote a nested module to top-level *without reindenting the body*

The `UnifiedFeeds` body was ~645 lines living as a nested `module UnifiedFeeds =` inside
`GenericBuilder.fs`, so every line was indented 4 spaces under the `=`. The naive extraction is to
dedent all 645 lines — a huge, noisy, error-prone diff.

You don't have to. A **top-level** module declared *without* the `=` may keep an indented body:

```fsharp
module UnifiedFeeds        // no '=' — top-level module

    open Domain            // body stays indented 4 spaces, verbatim
    open GenericBuilder
    // ... 645 unchanged lines ...
```

This is exactly the form `Views/Layouts.fs` already used (`module Layouts` then `    open ...`).
So the extraction became: drop the `=`, add a blank line, prepend the `open` lines the inner
module had inherited from its parent, and **paste the body unchanged**. Zero reindentation, tiny
semantic diff, trivially byte-identical output.

Rule of thumb: `module Foo =` forces a dedented top-level or a nested block; **`module Foo` (no
`=`)** is a top-level module whose body may be indented. Use the latter to move big nested modules
cheaply.

## Gotcha 2 — A moved `open` path silently flips a record-field inference

After the move, the build failed with ~100 errors in a *different, untouched* file
(`TextOnlyViews.fs`), all of the form "this expression was expected to have type
`UnifiedFeedItem` but here has type `TravelCollectionItem`." The function was already annotated:

```fsharp
let textOnlyTagPage (tag: string) (content: UnifiedFeedItem list) = ...
```

The annotation uses the **bare** name `UnifiedFeedItem`. That bare name only resolved because the
file did `open GenericBuilder.UnifiedFeeds`. Once `UnifiedFeeds` became top-level, the path
`GenericBuilder.UnifiedFeeds` **no longer exists**, so the bare `UnifiedFeedItem` annotation went
unresolved — and F# fell back to inferring the type from field access (`item.Tags`,
`item.ContentType`, `item.Url`). Those field names also exist on `TravelCollectionItem`, which is
declared *later* in compile order, so inference picked **the most-recently-declared record that
has the fields** — the wrong one. One broken `open` cascaded into 100 errors in a file I never
edited.

### Why a find/replace missed it

I had bulk-replaced `GenericBuilder.UnifiedFeeds.` → `UnifiedFeeds.` (note the **trailing dot**),
which fixes qualified call sites like `GenericBuilder.UnifiedFeeds.buildAllFeeds`. But
`open GenericBuilder.UnifiedFeeds` has **no trailing dot**, so it slipped through. There were three
of them (`SearchIndex.fs`, `TextOnlyBuilder.fs`, `Views/TextOnlyViews.fs`).

## Solution

1. **Prefer module promotion over reindent**: declare the extracted module as top-level
   `module Name` (no `=`) and move the indented body verbatim.
2. **Fix every reference form, not just the dotted one.** After moving a module `Outer.Inner` →
   top-level `Inner`, search for *both*:
   - qualified uses: `Outer.Inner.` → `Inner.`
   - `open` statements: `open Outer.Inner` → `open Inner`
3. **Let the compiler find the stragglers** — a missed `open` shows up not as "unknown module"
   but as a confusing *type mismatch* far away. When a pure module move produces field/type
   errors in an unrelated file, suspect a broken `open` path before suspecting real logic damage.
4. **Mind compile order for the new file.** `UnifiedFeeds.fs` had to sit after `GenericBuilder.fs`
   (it `open GenericBuilder`) and before its consumers; `TimelineViews.fs` had to sit after the
   view modules it uses and before `LayoutViews.fs`. The `.fsproj` `<Compile Include>` order *is*
   the dependency contract.

## Prevention

- **Annotate derived bindings that carry the moved type.** Bare-name annotations are hostage to
  `open` paths; when two records share field names, an unresolved annotation degrades to
  declaration-order inference. Fully-qualified annotations (`UnifiedFeeds.UnifiedFeedItem list`)
  are immune to the flip.
- **When two record types share field names** (here `Tags`/`ContentType`/`Url` on both
  `UnifiedFeedItem` and `TravelCollectionItem`), treat unannotated field access as a latent bug —
  it resolves to the most-recently-declared type and will flip if compile order changes.
- **For mechanical module splits, verify byte-identity after *each* move** (SHA-256 hash-manifest
  diff of `_public/`). Both moves here came out 0-diff, confirming "no logic change" objectively
  rather than by eyeballing.

## Outcome

Both extractions landed byte-identical (0 diffs). `GenericBuilder.fs` 2123 → 1476;
`LayoutViews.fs` 1402 → 767 (under the ~800-line budget). The only real work beyond the paste was
re-pointing 6 consumer files plus the 3 stray `open` statements — the compiler enumerated exactly
which, once the first `open` was fixed.
