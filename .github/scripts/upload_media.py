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


def download_from_github(url):
    """
    Download a file from GitHub CDN.
    Returns the file content as bytes.
    """
    print(f"  📥 Downloading from: {url}")
    response = requests.get(url, timeout=30)
    response.raise_for_status()
    print(f"  ✅ Downloaded {len(response.content)} bytes")
    return response.content


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
    html_pattern = r'<img[^>]*src=["\'](https://github\.com/user-attachments/[^"\']+)["\'][^>]*>'
    for match in re.finditer(html_pattern, content):
        url = match.group(1).strip()
        # Try to extract alt text if present
        alt_match = re.search(r'alt=["\']([^"\']+)["\']', match.group(0))
        alt_text = alt_match.group(1) if alt_match else 'media'
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


def transform_content_to_media_blocks(content, url_mapping):
    """
    Transform content by replacing GitHub URLs with permanent CDN URLs
    and converting to :::media blocks.
    
    url_mapping: dict of {github_url: (permanent_url, alt_text, media_type)}
    """
    transformed = content
    
    # Remove all GitHub attachment references
    for github_url in url_mapping.keys():
        # Remove markdown images
        transformed = re.sub(
            rf'!\[[^\]]*\]\({re.escape(github_url)}\)',
            '',
            transformed
        )
        # Remove HTML img tags
        transformed = re.sub(
            rf'<img[^>]*src=["\']({re.escape(github_url)})["\'][^>]*>',
            '',
            transformed
        )
        # Remove plain URLs
        transformed = transformed.replace(github_url, '')
    
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
    
    if not attachments:
        print("ℹ️  No GitHub attachments found. Skipping upload.")
        # Write original content back (no transformation needed)
        with open(content_file, 'w', encoding='utf-8') as f:
            f.write(content)
        sys.exit(0)
    
    print(f"✅ Found {len(attachments)} GitHub attachment(s)")
    
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
    s3_client = boto3.client(
        's3',
        aws_access_key_id=access_key,
        aws_secret_access_key=secret_key,
        endpoint_url=endpoint_url
    )
    
    # Process each attachment
    url_mapping = {}
    
    for i, (github_url, alt_text) in enumerate(attachments, 1):
        print(f"\n📦 Processing attachment {i}/{len(attachments)}")
        
        try:
            # Download from GitHub
            file_content = download_from_github(github_url)
            
            # Extract filename from URL or generate one
            parsed_url = urlparse(github_url)
            path_parts = parsed_url.path.split('/')
            filename = path_parts[-1] if path_parts else f'attachment-{i}'
            
            # If filename doesn't have extension, try to detect from content
            if '.' not in filename:
                # Default to .jpg for images, but this should be rare
                filename += '.jpg'
            
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
            
        except Exception as e:
            print(f"  ❌ Error processing attachment: {e}")
            sys.exit(1)
    
    # Transform content to use permanent URLs
    print("\n🔄 Transforming content...")
    transformed_content = transform_content_to_media_blocks(content, url_mapping)
    
    # Generate media blocks for uploaded files
    media_blocks = []
    for github_url, (permanent_url, alt_text, media_type) in url_mapping.items():
        # Create media block for each file
        media_block = f''':::media
- url: "{permanent_url}"
  mediaType: "{media_type}"
  aspectRatio: "landscape"
  caption: "{alt_text}"
:::media'''
        media_blocks.append(media_block)
    
    # Combine transformed content with media blocks
    if transformed_content:
        final_content = transformed_content + '\n\n' + '\n\n'.join(media_blocks)
    else:
        final_content = '\n\n'.join(media_blocks)
    
    # Write transformed content back
    with open(content_file, 'w', encoding='utf-8') as f:
        f.write(final_content)
    
    print("\n✅ Media upload and transformation complete!")
    print(f"📊 Uploaded {len(url_mapping)} file(s) to S3")
    print(f"📄 Transformed content written to: {content_file}")


if __name__ == '__main__':
    main()
