---
name: import-ai-memex
description: "Import AI Memex entries from other projects into the central lqdev.me store for publishing"
---

# Import AI Memex Entries

Consolidate AI Memex entries from other projects' `.ai-memex/` directories into
the central store at `_src/resources/ai-memex/` in the lqdev.me repo.

## Prerequisites

- Must be run from the lqdev.me repo (verify `PersonalSite.fsproj` exists)
- Import sources configured in `~/.agents/skills/import-ai-memex/import-sources.json`

## Import Process

### Option A: Use the import script

Run from the lqdev.me repo root:

```powershell
# Normal import (new entries + updates where spoke is newer)
& "$HOME/.agents/skills/import-ai-memex/scripts/import.ps1"

# Preview what would change
& "$HOME/.agents/skills/import-ai-memex/scripts/import.ps1" -DryRun

# Force-overwrite all spoke entries (bulk resync)
& "$HOME/.agents/skills/import-ai-memex/scripts/import.ps1" -Force
```

### Option B: Manual import
1. List configured source directories from import-sources.json
2. For each source, find `.md` files in the `.ai-memex/` directory
3. Check if an entry with the same filename already exists in `_src/resources/ai-memex/`
4. If new: copy to `_src/resources/ai-memex/`, adding `source_project` if missing
5. If exists: compare `last_updated_date` — update hub if spoke is newer
6. Report what was imported, updated, or skipped

### Post-import
After importing, remind the user:
- Review imported/updated entries for quality and accuracy
- Run `dotnet run` to generate the site (knowledge graph will seed `related_entries`)
- Commit the new/updated entries to the repo

## Import Sources Configuration

The `import-sources.json` file is user-local (never committed to any repo).
Location: `~/.agents/skills/import-ai-memex/import-sources.json`

See `assets/import-sources.example.json` for the format.

## Update Behavior

When a file already exists in the hub:
- **Default**: Compare `last_updated_date` in spoke vs hub YAML frontmatter.
  If spoke is newer, update the hub copy while preserving hub-only data.
- **`-Force`**: Overwrite all entries regardless of dates (useful for bulk resync).
- **Merge**: `related_entries` from both spoke and hub are merged (union of unique values).
  The hub's knowledge-graph-seeded entries are preserved alongside spoke's explicit entries.

## Gotchas

- The `related_entries` field in hub entries is seeded by the knowledge graph at build time.
  The import script merges spoke + hub values to avoid losing graph-generated connections.
- Running `install-skills.ps1 -Force` preserves `import-sources.json` (user-local config).
- Entries in `_src/resources/ai-memex/` with no spoke equivalent are hub-only entries
  (written directly in this repo). The import script never touches these.
