#r "../bin/Debug/net10.0/PersonalSite.dll"

open GenericBuilder

let testFile = "_src/reviews/library/test-novel-2025.md"
let processor = BookProcessor.create()

match processor.Parse testFile with
| Some book ->
    printfn "✅ Parsing successful!"
    printfn "Title: %s" book.Metadata.Title
    printfn "Author: %s" book.Metadata.Author
    printfn "ISBN: %s" book.Metadata.Isbn
    printfn "Cover: %s" book.Metadata.Cover
    printfn "Rating: %.1f" book.Metadata.Rating
    printfn "Date: %s" book.Metadata.DatePublished
| None ->
    printfn "❌ Parsing failed!"
