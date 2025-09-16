# Media Publishing Workflow

This document explains how to use the GitHub Issue Forms workflow to publish media posts (images, videos, audio) to your website.

## How to Create a Media Post

1. **Go to Issues**: Navigate to your repository's Issues tab
2. **Create New Issue**: Click "New issue"
3. **Select Template**: Choose "📷 Post Media" template
4. **Fill out the form**:
   - **Media Type**: Select image, video, or audio (required)
   - **Title**: A descriptive title for your media post (required)
   - **Attachment**: Upload your media file and paste the generated URL (required)
   - **Caption**: A caption or description for your media (optional)
   - **Orientation**: Select landscape or portrait aspect ratio (optional)
   - **Slug**: Custom URL slug (optional - auto-generated if not provided)
   - **Tags**: Comma-separated tags (optional)

5. **Submit Issue**: The workflow will automatically:
   - Validate your input and media type
   - Generate a properly formatted markdown file with :::media::: block
   - Create a pull request with your content
   - Close the issue automatically

## Attachment Upload Process

The attachment field requires a specific process:

1. **Click the attachment area** in the issue form
2. **Upload your media file** (image, video, or audio)
3. **GitHub generates a URL** like: `https://github.com/user/repo/assets/123/filename.ext`
4. **Copy and paste that URL** into the attachment field

**Example attachment URLs:**
- Images: `https://github.com/lqdev/luisquintanilla.me/assets/11130940/sunset.jpg`
- Videos: `https://cdn.lqdev.tech/files/videos/demo.mp4`
- Audio: `https://cdn.lqdev.tech/files/audio/podcast.mp3`

## Generated File Format

The workflow creates files in `_src/media/` with proper frontmatter and custom media blocks:

### Full Example (with all fields)
```markdown
---
title: "Beautiful Sunset"
post_type: media
published_date: "2025-09-15 20:30 -05:00"
tags: ["photography","nature","travel"]
---

:::media
- url: "https://cdn.lqdev.tech/files/images/sunset.jpg"
  mediaType: "image"
  aspectRatio: "landscape"
  caption: "Golden hour at the beach"
:::media
```

### Minimal Example (required fields only)
```markdown
---
title: "Test Video"
post_type: media
published_date: "2025-09-15 20:30 -05:00"
---

:::media
- url: "https://cdn.lqdev.tech/files/videos/test.mp4"
  mediaType: "video"
  aspectRatio: "landscape"
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
- ✅ **URL Validation**: Checks for proper HTTP/HTTPS URLs
- ✅ **Custom Media Blocks**: Generates :::media::: blocks for proper rendering
- ✅ **Auto Slug Generation**: Creates URL-friendly slugs from titles
- ✅ **Optional Fields**: Gracefully handles missing caption and orientation
- ✅ **Tag Processing**: Handles comma-separated tags with deduplication
- ✅ **Timezone Aware**: Generates proper EST timestamps
- ✅ **Error Handling**: Clear error messages for invalid input
- ✅ **Pull Request Workflow**: Review changes before publishing
- ✅ **Consolidated Processing**: Uses single workflow to avoid dual triggering

## Field Reference

| Field | Type | Required | Description | Example |
|-------|------|----------|-------------|---------|
| Media Type | Dropdown | Yes | Type of media content | `image`, `video`, `audio` |
| Title | Text | Yes | Descriptive title for the post | `"Beautiful Mountain View"` |
| Attachment | Textarea | Yes | GitHub-generated URL after upload | `https://github.com/.../image.jpg` |
| Caption | Text | No | Description or caption text | `"Taken during my hiking trip"` |
| Orientation | Dropdown | No | Aspect ratio for display | `landscape`, `portrait` |
| Slug | Text | No | Custom URL slug | `mountain-view` |
| Tags | Text | No | Comma-separated tags | `photography, nature, hiking` |

## Default Behaviors

- **Orientation**: Defaults to `landscape` if not specified
- **Caption**: Omitted from output if empty
- **Slug**: Auto-generated from title if not provided
- **Tags**: Empty array if not provided
- **Aspect Ratio**: Uses orientation value or defaults to landscape

## Troubleshooting

### Common Issues

**❌ "Media type must be one of: image, video, audio"**
- Solution: Select a valid media type from the dropdown

**❌ "Attachment URL must be a valid HTTP/HTTPS URL"**
- Solution: Ensure you've uploaded a file and copied the GitHub-generated URL
- Check that the URL starts with `http://` or `https://`

**❌ "Title is required and cannot be empty"**
- Solution: Provide a descriptive title for your media post

**❌ "Missing required arguments"**
- Solution: Fill out all required fields (Media Type, Title, Attachment)

### Workflow Issues

**Issue stays open after submission:**
- Check that you are the repository owner (@lqdev)
- Verify the issue has the "media" label
- Check the Actions tab for workflow execution logs

**Pull request not created:**
- Check the F# script execution in GitHub Actions logs
- Verify all required fields were filled correctly
- Ensure attachment URL is accessible

### File Upload Tips

1. **File Size**: Keep media files reasonable size for web use
2. **Format Support**: Use common web formats (JPG, MP4, MP3)
3. **URL Copying**: Make sure to copy the complete GitHub-generated URL
4. **External URLs**: Can use external CDN URLs if preferred

## Testing

The workflow includes a comprehensive test suite (`Scripts/test-media-workflow.sh`) that validates:

- ✅ Image posts with full metadata
- ✅ Video posts with minimal fields
- ✅ Audio posts with custom orientation
- ✅ Error handling for invalid media types
- ✅ Error handling for missing arguments
- ✅ Proper :::media::: block generation
- ✅ Frontmatter format validation

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

### Generated Output

The workflow transforms form input into structured markdown with:
- Standard frontmatter following existing media patterns
- Custom :::media::: blocks with proper metadata
- URL-safe filenames and directory structure
- Timezone-aware timestamps in EST (-05:00)

This implementation follows the proven pattern established by the bookmark (#259→#260) and response (#249→#250) publishing workflows while being specifically optimized for media content requirements.