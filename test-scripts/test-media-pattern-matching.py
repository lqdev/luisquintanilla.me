#!/usr/bin/env python3
"""
Test script for media pattern matching in upload_media.py
Tests various GitHub img tag formats and URL patterns
"""

import re

# Test content samples
test_cases = [
    {
        "name": "Empty src with attributes",
        "content": '<img width=1080 height=463 alt=Image src= />',
        "should_match": True,
        "description": "GitHub drag-and-drop after extraction"
    },
    {
        "name": "Empty src quoted",
        "content": '<img width=1080 height=463 alt=Image src="" />',
        "should_match": True,
        "description": "Empty src with quotes"
    },
    {
        "name": "Normal GitHub attachment markdown",
        "content": '![Image](https://github.com/user-attachments/assets/abc123.jpg)',
        "should_match": True,
        "description": "Standard markdown image"
    },
    {
        "name": "HTML img with GitHub URL",
        "content": '<img src="https://github.com/user-attachments/assets/abc123.jpg" alt="Image">',
        "should_match": True,
        "description": "HTML img tag with GitHub URL"
    },
    {
        "name": "YouTube URL",
        "content": 'https://youtube.com/watch?v=abc123',
        "should_match": True,
        "description": "YouTube URL to convert"
    },
    {
        "name": "Direct image URL",
        "content": 'https://example.com/image.jpg',
        "should_match": True,
        "description": "Direct media URL"
    },
    {
        "name": "Multiple img tags",
        "content": '''<img width=1080 height=463 alt=Image src= />
<img src="https://github.com/user-attachments/assets/test.jpg" alt="Test">''',
        "should_match": True,
        "description": "Multiple img tags to remove"
    },
]

def test_current_patterns():
    """Test current regex patterns from upload_media.py"""
    
    print("=" * 60)
    print("Testing Current Patterns from upload_media.py")
    print("=" * 60)
    
    # Current pattern from line 281-285
    current_github_pattern = r'<img[^>]*src=["\']([^"\']+)["\'][^>]*>'
    
    for test_case in test_cases:
        print(f"\nTest: {test_case['name']}")
        print(f"Description: {test_case['description']}")
        print(f"Content: {test_case['content']}")
        
        matches = re.findall(current_github_pattern, test_case['content'])
        if matches:
            print(f"✅ MATCHED: {matches}")
        else:
            print(f"❌ NO MATCH")
        
        print("-" * 60)

def test_enhanced_patterns():
    """Test enhanced patterns that should handle all cases"""
    
    print("\n" + "=" * 60)
    print("Testing Enhanced Patterns")
    print("=" * 60)
    
    # Enhanced pattern to catch all img tags
    enhanced_pattern = r'<img[^>]*>'
    
    for test_case in test_cases:
        if '<img' not in test_case['content']:
            continue
            
        print(f"\nTest: {test_case['name']}")
        print(f"Content: {test_case['content']}")
        
        matches = re.findall(enhanced_pattern, test_case['content'])
        if matches:
            print(f"✅ MATCHED: {matches}")
        else:
            print(f"❌ NO MATCH")
        
        print("-" * 60)

def test_removal_logic():
    """Test the complete removal logic"""
    
    print("\n" + "=" * 60)
    print("Testing Complete Removal Logic")
    print("=" * 60)
    
    test_content = '''My post content

<img width=1080 height=463 alt=Image src= />

Some more text

![Image](https://github.com/user-attachments/assets/test.jpg)

<img src="https://github.com/user-attachments/assets/test2.jpg" alt="Test">

Final text'''
    
    print("Original content:")
    print(test_content)
    print("\n" + "-" * 60)
    
    # Simulate removal
    transformed = test_content
    
    # Remove all img tags
    transformed = re.sub(r'<img[^>]*>', '', transformed)
    
    # Remove markdown images with GitHub URLs
    transformed = re.sub(
        r'!\[[^\]]*\]\(https://github\.com/user-attachments/[^)]+\)',
        '',
        transformed
    )
    
    # Clean up extra whitespace
    transformed = re.sub(r'\n\n+', '\n\n', transformed).strip()
    
    print("\nTransformed content:")
    print(transformed)
    print("\n" + "=" * 60)

if __name__ == '__main__':
    test_current_patterns()
    test_enhanced_patterns()
    test_removal_logic()
