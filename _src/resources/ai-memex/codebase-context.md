---
title: "lqdev.me Codebase Architecture"
description: A comprehensive reference to the F# static site generator architecture, modules, content types, and build pipeline.
entry_type: reference
published_date: "2026-07-22 00:00 -05:00"
last_updated_date: "2026-07-22 00:00 -05:00"
tags: fsharp, dotnet, architecture, lqdev-me, reference
related_skill: add-content-type
source_project: lqdev-me
---

A living reference document covering the architecture of the lqdev.me static site generator — an F# application targeting .NET 10 that generates a personal IndieWeb-compliant website.

## Technology Stack

- **Language**: F# (.NET 10, target framework `net10.0`)
- **HTML Generation**: Giraffe ViewEngine (type-safe HTML via F# functions)
- **Markdown**: Markdig with custom extensions
- **YAML**: YamlDotNet for frontmatter parsing
- **Web Standards**: IndieWeb microformats2, h-entry, h-card, Webmention
- **Feeds**: RSS 2.0 via custom XML generation

## Module Architecture

The generator follows a pipeline architecture where each module handles one concern:

| Module | Responsibility |
|--------|---------------|
| `Domain.fs` | Type definitions — all content types, metadata records, interfaces |
| `ASTParsing.fs` | YAML frontmatter + Markdown parsing with Markdig |
| `GenericBuilder.fs` | Content processors, unified feed, RSS generation |
| `Builder.fs` | Build orchestration — loads, processes, writes each content type |
| `Program.fs` | Entry point — wires everything together |
| `Views/*.fs` | Giraffe ViewEngine templates for all page types |

## Content Types

The generator supports 10+ content types, each following a consistent pattern:

- **Posts** (`_src/posts/`): Blog articles with `published_date`
- **Notes** (`_src/notes/`): Short microblog posts
- **Responses** (`_src/responses/`): Stars, replies, reshares with `dt_published`
- **Bookmarks** (`_src/bookmarks/`): Saved links using response infrastructure
- **Wiki** (`_src/wiki/`): Knowledge base entries with `last_updated_date`
- **Snippets** (`_src/snippets/`): Code snippets with `created_date`
- **AI Memex** (`_src/resources/ai-memex/`): This knowledge system with `published_date`
- **Projects** (`_src/projects/`): Portfolio items
- **Docs** (`docs/`): Internal documentation

## Build Pipeline

```
_src/**/*.md  →  ASTParsing  →  GenericBuilder  →  Builder  →  _public/
  (source)       (parse)        (process)         (render)     (output)
```

1. `Program.fs` loads all source files per content type
2. `ASTParsing.fs` extracts YAML frontmatter into typed records
3. `GenericBuilder.fs` processors convert markdown to HTML
4. `Builder.fs` orchestrates rendering via `Views/*.fs` functions
5. Output written to `_public/` directory structure

## Key Patterns

- **IgnoreUnmatchedProperties**: YAML parser ignores unknown fields, making schema evolution safe
- **CLIMutable records**: All metadata types use `[<CLIMutable>]` so new string fields default to null
- **ContentProcessor pattern**: Generic interface for parse/render/output per content type
- **UnifiedFeedItem**: Common wrapper enabling cross-content-type feeds and timelines

## Feed Architecture

- **Unified feed**: `_public/feed/feed.xml` — all content types combined
- **Per-type feeds**: `_public/posts/feed.xml`, `_public/resources/ai-memex/feed.xml`, etc.
- **Feed aliases**: User-friendly URLs like `/all.rss`, `/ai-memex.rss`
