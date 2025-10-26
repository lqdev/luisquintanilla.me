# GitHub Scripts

This directory contains scripts used by GitHub Actions workflows.

## upload_media.py

Python script that handles media file uploads from GitHub Issue Forms to Linode Object Storage.

### Purpose

When creating media posts via GitHub Issue Forms, users can upload files and include media URLs that are automatically processed:

**For uploaded files (GitHub attachments):**
1. Downloads from GitHub's temporary storage
2. Uploads to permanent Linode S3 storage with timestamp-based organization
3. Transforms content to use permanent CDN URLs
4. Creates `:::media` blocks for the site generator

**For YouTube URLs:**
- Automatically converted to Markdown clickable thumbnail format
- Format: `[![Video](THUMBNAIL_URL)](VIDEO_URL "Video")`
- Enables easy mobile publishing without manual formatting
- Uses YouTube's thumbnail service for preview images

**For direct media URLs (non-GitHub, non-YouTube):**
- Detects direct URLs to images, videos, and audio files
- Wraps them in `:::media` blocks for proper rendering
- Overcomes GitHub file size and type restrictions
- Supports any publicly accessible media URL

### Supported Upload Formats

**GitHub Attachments (processed and uploaded to S3):**
- Markdown syntax: `![alt text](url)`
- HTML img tags: `<img src="url" alt="alt">` (from drag-and-drop/paste)
- Plain URLs: `https://github.com/user-attachments/assets/...`

**YouTube URLs (converted to Markdown thumbnail format):**
- `https://www.youtube.com/watch?v=VIDEO_ID`
- `https://youtu.be/VIDEO_ID`

**Direct Media URLs (wrapped in :::media blocks):**
- Images: `.jpg`, `.jpeg`, `.png`, `.gif`, `.webp`, `.bmp`, `.svg`, `.ico`
- Videos: `.mp4`, `.webm`, `.mov`, `.avi`, `.mkv`, `.flv`, `.wmv`, `.m4v`
- Audio: `.mp3`, `.wav`, `.ogg`, `.m4a`, `.flac`, `.aac`, `.wma`

**Other URLs:**
- Vimeo and other video platform URLs remain unchanged
- URLs without media extensions remain unchanged

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

**Example 3: YouTube URL (Markdown thumbnail formatting)**

**Input:**
```markdown
Check out this video:

https://www.youtube.com/watch?v=dQw4w9WgXcQ

Really interesting content!
```

**Output:**
```markdown
Check out this video:

[![Video](http://img.youtube.com/vi/dQw4w9WgXcQ/0.jpg)](https://www.youtube.com/watch?v=dQw4w9WgXcQ "Video")

Really interesting content!
```

*Note: YouTube URLs are automatically converted to Markdown clickable thumbnail format for easy mobile publishing.*

**Example 4: Direct media URL (wrapped in :::media block)**

**Input:**
```markdown
Check out this diagram:

https://example.com/architecture-diagram.png

And this tutorial video:

https://example.com/tutorial.mp4
```

**Output:**
```markdown
Check out this diagram:

And this tutorial video:

:::media
- url: "https://example.com/architecture-diagram.png"
  mediaType: "image"
  aspectRatio: "landscape"
  caption: "architecture-diagram.png"
:::media

:::media
- url: "https://example.com/tutorial.mp4"
  mediaType: "video"
  aspectRatio: "landscape"
  caption: "tutorial.mp4"
:::media
```

*Note: Direct media URLs are detected by file extension and wrapped in :::media blocks. This overcomes GitHub file size and type restrictions.*

### Integration

This script is called by the `process-content-issue.yml` workflow as part of the media post creation process. It runs before the F# script that generates the final markdown file.

### Dependencies

- `boto3` - AWS SDK for Python (S3 operations)
- `requests` - HTTP library for downloading files

Dependencies are installed via `uv` in the GitHub Actions workflow.

### S3 Configuration

The script is configured to work with Linode Object Storage (S3-compatible) using the exact same configuration as the discord-publish-bot (which works successfully):

- **boto3 Version**: Uses boto3==1.34.0 and botocore==1.34.0 (same versions as discord-publish-bot)
- **Region Detection**: Automatically extracts region from endpoint URL (e.g., `us-east-1` from `https://us-east-1.linodeobjects.com`)
- **Signature Version**: Uses `s3v4` for compatibility with Linode
- **Addressing Style**: Virtual-hosted style addressing for proper URL formation
- **Parameter Order**: Uses `endpoint_url` first, matching discord-publish-bot implementation
- **boto3 Config**: Properly configured via `botocore.config.Config` for S3-compatible storage

This configuration matches the working discord-publish-bot implementation exactly, including using the same boto3/botocore versions to ensure consistent behavior.

### Troubleshooting

#### S3 Connection Errors

**Error**: `Connection was closed before we received a valid response from endpoint URL`

**Causes**:
- Missing `region_name` parameter in boto3 client initialization
- Missing or incorrect boto3 Config settings
- Incorrect parameter order in boto3.client() call
- Invalid endpoint URL format
- Incorrect credentials

**Solutions**:
- Verify endpoint URL format: `https://REGION.linodeobjects.com` (e.g., `https://us-east-1.linodeobjects.com`)
- Ensure all required environment variables are set
- Check that region is being extracted correctly from endpoint URL
- Verify boto3 Config includes:
  - `signature_version='s3v4'`
  - `addressing_style='virtual'`
- Use the same parameter order as discord-publish-bot (endpoint_url first)
- Verify credentials are correct and have proper permissions

#### Upload Failures

**Error**: `Error processing attachment: <various boto3 errors>`

**Common causes**:
- Invalid S3 credentials
- Incorrect bucket name
- Missing bucket permissions
- Bucket does not exist

**Solutions**:
- Verify all secrets are correctly configured in GitHub repository settings
- Check bucket exists and is accessible with provided credentials
- Ensure bucket has proper CORS and ACL settings for public-read access
- Test credentials using AWS CLI or boto3 directly

#### No Attachments Detected

**Issue**: Script reports "No GitHub attachments found" but files were uploaded

**Causes**:
- GitHub Issue Form content not in expected format
- Files uploaded but markdown not generated
- Unsupported markdown syntax

**Solutions**:
- Ensure files are dragged into the "Content and Attachments" field
- Check that GitHub generated proper markdown (should see `![filename](url)` or `<img src="url">`)
- Review workflow logs to see actual issue content
- Verify attachment URLs match expected patterns (`https://github.com/user-attachments/...`)
