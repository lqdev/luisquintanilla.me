#!/bin/bash
# Verify collection index generation

cd /workspaces/luisquintanilla.me/unix-ssg-mvp

echo "=== Current Build Structure ==="
echo "Root directory files:"
ls -la build/ | grep -E "^d" | head -10

echo ""
echo "Playlists directory:"
ls -la build/playlists/ 2>/dev/null || echo "Empty or doesn't exist"

echo ""
echo "Notes directory:"
ls -la build/notes/ 2>/dev/null || echo "Empty or doesn't exist"

echo ""
echo "=== Testing collection index generation ==="
chmod +x bin/generate-collection-indexes.sh
bash -x bin/generate-collection-indexes.sh build src

echo ""
echo "=== After generation ==="
echo "Playlists index:"
ls -la build/playlists/ 2>/dev/null || echo "Still empty"

echo ""
echo "Notes index:"
ls -la build/notes/ 2>/dev/null || echo "Still empty"
