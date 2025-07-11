#r "bin/Debug/net9.0/PersonalSite.dll"

open Domain
open Loaders

printfn "=== Posts Migration Phase 1 Validation ==="

// Test 1: Load actual posts
let srcDir = "_src"
let posts = loadPosts srcDir
printfn "✅ Loaded %d posts" posts.Length

// Test 2: Test ITaggable interface on first post
if posts.Length > 0 then
    let firstPost = posts.[0]
    printfn "Testing ITaggable on: %s" firstPost.FileName
    
    let taggable = firstPost :> ITaggable
    printfn "✅ ITaggable.ContentType: %s" taggable.ContentType
    printfn "✅ ITaggable.Title: %s" taggable.Title
    printfn "✅ ITaggable.FileName: %s" taggable.FileName
    printfn "✅ ITaggable.Date: %s" taggable.Date
    printfn "✅ ITaggable.Tags: %d tags" taggable.Tags.Length

printfn "Phase 1 validation complete!"
