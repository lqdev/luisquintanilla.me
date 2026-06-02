---
title: "Pattern: Content-Type Taxonomy Mismatch Between Producer and Consumer"
description: "When a derived view assumes a coarser content-type taxonomy than the unified feed actually emits, landing pages go empty, links break, and individual pages orphan — diagnosable by comparing generated output, not source."
entry_type: pattern
published_date: "2026-06-01 15:40 -05:00"
last_updated_date: "2026-06-01 15:40 -05:00"
tags: "fsharp, web, architecture, accessibility, patterns, lqdev-me"
related_skill: "write-ai-memex"
source_project: "lqdev-me"
related_entries: pattern-content-type-landing-page, pattern-generic-builder-content-processor, pattern-text-only-accessibility, pattern-response-type-badge-specificity
---

## Discovery

The text-only accessibility site (`/text/`) was generating an **empty** `/text/content/responses/` landing page and a dead `/text/content/albums/` landing — both linked from the navigation. Roughly 887 individual response pages were orphaned (reachable by direct URL but not from any landing or nav), and 7 landing links from the all-content page pointed nowhere.

Critically, **nothing in the source markdown looked wrong**, and the F# compiled cleanly. The bug was only visible by inspecting the *generated* `_public/` tree and counting items per landing directory. Source-level review would never have surfaced it.

## Root Cause

A **producer/consumer taxonomy mismatch** in the content pipeline:

- **Producer** (`GenericBuilder.fs`, the unified-feed builder) assigns each response item its *subtype* as its `ContentType`:
  ```fsharp
  contentType = feedData.Content.Metadata.ResponseType   // "reply" | "reshare" | "star" | "rsvp"
  ```
  and uses newer fine-grained values like `"ai-memex"`, `"album-collection"`, `"playlist-collection"` for other types.
- **Consumer** (the text-only builder) had hard-coded a *stale, coarser* list it assumed the feed used: `posts, notes, responses, snippets, wiki, presentations, reviews, albums, bookmarks, media`.

So the consumer built a `responses` landing and filtered for items whose `ContentType = "responses"` — but **no item ever has that value** (they're `reply`/`reshare`/`star`/`rsvp`). The landing was empty. Meanwhile it built an `albums` landing for a value the producer had renamed to `album-collection`, and never built landings for `ai-memex`/`album-collection`/`playlist-collection` at all.

A second, related defect: the consumer re-processed items as markdown to generate HTML, but responses, bookmarks, and **reviews** store **pre-rendered `CardHtml`** in their `Content` field (not raw markdown). Double-processing mangled them. The `Content` field's meaning **varies by content type** — some store raw markdown, some store rendered HTML — and the consumer must branch accordingly.

## Solution

1. **Normalize at the consumer boundary.** Add a single helper that collapses producer subtypes into the consumer's coarser sections, and apply it *everywhere* a content type becomes a path, filter key, or grouping key:
   ```fsharp
   let normalizeContentType (contentType: string) =
       if System.String.IsNullOrWhiteSpace contentType then ""
       else
           match contentType.Trim().ToLowerInvariant() with
           | "reply" | "reshare" | "star" | "rsvp" -> "responses"
           | other -> other
   ```
   Use `Trim().ToLowerInvariant()` (not culture-sensitive `ToLower()`) so routing is stable across build-machine locales and tolerant of stray frontmatter whitespace.

2. **Align the generated content-type list** with what the producer actually emits: drop dead values (`albums`), add the real ones (`ai-memex`, `album-collection`, `playlist-collection`).

3. **Branch HTML generation on the `Content` field's true source.** For types that store pre-rendered `CardHtml` (responses, bookmarks, reviews), emit `content.Content` directly; only run `convertMdToHtml` for types that store raw markdown (posts, notes, media, snippets, wiki, ai-memex, album/playlist-collection).

4. **Apply the normalization at every URL-construction site**, not just the landing filter: homepage recent-item links, content-type page filter *and* title-match cases, nav list, individual-page back-links, all-content `groupBy`, tag-page links, and monthly-archive links. Missing any one re-introduces a broken or stale link.

After the fix: `/text/content/responses/` recovered **807** items (from 0), subtype directories disappeared, new landings (album-collection 2, playlist-collection 22, ai-memex 56) appeared, and a full scan of `_public/text/` found **zero** broken landing links and **zero** stale `reply`/`reshare`/`rsvp`/`star`/`albums` links.

## Prevention

- **Diagnose generator bugs from the generated artifact, not the source.** Empty landings, orphan pages, and broken links are invisible at the source/compile level; count items per output directory and crawl the emitted links. The mismatch was found empirically by comparing `_public/` output, not by reading `.fs` files.
- **Treat any hard-coded list of "valid content types" in a consumer as a coupling hazard.** When the producer's taxonomy can change independently (new response subtypes, renamed collections), the consumer must either derive its list from the actual feed data or be guarded by a test that asserts producer/consumer taxonomies agree.
- **Document the `Content` field's contract per type.** Because some content types store raw markdown and others store pre-rendered HTML in the same field, every consumer that renders `Content` needs an explicit per-type branch. A field whose meaning depends on a sibling discriminator is a recurring source of double-processing bugs.
- **Normalize content types to lowercase-invariant + trimmed at every routing boundary** to avoid locale-dependent path generation (the same environment-independence concern that motivated switching date parsing to `DateTimeOffset.Parse`).
- When a fix is a behavioral no-op for current data but hardens a latent class of bug, ship it but **say so explicitly** in the review thread, and **defer the broader sweep** (e.g. the remaining culture-sensitive `.ToLower()` routing calls) to a separate shovel-ready issue rather than expanding the PR's scope.
