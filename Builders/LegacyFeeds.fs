module LegacyFeedsBuilder

    open System.IO
    open BuilderCommon

    let buildLegacyRssFeedAliases () =
        // Create legacy RSS feed aliases for backward compatibility
        let sourceBlogFeed = Path.GetFullPath(Path.Join(outputDir, "posts", "feed.xml"))
        let sourceMicroblogFeed = Path.GetFullPath(Path.Join(outputDir, "feed", "notes.xml"))
        let sourceResponsesFeed = Path.GetFullPath(Path.Join(outputDir, "responses", "feed.xml"))
        let sourceUnifiedFeed = Path.GetFullPath(Path.Join(outputDir, "feed", "feed.xml"))
        
        let targetBlogRss = Path.GetFullPath(Path.Join(outputDir, "blog.rss"))
        let targetMicroblogRss = Path.GetFullPath(Path.Join(outputDir, "microblog.rss"))
        let targetResponsesRss = Path.GetFullPath(Path.Join(outputDir, "responses.rss"))
        let targetAllRss = Path.GetFullPath(Path.Join(outputDir, "all.rss"))
        
        printfn "Building legacy RSS feed aliases..."
        printfn "Looking for source files:"
        printfn "  Blog: %s (exists: %b)" sourceBlogFeed (File.Exists(sourceBlogFeed))
        printfn "  Microblog: %s (exists: %b)" sourceMicroblogFeed (File.Exists(sourceMicroblogFeed))
        printfn "  Responses: %s (exists: %b)" sourceResponsesFeed (File.Exists(sourceResponsesFeed))
        printfn "  Unified: %s (exists: %b)" sourceUnifiedFeed (File.Exists(sourceUnifiedFeed))
        
        // Copy feeds to legacy RSS locations
        if File.Exists(sourceBlogFeed) then
            File.Copy(sourceBlogFeed, targetBlogRss, true)
            printfn "✅ Created blog.rss"
        else
            printfn "❌ Source blog feed not found: %s" sourceBlogFeed
            
        if File.Exists(sourceMicroblogFeed) then
            File.Copy(sourceMicroblogFeed, targetMicroblogRss, true)
            printfn "✅ Created microblog.rss"
        else
            printfn "❌ Source microblog feed not found: %s" sourceMicroblogFeed
            
        if File.Exists(sourceResponsesFeed) then
            File.Copy(sourceResponsesFeed, targetResponsesRss, true)
            printfn "✅ Created responses.rss"
            
            // Also copy to the legacy /feed/responses/index.xml location for backward compatibility
            let legacyResponsesFeedDir = Path.GetFullPath(Path.Join(outputDir, "feed", "responses"))
            Directory.CreateDirectory(legacyResponsesFeedDir) |> ignore
            let legacyResponsesFeedPath = Path.Join(legacyResponsesFeedDir, "index.xml")
            File.Copy(sourceResponsesFeed, legacyResponsesFeedPath, true)
            printfn "✅ Created legacy responses feed at /feed/responses/index.xml"
        else
            printfn "❌ Source responses feed not found: %s" sourceResponsesFeed
            
        if File.Exists(sourceUnifiedFeed) then
            File.Copy(sourceUnifiedFeed, targetAllRss, true)
            printfn "✅ Created all.rss (unified feed alias)"
        else
            printfn "❌ Source unified feed not found: %s" sourceUnifiedFeed
