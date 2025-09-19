#r "bin/Debug/net9.0/PersonalSite.dll"

open System
open System.IO
open Domain

// Test 1: Load a review file and check its processing
let sampleReviewPath = "_src/reviews/library/serviceberry-robin-wall-kimerrer.md"
let sampleReviewPath2 = "_src/reviews/library/tiny-experiments-le-cunff.md"

// Test if files exist
printfn "Testing review files exist:"
printfn "File 1 exists: %b" (File.Exists(sampleReviewPath))
printfn "File 2 exists: %b" (File.Exists(sampleReviewPath2))

// Test 2: Load reviews content and check UnifiedFeedItem
printfn "\n=== Testing Review Loading ==="
let reviewsFiles = Directory.GetFiles("_src/reviews/library", "*.md")
printfn "Found %d review files" reviewsFiles.Length

// Take first few reviews for testing
let testReviews = reviewsFiles |> Array.take 3
for file in testReviews do
    printfn "Testing file: %s" (Path.GetFileName(file))
    let content = File.ReadAllText(file)
    printfn "Has custom blocks (:::): %b" (content.Contains(":::"))
    printfn "Has imageUrl: %b" (content.Contains("imageUrl"))
    if content.Contains("imageUrl") then
        let lines = content.Split('\n') |> Array.filter (fun line -> line.Contains("imageUrl"))
        for line in lines do
            printfn "  Image line: %s" (line.Trim())

// Test 3: Simple file content analysis
printfn "\n=== Testing File Content Analysis ==="
let sampleContent = File.ReadAllText(sampleReviewPath)

printfn "Content preview:"
let lines = sampleContent.Split('\n') |> Array.take 10
for line in lines do
    printfn "  %s" line

printfn "Has custom review block: %b" (sampleContent.Contains(":::review"))
printfn "Has imageUrl field: %b" (sampleContent.Contains("imageUrl:"))
printfn "Has rating field: %b" (sampleContent.Contains("rating:"))

// Extract imageUrl if present
if sampleContent.Contains("imageUrl:") then
    let imageLines = sampleContent.Split('\n') |> Array.filter (fun line -> line.Contains("imageUrl:"))
    for line in imageLines do
        printfn "Found image line: %s" (line.Trim())

// Test 4: Check simplified review content function
printfn "\n=== Current Issue Summary ==="
printfn "Based on the issue description, the problems are:"
printfn "1. Images not displaying in timeline reviews (empty src)"
printfn "2. Blog posts appearing when Reviews filter is toggled"
printfn "3. Simplified format not applied to all reviews"
printfn "4. Content type badge says 'Book Review' instead of just 'Book'"

printfn "\nCurrent simplified content is static placeholder:"
let simplifiedContent = """<h3 class="simplified-review-title">Book Review</h3>"""
printfn "Static title issue: %b" (simplifiedContent.Contains("Book Review"))
printfn "Should be dynamic based on item type: Book, Movie, etc."