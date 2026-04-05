---
title: "Project Report: copilot-sdk-elisp — Building the Emacs AI OS"
description: "Built an Emacs Lisp SDK for GitHub Copilot via ACP protocol, evolved from chat plugin to AI-native personal operating system vision"
entry_type: project-report
published_date: "2026-04-02 00:00 +00:00"
last_updated_date: "2026-04-02 00:00 +00:00"
tags: "copilot-sdk-elisp, emacs, elisp, copilot, acp, ai-collaboration"
related_skill: ""
source_project: "copilot-sdk-elisp"
related_entries: "research-emacs-personal-os-ecosystem, research-acp-vs-sdk-protocol-copilot-cli, pattern-agent-as-unix-citizen, pattern-deterministic-first-intelligent-second, pattern-emacs-ai-operating-system-architecture"
---

## Objective

Build native agentic GitHub Copilot capabilities in Emacs, leveraging Lisp's metaprogramming to create something no other editor can offer — a self-modifying AI-native personal operating system.

## Approach

### Phase 1: Research & Architecture Decision

Evaluated two approaches:
- **CLI-in-terminal**: Run `copilot` in eshell/vterm. Zero development but zero integration.
- **Native Elisp SDK**: Speak JSON-RPC directly to Copilot CLI in server mode.

Chose the Elisp SDK approach because Emacs uniquely offers: eval (self-modification), advice system (hook into everything), macros (compress tool authoring), full introspection, and a rich ecosystem of subsystems (email, calendar, etc.).

### Phase 2: Protocol Discovery

**Critical discovery**: Copilot CLI supports TWO protocols:
1. **SDK protocol** (`--headless --stdio`): Hidden flags, Content-Length framed JSON-RPC, used by copilot-sdk mono-repo. Broke in CLI v0.0.410.
2. **ACP protocol** (`--acp --stdio`): Public flags, NDJSON JSON-RPC, full spec at agentclientprotocol.com. Industry standard.

Initially built against the SDK protocol (using Emacs built-in `jsonrpc.el`), then discovered ACP and fully migrated. The NDJSON transport is only ~50 lines of Elisp.

### Phase 3: Implementation (5 commits, 14 files, ~3000 lines)

| Commit | What |
|--------|------|
| `7902593` | Initial implementation: core, chat, tools, meta, workflows |
| `d2227fe` | Migrate from SDK headless to ACP protocol |
| `5f88cef` | Fix log level: `warning` not `warn` |
| `518b344` | Fix keymap: `text-mode` not `special-mode` |
| `8522c2c` | Fix "You>" duplication, ACP permissions, markdown rendering |

**What was built:**
- Core ACP client with NDJSON transport, initialize, session/new, session/prompt, session/cancel, session/load
- Chat buffer with streaming text, tool visualization, markdown faces
- 14 built-in Emacs tools (buffer ops, files, git, shell, org-babel, eval)
- `deftool` macro: one line creates function + JSON schema + tool registration
- `defworkflow` macro: multi-step agentic workflows
- Advice hooks for automatic agent triggers
- 23 ERT tests, all passing

### Phase 4: Runtime Debugging in WSL

Tested on WSL Debian with Emacs 28.2 and Copilot CLI v1.0.16 (native Linux binary). Fixed three runtime bugs:
1. **"You>" prompt duplicating**: Added prompt-start marker, commit-input deletes from marker
2. **Permission showing "unknown"**: ACP sends `toolCall.toolCallId`, not `toolName`. Track ID→title via tool_call events.
3. **Permission approval doing nothing**: ACP expects `{outcome: "selected", optionId: "allow-once"}`, not `{outcome: "approved"}`

### Phase 5: Architecture Evolution

The vision expanded from "chat plugin" to "AI Operating System":

**Five-layer architecture:**
1. **Life Layer** — Email, calendar, finance, habits, journal, RSS, writing
2. **GTD Layer** — org-agenda strategic tasks
3. **Knowledge Layer** — org-roam knowledge graph
4. **Agent Layer** — Copilot CLI (intelligent orchestrator)
5. **Body Layer** — Emacs (capabilities, display, self-modification)

**Foundational principle**: Deterministic first, intelligent second. Unix tools do the work. The LLM orchestrates.

## Outcome

**Delivered:**
- Working Emacs Copilot SDK via ACP protocol
- Live chat with streaming, tool approval, markdown rendering
- 14 tools + deftool macro for extensibility
- Complete architectural vision for 6 phases, 34 features

**Planned (34 todos):**
- Phase 1: Self-modification (code blocks, eval, MCP bridge)
- Phase 2: Context injection (resources, @-mentions, auto-context)
- Phase 3: Cognitive workspace (plan buffer, tool buffer, layout)
- Phase 4: Memory (org-roam + org GTD)
- Phase 5: Life OS (email, calendar, finance, habits, journal, RSS, research, writing, contacts)
- Phase 6: Unix citizen (CLI wrappers, dotfile context, .plan, pipes, tool discovery)

## Lessons Learned

### What Worked
1. **ACP over SDK protocol** — documented, stable, future-proof. Worth the NDJSON transport effort.
2. **Separate repo** — Elisp packages belong in their own repos (MELPA, community culture, tooling).
3. **deftool macro** — one line to create a function AND register it as a tool. Lisp's power.
4. **Iterative debugging** — test in WSL, fix bugs one at a time, commit each fix separately.

### What Didn't Work
1. **Starting with SDK protocol** — cost a full rewrite when we discovered ACP.
2. **`special-mode` for chat** — suppresses all key input (read-only default). Should have used `text-mode` from the start.
3. **Assuming field names** — ACP permission handler uses `toolCall.toolCallId`, not `toolName`. Must read the actual protocol data, not guess.

### What We'd Do Differently
1. **Start with ACP from day one** — run `copilot --help` first, notice `--acp` is public.
2. **Test with real CLI earlier** — unit tests passed but runtime had three bugs. Integration testing sooner would have caught them.
3. **Read existing `acp.el` (141 ⭐) first** — could have studied its NDJSON transport and UI patterns before building our own.

### Key Technical Gotchas
- CLI rejects `--log-level warn` — must be `warning` (the full word)
- Stale `.elc` byte-compiled files cause `void-function` errors when structs change — always `rm *.elc` after modifying struct definitions
- The npm shim for copilot at `/mnt/c/.../npm/copilot` is BROKEN in WSL (tries to run Node with Windows paths) — must set `copilot-sdk-cli-path` explicitly to the native binary
- `executable-find` in Emacs finds the broken npm shim first — explicit path required
