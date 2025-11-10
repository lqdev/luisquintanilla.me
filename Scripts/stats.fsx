// Reference DLL
#r "../bin/Debug/net9.0/PersonalSite.dll"

// Add modules
open Domain
open Loaders
open System
open GenericBuilder

// Load posts
let posts = loadPosts "_src"
let notes = loadFeed "_src"
let responses = loadReponses "_src"

// Load bookmarks (Response objects from bookmarks directory)
let bookmarkResponses = 
    let bookmarkFiles = 
        System.IO.Directory.GetFiles(System.IO.Path.Join("_src", "bookmarks"))
        |> Array.filter (fun f -> f.EndsWith(".md"))
        |> Array.toList
    let processor = GenericBuilder.ResponseProcessor.create()
    let feedData = GenericBuilder.buildContentWithFeeds processor bookmarkFiles
    feedData |> List.map (fun item -> item.Content) |> List.toArray

// Load reviews (Book objects from reviews/library directory)
let reviews = 
    let bookFiles = 
        System.IO.Directory.GetFiles(System.IO.Path.Join("_src", "reviews", "library"))
        |> Array.filter (fun f -> f.EndsWith(".md"))
        |> Array.toList
    let processor = GenericBuilder.BookProcessor.create()
    let feedData = GenericBuilder.buildContentWithFeeds processor bookFiles
    feedData |> List.map (fun item -> item.Content) |> List.toArray

// Load media (Album objects from media directory)
let media = 
    let albumFiles = 
        System.IO.Directory.GetFiles(System.IO.Path.Join("_src", "media"))
        |> Array.filter (fun f -> f.EndsWith(".md"))
        |> Array.toList
    let processor = GenericBuilder.AlbumProcessor.create()
    let feedData = GenericBuilder.buildContentWithFeeds processor albumFiles
    feedData |> List.map (fun item -> item.Content) |> List.toArray

// Organize posts by year
let postCountsByYear = 
    posts
    |> Array.countBy (fun (x:Post) -> DateTime.Parse(x.Metadata.Date) |> _.Year)
    |> Array.sortByDescending fst 

let noteCountsByYear = 
    notes
    |> Array.countBy (fun (x:Post) -> DateTime.Parse(x.Metadata.Date) |> _.Year)
    |> Array.sortByDescending fst

let responseCountsByYear = 
    responses
    |> Array.countBy (fun (x:Response) -> DateTime.Parse(x.Metadata.DatePublished) |> _.Year)
    |> Array.sortByDescending fst

let bookmarkCountsByYear = 
    bookmarkResponses
    |> Array.countBy (fun (x:Response) -> DateTime.Parse(x.Metadata.DatePublished) |> _.Year)
    |> Array.sortByDescending fst

let reviewCountsByYear = 
    reviews
    |> Array.countBy (fun (x:Book) -> DateTime.Parse(x.Metadata.DatePublished) |> _.Year)
    |> Array.sortByDescending fst

let mediaCountsByYear = 
    media
    |> Array.countBy (fun (x:Album) -> DateTime.Parse(x.Metadata.Date) |> _.Year)
    |> Array.sortByDescending fst

// Aggregate timeline content (all content that appears in homepage timeline)
let timelineCountsByYear = 
    let allTimelineItems = 
        [
            posts |> Array.map (fun x -> DateTime.Parse(x.Metadata.Date) |> _.Year)
            notes |> Array.map (fun x -> DateTime.Parse(x.Metadata.Date) |> _.Year)
            responses |> Array.map (fun x -> DateTime.Parse(x.Metadata.DatePublished) |> _.Year)
            bookmarkResponses |> Array.map (fun x -> DateTime.Parse(x.Metadata.DatePublished) |> _.Year)
            reviews |> Array.map (fun x -> DateTime.Parse(x.Metadata.DatePublished) |> _.Year)
            media |> Array.map (fun x -> DateTime.Parse(x.Metadata.Date) |> _.Year)
        ]
        |> Array.concat
    allTimelineItems
    |> Array.countBy id
    |> Array.sortByDescending fst

