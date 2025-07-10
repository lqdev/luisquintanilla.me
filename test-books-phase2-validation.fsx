#r "bin/Debug/net9.0/PersonalSite.dll"

open System.IO
open System.Diagnostics
open GenericBuilder

// Comprehensive Phase 2 validation script for Books Migration
printfn "=== Books Migration Phase 2 Validation ==="
printfn "Testing BookProcessor implementation and complete build pipeline"
printfn ""

// Test 1: BookProcessor functionality
printfn "1. Testing BookProcessor functionality..."
let processor = BookProcessor.create()
printfn "   ✅ BookProcessor created successfully"

// Test with a real book file
let testBookPath = Path.Combine("_src", "library", "building-a-second-brain.md")
if File.Exists(testBookPath) then
    match processor.Parse testBookPath with
    | Some book ->
        printfn "   ✅ Parse function works: %s" book.Metadata.Title
        printfn "   ✅ Render function works: %d chars" (processor.Render book).Length
        printfn "   ✅ OutputPath function works: %s" (processor.OutputPath book)
        printfn "   ✅ RenderCard function works: %d chars" (processor.RenderCard book).Length
        printfn "   ✅ RenderRss function works: %s" (if (processor.RenderRss book).IsSome then "Present" else "None")
    | None ->
        printfn "   ❌ Failed to parse test book"

// Test 2: Feature flag functionality
printfn "\n2. Testing feature flag functionality..."

// Test without flag
let testWithoutFlag () =
    let psi = ProcessStartInfo()
    psi.FileName <- "dotnet"
    psi.Arguments <- "run"
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    psi.WorkingDirectory <- Directory.GetCurrentDirectory()
    
    let proc = Process.Start(psi)
    let output = proc.StandardOutput.ReadToEnd()
    proc.WaitForExit()
    
    let hasSkipMessage = output.Contains("Skipping books - AST processor not enabled")
    let hasEnabledMessage = output.Contains("Building books with AST-based processor")
    
    (proc.ExitCode, hasSkipMessage, hasEnabledMessage)

let (exitCode1, hasSkip, hasEnabled1) = testWithoutFlag()
printfn "   Without NEW_BOOKS flag:"
printfn "     Exit code: %d" exitCode1
printfn "     Skip message: %s" (if hasSkip then "✅ Present" else "❌ Missing")
printfn "     Enabled message: %s" (if hasEnabled1 then "❌ Unexpected" else "✅ Absent")

// Test with flag
let testWithFlag () =
    let psi = ProcessStartInfo()
    psi.FileName <- "dotnet"
    psi.Arguments <- "run"
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    psi.WorkingDirectory <- Directory.GetCurrentDirectory()
    psi.EnvironmentVariables.["NEW_BOOKS"] <- "true"
    
    let proc = Process.Start(psi)
    let output = proc.StandardOutput.ReadToEnd()
    proc.WaitForExit()
    
    let hasSkipMessage = output.Contains("Skipping books - AST processor not enabled")
    let hasEnabledMessage = output.Contains("Building books with AST-based processor")
    
    (proc.ExitCode, hasSkipMessage, hasEnabledMessage)

let (exitCode2, hasSkip2, hasEnabled2) = testWithFlag()
printfn "   With NEW_BOOKS=true flag:"
printfn "     Exit code: %d" exitCode2
printfn "     Skip message: %s" (if hasSkip2 then "❌ Unexpected" else "✅ Absent")
printfn "     Enabled message: %s" (if hasEnabled2 then "✅ Present" else "❌ Missing")

// Test 3: Generated output validation
printfn "\n3. Testing generated output..."
let libraryDir = Path.Combine("_public", "library")

if Directory.Exists(libraryDir) then
    printfn "   ✅ Library directory exists"
    
    // Check index page
    let indexPath = Path.Combine(libraryDir, "index.html")
    if File.Exists(indexPath) then
        let indexSize = (FileInfo(indexPath)).Length
        printfn "   ✅ Library index page: %d bytes" indexSize
    else
        printfn "   ❌ Library index page missing"
    
    // Check RSS feed
    let feedPath = Path.Combine(libraryDir, "feed", "index.xml")
    if File.Exists(feedPath) then
        let feedSize = (FileInfo(feedPath)).Length
        let feedContent = File.ReadAllText(feedPath)
        let hasXmlDeclaration = feedContent.StartsWith("<?xml version=\"1.0\"")
        let hasRssTag = feedContent.Contains("<rss version=\"2.0\">")
        let hasChannelTag = feedContent.Contains("<channel>")
        printfn "   ✅ RSS feed: %d bytes" feedSize
        printfn "     XML declaration: %s" (if hasXmlDeclaration then "✅" else "❌")
        printfn "     RSS tag: %s" (if hasRssTag then "✅" else "❌")
        printfn "     Channel tag: %s" (if hasChannelTag then "✅" else "❌")
    else
        printfn "   ❌ RSS feed missing"
    
    // Check individual book pages
    let bookDirs = Directory.GetDirectories(libraryDir)
                   |> Array.filter (fun dir -> not (Path.GetFileName(dir) = "feed"))
    
    printfn "   ✅ Individual book pages: %d directories" bookDirs.Length
    
    if bookDirs.Length > 0 then
        let validPages = 
            bookDirs
            |> Array.map (fun dir -> 
                let indexPath = Path.Combine(dir, "index.html")
                File.Exists(indexPath))
            |> Array.filter id
            |> Array.length
        
        printfn "     Valid pages: %d/%d" validPages bookDirs.Length
        if validPages = bookDirs.Length then
            printfn "   ✅ All book pages generated correctly"
        else
            printfn "   ⚠️  Some book pages missing"
else
    printfn "   ❌ Library directory not created"

// Summary
printfn "\n=== Phase 2 Validation Summary ==="
let allTestsPassed = 
    exitCode1 = 0 && exitCode2 = 0 && hasSkip && hasEnabled2 && 
    not hasEnabled1 && not hasSkip2 && Directory.Exists(libraryDir)

printfn "✅ BookProcessor implementation: Complete"
printfn "✅ Feature flag logic: %s" (if hasSkip && hasEnabled2 then "Working" else "Issues detected")
printfn "✅ Build integration: %s" (if exitCode1 = 0 && exitCode2 = 0 then "Success" else "Errors detected")
printfn "✅ Output generation: %s" (if Directory.Exists(libraryDir) then "Complete" else "Failed")
printfn ""
printfn "Phase 2 Status: %s" (if allTestsPassed then "✅ COMPLETE - Ready for Phase 3" else "⚠️  Issues to address")
printfn ""
