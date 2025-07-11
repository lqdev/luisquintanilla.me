#r "../bin/Debug/net9.0/PersonalSite.dll"

open System
open System.IO
open System.Xml

// =============================================================================
// Posts Migration Phase 3 - Integration and Regression Testing
// Validates that new posts processor doesn't break existing functionality
// =============================================================================

printfn "=== Posts Migration Phase 3: Integration & Regression Testing ==="
printfn "Validating that new posts processor doesn't affect other content types..."
printfn ""

// =============================================================================
// Check Other Content Types Still Work
// =============================================================================

let validateOtherContentTypes () =
    printfn "=== Other Content Types Validation ==="
    
    let testCases = [
        ("Snippets", "_public/snippets", "*.html")
        ("Wiki", "_public/wiki", "*.html") 
        ("Presentations", "_public/presentations", "*.html")
        ("Books/Library", "_public/library", "*.html")
        ("Feed", "_public/feed", "*.html")
        ("Responses", "_public/responses", "*.html")
    ]
    
    let mutable allPassed = true
    
    for (contentType, directory, pattern) in testCases do
        if Directory.Exists(directory) then
            let files = Directory.GetFiles(directory, pattern, SearchOption.AllDirectories)
            printfn "✅ %s: %d files found" contentType files.Length
            
            // Sample a few files to check they're not empty
            if files.Length > 0 then
                let sampleFile = files.[0]
                let content = File.ReadAllText(sampleFile)
                if content.Length > 100 then
                    printfn "   Sample file has content (%d chars)" content.Length
                else
                    printfn "   ⚠️  Sample file seems small (%d chars)" content.Length
        else
            // Special case: Responses are loaded but pages not built (commented out in Program.fs)
            if contentType = "Responses" then
                printfn "⚠️  %s: Directory not found (expected - response pages disabled)" contentType
            else
                printfn "❌ %s: Directory not found (%s)" contentType directory
                allPassed <- false
    
    allPassed

// =============================================================================
// Validate RSS Feeds for Other Content Types  
// =============================================================================

let validateOtherRssFeeds () =
    printfn "\n=== Other RSS Feeds Validation ==="
    
    let feedPaths = [
        ("Snippets", "_public/snippets/feed/index.xml")
        ("Wiki", "_public/wiki/feed/index.xml")
        ("Presentations", "_public/presentations/feed/index.xml") 
        ("Books", "_public/library/feed/index.xml")
        ("Posts", "_public/posts/feed/index.xml")
    ]
    
    let mutable allFeedsValid = true
    
    for (contentType, feedPath) in feedPaths do
        if File.Exists(feedPath) then
            try
                let doc = XmlDocument()
                doc.Load(feedPath)
                
                let titleNode = doc.SelectSingleNode("//rss/channel/title")
                let itemNodes = doc.SelectNodes("//rss/channel/item")
                
                if titleNode <> null && itemNodes <> null then
                    printfn "✅ %s RSS: %s (%d items)" contentType titleNode.InnerText itemNodes.Count
                else
                    printfn "❌ %s RSS: Invalid structure" contentType
                    allFeedsValid <- false
                    
            with ex ->
                printfn "❌ %s RSS: Parse error - %s" contentType ex.Message
                allFeedsValid <- false
        else
            // Only warn for content types that should have feeds
            if contentType <> "Posts" then
                printfn "⚠️  %s RSS: Not found (may be expected)" contentType
            else
                printfn "❌ %s RSS: Missing - this should exist!" contentType
                allFeedsValid <- false
    
    allFeedsValid

// =============================================================================
// Validate Main Site Navigation
// =============================================================================

let validateSiteNavigation () =
    printfn "\n=== Site Navigation Validation ==="
    
    let keyPages = [
        ("Home", "_public/index.html")
        ("About", "_public/about/index.html")
        ("Contact", "_public/contact/index.html")
        ("Tags", "_public/tags/index.html")
        ("Posts Index", "_public/posts/1/index.html")  // First page of posts
    ]
    
    let mutable allNavValid = true
    
    for (pageName, pagePath) in keyPages do
        if File.Exists(pagePath) then
            let content = File.ReadAllText(pagePath)
            if content.Contains("<html") && content.Contains("</html>") then
                printfn "✅ %s: Valid HTML structure" pageName
            else
                printfn "❌ %s: Invalid HTML structure" pageName
                allNavValid <- false
        else
            printfn "❌ %s: Page missing (%s)" pageName pagePath
            allNavValid <- false
    
    allNavValid

// =============================================================================
// Check Build Performance/Output Size
// =============================================================================

let validateBuildMetrics () =
    printfn "\n=== Build Metrics Validation ==="
    
    if Directory.Exists("_public") then
        let allFiles = Directory.GetFiles("_public", "*", SearchOption.AllDirectories)
        let htmlFiles = allFiles |> Array.filter (fun f -> f.EndsWith(".html"))
        let xmlFiles = allFiles |> Array.filter (fun f -> f.EndsWith(".xml"))
        let totalSize = allFiles |> Array.sumBy (fun f -> (FileInfo(f)).Length)
        
        printfn "✅ Total files: %d" allFiles.Length
        printfn "✅ HTML files: %d" htmlFiles.Length  
        printfn "✅ XML files: %d" xmlFiles.Length
        printfn "✅ Total size: %.2f MB" (float totalSize / 1024.0 / 1024.0)
        
        // Basic sanity checks
        let sizeOk = totalSize > 1000000L && totalSize < 100000000L  // Between 1MB and 100MB
        let htmlCountOk = htmlFiles.Length > 50 && htmlFiles.Length < 2000  // Allow for tag pages growth
        
        if sizeOk && htmlCountOk then
            printfn "✅ Build metrics look reasonable"
            true
        else
            printfn "❌ Build metrics look unusual (size: %b, html count: %b)" sizeOk htmlCountOk
            false
    else
        printfn "❌ Build output directory not found"
        false

// =============================================================================
// Execute All Integration Tests
// =============================================================================

let runIntegrationTests () =
    let results = [
        ("Other Content Types", validateOtherContentTypes())
        ("RSS Feeds", validateOtherRssFeeds())
        ("Site Navigation", validateSiteNavigation())
        ("Build Metrics", validateBuildMetrics())
    ]
    
    printfn "\n=== Integration Test Results ==="
    let mutable allPassed = true
    
    for (testName, passed) in results do
        let status = if passed then "✅ PASSED" else "❌ FAILED"
        printfn "%s: %s" testName status
        if not passed then allPassed <- false
    
    if allPassed then
        printfn "\n✅ ALL INTEGRATION TESTS PASSED"
        printfn "✅ Posts migration does not break existing functionality"
        printfn "✅ System integration validated"
    else
        printfn "\n❌ SOME INTEGRATION TESTS FAILED"
        printfn "❌ Review failures above before proceeding"
    
    allPassed

// =============================================================================
// Execute Integration Testing
// =============================================================================

let success = runIntegrationTests()
if not success then
    exit 1
else
    printfn "\n✅ Phase 3 integration testing completed successfully"
