#!/bin/bash
# Debug tags generation

cd /workspaces/luisquintanilla.me/unix-ssg-mvp

echo "=== DEBUG: Tags Generation ==="
echo "SRC_DIR: $PWD/src"
echo "BUILD_DIR: $PWD/build"
echo ""

echo "Markdown files found:"
find src -name "*.md" -type f 2>/dev/null | head -5

echo ""
echo "Checking one file for tags:"
if [ -f "src/playlists/crate-finds-example.md" ]; then
    echo "File exists. Contents:"
    head -15 "src/playlists/crate-finds-example.md"
    echo ""
    echo "Tags line:"
    grep "^tags:" "src/playlists/crate-finds-example.md" || echo "No tags line found"
else
    echo "File doesn't exist"
fi

echo ""
echo "Running tags script manually:"
bash -x bin/generate-tags.sh build src