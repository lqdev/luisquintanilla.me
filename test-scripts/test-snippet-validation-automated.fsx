// Test snippet output comparison between old and new systems
// This script builds with both systems and compares snippet outputs

#r "../bin/Debug/net9.0/PersonalSite.dll"

open System
open System.IO
open FeatureFlags
open OutputComparison

printfn "=== Snippet Output Validation Test ==="
printfn ""

// Save current environment
let originalNewSnippets = Environment.GetEnvironmentVariable("NEW_SNIPPETS")

// Test configuration - use existing _public directory structure
let publicDir = "_public"
let oldOutputDir = Path.Join(Directory.GetCurrentDirectory(), "_test_validation", "old_snippets")
let newOutputDir = Path.Join(Directory.GetCurrentDirectory(), "_test_validation", "new_snippets")

printfn "Creating backup directories for comparison..."
if Directory.Exists("_test_validation") then Directory.Delete("_test_validation", true)
Directory.CreateDirectory(oldOutputDir) |> ignore
Directory.CreateDirectory(newOutputDir) |> ignore

try
    // Helper function to copy snippet files
    let backupSnippetFiles targetDir =
        let snippetsDir = Path.Join(publicDir, "snippets")
        if Directory.Exists(snippetsDir) then
            let targetSnippetsDir = Path.Join(targetDir, "snippets")
            if Directory.Exists(targetSnippetsDir) then Directory.Delete(targetSnippetsDir, true)
            
            // Copy entire snippets directory
            let copyDirectory source target =
                Directory.CreateDirectory(target) |> ignore
                
                // Copy all files
                Directory.GetFiles(source, "*", SearchOption.AllDirectories)
                |> Array.iter (fun file ->
                    let relativePath = Path.GetRelativePath(source, file)
                    let targetFile = Path.Join(target, relativePath)
                    Directory.CreateDirectory(Path.GetDirectoryName(targetFile)) |> ignore
                    File.Copy(file, targetFile, true)
                )
            
            copyDirectory snippetsDir targetSnippetsDir
            printfn "Backed up snippets to: %s" targetDir
        else
            printfn "No snippets directory found at: %s" snippetsDir

    printfn "=== Step 1: Build with Old System ==="
    Environment.SetEnvironmentVariable("NEW_SNIPPETS", "false")
    printfn "NEW_SNIPPETS set to: false"
    printfn "Feature flag status: %b" (isEnabled ContentType.Snippets)
    
    // Run the program with old system
    let oldProcInfo = System.Diagnostics.ProcessStartInfo()
    oldProcInfo.FileName <- "dotnet"
    oldProcInfo.Arguments <- "run"
    oldProcInfo.UseShellExecute <- false
    oldProcInfo.RedirectStandardOutput <- true
    oldProcInfo.RedirectStandardError <- true
    oldProcInfo.EnvironmentVariables.["NEW_SNIPPETS"] <- "false"
    
    use oldProcess = System.Diagnostics.Process.Start(oldProcInfo)
    oldProcess.WaitForExit()
    
    if oldProcess.ExitCode = 0 then
        printfn "Old system build completed successfully"
        backupSnippetFiles oldOutputDir
    else
        printfn "ERROR: Old system build failed with exit code: %d" oldProcess.ExitCode
        let error = oldProcess.StandardError.ReadToEnd()
        printfn "Error output: %s" error
    
    printfn ""
    printfn "=== Step 2: Build with New System ==="
    Environment.SetEnvironmentVariable("NEW_SNIPPETS", "true")
    printfn "NEW_SNIPPETS set to: true"
    printfn "Feature flag status: %b" (isEnabled ContentType.Snippets)
    
    // Run the program with new system
    let newProcInfo = System.Diagnostics.ProcessStartInfo()
    newProcInfo.FileName <- "dotnet"
    newProcInfo.Arguments <- "run"
    newProcInfo.UseShellExecute <- false
    newProcInfo.RedirectStandardOutput <- true
    newProcInfo.RedirectStandardError <- true
    newProcInfo.EnvironmentVariables.["NEW_SNIPPETS"] <- "true"
    
    use newProcess = System.Diagnostics.Process.Start(newProcInfo)
    newProcess.WaitForExit()
    
    if newProcess.ExitCode = 0 then
        printfn "New system build completed successfully"
        backupSnippetFiles newOutputDir
    else
        printfn "ERROR: New system build failed with exit code: %d" newProcess.ExitCode
        let error = newProcess.StandardError.ReadToEnd()
        printfn "Error output: %s" error
    
    printfn ""
    
    if oldProcess.ExitCode = 0 && newProcess.ExitCode = 0 then
        printfn "=== Step 3: Compare Snippet Outputs ==="
        
        // Find all snippet HTML files in old output
        let oldSnippetsDir = Path.Join(oldOutputDir, "snippets")
        let allSnippetFiles = 
            if Directory.Exists(oldSnippetsDir) then
                Directory.GetFiles(oldSnippetsDir, "*.html", SearchOption.AllDirectories)
                |> Array.map (fun fullPath -> 
                    Path.GetRelativePath(oldOutputDir, fullPath).Replace("\\", "/"))
                |> Array.toList
            else []
        
        printfn "Found %d snippet files to compare" allSnippetFiles.Length
        
        let results = 
            allSnippetFiles
            |> List.map (fun relativePath ->
                let oldFile = Path.Join(oldOutputDir, relativePath.Replace("/", "\\"))
                let newFile = Path.Join(newOutputDir, relativePath.Replace("/", "\\"))
                
                printfn "Comparing: %s" relativePath
                
                if File.Exists(oldFile) && File.Exists(newFile) then
                    let comparison = compareHtmlFiles oldFile newFile
                    (relativePath, Some comparison)
                else
                    printfn "  WARNING: Missing file - Old exists: %b, New exists: %b" 
                        (File.Exists(oldFile)) (File.Exists(newFile))
                    (relativePath, None)
            )
        
        printfn ""
        printfn "=== Detailed Results ==="
        
        // Print results
        results
        |> List.iter (fun (file, comparisonOpt) ->
            match comparisonOpt with
            | Some comparison when comparison.IsIdentical ->
                printfn "✓ IDENTICAL: %s" file
            | Some comparison ->
                printfn "✗ DIFFERENT: %s" file
                printfn "  - Size difference: Old=%d, New=%d" comparison.OldSize comparison.NewSize
                printfn "  - Line count difference: Old=%d, New=%d" comparison.OldLines comparison.NewLines
                if comparison.Differences.Length > 0 then
                    printfn "  - First few differences:"
                    comparison.Differences
                    |> List.take (min 5 comparison.Differences.Length)
                    |> List.iteri (fun i diff -> printfn "    %d. %s" (i+1) diff)
            | None ->
                printfn "⚠ MISSING: %s (file doesn't exist in one or both outputs)" file
        )
        
        let identicalCount = results |> List.filter (fun (_, comp) -> 
            match comp with Some c -> c.IsIdentical | None -> false) |> List.length
        let totalCount = results.Length
        
        printfn ""
        printfn "=== Summary ==="
        printfn "Identical files: %d/%d" identicalCount totalCount
        
        if identicalCount = totalCount && totalCount > 0 then
            printfn "🎉 SUCCESS: All snippet outputs are identical!"
            printfn "Migration validation PASSED - new system produces identical output"
        else
            printfn "❌ FAILURE: Output differences detected or no files to compare"
            printfn "Migration validation FAILED - manual review required"
    else
        printfn "❌ Cannot compare outputs due to build failures"

finally
    printfn ""
    printfn "=== Cleanup ==="
    
    // Restore original environment
    match originalNewSnippets with
    | null -> Environment.SetEnvironmentVariable("NEW_SNIPPETS", null)
    | value -> Environment.SetEnvironmentVariable("NEW_SNIPPETS", value)
    
    printfn "Environment variables restored"
    printfn "Test completed - comparison files remain in: _test_validation/"
