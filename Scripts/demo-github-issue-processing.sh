#!/bin/bash

# Demo script to test GitHub issue processing
# This demonstrates how the F# script processes markdown content and persists files

cd "$(dirname "$0")/.."

echo "🔬 Demo: GitHub Issue Processing with Markdown Persistence"
echo "=========================================================="

# Ensure project is built
echo "🔨 Building project..."
dotnet build --verbosity quiet

echo ""
echo "📋 Testing with sample markdown content:"
echo "----------------------------------------"

# Create sample content with markdown formatting
SAMPLE_CONTENT="Here is some **bold text** and *italic text*.

Here's a code block:
\`\`\`javascript
const x = 1;
console.log(x);
\`\`\`

And a list:
- Item 1
- Item 2
- Item 3

> This is a blockquote

[Here's a link](https://example.com)"

echo "Processing content..."
OUTPUT=$(dotnet fsi Scripts/process-github-issue.fsx -- "Demo Markdown Post" "$SAMPLE_CONTENT" "demo-markdown" "demo,testing,markdown" 2>&1)

if [ $? -eq 0 ]; then
    echo "✅ Processing successful!"
    echo ""
    echo "$OUTPUT"
    
    # Check if file exists
    if [ -f "_src/notes/demo-markdown.md" ]; then
        echo ""
        echo "📂 Generated file content verification:"
        echo "--------------------------------------"
        cat _src/notes/demo-markdown.md
        echo ""
        echo "--------------------------------------"
        echo "✅ File successfully persisted to _src/notes/demo-markdown.md"
        
        # Clean up demo file
        rm _src/notes/demo-markdown.md
        echo "🧹 Demo file cleaned up"
    else
        echo "❌ Error: Expected file was not created"
        exit 1
    fi
else
    echo "❌ Processing failed:"
    echo "$OUTPUT"
    exit 1
fi

echo ""
echo "🎉 Demo completed successfully!"
echo "The processing script correctly handles markdown content and persists files."