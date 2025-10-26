# GitHub Scripts

This directory contains scripts used by GitHub Actions workflows.

## upload_media.py

Python script that handles media file uploads from GitHub Issue Forms to Linode Object Storage.

### Purpose

When creating media posts via GitHub Issue Forms, users can upload files and include video URLs that are automatically processed:

**For uploaded files (GitHub attachments):**
1. Downloads from GitHub's temporary storage
2. Uploads to permanent Linode S3 storage with timestamp-based organization
3. Transforms content to use permanent CDN URLs
4. Creates `:::media` blocks for the site generator

**For YouTube URLs:**
- Automatically converted to clickable thumbnail format
- Format: `<a href="VIDEO_URL"><img src="THUMBNAIL_URL"></a>`
- Enables easy mobile publishing without manual formatting
- Uses YouTube's thumbnail service for preview images

### Supported Upload Formats

**GitHub Attachments (processed and uploaded to S3):**
- Markdown syntax: `![alt text](url)`
- HTML img tags: `<img src="url" alt="alt">` (from drag-and-drop/paste)
- Plain URLs: `https://github.com/user-attachments/assets/...`

**YouTube URLs (converted to thumbnail format):**
- `https://www.youtube.com/watch?v=VIDEO_ID`
- `https://youtu.be/VIDEO_ID`

**Other URLs (left unchanged):**
- Vimeo and other video platform URLs remain in content
- External image URLs remain in content

### Usage

```bash
python upload_media.py <content-file>
```

The script reads content from the specified file, processes it, and writes the transformed content back to the same file.

### Environment Variables

Required environment variables (set as GitHub Secrets):

- `LINODE_STORAGE_ACCESS_KEY_ID` - Linode Object Storage access key
- `LINODE_STORAGE_SECRET_ACCESS_KEY` - Linode Object Storage secret key
- `LINODE_STORAGE_ENDPOINT_URL` - Linode endpoint (e.g., `https://us-east-1.linodeobjects.com`)
- `LINODE_STORAGE_BUCKET_NAME` - Name of the S3 bucket
- `LINODE_STORAGE_CUSTOM_DOMAIN` (optional) - Custom CDN domain (e.g., `https://cdn.luisquintanilla.me`)

### File Organization

Uploaded files are organized by media type with timestamp-prefixed filenames:

```
/files/images/20251026_140530_filename.jpg
/files/videos/20251026_140530_filename.mp4
/files/audio/20251026_140530_filename.mp3
```

The timestamp prefix format is `YYYYMMDD_HHMMSS_` which:
- Ensures chronological ordering
- Prevents filename conflicts
- Makes browsing media chronologically easy
- Maintains consistency with Discord bot uploads

### Supported Media Types

- **Images**: .jpg, .jpeg, .png, .gif, .webp, .bmp, .svg, .ico
- **Videos**: .mp4, .webm, .mov, .avi, .mkv, .flv, .wmv, .m4v
- **Audio**: .mp3, .wav, .ogg, .m4a, .flac, .aac, .wma

### Example Input/Output

**Example 1: GitHub Attachment (uploaded file)**

**Input:**
```markdown
Here's my photo:

![sunset.jpg](https://github.com/user-attachments/assets/abc123...)

Beautiful evening!
```

**Output:**
```markdown
Here's my photo:

Beautiful evening!

:::media
- url: "https://cdn.luisquintanilla.me/files/images/20251026_140530_sunset.jpg"
  mediaType: "image"
  aspectRatio: "landscape"
  caption: "sunset.jpg"
:::media
```

**Example 2: HTML img tag (drag-and-drop upload)**

**Input:**
```markdown
My vacation photo:

<img src="https://github.com/user-attachments/assets/def456" alt="beach">
```

**Output:**
```markdown
My vacation photo:

:::media
- url: "https://cdn.luisquintanilla.me/files/images/20251026_140530_beach.jpg"
  mediaType: "image"
  aspectRatio: "landscape"
  caption: "beach"
:::media
```

**Example 3: YouTube URL (automatic thumbnail formatting)**

**Input:**
```markdown
Check out this video:

https://www.youtube.com/watch?v=dQw4w9WgXcQ

Really interesting content!
```

**Output:**
```markdown
Check out this video:

<a href="https://www.youtube.com/watch?v=dQw4w9WgXcQ"><img src="http://img.youtube.com/vi/dQw4w9WgXcQ/0.jpg"></a>

Really interesting content!
```

*Note: YouTube URLs are automatically converted to clickable thumbnail format for easy mobile publishing.*

### Integration

This script is called by the `process-content-issue.yml` workflow as part of the media post creation process. It runs before the F# script that generates the final markdown file.

### Dependencies

- `boto3` - AWS SDK for Python (S3 operations)
- `requests` - HTTP library for downloading files

Dependencies are installed via `uv` in the GitHub Actions workflow.
