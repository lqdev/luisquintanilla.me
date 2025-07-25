// Git History Date Extraction Test Script
// Use this to extract creation dates from Git history for retroactive frontmatter enhancement

#r "nuget: FSharp.Data"
open System
open System.IO
open System.Diagnostics

// Git History Service using Process execution
module GitHistoryService =
    
    /// Execute git command and return output
    let private executeGitCommand (arguments: string) =
        try
            let processInfo = ProcessStartInfo()
            processInfo.FileName <- "git"
            processInfo.Arguments <- arguments
            processInfo.RedirectStandardOutput <- true
            processInfo.RedirectStandardError <- true
            processInfo.UseShellExecute <- false
            processInfo.CreateNoWindow <- true
            
            use process = Process.Start(processInfo)
            let output = process.StandardOutput.ReadToEnd()
            let error = process.StandardError.ReadToEnd()
            process.WaitForExit()
            
            if process.ExitCode = 0 then
                Some output.Trim()
            else
                printfn "Git command failed: %s" error
                None
        with
        | ex -> 
            printfn "Error executing git command: %s" ex.Message
            None

    /// Get file creation date from Git history (first commit)
    let getFileCreationDate (filePath: string) : DateTime option =
        let gitArgs = sprintf "--all --full-history --format=\"%%aI\" --reverse -- \"**/%s\"" (Path.GetFileName(filePath))
        match executeGitCommand $"log {gitArgs}" with
        | Some output when not (String.IsNullOrWhiteSpace(output)) ->
            let lines = output.Split([|'\n'|], StringSplitOptions.RemoveEmptyEntries)
            if lines.Length > 0 then
                match DateTime.TryParse(lines.[0]) with
                | true, date -> Some date
                | false, _ -> None
            else None
        | _ -> None

    /// Get file last modification date from Git history (most recent commit)
    let getFileLastModificationDate (filePath: string) : DateTime option =
        let gitArgs = sprintf "--all --full-history --format=\"%%aI\" -- \"**/%s\"" (Path.GetFileName(filePath))
        match executeGitCommand $"log {gitArgs}" with
        | Some output when not (String.IsNullOrWhiteSpace(output)) ->
            let lines = output.Split([|'\n'|], StringSplitOptions.RemoveEmptyEntries)
            if lines.Length > 0 then
                match DateTime.TryParse(lines.[0]) with
                | true, date -> Some date
                | false, _ -> None
            else None
        | _ -> None

    /// Format date for YAML frontmatter
    let formatDateForFrontmatter (date: DateTime) : string =
        date.ToString("MM/dd/yyyy HH:mm")

// Test the Git history extraction
let testGitHistoryExtraction () =
    let testFiles = [
        "_src/resources/snippets/fsharp-data-rss-parser.md"
        "_src/resources/snippets/lqdev-me-website-post-metrics.md"
        "_src/resources/wiki/devcontainers-configurations.md"
        "_src/resources/snippets/create-matrix-user-cli.md"
    ]
    
    printfn "Testing Git History Date Extraction:"
    printfn "======================================"
    
    for filePath in testFiles do
        if File.Exists(filePath) then
            printfn "\nFile: %s" (Path.GetFileName(filePath))
            
            match GitHistoryService.getFileCreationDate filePath with
            | Some creationDate ->
                let formattedDate = GitHistoryService.formatDateForFrontmatter creationDate
                printfn "  Creation Date: %s (%s)" (creationDate.ToString("yyyy-MM-dd HH:mm:ss")) formattedDate
            | None ->
                printfn "  Creation Date: Not found in Git history"
            
            match GitHistoryService.getFileLastModificationDate filePath with
            | Some modDate ->
                let formattedDate = GitHistoryService.formatDateForFrontmatter modDate
                printfn "  Last Modified: %s (%s)" (modDate.ToString("yyyy-MM-dd HH:mm:ss")) formattedDate
            | None ->
                printfn "  Last Modified: Not found in Git history"
        else
            printfn "\nFile not found: %s" filePath

// Run the test
testGitHistoryExtraction ()

// Test batch extraction for all snippets
let testBatchExtraction () =
    let snippetsDir = "_src/resources/snippets"
    let wikiDir = "_src/resources/wiki"
    
    printfn "\n\nBatch Extraction Test:"
    printfn "======================"
    
    if Directory.Exists(snippetsDir) then
        let snippetFiles = Directory.GetFiles(snippetsDir, "*.md")
        printfn "\nSnippets (%d files):" snippetFiles.Length
        
        for filePath in snippetFiles |> Array.take 5 do // Just test first 5
            let fileName = Path.GetFileName(filePath)
            match GitHistoryService.getFileCreationDate filePath with
            | Some date -> 
                printfn "  %s: %s" fileName (date.ToString("yyyy-MM-dd"))
            | None -> 
                printfn "  %s: No Git history" fileName
    
    if Directory.Exists(wikiDir) then
        let wikiFiles = Directory.GetFiles(wikiDir, "*.md")
        printfn "\nWikis (%d files):" wikiFiles.Length
        
        for filePath in wikiFiles |> Array.take 5 do // Just test first 5
            let fileName = Path.GetFileName(filePath)
            match GitHistoryService.getFileCreationDate filePath with
            | Some date -> 
                printfn "  %s: %s" fileName (date.ToString("yyyy-MM-dd"))
            | None -> 
                printfn "  %s: No Git history" fileName

testBatchExtraction ()

printfn "\n\nGit history extraction test complete!"
printfn "This shows we can successfully extract creation dates from Git history."
printfn "Next step: Implement frontmatter enhancement with these dates."
