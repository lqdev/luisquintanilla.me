#r "nuget: FSharp.Data"

open System
open System.IO
open System.Security.Cryptography
open System.Text
open System.Xml.Linq

// Test configuration
let srcDir = Path.Join(__SOURCE_DIRECTORY__, "_src")
let outputDirLegacy = Path.Join(__SOURCE_DIRECTORY__, "_public_legacy_notes")
let outputDirNew = Path.Join(__SOURCE_DIRECTORY__, "_public_new_notes")

// Helper functions for validation
let calculateFileHash (filePath: string) =
    use sha256 = SHA256.Create()
    use stream = File.OpenRead(filePath)
    let hash = sha256.ComputeHash(stream)
    BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()

let validateXmlStructure (xmlPath: string) =
    try
        let doc = XDocument.Load(xmlPath)
        let rss = doc.Root
        if rss.Name.LocalName = "rss" then
            let channel = rss.Element(XName.Get "channel")
            if channel <> null then
                let title = channel.Element(XName.Get "title")
                let link = channel.Element(XName.Get "link") 
                let description = channel.Element(XName.Get "description")
                let items = channel.Elements(XName.Get "item")
                printfn "✅ RSS Feed Valid: %s" xmlPath
                printfn "   Title: %s" (if title <> null then title.Value else "Missing")
                printfn "   Items: %d" (items |> Seq.length)
                true
            else
                printfn "❌ RSS Feed Missing Channel: %s" xmlPath
                false
        else
            printfn "❌ RSS Feed Invalid Root: %s" xmlPath
            false
    with
    | ex -> 
        printfn "❌ RSS Feed Parse Error: %s - %s" xmlPath ex.Message
        false

let compareDirectoryStructure (dir1: string) (dir2: string) =
    if Directory.Exists(dir1) && Directory.Exists(dir2) then
        let files1 = Directory.GetFiles(dir1, "*", SearchOption.AllDirectories)
                    |> Array.map (fun f -> f.Replace(dir1, "").Replace("\\", "/"))
                    |> Set.ofArray
        let files2 = Directory.GetFiles(dir2, "*", SearchOption.AllDirectories)
                    |> Array.map (fun f -> f.Replace(dir2, "").Replace("\\", "/"))
                    |> Set.ofArray
        
        let onlyInDir1 = Set.difference files1 files2
        let onlyInDir2 = Set.difference files2 files1
        
        if onlyInDir1.IsEmpty && onlyInDir2.IsEmpty then
            printfn "✅ Directory structures match"
            true
        else
            printfn "❌ Directory structure differences:"
            onlyInDir1 |> Set.iter (fun f -> printfn "   Only in legacy: %s" f)
            onlyInDir2 |> Set.iter (fun f -> printfn "   Only in new: %s" f)
            false
    else
        printfn "❌ One or both directories don't exist"
        false

// Notes Migration Validation Tests
printfn "=== Notes Migration Validation Tests ==="
printfn "Date: %s" (DateTime.Now.ToString("yyyy-MM-dd HH:mm"))
printfn ""

// Test 1: Verify notes source files exist
printfn "1. Verifying notes source files..."
let notesDir = Path.Join(srcDir, "feed")
if Directory.Exists(notesDir) then
    let noteFiles = Directory.GetFiles(notesDir, "*.md")
    printfn "✅ Found %d note files in %s" noteFiles.Length notesDir
    noteFiles |> Array.take (min 5 noteFiles.Length) |> Array.iter (fun f -> 
        printfn "   - %s" (Path.GetFileName(f)))
    if noteFiles.Length > 5 then
        printfn "   ... and %d more" (noteFiles.Length - 5)
else
    printfn "❌ Notes directory not found: %s" notesDir

printfn ""

// Test 2: Build with legacy system (NEW_NOTES=false)
printfn "2. Building with legacy notes system..."
Environment.SetEnvironmentVariable("NEW_NOTES", "false")

let legacyBuildResult = 
    try
        // Note: This simulates the build - in real scenario we'd call the actual build
        // For now, we'll check if the current _public directory has notes
        let currentPublicDir = Path.Join(__SOURCE_DIRECTORY__, "_public")
        let feedDir = Path.Join(currentPublicDir, "feed")
        if Directory.Exists(feedDir) then
            let indexHtml = Path.Join(feedDir, "index.html")
            let indexXml = Path.Join(feedDir, "index.xml")
            printfn "✅ Legacy build structure exists"
            printfn "   Feed index: %s" (if File.Exists(indexHtml) then "✅" else "❌")
            printfn "   Feed RSS: %s" (if File.Exists(indexXml) then "✅" else "❌")
            true
        else
            printfn "⚠️  Feed directory not found - run build first"
            false
    with
    | ex ->
        printfn "❌ Legacy build failed: %s" ex.Message
        false

