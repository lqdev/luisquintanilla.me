# Playlist Automation Implementation

## Overview

This document describes the automated playlist creation system that processes Spotify playlists through GitHub Issues, fetches track information, generates YouTube links, and creates properly formatted markdown posts.

## Architecture

### Components

1. **GitHub Issue Template** (`.github/ISSUE_TEMPLATE/post-playlist.yml`)
   - User-friendly form interface for playlist submission
   - Collects: Title, Spotify URL, commentary, slug, tags
   - Automatically applies `playlist` label

2. **GitHub Actions Workflow** (`.github/workflows/process-playlist-issue.yml`)
   - Triggered on issue creation with `playlist` label
   - Sets up Python environment with `uv` package manager
   - Installs and runs `playlist-creator` tool
   - Processes output with F# script
   - Creates pull request for review

3. **F# Processing Script** (`Scripts/process-playlist-issue.fsx`)
   - Validates Spotify URL format
   - Generates proper frontmatter with timezone
   - Combines user commentary with track list
   - Creates markdown file in `_src/playlists/`

4. **External Tool Integration**
   - `playlist-creator`: Python tool that fetches Spotify track info and generates YouTube links
   - Repository: https://github.com/lqdev/playlist-creator

## Workflow Process

### 1. Issue Submission
User creates issue using template with:
- **Playlist Title**: "Crate Finds - November 2025"
- **Spotify URL**: `https://open.spotify.com/playlist/5CTCAnbYOoWS1sdYolrZAG`
- **Commentary**: Optional thoughts about the playlist
- **Slug**: Optional custom URL slug
- **Tags**: Optional additional tags

### 2. Data Extraction
GitHub Actions workflow parses issue body using regex patterns:
```javascript
function extractFormValue(body, label) {
  const regex = new RegExp(`### ${label}\\s*\\n\\s*\\n([\\s\\S]*?)(?=\\n\\n###|\\n\\n---|\$)`, 'i');
  const match = body.match(regex);
  return match ? match[1].trim() : '';
}
```

### 3. Python Environment Setup
```bash
# Install uv package manager
uses: astral-sh/setup-uv@v4

# Create virtual environment
uv venv

# Install playlist-creator
uv pip install git+https://github.com/lqdev/playlist-creator.git
```

### 4. Track Information Fetching
```bash
# Run playlist-creator with Spotify credentials
uv run playlist-creator "$SPOTIFY_URL" > /tmp/playlist_tracks.md
```

Output format:
```markdown
## Tracks

