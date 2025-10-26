#!/bin/bash

# Integration test for complete media workflow
# Tests the Python upload script + F# script integration

set -e  # Exit on error

echo "🧪 Media Workflow Integration Test"
echo "======================================"
echo ""

# Ensure we're in the project root
cd "$(dirname "$0")/.."

# Build F# project
echo "📦 Building F# project..."
dotnet build > /dev/null 2>&1
echo "✅ Build successful"
echo ""

# Test Case 1: GitHub drag-and-drop with empty img tag
echo "🧪 Test 1: GitHub drag-and-drop (empty img tag removal)"
echo "--------------------------------------------------------"

# Create test content file with the exact scenario from issue #688
TEST_CONTENT_1='My post

<img width=1080 height=463 alt=Image src= />

Some additional text'

echo "$TEST_CONTENT_1" > /tmp/test_media_content_1.txt

# Mock environment variables (not actually uploading)
export LINODE_STORAGE_ACCESS_KEY_ID="mock"
export LINODE_STORAGE_SECRET_ACCESS_KEY="mock"
export LINODE_STORAGE_ENDPOINT_URL="https://us-east-1.linodeobjects.com"
export LINODE_STORAGE_BUCKET_NAME="mock-bucket"
export LINODE_STORAGE_CUSTOM_DOMAIN="https://cdn.lqdev.tech"

# Run Python script (will skip upload since no GitHub URLs, but will transform)
echo "  Running Python transformation..."
python .github/scripts/upload_media.py /tmp/test_media_content_1.txt 2>&1 | grep -E "(Found|No GitHub|img tag)" || true

# Check transformed content
TRANSFORMED_1=$(cat /tmp/test_media_content_1.txt)

if echo "$TRANSFORMED_1" | grep -q '<img'; then
    echo "❌ FAILED: img tag still present in transformed content"
    echo "Content:"
    cat /tmp/test_media_content_1.txt
    exit 1
else
    echo "✅ PASSED: img tag removed successfully"
fi

if echo "$TRANSFORMED_1" | grep -q 'My post'; then
    echo "✅ PASSED: Original text content preserved"
else
    echo "❌ FAILED: Original text content lost"
    exit 1
fi

# Clean up
rm /tmp/test_media_content_1.txt

echo ""

# Test Case 2: Multiple img tag formats
echo "🧪 Test 2: Multiple img tag formats"
echo "--------------------------------------------------------"

TEST_CONTENT_2='My post content

<img width=1080 height=463 alt=Image src= />

Some text

<img src="" alt="Empty src">

More content

<img src="https://example.com/image.jpg" alt="Example">

Final text'

echo "$TEST_CONTENT_2" > /tmp/test_media_content_2.txt

echo "  Running Python transformation..."
python .github/scripts/upload_media.py /tmp/test_media_content_2.txt 2>&1 | grep -E "(Found|No GitHub)" || true

TRANSFORMED_2=$(cat /tmp/test_media_content_2.txt)

if echo "$TRANSFORMED_2" | grep -q '<img'; then
    echo "❌ FAILED: img tags still present"
    echo "Content:"
    cat /tmp/test_media_content_2.txt
    exit 1
else
    echo "✅ PASSED: All img tags removed"
fi

rm /tmp/test_media_content_2.txt

echo ""

# Test Case 3: YouTube URL formatting
echo "🧪 Test 3: YouTube URL thumbnail formatting"
echo "--------------------------------------------------------"

TEST_CONTENT_3='Check out this video:

https://youtube.com/watch?v=dQw4w9WgXcQ

Great stuff!'

echo "$TEST_CONTENT_3" > /tmp/test_media_content_3.txt

echo "  Running Python transformation..."
python .github/scripts/upload_media.py /tmp/test_media_content_3.txt 2>&1 | grep -E "(YouTube|Found)" || true

TRANSFORMED_3=$(cat /tmp/test_media_content_3.txt)

if echo "$TRANSFORMED_3" | grep -q 'img.youtube.com/vi/dQw4w9WgXcQ'; then
    echo "✅ PASSED: YouTube URL converted to thumbnail click syntax"
else
    echo "❌ FAILED: YouTube URL not formatted correctly"
    echo "Expected thumbnail syntax, got:"
    cat /tmp/test_media_content_3.txt
    exit 1
fi

rm /tmp/test_media_content_3.txt

echo ""

# Test Case 4: Direct media URL to :::media block
echo "🧪 Test 4: Direct media URL conversion"
echo "--------------------------------------------------------"

TEST_CONTENT_4='Here is a direct image:

https://example.com/photo.jpg

And some text.'

echo "$TEST_CONTENT_4" > /tmp/test_media_content_4.txt

echo "  Running Python transformation..."
python .github/scripts/upload_media.py /tmp/test_media_content_4.txt 2>&1 | grep -E "(direct media|Found)" || true

TRANSFORMED_4=$(cat /tmp/test_media_content_4.txt)

if echo "$TRANSFORMED_4" | grep -q ':::media'; then
    echo "✅ PASSED: Direct media URL converted to :::media block"
else
    echo "❌ FAILED: Direct media URL not converted to :::media block"
    echo "Content:"
    cat /tmp/test_media_content_4.txt
    exit 1
fi

if echo "$TRANSFORMED_4" | grep -q 'https://example.com/photo.jpg' && echo "$TRANSFORMED_4" | grep -q 'url:'; then
    echo "✅ PASSED: URL preserved in media block"
else
    echo "❌ FAILED: URL not preserved correctly in media block"
    exit 1
fi

rm /tmp/test_media_content_4.txt

echo ""

# Test Case 5: F# script integration with clean content
echo "🧪 Test 5: F# script integration with cleaned content"
echo "--------------------------------------------------------"

CLEAN_CONTENT='My media post content

No img tags or GitHub URLs here!'

echo "  Creating media post via F# script..."
OUTPUT=$(dotnet fsi Scripts/process-media-issue.fsx -- "image" "Test Integration" "$CLEAN_CONTENT" "landscape" "test-integration" "test" 2>&1)
EXIT_CODE=$?

if [ $EXIT_CODE -eq 0 ]; then
    echo "✅ PASSED: F# script executed successfully"
    
    if [ -f "_src/media/test-integration.md" ]; then
        echo "✅ PASSED: File created successfully"
        
        if grep -q "No img tags or GitHub URLs here" "_src/media/test-integration.md"; then
            echo "✅ PASSED: Content preserved correctly"
        else
            echo "❌ FAILED: Content not preserved"
            exit 1
        fi
        
        # Clean up
        rm "_src/media/test-integration.md"
    else
        echo "❌ FAILED: File not created"
        exit 1
    fi
else
    echo "❌ FAILED: F# script failed with exit code $EXIT_CODE"
    echo "$OUTPUT"
    exit 1
fi

echo ""

# Summary
echo "======================================"
echo "🎉 ALL INTEGRATION TESTS PASSED!"
echo "======================================"
echo ""
echo "✅ GitHub img tags with empty src removed"
echo "✅ Multiple img tag formats handled"
echo "✅ YouTube URLs converted to thumbnail click syntax"
echo "✅ Direct media URLs converted to :::media blocks"
echo "✅ F# script integration working"
echo ""
echo "The enhanced media workflow is ready for production!"
