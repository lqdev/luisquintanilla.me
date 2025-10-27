#!/usr/bin/env python3
"""
Media Upload Script for GitHub Issue Forms
Migrated from discord-publish-bot to luisquintanilla.me

This script:
1. Parses GitHub attachment URLs from issue content
2. Downloads media files from GitHub CDN
3. Uploads to Linode S3 with proper directory structure
4. Transforms markdown to use permanent CDN URLs
"""

import os
import sys
import re
import requests
import boto3
from botocore.config import Config
from datetime import datetime
from urllib.parse import urlparse
from pathlib import Path


def sanitize_filename(filename):
    """
    Sanitize filename for S3 storage.
    Adapted from discord-publish-bot storage logic.
    """
    # Remove path components and get just the filename
    filename = os.path.basename(filename)
    
    # Convert to lowercase
    filename = filename.lower()
    
    # Replace spaces with hyphens
    filename = filename.replace(' ', '-')
    
    # Remove or replace problematic characters
    filename = re.sub(r'[^a-z0-9\-_.]', '', filename)
    
    # Remove multiple consecutive hyphens
    filename = re.sub(r'-+', '-', filename)
    
    # Ensure filename is not empty
    if not filename or filename == '.':
        filename = 'file'
    
    return filename


def get_media_type_folder(url, filename):
    """
    Determine the media type folder based on file extension.
    Adapted from discord-publish-bot storage logic.
    """
    # Get file extension
    ext = Path(filename).suffix.lower()
    
    # Image extensions
    image_exts = ['.jpg', '.jpeg', '.png', '.gif', '.webp', '.bmp', '.svg', '.ico']
    if ext in image_exts:
        return 'images'
    
    # Video extensions
    video_exts = ['.mp4', '.webm', '.mov', '.avi', '.mkv', '.flv', '.wmv', '.m4v']
    if ext in video_exts:
        return 'videos'
    
    # Audio extensions
    audio_exts = ['.mp3', '.wav', '.ogg', '.m4a', '.flac', '.aac', '.wma']
    if ext in audio_exts:
        return 'audio'
    
    # Default to 'files' for unknown types
    return 'files'


def detect_file_extension_from_content(content):
    """
    Detect file extension from file content using magic numbers (file signatures).
    Returns the detected extension (e.g., '.mp4', '.jpg') or None if unknown.
    """
    if not content:
        return None
    
    # Video file signatures
    # MP4 files start with ftyp box (need at least 12 bytes)
    if len(content) >= 12 and content[4:8] == b'ftyp':
        # Check common MP4 subtypes
        subtype = content[8:12]
        if subtype in (b'isom', b'iso2', b'mp41', b'mp42', b'avc1', b'M4V ', b'M4A '):
            return '.mp4'
        elif subtype == b'M4V ':
            return '.m4v'
        elif subtype == b'M4A ':
            return '.m4a'
        return '.mp4'  # Default to .mp4 for other ftyp variants
    
    # WebM/MKV (Matroska) - need at least 4 bytes
    if len(content) >= 4 and content[:4] == b'\x1a\x45\xdf\xa3':
        return '.mkv'
    
    # AVI - need at least 12 bytes
    if len(content) >= 12 and content[:4] == b'RIFF' and content[8:12] == b'AVI ':
        return '.avi'
    
    # MOV (QuickTime) - similar to MP4 but with different atoms - need at least 4 bytes
    if len(content) >= 4 and content[:4] in (b'moov', b'mdat', b'wide', b'free'):
        return '.mov'
    
    # Image file signatures
    # JPEG - need at least 3 bytes
    if len(content) >= 3 and content[:3] == b'\xff\xd8\xff':
        return '.jpg'
    
    # PNG - need at least 8 bytes
    if len(content) >= 8 and content[:8] == b'\x89PNG\r\n\x1a\n':
        return '.png'
    
    # GIF - need at least 6 bytes
    if len(content) >= 6 and content[:6] in (b'GIF87a', b'GIF89a'):
        return '.gif'
    
    # WebP - need at least 12 bytes
    if len(content) >= 12 and content[:4] == b'RIFF' and content[8:12] == b'WEBP':
        return '.webp'
    
    # BMP - need at least 2 bytes
    if len(content) >= 2 and content[:2] == b'BM':
        return '.bmp'
    
    # Audio file signatures
    # MP3 - ID3 tag is 3 bytes, frame sync is 2 bytes
    if len(content) >= 3 and content[:3] == b'ID3':
        return '.mp3'
    if len(content) >= 2 and content[:2] in (b'\xff\xfb', b'\xff\xf3', b'\xff\xf2'):
        return '.mp3'
    
    # WAV - need at least 12 bytes
    if len(content) >= 12 and content[:4] == b'RIFF' and content[8:12] == b'WAVE':
        return '.wav'
    
    # OGG - need at least 4 bytes
    if len(content) >= 4 and content[:4] == b'OggS':
        return '.ogg'
    
    # FLAC - need at least 4 bytes
    if len(content) >= 4 and content[:4] == b'fLaC':
        return '.flac'
    
    return None


