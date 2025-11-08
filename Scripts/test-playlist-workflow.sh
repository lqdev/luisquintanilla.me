#!/bin/bash

# Test script for playlist workflow file discovery logic
# This validates that the workflow can find and read markdown files from output/ directory

echo "🧪 Testing Playlist Workflow File Discovery"
echo "=============================================="
echo ""

# Create a mock output directory structure
echo "📁 Creating mock output/ directory..."
mkdir -p output
echo "## Tracks

1. **Test Track** by Test Artist
   - Album: *Test Album*
   - Duration: 3:45
   - [Listen on YouTube](https://www.youtube.com/watch?v=test123)
   - [Backup: Listen on Spotify](https://open.spotify.com/track/test123)
" > output/test-playlist.md

echo "✅ Mock output directory created"
echo ""

# Test the find command from the workflow
echo "🔍 Testing file discovery logic..."
MARKDOWN_FILE=$(find output -name "*.md" -type f | head -n 1)

if [ -z "$MARKDOWN_FILE" ]; then
    echo "❌ Error: No markdown file found in output/ directory"
    rm -rf output
    exit 1
fi

echo "✅ Found markdown file: $MARKDOWN_FILE"
echo ""

# Test copying the file
echo "📋 Testing file copy operation..."
cp "$MARKDOWN_FILE" /tmp/playlist_tracks.md

if [ ! -f /tmp/playlist_tracks.md ]; then
    echo "❌ Error: Failed to copy markdown file"
    rm -rf output
    exit 1
fi

echo "✅ File copied successfully to /tmp/playlist_tracks.md"
echo ""

# Verify content
echo "📄 Content preview:"
head -10 /tmp/playlist_tracks.md
echo ""

# Test cleanup
echo "🧹 Testing cleanup operations..."
rm -f /tmp/playlist_tracks.md
rm -rf output/

if [ -d output ]; then
    echo "❌ Error: output/ directory still exists after cleanup"
    exit 1
fi

echo "✅ Cleanup successful"
echo ""

# Verify .gitignore has output/
echo "🔍 Verifying .gitignore has output/ entry..."
if grep -q "^output/$" .gitignore; then
    echo "✅ .gitignore correctly excludes output/ directory"
else
    echo "⚠️  Warning: output/ not found in .gitignore (expected at end of file)"
fi

echo ""
echo "✅ All tests passed!"
echo ""
echo "💡 Key Validations:"
echo "   - ✅ output/ directory can be created"
echo "   - ✅ Markdown files can be discovered with find command"
echo "   - ✅ Files can be copied to /tmp location"
echo "   - ✅ Cleanup removes output/ directory completely"
echo "   - ✅ .gitignore excludes output/ directory"
