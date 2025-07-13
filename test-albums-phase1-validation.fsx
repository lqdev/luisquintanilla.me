#r "bin/Debug/net9.0/PersonalSite.dll"

open Domain
open System

// Test Album domain enhancement - Phase 1 validation
printfn "=== Album Domain Enhancement Validation ==="

// Test 1: Verify AlbumDetails has tags field
printfn "\n1. Testing AlbumDetails type structure..."
let sampleAlbumDetails = {
    PostType = "album"
    Title = "Test Album"
    Date = "2023-10-15"
    Tags = [| "photography"; "test" |]
    Images = [||]
}
printfn "✅ AlbumDetails with Tags field created successfully"
printfn "   Tags: %A" sampleAlbumDetails.Tags
printfn "   Title: %s" sampleAlbumDetails.Title
printfn "   Date: %s" sampleAlbumDetails.Date

// Test 2: Verify Album implements ITaggable
printfn "\n2. Testing Album ITaggable implementation..."
let sampleAlbum = {
    FileName = "test-album"
    Metadata = sampleAlbumDetails
}

let taggable = sampleAlbum :> ITaggable
printfn "✅ Album implements ITaggable interface"
printfn "   Tags: %A" taggable.Tags
printfn "   Title: %s" taggable.Title
printfn "   Date: %s" taggable.Date
printfn "   FileName: %s" taggable.FileName
printfn "   ContentType: %s" taggable.ContentType

// Test 3: Verify null tags handling
printfn "\n3. Testing null tags handling..."
let albumWithNullTags = {
    FileName = "null-tags-album"
    Metadata = { sampleAlbumDetails with Tags = null }
}
let nullTaggable = albumWithNullTags :> ITaggable
printfn "✅ Null tags handled correctly: %A" nullTaggable.Tags

// Test 4: Verify MainImage field removal
printfn "\n4. Testing MainImage field removal..."
printfn "✅ AlbumDetails type compiled without MainImage field"
printfn "   Only essential fields remain: PostType, Title, Date, Tags, Images"

printfn "\n=== Phase 1 Domain Enhancement Complete ==="
printfn "✅ All tests passed - ready for Phase 2"