1. **Track Name** by Artist
   - Album: *Album Name*
   - Duration: 4:15
   - [Listen on YouTube](https://www.youtube.com/watch?v=...)
   - [Backup: Listen on Spotify](https://open.spotify.com/track/...)
```

### 5. F# Script Processing
The F# script (`process-playlist-issue.fsx`):

**Input Parameters:**
1. Title
2. Spotify URL
3. Commentary (optional)
4. Slug (optional)
5. Additional tags (optional)
6. Playlist content (from playlist-creator)

**Processing:**
- Validates Spotify URL format
- Sanitizes slug (lowercase, alphanumeric + hyphens)
- Merges default tags with additional tags
- Generates EST timezone timestamp
- Combines commentary + tracks + Spotify link
- Writes to `_src/playlists/[slug].md`

**Output Example:**
```yaml
---
title: "Crate Finds - November 2025"
date: "2025-11-07 20:22 -05:00"
tags: ["cratefinds","electronic","music","playlist","spotify","youtube"]
---

Optional user commentary here...

## Tracks

[Generated track list from playlist-creator]

---

**Original Spotify Playlist:** [Listen on Spotify](https://open.spotify.com/playlist/...).
```

### 6. Pull Request Creation
```yaml
- name: Create Pull Request
  uses: peter-evans/create-pull-request@v5
  with:
    commit-message: "Add playlist: ${{ title }}"
    title: "Add playlist: ${{ title }}"
    branch: content/issue-${{ issue.number }}/playlist/processed
    delete-branch: true
```

PR body includes:
- Title and Spotify URL
- Processing details checklist
- File location
- Issue reference

### 7. Issue Closure
Successful processing:
- Closes original issue
- Adds comment with success details
- Links to created PR

Failed processing:
- Issue remains open
- Error comment added with details
- User can edit and retry

## Configuration Requirements

### Repository Secrets
Must be configured in GitHub repository settings:

1. **SPOTIFY_CLIENT_ID**: Spotify API application client ID
2. **SPOTIFY_CLIENT_SECRET**: Spotify API application client secret

### Getting Spotify Credentials
1. Go to https://developer.spotify.com/dashboard
2. Create a new application
3. Copy Client ID and Client Secret
4. Add to repository secrets

### Label Configuration
The `playlist` label is automatically synced via `.github/workflows/sync-issue-labels.yml`:
```javascript
{
  name: 'playlist',
  description: 'Playlist content creation and management',
  color: '1D76DB'
}
```

## File Naming Convention

- Custom slug provided: `[slug].md`
- No slug: `[generated-from-title].md`
- Slug sanitization: lowercase, alphanumeric + hyphens only

Examples:
- Input: "Crate Finds - November 2025" → `crate-finds-november-2025.md`
- Input: "My Awesome Playlist!!!" → `my-awesome-playlist.md`

## Default Tags

Every playlist automatically gets these tags:
- `playlist`
- `music`
- `spotify`
- `youtube`
- `cratefinds`

Additional tags from the issue form are merged with these defaults.

## Error Handling

### Validation Errors
- Empty title → Script exits with error
- Empty Spotify URL → Script exits with error
- Invalid Spotify URL format → Script exits with error
- Content too short → Script exits with error

### Runtime Errors
- Spotify API failure → Captured in Action logs
- playlist-creator errors → Captured in Action logs
- F# script errors → Displayed in issue comment

### Recovery
- Issue remains open on failure
- User can edit issue and workflow retries
- Error details provided in issue comment

## Testing

### Local Testing
```bash
# Build F# project
dotnet build

# Test F# script
dotnet fsi Scripts/process-playlist-issue.fsx -- \
  "Test Playlist" \
  "https://open.spotify.com/playlist/5CTCAnbYOoWS1sdYolrZAG" \
  "Commentary text" \
  "test-slug" \
  "electronic,house" \
  "$(cat test-tracks.md)"
```

### Integration Testing
Requires repository secrets to be configured:
1. Create test issue using template
2. Monitor Actions tab for workflow execution
3. Verify PR creation with correct content
4. Check issue closure and comments

## Maintenance

### Updating playlist-creator
The workflow installs from the main branch:
```bash
uv pip install git+https://github.com/lqdev/playlist-creator.git
```

To pin to a specific version:
```bash
uv pip install git+https://github.com/lqdev/playlist-creator.git@v1.0.0
```

### Updating F# Script
Changes to `Scripts/process-playlist-issue.fsx`:
- Test locally before committing
- Ensure backward compatibility with issue template
- Update documentation if parameters change

### Workflow Updates
Changes to `.github/workflows/process-playlist-issue.yml`:
- Test with workflow_dispatch trigger first
- Monitor for secret masking issues
- Verify file-based parameter passing works correctly

## Security Considerations

### Secret Handling
- Spotify credentials stored as GitHub secrets
- Never logged or exposed in output
- Passed via environment variables only

### Input Validation
- Spotify URL regex validation
- Slug sanitization prevents directory traversal
- File writes restricted to `_src/playlists/`

### User Authorization
Workflow only runs if:
- Issue has `playlist` label
- Issue author is `@lqdev`

## Performance

### Typical Execution Time
- Issue parsing: < 1 second
- Python environment setup: ~ 10-15 seconds
- playlist-creator execution: ~ 5-10 seconds (depends on playlist size)
- F# script processing: < 1 second
- PR creation: ~ 2-3 seconds

**Total: ~20-30 seconds for standard playlist**

### Rate Limits
- Spotify API: 429 Too Many Requests if exceeded
- GitHub API: Actions-level rate limiting
- YouTube search: playlist-creator handles rate limiting

## Troubleshooting Guide

### "Spotify URL is required and cannot be empty"
- Issue: Spotify URL field was left blank
- Fix: Fill in the Spotify Playlist URL field

### "Invalid Spotify playlist URL format"
- Issue: URL doesn't match expected pattern
- Fix: Use format `https://open.spotify.com/playlist/[playlist-id]`

### "playlist-creator installation failed"
- Issue: Python dependency conflict
- Fix: Check workflow logs, may need to pin dependency versions

### "No YouTube links found"
- Issue: playlist-creator couldn't match tracks
- Result: Spotify links still work as backup

### "Permission denied" on file write
- Issue: Script trying to write outside `_src/playlists/`
- Fix: Check slug sanitization logic

## Future Enhancements

### Planned Features
1. **Multiple Music Services**: Apple Music, YouTube Music support
2. **Playlist Updates**: Re-run to update existing playlists
3. **Auto-merge**: For trusted users with successful builds
4. **Preview Comments**: Show track list in issue comment before processing
5. **Scheduled Publishing**: Delay publication to specific date/time

### Considered Features
- Collaborative playlists (multiple contributors)
- Playlist comparisons (diff between versions)
- Automatic tag suggestions based on genres
- Integration with last.fm for listening stats

## References

- **playlist-creator Documentation**: https://github.com/lqdev/playlist-creator
- **Sample Playlist**: `_src/playlists/crate-finds-august-2025.md`
- **Issue Template**: `.github/ISSUE_TEMPLATE/post-playlist.yml`
- **Workflow**: `.github/workflows/process-playlist-issue.yml`
- **F# Script**: `Scripts/process-playlist-issue.fsx`
