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

// Organize responses by tag
let responsesByTag = 
    responses
    |> Array.filter(fun x -> (DateTime.Parse(x.Metadata.DatePublished) |> _.Year) = DateTime.UtcNow.Year)
    |> Array.collect(fun x -> 
            match x.Metadata.Tags with
            | null -> [|"untagged"|]
            | [||] -> [|"untagged"|]
            | _ -> x.Metadata.Tags
        )
    |> Array.countBy(fun x -> x)
    |> Array.sortByDescending(snd)

// Organize responses by host name (domain)
let responsesByDomain = 
    responses
    |> Array.filter(fun x -> (DateTime.Parse(x.Metadata.DatePublished) |> _.Year) = DateTime.UtcNow.Year)
    |> Array.countBy(fun x -> Uri(x.Metadata.TargetUrl).Host)
    |> Array.sortByDescending(snd)

// Utility function to display counts
let printEntryCounts<'a> (title:string) (entryCounts:('a * int) array) (n:int) = 
    printfn $"{title}"

    match entryCounts.Length with
    | 0 -> 
        printfn $"No entries"
        printfn $""
    | a when a > 0 -> 
        match n with 
        | n when n = -1 || n > entryCounts.Length -> 
            entryCounts
            |> Array.iter(fun x -> printfn $"{fst x} {snd x}")
            |> fun _ -> printfn $""
        | n when n > 0 -> 
            entryCounts
            |> Array.take n
            |> Array.iter(fun x -> printfn $"{fst x} {snd x}")
            |> fun _ -> printfn $""

// Print yearly counts
printEntryCounts "Blogs" postCountsByYear 2

printEntryCounts "Notes" noteCountsByYear 2

printEntryCounts "Responses" responseCountsByYear 2

printEntryCounts "Bookmarks" bookmarkCountsByYear 2

printEntryCounts "Reviews" reviewCountsByYear 2

printEntryCounts "Media" mediaCountsByYear 2

printEntryCounts "Timeline (All)" timelineCountsByYear 2

// Print response types
printEntryCounts "Response Types" responsesByType -1

// Print response tag counts
printEntryCounts "Response Tags" responsesByTag 5

// Print response by host name
printEntryCounts "Domains" responsesByDomain 5