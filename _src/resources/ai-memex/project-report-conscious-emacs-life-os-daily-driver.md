---
title: "The Conscious Emacs: Life OS + Daily Driver Shipped"
description: "Milestone report covering Life OS tool wrappers, morning briefing, @-mentions, cancel fix, and 76 tests across two repos"
entry_type: project-report
published_date: "2026-04-04 00:00 +00:00"
last_updated_date: "2026-04-04 00:00 +00:00"
tags: "emacs, elisp, ai-collaboration, architecture, patterns"
related_skill: ""
source_project: "copilot-sdk-elisp"
related_entries: "pattern-async-cancel-race-elisp, pattern-at-mention-inline-context-expansion, project-report-the-agentic-leap"
---

# The Conscious Emacs: Life OS + Daily Driver Shipped

## Objective

Take the Emacs AI OS from "working prototype" to "daily driver" by fixing UX pain
points discovered during smoke testing, then build the Life Layer — the top of the
5-layer stack — so the AI agent can compose across ALL Emacs subsystems.

## Approach

Two back-to-back plans executed in a single session:

### Plan 1: Daily Driver UX Hardening (8 items)

Addressed every friction point from real-world testing:

1. **Cancel race condition** — async tool events were flipping state back after
   `C-c C-k`. Fixed with a `--cancelled` buffer-local flag guarding all event handlers.

2. **Multi-line input** — `C-j` inserts newline, `RET` sends. Standard Emacs convention.

3. **Model in mode-line** — `🤖 idle [gpt-4.1]` so you always know which model.

4. **Tool result preview** — truncated result shown after `✓ Done` (500 char cap).

5. **@-mentions** — `@file:path`, `@buffer:name`, `@git-diff`, `@git-log` expand
   inline to XML blocks (10KB cap). CAPF completion for discoverability.

6. **Dotfile identity** — `git config --global user.name/email` injected as
   `<user_identity>` XML in every prompt.

7. **Session auto-naming** — first prompt saved as `.topic` file, used as fallback
   in session resume picker.

### Plan 2: The Conscious Emacs (7 items)

Built the Life Layer with a key design principle: **multi-surface integration**.
Not everything lives in chat — the system spans chat buffer, dedicated buffers,
minibuffer, and existing Emacs modes.

**10 new `deftool` wrappers** making existing Emacs subsystems agent-accessible:

- **elfeed** (3): `elfeed-search`, `elfeed-unread`, `elfeed-refresh`
- **org-habit** (2): `habits-due-today`, `habit-log`
- **org-journal** (3): `journal-today`, `journal-search`, `journal-write`
- **org-contacts** (2): `contacts-search`, `contacts-create`

All `fboundp`-guarded — if the package isn't loaded, the tools simply don't register.

**Morning Briefing** — the crown jewel:

```
M-x copilot-morning-briefing  (or C-c C-p b)
```

Creates a dedicated `*Copilot Briefing*` buffer (read-only, custom mode) that:
1. Gathers data from ALL subsystems (agenda, habits, feeds, journal, .plan)
2. Renders raw sections immediately
3. Streams AI synthesis with suggested priorities

Also: `copilot-evening-review`, `copilot-habit-log` (completing-read),
`copilot-journal-quick` (minibuffer).

**App test suite** — went from 13 to 25 tests covering @-mentions, context
injection, briefing mode, and CAPF.

## Outcome

### Numbers

| Metric | Before | After |
|--------|--------|-------|
| SDK tests | 48 | 51 |
| App tests | 13 | 25 |
| Total tests | 61 | **76** |
| Total tools | ~31 | **41+** |
| Commits | — | 3 |

### Phase Scorecard

```
Phase 1: Self-modification    ✅ 100%
Phase 2: Context injection    ✅ ~100%  (was ~80%)
Phase 3: Cognitive workspace  ~90%
Phase 4: Memory               ✅ 100%
Phase 5: Life OS              ~60%      (was 0%)
Phase 6: Unix citizen         ~60%      (was ~50%)
Overall:                      ~90%
```

### Architecture

The 5-layer stack is now fully wired:

```
Life Layer      → elfeed, org-habit, org-journal, org-contacts    ✅ NEW
GTD Layer       → org-agenda, org-capture, org-todo               ✅
Knowledge Layer → org-roam search/read/create/backlinks           ✅
Agent Layer     → Copilot CLI via ACP, MCP bridge, skills         ✅
Body Layer      → Emacs tools, eval, self-modification            ✅
```

The morning briefing demonstrates emergent composition — one command that
traverses ALL five layers without any of them knowing about each other.

## Lessons Learned

1. **Async event races are subtle in Elisp.** Process filters fire at any time.
   A simple boolean flag (`--cancelled`) was more robust than trying to debounce
   or sequence events.

2. **Multi-surface > chat-only.** Users expect Emacs-native interactions: dedicated
   buffers with modes, completing-read for quick actions, minibuffer for one-liners.
   Chat is for conversation, not for everything.

3. **`fboundp` guards make optional dependencies trivial.** The agent gains
   capabilities as you install packages, with zero configuration.

4. **The elfeed API requires manual filtering.** `elfeed-search--update-list` is
   internal — the correct approach is `elfeed-search-parse-filter` + 
   `elfeed-db-get-all-entries` + `seq-filter`.

5. **76 tests across two repos gives real confidence.** The app repo having zero
   tests was a risk — the 12 new @-mention tests immediately caught an edge case
   with buffer names containing special characters.