// Organize responses by type
let responsesByType = 
    responses
    |> Array.filter(fun x -> (DateTime.Parse(x.Metadata.DatePublished) |> _.Year) = DateTime.UtcNow.Year)
    |> Array.countBy(fun x -> x.Metadata.ResponseType)
    |> Array.sortByDescending(snd)

// Combine responses and bookmarks for tag and domain analysis
let allResponsesIncludingBookmarks = Array.append responses bookmarkResponses

// Organize responses by tag (includes both responses and bookmarks)
let responsesByTag = 
    allResponsesIncludingBookmarks
    |> Array.filter(fun x -> (DateTime.Parse(x.Metadata.DatePublished) |> _.Year) = DateTime.UtcNow.Year)
    |> Array.collect(fun x -> 
            match x.Metadata.Tags with
            | null -> [|"untagged"|]
            | [||] -> [|"untagged"|]
            | _ -> x.Metadata.Tags
        )
    |> Array.countBy(fun x -> x)
    |> Array.sortByDescending(snd)

// Organize responses by host name (domain) (includes both responses and bookmarks)
let responsesByDomain = 
    allResponsesIncludingBookmarks
    |> Array.filter(fun x -> (DateTime.Parse(x.Metadata.DatePublished) |> _.Year) = DateTime.UtcNow.Year)
    |> Array.countBy(fun x -> Uri(x.Metadata.TargetUrl).Host)
    |> Array.sortByDescending(snd)

// Utility function to display counts in markdown format
let printEntryCounts<'a> (title:string) (entryCounts:('a * int) array) (n:int) = 
    printfn $"### {title}"
    printfn ""

    match entryCounts.Length with
    | 0 -> 
        printfn "No entries"
        printfn ""
    | a when a > 0 -> 
        printfn "| Item | Count |"
        printfn "|------|-------|"
        match n with 
        | n when n = -1 || n > entryCounts.Length -> 
            entryCounts
            |> Array.iter(fun x -> printfn $"| {fst x} | {snd x} |")
            |> fun _ -> printfn ""
        | n when n > 0 -> 
            entryCounts
            |> Array.take n
            |> Array.iter(fun x -> printfn $"| {fst x} | {snd x} |")
            |> fun _ -> printfn ""

// Print header
printfn "# 📊 Content Statistics"
printfn ""
let timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm")
printfn $"Generated on: {timestamp} UTC"
printfn ""
printfn "## 📋 Measurement Coverage"
printfn ""
printfn "This report analyzes content from the following directories and types:"
printfn ""
printfn "- **Blogs**: `_src/posts/` - Long-form blog posts"
printfn "- **Notes**: `_src/feed/` - Short-form notes and updates"
printfn "- **Responses**: `_src/responses/` - Replies, stars, and reshares"
printfn "- **Bookmarks**: `_src/bookmarks/` - Saved links and bookmarks"
printfn "- **Reviews**: `_src/reviews/library/` - Book reviews"
printfn "- **Media**: `_src/media/` - Photo albums and media collections"
printfn "- **Timeline (All)**: Aggregation of all content types above"
printfn ""
printfn "**Note**: Popular Tags and Top Domains sections include both direct responses (`_src/responses/`) and bookmarks (`_src/bookmarks/`) to provide comprehensive external content interaction statistics."
printfn ""

// Print yearly counts
printfn "## 📅 Content by Year (Top 2)"
printfn ""
printEntryCounts "Blogs" postCountsByYear 2
printEntryCounts "Notes" noteCountsByYear 2
printEntryCounts "Responses" responseCountsByYear 2
printEntryCounts "Bookmarks" bookmarkCountsByYear 2
printEntryCounts "Reviews" reviewCountsByYear 2
printEntryCounts "Media" mediaCountsByYear 2
printEntryCounts "Timeline (All)" timelineCountsByYear 2

// Print response types
printfn "## 🔖 Response Analysis"
printfn ""
printEntryCounts "Response Types" responsesByType -1

// Print response tag counts
printfn "## 🏷️ Popular Tags (Top 5)"
printfn ""
printEntryCounts "Response Tags" responsesByTag 5

// Print response by host name
printfn "## 🌐 Top Domains (Top 5)"
printfn ""
printEntryCounts "Domains" responsesByDomain 5