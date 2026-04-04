---
title: "The Agentic Leap — Diff Preview, Unix Pipes, Compound Workflows"
description: "Milestone report: three capabilities that transformed the Emacs AI OS from impressive project to daily driver (92% → 97%)"
entry_type: project-report
published_date: "2026-04-04 00:00 +00:00"
last_updated_date: "2026-04-04 00:00 +00:00"
tags: "emacs, ai-collaboration, architecture, patterns, project-report"
related_skill: ""
source_project: "copilot-sdk-elisp"
related_entries: "pattern-diff-preview-as-trust-mechanism, project-report-conscious-emacs-life-os-daily-driver, project-report-copilot-sdk-elisp-emacs-ai-os"
---

# The Agentic Leap — Diff Preview, Unix Pipes, Compound Workflows

## Objective

Bridge the gap between "impressive demo" and "daily driver" for the Emacs AI
Operating System. Three capabilities were identified as the critical 8% separating
a good project from one you can't live without:

1. **Trust** — Users need to see changes before they land (diff preview)
2. **Composability** — The agent should compose deterministic pipelines, not
   burn LLM turns for each step (unix pipes)
3. **Orchestration** — Compound workflows that chain multiple tools into
   coherent daily routines (plan-day, research-capture)

## Approach

Parallel implementation across two repos (SDK + App), 9 tasks organized in
dependency waves:

**Wave 1 (parallel, no dependencies):**
- Fix journal format mismatch (critical bug blocking Life OS)
- Create `copilot-diff.el` (new 523-line file)
- Create unix-pipe + system-tools (SDK additions)
- Integrate diff preview with buffer-edit tool

**Wave 2 (depends on Wave 1):**
- Compound workflows using fixed journal tools
- Tests covering all new features

## Outcome

### Diff Preview Mode (`copilot-diff.el` — 523 lines)
- Side-window display with syntax-highlighted diffs
- Accept (`C-c C-c`), Reject (`C-c C-k`), Edit (`e`) response modes
- Multi-hunk navigation with `n`/`p` keys
- Auto-accept timer for power users
- Batch mode fallthrough (tests don't hang)

### Unix Citizen Tools
- `unix-pipe`: compose shell pipelines with safety guards
  - Configurable allowlist (grep, sort, wc, head, tail, cat, etc.)
  - Dangerous pattern rejection (rm -rf, dd if=, fork bombs)
  - 30s timeout, output truncation at 100K chars
- `system-tools`: discovers 24 CLI tools with version info

### Compound Workflows
- `copilot-plan-day`: gathers agenda + habits + feeds + journal + tasks + .plan,
  sends to agent for priority synthesis with tool-backed persistence
- `copilot-research`: multi-source search (org-roam + elfeed + codebase + journal)
  → agent synthesis → optional org-roam node creation

### Journal Format Fix
- Root cause: hardcoded `%Y%m%d` vs user's `%Y-%m-%d.org` format
- Fix: 3 helper functions read `org-journal-file-format` dynamically
- All 3 journal tools (today/search/write) now use correct format

### Test Coverage
- 15 new tests added (6 SDK + 9 App)
- **91 total tests passing** (57 SDK + 34 App)
- Smoke test guide expanded: 116 test scenarios across 25 phases

## Scorecard

```
Phase 1: Self-modification    ✅ 100%
Phase 2: Context injection    ✅ 100%
Phase 3: Cognitive workspace     98%  (diff preview added)
Phase 4: Memory               ✅ 100%
Phase 5: Life OS                 95%  (bug fixes + compound workflows)
Phase 6: Unix citizen            85%  (pipes + discovery added)
Overall:                    92% → 97%
```

## Lessons Learned

1. **Parallel implementation works** — Running 4 background agents on independent
   tasks (diff, unix-pipe, journal fix, integration) cut implementation time
   dramatically. The dependency graph made this safe.

2. **Batch fallthrough is non-negotiable** — Any interactive feature that blocks
   in tests will make CI unusable. Always check `noninteractive` first.

3. **Format mismatches hide in plain sight** — The journal bug existed for sessions
   because nobody thought to check `org-journal-file-format` vs the hardcoded
   format string. Dynamic reading of user config is always better.

4. **Compound workflows are just the gather→prompt→stream pattern** — Once you
   have one working workflow (morning briefing), new ones are just different
   data gathering functions + different system prompts. The pattern is the
   architecture.

5. **The trust gradient matters more than features** — Diff preview is conceptually
   simple but has outsized UX impact. Users who wouldn't ask the agent to edit code
   now can. This unlocks everything else.
