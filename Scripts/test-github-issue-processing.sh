#!/bin/bash

# Test script for process-github-issue.fsx
# This demonstrates local testing capability

echo "🧪 Testing F# GitHub Issue Processing Script"
echo "=============================================="

# Build the project first
echo "🔨 Building F# project..."
dotnet build

if [ $? -ne 0 ]; then
    echo "❌ Build failed"
    exit 1
fi

echo "✅ Build successful"
echo ""

# Test 1: Full parameters
echo "📋 Test 1: Full parameters (title, content, slug, tags)"
dotnet fsi Scripts/process-github-issue.fsx -- \
    "My Test Note" \
    "This is a comprehensive test of the F# script with all parameters provided. It demonstrates the functionality working locally." \
    "my-custom-slug" \
    "test,fsharp,local"

echo ""

# Test 2: Minimal parameters
echo "📋 Test 2: Minimal parameters (title and content only)"
dotnet fsi Scripts/process-github-issue.fsx -- \
    "Another Test Note" \
    "This test only provides title and content, letting the script auto-generate slug and use no tags."

echo ""

# Test 3: Error case - missing content
echo "📋 Test 3: Error case (missing content)"
dotnet fsi Scripts/process-github-issue.fsx -- \
    "Test Title"

echo ""
echo "🧹 Cleaning up test files..."
rm -f _src/notes/my-custom-slug-*.md
rm -f _src/notes/another-test-note-*.md

echo "✅ Tests completed!"
echo ""
echo "💡 Key Benefits:"
echo "   - Native F# integration with PersonalSite.dll"
echo "   - Local testing with familiar .NET tooling"
echo "   - Simple parameter-based interface"
echo "   - Consistent with weekly-wrapup.fsx pattern"
echo "   - No Node.js dependencies required"