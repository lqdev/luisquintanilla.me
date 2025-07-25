#r "nuget: FSharp.Data"

open System
open System.IO
open System.Xml.Linq
open FSharp.Data

// Debug tag extraction
printfn "=== Debug Tag Extraction ==="

// Check unified feed items for tags
let feedPath = "_public/feed/index.xml"
if File.Exists(feedPath) then
    printfn "Found main feed: %s" feedPath
    
    try
        type MainFeed = XmlProvider<"https://www.luisquintanilla.me/feed/index.xml">
        let feed = MainFeed.Load(feedPath)
        
        printfn "Main feed items: %d" feed.Channel.Items.Length
        
        // Check first few items for categories/tags
        let sampleItems = feed.Channel.Items |> Array.take (min 5 feed.Channel.Items.Length)
        
        for item in sampleItems do
            printfn "\n--- Item: %s ---" item.Title
            printfn "Categories: %d" item.Categories.Length
            if item.Categories.Length > 0 then
                printfn "Tags: %s" (String.Join(", ", item.Categories))
            else
                printfn "No tags found!"
    with
    | ex -> printfn "❌ Error reading main feed: %s" ex.Message
else
    printfn "❌ Main feed not found at: %s" feedPath

// Also check a specific type feed
let postsFeedPath = "_public/posts/feed.xml"
if File.Exists(postsFeedPath) then
    printfn "\n=== Posts Feed Check ==="
    try
        type PostsFeed = XmlProvider<"https://www.luisquintanilla.me/posts/feed.xml">
        let postsFeed = PostsFeed.Load(postsFeedPath)
        
        let samplePosts = postsFeed.Channel.Items |> Array.take (min 3 postsFeed.Channel.Items.Length)
        
        for item in samplePosts do
            printfn "\n--- Post: %s ---" item.Title
            printfn "Categories: %d" item.Categories.Length
            if item.Categories.Length > 0 then
                printfn "Tags: %s" (String.Join(", ", item.Categories))
            else
                printfn "No tags found in post!"
    with
    | ex -> printfn "❌ Error reading posts feed: %s" ex.Message

printfn "\n=== Debug Complete ==="
