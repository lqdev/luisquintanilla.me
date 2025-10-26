#!/usr/bin/env python3
"""
Test case for issue #698 - img tags with width/height attributes
"""

import re
import sys
import os

# Add parent directory to path to import upload_media functions
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', '.github', 'scripts'))

from upload_media import transform_content_preserving_positions

def test_issue_698():
    """
    Test the exact case from issue #698 with img tag containing width/height attributes
    """
    
    print("=" * 80)
    print("Test: Issue #698 - img tags with width/height attributes")
    print("=" * 80)
    
    # Exact content from issue #698
    original_content = '''This is an upload

<img width="1080" height="463" alt="Image" src="https://github.com/user-attachments/assets/d5017208-9919-4387-99e1-77a96f3ec654" />

And this is a direct link

https://cdn.lqdev.tech/files/images/20251026_214154_4e8bfb4a-3a33-4ad9-a28c-d17c60fa3615.jpg

And this is a YT link

https://www.youtube.com/watch?v=fAV_J5-dMls'''
    
    print("\nOriginal content:")
    print(original_content)
    print("\n" + "-" * 80)
    
    # Simulate uploaded files (GitHub attachment)
    url_mapping = {
        "https://github.com/user-attachments/assets/d5017208-9919-4387-99e1-77a96f3ec654": (
            "https://cdn.lqdev.tech/files/images/20251026_231430_d5017208-9919-4387-99e1-77a96f3ec654.jpg",
            "Image",
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
    
    # Verify all 3 media items are present
    github_img_present = "20251026_231430_d5017208-9919-4387-99e1-77a96f3ec654.jpg" in transformed
    direct_link_present = "20251026_214154" in transformed
    youtube_present = "[![Video]" in transformed
    
    # Verify order is preserved
    if github_img_present and direct_link_present and youtube_present:
        github_pos = transformed.find("20251026_231430_d5017208-9919-4387-99e1-77a96f3ec654.jpg")
        direct_pos = transformed.find("20251026_214154")
        youtube_pos = transformed.find("[![Video]")
        
        print("\nElement positions in transformed output:")
        print(f"  GitHub image media block: position {github_pos}")
        print(f"  Direct link media block: position {direct_pos}")
        print(f"  YouTube video: position {youtube_pos}")
        
        # Expected order: "This is an upload" < GitHub img < "And this is a direct link" < Direct link < "And this is a YT link" < YouTube
        text1_pos = transformed.find("This is an upload")
        text2_pos = transformed.find("And this is a direct link")
        text3_pos = transformed.find("And this is a YT link")
        
        if (text1_pos < github_pos < text2_pos < direct_pos < text3_pos < youtube_pos):
            print("\n✅ PASSED: All 3 media items are present and in correct order!")
            print(f"✅ GitHub attachment media block with width/height attributes was correctly processed")
            return True
        else:
            print("\n❌ FAILED: Media items are not in correct order")
            return False
    else:
        print("\n❌ FAILED: Not all media items are present")
        print(f"  GitHub image: {'✅' if github_img_present else '❌'}")
        print(f"  Direct link: {'✅' if direct_link_present else '❌'}")
        print(f"  YouTube video: {'✅' if youtube_present else '❌'}")
        return False

if __name__ == '__main__':
    result = test_issue_698()
    sys.exit(0 if result else 1)
