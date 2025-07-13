#r "bin/Debug/net9.0/PersonalSite.dll"

open Domain
open Builder
open System
open System.IO
open System.Xml
open System.Xml.Linq

// Test RSS feed generation and validation - Phase 3
printfn "=== Album RSS Feed Validation ==="

// Test 1: Enable NEW_ALBUMS and generate RSS feed
printfn "\n1. Testing RSS feed generation with NEW_ALBUMS=true..."
Environment.SetEnvironmentVariable("NEW_ALBUMS", "true")

try
    // Run album build process
    let feedData = buildAlbums()
    printfn "✅ Album build completed successfully"
    printfn "   Albums processed: %d" feedData.Length
    
    if feedData.Length > 0 then
        let firstAlbum = feedData.[0].Content
        printfn "   Sample album: %s" firstAlbum.Metadata.Title
        printfn "   Images in album: %d" (Array.length firstAlbum.Metadata.Images)
        
        // Test RSS XML generation
        match feedData.[0].RssXml with
        | Some rssElement ->
            printfn "   RSS XML element created: %s" (rssElement.Name.LocalName)
            printfn "   RSS title: %s" (rssElement.Element(XName.Get "title").Value)
        | None ->
            printfn "   ❌ No RSS XML generated"
    
    // Test 2: Validate RSS feed file creation
    printfn "\n2. Testing RSS feed file generation..."
    let rssFeedPath = Path.Combine("_public", "feed", "media", "rss.xml")
    if File.Exists rssFeedPath then
        printfn "✅ RSS feed file created at %s" rssFeedPath
        
        // Validate XML structure
        try
            let xmlDoc = XDocument.Load(rssFeedPath)
            let channels = xmlDoc.Descendants(XName.Get "channel") |> Seq.toList
            if not channels.IsEmpty then
                let channel = channels.Head
                let title = channel.Element(XName.Get "title")
                let link = channel.Element(XName.Get "link") 
                let description = channel.Element(XName.Get "description")
                let items = channel.Elements(XName.Get "item") |> Seq.toList
                
                printfn "✅ RSS XML structure valid"
                printfn "   Channel title: %s" (if title <> null then title.Value else "missing")
                printfn "   Channel link: %s" (if link <> null then link.Value else "missing") 
                printfn "   Items count: %d" items.Length
            else
                printfn "❌ RSS channel element missing"
        with ex ->
            printfn "❌ RSS XML validation failed: %s" ex.Message
    else
        printfn "❌ RSS feed file not found at %s" rssFeedPath
    
    // Test 3: Validate HTML index creation
    printfn "\n3. Testing HTML index generation..."
    let htmlIndexPath = Path.Combine("_public", "feed", "media", "index.html")
    if File.Exists htmlIndexPath then
        printfn "✅ Album HTML index created at %s" htmlIndexPath
        let htmlContent = File.ReadAllText(htmlIndexPath)
        printfn "   HTML content length: %d characters" htmlContent.Length
        if htmlContent.Contains("Fall Mountains") then
            printfn "   ✅ Sample album found in HTML index"
        else
            printfn "   ⚠️ Sample album not found in HTML index"
    else
        printfn "❌ Album HTML index not found at %s" htmlIndexPath
        
    // Test 4: Validate media index page
    printfn "\n4. Testing media index page..."
    let mediaIndexPath = Path.Combine("_public", "media", "index.html")
    if File.Exists mediaIndexPath then
        printfn "✅ Media index created at %s" mediaIndexPath
        let mediaContent = File.ReadAllText(mediaIndexPath)
        printfn "   Media index length: %d characters" mediaContent.Length
    else
        printfn "❌ Media index not found at %s" mediaIndexPath

with ex ->
    printfn "❌ Album build failed: %s" ex.Message

// Restore environment
Environment.SetEnvironmentVariable("NEW_ALBUMS", null)

printfn "\n=== Album RSS Feed Validation Complete ==="
