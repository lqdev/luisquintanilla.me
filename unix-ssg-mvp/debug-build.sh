#!/bin/bash
# Debug script to test markdown processing

cd /workspaces/luisquintanilla.me/unix-ssg-mvp

echo "=== Testing markdown processing ==="
echo ""
echo "Source files:"
ls -la src/notes/
ls -la src/playlists/

echo ""
echo "=== Rebuilding with updated script ==="
make clean build

echo ""
echo "=== Checking build output ==="
echo "Notes directory after build:"
ls -la build/notes/ 2>/dev/null || echo "Empty"

echo ""
echo "Playlists directory after build:"
ls -la build/playlists/ 2>/dev/null || echo "Empty"

echo ""
echo "All files in build/notes/:"
find build/notes -type f 2>/dev/null || echo "None found"

echo ""
echo "All files in build/playlists/:"
find build/playlists -type f 2>/dev/null || echo "None found"
