# AI Memex System

The AI Memex is a distributed knowledge capture system where AI coding agents help write entries about patterns, research, decisions, and project outcomes. Entries are published at [lqdev.me/resources/ai-memex/](https://www.lqdev.me/resources/ai-memex/).

## Architecture

### Distributed Capture Model

The AI Memex uses a hub-and-spoke architecture:

- **Hub** (this repo): `_src/resources/ai-memex/` → published by `dotnet run`
- **Spokes** (any project): `.ai-memex/` directories → imported to hub when ready

```
C:\Dev\website\                          ← HUB (lqdev.me)
└── _src\resources\ai-memex\             ← Canonical source, published entries
    ├── hello-world.md
    ├── codebase-context.md
    └── pattern-*.md                     ← 10+ proven pattern entries

C:\Dev\[any-project]\                    ← SPOKE (any other project)
└── .ai-memex\                           ← Per-project staging (zero config)
    ├── pattern-xyz.md                   ← Same YAML schema as hub
    └── drafts\                          ← Gitignored WIP
```

### Content Type Details
- **Source**: `_src/resources/ai-memex/`
- **Output**: `/resources/ai-memex/{slug}/`
- **RSS Feed**: `/resources/ai-memex/feed.xml` (alias: `/ai-memex.rss`)
- **Landing Page**: `/resources/ai-memex/`

### Timeline & Feed Integration
- **NOT on homepage timeline** (like wiki)
- **IS in the "all" fire-hose feed** (like wiki)
- **Has own RSS feed** at `/resources/ai-memex/feed.xml`
- **Included in tag feeds** for its tags

## Entry Types

| Type | Purpose | Icon |
|------|---------|------|
| `project-report` | Document completed work and outcomes | bi-clipboard-check |
| `research` | Technology investigations and comparisons | bi-search |
| `reference` | Living documents on specific topics | bi-book |
| `pattern` | Recurring solutions and anti-patterns | bi-lightbulb |
| `blog-post` | Reflections and commentary | bi-pen |

## YAML Schema

```yaml
---
title: Entry Title
description: Brief summary of the entry
entry_type: pattern
published_date: "YYYY-MM-DD HH:mm zzz"
last_updated_date: "YYYY-MM-DD HH:mm zzz"
tags: tag1, tag2, tag3
related_skill: write-ai-memex
source_project: lqdev-me
related_entries: slug-1, slug-2, slug-3
---
```

**Fields**:
- `title` (required): Entry title
- `description` (required): Brief summary, displayed as a callout on the page
- `entry_type` (required): One of: project-report, research, reference, pattern, blog-post
- `published_date` (required): Initial publication date
- `last_updated_date` (required): Last modification date (same as published_date for new entries)
- `tags` (required): Comma-separated tags
- `related_skill` (optional): Agent skill that produced this entry
- `source_project` (optional): Project where this knowledge originated
- `related_entries` (optional): Comma-separated slugs of related Memex entries

## Trigger System

AI agents watch for moments worth capturing during coding sessions. When a trigger fires, the agent proposes (never auto-creates) a Memex entry.

| Event | Entry Type |
|-------|-----------|
| Non-obvious bug fixed | pattern |
| Architecture decision made | pattern |
| Platform/language gotcha discovered | pattern |
| Same solution used a 2nd time | pattern |
| Multi-approach research completed | research |
| Technology evaluated (pros/cons) | research |
| Feature shipped (build succeeds) | project-report |
| Something genuinely reusable built | reference |
| AI-human collaboration insight | blog-post |

At session end: "Anything from this session worth capturing?"

## Three-File Instruction Architecture

The AI Memex companion system uses three files that complement each other:

| File | Purpose | Loaded By |
|------|---------|-----------|
| `AGENTS.md` (repo root) | Full Memex system + project overview | All AI tools |
| `.github/copilot-instructions.md` | F# patterns, ViewEngine details | Copilot only |
| `$HOME/.copilot/copilot-instructions.md` | Lean cross-project triggers | Copilot (every session) |

## Agent Skills

Skills provide procedural "how-to" instructions loaded on-demand:

| Skill | Scope | Purpose |
|-------|-------|---------|
| `write-ai-memex` | Universal (`~/.agents/skills/`) | Write entries anywhere |
| `query-ai-memex` | Universal (`~/.agents/skills/`) | Search knowledge base |
| `import-ai-memex` | Universal (`~/.agents/skills/`) | Consolidate spokes to hub |
| `add-content-type` | Repo (`.github/skills/`) | Add new content types |
| `build-validate` | Repo (`.github/skills/`) | Build and validate site |

Install universal skills: `.\Scripts\install-skills.ps1`

## Import Flow

When working in the lqdev.me repo, the import skill consolidates entries from other projects:

1. Reads `~/.agents/skills/import-ai-memex/import-sources.json` (user-local, never committed)
2. Scans each source `.ai-memex/` directory for `.md` files
3. Copies new entries to `_src/resources/ai-memex/` (skips existing)
4. Sets `source_project` in frontmatter during import
5. Run `dotnet run` to publish

## Tag Taxonomy

- **Technology**: `fsharp`, `dotnet`, `python`, `typescript`, `javascript`, `rust`, `go`, `docker`, `azure`, `git`
- **Domain**: `web`, `api`, `databases`, `devops`, `architecture`, `security`, `performance`, `accessibility`, `indieweb`
- **Project**: `lqdev-me` + any project slug
- **Meta**: `patterns`, `research`, `ai-collaboration`, `meta`

## Visual Design

### Desert Twilight Palette
The AI Memex uses a purple color scheme ("Desert Twilight") within the site's desert-inspired design system:

- Base: `#8B5FBF` (--desert-twilight)
- Light: `#A67FD4` (--desert-twilight-light)
- Background: `rgba(139, 95, 191, 0.08)` (--desert-twilight-bg)

### AI Author Card
The AI author card is **visible** — displayed with a purple left border and background tint. This follows IndieWeb authorship principles while clearly identifying AI-authored content.

## Quality Standards
- Use clear, direct prose — no filler or marketing language
- Include code examples where relevant
- Link to related content on the site
- Update `last_updated_date` when making substantive changes
- Always ask before creating — never auto-generate without user consent

## Knowledge Graph

The AI Memex includes a build-time knowledge graph that connects entries via multiple signal layers.

### Connection Layers

| Layer | Signal | Weight | Description |
|-------|--------|--------|-------------|
| 1 | `related_entries` frontmatter | 1.0 | Author-curated, highest trust |
| 2 | `[[slug]]` wikilinks in content | 0.9 | Inline references in body text |
| 3 | Tag overlap (Jaccard ≥ 0.3) | 0.3–0.7 | Scaled by Jaccard coefficient |
| 4 | Same `source_project` | 0.2 | Weak clustering signal |
| 5 | Same `entry_type` + shared tag | 0.1 | "More like this" signal |

### Wikilinks

Use `[[slug]]` syntax in entry content to create inline links to other Memex entries:
- `[[pattern-viewengine-integration]]` → renders as a link with the entry's title
- `[[pattern-viewengine-integration|custom text]]` → renders with custom display text
- Broken links (unknown slugs) render as styled spans, not errors

### Generated Outputs

- **Backlinks section**: "Linked From" on each entry page — only explicit + wikilink edges
- **Related Entries section**: Top 5 entries by combined edge weight with reasons
- **Related on this site**: Posts, wiki, snippets sharing 2+ tags (cross-content-type)
- **graph.json**: `/resources/ai-memex/graph.json` — full graph for visualization and AI tools
- **JSON-LD**: Per-entry `<script type="application/ld+json">` with Schema.org vocabulary
- **Graph visualization**: Interactive D3.js force-directed graph on index page

### Architecture

- `KnowledgeGraph.fs` — Graph construction, edge discovery, serialization, JSON-LD, wikilinks
- Must compile before `Views/` in `PersonalSite.fsproj`
- `buildAiMemex()` in `Builder.fs` orchestrates graph building + wikilink resolution
- JSON-LD maps entry types to Schema.org: pattern→TechArticle, research→ScholarlyArticle, project-report→Article, blog-post→BlogPosting

### JSON-LD / Linked Data

Each entry page embeds JSON-LD structured data for AI crawlers (GPTBot, ClaudeBot, Googlebot):
- `author: SoftwareApplication` (GitHub Copilot) — AI authorship attribution
- `publisher: Person` (Luis Quintanilla) — human curator
- `isPartOf: Collection` — membership in the AI Memex
- `relatedLink` — bidirectional links from backlinks + related entries
- `about` — extracted from tags as Schema.org Thing entities
