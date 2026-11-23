# Media Publishing Workflow

This document explains how to use the GitHub Issue Forms workflow to publish media posts (images, videos, audio) to your website using GitHub's built-in attachment functionality.

## How to Create a Media Post

1. **Go to Issues**: Navigate to your repository's Issues tab
2. **Create New Issue**: Click "New issue"
3. **Select Template**: Choose "📷 Post Media" template
4. **Fill out the form**:
   - **Media Type**: Select image, video, or audio (required)
   - **Title**: A descriptive title for your media post (required)
   - **Content and Attachments**: Write your content and drag/drop media files directly into this field (required)
   - **Orientation**: Select landscape or portrait aspect ratio (optional)
   - **Slug**: Custom URL slug (optional - auto-generated if not provided)
   - **Tags**: Comma-separated tags (optional)

5. **Submit Issue**: The workflow will automatically:
   - Parse markdown images from your content
   - Extract alt text as captions for each media item
   - Generate a properly formatted markdown file with :::media::: block
   - Create a pull request for review
   - Close the issue automatically

## Content and Attachment Process

The enhanced workflow uses GitHub's built-in attachment functionality:

### How It Works
1. **Write your content** in the "Content and Attachments" field using normal markdown
2. **Drag and drop media files** directly into the text area
3. **GitHub automatically generates** markdown like `![alt-text](URL)` for each file
4. **Mix text and media** naturally - write content, add attachments, add more content
5. **Alt text becomes captions** - the alt text you provide becomes the caption for each media item

### Example Content Format
```markdown
Here's my latest photo from the hiking trip:

![Beautiful mountain sunrise](https://github.com/user/repo/assets/12345/sunrise.jpg)

The sunrise was absolutely incredible. Here's another view:

![Valley view from summit](https://github.com/user/repo/assets/12345/valley.jpg)

What an amazing experience!
```

This automatically becomes:
- **Content**: "Here's my latest photo... What an amazing experience!" (with images removed)
- **Media Block**: Two media items with captions "Beautiful mountain sunrise" and "Valley view from summit"

### Advantages of This Approach
- **Natural workflow**: Just like commenting on GitHub issues
- **No manual URL copying**: GitHub handles the URLs automatically
- **Individual captions**: Each image can have its own descriptive alt text
- **Mixed content**: Combine text and media naturally
- **Filename fallback**: If alt text is empty, uses filename as caption

## Generated File Format

The workflow creates files in `_src/media/` with proper frontmatter and custom media blocks:

### Single Media Example
```markdown
---
title: "Beautiful Sunset"
post_type: media
published_date: "2025-09-15 20:30 -05:00"
tags: ["photography","nature"]
---

Here's my sunset photo from the beach trip:

:::media
- url: "https://github.com/user/repo/assets/12345/sunset.jpg"
  mediaType: "image"
  aspectRatio: "landscape"
  caption: "Golden hour at the beach"
:::media
```

### Multiple Media Example
```markdown
---
title: "Mountain Views"
post_type: media
published_date: "2025-09-15 20:30 -05:00"
tags: ["photography","nature"]
---

Here are my photos from the mountain trip:

The sunrise was absolutely incredible.

And the sunset was equally beautiful.

:::media
- url: "https://github.com/user/repo/assets/12345/sunrise.jpg"
  mediaType: "image"
  aspectRatio: "landscape"
  caption: "Sunrise at the summit"
- url: "https://github.com/user/repo/assets/12345/sunset.jpg"
  mediaType: "image"
  aspectRatio: "landscape"
  caption: "Evening sunset view"
:::media
```

## Media Type Support

The workflow supports three types of media:

### 🖼️ Images
- **Supported formats**: JPG, PNG, GIF, WebP, etc.
- **Use cases**: Photos, screenshots, artwork, diagrams
- **Example**: Photo blog posts, visual documentation

### 🎥 Videos
- **Supported formats**: MP4, WebM, MOV, etc.
- **Use cases**: Tutorials, demos, vlogs, presentations
- **Example**: Product demonstrations, behind-the-scenes content

### 🎵 Audio
- **Supported formats**: MP3, WAV, OGG, etc.
- **Use cases**: Podcasts, music, voice notes, interviews
- **Example**: Podcast episodes, audio blogs, music sharing

## Features

