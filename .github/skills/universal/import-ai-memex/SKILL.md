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

Run from the lqdev.me repo root (so `_src/resources/ai-memex/` resolves correctly):

```powershell
& "$HOME/.agents/skills/import-ai-memex/scripts/import.ps1"
```

### Option B: Manual import
1. List configured source directories from import-sources.json
2. For each source, find `.md` files in the `.ai-memex/` directory
3. Check if an entry with the same filename already exists in `_src/resources/ai-memex/`
4. If new: copy to `_src/resources/ai-memex/`
5. If the entry lacks a `source_project` field, add it based on the source config
6. Report what was imported

### Post-import
After importing, remind the user:
- Review imported entries for quality/accuracy
- Run `dotnet run` to generate the site and publish the entries
- Commit the new entries to the repo

## Import Sources Configuration

The `import-sources.json` file is user-local (never committed to any repo).
Location: `~/.agents/skills/import-ai-memex/import-sources.json`

See `assets/import-sources.example.json` for the format.

## Conflict Resolution

- **Same filename exists**: Skip (don't overwrite). Report as "already exists".
- **User wants to update**: They should edit the canonical version in `_src/resources/ai-memex/`
  directly, not re-import.
