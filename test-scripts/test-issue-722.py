#!/usr/bin/env python3
"""
Simulate Issue #722 - Complete workflow test
Tests the exact scenario from the GitHub issue to verify the fix.
"""

import sys
import os
import tempfile

# Add parent directory to path
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', '.github', 'scripts'))

from upload_media import (
    detect_file_extension_from_content,
    get_media_type_folder,
    sanitize_filename
)

def create_test_video_content():
    """Create a realistic MP4 file header for testing."""
    # Real MP4 file signature (ftyp box with isom)
    # This is what an actual iPhone/Android video would have
    mp4_header = b'\x00\x00\x00\x20'  # Box size
    mp4_header += b'ftyp'              # Box type: ftyp
    mp4_header += b'isom'              # Major brand
    mp4_header += b'\x00\x00\x02\x00' # Minor version
    mp4_header += b'isomiso2avc1mp41' # Compatible brands
    
    # Add some dummy data to simulate file content
    mp4_content = mp4_header + b'\x00' * 1000
    return mp4_content

def main():
    print("=" * 70)
    print("Issue #722 Simulation - Video Upload via GitHub Issue Form")
    print("=" * 70)
    print()
    
    print("📝 Scenario:")
    print("  - User uploads video file (.MP4) via GitHub Issue Form")
    print("  - GitHub generates URL without file extension")
    print("  - System must detect video type and generate correct output")
    print()
    
    # Simulate the GitHub attachment URL (no extension)
    github_uuid = 'e8a5a067-dfe9-4aa0-975e-3536832e4bcb'
    github_url = f'https://github.com/user-attachments/assets/{github_uuid}'
    
    print(f"🔗 GitHub URL: {github_url}")
    print(f"   (Notice: No file extension in URL)")
    print()
    
    # Simulate video file content
    video_content = create_test_video_content()
    print(f"📦 Simulated video file: {len(video_content)} bytes")
    print(f"   First 20 bytes (hex): {video_content[:20].hex()}")
    print()
    
    # Step 1: Detect file type from content
    print("🔍 Step 1: Detecting file type from content...")
    detected_ext = detect_file_extension_from_content(video_content)
    
    if detected_ext:
        print(f"   ✅ Detection successful!")
        print(f"   Detected extension: {detected_ext}")
        print(f"   File type: {'VIDEO' if detected_ext in ['.mp4', '.mov', '.avi', '.mkv'] else 'OTHER'}")
    else:
        print(f"   ❌ Detection failed - would default to .jpg (THE BUG)")
        sys.exit(1)
    print()
    
    # Step 2: Generate filename
    print("🏷️  Step 2: Generating filename...")
    base_filename = github_uuid
    filename = base_filename + detected_ext
    print(f"   Base: {base_filename}")
    print(f"   Extension: {detected_ext}")
    print(f"   Final filename: {filename}")
    print()
    
    # Step 3: Sanitize filename
    print("🧹 Step 3: Sanitizing filename...")
    clean_filename = sanitize_filename(filename)
    print(f"   Sanitized: {clean_filename}")
    print()
    
    # Step 4: Determine media folder
    print("📁 Step 4: Determining storage folder...")
    media_folder = get_media_type_folder(filename, clean_filename)
    print(f"   Media folder: {media_folder}")
    
    if media_folder == 'videos':
        print(f"   ✅ Correct! Videos should go in 'videos' folder")
    elif media_folder == 'images':
        print(f"   ❌ WRONG! Video incorrectly categorized as image (THE BUG)")
        sys.exit(1)
    else:
        print(f"   ⚠️  Unexpected folder: {media_folder}")
    print()
    
    # Step 5: Generate S3 key
    print("☁️  Step 5: Generating S3 storage key...")
    timestamp = '20251027_050937'
    s3_key = f"files/{media_folder}/{timestamp}_{clean_filename}"
    print(f"   S3 key: {s3_key}")
    print()
    
    # Step 6: Determine media type
    print("🎬 Step 6: Determining media type...")
    if '/videos/' in s3_key:
        media_type = 'video'
    elif '/images/' in s3_key:
        media_type = 'image'
    else:
        media_type = 'file'
    
    print(f"   Media type: {media_type}")
    
    if media_type == 'video':
        print(f"   ✅ Correct! Media type is 'video'")
    elif media_type == 'image':
        print(f"   ❌ WRONG! Media type incorrectly set to 'image' (THE BUG)")
        sys.exit(1)
    print()
    
    # Step 7: Generate media block
    print("📄 Step 7: Generating :::media block...")
    permanent_url = f"https://cdn.lqdev.tech/{s3_key}"
    media_block = f''':::media
- url: "{permanent_url}"
  mediaType: "{media_type}"
  aspectRatio: "landscape"
  caption: "media"
:::media'''
    
    print("   Generated block:")
    print("   " + "─" * 60)
    for line in media_block.split('\n'):
        print(f"   {line}")
    print("   " + "─" * 60)
    print()
    
    # Verification
    print("✅ VERIFICATION:")
    print("   " + "─" * 60)
    
    checks = [
        (detected_ext == '.mp4', f"File extension is .mp4 (not .jpg)"),
        (media_folder == 'videos', f"Storage folder is 'videos' (not 'images')"),
        (media_type == 'video', f"Media type is 'video' (not 'image')"),
        ('.jpg' not in s3_key, f"S3 key does not contain .jpg"),
        ('.mp4' in s3_key, f"S3 key contains .mp4"),
        ('/videos/' in s3_key, f"S3 key uses /videos/ path"),
        (permanent_url.endswith('.mp4'), f"Permanent URL ends with .mp4"),
    ]
    
    all_passed = True
    for passed, message in checks:
        status = "✅" if passed else "❌"
        print(f"   {status} {message}")
        if not passed:
            all_passed = False
    
    print("   " + "─" * 60)
    print()
    
    if all_passed:
        print("=" * 70)
        print("🎉 SUCCESS! Issue #722 is FIXED!")
        print("=" * 70)
        print()
        print("The video upload workflow now:")
        print("  ✅ Correctly detects MP4 files from content")
        print("  ✅ Uses .mp4 extension (not .jpg)")
        print("  ✅ Stores in videos/ folder (not images/)")
        print("  ✅ Sets mediaType to 'video' (not 'image')")
        print()
        return 0
    else:
        print("=" * 70)
        print("❌ FAILED - Issue #722 is NOT fixed")
        print("=" * 70)
        return 1

if __name__ == '__main__':
    sys.exit(main())
