// Test script to debug snippet date handling
#r "bin/Debug/net9.0/PersonalSite.dll"

open Domain
open GenericBuilder
open System.IO

// Test reading a specific snippet
let testFile = "_src/resources/snippets/winget-config.md"

if File.Exists(testFile) then
    printfn "Testing file: %s" testFile
    
    // Parse the snippet
    let processor = SnippetProcessor.create()
    match processor.Parse testFile with
    | Some snippet ->
        printfn "✅ Successfully parsed snippet:"
        printfn "  Title: %s" snippet.Metadata.Title
        printfn "  Language: %s" snippet.Metadata.Language
        printfn "  Tags: %s" snippet.Metadata.Tags
        printfn "  CreatedDate: '%s'" snippet.Metadata.CreatedDate
        printfn "  IsCreatedDateEmpty: %b" (System.String.IsNullOrEmpty(snippet.Metadata.CreatedDate))
        
        // Test RSS generation
        printfn "\n📰 Testing RSS generation:"
        match processor.RenderRss snippet with
        | Some rssItem ->
            printfn "✅ RSS item generated:"
            printfn "%s" (rssItem.ToString())
        | None ->
            printfn "❌ No RSS item generated"
    | None ->
        printfn "❌ Failed to parse snippet"
else
    printfn "❌ File not found: %s" testFile
