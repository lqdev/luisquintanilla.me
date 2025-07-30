#!/usr/bin/env dotnet fsi

// Script to fix legacy /feed/ links in source content files
// This addresses broken links by updating source markdown files directly

open System
open System.IO
open System.Text.RegularExpressions

// Define mapping from legacy /feed/ URLs to new architecture
let feedLinkMappings = [
    // Collections
    ("/feed/blogroll", "/collections/blogroll/")
    ("/feed/podroll", "/collections/podroll/")
    ("/feed/starter", "/collections/starter-packs/")
    ("/feed/forums", "/collections/forums/")
    ("/feed/youtube", "/collections/youtube/")
    
    // Weekly summaries (posts)
    ("/feed/2024-03-10-weekly-post-summary", "/posts/2024-03-10-weekly-post-summary/")
    ("/feed/2024-03-18-weekly-post-summary", "/posts/2024-03-18-weekly-post-summary/")
    ("/feed/2024-03-24-weekly-post-summary", "/posts/2024-03-24-weekly-post-summary/")
    ("/feed/2024-04-01-weekly-post-summary", "/posts/2024-04-01-weekly-post-summary/")
    ("/feed/2024-04-07-weekly-post-summary", "/posts/2024-04-07-weekly-post-summary/")
    ("/feed/2024-04-14-weekly-post-summary", "/posts/2024-04-14-weekly-post-summary/")
    ("/feed/2024-04-29-weekly-post-summary", "/posts/2024-04-29-weekly-post-summary/")
    ("/feed/2024-05-06-weekly-post-summary", "/posts/2024-05-06-weekly-post-summary/")
    
    // Content posts/notes/responses
    ("/feed/1-percent-rule", "/posts/1-percent-rule/")
    ("/feed/1mb-club", "/posts/1mb-club/")
    ("/feed/2022-04-21-assorted-links", "/posts/2022-04-21-assorted-links/")
    ("/feed/2022-state-of-ai-report", "/posts/2022-state-of-ai-report/")
    ("/feed/2023-goals-accomplishments-purple-life", "/posts/2023-goals-accomplishments-purple-life/")
    ("/feed/2023-social-media-case-fediverse", "/posts/2023-social-media-case-fediverse/")
    ("/feed/2023-state-of-ai-report", "/posts/2023-state-of-ai-report/")
    ("/feed/bluesky-rss-support", "/notes/bluesky-rss-support/")
    ("/feed/cosmopedia-ai-synthetic-dataset", "/notes/cosmopedia-ai-synthetic-dataset/")
    ("/feed/phi-2-huggingface", "/notes/phi-2-huggingface/")
    ("/feed/verge-ai-robots-txt", "/notes/verge-ai-robots-txt/")
    ("/feed/decoder-nilay-patel-why-websites-are-the-future", "/notes/decoder-nilay-patel-why-websites-are-the-future/")
    ("/feed/farewell-privacy-security-osint-podcast", "/responses/farewell-privacy-security-osint-podcast/")
    ("/feed/floss-weekly-ends-twit-start-of-an-era", "/responses/floss-weekly-ends-twit-start-of-an-era/")
    ("/feed/shipping-wordpress-tumblr", "/notes/shipping-wordpress-tumblr/")
    ("/feed/tumblr-still-working-fediverse-integration", "/notes/tumblr-still-working-fediverse-integration/")
    ("/feed/windows-12-mobile-concept", "/notes/windows-12-mobile-concept/")
]

let srcDirectory = "c:\\Dev\\website\\_src"

let processFile (filePath: string) =
    if File.Exists(filePath) && filePath.EndsWith(".md") then
        let content = File.ReadAllText(filePath)
        let mutable updatedContent = content
        let mutable changesCount = 0
        
        // Apply all mappings
        for (oldUrl, newUrl) in feedLinkMappings do
            let pattern = Regex.Escape(oldUrl)
            let regex = new Regex(pattern)
            if regex.IsMatch(updatedContent) then
                updatedContent <- regex.Replace(updatedContent, newUrl)
                changesCount <- changesCount + regex.Matches(content).Count
        
        // Write back if changes were made
        if changesCount > 0 then
            File.WriteAllText(filePath, updatedContent)
            printfn "✅ Updated %s - Fixed %d link(s)" (Path.GetFileName(filePath)) changesCount
            changesCount
        else
            0
    else
        0

let processDirectory (directory: string) =
    let mutableTotalChanges = ref 0
    let allFiles = Directory.GetFiles(directory, "*.md", SearchOption.AllDirectories)
    
    printfn "🔍 Processing %d markdown files in %s" allFiles.Length directory
    printfn ""
    
    for file in allFiles do
        let changes = processFile file
        mutableTotalChanges := !mutableTotalChanges + changes
    
    !mutableTotalChanges

// Run the script
printfn "🚀 Starting legacy /feed/ link fix process..."
printfn "📋 Mapping %d legacy URLs to new architecture" feedLinkMappings.Length
printfn ""

let totalChanges = processDirectory srcDirectory

printfn ""
printfn "✅ Link fixing complete!"
printfn "📊 Total links fixed: %d" totalChanges

if totalChanges > 0 then
    printfn ""
    printfn "🔄 Next steps:"
    printfn "1. Build the site: dotnet run"
    printfn "2. Re-run link analysis to verify fixes"
    printfn "3. Test affected pages manually"
