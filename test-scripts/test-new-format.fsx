// Test script to validate new review format parsing
#r "../bin/Debug/net9.0/PersonalSite.dll"

open System
open System.IO
open Domain
open GenericBuilder

// Create test file with new format
let testContent = """---
title: "Test Book Review"
post_type: "review"
published_date: "01/01/2025 10:00 -05:00"
tags: ["test", "books"]
---

# Test Book Review

:::review
item: "Test Book: A Novel"
itemType: "book"
author: "Test Author"
isbn: "9781234567890"
cover: "https://example.com/cover.jpg"
rating: 4.5
scale: 5.0
summary: "This is a test book review."
itemUrl: "https://example.com/book"
imageUrl: "https://example.com/book-cover.jpg"
datePublished: "01/01/2025 10:00 -05:00"
:::

## Review Content

This is the main review content.
"""

printfn "=== Testing New Review Format ==="
printfn ""

// Write test file
let testFilePath = "/tmp/test-review-new-format.md"
File.WriteAllText(testFilePath, testContent)

// Test parsing with BookProcessor
let processor = BookProcessor.create()
match processor.Parse testFilePath with
| Some book ->
    printfn "✅ Parsing successful!"
    printfn ""
    printfn "Book Metadata (populated from review block):"
    printfn "  Title: %s" book.Metadata.Title
    printfn "  Author: %s" book.Metadata.Author
    printfn "  ISBN: %s" book.Metadata.Isbn
    printfn "  Cover: %s" book.Metadata.Cover
    printfn "  Rating: %.1f" book.Metadata.Rating
    printfn "  Date: %s" book.Metadata.DatePublished
    printfn ""
    
    // Verify the data came from review block
    // Note: Title comes from frontmatter, other fields from review block
    if book.Metadata.Title = "Test Book Review" &&
       book.Metadata.Author = "Test Author" &&
       book.Metadata.Isbn = "9781234567890" &&
       book.Metadata.Rating = 4.5 then
        printfn "✅ All fields correctly extracted!"
        printfn "   (Title from frontmatter, metadata from review block)"
    else
        printfn "⚠️ Some fields may not have been extracted correctly"
        
| None ->
    printfn "❌ Parsing failed!"

// Clean up
File.Delete(testFilePath)

printfn ""
printfn "=== Test Existing Format (Backward Compatibility) ==="
printfn ""

let existingFile = "/home/runner/work/luisquintanilla.me/luisquintanilla.me/_src/reviews/library/project-hail-mary.md"
match processor.Parse existingFile with
| Some book ->
    printfn "✅ Existing format still works!"
    printfn "  Title: %s" book.Metadata.Title
    printfn "  Author: %s" book.Metadata.Author
    printfn "  Rating: %.1f" book.Metadata.Rating
| None ->
    printfn "❌ Existing format parsing failed!"

printfn ""
printfn "=== Test Complete ==="
