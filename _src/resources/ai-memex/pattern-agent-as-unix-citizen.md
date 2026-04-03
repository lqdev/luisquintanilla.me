---
title: "Pattern: Agent as Unix Citizen"
description: "Design pattern where an AI agent joins the Unix ecosystem by wrapping CLI tools as callable functions rather than replacing them"
entry_type: pattern
published_date: "2026-04-02"
last_updated_date: "2026-04-02"
tags: "unix, cli, ai-collaboration, patterns, emacs, elisp"
related_skill: ""
source_project: "copilot-sdk-elisp"
---

## Discovery

When building Emacs tools for an AI agent (Copilot CLI via ACP), we initially considered having the agent generate raw shell commands. This felt dangerous and unpredictable. The better pattern emerged: wrap each Unix tool as a structured, typed tool the agent calls by name — the agent becomes a citizen of the Unix ecosystem, not a foreign invader.

## Root Cause

The tension between AI agents and Unix tools is philosophical:
- Unix tools are **deterministic**: same input → same output, always
- AI agents are **probabilistic**: smart but not always exact

Naively mixing them (agent generates arbitrary shell commands) creates unpredictable behavior. But separating them into clean layers — where the agent DECIDES and Unix tools EXECUTE — preserves the strengths of both.

## Solution

### The Agent as a Unix User

Think of the agent as a very skilled Unix user who:
1. Knows which tools exist on the system
2. Knows the right tool for each job
3. Composes tools into pipelines
4. Interprets the results
5. Asks for permission before destructive operations

### Tool Discovery

```elisp
(copilot-sdk-deftool discover-tools
  "Discover CLI tools available on this system"
  ()
  (let ((tools '(("rg" . "ripgrep") ("fd" . "fd-find")
                 ("bat" . "bat") ("jq" . "jq")
                 ("fzf" . "fzf") ("pandoc" . "pandoc")
                 ("hledger" . "hledger") ("task" . "taskwarrior"))))
    (json-encode
     (cl-loop for (cmd . desc) in tools
              when (executable-find cmd)
              collect (cons cmd desc)))))
```

The agent adapts: if `rg` exists, use ripgrep. If only `grep`, use grep.

### Wrapping CLI Tools (5 lines each)

```elisp
(copilot-sdk-deftool ripgrep-search
  "Search files using ripgrep (deterministic, fast)"
  (:pattern "Regex" :type "File type filter")
  (shell-command-to-string
   (format "rg --json %s %s ."
           (if type (format "--type %s" type) "")
           (shell-quote-argument pattern))))

(copilot-sdk-deftool jq-query
  "Process JSON with jq (deterministic)"
  (:input "JSON string" :filter "jq expression")
  (shell-command-to-string
   (format "echo %s | jq %s"
           (shell-quote-argument input)
           (shell-quote-argument filter))))
```

### Pipeline Composition

```elisp
(copilot-sdk-deftool unix-pipeline
  "Execute a composed Unix pipeline with audit logging"
  (:commands "Array of shell commands to pipe together"
   :description "What this pipeline does")
  ;; Each component is deterministic
  ;; The composition is intelligent (agent decided this pipeline)
  (let ((pipeline (mapconcat #'identity commands " | ")))
    (copilot-sdk--audit-log "pipeline" pipeline description)
    (shell-command-to-string pipeline)))
```

Agent composes: `rg "TODO" --type py | sort | uniq -c | sort -rn | head -10`
Each command is deterministic. The agent chose the combination.

### The Flow

```
User: "What are the most common error patterns in my Python code?"

Agent (intelligent) decides:
  1. Call ripgrep-search with pattern "except.*:" and type "py"
  2. Parse results (intelligent interpretation)
  3. Call unix-pipeline: ["sort", "uniq -c", "sort -rn", "head -20"]
  4. Interpret aggregated results (intelligent synthesis)
  5. Cross-reference with org-roam knowledge graph
  6. Present findings with recommendations
```

### Security: Allowlists and Audit

```elisp
(defcustom copilot-sdk-unix-allowed-commands
  '("rg" "grep" "fd" "find" "cat" "bat" "wc" "sort" "uniq"
    "head" "tail" "jq" "sed" "awk" "diff" "git" "curl")
  "Commands the agent may invoke without explicit approval.")

(defcustom copilot-sdk-unix-dangerous-commands
  '("rm" "mv" "cp" "chmod" "chown" "sudo" "dd" "mkfs")
  "Commands that ALWAYS require explicit user approval.")
```

## Prevention

When integrating AI agents with shell/CLI environments:

1. **Wrap, don't expose raw shell** — typed tools with parameter validation, not arbitrary command execution
2. **Discover, don't assume** — check which tools exist on the system before using them
3. **Allowlist, don't blocklist** — enumerate safe commands rather than trying to block dangerous ones
4. **Audit everything** — log every tool invocation with timestamp, command, and results
5. **Adapt gracefully** — if the preferred tool is missing, fall back to alternatives (rg → grep, fd → find)
6. **The agent composes, tools execute** — the intelligent layer decides WHICH tools and HOW; the deterministic layer does the actual work
