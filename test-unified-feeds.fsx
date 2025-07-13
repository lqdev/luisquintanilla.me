#!/usr/bin/env dotnet fsi

// Test script to validate unified feed system implementation
// Run with: dotnet fsi test-unified-feeds.fsx

open System
open System.IO
open System.Xml

printfn "=== Unified Feed System Validation ==="
printfn ""

// Test 1: Verify unified feed exists and has content
let unifiedFeedPath = "_public/feed/index.xml"
if File.Exists(unifiedFeedPath) then
    let content = File.ReadAllText(unifiedFeedPath)
    printfn "✅ Unified feed exists: %s" unifiedFeedPath
    printfn "   File size: %d bytes" content.Length
    
    // Parse XML to count items
    try
        let doc = XmlDocument()
        doc.LoadXml(content)
        let items = doc.SelectNodes("//item")
        printfn "   Total items: %d" items.Count
        
        // Check feed metadata
        let titleNode = doc.SelectSingleNode("//channel/title")
        let descNode = doc.SelectSingleNode("//channel/description")
        let title = if titleNode <> null then titleNode.InnerText else "N/A"
        let description = if descNode <> null then descNode.InnerText else "N/A"
        printfn "   Title: %s" title
        printfn "   Description: %s" description
    with
    | ex -> printfn "   ⚠️  XML parsing error: %s" ex.Message
else
    printfn "❌ Unified feed not found: %s" unifiedFeedPath

printfn ""

// Test 2: Verify individual content type feeds exist
let expectedFeeds = [
    "_public/posts/index.xml", "Posts"
    "_public/feed/notes/index.xml", "Notes" 
    "_public/feed/responses/index.xml", "Responses"
    "_public/presentations/feed/index.xml", "Presentations"
    "_public/snippets/feed/index.xml", "Snippets"
    "_public/wiki/feed/index.xml", "Wiki"
    "_public/library/feed/index.xml", "Books"
]

let mutable totalItems = 0
let mutable validFeeds = 0

for feedPath, feedType in expectedFeeds do
    if File.Exists(feedPath) then
        try
            let content = File.ReadAllText(feedPath)
            let doc = XmlDocument()
            doc.LoadXml(content)
            let items = doc.SelectNodes("//item")
            let itemCount = items.Count
            totalItems <- totalItems + itemCount
            validFeeds <- validFeeds + 1
            printfn "✅ %s feed: %d items" feedType itemCount
        with
        | ex -> printfn "❌ %s feed XML error: %s" feedType ex.Message
    else
        printfn "❌ %s feed not found: %s" feedType feedPath

printfn ""
printfn "=== Summary ==="
printfn "Valid feeds: %d / %d" validFeeds expectedFeeds.Length
printfn "Total items in individual feeds: %d" totalItems
printfn ""

// Test 3: Verify no duplicate RSS generation in build functions
let buildModules = [
    "Builder.fs"
]

for moduleFile in buildModules do
    if File.Exists(moduleFile) then
        let content = File.ReadAllText(moduleFile)
        let rssGenerationCount = 
            content.Split('\n')
            |> Array.filter (fun line -> 
                line.Contains("generateRssFeed") || 
                line.Contains("saveRssFeed") ||
                line.Contains("writeRss"))
            |> Array.length
            
        if rssGenerationCount = 0 then
            printfn "✅ %s: No individual RSS generation (unified system in use)" moduleFile
        else
            printfn "⚠️  %s: Found %d RSS generation calls (may indicate migration incomplete)" moduleFile rssGenerationCount
    else
        printfn "❌ Build module not found: %s" moduleFile

printfn ""
printfn "=== Test Complete ==="
