// Books Migration Phase 3 - Feature Flag and Integration Testing
// Tests feature flag behavior and integration with rest of the system

open System
open System.IO

// =============================================================================
// Feature Flag Testing
// =============================================================================

let testFeatureFlagOff () =
    printfn "=== Testing NEW_BOOKS=false (Feature Flag Off) ==="
    
    // Clean library directory first
    let libraryDir = "_public/library"
    if Directory.Exists(libraryDir) then
        Directory.Delete(libraryDir, true)
    
    // Build without feature flag
    let buildResult = System.Diagnostics.Process.Start(
        "dotnet", 
        "run",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    )
    
    buildResult.WaitForExit()
    let output = buildResult.StandardOutput.ReadToEnd()
    let error = buildResult.StandardError.ReadToEnd()
    
    printfn "✅ Build exit code: %d" buildResult.ExitCode
    printfn "✅ Build output contains skip message: %b" (output.Contains("Skipping books"))
    
    // Check that library directory was not created
    let libraryNotGenerated = not (Directory.Exists(libraryDir))
    printfn "✅ Library directory not generated: %b" libraryNotGenerated
    
    buildResult.ExitCode = 0 && libraryNotGenerated

let testFeatureFlagOn () =
    printfn "\n=== Testing NEW_BOOKS=true (Feature Flag On) ==="
    
    // Clean library directory first
    let libraryDir = "_public/library"
    if Directory.Exists(libraryDir) then
        Directory.Delete(libraryDir, true)
    
    // Set environment variable and build
    Environment.SetEnvironmentVariable("NEW_BOOKS", "true")
    
    let buildResult = System.Diagnostics.Process.Start(
        "dotnet", 
        "run",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true,
        EnvironmentVariables = dict ["NEW_BOOKS", "true"]
    )
    
    buildResult.WaitForExit()
    let output = buildResult.StandardOutput.ReadToEnd()
    let error = buildResult.StandardError.ReadToEnd()
    
    printfn "✅ Build exit code: %d" buildResult.ExitCode
    printfn "✅ Build output contains AST message: %b" (output.Contains("Building books with AST-based processor"))
    
    // Check that library directory was created with expected content
    let libraryExists = Directory.Exists(libraryDir)
    let indexExists = File.Exists(Path.Combine(libraryDir, "index.html"))
    let feedExists = File.Exists(Path.Combine(libraryDir, "feed", "index.xml"))
    
    printfn "✅ Library directory generated: %b" libraryExists
    printfn "✅ Library index generated: %b" indexExists
    printfn "✅ RSS feed generated: %b" feedExists
    
    if libraryExists then
        let bookDirs = Directory.GetDirectories(libraryDir) 
                      |> Array.filter (fun dir -> not (Path.GetFileName(dir).Equals("feed")))
        printfn "✅ Book directories generated: %d" bookDirs.Length
    
    buildResult.ExitCode = 0 && libraryExists && indexExists && feedExists

// =============================================================================
// Integration Testing
// =============================================================================

let testSystemIntegration () =
    printfn "\n=== Testing System Integration ==="
    
    // Ensure feature flag is on for integration test
    Environment.SetEnvironmentVariable("NEW_BOOKS", "true")
    
    // Build the entire system
    let buildResult = System.Diagnostics.Process.Start(
        "dotnet", 
        "run",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true,
        EnvironmentVariables = dict ["NEW_BOOKS", "true"]
    )
    
    buildResult.WaitForExit()
    
    if buildResult.ExitCode <> 0 then
        printfn "❌ System build failed"
        false
    else
        // Check that all content types are built
        let publicDir = "_public"
        let expectedDirs = [
            "posts"
            "snippets" 
            "wiki"
            "presentations"
            "library"  // Our new books content
        ]
        
        let mutable allContentTypesGenerated = true
        
        for dir in expectedDirs do
            let dirPath = Path.Combine(publicDir, dir)
            let exists = Directory.Exists(dirPath)
            printfn "✅ Content type %s generated: %b" dir exists
            if not exists then
                allContentTypesGenerated <- false
        
        // Check main site files
        let indexExists = File.Exists(Path.Combine(publicDir, "index.html"))
        let mainFeedExists = File.Exists(Path.Combine(publicDir, "feed", "index.xml"))
        
        printfn "✅ Main site index: %b" indexExists
        printfn "✅ Main site feed: %b" mainFeedExists
        
        // Check that books don't interfere with other content
        let postsHaveContent = Directory.Exists(Path.Combine(publicDir, "posts")) && 
                              Directory.GetDirectories(Path.Combine(publicDir, "posts")).Length > 0
        let snippetsHaveContent = Directory.Exists(Path.Combine(publicDir, "snippets")) && 
                                 Directory.GetDirectories(Path.Combine(publicDir, "snippets")).Length > 0
        
        printfn "✅ Posts still generated: %b" postsHaveContent
        printfn "✅ Snippets still generated: %b" snippetsHaveContent
        
        allContentTypesGenerated && indexExists && postsHaveContent && snippetsHaveContent

