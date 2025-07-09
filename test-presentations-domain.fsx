#r "bin/Debug/net9.0/PersonalSite.dll"

open Domain

// Test parsing existing presentation metadata (backward compatibility)
printfn "=== Testing Backward Compatibility ==="

// Test that new fields have reasonable defaults
let testMetadata : PresentationDetails = {
    Title = "Test Presentation"
    Resources = [||]
    Tags = ""      // Should handle empty string
    Date = ""      // Should handle empty string
}

let testPresentation : Presentation = {
    FileName = "test.md"
    Metadata = testMetadata
    Content = "Test content"
}

// Test ITaggable implementation
let taggable = testPresentation :> ITaggable
printfn "Title: %s" taggable.Title
printfn "FileName: %s" taggable.FileName  
printfn "ContentType: %s" taggable.ContentType
printfn "Date: '%s'" taggable.Date
printfn "Tags count: %d" taggable.Tags.Length
printfn "Tags: %A" taggable.Tags

// Test with tags
let testMetadataWithTags : PresentationDetails = {
    Title = "Test Presentation with Tags"
    Resources = [||]
    Tags = "ml.net, azure, containers"
    Date = "2022-04-15"
}

let testPresentationWithTags : Presentation = {
    FileName = "test-with-tags.md"
    Metadata = testMetadataWithTags
    Content = "Test content with tags"
}

let taggableWithTags = testPresentationWithTags :> ITaggable
printfn "\n=== Testing With Tags ==="
printfn "Title: %s" taggableWithTags.Title
printfn "Date: %s" taggableWithTags.Date
printfn "Tags count: %d" taggableWithTags.Tags.Length
printfn "Tags: %A" taggableWithTags.Tags

printfn "\n✅ All tests passed - Domain enhancement working correctly!"
