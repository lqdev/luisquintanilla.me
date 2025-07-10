#r "bin/Debug/net9.0/PersonalSite.dll"

open System.IO
open System.Diagnostics

// Test the complete book build process
printfn "=== Testing Complete Book Build Process ==="

// Set up environment
let originalDir = Directory.GetCurrentDirectory()
let outputDir = "_public"
let libraryOutputDir = Path.Combine(outputDir, "library")

printfn "1. Setting up test environment..."
printfn "   Working directory: %s" originalDir
printfn "   Output directory: %s" outputDir

// Clean up any existing library output
if Directory.Exists(libraryOutputDir) then
    printfn "   Cleaning existing library output..."
    Directory.Delete(libraryOutputDir, true)

// Test with NEW_BOOKS=true
printfn "\n2. Testing build with NEW_BOOKS=true..."
let psi = ProcessStartInfo()
psi.FileName <- "dotnet"
psi.Arguments <- "run"
psi.UseShellExecute <- false
psi.RedirectStandardOutput <- true
psi.RedirectStandardError <- true
psi.WorkingDirectory <- originalDir
psi.EnvironmentVariables.["NEW_BOOKS"] <- "true"

let proc = Process.Start(psi)
let output = proc.StandardOutput.ReadToEnd()
let error = proc.StandardError.ReadToEnd()
proc.WaitForExit()

printfn "   Process exit code: %d" proc.ExitCode
if not (System.String.IsNullOrEmpty(error)) then
    printfn "   Errors: %s" error

// Check for book-related output
let bookOutput = 
    output.Split('\n') 
    |> Array.filter (fun line -> 
        line.Contains("book", System.StringComparison.OrdinalIgnoreCase) ||
        line.Contains("library", System.StringComparison.OrdinalIgnoreCase))

if bookOutput.Length > 0 then
    printfn "   Book-related output:"
    bookOutput |> Array.iter (fun line -> printfn "     %s" (line.Trim()))
else
    printfn "   No book-related output found"

// Check generated files
printfn "\n3. Checking generated files..."

if Directory.Exists(libraryOutputDir) then
    printfn "   ✅ Library directory created: %s" libraryOutputDir
    
    // Check for index.html
    let indexPath = Path.Combine(libraryOutputDir, "index.html")
    if File.Exists(indexPath) then
        printfn "   ✅ Library index page created: %s" indexPath
        let indexContent = File.ReadAllText(indexPath)
        printfn "      Size: %d bytes" indexContent.Length
    else
        printfn "   ❌ Library index page not found"
    
    // Check for RSS feed
    let feedPath = Path.Combine(libraryOutputDir, "feed", "index.xml")
    if File.Exists(feedPath) then
        printfn "   ✅ RSS feed created: %s" feedPath
        let feedContent = File.ReadAllText(feedPath)
        printfn "      Size: %d bytes" feedContent.Length
    else
        printfn "   ❌ RSS feed not found"
    
    // Check for individual book pages
    let bookDirs = Directory.GetDirectories(libraryOutputDir)
                   |> Array.filter (fun dir -> not (Path.GetFileName(dir) = "feed"))
    
    printfn "   Book directories found: %d" bookDirs.Length
    if bookDirs.Length > 0 then
        bookDirs 
        |> Array.take (min 3 bookDirs.Length)
        |> Array.iter (fun dir ->
            let bookName = Path.GetFileName(dir)
            let bookIndexPath = Path.Combine(dir, "index.html")
            if File.Exists(bookIndexPath) then
                printfn "   ✅ Book page: %s" bookName
            else
                printfn "   ❌ Book page missing: %s" bookName)
    else
        printfn "   ⚠️  No individual book directories found"
        
else
    printfn "   ❌ Library directory not created"

printfn "\n4. Summary:"
printfn "   Build process completed with exit code: %d" proc.ExitCode
if Directory.Exists(libraryOutputDir) then
    let allFiles = Directory.GetFiles(libraryOutputDir, "*", SearchOption.AllDirectories)
    printfn "   Total files generated: %d" allFiles.Length
    printfn "   Library structure:"
    allFiles 
    |> Array.take (min 10 allFiles.Length)
    |> Array.iter (fun file -> 
        let relativePath = Path.GetRelativePath(outputDir, file)
        printfn "     %s" relativePath)
    if allFiles.Length > 10 then
        printfn "     ... and %d more files" (allFiles.Length - 10)

printfn "\n=== Build Process Test Complete ==="
