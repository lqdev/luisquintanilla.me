---
title: "Pattern: Inject Cross-Cutting Structured Data at the Generic Build-Driver Seam"
description: "To add a schema.org node (CollectionPage/Blog + BreadcrumbList) to every content-type landing page, wrap the index render once in the generic build driver — which already knows the output path, title, and type — instead of editing N per-type index views. Put per-item breadcrumbs in the type-specific views where clean titles already exist, avoiding fragile title-extraction heuristics."
entry_type: pattern
published_date: "2026-07-16 13:05 -05:00"
last_updated_date: "2026-07-16 13:05 -05:00"
tags: fsharp, dotnet, static-site-generation, jsonld, schemaorg, seo, structured-data, breadcrumbs, architecture, dry
related_entries: pattern-jsonld-shared-identity-graph, pattern-jsonld-merge-legacy-nodes-into-graph, pattern-content-type-landing-page
related_skill: write-ai-memex
source_project: lqdev-me
---

## Discovery

After a shared identity `@graph` and per-item content nodes were in place (see
[[pattern-jsonld-shared-identity-graph]]), the remaining gaps were **list pages**
(a `Blog`/`CollectionPage` node) and **navigation** (`BreadcrumbList`). The naive
plan was to edit each of ~11 content-type index views plus every item view. That is
a lot of near-identical edits and a lot of drift risk. The build already had a
generic driver — `BuildDriver.buildContentType` — that renders every type's item
pages and index page from one declarative config. That seam is the right place.

## The Pattern

**1. List-page node: inject once in the generic driver.** `buildContentType` already
has everything a `CollectionPage`/`Blog` node needs — the output-dir segments (→ URL),
the index title, and the type identity. Wrap the index content there:

```fsharp
let indexUrl = "/" + (joinPath cfg.OutputDir).Replace("\\", "/") + "/"
let sectionTitle = index.Title.Split([| '|'; '-' |]).[0].Trim()
let schemaType = if cfg.Name = ContentTypes.Posts then "Blog" else "CollectionPage"
let indexContent =
    div [] [
        index.View indexItems
        script [ _type "application/ld+json" ]
            [ rawText (StructuredData.listPageJson schemaType indexUrl sectionTitle) ]
    ]
```

One edit → **every** landing page (posts, notes, responses, snippets, wiki,
presentations, media, marketplace, albums, playlists) gets a list node + breadcrumb,
linked to the shared `#website`/`#person`. Deriving `indexUrl` from `cfg.OutputDir`
(not a hardcoded map) guarantees it matches exactly where the file is written.

**2. Semantic special-casing by identity, not by string URL.** The posts index should
be a `Blog` (the canonical container for `BlogPosting`), everything else a
`CollectionPage`. Branch on `cfg.Name = ContentTypes.Posts` — the type literal already
in the config — rather than sniffing the URL. Adding a new type needs no driver change.

**3. Per-item breadcrumbs go in the type-specific views, not the driver.** A
`BreadcrumbList`'s last crumb is the page title. The generic driver only has
`cfg.ItemTitle` (which bakes in the site suffix, e.g. `"Snippet | X | Luis Quintanilla"`),
so extracting a clean title there is heuristic and brittle. But each type-specific view
(`blogPostView`, `snippetPageView`, …) *already* has the clean `title` and computes the
page URL for its content node. Emit the breadcrumb there, in the **same** page-level
`@graph` as the content node:

```fsharp
// StructuredData.contentPageJson bundles content node + Home -> Section -> Page crumb
rawText (StructuredData.contentPageJson "SoftwareSourceCode"
            "Snippets" "/resources/snippets/" snippetUrl title isoDate "" tags [])
```

The section name/URL are hardcoded *inside each view* — correct and heuristic-free,
because the view is type-specific (a snippet view always sits under `/resources/snippets/`).

## Why This Works

- **DRY at the correct layer.** Cross-cutting concerns (list nodes) land where the loop
  already is; type-specific data (clean titles) lands where that data already is.
- **New content types inherit it for free** — the driver injection is generic.
- Result: 3,339 pages gained a `BreadcrumbList`, all landing pages gained a list node,
  with ~1 driver edit + 6 one-line view swaps. Full-site scan: 6,956 ld+json scripts,
  0 parse errors.

## Gotchas

- Pages **not** built through the generic driver (reviews, the bookmarks/rsvp landing
  pages, `/about`, tags) need their own injection — do them at their own builders
  (e.g. `TagPagesBuilder` wraps its views in a `div` + ld+json `script`; `/about` emits
  a `ProfilePage`). The driver only covers driver-built types.
- Requires `open Giraffe.ViewEngine` in any builder module you add a `script` node to
  (`TagPages.fs`, `StaticPages.fs` did not have it).
- Compile order: `StructuredData.fs` and `ContentTypes.fs` must precede `BuildDriver.fs`
  in the `.fsproj` (they do — both sit just after `Constants.fs`).

See also [[pattern-jsonld-merge-legacy-nodes-into-graph]] for retrofitting the
content types that already had their own (isolated) JSON-LD.
