#!/usr/bin/env python3
"""
Test the exact scenario from issue #688/#690

This reproduces the problem where GitHub-generated img tags with empty src
remain in the final markdown after the Python script processes them.
"""

import sys
import os
import tempfile

# Add parent directory to path to import upload_media functions
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', '.github', 'scripts'))

def test_issue_688_scenario():
    """
    Test the exact scenario from issue #688
    
    Original problem content had an img tag with empty src that remained
    after the Python script processed the GitHub attachments.
    
    Expected behavior: The <img> tag should be removed
    """
    
    print("=" * 80)
    print("Testing Issue #688 Scenario")
    print("=" * 80)
    print()
    
    # This is example content showing the problem from issue #688
    original_content = '''My post

<img width=1080 height=463 alt=Image src= />

:::media
- url: "https://cdn.lqdev.tech/files/images/20251026_214154_4e8bfb4a-3a33-4ad9-a28c-d17c60fa3615.jpg"
  mediaType: "image"
  aspectRatio: "landscape"
  caption: "media"
:::media'''
    
    print("Original content (demonstrating issue #688):")
    print("-" * 80)
    print(original_content)
    print("-" * 80)
    print()
    
    # Create a temporary file
    with tempfile.NamedTemporaryFile(mode='w', delete=False, suffix='.txt') as f:
        f.write(original_content)
        temp_file = f.name
    
    try:
        # Run the upload script
        print("Running upload_media.py...")
        import subprocess
        result = subprocess.run(
            ['python', '.github/scripts/upload_media.py', temp_file],
            capture_output=True,
            text=True,
            env=os.environ.copy()
        )
        
        print("Script output:")
        print(result.stdout)
        if result.stderr:
            print("Errors:", result.stderr)
        print()
        
        # Read the transformed content
        with open(temp_file, 'r') as f:
            transformed_content = f.read()
        
        print("Transformed content:")
        print("-" * 80)
        print(transformed_content)
        print("-" * 80)
        print()
        
        # Verify the img tag is removed
        if '<img' in transformed_content:
            print("❌ FAILED: <img> tag still present in transformed content")
            print("\nThis was the exact problem reported in issue #688!")
            return False
        else:
            print("✅ PASSED: <img> tag successfully removed")
        
        # Verify the content is preserved
        if 'My post' in transformed_content:
            print("✅ PASSED: Original text content preserved")
        else:
            print("❌ FAILED: Original text content lost")
            return False
        
        # Verify the media block is preserved
        if ':::media' in transformed_content and 'cdn.lqdev.tech' in transformed_content:
            print("✅ PASSED: Media block preserved correctly")
        else:
            print("❌ FAILED: Media block not preserved")
            return False
        
        print()
        print("=" * 80)
        print("🎉 Issue #688 is FIXED!")
        print("=" * 80)
        print()
        print("Summary:")
        print("- GitHub-generated <img> tags with empty src are now removed")
        print("- Original post content is preserved")
        print("- Media blocks remain intact")
        print("- The workflow now produces clean markdown files")
        
        return True
        
    finally:
        # Clean up
        if os.path.exists(temp_file):
            os.remove(temp_file)

if __name__ == '__main__':
    success = test_issue_688_scenario()
    sys.exit(0 if success else 1)
