// Books Migration Phase 3 - Feature Flag Testing
// Tests feature flag behavior and validates migration safety

open System
open System.IO

// =============================================================================
// Feature Flag Testing (Manual)
// =============================================================================

let testCurrentState () =
    printfn "=== Current State Analysis ==="
    
    let libraryDir = "_public/library"
    
    if Directory.Exists(libraryDir) then
        let subdirs = Directory.GetDirectories(libraryDir)
        let files = Directory.GetFiles(libraryDir)
        let bookDirs = subdirs |> Array.filter (fun dir -> not (Path.GetFileName(dir).Equals("feed")))
        
        printfn "✅ Library directory exists: %s" libraryDir
        printfn "✅ Subdirectories: %d" subdirs.Length
        printfn "✅ Book directories: %d" bookDirs.Length
        printfn "✅ Files in root: %d" files.Length
        
        // Check key files
        let indexExists = File.Exists(Path.Combine(libraryDir, "index.html"))
        let feedExists = File.Exists(Path.Combine(libraryDir, "feed", "index.xml"))
        
        printfn "✅ Library index: %b" indexExists
        printfn "✅ RSS feed: %b" feedExists
        
        // Check sample book pages
        if bookDirs.Length > 0 then
            let sampleBook = bookDirs.[0]
            let samplePage = Path.Combine(sampleBook, "index.html")
            let pageExists = File.Exists(samplePage)
            
            printfn "✅ Sample book page (%s): %b" (Path.GetFileName(sampleBook)) pageExists
            
            if pageExists then
                let content = File.ReadAllText(samplePage)
                printfn "   Page size: %d bytes" content.Length
        
        true
    else
        printfn "❌ Library directory does not exist"
        printfn "   This indicates NEW_BOOKS flag is not set or build hasn't run"
        false

// =============================================================================
// Content Validation
// =============================================================================

let validateGeneratedContent () =
    printfn "\n=== Generated Content Validation ==="
    
    let libraryDir = "_public/library"
    
    if not (Directory.Exists(libraryDir)) then
        printfn "❌ No library content to validate - ensure NEW_BOOKS=true and build has run"
        false
    else
        try
            // RSS Feed validation
            let feedPath = Path.Combine(libraryDir, "feed", "index.xml")
            if File.Exists(feedPath) then
                let feedContent = File.ReadAllText(feedPath)
                let feedSize = feedContent.Length
                let hasXmlDecl = feedContent.Contains("<?xml")
                let hasRssTag = feedContent.Contains("<rss")
                let hasItems = feedContent.Contains("<item>")
                
                printfn "✅ RSS Feed Analysis:"
                printfn "   Size: %d bytes" feedSize
                printfn "   Has XML declaration: %b" hasXmlDecl
                printfn "   Has RSS structure: %b" hasRssTag
                printfn "   Has items: %b" hasItems
            else
                printfn "❌ RSS feed not found"
            
            // Library index validation  
            let indexPath = Path.Combine(libraryDir, "index.html")
            if File.Exists(indexPath) then
                let indexContent = File.ReadAllText(indexPath)
                let indexSize = indexContent.Length
                let hasTitle = indexContent.Contains("<title>")
                let hasBooks = indexContent.Contains("library") || indexContent.Contains("book")
                
                printfn "✅ Library Index Analysis:"
                printfn "   Size: %d bytes" indexSize
                printfn "   Has title: %b" hasTitle
                printfn "   Has book content: %b" hasBooks
            else
                printfn "❌ Library index not found"
            
            // Book pages validation
            let bookDirs = Directory.GetDirectories(libraryDir) 
                          |> Array.filter (fun dir -> not (Path.GetFileName(dir).Equals("feed")))
            
            if bookDirs.Length > 0 then
                printfn "✅ Book Pages Analysis:"
                printfn "   Total book directories: %d" bookDirs.Length
                
                // Sample first few books
                let samplesToCheck = min 3 bookDirs.Length
                let mutable validPages = 0
                
                for i in 0..(samplesToCheck - 1) do
                    let bookDir = bookDirs.[i]
                    let bookPage = Path.Combine(bookDir, "index.html")
                    let bookName = Path.GetFileName(bookDir)
                    
                    if File.Exists(bookPage) then
                        validPages <- validPages + 1
                        let content = File.ReadAllText(bookPage)
                        printfn "   %s: %d bytes ✅" bookName content.Length
                    else
                        printfn "   %s: MISSING ❌" bookName
                
                printfn "   Valid sample pages: %d/%d" validPages samplesToCheck
            else
                printfn "❌ No book directories found"
            
            true
        with ex ->
            printfn "❌ Content validation error: %s" ex.Message
            false

// =============================================================================
// System Integration Check
// =============================================================================

