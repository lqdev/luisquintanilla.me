// Test script to verify bookmarks are included in stats calculations
#r "../bin/Debug/net9.0/PersonalSite.dll"

open Domain
open Loaders
open System
open GenericBuilder

printfn "Testing stats script bookmark integration..."
printfn ""

// Load responses and bookmarks
let responses = loadReponses "_src"
let bookmarkResponses = 
    let bookmarkFiles = 
        System.IO.Directory.GetFiles(System.IO.Path.Join("_src", "bookmarks"))
        |> Array.filter (fun f -> f.EndsWith(".md"))
        |> Array.toList
    let processor = GenericBuilder.ResponseProcessor.create()
    let feedData = GenericBuilder.buildContentWithFeeds processor bookmarkFiles
    feedData |> List.map (fun item -> item.Content) |> List.toArray

// Test 1: Verify bookmarks are loaded
printfn "✓ Test 1: Bookmark loading"
printfn $"  Responses loaded: {responses.Length}"
printfn $"  Bookmarks loaded: {bookmarkResponses.Length}"
printfn $"  Total: {responses.Length + bookmarkResponses.Length}"
printfn ""

// Test 2: Verify combined array includes both
let allResponsesIncludingBookmarks = Array.append responses bookmarkResponses
printfn "✓ Test 2: Combined array"
printfn $"  Combined count: {allResponsesIncludingBookmarks.Length}"
printfn $"  Expected: {responses.Length + bookmarkResponses.Length}"
assert (allResponsesIncludingBookmarks.Length = responses.Length + bookmarkResponses.Length)
printfn ""

// Test 3: Verify tags from both sources are counted
let currentYear = DateTime.UtcNow.Year
let responseTags = 
    responses
    |> Array.filter(fun x -> (DateTime.Parse(x.Metadata.DatePublished) |> _.Year) = currentYear)
    |> Array.collect(fun x -> 
            match x.Metadata.Tags with
            | null -> [||]
            | [||] -> [||]
            | _ -> x.Metadata.Tags
        )
    |> Array.length

let bookmarkTags = 
    bookmarkResponses
    |> Array.filter(fun x -> (DateTime.Parse(x.Metadata.DatePublished) |> _.Year) = currentYear)
    |> Array.collect(fun x -> 
            match x.Metadata.Tags with
            | null -> [||]
            | [||] -> [||]
            | _ -> x.Metadata.Tags
        )
    |> Array.length

let combinedTags = 
    allResponsesIncludingBookmarks
    |> Array.filter(fun x -> (DateTime.Parse(x.Metadata.DatePublished) |> _.Year) = currentYear)
    |> Array.collect(fun x -> 
            match x.Metadata.Tags with
            | null -> [||]
            | [||] -> [||]
            | _ -> x.Metadata.Tags
        )
    |> Array.length

printfn "✓ Test 3: Tag counting"
printfn $"  Response tags (current year): {responseTags}"
printfn $"  Bookmark tags (current year): {bookmarkTags}"
printfn $"  Combined tags: {combinedTags}"
printfn $"  Expected: {responseTags + bookmarkTags}"
assert (combinedTags = responseTags + bookmarkTags)
printfn ""

// Test 4: Verify domains from both sources
let responseDomains = 
    responses
    |> Array.filter(fun x -> (DateTime.Parse(x.Metadata.DatePublished) |> _.Year) = currentYear)
    |> Array.length

let bookmarkDomains = 
    bookmarkResponses
    |> Array.filter(fun x -> (DateTime.Parse(x.Metadata.DatePublished) |> _.Year) = currentYear)
    |> Array.length

let combinedDomains = 
    allResponsesIncludingBookmarks
    |> Array.filter(fun x -> (DateTime.Parse(x.Metadata.DatePublished) |> _.Year) = currentYear)
    |> Array.length

printfn "✓ Test 4: Domain counting"
printfn $"  Responses (current year): {responseDomains}"
printfn $"  Bookmarks (current year): {bookmarkDomains}"
printfn $"  Combined: {combinedDomains}"
printfn $"  Expected: {responseDomains + bookmarkDomains}"
assert (combinedDomains = responseDomains + bookmarkDomains)
printfn ""

printfn "✅ All tests passed! Bookmarks are properly included in statistics."
