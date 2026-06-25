---
title: "Pattern: Standalone Content Type (In Search, Not In Firehose)"
description: "Adding a content type that is fully searchable and has its own feed, but is excluded from the timeline, public RSS firehose, tag-syndication feeds, and the ActivityPub outbox."
entry_type: pattern
published_date: "2026-06-24 15:05 -05:00"
last_updated_date: "2026-06-24 15:05 -05:00"
tags: fsharp, dotnet, architecture, patterns, lqdev-me
related_skill: add-content-type
source_project: lqdev-me
related_entries: pattern-content-type-landing-page, pattern-feed-architecture-consistency, pattern-content-type-taxonomy-mismatch, pattern-closed-du-identity-vs-wire-boundary, pattern-composite-action-shared-cdn-upload
---

## Discovery

While building a `marketplace` content type (a personal classifieds / "for sale"
section), the requirement was unusual: each listing must be **discoverable in
on-site search and have its own RSS feed**, but it must **not** be broadcast to the
main timeline, the public RSS firehose (`/feed/feed.xml`, `/all.rss`), the
tag-syndication feeds, or the **ActivityPub outbox** (so fediverse followers and RSS
subscribers aren't spammed with yard-sale items).

The existing **AI Memex** content type already had exactly this shape. The fastest,
safest implementation was to grep for every place `ContentTypes.AiMemex` is special-cased
and mirror each one for the new type.

## Root Cause / Problem

"Participation" in this generator is **not a single boolean**. It is split between a
data-driven roster and several *separate, explicit* exclusion filters scattered across
modules. The `contentRoster` flags (`InTimeline` / `InAllFeeds` / `InBlogArchive`) drive
the timeline / all-content / blog-archive lists, but three other surfaces are governed
by their own filters:

1. the **public RSS firehose** filter (`publicItems`)
2. the **tag-syndication** feed filter
3. the **ActivityPub** outbox content filter

Miss any one of them and the content silently leaks onto a surface you intended to keep
it off. The F# compiler only protects the *exhaustive `match`* sites (e.g. `toJsonFeedContent`)
via `FS0025`-as-error — it does **not** flag a missing `&& item.ContentType <> ...` in a
firehose filter. Those are by-convention, discovered only by grepping the precedent.

## Solution

Treat the existing standalone type (`AiMemex`) as the executable spec. For the new
`Marketplace` type, the changes were:

**Roster (set participation once):** in `Program.fs`, one row —

```fsharp
{ Identity = ContentTypes.ContentType.Marketplace
  Unified = marketplaceUnified
  InTimeline = false      // keep out of homepage timeline
  InAllFeeds = true       // drives search + text-only participation
  InBlogArchive = false }
```

**Three explicit exclusion sites (mirror AiMemex exactly):**

- `UnifiedFeeds.fs` firehose `publicItems` filter — add
  `&& item.ContentType <> ContentTypes.Marketplace` so listings stay out of
  `/feed/feed.xml` and `/all.rss`.
- `UnifiedFeeds.fs` tag-syndication feed filter — same exclusion, so per-tag RSS feeds
  don't carry listings.
- `Program.fs` ActivityPub content filter — exclude `Marketplace` exactly where
  `AiMemex` is already excluded.

**Own feed + search (the inclusions):** add a `Marketplace` entry to
`typeConfigurations` in `UnifiedFeeds.fs` (emits `marketplace/feed.xml`), a
`/marketplace.rss` alias in `LegacyFeeds.fs`, and `InAllFeeds = true` carries it into the
search index automatically.

**Compiler-enforced arms:** adding the DU case in `ContentTypes.fs` forces arms in the
exhaustive `serialize` / `urlPrefix` and in `UnifiedFeeds.toJsonFeedContent` (`FS0025`).
See [[pattern-closed-du-identity-vs-wire-boundary]].

## Key Components

- **`Program.fs`** — the `contentRoster` table (B1 content-type registry) and the
  ActivityPub content exclusion list.
- **`UnifiedFeeds.fs`** — `typeConfigurations` (own feed), the firehose `publicItems`
  filter, the tag-syndication filter, and the exhaustive `toJsonFeedContent` match.
- **`ContentTypes.fs`** — the closed DU + `serialize`/`parse`/`urlPrefix` arms.
- **`TextOnlyBuilder.fs`** — a **hardcoded** `contentTypes` list for per-type text-only
  *listing* pages (see Gotchas).

## Gotchas

- **Text-only listing pages come from a hardcoded list.** Individual text-only item
  pages generate dynamically (they worked immediately), but the per-type *listing* page
  (`/text/content/marketplace/`) is driven by a literal list in `TextOnlyBuilder.fs`. Omit
  the new type there and the "← All marketplace" backlink 404s. Add a display-name arm in
  `Views/TextOnly/Pages.fs` too.
- **The compiler is a partial safety net.** It catches the exhaustive matches but not the
  by-convention firehose / tag-feed / ActivityPub filters. Grep the precedent type's
  literal across the whole repo and mirror every hit.

## Prevention

When adding a "private-ish but searchable" content type, don't reason about feeds from
scratch — pick the nearest existing standalone type and
`grep -rn "ContentTypes.AiMemex"` (or the relevant literal). Every match is a decision
point you must consciously mirror or intentionally differ from. Pair this with the
[[pattern-composite-action-shared-cdn-upload]] pattern if the new type also needs
issue-driven photo publishing.
