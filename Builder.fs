module Builder

    open System
    open System.Globalization
    open System.IO
    open System.Linq
    open System.Text.Json
    open System.Xml.Linq
    open Domain
    open MarkdownService
    open TagService
    open OpmlService
    open RelatedContentService
    open ViewGenerator
    open PartialViews
    open TagViews
    open Loaders
    open Markdig
    open Markdig.Parsers
    open Giraffe.ViewEngine
    
    let private srcDir = "_src"
    let private outputDir = "_public"

    /// Sanitize tag names for safe file system paths while preserving readability
    let private sanitizeTagForPath (tag: string) =
        tag.Trim()
            .Replace("\"", "")       // Remove quotes
            .Replace("#", "sharp")   // Replace # with "sharp" (f# -> fsharp, c# -> csharp)
            .Replace(" ", "-")       // Replace spaces with hyphens
            .Replace(".", "dot")     // Replace dots with "dot" (.net -> dotnet)
            .Replace("/", "-")       // Replace slashes with hyphens
            .Replace("\\", "-")      // Replace backslashes with hyphens
            .Replace(":", "-")       // Replace colons with hyphens
            .Replace("*", "star")    // Replace asterisks
            .Replace("?", "q")       // Replace question marks
            .Replace("<", "lt")      // Replace less than
            .Replace(">", "gt")      // Replace greater than
            .Replace("|", "pipe")    // Replace pipes
            .ToLowerInvariant()      // Make lowercase for consistency

    // =====================================================================
    // Common Build Helper Functions - Reduce Duplication
    // =====================================================================
    
    /// Helper: Write HTML page to a directory, ensuring directory exists
    let private writePageToDir (dir: string) (filename: string) (content: string) =
        Directory.CreateDirectory(dir) |> ignore
        File.WriteAllText(Path.Join(dir, filename), content)
    
    /// Helper: Write file content to a directory, ensuring directory exists
    let private writeFileToDir (dir: string) (filename: string) (content: string) =
        Directory.CreateDirectory(dir) |> ignore
        File.WriteAllText(Path.Join(dir, filename), content)
    
    /// Helper: Get markdown files from a source directory
    let private getContentFiles (relativePath: string) =
        Directory.GetFiles(Path.Join(srcDir, relativePath))
        |> Array.filter (fun f -> f.EndsWith(".md"))
        |> Array.toList

    let rec cleanOutputDirectory (outputDir:string) = 
        if Directory.Exists(outputDir) then
            let dirInfo = DirectoryInfo(outputDir)

            dirInfo.GetFiles()
            |> Array.iter(fun x -> x.Delete())

            dirInfo.GetDirectories()
            |> Array.iter(fun x -> 
                cleanOutputDirectory x.FullName
                x.Delete())

    let copyStaticFiles () =
        // Asset directories to copy to /assets/
        let assetDirectories = [
            ("css", "assets/css")
            ("js", "assets/js") 
            ("lib", "assets/lib")
        ]

        // Copy asset directories to /assets/
        assetDirectories
        |> List.iter(fun (srcPath, destPath) ->
            let sourcePath = Path.Join(srcDir, srcPath)
            let destPath = Path.Join(outputDir, destPath)
            
            if Directory.Exists(sourcePath) then
                Directory.CreateDirectory(destPath) |> ignore
                
                // Copy all files recursively
                Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories)
                |> Array.iter(fun file ->
                    let relativePath = Path.GetRelativePath(sourcePath, file)
                    let destFile = Path.Join(destPath, relativePath)
                    Directory.CreateDirectory(Path.GetDirectoryName(destFile)) |> ignore
                    File.Copy(file, destFile, true))
        )

        // Copy other static directories at root level
        let staticDirectories = [
            "assets/images"
            ".well-known"
            "lib"
        ]

        staticDirectories
        |> List.iter(fun dir ->
            let sourcePath = Path.Join(srcDir, dir)
            let destPath = Path.Join(outputDir, dir)
            
            if Directory.Exists(sourcePath) then
                Directory.CreateDirectory(destPath) |> ignore
                
                Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories)
                |> Array.iter(fun file ->
                    let relativePath = Path.GetRelativePath(sourcePath, file)
                    let destFile = Path.Join(destPath, relativePath)
                    Directory.CreateDirectory(Path.GetDirectoryName(destFile)) |> ignore
                    File.Copy(file, destFile, true))
        )
        
        // Copy favicon & avatar
        File.Copy(Path.Join(srcDir,"favicon.ico"),Path.Join(outputDir,"favicon.ico"),true)
        File.Copy(Path.Join(srcDir,"avatar.png"),Path.Join(outputDir,"avatar.png"),true)
        File.Copy(Path.Join(srcDir,"art-profile.png"),Path.Join(outputDir,"art-profile.png"),true)

        // Copy contact cards
        File.Copy(Path.Join(srcDir,"vcard.vcf"),Path.Join(outputDir,"vcard.vcf"),true)
        File.Copy(Path.Join(srcDir,"mecard.txt"),Path.Join(outputDir,"mecard.txt"),true)
        
        // Copy PWA files
        File.Copy(Path.Join(srcDir,"service-worker.js"),Path.Join(outputDir,"service-worker.js"),true)
        File.Copy(Path.Join(srcDir,"manifest.json"),Path.Join(outputDir,"manifest.json"),true)
        File.Copy(Path.Join(srcDir,"offline.html"),Path.Join(outputDir,"offline.html"),true)

    let buildHomePage (blogPosts:Post array) (feedPosts:Post array) (responsePosts:Response array)= 
        let recentBlog = 
            blogPosts 
            |> Array.sortByDescending(fun x-> DateTime.Parse(x.Metadata.Date))
            |> Array.head

        let recentFeedPost = 
            feedPosts
            |> Array.sortByDescending(fun x -> DateTime.Parse(x.Metadata.Date))
            |> Array.head

        let recentResponsePost = 
            responsePosts
            |> Array.sortByDescending(fun x -> DateTime.Parse(x.Metadata.DateUpdated))
            |> Array.head

        // let recentPostsContent = generatePartial (recentPostsView recentPosts)
        let homePage = generate (homeView recentBlog recentFeedPost recentResponsePost) "default" "Home - Luis Quintanilla"
        File.WriteAllText(Path.Join(outputDir,"index.html"),homePage)

    // New timeline homepage for Phase 3 - Feed-as-Homepage Interface
    let buildTimelineHomePage (allUnifiedItems: (string * GenericBuilder.UnifiedFeeds.UnifiedFeedItem list) list) =
        // Load pinned posts configuration
        let pinnedPostsConfig = loadPinnedPosts()
        
        // Flatten all items
        let allItemsFlattened = 
            allUnifiedItems
            |> List.collect snd
            |> List.sortByDescending (fun item -> DateTimeOffset.Parse(item.Date))
        
        // Extract pinned items based on configuration (using filename matching)
        let pinnedItems = 
            pinnedPostsConfig
            |> Array.choose (fun (config: PinnedPost) ->
                allItemsFlattened 
                |> List.tryFind (fun item -> 
                    // Extract filename from URL: /posts/my-post/ -> my-post
                    let urlFileName = 
                        item.Url.TrimEnd('/').Split('/')
                        |> Array.last
                    urlFileName = config.FileName && 
                    item.ContentType = config.ContentType))
            |> Array.toList
        
        // Remove pinned items from chronological list
        let unpinnedItemsFlattened = 
            allItemsFlattened
            |> List.filter (fun item ->
                not (pinnedItems |> List.exists (fun pinned -> pinned.Url = item.Url)))
        
        // Take initial items from unpinned list
        let chronologicalInitialItems = 
            unpinnedItemsFlattened
            |> List.take (min 50 unpinnedItemsFlattened.Length)
        
        // Combine: pinned first, then chronological
        let initialItems = pinnedItems @ chronologicalInitialItems
        
        // Group remaining items by content type for type-aware progressive loading
        let remainingItemsByType = 
            allUnifiedItems
            |> List.map (fun (contentType, items) ->
                let sortedItems = items |> List.sortByDescending (fun item -> DateTimeOffset.Parse(item.Date))
                // Skip items that are already in the initial load (pinned + chronological)
                let remainingItems = 
                    sortedItems 
                    |> List.filter (fun item -> 
                        not (initialItems |> List.exists (fun initial -> initial.Url = item.Url)))
                (contentType, remainingItems)
            )
            |> List.filter (fun (_, items) -> not items.IsEmpty)
        
        // Create set of pinned URLs for efficient lookup
        let pinnedUrls = pinnedItems |> List.map (fun item -> item.Url) |> Set.ofList
        
        // Generate the timeline homepage with pinned posts support
        let timelineHomePage = generate (LayoutViews.timelineHomeViewStratified (initialItems |> List.toArray) remainingItemsByType pinnedUrls) "default" "Luis Quintanilla - Personal Website"
        File.WriteAllText(Path.Join(outputDir,"index.html"), timelineHomePage)
        
        let totalItems = allUnifiedItems |> List.sumBy (fun (_, items) -> items.Length)
        let pinnedCount = pinnedItems.Length
        printfn "✅ Timeline homepage created with %d pinned posts, %d chronological items from %d total items across all content types" pinnedCount chronologicalInitialItems.Length totalItems

    let buildAboutPage () = 
        let aboutContent = convertFileToHtml (Path.Join(srcDir,"about.md")) |> contentView
        let aboutPage = generate aboutContent "default" "About - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"about")
        writePageToDir saveDir "index.html" aboutPage

    let buildCollectionsPage () = 
        let collectionsContent = convertFileToHtml (Path.Join(srcDir,"collections.md")) |> contentView
        let collectionsPage = generate collectionsContent "default" "Collections - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"collections")
        writePageToDir saveDir "index.html" collectionsPage

    let buildStarterPackPage () = 
        let starterContent = convertFileToHtml (Path.Join(srcDir,"starter-packs.md")) |> contentView
        let starterPage = generate starterContent "default" "Starter Packs - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"collections","starter-packs")
        writePageToDir saveDir "index.html" starterPage

    let buildTravelGuidesPage () = 
        let travelContent = convertFileToHtml (Path.Join(srcDir,"travel-guides.md")) |> contentView
        let travelPage = generate travelContent "default" "Travel Guides - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"collections","travel-guides")
        writePageToDir saveDir "index.html" travelPage

    let buildBlogrollPage (links:Outline array) = 
        let blogRollContent = links |> blogRollView
        let blogRollPage = generate blogRollContent "default" "Blogroll - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"collections","blogroll")
        writePageToDir saveDir "index.html" blogRollPage

    let buildPodrollPage (links:Outline array) = 
        let podrollContent = links |> podRollView
        let podrollPage = generate podrollContent "default" "Podroll - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"collections","podroll")
        writePageToDir saveDir "index.html" podrollPage

    let buildForumsPage (links:Outline array) = 
        let forumContent = links |> forumsView
        let forumsPage = generate forumContent "default" "Forums - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"collections","forums")
        writePageToDir saveDir "index.html" forumsPage

    let buildYouTubeChannelsPage (links:Outline array) = 
        let ytContent = links |> youTubeFeedView
        let ytFeedPage = generate ytContent "default" "YouTube Channels - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"collections","youtube")
        writePageToDir saveDir "index.html" ytFeedPage

    let buildAIStarterPackPage (links:Outline array) = 
        let aiStarterPackContent = links |> aiStarterPackFeedView
        let ytFeedPage = generate aiStarterPackContent "default" "AI Starter Pack - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"collections","starter-packs","ai")
        writePageToDir saveDir "index.html" ytFeedPage

    let buildReadLaterPage (links:ReadLaterLink array) = 
        let readLaterContent = links |> readLaterView
        let readLaterPage = generate readLaterContent "default" "Read Later - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"resources","read-later")
        writePageToDir saveDir "index.html" readLaterPage

    let buildIRLStackPage () = 
        let irlStackContent = Path.Join(srcDir,"uses.md") |> convertFileToHtml |> contentView
        let irlStackPage = generate irlStackContent "default" "In Real Life Stack - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"uses")
        writePageToDir saveDir "index.html" irlStackPage

    let buildColophonPage () = 
        let colophonContent = Path.Join(srcDir,"colophon.md") |> convertFileToHtml |> contentView
        let colophonPage = generate colophonContent "default" "Colophon - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"colophon")
        writePageToDir saveDir "index.html" colophonPage

    let buildContactPage () = 
        let contactContent = convertFileToHtml (Path.Join(srcDir,"contact.md")) |> contentView
        let contactPage = generate contactContent "default" "Contact - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"contact")
        writePageToDir saveDir "index.html" contactPage

    let buildSearchPage () = 
        let searchContent = convertFileToHtml (Path.Join(srcDir,"search.md")) |> contentView
        let searchPage = generate searchContent "default" "Search - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"search")
        writePageToDir saveDir "index.html" searchPage

    let buildOnlineRadioPage () = 
        let onlineRadioContent = convertFileToHtml (Path.Join(srcDir,"radio.md")) |> contentView
        let onlineRadioPage = generate onlineRadioContent "default" "Online Radio - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"radio")
        Directory.CreateDirectory(saveDir) |> ignore

        // Copy playlist file
        File.Copy(Path.Join(srcDir,"OnlineRadioPlaylist.m3u"),Path.Join(saveDir,"OnlineRadioPlaylist.m3u"),true)
        
        // Write out page
        writePageToDir saveDir "index.html" onlineRadioPage        

    // =====================================================================
    // Resume Page Builder
    // =====================================================================
    
    /// Extract content between headings in markdown document
    let private extractSectionContent (doc: Markdig.Syntax.MarkdownDocument) (headingText: string) =
        let headings = 
            Markdig.Syntax.MarkdownObjectExtensions.Descendants<Markdig.Syntax.HeadingBlock>(doc)
            |> Seq.toList
        
        // Helper to extract text from heading inline content
        let getHeadingText (h: Markdig.Syntax.HeadingBlock) =
            if h.Inline <> null then
                // Get first child literal if it exists
                let literals = 
                    Markdig.Syntax.MarkdownObjectExtensions.Descendants<Markdig.Syntax.Inlines.LiteralInline>(h.Inline)
                    |> Seq.toList
                if not literals.IsEmpty then
                    literals |> List.map (fun l -> l.Content.ToString()) |> String.concat ""
                else
                    ""
            else
                ""
        
        // Find the heading that matches the target text (case-insensitive)
        let targetHeadingOpt = 
            headings 
            |> List.tryFind (fun h -> 
                let text = getHeadingText h
                text.Trim().Equals(headingText, StringComparison.OrdinalIgnoreCase))
        
        match targetHeadingOpt with
        | None -> None
        | Some targetHeading ->
            // Find all blocks that come after this heading until the next heading of same or higher level
            let allBlocks = doc |> Seq.toList
            let targetIndex = allBlocks |> List.findIndex (fun b -> Object.ReferenceEquals(b, targetHeading))
            let targetLevel = targetHeading.Level
            
            // Get blocks until next heading of same or higher level
            let contentBlocks = 
                allBlocks
                |> List.skip (targetIndex + 1)
                |> List.takeWhile (fun block ->
                    match block with
                    | :? Markdig.Syntax.HeadingBlock as h -> h.Level > targetLevel
                    | _ -> true)
            
            if contentBlocks.IsEmpty then
                None
            else
                // Convert blocks back to markdown using Markdig renderer
                use writer = new System.IO.StringWriter()
                let renderer = Markdig.Renderers.HtmlRenderer(writer)
                for block in contentBlocks do
                    renderer.Write(block)
                let html = writer.ToString().Trim()
                if String.IsNullOrWhiteSpace(html) then None else Some html
    
    let buildResumePage () =
        let resumePath = Path.Join(srcDir, "resume", "resume.md")
        
        match Loaders.loadResume resumePath with
        | None ->
            printfn "⚠ No resume found at %s - skipping resume page" resumePath
        | Some baseResume ->
            try
                // Parse the markdown with resume block extensions
                let pipeline = 
                    MarkdownPipelineBuilder()
                        .UseYamlFrontMatter()
                        .UsePipeTables()
                        .UseTaskLists()
                        .UseDiagrams()
                        .UseMediaLinks()
                        .UseMathematics()
                        .UseEmojiAndSmiley()
                        .UseEmphasisExtras()
                        .UseBootstrap()
                        .UseFigures()
                        .Use(CustomBlocks.ResumeBlockExtension())
                        .Build()
                
                let doc = Markdown.Parse(baseResume.Content, pipeline)
                
                // Extract resume blocks from AST
                let experiences = 
                    Markdig.Syntax.MarkdownObjectExtensions.Descendants<CustomBlocks.ExperienceBlock>(doc)
                    |> Seq.map (fun (block: CustomBlocks.ExperienceBlock) ->
                        {
                            Role = block.Role
                            Company = block.Company
                            StartDate = DateTime.Parse(block.Start)
                            EndDate = 
                                match block.End with
                                | Some "current" -> None
                                | Some date -> Some (DateTime.Parse(date))
                                | None -> None
                            Highlights = 
                                if String.IsNullOrWhiteSpace(block.Content) || block.Content = " " then None
                                else 
                                    block.Content.Split('\n')
                                    |> Array.filter (fun s -> not (String.IsNullOrWhiteSpace s))
                                    |> Array.toList
                                    |> Some
                        } : Domain.Experience)
                    |> Seq.toList
                
                let projects = 
                    Markdig.Syntax.MarkdownObjectExtensions.Descendants<CustomBlocks.ProjectBlock>(doc)
                    |> Seq.map (fun (block: CustomBlocks.ProjectBlock) ->
                        let description = if String.IsNullOrWhiteSpace(block.Content) || block.Content = " " then "" else block.Content
                        let techs = 
                            match block.Tech with
                            | Some t when not (String.IsNullOrWhiteSpace t) ->
                                t.Split(',') |> Array.map (fun s -> s.Trim()) |> Array.toList |> Some
                            | _ -> None
                        {
                            Title = block.Title
                            Description = description
                            Url = block.Url
                            Technologies = techs
                            Highlights = None  // Not used in current design
                        } : Domain.Project)
                    |> Seq.toList
                
                let skills = 
                    Markdig.Syntax.MarkdownObjectExtensions.Descendants<CustomBlocks.SkillsBlock>(doc)
                    |> Seq.map (fun (block: CustomBlocks.SkillsBlock) ->
                        let skillList = 
                            if String.IsNullOrWhiteSpace(block.Content) || block.Content = " " then []
                            elif block.Content.Contains(",") && not (block.Content.Contains("\n-") || block.Content.Contains("\n*")) then
                                // Comma-separated
                                block.Content.Split(',') |> Array.map (fun s -> s.Trim()) |> Array.toList
                            else
                                // Bullet list
                                block.Content.Split('\n') 
                                |> Array.filter (fun s -> not (String.IsNullOrWhiteSpace s))
                                |> Array.map (fun s -> s.Trim().TrimStart('-', '*').Trim())
                                |> Array.toList
                        {
                            Category = block.Category
                            Skills = skillList
                        } : Domain.SkillCategory)
                    |> Seq.toList
                
                let testimonials = 
                    Markdig.Syntax.MarkdownObjectExtensions.Descendants<CustomBlocks.TestimonialBlock>(doc)
                    |> Seq.map (fun (block: CustomBlocks.TestimonialBlock) ->
                        {
                            Quote = if String.IsNullOrWhiteSpace(block.Content) || block.Content = " " then "" else block.Content
                            Author = block.Author
                        } : Domain.Testimonial)
                    |> Seq.toList
                
                let education = 
                    Markdig.Syntax.MarkdownObjectExtensions.Descendants<CustomBlocks.EducationBlock>(doc)
                    |> Seq.map (fun (block: CustomBlocks.EducationBlock) ->
                        let yearOpt = 
                            match block.Year with
                            | Some y when not (String.IsNullOrWhiteSpace y) ->
                                match Int32.TryParse(y) with
                                | (true, year) -> Some year
                                | _ -> None
                            | _ -> None
                        let details = 
                            if String.IsNullOrWhiteSpace(block.Content) || block.Content = " " then None
                            else Some block.Content
                        {
                            Degree = block.Degree
                            Institution = block.Institution
                            GraduationYear = yearOpt
                            Details = details
                        } : Domain.Education)
                    |> Seq.toList
                
                // Extract About and Interests sections from markdown
                let aboutSection = extractSectionContent doc "About"
                let interestsSection = 
                    match extractSectionContent doc "Currently Interested In" with
                    | Some content -> Some content
                    | None -> extractSectionContent doc "Interests"
                
                // Build complete resume with extracted data
                let completeResume = 
                    { baseResume with
                        AboutSection = aboutSection
                        InterestsSection = interestsSection
                        Experience = experiences
                        Projects = projects
                        Skills = skills
                        Testimonials = testimonials
                        Education = education
                    }
                
                // Generate HTML (will add view in next phase)
                let html = ContentViews.ResumeView.render completeResume
                let resumePage = generate html "default" "Resume - Luis Quintanilla"
                
                let saveDir = Path.Join(outputDir, "resume")
                Directory.CreateDirectory(saveDir) |> ignore
                File.WriteAllText(Path.Join(saveDir, "index.html"), resumePage)
                
                printfn "✅ Resume page built successfully"
            with
            | ex ->
                printfn "❌ Error building resume page: %s" ex.Message

    let buildFeedsOpml (links:Outline array) = 
        let feed = buildOpmlFeed "Luis Quintanilla Feeds" "https://www.lqdev.me" links
        let saveDir = Path.Join(outputDir,"feed")
        writeFileToDir saveDir "index.opml" (feed.ToString())

    let buildBlogrollOpml (links:Outline array) = 
        let feed = buildOpmlFeed "Luis Quintanilla Blogroll" "https://www.lqdev.me" links
        let saveDir = Path.Join(outputDir,"collections","blogroll")
        let content = feed.ToString()
        writeFileToDir saveDir "index.xml" content
        writeFileToDir saveDir "index.opml" content

    let buildPodrollOpml (links:Outline array) = 
        let feed = buildOpmlFeed "Luis Quintanilla Podroll" "https://www.lqdev.me" links
        let saveDir = Path.Join(outputDir,"collections","podroll")
        let content = feed.ToString()
        writeFileToDir saveDir "index.xml" content
        writeFileToDir saveDir "index.opml" content

    let buildForumsOpml (links:Outline array) = 
        let feed = buildOpmlFeed "Luis Quintanilla Forums" "https://www.lqdev.me" links
        let saveDir = Path.Join(outputDir,"collections","forums")
        let content = feed.ToString()
        writeFileToDir saveDir "index.xml" content
        writeFileToDir saveDir "index.opml" content

    let buildYouTubeOpml (links:Outline array) = 
        let feed = buildOpmlFeed "Luis Quintanilla YouTube Channels" "https://www.lqdev.me" links
        let saveDir = Path.Join(outputDir,"collections","youtube")
        let content = feed.ToString()
        writeFileToDir saveDir "index.xml" content
        writeFileToDir saveDir "index.opml" content

    let buildAIStarterPackOpml (links:Outline array) = 
        let feed = buildOpmlFeed "Luis Quintanilla AI Starter Pack" "https://www.lqdev.me" links
        let saveDir = Path.Join(outputDir,"collections","starter-packs","ai")
        let content = feed.ToString()
        writeFileToDir saveDir "index.xml" content
        writeFileToDir saveDir "index.opml" content

    // =============================================================================
    // Unified Collection Processing - New composable collection system
    // =============================================================================
    
    let buildUnifiedCollections () = 
        // Get all collections and their data
        let collections = Collections.CollectionBuilder.buildCollections ()
        
        printfn "🔧 Building unified collections..."
        
        for (collection, data) in collections do
            try
                // Generate HTML page using the new system
                let htmlContent = Collections.CollectionProcessor.generateCollectionPage data
                let htmlPage = generate htmlContent "default" $"{collection.Title} - Luis Quintanilla"
                
                // Calculate paths
                let paths = Collections.CollectionProcessor.getCollectionPaths collection
                
                // Ensure output directory exists
                let outputPath = Path.Join(outputDir, Path.GetDirectoryName(paths.HtmlPath))
                Directory.CreateDirectory(outputPath) |> ignore
                
                // Write HTML file
                File.WriteAllText(Path.Join(outputDir, paths.HtmlPath), htmlPage)
                
                // Generate and write OPML file
                let opmlContent = Collections.CollectionBuilder.generateCollectionOpmlContent data
                File.WriteAllText(Path.Join(outputDir, paths.OpmlPath), opmlContent)
                
                // Generate and write RSS file
                let rssContent = Collections.CollectionBuilder.generateCollectionRssContent data
                File.WriteAllText(Path.Join(outputDir, paths.RssPath), rssContent)
                
                // Generate and write GPX file (if applicable)
                match paths.GpxPath with
                | Some gpxRelativePath ->
                    let processor = Collections.CollectionProcessor.createCollectionProcessor collection
                    match processor.GenerateGpxFile data with
                    | Some gpxContent ->
                        File.WriteAllText(Path.Join(outputDir, gpxRelativePath), gpxContent)
                        printfn "✅ Generated GPX file: %s" gpxRelativePath
                    | None ->
                        printfn "⚠️  No GPX content generated for %s" collection.Title
                | None -> 
                    () // No GPX file expected for this collection
                
                printfn "✅ Built collection: %s (%d items)" collection.Title (data.Items.Length)
                
            with
            | ex -> 
                printfn "❌ Error building collection %s: %s" collection.Title ex.Message
    
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
    
    let buildTagsPages (posts: Post array) (notes: Post array) (responses: Response array) = 

        let taggedPosts = 
            processTaggedPost posts
            |> Array.map(fun (t,p) -> 
                let sortedPosts = p |> Array.sortByDescending(fun x -> DateTime.Parse(x.Metadata.Date))
                t, sortedPosts)
            |> Array.sortBy(fst)

        let taggedNotes = 
            processTaggedPost notes
            |> Array.map(fun (t,p) -> 
                let sortedNotes = p |> Array.sortByDescending(fun x -> DateTime.Parse(x.Metadata.Date))
                t,sortedNotes)
            |> Array.sortBy(fst)

        let taggedResponses = 
            processTaggedResponse responses
            |> Array.map(fun (t,p) -> 
                let sortedResponses = p |> Array.sortByDescending(fun x -> DateTime.Parse(x.Metadata.DatePublished))
                t,sortedResponses)
            |> Array.sortBy(fst)

        let postTagNames = 
            taggedPosts |> Array.map(fst)

        let noteTagNames = 
            taggedNotes |> Array.map(fst)

        let responseTagNames = 
            taggedResponses |> Array.map(fst)

        let tagNames = 
            postTagNames 
            |> Array.append noteTagNames
            |> Array.append responseTagNames
            |> Array.distinct
            |> Array.sort  

        let mutable taggedPostDict = 
            tagNames
            |> Array.map(fun n -> (n, {Posts=[||];Notes=[||];Responses=[||]}))
            |> Map

        taggedPosts
        |> Array.iter(fun (t,c) -> 
            taggedPostDict <- taggedPostDict.Add(t,{taggedPostDict[t] with Posts=c}))

        taggedNotes
        |> Array.iter(fun (t,c) -> 
            taggedPostDict <- taggedPostDict.Add(t,{taggedPostDict[t] with Notes=c}))

        taggedResponses
        |> Array.iter(fun (t,c) -> 
            taggedPostDict <- taggedPostDict.Add(t,{taggedPostDict[t] with Responses=c}))

        let combinedTaggedPosts = 
            taggedPostDict
            |> Map.toArray

        let tagPage = generate (allTagsView tagNames) "default" "Tags - Luis Quintanilla"

        let saveDir = Path.Join(outputDir,"tags")
        File.WriteAllText(Path.Join(saveDir,"index.html"), tagPage)

        combinedTaggedPosts
        |> Array.iter(fun (tag,pc)-> 
            let sanitizedTag = sanitizeTagForPath tag
            let individualTagSaveDir = Path.Join(saveDir, sanitizedTag)
            Directory.CreateDirectory(individualTagSaveDir) |> ignore
            let individualTagPage = generate (individualTagView tag pc.Posts pc.Notes pc.Responses) "default" $"{tag} - Tags - Luis Quintanilla"
            File.WriteAllText(Path.Join(individualTagSaveDir,"index.html"),individualTagPage)
        )

    /// Enhanced unified tag processing function supporting all ITaggable content types
    let buildUnifiedTagsPages (contentArrays: (string * ITaggable array) list) = 
        // Process all tagged content using unified ITaggable interface
        let processTaggedContent (contentType: string) (items: ITaggable array) =
            items
            |> Array.filter (fun item -> item.Tags <> null && item.Tags.Length > 0)
            |> Array.collect (fun item -> 
                item.Tags |> Array.map (fun tag -> (TagService.processTagName tag, item)))
            |> Array.groupBy fst
            |> Array.map (fun (tag, items) -> 
                let sortedItems = items |> Array.map snd |> Array.sortByDescending (fun x -> DateTime.Parse(x.Date))
                (tag, contentType, sortedItems))

        // Process all content types
        let allTaggedContent = 
            contentArrays
            |> List.collect (fun (contentType, items) -> 
                processTaggedContent contentType items |> Array.toList)
            |> List.toArray

        // Group by tag name across all content types
        let groupedByTag = 
            allTaggedContent
            |> Array.groupBy (fun (tag, _, _) -> tag)
            |> Array.map (fun (tag, tagGroups) -> 
                let contentByType = 
                    tagGroups 
                    |> Array.map (fun (_, contentType, items) -> (contentType, items))
                    |> Array.groupBy fst
                    |> Array.map (fun (contentType, groups) -> 
                        let allItems = groups |> Array.collect snd
                        (contentType, allItems))
                (tag, contentByType))
            |> Array.sortBy fst

        // Create tag directory structure and generate pages
        let saveDir = Path.Join(outputDir, "tags")
        Directory.CreateDirectory(saveDir) |> ignore

        // Generate individual tag pages for unified content
        groupedByTag
        |> Array.iter (fun (tag, contentByType) -> 
            let sanitizedTag = sanitizeTagForPath tag
            let individualTagSaveDir = Path.Join(saveDir, sanitizedTag)
            Directory.CreateDirectory(individualTagSaveDir) |> ignore
            
            // Prepare content arrays with proper prefixes and display names
            let taggedContentForView = 
                contentByType
                |> Array.collect (fun (contentType, items) -> 
                    match contentType with
                    | "responses" -> 
                        // Handle bookmark responses separately
                        let regularResponses = items |> Array.filter (fun item -> 
                            match item with
                            | :? Response as r -> r.Metadata.ResponseType <> "bookmark"
                            | _ -> true)
                        let bookmarkResponses = items |> Array.filter (fun item -> 
                            match item with
                            | :? Response as r -> r.Metadata.ResponseType = "bookmark"
                            | _ -> false)
                        
                        // Create separate entries for regular responses and bookmarks
                        let responseEntries = 
                            if regularResponses.Length > 0 then [(regularResponses, "responses", "Responses")] else []
                        let bookmarkEntries = 
                            if bookmarkResponses.Length > 0 then [(bookmarkResponses, "bookmarks", "Bookmarks")] else []
                        
                        Array.ofList (responseEntries @ bookmarkEntries)
                    | _ ->
                        let (prefix, displayName) = 
                            match contentType with
                            | "posts" -> ("posts", "Blogs")
                            | "notes" -> ("notes", "Notes") 
                            | "snippets" -> ("snippets", "Snippets")
                            | "wikis" -> ("wiki", "Wiki")
                            | "presentations" -> ("resources/presentations", "Presentations")
                            | "albums" -> ("media/albums", "Albums")
                            | _ -> (contentType, contentType)
                        [|(items, prefix, displayName)|]
                    )
                |> Array.toList
            
            let individualTagPage = generate (individualTagViewUnified tag taggedContentForView) "default" $"{tag} - Tags - Luis Quintanilla"
            File.WriteAllText(Path.Join(individualTagSaveDir, "index.html"), individualTagPage))

        // Generate main tags index page
        let allTagNames = groupedByTag |> Array.map fst
        let tagPage = generate (allTagsView allTagNames) "default" "Tags - Luis Quintanilla"
        File.WriteAllText(Path.Join(saveDir, "index.html"), tagPage)

        printfn "✅ Unified tags pages created for %d tags across %d content types" allTagNames.Length contentArrays.Length

    let buildEventPage () = 
        let events =  
            File.ReadAllText(Path.Join("Data","events.json"))
            |> JsonSerializer.Deserialize<Event array>
            |> Array.sortByDescending(fun x -> DateTime.Parse(x.Date))

        let eventPage = generate (eventView events) "default" "Events - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"events")
        Directory.CreateDirectory(saveDir) |> ignore
        File.WriteAllText(Path.Join(saveDir,"index.html"),eventPage)

    let filterFeedByPostType (posts: Post array) (postType: string) = 
        posts |> Array.filter(fun post -> post.Metadata.PostType = postType)

    let buildLiveStreamPage () = 
        let title = "Live Stream - Luis Quintanilla"
        let page = generate (liveStreamView title) "default" title
        let saveDir = Path.Join(outputDir,"live")
        Directory.CreateDirectory(saveDir) |> ignore

        File.WriteAllText(Path.Join(saveDir,"index.html"), page)

    let buildLiveStreamsPage (streams: Livestream array) = 
        let liveStreamsPage = generate (liveStreamsView streams) "defaultindex" "Live Stream Recordings - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"streams")
        Directory.CreateDirectory(saveDir) |> ignore
        File.WriteAllText(Path.Join(saveDir,"index.html"),liveStreamsPage)

    let buildLiveStreamPages (streams:Livestream array) = 
        streams
        |> Array.iter(fun stream ->
            let rootSaveDir = Path.Join(outputDir,"streams")
            let html = liveStreamPageView {stream with Content = stream.Content |> convertMdToHtml}
            let streamView = generate  html "defaultindex" $"Live Stream Recording | {stream.Metadata.Title} | Luis Quintanilla"
            let saveDir = Path.Join(rootSaveDir,$"{stream.FileName}")
            Directory.CreateDirectory(saveDir) |> ignore
            File.WriteAllText(Path.Join(saveDir,"index.html"),streamView))

    // AST-based snippet processing using GenericBuilder infrastructure
    let buildSnippets() = 
        let snippetFiles = 
            Directory.GetFiles(Path.Join(srcDir, "resources", "snippets"))
            |> Array.filter (fun f -> f.EndsWith(".md"))
            |> Array.toList
        
        let processor = GenericBuilder.SnippetProcessor.create()
        let feedData = GenericBuilder.buildContentWithFeeds processor snippetFiles
        
        // Get all snippets for related content calculation
        let allSnippets = feedData |> List.map (fun item -> item.Content) |> List.toArray
        
        // Generate individual snippet pages
        feedData
        |> List.iter (fun item ->
            let snippet = item.Content
            let saveDir = Path.Join(outputDir, "resources", "snippets", snippet.FileName)
            Directory.CreateDirectory(saveDir) |> ignore
            
            let snippetTags = 
                if String.IsNullOrEmpty(snippet.Metadata.Tags) then [||]
                else snippet.Metadata.Tags.Split(',') |> Array.map (fun s -> s.Trim())
            
            // Find related snippets for this snippet (limit to 5)
            let relatedSnippets = RelatedContentService.findRelatedContent snippet allSnippets 5
            
            let html = snippetPageView snippet.Metadata.Title (snippet.Content |> convertMdToHtml) snippet.Metadata.CreatedDate snippet.FileName snippetTags relatedSnippets
            let snippetView = generate html "defaultindex" $"Snippet | {snippet.Metadata.Title} | Luis Quintanilla"
            let saveFileName = Path.Join(saveDir, "index.html")
            File.WriteAllText(saveFileName, snippetView))
        
        // Generate snippet index page using existing view for now
        let snippets = feedData |> List.map (fun item -> item.Content) |> List.toArray
        let snippetIndexHtml = generate (snippetsView snippets) "defaultindex" "Snippets | Luis Quintanilla"
        let indexSaveDir = Path.Join(outputDir, "resources", "snippets")
        Directory.CreateDirectory(indexSaveDir) |> ignore
        File.WriteAllText(Path.Join(indexSaveDir, "index.html"), snippetIndexHtml)
        
        // Return feed data for potential RSS generation
        feedData

    // AST-based wiki processing using GenericBuilder infrastructure  
    let buildWikis() = 
        let wikiFiles = 
            Directory.GetFiles(Path.Join(srcDir, "resources", "wiki"))
            |> Array.filter (fun f -> f.EndsWith(".md"))
            |> Array.toList
        
        let processor = GenericBuilder.WikiProcessor.create()
        let feedData = GenericBuilder.buildContentWithFeeds processor wikiFiles
        
        // Get all wikis for related content calculation
        let allWikis = feedData |> List.map (fun item -> item.Content) |> List.toArray
        
        // Generate individual wiki pages
        feedData
        |> List.iter (fun item ->
            let wiki = item.Content
            let saveDir = Path.Join(outputDir, "resources", "wiki", wiki.FileName)
            Directory.CreateDirectory(saveDir) |> ignore
            
            let wikiTags = 
                if String.IsNullOrEmpty(wiki.Metadata.Tags) then [||]
                else wiki.Metadata.Tags.Split(',') |> Array.map (fun s -> s.Trim())
            
            // Find related wikis for this wiki (limit to 5)
            let relatedWikis = RelatedContentService.findRelatedContent wiki allWikis 5
            
            let html = wikiPageView wiki.Metadata.Title (wiki.Content |> convertMdToHtml) wiki.Metadata.LastUpdatedDate wiki.FileName wikiTags relatedWikis
            let wikiView = generate html "defaultindex" $"{wiki.Metadata.Title} | Wiki | Luis Quintanilla"
            let saveFileName = Path.Join(saveDir, "index.html")
            File.WriteAllText(saveFileName, wikiView))
        
        // Generate wiki index page using existing view
        let wikis = feedData |> List.map (fun item -> item.Content) |> List.toArray |> Array.sortBy(fun x -> x.Metadata.Title)
        let wikiIndexHtml = generate (wikisView wikis) "defaultindex" "Wiki | Luis Quintanilla"
        let indexSaveDir = Path.Join(outputDir, "resources", "wiki")
        Directory.CreateDirectory(indexSaveDir) |> ignore
        File.WriteAllText(Path.Join(indexSaveDir, "index.html"), wikiIndexHtml)
        
        // Return feed data for potential RSS generation
        feedData

    // AST-based presentation processing using GenericBuilder infrastructure
    let buildPresentations() = 
        // Use helper to get files
        let presentationFiles = 
            Directory.GetFiles(Path.Join(srcDir, "resources", "presentations"))
            |> Array.filter (fun f -> f.EndsWith(".md"))
            |> Array.toList
        
        let processor = GenericBuilder.PresentationProcessor.create()
        let feedData = GenericBuilder.buildContentWithFeeds processor presentationFiles
        
        // Generate individual presentation pages
        feedData
        |> List.iter (fun item ->
            let presentation = item.Content
            let saveDir = Path.Join(outputDir, "resources", "presentations", presentation.FileName)
            
            // Use standard individual post layout like other content types
            let html = LayoutViews.presentationPageView presentation
            let page = generate html "defaultindex" $"{presentation.Metadata.Title} | Presentation | Luis Quintanilla"
            // Use helper to write file
            writePageToDir saveDir "index.html" page)
        
        // Generate presentation index page
        let presentations = feedData |> List.map (fun item -> item.Content) |> List.toArray
        let presentationIndexHtml = generate (presentationsView presentations) "defaultindex" "Presentations | Luis Quintanilla"
        let indexDir = Path.Join(outputDir, "resources", "presentations")
        // Use helper to write file
        writePageToDir indexDir "index.html" presentationIndexHtml
        
        // Return feed data for unified RSS generation
        feedData

    // AST-based book processing using GenericBuilder infrastructure
    let buildBooks() = 
        // Use helper to get files (reviews/library is a subdirectory)
        let bookFiles = 
            Directory.GetFiles(Path.Join(srcDir, "reviews", "library"))
            |> Array.filter (fun f -> f.EndsWith(".md"))
            |> Array.toList
        
        let processor = GenericBuilder.BookProcessor.create()
        let feedData = GenericBuilder.buildContentWithFeeds processor bookFiles
        
        // Generate individual book pages
        feedData
        |> List.iter (fun item ->
            let book = item.Content
            let saveDir = Path.Join(outputDir, "reviews", book.FileName)
            
            // Get review metadata for the image URL
            let reviewMetadata = GenericBuilder.BookProcessor.getReviewMetadata book.FileName
            let imageUrl = 
                match reviewMetadata with
                | Some rm -> rm.ImageUrl
                | None -> 
                    // Fallback to cover from book metadata
                    if String.IsNullOrWhiteSpace(book.Metadata.Cover) then None
                    else Some book.Metadata.Cover
            
            let html = reviewPageView book.Metadata.Title (book.Content |> convertMdToHtml) book.Metadata.DatePublished book.FileName imageUrl
            let page = generate html "defaultindex" $"{book.Metadata.Title} | Reviews | Luis Quintanilla"
            // Use helper to write file
            writePageToDir saveDir "index.html" page)
        
        // Generate reviews index page using existing libraryView (rename later)
        let books = feedData |> List.map (fun item -> item.Content) |> List.toArray
        let reviewsIndexHtml = generate (libraryView books) "defaultindex" "Reviews | Luis Quintanilla"
        let indexDir = Path.Join(outputDir, "reviews")
        // Use helper to write file
        writePageToDir indexDir "index.html" reviewsIndexHtml
        
        // Return feed data for unified RSS generation
        feedData

    // AST-based post processing using GenericBuilder infrastructure
    let buildPosts() = 
        let postFiles = 
            Directory.GetFiles(Path.Join(srcDir, "posts"))
            |> Array.filter (fun f -> f.EndsWith(".md"))
            |> Array.toList
        
        let processor = GenericBuilder.PostProcessor.create()
        let feedData = GenericBuilder.buildContentWithFeeds processor postFiles
        
        // Get all posts for related content calculation
        let allPosts = feedData |> List.map (fun item -> item.Content) |> List.toArray
        
        // Generate individual post pages
        feedData
        |> List.iter (fun item ->
            let post = item.Content
            let saveDir = Path.Join(outputDir, "posts", post.FileName)
            Directory.CreateDirectory(saveDir) |> ignore
            
            // Find related posts for this post (limit to 5)
            let relatedPosts = RelatedContentService.findRelatedContent post allPosts 5
            
            let html = blogPostView post.Metadata.Title (post.Content |> convertMdToHtml) post.Metadata.Date post.FileName post.Metadata.Tags post.Metadata.ReadingTimeMinutes relatedPosts
            let postView = generate html "defaultindex" $"{post.Metadata.Title} - Luis Quintanilla"
            let saveFileName = Path.Join(saveDir, "index.html")
            File.WriteAllText(saveFileName, postView))
        
        // Generate posts index page at /posts/ (no pagination - show all like notes/responses)
        let posts = feedData |> List.map (fun item -> item.Content) |> List.toArray
        let sortedPosts = posts |> Array.sortByDescending(fun x -> DateTime.Parse(x.Metadata.Date))
        let postsIndexHtml = generate (feedView sortedPosts) "defaultindex" "Posts - Luis Quintanilla"
        let indexSaveDir = Path.Join(outputDir, "posts")
        Directory.CreateDirectory(indexSaveDir) |> ignore
        File.WriteAllText(Path.Join(indexSaveDir, "index.html"), postsIndexHtml)
        
        // Return feed data for unified RSS generation
        feedData

    // AST-based notes processing using GenericBuilder infrastructure
    let buildNotes() = 
        let noteFiles = 
            Directory.GetFiles(Path.Join(srcDir, "notes"))
            |> Array.filter (fun f -> f.EndsWith(".md"))
            |> Array.toList
        
        let processor = GenericBuilder.NoteProcessor.create()
        let feedData = GenericBuilder.buildContentWithFeeds processor noteFiles
        
        // Get all notes for related content calculation
        let allNotes = feedData |> List.map (fun item -> item.Content) |> List.toArray
        
        // Generate individual note pages at /notes/[slug]/
        feedData
        |> List.iter (fun item ->
            let note = item.Content
            let saveDir = Path.Join(outputDir, "notes", note.FileName)
            Directory.CreateDirectory(saveDir) |> ignore
            
            // Find related notes for this note (limit to 5)
            let relatedNotes = RelatedContentService.findRelatedContent note allNotes 5
            
            let html = LayoutViews.notePostView note.Metadata.Title (note.Content |> convertMdToHtml) note.Metadata.Date note.FileName note.Metadata.Tags note.Metadata.ReadingTimeMinutes relatedNotes
            let noteView = generate html "defaultindex" note.Metadata.Title
            let saveFileName = Path.Join(saveDir, "index.html")
            File.WriteAllText(saveFileName, noteView))
        
        // Generate notes index page at /notes/
        let notes = feedData |> List.map (fun item -> item.Content) |> List.toArray
        let sortedNotes = notes |> Array.sortByDescending(fun (x: Post) -> DateTime.Parse(x.Metadata.Date))
        let notesIndexHtml = generate (notesView sortedNotes) "defaultindex" "Notes - Luis Quintanilla"
        let indexSaveDir = Path.Join(outputDir, "notes")
        Directory.CreateDirectory(indexSaveDir) |> ignore
        File.WriteAllText(Path.Join(indexSaveDir, "index.html"), notesIndexHtml)
        
        // Return feed data for unified RSS generation
        feedData

    // AST-based responses processing using GenericBuilder infrastructure
    let buildResponses() = 
        // Use helper to get files
        let responseFiles = getContentFiles "responses"
        
        let processor = GenericBuilder.ResponseProcessor.create()
        let feedData = GenericBuilder.buildContentWithFeeds processor responseFiles
        
        // Generate individual response pages at /responses/[slug]/
        feedData
        |> List.iter (fun item ->
            let response = item.Content
            let saveDir = Path.Join(outputDir, "responses", response.FileName)
            
            let html = LayoutViews.responsePostView response.Metadata.Title (response.Content |> convertMdToHtml) response.Metadata.DatePublished response.FileName response.Metadata.TargetUrl response.Metadata.Tags response.Metadata.ReadingTimeMinutes response.Metadata.ResponseType response.Metadata.RsvpStatus
            let page = generate html "defaultindex" response.Metadata.Title
            // Use helper to write file
            writePageToDir saveDir "index.html" page)
        
        // Generate responses index page at /responses/
        let responses = feedData |> List.map (fun item -> item.Content) |> List.toArray
        let sortedResponses = responses |> Array.sortByDescending(fun (x: Response) -> DateTime.Parse(x.Metadata.DatePublished))
        
        // Create HTML index page for responses
        let indexHtml = generate (responseView sortedResponses) "defaultindex" "Responses - Luis Quintanilla"
        let indexDir = Path.Join(outputDir, "responses")
        // Use helper to write file
        writePageToDir indexDir "index.html" indexHtml
        
        // Return feed data for unified RSS generation
        feedData

    // Generate bookmarks landing page from bookmark-type responses
    let buildBookmarksLandingPage (responsesFeedData: GenericBuilder.FeedData<Response> list) =
        // Filter for bookmark-type responses only
        let bookmarkResponses = 
            responsesFeedData 
            |> List.map (fun item -> item.Content)
            |> List.filter (fun response -> response.Metadata.ResponseType = "bookmark")
            |> List.sortByDescending (fun response -> DateTime.Parse(response.Metadata.DatePublished))
            |> List.toArray
        
        // Create the bookmarks landing page using bookmarkResponseView (which handles Response arrays but displays as bookmarks)
        let bookmarksLandingHtml = generate (bookmarkResponseView bookmarkResponses) "defaultindex" "Bookmarks - Luis Quintanilla"
        let bookmarksIndexDir = Path.Join(outputDir, "bookmarks")
        // Use helper to write file
        writePageToDir bookmarksIndexDir "index.html" bookmarksLandingHtml
        
        printfn "✅ Bookmarks landing page created with %d bookmark responses" bookmarkResponses.Length

    // AST-based media processing using GenericBuilder infrastructure
    let buildMedia() = 
        // Use helper to get files
        let mediaFiles = getContentFiles "media"
        
        let processor = GenericBuilder.AlbumProcessor.create()
        let feedData = GenericBuilder.buildContentWithFeeds processor mediaFiles
        
        // Generate individual media pages
        feedData
        |> List.iter (fun item ->
            let album = item.Content
            let saveDir = Path.Join(outputDir, "media", album.FileName)

            // Use AST-based markdown processing for custom blocks
            let rawContent = processor.Render album
            let processedContent = MarkdownService.convertMdToHtml rawContent
            let html = mediaPageView album.Metadata.Title processedContent album.Metadata.Date album.FileName album.Metadata.Tags
            let page = generate html "defaultindex" $"{album.Metadata.Title} | Media | Luis Quintanilla"
            // Use helper to write file
            writePageToDir saveDir "index.html" page)
        
        // Generate media index page using simple list view for consistency
        try
            let albums = feedData |> List.map (fun item -> item.Content) |> List.toArray
            let sortedAlbums = albums |> Array.sortByDescending(fun x -> DateTimeOffset.Parse(x.Metadata.Date))
            let mediaIndexHtml = generate (albumsPageView sortedAlbums) "defaultindex" "Media | Luis Quintanilla"
            let indexDir = Path.Join(outputDir, "media")
            // Use helper to write file
            writePageToDir indexDir "index.html" mediaIndexHtml
        with
        | ex -> 
            printfn $"Error in media unified feed view: {ex.Message}"
            printfn $"Stack trace: {ex.StackTrace}"
            printfn $"Feed data count: {feedData.Length}"
            reraise()
        
        // Return feed data for unified RSS generation
        feedData

    let buildAlbumCollections() = 
        let albumCollectionFiles = 
            let albumPath = Path.Join(srcDir, "albums")
            if Directory.Exists(albumPath) then
                Directory.GetFiles(albumPath)
                |> Array.filter (fun f -> f.EndsWith(".md"))
                |> Array.toList
            else
                []
        
        let processor = GenericBuilder.AlbumCollectionProcessor.create()
        let feedData = GenericBuilder.buildContentWithFeeds processor albumCollectionFiles
        
        // Generate individual album collection pages at /collections/albums/[slug]/
        feedData
        |> List.iter (fun item ->
            let albumCollection = item.Content
            let saveDir = Path.Join(outputDir, "collections", "albums", albumCollection.FileName)

            // Use AST-based markdown processing for custom blocks
            let rawContent = processor.Render albumCollection
            let processedContent = MarkdownService.convertMdToHtml rawContent
            let html = albumCollectionDetailView albumCollection processedContent
            let page = generate html "defaultindex" $"{albumCollection.Metadata.Title} | Albums | Luis Quintanilla"
            // Use helper to write file
            writePageToDir saveDir "index.html" page)
        
        // Generate album collections index page
        try
            let albumCollections = feedData |> List.map (fun item -> item.Content) |> List.toArray
            let albumsIndexHtml = generate (albumCollectionsPageView albumCollections) "defaultindex" "Albums | Luis Quintanilla"
            let indexDir = Path.Join(outputDir, "collections", "albums")
            // Use helper to write file
            writePageToDir indexDir "index.html" albumsIndexHtml
            printfn "✅ Album collections index page created with %d albums" albumCollections.Length
        with
        | ex -> 
            printfn $"Error creating album collections index: {ex.Message}"
            reraise()
        
        // Return feed data for unified RSS generation
        feedData

    let buildPlaylistCollections() = 
        let playlistCollectionFiles = 
            let playlistPath = Path.Join(srcDir, "playlists")
            if Directory.Exists(playlistPath) then
                Directory.GetFiles(playlistPath)
                |> Array.filter (fun f -> f.EndsWith(".md"))
                |> Array.toList
            else
                []
        
        let processor = GenericBuilder.PlaylistCollectionProcessor.create()
        let feedData = GenericBuilder.buildContentWithFeeds processor playlistCollectionFiles
        
        // Generate individual playlist collection pages at /collections/playlists/[slug]/
        feedData
        |> List.iter (fun item ->
            let playlistCollection = item.Content
            let saveDir = Path.Join(outputDir, "collections", "playlists", playlistCollection.FileName)

            // Use AST-based markdown processing
            let rawContent = processor.Render playlistCollection
            let processedContent = MarkdownService.convertMdToHtml rawContent
            let html = playlistCollectionDetailView playlistCollection processedContent
            let page = generate html "defaultindex" $"{playlistCollection.Metadata.Title} | Playlists | Luis Quintanilla"
            // Use helper to write file
            writePageToDir saveDir "index.html" page)
        
        // Generate playlist collections index page
        try
            let playlistCollections = 
                feedData 
                |> List.map (fun item -> item.Content) 
                |> List.toArray
                |> Array.sortByDescending (fun x -> DateTimeOffset.Parse(x.Metadata.Date))
            let playlistsIndexHtml = generate (playlistCollectionsPageView playlistCollections) "defaultindex" "Playlists | Luis Quintanilla"
            let indexDir = Path.Join(outputDir, "collections", "playlists")
            // Use helper to write file
            writePageToDir indexDir "index.html" playlistsIndexHtml
            printfn "✅ Playlist collections index page created with %d playlists" playlistCollections.Length
        with
        | ex -> 
            printfn $"Error creating playlist collections index: {ex.Message}"
            reraise()
        
        // Return feed data for unified RSS generation
        feedData

    // Build unified feed HTML page with all content types
    let buildUnifiedFeedPage (allUnifiedItems: (string * GenericBuilder.UnifiedFeeds.UnifiedFeedItem list) list) =
        // Flatten all feed items and sort chronologically
        let flattenedItems = 
            allUnifiedItems
            |> List.collect snd
            |> List.sortByDescending (fun item -> DateTime.Parse(item.Date))
            |> List.take (min 30 (allUnifiedItems |> List.collect snd |> List.length)) // Limit to 30 items
            |> List.toArray
        
        // Generate the unified feed page
        let unifiedFeedHtml = generate (enhancedSubscriptionHubView flattenedItems) "defaultindex" "Feeds & Content Discovery - Luis Quintanilla"
        let feedIndexDir = Path.Join(outputDir, "feed")
        Directory.CreateDirectory(feedIndexDir) |> ignore
        File.WriteAllText(Path.Join(feedIndexDir, "index.html"), unifiedFeedHtml)
        
        printfn "✅ Unified feed page created at /feed/index.html with %d items" flattenedItems.Length

    // AST-based bookmark processing using GenericBuilder infrastructure
    // Note: Bookmarks are Response objects with response_type: "bookmark"
    let buildBookmarks() = 
        // Use helper to get files
        let bookmarkFiles = getContentFiles "bookmarks"
        
        // Use Response processor since bookmarks are Response objects
        let processor = GenericBuilder.ResponseProcessor.create()
        let feedData = GenericBuilder.buildContentWithFeeds processor bookmarkFiles
        
        // Generate individual bookmark pages at /bookmarks/[slug]/
        feedData
        |> List.iter (fun item ->
            let response = item.Content
            let saveDir = Path.Join(outputDir, "bookmarks", response.FileName)
            
            let html = LayoutViews.responsePostView response.Metadata.Title (response.Content |> convertMdToHtml) response.Metadata.DatePublished response.FileName response.Metadata.TargetUrl response.Metadata.Tags response.Metadata.ReadingTimeMinutes response.Metadata.ResponseType response.Metadata.RsvpStatus
            let page = generate html "defaultindex" response.Metadata.Title
            // Use helper to write file
            writePageToDir saveDir "index.html" page)
        
        // Return feed data for unified RSS generation (converted to Response feed data format)
        feedData
