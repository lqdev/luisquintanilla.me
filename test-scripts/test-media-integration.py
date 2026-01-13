#!/usr/bin/env python3
"""
Integration test for media upload workflow.
Tests the complete flow from content detection to S3 key generation.
"""

import sys
import os
import tempfile

# Add parent directory to path to import the upload_media module
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', '.github', 'scripts'))

from upload_media import (
    detect_file_extension_from_content,
    detect_extension_from_content_type,
    get_media_type_folder,
    sanitize_filename
)

def test_video_workflow():
    """Test complete workflow for video files."""
    print("Testing video file workflow...")
    
    # Simulate MP4 video file
    mp4_content = b'\x00\x00\x00\x20ftypisom\x00\x00\x02\x00isomiso2avc1mp41' + b'\x00' * 100
    
    # Step 1: Detect extension from content
    detected_ext = detect_file_extension_from_content(mp4_content)
    assert detected_ext == '.mp4', f"Expected .mp4, got {detected_ext}"
    print(f"  ✅ Extension detected: {detected_ext}")
    
    # Step 2: Generate filename (simulating GitHub attachment UUID)
    base_filename = 'e8a5a067-dfe9-4aa0-975e-3536832e4bcb'
    filename = base_filename + detected_ext
    print(f"  ✅ Generated filename: {filename}")
    
    # Step 3: Sanitize filename
    clean_filename = sanitize_filename(filename)
    assert clean_filename == filename.lower(), f"Expected {filename.lower()}, got {clean_filename}"
    print(f"  ✅ Sanitized filename: {clean_filename}")
    
    # Step 4: Determine media type folder
    media_folder = get_media_type_folder(filename, clean_filename)
    assert media_folder == 'videos', f"Expected 'videos', got {media_folder}"
    print(f"  ✅ Media folder: {media_folder}")
    
    # Step 5: Generate S3 key (simulating)
    timestamp = '20251027_050937'
    s3_key = f"files/{media_folder}/{timestamp}_{clean_filename}"
    expected_key = f"files/videos/{timestamp}_e8a5a067-dfe9-4aa0-975e-3536832e4bcb.mp4"
    assert s3_key == expected_key, f"Expected {expected_key}, got {s3_key}"
    print(f"  ✅ S3 key: {s3_key}")
    
    # Verify media type from S3 key
    if '/videos/' in s3_key:
        media_type = 'video'
    else:
        media_type = 'unknown'
    assert media_type == 'video', f"Expected 'video', got {media_type}"
    print(f"  ✅ Media type: {media_type}")
    
    print("  ✅ Video workflow: PASSED\n")


def test_image_workflow():
    """Test complete workflow for image files."""
    print("Testing image file workflow...")
    
    # Simulate JPEG image file
    jpeg_content = b'\xff\xd8\xff\xe0\x00\x10JFIF' + b'\x00' * 100
    
    # Step 1: Detect extension from content
    detected_ext = detect_file_extension_from_content(jpeg_content)
    assert detected_ext == '.jpg', f"Expected .jpg, got {detected_ext}"
    print(f"  ✅ Extension detected: {detected_ext}")
    
    # Step 2: Generate filename
    base_filename = 'abc123-def456-ghi789'
    filename = base_filename + detected_ext
    
    # Step 3: Sanitize filename
    clean_filename = sanitize_filename(filename)
    
    # Step 4: Determine media type folder
    media_folder = get_media_type_folder(filename, clean_filename)
    assert media_folder == 'images', f"Expected 'images', got {media_folder}"
    print(f"  ✅ Media folder: {media_folder}")
    
    # Step 5: Verify media type
    timestamp = '20251027_050937'
    s3_key = f"files/{media_folder}/{timestamp}_{clean_filename}"
    
    if '/images/' in s3_key:
        media_type = 'image'
    else:
        media_type = 'unknown'
    assert media_type == 'image', f"Expected 'image', got {media_type}"
    print(f"  ✅ Media type: {media_type}")
    
    print("  ✅ Image workflow: PASSED\n")


def test_content_type_fallback():
    """Test Content-Type header fallback when content detection fails."""
    print("Testing Content-Type fallback workflow...")
    
    # Simulate unknown binary content
    unknown_content = b'\x00\x01\x02\x03\x04\x05\x06\x07\x08\x09'
    
    # Step 1: Try content-based detection (should fail)
    detected_ext = detect_file_extension_from_content(unknown_content)
    assert detected_ext is None, f"Expected None, got {detected_ext}"
    print(f"  ✅ Content detection failed as expected")
    
    # Step 2: Fallback to Content-Type header
    content_type = 'video/mp4'
    detected_ext = detect_extension_from_content_type(content_type)
    assert detected_ext == '.mp4', f"Expected .mp4, got {detected_ext}"
    print(f"  ✅ Content-Type fallback worked: {detected_ext}")
    
    print("  ✅ Content-Type fallback: PASSED\n")


def test_issue_scenario():
    """Test the exact scenario from GitHub issue #722."""
    print("Testing GitHub issue #722 scenario...")
    print("  Scenario: Video file (.MP4) uploaded via GitHub Issue Form")
    print("  Expected: .mp4 extension and 'video' media type")
    
    # Simulate real MP4 file (iPhone video)
    # Real MP4 files start with ftyp atom
    mp4_content = b'\x00\x00\x00\x20ftypM4V \x00\x00\x00\x00M4V isommp42' + b'\x00' * 100
    
    # Detect extension from content
    detected_ext = detect_file_extension_from_content(mp4_content)
    print(f"  Detected extension: {detected_ext}")
    
    # GitHub URL doesn't include extension
    github_uuid = 'e8a5a067-dfe9-4aa0-975e-3536832e4bcb'
    
    if detected_ext:
        filename = github_uuid + detected_ext
    else:
        # This was the bug - defaulting to .jpg
        filename = github_uuid + '.jpg'
    
    print(f"  Generated filename: {filename}")
    
    # Determine folder and media type
    media_folder = get_media_type_folder(filename, filename)
    print(f"  Media folder: {media_folder}")
    
    # Generate S3 key
    timestamp = '20251027_050937'
    s3_key = f"files/{media_folder}/{timestamp}_{filename}"
    print(f"  S3 key: {s3_key}")
    
    # Determine media type from S3 key
    if '/videos/' in s3_key:
        media_type = 'video'
    elif '/images/' in s3_key:
        media_type = 'image'
    else:
        media_type = 'file'
    
    print(f"  Media type: {media_type}")
    
    # Verify the fix worked
    assert detected_ext in ('.mp4', '.m4v'), f"Expected video extension, got {detected_ext}"
    assert media_folder == 'videos', f"Expected 'videos' folder, got {media_folder}"
    assert media_type == 'video', f"Expected 'video' type, got {media_type}"
    assert '.jpg' not in filename, f"Filename should not contain .jpg: {filename}"
    
    print("  ✅ Issue #722 scenario: FIXED!\n")


if __name__ == '__main__':
    print("=" * 60)
    print("Media Upload Workflow Integration Tests")
    print("=" * 60)
    print()
    
    try:
        test_video_workflow()
        test_image_workflow()
        test_content_type_fallback()
        test_issue_scenario()
        
        print("=" * 60)
        print("✅ All integration tests PASSED!")
        print("=" * 60)
        sys.exit(0)
    except AssertionError as e:
        print(f"\n❌ Test FAILED: {e}")
        sys.exit(1)
    except Exception as e:
        print(f"\n❌ Unexpected error: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)
