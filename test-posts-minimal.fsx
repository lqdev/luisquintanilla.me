#r "bin/Debug/net9.0/PersonalSite.dll"

open Domain
open System.IO

// Quick test to validate Post type compilation
let testPostITaggable() =
    printfn "Testing Post ITaggable implementation..."
    
    // This will only compile if Post implements ITaggable correctly
    let dummyPost : Post = {
        FileName = "test"
        Metadata = {
            PostType = "article"
            Title = "Test"
            Description = "Test"
            Date = "2025-01-01"
            Tags = [|"test"|]
        }
        Content = "Test content"
    }
    
    // Test ITaggable interface
    let taggable = dummyPost :> ITaggable
    printfn "✅ Post implements ITaggable"
    printfn "  ContentType: %s" taggable.ContentType
    printfn "  Title: %s" taggable.Title
    printfn "  FileName: %s" taggable.FileName
    printfn "Success: Post ITaggable implementation verified"

testPostITaggable()
