---
title: "Ambient Intelligence in Emacs: From Reactive to Proactive AI"
description: "Pattern for building non-intrusive ambient AI features using idle timers, state persistence, and trust-graduated autonomy in Emacs"
entry_type: pattern
published_date: "2025-07-25"
last_updated_date: "2025-07-25"
tags: "emacs, ai-collaboration, patterns, architecture, elisp"
related_skill: ""
source_project: "copilot-sdk-elisp"
related_entries: "pattern-emacs-ai-operating-system-architecture, project-report-ambient-intelligence-phase, pattern-gtd-ai-agent-complementarity"
---

# Ambient Intelligence in Emacs: From Reactive to Proactive AI

## Discovery

Building an AI-native operating system in Emacs revealed a fundamental UX gap:
every interaction required explicit invocation. The system was powerful but
**reactive** — it sat idle until commanded. Users of modern AI tools (Cursor,
GitHub Copilot in VS Code) expect ambient awareness: the system notices context,
pre-gathers data, and offers timely suggestions without being asked.

The challenge: Emacs's single-threaded architecture makes "background AI"
genuinely difficult. Any blocking operation freezes the entire editor. And
overly aggressive ambient features feel like clippy — actively hostile to flow.

## Root Cause

Three architectural gaps prevented ambient behavior:

1. **No state persistence across sessions** — The system couldn't remember
   "did I already brief the user today?" or "when was the last check-in?"
2. **No idle awareness** — No mechanism to do work when the user isn't typing
3. **No trust gradient** — All features were equally intrusive (or equally silent)

## Solution

### 1. State Persistence via Printed Alists

```elisp
;; Simple, debuggable, no dependencies
(defvar copilot-ambient--state nil)
(defconst copilot-ambient--state-file "~/.copilot/ambient-state.el")

(defun copilot-ambient--save-state ()
  (with-temp-file copilot-ambient--state-file
    (prin1 copilot-ambient--state (current-buffer))))

(defun copilot-ambient--load-state ()
  (when (file-exists-p copilot-ambient--state-file)
    (with-temp-buffer
      (insert-file-contents copilot-ambient--state-file)
      (setq copilot-ambient--state (read (current-buffer))))))
```

Key insight: `prin1` + `read` is the Elisp equivalent of JSON serialization.
No external dependencies, human-readable files, trivial to debug.

### 2. Layered Timer Architecture

```
Startup (10s delay)     → Morning briefing (once per day)
Idle (60s)              → Context prefetch (gather agenda/feeds/habits)
Periodic (2h)           → .plan check-in (gentle reminder)
Polling (5min after 5pm) → Evening review nudge (once per day)
```

Critical design decisions:
- **`run-with-idle-timer`** for user-idle work (prefetch) — only fires when
  truly idle, never interrupts typing
- **`run-with-timer`** for periodic work (check-ins) — fires on schedule
  regardless of activity
- **Startup delay** (10s) — lets Emacs finish initializing before AI speaks
- **All timers cancelled on mode disable** — clean teardown, no orphan timers

### 3. Non-Intrusive Messaging

```elisp
(defun copilot-ambient--notify (message)
  "Notify user without interrupting their flow."
  (unless (minibufferp)  ;; NEVER interrupt minibuffer input
    (message "🌊 %s" message)))
```

Rules for ambient notifications:
- Use `message` (echo area), never `display-buffer` or `pop-to-buffer`
- Check `(minibufferp)` before every message
- Prefix with emoji (🌊) so user recognizes AI messages at a glance
- Wrap everything in `condition-case` — ambient features must NEVER crash Emacs

### 4. Trust Gradient

Features arranged from least to most autonomous:

| Level | Feature | Autonomy | User Action |
|-------|---------|----------|-------------|
| 0 | Model display in header | Passive info | None needed |
| 1 | Quick-ask minibuffer | User-initiated | Explicit prompt |
| 2 | Morning briefing | Auto-triggered | Read message |
| 3 | Context prefetch | Invisible | None (data cached) |
| 4 | .plan check-in | Proactive nudge | Dismiss or act |
| 5 | Evening review | Proactive nudge | Dismiss or act |

Users enable ambient mode as a whole, but each feature can be individually
toggled. Start conservative, let users opt into more autonomy.

## Prevention / Best Practices

1. **Never block the main thread** — All ambient work should be async
   (process sentinels, idle timers, `run-with-timer`)
2. **Always gate behind a minor mode** — `copilot-ambient-mode` toggle,
   lighter indicator in mode-line (🌊), clean enable/disable lifecycle
3. **Persist state, not behavior** — Store "last briefing date" not
   "should briefing run". Let the logic derive behavior from state.
4. **Conservative defaults** — 60s idle (not 10s), 2h check-ins (not 30min),
   10s startup delay (not immediate)
5. **Graceful degradation** — If org-journal isn't loaded, skip journal
   features. If elfeed isn't available, skip feed features. Never error.

## The Minibuffer-First Principle

A complementary pattern emerged: the minibuffer as the primary AI interaction
surface. Instead of switching to a chat buffer:

```
M-x copilot-quick → "What's on my agenda today?"
→ Streaming response appears in bottom popup
→ Press 'c' to continue, 'y' to copy, 'i' to insert at point
→ Never left the current buffer
```

This respects flow state — the user's eyes stay on their code/text, AI
response appears peripherally, and they can act on it or dismiss it without
context switching.

## Key Metrics

- 5 ambient features implemented with 0 blocking operations
- All timers configurable via `defcustom`
- State file is 1 alist (< 1KB), loads in < 1ms
- 29 tests covering all ambient behaviors
- Zero crashes in testing (every feature wrapped in `condition-case`)