def detect_extension_from_content_type(content_type):
    """
    Detect file extension from HTTP Content-Type header.
    Returns the detected extension (e.g., '.mp4', '.jpg') or None if unknown.
    """
    if not content_type:
        return None
    
    # Normalize content type (remove parameters like charset)
    content_type = content_type.split(';')[0].strip().lower()
    
    # Video types
    video_types = {
        'video/mp4': '.mp4',
        'video/mpeg': '.mp4',
        'video/quicktime': '.mov',
        'video/x-msvideo': '.avi',
        'video/x-matroska': '.mkv',
        'video/webm': '.webm',
        'video/x-flv': '.flv',
        'video/x-ms-wmv': '.wmv',
    }
    
    # Image types
    image_types = {
        'image/jpeg': '.jpg',
        'image/png': '.png',
        'image/gif': '.gif',
        'image/webp': '.webp',
        'image/bmp': '.bmp',
        'image/svg+xml': '.svg',
        'image/x-icon': '.ico',
    }
    
    # Audio types
    audio_types = {
        'audio/mpeg': '.mp3',
        'audio/mp3': '.mp3',
        'audio/wav': '.wav',
        'audio/wave': '.wav',
        'audio/x-wav': '.wav',
        'audio/ogg': '.ogg',
        'audio/flac': '.flac',
        'audio/aac': '.aac',
        'audio/x-m4a': '.m4a',
    }
    
    # Check all type mappings
    for mapping in (video_types, image_types, audio_types):
        if content_type in mapping:
            return mapping[content_type]
    
    return None


def download_from_github(url):
    """
    Download a file from GitHub CDN.
    Returns a tuple of (file_content, detected_extension).
    The detected_extension is determined from Content-Type header and/or file content.
    """
    print(f"  📥 Downloading from: {url}")
    response = requests.get(url, timeout=30)
    response.raise_for_status()
    print(f"  ✅ Downloaded {len(response.content)} bytes")
    
    # Try to detect extension from Content-Type header
    content_type = response.headers.get('Content-Type', '')
    detected_ext = detect_extension_from_content_type(content_type)
    
    if detected_ext:
        print(f"  🔍 Detected extension from Content-Type '{content_type}': {detected_ext}")
    else:
        # Fall back to content-based detection
        detected_ext = detect_file_extension_from_content(response.content)
        if detected_ext:
            print(f"  🔍 Detected extension from file content: {detected_ext}")
        else:
            print(f"  ⚠️  Could not detect file type from Content-Type or content")
    
    return response.content, detected_ext


