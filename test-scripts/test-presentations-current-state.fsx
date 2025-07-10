// Simplified presentations migration validation script
// Focuses on output comparison and file validation

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

// Test configuration
let sourceDir = "../_src/presentations"
let outputDir = "../_public/presentations"
let feedPath = "../_public/presentations/feed/index.xml"

// Test 1: Check source files
let testSourceFiles() =
    printfn "=== SOURCE FILES TEST ==="
    
    if Directory.Exists(sourceDir) then
        let files = Directory.GetFiles(sourceDir, "*.md")
        printfn "📁 Source directory: %s" sourceDir
        printfn "📄 Files found: %d" files.Length
        
        for file in files do
            let fileName = Path.GetFileName(file)
            let content = File.ReadAllText(file)
            let hasTitle = content.Contains("title:")
            let hasRevealJs = content.Contains("reveal") || content.Contains("slides")
            printfn "   %s (Title: %b, Reveal: %b)" fileName hasTitle hasRevealJs
        
        files.Length > 0
    else
        printfn "❌ Source directory not found: %s" sourceDir
        false

// Test 2: Check output files
let testOutputFiles() =
    printfn "\n=== OUTPUT FILES TEST ==="
    
    if Directory.Exists(outputDir) then
        let sourceFiles = Directory.GetFiles(sourceDir, "*.md")
        let mutable allGenerated = true
        
        printfn "📁 Output directory: %s" outputDir
        
        for sourceFile in sourceFiles do
            let fileName = Path.GetFileNameWithoutExtension(sourceFile)
            let outputPath = Path.Combine(outputDir, fileName, "index.html")
            
            if File.Exists(outputPath) then
                let content = File.ReadAllText(outputPath)
                let hasHtml = content.Contains("<html")
                let hasRevealJs = content.Contains("reveal.js") || content.Contains("Reveal")
                let hasTitle = content.Contains("<title>")
                
                printfn "✅ %s/index.html (HTML: %b, Reveal: %b, Title: %b)" fileName hasHtml hasRevealJs hasTitle
            else
                printfn "❌ %s/index.html - MISSING" fileName
                allGenerated <- false
        
        allGenerated
    else
        printfn "❌ Output directory not found: %s" outputDir
        false

// Test 3: Check RSS feed
let testRssFeed() =
    printfn "\n=== RSS FEED TEST ==="
    
    if File.Exists(feedPath) then
        let content = File.ReadAllText(feedPath)
        let hasXmlDeclaration = content.Contains("<?xml")
        let hasRssTag = content.Contains("<rss")
        let hasChannelTag = content.Contains("<channel>")
        let hasItems = content.Contains("<item>")
        
        let itemCount = 
            content.Split("<item>")
            |> Array.length
            |> fun x -> x - 1
        
        printfn "✅ Feed exists: %s" feedPath
        printfn "✅ XML Declaration: %b" hasXmlDeclaration
        printfn "✅ RSS Tag: %b" hasRssTag
        printfn "✅ Channel Tag: %b" hasChannelTag
        printfn "✅ Has Items: %b (Count: %d)" hasItems itemCount
        
        hasXmlDeclaration && hasRssTag && hasChannelTag && itemCount > 0
    else
        printfn "❌ RSS feed not found: %s" feedPath
        false

// Test 4: Check index page
let testIndexPage() =
    printfn "\n=== INDEX PAGE TEST ==="
    
    let indexPath = Path.Combine(outputDir, "index.html")
    
    if File.Exists(indexPath) then
        let content = File.ReadAllText(indexPath)
        let hasHtml = content.Contains("<html")
        let hasPresentationLinks = content.Contains("href=") && (content.Contains("/presentations/") || content.Contains("presentations/"))
        let hasNavigation = content.Contains("nav") || content.Contains("menu") || content.Contains("link")
        
        printfn "✅ Index page exists: %s" indexPath
        printfn "✅ HTML structure: %b" hasHtml
        printfn "✅ Presentation links: %b" hasPresentationLinks
        printfn "✅ Navigation elements: %b" hasNavigation
        
        hasHtml && hasPresentationLinks
    else
        printfn "❌ Index page not found: %s" indexPath
        false

// Test 5: Content size analysis
let testContentSizes() =
    printfn "\n=== CONTENT SIZE ANALYSIS ==="
    
    if Directory.Exists(outputDir) then
        let sourceFiles = Directory.GetFiles(sourceDir, "*.md")
        
        for sourceFile in sourceFiles do
            let fileName = Path.GetFileNameWithoutExtension(sourceFile)
            let outputPath = Path.Combine(outputDir, fileName, "index.html")
            
            if File.Exists(outputPath) then
                let sourceSize = (new FileInfo(sourceFile)).Length
                let outputSize = (new FileInfo(outputPath)).Length
                let ratio = float outputSize / float sourceSize
                
                printfn "📊 %s: %dB → %dB (%.1fx)" fileName sourceSize outputSize ratio
            else
                printfn "❌ %s: Output missing" fileName
        
        true
    else
        false

// Main execution
let runValidation() =
    printfn "🚀 PRESENTATIONS MIGRATION VALIDATION"
    printfn "======================================"
    
    let tests = [
        ("Source Files", testSourceFiles())
        ("Output Files", testOutputFiles())
        ("RSS Feed", testRssFeed())
        ("Index Page", testIndexPage())
        ("Content Sizes", testContentSizes())
    ]
    
    printfn "\n======================================"
    printfn "📋 VALIDATION SUMMARY"
    printfn "======================================"
    
    let mutable allPassed = true
    for name, result in tests do
        let status = if result then "✅ PASS" else "❌ FAIL"
        printfn "%s: %s" name status
        if not result then allPassed <- false
    
    printfn "\n🎯 OVERALL RESULT: %s" (if allPassed then "✅ ALL TESTS PASSED" else "❌ SOME TESTS FAILED")
    
    if allPassed then
        printfn "\n🎉 Current state validation complete!"
        printfn "💡 Next: Test with feature flag switching to compare old vs new outputs"
    else
        printfn "\n⚠️  Fix issues before proceeding with comparison testing"
    
    allPassed

// Execute validation
runValidation()
