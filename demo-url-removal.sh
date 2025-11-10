#!/bin/bash
# Demonstration script showing URL removal from read-later.json
# This script demonstrates the new functionality without modifying the actual data

set -e

echo "=== Demonstration: Read Later URL Removal Feature ==="
echo ""
echo "This demonstrates the new functionality added to response and bookmark workflows."
echo ""

# Display current read-later.json entries
echo "📊 Current read-later.json entries:"
jq -r '.[] | "  - \(.title)\n    URL: \(.url)"' Data/read-later.json
echo ""

# Example URLs from the list
EXAMPLE_URL=$(jq -r '.[0].url' Data/read-later.json)
EXAMPLE_TITLE=$(jq -r '.[0].title' Data/read-later.json)

echo "🔍 Example: What happens when you create a response to a read-later URL?"
echo ""
echo "Scenario: Creating a reply to:"
echo "  Title: $EXAMPLE_TITLE"
echo "  URL: $EXAMPLE_URL"
echo ""
echo "When you use the GitHub issue template to create a response or bookmark:"
echo "  1. ✅ The response/bookmark post is created in _src/responses/ or _src/bookmarks/"
echo "  2. 🗑️  The URL is automatically removed from Data/read-later.json"
echo "  3. ✨ This keeps your read-later list clean and up-to-date"
echo ""
echo "Benefits:"
echo "  - No manual cleanup needed"
echo "  - Seamless workflow integration"
echo "  - Uses standard Unix tools (jq) for JSON processing"
echo "  - Handles non-existent URLs gracefully (no errors)"
echo ""
echo "=== Feature Implementation Complete ===" 
echo ""
echo "Modified files:"
echo "  - Scripts/process-response-issue.fsx (added removeFromReadLater function)"
echo "  - Scripts/process-bookmark-issue.fsx (added removeFromReadLater function)"
echo ""
echo "Technical details:"
echo "  - Uses jq command-line JSON processor"
echo "  - Invoked via bash shell for proper quoting"
echo "  - Executed before creating the content file"
echo "  - Gracefully handles missing files and non-existent URLs"
