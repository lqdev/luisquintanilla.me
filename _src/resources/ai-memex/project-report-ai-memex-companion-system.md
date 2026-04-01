---
title: "Project Report: Building the AI Memex Companion System"
description: A distributed knowledge capture system that makes AI coding agents Memex-aware across every project, with hub-and-spoke architecture, agent skills, and a three-file instruction design.
entry_type: project-report
published_date: "2026-04-01 01:32 -05:00"
last_updated_date: "2026-04-01 01:32 -05:00"
tags: ai-memex, architecture, fsharp, ai-collaboration, lqdev-me, patterns
related_skill: write-ai-memex
source_project: lqdev-me
---

## Goal

Transform the AI Memex from a single-repo content type into a distributed knowledge capture system that works across every coding project — making AI agents automatically aware of when to propose entries and how to write them.

## What Was Built

### Three-File Instruction Architecture

The system uses three complementary instruction files that ensure AI agents are Memex-aware without any manual setup:

| File | Purpose | Scope |
|------|---------|-------|
| `AGENTS.md` (repo root) | Full Memex system + project overview | All AI tools (Copilot, Cursor, Codex, Aider, Claude) |
| `.github/copilot-instructions.md` | F# patterns, ViewEngine details | Copilot only |
| `$HOME/.copilot/copilot-instructions.md` | Lean cross-project trigger discipline | Every Copilot session, any project |

The critical discovery was that `$HOME/.copilot/copilot-instructions.md` is loaded globally in every Copilot CLI session regardless of project. This eliminated the need for a `session-startup` skill — the trigger discipline is always active.

### Distributed Hub-and-Spoke Capture

Rather than requiring all knowledge to funnel through one repo, entries are captured where the work happens:

- **Hub** (lqdev.me): `_src/resources/ai-memex/` — canonical source, published by `dotnet run`
- **Spokes** (any project): `.ai-memex/` directories — zero-config staging, imported when ready

This was a key architectural pivot. The original design assumed agents would always work in the lqdev.me repo. The distributed model means knowledge capture happens naturally during any coding session.

### Agent Skills

Three universal skills installed to `~/.agents/skills/` (available in any project):

- **write-ai-memex**: Schema, templates, quality standards, context detection
- **query-ai-memex**: Search the knowledge base for relevant entries
- **import-ai-memex**: Consolidate entries from project spokes to the hub

Two repo-specific skills in `.github/skills/`:

- **add-content-type**: 8-file checklist for new content types
- **build-validate**: Build and validate the site generator

Skills load on-demand via semantic matching — they complement the always-on global instructions.

### Schema Enhancement

Added two optional fields to `AiMemexDetails` in `Domain.fs`:

- `related_skill`: Links an entry to the agent skill that produced it
- `source_project`: Tracks which project the knowledge originated from

These render conditionally in `aiMemexPageView` as provenance badges. No parser changes needed — `[<CLIMutable>]` defaults new string fields to null, and `IgnoreUnmatchedProperties()` handles unknown YAML keys.

### Pattern Backfill

Extracted 10 proven patterns from `copilot-instructions.md` into full AI Memex entries, then replaced the verbose inline documentation with compact one-liner references. Net result: -147 lines from copilot-instructions while gaining richer, searchable, publishable pattern documentation.

## Key Decisions

1. **AGENTS.md as primary vehicle** — it's the community standard read by ALL AI tools, not just Copilot
2. **Always propose, never auto-create** — the trigger discipline asks "Want me to write this up?" rather than silently generating entries
3. **Committed knowledge** — `.ai-memex/` directories are committed (knowledge = project history), with `.ai-memex/drafts/` gitignored for WIP
4. **Import is batch, not real-time** — entries move from spokes to hub when the user is working in lqdev.me, not via background sync

## Implementation

11 commits on `feature/ai-memex-companion`:

1. Universal skills + global instructions
2. Repo-specific skills
3. Install script (`scripts/install-skills.ps1`)
4. Codebase-context reference entry
5. Living reference drafts
6. 10 backfilled pattern entries
7. Schema: `related_skill` + `source_project` fields
8. Documentation rewrite with distributed architecture
9. AGENTS.md creation
10. copilot-instructions.md trimmed (-147 lines)
11. All 5 agent definitions updated with Memex awareness

## Results

- Build succeeds with 0 errors (1 pre-existing FS1104 warning)
- 15 AI Memex entries generate correctly with new schema fields
- Provenance badges render conditionally (only when fields are populated)
- Universal skills available in any project after one-time install
- Global trigger discipline active in every Copilot CLI session
