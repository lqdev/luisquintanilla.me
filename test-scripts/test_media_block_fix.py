#!/usr/bin/env python3
"""
Quick test to verify the media block fix handles unquoted HTML attributes.
Run this to validate the fix after any changes to upload_media.py
"""

import sys
import os

# Add the scripts directory to path
script_dir = os.path.join(os.path.dirname(__file__), '..', '.github', 'scripts')
sys.path.insert(0, script_dir)

from upload_media import extract_github_attachments, transform_content_preserving_positions

def test_unquoted_html():
    """Test that unquoted HTML img tags are handled correctly (bug fix test)"""
    content = '<img width=1080 height=463 alt=Image src=https://github.com/user-attachments/assets/test-id />'
    
    # Extract attachments
    attachments = extract_github_attachments(content)
    assert len(attachments) == 1, f"Expected 1 attachment, got {len(attachments)}"
    assert attachments[0][1] == "Image", f"Expected alt='Image', got '{attachments[0][1]}'"
    
    # Transform content
    url_mapping = {
        "https://github.com/user-attachments/assets/test-id": (
            "https://cdn.lqdev.tech/final.jpg",
            "Image",
            "image"
        )
    }
    
    transformed = transform_content_preserving_positions(content, url_mapping, [], [])
    
    # Verify transformation
    assert '<img' not in transformed, "HTML img tag should be completely replaced"
    assert ':::media' in transformed, "Should contain media block"
    assert 'https://cdn.lqdev.tech/final.jpg' in transformed, "Should contain final URL"
    
    print("✅ Unquoted HTML test PASSED")

def test_quoted_html():
    """Test that quoted HTML img tags still work correctly"""
    content = '<img width=1080 height=463 alt="Test Image" src="https://github.com/user-attachments/assets/test-id">'
    
    attachments = extract_github_attachments(content)
    assert len(attachments) == 1, f"Expected 1 attachment, got {len(attachments)}"
    assert attachments[0][1] == "Test Image", f"Expected alt='Test Image', got '{attachments[0][1]}'"
    
    print("✅ Quoted HTML test PASSED")

def test_markdown():
    """Test that markdown images still work correctly"""
    content = '![My Photo](https://github.com/user-attachments/assets/test-id)'
    
    attachments = extract_github_attachments(content)
    assert len(attachments) == 1, f"Expected 1 attachment, got {len(attachments)}"
    assert attachments[0][1] == "My Photo", f"Expected alt='My Photo', got '{attachments[0][1]}'"
    
    print("✅ Markdown test PASSED")

if __name__ == '__main__':
    try:
        test_unquoted_html()
        test_quoted_html()
        test_markdown()
        print("\n🎉 All tests PASSED! Media block fix is working correctly.")
        sys.exit(0)
    except AssertionError as e:
        print(f"\n❌ TEST FAILED: {e}")
        sys.exit(1)
    except Exception as e:
        print(f"\n❌ ERROR: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)
