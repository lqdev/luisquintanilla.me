---
title: "Pattern: Merge Legacy Isolated JSON-LD Into the Shared @id Graph"
description: "Content types that already emitted their own self-contained JSON-LD (full embedded author/publisher nodes, a legacy domain) stay invisible to the site's entity graph. Retrofit them by canonicalizing the @id domain and replacing embedded identity nodes with slim {\"@id\": \".../#person\"} references — safe under URL permanence because an @id is an identifier, not a navigable permalink."
entry_type: pattern
published_date: "2026-07-16 13:05 -05:00"
last_updated_date: "2026-07-16 13:05 -05:00"
tags: fsharp, dotnet, jsonld, schemaorg, structured-data, seo, knowledge-graph, url-permanence, refactoring
related_entries: pattern-jsonld-shared-identity-graph, pattern-jsonld-central-injection-build-driver, pattern-single-source-config-generate-vs-verify
related_skill: write-ai-memex
source_project: lqdev-me
---

## Discovery

Two content types shipped rich JSON-LD *before* the site had any shared identity graph:
AI Memex entries (`BlogPosting`/`Article` via `KnowledgeGraph.fs`) and Marketplace
listings (`Product`/`Offer` via `MarketplaceProcessor.fs`). Each was **self-contained**:
its own embedded `Person` publisher node and a hardcoded **legacy** domain
(`https://www.lqdev.me/...`). Once a shared graph existed at the canonical apex
(`https://lqdev.me/#person`, `.../#website`), these older nodes were *orphans* — a
crawler merging by `@id` saw two different "Luis Quintanilla" publishers on different
domains and never linked them to the site identity.

## The Pattern

**1. Canonicalize the `@id` domain.** Replace every `www.lqdev.me` (legacy) with the
canonical apex `lqdev.me` inside the JSON-LD templates — `@id`, `url`, `mainEntityOfPage`,
`relatedLink`, list-item URLs.

**2. Swap embedded identity nodes for slim `@id` references.** Turn full nodes into
pointers at the shared graph:

```diff
- "publisher":{"@type":"Person","name":"Luis Quintanilla","url":"https://www.lqdev.me"},
+ "publisher":{"@id":"https://lqdev.me/#person"},
```

For Marketplace, the `Offer.seller` became `{"@id":"https://lqdev.me/#person"}`; for
AI Memex, `isPartOf` became an **array** `[ {Collection ...}, {"@id":".../#website"} ]`
so the entry belongs to both its sub-collection and the site. Deliberately kept the AI
Memex `author` as the `GitHub Copilot` `SoftwareApplication` — those entries *are*
AI-authored; only the human `publisher` links to `#person`.

**3. The shared nodes must actually be on the page.** These references only resolve
because the `WebSite`+`Person` graph is injected in the shared layout head on every
indexed page (see [[pattern-jsonld-shared-identity-graph]]). Referencing an `@id` that
appears nowhere on the page is a dangling pointer.

## Why This Is Safe (URL Permanence)

Changing an `@id` from `www.lqdev.me` to `lqdev.me` looks like a URL change, but a
schema.org `@id` is an **identifier for a node**, not a fetched permalink. No published
page URL, feed GUID, or `<link>` moves — those legacy `www.lqdev.me` strings in RSS
feeds, `.bar` archives, and processor `url` fields were **left untouched on purpose**
(they *are* content permalinks / feed identity; changing them would break subscribers).
Only the JSON-LD identifier graph was canonicalized. This is the important distinction:
`@id` (identifier) is refactorable; a permalink is not.

## Validation

A full-site scan is the cheap proof the retrofit is complete and clean:

```
6,956 <script type="application/ld+json"> blocks across 3,533 pages
→ 0 JSON parse errors, 0 occurrences of "www.lqdev.me" inside any ld+json block
```

Grepping for the legacy domain *scoped to ld+json blocks* (not the whole HTML) catches
any template you missed without false-positiving on legitimate content links.

## Gotchas

- These are deep positional `sprintf` templates (`KnowledgeGraph.fs`). Editing the
  **string literals** inside the template (domain, `publisher` object) avoids reshuffling
  the positional `%s` argument list — far less error-prone than adding new args.
- Leave `author` semantics alone when they're intentionally non-human (AI-authored
  Memex entries). Merging identity ≠ flattening authorship.

See also [[pattern-jsonld-central-injection-build-driver]] for wiring the content types
that had *no* prior JSON-LD.