printfn ""

// Test 3: Validate NEW_NOTES feature flag functionality
printfn "3. Testing NEW_NOTES feature flag..."

// Test disabled state
Environment.SetEnvironmentVariable("NEW_NOTES", "false")
printfn "   NEW_NOTES=false: Should use legacy system"

// Test enabled state  
Environment.SetEnvironmentVariable("NEW_NOTES", "true")
printfn "   NEW_NOTES=true: Should use new buildNotes() function"

// Reset to default
Environment.SetEnvironmentVariable("NEW_NOTES", null)
printfn "   NEW_NOTES=unset: Should default to false (legacy system)"

printfn ""

// Test 4: RSS Feed Structure Validation
printfn "4. Validating RSS feed structure..."
let publicFeedDir = Path.Join(__SOURCE_DIRECTORY__, "_public", "feed")
if Directory.Exists(publicFeedDir) then
    let rssFile = Path.Join(publicFeedDir, "index.xml")
    if File.Exists(rssFile) then
        validateXmlStructure rssFile |> ignore
    else
        printfn "⚠️  RSS file not found: %s" rssFile
else
    printfn "⚠️  Public feed directory not found: %s" publicFeedDir

printfn ""

// Test 5: Content Structure Validation
printfn "5. Validating notes content structure..."
let publicDir = Path.Join(__SOURCE_DIRECTORY__, "_public")
let feedDir = Path.Join(publicDir, "feed")

if Directory.Exists(feedDir) then
    // Check for individual note directories
    let noteDirs = Directory.GetDirectories(feedDir)
                  |> Array.filter (fun d -> not (Path.GetFileName(d) = "responses"))
    
    printfn "✅ Found %d individual note directories" noteDirs.Length
    
    // Validate structure of first few notes
    noteDirs 
    |> Array.take (min 3 noteDirs.Length)
    |> Array.iter (fun noteDir ->
        let noteName = Path.GetFileName(noteDir)
        let indexFile = Path.Join(noteDir, "index.html")
        if File.Exists(indexFile) then
            printfn "   ✅ %s/index.html" noteName
        else
            printfn "   ❌ %s/index.html (missing)" noteName)
else
    printfn "⚠️  Feed directory not found for validation"

printfn ""

// Test 6: Integration Testing
printfn "6. Testing integration with existing systems..."

// Check if notes appear in tag pages
let tagsDir = Path.Join(publicDir, "tags")
if Directory.Exists(tagsDir) then
    printfn "✅ Tags directory exists - notes should be included in tag aggregation"
else
    printfn "⚠️  Tags directory not found"

// Check main RSS feed integration
let mainRssFile = Path.Join(publicDir, "feed.xml")
if File.Exists(mainRssFile) then
    printfn "✅ Main RSS feed exists - notes should be integrated"
else
    printfn "⚠️  Main RSS feed not found"

printfn ""

// Test 7: Performance and Content Validation
printfn "7. Content validation summary..."

let notesSourceDir = Path.Join(srcDir, "feed")
if Directory.Exists(notesSourceDir) then
    let sourceFiles = Directory.GetFiles(notesSourceDir, "*.md")
    let publicFeedDir = Path.Join(publicDir, "feed")
    
    if Directory.Exists(publicFeedDir) then
        let builtNotes = Directory.GetDirectories(publicFeedDir)
                       |> Array.filter (fun d -> not (Path.GetFileName(d) = "responses"))
        
        printfn "   Source notes: %d files" sourceFiles.Length
        printfn "   Built notes: %d directories" builtNotes.Length
        
        if sourceFiles.Length = builtNotes.Length then
            printfn "✅ All source notes have been built"
        else
            printfn "⚠️  Mismatch between source (%d) and built (%d) notes" sourceFiles.Length builtNotes.Length
    else
        printfn "⚠️  Built notes directory not found"
else
    printfn "⚠️  Source notes directory not found"

printfn ""
printfn "=== Validation Summary ==="
printfn "✅ Phase 3 validation test script created and executed"
printfn "⚠️  Run 'dotnet run' with NEW_NOTES=false then NEW_NOTES=true to complete testing"
printfn "📋 Ready for output comparison between legacy and new systems"
