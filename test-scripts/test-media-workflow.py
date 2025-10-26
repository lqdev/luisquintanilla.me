#!/usr/bin/env python3
"""
Comprehensive test for the enhanced media workflow
Tests all scenarios from the issue:
1. GitHub upload/drag-and-drop img tags (with empty src)
2. YouTube URLs (should become thumbnail click syntax)
3. Direct media URLs (should become :::media blocks)
"""

import re
import sys
import os

# Add parent directory to path to import upload_media functions
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', '.github', 'scripts'))

def test_transform_content_to_media_blocks():
    """Test the transform_content_to_media_blocks function"""
    
    print("=" * 80)
    print("Test 1: GitHub img tags with empty src (Issue #688 scenario)")
    print("=" * 80)
    
    test_content = '''My post

<img width=1080 height=463 alt=Image src= />

:::media
- url: "https://cdn.lqdev.tech/files/images/20251026_214154_4e8bfb4a.jpg"
  mediaType: "image"
  aspectRatio: "landscape"
  caption: "media"
:::media'''
    
    # The img tag should be removed completely
    # Import the function
    from upload_media import transform_content_to_media_blocks
    
    # Empty url_mapping since the URL was already extracted
    url_mapping = {}
    
    result = transform_content_to_media_blocks(test_content, url_mapping)
    
    print("Input content:")
    print(test_content)
    print("\n" + "-" * 80)
    print("Output content:")
    print(result)
    print("\n" + "-" * 80)
    
    if '<img' in result:
        print("❌ FAILED: img tag still present in output")
        return False
    else:
        print("✅ PASSED: img tag removed successfully")
    
    print("\n" + "=" * 80)
    print("Test 2: Multiple img tag formats")
    print("=" * 80)
    
    test_content_2 = '''My post content

<img width=1080 height=463 alt=Image src= />

Some text

<img src="" alt="Empty src">

More content

<img src="https://github.com/user-attachments/assets/test.jpg" alt="GitHub attachment">

Final text'''
    
    # Simulate that we've extracted the GitHub URL
    url_mapping = {
        "https://github.com/user-attachments/assets/test.jpg": (
            "https://cdn.lqdev.tech/files/images/test.jpg",
            "GitHub attachment",
            "image"
        )
    }
    
    result = transform_content_to_media_blocks(test_content_2, url_mapping)
    
    print("Input content:")
    print(test_content_2)
    print("\n" + "-" * 80)
    print("Output content:")
    print(result)
    print("\n" + "-" * 80)
    
    if '<img' in result:
        print("❌ FAILED: img tags still present in output")
        return False
    else:
        print("✅ PASSED: All img tags removed successfully")
    
    if 'https://github.com/user-attachments/assets/test.jpg' in result:
        print("❌ FAILED: GitHub URL still present in output")
        return False
    else:
        print("✅ PASSED: GitHub URL removed successfully")
    
    return True

def test_youtube_formatting():
    """Test YouTube URL formatting"""
    
    print("\n" + "=" * 80)
    print("Test 3: YouTube URL formatting")
    print("=" * 80)
    
    from upload_media import extract_and_format_youtube_urls
    
    test_content = '''Check out this video:

https://youtube.com/watch?v=dQw4w9WgXcQ

And this one too:

https://youtu.be/abc123xyz'''
    
    youtube_urls = extract_and_format_youtube_urls(test_content)
    
    print("Input content:")
    print(test_content)
    print("\n" + "-" * 80)
    print(f"Found {len(youtube_urls)} YouTube URL(s):")
    
    for original_url, formatted_markdown, video_id in youtube_urls:
        print(f"\n  Original: {original_url}")
        print(f"  Video ID: {video_id}")
        print(f"  Formatted: {formatted_markdown}")
    
    if len(youtube_urls) != 2:
        print(f"\n❌ FAILED: Expected 2 YouTube URLs, found {len(youtube_urls)}")
        return False
    
    # Check that formatted markdown contains thumbnail syntax
    for original_url, formatted_markdown, video_id in youtube_urls:
        if not formatted_markdown.startswith('[![Video](http://img.youtube.com/vi/'):
            print(f"\n❌ FAILED: Incorrect thumbnail format for {video_id}")
            return False
        if not formatted_markdown.endswith(f']({original_url} "Video")'):
            print(f"\n❌ FAILED: Incorrect link format for {video_id}")
            return False
    
    print("\n✅ PASSED: YouTube URLs formatted correctly as thumbnail click syntax")
    return True

