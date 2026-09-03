#!/bin/bash

# Test script for new media publishing workflow
# Tests the simplified F# script that works with pre-transformed content

echo "🧪 Testing New Media Publishing Workflow"
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

# Test 1: Pre-transformed content with media block
echo "🧪 Test 1: Pre-transformed content with media block"
CONTENT1='Here is my photo:

Beautiful evening!

:::media
- url: "https://cdn.luisquintanilla.me/files/images/20251026_140530_sunset.jpg"
  mediaType: "image"
  aspectRatio: "landscape"
  caption: "sunset.jpg"
:::media'

OUTPUT=$(dotnet fsi Scripts/process-media-issue.fsx -- 'image' 'Beautiful Sunset' "$CONTENT1" 'landscape' 'sunset-test' 'photography,nature' 2>&1)
EXIT_CODE=$?

if [ $EXIT_CODE -eq 0 ]; then
    echo "✅ Test 1 passed"
    if [ -f "_src/media/sunset-test.md" ]; then
        echo "✅ File created successfully"
        if grep -q ":::media" "_src/media/sunset-test.md"; then
            echo "✅ Media block preserved correctly"
        else
            echo "❌ Media block missing"
            exit 1
        fi
        if grep -q "Beautiful evening!" "_src/media/sunset-test.md"; then
            echo "✅ Content preserved correctly"
        else
            echo "❌ Content not preserved"
            exit 1
        fi
        if grep -q "cdn.luisquintanilla.me" "_src/media/sunset-test.md"; then
            echo "✅ Permanent CDN URL preserved"
        else
            echo "❌ CDN URL not preserved"
            exit 1
        fi
        rm "_src/media/sunset-test.md"
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

# Test 2: Multiple media blocks
echo "🧪 Test 2: Multiple media blocks"
CONTENT2='My collection:

:::media
- url: "https://cdn.luisquintanilla.me/files/images/20251026_140530_photo1.jpg"
  mediaType: "image"
  aspectRatio: "landscape"
  caption: "photo1.jpg"
:::media

:::media
- url: "https://cdn.luisquintanilla.me/files/videos/20251026_140531_video1.mp4"
  mediaType: "video"
  aspectRatio: "landscape"
  caption: "video1.mp4"
:::media'

OUTPUT=$(dotnet fsi Scripts/process-media-issue.fsx -- 'mixed' 'Mixed Media' "$CONTENT2" '' 'mixed-test' 'media' 2>&1)
EXIT_CODE=$?

if [ $EXIT_CODE -eq 0 ]; then
    echo "✅ Test 2 passed"
    if [ -f "_src/media/mixed-test.md" ]; then
        echo "✅ File created successfully"
        MEDIA_COUNT=$(grep -c ":::media" "_src/media/mixed-test.md")
        if [ $MEDIA_COUNT -eq 4 ]; then  # 2 opening + 2 closing tags
            echo "✅ Multiple media blocks preserved"
        else
            echo "❌ Media blocks count incorrect: $MEDIA_COUNT"
            exit 1
        fi
        rm "_src/media/mixed-test.md"
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

# Test 3: Frontmatter generation
echo "🧪 Test 3: Frontmatter generation"
CONTENT3=':::media
- url: "https://cdn.luisquintanilla.me/files/images/20251026_140532_test.jpg"
  mediaType: "image"
  aspectRatio: "portrait"
  caption: "test.jpg"
:::media'

OUTPUT=$(dotnet fsi Scripts/process-media-issue.fsx -- 'image' 'Test Frontmatter' "$CONTENT3" 'portrait' 'frontmatter-test' 'tag1,tag2,tag3' 2>&1)
EXIT_CODE=$?

if [ $EXIT_CODE -eq 0 ]; then
    echo "✅ Test 3 passed"
    if [ -f "_src/media/frontmatter-test.md" ]; then
        echo "✅ File created successfully"
        if grep -q "title: Test Frontmatter" "_src/media/frontmatter-test.md"; then
            echo "✅ Title in frontmatter"
        else
            echo "❌ Title missing from frontmatter"
            exit 1
        fi
        if grep -q "post_type: media" "_src/media/frontmatter-test.md"; then
            echo "✅ Post type in frontmatter"
        else
            echo "❌ Post type missing from frontmatter"
            exit 1
        fi
        if grep -q 'tags: \["tag1","tag2","tag3"\]' "_src/media/frontmatter-test.md"; then
            echo "✅ Tags in frontmatter"
        else
            echo "❌ Tags missing or incorrect in frontmatter"
            exit 1
        fi
        rm "_src/media/frontmatter-test.md"
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

# Test 4: Empty content with media metadata
echo "🧪 Test 4: Empty content with media metadata"
OUTPUT=$(dotnet fsi Scripts/process-media-issue.fsx -- 'image' 'Metadata Only' '' 'portrait' 'metadata-only' 'photography' 2>&1)
EXIT_CODE=$?

if [ $EXIT_CODE -eq 0 ]; then
    echo "✅ Test 4 passed"
    if [ -f "_src/media/metadata-only.md" ]; then
        if grep -q "title: Metadata Only" "_src/media/metadata-only.md" && \
           grep -q 'tags: \["photography"\]' "_src/media/metadata-only.md"; then
            echo "✅ Metadata-only media post created successfully"
        else
            echo "❌ Metadata-only frontmatter is incorrect"
            exit 1
        fi
        rm "_src/media/metadata-only.md"
    else
        echo "❌ Metadata-only file not created"
        exit 1
    fi
else
    echo "❌ Test 4 failed with exit code $EXIT_CODE"
    echo "$OUTPUT"
    exit 1
fi
echo

echo "🎉 All tests passed!"
echo "✅ New media publishing workflow is working correctly"
echo ""
echo "📋 Summary:"
echo "- ✅ Pre-transformed content with media blocks preserved"
echo "- ✅ Multiple media blocks handled correctly"
echo "- ✅ Frontmatter generated with proper metadata"
echo "- ✅ Permanent CDN URLs preserved in output"
echo "- ✅ Simplified F# script works as expected"
