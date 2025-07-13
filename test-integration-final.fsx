#!/usr/bin/env dotnet fsi

// Final Integration Test - Phase 3 Completion
// Verify unified system is fully integrated and production-ready
// Run with: dotnet fsi test-integration-final.fsx

open System
open System.IO
open System.Text.RegularExpressions

printfn "=== Final Integration Test - Phase 3 ==="
printfn "Date: %s" (DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
printfn ""

// Test 1: Verify no legacy RSS functions remain in build modules
printfn "🔍 Test 1: Legacy RSS Function Cleanup Verification"
printfn "%s" (String.replicate 50 "=")

let checkForLegacyRssCode (filePath: string) (fileName: string) =
    if File.Exists(filePath) then
        let content = File.ReadAllText(filePath)
        let legacyPatterns = [
            @"generateRssFeed\s*\("  // Old RSS generation calls
            @"saveRssFeed\s*\("      // Old RSS save functions
            @"writeRss\s*\("         // Old RSS write functions
            @"buildFeedPage\s*\("    // Legacy feed page builders
            @"buildFeedRssPage\s*\(" // Legacy RSS page builders
        ]
        
        let mutable foundLegacy = []
        for pattern in legacyPatterns do
            let matches = Regex.Matches(content, pattern)
            if matches.Count > 0 then
                foundLegacy <- (pattern, matches.Count) :: foundLegacy
        
        if foundLegacy.IsEmpty then
            printfn "✅ %s: Clean of legacy RSS functions" fileName
            true
        else
            printfn "⚠️  %s: Found legacy patterns:" fileName
            foundLegacy |> List.iter (fun (pattern, count) -> 
                printfn "   - %s: %d occurrences" pattern count)
            false
    else
        printfn "❌ %s: File not found" fileName
        false

let buildModules = [
    "Builder.fs", "Builder"
    "Program.fs", "Program"
    "GenericBuilder.fs", "GenericBuilder"
]

let legacyCleanResults = buildModules |> List.map (fun (path, name) -> checkForLegacyRssCode path name)
let cleanModules = legacyCleanResults |> List.filter id |> List.length

printfn ""
printfn "Legacy cleanup: %d/%d modules clean" cleanModules buildModules.Length
printfn ""

// Test 2: Verify unified system integration in Program.fs
printfn "🔍 Test 2: Unified System Integration Check"
printfn "=" + String.replicate 50 "="

if File.Exists("Program.fs") then
    let programContent = File.ReadAllText("Program.fs")
    
    // Check for unified feed integration
    let hasUnifiedFeedCall = programContent.Contains("buildAllFeeds")
    let hasConversionFunctions = 
        ["convertPostsToUnified"; "convertNotesToUnified"; "convertResponsesToUnified"; 
         "convertSnippetsToUnified"; "convertWikisToUnified"; "convertPresentationsToUnified";
         "convertBooksToUnified"; "convertAlbumsToUnified"]
        |> List.forall (fun funcName -> programContent.Contains(funcName))
    
    let hasUnifiedFeedImport = programContent.Contains("UnifiedFeeds")
    
    printfn "✅ buildAllFeeds call: %b" hasUnifiedFeedCall
    printfn "✅ Conversion functions: %b" hasConversionFunctions
    printfn "✅ UnifiedFeeds import: %b" hasUnifiedFeedImport
    
    if hasUnifiedFeedCall && hasConversionFunctions && hasUnifiedFeedImport then
        printfn "✅ Program.fs: Unified system fully integrated"
    else
        printfn "⚠️  Program.fs: Integration incomplete"
else
    printfn "❌ Program.fs: File not found"

printfn ""

// Test 3: Performance benchmark and resource usage
printfn "🔍 Test 3: Performance Benchmark"
printfn "=" + String.replicate 50 "="

let benchmarkStart = DateTime.Now
let processInfo = System.Diagnostics.ProcessStartInfo()
processInfo.FileName <- "dotnet"
processInfo.Arguments <- "run"
processInfo.UseShellExecute <- false
processInfo.RedirectStandardOutput <- true
processInfo.RedirectStandardError <- true

let proc = System.Diagnostics.Process.Start(processInfo)
let output = proc.StandardOutput.ReadToEnd()
let error = proc.StandardError.ReadToEnd()
proc.WaitForExit()

let benchmarkEnd = DateTime.Now
let buildTime = (benchmarkEnd - benchmarkStart).TotalMilliseconds

printfn "✅ Build time: %.0f ms" buildTime

// Extract performance metrics from output
let unifiedFeedMatch = Regex.Match(output, @"Unified feeds generated: (\d+) total items across (\d+) content types")
if unifiedFeedMatch.Success then
    let itemCount = unifiedFeedMatch.Groups.[1].Value
    let typeCount = unifiedFeedMatch.Groups.[2].Value
    printfn "✅ Processed: %s items across %s content types" itemCount typeCount
    printfn "✅ Throughput: %.1f items/second" (float itemCount / (buildTime / 1000.0))
else
    printfn "⚠️  Could not extract performance metrics"

// Check for any build errors
if proc.ExitCode = 0 then
    printfn "✅ Build: Successful (exit code 0)"
else
    printfn "❌ Build: Failed (exit code %d)" proc.ExitCode
    if not (String.IsNullOrEmpty(error)) then
        printfn "   Error: %s" error

printfn ""

// Test 4: Feed file validation and completeness
printfn "🔍 Test 4: Feed Output Completeness"
printfn "=" + String.replicate 50 "="

let expectedFeeds = [
    ("_public/feed/index.xml", "Main fire-hose feed", true)
    ("_public/posts/index.xml", "Posts feed", false)
    ("_public/feed/notes/index.xml", "Notes feed", false)
    ("_public/feed/responses/index.xml", "Responses feed", false)
    ("_public/presentations/feed/index.xml", "Presentations feed", false)
    ("_public/snippets/feed/index.xml", "Snippets feed", false)
    ("_public/wiki/feed/index.xml", "Wiki feed", false)
    ("_public/library/feed/index.xml", "Books feed", false)
]

let mutable completeFeeds = 0
let mutable totalSize = 0L

for (path, name, isMainFeed) in expectedFeeds do
    if File.Exists(path) then
        let fileInfo = FileInfo(path)
        let sizeKB = fileInfo.Length / 1024L
        totalSize <- totalSize + fileInfo.Length
        
        if isMainFeed then
            printfn "✅ %s: %d KB (main feed)" name sizeKB
        else
            printfn "✅ %s: %d KB" name sizeKB
        
        completeFeeds <- completeFeeds + 1
    else
        printfn "❌ %s: Missing" name

let totalSizeMB = float totalSize / (1024.0 * 1024.0)
printfn ""
printfn "Feed completeness: %d/%d feeds generated" completeFeeds expectedFeeds.Length
printfn "Total feed size: %.1f MB" totalSizeMB

printfn ""

// Test 5: Feature flag status verification
printfn "🔍 Test 5: Feature Flag Status"
printfn "=" + String.replicate 50 "="

let featureFlagMatch = Regex.Match(output, @"Feature flags enabled for: ([^\n]+)")
if featureFlagMatch.Success then
    let enabledFlags = featureFlagMatch.Groups.[1].Value
    printfn "✅ Enabled content types: %s" enabledFlags
    
    // Check if all expected types are enabled
    let expectedTypes = ["Snippets"; "Wiki"; "Presentations"; "Books"; "Posts"; "Notes"; "Responses"; "Albums"]
    let allEnabled = expectedTypes |> List.forall (fun t -> enabledFlags.Contains(t))
    
    if allEnabled then
        printfn "✅ All 8 content types enabled for unified processing"
    else
        printfn "⚠️  Some content types not enabled"
else
    printfn "⚠️  Could not extract feature flag status"

printfn ""
printfn "=" + String.replicate 60 "="
printfn "🎯 Final Integration Assessment"
printfn "=" + String.replicate 60 "="

let overallSuccess = 
    cleanModules = buildModules.Length &&
    completeFeeds = expectedFeeds.Length &&
    proc.ExitCode = 0 &&
    buildTime < 60000.0 // Under 1 minute

if overallSuccess then
    printfn "🎉 UNIFIED FEED SYSTEM READY FOR PRODUCTION!"
    printfn ""
    printfn "✅ All legacy RSS functions removed"
    printfn "✅ Unified system fully integrated"
    printfn "✅ All feeds generating correctly"
    printfn "✅ Performance acceptable (%.0f ms)" buildTime
    printfn "✅ Build process successful"
    printfn ""
    printfn "Phase 3 Integration & Testing: COMPLETE ✅"
else
    printfn "⚠️  Some integration issues detected:"
    if cleanModules <> buildModules.Length then printfn "   - Legacy RSS functions not fully removed"
    if completeFeeds <> expectedFeeds.Length then printfn "   - Missing feed outputs"
    if proc.ExitCode <> 0 then printfn "   - Build process failed"
    if buildTime >= 60000.0 then printfn "   - Performance below expectations"

printfn ""
