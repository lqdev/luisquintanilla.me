# lqdev-toolkit

Luis Quintanilla's portable Agent Plugin 1.0 package.

| Skill | Use |
| --- | --- |
| `write-ai-memex` | Capture patterns, research, decisions, and project reports |
| `query-ai-memex` | Search the AI Memex |
| `import-ai-memex` | Import entries from project spokes into the central store |
| `audit-ai-memex` | Find stale, orphaned, broken, or drifting knowledge entries |
| `triage-review-comments` | Validate and address pull request review threads |
| `shovel-ready` | Write a detailed plan or GitHub issue |
| `research-brief` | Research a standard or technology and map it to a project |

Install a local copy from the repository root:

```text
copilot plugin install ./.agents
```

Install from the repository:

```text
copilot plugin install lqdev/luisquintanilla.me:.agents
```

Copy `.agents/user-instructions.md` to `~/.copilot/copilot-instructions.md` once, after Luis reviews it.

Update the installed plugin:

```text
copilot plugin update lqdev-toolkit
```
