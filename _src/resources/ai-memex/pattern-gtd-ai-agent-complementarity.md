---
title: "Pattern: GTD + AI Agent Complementarity"
description: "How org-agenda strategic tasks and Copilot CLI tactical plans are complementary time horizons, not redundant systems"
entry_type: pattern
published_date: "2026-04-02 00:00 +00:00"
last_updated_date: "2026-04-02 00:00 +00:00"
tags: "org-mode, gtd, ai-collaboration, patterns, emacs, productivity"
related_skill: ""
source_project: "copilot-sdk-elisp"
related_entries: "pattern-emacs-ai-operating-system-architecture, research-emacs-personal-os-ecosystem, ai-collaboration-patterns"
---

## Discovery

When adding org-mode GTD integration to our Emacs Copilot SDK, we faced an apparent redundancy: Copilot CLI already generates execution plans ("Step 1: Read file. Step 2: Fix bug. Step 3: Run tests."). Why does the agent ALSO need org-agenda access? Aren't these the same thing?

They're not. They operate at completely different time horizons and serve different purposes.

## Root Cause

The confusion arises from using the word "plan" for both:

| | Copilot CLI Plan | Org-Agenda Task |
|--|-----------------|-----------------|
| **Time horizon** | 30 seconds to 5 minutes | Days, weeks, months |
| **Scope** | "Fix this bug right now" | "Refactor the auth system" |
| **Persistence** | Gone after the turn ends | Persists across sessions, weeks, months |
| **Detail level** | Specific code operations | Strategic goals with deadlines |
| **Tracking** | Not tracked after completion | TODO states, time clocking, effort estimates |
| **Dependencies** | None (single turn) | Blocked tasks, sequencing, priorities |
| **Example** | "1. Read auth.js 2. Find MD5 call 3. Replace with bcrypt 4. Run tests" | "TODO Refactor auth system :security: DEADLINE:<2026-04-15>" |

The CLI plan EXECUTES within an org task. Org is the roadmap. Copilot is the driver.

## Solution

### Three Complementary Time Horizons

```
ORG-ROAM (permanent)     "JWT: always validate exp claim"
  Knowledge that never expires. Patterns, research, decisions.
  The agent QUERIES this before starting work.

ORG-AGENDA (strategic)    "TODO Refactor auth  DEADLINE:<Apr 15>"
  Tasks that span days/weeks. Priorities, deadlines, clocking.
  The agent READS this to know what to work on.
  The agent WRITES this to track progress.

COPILOT PLAN (tactical)   "Step 1: Read file. Step 2: Fix bug."
  Per-turn execution plan. Gone after the turn.
  The agent GENERATES this to organize its immediate work.
```

### The GTD + Agent Meta-Loop

David Allen's GTD (Getting Things Done) has 5 stages. Here's how each maps to AI assistance:

**1. Capture** — Agent discovers issues while working:
```
Agent finds SQL injection → creates org capture:
  "TODO Fix SQL injection in auth.js :security:urgent:"
```

**2. Clarify** — Agent helps assess captured items:
```
"This TODO is vague. Based on the code, the specific issue is
 unsanitized user input in the login query on line 47."
```

**3. Organize** — Agent suggests priority, deadline, effort:
```
"This is a security vulnerability. I'd recommend:
 Priority: A, Deadline: this week, Effort: 2h"
```

**4. Reflect** — Agent facilitates weekly review:
```
"Here's your week: 5 tasks completed, 2 still open.
 The auth refactor is 60% done (3/5 subtasks).
 You clocked 12h this week, 4h on security items."
```

**5. Engage** — Agent executes within the chosen task:
```
User selects "Add rate limiting" from org-agenda
→ Agent clocks in
→ Agent generates tactical plan for this specific task
→ Agent executes (code changes, tests, etc.)
→ Agent clocks out, marks DONE, adds notes
```

### Practical Tool Integration

```elisp
;; Agent queries today's tasks
(copilot-sdk-deftool org-agenda-today
  "Get today's actionable tasks from org-agenda"
  ()
  (let ((tasks '()))
    (org-map-entries
     (lambda ()
       (push (list (cons 'heading (org-get-heading t t t t))
                   (cons 'state (org-get-todo-state))
                   (cons 'priority (org-get-priority))
                   (cons 'tags (org-get-tags)))
             tasks))
     "TODO=\"TODO\"+SCHEDULED<=\"<today>\"")
    (json-encode (nreverse tasks))))

;; Agent updates task state
(copilot-sdk-deftool org-todo-update
  "Change a task's TODO state"
  (:heading "Task heading" :new-state "New state (DONE, IN-PROGRESS, etc.)")
  (org-map-entries
   (lambda () (org-todo new-state))
   (format "TODO={.*}+ITEM={%s}" heading))
  (format "Updated '%s' to %s" heading new-state))

;; Agent clocks time
(copilot-sdk-deftool org-clock-in-task
  "Start time tracking on a task"
  (:heading "Task heading to clock into")
  (org-map-entries #'org-clock-in
   (format "ITEM={%s}" heading))
  (format "Clocked in to '%s'" heading))
```

### Session-Start Awareness

```elisp
;; When chat opens, inject agenda context:
(defun copilot-sdk--inject-agenda-context ()
  "Build context string from today's org-agenda."
  (format "Today's tasks:\n%s\n\nOverdue:\n%s"
          (copilot-sdk-tool--org-agenda-today)
          (copilot-sdk-tool--org-agenda-overdue)))
```

The agent starts every session knowing your priorities.

## Prevention

When integrating AI agents with task management systems:

1. **Don't conflate time horizons** — the agent's per-turn plan and your weekly roadmap are different things serving different purposes
2. **Let the agent READ tasks** — it should know what you're working on and what's coming up
3. **Let the agent WRITE task updates** — clock time, change states, add notes, create new captures
4. **Don't automate the strategic layer** — the agent helps you EXECUTE tasks, but YOU choose which tasks matter. Keep human agency over priorities and goals.
5. **Bridge the layers** — the agent's tactical plan should reference the strategic task it's executing within. "I'm working on the auth refactor (3/5 subtasks done, deadline April 15)."