def upload_to_s3(file_content, filename, s3_client, bucket_name):
    """
    Upload file to Linode S3 with timestamp-prefixed filename.
    Returns the S3 key (path) where the file was uploaded.
    """
    # Get current timestamp for filename prefix
    now = datetime.now()
    timestamp = now.strftime('%Y%m%d_%H%M%S')
    
    # Sanitize filename
    clean_filename = sanitize_filename(filename)
    
    # Determine media type folder
    media_folder = get_media_type_folder(filename, clean_filename)
    
    # Create timestamp-prefixed filename to prevent conflicts
    timestamped_filename = f"{timestamp}_{clean_filename}"
    
    # Create S3 key with flat structure: /files/{type}/{timestamp}_{filename}
    s3_key = f"files/{media_folder}/{timestamped_filename}"
    
    print(f"  📤 Uploading to S3: {s3_key}")
    
    # Upload to S3
    s3_client.put_object(
        Bucket=bucket_name,
        Key=s3_key,
        Body=file_content,
        ACL='public-read'  # Make file publicly accessible
    )
    
    print(f"  ✅ Uploaded successfully")
    return s3_key


def generate_permanent_url(s3_key, endpoint_url, bucket_name, custom_domain=None):
    """
    Generate the permanent CDN URL for an uploaded file.
    
    If custom_domain is set, use that (e.g., https://cdn.luisquintanilla.me)
    Otherwise, use the standard Linode Object Storage URL.
    """
    if custom_domain:
        # Use custom domain (e.g., https://cdn.luisquintanilla.me/files/images/...)
        return f"{custom_domain.rstrip('/')}/{s3_key}"
    else:
        # Use standard Linode URL (e.g., https://bucket-name.us-east-1.linodeobjects.com/files/...)
        # Extract region from endpoint URL
        parsed = urlparse(endpoint_url)
        hostname = parsed.netloc
        # Expected format: us-east-1.linodeobjects.com
        return f"https://{bucket_name}.{hostname}/{s3_key}"


def extract_github_attachments(content):
    """
    Extract all GitHub attachment URLs from content.
    Supports multiple GitHub upload formats:
    - Markdown: ![alt](url)
    - HTML img tags: <img src="url" alt="alt">
    - Plain URLs: https://github.com/user-attachments/...
    
    Returns list of (url, alt_text) tuples.
    """
    attachments = []
    
    # Pattern 1: Markdown images ![alt](url)
    markdown_pattern = r'!\[([^\]]*)\]\((https://github\.com/user-attachments/[^)]+)\)'
    for match in re.finditer(markdown_pattern, content):
        alt_text = match.group(1).strip()
        url = match.group(2).strip()
        attachments.append((url, alt_text or 'media'))
    
    # Pattern 2: HTML img tags (for drag-and-drop and paste uploads)
    # Match both quoted and unquoted src attributes
    # Handles: src="URL", src='URL', and src=URL
    html_pattern = r'<img[^>]*src=(["\']?)(https://github\.com/user-attachments/[^"\'\s>]+)\1[^>]*>'
    for match in re.finditer(html_pattern, content):
        url = match.group(2).strip()  # URL is now in group 2
        # Try to extract alt text if present (handle both quoted and unquoted)
        # For quoted: match everything inside quotes
        # For unquoted: match non-whitespace, non-quote, non-> characters
        alt_match = re.search(r'alt=(["\'])([^\1]+?)\1|alt=([^\s>]+)', match.group(0))
        if alt_match:
            alt_text = alt_match.group(2) if alt_match.group(2) else alt_match.group(3)
        else:
            alt_text = 'media'
        # Skip if already found in markdown pattern
        if not any(url == existing_url for existing_url, _ in attachments):
            attachments.append((url, alt_text))
    
    # Pattern 3: Plain GitHub attachment URLs (for videos/audio)
    plain_pattern = r'(https://github\.com/user-attachments/assets/[a-zA-Z0-9\-]+)'
    for match in re.finditer(plain_pattern, content):
        url = match.group(1).strip()
        # Skip if already found in other patterns
        if not any(url == existing_url for existing_url, _ in attachments):
            attachments.append((url, 'media'))
    
    return attachments