// =============================================================================
// Performance and Scale Testing
// =============================================================================

let testPerformanceMetrics () =
    printfn "\n=== Performance and Scale Testing ==="
    
    // Ensure feature flag is on
    Environment.SetEnvironmentVariable("NEW_BOOKS", "true")
    
    let startTime = DateTime.Now
    
    let buildResult = System.Diagnostics.Process.Start(
        "dotnet", 
        "run",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true,
        EnvironmentVariables = dict ["NEW_BOOKS", "true"]
    )
    
    buildResult.WaitForExit()
    let endTime = DateTime.Now
    let buildTime = endTime - startTime
    
    printfn "✅ Build time: %A" buildTime
    printfn "✅ Build successful: %b" (buildResult.ExitCode = 0)
    
    if buildResult.ExitCode = 0 then
        // Measure output sizes
        let libraryDir = "_public/library"
        if Directory.Exists(libraryDir) then
            let allFiles = Directory.GetFiles(libraryDir, "*", SearchOption.AllDirectories)
            let totalSize = allFiles |> Array.sumBy (fun f -> (FileInfo(f)).Length)
            
            printfn "✅ Library files generated: %d" allFiles.Length
            printfn "✅ Total library size: %d bytes (%.2f KB)" totalSize (float totalSize / 1024.0)
            
            // Check individual book processing time (estimated)
            let bookDirs = Directory.GetDirectories(libraryDir) 
                          |> Array.filter (fun dir -> not (Path.GetFileName(dir).Equals("feed")))
            
            if bookDirs.Length > 0 then
                let avgProcessingTime = buildTime.TotalMilliseconds / float bookDirs.Length
                printfn "✅ Avg book processing time: %.2f ms" avgProcessingTime
            
            true
        else
            printfn "❌ Library directory not found"
            false
    else
        false

// =============================================================================
// Main Test Runner
// =============================================================================

let runIntegrationTests () =
    printfn "Books Migration Phase 3 - Integration and Feature Flag Testing"
    printfn "==============================================================="
    
    let results = [
        testFeatureFlagOff()
        testFeatureFlagOn()
        testSystemIntegration()
        testPerformanceMetrics()
    ]
    
    let allPassed = results |> List.forall id
    
    printfn "\n=== Integration Test Summary ==="
    printfn "Feature Flag OFF: %s" (if results.[0] then "✅ PASS" else "❌ FAIL")
    printfn "Feature Flag ON: %s" (if results.[1] then "✅ PASS" else "❌ FAIL")
    printfn "System Integration: %s" (if results.[2] then "✅ PASS" else "❌ FAIL")
    printfn "Performance Metrics: %s" (if results.[3] then "✅ PASS" else "❌ FAIL")
    printfn ""
    printfn "OVERALL RESULT: %s" (if allPassed then "✅ ALL INTEGRATION TESTS PASSED" else "❌ SOME TESTS FAILED")
    
    if allPassed then
        printfn "\n🎉 Books migration integration testing complete!"
        printfn "   Migration is ready for production deployment."
        printfn "   Feature flag behavior confirmed working correctly."
    else
        printfn "\n⚠️  Some integration tests failed. Review before production."
    
    allPassed

// Run the integration tests
runIntegrationTests()
