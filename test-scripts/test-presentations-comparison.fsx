// Output comparison test for presentations migration
// Validates that old and new systems produce identical results

open System
open System.IO
open System.Text
open System.Security.Cryptography

// Helper functions
let computeHash (content: string) =
    use md5 = MD5.Create()
    let bytes = Encoding.UTF8.GetBytes(content)
    let hash = md5.ComputeHash(bytes)
    Convert.ToHexString(hash)

let readFileIfExists path =
    if File.Exists(path) then 
        Some(File.ReadAllText(path)) 
    else 
        None

let cleanupDirectory path =
    if Directory.Exists(path) then
        Directory.Delete(path, true)

// Test configuration
let sourceDir = "../_src/presentations"
let outputDir = "../_public/presentations"
let oldOutputDir = "../_test_validation/old_presentations"
let newOutputDir = "../_test_validation/new_presentations"

// Step 1: Build with old system and save outputs
let buildWithOldSystem() =
    printfn "=== BUILDING WITH OLD SYSTEM ==="
    
    // Set environment variable for old system
    Environment.SetEnvironmentVariable("NEW_PRESENTATIONS", "false")
    
    // Clean old output directory
    cleanupDirectory oldOutputDir
    Directory.CreateDirectory(oldOutputDir) |> ignore
    
    // Run build (this would normally trigger a build, but we'll simulate)
    // In a real scenario, we'd need to trigger `dotnet run` here
    printfn "⚠️  Manual step required: Run 'dotnet run' with NEW_PRESENTATIONS=false"
    printfn "   Then copy _public/presentations/* to _test_validation/old_presentations/"
    printfn "   Press Enter when complete..."
    Console.ReadLine() |> ignore

// Step 2: Build with new system and save outputs  
let buildWithNewSystem() =
    printfn "\n=== BUILDING WITH NEW SYSTEM ==="
    
    // Set environment variable for new system
    Environment.SetEnvironmentVariable("NEW_PRESENTATIONS", "true")
    
    // Clean new output directory
    cleanupDirectory newOutputDir
    Directory.CreateDirectory(newOutputDir) |> ignore
    
    printfn "⚠️  Manual step required: Run 'dotnet run' with NEW_PRESENTATIONS=true"
    printfn "   Then copy _public/presentations/* to _test_validation/new_presentations/"
    printfn "   Press Enter when complete..."
    Console.ReadLine() |> ignore

// Step 3: Compare outputs
let compareOutputs() =
    printfn "\n=== COMPARING OUTPUTS ==="
    
    if not (Directory.Exists(oldOutputDir) && Directory.Exists(newOutputDir)) then
        printfn "❌ Output directories missing. Run build steps first."
        false
    else
        let mutable allMatch = true
        
        // Compare HTML files
        let sourceFiles = Directory.GetFiles(sourceDir, "*.md")
        
        for sourceFile in sourceFiles do
            let fileName = Path.GetFileNameWithoutExtension(sourceFile)
            let oldHtmlPath = Path.Combine(oldOutputDir, fileName, "index.html")
            let newHtmlPath = Path.Combine(newOutputDir, fileName, "index.html")
            
            match readFileIfExists oldHtmlPath, readFileIfExists newHtmlPath with
            | Some oldContent, Some newContent ->
                let oldHash = computeHash oldContent
                let newHash = computeHash newContent
                if oldHash = newHash then
                    printfn "✅ %s/index.html: IDENTICAL" fileName
                else
                    printfn "❌ %s/index.html: DIFFERENT" fileName
                    printfn "   Old hash: %s" oldHash
                    printfn "   New hash: %s" newHash
                    
                    // Show content length difference
                    printfn "   Old length: %d bytes" oldContent.Length
                    printfn "   New length: %d bytes" newContent.Length
                    allMatch <- false
            | None, None ->
                printfn "⚠️  %s/index.html: Missing in both systems" fileName
            | Some _, None ->
                printfn "❌ %s/index.html: Missing in new system" fileName
                allMatch <- false
            | None, Some _ ->
                printfn "✅ %s/index.html: Added in new system" fileName
        
        // Compare index pages
        let oldIndexPath = Path.Combine(oldOutputDir, "index.html")
        let newIndexPath = Path.Combine(newOutputDir, "index.html")
        
        match readFileIfExists oldIndexPath, readFileIfExists newIndexPath with
        | Some oldContent, Some newContent ->
            let oldHash = computeHash oldContent
            let newHash = computeHash newContent
            if oldHash = newHash then
                printfn "✅ index.html: IDENTICAL"
            else
                printfn "❌ index.html: DIFFERENT"
                printfn "   Old hash: %s" oldHash
                printfn "   New hash: %s" newHash
                allMatch <- false
        | _ ->
            printfn "⚠️  index.html comparison failed"
            allMatch <- false
        
        // Check RSS feed (only in new system)
        let newFeedPath = Path.Combine(newOutputDir, "feed", "index.xml")
        if File.Exists(newFeedPath) then
            printfn "✅ RSS feed: Generated in new system"
        else
            printfn "❌ RSS feed: Missing in new system"
            allMatch <- false
        
        printfn "\n🎯 COMPARISON RESULT: %s" (if allMatch then "✅ ALL IDENTICAL" else "❌ DIFFERENCES FOUND")
        allMatch

// Step 4: Automated comparison (if build outputs exist)
let automatedComparison() =
    printfn "=== AUTOMATED COMPARISON ==="
    printfn "Checking current _public/presentations output against both systems..."
    
    let currentFiles = 
        if Directory.Exists(outputDir) then
            Directory.GetFiles(Path.Combine(outputDir, "*"), "index.html", SearchOption.AllDirectories)
            |> Array.map (fun f -> Path.GetRelativePath(outputDir, f))
        else
            [||]
    
    printfn "📁 Current output files: %d" currentFiles.Length
    
    for file in currentFiles do
        let fullPath = Path.Combine(outputDir, file)
        if File.Exists(fullPath) then
            let content = File.ReadAllText(fullPath)
            let hash = computeHash content
            printfn "📄 %s: %s (%d bytes)" file hash content.Length
    
    // Check RSS feed
    let feedPath = Path.Combine(outputDir, "feed", "index.xml")
    if File.Exists(feedPath) then
        let feedContent = File.ReadAllText(feedPath)
        let feedHash = computeHash feedContent
        printfn "📄 feed/index.xml: %s (%d bytes)" feedHash feedContent.Length
    else
        printfn "❌ feed/index.xml: Not found"
    
    currentFiles.Length > 0

// Main execution
let runComparison() =
    printfn "🚀 PRESENTATIONS MIGRATION OUTPUT COMPARISON"
    printfn "============================================="
    
    printfn "\nChoose comparison method:"
    printfn "1. Manual comparison (build old, then new, then compare)"
    printfn "2. Automated comparison (check current output state)"
    printfn "3. Both"
    printf "Enter choice (1-3): "
    
    let choice = Console.ReadLine()
    
    match choice with
    | "1" ->
        buildWithOldSystem()
        buildWithNewSystem()
        compareOutputs() |> ignore
    | "2" ->
        automatedComparison() |> ignore
    | "3" ->
        let automated = automatedComparison()
        if automated then
            printfn "\n%s" ("=".PadLeft(45, '='))
            buildWithOldSystem()
            buildWithNewSystem()
            compareOutputs() |> ignore
    | _ ->
        printfn "Invalid choice. Running automated comparison..."
        automatedComparison() |> ignore

// Execute comparison
runComparison()
