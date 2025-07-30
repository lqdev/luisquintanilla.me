#!/usr/bin/env dotnet fsi

// Convert Absolute Domain Links to Relative Links Script
// Fixes absolute links to lqdev.me and luisquintanilla.me to be relative

open System
open System.IO
open System.Text.RegularExpressions

let sourceDirectory = "_src"

// Define domain-to-relative mappings
let domainMappings = [
    // lqdev.me domain conversions
    ("https://www.lqdev.me/", "/")
    ("https://lqdev.me/", "/")
    
    // luisquintanilla.me domain conversions  
    ("https://www.luisquintanilla.me/", "/")
    ("https://luisquintanilla.me/", "/")
]

let processFile filePath =
    try
        let content = File.ReadAllText(filePath)
        let mutable updatedContent = content
        let mutable changesCount = 0
        
        for (absolutePattern, relativePattern) in domainMappings do
            let beforeCount = updatedContent.Length
            updatedContent <- updatedContent.Replace(absolutePattern, relativePattern)
            let afterCount = updatedContent.Length
            
            if beforeCount <> afterCount then
                let replacementCount = (beforeCount - afterCount) / (absolutePattern.Length - relativePattern.Length)
                if replacementCount > 0 then
                    changesCount <- changesCount + replacementCount
                    printfn "  ✅ %s → %s (%d replacements)" absolutePattern relativePattern replacementCount
        
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

printfn "🔄 Converting Absolute Domain Links to Relative Links"
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
printfn "✅ ABSOLUTE TO RELATIVE CONVERSION COMPLETED"
printfn "📊 Files processed: %d/%d" processedFiles markdownFiles.Length  
printfn "🔧 Total replacements: %d" totalChanges
printfn ""

if totalChanges > 0 then
    printfn "🎯 Benefits:"
    printfn "• Fixed 404 errors caused by domain/deployment mismatches"
    printfn "• Links now work regardless of domain (lqdev.me vs luisquintanilla.me)"
    printfn "• Better for local testing and development"
    printfn "• More portable and maintainable"
    printfn ""
    printfn "🎯 Next Steps:"
    printfn "1. Build website: dotnet run"
    printfn "2. Test relative links work correctly"
    printfn "3. Run link analysis to verify 404 reduction"
else
    printfn "✅ No absolute domain links found - all links already relative"