def extract_and_format_youtube_urls(content):
    """
    Extract YouTube URLs and convert them to Markdown thumbnail syntax.
    
    Input:  https://youtube.com/watch?v=abc123
    Output: [![Video](http://img.youtube.com/vi/abc123/0.jpg)](https://youtube.com/watch?v=abc123 "Video")
    
    Returns list of (original_url, formatted_markdown, video_id) tuples.
    """
    youtube_urls = []
    
    # YouTube patterns
    youtube_patterns = [
        (r'https?://(?:www\.)?youtube\.com/watch\?v=([a-zA-Z0-9_-]+)', 'youtube.com'),
        (r'https?://(?:www\.)?youtu\.be/([a-zA-Z0-9_-]+)', 'youtu.be'),
    ]
    
    for pattern, source_type in youtube_patterns:
        for match in re.finditer(pattern, content):
            url = match.group(0)
            video_id = match.group(1)
            
            # Skip if already found
            if any(url == existing_url for existing_url, _, _ in youtube_urls):
                continue
            
            # Generate Markdown thumbnail syntax
            thumbnail_url = f"http://img.youtube.com/vi/{video_id}/0.jpg"
            formatted_markdown = f'[![Video]({thumbnail_url})]({url} "Video")'
            
            youtube_urls.append((url, formatted_markdown, video_id))
    
    return youtube_urls


def extract_direct_media_urls(content):
    """
    Extract direct media URLs (images, videos, audio) that are not GitHub attachments.
    These will be wrapped in :::media blocks.
    
    Detects URLs ending with common media extensions.
    Excludes URLs already in :::media blocks.
    
    Returns list of (url, media_type) tuples.
    """
    direct_media = []
    
    # Media file extensions by type
    image_extensions = r'\.(jpg|jpeg|png|gif|webp|bmp|svg|ico)'
    video_extensions = r'\.(mp4|webm|mov|avi|mkv|flv|wmv|m4v)'
    audio_extensions = r'\.(mp3|wav|ogg|m4a|flac|aac|wma)'
    
    # Pattern for direct media URLs (not GitHub attachments)
    # Must be a complete URL and not already part of markdown image syntax
    patterns = [
        (rf'https?://(?!github\.com/user-attachments)[^\s<>\[\]()]+{image_extensions}', 'image'),
        (rf'https?://(?!github\.com/user-attachments)[^\s<>\[\]()]+{video_extensions}', 'video'),
        (rf'https?://(?!github\.com/user-attachments)[^\s<>\[\]()]+{audio_extensions}', 'audio'),
    ]
    
    for pattern, media_type in patterns:
        for match in re.finditer(pattern, content, re.IGNORECASE):
            url = match.group(0)
            
            # Skip if already found
            if any(url == existing_url for existing_url, _ in direct_media):
                continue
            
            # Check if URL is already wrapped in markdown image syntax
            # Look for ![...](...url...)
            markdown_img_pattern = rf'!\[[^\]]*\]\([^)]*{re.escape(url)}[^)]*\)'
            if re.search(markdown_img_pattern, content):
                continue
            
            # Check if URL is already in a :::media block
            # Look for :::media...url: "..."...:::media
            # Use specific pattern to match media blocks efficiently
            media_block_start = content.find(':::media')
            while media_block_start != -1:
                media_block_end = content.find(':::media', media_block_start + 8)
                if media_block_end == -1:
                    break
                media_block = content[media_block_start:media_block_end + 8]
                if url in media_block:
                    # URL is already in a media block, skip it
                    break
                media_block_start = content.find(':::media', media_block_end + 8)
            else:
                # URL not found in any media block
                direct_media.append((url, media_type))
                continue
            
            # If we broke out of the while loop, URL was found in a media block
            continue
            
            direct_media.append((url, media_type))
    
    return direct_media


