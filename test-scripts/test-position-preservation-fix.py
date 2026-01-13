#!/usr/bin/env python3
"""
Test the fixed media position preservation implementation
Tests that the new transform_content_preserving_positions function works correctly
"""

import re
import sys
import os
import tempfile

# Add parent directory to path to import upload_media functions
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', '.github', 'scripts'))

from upload_media import transform_content_preserving_positions

def test_position_preservation_fix():
    """
    Test the actual fix for position preservation
    Uses the exact scenario from issue #693
    """
    
    print("=" * 80)
    print("Test: Position Preservation Fix (Issue #693)")
    print("=" * 80)
    
    # Original content from issue #693
    original_content = '''Here is another post

<img src="https://github.com/user-attachments/assets/a6dad8c2-0c9d-47d4-8abf-30a0045ac681">

I could also post YT

https://www.youtube.com/watch?v=fAV_J5-dMls

And direct links

https://cdn.lqdev.tech/files/images/20251026_214154_4e8bfb4a-3a33-4ad9-a28c-d17c60fa3615.jpg'''
    
    print("\nOriginal content:")
    print(original_content)
    print("\n" + "-" * 80)
    
    # Simulate uploaded files (GitHub attachment)
    url_mapping = {
        "https://github.com/user-attachments/assets/a6dad8c2-0c9d-47d4-8abf-30a0045ac681": (
            "https://cdn.lqdev.tech/files/images/20251026_222510_a6dad8c2-0c9d-47d4-8abf-30a0045ac681.jpg",
            "media",
            "image"
        )
    }
    
    # YouTube URLs
    youtube_urls = [
        (
            "https://www.youtube.com/watch?v=fAV_J5-dMls",
            '[![Video](http://img.youtube.com/vi/fAV_J5-dMls/0.jpg)](https://www.youtube.com/watch?v=fAV_J5-dMls "Video")',
            "fAV_J5-dMls"
        )
    ]
    
    # Direct media URLs
    direct_media_urls = [
        ("https://cdn.lqdev.tech/files/images/20251026_214154_4e8bfb4a-3a33-4ad9-a28c-d17c60fa3615.jpg", "image")
    ]
    
    # Transform content
    transformed = transform_content_preserving_positions(original_content, url_mapping, youtube_urls, direct_media_urls)
    
    print("\nTransformed content:")
    print(transformed)
    print("\n" + "-" * 80)
    
    # Verify order is preserved
    github_img_pos = transformed.find("a6dad8c2")  # GitHub uploaded image
    direct_link_pos = transformed.find("20251026_214154")  # Direct link
    text1_pos = transformed.find("Here is another post")
    text2_pos = transformed.find("I could also post YT")
    text3_pos = transformed.find("And direct links")
    youtube_pos = transformed.find("[![Video]")
    
    print("\nElement positions in transformed output:")
    print(f"  'Here is another post': position {text1_pos}")
    print(f"  GitHub image media block: position {github_img_pos}")
    print(f"  'I could also post YT': position {text2_pos}")
    print(f"  '[![Video]': position {youtube_pos}")
    print(f"  'And direct links': position {text3_pos}")
    print(f"  Direct link media block: position {direct_link_pos}")
    
    # Check order: Text1 < GitHub img < Text2 < YouTube < Text3 < Direct link
    if (text1_pos < github_img_pos < text2_pos < youtube_pos < text3_pos < direct_link_pos):
        print("\n✅ PASSED: Media items maintain correct order!")
        return True
    else:
        print("\n❌ FAILED: Media items are not in correct order")
        print(f"  Expected: Text1({text1_pos}) < GitHubImg({github_img_pos}) < Text2({text2_pos}) < YouTube({youtube_pos}) < Text3({text3_pos}) < DirectLink({direct_link_pos})")
        return False

def test_multiple_media_items():
    """Test with multiple media items of different types"""
    
    print("\n" + "=" * 80)
    print("Test: Multiple Mixed Media Items")
    print("=" * 80)
    
    content = '''First paragraph

<img src="https://github.com/user-attachments/assets/img1.jpg">

Second paragraph

https://www.youtube.com/watch?v=video1

Third paragraph

https://example.com/photo.jpg

Fourth paragraph

<img src="https://github.com/user-attachments/assets/img2.jpg">

Fifth paragraph'''
    
    print("\nOriginal content:")
    print(content)
    print("\n" + "-" * 80)
    
    url_mapping = {
        "https://github.com/user-attachments/assets/img1.jpg": (
            "https://cdn.lqdev.tech/files/images/img1.jpg",
            "media",
            "image"
        ),
        "https://github.com/user-attachments/assets/img2.jpg": (
            "https://cdn.lqdev.tech/files/images/img2.jpg",
            "media",
            "image"
        )
    }
    
    youtube_urls = [
        (
            "https://www.youtube.com/watch?v=video1",
            '[![Video](http://img.youtube.com/vi/video1/0.jpg)](https://www.youtube.com/watch?v=video1 "Video")',
            "video1"
        )
    ]
    
    direct_media_urls = [
        ("https://example.com/photo.jpg", "image")
    ]
    
    transformed = transform_content_preserving_positions(content, url_mapping, youtube_urls, direct_media_urls)
    
    print("\nTransformed content:")
    print(transformed)
    print("\n" + "-" * 80)
    
    # Check that all paragraphs and media are in order
    positions = [
        ("First paragraph", transformed.find("First paragraph")),
        ("img1 media block", transformed.find("img1.jpg")),
        ("Second paragraph", transformed.find("Second paragraph")),
        ("YouTube video", transformed.find("[![Video]")),
        ("Third paragraph", transformed.find("Third paragraph")),
        ("photo.jpg media block", transformed.find("photo.jpg")),
        ("Fourth paragraph", transformed.find("Fourth paragraph")),
        ("img2 media block", transformed.find("img2.jpg")),
        ("Fifth paragraph", transformed.find("Fifth paragraph"))
    ]
    
    print("\nElement positions:")
    for name, pos in positions:
        print(f"  {name}: position {pos}")
    
    # Verify all positions are found and in order
    pos_values = [pos for _, pos in positions]
    if all(p >= 0 for p in pos_values) and pos_values == sorted(pos_values):
        print("\n✅ PASSED: All media items maintain correct order!")
        return True
    else:
        print("\n❌ FAILED: Media items are not in correct order")
        return False

def main():
    """Run all tests"""
    print("\n" + "=" * 80)
    print("MEDIA POSITION PRESERVATION FIX TEST SUITE")
    print("=" * 80 + "\n")
    
    tests = [
        test_position_preservation_fix,
        test_multiple_media_items
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
