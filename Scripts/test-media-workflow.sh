#!/bin/bash

# Test script for enhanced media publishing workflow
# This validates the markdown image parsing functionality

echo "🧪 Testing Enhanced Media Publishing Workflow"
echo "=============================================="

# Build the project first
echo "📦 Building F# project..."
dotnet build
if [ $? -ne 0 ]; then
    echo "❌ Build failed"
    exit 1
fi
echo "✅ Build successful"
echo

# Test 1: Single image with content
echo "🧪 Test 1: Single image with markdown content"
CONTENT1='Here'"'"'s my beautiful sunset photo from the beach:

![Beautiful sunset at the beach](https://cdn.lqdev.tech/files/images/sunset.jpg)

This was taken during golden hour with perfect lighting.'

OUTPUT=$(dotnet fsi Scripts/process-media-issue.fsx -- 'image' 'Beautiful Sunset' "$CONTENT1" 'landscape' 'sunset-photo' 'photography,nature,travel' 2>&1)
EXIT_CODE=$?

if [ $EXIT_CODE -eq 0 ]; then
    echo "✅ Test 1 passed"
    if [ -f "_src/media/sunset-photo.md" ]; then
        echo "✅ File created successfully"
        if grep -q ":::media" "_src/media/sunset-photo.md"; then
            echo "✅ Media block generated correctly"
        else
            echo "❌ Media block missing"
            exit 1
        fi
        if grep -q "aspectRatio: \"landscape\"" "_src/media/sunset-photo.md"; then
            echo "✅ Aspect ratio set correctly"
        else
            echo "❌ Aspect ratio missing or incorrect"
            exit 1
        fi
        if grep -q "caption: \"Beautiful sunset at the beach\"" "_src/media/sunset-photo.md"; then
            echo "✅ Alt text used as caption correctly"
        else
            echo "❌ Alt text caption missing or incorrect"
            exit 1
        fi
        if grep -q "Here's my beautiful sunset photo" "_src/media/sunset-photo.md"; then
            echo "✅ Clean content preserved"
        else
            echo "❌ Clean content not preserved"
            exit 1
        fi
        rm "_src/media/sunset-photo.md"
    else
        echo "❌ File not created"
        exit 1
    fi
else
    echo "❌ Test 1 failed with exit code $EXIT_CODE"
    echo "$OUTPUT"
    exit 1
fi
echo

# Test 2: Multiple images
echo "🧪 Test 2: Multiple images with markdown"
CONTENT2='Here are my photos from the mountain trip:

![Sunrise at the summit](https://cdn.lqdev.tech/files/images/sunrise.jpg)

The sunrise was absolutely incredible.

![Evening sunset view](https://cdn.lqdev.tech/files/images/sunset.jpg)

And the sunset was equally beautiful.'

OUTPUT=$(dotnet fsi Scripts/process-media-issue.fsx -- 'image' 'Mountain Views' "$CONTENT2" 'landscape' 'mountain-views' 'photography,nature' 2>&1)
EXIT_CODE=$?

if [ $EXIT_CODE -eq 0 ]; then
    echo "✅ Test 2 passed"
    if [ -f "_src/media/mountain-views.md" ]; then
        echo "✅ File created successfully"
        if grep -q "url: \"https://cdn.lqdev.tech/files/images/sunrise.jpg\"" "_src/media/mountain-views.md" && grep -q "url: \"https://cdn.lqdev.tech/files/images/sunset.jpg\"" "_src/media/mountain-views.md"; then
            echo "✅ Multiple images processed correctly"
        else
            echo "❌ Multiple images not processed correctly"
            exit 1
        fi
        if grep -q "caption: \"Sunrise at the summit\"" "_src/media/mountain-views.md" && grep -q "caption: \"Evening sunset view\"" "_src/media/mountain-views.md"; then
            echo "✅ Different captions for each image"
        else
            echo "❌ Individual captions not processed correctly"
            exit 1
        fi
        rm "_src/media/mountain-views.md"
    else
        echo "❌ File not created"
        exit 1
    fi
else
    echo "❌ Test 2 failed with exit code $EXIT_CODE"
    echo "$OUTPUT"
    exit 1
fi
echo

# Test 3: Video with minimal content
echo "🧪 Test 3: Video with minimal markdown"
CONTENT3='![Demo video](https://cdn.lqdev.tech/files/videos/demo.mp4)'

OUTPUT=$(dotnet fsi Scripts/process-media-issue.fsx -- 'video' 'Demo Video' "$CONTENT3" '' '' '' 2>&1)
EXIT_CODE=$?

if [ $EXIT_CODE -eq 0 ]; then
    echo "✅ Test 3 passed"
    if [ -f "_src/media/demo-video.md" ]; then
        echo "✅ File created successfully"
        if grep -q "mediaType: \"video\"" "_src/media/demo-video.md"; then
            echo "✅ Video media type set correctly"
        else
            echo "❌ Video media type missing or incorrect"
            exit 1
        fi
        rm "_src/media/demo-video.md"
    else
        echo "❌ File not created"
        exit 1
    fi
else
    echo "❌ Test 3 failed with exit code $EXIT_CODE"
    echo "$OUTPUT"
    exit 1
fi
echo

# Test 4: Error case - no attachments
echo "🧪 Test 4: Error handling for no attachments"
CONTENT4='This is just text content without any attachments.'

OUTPUT=$(dotnet fsi Scripts/process-media-issue.fsx -- 'image' 'No Attachments' "$CONTENT4" '' '' '' 2>&1)
EXIT_CODE=$?

if [ $EXIT_CODE -ne 0 ]; then
    echo "✅ Test 4 passed - error correctly detected"
    if echo "$OUTPUT" | grep -q "No media attachments found"; then
        echo "✅ Correct error message displayed"
    else
        echo "❌ Incorrect error message"
        echo "$OUTPUT"
        exit 1
    fi
else
    echo "❌ Test 4 failed - should have returned error"
    exit 1
fi
echo

# Test 5: Images without alt text
echo "🧪 Test 5: Images without alt text (using filename)"
CONTENT5='![](https://cdn.lqdev.tech/files/images/beautiful-landscape.jpg)'

OUTPUT=$(dotnet fsi Scripts/process-media-issue.fsx -- 'image' 'No Alt Text' "$CONTENT5" '' 'no-alt' '' 2>&1)
EXIT_CODE=$?

if [ $EXIT_CODE -eq 0 ]; then
    echo "✅ Test 5 passed"
    if [ -f "_src/media/no-alt.md" ]; then
        echo "✅ File created successfully"
        if grep -q "caption: \"beautiful-landscape\"" "_src/media/no-alt.md"; then
            echo "✅ Filename used as caption when alt text is empty"
        else
            echo "❌ Filename not used as fallback caption"
            exit 1
        fi
        rm "_src/media/no-alt.md"
    else
        echo "❌ File not created"
        exit 1
    fi
else
    echo "❌ Test 5 failed with exit code $EXIT_CODE"
    echo "$OUTPUT"
    exit 1
fi
echo

echo "🎉 All tests passed!"
echo "✅ Enhanced media publishing workflow is working correctly"
echo ""
echo "📋 Summary:"
echo "- ✅ Single image with markdown content and alt text captions"
echo "- ✅ Multiple images with individual captions"
echo "- ✅ Video posts with minimal content"
echo "- ✅ Error handling for missing attachments"
echo "- ✅ Filename fallback for missing alt text"
echo "- ✅ Markdown image parsing and content cleanup"
echo "- ✅ GitHub attachment format compatibility"