def find_all_media_positions(content, github_attachments, youtube_urls, direct_media_urls):
    """
    Find all media items in the content with their positions.
    Returns a list of (position, match_text, media_type, data) tuples sorted by position.
    
    This is used to replace media items in-place while preserving their order.
    """
    media_items = []
    
    # Find GitHub attachments
    for github_url, alt_text in github_attachments:
        # Pattern 1: Markdown images ![alt](url)
        markdown_pattern = rf'!\[[^\]]*\]\({re.escape(github_url)}\)'
        markdown_matches = list(re.finditer(markdown_pattern, content))
        print(f"📝 DEBUG find_positions: GitHub URL {github_url[:60]}... - Markdown pattern matches: {len(markdown_matches)}")
        for match in markdown_matches:
            media_items.append((
                match.start(),
                match.group(0),
                'github_attachment',
                {'url': github_url, 'alt_text': alt_text}
            ))
            print(f"  - Found at position {match.start()} via markdown pattern")
        
        # Pattern 2: HTML img tags (handle both quoted and unquoted src)
        # Matches: src="URL", src='URL', and src=URL
        html_pattern = rf'<img[^>]*src=(["\']?)({re.escape(github_url)})\1[^>]*>'
        html_matches = list(re.finditer(html_pattern, content))
        print(f"📝 DEBUG find_positions: GitHub URL {github_url[:60]}... - HTML pattern matches: {len(html_matches)}")
        for match in html_matches:
            # Skip if already found as markdown
            if not any(item[0] == match.start() for item in media_items):
                media_items.append((
                    match.start(),
                    match.group(0),  # Full HTML tag
                    'github_attachment',
                    {'url': github_url, 'alt_text': alt_text}
                ))
                print(f"  - Found at position {match.start()} via HTML pattern, match text: {match.group(0)[:80]}...")
            else:
                print(f"  - Skipped duplicate at position {match.start()}")
        
        # Pattern 3: Plain URLs
        # Use word boundaries to avoid matching partial URLs
        plain_pattern = rf'(?<!["\']){re.escape(github_url)}(?!["\'])'
        plain_matches = list(re.finditer(plain_pattern, content))
        print(f"📝 DEBUG find_positions: GitHub URL {github_url[:60]}... - Plain pattern matches: {len(plain_matches)}")
        for match in plain_matches:
            # Skip if this position is already covered by an HTML tag match
            # Check if match position falls within any existing media_item range
            is_inside_existing = False
            for existing_pos, existing_text, _, _ in media_items:
                existing_end = existing_pos + len(existing_text)
                if existing_pos <= match.start() < existing_end:
                    is_inside_existing = True
                    print(f"  - Skipped plain match at {match.start()} (inside HTML tag at {existing_pos})")
                    break
            
            if not is_inside_existing and not any(item[0] == match.start() for item in media_items):
                media_items.append((
                    match.start(),
                    match.group(0),
                    'github_attachment',
                    {'url': github_url, 'alt_text': alt_text}
                ))
                print(f"  - Found at position {match.start()} via plain pattern")
            elif not is_inside_existing:
                print(f"  - Skipped duplicate at position {match.start()}")
    
    # Find YouTube URLs
    for original_url, formatted_markdown, video_id in youtube_urls:
        # Find plain YouTube URL (not already in markdown)
        plain_pattern = rf'(?<!["\(]){re.escape(original_url)}(?!["\)])'
        for match in re.finditer(plain_pattern, content):
            media_items.append((
                match.start(),
                match.group(0),
                'youtube',
                {'formatted': formatted_markdown}
            ))
    
    # Find direct media URLs
    for direct_url, media_type in direct_media_urls:
        # Find plain URL
        plain_pattern = rf'(?<!["\(]){re.escape(direct_url)}(?!["\)])'
        for match in re.finditer(plain_pattern, content):
            media_items.append((
                match.start(),
                match.group(0),
                'direct_media',
                {'url': direct_url, 'media_type': media_type}
            ))
    
    # Sort by position (descending, so we can replace from end to start)
    media_items.sort(key=lambda x: x[0], reverse=True)
    
    print(f"📝 DEBUG find_positions: Total media items found: {len(media_items)}")
    for pos, match_text, media_type, data in media_items:
        print(f"  - Position {pos}: type={media_type}, text={match_text[:50]}...")
    
    return media_items


