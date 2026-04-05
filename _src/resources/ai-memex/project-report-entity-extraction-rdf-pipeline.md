---
title: "Project Report: Entity Extraction & RDF Pipeline"
description: "Added LLM-powered entity extraction using Microsoft.Extensions.AI and RDF serialization via dotNetRDF, augmenting the AI Memex knowledge graph with typed entities, Wikidata links, and W3C-standard Turtle + JSON-LD output."
entry_type: project-report
published_date: "2026-04-04 20:10 -05:00"
last_updated_date: "2026-04-04 20:10 -05:00"
tags: ai-memex, knowledge-graph, fsharp, semantic-web, rdf, entity-extraction, microsoft-extensions-ai, dotnetrdf, lqdev-me, llm
related_skill: write-ai-memex
source_project: lqdev-me
related_entries: project-report-memex-knowledge-graph, codebase-context, my-tech-stack, pattern-custom-block-infrastructure
---

## Objective

Augment the existing AI Memex knowledge graph with **LLM-extracted entities** and **W3C-standard RDF output** — bridging the gap between human-readable markdown entries and machine-queryable linked data. Inspired by the [[https://github.com/lqdev/markdown-ld-kb|markdown-ld-kb]] project (Markdown → LLM extraction → RDF → SPARQL), but reimagined as a native F# pipeline integrated directly into the static site generator.

## The Problem

The [[project-report-memex-knowledge-graph|existing knowledge graph]] excels at discovering relationships between entries via five structural layers (explicit links, wikilinks, tag overlap, same-project, same-type-tag). But it has two blind spots:

1. **Entity blindness**: When an entry discusses "Giraffe ViewEngine," "dotNetRDF," or "Luis Quintanilla," those entities exist only as unstructured prose. The graph connects entries to entries, never entries to the things they discuss.

2. **No RDF output**: The graph serializes to proprietary JSON (`graph.json`). There's no W3C-standard representation that external tools, SPARQL engines, or semantic web crawlers can consume.

Research from Princeton and Georgia Tech suggests pages with comprehensive structured data see ~33-40% higher citation rates in AI-generated answers. Extracting typed entities with Wikidata `sameAs` links directly addresses this.

## Research Phase

### LLM Provider: Microsoft.Extensions.AI

We evaluated three approaches for LLM integration:

| Approach | Verdict |
|---|---|
| Raw OpenAI SDK | Too coupled — can't swap providers |
| Semantic Kernel | Overkill for single-task extraction |
| **Microsoft.Extensions.AI (MEAI)** | ✅ Provider-agnostic `IChatClient`, structured output, minimal surface area |

MEAI 10.4.1 (GA, March 2026) provides `IChatClient` — a clean abstraction over any LLM. The bridge pattern is elegant:

```fsharp
OpenAIClient(credential, options)      // concrete provider
  .GetChatClient("openai/gpt-4o-mini") // model selection
  .AsIChatClient()                     // → IChatClient abstraction
```

All extraction code depends only on `IChatClient`, never touches OpenAI directly. Swapping to Azure OpenAI, Anthropic, or local models requires changing one line.

### RDF Serializer: dotNetRDF

dotNetRDF 3.5.0 (active as of Feb 2026) provides everything needed: `CompressingTurtleWriter` for Turtle, `JsonLdWriter` for JSON-LD 1.1, namespace management, and `IGraph`/`ITripleStore` abstractions.

### Why F# Instead of Python

The original markdown-ld-kb uses Python (Markdown-it, OpenAI SDK, rdflib). Porting to F# is strictly better for this project:

- **Single build system**: `dotnet build` compiles everything — no Python venv, no polyglot CI
- **Type safety**: Extraction results are typed records, not dicts
- **Integrated pipeline**: Entity extraction runs inside the same process that builds pages, feeds, and the knowledge graph
- **Graceful degradation**: No `GITHUB_TOKEN`? Empty results, site still builds

## Architecture

### EntityExtraction.fs (~215 lines)

The extraction pipeline follows a cache-first strategy:

1. **Check cache**: `graph/cache/{slug}.v1.json` — if present and valid, skip LLM
2. **Call LLM**: Send entry content + system prompt to `IChatClient` with structured JSON output
3. **Parse response**: Deserialize into `ExtractionResult` (entities + assertions with confidence scores)
4. **Write cache**: Save result for future builds

Key types:

```fsharp
type ExtractedEntity = { Id: string; EntityType: string; Label: string; SameAs: string array }
type Assertion = { Subject: string; Predicate: string; Object: string; Confidence: float }
type ExtractionResult = { Entities: ExtractedEntity[]; Assertions: Assertion[] }
```

The system prompt (`ontology/extract_entities_v1.txt`) is tuned for AI Memex content: it understands schema.org types, expects Wikidata `sameAs` URIs, requests confidence scores, and includes two few-shot examples using lqdev.me-specific entities.

### KnowledgeGraph.fs — 6th Edge Layer

The existing five-layer edge discovery stays untouched. Entity mentions become a sixth layer:

| Layer | Weight | Source |
|---|---|---|
| Explicit `related_entries` | 1.0 | Human-curated frontmatter |
| Wikilinks `[[slug]]` | 0.9 | In-content references |
| Tag overlap (Jaccard) | 0.3–0.7 | Shared tags above threshold |
| Same project | 0.2 | `source_project` field |
| Same type + shared tag | 0.1 | Type + tag intersection |
| **Entity mentions** | **~0.8** | **LLM-extracted entities** |

New types added: `EntityNode` (with `MentionedIn` tracking across entries) and `EntityMention` edge variant. The `buildGraph` function now accepts an `extractions: Map<string, ExtractionResult>` parameter.

### RdfSerializer.fs (~105 lines)

Converts the augmented knowledge graph to two W3C-standard formats:

- **`graph.ttl`** (Turtle): Human-readable, namespace-compressed RDF
- **`graph.jsonld`** (JSON-LD 1.1): Machine-readable linked data

Each entry becomes a typed resource (`schema:TechArticle`, `schema:ScholarlyArticle`, `schema:BlogPosting`) with `schema:keywords`, `schema:description`, `kb:entryType`, `kb:sourceProject`, and `kb:relatedTo` edges. Extracted entities get `schema:sameAs` links and `schema:mentions` relationships from their parent entries.

Custom vocabulary at `https://www.lqdev.me/vocab/kb#` extends schema.org with domain-specific properties (`entryType`, `sourceProject`, `relatedTo`, `confidence`).

### Entry Page UI

Each AI Memex entry page now shows an "Entities Mentioned" section with styled chips — each chip typed by icon (code, person, org, globe, braces, document) and optionally linked to its Wikidata/external URI.

## F# + dotNetRDF Gotchas

Several non-obvious issues emerged during implementation:

1. **`Assert()` returns `bool`**: In C# this is silently discarded. In F# it's a type error inside `if` blocks. Solution: `inline` helper with `|> ignore`.

2. **Interface covariance**: `IUriNode` and `ILiteralNode` both implement `INode`, but F# doesn't auto-upcast in partial application. A helper function inferred `IUriNode` from its first callsite, then rejected `ILiteralNode` at the next. Solution: explicit `INode` parameter types + `inline`.

3. **`JsonLdWriter` expects `ITripleStore`**: Unlike `CompressingTurtleWriter` which accepts `IGraph`, the JSON-LD writer requires a triple store wrapper: `let store = new TripleStore(); store.Add(rdf)`.

## Results

| Metric | Before | After |
|---|---|---|
| Edge discovery layers | 5 (structural) | 6 (+ entity mentions) |
| RDF triples output | 0 | **1,125** |
| Output formats | graph.json only | graph.json + graph.ttl + graph.jsonld |
| Entity types tracked | 0 | schema.org typed entities with Wikidata links |
| LLM provider coupling | N/A | Zero — `IChatClient` abstraction |
| Build impact (no token) | N/A | ~0s (graceful skip) |
| Build impact (cached) | N/A | ~0s (file-based cache) |
| Build impact (first run) | N/A | ~90s (45 entries × ~2s each) |

### Output File Sizes

| File | Size | Content |
|---|---|---|
| `graph.json` | ~45KB | Full graph with entity stats |
| `graph.ttl` | ~170KB | W3C Turtle with namespace compression |
| `graph.jsonld` | ~129KB | JSON-LD 1.1 linked data |

## Lessons Learned

1. **MEAI is the right abstraction level**: `IChatClient` is exactly what's needed — not the full Semantic Kernel orchestrator, not raw HTTP calls. One interface, structured output, provider swappable. The `AsIChatClient()` bridge pattern is elegant.

2. **Cache-first beats rate limits**: GitHub Models free tier allows 150 requests/day. With per-entry file caching, only new/changed entries trigger LLM calls. A 45-entry corpus that would take 90 seconds on first run takes 0 seconds on subsequent builds.

3. **Graceful degradation is non-negotiable**: The site must build without `GITHUB_TOKEN`. Entity extraction returns empty results, the knowledge graph omits entity edges, RDF still serializes structural data. Zero special-casing in the build pipeline.

4. **dotNetRDF needs F# ergonomics work**: The library is excellent but designed for C#. Every `Assert()` call, every store wrapper, every type cast needs F# adaptation. The `inline` helper pattern should be reused in any future RDF work.

5. **RDF from a static site is powerful**: 1,125 triples from 45 markdown files — with proper namespaces, typed resources, and Wikidata links — is a legitimate linked data endpoint. No SPARQL server needed; the Turtle file is directly consumable by any RDF tool.

6. **F# beats Python for integrated pipelines**: The original markdown-ld-kb needed a separate Python environment, virtual env management, and CI coordination. The F# version runs inside the same `dotnet run` that builds the entire site. One process, one language, one build system.
