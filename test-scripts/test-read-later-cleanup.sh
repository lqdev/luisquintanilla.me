#!/bin/bash
set -e

echo "=== Testing Read Later Cleanup Logic ==="
echo ""

# Test environment setup
RESPONSES_DIR="_src/responses"
BOOKMARKS_DIR="_src/bookmarks"
JSON_FILE="Data/read-later.json"

echo "1. Extracting target URLs from responses and bookmarks..."
TARGET_URLS_FILE=$(mktemp)

# Extract from responses
if [ -d "$RESPONSES_DIR" ]; then
  grep -h "^targeturl:" "$RESPONSES_DIR"/*.md 2>/dev/null | sed 's/^targeturl: *//' | sed 's/ *$//' >> "$TARGET_URLS_FILE" || true
fi

# Extract from bookmarks
if [ -d "$BOOKMARKS_DIR" ]; then
  grep -h "^targeturl:" "$BOOKMARKS_DIR"/*.md 2>/dev/null | sed 's/^targeturl: *//' | sed 's/ *$//' >> "$TARGET_URLS_FILE" || true
fi

# Sort and deduplicate URLs
sort -u "$TARGET_URLS_FILE" -o "$TARGET_URLS_FILE"

URL_COUNT=$(wc -l < "$TARGET_URLS_FILE")
echo "   Found $URL_COUNT unique target URLs"
echo ""

echo "2. Checking read-later.json for matching entries..."
ORIGINAL_COUNT=$(jq 'length' "$JSON_FILE")
echo "   Original entry count: $ORIGINAL_COUNT"

# Build jq filter to remove matching URLs
TARGET_URLS_ARRAY=$(cat "$TARGET_URLS_FILE" | jq -R -s -c 'split("\n") | map(select(length > 0))')

# Find matching entries (for display purposes only, not modifying file)
MATCHING_ENTRIES=$(mktemp)
jq --argjson urls "$TARGET_URLS_ARRAY" '[.[] | select([.url] | inside($urls))]' "$JSON_FILE" > "$MATCHING_ENTRIES"

MATCHING_COUNT=$(jq 'length' "$MATCHING_ENTRIES")
echo "   Entries that match responses/bookmarks: $MATCHING_COUNT"
echo ""

if [ "$MATCHING_COUNT" -gt 0 ]; then
  echo "3. Matching entries (would be removed in actual cleanup):"
  echo ""
  jq -r '.[] | "   - \(.title // .url)"' "$MATCHING_ENTRIES"
  echo ""
  
  # Show first few target URLs that matched
  echo "4. Sample target URLs that caused matches:"
  jq -r '.[0:5] | .[] | .url' "$MATCHING_ENTRIES" | while read url; do
    # Find which file contains this URL
    if grep -l "targeturl: $url" "$RESPONSES_DIR"/*.md 2>/dev/null | head -1 | xargs -I {} basename {} .md; then
      echo "   - $url (found in response)"
    elif grep -l "targeturl: $url" "$BOOKMARKS_DIR"/*.md 2>/dev/null | head -1 | xargs -I {} basename {} .md; then
      echo "   - $url (found in bookmark)"
    fi
  done
else
  echo "3. No matching entries found - cleanup would not modify the file."
fi

echo ""
echo "5. Summary:"
echo "   - Total read-later entries: $ORIGINAL_COUNT"
echo "   - Target URLs from responses/bookmarks: $URL_COUNT"
echo "   - Entries that would be removed: $MATCHING_COUNT"
echo "   - Entries that would remain: $((ORIGINAL_COUNT - MATCHING_COUNT))"
echo ""

# Cleanup
rm "$TARGET_URLS_FILE"
rm "$MATCHING_ENTRIES"

echo "✅ Test complete - no files were modified"
