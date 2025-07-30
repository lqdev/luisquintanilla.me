#!/usr/bin/env dotnet fsi

// Direct Link Replacement Script
// Replaces collection shortcuts and legacy .html links directly in source files
// This is more efficient than redirects for internal links

open System
open System.IO
open System.Text.RegularExpressions

let sourceDirectory = "_src"

// Define replacement mappings
let replacements = [
    // Collection Navigation Shortcuts - Direct URL replacement
    ("/blogroll", "/collections/blogroll/")
    ("/podroll", "/collections/podroll/")
    
    // Legacy .html Extensions - Direct URL replacement  
    ("/posts/how-to-watch-twitch-using-vlc.html", "/posts/how-to-watch-twitch-using-vlc/")
    ("inspect-mlnet-models-netron.html", "/posts/inspect-mlnet-models-netron/")  
    ("vs-automate-mlnet-schema-generation.html", "/posts/vs-automate-mlnet-schema-generation/")
    ("/posts/inspect-mlnet-models-netron.html", "/posts/inspect-mlnet-models-netron/")
    ("/posts/vs-automate-mlnet-schema-generation.html", "/posts/vs-automate-mlnet-schema-generation/")
    ("/posts/rediscovering-rss-user-freedom.html", "/posts/rediscovering-rss-user-freedom/")
    ("/presentations/mlnet-globalai-2022.html", "/resources/presentations/mlnet-globalai-2022/")
]

let processFile filePath =
    try
        let content = File.ReadAllText(filePath)
        let mutable updatedContent = content
        let mutable changesCount = 0
        
        for (oldPattern, newPattern) in replacements do
            let beforeCount = updatedContent.Length
            updatedContent <- updatedContent.Replace(oldPattern, newPattern)
            let afterCount = updatedContent.Length
            
            if beforeCount <> afterCount then
                let replacementCount = (beforeCount - afterCount + newPattern.Length - oldPattern.Length) / (newPattern.Length - oldPattern.Length)
                if replacementCount > 0 then
                    changesCount <- changesCount + replacementCount
                    printfn "  ✅ %s → %s (%d replacements)" oldPattern newPattern replacementCount
        
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

printfn "🔄 Direct Link Replacement Script"
printfn "📂 Processing directory: %s" sourceDirectory
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
printfn "✅ DIRECT REPLACEMENT COMPLETED"
printfn "📊 Files processed: %d/%d" processedFiles markdownFiles.Length  
printfn "🔧 Total replacements: %d" totalChanges
printfn ""

if totalChanges > 0 then
    printfn "🎯 Next Steps:"
    printfn "1. Build website: dotnet run"
    printfn "2. Test replaced links work correctly"
    printfn "3. Run link analysis to verify broken link reduction"
else
    printfn "✅ No replacements needed - links already correct"
