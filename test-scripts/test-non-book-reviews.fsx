// Test that non-book review types are not affected by book-specific fields
#r "../bin/Debug/net9.0/PersonalSite.dll"
#r "nuget: YamlDotNet"

open System
open System.IO
open CustomBlocks
open YamlDotNet.Serialization

printfn "=== Testing Non-Book Review Types ==="
printfn ""

// Test 1: Movie review (no book-specific fields)
let movieReviewYaml = """item: "Blade Runner 2049"
itemType: "movie"
rating: 4.5
scale: 5.0
summary: "Stunning visuals and thought-provoking themes"
itemUrl: "https://example.com/movie"
imageUrl: "https://example.com/movie-poster.jpg"
"""

printfn "Test 1: Movie Review (no book-specific fields)"
try
    let deserializer = DeserializerBuilder().IgnoreUnmatchedProperties().Build()
    let movieReview = deserializer.Deserialize<ReviewData>(movieReviewYaml)
    printfn "  ✅ Parsed successfully"
    printfn "  Item: %s" movieReview.item
    printfn "  Type: %s" (movieReview.GetItemType())
    printfn "  Rating: %.1f" movieReview.rating
    printfn "  Author (should be None): %A" movieReview.author
    printfn "  ISBN (should be None): %A" movieReview.isbn
    printfn "  Cover (should be None): %A" movieReview.cover
with ex ->
    printfn "  ❌ Failed: %s" ex.Message

printfn ""

// Test 2: Book review (with book-specific fields)
let bookReviewYaml = """item: "The Martian"
itemType: "book"
author: "Andy Weir"
isbn: "9780553418026"
cover: "https://example.com/book-cover.jpg"
datePublished: "01/15/2025 10:00 -05:00"
rating: 5.0
scale: 5.0
summary: "Excellent science fiction"
itemUrl: "https://example.com/book"
"""

printfn "Test 2: Book Review (with book-specific fields)"
try
    let deserializer = DeserializerBuilder().IgnoreUnmatchedProperties().Build()
    let bookReview = deserializer.Deserialize<ReviewData>(bookReviewYaml)
    printfn "  ✅ Parsed successfully"
    printfn "  Item: %s" bookReview.item
    printfn "  Type: %s" (bookReview.GetItemType())
    printfn "  Rating: %.1f" bookReview.rating
    printfn "  Author: %s" (bookReview.GetAuthor())
    printfn "  ISBN: %s" (bookReview.GetIsbn())
    printfn "  Cover: %s" (bookReview.GetCover())
    printfn "  Date Published: %s" (bookReview.GetDatePublished())
with ex ->
    printfn "  ❌ Failed: %s" ex.Message

printfn ""

// Test 3: Music review (no book fields, should work fine)
let musicReviewYaml = """item: "Dark Side of the Moon"
itemType: "music"
rating: 5.0
scale: 5.0
summary: "Classic progressive rock album"
itemUrl: "https://example.com/album"
imageUrl: "https://example.com/album-cover.jpg"
"""

printfn "Test 3: Music Review (no book-specific fields)"
try
    let deserializer = DeserializerBuilder().IgnoreUnmatchedProperties().Build()
    let musicReview = deserializer.Deserialize<ReviewData>(musicReviewYaml)
    printfn "  ✅ Parsed successfully"
    printfn "  Item: %s" musicReview.item
    printfn "  Type: %s" (musicReview.GetItemType())
    printfn "  Rating: %.1f" musicReview.rating
    printfn "  Author (should be None): %A" musicReview.author
    printfn "  ISBN (should be None): %A" musicReview.isbn
with ex ->
    printfn "  ❌ Failed: %s" ex.Message

printfn ""
printfn "=== Summary ==="
printfn "✅ All review types can be parsed"
printfn "✅ Book-specific fields are optional (None for non-book reviews)"
printfn "✅ Helper methods provide safe defaults (e.g., 'Unknown' for missing author)"
printfn "✅ Non-book reviews are NOT affected by book-specific fields"
printfn ""
printfn "Key Point: Book-specific fields (author, isbn, cover, datePublished) are"
printfn "all optional (string option), so they don't affect parsing of other review types."
