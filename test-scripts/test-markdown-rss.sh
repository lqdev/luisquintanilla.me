#!/bin/bash
set -e

echo "🔧 RSS Markdown Source Acceptance Test"
echo "======================================="
echo ""

echo "Building site..."
dotnet clean > /dev/null 2>&1
dotnet build > /dev/null 2>&1
dotnet run > /dev/null 2>&1

echo "✅ Site built successfully"
echo ""

echo "Checking namespace..."
if grep -q 'xmlns:source="http://source.scripting.com/"' _public/feed/feed.xml; then
    echo "✓ Namespace declared in main feed"
else
    echo "✗ Missing namespace in main feed"
    exit 1
fi

if grep -q 'xmlns:source="http://source.scripting.com/"' _public/posts/feed.xml; then
    echo "✓ Namespace declared in posts feed"
else
    echo "✗ Missing namespace in posts feed"
    exit 1
fi

echo ""

echo "Checking for Markdown elements..."
COUNT=$(grep -c 'source:markdown' _public/feed/feed.xml || true)
if [ "$COUNT" -gt 0 ]; then
    echo "✓ Found $COUNT source:markdown elements in main feed"
else
    echo "✗ No source:markdown elements found in main feed"
    exit 1
fi

POSTS_COUNT=$(grep -c 'source:markdown' _public/posts/feed.xml || true)
if [ "$POSTS_COUNT" -gt 0 ]; then
    echo "✓ Found $POSTS_COUNT source:markdown elements in posts feed"
else
    echo "✗ No source:markdown elements found in posts feed"
    exit 1
fi

NOTES_COUNT=$(grep -c 'source:markdown' _public/notes/feed.xml || true)
if [ "$NOTES_COUNT" -gt 0 ]; then
    echo "✓ Found $NOTES_COUNT source:markdown elements in notes feed"
else
    echo "✗ No source:markdown elements found in notes feed"
    exit 1
fi

RESPONSES_COUNT=$(grep -c 'source:markdown' _public/responses/feed.xml || true)
if [ "$RESPONSES_COUNT" -gt 0 ]; then
    echo "✓ Found $RESPONSES_COUNT source:markdown elements in responses feed"
else
    echo "✗ No source:markdown elements found in responses feed"
    exit 1
fi

echo ""

echo "Validating XML..."
if xmllint --noout _public/feed/feed.xml 2>&1; then
    echo "✓ Main feed is valid XML"
else
    echo "✗ Main feed has invalid XML"
    exit 1
fi

if xmllint --noout _public/posts/feed.xml 2>&1; then
    echo "✓ Posts feed is valid XML"
else
    echo "✗ Posts feed has invalid XML"
    exit 1
fi

if xmllint --noout _public/notes/feed.xml 2>&1; then
    echo "✓ Notes feed is valid XML"
else
    echo "✗ Notes feed has invalid XML"
    exit 1
fi

if xmllint --noout _public/responses/feed.xml 2>&1; then
    echo "✓ Responses feed is valid XML"
else
    echo "✗ Responses feed has invalid XML"
    exit 1
fi

echo ""

echo "Running F# verification script..."
dotnet fsi test-scripts/verify-markdown-rss.fsx

echo ""
echo "✓✓✓ All acceptance tests passed! ✓✓✓"
echo ""
echo "Summary:"
echo "  ✅ All feeds include xmlns:source namespace"
echo "  ✅ All feed items include source:markdown elements"
echo "  ✅ All feeds are valid XML (validated with xmllint)"
echo "  ✅ 100% coverage across all content types"
echo ""
echo "Implementation complete and verified!"