def transform_content_preserving_positions(content, url_mapping, youtube_urls, direct_media_urls):
    """
    Transform content by replacing media items in-place with their corresponding
    media blocks or formatted syntax, preserving the original position of each item.
    
    Args:
        content: Original content string
        url_mapping: dict of {github_url: (permanent_url, alt_text, media_type)}
        youtube_urls: list of (original_url, formatted_markdown, video_id) tuples
        direct_media_urls: list of (url, media_type) tuples
    
    Returns:
        Transformed content with media items replaced in-place
    """
    # Build list of GitHub attachments from url_mapping
    github_attachments = [(url, data[1]) for url, data in url_mapping.items()]
    
    # Find all media items with their positions
    media_items = find_all_media_positions(content, github_attachments, youtube_urls, direct_media_urls)
    
    # Replace media items from end to start (to maintain string positions)
    transformed = content
    for position, match_text, media_type, data in media_items:
        if media_type == 'github_attachment':
            # Replace with :::media block
            github_url = data['url']
            permanent_url, alt_text, media_type_str = url_mapping[github_url]
            media_block = f''':::media
- url: "{permanent_url}"
  mediaType: "{media_type_str}"
  aspectRatio: "landscape"
  caption: "{alt_text}"
:::media'''
            transformed = transformed[:position] + media_block + transformed[position + len(match_text):]
        
        elif media_type == 'youtube':
            # Replace with formatted YouTube thumbnail markdown
            formatted = data['formatted']
            transformed = transformed[:position] + formatted + transformed[position + len(match_text):]
        
        elif media_type == 'direct_media':
            # Replace with :::media block
            direct_url = data['url']
            media_type_str = data['media_type']
            filename = direct_url.split('/')[-1].split('?')[0]  # Extract filename from URL
            media_block = f''':::media
- url: "{direct_url}"
  mediaType: "{media_type_str}"
  aspectRatio: "landscape"
  caption: "{filename}"
:::media'''
            transformed = transformed[:position] + media_block + transformed[position + len(match_text):]
    
    # Clean up extra whitespace but preserve intentional line breaks
    transformed = re.sub(r'\n\n\n+', '\n\n', transformed).strip()
    
    # Remove any remaining img tags (including empty src, malformed HTML, etc.)
    # This catches GitHub-generated img tags that remain after media processing
    # Examples: <img width=1080 height=463 alt=Image src= />
    #           <img src="" alt="Image" />
    transformed = re.sub(r'<img[^>]*>', '', transformed)
    
    # Clean up extra whitespace again after img tag removal
    transformed = re.sub(r'\n\n\n+', '\n\n', transformed).strip()
    
    return transformed


def transform_content_to_media_blocks(content, url_mapping):
    """
    Legacy function for backward compatibility with tests.
    This function only removes GitHub attachments and img tags.
    For new code, use transform_content_preserving_positions instead.
    
    url_mapping: dict of {github_url: (permanent_url, alt_text, media_type)}
    """
    transformed = content
    
    # Remove all GitHub attachment references
    for github_url in url_mapping.keys():
        # Remove markdown images with this GitHub URL
        transformed = re.sub(
            rf'!\[[^\]]*\]\({re.escape(github_url)}\)',
            '',
            transformed
        )
        # Remove HTML img tags with this specific GitHub URL
        transformed = re.sub(
            rf'<img[^>]*src=["\']({re.escape(github_url)})["\'][^>]*>',
            '',
            transformed
        )
        # Remove plain URLs
        transformed = transformed.replace(github_url, '')
    
    # Remove ALL remaining img tags (including empty src, malformed HTML, etc.)
    # This catches GitHub-generated img tags that remain after URL extraction
    # Examples: <img width=1080 height=463 alt=Image src= />
    #           <img src="" alt="Image" />
    transformed = re.sub(r'<img[^>]*>', '', transformed)
    
    # Clean up extra whitespace
    transformed = re.sub(r'\n\n+', '\n\n', transformed).strip()
    
    return transformed


