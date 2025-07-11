// Simple validation of Post ITaggable interface after domain enhancement
#r "bin/Debug/net9.0/PersonalSite.dll"

open Domain
open System

// Test that Post type implements ITaggable interface correctly
let testPost = {
    FileName = "test-post"
    Metadata = {
        PostType = "article"
        Title = "Test Article"
        Description = "Test description"
        Date = "2025-07-10"
        Tags = [| "test"; "article" |]
    }
    Content = "Test content"
}

printfn "=== Posts Phase 1 Domain Enhancement Validation ==="
printfn $"✅ Created test post: {testPost.FileName}"

// Test ITaggable interface
let taggable = testPost :> ITaggable
printfn $"✅ ITaggable.Title: {taggable.Title}"
printfn $"✅ ITaggable.Date: {taggable.Date}"
printfn $"✅ ITaggable.FileName: {taggable.FileName}"
printfn $"✅ ITaggable.ContentType: {taggable.ContentType}"
printfn "✅ ITaggable.Tags: [%s]" (taggable.Tags |> String.concat "; ")

printfn "\n✅ Post type successfully implements ITaggable interface"
printfn "✅ Phase 1 Domain Enhancement Complete"
printfn "✅ Ready for Phase 2: Processor Implementation"
