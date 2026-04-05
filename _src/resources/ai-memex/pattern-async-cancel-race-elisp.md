---
title: "Cancel Race Condition in Async Elisp Event Handlers"
description: "Pattern for safely cancelling async operations when process filter events can arrive after cancellation"
entry_type: pattern
published_date: "2026-04-04 00:00 +00:00"
last_updated_date: "2026-04-04 00:00 +00:00"
tags: "emacs, elisp, patterns, architecture"
related_skill: ""
source_project: "copilot-sdk-elisp"
related_entries: "pattern-at-mention-inline-context-expansion, project-report-conscious-emacs-life-os-daily-driver, pattern-emacs-ai-operating-system-architecture"
---

# Cancel Race Condition in Async Elisp Event Handlers

## Discovery

In the Copilot Emacs chat UI, pressing `C-c C-k` to cancel a running agent turn
would briefly show "idle" state, then immediately flip back to "streaming" or
"tool-running". The cancel appeared to not work.

## Root Cause

Emacs process filters fire asynchronously — they interrupt whatever Elisp is
running when data arrives on a subprocess pipe. The cancel flow was:

```
1. User presses C-c C-k
2. copilot-chat-cancel sets state → 'idle
3. Sends session/cancel to CLI
4. CLI acknowledges...
5. BUT: buffered events (tool_call_update, agent_message_chunk) still arrive
6. Event handlers set state → 'streaming or 'tool-running
7. User sees state flip BACK from idle
```

The fundamental issue: **you cannot stop events that are already in the pipe**.
The CLI sends cancellation acknowledgment, but events emitted *before* the
cancel was received still arrive afterward.

## Solution

A buffer-local boolean flag that all event handlers check:

```elisp
(defvar-local copilot-chat--cancelled nil
  "Non-nil when a cancel is in progress.")

;; In copilot-chat-cancel:
(setq copilot-chat--cancelled t)
(setq copilot-chat--state 'idle)
;; ... send cancel to CLI ...

;; In EVERY event handler (on-message-chunk, on-tool-call, on-tool-call-update):
(unless copilot-chat--cancelled
  ;; ... process the event normally ...
  )

;; In on-turn-end (fires when CLI confirms turn is done):
(setq copilot-chat--cancelled nil)
```

This is simpler and more robust than alternatives considered:

- **Debouncing**: Too complex, timing-dependent
- **Event queue flushing**: Not possible with Emacs process filters
- **Sequence numbers**: Over-engineered for this use case
- **`cl-return-from`**: Doesn't work in regular `defun` — requires `cl-defun`

## Prevention

When building async event-driven systems in Elisp:

1. **Always use a cancellation flag** for any operation that can be user-cancelled.
   Process filter events WILL arrive after your cancel call.

2. **Guard every event handler** that mutates state. Not just one — ALL of them.
   It's easy to guard the main text handler but forget the tool-call handler.

3. **Clear the flag in the terminal event** (turn-end, process-exit), not in the
   cancel function itself. The flag must persist across the entire draining period.

4. **Buffer-local variables are essential** for per-buffer state in Emacs. If you
   have multiple chat buffers, each needs its own cancellation flag.

5. **Avoid `cl-return-from` in regular `defun`** — it requires `cl-defun` or an
   explicit `cl-block`. This is a common Emacs 28 gotcha. Use `when`/`unless`
   wrappers instead.

## Applicability

This pattern applies to any Emacs package that:
- Communicates with external processes via stdio (LSP clients, REPL bridges, AI agents)
- Has user-cancellable operations
- Uses process filters for event dispatch
- Maintains UI state that events can modify

Examples: lsp-mode, eglot, comint-mode derivatives, any LLM chat interface.
