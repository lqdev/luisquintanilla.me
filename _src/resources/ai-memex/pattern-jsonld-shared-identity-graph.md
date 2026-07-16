---
title: "Pattern: Site-Wide JSON-LD via a Shared @graph with Stable @id Nodes"
description: "Emit a schema.org identity graph (WebSite + Person) once in the shared layout head with stable #website / #person @ids, then have every per-content-type node reference those ids — so crawlers and LLMs merge one coherent identity across the whole static site."
entry_type: pattern
published_date: "2026-07-16 11:58 -05:00"
last_updated_date: "2026-07-16 11:58 -05:00"
tags: fsharp, dotnet, static-site-generation, jsonld, schemaorg, seo, structured-data, indieweb, knowledge-graph
related_entries: pattern-single-source-config-generate-vs-verify, pattern-hidden-indieweb-microformats, pattern-entity-signal-not-content-slurping, pattern-jsonld-central-injection-build-driver, pattern-jsonld-merge-legacy-nodes-into-graph
related_skill: write-ai-memex
source_project: lqdev-me
---

## Discovery

The site had rich JSON-LD only on two niche content types (AI Memex → `BlogPosting`,
Marketplace → `Product`). Every other page — home, `/about`, 95 blog posts, notes,
responses, snippets, wiki, presentations — had **zero structured data**, and there
was **no site-wide identity graph at all**. Following Ethan Hawksley's
"[JSON-LD explained for personal websites](https://hawksley.dev/blog/json-ld-explained-for-personal-websites/)",
the highest-leverage fix isn't per-page nodes — it's a single shared `@graph`.

## The Pattern

**1. One identity graph, emitted in the shared layout head.** A `WebSite` node and a
`Person` node with *stable, canonical* `@id`s:

```
https://lqdev.me/#website   (WebSite,  publisher -> #person, image, alternateName)
https://lqdev.me/#person    (Person,   name, jobTitle, image, sameAs[...])
```

Both are built from the single-source `Constants` module (name, bio, avatar URL,
canonical origin) and serialized with `System.Text.Json` (`JsonObject`/`JsonArray`)
— never `sprintf`, so escaping is correct. Injected once in the private `layoutCore`
head (and the separate `presentationLayout` head) → appears on **every indexed page**
with no per-view changes. The `noindex` text-only site is deliberately excluded.

**2. Per-content-type nodes reference the shared ids.** Each individual page view
emits a second `<script type="application/ld+json">` in the article body whose
`author`, `publisher`, and `isPartOf` are slim `{"@id": ".../#person"}` /
`{"@id": ".../#website"}` references — not repeated full nodes. A single generic
`contentNodeJson schemaType pageUrl title datePublished dateModified tags extra`
helper produces all of them; thin wrappers pick the type:

| Content type | schema.org type |
|---|---|
| posts | `BlogPosting` |
| notes | `SocialMediaPosting` |
| responses (reply/like/repost/bookmark) | `SocialMediaPosting` + target link |
| snippets | `SoftwareSourceCode` |
| wiki | `Article` |
| presentations | `PresentationDigitalDocument` |

**3. IndieWeb responses get a semantic target link** chosen by response type:
`reply`/`rsvp` → `inReplyTo`, `reshare`/`share` → `sharedContent`, else (`star`,
`bookmark`) → `citation`, each a `{"@type":"WebPage","url": targetUrl}`.

## Why This Works

- Crawlers/search **merge** nodes that share an `@id` across pages, building one
  entity for "the site" and "the person" instead of fragmented per-page identities.
  Single-page scrapers (LLMs) still get enough context on any one page.
- Slim `@id` references keep every page's payload small (identity graph ≈ 1.1 KB).
- It stays **consistent with existing microformats2** (`h-card`/`h-entry`): same
  canonical URL, same author, same dates — redundant but non-conflicting signals.

## Gotchas

- **Two `presentationPageView`s existed** (`LayoutViews` and `ContentViews`); the
  build driver calls the `LayoutViews` one. Editing the other produced no output —
  verify which view the builder actually wires (`ContentTypePages.fs` `ItemView`).
- **`null` tag arrays crash `Seq.filter`** (`ArgumentNullException: source`). Guard
  with `if isNull (box tags) then [] else ...`. The build surfaced this by failing
  loudly mid-generation — a good reason to always run `dotnet run`, not just build.
- Put the identity graph *after* `<title>` in the head; keep per-item nodes in the
  body next to their `h-entry` so authorship stays co-located.

See also [[pattern-entity-signal-not-content-slurping]] for the crawler-policy side
(you can't selectively serve JSON-LD, so control ingestion via robots.txt).
