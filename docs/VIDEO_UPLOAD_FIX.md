# Video Media Upload Fix - Demonstration

## Problem Statement

When uploading video files (.MP4) via GitHub Issue Form (Issue #722), the generated content incorrectly used:
- Extension: `.jpg` (wrong - should be `.mp4`)
- Media type: `image` (wrong - should be `video`)

Example of the bug:
```markdown
:::media
- url: "https://cdn.lqdev.tech/files/images/20251027_050937_e8a5a067-dfe9-4aa0-975e-3536832e4bcb.jpg"
  mediaType: "image"
  aspectRatio: "landscape"
  caption: "media"
:::media
```

## Root Cause

GitHub attachment URLs don't include file extensions:
```
https://github.com/user-attachments/assets/e8a5a067-dfe9-4aa0-975e-3536832e4bcb
```

The Python upload script (`upload_media.py`) had this problematic code:
```python
# If filename doesn't have extension, try to detect from content
if '.' not in filename:
    # Default to .jpg for images, but this should be rare
    filename += '.jpg'
```

This defaulted ALL files without extensions to `.jpg`, including videos.

## Solution

Implemented content-based file type detection using magic numbers (file signatures):

### 1. Content-Based Detection

```python
def detect_file_extension_from_content(content):
    """
    Detect file extension from file content using magic numbers (file signatures).
    Returns the detected extension (e.g., '.mp4', '.jpg') or None if unknown.
    """
    # MP4 files start with ftyp box (need at least 12 bytes)
    if len(content) >= 12 and content[4:8] == b'ftyp':
        subtype = content[8:12]
        if subtype in (b'isom', b'iso2', b'mp41', b'mp42', b'avc1', b'M4V ', b'M4A '):
            return '.mp4'
        # ... more video types
    
    # JPEG - need at least 3 bytes
    if len(content) >= 3 and content[:3] == b'\xff\xd8\xff':
        return '.jpg'
    
    # ... more image and audio types
```

Supports:
- **Video**: MP4, MKV, AVI, MOV
- **Image**: JPEG, PNG, GIF, WebP, BMP
- **Audio**: MP3, WAV, OGG, FLAC

### 2. HTTP Header Fallback

```python
def detect_extension_from_content_type(content_type):
    """
    Detect file extension from HTTP Content-Type header.
    Returns the detected extension (e.g., '.mp4', '.jpg') or None if unknown.
    """
    content_type = content_type.split(';')[0].strip().lower()
    
    video_types = {
        'video/mp4': '.mp4',
        'video/quicktime': '.mov',
        # ... more mappings
    }
    # ... check mappings
```

### 3. Updated Download Function

```python
def download_from_github(url):
    """
    Download a file from GitHub CDN.
    Returns a tuple of (file_content, detected_extension).
    """
    response = requests.get(url, timeout=30)
    
    # Try Content-Type header first
    content_type = response.headers.get('Content-Type', '')
    detected_ext = detect_extension_from_content_type(content_type)
    
    if detected_ext:
        print(f"  🔍 Detected extension from Content-Type: {detected_ext}")
    else:
        # Fall back to content-based detection
        detected_ext = detect_file_extension_from_content(response.content)
        if detected_ext:
            print(f"  🔍 Detected extension from file content: {detected_ext}")
    
    return response.content, detected_ext
```

### 4. Updated Attachment Processing

```python
# Download from GitHub (now returns content and detected extension)
file_content, detected_ext = download_from_github(github_url)

# Use detected extension instead of defaulting to .jpg
if '.' not in base_filename:
    if detected_ext:
        filename = base_filename + detected_ext
        print(f"  ✅ Using detected extension: {detected_ext}")
    else:
        # Only default to .jpg if detection completely failed
        filename = base_filename + '.jpg'
        print(f"  ⚠️  No extension detected, defaulting to .jpg")
```

## Result - Fixed Output

Now when uploading a video file, the correct output is generated:
```markdown
:::media
- url: "https://cdn.lqdev.tech/files/videos/20251027_050937_e8a5a067-dfe9-4aa0-975e-3536832e4bcb.mp4"
  mediaType: "video"
  aspectRatio: "landscape"
  caption: "media"
:::media
```

Key improvements:
✅ Correct file path: `/files/videos/` instead of `/files/images/`
✅ Correct extension: `.mp4` instead of `.jpg`
✅ Correct media type: `video` instead of `image`

## Testing

### Unit Tests (11/11 passed)
- ✅ MP4 detection
- ✅ MKV detection
- ✅ AVI detection
- ✅ JPEG detection
- ✅ PNG detection
- ✅ GIF detection
- ✅ WebP detection
- ✅ MP3 detection
- ✅ WAV detection
- ✅ OGG detection
- ✅ Content-Type fallback

### Integration Tests (4/4 passed)
- ✅ Video workflow (complete S3 upload simulation)
- ✅ Image workflow (ensure images still work)
- ✅ Content-Type fallback workflow
- ✅ Issue #722 exact scenario

### Existing Workflow Tests (5/5 passed)
- ✅ GitHub drag-and-drop img tag removal
- ✅ Multiple img tag formats
- ✅ YouTube URL formatting
- ✅ Direct media URL conversion
- ✅ F# script integration

## How It Works - Step by Step

1. **User uploads video via GitHub Issue Form**
   - GitHub stores file and returns URL without extension
   - Example: `https://github.com/user-attachments/assets/abc123...`

2. **Python script downloads file**
   - Checks HTTP `Content-Type` header first
   - Falls back to analyzing file content (magic numbers)
   - Detects: This is an MP4 video

3. **Filename generation**
   - Base: `abc123...` (from URL)
   - Detected extension: `.mp4`
   - Final filename: `abc123....mp4`

4. **S3 folder determination**
   - Extension `.mp4` → maps to `videos` folder
   - S3 key: `files/videos/20251027_050937_abc123....mp4`

5. **Media type determination**
   - S3 key contains `/videos/` → media type = `video`
   - Generates correct :::media block

6. **F# script creates markdown**
   - Receives transformed content with :::media blocks
   - Generates final markdown with correct frontmatter

## Benefits

1. **Accurate media type detection** - Videos, images, and audio files are correctly identified
2. **Proper file organization** - Files go into correct S3 folders (videos/, images/, audio/)
3. **Better user experience** - Media displays correctly on the website
4. **Robust detection** - Uses both HTTP headers and file content analysis
5. **Backward compatible** - Existing image uploads continue to work correctly
6. **Comprehensive testing** - 20 tests covering all scenarios

## Edge Cases Handled

1. **Unknown file types** - Still defaults to `.jpg` if detection fails
2. **Short files** - Checks file length before accessing byte ranges
3. **Multiple formats** - Handles various MP4 subtypes (isom, mp41, mp42, M4V, M4A)
4. **Content-Type parameters** - Strips parameters like `video/mp4; codecs="avc1"`
5. **MP3 variations** - Detects both ID3 tags (3 bytes) and frame sync (2 bytes)
