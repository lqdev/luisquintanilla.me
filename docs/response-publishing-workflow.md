# Response Publishing Workflow

This document explains how to use the GitHub Issue Forms workflow to publish response posts (replies, reshares, stars) to your website.

## How to Create a Response Post

1. **Go to Issues**: Navigate to your repository's Issues tab
2. **Create New Issue**: Click "New issue"
3. **Select Template**: Choose "💬 Post a Response" template
4. **Fill out the form**:
   - **Response Type**: Select from reply, reshare, or star
   - **Target URL**: The URL you're responding to (required)
   - **Title**: A descriptive title for your response (required)
   - **Content**: Your response content (optional for star responses)
   - **Slug**: Custom URL slug (optional - auto-generated if not provided)
   - **Tags**: Comma-separated tags (optional)

5. **Submit Issue**: The workflow will automatically:
   - Validate your input
   - Generate a properly formatted markdown file
   - Create a pull request with your content
   - Close the issue automatically

## Response Types

### Reply
Use for responding to or commenting on content:
- **Requires**: Target URL, Title, Content
- **Example**: Replying to a blog post with your thoughts

### Reshare
Use for sharing/retweeting content with optional commentary:
- **Requires**: Target URL, Title
- **Optional**: Content (your commentary)
- **Example**: Sharing an article with your recommendations

### Star
Use for liking/favoriting content:
- **Requires**: Target URL, Title
- **Optional**: Content (usually left empty for simple likes)
- **Example**: Starring a useful resource without additional commentary

## Generated File Format

The workflow creates files in `_src/responses/` with proper IndieWeb frontmatter:

```yaml
---
title: "Your Response Title"
targeturl: https://example.com/target-post
response_type: reply
dt_published: "2025-09-15 13:40 -05:00"
dt_updated: "2025-09-15 13:40 -05:00"
tags: ["webdev","indieweb"]
---

Your response content here (if provided)
```

## Features

- ✅ **Form Validation**: Ensures proper response types and valid URLs
- ✅ **Flexible Content**: Star responses can omit content for simple likes
- ✅ **Auto Slug Generation**: Creates URL-friendly slugs from titles
- ✅ **Tag Processing**: Handles comma-separated tags with deduplication
- ✅ **Timezone Aware**: Generates proper EST timestamps
- ✅ **Error Handling**: Clear error messages for invalid input
- ✅ **IndieWeb Compatible**: Proper microformats and response types
- ✅ **Pull Request Workflow**: Review changes before publishing

## Troubleshooting

### Common Issues

1. **Invalid Response Type**: Must be exactly "reply", "reshare", or "star"
2. **Invalid URL**: Target URL must start with "http://" or "https://"
3. **Missing Content**: Reply and reshare responses should include content
4. **Processing Errors**: Check the GitHub Actions logs for detailed error messages

### Error Messages

- `Response type must be one of: reply, reshare, star`
- `Target URL must be a valid HTTP/HTTPS URL`
- `Content is required for [type] responses`
- `Missing required arguments`

## Integration with Existing Site

This workflow integrates seamlessly with your existing:
- **Domain Types**: Uses the existing `Response` and `ResponseDetails` types
- **Build Process**: Generated files are automatically included in site builds
- **RSS Feeds**: Response posts appear in your response feeds
- **IndieWeb Standards**: Proper microformats for webmentions and syndication

The workflow follows the same pattern as your existing note publishing workflow but is specifically tailored for IndieWeb response types.