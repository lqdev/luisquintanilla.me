---
title: "Advice-Based Module Wiring in Elisp AI Systems"
description: "Using Emacs advice-add as an architectural wiring pattern to connect modules without source modification — the nervous system of the AI OS"
entry_type: pattern
published_date: "2026-04-04 00:00 +00:00"
last_updated_date: "2026-04-04 00:00 +00:00"
tags: "emacs, elisp, patterns, architecture, ai-collaboration, metaprogramming, advice"
related_skill: ""
source_project: "copilot-sdk-elisp"
related_entries: "pattern-emacs-ai-operating-system-architecture, reference-emacs-primitives-ai-os-mapping, pattern-tool-nursery-lifecycle"
---

# Advice-Based Module Wiring in Elisp AI Systems

## Discovery

Phase 9 of the Emacs AI OS needed to wire 9 independent modules together:
agent registry, buffer memory, session capture, autonomous review, workflows,
chat, quick-ask, ambient mode, and the SDK. The naive approach — adding
explicit calls between modules — would create tight coupling and circular
dependencies. Instead, we discovered that `advice-add` serves as an
architectural wiring pattern: modules connect by advising each other's
functions without modifying source.

This is the software equivalent of the biological nervous system: signals
flow between organs (modules) through nerve pathways (advice) without
the organs needing to know about each other.

## Root Cause

Emacs Lisp's function dispatch is indirected through symbol lookup. When
you call `(some-function args)`, Emacs looks up the symbol's function slot
at runtime. `advice-add` wraps the original function with a new one,
intercepting every call site simultaneously. This means:

- Module A can react to Module B's functions without B knowing
- No registration API needed — just advise the function
- Multiple modules can advise the same function (composable)
- Advice is removable (idempotent install/uninstall)

## Solution

Three advice patterns emerged, each serving a distinct architectural role:

### 1. `:after` — Side Effects (Event Emission)

When module A needs to react to module B completing work:

```elisp
;; Session capture: log every tool call without modifying the tool system
(defun copilot-meta--on-tool-call (tool-name args result)
  "Track tool usage for session logging."
  (let ((session-data (gethash session-id copilot-meta--session-log)))
    (when session-data
      (push tool-name (plist-get session-data :tools-used)))))

(advice-add 'copilot-sdk-tools-dispatch :after #'copilot-meta--on-tool-call)
```

Used for: session capture, agent registration, usage tracking, metrics.

### 2. `:filter-return` — Data Enrichment

When module A needs to augment module B's return value:

```elisp
;; Auto-register every new session as an agent
(defun copilot-agent--on-session-create (session)
  "Register newly created session in agent registry."
  (when session
    (let ((id (copilot-sdk-session-id session))
          (project (copilot-sdk--project-name)))
      (copilot-agent-register id project "assistant")))
  session)  ; MUST return the session unchanged

(advice-add 'copilot-sdk-create-session :filter-return
            #'copilot-agent--on-session-create)
```

Used for: agent registration, context injection, result annotation.

### 3. `:around` — Behavior Modification

When module A needs to wrap module B's behavior:

```elisp
;; Inject buffer context into every chat auto-context call
(defun copilot-buffer--augment-auto-context (orig-fn)
  "Wrap auto-context to include buffer awareness."
  (let ((base-context (funcall orig-fn))
        (buffer-ctx (copilot-buffer-context-string)))
    (if buffer-ctx
        (concat base-context "\n" buffer-ctx)
      base-context)))

(advice-add 'copilot-chat--gather-editor-context :around
            #'copilot-buffer--augment-auto-context)
```

Used for: context injection, routing/dispatch, permission checks.

## The Idempotent Installer Pattern

Every module that uses advice follows this guard pattern:

```elisp
(defvar copilot-agent--auto-registration-active nil)

(defun copilot-agent-enable-auto-registration ()
  "Install advice hooks for agent auto-registration. Idempotent."
  (unless copilot-agent--auto-registration-active
    (when (fboundp 'copilot-sdk-create-session)
      (advice-add 'copilot-sdk-create-session :filter-return
                  #'copilot-agent--on-session-create))
    (when (fboundp 'copilot-sdk-cancel-session)
      (advice-add 'copilot-sdk-cancel-session :after
                  #'copilot-agent--on-session-end))
    (setq copilot-agent--auto-registration-active t)))
```

Key elements:
- **Guard variable** prevents double-installation
- **`fboundp` check** handles missing dependencies gracefully
- **Matching disable function** removes advice cleanly
- **Called from init** — not at load time (respects load order)

## Prevention / Best Practices

1. **Name advice functions with module prefix**: `copilot-buffer--augment-*`,
   `copilot-agent--on-*`. Makes `describe-function` advice list readable.

2. **`:filter-return` MUST return the value**: Forgetting to return breaks
   the advised function's callers. Always end with the original return value.

3. **Don't advise hot paths**: `advice-add` has ~nanosecond overhead per call
   but don't wrap functions called thousands of times per second (e.g.,
   `forward-char`). Tool dispatch and session creation are fine.

4. **Document advice in the advising module, not the target**: The target
   module shouldn't know it's being advised. The advising module should
   document which functions it advises and why.

5. **Prefer advice over hooks when**: the target function doesn't offer a
   hook, you need the return value, or you need `:around` wrapping. Prefer
   hooks when: the target explicitly provides one (like `after-change-functions`).

## Applicability

This pattern applies to any modular Elisp system where:
- Modules should be independently loadable
- Cross-module communication should be loose-coupled
- The wiring should be self-documenting (visible via `C-h f`)
- You want to add behavior without modifying existing code

In the AI OS, 5 modules use this pattern to wire 11 advice hooks, connecting
the entire stack without a single cross-module `require` for runtime behavior.
