---
title: "Pattern: MCP Bridge for Exposing Client-Side Tools to Copilot Agent"
description: "How to make the Copilot CLI agent discover and call tools registered in a host application by building a Python MCP stdio bridge and injecting it via --additional-mcp-config"
entry_type: pattern
published_date: "2026-04-03"
last_updated_date: "2026-04-03"
tags: "copilot, mcp, acp, emacs, elisp, architecture, patterns, ai-collaboration"
related_skill: ""
source_project: "copilot-sdk-elisp"
related_entries: "research-acp-vs-sdk-protocol-copilot-cli, pattern-tool-nursery-lifecycle, pattern-emacs-ai-operating-system-architecture"
---

## Discovery

When building an Elisp SDK for GitHub Copilot using the ACP (Agent Client Protocol), we registered 14+ tools (buffer operations, git, shell, eval, etc.) in Emacs and needed the Copilot CLI agent to discover and call them. The agent would respond "I don't have access to your editor's buffers" even though we had tools registered — it simply couldn't see them.

The ACP protocol defines tool registration, but the Copilot CLI v1 protocol doesn't support passing tools in `session/new` for the agent to discover. We spent multiple sessions trying different formats before finding the working approach.

## Root Cause

The Copilot CLI has **two separate mechanisms** for tool awareness:

1. **ACP session/new `mcpServers` field**: Creates the session successfully but the CLI **does not spawn or connect to** session-level MCP servers. The field is accepted in the ACP spec but not implemented for tool discovery in CLI v1.

2. **MCP config sources** (external to ACP): The CLI reads MCP server configurations from files and CLI flags, spawns them as subprocesses, and makes their tools available to the agent.

The tools need to arrive via an **MCP server**, not as ACP session parameters.

### What We Tried (and results)

| Approach | Result |
|----------|--------|
| `session/new` with `tools: [...]` | Hangs/timeout — v1 doesn't support `tools` field |
| `session/new` with `mcpServers` (object format) | Error: "expected array, received object" |
| `session/new` with `mcpServers` (array, ACP spec) | Session creates, but agent can't see tools |
| `session/new` with env as object | Error: "Internal error" |
| `session/new` with env as `[{name, value}]` array | Session creates, tools still invisible |
| Global `~/.copilot/mcp-config.json` | ✅ **Works** — agent discovers and calls tools |
| `--additional-mcp-config @file` CLI flag | ✅ **Works** — same result, per-session |

## Solution

### Architecture

```
Emacs (tools registered)
  ↓ writes temp config at startup
  ↓
Copilot CLI --additional-mcp-config @/tmp/bridge.json
  ↓ spawns MCP server from config
  ↓
copilot-sdk-mcp-bridge.py (Python, stdio MCP server)
  ↓ on tools/list → reads EMACS_TOOLS_JSON env var
  ↓ on tools/call → dispatches via emacsclient --eval
  ↓
Emacs (executes tool, returns result)
```

### The Bridge Config File

Written as a temp file at `copilot-sdk-start`, passed via `--additional-mcp-config @/path`:

```json
{
  "mcpServers": {
    "emacs-tools": {
      "type": "stdio",
      "command": "/usr/bin/python3",
      "args": ["/path/to/copilot-sdk-mcp-bridge.py"],
      "env": {
        "EMACS_TOOLS_JSON": "[{\"name\":\"emacs-buffer-list\",...}]"
      },
      "tools": ["*"]
    }
  }
}
```

Key format details:
- `env` is a flat **object** `{"KEY": "VAL"}` (NOT the ACP spec's `[{name, value}]` array)
- `type` must be `"stdio"`
- `tools: ["*"]` enables all tools from this server
- `command` should be an absolute path
- Server name becomes a prefix: tool `emacs-buffer-list` → agent sees `emacs-tools-emacs-buffer-list`

### The Python MCP Bridge

Pure stdlib Python 3, no dependencies. Handles the full MCP protocol:

```python
# Handles: initialize, notifications/initialized, tools/list, tools/call, ping
# tools/list: returns tools from EMACS_TOOLS_JSON env var
# tools/call: dispatches to Emacs via subprocess emacsclient --eval
```

The `EMACS_TOOLS_JSON` env var solves the **startup race condition** — the bridge starts before the Emacs server might be ready, but it can still respond to `tools/list` immediately from the env var. Tool *execution* (tools/call) goes through `emacsclient` which requires the Emacs server.

### Elisp Integration

```elisp
;; At copilot-sdk-start:
;; 1. Serialize registered tools to MCP format
;; 2. Write temp config file with bridge server config
;; 3. Pass to CLI via --additional-mcp-config @file

;; At copilot-sdk-stop:
;; 1. Delete temp config file (cleanup)
```

## Prevention / Advice for Future Work

1. **Don't rely on `session/new mcpServers`** for tool visibility in CLI v1. It's spec-compliant but not implemented for tool discovery.

2. **Use `--additional-mcp-config` over global config** — it's per-CLI-instance, doesn't pollute the global `~/.copilot/mcp-config.json`, and is cleaned up on stop.

3. **The MCP config format differs from ACP format** — `env` is `{"K":"V"}` in config files but `[{name, value}]` in ACP spec. Don't mix them.

4. **MCP server names become tool prefixes** — the agent sees `{server-name}-{tool-name}`. Choose short, descriptive server names.

5. **The `EMACS_TOOLS_JSON` env var pattern** is useful for any MCP bridge that needs to report tools before the host application is fully ready — bake the tool list into the bridge at startup time.

6. **Permission handling works end-to-end** — MCP tool calls trigger `session/request_permission` from the CLI, which the client can auto-approve or prompt the user.
