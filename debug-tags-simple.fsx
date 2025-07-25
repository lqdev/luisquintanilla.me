open System
open System.IO

// Debug tag extraction
printfn "=== Debug Tag Extraction ==="

// Check if unified feed was generated
let feedPath = "_public/feed/index.xml"
printfn "Main feed exists: %b" (File.Exists(feedPath))

// Check posts feed
let postsFeedPath = "_public/posts/feed.xml" 
printfn "Posts feed exists: %b" (File.Exists(postsFeedPath))

// Read a small sample from the main feed
if File.Exists(feedPath) then
    let feedContent = File.ReadAllText(feedPath)
    let hasCategories = feedContent.Contains("<category>")
    printfn "Feed contains categories: %b" hasCategories
    
    // Find the first category tag
    let categoryStart = feedContent.IndexOf("<category>")
    if categoryStart >= 0 then
        let categoryEnd = feedContent.IndexOf("</category>", categoryStart)
        if categoryEnd >= 0 then
            let categoryText = feedContent.Substring(categoryStart, categoryEnd - categoryStart + 11)
            printfn "First category found: %s" categoryText

// Check if tags directory exists
let tagsDir = "_public/tags"
if Directory.Exists(tagsDir) then
    let tagDirs = Directory.GetDirectories(tagsDir)
    printfn "Tag directories found: %d" tagDirs.Length
    
    // Check if any have feed.xml
    let feedCount = 
        tagDirs 
        |> Array.map (fun dir -> Path.Combine(dir, "feed.xml"))
        |> Array.filter File.Exists
        |> Array.length
    printfn "Tag directories with feed.xml: %d" feedCount
    
    // Show first few tag names
    let sampleTags = tagDirs |> Array.take (min 5 tagDirs.Length) |> Array.map Path.GetFileName
    printfn "Sample tag names: %s" (String.Join(", ", sampleTags))

printfn "\n=== Debug Complete ==="
