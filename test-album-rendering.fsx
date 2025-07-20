// Test script to verify album rendering fixes
#r "bin/Debug/net9.0/PersonalSite.dll"

open GenericBuilder
open Domain

// Test album rendering with content and images
let testAlbum: Album = {
    FileName = "test-album"
    Metadata = {
        PostType = "album"
        Title = "Test Album"
        Date = "2024-07-20"
        Tags = [||]
        Images = [|
            { ImagePath = "/media/test1.jpg"; Description = "Test image 1"; AltText = "First test image" }
            { ImagePath = "/media/test2.jpg"; Description = "Test image 2"; AltText = "Second test image" }
        |]
    }
    Content = "This is test content for an album. It should include :::media blocks and be properly rendered."
}

let processor = AlbumProcessor.create()
let cardHtml = processor.RenderCard testAlbum
let renderHtml = processor.Render testAlbum

printfn "Generated Album Card HTML:"
printfn "%s" cardHtml
printfn ""
printfn "Generated Album Render HTML:"
printfn "%s" renderHtml
printfn ""
printfn "✅ Verification: Card includes title: %b" (cardHtml.Contains("Test Album"))
printfn "✅ Verification: Card includes first image: %b" (cardHtml.Contains("/media/test1.jpg"))
printfn "✅ Verification: Card includes photo count: %b" (cardHtml.Contains("2 photos"))
printfn "✅ Verification: Render includes content: %b" (renderHtml.Contains("This is test content"))
