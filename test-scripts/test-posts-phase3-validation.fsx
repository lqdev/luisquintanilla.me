#r "../bin/Debug/net9.0/PersonalSite.dll"

open System
open System.IO
open System.Text
open System.Security.Cryptography
open System.Xml

// =============================================================================
// Posts Migration Phase 3 - Output Comparison Script
// Validates identical output between legacy and new post processing systems
// =============================================================================

printfn "=== Posts Migration Phase 3: Output Comparison ==="
printfn "Validating output compatibility between legacy and new processors..."
printfn ""

// =============================================================================
// File Comparison Utilities
// =============================================================================

let getFileHash (filePath: string) : string =
    use md5 = MD5.Create()
    use stream = File.OpenRead(filePath)
    let hash = md5.ComputeHash(stream)
    BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()

let compareFiles (oldFile: string) (newFile: string) : bool * string =
    if not (File.Exists oldFile) then
        (false, "Old file missing")
    elif not (File.Exists newFile) then
        (false, "New file missing")
    else
        let oldHash = getFileHash oldFile
        let newHash = getFileHash newFile
        (oldHash = newHash, sprintf "Old: %s, New: %s" oldHash newHash)

let findFilesRecursive (directory: string) (pattern: string) : string list =
    if Directory.Exists directory then
        Directory.GetFiles(directory, pattern, SearchOption.AllDirectories)
        |> Array.map (fun f -> f.Replace(directory + Path.DirectorySeparatorChar.ToString(), ""))
        |> Array.sort
        |> Array.toList
    else
        []

// =============================================================================
// Build with Feature Flags
// =============================================================================

let buildWithFeatureFlag (useNewProcessor: bool) : unit =
    printfn "Building with NEW_POSTS=%s..." (if useNewProcessor then "true" else "false")
    
    // Set environment variable
    Environment.SetEnvironmentVariable("NEW_POSTS", if useNewProcessor then "true" else "false")
    
    // Run the build (this will be done manually - we'll check outputs)
    printfn "  Environment variable set. Run build manually with:"
    printfn "  dotnet run"
    printfn ""

// =============================================================================
// Post Content Analysis
// =============================================================================

let analyzePostContent () : int =
    printfn "=== Post Content Analysis ==="
    let postsDir = "_src/posts"
    
    if Directory.Exists postsDir then
        let postFiles = Directory.GetFiles(postsDir, "*.md")
        printfn "✅ Found %d post files in %s" postFiles.Length postsDir
        
        // Sample a few files for analysis
        let sampleFiles = postFiles |> Array.take (min 3 postFiles.Length)
        printfn "Sample files:"
        sampleFiles |> Array.iter (fun f -> 
            let fileName = Path.GetFileNameWithoutExtension(f)
            printfn "  - %s" fileName)
        
        postFiles.Length
    else
        printfn "❌ Posts directory not found: %s" postsDir
        0

// =============================================================================
// RSS Feed Validation
// =============================================================================

let validateRssFeed (outputDir: string) : bool =
    printfn "\n=== RSS Feed Validation ==="
    
    let feedPath = Path.Combine(outputDir, "posts", "feed", "index.xml")
    
    if not (File.Exists feedPath) then
        printfn "❌ RSS feed not found at %s" feedPath
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
                
                printfn "✅ First item has title: %b" (itemTitle <> null)
                printfn "✅ First item has link: %b" (itemLink <> null)
                printfn "✅ First item has pubDate: %b" (itemPubDate <> null)
                printfn "✅ First item has description: %b" (itemDescription <> null)
                
                if itemTitle <> null then
                    printfn "  Title: %s" itemTitle.InnerText
            
            printfn "✅ RSS feed validation passed"
            true
            
        with ex ->
            printfn "❌ RSS feed validation failed: %s" ex.Message
            false

// =============================================================================
// Output Directory Comparison
// =============================================================================

