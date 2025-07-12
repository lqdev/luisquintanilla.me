#r "nuget: FSharp.Data"

open System
open System.IO
open System.Security.Cryptography
open System.Text
open System.Xml.Linq
open System.Diagnostics

// Test configuration
let outputDirLegacy = Path.Join(__SOURCE_DIRECTORY__, "_public_legacy_test")
let outputDirNew = Path.Join(__SOURCE_DIRECTORY__, "_public_new_test")
let currentPublicDir = Path.Join(__SOURCE_DIRECTORY__, "_public")

// Helper functions
let calculateFileHash (filePath: string) =
    if File.Exists(filePath) then
        use sha256 = SHA256.Create()
        use stream = File.OpenRead(filePath)
        let hash = sha256.ComputeHash(stream)
        BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()
    else
        "FILE_NOT_FOUND"

let runBuildWithEnvironment (envVarValue: string option) (outputDir: string) =
    try
        // Set environment variable
        match envVarValue with
        | Some value -> Environment.SetEnvironmentVariable("NEW_NOTES", value)
        | None -> Environment.SetEnvironmentVariable("NEW_NOTES", null)
        
        // Create process to run dotnet run
        let startInfo = ProcessStartInfo()
        startInfo.FileName <- "dotnet"
        startInfo.Arguments <- "run"
        startInfo.WorkingDirectory <- __SOURCE_DIRECTORY__
        startInfo.UseShellExecute <- false
        startInfo.RedirectStandardOutput <- true
        startInfo.RedirectStandardError <- true
        startInfo.CreateNoWindow <- true
        
        // Copy environment variables and set NEW_NOTES
        let envVars = Environment.GetEnvironmentVariables()
        for entry in envVars do
            let kvp = entry :?> System.Collections.DictionaryEntry
            let key = kvp.Key :?> string
            let value = kvp.Value :?> string
            if key <> "NEW_NOTES" then
                startInfo.EnvironmentVariables.[key] <- value
        
        match envVarValue with
        | Some value -> startInfo.EnvironmentVariables.["NEW_NOTES"] <- value
        | None -> ()
        
        use buildProcess = Process.Start(startInfo)
        let output = buildProcess.StandardOutput.ReadToEnd()
        let error = buildProcess.StandardError.ReadToEnd()
        buildProcess.WaitForExit()
        
        if buildProcess.ExitCode = 0 then
            // Copy output to test directory
            if Directory.Exists(currentPublicDir) then
                if Directory.Exists(outputDir) then
                    Directory.Delete(outputDir, true)
                Directory.CreateDirectory(outputDir) |> ignore
                
                // Copy all files
                let copyDirectory source target =
                    let rec copyRecursive sourceDir targetDir =
                        if not (Directory.Exists(targetDir)) then
                            Directory.CreateDirectory(targetDir) |> ignore
                        
                        // Copy files
                        Directory.GetFiles(sourceDir)
                        |> Array.iter (fun file ->
                            let fileName = Path.GetFileName(file)
                            let targetFile = Path.Combine(targetDir, fileName)
                            File.Copy(file, targetFile, true))
                        
                        // Copy subdirectories
                        Directory.GetDirectories(sourceDir)
                        |> Array.iter (fun dir ->
                            let dirName = Path.GetFileName(dir)
                            let targetSubDir = Path.Combine(targetDir, dirName)
                            copyRecursive dir targetSubDir)
                    
                    copyRecursive source target
                
                copyDirectory currentPublicDir outputDir
                printfn "✅ Build successful, output copied to %s" outputDir
                true
            else
                printfn "❌ Public directory not found after build"
                false
        else
            printfn "❌ Build failed (exit code %d)" buildProcess.ExitCode
            printfn "Error: %s" error
            false
    with
    | ex ->
        printfn "❌ Build process failed: %s" ex.Message
        false

