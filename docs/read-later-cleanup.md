# Read Later Cleanup Documentation

## Overview

The read-later workflow now includes two automated cleanup jobs that run daily:

1. **Time-based cleanup** (`cleanup-old-entries`): Removes entries older than 14 days
2. **Content-based cleanup** (`cleanup-by-responses-bookmarks`): Removes entries that have been shared as responses or bookmarks

## Content-Based Cleanup

### Purpose

Automatically removes read-later entries when their URL appears in a response or bookmark, eliminating the need to manually track which links have been read or shared.

### How It Works

1. **Extract Target URLs**: Scans all markdown files in `_src/responses/` and `_src/bookmarks/` for `targeturl:` frontmatter field
2. **Deduplicate URLs**: Creates a unique list of all target URLs from responses and bookmarks
3. **Match & Remove**: Compares read-later entries against target URLs and removes matches
4. **Create PR**: If entries are removed, creates a PR with details and auto-merges if no conflicts

### Schedule

- Runs daily at midnight UTC (via cron: `0 0 * * *`)
- Can also be triggered manually via workflow_dispatch

### Example

If you have:
- A read-later entry: `https://example.com/article`
- A response in `_src/responses/my-response.md` with `targeturl: https://example.com/article`

The cleanup job will:
1. Find the matching URL
2. Remove the read-later entry
3. Create a PR showing what was removed
4. Auto-merge the PR (if no conflicts)

### Testing

To test the cleanup logic locally without modifying files:

```bash
bash test-scripts/test-read-later-cleanup.sh
```

This will show:
- How many target URLs were found
- Which read-later entries would be removed
- Which entries would remain

### Current Statistics (as of last test)

- Target URLs from responses/bookmarks: **1,020**
- Read-later entries: **56**
- Entries that would be removed: **24** (43%)
- Entries that would remain: **32**

## Workflow Structure

```yaml
cleanup-by-responses-bookmarks:
  steps:
    1. Extract target URLs from responses and bookmarks
    2. Clean up entries matching responses/bookmarks
    3. Create Pull Request (if changes)
    4. Auto-merge Pull Request
    5. Log cleanup summary
```

## Benefits

- **Automatic tracking**: No need to remember which read-later links have been shared
- **Reduces duplicates**: Prevents read-later list from containing already-shared content
- **Maintains history**: Removed entries are logged in PR description
- **Safe operation**: Creates PR for review before merging
- **Comprehensive logs**: GitHub Actions summary shows exactly what was cleaned up

## Manual Trigger

To manually trigger either cleanup job:

1. Go to Actions tab in GitHub
2. Select "Process Read Later Issue" workflow
3. Click "Run workflow"
4. Select branch and click "Run workflow"

Both cleanup jobs will run when triggered this way.
