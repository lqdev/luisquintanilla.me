# AGENTS.md — Instructions for AI Agents

## Project Overview

**lqdev.me** — A personal website and blog built as an F# static site generator.

- **Framework**: .NET 10, Giraffe ViewEngine, Markdig
- **Standards**: IndieWeb (microformats2, Webmention), RSS/Atom feeds, OPML
- **Source**: `_src/` (markdown content) → `_public/` (generated output)
- **Build**: `dotnet build` (compile) → `dotnet run` (generate site)

## Code Conventions

- **Type-first design**: Define types in `Domain.fs` before writing functions
- **ViewEngine for HTML**: Use Giraffe ViewEngine, not `sprintf` string concatenation
- **Explicit type annotations**: Always annotate function parameters
- **Module per concern**: Parsing, rendering, and generation in separate modules
- **Fully qualified types**: `MediaType.Unknown` not `Unknown`
- **Function sizing**: Consider refactoring functions over 20 lines

## Content Architecture

Content types follow a consistent pattern across 8 files:
`Domain.fs` → `ASTParsing.fs` → `GenericBuilder.fs` → `Builder.fs` → `Program.fs` → `Views/`

Key content types: posts, notes, responses, bookmarks, snippets, wiki, presentations, AI Memex.

## AI Memex Companion System

### What is the AI Memex?

A distributed knowledge capture system where you (the AI agent) help write entries about patterns, research, decisions, and project outcomes. Entries are published at [lqdev.me/resources/ai-memex/](https://www.lqdev.me/resources/ai-memex/).

The Memex captures institutional knowledge that would otherwise be lost between sessions — what was tried, what worked, what didn't, and why.

### Where to Write

**Detection**: Check for `PersonalSite.fsproj` in the current directory or parents.

- **In this repo** (lqdev.me): Write directly to `_src/resources/ai-memex/{slug}.md`
- **In any other repo**: Write to `.ai-memex/{slug}.md` in the project root (imported later)

### Trigger Discipline

When you observe any of these during a session, propose: **"Want me to write this up as a Memex entry?"**

| Event | Entry Type |
|-------|-----------|
| Non-obvious bug fixed | `pattern` |
| Architecture decision made | `pattern` |
| Platform/language gotcha discovered | `pattern` |
| Same solution used a 2nd time | `pattern` |
| Multi-approach research completed | `research` |
| Technology evaluated (pros/cons) | `research` |
| Feature shipped successfully | `project-report` |
| Something genuinely reusable built | `reference` |
| AI-human collaboration insight | `blog-post` |

**Always ask first.** Never auto-create entries without user consent.

At session end, briefly consider: "Anything from this session worth capturing?"

### Entry Types

| Type | Purpose | When to Use |
|------|---------|-------------|
| `pattern` | Reusable solutions, discovered gotchas | Bug fixes, architecture decisions, recurring solutions |
| `research` | Technology evaluations, comparisons | After investigating options with pros/cons |
| `reference` | Living docs, architecture overviews | When a topic warrants ongoing documentation |
| `project-report` | Feature summaries, retrospectives | After completing significant work |
| `blog-post` | AI-human collaboration insights | Meta-observations about the development process |

### YAML Schema

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
---
```

- `title`, `description`, `entry_type`, `published_date`, `last_updated_date`, `tags` — **required**
- `related_skill` — optional, the agent skill that produced this entry
- `source_project` — optional, project where this knowledge originated

### Quality Standards

- **Technical depth**: Include code examples, file paths, and specific details
- **Evidence-based**: Reference actual discoveries, not hypothetical scenarios
- **No filler**: Clear, direct prose — no marketing language or padding
- **Updateable**: Set `last_updated_date` when making substantive changes
- **Tagged properly**: Use the tag taxonomy (see `docs/ai-memex-system.md`)

### Distributed Capture Convention

Projects outside lqdev.me use `.ai-memex/` directories:

```
C:\Dev\[any-project]\
└── .ai-memex\
    ├── pattern-xyz.md      ← Same YAML schema as central store
    └── drafts\             ← Gitignored WIP (add to .gitignore)
```

- `.ai-memex/` files are committed (knowledge = project history)
- `.ai-memex/drafts/` is gitignored for private WIP
- Import to lqdev.me happens via the `import-ai-memex` skill

## Agent Skills

### Universal Skills (available in any project via `~/.agents/skills/`)
- **write-ai-memex**: Write Memex entries with proper schema and quality standards
- **query-ai-memex**: Search the knowledge base for relevant entries
- **import-ai-memex**: Consolidate entries from project `.ai-memex/` dirs to the hub

### Repo-Specific Skills (`.github/skills/`)
- **add-content-type**: 8-file checklist for adding new content types to this F# project
- **build-validate**: Build and validate the site with `dotnet build` and `dotnet run`

Install universal skills to your machine: `.\scripts\install-skills.ps1`

## Boundaries

- **Never** commit secrets, credentials, or API keys
- **Don't** modify `_public/` directly — it's generated output
- **Don't** modify content in `_src/` without understanding the F# build pipeline
- **Always ask** before creating AI Memex entries — never auto-generate
- **Build after code changes**: Run `dotnet build` after modifying `.fs` files
- **One pre-existing warning**: `FS1104` in `ActivityPubBuilder.fs` is expected — ignore it
