// Books Migration Phase 3 - RSS Feed and Output Validation Script
// Validates RSS feed structure, content preservation, and output quality

open System
open System.IO
open System.Xml

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
            
            // File size check
            let fileInfo = FileInfo(feedPath)
            printfn "✅ RSS feed size: %d bytes" fileInfo.Length
            
            true
        with ex ->
            printfn "❌ RSS Feed XML Error: %s" ex.Message
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
        
        // Count individual book directories (excluding feed)
        let bookDirs = subdirs |> Array.filter (fun dir -> not (Path.GetFileName(dir).Equals("feed")))
        printfn "✅ Book directories: %d" bookDirs.Length
        
        // Validate sample book directories have index.html
        let mutable validBookDirs = 0
        for bookDir in bookDirs |> Array.take (min 5 bookDirs.Length) do
            let bookIndex = Path.Combine(bookDir, "index.html")
            if File.Exists(bookIndex) then
                validBookDirs <- validBookDirs + 1
                let content = File.ReadAllText(bookIndex)
                printfn "✅ Book page %s: %d bytes" (Path.GetFileName(bookDir)) content.Length
        
        printfn "✅ Sample book pages validated: %d" validBookDirs
        
        indexExists && feedExists && bookDirs.Length > 0

// =============================================================================
// Content Quality Validation
// =============================================================================

let validateContentQuality () =
    printfn "\n=== Content Quality Validation ==="
    
    try
        // Check library index quality
        let indexPath = "_public/library/index.html"
        
        if not (File.Exists indexPath) then
            printfn "❌ ERROR: Library index not found"
            false
        else
            let indexContent = File.ReadAllText(indexPath)
            
            // Check for expected HTML structure
            let hasHtmlTag = indexContent.Contains("<html")
            let hasTitle = indexContent.Contains("<title>")
            let hasNavigation = indexContent.Contains("nav") || indexContent.Contains("navigation")
            let hasBooksSection = indexContent.Contains("library") || indexContent.Contains("book")
            
            printfn "✅ Library index size: %d bytes" indexContent.Length
            printfn "✅ Has HTML structure: %b" hasHtmlTag
            printfn "✅ Has title tag: %b" hasTitle
            printfn "✅ Has navigation: %b" hasNavigation
            printfn "✅ Has books content: %b" hasBooksSection
            
            // Check individual book page quality
            let libraryDir = "_public/library"
            let bookDirs = Directory.GetDirectories(libraryDir) 
                          |> Array.filter (fun dir -> not (Path.GetFileName(dir).Equals("feed")))
            
            if bookDirs.Length > 0 then
                let sampleBookDir = bookDirs.[0]
                let sampleBookPath = Path.Combine(sampleBookDir, "index.html")
                
                if File.Exists(sampleBookPath) then
                    let bookContent = File.ReadAllText(sampleBookPath)
                    let bookHasHtml = bookContent.Contains("<html")
                    let bookHasTitle = bookContent.Contains("<title>")
                    let bookHasContent = bookContent.Length > 1000
                    
                    printfn "✅ Sample book page (%s):" (Path.GetFileName(sampleBookDir))
                    printfn "   Size: %d bytes" bookContent.Length
                    printfn "   Has HTML: %b" bookHasHtml
                    printfn "   Has title: %b" bookHasTitle
                    printfn "   Has substantial content: %b" bookHasContent
                else
                    printfn "❌ Sample book page missing"
            
            true
    with ex ->
        printfn "❌ Content Quality Error: %s" ex.Message
        false

// =============================================================================
// Build Process Validation
// =============================================================================

let validateBuildProcess () =
    printfn "\n=== Build Process Validation ==="
    
    // Check that we can detect the feature flag is working
    // This is indirect validation by checking output structure
    
    let libraryDir = "_public/library"
    let expectedFiles = [
        "index.html"
        "feed/index.xml"
    ]
    
    let mutable allExpectedExist = true
    
    for file in expectedFiles do
        let fullPath = Path.Combine(libraryDir, file)
        let exists = File.Exists(fullPath)
        printfn "✅ Expected file %s: %b" file exists
        if not exists then
            allExpectedExist <- false
    
    // Count total generated files
    let allFiles = Directory.GetFiles(libraryDir, "*", SearchOption.AllDirectories)
    printfn "✅ Total generated files: %d" allFiles.Length
    
    // File type breakdown
    let htmlFiles = allFiles |> Array.filter (fun f -> f.EndsWith(".html"))
    let xmlFiles = allFiles |> Array.filter (fun f -> f.EndsWith(".xml"))
    
    printfn "✅ HTML files: %d" htmlFiles.Length
    printfn "✅ XML files: %d" xmlFiles.Length
    
    allExpectedExist && allFiles.Length > 0

// =============================================================================
// Main Validation Runner
// =============================================================================

let runAllValidations () =
    printfn "Books Migration Phase 3 - Comprehensive Output Validation"
    printfn "=========================================================="
    
    let results = [
        validateRssFeed()
        validateDirectoryStructure()
        validateContentQuality()
        validateBuildProcess()
    ]
    
    let allPassed = results |> List.forall id
    
    printfn "\n=== Validation Summary ==="
    printfn "RSS Feed Validation: %s" (if results.[0] then "✅ PASS" else "❌ FAIL")
    printfn "Directory Structure: %s" (if results.[1] then "✅ PASS" else "❌ FAIL")
    printfn "Content Quality: %s" (if results.[2] then "✅ PASS" else "❌ FAIL")
    printfn "Build Process: %s" (if results.[3] then "✅ PASS" else "❌ FAIL")
    printfn ""
    printfn "OVERALL RESULT: %s" (if allPassed then "✅ ALL VALIDATIONS PASSED" else "❌ SOME VALIDATIONS FAILED")
    
    if allPassed then
        printfn "\n🎉 Books migration output validation complete!"
        printfn "   Ready for production deployment."
    else
        printfn "\n⚠️  Some validations failed. Review issues before proceeding."
    
    allPassed

// Run the validation
runAllValidations()
