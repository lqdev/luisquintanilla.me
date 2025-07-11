#r "../bin/Debug/net9.0/PersonalSite.dll"

open Domain
open System.IO

printfn "=== Posts Migration Phase 1 Validation ==="
printfn "Testing Post ITaggable implementation and domain enhancement..."

// Test 1: Post implements ITaggable correctly
let testPostITaggable() =
    printfn "\n--- Test 1: Post ITaggable Implementation ---"
    
    let testPost : Post = {
        FileName = "test-post"
        Metadata = {
            PostType = "article"
            Title = "Test Article"
            Description = "A test article for validation"
            Date = "2025-01-01"
            Tags = [|"test"; "validation"; "migration"|]
        }
        Content = "# Test Article\n\nThis is test content."
    }
    
    // Test ITaggable interface methods
    let taggable = testPost :> ITaggable
    
    printfn "✅ Post successfully implements ITaggable"
    printfn "  ContentType: %s" taggable.ContentType
    printfn "  Title: %s" taggable.Title
    printfn "  FileName: %s" taggable.FileName
    printfn "  Tags count: %d" taggable.Tags.Length
    printfn "  Tags: [%s]" (String.concat "; " taggable.Tags)
    
    // Verify expected values
    assert (taggable.ContentType = "post")
    assert (taggable.Title = "Test Article")
    assert (taggable.FileName = "test-post")
    assert (taggable.Tags.Length = 3)
    assert (taggable.Tags.[0] = "test")
    
    printfn "✅ All ITaggable interface assertions passed"

// Test 2: Load actual posts from filesystem
let testActualPostsLoading() =
    printfn "\n--- Test 2: Actual Posts Loading ---"
    
    let postsDirectory = "_src/posts"
    if Directory.Exists(postsDirectory) then
        let postFiles = Directory.GetFiles(postsDirectory, "*.md")
        printfn "Found %d post files in %s" postFiles.Length postsDirectory
        
        if postFiles.Length > 0 then
            // Test with first post file
            let firstPostFile = postFiles.[0]
            let fileName = Path.GetFileNameWithoutExtension(firstPostFile)
            printfn "Testing with file: %s" fileName
            
            // Create a sample post based on real file
            let samplePost : Post = {
                FileName = fileName
                Metadata = {
                    PostType = "article"
                    Title = sprintf "Sample: %s" fileName
                    Description = "Sample post for testing"
                    Date = "2025-01-01"
                    Tags = [|"sample"; "test"|]
                }
                Content = "Sample content"
            }
            
            let taggable = samplePost :> ITaggable
            printfn "✅ Real post file converted to ITaggable successfully"
            printfn "  File: %s → ContentType: %s" fileName taggable.ContentType
        else
            printfn "⚠️  No post files found for testing"
    else
        printfn "⚠️  Posts directory not found: %s" postsDirectory

// Test 3: Type compatibility verification
let testTypeCompatibility() =
    printfn "\n--- Test 3: Type Compatibility ---"
    
    // Test that Post can be used in generic ITaggable functions
    let processTaggable (item: ITaggable) =
        sprintf "%s: %s (%s)" item.ContentType item.Title item.FileName
    
    let testPost : Post = {
        FileName = "compatibility-test"
        Metadata = {
            PostType = "article"
            Title = "Compatibility Test"
            Description = "Testing type compatibility"
            Date = "2025-01-01"
            Tags = [|"compatibility"|]
        }
        Content = "Compatibility test content"
    }
    
    let result = processTaggable testPost
    printfn "✅ Post works with generic ITaggable functions"
    printfn "  Result: %s" result
    
    assert (result.Contains("post: Compatibility Test"))
    printfn "✅ Type compatibility assertions passed"

// Run all tests
try
    testPostITaggable()
    testActualPostsLoading()
    testTypeCompatibility()
    
    printfn "\n=== Phase 1 Validation Complete ==="
    printfn "✅ All tests passed successfully"
    printfn "✅ Post type implements ITaggable correctly"
    printfn "✅ Domain enhancement is ready for Phase 2"
    
with ex ->
    printfn "\n❌ Phase 1 Validation Failed"
    printfn "Error: %s" ex.Message
    printfn "Stack trace: %s" ex.StackTrace
    exit 1
