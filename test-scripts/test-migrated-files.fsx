// Test script to validate migrated book review files
#r "../bin/Debug/net9.0/PersonalSite.dll"

open System.IO
open GenericBuilder

printfn "=== Testing Migrated Book Review Files ==="
printfn ""

let reviewsDir = "_src/reviews/library"
let reviewFiles = Directory.GetFiles(reviewsDir, "*.md")

printfn "Testing %d book review files..." reviewFiles.Length
printfn ""

let processor = BookProcessor.create()
let mutable successCount = 0
let mutable errorCount = 0

for file in reviewFiles do
    let fileName = Path.GetFileName(file)
    match processor.Parse file with
    | Some book ->
        printfn "✅ %s" fileName
        printfn "   Title: %s" book.Metadata.Title
        printfn "   Author: %s" book.Metadata.Author
        printfn "   ISBN: %s" book.Metadata.Isbn
        printfn "   Rating: %.1f" book.Metadata.Rating
        successCount <- successCount + 1
    | None ->
        printfn "❌ %s - Failed to parse" fileName
        errorCount <- errorCount + 1
    printfn ""

printfn "=== Summary ==="
printfn "Total files: %d" reviewFiles.Length
printfn "Successful: %d" successCount
printfn "Errors: %d" errorCount

if errorCount = 0 then
    printfn "✅ All files migrated and parsing correctly!"
else
    printfn "⚠️  Some files have issues"
    exit 1
