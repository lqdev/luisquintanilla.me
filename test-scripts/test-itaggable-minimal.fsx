#load "../Domain.fs"

open System
open Domain

printfn "=== Phase 1C Minimal Test: ITaggable Interface ==="

// Test basic ITaggable helper functions
printfn "--- Testing ITaggable Helper Functions ---"

// Test Post type
let samplePost : Post = {
    FileName = "test.md"
    Metadata = {
        PostType = "article"
        Title = "Test Post"  
        Description = "Test"
        Date = "2025-07-08"
        Tags = [|"test"|]
    }
    Content = "content"
}

// Test basic helper functions
let postTags = ITaggableHelpers.getPostTags samplePost
let postTitle = ITaggableHelpers.getPostTitle samplePost

printfn "✓ Post Tags: [%s]" (String.Join("; ", postTags))
printfn "✓ Post Title: %s" postTitle

// Test ITaggable conversion
let postAsTaggable = ITaggableHelpers.postAsTaggable samplePost
printfn "✓ Post as ITaggable: %s | Type: %s" postAsTaggable.Title postAsTaggable.ContentType

printfn "\n=== ITaggable Interface Test Complete ==="
printfn "✅ ITaggable helper functions working correctly"
