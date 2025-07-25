#r "nuget: FSharp.Data"

open System
open System.IO
open FSharp.Data

// Test tag RSS feed generation
printfn "=== Tag RSS Feed Validation Test ==="

// Run the website generator first (commented out for testing)
// System.Diagnostics.Process.Start("dotnet", "run").WaitForExit()

// Check if tag feeds were generated
let tagsDir = Path.GetFullPath("_public/tags")
printfn "Tags directory: %s" tagsDir

if Directory.Exists(tagsDir) then
    let tagDirectories = Directory.GetDirectories(tagsDir)
    printfn "Found %d tag directories" tagDirectories.Length
    
    // Check first few tags for feed.xml files
    let testTags = tagDirectories |> Array.take (min 5 tagDirectories.Length)
    
    for tagDir in testTags do
        let tagName = Path.GetFileName(tagDir)
        let feedPath = Path.Combine(tagDir, "feed.xml")
        let htmlPath = Path.Combine(tagDir, "index.html")
        
        printfn "\n--- Tag: %s ---" tagName
        printfn "HTML exists: %b" (File.Exists(htmlPath))
        printfn "RSS exists: %b" (File.Exists(feedPath))
        
        if File.Exists(feedPath) then
            try
                // Validate RSS structure
                type RssFeed = XmlProvider<"https://www.luisquintanilla.me/posts/feed.xml">
                let feed = RssFeed.Load(feedPath)
                printfn "RSS Title: %s" feed.Channel.Title
                printfn "RSS Items: %d" feed.Channel.Items.Length
                printfn "RSS URL: %s" feed.Channel.Link
                
                if feed.Channel.Items.Length > 0 then
                    let firstItem = feed.Channel.Items.[0]
                    printfn "First item: %s" firstItem.Title
                    // Check if categories (tags) exist
                    if firstItem.Categories.Length > 0 then
                        printfn "Item tags: %s" (String.Join(", ", firstItem.Categories))
            with
            | ex -> printfn "❌ RSS validation failed: %s" ex.Message
        else
            printfn "❌ RSS feed not found"
else
    printfn "❌ Tags directory not found"

printfn "\n=== Test Complete ==="
