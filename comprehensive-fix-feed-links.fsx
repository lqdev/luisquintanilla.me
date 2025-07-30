#!/usr/bin/env dotnet fsi

// Comprehensive /feed/ link fixing script
// Based on analysis of all remaining broken links from link-analysis-report.json

open System
open System.IO
open System.Text.RegularExpressions

// Complete mapping based on actual broken link analysis
let feedLinkMappings = [
    // Weekly summaries (notes) - verified from existing content structure
    ("/feed/2024-03-10-weekly-post-summary", "/notes/2024-03-10-weekly-post-summary/")
    ("/feed/2024-03-18-weekly-post-summary", "/notes/2024-03-18-weekly-post-summary/")
    ("/feed/2024-03-24-weekly-post-summary", "/notes/2024-03-24-weekly-post-summary/")
    ("/feed/2024-04-01-weekly-post-summary", "/notes/2024-04-01-weekly-post-summary/")
    ("/feed/2024-04-07-weekly-post-summary", "/notes/2024-04-07-weekly-post-summary/")
    ("/feed/2024-04-14-weekly-post-summary", "/notes/2024-04-14-weekly-post-summary/")
    ("/feed/2024-04-29-weekly-post-summary", "/notes/2024-04-29-weekly-post-summary/")
    ("/feed/2024-05-06-weekly-post-summary", "/notes/2024-05-06-weekly-post-summary/")
    ("/feed/2024-05-26-weekly-post-summary", "/notes/2024-05-26-weekly-post-summary/")
    ("/feed/2024-06-03-weekly-post-summary", "/notes/2024-06-03-weekly-post-summary/")
    ("/feed/2024-07-21-weekly-post-summary", "/notes/2024-07-21-weekly-post-summary/")
    ("/feed/2024-07-28-weekly-post-summary", "/notes/2024-07-28-weekly-post-summary/")
    ("/feed/2024-08-11-weekly-post-summary", "/notes/2024-08-11-weekly-post-summary/")
    ("/feed/2024-09-15-weekly-post-summary", "/notes/2024-09-15-weekly-post-summary/")
    ("/feed/2024-10-20-weekly-post-summary", "/notes/2024-10-20-weekly-post-summary/")
    ("/feed/2024-11-03-weekly-post-summary", "/notes/2024-11-03-weekly-post-summary/")
    ("/feed/2024-11-10-weekly-post-summary", "/notes/2024-11-10-weekly-post-summary/")
    ("/feed/2024-11-18-weekly-post-summary", "/notes/2024-11-18-weekly-post-summary/")
    ("/feed/2024-11-25-weekly-post-summary", "/notes/2024-11-25-weekly-post-summary/")
    ("/feed/2024-12-09-weekly-post-summary", "/notes/2024-12-09-weekly-post-summary/")
    ("/feed/2024-12-15-weekly-post-summary", "/notes/2024-12-15-weekly-post-summary/")
    ("/feed/2025-01-05-weekly-post-summary", "/notes/2025-01-05-weekly-post-summary/")
    ("/feed/2025-01-12-weekly-post-summary", "/notes/2025-01-12-weekly-post-summary/")
    ("/feed/2025-01-19-weekly-post-summary", "/notes/2025-01-19-weekly-post-summary/")
    ("/feed/2025-02-02-weekly-post-summary", "/notes/2025-02-02-weekly-post-summary/")
    ("/feed/2025-02-09-weekly-post-summary", "/notes/2025-02-09-weekly-post-summary/")
    ("/feed/2025-03-02-weekly-post-summary", "/notes/2025-03-02-weekly-post-summary/")
    ("/feed/2025-03-23-weekly-post-summary", "/notes/2025-03-23-weekly-post-summary/")
    ("/feed/2025-05-12-weekly-post-summary", "/notes/2025-05-12-weekly-post-summary/")
    ("/feed/2025-05-26-weekly-post-summary", "/notes/2025-05-26-weekly-post-summary/")
    ("/feed/2025-06-29-weekly-post-summary", "/notes/2025-06-29-weekly-post-summary/")
    ("/feed/2025-07-06-weekly-post-summary", "/notes/2025-07-06-weekly-post-summary/")

    // Notes content
    ("/feed/2022-04-21-assorted-links", "/notes/2022-04-21-assorted-links/")
    ("/feed/weblogging-rewind-2023", "/notes/weblogging-rewind-2023/")
    ("/feed/weblogging-rewind-2023-continued", "/notes/weblogging-rewind-2023-continued/")
    ("/feed/webmentions-partially-implemented", "/notes/webmentions-partially-implemented/")
    ("/feed/website-feeds-opml", "/notes/website-feeds-opml/")
    ("/feed/website-upgraded-dotnet-9", "/notes/website-upgraded-dotnet-9/")
    ("/feed/welcome-to-fediverse-tips", "/notes/welcome-to-fediverse-tips/")
    ("/feed/well-known-feeds", "/notes/well-known-feeds/")
    ("/feed/what-is-kick", "/notes/what-is-kick/")
    ("/feed/windows-11-upgrade", "/notes/windows-11-upgrade/")
    ("/feed/windows-phone-lumia-reminiscing", "/notes/windows-phone-lumia-reminiscing/")
    ("/feed/world-wide-whack-tierra-whack-review", "/notes/world-wide-whack-tierra-whack-review/")
    ("/feed/worlds-largest-smartphone-camera", "/notes/worlds-largest-smartphone-camera/")
    ("/feed/year-of-curiosity", "/notes/year-of-curiosity/")
    ("/feed/year-of-linux-desktop-4-percent", "/notes/year-of-linux-desktop-4-percent/")
    ("/feed/you-should-be-using-rss-reader-pluralistic", "/notes/you-should-be-using-rss-reader-pluralistic/")
    ("/feed/your-site-is-a-home-hamid", "/notes/your-site-is-a-home-hamid/")
    ("/feed/zero-search-alibaba", "/notes/zero-search-alibaba/")
    ("/feed/windows-subsystem-for-android-end-of-support-2025", "/notes/windows-subsystem-for-android-end-of-support-2025/")
    ("/feed/wix-website-builder-ai-chatbot", "/notes/wix-website-builder-ai-chatbot/")
    ("/feed/why-you-should-make-a-website-luvstarkei", "/notes/why-you-should-make-a-website-luvstarkei/")
    ("/feed/willison-tools-colophon", "/notes/willison-tools-colophon/")
    ("/feed/windows-12-mobile-concept", "/notes/windows-12-mobile-concept/")
    ("/feed/when-planets-mate", "/notes/when-planets-mate/")
    ("/feed/marvin-gaye-whats-going-on", "/notes/marvin-gaye-whats-going-on/")

    // Cross-referenced notes that need different targets
    ("/feed/bluesky-rss-support", "/notes/bluesky-rss-support/")
    ("/feed/cosmopedia-ai-synthetic-dataset", "/notes/cosmopedia-ai-synthetic-dataset/")
    ("/feed/decoder-nilay-patel-why-websites-are-the-future", "/notes/decoder-nilay-patel-why-websites-are-the-future/")
    ("/feed/phi-2-huggingface", "/notes/phi-2-huggingface/")
    ("/feed/shipping-wordpress-tumblr", "/notes/shipping-wordpress-tumblr/")
    ("/feed/tumblr-still-working-fediverse-integration", "/notes/tumblr-still-working-fediverse-integration/")
    ("/feed/verge-ai-robots-txt", "/notes/verge-ai-robots-txt/")

    // Responses
    ("/feed/1-percent-rule", "/responses/1-percent-rule/")
    ("/feed/1mb-club", "/responses/1mb-club/")
    ("/feed/2022-state-of-ai-report", "/responses/2022-state-of-ai-report/")
    ("/feed/2023-goals-accomplishments-purple-life", "/responses/2023-goals-accomplishments-purple-life/")
    ("/feed/2023-social-media-case-fediverse", "/responses/2023-social-media-case-fediverse/")
    ("/feed/2023-state-of-ai-report", "/responses/2023-state-of-ai-report/")
    ("/feed/why-open-source-ai-will-win-varun", "/responses/why-open-source-ai-will-win-varun/")
    ("/feed/x-ai-launch", "/responses/x-ai-launch/")
    ("/feed/wordpress-activitypub-plugin", "/responses/wordpress-activitypub-plugin/")

    // Collections  
    ("/feed/blogroll", "/collections/blogroll/")
    ("/feed/podroll", "/collections/podroll/")
    ("/feed/starter", "/collections/starter-packs/")
    ("/feed/forums", "/collections/forums/")
    ("/feed/youtube", "/collections/youtube/")

    // Also handle posts that are incorrectly referenced as weekly summaries in /posts/ instead of /notes/
    ("/posts/2024-03-18-weekly-post-summary", "/notes/2024-03-18-weekly-post-summary/")
    ("/posts/2024-04-01-weekly-post-summary", "/notes/2024-04-01-weekly-post-summary/")
    ("/posts/2024-04-07-weekly-post-summary", "/notes/2024-04-07-weekly-post-summary/")
    ("/posts/2024-04-29-weekly-post-summary", "/notes/2024-04-29-weekly-post-summary/")
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
                let matchCount = regex.Matches(content).Count
                updatedContent <- regex.Replace(updatedContent, newUrl)
                changesCount <- changesCount + matchCount
        
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
    printfn "📋 Comprehensive mapping with %d URL patterns" feedLinkMappings.Length
    printfn ""
    
    for file in allFiles do
        let changes = processFile file
        mutableTotalChanges := !mutableTotalChanges + changes
    
    !mutableTotalChanges

// Run the script
printfn "🚀 Starting comprehensive /feed/ link fix process..."
printfn ""

let totalChanges = processDirectory srcDirectory

printfn ""
printfn "✅ Comprehensive link fixing complete!"
printfn "📊 Total links fixed: %d" totalChanges

if totalChanges > 0 then
    printfn ""
    printfn "🔄 Next steps:"
    printfn "1. Rebuild site: dotnet run"
    printfn "2. Re-run link analysis to verify impact"
    printfn "3. Test a few fixed links manually"
else
    printfn ""
    printfn "ℹ️ No additional links found to fix in source files."
    printfn "💡 Remaining broken links may be:"
    printfn "   - Generated dynamically from data/content aggregation"
    printfn "   - Cross-references that need content updates"
    printfn "   - Template-level fixes needed"
