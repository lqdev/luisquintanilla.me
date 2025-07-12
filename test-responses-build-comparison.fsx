open System
open System.IO
open System.Security.Cryptography
open System.Text
open System.Diagnostics

// Test configuration
let responseDir = "_src/responses"
let outputDir = "_test_validation"
let legacyOutputDir = Path.Combine(outputDir, "legacy_responses")
let astOutputDir = Path.Combine(outputDir, "ast_responses")

// Create output directories
Directory.CreateDirectory(outputDir) |> ignore
Directory.CreateDirectory(legacyOutputDir) |> ignore  
Directory.CreateDirectory(astOutputDir) |> ignore

printfn "=== Response Migration Validation Test ==="
printfn "Testing legacy vs AST-based response processing using actual builds"
printfn ""

// Get sample of response files for testing
let allResponseFiles = 
    Directory.GetFiles(responseDir, "*.md")
    |> Array.sort

let sampleSize = 10 // Start with small sample for testing
let testFiles = allResponseFiles.[..Math.Min(sampleSize-1, allResponseFiles.Length-1)]

printfn $"Total response files: {allResponseFiles.Length}"
printfn $"Testing with sample: {testFiles.Length} files"
printfn ""

// Helper function to run dotnet build and capture responses
let runBuildWithFeatureFlag (flagValue: string) (outputSuffix: string) =
    try
        // Set environment variable
        Environment.SetEnvironmentVariable("NEW_RESPONSES", flagValue)
        
        printfn $"Building with NEW_RESPONSES={flagValue}..."
        
        let startInfo = ProcessStartInfo()
        startInfo.FileName <- "dotnet"
        startInfo.Arguments <- "run"
        startInfo.WorkingDirectory <- Directory.GetCurrentDirectory()
        startInfo.RedirectStandardOutput <- true
        startInfo.RedirectStandardError <- true
        startInfo.UseShellExecute <- false
        
        use process = Process.Start(startInfo)
        process.WaitForExit()
        
        let output = process.StandardOutput.ReadToEnd()
        let error = process.StandardError.ReadToEnd()
        
        if process.ExitCode = 0 then
            printfn $"✅ Build successful with {flagValue}"
            
            // Copy generated response files for comparison
            let sourceResponseDir = "_public/feed"
            if Directory.Exists(sourceResponseDir) then
                let targetDir = Path.Combine(outputDir, $"responses_{outputSuffix}")
                if Directory.Exists(targetDir) then
                    Directory.Delete(targetDir, true)
                Directory.CreateDirectory(targetDir) |> ignore
                
                // Copy individual response pages and RSS
                let sourcePaths = Directory.GetFiles(sourceResponseDir, "*", SearchOption.AllDirectories)
                for sourcePath in sourcePaths do
                    if sourcePath.Contains("responses") || Path.GetFileName(sourcePath).Contains("response") then
                        let relativePath = Path.GetRelativePath(sourceResponseDir, sourcePath)
                        let targetPath = Path.Combine(targetDir, relativePath)
                        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)) |> ignore
                        File.Copy(sourcePath, targetPath, true)
                
                printfn $"📁 Responses copied to {targetDir}"
            else
                printfn $"❌ Source response directory not found: {sourceResponseDir}"
                
            true
        else
            printfn $"❌ Build failed with {flagValue}"
            printfn $"Error: {error}"
            false
    with
    | ex ->
        printfn $"❌ Exception during build with {flagValue}: {ex.Message}"
        false

// Test 1: Build with legacy system (NEW_RESPONSES=false)
printfn "=== Test 1: Legacy System Build ==="
let legacySuccess = runBuildWithFeatureFlag "false" "legacy"
printfn ""

// Test 2: Build with AST system (NEW_RESPONSES=true)
printfn "=== Test 2: AST System Build ==="
let astSuccess = runBuildWithFeatureFlag "true" "ast"
printfn ""