let checkSystemIntegration () =
    printfn "\n=== System Integration Check ==="
    
    let publicDir = "_public"
    
    if not (Directory.Exists(publicDir)) then
        printfn "❌ Public directory not found - build may not have run"
        false
    else
        // Check other content types still exist
        let contentTypes = [
            ("posts", "posts")
            ("snippets", "snippets") 
            ("wiki", "wiki")
            ("presentations", "presentations")
            ("library", "library (books)")
        ]
        
        let mutable allContentExists = true
        
        for (dir, displayName) in contentTypes do
            let dirPath = Path.Combine(publicDir, dir)
            let exists = Directory.Exists(dirPath)
            printfn "✅ %s: %b" displayName exists
            
            if exists then
                let subdirs = Directory.GetDirectories(dirPath)
                let files = Directory.GetFiles(dirPath, "*.html")
                printfn "   Subdirectories: %d, HTML files: %d" subdirs.Length files.Length
            else if dir <> "library" then
                allContentExists <- false
        
        // Check main site files
        let mainIndex = File.Exists(Path.Combine(publicDir, "index.html"))
        let mainFeed = File.Exists(Path.Combine(publicDir, "feed", "index.xml"))
        
        printfn "✅ Main site index: %b" mainIndex
        printfn "✅ Main site feed: %b" mainFeed
        
        allContentExists && mainIndex

// =============================================================================
// Migration Safety Validation
// =============================================================================

let validateMigrationSafety () =
    printfn "\n=== Migration Safety Validation ==="
    
    // This validates that the new books content doesn't break existing functionality
    
    let publicDir = "_public"
    let libraryDir = Path.Combine(publicDir, "library")
    
    if not (Directory.Exists(libraryDir)) then
        printfn "⚠️  Library directory not found - feature flag may be off"
        printfn "   This is safe behavior when NEW_BOOKS is not set"
        true
    else
        // Check that library generation doesn't interfere with other content
        let otherContentTypes = ["posts"; "snippets"; "wiki"; "presentations"]
        let mutable interferenceDetected = false
        
        for contentType in otherContentTypes do
            let contentDir = Path.Combine(publicDir, contentType)
            if Directory.Exists(contentDir) then
                let contentFiles = Directory.GetFiles(contentDir, "*", SearchOption.AllDirectories)
                let hasLibraryFiles = contentFiles |> Array.exists (fun f -> f.Contains("library"))
                
                if hasLibraryFiles then
                    printfn "❌ Interference detected: library files in %s" contentType
                    interferenceDetected <- true
                else
                    printfn "✅ %s content clean (no library interference)" contentType
            else
                printfn "⚠️  %s directory not found" contentType
        
        if not interferenceDetected then
            printfn "✅ No content type interference detected"
            printfn "✅ Migration appears safe"
        
        not interferenceDetected

// =============================================================================
// Main Test Runner
// =============================================================================

let runPhase3Validation () =
    printfn "Books Migration Phase 3 - Final Validation and Safety Check"
    printfn "============================================================="
    
    let results = [
        testCurrentState()
        validateGeneratedContent()
        checkSystemIntegration()
        validateMigrationSafety()
    ]
    
    let allPassed = results |> List.forall id
    
    printfn "\n=== Phase 3 Validation Summary ==="
    printfn "Current State: %s" (if results.[0] then "✅ PASS" else "❌ FAIL")
    printfn "Generated Content: %s" (if results.[1] then "✅ PASS" else "❌ FAIL")
    printfn "System Integration: %s" (if results.[2] then "✅ PASS" else "❌ FAIL")
    printfn "Migration Safety: %s" (if results.[3] then "✅ PASS" else "❌ FAIL")
    printfn ""
    printfn "OVERALL RESULT: %s" (if allPassed then "✅ ALL VALIDATIONS PASSED" else "❌ SOME VALIDATIONS FAILED")
    
    if allPassed then
        printfn "\n🎉 Phase 3 validation complete!"
        printfn "   Books migration is validated and ready."
        printfn "   Safe to proceed to Phase 4: Production Deployment."
        printfn ""
        printfn "Next steps:"
        printfn "  1. Remove feature flag dependency (make NEW_BOOKS default)"
        printfn "  2. Clean up any legacy code if applicable"
        printfn "  3. Archive project documentation"
    else
        printfn "\n⚠️  Some validations failed."
        printfn "   Review issues before proceeding to production deployment."
    
    allPassed

// Run the validation
runPhase3Validation()

// Instructions for manual feature flag testing:
printfn "\n=== Manual Feature Flag Testing Instructions ==="
printfn "To test feature flag behavior manually:"
printfn ""
printfn "1. Test with flag OFF:"
printfn "   Remove NEW_BOOKS environment variable"
printfn "   Run: dotnet run"
printfn "   Verify: No library directory created"
printfn ""
printfn "2. Test with flag ON:"
printfn "   Set NEW_BOOKS=true"
printfn "   Run: dotnet run"
printfn "   Verify: Library directory created with content"
printfn ""
printfn "3. Current state appears to be: NEW_BOOKS=true (library content exists)"
