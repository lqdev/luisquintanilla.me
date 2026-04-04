---
title: "Project Report: AI Memex Knowledge Graph"
description: "Transformed isolated AI Memex entries into an interconnected knowledge base with build-time graph construction, JSON-LD structured data, backlinks, related entries, and D3.js visualization."
entry_type: project-report
published_date: "2026-04-04 10:16 -05:00"
last_updated_date: "2026-04-04 10:16 -05:00"
tags: ai-memex, architecture, knowledge-graph, fsharp, lqdev-me, json-ld, indieweb, semantic-web
related_skill: write-ai-memex
source_project: lqdev-me
related_entries: project-report-ai-memex-companion-system, codebase-context, pattern-generic-builder-content-processor, blog-building-my-own-memory
---

## Objective

Transform the AI Memex from 25 isolated documents into an interconnected knowledge base — inspired by Obsidian, org-roam, and Zettelkasten — that serves both human readers and AI consumers.

## The Problem

The AI Memex had rich content across five entry types (patterns, research, references, project reports, blog posts) but **zero explicit connections between them**. Tags provided the only implicit link. No backlinks, no "related entries," no graph visualization, no structured data for AI crawlers. Each entry felt like a standalone page rather than a node in a knowledge network.

## Research Phase

We investigated four knowledge management paradigms:

- **Zettelkasten / org-roam**: Atomic notes with bidirectional links forming emergent structure
- **Obsidian / Quartz**: Wikilink syntax, backlink panels, local graph visualization
- **Digital Gardens**: Progressive disclosure, evergreen content, connection-dense navigation
- **Semantic Web / JSON-LD**: Machine-readable structured data for AI discoverability

Key finding: pages with comprehensive JSON-LD structured data are ~33% more likely to be cited in AI-generated answers. Schema.org's `relatedLink` property enables bidirectional connections that AI crawlers (GPTBot, ClaudeBot) understand natively.

## Architecture Decisions

### Build-Time Graph Construction

All intelligence is computed during `dotnet run` and serialized as static JSON. No backend, no databases, no API calls at runtime. This is a static site — the graph is built once and baked into the output.

### Five-Layer Edge Discovery

Connections between entries are discovered through five independent layers, each producing weighted edges:

1. **Explicit links** (weight 1.0): `related_entries` frontmatter field — human-curated connections
2. **Wikilinks** (weight 0.9): `[[slug]]` references within content body — contextual references
3. **Tag overlap** (weight 0.3–0.7): Jaccard similarity coefficient — entries sharing tags above threshold
4. **Same project** (weight 0.2): Entries from the same `source_project` — project clustering
5. **Same type + shared tag** (weight 0.1): Entries of the same `entry_type` with overlapping tags

Edges are deduplicated per node pair, keeping only the highest-weight edge. This prevents noise while preserving the strongest signal.

### Backlinks vs Related Entries

Two distinct relationship views serve different purposes:

- **Backlinks** ("Linked From"): Only explicit and wikilink edges — entries that intentionally reference this one. High signal, curated.
- **Related Entries**: Top 5 by combined weight across all edge types — algorithmic discovery. Includes reasons ("shared tags: fsharp, patterns" or "same project: copilot-sdk-elisp").

### JSON-LD Type Mapping

Each entry type maps to a Schema.org class:

| Entry Type | Schema.org Type |
|---|---|
| pattern | TechArticle |
| research | ScholarlyArticle |
| reference | TechArticle |
| project-report | Article |
| blog-post | BlogPosting |

The AI author is modeled as `SoftwareApplication` (GitHub Copilot), with the human curator as `publisher` (Person). The collection page uses `CollectionPage` with `ItemList` elements grouped by entry type.

### Skill Propagation

The `write-ai-memex` skill is the single source of truth for entry schema. One skill update propagates `related_entries` and wikilink guidance to every project where an AI agent writes Memex entries. Old entries gracefully degrade — `IgnoreUnmatchedProperties()` in YAML deserialization means missing fields default to null.

## What Was Built

### KnowledgeGraph.fs (~400 lines)

A new F# module containing the complete graph engine:

- **Types**: `GraphNode`, `GraphEdge`, `EdgeType`, `BacklinkData`, `RelatedEntryData`, `KnowledgeGraph`
- **Edge extraction**: Five independent functions, each returning weighted edges
- **Graph construction**: `buildGraph` orchestrates all layers, deduplicates, computes backlinks and related entries
- **JSON-LD generation**: Per-entry structured data and CollectionPage for the index
- **graph.json serialization**: Complete graph with nodes, edges, clusters, stats — consumable by AI tools
- **Wikilink resolution**: `[[slug]]` → HTML links with broken-link detection

### View Integration

- **Related Entries section**: Shown on every entry page with entry type icons and human-readable reasons
- **Backlinks section**: Shows entries that explicitly link to the current entry
- **JSON-LD script block**: Embedded in every entry page and the collection index
- **Knowledge Graph button**: Toggles D3.js force-directed graph visualization on the index page

### D3.js Graph Visualization

Interactive force-directed layout loaded on demand (D3 v7 from CDN):

- Nodes colored by entry type (Desert Twilight palette)
- Node size scaled by connection count
- Click to navigate, hover for tooltip, drag to rearrange
- Boundary constraints keep nodes visible

### graph.json Output

Machine-readable knowledge graph at `/resources/ai-memex/graph.json`:

- 25 nodes with metadata (title, type, tags, URL, description, connection count)
- 195 edges with type, weight, and human-readable reason
- Clusters grouped by source project
- Generation stats (node count, edge count, average connections)

## Results

| Metric | Before | After |
|---|---|---|
| Explicit connections | 0 | 16+ (seeded entries) |
| Discoverable edges | 0 | 195 |
| Average connections per entry | 0 | 15.6 |
| JSON-LD pages | 0 | 26 (25 entries + 1 collection) |
| Machine-readable graph | None | graph.json (45KB) |
| Backlinks shown | 0 | Per-entry, explicit links only |
| Related entries shown | 0 | Top 5 per entry with reasons |

## Key Learnings

1. **Build-time is powerful**: Computing a full knowledge graph for 25 entries takes negligible time. The approach scales well to hundreds of entries.

2. **Edge deduplication matters**: Without it, the same pair generates edges from multiple layers (tag overlap AND same project AND same type). Keeping only the strongest signal per pair dramatically improves readability.

3. **Backlinks need filtering**: Initially, backlinks included all edge types bidirectionally — producing 15+ backlinks per entry. Restricting to explicit and wikilink edges only produces focused, meaningful backlinks.

4. **F# compile order is strict**: `KnowledgeGraph.fs` must appear before `Views/` in the `.fsproj` because ViewEngine templates reference `KnowledgeGraph.BacklinkData` and `KnowledgeGraph.RelatedEntryData` types.

5. **Skills as schema authority**: Updating three skill files (`write-ai-memex`, `import-ai-memex`, `query-ai-memex`) propagates the knowledge graph schema to every project. One change, universal effect.

6. **JSON-LD is the AI-web bridge**: Structured data isn't just SEO — it's how AI crawlers understand content relationships. `relatedLink` and `isPartOf` create a machine-readable web of connections.