def test_direct_media_urls():
    """Test direct media URL detection"""
    
    print("\n" + "=" * 80)
    print("Test 4: Direct media URL detection")
    print("=" * 80)
    
    from upload_media import extract_direct_media_urls
    
    test_content = '''Here's a direct image:

https://example.com/photo.jpg

And a video:

https://cdn.example.com/video.mp4

Audio file:

https://sounds.example.com/track.mp3'''
    
    direct_media = extract_direct_media_urls(test_content)
    
    print("Input content:")
    print(test_content)
    print("\n" + "-" * 80)
    print(f"Found {len(direct_media)} direct media URL(s):")
    
    for url, media_type in direct_media:
        print(f"  {media_type}: {url}")
    
    if len(direct_media) != 3:
        print(f"\n❌ FAILED: Expected 3 direct media URLs, found {len(direct_media)}")
        return False
    
    # Check media types
    expected_types = {'image', 'video', 'audio'}
    found_types = {media_type for _, media_type in direct_media}
    
    if found_types != expected_types:
        print(f"\n❌ FAILED: Expected types {expected_types}, found {found_types}")
        return False
    
    print("\n✅ PASSED: Direct media URLs detected correctly")
    return True

def test_complete_workflow():
    """Test the complete workflow with mixed content"""
    
    print("\n" + "=" * 80)
    print("Test 5: Complete workflow with mixed content")
    print("=" * 80)
    
    test_content = '''My comprehensive media post

<img width=1080 height=463 alt=Image src= />

Check out this YouTube video:
https://youtube.com/watch?v=test123

Here's a direct image link:
https://example.com/image.jpg

<img src="https://github.com/user-attachments/assets/github-img.jpg" alt="GitHub">

Some final text'''
    
    print("Input content:")
    print(test_content)
    print("\n" + "-" * 80)
    
    # Simulate the complete transformation
    from upload_media import (
        transform_content_to_media_blocks,
        extract_and_format_youtube_urls,
        extract_direct_media_urls
    )
    
    # Simulate URL mapping after GitHub attachment processing
    url_mapping = {
        "https://github.com/user-attachments/assets/github-img.jpg": (
            "https://cdn.lqdev.tech/files/images/20251026_214154_github-img.jpg",
            "GitHub",
            "image"
        )
    }
    
    # Transform to remove GitHub references
    transformed = transform_content_to_media_blocks(test_content, url_mapping)
    
    # Extract YouTube URLs
    youtube_urls = extract_and_format_youtube_urls(transformed)
    
    # Extract direct media URLs
    direct_media = extract_direct_media_urls(transformed)
    
    print("After transformation:")
    print(f"  - Removed {len(url_mapping)} GitHub attachment(s)")
    print(f"  - Found {len(youtube_urls)} YouTube URL(s)")
    print(f"  - Found {len(direct_media)} direct media URL(s)")
    
    # Check that all img tags are removed
    if '<img' in transformed:
        print("\n❌ FAILED: img tags still present after transformation")
        return False
    
    print("\n✅ PASSED: Complete workflow executed successfully")
    return True

def main():
    """Run all tests"""
    print("\n" + "=" * 80)
    print("MEDIA WORKFLOW ENHANCEMENT TEST SUITE")
    print("=" * 80 + "\n")
    
    tests = [
        test_transform_content_to_media_blocks,
        test_youtube_formatting,
        test_direct_media_urls,
        test_complete_workflow
    ]
    
    results = []
    for test in tests:
        try:
            result = test()
            results.append(result)
        except Exception as e:
            print(f"\n❌ TEST FAILED WITH EXCEPTION: {e}")
            import traceback
            traceback.print_exc()
            results.append(False)
    
    print("\n" + "=" * 80)
    print("TEST SUMMARY")
    print("=" * 80)
    
    passed = sum(1 for r in results if r)
    total = len(results)
    
    print(f"\nPassed: {passed}/{total}")
    
    if all(results):
        print("\n✅ ALL TESTS PASSED")
        return 0
    else:
        print("\n❌ SOME TESTS FAILED")
        return 1

if __name__ == '__main__':
    sys.exit(main())
