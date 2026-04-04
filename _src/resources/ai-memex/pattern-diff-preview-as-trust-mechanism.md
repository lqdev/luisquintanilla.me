---
title: "Diff Preview as Trust Mechanism for Agent Edits"
description: "How showing diffs before applying transforms agent code edits from scary to collaborative — the pattern that made AI coding assistants viable"
entry_type: pattern
published_date: "2026-04-04 00:00 +00:00"
last_updated_date: "2026-04-04 00:00 +00:00"
tags: "patterns, emacs, ai-collaboration, architecture, ux"
related_skill: ""
source_project: "copilot-sdk-elisp"
related_entries: "project-report-the-agentic-leap, pattern-agent-as-unix-citizen, ai-collaboration-patterns"
---

# Diff Preview as Trust Mechanism for Agent Edits

## Discovery

While building an AI agent that edits Emacs buffers via Copilot, we faced a trust
problem: the agent could silently modify any open buffer. Users had no way to review
changes before they landed. This is the exact problem Cursor solved to become dominant
in AI-assisted coding — and the pattern generalizes beyond any specific tool.

## Root Cause

Agent-mediated edits have a fundamental UX problem: **the user surrenders control at
the moment of highest stakes** (code modification). Without preview:

- Users restrict the agent to safe operations (read-only queries)
- They avoid asking for refactoring or multi-file edits
- Trust never builds because there's no safe way to experiment
- The agent's most powerful capability (code editing) goes unused

This is the "trust gap" — the agent CAN do powerful things, but the user WON'T let it.

## Solution

Intercept every `buffer-edit` tool call with a diff preview layer:

```elisp
(defun copilot-diff-wrap-edit (args)
  "Show diff preview instead of applying edit directly."
  (if (or noninteractive (not copilot-diff-preview-enabled))
      ;; Batch mode or preview disabled → direct edit
      (copilot-emacs-tools--buffer-edit args)
    ;; Interactive mode → show diff, wait for user decision
    (copilot-diff-show
     :buffer-name target
     :old-content old
     :new-content new
     :on-accept (lambda () (apply-edit ...))
     :on-reject (lambda () (discard ...))
     :on-edit   (lambda () (open-editable ...)))))
```

Key design decisions:

1. **Side-window display** — diff appears alongside the buffer being edited, not
   replacing it. User keeps context.

2. **Three response modes** — Accept (`C-c C-c`), Reject (`C-c C-k`), Edit (`e`).
   Edit lets the user modify the proposed change before accepting — this is where
   trust actually builds. The user thinks "I'll just tweak this" and discovers the
   agent got 95% right.

3. **Batch fallthrough** — In `noninteractive` mode (tests, scripts), the diff
   preview is bypassed entirely. This prevents tests from hanging on user input
   while keeping the same code path.

4. **Multi-hunk navigation** — When the agent makes changes in multiple places,
   each hunk gets its own section with `n`/`p` navigation. Users can review
   changes atomically.

5. **Auto-accept timer** — Optional `copilot-diff-auto-accept-timeout` for
   users who've built enough trust to auto-approve after N seconds. This is the
   end state of the trust gradient.

## The Trust Gradient

```
Level 0: Agent can only read      (no edits)
Level 1: Agent proposes, human accepts  (diff preview — default)
Level 2: Agent proposes, auto-accept    (timer mode)
Level 3: Agent edits directly          (preview disabled)
```

Users naturally progress through these levels as trust builds. The key insight is
that **Level 1 is the correct default**, not Level 0 or Level 3. Level 0 cripples
the agent. Level 3 scares users. Level 1 is collaborative.

## Prevention

When building any agent that modifies user state (files, buffers, configs, databases):

- **Always default to preview mode** — let users opt into auto-apply
- **Preserve the original** — always keep undo capability
- **Make reviewing fast** — syntax highlighting, faces, context lines
- **Allow partial acceptance** — multi-hunk with per-hunk review
- **Provide escape velocity** — auto-accept for power users

The pattern extends beyond code: any agent action with side effects benefits from
a preview → approve → apply cycle.

## Implementation Notes

- Custom Emacs faces for added/removed/context lines (green/red/dim)
- Buffer-local callbacks for accept/reject/edit actions
- Cleanup function removes temp buffers and kills windows
- Falls through to direct edit when `noninteractive` is true
- Total implementation: ~520 lines in `copilot-diff.el`
