module OutputComparison

open System
open System.IO
open System.Security.Cryptography
open System.Text

/// Result of comparing two pieces of content
type ComparisonResult = {
    FilePath: string
    OldExists: bool
    NewExists: bool
    ContentsMatch: bool
    OldHash: string option
    NewHash: string option
    Differences: string list
}

/// Summary of validation results
type ValidationSummary = {
    TotalFiles: int
    MatchingFiles: int
    DifferentFiles: int
    MissingInOld: int
    MissingInNew: int
    Results: ComparisonResult list
}

/// Calculate MD5 hash of file contents
let calculateHash (content: string) =
    use md5 = MD5.Create()
    let bytes = Encoding.UTF8.GetBytes(content)
    let hashBytes = md5.ComputeHash(bytes)
    Convert.ToHexString(hashBytes)

/// Read file content safely
let readFileContent (filePath: string) =
    try
        if File.Exists(filePath) then
            let content = File.ReadAllText(filePath)
            Some content
        else
            None
    with
    | ex -> 
        printfn $"Warning: Could not read file {filePath}: {ex.Message}"
        None

/// Compare two file contents
let compareContents (oldContent: string option) (newContent: string option) =
    match oldContent, newContent with
    | Some old, Some new_ ->
        if old = new_ then
            [], true
        else
            let oldLines = old.Split('\n')
            let newLines = new_.Split('\n')
            let maxLines = max oldLines.Length newLines.Length
            let differences = 
                [0..maxLines-1]
                |> List.choose (fun i ->
                    let oldLine = if i < oldLines.Length then Some oldLines.[i] else None
                    let newLine = if i < newLines.Length then Some newLines.[i] else None
                    match oldLine, newLine with
                    | Some o, Some n when o <> n -> Some $"Line {i+1}: OLD='{o}' NEW='{n}'"
                    | Some o, None -> Some $"Line {i+1}: REMOVED='{o}'"
                    | None, Some n -> Some $"Line {i+1}: ADDED='{n}'"
                    | _ -> None)
                |> List.truncate 10 // Limit to first 10 differences
            differences, false
    | Some _, None -> ["File missing in new output"], false
    | None, Some _ -> ["File missing in old output"], false
    | None, None -> ["Both files missing"], false

/// Compare a single file between old and new output
let compareFile (oldBasePath: string) (newBasePath: string) (relativePath: string) =
    let oldPath = Path.Combine(oldBasePath, relativePath)
    let newPath = Path.Combine(newBasePath, relativePath)
    
    let oldContent = readFileContent oldPath
    let newContent = readFileContent newPath
    
    let differences, contentsMatch = compareContents oldContent newContent
    
    {
        FilePath = relativePath
        OldExists = oldContent.IsSome
        NewExists = newContent.IsSome
        ContentsMatch = contentsMatch
        OldHash = oldContent |> Option.map calculateHash
        NewHash = newContent |> Option.map calculateHash
        Differences = differences
    }

/// Get all files to compare from both directories
let getFilesToCompare (oldBasePath: string) (newBasePath: string) =
    let getRelativeFiles basePath =
        if Directory.Exists(basePath) then
            Directory.GetFiles(basePath, "*", SearchOption.AllDirectories)
            |> Array.map (fun fullPath -> 
                Path.GetRelativePath(basePath, fullPath).Replace('\\', '/'))
            |> Set.ofArray
        else
            Set.empty
    
    let oldFiles = getRelativeFiles oldBasePath
    let newFiles = getRelativeFiles newBasePath
    
    Set.union oldFiles newFiles |> Set.toList

/// Run complete output comparison
let compareOutputs (oldBasePath: string) (newBasePath: string) =
    printfn $"Comparing outputs:"
    printfn $"  Old: {oldBasePath}"
    printfn $"  New: {newBasePath}"
    
    let filesToCompare = getFilesToCompare oldBasePath newBasePath
    printfn $"  Files to compare: {filesToCompare.Length}"
    
    let results = 
        filesToCompare
        |> List.map (compareFile oldBasePath newBasePath)
    
    let matchingFiles = results |> List.filter (fun r -> r.ContentsMatch) |> List.length
    let differentFiles = results |> List.filter (fun r -> not r.ContentsMatch && r.OldExists && r.NewExists) |> List.length
    let missingInOld = results |> List.filter (fun r -> not r.OldExists && r.NewExists) |> List.length
    let missingInNew = results |> List.filter (fun r -> r.OldExists && not r.NewExists) |> List.length
    
    {
        TotalFiles = results.Length
        MatchingFiles = matchingFiles
        DifferentFiles = differentFiles
        MissingInOld = missingInOld
        MissingInNew = missingInNew
        Results = results
    }

/// Print validation summary
let printSummary (summary: ValidationSummary) =
    printfn ""
    printfn "=== Output Comparison Summary ==="
    printfn $"Total files: {summary.TotalFiles}"
    printfn $"✅ Matching: {summary.MatchingFiles}"
    printfn $"❌ Different: {summary.DifferentFiles}"
    printfn $"➕ Missing in old: {summary.MissingInOld}"
    printfn $"➖ Missing in new: {summary.MissingInNew}"
    
    if summary.DifferentFiles > 0 || summary.MissingInOld > 0 || summary.MissingInNew > 0 then
        printfn ""
        printfn "=== Issues Found ==="
        
        summary.Results
        |> List.filter (fun r -> not r.ContentsMatch)
        |> List.iter (fun result ->
            printfn $"📄 {result.FilePath}:"
            if not result.OldExists then
                printfn "    ➕ File only exists in new output"
            elif not result.NewExists then
                printfn "    ➖ File only exists in old output"
            else
                printfn "    ❌ Content differs"
                result.Differences 
                |> List.take (min 5 result.Differences.Length)
                |> List.iter (fun diff -> printfn $"      {diff}")
                if result.Differences.Length > 5 then
                    printfn $"      ... and {result.Differences.Length - 5} more differences"
            printfn "")
    
    let isValid = summary.DifferentFiles = 0 && summary.MissingInOld = 0 && summary.MissingInNew = 0
    let resultText = if isValid then "✅ PASS" else "❌ FAIL"
    printfn $"=== Validation Result: {resultText} ==="
    printfn ""
    
    isValid

/// Validate that old and new outputs are identical
let validateOutputs (oldBasePath: string) (newBasePath: string) =
    let summary = compareOutputs oldBasePath newBasePath
    printSummary summary
