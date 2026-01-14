#!/bin/bash
# Debug script to test markdown processing

FILE="/workspaces/luisquintanilla.me/unix-ssg-mvp/src/posts/welcome.md"
BUILD_DIR="/workspaces/luisquintanilla.me/unix-ssg-mvp/build"
SRC_DIR="/workspaces/luisquintanilla.me/unix-ssg-mvp/src"

echo "=== Debug: Processing $FILE ==="
echo ""

echo "Step 1: Extract title"
TITLE=$(grep "^title:" "$FILE" | sed 's/^title:[[:space:]]*//;s/["]//g' | head -1)
echo "Title: $TITLE"

echo ""
echo "Step 2: Extract published_date"
DATE=$(grep "^published_date:" "$FILE" | sed 's/^published_date:[[:space:]]*//;s/["]//g' | head -1)
echo "Date: $DATE"

echo ""
echo "Step 3: Extract post_type"
POST_TYPE=$(grep "^post_type:" "$FILE" | sed 's/^post_type:[[:space:]]*//;s/["]//g' | head -1)
echo "Post Type: $POST_TYPE"

echo ""
echo "Step 4: Get filename"
FILENAME=$(basename "$FILE" .md)
echo "Filename: $FILENAME"

echo ""
echo "Step 5: Determine output type"
OUTPUT_TYPE="${POST_TYPE:-posts}"
echo "Output Type: $OUTPUT_TYPE"

echo ""
echo "Step 6: Extract content (first 10 lines)"
CONTENT=$(awk '/^---$/{if(++count==2) {flag=1; next}} flag' "$FILE")
echo "First 10 lines:"
echo "$CONTENT" | head -10

echo ""
echo "Step 7: Output directory would be:"
OUTPUT_DIR="$BUILD_DIR/$OUTPUT_TYPE/$FILENAME"
echo "$OUTPUT_DIR"

echo ""
echo "Step 8: Test template substitution"
TEMPLATE="/workspaces/luisquintanilla.me/unix-ssg-mvp/templates/post.html"
if [ -f "$TEMPLATE" ]; then
    echo "Template exists: $TEMPLATE"
    echo "Template size: $(wc -c < "$TEMPLATE") bytes"
else
    echo "Template NOT found: $TEMPLATE"
fi
