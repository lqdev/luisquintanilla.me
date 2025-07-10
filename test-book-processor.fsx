#r "bin/Debug/net9.0/PersonalSite.dll"

open Domain
open GenericBuilder
open System.IO

// Test BookProcessor implementation
printfn "=== Testing BookProcessor Implementation ==="

// Test 1: Create BookProcessor
printfn "1. Creating BookProcessor..."
let processor = BookProcessor.create()
printfn "   ✅ BookProcessor created successfully"

// Test 2: Test parsing function with a real book file
printfn "\n2. Testing parsing function..."
let testBookPath = Path.Combine("_src", "library", "building-a-second-brain.md")
if File.Exists(testBookPath) then
    match processor.Parse testBookPath with
    | Some book ->
        printfn "   ✅ Successfully parsed book: %s" book.Metadata.Title
        printfn "      Author: %s" book.Metadata.Author
        printfn "      Rating: %.1f" book.Metadata.Rating
        printfn "      Status: %s" book.Metadata.Status
        printfn "      FileName: %s" book.FileName
        printfn "      Content length: %d characters" book.Content.Length
    | None ->
        printfn "   ❌ Failed to parse book"
else
    printfn "   ⚠️  Test book file not found: %s" testBookPath

// Test 3: Test all processor functions with sample data
printfn "\n3. Testing processor functions..."
let sampleBook : Book = {
    FileName = "test-book"
    Metadata = {
        Title = "Test Book Title"
        Author = "Test Author"
        Isbn = "1234567890"
        Cover = "https://example.com/cover.jpg"
        Status = "Read"
        Rating = 4.5
        Source = "https://example.com/book"
        DatePublished = "2024-01-01"
    }
    Content = "This is a test book review content."
}

// Test Render function
let rendered = processor.Render sampleBook
printfn "   ✅ Render function works (length: %d)" rendered.Length

// Test OutputPath function
let outputPath = processor.OutputPath sampleBook
printfn "   ✅ OutputPath: %s" outputPath

// Test RenderCard function
let card = processor.RenderCard sampleBook
printfn "   ✅ RenderCard function works (length: %d)" card.Length
printfn "      Card preview: %s..." (card.Substring(0, min 100 card.Length))

// Test RenderRss function
match processor.RenderRss sampleBook with
| Some rssElement ->
    printfn "   ✅ RenderRss function works"
    printfn "      RSS element: %s" (rssElement.ToString().Substring(0, min 150 (rssElement.ToString().Length)))
| None ->
    printfn "   ❌ RenderRss returned None"

// Test 4: Test buildContentWithFeeds with multiple books
printfn "\n4. Testing buildContentWithFeeds with real files..."
let libraryDir = Path.Combine("_src", "library")
if Directory.Exists(libraryDir) then
    let bookFiles = 
        Directory.GetFiles(libraryDir, "*.md")
        |> Array.take 3  // Test with first 3 books
        |> Array.toList
    
    let feedData = buildContentWithFeeds processor bookFiles
    printfn "   ✅ Processed %d books into feed data" feedData.Length
    
    feedData
    |> List.take (min 2 feedData.Length)
    |> List.iteri (fun i item ->
        printfn "      Book %d: %s by %s" (i + 1) item.Content.Metadata.Title item.Content.Metadata.Author
        printfn "        Card HTML length: %d" item.CardHtml.Length
        printfn "        RSS XML: %s" (if item.RssXml.IsSome then "Present" else "None"))
else
    printfn "   ⚠️  Library directory not found: %s" libraryDir

printfn "\n=== BookProcessor Test Complete ==="
