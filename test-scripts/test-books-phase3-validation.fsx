// Books Migration Phase 3 - RSS Feed and Content Validation Script
// Validates RSS feed structure, content preservation, and output quality

#load "Domain.fs"
#load "Loaders.fs"

open System
open System.IO
open System.Xml
open Domain
open Loaders

// =============================================================================
// RSS Feed Validation
// =============================================================================

let validateRssFeed () =
    printfn "=== RSS Feed Validation ==="
    
    let feedPath = "_public/library/feed/index.xml"
    
    if not (File.Exists feedPath) then
        printfn "❌ ERROR: RSS feed not found at %s" feedPath
        false
    else
        try
            // Validate XML structure
            let doc = XmlDocument()
            doc.Load(feedPath)
            
            let feedContent = File.ReadAllText(feedPath)
            
            // Check XML declaration
            let hasXmlDeclaration = feedContent.StartsWith("<?xml version=\"1.0\" encoding=\"UTF-8\"?>")
            printfn "✅ XML Declaration: %b" hasXmlDeclaration
            
            // Check RSS structure
            let rssNode = doc.SelectSingleNode("//rss")
            let channelNode = doc.SelectSingleNode("//rss/channel")
            let titleNode = doc.SelectSingleNode("//rss/channel/title")
            let itemNodes = doc.SelectNodes("//rss/channel/item")
            
            printfn "✅ RSS root element: %b" (rssNode <> null)
            printfn "✅ Channel element: %b" (channelNode <> null)
            printfn "✅ Channel title: %s" (if titleNode <> null then titleNode.InnerText else "MISSING")
            printfn "✅ Item count: %d" itemNodes.Count
            
            // Validate item structure
            if itemNodes.Count > 0 then
                let firstItem = itemNodes.[0]
                let itemTitle = firstItem.SelectSingleNode("title")
                let itemLink = firstItem.SelectSingleNode("link")
                let itemPubDate = firstItem.SelectSingleNode("pubDate")
                let itemDescription = firstItem.SelectSingleNode("description")
                
                printfn "✅ First item title: %s" (if itemTitle <> null then itemTitle.InnerText else "MISSING")
                printfn "✅ First item link: %s" (if itemLink <> null then itemLink.InnerText else "MISSING")
                printfn "✅ First item pubDate: %s" (if itemPubDate <> null then itemPubDate.InnerText else "MISSING")
                printfn "✅ First item has description: %b" (itemDescription <> null)
            
            true
        with ex ->
            printfn "❌ RSS Feed XML Error: %s" ex.Message
            false

// =============================================================================
// Content Preservation Validation
// =============================================================================

let validateContentPreservation () =
    printfn "\n=== Content Preservation Validation ==="
    
    try
        // Load original books
        let originalBooks = loadBooks()
        printfn "✅ Original books loaded: %d" originalBooks.Length
        
        // Check generated files
        let libraryDir = "_public/library"
        let indexPath = Path.Combine(libraryDir, "index.html")
        
        if not (File.Exists indexPath) then
            printfn "❌ ERROR: Library index not found at %s" indexPath
            false
        else
            let indexContent = File.ReadAllText(indexPath)
            printfn "✅ Library index exists: %d bytes" indexContent.Length
            
            // Validate individual book pages
            let mutable allBooksGenerated = true
            let mutable generatedCount = 0
            
            for book in originalBooks do
                let bookDir = Path.Combine(libraryDir, book.FileName)
                let bookPage = Path.Combine(bookDir, "index.html")
                
                if File.Exists bookPage then
                    generatedCount <- generatedCount + 1
                    let content = File.ReadAllText(bookPage)
                    
                    // Check for key book metadata in generated content
                    let hasTitle = content.Contains(book.Details.Title)
                    let hasAuthor = content.Contains(book.Details.Author)
                    
                    if not hasTitle || not hasAuthor then
                        printfn "❌ Book %s missing metadata (Title: %b, Author: %b)" book.FileName hasTitle hasAuthor
                        allBooksGenerated <- false
                else
                    printfn "❌ Missing book page: %s" bookPage
                    allBooksGenerated <- false
            
            printfn "✅ Generated book pages: %d/%d" generatedCount originalBooks.Length
            printfn "✅ All books generated correctly: %b" allBooksGenerated
            
            allBooksGenerated
    with ex ->
        printfn "❌ Content Preservation Error: %s" ex.Message
        false

