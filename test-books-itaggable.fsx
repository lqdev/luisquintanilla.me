#r "bin/Debug/net9.0/PersonalSite.dll"

open Domain
open Domain.ITaggableHelpers

// Test Book ITaggable implementation
printfn "=== Testing Book ITaggable Implementation ==="

// Test 1: Create a sample book with all fields including date_published
let sampleBookDetails : BookDetails = {
    Title = "Building a Second Brain"
    Author = "Tiago Forte"
    Isbn = "9781982167387"
    Cover = "https://covers.openlibrary.org/b/id/12372866-M.jpg"
    Status = "Read"
    Rating = 4.0
    Source = "https://openlibrary.org/works/OL26417584W/Building_a_Second_Brain"
    DatePublished = "2023-06-14"
}

let sampleBook : Book = {
    FileName = "building-a-second-brain.md"
    Metadata = sampleBookDetails
    Content = "## Review\n\nExcellent book on knowledge management..."
}

// Test 2: Verify ITaggable interface implementation
let bookAsITaggable = sampleBook :> ITaggable

printfn "Book Title: %s" bookAsITaggable.Title
printfn "Book Date: %s" bookAsITaggable.Date
printfn "Book FileName: %s" bookAsITaggable.FileName
printfn "Book ContentType: %s" bookAsITaggable.ContentType
printfn "Book Tags: %A" bookAsITaggable.Tags
printfn "Book Tags Length: %d" bookAsITaggable.Tags.Length

// Test 3: Test helper functions
printfn "\n=== Testing Helper Functions ==="
printfn "getBookTitle: %s" (getBookTitle sampleBook)
printfn "getBookDate: %s" (getBookDate sampleBook)
printfn "getBookFileName: %s" (getBookFileName sampleBook)
printfn "getBookContentType: %s" (getBookContentType sampleBook)
printfn "getBookTags: %A" (getBookTags sampleBook)

// Test 4: Test bookAsTaggable conversion
printfn "\n=== Testing bookAsTaggable Conversion ==="
let convertedBook = bookAsTaggable sampleBook
printfn "Converted Title: %s" convertedBook.Title
printfn "Converted Date: %s" convertedBook.Date
printfn "Converted FileName: %s" convertedBook.FileName
printfn "Converted ContentType: %s" convertedBook.ContentType
printfn "Converted Tags: %A" convertedBook.Tags

// Test 5: Test book without DatePublished (empty string)
let bookWithoutDate : Book = {
    FileName = "a-city-on-mars.md"
    Metadata = { sampleBookDetails with DatePublished = "" }
    Content = "## Description\n\nSpace settlement analysis..."
}

printfn "\n=== Testing Book Without DatePublished ==="
let bookNoDate = bookWithoutDate :> ITaggable
printfn "Book Date (no date): '%s'" bookNoDate.Date
printfn "Book Date is null or empty: %b" (System.String.IsNullOrEmpty(bookNoDate.Date))

printfn "\n=== Test Complete ==="
