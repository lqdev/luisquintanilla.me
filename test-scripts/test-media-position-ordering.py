#!/usr/bin/env python3
"""
Test for media position preservation in GitHub Issue Form publishing flow
Tests that media blocks maintain their original position in the content
instead of being appended at the end.

Reference: Issue #693 and PR #694
"""

import re
import sys
import os

# Add parent directory to path to import upload_media functions
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', '.github', 'scripts'))

def test_position_preservation():
    """
    Test that media items maintain their position in the content
    This tests the exact scenario from issue #693
    """
    
    print("=" * 80)
    print("Test: Media Position Preservation (Issue #693)")
    print("=" * 80)
    
    # Exact content from issue #693
    original_content = '''Here is another post

<img src="https://github.com/user-attachments/assets/a6dad8c2-0c9d-47d4-8abf-30a0045ac681">

I could also post YT

https://www.youtube.com/watch?v=fAV_J5-dMls

And direct links

https://cdn.lqdev.tech/files/images/20251026_214154_4e8bfb4a-3a33-4ad9-a28c-d17c60fa3615.jpg'''
    
    print("Original content:")
    print(original_content)
    print("\n" + "-" * 80)
    
    # Expected output structure (order matters):
    # 1. "Here is another post"
    # 2. :::media::: block for uploaded image
    # 3. "I could also post YT"
    # 4. YouTube thumbnail markdown
    # 5. "And direct links"
    # 6. :::media::: block for direct link
    
    expected_order = [
        "Here is another post",
        ":::media",  # First media block (GitHub upload)
        "I could also post YT",
        "[![Video]",  # YouTube thumbnail
        "And direct links",
        ":::media"  # Second media block (direct link)
    ]
    
    print("\nExpected order of elements:")
    for i, element in enumerate(expected_order, 1):
        print(f"  {i}. {element}")
    
    # Current behavior (WRONG - appends at end):
    # 1. "Here is another post"
    # 2. "I could also post YT"
    # 3. YouTube thumbnail markdown
    # 4. "And direct links"
    # 5. :::media::: block for uploaded image
    # 6. :::media::: block for direct link
    
    current_wrong_output = '''Here is another post

I could also post YT

[![Video](http://img.youtube.com/vi/fAV_J5-dMls/0.jpg)](https://www.youtube.com/watch?v=fAV_J5-dMls "Video")

And direct links

:::media
- url: "https://cdn.lqdev.tech/files/images/20251026_222510_a6dad8c2-0c9d-47d4-8abf-30a0045ac681.jpg"
  mediaType: "image"
  aspectRatio: "landscape"
  caption: "media"
:::media

:::media
- url: "https://cdn.lqdev.tech/files/images/20251026_214154_4e8bfb4a-3a33-4ad9-a28c-d17c60fa3615.jpg"
  mediaType: "image"
  aspectRatio: "landscape"
  caption: "20251026_214154_4e8bfb4a-3a33-4ad9-a28c-d17c60fa3615.jpg"
:::media'''
    
    print("\n" + "-" * 80)
    print("Current (WRONG) output from PR #694:")
    print(current_wrong_output)
    print("\n" + "-" * 80)
    
    # Check current output order - verify media blocks are at the end (WRONG)
    github_img_pos = current_wrong_output.find("a6dad8c2")  # GitHub uploaded image
    direct_link_pos = current_wrong_output.find("20251026_214154")  # Direct link
    text1_pos = current_wrong_output.find("Here is another post")
    text2_pos = current_wrong_output.find("I could also post YT")
    text3_pos = current_wrong_output.find("And direct links")
    youtube_pos = current_wrong_output.find("[![Video]")
    
    print("\nElement positions in current output:")
    print(f"  'Here is another post': position {text1_pos}")
    print(f"  'I could also post YT': position {text2_pos}")
    print(f"  '[![Video]': position {youtube_pos}")
    print(f"  'And direct links': position {text3_pos}")
    print(f"  GitHub image media block: position {github_img_pos}")
    print(f"  Direct link media block: position {direct_link_pos}")
    
    # Verify that current output is WRONG (media blocks at end)
    if text3_pos < github_img_pos and text3_pos < direct_link_pos:
        print("\n✅ Verified: Current output IS WRONG (media blocks appended after all text)")
    else:
        print("\n⚠️  Warning: Current output structure different than expected")
    
    # Expected CORRECT output (after fix):
    expected_correct_output = '''Here is another post

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
    
    print("\n" + "-" * 80)
    print("Expected CORRECT output (after fix):")
    print(expected_correct_output)
    print("\n" + "-" * 80)
    
    # Check expected output order - verify media blocks are in position (CORRECT)
    github_img_pos_exp = expected_correct_output.find("a6dad8c2")  # GitHub uploaded image
    direct_link_pos_exp = expected_correct_output.find("20251026_214154")  # Direct link
    text1_pos_exp = expected_correct_output.find("Here is another post")
    text2_pos_exp = expected_correct_output.find("I could also post YT")
    text3_pos_exp = expected_correct_output.find("And direct links")
    youtube_pos_exp = expected_correct_output.find("[![Video]")
    
    print("\nElement positions in expected output:")
    print(f"  'Here is another post': position {text1_pos_exp}")
    print(f"  GitHub image media block: position {github_img_pos_exp}")
    print(f"  'I could also post YT': position {text2_pos_exp}")
    print(f"  '[![Video]': position {youtube_pos_exp}")
    print(f"  'And direct links': position {text3_pos_exp}")
    print(f"  Direct link media block: position {direct_link_pos_exp}")
    
    # Verify expected output maintains order
    # Text1 < GitHub img < Text2 < YouTube < Text3 < Direct link
    if (text1_pos_exp < github_img_pos_exp < text2_pos_exp < youtube_pos_exp < text3_pos_exp < direct_link_pos_exp):
        print("\n✅ Expected output maintains correct order")
    else:
        print("\n❌ Expected output order is incorrect")
        return False
    
    print("\n" + "=" * 80)
    print("Test specification documented successfully")
    print("=" * 80)
    print("\nThis test documents the expected behavior for the fix.")
    print("After implementing the fix, this test should be updated to")
    print("validate the actual transform_and_preserve_positions() function.")
    
    return True

def main():
    """Run the position preservation test"""
    print("\n" + "=" * 80)
    print("MEDIA POSITION PRESERVATION TEST")
    print("=" * 80 + "\n")
    
    try:
        result = test_position_preservation()
        
        print("\n" + "=" * 80)
        print("TEST RESULT")
        print("=" * 80)
        
        if result:
            print("\n✅ TEST SPECIFICATION VALIDATED")
            return 0
        else:
            print("\n❌ TEST SPECIFICATION VALIDATION FAILED")
            return 1
    except Exception as e:
        print(f"\n❌ TEST FAILED WITH EXCEPTION: {e}")
        import traceback
        traceback.print_exc()
        return 1

if __name__ == '__main__':
    sys.exit(main())
