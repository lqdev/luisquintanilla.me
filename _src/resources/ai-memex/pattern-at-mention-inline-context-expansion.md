---
title: "@-Mention Inline Context Expansion for AI Chat"
description: "Pattern for expanding @file:, @buffer:, @git-diff references into XML context blocks before sending to an AI agent"
entry_type: pattern
published_date: "2026-04-04"
last_updated_date: "2026-04-04"
tags: "emacs, elisp, ai-collaboration, patterns, architecture"
related_skill: ""
source_project: "copilot-emacs"
related_entries: "pattern-async-cancel-race-elisp, project-report-conscious-emacs-life-os-daily-driver, pattern-emacs-ai-operating-system-architecture"
---

# @-Mention Inline Context Expansion for AI Chat

## Discovery

When chatting with an AI agent inside Emacs, users frequently need to reference
specific files, buffers, or git state. Typing "look at my init.el" forces the
agent to use a tool call to read the file — adding latency and a round trip.
The user already knows WHAT context they want injected.

## Root Cause

AI chat interfaces typically send the user's message verbatim. Context injection
(auto-context, skills, editor state) happens at the system level. But there's no
mechanism for **user-directed inline context** — "include THIS specific thing."

This is the gap between:
- **System context** (auto-injected, user doesn't control)
- **Tool calls** (agent-initiated, adds latency)
- **User-directed context** (user specifies, expanded before sending) ← missing

## Solution

A regex-based expansion pipeline that runs on the user's input BEFORE sending to
the agent. Mentions are replaced with XML blocks containing the referenced content.

### Supported Mentions

| Mention | Expands To |
|---------|-----------|
| `@file:path/to/file` | `<mentioned_file path="...">contents</mentioned_file>` |
| `@buffer:*scratch*` | `<mentioned_buffer name="...">contents</mentioned_buffer>` |
| `@git-diff` | `<git_diff>staged or unstaged diff</git_diff>` |
| `@git-log` | `<git_log>last 20 commits</git_log>` |

### Implementation

```elisp
(defconst copilot-chat--mention-max-size 10240
  "Maximum bytes to inline from @-mention expansion.")

(defun copilot-chat--expand-mentions (text)
  "Expand @-mentions in TEXT and return augmented string."
  (let ((result text))
    ;; @file:path
    (setq result
          (replace-regexp-in-string
           "@file:\\([^ \t\n]+\\)"
           (lambda (match)
             (let* ((path (match-string 1 match))
                    (expanded (expand-file-name path)))
               (if (file-readable-p expanded)
                   (let ((content (with-temp-buffer
                                    (insert-file-contents expanded nil 0
                                                          copilot-chat--mention-max-size)
                                    (buffer-string))))
                     (format "\n<mentioned_file path=\"%s\">\n%s\n</mentioned_file>\n"
                             expanded content))
                 (format "[file not found: %s]" path))))
           result t t))
    ;; ... similar for @buffer:, @git-diff, @git-log ...
    result))
```

### Key Design Decisions

1. **XML wrapping**: Uses semantic XML tags (`<mentioned_file>`, `<git_diff>`)
   so the AI agent understands WHAT the context is, not just raw text.

2. **Size cap**: 10KB per mention prevents accidentally injecting a 500MB log file.
   Truncated with "…" indicator.

3. **Graceful failure**: Missing files show `[file not found: path]` inline instead
   of erroring. Missing buffers show `[buffer not found: name]`.

4. **Git diff fallback**: `@git-diff` tries `--staged` first, falls back to
   unstaged. This matches the common "what am I about to commit?" use case.

5. **User sees original**: The chat buffer shows the user's raw `@file:init.el`
   message. The expanded version goes to the agent silently.

### CAPF Completion

For discoverability, a `completion-at-point-functions` entry triggers on `@`:

```elisp
(defun copilot-chat--mention-completion-at-point ()
  "CAPF for @-mentions in chat input."
  (when (looking-back "@\\([^ \t\n]*\\)" (line-beginning-position))
    (let* ((start (match-beginning 0))
           (end (point))
           (prefix (match-string 1))
           (candidates
            (cond
             ((string-prefix-p "file:" prefix)
              ;; Complete with project files
              ...)
             ((string-prefix-p "buffer:" prefix)
              ;; Complete with buffer names
              ...)
             (t '("file:" "buffer:" "git-diff" "git-log")))))
      (when candidates
        (list start end
              (mapcar (lambda (c) (concat "@" c)) candidates)
              :exclusive 'no)))))
```

### Prompt Build Order

The full prompt assembly chain:

```
[user_identity] + [user_plan] + [active_skills] + [editor_context] + [expanded_mentions + user_text]
```

Each layer is independently optional — `concat` ignores nil args.

## Prevention

When building @-mention systems:

1. **Cap content size aggressively.** Users will `@file:` a binary or huge log.
   10KB is generous for context; 500 chars is enough for a preview.

2. **Use XML tags, not markdown.** Agents parse XML structure more reliably than
   freeform text. Tags like `<mentioned_file path="...">` give both content and
   metadata.

3. **Show the original to the user.** Don't expand in the visible chat — it's
   noisy. The user wrote `@file:init.el`, they don't need to see 200 lines of
   Elisp in the chat buffer.

4. **Test with missing references.** `@buffer:*nonexistent*` and
   `@file:/no/such/path` must degrade gracefully, not crash.

5. **CAPF makes it discoverable.** Without completion, users won't know what
   mention types exist. `@` + TAB should show all options.

## Applicability

This pattern applies to any AI chat interface that wants user-directed context
injection. The same approach works in VS Code extensions, terminal UIs, web apps —
anywhere users want to say "look at THIS" without waiting for a tool call round trip.
