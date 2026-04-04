---
title: "Tool Lifecycle & Promotion System — A Memory Model for Executable Code"
description: "Design exploration for a three-tier tool lifecycle (ephemeral → nursery → custom) with usage-based promotion, inspired by spaced repetition and cognitive memory models"
entry_type: research
published_date: "2026-04-03 00:00 +00:00"
last_updated_date: "2026-04-03 00:00 +00:00"
tags: "emacs, copilot, architecture, patterns, ai-collaboration, metaprogramming"
related_skill: ""
source_project: "copilot-sdk-elisp"
related_entries: "pattern-tool-nursery-lifecycle, pattern-mcp-bridge-client-side-tools, pattern-emacs-ai-operating-system-architecture"
---

# Tool Lifecycle & Promotion System

## The Problem

In an AI-native Emacs environment, tools are defined at runtime — via chat
conversations, `M-x copilot-sdk-deftool`, or agent suggestion. These tools
live only in memory. When Emacs restarts, they vanish.

The naive fix is "save to a file." But that creates a different problem: tool
sprawl. Over time, you accumulate tools you no longer use, tools that were
one-off experiments, and tools that should have been refined before being
committed to your config.

## The Insight: Tools Should Earn Their Permanence

The core idea is a **three-tier lifecycle** modeled on how memory works — both
human cognition and computing systems:

```
┌─────────────┐    persist    ┌─────────────┐    promote    ┌─────────────┐
│  Ephemeral  │──────────────►│   Nursery   │─────────────►│   Custom    │
│  (runtime)  │               │  (staging)  │              │ (permanent) │
└─────────────┘               └─────────────┘              └─────────────┘
  In memory only              Persisted to disk             In user's config
  Gone on restart             Tracks usage stats            Clean, curated
                              Auto-loaded on start          "Blessed" tools
                              Decays if unused
```

## The Analogy Table

This pattern appears everywhere:

| Domain               | Ephemeral        | Staging           | Permanent        |
|----------------------|------------------|-------------------|------------------|
| **Human memory**     | Working memory   | Short-term memory | Long-term memory |
| **Git**              | Working tree     | Index/staging     | Committed        |
| **Unix tools**       | One-liner        | Script in ~/bin   | Installed package|
| **Emacs**            | `M-:` eval       | *scratch* buffer  | init.el          |
| **Habits**           | Try something    | Do it a few times | Muscle memory    |
| **Spaced repetition**| See card         | Review interval   | Mastered         |
| **AI Memex**         | Observation      | Draft entry       | Published        |

The nursery is essentially a **spaced repetition system for executable code**.

## Nursery Data Model

Each nursery entry tracks:

```elisp
(:name "greet-user"
 :deftool-form "(copilot-sdk-deftool greet-user ...)"
 :created "2026-04-03T19:00:00Z"
 :source "chat"           ; "chat" | "manual" | "agent-suggested"
 :usage-count 0           ; incremented on each invocation
 :session-count 0         ; incremented once per Emacs session
 :last-used nil           ; timestamp of last invocation
 :version 1               ; bumped when tool is refined
 :notes "")               ; user or agent annotations
```

## Lifecycle Operations

### Persist (Ephemeral → Nursery)
- **User**: `M-x copilot-tools-save` → select from active runtime tools
- **Agent**: Suggests persistence after defining a tool via chat
- Writes deftool form + metadata to `copilot-tool-nursery.el`
- Auto-loaded on next Emacs startup

### Promote (Nursery → Custom)
- **Threshold-based**: After N uses across M sessions, agent suggests promotion
- **Manual**: `M-x copilot-tools-promote`
- Moves clean deftool form to `copilot-custom-tools.el` (no metadata)
- Optionally generates a Memex entry documenting the tool's journey

### Decay (Nursery → Archive)
- Tools unused for K sessions get flagged for archival
- Agent: "You haven't used `greet-user` in 20 sessions. Archive?"
- Archived tools can be restored anytime

### Demote (Custom → Nursery)
- `M-x copilot-tools-demote` — moves back to nursery for re-evaluation

## The Agent as Tool Gardener

In the full vision, the AI agent becomes a lifecycle advisor:

1. **Observation**: "You keep asking me to format JSON. Want me to create a tool?"
2. **Creation**: Defines the tool via `emacs-define-tool`
3. **Persistence**: "Save this to the nursery?"
4. **Refinement**: "You always pass `--indent 2`. Want me to update the default?"
5. **Promotion**: "Used 15 times across 5 sessions. Promote to custom?"
6. **Composition**: "You always run `lint` then `format`. Compose them?"
7. **Pruning**: "Unused for 10 sessions. Archive?"

## Implementation Philosophy: Manual Until It Hurts

The system itself should follow its own lifecycle:

- **Phase 0**: Just save/load (~50 lines). Solves the immediate pain.
- **Phase 1**: Add nursery list buffer, promote/remove commands. Still manual.
- **Phase 2**: Add usage tracking via dispatch hook. Stats visible, decisions manual.
- **Phase 3**: Agent reads stats, suggests promotions. User approves.
- **Phase 4**: Proactive pattern detection, composition, Memex integration.

Each phase is independently useful. You stop wherever feels right and graduate
only when friction demands it.

## Deeper Implications

### Emergent Configuration
Traditional config is declarative — you specify what you want upfront. This is
**emergent** — the system observes what you actually do and crystallizes it.

### Tools as Living Entities
With version tracking, tools aren't static definitions. They evolve through
refinement, and the nursery captures that evolution.

### The Memex Connection
Promotion is a signal: this is a **proven pattern**. Auto-generating a Memex entry
on promotion captures not just the tool but its story — why it was created, how
it evolved, how often it's used. The tool lifecycle feeds the knowledge system.

### Composability Analysis
Usage telemetry enables compositional insights: tools frequently used together
could be merged, tools with similar inputs could be unified, tools wrapping the
same underlying command could be consolidated.

## Where This Lives (Repo Boundary)

- **copilot-sdk-elisp** (SDK): Persistence primitives, usage tracking hooks,
  serialization/deserialization of deftool forms
- **copilot-emacs** (App): Promotion logic, agent-facing tools, review UI,
  Memex integration, advice hook suggestions

## Open Questions

1. Should `emacs-define-tool` always auto-persist to nursery, or require opt-in?
2. What defines a "session" for counting? Emacs startup is simplest.
3. Could teams share nursery files for tool discovery? (Future)
4. If tool B depends on tool A, should promoting B also promote A? (Future)
5. Plain `.el` for readability vs structured `.eld` for machine parsing?

## Status

This is a **design exploration** — not yet implemented. Starting with Phase 0
(save/load) is the recommended first step.