let compareNotesOutput (legacyDir: string) (newDir: string) =
    let legacyFeedDir = Path.Join(legacyDir, "feed")
    let newFeedDir = Path.Join(newDir, "feed")
    
    if Directory.Exists(legacyFeedDir) && Directory.Exists(newFeedDir) then
        printfn "📊 Comparing notes output between legacy and new systems..."
        
        // Compare index pages
        let legacyIndex = Path.Join(legacyFeedDir, "index.html")
        let newIndex = Path.Join(newFeedDir, "index.html")
        
        if File.Exists(legacyIndex) && File.Exists(newIndex) then
            let legacyHash = calculateFileHash legacyIndex
            let newHash = calculateFileHash newIndex
            if legacyHash = newHash then
                printfn "✅ Feed index pages identical"
            else
                printfn "⚠️  Feed index pages differ"
                printfn "   Legacy hash: %s" legacyHash
                printfn "   New hash: %s" newHash
        else
            printfn "❌ One or both index pages missing"
        
        // Compare RSS feeds
        let legacyRss = Path.Join(legacyFeedDir, "index.xml")
        let newRss = Path.Join(newFeedDir, "index.xml")
        
        if File.Exists(legacyRss) && File.Exists(newRss) then
            let legacyHash = calculateFileHash legacyRss
            let newHash = calculateFileHash newRss
            if legacyHash = newHash then
                printfn "✅ RSS feeds identical"
            else
                printfn "⚠️  RSS feeds differ"
                printfn "   Legacy hash: %s" legacyHash
                printfn "   New hash: %s" newHash
                
                // Analyze RSS differences
                try
                    let legacyDoc = XDocument.Load(legacyRss)
                    let newDoc = XDocument.Load(newRss)
                    let legacyItems = legacyDoc.Descendants(XName.Get "item") |> Seq.length
                    let newItems = newDoc.Descendants(XName.Get "item") |> Seq.length
                    printfn "   Legacy RSS items: %d" legacyItems
                    printfn "   New RSS items: %d" newItems
                with
                | ex -> printfn "   Error analyzing RSS: %s" ex.Message
        else
            printfn "❌ One or both RSS feeds missing"
        
        // Compare individual note directories
        let legacyNotes = Directory.GetDirectories(legacyFeedDir) 
                         |> Array.filter (fun d -> Path.GetFileName(d) <> "responses")
                         |> Array.sort
        let newNotes = Directory.GetDirectories(newFeedDir) 
                      |> Array.filter (fun d -> Path.GetFileName(d) <> "responses")
                      |> Array.sort
        
        printfn "   Legacy notes: %d directories" legacyNotes.Length
        printfn "   New notes: %d directories" newNotes.Length
        
        if legacyNotes.Length = newNotes.Length then
            printfn "✅ Same number of note directories"
            
            // Sample comparison of first few notes
            let sampleSize = min 5 legacyNotes.Length
            let mutable matchingNotes = 0
            
            for i in 0 .. sampleSize - 1 do
                let legacyNoteName = Path.GetFileName(legacyNotes.[i])
                let newNoteName = Path.GetFileName(newNotes.[i])
                
                if legacyNoteName = newNoteName then
                    let legacyNoteFile = Path.Join(legacyNotes.[i], "index.html")
                    let newNoteFile = Path.Join(newNotes.[i], "index.html")
                    
                    if File.Exists(legacyNoteFile) && File.Exists(newNoteFile) then
                        let legacyHash = calculateFileHash legacyNoteFile
                        let newHash = calculateFileHash newNoteFile
                        if legacyHash = newHash then
                            matchingNotes <- matchingNotes + 1
                            printfn "   ✅ %s identical" legacyNoteName
                        else
                            printfn "   ⚠️  %s differs" legacyNoteName
                    else
                        printfn "   ❌ %s missing index.html" legacyNoteName
                else
                    printfn "   ❌ Note name mismatch: %s vs %s" legacyNoteName newNoteName
            
            printfn "   📈 Sample validation: %d/%d notes identical" matchingNotes sampleSize
        else
            printfn "⚠️  Different number of note directories"
    else
        printfn "❌ One or both feed directories missing"

// Main validation process
printfn "=== Notes Migration Output Comparison Test ==="
printfn "Date: %s" (DateTime.Now.ToString("yyyy-MM-dd HH:mm"))
printfn ""

printfn "🔄 Step 1: Building with legacy system (NEW_NOTES=false)..."
let legacySuccess = runBuildWithEnvironment (Some "false") outputDirLegacy

printfn ""
printfn "🔄 Step 2: Building with new system (NEW_NOTES=true)..."
let newSuccess = runBuildWithEnvironment (Some "true") outputDirNew

printfn ""
if legacySuccess && newSuccess then
    printfn "🔍 Step 3: Comparing outputs..."
    compareNotesOutput outputDirLegacy outputDirNew
    
    printfn ""
    printfn "=== Output Comparison Summary ==="
    printfn "✅ Both legacy and new systems built successfully"
    printfn "📋 Output comparison completed"
    printfn "📁 Legacy output: %s" outputDirLegacy
    printfn "📁 New output: %s" outputDirNew
else
    printfn "❌ One or both builds failed - cannot compare outputs"
    if not legacySuccess then printfn "   Legacy build failed"
    if not newSuccess then printfn "   New build failed"

// Cleanup environment
Environment.SetEnvironmentVariable("NEW_NOTES", null)
printfn ""
printfn "🧹 Environment reset to default"
