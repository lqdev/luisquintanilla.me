#r "bin/Debug/net9.0/PersonalSite.dll"

open Domain
open GenericBuilder
open Builder
open MarkdownService
open System
open System.IO

// Test :::media block rendering validation - Phase 3
printfn "=== Media Block Rendering Validation ==="

// Test 1: Test album content conversion to :::media blocks
printfn "\n1. Testing AlbumImage to :::media block conversion..."
let sampleImages = [|
    { ImagePath = "/images/test1.jpg"; AltText = "Test image 1"; Description = "First test image" }
    { ImagePath = "/images/test2.jpg"; AltText = "Test image 2"; Description = "Second test image" }
|]

let sampleAlbum = {
    FileName = "test-album"
    Metadata = {
        PostType = "album"
        Title = "Test Album"
        Date = "2023-10-15"
        Tags = [| "test"; "photography" |]
        Images = sampleImages
    }
}

// Test media block conversion
let processor = AlbumProcessor.create()
let renderedContent = processor.Render sampleAlbum
printfn "✅ Album content rendered successfully"
printfn "   Rendered content length: %d characters" renderedContent.Length

// Check for :::media block syntax
if renderedContent.Contains(":::media") then
    printfn "   ✅ Contains :::media blocks"
    let mediaBlocks = renderedContent.Split(":::media") |> Array.length |> fun x -> x - 1
    printfn "   Media blocks found: %d" mediaBlocks
else
    printfn "   ❌ No :::media blocks found"

if renderedContent.Contains("h-entry") then
    printfn "   ✅ Contains IndieWeb h-entry microformat"
else
    printfn "   ❌ Missing h-entry microformat"

// Test 2: Test actual sample album rendering
printfn "\n2. Testing sample album rendering..."
let sampleAlbumPath = Path.Combine("_src", "albums", "fall-mountains.md")
if File.Exists sampleAlbumPath then
    match processor.Parse sampleAlbumPath with
    | Some realAlbum ->
        let realRenderedContent = processor.Render realAlbum
        printfn "✅ Real album rendered successfully"
        printfn "   Album: %s" realAlbum.Metadata.Title
        printfn "   Rendered length: %d characters" realRenderedContent.Length
        
        // Test markdown to HTML conversion
        let htmlContent = convertMdToHtml realRenderedContent
        printfn "   ✅ Markdown to HTML conversion successful"
        printfn "   HTML length: %d characters" htmlContent.Length
        
        // Check for image tags in HTML
        if htmlContent.Contains("<img") then
            printfn "   ✅ Contains HTML image tags"
        else
            printfn "   ⚠️ No HTML image tags found"
            
        // Check for responsive/lazy loading attributes
        if htmlContent.Contains("loading") then
            printfn "   ✅ Contains lazy loading attributes"
        else
            printfn "   ⚠️ No lazy loading attributes found"
            
    | None ->
        printfn "❌ Failed to parse sample album"
else
    printfn "❌ Sample album file not found"

// Test 3: Test individual album page generation
printfn "\n3. Testing individual album page generation..."
Environment.SetEnvironmentVariable("NEW_ALBUMS", "true")

try
    let _ = buildAlbums()
    
    // Check if individual album page was created
    let albumPagePath = Path.Combine("_public", "media", "fall-mountains", "index.html")
    if File.Exists albumPagePath then
        printfn "✅ Individual album page created at %s" albumPagePath
        let pageContent = File.ReadAllText(albumPagePath)
        printfn "   Page content length: %d characters" pageContent.Length
        
        // Check for essential content
        if pageContent.Contains("Fall Mountains") then
            printfn "   ✅ Contains album title"
        if pageContent.Contains("img") then
            printfn "   ✅ Contains image elements"
        if pageContent.Contains("h-entry") then
            printfn "   ✅ Contains IndieWeb microformats"
            
    else
        printfn "❌ Individual album page not found at %s" albumPagePath

with ex ->
    printfn "❌ Album page generation failed: %s" ex.Message

// Restore environment
Environment.SetEnvironmentVariable("NEW_ALBUMS", null)

printfn "\n=== Media Block Rendering Validation Complete ==="
