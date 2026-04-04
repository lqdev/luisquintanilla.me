---
title: "Research: .plan Files and Unix Context Traditions"
description: "How the 1971 Unix finger protocol's .plan/.project files inform modern AI agent context mechanisms"
entry_type: research
published_date: "2026-04-02"
last_updated_date: "2026-04-02"
tags: "unix, dotfiles, architecture, research, ai-collaboration"
related_skill: ""
source_project: "copilot-sdk-elisp"
related_entries: "pattern-agent-as-unix-citizen, pattern-deterministic-first-intelligent-second, research-emacs-personal-os-ecosystem"
---

## Context

While designing how an AI agent (Copilot CLI) should understand a user's current work context, we discovered that Unix already solved this problem in 1971. The finger protocol's `.plan` and `.project` files are a perfect model for AI agent context — plain text files that describe current intentions, readable by any tool.

## Options Considered

### Option A: Structured Config Files (JSON/YAML)

- **Pros**: Machine-parseable, strongly typed, familiar to modern devs
- **Cons**: Not human-friendly for quick edits, requires schema, version-sensitive

### Option B: Database/State Store

- **Pros**: Queryable, structured, can store complex state
- **Cons**: Opaque, not versionable with git, requires tooling to read

### Option C: Plain Text Files (.plan tradition)

- **Pros**: Human-readable, git-versionable, editable with any tool, zero dependencies, searchable with grep
- **Cons**: No enforced structure, relies on conventions

## Evaluation Criteria

1. **Simplicity**: Can the user read/write it without special tools?
2. **Versionability**: Does it work with git for history tracking?
3. **Tool compatibility**: Can grep, cat, sed work with it?
4. **Agent readability**: Can the AI agent parse useful context from it?
5. **Tradition**: Does it fit existing Unix/Emacs culture?

## Recommendation

**Option C: Plain text files** following the Unix .plan tradition.

### The Historical .plan/.project Convention

The finger protocol (Les Earnest, Stanford, 1971, formalized in RFC 1288) displayed the contents of `~/.plan` and `~/.project` when users queried information about another user:

- `~/.plan` — Current work focus, status, immediate intentions
- `~/.project` — Longer-term project description

These were plain text files maintained by individual users. They functioned as an early form of micro-blogging / status updates. The protocol ran on TCP port 79 and was common across Unix systems until security concerns reduced its usage.

### Modern Revival for AI Agents

The same files serve perfectly as AI agent context:

```
~/.plan (or project-local .plan)
──────────────────────────────────
## Current Focus
Auth refactor: replacing MD5 with bcrypt across all services.

## Blocked
Waiting for API spec from Alex (due 2026-04-05).

## Next Actions
- Rate limiting tests
- Security audit prep

## Recent Sessions
- 2026-04-02: Fixed SQL injection in auth.js
- 2026-04-01: Began bcrypt migration, 3/7 services done
```

**Agent reads `.plan` on session start** → knows current context instantly.
**Agent updates `.plan` at session end** → persistence without a database.
**`git log .plan`** → complete work history.

### Extended Dotfile Context System

Beyond .plan, many existing dotfiles provide rich agent context:

| File | What Agent Learns |
|------|-------------------|
| `.plan` | Current work focus, blockers, next actions |
| `.project` | Project description and goals |
| `Makefile` / `justfile` | Available build/test/lint commands |
| `.gitignore` | What matters in this project (by exclusion) |
| `.editorconfig` | Code style conventions |
| `README.md` | Project documentation and setup |
| `CHANGELOG.md` | What changed recently |
| `.envrc` / `.env` | Environment variables |
| `.copilot-context` | Explicit agent context (custom convention) |

Auto-reading these on session start gives the agent MASSIVE context improvement for zero additional effort.

## Trade-offs

**What we sacrificed**: Enforced structure. The .plan file has no schema — the agent must parse natural language. But this is exactly what LLMs are good at: understanding unstructured text.

**What we gained**:
- Zero tooling dependencies
- Works with every Unix tool (grep, cat, sed, git)
- Human-readable and human-editable
- Git-versionable with full history
- Consistent with 50+ years of Unix tradition
- The agent can both read AND write it naturally

**The key insight**: The .plan file is a BRIDGE between three worlds:
- **Org-mode** (structured, detailed task management)
- **Unix** (plain text, simple, universal)
- **AI agent** (reads context, writes updates)

All three can read and write plain text. It's the universal interface.
