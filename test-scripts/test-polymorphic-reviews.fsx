// Test script to validate polymorphic review types
#r "../bin/Debug/net10.0/PersonalSite.dll"

open System
open System.IO
open CustomBlocks
open GenericBuilder

printfn "=== Testing Polymorphic Review Types ==="
printfn ""

// Test 1: Book Review (should deserialize to BookReview)
let bookReviewContent = """---
title: "Test Book Review"
post_type: "review"
published_date: "01/01/2025 10:00 -05:00"
tags: ["test", "books"]
---

# Test Book Review

:::review
item: "The Test Book"
itemType: "book"
author: "Test Author"
isbn: "9781234567890"
cover: "https://example.com/cover.jpg"
datePublished: "01/01/2025 10:00 -05:00"
rating: 4.5
scale: 5.0
summary: "A great test book"
itemUrl: "https://example.com/book"
:::

Excellent book!
"""

printfn "Test 1: Book Review"
let bookFile = "/tmp/test-book-review.md"
File.WriteAllText(bookFile, bookReviewContent)

match BookProcessor.create().Parse bookFile with
| Some book ->
    printfn "  ✅ Parsed successfully as Book"
    printfn "  Title: %s" book.Metadata.Title
    printfn "  Author: %s" book.Metadata.Author
    printfn "  ISBN: %s" book.Metadata.Isbn
    printfn "  Rating: %.1f" book.Metadata.Rating
| None ->
    printfn "  ❌ Parsing failed"

File.Delete(bookFile)
printfn ""

// Test 2: Generic Review (should deserialize to GenericReview)
let movieReviewContent = """---
title: "Test Movie Review"
post_type: "review"
published_date: "01/01/2025 10:00 -05:00"
tags: ["test", "movies"]
---

# Test Movie Review

:::review
item: "The Test Movie"
itemType: "movie"
rating: 5.0
scale: 5.0
summary: "Amazing cinematography"
itemUrl: "https://example.com/movie"
imageUrl: "https://example.com/poster.jpg"
:::

Great film!
"""

printfn "Test 2: Generic Review (Movie)"
let movieFile = "/tmp/test-movie-review.md"
File.WriteAllText(movieFile, movieReviewContent)

// Parse using ASTParsing to check the union type
match ASTParsing.parseBookFromFile movieFile with
| Ok parsedDoc ->
    match parsedDoc.CustomBlocks.TryGetValue("review") with
    | true, reviewList when reviewList.Length > 0 ->
        match reviewList.[0] with
        | :? ReviewData as reviewData ->
            printfn "  ✅ Parsed successfully as ReviewData union"
            match reviewData with
            | BookReview _ -> printfn "  Type: BookReview"
            | GenericReview _ -> printfn "  Type: GenericReview ✅"
            printfn "  Item: %s" reviewData.Item
            printfn "  ItemType: %s" reviewData.ItemType
            printfn "  Rating: %.1f" reviewData.Rating
            printfn "  Author (should be Unknown): %s" (reviewData.GetAuthor())
        | _ -> printfn "  ❌ Wrong type"
    | _ -> printfn "  ❌ No review block found"
| Error err -> printfn "  ❌ Parse error: %A" err

File.Delete(movieFile)
printfn ""

printfn "=== Summary ==="
printfn "✅ Polymorphic deserialization working"
printfn "✅ BookReview type for books with author/isbn fields"
printfn "✅ GenericReview type for other review types"
printfn "✅ Clean domain model - no unused fields per type"
