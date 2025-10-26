# GitHub Scripts

This directory contains scripts used by GitHub Actions workflows.

## upload_media.py

Python script that handles media file uploads from GitHub Issue Forms to Linode Object Storage.

### Purpose

When creating media posts via GitHub Issue Forms, users can:
1. **Upload files** - Drag and drop or attach images/videos/audio
2. **Paste YouTube/Vimeo URLs** - Direct video links for easy mobile publishing

The script handles both types:

**For uploaded files:**
1. Downloads from GitHub's temporary storage
2. Uploads to permanent Linode S3 storage with timestamp-based organization
3. Transforms content to use permanent CDN URLs
4. Creates `:::media` blocks for the site generator

**For direct video URLs (YouTube, Vimeo):**
1. Detects video platform URLs
2. Formats them into proper `:::media` blocks
3. Adds platform-specific metadata (videoId, platform)
4. No upload needed - videos stay on their platforms

### Supported Upload Formats

**GitHub Attachments:**
- Markdown syntax: `![alt text](url)`
- HTML img tags: `<img src="url" alt="alt">` (from drag-and-drop/paste)
- Plain URLs: `https://github.com/user-attachments/assets/...`

**Direct Video URLs:**
- YouTube: `https://www.youtube.com/watch?v=...` or `https://youtu.be/...`
- Vimeo: `https://vimeo.com/...`

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

**Example 2: Direct YouTube URL**

**Input:**
```markdown
Check out this video:

https://www.youtube.com/watch?v=dQw4w9WgXcQ

Really interesting content!
```

**Output:**
```markdown
Check out this video:

Really interesting content!

:::media
- url: "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
  mediaType: "video"
  platform: "youtube"
  videoId: "dQw4w9WgXcQ"
  aspectRatio: "landscape"
:::media
```

**Example 3: HTML img tag (drag-and-drop upload)**

**Input:**
```markdown
My vacation photo:

<img src="https://github.com/user-attachments/assets/def456" alt="beach">
```

**Output:**
```markdown
Here's my photo:

Beautiful evening!

:::media
- url: "https://cdn.luisquintanilla.me/files/images/20251026_140530_beach.jpg"
  mediaType: "image"
  aspectRatio: "landscape"
  caption: "beach"
:::media
```

### Integration

This script is called by the `process-content-issue.yml` workflow as part of the media post creation process. It runs before the F# script that generates the final markdown file.

### Dependencies

- `boto3` - AWS SDK for Python (S3 operations)
- `requests` - HTTP library for downloading files

Dependencies are installed via `uv` in the GitHub Actions workflow.
