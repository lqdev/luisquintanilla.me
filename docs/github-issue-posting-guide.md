# GitHub Issue Template Posting Guide

## Overview

You can now create content for the website directly through GitHub Issues using a simple form interface. This provides better version control and review workflows for multiple content types including notes, bookmarks, responses, media, playlists, and reviews.

## How to Create a Note Post

### Step 1: Create a New Issue

1. Go to the [Issues tab](https://github.com/lqdev/luisquintanilla.me/issues) of this repository
2. Click "New issue"
3. Select "📝 Post a Note" from the issue templates

### Step 2: Fill Out the Form

The issue template will present you with a form containing:

**Required Fields:**
- **Title**: The title of your note post (will appear as the main heading)
- **Content**: The main content of your note (supports full Markdown formatting)

**Optional Fields:**
- **Tags**: Comma-separated tags for categorizing your note (e.g., "tech, programming, thoughts")

### Step 3: Submit the Issue

- The issue will automatically be labeled with "post" and "note"
- Click "Submit new issue" to create the issue

### Step 4: Automated Processing

Once you submit the issue, the following happens automatically:

1. 🤖 **GitHub Action Triggers**: A workflow processes your issue
2. 📄 **Content Generation**: Creates a properly formatted markdown file in `_src/notes/`
3. 🔀 **Pull Request Creation**: Generates a PR with your content
4. ✅ **Issue Closure**: The original issue is closed automatically
5. 🚀 **Website Publication**: After PR review and merge, your note goes live

## Content Format

The generated content will have proper frontmatter matching the existing site structure:

```yaml
---
title: Your Note Title
post_type: note
published_date: "2025-09-14 16:32 -05:00"
tags: ["tag1", "tag2", "tag3"]
---

Your note content here with full Markdown support.
```

## Markdown Support

The content field supports full Markdown formatting:

- **Headers**: `# Header 1`, `## Header 2`, etc.
- **Emphasis**: `**bold**`, `*italic*`
- **Lists**: Both bulleted (`-`) and numbered (`1.`)
- **Links**: `[Link Text](https://example.com)`
- **Code**: Inline `` `code` `` and code blocks with ``` 
- **Blockquotes**: `> Quote text`
- **Images**: `![Alt text](image-url)`

## File Naming

Files are automatically named using:
- Slug generated from your title (lowercase, special characters removed)
- Current date for uniqueness
- Example: `thoughts-on-latest-tech-2025-09-14.md`

## Tags

- Tags should be comma-separated: `tech, programming, thoughts`
- They will be automatically converted to lowercase
- Spaces around commas are automatically trimmed
- Tags appear in the generated frontmatter as an array

## Error Handling

If there's an issue with your submission:

- ❌ The workflow will comment on your issue with error details
- 🔄 Your issue will remain open for you to edit and resubmit
- 📝 Common issues: missing title/content, content too short

## Review Process

1. **Automatic PR Creation**: Your content is turned into a pull request
2. **Review**: Repository maintainers can review the content
3. **Merge**: Once approved, your content goes live on the website
4. **Build & Deploy**: The site automatically rebuilds and deploys

## Advantages Over Discord Workflow

✅ **Native GitHub Integration**: Everything happens within GitHub
✅ **Version Control**: Full history and tracking of content changes  
✅ **Review Workflow**: Content can be reviewed before publication
✅ **Accessibility**: Works from any device with GitHub access
✅ **No External Dependencies**: No Discord server or bot needed
✅ **Audit Trail**: Complete record of who created what and when

## Troubleshooting

### Issue Not Processing
- Check that your issue has the "post" label
- Ensure you used the official issue template
- Verify that title and content are not empty

### Content Formatting Issues
- Check that your Markdown syntax is correct
- Ensure special characters in title don't break the filename
- Verify tags are properly comma-separated

### Permission Issues
- You must be a repository collaborator to create posts
- Issues from external contributors will need manual processing

## How to Create a Playlist Post

### Overview

The playlist workflow automates the process of creating music playlist posts by fetching track information from Spotify and generating YouTube links for each track.

### Step 1: Create a New Issue

1. Go to the [Issues tab](https://github.com/lqdev/luisquintanilla.me/issues) of this repository
2. Click "New issue"
3. Select "🎵 Post a Playlist" from the issue templates

### Step 2: Fill Out the Form

**Required Fields:**
- **Playlist Title**: The title of your playlist post (e.g., "Crate Finds - November 2025")
- **Spotify Playlist URL**: The full Spotify playlist URL (e.g., `https://open.spotify.com/playlist/5CTCAnbYOoWS1sdYolrZAG`)

**Optional Fields:**
- **Commentary**: Your thoughts, context, or highlights about the playlist (supports Markdown)
- **Slug**: Custom URL slug (defaults to auto-generated from title)
- **Additional Tags**: Extra tags beyond the defaults (playlist, music, spotify, youtube, cratefinds)

### Step 3: Submit and Process

Once you submit:

1. 🤖 **GitHub Action Triggers**: Dedicated playlist workflow starts
2. 🎵 **Spotify Integration**: playlist-creator tool fetches track information
3. 🔍 **YouTube Links**: Automatically finds YouTube links for each track
4. 📄 **Markdown Generation**: Creates formatted file in `_src/playlists/`
5. 🔀 **Pull Request**: Creates PR for review (no auto-merge)
6. ✅ **Issue Closure**: Original issue is closed automatically

### Generated Content Format

```yaml
---
title: "Crate Finds - November 2025"
date: "2025-11-07 20:22 -05:00"
tags: ["cratefinds","electronic","music","playlist","spotify","youtube"]
---

Your optional commentary here...

## Tracks

1. **Track Name** by Artist
   - Album: *Album Name*
   - Duration: 4:15
   - [Listen on YouTube](https://www.youtube.com/watch?v=...)
   - [Backup: Listen on Spotify](https://open.spotify.com/track/...)

---

**Original Spotify Playlist:** [Listen on Spotify](https://open.spotify.com/playlist/...).
```

### Requirements

**Repository Secrets Required:**
- `SPOTIFY_CLIENT_ID`: Your Spotify API client ID
- `SPOTIFY_CLIENT_SECRET`: Your Spotify API client secret

These must be configured in the repository settings for the workflow to access the Spotify API.

### Troubleshooting Playlists

**Issue Not Processing:**
- Verify the Spotify URL is in the correct format: `https://open.spotify.com/playlist/[playlist-id]`
- Check that repository secrets are properly configured
- Ensure you used the official issue template

**Track Information Issues:**
- The playlist must be publicly accessible on Spotify
- YouTube links are best-effort; some tracks may not have YouTube matches
- Spotify backup links are always included

**Processing Errors:**
- Check the Actions tab for detailed error logs
- Verify the Spotify playlist ID is valid
- Ensure the playlist is not empty

## Future Enhancements

Planned improvements include:
- Content scheduling capabilities
- Auto-merge for trusted contributors
- Enhanced content preview in PRs
- Additional music service integrations

## Questions?

If you encounter any issues or have suggestions for improvements, please:
1. Create a regular issue (not using the post template)
2. Include details about what went wrong
3. Provide the issue number that failed if applicable