- ✅ **Media Type Validation**: Ensures only valid media types (image, video, audio)
- ✅ **GitHub Attachment Integration**: Uses native drag-and-drop functionality
- ✅ **Automatic Caption Extraction**: Alt text becomes captions automatically
- ✅ **Multiple Media Support**: Supports multiple media files in a single post
- ✅ **Content Mixing**: Natural integration of text and media content
- ✅ **Custom Media Blocks**: Generates :::media::: blocks for proper rendering
- ✅ **Auto Slug Generation**: Creates URL-friendly slugs from titles
- ✅ **Optional Field Handling**: Gracefully handles missing orientation and fields
- ✅ **Tag Processing**: Handles comma-separated tags with deduplication
- ✅ **Timezone Aware**: Generates proper EST timestamps
- ✅ **Error Handling**: Clear error messages for invalid input
- ✅ **Pull Request Workflow**: Review changes before publishing
- ✅ **Consolidated Architecture**: Integrates seamlessly with existing workflows

## Field Reference

| Field | Type | Required | Description | Example |
|-------|------|----------|-------------|---------|
| Media Type | Dropdown | Yes | Type of media content | `image`, `video`, `audio` |
| Title | Text | Yes | Descriptive title for the post | `"Beautiful Mountain View"` |
| Content and Attachments | Textarea | Yes | Content with drag-and-drop media | Text with `![alt](URL)` images |
| Orientation | Dropdown | No | Aspect ratio for display | `landscape`, `portrait` |
| Slug | Text | No | Custom URL slug | `mountain-view` |
| Tags | Text | No | Comma-separated tags | `photography, nature, hiking` |

## Default Behaviors

- **Orientation**: Defaults to `landscape` if not specified
- **Caption**: Uses alt text from markdown images, filename as fallback
- **Slug**: Auto-generated from title if not provided
- **Tags**: Empty array if not provided
- **Content Processing**: Images are extracted and moved to :::media::: block

## Troubleshooting

### Common Issues

**❌ "Media type must be one of: image, video, audio"**
- Solution: Select a valid media type from the dropdown

**❌ "No media attachments found"**
- Solution: Drag and drop media files into the content field
- Ensure files generate `![alt-text](URL)` markdown format

**❌ "Title is required and cannot be empty"**
- Solution: Provide a descriptive title for your media post

**❌ "Content with attachments is required"**
- Solution: Add content and at least one media attachment

**❌ "Invalid attachment URL"**
- Solution: Ensure dragged files generate proper GitHub URLs
- Check that URLs start with `http://` or `https://`

### Workflow Issues

**Issue stays open after submission:**
- Check that you are the repository owner (@lqdev)
- Verify the issue has the "media" label
- Check the Actions tab for workflow execution logs

**Pull request not created:**
- Check the F# script execution in GitHub Actions logs
- Verify media attachments were properly formatted as markdown images
- Ensure at least one media attachment is present

### Content Tips

1. **File Uploads**: Drag and drop files directly into the content textarea
2. **Alt Text**: Provide descriptive alt text for better captions
3. **Mixed Content**: Combine text and media naturally
4. **Format Support**: Use common web formats (JPG, MP4, MP3)

## Testing

The workflow includes a comprehensive test suite (`Scripts/test-media-workflow.sh`) that validates:

- ✅ Single image with markdown content and alt text captions
- ✅ Multiple images with individual captions
- ✅ Video posts with minimal content
- ✅ Error handling for missing attachments
- ✅ Filename fallback for missing alt text
- ✅ Markdown image parsing and content cleanup
- ✅ GitHub attachment format compatibility

Run tests locally:
```bash
./Scripts/test-media-workflow.sh
```

## Technical Implementation

### Workflow Components

1. **Issue Template**: `.github/ISSUE_TEMPLATE/post-media.yml`
2. **Processing Script**: `Scripts/process-media-issue.fsx`
3. **Workflow Integration**: `process-content-issue.yml` (process-media job)
4. **Test Suite**: `Scripts/test-media-workflow.sh`

### Markdown Image Processing

The workflow parses markdown images using regex pattern `!\[([^\]]*)\]\(([^)]+)\)`:
- Extracts URL and alt text from each `![alt-text](URL)` occurrence
- Uses alt text as caption, filename as fallback if alt text is empty
- Removes image markdown from content text
- Generates individual media items in :::media::: block

### Generated Output

The workflow transforms GitHub's native attachment format into structured markdown with:
- Standard frontmatter following existing media patterns
- Custom :::media::: blocks with proper metadata and individual captions
- Clean content with images moved to dedicated media section
- URL-safe filenames and directory structure
- Timezone-aware timestamps in EST (-05:00)

This implementation leverages GitHub's built-in attachment functionality while providing media-specific optimizations and maintaining compatibility with the existing domain model and build system.