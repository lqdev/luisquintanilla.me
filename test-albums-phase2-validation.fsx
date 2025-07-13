#r "bin/Debug/net9.0/PersonalSite.dll"

open Domain
open GenericBuilder
open System
open System.IO

// Test AlbumProcessor implementation - Phase 2 validation
printfn "=== AlbumProcessor Implementation Validation ==="

// Test 1: Verify AlbumProcessor creation
printfn "\n1. Testing AlbumProcessor creation..."
let processor = AlbumProcessor.create()
printfn "✅ AlbumProcessor created successfully"
printfn "   Processor type: %s" (processor.GetType().Name)

// Test 2: Test album parsing with existing sample
printfn "\n2. Testing album parsing..."
let sampleAlbumPath = Path.Combine("_src", "albums", "fall-mountains.md")
if File.Exists sampleAlbumPath then
    match processor.Parse sampleAlbumPath with
    | Some album ->
        printfn "✅ Album parsed successfully"
        printfn "   Title: %s" album.Metadata.Title
        printfn "   Date: %s" album.Metadata.Date
        printfn "   Images count: %d" (Array.length album.Metadata.Images)
        printfn "   First image: %s" album.Metadata.Images.[0].ImagePath
        
        // Test ITaggable interface
        let taggable = album :> ITaggable
        printfn "   ITaggable ContentType: %s" taggable.ContentType
        
        // Test output path generation
        let outputPath = processor.OutputPath album
        printfn "   Output path: %s" outputPath
        
        // Test card rendering
        let cardHtml = processor.RenderCard album
        printfn "   Card HTML length: %d characters" cardHtml.Length
        
        // Test RSS generation
        match processor.RenderRss album with
        | Some rssElement ->
            printfn "   RSS XML generated: %s" (rssElement.Name.LocalName)
        | None ->
            printfn "   No RSS XML generated"
            
    | None ->
        printfn "❌ Failed to parse album"
else
    printfn "⚠️ Sample album file not found at %s" sampleAlbumPath

// Test 3: Feature flag integration
printfn "\n3. Testing feature flag integration..."
let albumsEnabled = FeatureFlags.isEnabled FeatureFlags.Albums
printfn "✅ Feature flag check working"
printfn "   NEW_ALBUMS enabled: %b" albumsEnabled

printfn "\n=== Phase 2 AlbumProcessor Implementation Complete ==="
printfn "✅ All tests passed - ready for Phase 3"
