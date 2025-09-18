#!/bin/bash

# Test script to reproduce the original failing case from issue #294
# This simulates the exact parameters that were failing in GitHub Actions

set -e

echo "🧪 Testing note publishing workflow fix..."
echo "📋 Using the exact parameters from the failing run:"
echo "   Title: 'Test note'"
echo "   Content: 'Test note'"  
echo "   Slug: 'my-sample'"
echo "   Tags: 'test,tag'"
echo ""

# Navigate to repository root
cd "$(dirname "$0")/.."

# Build the project first
echo "🔨 Building F# project..."
dotnet build > /dev/null

# Test the script with the exact failing parameters
echo "🚀 Running F# script with failing parameters..."
OUTPUT=$(dotnet fsi Scripts/process-github-issue.fsx -- "Test note" "Test note" "my-sample" "test,tag" 2>&1)
EXIT_CODE=$?

echo "📤 Script output:"
echo "$OUTPUT"
echo ""

if [ $EXIT_CODE -eq 0 ]; then
    echo "✅ SUCCESS: Script executed successfully with exit code 0"
    
    # Extract filename from output to verify it was created
    FILENAME=$(echo "$OUTPUT" | grep "📁 File:" | sed 's/.*📁 File: _src\/notes\///' | sed 's/ .*//')
    
    if [ -f "_src/notes/$FILENAME" ]; then
        echo "✅ SUCCESS: File created at _src/notes/$FILENAME"
        echo "📄 File contents:"
        cat "_src/notes/$FILENAME"
        echo ""
        
        # Clean up test file
        rm "_src/notes/$FILENAME"
        echo "🧹 Cleaned up test file"
    else
        echo "❌ ERROR: Expected file _src/notes/$FILENAME was not created"
        exit 1
    fi
else
    echo "❌ FAILURE: Script failed with exit code $EXIT_CODE"
    echo "This was the original issue - content validation was too strict"
    exit 1
fi

echo ""
echo "🎉 All tests passed! The note publishing workflow is now fixed."