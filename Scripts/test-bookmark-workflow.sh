#!/bin/bash

# Test script for bookmark publishing workflow
# This script demonstrates how the bookmark processing works

echo "🔖 Testing Bookmark Publishing Workflow"
echo "========================================"

# Navigate to project directory
cd "$(dirname "$0")/.."

# Build the project
echo "📦 Building F# project..."
dotnet build --verbosity quiet

if [ $? -ne 0 ]; then
    echo "❌ Build failed"
    exit 1
fi

echo "✅ Build successful"
echo ""

# Test 1: Valid bookmark with all fields
echo "🧪 Test 1: Complete bookmark with all fields"
echo "----------------------------------------------"
dotnet fsi Scripts/process-bookmark-issue.fsx -- \
    "https://github.com/lqdev/luisquintanilla.me" \
    "Luis Quintanilla's Personal Website" \
    "A great example of a personal website built with F# and static site generation" \
    "luis-personal-site" \
    "fsharp,staticsite,personal"

if [ $? -eq 0 ]; then
    echo "✅ Test 1 passed: Complete bookmark created successfully"
    # Clean up test file
    rm -f _src/bookmarks/luis-personal-site.md
else
    echo "❌ Test 1 failed"
fi

echo ""

# Test 2: Minimal bookmark (just URL and title)
echo "🧪 Test 2: Minimal bookmark (URL + title only)"
echo "-----------------------------------------------"
dotnet fsi Scripts/process-bookmark-issue.fsx -- \
    "https://fsharp.org" \
    "F# Programming Language"

if [ $? -eq 0 ]; then
    echo "✅ Test 2 passed: Minimal bookmark created successfully"
    # Clean up test file
    rm -f _src/bookmarks/f-programming-language.md
else
    echo "❌ Test 2 failed"
fi

echo ""

# Test 3: Invalid URL (should fail)
echo "🧪 Test 3: Invalid URL (should fail)"
echo "------------------------------------"
dotnet fsi Scripts/process-bookmark-issue.fsx -- \
    "not-a-valid-url" \
    "This Should Fail"

if [ $? -ne 0 ]; then
    echo "✅ Test 3 passed: Invalid URL properly rejected"
else
    echo "❌ Test 3 failed: Should have rejected invalid URL"
fi

echo ""

# Test 4: Missing arguments (should fail)
echo "🧪 Test 4: Missing arguments (should fail)"
echo "------------------------------------------"
dotnet fsi Scripts/process-bookmark-issue.fsx -- "https://example.com"

if [ $? -ne 0 ]; then
    echo "✅ Test 4 passed: Missing arguments properly detected"
else
    echo "❌ Test 4 failed: Should have detected missing arguments"
fi

echo ""
echo "🎉 Bookmark workflow testing complete!"
echo "======================================="
echo ""
echo "📚 To use the workflow:"
echo "1. Go to GitHub Issues"
echo "2. Create new issue with '🔖 Post a Bookmark' template"
echo "3. Fill out the form and submit"
echo "4. GitHub Actions will process and create a PR"
echo ""
echo "📖 For more details, see: docs/bookmark-publishing-workflow.md"