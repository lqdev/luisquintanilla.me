#!/usr/bin/env dotnet fsi

// Fix Content Type References Script
// Fixes links that reference /notes/ but should be /responses/

open System
open System.IO
open System.Text.RegularExpressions

let sourceDirectory = "_src"

// Define misclassified content mappings (notes -> responses)
let contentTypeMappings = [
    // Content that exists as responses but is referenced as notes
    ("/notes/bluesky-rss-support/", "/responses/bluesky-rss-support/")
    ("/notes/cosmopedia-ai-synthetic-dataset/", "/responses/cosmopedia-ai-synthetic-dataset/")
    ("/notes/decoder-nilay-patel-why-websites-are-the-future/", "/responses/decoder-nilay-patel-why-websites-are-the-future/")
    ("/notes/marvin-gaye-whats-going-on/", "/responses/marvin-gaye-whats-going-on/")
    ("/notes/phi-2-huggingface/", "/responses/phi-2-huggingface/")
    ("/notes/shipping-wordpress-tumblr/", "/responses/shipping-wordpress-tumblr/")
    ("/notes/tumblr-still-working-fediverse-integration/", "/responses/tumblr-still-working-fediverse-integration/")
    ("/notes/verge-ai-robots-txt/", "/responses/verge-ai-robots-txt/")
    ("/notes/well-known-feeds/", "/responses/well-known-feeds/")
    ("/notes/why-you-should-make-a-website-luvstarkei/", "/responses/why-you-should-make-a-website-luvstarkei/")
    ("/notes/willison-tools-colophon/", "/responses/willison-tools-colophon/")
    ("/notes/windows-12-mobile-concept/", "/responses/windows-12-mobile-concept/")
    ("/notes/windows-subsystem-for-android-end-of-support-2025/", "/responses/windows-subsystem-for-android-end-of-support-2025/")
    ("/notes/wix-website-builder-ai-chatbot/", "/responses/wix-website-builder-ai-chatbot/")
    ("/notes/year-of-curiosity/", "/responses/year-of-curiosity/")
    ("/notes/year-of-linux-desktop-4-percent/", "/responses/year-of-linux-desktop-4-percent/")
    ("/notes/you-should-be-using-rss-reader-pluralistic/", "/responses/you-should-be-using-rss-reader-pluralistic/")
    ("/notes/your-site-is-a-home-hamid/", "/responses/your-site-is-a-home-hamid/")
    ("/notes/zero-search-alibaba/", "/responses/zero-search-alibaba/")
]

let processFile filePath =
    try
        let content = File.ReadAllText(filePath)
        let mutable updatedContent = content
        let mutable changesCount = 0
        
        for (notesPattern, responsesPattern) in contentTypeMappings do
            let beforeCount = updatedContent.Length
            updatedContent <- updatedContent.Replace(notesPattern, responsesPattern)
            let afterCount = updatedContent.Length
            
            if beforeCount <> afterCount then
                let replacementCount = (beforeCount - afterCount) / (notesPattern.Length - responsesPattern.Length)
                if replacementCount > 0 then
                    changesCount <- changesCount + replacementCount
                    printfn "  ✅ %s → %s (%d replacements)" notesPattern responsesPattern replacementCount
        
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

printfn "🔄 Fixing Content Type References (Notes → Responses)"
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
printfn "✅ CONTENT TYPE REFERENCE FIXES COMPLETED"
printfn "📊 Files processed: %d/%d" processedFiles markdownFiles.Length  
printfn "🔧 Total replacements: %d" totalChanges
printfn ""

if totalChanges > 0 then
    printfn "🎯 Benefits:"
    printfn "• Fixed broken links by correcting content type classification"
    printfn "• Links now point to existing /responses/ content instead of missing /notes/"
    printfn "• Improved content discoverability and navigation"
    printfn "• Reduced 404 errors from misclassified content references"
    printfn ""
    printfn "🎯 Next Steps:"
    printfn "1. Build website: dotnet run"
    printfn "2. Test corrected links work properly" 
    printfn "3. Run link analysis to verify broken link reduction"
else
    printfn "✅ No content type reference issues found - all links correctly classified"
