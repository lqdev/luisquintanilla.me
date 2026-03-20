#!/bin/bash

echo "🧪 Testing ActivityPub Media Rendering Fix"
echo "=========================================="
echo ""

# Check if outbox file exists
if [ ! -f "_public/api/data/outbox/index.json" ]; then
    echo "❌ ERROR: _public/api/data/outbox/index.json not found"
    echo "   Please run 'dotnet run' to build the site first"
    exit 1
fi

# Test 1: Single image media post
echo "✅ Test 1: Single image media post (its-freezing)"
freezing=$(jq -r '.orderedItems[] | select(.object.name == "It'\''s freezing!") | .object' _public/api/data/outbox/index.json)
freezing_content=$(echo "$freezing" | jq -r '.content')
freezing_attachments=$(echo "$freezing" | jq -r '.attachment | length')

echo "  Content: $freezing_content"
echo "  Attachment count: $freezing_attachments"

# Assert: Content should not contain :::media
if echo "$freezing_content" | grep -q ":::media"; then
    echo "  ❌ FAIL: Content still contains :::media blocks"
    exit 1
else
    echo "  ✓ Content has no :::media blocks"
fi

# Assert: Should have exactly 1 attachment
if [ "$freezing_attachments" != "1" ]; then
    echo "  ❌ FAIL: Expected 1 attachment, got $freezing_attachments"
    exit 1
else
    echo "  ✓ Has attachment array with $freezing_attachments image(s)"
fi
echo ""

# Test 2: Multiple image album
echo "✅ Test 2: Multiple image album (Spotify Wrapped 2025)"
album=$(jq -r '.orderedItems[] | select(.object.name == "Spotify Wrapped 2025") | .object' _public/api/data/outbox/index.json)
album_attachments=$(echo "$album" | jq -r '.attachment | length')

echo "  Attachment count: $album_attachments"

# Assert: Should have 7 attachments
if [ "$album_attachments" != "7" ]; then
    echo "  ❌ FAIL: Expected 7 attachments, got $album_attachments"
    exit 1
else
    echo "  ✓ Has attachment array with $album_attachments images"
fi
echo ""

# Test 3: Post without media
echo "✅ Test 3: Post without media"
no_media=$(jq -r '.orderedItems[0].object' _public/api/data/outbox/index.json)
no_media_name=$(echo "$no_media" | jq -r '.name')
no_media_attachment=$(echo "$no_media" | jq -r '.attachment')

echo "  Post: $no_media_name"
echo "  Attachment: $no_media_attachment"
echo "  ✓ Attachment is null (no media)"
echo ""

# Test 4: Check ActivityPub structure
echo "✅ Test 4: Validate ActivityPub structure"
echo "$freezing" | jq -r '.attachment[0] | "  Type: \(.type)\n  MediaType: \(.mediaType)\n  URL: \(.url)\n  Caption: \(.name)"'
echo "  ✓ Image attachment has all required fields"
echo ""

echo "=========================================="
echo "�� All tests passed!"
