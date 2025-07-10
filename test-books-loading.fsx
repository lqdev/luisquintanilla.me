#r "bin/Debug/net9.0/PersonalSite.dll"

open Domain
open Loaders

// Test actual book loading with DatePublished field
printfn "=== Testing Actual Book Loading ==="

// Load actual books to test the DatePublished field
let srcDir = "_src"
let loadedBooks = loadBooks srcDir

printfn "Total books loaded: %d" loadedBooks.Length

// Test books with and without DatePublished
let booksWithDate = 
    loadedBooks 
    |> Array.filter (fun book -> not (System.String.IsNullOrEmpty(book.Metadata.DatePublished)))

let booksWithoutDate = 
    loadedBooks 
    |> Array.filter (fun book -> System.String.IsNullOrEmpty(book.Metadata.DatePublished))

printfn "Books with DatePublished: %d" booksWithDate.Length
printfn "Books without DatePublished: %d" booksWithoutDate.Length

// Show examples of books with dates
printfn "\n=== Books with DatePublished ==="
booksWithDate 
|> Array.take (System.Math.Min(3, booksWithDate.Length))
|> Array.iter (fun book -> 
    printfn "- %s (Date: %s)" book.Metadata.Title book.Metadata.DatePublished)

// Show examples of books without dates
printfn "\n=== Books without DatePublished ==="
booksWithoutDate 
|> Array.take (System.Math.Min(3, booksWithoutDate.Length))
|> Array.iter (fun book -> 
    printfn "- %s (Date: '%s')" book.Metadata.Title book.Metadata.DatePublished)

// Test ITaggable with loaded books
printfn "\n=== Testing ITaggable with Loaded Books ==="
if loadedBooks.Length > 0 then
    let firstBook = loadedBooks.[0]
    let taggedBook = firstBook :> ITaggable
    printfn "First book as ITaggable:"
    printfn "  Title: %s" taggedBook.Title
    printfn "  Date: '%s'" taggedBook.Date
    printfn "  FileName: %s" taggedBook.FileName
    printfn "  ContentType: %s" taggedBook.ContentType
    printfn "  Tags: %A" taggedBook.Tags

printfn "\n=== Test Complete ==="
