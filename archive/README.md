# Archive Directory

This directory contains historical files from development, migration, and project work that have been cleaned up from the root directory but may have reference value.

## Directory Structure

```
archive/
├── README.md               # This file
├── summaries/              # Implementation summaries and fix documentation
├── demos/                  # Demo files and test artifacts
├── bookmark-files-to-move.txt
├── bookmarks-migration-log.txt
├── broken-links-repair-final-summary.md
├── changelog-entry-*.md    # Draft changelog entries
├── link-analysis-report.json
├── note-files-to-move.txt
├── notes-migration-log.txt
├── repo-cleanup-summary.md
└── text-only-navigation-cleanup-summary.md
```

## Contents

### `/summaries/` - Implementation Summaries

Documentation of completed features and fixes:
- ActivityPub integration summaries
- Media workflow implementations
- S3 integration fixes
- Pinned posts feature documentation
- Workflow optimizations

### `/demos/` - Demo and Test Files

Demo files and test artifacts:
- Blogroll demo HTML
- Rome travel guide demos (GPX and HTML)
- ActivityPub test files

### Migration Logs and Artifacts

Files from content migration projects, including logs of files moved and migration scripts.

### Changelog Entries

Draft changelog entries that were incorporated into the main `changelog.md` file.

### Analysis Reports

Results from link analysis, content auditing, and other site analysis activities.

## Archival Criteria

Files are moved to the archive when they:

1. **Are no longer actively referenced** - The information has been incorporated elsewhere
2. **Document completed work** - Implementation summaries for finished features
3. **Have historical value** - May be useful for future reference but not current work
4. **Clutter the root directory** - One-time artifacts that accumulated over time

## Cleanup History

| Date | Files Moved | Reason |
|------|-------------|--------|
| 2025-09-14 | Initial archive setup | Repository cleanup |
| 2026-02-01 | 15 summaries, 5 demos | Root directory declutter (ADR-0005) |

## Restoration

If you need to restore a file to active use:

1. Move it to the appropriate location (e.g., `/docs/` for documentation)
2. Update any broken references
3. Remove from this archive
4. Document the restoration in your PR description