---
title: "Pattern: Deterministic First, Intelligent Second"
description: "Design principle for AI+CLI systems: use Unix tools for exact work, LLMs for orchestration and interpretation"
entry_type: pattern
published_date: "2026-04-02"
last_updated_date: "2026-04-02"
tags: "architecture, ai-collaboration, patterns, unix, cli"
related_skill: ""
source_project: "copilot-sdk-elisp"
---

## Discovery

While designing an AI-native personal operating system in Emacs backed by GitHub Copilot CLI, we faced a fundamental question: what should the LLM do vs. what should traditional tools do? The temptation is to route everything through the LLM ("search for TODOs", "parse this JSON", "count lines"). But this is wrong — it's slower, less reliable, and non-deterministic.

The breakthrough came from the Unix philosophy: "Write programs that do one thing and do it well. Write programs to work together." The LLM is the NEW orchestration layer that replaces the human writing shell scripts — but the tools themselves stay deterministic.

## Root Cause

LLMs are probabilistic. They estimate, approximate, and occasionally hallucinate. Unix tools are deterministic — `grep` finds EXACTLY what matches, `wc -l` counts EXACTLY the right number of lines, `jq` parses JSON with ZERO errors. Using an LLM for deterministic tasks is like using a poet to do arithmetic.

The confusion arises because LLMs CAN do these tasks — they just shouldn't. The value of the LLM is deciding WHICH tools to use, HOW to combine them, and WHAT the results mean.

## Solution

### The Two-Layer Architecture

```
┌─────────────────────────────────────────────────────────┐
│  INTELLIGENT LAYER (LLM / Copilot CLI)                   │
│  "What should I do? Which tools? What does output mean?" │
│  → Orchestration, interpretation, reasoning, creativity  │
│  → PROBABILISTIC: smart but not always exact             │
├─────────────────────────────────────────────────────────┤
│  DETERMINISTIC LAYER (Unix tools + language primitives)   │
│  "Do the actual work. Reliably. Every time."             │
│  → grep, sed, awk, curl, jq, git, make, find, sort...  │
│  → DETERMINISTIC: fast, auditable, composable, trusted   │
└─────────────────────────────────────────────────────────┘
```

### Decision Table

| Task | Use This (Deterministic) | NOT This (LLM) | Why |
|------|--------------------------|-----------------|-----|
| Find pattern in files | `rg "TODO" --type py` | "Search for TODOs" | grep is instant, exact, complete |
| Parse JSON | `jq '.items[].name'` | "Extract the names" | jq is deterministic, LLM may hallucinate fields |
| Count lines | `wc -l` | "How many lines?" | wc is exact, LLM estimates |
| Git diff | `git diff HEAD~1` | "What changed?" | git is authoritative |
| HTTP request | `curl -s api.example.com` | "Fetch the data" | curl handles HTTP correctly |
| Text replace | `sed 's/old/new/g'` | "Replace old with new" | sed is atomic, predictable |
| Sort/filter | `sort \| uniq -c \| head` | "Most common?" | sort is O(n log n), exact |

### But the LLM ORCHESTRATES these tools

```
User: "Find all security-related TODOs, prioritize by severity, create org tasks"

LLM decides:
  1. rg "TODO.*security\|FIXME.*security\|HACK.*auth" --type py  (deterministic)
  2. Interprets results, classifies severity  (intelligent)
  3. Calls org-capture to create tasks  (deterministic)
  4. Summarizes findings to user  (intelligent)
```

### In Emacs (deftool wraps CLI tools trivially)

```elisp
(copilot-sdk-deftool ripgrep-search
  "Search project files using ripgrep (deterministic, fast)"
  (:pattern "Regex" :file-type "Optional type filter")
  (shell-command-to-string
   (format "rg %s %s ."
           (if file-type (format "--type %s" file-type) "")
           (shell-quote-argument pattern))))
```

The agent calls `ripgrep-search` (deterministic, fast, exact).
The agent interprets the results (intelligent, contextual).
Best of both worlds.

## Prevention

When building any AI agent system:

1. **Before adding an LLM tool call, ask**: "Can a Unix tool do this exactly?" If yes, wrap the Unix tool instead.
2. **Audit existing LLM tool calls**: Are any doing deterministic work? Replace them with tool wrappers.
3. **Compose, don't replace**: The LLM decides which tools and in what order. The tools do the work.
4. **Audit trail**: Log all tool invocations. Deterministic tools produce reproducible results you can verify.
5. **Test deterministically**: Unit test the tool wrappers with known inputs/outputs. The LLM orchestration can be tested separately.

This principle applies universally — not just Emacs, not just Copilot CLI. Any system combining LLMs with shell/CLI tools benefits from this separation.
