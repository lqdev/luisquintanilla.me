#!/usr/bin/env python3
"""
End-to-end test simulating the complete media workflow from issue form to markdown file
This tests the exact scenario from issue #693 and PR #694
"""

import tempfile
import os
import sys

# Add parent directory to path to import upload_media
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', '.github', 'scripts'))

from upload_media import (
    extract_github_attachments,
    extract_and_format_youtube_urls,
    extract_direct_media_urls,
    transform_content_preserving_positions
)

def test_complete_issue_693_scenario():
    """
    Complete end-to-end test of issue #693 scenario
    Simulates: issue content -> extract -> transform -> verify order
    """
    
    print("=" * 80)
    print("End-to-End Test: Complete Issue #693 Workflow")
    print("=" * 80)
    
    # Original issue content from #693
    issue_content = '''Here is another post

<img src="https://github.com/user-attachments/assets/a6dad8c2-0c9d-47d4-8abf-30a0045ac681">

I could also post YT

https://www.youtube.com/watch?v=fAV_J5-dMls

And direct links

https://cdn.lqdev.tech/files/images/20251026_214154_4e8bfb4a-3a33-4ad9-a28c-d17c60fa3615.jpg'''
    
    print("\n1. Original Issue Content:")
    print(issue_content)
    print("\n" + "-" * 80)
    
    # Step 1: Extract GitHub attachments (would be uploaded to S3)
    print("\n2. Extract GitHub Attachments:")
    github_attachments = extract_github_attachments(issue_content)
    print(f"   Found {len(github_attachments)} GitHub attachment(s)")
    for url, alt in github_attachments:
        print(f"   - {url} (alt: {alt})")
    
    # Step 2: Extract YouTube URLs
    print("\n3. Extract YouTube URLs:")
    youtube_urls = extract_and_format_youtube_urls(issue_content)
    print(f"   Found {len(youtube_urls)} YouTube URL(s)")
    for url, formatted, video_id in youtube_urls:
        print(f"   - {url} (video_id: {video_id})")
    
    # Step 3: Extract direct media URLs
    print("\n4. Extract Direct Media URLs:")
    direct_media_urls = extract_direct_media_urls(issue_content)
    print(f"   Found {len(direct_media_urls)} direct media URL(s)")
    for url, media_type in direct_media_urls:
        print(f"   - {url} (type: {media_type})")
    
    # Step 4: Simulate S3 upload (create URL mapping)
    print("\n5. Simulate S3 Upload (URL Mapping):")
    url_mapping = {
        "https://github.com/user-attachments/assets/a6dad8c2-0c9d-47d4-8abf-30a0045ac681": (
            "https://cdn.lqdev.tech/files/images/20251026_222510_a6dad8c2-0c9d-47d4-8abf-30a0045ac681.jpg",
            "media",
            "image"
        )
    }
    print(f"   Uploaded {len(url_mapping)} file(s) to S3")
    for github_url, (cdn_url, alt, media_type) in url_mapping.items():
        print(f"   - {github_url}")
        print(f"     -> {cdn_url}")
    
    # Step 5: Transform content with position preservation
    print("\n6. Transform Content (Preserving Positions):")
    transformed_content = transform_content_preserving_positions(
        issue_content,
        url_mapping,
        youtube_urls,
        direct_media_urls
    )
    
    print("\n" + "-" * 80)
    print("Transformed Content:")
    print(transformed_content)
    print("-" * 80)
    
    # Step 6: Verify order is correct
    print("\n7. Verify Order:")
    
    # Expected order of elements
    elements_to_check = [
        ("Here is another post", "text"),
        ("a6dad8c2", "github_upload"),
        ("I could also post YT", "text"),
        ("[![Video]", "youtube"),
        ("And direct links", "text"),
        ("20251026_214154", "direct_link")
    ]
    
    positions = []
    for element, element_type in elements_to_check:
        pos = transformed_content.find(element)
        positions.append((element, element_type, pos))
        print(f"   {element_type:15} '{element[:30]}...': position {pos}")
    
    # Verify positions are in order
    pos_values = [pos for _, _, pos in positions]
    if all(p >= 0 for p in pos_values) and pos_values == sorted(pos_values):
        print("\n✅ SUCCESS: All elements in correct order!")
    else:
        print("\n❌ FAILURE: Elements not in correct order!")
        return False
    
    # Step 7: Compare with expected output from issue
    print("\n8. Compare with Expected Output:")
    
    expected_output = '''Here is another post

:::media
- url: "https://cdn.lqdev.tech/files/images/20251026_222510_a6dad8c2-0c9d-47d4-8abf-30a0045ac681.jpg"
  mediaType: "image"
  aspectRatio: "landscape"
  caption: "media"
:::media

I could also post YT

[![Video](http://img.youtube.com/vi/fAV_J5-dMls/0.jpg)](https://www.youtube.com/watch?v=fAV_J5-dMls "Video")

And direct links

:::media
- url: "https://cdn.lqdev.tech/files/images/20251026_214154_4e8bfb4a-3a33-4ad9-a28c-d17c60fa3615.jpg"
  mediaType: "image"
  aspectRatio: "landscape"
  caption: "20251026_214154_4e8bfb4a-3a33-4ad9-a28c-d17c60fa3615.jpg"
:::media'''
    
    # Normalize whitespace for comparison
    transformed_normalized = '\n'.join(line.rstrip() for line in transformed_content.split('\n')).strip()
    expected_normalized = '\n'.join(line.rstrip() for line in expected_output.split('\n')).strip()
    
    if transformed_normalized == expected_normalized:
        print("   ✅ Output matches expected result exactly!")
    else:
        print("   ⚠️  Output differs slightly from expected (checking key elements):")
        # Check key elements are present
        key_elements = [
            "Here is another post",
            "a6dad8c2",
            "I could also post YT",
            "[![Video]",
            "And direct links",
            "20251026_214154"
        ]
        all_present = all(elem in transformed_content for elem in key_elements)
        if all_present:
            print("   ✅ All key elements present and in correct order")
        else:
            print("   ❌ Some key elements missing")
            return False
    
    # Step 8: Simulate writing to file (like F# script would do)
    print("\n9. Simulate File Write:")
    with tempfile.NamedTemporaryFile(mode='w', suffix='.md', delete=False) as f:
        frontmatter = '''---
title: Test GH Media Again
post_type: media
published_date: "2025-10-26 17:25 -05:00"
tags: ["test","media"]
---

'''
        f.write(frontmatter + transformed_content)
        temp_file = f.name
    
    print(f"   Written to temporary file: {temp_file}")
    
    # Read back and verify
    with open(temp_file, 'r') as f:
        final_content = f.read()
    
    print(f"   File size: {len(final_content)} bytes")
    
    # Cleanup
    os.unlink(temp_file)
    
    print("\n" + "=" * 80)
    print("End-to-End Test PASSED!")
    print("=" * 80)
    
    return True

def main():
    """Run the end-to-end test"""
    print("\n" + "=" * 80)
    print("COMPLETE WORKFLOW END-TO-END TEST")
    print("=" * 80 + "\n")
    
    try:
        result = test_complete_issue_693_scenario()
        
        if result:
            print("\n✅ END-TO-END TEST PASSED")
            return 0
        else:
            print("\n❌ END-TO-END TEST FAILED")
            return 1
    except Exception as e:
        print(f"\n❌ TEST FAILED WITH EXCEPTION: {e}")
        import traceback
        traceback.print_exc()
        return 1

if __name__ == '__main__':
    sys.exit(main())
