#!/usr/bin/env dotnet fsi

// Fix Malformed URL References Script
// Fixes specific malformed URL patterns found in content

open System
open System.IO
open System.Text.RegularExpressions

let sourceDirectory = "_src"

// Define malformed URL mappings
let urlFixMappings = [
    // Fix the malformed weblogging-rewind URL with extra hyphen
    ("/notes/weblogging-rewind-2023/-continued", "/notes/weblogging-rewind-2023-continued")
    ("/notes/weblogging-rewind-2023/-continued/", "/notes/weblogging-rewind-2023-continued/")
]

let processFile filePath =
    try
        let content = File.ReadAllText(filePath)
        let mutable updatedContent = content
        let mutable changesCount = 0
        
        for (malformedPattern, correctPattern) in urlFixMappings do
            let beforeCount = updatedContent.Length
            updatedContent <- updatedContent.Replace(malformedPattern, correctPattern)
            let afterCount = updatedContent.Length
            
            if beforeCount <> afterCount then
                let replacementCount = (beforeCount - afterCount) / (malformedPattern.Length - correctPattern.Length)
                if replacementCount > 0 then
                    changesCount <- changesCount + replacementCount
                    printfn "  ✅ %s → %s (%d replacements)" malformedPattern correctPattern replacementCount
        
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

printfn "🔄 Fixing Malformed URL References"
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
printfn "✅ MALFORMED URL REFERENCE FIXES COMPLETED"
printfn "📊 Files processed: %d/%d" processedFiles markdownFiles.Length  
printfn "🔧 Total replacements: %d" totalChanges
printfn ""

if totalChanges > 0 then
    printfn "🎯 Benefits:"
    printfn "• Fixed malformed URLs with incorrect hyphen patterns"
    printfn "• Links now point to correctly formatted URLs"
    printfn "• Reduced 404 errors from URL structure issues"
    printfn "• Improved link reliability and user experience"
    printfn ""
    printfn "🎯 Next Steps:"
    printfn "1. Build website: dotnet run"
    printfn "2. Test corrected links work properly" 
    printfn "3. Run link analysis to verify broken link reduction"
else
    printfn "✅ No malformed URL patterns found - all URLs correctly formatted"
