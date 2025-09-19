#r "bin/Debug/net9.0/PersonalSite.dll"

open System
open System.IO

// Test the content that would be passed to the timeline functions

// Read the sample review file
let sampleContent = File.ReadAllText("_src/reviews/library/serviceberry-robin-wall-kimerrer.md")

printfn "=== Original markdown content ==="
printfn "Content length: %d" sampleContent.Length
printfn "Has custom blocks: %b" (sampleContent.Contains(":::review"))
printfn "First 300 chars:"
printfn "%s..." (sampleContent.Substring(0, min 300 sampleContent.Length))

// Now test what happens when we parse through our domain types
// Let's see what the processing pipeline does to this content

printfn "\n=== Testing processing pipeline ==="

// The content gets processed through Book type and then convertBooksToUnified
// After our fix, the content should still be markdown (not HTML)

// Let's test our functions manually with the expected content
let testContent = sampleContent

printfn "Testing with content type: 'reviews'"
printfn "Content passed to timeline starts with: %s" (testContent.Substring(0, min 50 testContent.Length))

// The issue might be that our sed replacement broke the F# syntax
// Let's check if we can simulate what happens in the timeline

printfn "\n=== Manual function simulation ==="
printfn "1. extractReviewItemType should parse and return Some itemType"
printfn "2. createSimplifiedReviewContent should generate HTML"
printfn "3. Both functions have try-catch that returns empty on error"

printfn "\nPossible issues:"
printfn "- Sed replacement might have broken F# syntax"
printfn "- Functions might throw exceptions that are being caught"
printfn "- Import statements might be missing"
printfn "- Custom block parsing might fail in timeline context"

printfn "\nNext steps:"
printfn "1. Check if LayoutViews.fs compiles correctly"
printfn "2. Add debugging to the actual functions"
printfn "3. Test individual function calls"