---
name: audit-ai-memex
description: "Audit AI Memex health — detect stale entries, spoke-hub drift, broken references, and dependency propagation risks across the knowledge base"
---

# Audit AI Memex

Assess the health and freshness of the AI Memex knowledge base. Run from the
lqdev.me repo root where `_src/resources/ai-memex/` and `graph.json` are available.

## When to Run

- After importing entries (verify integration with existing knowledge graph)
- At session end ("Anything stale or broken in the Memex?")
- When updating a hub entry (check what depends on it)
- Periodically to catch drift between spokes and hub
- When user asks: "How fresh is my Memex?", "What needs updating?"

## Quick Audit

Run the backing script from the lqdev.me repo root:

```powershell
& "$HOME/.agents/skills/audit-ai-memex/scripts/audit-memex.ps1"
```

The script outputs a structured report covering:
1. **Entry freshness** — age since last update, flagging entries older than 90 days
2. **Spoke-hub drift** — entries where spoke has a newer `last_updated_date`
3. **Broken references** — `related_entries` pointing to non-existent files
4. **Orphan entries** — entries with 0 inbound references (isolated knowledge)
5. **Critical hubs** — entries with 3+ inbound references (high propagation risk)

## Interpreting Results

### Staleness
Entries unchanged for 90+ days aren't necessarily stale — patterns and references
may be stable. Focus on:
- `project-report` entries (likely outdated as projects evolve)
- Entries whose `tags` reference fast-moving technologies

### Spoke-Hub Drift
If a spoke entry is newer, use the import script to update:
```powershell
& "$HOME/.agents/skills/import-ai-memex/scripts/import.ps1"
```

### Broken References
Fix by either:
1. Removing the dead reference from `related_entries`
2. Creating the missing entry if the knowledge gap is real

### Update Propagation
When updating a critical hub entry (3+ inbound refs), review its dependents.
Read `references/REPORT-FORMAT.md` for the dependency impact template.

## Gotchas

- `graph.json` must exist at `_public/resources/ai-memex/graph.json` for dependency
  analysis. Run `dotnet run` first if it's missing.
- Entries written directly in the hub (no spoke equivalent) are never flagged as
  drifted — they're hub-only by design.
- The `related_entries` field is seeded by the knowledge graph at build time.
  An entry showing 0 inbound refs may gain connections after the next build.
