# GitHub Scripts

This directory contains scripts used by GitHub Actions workflows.

## upload_media.py

Python script that handles media file uploads from GitHub Issue Forms to Linode Object Storage.

### Purpose

When creating media posts via GitHub Issue Forms, users can drag and drop files directly into the issue. GitHub temporarily hosts these files and generates markdown like `![filename](https://github.com/user-attachments/...)`. This script:

1. Downloads the files from GitHub's temporary storage
2. Uploads them to permanent Linode S3 storage with date-based organization
3. Transforms the content to use permanent CDN URLs
4. Creates `:::media` blocks for the site generator

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

**Input** (content with GitHub attachment):
```markdown
Here's my photo:

![sunset.jpg](https://github.com/user-attachments/assets/abc123...)

Beautiful evening!
```

**Output** (transformed content):
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

### Integration

This script is called by the `process-content-issue.yml` workflow as part of the media post creation process. It runs before the F# script that generates the final markdown file.

### Dependencies

- `boto3` - AWS SDK for Python (S3 operations)
- `requests` - HTTP library for downloading files

Dependencies are installed via `uv` in the GitHub Actions workflow.