// Test 3: Compare outputs if both builds succeeded
if legacySuccess && astSuccess then
    printfn "=== Test 3: Output Comparison ==="
    
    let legacyDir = Path.Combine(outputDir, "responses_legacy")
    let astDir = Path.Combine(outputDir, "responses_ast")
    
    if Directory.Exists(legacyDir) && Directory.Exists(astDir) then
        let legacyFiles = Directory.GetFiles(legacyDir, "*", SearchOption.AllDirectories)
        let astFiles = Directory.GetFiles(astDir, "*", SearchOption.AllDirectories)
        
        printfn $"Legacy files: {legacyFiles.Length}"
        printfn $"AST files: {astFiles.Length}"
        
        // Compare RSS feeds
        let legacyRssPath = Path.Combine(legacyDir, "responses", "index.xml")
        let astRssPath = Path.Combine(astDir, "responses", "index.xml")
        
        if File.Exists(legacyRssPath) && File.Exists(astRssPath) then
            let legacyRss = File.ReadAllText(legacyRssPath)
            let astRss = File.ReadAllText(astRssPath)
            
            if legacyRss = astRss then
                printfn "✅ RSS feeds are identical"
            else
                printfn "❌ RSS feeds differ"
                File.WriteAllText(Path.Combine(outputDir, "rss_legacy.xml"), legacyRss)
                File.WriteAllText(Path.Combine(outputDir, "rss_ast.xml"), astRss)
                printfn "📄 RSS files saved for manual comparison"
        else
            printfn "❌ RSS feed files not found for comparison"
        
        // Compare individual response pages (sample)
        let mutable pageMatches = 0
        let mutable pageDifferences = 0
        
        for legacyFile in legacyFiles.[..Math.Min(4, legacyFiles.Length-1)] do
            let relativePath = Path.GetRelativePath(legacyDir, legacyFile)
            let astFile = Path.Combine(astDir, relativePath)
            
            if File.Exists(astFile) then
                let legacyContent = File.ReadAllText(legacyFile)
                let astContent = File.ReadAllText(astFile)
                
                if legacyContent = astContent then
                    pageMatches <- pageMatches + 1
                else
                    pageDifferences <- pageDifferences + 1
                    printfn $"❌ Page differs: {relativePath}"
            else
                printfn $"❌ AST file missing: {relativePath}"
        
        printfn $"Page comparison: {pageMatches} matches, {pageDifferences} differences"
        
    else
        printfn "❌ Output directories not found for comparison"
else
    printfn "❌ Cannot compare outputs - one or both builds failed"

// Test 4: Microformat validation on AST output
if astSuccess then
    printfn ""
    printfn "=== Test 4: Microformat Validation ==="
    
    let astDir = Path.Combine(outputDir, "responses_ast")
    if Directory.Exists(astDir) then
        let htmlFiles = Directory.GetFiles(astDir, "*.html", SearchOption.AllDirectories)
        let mutable microformatIssues = 0
        
        for htmlFile in htmlFiles.[..Math.Min(4, htmlFiles.Length-1)] do
            let content = File.ReadAllText(htmlFile)
            let fileName = Path.GetFileName(htmlFile)
            
            let hasHEntry = content.Contains("h-entry")
            let hasResponseType = content.Contains("response-type")
            
            if not hasHEntry then
                microformatIssues <- microformatIssues + 1
                printfn $"❌ Missing h-entry: {fileName}"
            
            if not hasResponseType then
                microformatIssues <- microformatIssues + 1
                printfn $"❌ Missing response-type: {fileName}"
        
        if microformatIssues = 0 then
            printfn "✅ Microformat validation passed"
        else
            printfn $"❌ {microformatIssues} microformat issues found"
    else
        printfn "❌ AST output directory not found for microformat validation"

// Final Summary
printfn ""
printfn "=== VALIDATION SUMMARY ==="
printfn $"Legacy Build: {if legacySuccess then "✅ Success" else "❌ Failed"}"
printfn $"AST Build: {if astSuccess then "✅ Success" else "❌ Failed"}"

if legacySuccess && astSuccess then
    printfn "🔄 Both systems built successfully - ready for detailed comparison"
    printfn $"📁 Comparison files saved to: {outputDir}"
else
    printfn "❌ Build issues found - resolve before proceeding with migration"

// Reset environment
Environment.SetEnvironmentVariable("NEW_RESPONSES", null)
printfn ""
printfn "Environment reset - NEW_RESPONSES cleared"
