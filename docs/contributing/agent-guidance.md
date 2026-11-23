# Agent guidance

How the instructions for AI coding agents in this repo are split up, and where a new rule goes.

## What loads when

| File | Loaded | Size |
| --- | --- | --- |
| `AGENTS.md` | every session | 150 lines or fewer |
| `.github/copilot-instructions.md` | every session | 3 lines, pointing at `AGENTS.md` |
| `.github/instructions/*.instructions.md` | only when a matching file is being worked on | 60 lines or fewer each |
| `.github/skills/` | when an agent picks the skill for a task | one procedure per skill |
| `.agents/skills/` | portable skills in this repo and in the `lqdev-toolkit` plugin | one procedure per skill |

I keep the always-loaded part short on purpose. Short, specific, non-obvious instructions get
followed; long files and repo overviews cost tokens without improving results (Gloaguen et al.,
arXiv 2602.11988). The test for every line: would an agent get this wrong without being told?
If it could learn it by reading the code, leave it out.

## Where a new piece of guidance goes

| The guidance is | It goes in |
| --- | --- |
| Something an agent can find by reading the code | Nowhere |
| Needed on almost every task | `AGENTS.md` |
| Needed only for some files | An instruction file |
| A multi-step procedure | A skill |
| Something that must never happen | A CI check |
| Useful in every repo | The plugin in `.agents/` (layer 9) |
| Reasons a person wants | `docs/` |

If a skill only makes sense in this repository, put it in `.github/skills/`. If it would help in
any repository, put it in `.agents/skills/` so the project copy and plugin copy stay in one place.

## Adding an instruction file

Create `.github/instructions/{name}.instructions.md`. Start it with frontmatter whose `applyTo`
lists the comma-separated globs it covers, then a heading, one sentence saying what those files
are, 5 to 10 rules, and a "Read first" line naming the doc to open next. Write each rule as an
instruction with its reason in the same sentence, and check it against the code before you add it.
`test-scripts/test-docs-integrity.fsx` holds each file to 60 lines and checks its paths and voice.

## Is it being used?

This session-store query counts agent and skill calls in this repo over the last 120 days:

```sql
SELECT r.name AS tool,
       COALESCE(json_extract_string(r.arguments_json, '$.agent_type'),
                json_extract_string(r.arguments_json, '$.skill'), '') AS target,
       count(*) AS calls, count(DISTINCT r.session_id) AS sessions
FROM tool_requests r JOIN sessions s ON s.id = r.session_id
WHERE s.repository ILIKE '%luisquintanilla.me%'
  AND s.created_at >= now() - INTERVAL '120 days'
  AND r.name IN ('task', 'skill')
GROUP BY 1, 2 ORDER BY calls DESC
```

After 15 more sessions, delete any skill that was never called.

## Where the old material went

The old 943-line `.github/copilot-instructions.md` and the five custom agents are in
`archive/agent-guidance/`. The agents were called 12 times in 63 sessions, and two of them never.
