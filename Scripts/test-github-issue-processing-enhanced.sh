#!/bin/bash

# Test script for GitHub issue processing with nested codeblocks
# This script validates that the enhanced process-github-issue.fsx correctly
# detects and formats code within text/markdown wrapped content

cd "$(dirname "$0")/.."

echo "🔬 Testing GitHub Issue Processing with Nested Codeblocks"
echo "==========================================================="

# Test 1: Original problematic case from issue #242
echo "📋 Test 1: F# code without explicit formatting"
cat > /tmp/test1_content.txt << 'EOF'
```text
## This is my post

I **like** posting from *GitHub*

I like to write code


let x = 1
printfn $"{x * 2}"


> To be or not to be

![Even add images](https://www.lqdev.me/avatar.png)
```
EOF

echo "   Running F# script..."
OUTPUT1=$(dotnet fsi Scripts/process-github-issue.fsx -- "Test F# Detection" "$(cat /tmp/test1_content.txt)" "test-fsharp-detection" "test" 2>&1)
if [ $? -eq 0 ]; then
    echo "   ✅ Script executed successfully"
    if grep -q '```fsharp' _src/notes/test-fsharp-detection.md; then
        echo "   ✅ F# code properly detected and formatted"
    else
        echo "   ❌ F# code not properly formatted"
        exit 1
    fi
else
    echo "   ❌ Script failed: $OUTPUT1"
    exit 1
fi

# Test 2: Multiple languages
echo ""
echo "📋 Test 2: Multiple programming languages"
cat > /tmp/test2_content.txt << 'EOF'
```markdown
# Multi-language test

Python code:
import sys
print("hello")

JavaScript code:
var x = 1;
console.log(x);

C# code:
using System;
Console.WriteLine("Hello");
```
EOF

echo "   Running F# script..."
OUTPUT2=$(dotnet fsi Scripts/process-github-issue.fsx -- "Test Multi Languages" "$(cat /tmp/test2_content.txt)" "test-multi-languages" "test" 2>&1)
if [ $? -eq 0 ]; then
    echo "   ✅ Script executed successfully"
    CONTENT=$(cat _src/notes/test-multi-languages.md)
    if echo "$CONTENT" | grep -q '```python' && echo "$CONTENT" | grep -q '```javascript' && echo "$CONTENT" | grep -q '```csharp'; then
        echo "   ✅ Multiple languages properly detected and formatted"
    else
        echo "   ❌ Not all languages properly detected"
        echo "Generated content:"
        cat _src/notes/test-multi-languages.md
        exit 1
    fi
else
    echo "   ❌ Script failed: $OUTPUT2"
    exit 1
fi

# Test 3: Preserve existing codeblocks
echo ""
echo "📋 Test 3: Preserve existing markdown codeblocks"
cat > /tmp/test3_content.txt << 'EOF'
```text
# Mixed content

Raw F# code:
let x = 1
printfn "hello"

Existing formatted code:
```javascript
let existing = "preserve me";
```

More raw code:
def python_func():
    return True
```
EOF

echo "   Running F# script..."
OUTPUT3=$(dotnet fsi Scripts/process-github-issue.fsx -- "Test Preserve Existing" "$(cat /tmp/test3_content.txt)" "test-preserve-existing" "test" 2>&1)
if [ $? -eq 0 ]; then
    echo "   ✅ Script executed successfully"
    CONTENT=$(cat _src/notes/test-preserve-existing.md)
    
    # Count javascript blocks - should have exactly 1 from existing, 1 new
    JS_COUNT=$(echo "$CONTENT" | grep -c '```javascript')
    PYTHON_COUNT=$(echo "$CONTENT" | grep -c '```python')
    FSHARP_COUNT=$(echo "$CONTENT" | grep -c '```fsharp')
    
    if [ "$JS_COUNT" -eq 1 ] && [ "$PYTHON_COUNT" -eq 1 ] && [ "$FSHARP_COUNT" -eq 1 ]; then
        echo "   ✅ Existing blocks preserved, new blocks detected"
    else
        echo "   ❌ Code block detection incorrect (JS:$JS_COUNT, Python:$PYTHON_COUNT, F#:$FSHARP_COUNT)"
        echo "Generated content:"
        cat _src/notes/test-preserve-existing.md
        exit 1
    fi
else
    echo "   ❌ Script failed: $OUTPUT3"
    exit 1
fi

# Cleanup
echo ""
echo "🧹 Cleaning up test files..."
rm -f _src/notes/test-*.md
rm -f /tmp/test*_content.txt

echo ""
echo "🎉 All tests passed! GitHub issue processing with nested codeblocks is working correctly."
echo ""
echo "✅ F# code detection and formatting"
echo "✅ Multiple language support (Python, JavaScript, C#, F#)"
echo "✅ Preservation of existing markdown codeblocks"
echo "✅ Proper unwrapping of text/markdown wrapped content"