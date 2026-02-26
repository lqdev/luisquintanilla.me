# Repository File Organization

- **Status**: Accepted
- **Date**: 2026-02-01
- **Decision-makers**: Luis Quintanilla, GitHub Copilot
- **Consulted**: Repository audit

## Context and Problem Statement

Over time, the repository accumulated documentation artifacts, test files, and implementation summaries at the root level. This clutter makes navigation difficult and confuses new contributors about the canonical locations for different file types.

## Decision Drivers

- Root directory clutter (15+ markdown files at root)
- Unclear separation between active docs and historical artifacts
- Test files and demo content mixed with production code
- Need for clear contributor guidance

## Considered Options

1. **Leave as-is with better README navigation**
2. **Archive historical documents, clean root**
3. **Complete restructure into /src, /docs, /tests pattern**

## Decision Outcome

**Chosen option**: "Archive historical documents, clean root", because it maintains the working structure while cleaning up accumulated artifacts.

### Consequences

**Good:**
- Clean root directory with only essential files
- Historical artifacts preserved in `/archive`
- Clear separation between active and archived content
- Better discoverability of current documentation

**Bad:**
- Some file moves may break external links
- Requires updating some internal references

### Confirmation

- Build still works after changes
- No broken internal links
- Historical content accessible in archive

## File Organization Structure

### Root Directory (Essential Only)

```
/
├── README.md                 # Main project documentation
├── CONTRIBUTING.md           # Contributor guidelines
├── changelog.md              # Project history
├── .gitignore
├── PersonalSite.fsproj       # F# project file
├── Program.fs                # Entry point
├── *.fs                      # Core F# modules
├── staticwebapp.config.json  # Azure SWA configuration
└── package.json              # Node.js dependencies (if needed)
```

### Archived Implementation Summaries → `/archive/summaries/`

Files moved:
- ACTIVITYPUB_FIX_VISUALIZATION.md
- ACTIVITYPUB_MEDIA_FIX.md
- BEFORE_AFTER_COMPARISON.md
- FIX_SUMMARY.md
- IMPLEMENTATION_SUMMARY.md
- INVESTIGATION_SUMMARY.md
- MEDIA_POSITION_FIX.md
- MEDIA_WORKFLOW_UPGRADE.md
- PINNED_POSTS_IMPLEMENTATION.md
- PINNED_POSTS_RECOMMENDATIONS.md
- S3_FIX_SUMMARY.md
- SECURITY-UPDATE.md
- SOLUTION_SUMMARY.md
- TRAVEL_IMPLEMENTATION_SUMMARY.md
- WORKFLOW_OPTIMIZATION_SUMMARY.md

### Demo/Test Files → `/archive/demos/`

Files moved:
- blogroll-demo.html
- rome-favorites-demo.gpx
- rome-favorites-demo.html
- test-follow.json
- test_activitypub_media.sh

### Directory Structure

```
/
├── _src/               # Source content (markdown)
├── _scratch/           # Development scratchpad (drafts)
├── archive/            # Historical files
│   ├── README.md
│   ├── summaries/      # Implementation summaries
│   └── demos/          # Demo/test files
├── api/                # Azure Functions
├── Data/               # JSON data files
├── docs/               # Documentation
│   ├── README.md
│   ├── adr/            # Architectural Decision Records
│   └── [feature docs]
├── projects/           # Project management
│   ├── active/
│   ├── archive/
│   ├── completed/
│   └── templates/
├── Scripts/            # Development scripts
├── Services/           # Shared services
├── test-content/       # Test content samples
├── test-scripts/       # F# test scripts
└── Views/              # View modules
```

## More Information

- See `CONTRIBUTING.md` for where to place new files
- See `docs/README.md` for documentation index
- See `archive/README.md` for archival criteria
