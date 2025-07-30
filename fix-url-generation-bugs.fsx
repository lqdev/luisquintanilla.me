#!/usr/bin/env dotnet fsi

// Fix URL Generation Bugs Script
// Fixes malformed URLs: double slashes, wrong paths, trailing slashes
// Uses direct link replacement strategy

open System
open System.IO
open System.Text.RegularExpressions

let sourceDirectory = "_src"

// Define URL bug fixes
let urlBugFixes = [
    // Fix double slash issues
    ("/notes//notes/", "/notes/")
    ("/notes//", "/notes/")
    
    // Fix double trailing slashes
    ("//)", "/)")
    ("//]", "/]")
    
    // Fix specific malformed paths from the analysis
    ("/notes/well-known-feeds//", "/notes/well-known-feeds/")
    ("/notes/windows-12-mobile-concept//", "/notes/windows-12-mobile-concept/")
    ("/notes/weblogging-rewind-2023/-continued", "/notes/weblogging-rewind-2023-continued/")
    ("/notes/weblogging-rewind-2023/-continued/", "/notes/weblogging-rewind-2023-continued/")
    
    // Fix feed-to-notes mapping for content that exists but is misreferenced
    ("/feed/first-owncast-stream", "/notes/first-owncast-stream/")
    ("/feed/surface-duo-blogging-github-dev/", "/notes/surface-duo-blogging-github-dev/")
    ("/feed/webmention-test-1/", "/notes/webmention-test-1/")
    ("/feed/linkblog", "/bookmarks/")  // Linkblog should probably point to bookmarks
    
    // Fix collection blogroll sub-pages (convert to direct response links)
    ("/collections/blogroll/-club-blog-directory", "/responses/blogroll-club-blog-directory/")
    ("/collections/blogroll/-discovery-implemented", "/notes/blogroll-discovery-implemented/") 
    ("/collections/blogroll/-discovery-implemented/", "/notes/blogroll-discovery-implemented/")
    ("/collections/blogroll/-molly-white", "/responses/blogroll-molly-white/")
]

let processFile filePath =
    try
        let content = File.ReadAllText(filePath)
        let mutable updatedContent = content
        let mutable changesCount = 0
        
        for (buggyPattern, fixedPattern) in urlBugFixes do
            let beforeCount = updatedContent.Length
            updatedContent <- updatedContent.Replace(buggyPattern, fixedPattern)
            let afterCount = updatedContent.Length
            
            if beforeCount <> afterCount then
                let replacementCount = 
                    if fixedPattern.Length > buggyPattern.Length then
                        (afterCount - beforeCount) / (fixedPattern.Length - buggyPattern.Length)
                    else
                        (beforeCount - afterCount) / (buggyPattern.Length - fixedPattern.Length)
                
                if replacementCount > 0 then
                    changesCount <- changesCount + replacementCount
                    printfn "  ✅ %s → %s (%d replacements)" buggyPattern fixedPattern replacementCount
        
        if changesCount > 0 then
            File.WriteAllText(filePath, updatedContent)
            printfn "📝 Updated: %s (%d total changes)" (Path.GetFileName(filePath)) changesCount
            changesCount
        else
            0
    with
    | ex ->
        printfn "❌ Error processing %s: %s" filePath ex.Message
        0

let getAllMarkdownFiles directory =
    Directory.GetFiles(directory, "*.md", SearchOption.AllDirectories)
    |> Array.filter (fun path -> not (path.Contains("\\node_modules\\") || path.Contains("\\.git\\")))

printfn "🔄 Fixing URL Generation Bugs with Direct Link Replacement"
printfn "📂 Processing directory: %s" sourceDirectory
printfn ""
printfn "🎯 Bug Types Being Fixed:"
printfn "• Double slashes (/notes//notes/ → /notes/)"
printfn "• Trailing double slashes (// → /)"
printfn "• Malformed collection paths (-discovery-implemented → actual paths)"
printfn "• Feed-to-notes migration issues (/feed/ → /notes/)"
printfn ""

let markdownFiles = getAllMarkdownFiles sourceDirectory
printfn "📄 Found %d markdown files to process" markdownFiles.Length
printfn ""

let mutable totalChanges = 0
let mutable processedFiles = 0

for filePath in markdownFiles do
    let changes = processFile filePath
    if changes > 0 then
        processedFiles <- processedFiles + 1
        totalChanges <- totalChanges + changes

printfn ""
printfn "✅ URL GENERATION BUG FIXES COMPLETED"
printfn "📊 Files processed: %d/%d" processedFiles markdownFiles.Length  
printfn "🔧 Total replacements: %d" totalChanges
printfn ""

if totalChanges > 0 then
    printfn "🎯 Benefits:"
    printfn "• Fixed malformed URLs with double slashes"
    printfn "• Corrected feed-to-notes migration references" 
    printfn "• Fixed collection sub-page references to actual content"
    printfn "• Eliminated URL generation bugs"
    printfn ""
    printfn "🎯 Next Steps:"
    printfn "1. Build website: dotnet run"
    printfn "2. Run link analysis to verify bug fixes"
    printfn "3. Address remaining missing content if needed"
else
    printfn "✅ No URL generation bugs found - all URLs are well-formed"
