# GitHub Issue Template Posting Guide

## Overview

You can now create note posts for the website directly through GitHub Issues using a simple form interface. This replaces the need for Discord-based publishing while providing better version control and review workflows.

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

## Future Enhancements

Planned improvements include:
- Support for other post types (full posts, responses)
- Content scheduling capabilities
- Auto-merge for trusted contributors
- Enhanced content preview in PRs

## Questions?

If you encounter any issues or have suggestions for improvements, please:
1. Create a regular issue (not using the post template)
2. Include details about what went wrong
3. Provide the issue number that failed if applicable