#!/usr/bin/env dotnet fsi

// Content Audit Script - Analyze content types in feed and responses directories
// This script will scan frontmatter to identify misplaced content

open System
open System.IO
open System.Text.RegularExpressions

let srcDir = @"c:\Dev\website\_src"
let feedDir = Path.Join(srcDir, "feed")
let responsesDir = Path.Join(srcDir, "responses")

// Extract frontmatter from markdown file
let extractFrontmatter (filePath: string) =
    try
        let content = File.ReadAllText(filePath)
        let frontmatterPattern = @"^---\s*\n(.*?)\n---"
        let regexMatch = Regex.Match(content, frontmatterPattern, RegexOptions.Singleline)
        if regexMatch.Success then
            Some regexMatch.Groups.[1].Value
        else
            None
    with
    | ex -> 
        printfn $"Error reading {filePath}: {ex.Message}"
        None

// Extract specific field from frontmatter
let extractField (frontmatter: string) (fieldName: string) =
    let pattern = $@"{fieldName}:\s*[""']?([^""'\n\r]+)[""']?"
    let regexMatch = Regex.Match(frontmatter, pattern)
    if regexMatch.Success then
        Some (regexMatch.Groups.[1].Value.Trim())
    else
        None

// Audit feed directory for notes
let auditFeedDirectory () =
    printfn "=== FEED DIRECTORY AUDIT ==="
    let feedFiles = Directory.GetFiles(feedDir, "*.md")
    printfn $"Total files in feed directory: {feedFiles.Length}"
    
    let mutable noteCount = 0
    let mutable feedCount = 0
    let mutable otherCount = 0
    let mutable noTypeCount = 0
    
    let noteFiles = ResizeArray<string>()
    let feedFiles = ResizeArray<string>()
    let otherFiles = ResizeArray<string>()
    let noTypeFiles = ResizeArray<string>()
    
    for file in feedFiles do
        let fileName = Path.GetFileName(file)
        match extractFrontmatter file with
        | Some frontmatter ->
            match extractField frontmatter "post_type" with
            | Some postType ->
                match postType.ToLower() with
                | "note" -> 
                    noteCount <- noteCount + 1
                    noteFiles.Add(fileName)
                | "feed" -> 
                    feedCount <- feedCount + 1
                    feedFiles.Add(fileName)
                | other -> 
                    otherCount <- otherCount + 1
                    otherFiles.Add($"{fileName} ({other})")
            | None ->
                noTypeCount <- noTypeCount + 1
                noTypeFiles.Add(fileName)
        | None ->
            noTypeCount <- noTypeCount + 1
            noTypeFiles.Add($"{fileName} (no frontmatter)")
    
    printfn $"Notes (should move to _src/notes/): {noteCount}"
    printfn $"Feed posts (stay in _src/feed/): {feedCount}"
    printfn $"Other post types: {otherCount}"
    printfn $"No post_type specified: {noTypeCount}"
    
    if noteCount > 0 then
        printfn "\n--- Note files to move ---"
        noteFiles |> Seq.take (min 10 noteFiles.Count) |> Seq.iter (printfn "  %s")
        if noteFiles.Count > 10 then printfn $"  ... and {noteFiles.Count - 10} more"
    
    if otherCount > 0 then
        printfn "\n--- Other post types found ---"
        otherFiles |> Seq.iter (printfn "  %s")
    
    noteCount, feedCount, otherCount, noTypeCount

// Audit responses directory for bookmarks
let auditResponsesDirectory () =
    printfn "\n=== RESPONSES DIRECTORY AUDIT ==="
    let responseFiles = Directory.GetFiles(responsesDir, "*.md")
    printfn $"Total files in responses directory: {responseFiles.Length}"
    
    let mutable bookmarkCount = 0
    let mutable replyCount = 0
    let mutable reshareCount = 0
    let mutable starCount = 0
    let mutable otherCount = 0
    let mutable noTypeCount = 0
    
    let bookmarkFiles = ResizeArray<string>()
    let replyFiles = ResizeArray<string>()
    let reshareFiles = ResizeArray<string>()
    let starFiles = ResizeArray<string>()
    let otherFiles = ResizeArray<string>()
    let noTypeFiles = ResizeArray<string>()
    
    for file in responseFiles do
        let fileName = Path.GetFileName(file)
        match extractFrontmatter file with
        | Some frontmatter ->
            match extractField frontmatter "response_type" with
            | Some responseType ->
                match responseType.ToLower() with
                | "bookmark" -> 
                    bookmarkCount <- bookmarkCount + 1
                    bookmarkFiles.Add(fileName)
                | "reply" -> 
                    replyCount <- replyCount + 1
                    replyFiles.Add(fileName)
                | "reshare" -> 
                    reshareCount <- reshareCount + 1
                    reshareFiles.Add(fileName)
                | "star" -> 
                    starCount <- starCount + 1
                    starFiles.Add(fileName)
                | other -> 
                    otherCount <- otherCount + 1
                    otherFiles.Add($"{fileName} ({other})")
            | None ->
                noTypeCount <- noTypeCount + 1
                noTypeFiles.Add(fileName)
        | None ->
            noTypeCount <- noTypeCount + 1
            noTypeFiles.Add($"{fileName} (no frontmatter)")
    
    printfn $"Bookmarks (should move to _src/bookmarks/): {bookmarkCount}"
    printfn $"Replies (stay in _src/responses/): {replyCount}"
    printfn $"Reshares (stay in _src/responses/): {reshareCount}"
    printfn $"Stars (stay in _src/responses/): {starCount}"
    printfn $"Other response types: {otherCount}"
    printfn $"No response_type specified: {noTypeCount}"
    
    if bookmarkCount > 0 then
        printfn "\n--- Bookmark files to move ---"
        bookmarkFiles |> Seq.take (min 10 bookmarkFiles.Count) |> Seq.iter (printfn "  %s")
        if bookmarkFiles.Count > 10 then printfn $"  ... and {bookmarkFiles.Count - 10} more"
    
    if otherCount > 0 then
        printfn "\n--- Other response types found ---"
        otherFiles |> Seq.iter (printfn "  %s")
    
    bookmarkCount, replyCount, reshareCount, starCount, otherCount, noTypeCount

// Main audit execution
let runAudit () =
    printfn "CONTENT STRUCTURE AUDIT"
    printfn "======================"
    printfn $"Audit Date: {DateTime.Now.ToString(\"yyyy-MM-dd HH:mm:ss\")}"
    printfn ""
    
    let (noteCount, feedCount, otherFeedCount, noFeedTypeCount) = auditFeedDirectory()
    let (bookmarkCount, replyCount, reshareCount, starCount, otherResponseCount, noResponseTypeCount) = auditResponsesDirectory()
    
    printfn "\n=== AUDIT SUMMARY ==="
    printfn $"Files to move from _src/feed/ to _src/notes/: {noteCount}"
    printfn $"Files to move from _src/responses/ to _src/bookmarks/: {bookmarkCount}"
    printfn $"Files remaining in _src/feed/: {feedCount + otherFeedCount + noFeedTypeCount}"
    printfn $"Files remaining in _src/responses/: {replyCount + reshareCount + starCount + otherResponseCount + noResponseTypeCount}"
    
    printfn "\n=== NEXT STEPS ==="
    if noteCount > 0 then
        printfn $"1. Create migration script to move {noteCount} note files to _src/notes/"
    if bookmarkCount > 0 then
        printfn $"2. Create migration script to move {bookmarkCount} bookmark files to _src/bookmarks/"
    printfn "3. Update Builder.fs file path references"
    printfn "4. Test build process"

// Run the audit
runAudit()