let compareOutputDirectories (oldDir: string) (newDir: string) : bool =
    printfn "\n=== Output Directory Comparison ==="
    printfn "Comparing: %s vs %s" oldDir newDir
    
    if not (Directory.Exists oldDir) then
        printfn "❌ Old output directory not found: %s" oldDir
        false
    elif not (Directory.Exists newDir) then
        printfn "❌ New output directory not found: %s" newDir
        false
    else
        // Find all HTML files in both directories
        let oldFiles = findFilesRecursive oldDir "*.html"
        let newFiles = findFilesRecursive newDir "*.html"
        
        printfn "Old directory files: %d" oldFiles.Length
        printfn "New directory files: %d" newFiles.Length
        
        // Find files in common, missing from old, missing from new
        let allFiles = (oldFiles @ newFiles) |> List.distinct |> List.sort
        let mutable totalFiles = 0
        let mutable matchingFiles = 0
        let mutable differentFiles = 0
        let mutable missingInOld = 0
        let mutable missingInNew = 0
        
        printfn "\nFile comparison results:"
        
        for file in allFiles do
            totalFiles <- totalFiles + 1
            let oldFilePath = Path.Combine(oldDir, file)
            let newFilePath = Path.Combine(newDir, file)
            
            let oldExists = File.Exists(oldFilePath)
            let newExists = File.Exists(newFilePath)
            
            match (oldExists, newExists) with
            | (true, true) ->
                let (isMatch, hashInfo) = compareFiles oldFilePath newFilePath
                if isMatch then
                    matchingFiles <- matchingFiles + 1
                    printfn "✅ MATCH: %s" file
                else
                    differentFiles <- differentFiles + 1
                    printfn "❌ DIFF:  %s (%s)" file hashInfo
            | (false, true) ->
                missingInOld <- missingInOld + 1
                printfn "⚠️  NEW:   %s (only in new)" file
            | (true, false) ->
                missingInNew <- missingInNew + 1
                printfn "⚠️  OLD:   %s (only in old)" file
            | (false, false) ->
                printfn "❌ ERROR: %s (missing in both - should not happen)" file
        
        printfn "\n=== Comparison Summary ==="
        printfn "Total files compared: %d" totalFiles
        printfn "Matching files: %d" matchingFiles
        printfn "Different files: %d" differentFiles
        printfn "Missing in old: %d" missingInOld
        printfn "Missing in new: %d" missingInNew
        
        let isSuccess = differentFiles = 0 && missingInOld = 0 && missingInNew = 0
        
        if isSuccess then
            printfn "✅ SUCCESS: All files match perfectly!"
        else
            printfn "❌ ISSUES: Found differences or missing files"
        
        isSuccess

// =============================================================================
// Main Validation Workflow
// =============================================================================

let runOutputComparison () =
    printfn "=== Posts Migration Phase 3: Output Comparison Workflow ==="
    printfn ""
    
    // Step 1: Analyze content
    let postCount = analyzePostContent()
    if postCount = 0 then
        printfn "❌ No posts found - cannot continue validation"
        false
    else
        printfn ""
        
        // Step 2: Instructions for manual builds
        printfn "=== Manual Build Instructions ==="
        printfn "To complete output comparison, follow these steps:"
        printfn ""
        printfn "1. Build with legacy processor:"
        printfn "   $env:NEW_POSTS=\"false\""
        printfn "   dotnet run"
        printfn "   Rename-Item _public _public_old"
        printfn ""
        printfn "2. Build with new processor:"
        printfn "   $env:NEW_POSTS=\"true\""
        printfn "   dotnet run"
        printfn "   # _public will contain new output"
        printfn ""
        printfn "3. Run comparison (after both builds complete):"
        printfn "   # This script will continue automatically"
        printfn ""
        
        // Step 3: Check if both output directories exist for comparison
        let oldDir = "_public_old"
        let newDir = "_public"
        
        if Directory.Exists(oldDir) && Directory.Exists(newDir) then
            printfn "=== Both output directories found - running comparison ==="
            
            // Compare post-specific files
            let postsOldDir = Path.Combine(oldDir, "posts")
            let postsNewDir = Path.Combine(newDir, "posts")
            
            let comparisonResult = compareOutputDirectories postsOldDir postsNewDir
            
            // Validate RSS feeds in new output
            let rssResult = validateRssFeed newDir
            
            let overallSuccess = comparisonResult && rssResult
            
            printfn "\n=== Final Results ==="
            printfn "Output comparison: %s" (if comparisonResult then "✅ PASSED" else "❌ FAILED")
            printfn "RSS validation: %s" (if rssResult then "✅ PASSED" else "❌ FAILED")
            printfn "Overall success: %s" (if overallSuccess then "✅ READY FOR PRODUCTION" else "❌ NEEDS FIXES")
            
            overallSuccess
        else
            printfn "⚠️  Output directories not ready for comparison"
            printfn "   Old dir exists: %b (%s)" (Directory.Exists(oldDir)) oldDir
            printfn "   New dir exists: %b (%s)" (Directory.Exists(newDir)) newDir
            printfn ""
            printfn "Complete the manual build steps above, then run this script again."
            false

// =============================================================================
// Execute Validation
// =============================================================================

let success = runOutputComparison()
if not success then
    exit 1
else
    printfn "\n✅ Phase 3 validation script completed successfully"
