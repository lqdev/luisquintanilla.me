---
title: "Research: ACP vs SDK Protocol for Copilot CLI"
description: "Comparison of the two JSON-RPC protocols supported by GitHub Copilot CLI: hidden SDK headless protocol vs public ACP"
entry_type: research
published_date: "2026-04-02"
last_updated_date: "2026-04-02"
tags: "copilot, acp, json-rpc, protocols, research"
related_skill: ""
source_project: "copilot-sdk-elisp"
---

## Context

Building an Emacs Lisp SDK for GitHub Copilot required choosing which protocol to speak to the Copilot CLI. We discovered the CLI supports TWO completely different JSON-RPC protocols, and picking the wrong one cost significant debugging time. This research documents both protocols to save others the same pain.

## Options Considered

### Option A: SDK Protocol (--headless --stdio)

The protocol used by the official copilot-sdk mono-repo (Node/Python/Go/.NET SDKs).

- **CLI flags**: `--headless --stdio` (HIDDEN — not shown in `copilot --help`)
- **Transport**: Content-Length framed JSON-RPC 2.0 (LSP-style headers)
- **Init**: `ping` → `protocolVersion` response
- **Session**: `session.create` / `session.send` / `session.disconnect`
- **Events**: `session.event` notifications (typed union of event kinds)
- **Tools**: Registered via `session.create`, called via `tool.call` (server→client)
- **Permissions**: `permission.request` (server→client)
- **Emacs compat**: `jsonrpc.el` works directly (built-in Content-Length transport)

- **Pros**: Emacs `jsonrpc.el` works out of the box; existing SDK source code to reference
- **Cons**: Hidden/undocumented flags; BROKE in CLI v0.0.410 (issue #530, restored in v1.0.16); no official spec; used only by copilot-sdk repo

### Option B: ACP — Agent Client Protocol (--acp --stdio)

The public, officially documented protocol for building Copilot into any editor.

- **CLI flags**: `--acp --stdio` (PUBLIC — shown in `copilot --help`)
- **Transport**: NDJSON (newline-delimited JSON-RPC 2.0) — each message = JSON + `\n`
- **Init**: `initialize` → capabilities negotiation (like LSP but for agents)
- **Session**: `session/new` / `session/prompt` / `session/cancel` / `session/load`
- **Events**: `session/update` notifications (discriminated by `sessionUpdate` field)
- **Tools**: Exposed via MCP servers passed in `session/new`'s `mcpServers` param
- **Permissions**: `session/request_permission` with `toolCall`/`options` format
- **Client caps**: `fs` (file read/write), `terminal` (create/output/kill/wait)
- **Spec**: Full documentation at [agentclientprotocol.com](https://agentclientprotocol.com)
- **GitHub docs**: [docs.github.com/copilot/reference/acp-server](https://docs.github.com/copilot/reference/acp-server)

- **Pros**: Officially documented; public API; industry standard (Zed, VS Code use it); full spec; future-proof
- **Cons**: Emacs `jsonrpc.el` can't be used directly (no Content-Length framing); need ~50 lines of custom NDJSON transport

## Evaluation Criteria

1. **Stability**: Will this protocol continue working across CLI updates?
2. **Documentation**: Is there a spec we can rely on?
3. **Future-proofing**: Where is GitHub investing?
4. **Implementation effort**: How much code to support it in Emacs?
5. **Ecosystem**: Are other editors using this protocol?

## Recommendation

**ACP (Option B)** is the correct choice for any new Copilot integration.

| Criterion | SDK Protocol | ACP |
|-----------|-------------|-----|
| Stability | ❌ Broke once (v0.0.410) | ✅ Official public API |
| Documentation | ❌ Undocumented, reverse-engineered | ✅ Full spec at agentclientprotocol.com |
| Future-proofing | ❌ Hidden flags may be deprecated | ✅ Where GitHub is investing |
| Implementation | ✅ jsonrpc.el works directly | ⚠️ ~50 lines NDJSON transport |
| Ecosystem | ❌ Only copilot-sdk repo | ✅ Zed, VS Code, any ACP client |

## Trade-offs

**What we gave up**: Free `jsonrpc.el` Content-Length transport (built into Emacs 27.1+).

**What we gained**: A ~50-line NDJSON transport layer that talks to the officially supported, documented, stable protocol with a full spec and industry adoption.

**The NDJSON transport is trivial**:

```elisp
(defun copilot-sdk--ndjson-filter (process output)
  "Accumulate output, parse complete JSON lines."
  (with-current-buffer (process-buffer process)
    (goto-char (point-max))
    (insert output)
    (goto-char (point-min))
    (while (search-forward "\n" nil t)
      (let* ((line (buffer-substring (point-min) (1- (point))))
             (json (json-parse-string line :object-type 'alist)))
        (delete-region (point-min) (point))
        (copilot-sdk--dispatch-message json)))))
```

### Key ACP Details Discovered During Implementation

**Permission protocol format** (not obvious from spec):
- Request: `{ toolCall: { toolCallId: "..." }, options: [{ optionId, name, kind }] }`
- Response: `{ outcome: { outcome: "selected", optionId: "allow-once" } }`
- Cancel: `{ outcome: { outcome: "cancelled" } }`

**session/update event types**:
- `agent_message_chunk` — streaming text
- `tool_call` — tool invocation announced (has `title`, `toolCallId`)
- `tool_call_update` — status change (completed/in_progress/cancelled)
- `plan` — agent's execution plan
- `user_message_chunk` — echoed user message during session/load

**CLI log levels**: `none`, `error`, `warning`, `info`, `debug`, `all`, `default` (NOT "warn" — this caused a bug).

**CLI version tested**: v1.0.16 (both protocols work, but ACP is the future).
