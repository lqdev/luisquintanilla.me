# Publish a bookmark

To publish a bookmark, open an issue with the "🔖 Post a Bookmark" form. The `process-content-issue.yml` workflow writes the post to `_src/bookmarks/` and opens a pull request.

## How to Create a Bookmark Post

1. **Go to Issues**: Navigate to your repository's Issues tab
2. **Create New Issue**: Click "New issue"
3. **Select Template**: Choose "🔖 Post a Bookmark" template
4. **Fill out the form**:
   - **Target URL**: The URL you are bookmarking (required)
   - **Title**: A descriptive title for your bookmark (required)
   - **Content**: Your bookmark description or notes (optional)
   - **Slug**: Custom URL slug (optional - auto-generated if not provided)
   - **Tags**: Comma-separated tags (optional)

5. **Submit Issue**: The workflow will automatically:
   - Validate your input
   - Generate a properly formatted markdown file
   - Create a pull request with your content
   - Close the issue automatically

## Generated File Format

The workflow creates files in `_src/bookmarks/` with proper frontmatter:

```yaml
---
title: "Your Bookmark Title"
targeturl: https://example.com/target-resource
response_type: bookmark
dt_published: "2025-09-15 13:40 -05:00"
dt_updated: "2025-09-15 13:40 -05:00"
tags: ["tools","webdev"]
---

Your bookmark description here (if provided)
```

## Features

- ✅ **Form Validation**: Ensures proper URL format and required fields
- ✅ **Auto Slug Generation**: Creates URL-friendly slugs from titles
- ✅ **Tag Processing**: Handles comma-separated tags with deduplication
- ✅ **Timezone Aware**: Generates proper EST timestamps
- ✅ **Error Handling**: Clear error messages for invalid input
- ✅ **Pull Request Workflow**: Review changes before publishing
- ✅ **Consolidated Processing**: Uses single workflow to avoid dual triggering

## Troubleshooting

### Common Issues

1. **Invalid URL**: Target URL must start with "http://" or "https://"
2. **Missing Title**: Title is required for all bookmark posts
3. **Processing Errors**: Check the GitHub Actions logs for detailed error messages

### Error Messages

- `Target URL must be a valid HTTP/HTTPS URL`
- `Title is required and cannot be empty`
- `Missing required arguments`

## Integration with Existing Site

This workflow integrates seamlessly with your existing:
- **Domain Types**: Uses the existing `Bookmark` types and response format
- **Build Process**: Generated files are automatically included in site builds
- **RSS Feeds**: Bookmark posts appear in your feeds
- **Content Organization**: Follows established bookmark patterns

The workflow follows the same consolidated pattern as note and response publishing workflows, ensuring consistent behavior and avoiding workflow conflicts.

## Comparison with Response Workflow

While bookmarks could technically be created as responses with `response_type: bookmark`, the dedicated bookmark workflow provides:

- **Dedicated Directory**: Files go directly to `_src/bookmarks/` 
- **Simplified Interface**: No need to select response type
- **Bookmark-Specific Validation**: Tailored for bookmark use cases
- **Clear Organization**: Separate bookmarks from other response types

Both approaches generate compatible content that works with the existing site architecture.