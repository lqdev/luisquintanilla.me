#r "../bin/Debug/net10.0/PersonalSite.dll"

open GenericBuilder

let existingFile = "_src/reviews/library/project-hail-mary.md"
let processor = BookProcessor.create()

printfn "Testing with existing book file: %s" existingFile
match processor.Parse existingFile with
| Some book ->
    printfn "✅ Parsed successfully"
    printfn "  Title: %s" book.Metadata.Title
    printfn "  Author: %s" book.Metadata.Author
    printfn "  ISBN: %s" book.Metadata.Isbn
    printfn "  Rating: %.1f" book.Metadata.Rating
| None ->
    printfn "❌ Failed to parse"