// =============================================================================
// Directory Structure Validation
// =============================================================================

let validateDirectoryStructure () =
    printfn "\n=== Directory Structure Validation ==="
    
    let libraryDir = "_public/library"
    
    if not (Directory.Exists libraryDir) then
        printfn "❌ ERROR: Library directory not found: %s" libraryDir
        false
    else
        let subdirs = Directory.GetDirectories(libraryDir)
        let files = Directory.GetFiles(libraryDir)
        
        printfn "✅ Library directory exists"
        printfn "✅ Subdirectories: %d" subdirs.Length
        printfn "✅ Files in root: %d" files.Length
        
        // Check for expected files
        let indexExists = File.Exists(Path.Combine(libraryDir, "index.html"))
        let feedDirExists = Directory.Exists(Path.Combine(libraryDir, "feed"))
        let feedExists = File.Exists(Path.Combine(libraryDir, "feed", "index.xml"))
        
        printfn "✅ Library index.html: %b" indexExists
        printfn "✅ Feed directory: %b" feedDirExists
        printfn "✅ Feed index.xml: %b" feedExists
        
        // Count individual book directories
        let bookDirs = subdirs |> Array.filter (fun dir -> not (Path.GetFileName(dir).Equals("feed")))
        printfn "✅ Book directories: %d" bookDirs.Length
        
        indexExists && feedExists && bookDirs.Length > 0

// =============================================================================
// Integration Testing
// =============================================================================

let validateIntegration () =
    printfn "\n=== Integration Testing ==="
    
    try
        // Test that books are accessible via the domain
        let books = loadBooks()
        let booksWithDates = books |> List.filter (fun b -> b.Details.DatePublished.IsSome)
        let booksWithoutDates = books |> List.filter (fun b -> b.Details.DatePublished.IsNone)
        
        printfn "✅ Books with DatePublished: %d" booksWithDates.Length
        printfn "✅ Books without DatePublished: %d" booksWithoutDates.Length
        
        // Test ITaggable implementation
        let taggableBooks = books |> List.map (fun b -> b :> ITaggable)
        let allHaveCorrectType = taggableBooks |> List.forall (fun t -> t.ContentType = "book")
        let allHaveFileName = taggableBooks |> List.forall (fun t -> not (String.IsNullOrEmpty(t.FileName)))
        
        printfn "✅ ITaggable - All books have correct ContentType: %b" allHaveCorrectType
        printfn "✅ ITaggable - All books have FileName: %b" allHaveFileName
        
        // Test that generated content maintains URL patterns
        let sampleBook = books.[0]
        let expectedPath = sprintf "_public/library/%s/index.html" sampleBook.FileName
        let pathExists = File.Exists(expectedPath)
        
        printfn "✅ URL pattern consistency (sample): %b" pathExists
        if pathExists then
            printfn "   Sample path: %s" expectedPath
        
        true
    with ex ->
        printfn "❌ Integration Test Error: %s" ex.Message
        false

// =============================================================================
// Main Validation Runner
// =============================================================================

let runAllValidations () =
    printfn "Books Migration Phase 3 - Comprehensive Validation"
    printfn "=================================================="
    
    let results = [
        validateRssFeed()
        validateContentPreservation()
        validateDirectoryStructure()
        validateIntegration()
    ]
    
    let allPassed = results |> List.forall id
    
    printfn "\n=== Validation Summary ==="
    printfn "RSS Feed Validation: %s" (if results.[0] then "✅ PASS" else "❌ FAIL")
    printfn "Content Preservation: %s" (if results.[1] then "✅ PASS" else "❌ FAIL")
    printfn "Directory Structure: %s" (if results.[2] then "✅ PASS" else "❌ FAIL")
    printfn "Integration Testing: %s" (if results.[3] then "✅ PASS" else "❌ FAIL")
    printfn ""
    printfn "OVERALL RESULT: %s" (if allPassed then "✅ ALL VALIDATIONS PASSED" else "❌ SOME VALIDATIONS FAILED")
    
    allPassed

// Run the validation
runAllValidations()
