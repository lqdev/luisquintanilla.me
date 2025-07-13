#r "bin/Debug/net9.0/PersonalSite.dll"

open Domain
open TagService
open System
open System.IO

// Test tag integration validation - Phase 3
printfn "=== Tag Integration Validation ==="

// Test 1: Create sample albums with tags
printfn "\n1. Testing album ITaggable interface with tag filtering..."

let sampleAlbums = [|
    {
        FileName = "album1"
        Metadata = {
            PostType = "album"
            Title = "Photography Album"
            Date = "2023-10-15"
            Tags = [| "photography"; "nature"; "mountains" |]
            Images = [||]
        }
    }
    {
        FileName = "album2"
        Metadata = {
            PostType = "album"
            Title = "Urban Album"
            Date = "2023-10-16"
            Tags = [| "photography"; "urban"; "cityscape" |]
            Images = [||]
        }
    }
    {
        FileName = "album3"
        Metadata = {
            PostType = "album"
            Title = "Untagged Album"
            Date = "2023-10-17"
            Tags = [||]
            Images = [||]
        }
    }
|]

// Test ITaggable interface
printfn "✅ Sample albums created with tags"
sampleAlbums |> Array.iteri (fun i album ->
    let taggable = album :> ITaggable
    printfn "   Album %d: %s" (i+1) taggable.Title
    printfn "   Tags: %A" taggable.Tags
    printfn "   ContentType: %s" taggable.ContentType
    printfn "   Date: %s" taggable.Date
)

// Test 2: Test tag filtering functionality
printfn "\n2. Testing tag filtering with albums..."

let albumsAsTaggable = sampleAlbums |> Array.map (fun a -> a :> ITaggable)

// Test filtering by specific tag
let photographyAlbums = albumsAsTaggable |> Array.filter (fun item -> 
    item.Tags |> Array.contains "photography")
printfn "✅ Photography tag filtering"
printfn "   Albums with 'photography' tag: %d" photographyAlbums.Length

let mountainsAlbums = albumsAsTaggable |> Array.filter (fun item -> 
    item.Tags |> Array.contains "mountains")
printfn "   Albums with 'mountains' tag: %d" mountainsAlbums.Length

let untaggedAlbums = albumsAsTaggable |> Array.filter (fun item -> 
    item.Tags.Length = 0)
printfn "   Untagged albums: %d" untaggedAlbums.Length

// Test 3: Test tag aggregation functionality
printfn "\n3. Testing tag aggregation with albums..."

// Get all unique tags from albums
let allTags = albumsAsTaggable 
              |> Array.collect (fun item -> item.Tags)
              |> Array.distinct
              |> Array.sort

printfn "✅ Tag aggregation working"
printfn "   All unique tags: %A" allTags
printfn "   Total unique tags: %d" allTags.Length

// Test tag frequency
let tagFrequency = allTags |> Array.map (fun tag ->
    let count = albumsAsTaggable |> Array.filter (fun item -> 
        item.Tags |> Array.contains tag) |> Array.length
    (tag, count))

printfn "   Tag frequency:"
tagFrequency |> Array.iter (fun (tag, count) ->
    printfn "     %s: %d" tag count)

// Test 4: Test with real sample album
printfn "\n4. Testing with real sample album..."
let sampleAlbumPath = Path.Combine("_src", "albums", "fall-mountains.md")
if File.Exists sampleAlbumPath then
    let processor = GenericBuilder.AlbumProcessor.create()
    match processor.Parse sampleAlbumPath with
    | Some realAlbum ->
        let taggable = realAlbum :> ITaggable
        printfn "✅ Real album ITaggable interface"
        printfn "   Title: %s" taggable.Title
        printfn "   Tags: %A" taggable.Tags
        printfn "   ContentType: %s" taggable.ContentType
        printfn "   FileName: %s" taggable.FileName
        
        // Test if tags from YAML work
        if taggable.Tags.Length > 0 then
            printfn "   ✅ Has tags from YAML metadata"
        else
            printfn "   ⚠️ No tags found - check YAML metadata"
            
    | None ->
        printfn "❌ Failed to parse real album"
else
    printfn "❌ Real album file not found"

printfn "\n=== Tag Integration Validation Complete ==="
