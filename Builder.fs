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
    open ViewGenerator
    open PartialViews
    
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
        // Content-type stratified sampling: Take first 5 items from each content type
        // This ensures equal representation and better content discovery in initial load
        let stratifiedInitialItems = 
            allUnifiedItems
            |> List.collect (fun (contentType, items) ->
                items 
                |> List.sortByDescending (fun item -> DateTime.Parse(item.Date))
                |> List.take (min 5 items.Length) // Take top 5 from each content type
            )
            |> List.sortByDescending (fun item -> DateTime.Parse(item.Date)) // Sort mixed types chronologically
        
        // Group remaining items by content type for type-aware progressive loading
        let remainingItemsByType = 
            allUnifiedItems
            |> List.map (fun (contentType, items) ->
                let sortedItems = items |> List.sortByDescending (fun item -> DateTime.Parse(item.Date))
                let remainingItems = sortedItems |> List.skip (min 5 items.Length)
                (contentType, remainingItems)
            )
            |> List.filter (fun (_, items) -> not items.IsEmpty)
        
        printfn "Debug: About to render timeline with %d initial items from stratified sampling" stratifiedInitialItems.Length
        printfn "Debug: Remaining items by type: %s" 
            (remainingItemsByType |> List.map (fun (t, items) -> $"{t}:{items.Length}") |> String.concat ", ")
        
        // Generate the timeline homepage with stratified approach
        let timelineHomePage = generate (LayoutViews.timelineHomeViewStratified (stratifiedInitialItems |> List.toArray) remainingItemsByType) "default" "Luis Quintanilla - Personal Website"
        File.WriteAllText(Path.Join(outputDir,"index.html"), timelineHomePage)
        
        let totalItems = allUnifiedItems |> List.sumBy (fun (_, items) -> items.Length)
        printfn "✅ Timeline homepage created with %d initial items (stratified) from %d total items across all content types" stratifiedInitialItems.Length totalItems

    let buildAboutPage () = 

        let aboutContent = convertFileToHtml (Path.Join(srcDir,"about.md")) |> contentView
        let aboutPage = generate aboutContent "default" "About - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"about")
        Directory.CreateDirectory(saveDir) |> ignore
        File.WriteAllText(Path.Join(saveDir,"index.html"), aboutPage)

    let buildStarterPackPage () = 

        let starterContent = convertFileToHtml (Path.Join(srcDir,"starter-packs.md")) |> contentView
        let starterPage = generate starterContent "default" "Starter Packs - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"collections","starter-packs")
        Directory.CreateDirectory(saveDir) |> ignore
        File.WriteAllText(Path.Join(saveDir,"index.html"), starterPage)

    let buildBlogrollPage (links:Outline array) = 
        let blogRollContent = 
            links
            |> blogRollView
            
        let blogRollPage = generate blogRollContent "default" "Blogroll - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"collections","blogroll")
        Directory.CreateDirectory(saveDir) |> ignore
        File.WriteAllText(Path.Join(saveDir,"index.html"), blogRollPage)

    let buildPodrollPage (links:Outline array) = 
        let podrollContent = 
            links
            |> podRollView
            
        let podrollPage = generate podrollContent "default" "Podroll - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"collections","podroll")
        Directory.CreateDirectory(saveDir) |> ignore
        File.WriteAllText(Path.Join(saveDir,"index.html"), podrollPage)

    let buildForumsPage (links:Outline array) = 
        let forumContent = 
            links
            |> forumsView
            
        let forumsPage = generate forumContent "default" "Forums - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"collections","forums")
        Directory.CreateDirectory(saveDir) |> ignore
        File.WriteAllText(Path.Join(saveDir,"index.html"), forumsPage)

    let buildYouTubeChannelsPage (links:Outline array) = 
        let ytContent = 
            links
            |> youTubeFeedView

        let ytFeedPage = generate ytContent "default" "YouTube Channels - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"collections","youtube")
        Directory.CreateDirectory(saveDir) |> ignore
        File.WriteAllText(Path.Join(saveDir,"index.html"), ytFeedPage)

    let buildAIStarterPackPage (links:Outline array) = 
        let aiStarterPackContent = 
            links
            |> aiStarterPackFeedView

        let ytFeedPage = generate aiStarterPackContent "default" "AI Starter Pack - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"collections","starter-packs","ai")
        Directory.CreateDirectory(saveDir) |> ignore
        File.WriteAllText(Path.Join(saveDir,"index.html"), ytFeedPage)

    let buildIRLStackPage () = 
        let irlStackContent = Path.Join(srcDir,"uses.md") |> convertFileToHtml |> contentView
        let irlStackPage = generate irlStackContent "default" "In Real Life Stack - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"uses")
        Directory.CreateDirectory(saveDir) |> ignore   
        File.WriteAllText(Path.Join(saveDir,"index.html"), irlStackPage)

    let buildColophonPage () = 
        let colophonContent = Path.Join(srcDir,"colophon.md") |> convertFileToHtml |> contentView
        let colophonPage = generate colophonContent "default" "Colophon - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"colophon")
        Directory.CreateDirectory(saveDir) |> ignore               
        File.WriteAllText(Path.Join(saveDir,"index.html"), colophonPage)        

    let buildContactPage () = 
        let contactContent = convertFileToHtml (Path.Join(srcDir,"contact.md")) |> contentView
        let contactPage = generate contactContent "default" "Contact - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"contact")
        Directory.CreateDirectory(saveDir) |> ignore
        File.WriteAllText(Path.Join(saveDir, "index.html"), contactPage)

    let buildOnlineRadioPage () = 
        let onlineRadioContent = convertFileToHtml (Path.Join(srcDir,"radio.md")) |> contentView
        let onlineRadioPage = generate onlineRadioContent "default" "Online Radio - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"radio")
        Directory.CreateDirectory(saveDir) |> ignore

        // Copy playlist file
        File.Copy(Path.Join(srcDir,"OnlineRadioPlaylist.m3u"),Path.Join(saveDir,"OnlineRadioPlaylist.m3u"),true)
        
        // Write out page
        File.WriteAllText(Path.Join(saveDir,"index.html"), onlineRadioPage)        

    let buildFeedsOpml (links:Outline array) = 
        let feed = buildOpmlFeed "Luis Quintanilla Feeds" "https://www.luisquintanilla.me" links
        let saveDir = Path.Join(outputDir,"feed")
        // File.WriteAllText(Path.Join(saveDir,"index.xml"), feed.ToString())
        File.WriteAllText(Path.Join(saveDir,"index.opml"), feed.ToString())

    let buildBlogrollOpml (links:Outline array) = 
        let feed = buildOpmlFeed "Luis Quintanilla Blogroll" "https://www.luisquintanilla.me" links
        let saveDir = Path.Join(outputDir,"collections","blogroll")
        File.WriteAllText(Path.Join(saveDir,"index.xml"), feed.ToString())
        File.WriteAllText(Path.Join(saveDir,"index.opml"), feed.ToString())

    let buildPodrollOpml (links:Outline array) = 
        let feed = buildOpmlFeed "Luis Quintanilla Podroll" "https://www.lqdev.me" links
        let saveDir = Path.Join(outputDir,"collections","podroll")
        File.WriteAllText(Path.Join(saveDir,"index.xml"), feed.ToString())
        File.WriteAllText(Path.Join(saveDir,"index.opml"), feed.ToString())

    let buildForumsOpml (links:Outline array) = 
        let feed = buildOpmlFeed "Luis Quintanilla Forums" "https://www.lqdev.me" links
        let saveDir = Path.Join(outputDir,"collections","forums")
        File.WriteAllText(Path.Join(saveDir,"index.xml"), feed.ToString())
        File.WriteAllText(Path.Join(saveDir,"index.opml"), feed.ToString())

    let buildYouTubeOpml (links:Outline array) = 
        let feed = buildOpmlFeed "Luis Quintanilla YouTube Channels" "https://www.lqdev.me" links
        let saveDir = Path.Join(outputDir,"collections","youtube")
        File.WriteAllText(Path.Join(saveDir,"index.xml"), feed.ToString())
        File.WriteAllText(Path.Join(saveDir,"index.opml"), feed.ToString())

    let buildAIStarterPackOpml (links:Outline array) = 
        let feed = buildOpmlFeed "Luis Quintanilla AI Starter Pack" "https://www.lqdev.me" links
        let saveDir = Path.Join(outputDir,"collections","starter-packs","ai")
        Directory.CreateDirectory(saveDir) |> ignore
        File.WriteAllText(Path.Join(saveDir,"index.xml"), feed.ToString())
        File.WriteAllText(Path.Join(saveDir,"index.opml"), feed.ToString())
    
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
        
        // Generate individual snippet pages
        feedData
        |> List.iter (fun item ->
            let snippet = item.Content
            let saveDir = Path.Join(outputDir, "resources", "snippets", snippet.FileName)
            Directory.CreateDirectory(saveDir) |> ignore
            
            let html = snippetPageView snippet.Metadata.Title (snippet.Content |> convertMdToHtml) snippet.Metadata.CreatedDate snippet.FileName
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
        
        // Generate individual wiki pages
        feedData
        |> List.iter (fun item ->
            let wiki = item.Content
            let saveDir = Path.Join(outputDir, "resources", "wiki", wiki.FileName)
            Directory.CreateDirectory(saveDir) |> ignore
            
            let html = wikiPageView wiki.Metadata.Title (wiki.Content |> convertMdToHtml) wiki.Metadata.LastUpdatedDate wiki.FileName
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
            Directory.CreateDirectory(saveDir) |> ignore
            
            // Use standard individual post layout like other content types
            let html = LayoutViews.presentationPageView presentation
            let presentationView = generate html "defaultindex" $"{presentation.Metadata.Title} | Presentation | Luis Quintanilla"
            let saveFileName = Path.Join(saveDir, "index.html")
            File.WriteAllText(saveFileName, presentationView))
        
        // Generate presentation index page
        let presentations = feedData |> List.map (fun item -> item.Content) |> List.toArray
        let presentationIndexHtml = generate (presentationsView presentations) "defaultindex" "Presentations | Luis Quintanilla"
        let indexSaveDir = Path.Join(outputDir, "resources", "presentations")
        Directory.CreateDirectory(indexSaveDir) |> ignore
        File.WriteAllText(Path.Join(indexSaveDir, "index.html"), presentationIndexHtml)
        
        // Return feed data for unified RSS generation
        feedData

    // AST-based book processing using GenericBuilder infrastructure
    let buildBooks() = 
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
            Directory.CreateDirectory(saveDir) |> ignore
            
            let html = reviewPageView book.Metadata.Title (book.Content |> convertMdToHtml) book.Metadata.DatePublished book.FileName
            let bookView = generate html "defaultindex" $"{book.Metadata.Title} | Reviews | Luis Quintanilla"
            let saveFileName = Path.Join(saveDir, "index.html")
            File.WriteAllText(saveFileName, bookView))
        
        // Generate reviews index page using existing libraryView (rename later)
        let books = feedData |> List.map (fun item -> item.Content) |> List.toArray
        let reviewsIndexHtml = generate (libraryView books) "defaultindex" "Reviews | Luis Quintanilla"
        let indexSaveDir = Path.Join(outputDir, "reviews")
        Directory.CreateDirectory(indexSaveDir) |> ignore
        File.WriteAllText(Path.Join(indexSaveDir, "index.html"), reviewsIndexHtml)
        
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
        
        // Generate individual post pages
        feedData
        |> List.iter (fun item ->
            let post = item.Content
            let saveDir = Path.Join(outputDir, "posts", post.FileName)
            Directory.CreateDirectory(saveDir) |> ignore
            
            let html = blogPostView post.Metadata.Title (post.Content |> convertMdToHtml) post.Metadata.Date post.FileName
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
            Directory.GetFiles(Path.Join(srcDir, "feed"))
            |> Array.filter (fun f -> f.EndsWith(".md"))
            |> Array.toList
        
        let processor = GenericBuilder.NoteProcessor.create()
        let feedData = GenericBuilder.buildContentWithFeeds processor noteFiles
        
        // Generate individual note pages at /notes/[slug]/
        feedData
        |> List.iter (fun item ->
            let note = item.Content
            let saveDir = Path.Join(outputDir, "notes", note.FileName)
            Directory.CreateDirectory(saveDir) |> ignore
            
            let html = LayoutViews.notePostView note.Metadata.Title (note.Content |> convertMdToHtml) note.Metadata.Date note.FileName
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
        let responseFiles = 
            Directory.GetFiles(Path.Join(srcDir, "responses"))
            |> Array.filter (fun f -> f.EndsWith(".md"))
            |> Array.toList
        
        let processor = GenericBuilder.ResponseProcessor.create()
        let feedData = GenericBuilder.buildContentWithFeeds processor responseFiles
        
        // Generate individual response pages at /responses/[slug]/
        feedData
        |> List.iter (fun item ->
            let response = item.Content
            let saveDir = Path.Join(outputDir, "responses", response.FileName)
            Directory.CreateDirectory(saveDir) |> ignore
            
            let html = LayoutViews.responsePostView response.Metadata.Title (response.Content |> convertMdToHtml) response.Metadata.DatePublished response.FileName
            let responseView = generate html "defaultindex" response.Metadata.Title
            let saveFileName = Path.Join(saveDir, "index.html")
            File.WriteAllText(saveFileName, responseView))
        
        // Generate responses index page at /responses/
        let responses = feedData |> List.map (fun item -> item.Content) |> List.toArray
        let sortedResponses = responses |> Array.sortByDescending(fun (x: Response) -> DateTime.Parse(x.Metadata.DatePublished))
        
        // Create HTML index page for responses
        let responsesIndexHtml = generate (responseView sortedResponses) "defaultindex" "Responses - Luis Quintanilla"
        let responsesIndexSaveDir = Path.Join(outputDir, "responses")
        Directory.CreateDirectory(responsesIndexSaveDir) |> ignore
        File.WriteAllText(Path.Join(responsesIndexSaveDir, "index.html"), responsesIndexHtml)
        
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
        
        // Create the bookmarks landing page using responseView (which handles Response arrays)
        let bookmarksLandingHtml = generate (responseView bookmarkResponses) "defaultindex" "Bookmarks - Luis Quintanilla"
        let bookmarksIndexSaveDir = Path.Join(outputDir, "bookmarks")
        Directory.CreateDirectory(bookmarksIndexSaveDir) |> ignore
        File.WriteAllText(Path.Join(bookmarksIndexSaveDir, "index.html"), bookmarksLandingHtml)
        
        printfn "✅ Bookmarks landing page created with %d bookmark responses" bookmarkResponses.Length

    // AST-based media processing using GenericBuilder infrastructure
    let buildMedia() = 
        let mediaFiles = 
            Directory.GetFiles(Path.Join(srcDir, "media"))
            |> Array.filter (fun f -> f.EndsWith(".md"))
            |> Array.toList
        
        let processor = GenericBuilder.AlbumProcessor.create()
        let feedData = GenericBuilder.buildContentWithFeeds processor mediaFiles
        
        // Generate individual media pages
        feedData
        |> List.iter (fun item ->
            let album = item.Content
            let saveDir = Path.Join(outputDir, "media", album.FileName)
            Directory.CreateDirectory(saveDir) |> ignore

            // Use AST-based markdown processing for custom blocks
            let rawContent = processor.Render album
            let processedContent = MarkdownService.convertMdToHtml rawContent
            let html = contentViewWithTitle album.Metadata.Title processedContent
            let albumView = generate html "defaultindex" $"{album.Metadata.Title} | Media | Luis Quintanilla"
            let saveFileName = Path.Join(saveDir, "index.html")
            File.WriteAllText(saveFileName, albumView))
        
        // Generate media index page using simple list view for consistency
        try
            let albums = feedData |> List.map (fun item -> item.Content) |> List.toArray
            let mediaIndexHtml = generate (albumsPageView albums) "defaultindex" "Media | Luis Quintanilla"
            let indexSaveDir = Path.Join(outputDir, "media")
            Directory.CreateDirectory(indexSaveDir) |> ignore
            File.WriteAllText(Path.Join(indexSaveDir, "index.html"), mediaIndexHtml)
        with
        | ex -> 
            printfn $"Error in media unified feed view: {ex.Message}"
            printfn $"Stack trace: {ex.StackTrace}"
            printfn $"Feed data count: {feedData.Length}"
            reraise()
        
        // Return feed data for unified RSS generation
        feedData

    // Build redirect pages for URL migration
    let buildRedirectPages (redirects: RedirectDetails array) =
        redirects
        |> Array.iter (fun (sourceUrl, targetUrl, title) ->
            let redirectHtml = ViewGenerator.generateRedirect targetUrl title
            
            // Handle different redirect types
            if sourceUrl.EndsWith(".xml") || sourceUrl.EndsWith(".html") then
                // File-level redirect - create the exact file
                let saveFilePath = Path.Join(outputDir, sourceUrl.TrimStart('/'))
                let dirPath = Path.GetDirectoryName(saveFilePath)
                Directory.CreateDirectory(dirPath) |> ignore
                File.WriteAllText(saveFilePath, redirectHtml)
            else
                // Directory-level redirect - create index.html in directory
                let saveDir = Path.Join(outputDir, sourceUrl.TrimStart('/'))
                Directory.CreateDirectory(saveDir) |> ignore
                let saveFileName = Path.Join(saveDir, "index.html")
                File.WriteAllText(saveFileName, redirectHtml))

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
    let buildBookmarks() = 
        let bookmarkFiles = 
            Directory.GetFiles(Path.Join(srcDir, "bookmarks"))
            |> Array.filter (fun f -> f.EndsWith(".md"))
            |> Array.toList
        
        let processor = GenericBuilder.BookmarkProcessor.create()
        let feedData = GenericBuilder.buildContentWithFeeds processor bookmarkFiles
        
        // Generate individual bookmark pages at /bookmarks/[slug]/
        feedData
        |> List.iter (fun item ->
            let bookmark = item.Content
            let saveDir = Path.Join(outputDir, "bookmarks", bookmark.FileName)
            Directory.CreateDirectory(saveDir) |> ignore
            
            let html = bookmarkPostView bookmark
            let bookmarkView = generate html "defaultindex" bookmark.Metadata.Title
            let saveFileName = Path.Join(saveDir, "index.html")
            File.WriteAllText(saveFileName, bookmarkView))
        
        // Generate bookmarks index page at /bookmarks/
        let bookmarks = feedData |> List.map (fun item -> item.Content) |> List.toArray
        let sortedBookmarks = bookmarks |> Array.sortByDescending(fun (x: Bookmark) -> DateTime.Parse(x.Metadata.DatePublished))
        
        // Create HTML index page for bookmarks
        let bookmarksIndexHtml = generate (bookmarkView sortedBookmarks) "defaultindex" "Bookmarks - Luis Quintanilla"
        let bookmarksIndexSaveDir = Path.Join(outputDir, "bookmarks")
        Directory.CreateDirectory(bookmarksIndexSaveDir) |> ignore
        File.WriteAllText(Path.Join(bookmarksIndexSaveDir, "index.html"), bookmarksIndexHtml)
        
        // Return feed data for unified RSS generation
        feedData
