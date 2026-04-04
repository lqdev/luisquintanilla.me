---
title: "Three-Tier Tool Nursery: Ephemeral → Staged → Permanent Lifecycle"
description: "Pattern for managing runtime-defined tools through a staging lifecycle with persistence, promotion, and file-based discovery"
entry_type: pattern
published_date: "2026-04-03"
last_updated_date: "2026-04-03"
tags: "emacs, elisp, patterns, architecture, ai-collaboration, metaprogramming"
related_skill: ""
source_project: "copilot-sdk-elisp"
related_entries: "research-tool-lifecycle-promotion-system, pattern-mcp-bridge-client-side-tools, pattern-emacs-ai-operating-system-architecture"
---

# Three-Tier Tool Nursery Lifecycle

## Discovery

While building an AI OS in Emacs backed by GitHub Copilot, we hit a fundamental
tension: the agent can define tools at runtime (via `copilot-sdk-deftool`), but
those tools vanish when Emacs restarts. Simply auto-persisting everything would
create permanent clutter from throwaway experiments. We needed a middle ground.

## Root Cause

The core problem has two faces:

1. **Ephemeral loss** — Runtime-defined tools live only in a hash table. Restart
   Emacs and they're gone. Useful tools created during a conversation disappear.

2. **Premature permanence** — Auto-saving everything to a config file creates
   noise. Not every experiment deserves to be permanent.

This mirrors how human memory works: not everything that enters working memory
should be committed to long-term storage. There needs to be a staging area.

## Solution: Three-Tier Lifecycle

```
Ephemeral (runtime)  →  Nursery (staged/persisted)  →  Custom (permanent)
```

### Tier 1: Ephemeral (runtime only)
- Tools defined via `copilot-sdk-deftool` or `copilot-sdk-tools-register`
- Live in `copilot-sdk--tool-registry` hash table
- Gone on restart — this is intentional and correct for experiments

### Tier 2: Nursery (persisted staging)
- User explicitly saves a tool: `M-x copilot-sdk-nursery-save`
- Stored in `~/.emacs.d/copilot-tool-nursery.eld` — Emacs Lisp Data format
- Each entry is a plist: `:name`, `:description`, `:created`, `:source`, `:form`
- The `:form` field contains the original `copilot-sdk-deftool` sexp
- Loaded on startup via `(copilot-sdk-nursery-load-all)` — evaluates stored forms
- A tool can sit in the nursery indefinitely, proving its value through use

### Tier 3: Custom (permanent config)
- User promotes from nursery: `M-x copilot-sdk-nursery-promote`
- Appends clean `copilot-sdk-deftool` form to `~/.emacs.d/copilot-custom-tools.el`
- Removes from nursery file — no duplication
- Custom tools file is plain Elisp with a `(provide)` — loads via standard `(load)`

### Key Implementation Detail: Deftool Form Capture

The `copilot-sdk-deftool` macro stores its own source form at expansion time:

```elisp
;; Inside the macro expansion:
(puthash tool-name
         ',(append (list 'copilot-sdk-deftool name description params) body)
         copilot-sdk--tool-deftool-forms)
```

This gives us a re-evaluable sexp that reproduces the tool exactly. Only
deftool-defined tools can be saved — tools registered via the raw API don't
have a capturable form.

**Gotcha**: Emacs 28 doesn't have standalone `list*` — use
`(append (list ...) body)` instead to avoid requiring `cl-lib` at macro level.

### Dynamic Discovery (companion pattern)

Runtime tool registration also needed to be visible to the MCP bridge (a
separate Python process). Solution: file-based manifest.

- SDK writes `copilot-sdk--tools-manifest-file` (temp file) on every
  register/unregister
- Bridge reads the file on each `tools/list` request (always fresh)
- Bridge declares `listChanged: true` and sends `notifications/tools/list_changed`
- Fallback chain: file → legacy env var → emacsclient query

## Prevention / Guidelines

1. **Start manual** — Don't auto-save or auto-promote. Let the user decide
   what's worth keeping. Automation can come later (usage tracking → smart
   suggestions).

2. **Form capture at definition time** — Store the original macro form, not a
   reconstructed approximation. This ensures perfect round-tripping.

3. **Separation of concerns** — Nursery file (`.eld`) uses Emacs Lisp Data
   format with plists for metadata. Custom tools file (`.el`) is plain Elisp.
   Different formats for different purposes.

4. **Tabulated list for browsing** — `tabulated-list-mode` gives sorting,
   column alignment, and a familiar Emacs buffer interface for free.

## Analogies

- **Git staging area**: Working tree (ephemeral) → Index (nursery) → Commit (custom)
- **Spaced repetition**: New cards → Review deck → Mastered
- **Unix tool philosophy**: Scripts → ~/bin → /usr/local/bin → system PATH
- **Human memory**: Working memory → Hippocampus → Long-term storage