def main():
    if len(sys.argv) < 2:
        print("❌ Usage: python upload_media.py <issue-content-file>")
        sys.exit(1)
    
    content_file = sys.argv[1]
    
    # Read issue content
    print("📄 Reading issue content...")
    with open(content_file, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Extract GitHub attachments
    print("🔍 Extracting GitHub attachments...")
    attachments = extract_github_attachments(content)
    
    # Extract and format YouTube URLs
    print("🔍 Extracting YouTube URLs...")
    youtube_urls = extract_and_format_youtube_urls(content)
    
    # Extract direct media URLs (non-GitHub, non-YouTube)
    print("🔍 Extracting direct media URLs...")
    direct_media_urls = extract_direct_media_urls(content)
    
    if not attachments and not youtube_urls and not direct_media_urls:
        print("ℹ️  No GitHub attachments, YouTube URLs, or direct media URLs found.")
        
        # However, we still need to clean up any leftover img tags
        # (e.g., from GitHub drag-and-drop that had src extracted already)
        cleaned_content = content
        
        # Remove any remaining img tags (including empty src, malformed HTML, etc.)
        img_count_before = len(re.findall(r'<img[^>]*>', cleaned_content))
        if img_count_before > 0:
            print(f"🧹 Cleaning up {img_count_before} leftover img tag(s)...")
            cleaned_content = re.sub(r'<img[^>]*>', '', cleaned_content)
            cleaned_content = re.sub(r'\n\n+', '\n\n', cleaned_content).strip()
            print("✅ Cleanup complete")
        
        # Write cleaned content back
        with open(content_file, 'w', encoding='utf-8') as f:
            f.write(cleaned_content)
        sys.exit(0)
    
    print(f"✅ Found {len(attachments)} GitHub attachment(s)")
    print(f"✅ Found {len(youtube_urls)} YouTube URL(s)")
    print(f"✅ Found {len(direct_media_urls)} direct media URL(s)")
    
    # Process GitHub attachments (upload to S3)
    url_mapping = {}
    
    if attachments:
        # Get S3 configuration from environment
        access_key = os.environ.get('LINODE_STORAGE_ACCESS_KEY_ID')
        secret_key = os.environ.get('LINODE_STORAGE_SECRET_ACCESS_KEY')
        endpoint_url = os.environ.get('LINODE_STORAGE_ENDPOINT_URL')
        bucket_name = os.environ.get('LINODE_STORAGE_BUCKET_NAME')
        custom_domain = os.environ.get('LINODE_STORAGE_CUSTOM_DOMAIN')
        
        if not all([access_key, secret_key, endpoint_url, bucket_name]):
            print("❌ Missing required environment variables:")
            print("   - LINODE_STORAGE_ACCESS_KEY_ID")
            print("   - LINODE_STORAGE_SECRET_ACCESS_KEY")
            print("   - LINODE_STORAGE_ENDPOINT_URL")
            print("   - LINODE_STORAGE_BUCKET_NAME")
            sys.exit(1)
        
        # Initialize S3 client
        print("🔧 Initializing S3 client...")
        
        # Extract region from endpoint URL (e.g., us-east-1 from us-east-1.linodeobjects.com)
        parsed_endpoint = urlparse(endpoint_url)
        region = parsed_endpoint.hostname.split('.')[0] if parsed_endpoint.hostname else 'us-east-1'
        
        # Configure boto3 for S3-compatible storage (Linode Object Storage)
        # Using exact configuration from discord-publish-bot which is known to work
        s3_client = boto3.client(
            's3',
            endpoint_url=endpoint_url,
            aws_access_key_id=access_key,
            aws_secret_access_key=secret_key,
            region_name=region,
            config=Config(
                signature_version='s3v4',
                s3={
                    'addressing_style': 'virtual'
                }
            )
        )
        
        # Process each attachment
        for i, (github_url, alt_text) in enumerate(attachments, 1):
            print(f"\n📦 Processing attachment {i}/{len(attachments)}")
            
            try:
                # Download from GitHub (now returns content and detected extension)
                file_content, detected_ext = download_from_github(github_url)
                
                # Extract filename from URL or generate one
                parsed_url = urlparse(github_url)
                path_parts = parsed_url.path.split('/')
                base_filename = path_parts[-1] if path_parts else f'attachment-{i}'
                
                # If filename doesn't have extension, use detected extension
                if '.' not in base_filename:
                    if detected_ext:
                        filename = base_filename + detected_ext
                        print(f"  ✅ Using detected extension: {detected_ext}")
                    else:
                        # Only default to .jpg if detection completely failed
                        filename = base_filename + '.jpg'
                        print(f"  ⚠️  No extension detected, defaulting to .jpg")
                else:
                    filename = base_filename
                
                # Upload to S3
                s3_key = upload_to_s3(file_content, filename, s3_client, bucket_name)
                
                # Generate permanent URL
                permanent_url = generate_permanent_url(s3_key, endpoint_url, bucket_name, custom_domain)
                
                # Determine media type from S3 key
                if '/images/' in s3_key:
                    media_type = 'image'
                elif '/videos/' in s3_key:
                    media_type = 'video'
                elif '/audio/' in s3_key:
                    media_type = 'audio'
                else:
                    media_type = 'file'
                
                url_mapping[github_url] = (permanent_url, alt_text, media_type)
                print(f"  🔗 Permanent URL: {permanent_url}")
                print(f"  📁 Media type: {media_type}")
                
            except Exception as e:
                print(f"  ❌ Error processing attachment: {e}")
                sys.exit(1)
    
    # Transform content to use permanent URLs and preserve positions
    print("\n🔄 Transforming content...")
    print(f"📝 DEBUG: Original content length: {len(content)} chars")
    print(f"📝 DEBUG: GitHub attachments to replace: {len(url_mapping)}")
    for gh_url in url_mapping.keys():
        print(f"  - {gh_url[:80]}...")
    
    try:
        final_content = transform_content_preserving_positions(content, url_mapping, youtube_urls, direct_media_urls)
        print(f"📝 DEBUG: Transformation completed successfully")
    except Exception as e:
        print(f"❌ ERROR during transformation: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)
    
    print(f"📝 DEBUG: Transformed content length: {len(final_content)} chars")
    
    # Debug: Check if media blocks are present in final content
    media_block_count = final_content.count(':::media')
    print(f"📝 DEBUG: Media blocks in final content: {media_block_count}")
    if media_block_count > 0:
        print(f"📝 DEBUG: First 500 chars of transformed content:")
        print(final_content[:500])
        print(f"📝 DEBUG: Last 500 chars of transformed content:")
        print(final_content[-500:])
    
    # Write transformed content back
    try:
        with open(content_file, 'w', encoding='utf-8') as f:
            f.write(final_content)
        print(f"📝 DEBUG: File write completed")
    except Exception as e:
        print(f"❌ ERROR writing file: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)
    
    # Verify file was written
    try:
        with open(content_file, 'r', encoding='utf-8') as f:
            written_content = f.read()
        
        print(f"📝 DEBUG: File written successfully, length: {len(written_content)} chars")
        print(f"📝 DEBUG: Media blocks in written file: {written_content.count(':::media')}")
        
        if len(written_content) != len(final_content):
            print(f"⚠️  WARNING: File length mismatch! Expected {len(final_content)}, got {len(written_content)}")
        
        if written_content != final_content:
            print(f"⚠️  WARNING: File content doesn't match transformed content!")
    except Exception as e:
        print(f"❌ ERROR verifying file: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)
    
    print("\n✅ Media upload and transformation complete!")
    print(f"📊 Uploaded {len(url_mapping)} file(s) to S3")
    print(f"📊 Formatted {len(youtube_urls)} YouTube URL(s)")
    print(f"📊 Created {len(direct_media_urls)} media block(s) for direct URLs")
    print(f"📊 All media items replaced in-place, preserving original positions")
    print(f"📄 Transformed content written to: {content_file}")


if __name__ == '__main__':
    main()
