#r "bin/Debug/net9.0/PersonalSite.dll"

open System
open Domain
open CustomBlocks
open Markdig
open Markdig.Extensions.CustomContainers

// Test extracting review image from book content
let sampleBookContent = """:::review
item: "The Four Agreements"
itemType: "book"
rating: 4.4
scale: 5.0
summary: "I purchased the paperback version but ended up listening to the audiobook version while doing chores..."
itemUrl: "https://openlibrary.org/works/OL27203W/The_Four_Agreements?edition="
imageUrl: "https://covers.openlibrary.org/b/id/15101528-L.jpg"
additionalFields:
  author: "Don Miguel Ruiz"
  isbn: "9781878424945"
  status: "Read"
:::

## Description
Some content here...
"""

// Create pipeline to parse custom blocks
let pipeline = 
    MarkdownPipelineBuilder()
        .Use(CustomBlockExtension())
        .Build()

// Parse the content
let document = Markdown.Parse(sampleBookContent, pipeline)

// Extract custom blocks
let customBlocks = extractCustomBlocks document

printfn "Custom blocks found: %A" customBlocks.Keys

match customBlocks.TryGetValue("review") with
| true, reviewList ->
    printfn "Found %d review blocks" reviewList.Length
    for reviewObj in reviewList do
        match reviewObj with
        | :? ReviewData as reviewData ->
            printfn "Review item: %s" reviewData.item
            printfn "Image URL: %A" reviewData.image_url
            printfn "Rating: %f" reviewData.rating
        | _ -> printfn "Unexpected review object type"
| false, _ ->
    printfn "No review blocks found"

// Helper function to extract image URL from book content
let extractReviewImageUrl (content: string) : string option =
    try
        let pipeline = 
            MarkdownPipelineBuilder()
                .Use(CustomBlockExtension())
                .Build()
        let document = Markdown.Parse(content, pipeline)
        let customBlocks = extractCustomBlocks document
        
        match customBlocks.TryGetValue("review") with
        | true, reviewList when reviewList.Length > 0 ->
            match reviewList.[0] with
            | :? ReviewData as reviewData -> reviewData.image_url
            | _ -> None
        | _ -> None
    with
    | ex -> 
        printfn "Error extracting review image: %s" ex.Message
        None

// Test the helper function
let testImageUrl = extractReviewImageUrl sampleBookContent
printfn "Extracted image URL: %A" testImageUrl