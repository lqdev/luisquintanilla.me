module Builder

    open System
    open System.Globalization
    open System.IO
    open System.IO.Compression
    open System.Linq
    open System.Text.Json
    open System.Text.Json.Nodes
    open System.Text.RegularExpressions
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
    open BuilderCommon

    type private ArchiveFeedItem = {
        Item: UnifiedFeeds.UnifiedFeedItem
        ContentHtml: string
    }

    type private ArchiveDownload = {
        FileName: string
        Label: string
        Description: string
        SizeBytes: int64
    }

    let buildHomePage (blogPosts:Post array) (feedPosts:Post array) (responsePosts:Response array)= 
        let recentBlog = 
            blogPosts 
            |> Array.sortByDescending(fun x-> DateTimeOffset.Parse(x.Metadata.Date))
            |> Array.head

        let recentFeedPost = 
            feedPosts
            |> Array.sortByDescending(fun x -> DateTimeOffset.Parse(x.Metadata.Date))
            |> Array.head

        let recentResponsePost = 
            responsePosts
            |> Array.sortByDescending(fun x -> DateTimeOffset.Parse(x.Metadata.DateUpdated))
            |> Array.head

        // let recentPostsContent = generatePartial (recentPostsView recentPosts)
        let homePage = generate (homeView recentBlog recentFeedPost recentResponsePost) "default" "Home - Luis Quintanilla"
        File.WriteAllText(Path.Join(outputDir,"index.html"),homePage)

    // New timeline homepage for Phase 3 - Feed-as-Homepage Interface
    let buildTimelineHomePage (allUnifiedItems: (string * UnifiedFeeds.UnifiedFeedItem list) list) =
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
        let timelineHomePage = generate (TimelineViews.timelineHomeViewStratified (initialItems |> List.toArray) remainingItemsByType pinnedUrls) "default" "Luis Quintanilla - Personal Website"
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

    let buildToolsPage () = 
        let toolsContent = Path.Join(srcDir,"tools.md") |> convertFileToHtml |> contentView
        let toolsPage = generate toolsContent "default" "Tools - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"tools")
        writePageToDir saveDir "index.html" toolsPage

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

    // Single source of truth lives in GenericBuilder.UnifiedFeeds; reuse it here
    // so BAR exports stay consistent with the JSON feed response stream.
    let private responseFeedTypes = UnifiedFeeds.responseStreamContentTypes
    let private maxUploadNameCollisions = 10000

    let private isRemoteUrl (value: string) =
        value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("//", StringComparison.OrdinalIgnoreCase)

    let private renderArchiveContent (item: UnifiedFeeds.UnifiedFeedItem) =
        match item.ContentType with
        | "posts"
        | "notes" -> convertMdToHtml item.Content
        | _ -> item.Content

    let private rewriteLocalImageSources (html: string) (registerUpload: string -> string option) =
        let imgPattern = @"(<img[^>]*\bsrc\s*=\s*[""'])([^""']+)([""'])"
        Regex.Replace(html, imgPattern, fun m ->
            let imgPrefix = m.Groups.[1].Value
            let imgSource = m.Groups.[2].Value
            let imgSuffix = m.Groups.[3].Value

            if isRemoteUrl imgSource then m.Value
            else
                match registerUpload imgSource with
                | Some archivedPath -> $"{imgPrefix}{archivedPath}{imgSuffix}"
                | None -> m.Value
        )

    let private toArchiveIsoDate (value: string) =
        if String.IsNullOrWhiteSpace(value) then DateTimeOffset.UtcNow.ToString("o")
        else
            try DateTimeOffset.Parse(value).ToString("o")
            with _ -> DateTimeOffset.UtcNow.ToString("o")

    let private formatArchiveDisplayDate (value: string) =
        if String.IsNullOrWhiteSpace(value) then DateTimeOffset.UtcNow.ToString("MMM dd, yyyy")
        else
            try DateTimeOffset.Parse(value).ToString("MMM dd, yyyy")
            with _ -> DateTimeOffset.UtcNow.ToString("MMM dd, yyyy")

    let private parseArchiveSortDate (value: string) =
        if String.IsNullOrWhiteSpace(value) then DateTimeOffset.MinValue
        else
            try DateTimeOffset.Parse(value)
            with _ -> DateTimeOffset.MinValue

    let private formatArchiveSize (sizeBytes: int64) =
        let oneKb = 1024.0
        let oneMb = oneKb * 1024.0
        if sizeBytes < int64 oneKb then $"{sizeBytes} B"
        elif sizeBytes < int64 oneMb then $"{(float sizeBytes / oneKb):F1} KB"
        else $"{(float sizeBytes / oneMb):F2} MB"

    let private buildArchiveLandingPage (downloads: ArchiveDownload list) =
        let archiveView =
            div [ _class "content-wrapper" ] [
                div [ _class "mr-auto" ] [
                    h2 [] [ Text "Blog Archive Format (.bar)" ]
                    p [] [
                        Text "Download portable blog backups in Blog Archive Format (BAR). "
                        Text "Each file contains an h-feed/h-entry index, JSON Feed v1.1 metadata, and bundled local image/media uploads when available."
                    ]
                    p [] [
                        Text "BAR files can be imported into platforms and tools that support blogarchive.org."
                    ]
                    ul [] [
                        for archive in downloads do
                            li [] [
                                a [ _href $"/archive/{archive.FileName}" ] [ Text archive.FileName ]
                                Text $" — {archive.Label} ({formatArchiveSize archive.SizeBytes})"
                                if not (String.IsNullOrWhiteSpace(archive.Description)) then
                                    Text $" — {archive.Description}"
                            ]
                    ]
                ]
            ]

        let archivePage = generate archiveView "defaultindex" "Archive Exports - Luis Quintanilla"
        writePageToDir (Path.Join(outputDir, "archive")) "index.html" archivePage

    let private generateBarFeedJson (archiveTitle: string) (homePageUrl: string) (entries: ArchiveFeedItem list) =
        let items = JsonArray()
        entries
        |> List.iter (fun entry ->
            let item = JsonObject()
            item["id"] <- entry.Item.Url
            item["url"] <- entry.Item.Url
            item["title"] <- entry.Item.Title
            item["content_html"] <- entry.ContentHtml
            item["date_published"] <- toArchiveIsoDate entry.Item.Date
            if not (isNull entry.Item.Tags) && entry.Item.Tags.Length > 0 then
                let tags = JsonArray()
                entry.Item.Tags |> Array.iter (fun tag -> tags.Add(tag))
                item["tags"] <- tags
            items.Add(item)
        )

        let author = JsonObject()
        author["name"] <- "Luis Quintanilla"
        author["url"] <- "https://www.lqdev.me"
        let authors = JsonArray()
        authors.Add(author)

        let feed = JsonObject()
        feed["version"] <- "https://jsonfeed.org/version/1.1"
        feed["title"] <- archiveTitle
        feed["home_page_url"] <- homePageUrl
        feed["feed_url"] <- "feed.json"
        feed["authors"] <- authors
        feed["items"] <- items

        feed.ToJsonString(JsonSerializerOptions(WriteIndented = true))

    let private buildBarIndexHtml (archiveTitle: string) (homePageUrl: string) (entries: ArchiveFeedItem list) =
        let content =
            html [] [
                head [] [
                    meta [ _charset "utf-8" ]
                    title [] [ Text archiveTitle ]
                ]
                body [] [
                    div [ _class "h-feed" ] [
                        h1 [ _class "p-name" ] [ Text archiveTitle ]
                        a [ _class "u-url"; _href homePageUrl ] []
                        for entry in entries do
                            article [ _class "h-entry" ] [
                                h2 [ _class "p-name" ] [ Text entry.Item.Title ]
                                a [ _class "u-url"; _href entry.Item.Url ] [ Text "Permalink" ]
                                time [ _class "dt-published"; _datetime (toArchiveIsoDate entry.Item.Date) ] [ Text (formatArchiveDisplayDate entry.Item.Date) ]
                                div [ _class "e-content" ] [
                                    rawText entry.ContentHtml
                                ]
                            ]
                    ]
                ]
            ]

        RenderView.AsString.htmlDocument content

    let private buildBarArchive (archiveName: string) (title: string) (homePageUrl: string) (items: UnifiedFeeds.UnifiedFeedItem list) =
        let archiveOutputDir = Path.Join(outputDir, "archive")
        Directory.CreateDirectory(archiveOutputDir) |> ignore

        let uploadPathBySource = Collections.Generic.Dictionary<string, string>()
        let usedUploadPaths = Collections.Generic.HashSet<string>()

        let registerUpload (imgSource: string) =
            let relativePathCandidate =
                let normalizedSource = imgSource.Trim().Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/')
                let canonicalUri = Uri($"https://www.lqdev.me/{normalizedSource.TrimStart('/')}")
                canonicalUri.AbsolutePath.TrimStart('/')

            if String.IsNullOrWhiteSpace(relativePathCandidate) then None
            else
                let pathSegments =
                    relativePathCandidate.Split('/', StringSplitOptions.RemoveEmptyEntries)
                    |> Array.toList

                let sourcePath =
                    pathSegments
                    |> List.fold (fun currentPath segment -> Path.Combine(currentPath, segment)) outputDir
                if File.Exists(sourcePath) then
                    if uploadPathBySource.ContainsKey(sourcePath) then
                        Some uploadPathBySource.[sourcePath]
                    else
                        let extension = Path.GetExtension(relativePathCandidate)
                        let baseName = Path.GetFileNameWithoutExtension(relativePathCandidate)
                        let mutable candidate = $"uploads/images/{baseName}{extension}"
                        let mutable duplicateCount = 1
                        while usedUploadPaths.Contains(candidate) && duplicateCount <= maxUploadNameCollisions do
                            candidate <- $"uploads/images/{baseName}-{duplicateCount}{extension}"
                            duplicateCount <- duplicateCount + 1

                        if duplicateCount > maxUploadNameCollisions then
                            failwith $"Unable to generate unique upload path for {relativePathCandidate}"

                        usedUploadPaths.Add(candidate) |> ignore
                        uploadPathBySource.[sourcePath] <- candidate
                        Some candidate
                else
                    None

        let archiveEntries =
            items
            |> List.sortByDescending (fun item -> parseArchiveSortDate item.Date)
            |> List.map (fun item ->
                let contentHtml = renderArchiveContent item |> fun html -> rewriteLocalImageSources html registerUpload
                { Item = item; ContentHtml = contentHtml }
            )

        let zipPath = Path.Join(archiveOutputDir, $"{archiveName}.bar")
        if File.Exists(zipPath) then File.Delete(zipPath)

        // ZIP DOS-time minimum is 1980-01-01; clamp to keep ZipArchiveEntry.LastWriteTime happy.
        let zipEpoch = DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero)
        let clampForZip (d: DateTimeOffset) = if d < zipEpoch then zipEpoch else d
        let mostRecentDate =
            archiveEntries
            |> List.tryHead
            |> Option.map (fun e -> parseArchiveSortDate e.Item.Date)
            |> Option.defaultValue zipEpoch
            |> clampForZip

        use archiveStream = File.Open(zipPath, FileMode.CreateNew, FileAccess.Write)
        use archive = new ZipArchive(archiveStream, ZipArchiveMode.Create)

        let indexEntry = archive.CreateEntry("index.html")
        indexEntry.LastWriteTime <- mostRecentDate
        do
            use indexWriter = new StreamWriter(indexEntry.Open())
            indexWriter.Write(buildBarIndexHtml title homePageUrl archiveEntries)

        let feedEntry = archive.CreateEntry("feed.json")
        feedEntry.LastWriteTime <- mostRecentDate
        do
            use feedWriter = new StreamWriter(feedEntry.Open())
            feedWriter.Write(generateBarFeedJson title homePageUrl archiveEntries)

        // Sort uploads by in-archive path so iteration order is deterministic
        // regardless of Dictionary insertion semantics.
        uploadPathBySource
        |> Seq.sortBy (fun kvp -> kvp.Value)
        |> Seq.iter (fun kvp ->
            let uploadEntry = archive.CreateEntry(kvp.Value)
            uploadEntry.LastWriteTime <- mostRecentDate
            use uploadEntryStream = uploadEntry.Open()
            use uploadSourceStream = File.OpenRead(kvp.Key)
            uploadSourceStream.CopyTo(uploadEntryStream)
        )

        FileInfo(zipPath).Length

    let buildBlogArchiveExports (feedDataSets: (string * (UnifiedFeeds.UnifiedFeedItem list)) list) =
        let postsItems = feedDataSets |> List.tryFind (fun (name, _) -> name = "posts") |> Option.map snd |> Option.defaultValue []
        let notesItems = feedDataSets |> List.tryFind (fun (name, _) -> name = "notes") |> Option.map snd |> Option.defaultValue []
        let responseItems =
            feedDataSets
            |> List.collect snd
            |> List.filter (fun item -> responseFeedTypes.Contains(item.ContentType))

        let allItems =
            [ postsItems; notesItems; responseItems ]
            |> List.concat
            |> List.sortByDescending (fun item -> parseArchiveSortDate item.Date)

        let downloads = [
            {
                FileName = "posts.bar"
                Label = "Posts"
                Description = "Long-form blog posts"
                SizeBytes = buildBarArchive "posts" "Luis Quintanilla — Posts" "https://www.lqdev.me/posts/" postsItems
            }
            {
                FileName = "notes.bar"
                Label = "Notes"
                Description = "Short-form notes and updates"
                SizeBytes = buildBarArchive "notes" "Luis Quintanilla — Notes" "https://www.lqdev.me/notes/" notesItems
            }
            {
                FileName = "responses.bar"
                Label = "Responses"
                Description = "Replies, stars, and reshares"
                SizeBytes = buildBarArchive "responses" "Luis Quintanilla — Responses" "https://www.lqdev.me/responses/" responseItems
            }
            {
                FileName = "all.bar"
                Label = "All"
                Description = "Combined posts, notes, and responses stream"
                SizeBytes = buildBarArchive "all" "Luis Quintanilla — All Streams" "https://www.lqdev.me/feed/" allItems
            }
        ]

        buildArchiveLandingPage downloads
        printfn "✅ Blog archive exports generated: posts.bar, notes.bar, responses.bar, all.bar"

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
    
    let buildEventPage () = 
        let events =  
            File.ReadAllText(Path.Join("Data","events.json"))
            |> JsonSerializer.Deserialize<Event array>
            |> Array.sortByDescending(fun x -> DateTimeOffset.Parse(x.Date))

        let eventPage = generate (eventView events) "default" "Events - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"events")
        Directory.CreateDirectory(saveDir) |> ignore
        File.WriteAllText(Path.Join(saveDir,"index.html"),eventPage)

    // AST-based snippet processing using GenericBuilder infrastructure
    let buildSnippets() =
        let snippetTags (snippet: Snippet) =
            if String.IsNullOrEmpty(snippet.Metadata.Tags) then [||]
            else snippet.Metadata.Tags.Split(',') |> Array.map (fun s -> s.Trim())
        BuildDriver.buildContentType srcDir outputDir {
            Name = ContentTypes.Snippets
            SourceDir = [ "resources"; "snippets" ]
            OutputDir = [ "resources"; "snippets" ]
            Processor = GenericBuilder.SnippetProcessor.create()
            Slug = fun snippet -> snippet.FileName
            ItemView = fun snippet allSnippets ->
                let relatedSnippets = RelatedContentService.findRelatedContent snippet allSnippets 5
                snippetPageView snippet.Metadata.Title (snippet.Content |> convertMdToHtml) snippet.Metadata.CreatedDate snippet.FileName (snippetTags snippet) relatedSnippets
            ItemTitle = fun snippet -> $"Snippet | {snippet.Metadata.Title} | Luis Quintanilla"
            Layout = "defaultindex"
            Index = Some { View = snippetsView; Title = "Snippets | Luis Quintanilla"; Sort = None }
        }

    // AST-based wiki processing using GenericBuilder infrastructure  
    let buildWikis() =
        let wikiTags (wiki: Wiki) =
            if String.IsNullOrEmpty(wiki.Metadata.Tags) then [||]
            else wiki.Metadata.Tags.Split(',') |> Array.map (fun s -> s.Trim())
        BuildDriver.buildContentType srcDir outputDir {
            Name = ContentTypes.Wiki
            SourceDir = [ "resources"; "wiki" ]
            OutputDir = [ "resources"; "wiki" ]
            Processor = GenericBuilder.WikiProcessor.create()
            Slug = fun wiki -> wiki.FileName
            ItemView = fun wiki allWikis ->
                let relatedWikis = RelatedContentService.findRelatedContent wiki allWikis 5
                wikiPageView wiki.Metadata.Title (wiki.Content |> convertMdToHtml) wiki.Metadata.LastUpdatedDate wiki.FileName (wikiTags wiki) relatedWikis
            ItemTitle = fun wiki -> $"{wiki.Metadata.Title} | Wiki | Luis Quintanilla"
            Layout = "defaultindex"
            Index = Some { View = wikisView; Title = "Wiki | Luis Quintanilla"; Sort = Some (Array.sortBy (fun (x: Wiki) -> x.Metadata.Title)) }
        }

    // AST-based AI Memex processing — load feed data only (no page generation)
    // AST-based presentation processing using GenericBuilder infrastructure
    let buildPresentations() =
        BuildDriver.buildContentType srcDir outputDir {
            Name = ContentTypes.Presentations
            SourceDir = [ "resources"; "presentations" ]
            OutputDir = [ "resources"; "presentations" ]
            Processor = GenericBuilder.PresentationProcessor.create()
            Slug = fun presentation -> presentation.FileName
            ItemView = fun presentation _ -> LayoutViews.presentationPageView presentation
            ItemTitle = fun presentation -> $"{presentation.Metadata.Title} | Presentation | Luis Quintanilla"
            Layout = "defaultindex"
            Index = Some { View = presentationsView; Title = "Presentations | Luis Quintanilla"; Sort = None }
        }

    // AST-based book processing using GenericBuilder infrastructure
    let buildBooks() =
        BuildDriver.buildContentType srcDir outputDir {
            Name = ContentTypes.Reviews
            SourceDir = [ "reviews"; "library" ]
            OutputDir = [ "reviews" ]
            Processor = GenericBuilder.BookProcessor.create()
            Slug = fun book -> book.FileName
            ItemView = fun book _ -> reviewPageView book.Metadata.Title (book.Content |> convertMdToHtml) book.Metadata.DatePublished book.FileName
            ItemTitle = fun book -> $"{book.Metadata.Title} | Reviews | Luis Quintanilla"
            Layout = "defaultindex"
            Index = Some { View = libraryView; Title = "Reviews | Luis Quintanilla"; Sort = None }
        }

    // AST-based post processing using GenericBuilder infrastructure
    let buildPosts() =
        BuildDriver.buildContentType srcDir outputDir {
            Name = ContentTypes.Posts
            SourceDir = [ "posts" ]
            OutputDir = [ "posts" ]
            Processor = GenericBuilder.PostProcessor.create()
            Slug = fun post -> post.FileName
            ItemView = fun post allPosts ->
                let relatedPosts = RelatedContentService.findRelatedContent post allPosts 5
                blogPostView post.Metadata.Title (post.Content |> convertMdToHtml) post.Metadata.Date post.FileName post.Metadata.Tags post.Metadata.ReadingTimeMinutes relatedPosts
            ItemTitle = fun post -> $"{post.Metadata.Title} - Luis Quintanilla"
            Layout = "defaultindex"
            Index = Some { View = feedView; Title = "Posts - Luis Quintanilla"; Sort = Some (Array.sortByDescending (fun (x: Post) -> DateTimeOffset.Parse(x.Metadata.Date))) }
        }

    // AST-based notes processing using GenericBuilder infrastructure
    let buildNotes() =
        BuildDriver.buildContentType srcDir outputDir {
            Name = ContentTypes.Notes
            SourceDir = [ "notes" ]
            OutputDir = [ "notes" ]
            Processor = GenericBuilder.NoteProcessor.create()
            Slug = fun note -> note.FileName
            ItemView = fun note allNotes ->
                let relatedNotes = RelatedContentService.findRelatedContent note allNotes 5
                LayoutViews.notePostView note.Metadata.Title (note.Content |> convertMdToHtml) note.Metadata.Date note.FileName note.Metadata.Tags note.Metadata.ReadingTimeMinutes relatedNotes
            ItemTitle = fun note -> note.Metadata.Title
            Layout = "defaultindex"
            Index = Some { View = notesView; Title = "Notes - Luis Quintanilla"; Sort = Some (Array.sortByDescending (fun (x: Post) -> DateTimeOffset.Parse(x.Metadata.Date))) }
        }

    // AST-based responses processing using GenericBuilder infrastructure
    let buildResponses() =
        BuildDriver.buildContentType srcDir outputDir {
            Name = ContentTypes.Responses
            SourceDir = [ "responses" ]
            OutputDir = [ "responses" ]
            Processor = GenericBuilder.ResponseProcessor.create()
            Slug = fun response -> response.FileName
            ItemView = fun response _ ->
                LayoutViews.responsePostView response.Metadata.Title (response.Content |> convertMdToHtml) response.Metadata.DatePublished response.FileName response.Metadata.TargetUrl response.Metadata.Tags response.Metadata.ReadingTimeMinutes response.Metadata.ResponseType response.Metadata.RsvpStatus
            ItemTitle = fun response -> response.Metadata.Title
            Layout = "defaultindex"
            Index = Some { View = responseView; Title = "Responses - Luis Quintanilla"; Sort = Some (Array.sortByDescending (fun (x: Response) -> DateTimeOffset.Parse(x.Metadata.DatePublished))) }
        }

    // Generate bookmarks landing page from bookmark-type responses
    let buildBookmarksLandingPage (responsesFeedData: GenericBuilder.FeedData<Response> list) =
        // Filter for bookmark-type responses only
        let bookmarkResponses = 
            responsesFeedData 
            |> List.map (fun item -> item.Content)
            |> List.filter (fun response -> response.Metadata.ResponseType = "bookmark")
            |> List.sortByDescending (fun response -> DateTimeOffset.Parse(response.Metadata.DatePublished))
            |> List.toArray
        
        // Create the bookmarks landing page using bookmarkResponseView (which handles Response arrays but displays as bookmarks)
        let bookmarksLandingHtml = generate (bookmarkResponseView bookmarkResponses) "defaultindex" "Bookmarks - Luis Quintanilla"
        let bookmarksIndexDir = Path.Join(outputDir, "bookmarks")
        // Use helper to write file
        writePageToDir bookmarksIndexDir "index.html" bookmarksLandingHtml
        
        printfn "✅ Bookmarks landing page created with %d bookmark responses" bookmarkResponses.Length

    // Generate /rsvp landing page from rsvp-type responses (temporal facet of responses;
    // detail pages remain at /responses/{file}/, so no URLs move).
    let buildRsvpLandingPage (responsesFeedData: GenericBuilder.FeedData<Response> list) =
        let rsvpResponses =
            responsesFeedData
            |> List.map (fun item -> item.Content)
            |> List.filter (fun response -> response.Metadata.ResponseType = "rsvp")
            |> List.sortByDescending (fun response -> DateTimeOffset.Parse(response.Metadata.DatePublished))
            |> List.toArray

        let rsvpLandingHtml = generate (rsvpView rsvpResponses) "defaultindex" "RSVPs - Luis Quintanilla"
        let rsvpIndexDir = Path.Join(outputDir, "rsvp")
        writePageToDir rsvpIndexDir "index.html" rsvpLandingHtml

        printfn "✅ RSVPs landing page created with %d rsvp responses" rsvpResponses.Length

    // AST-based media processing using GenericBuilder infrastructure
    let buildMedia() =
        let processor = GenericBuilder.AlbumProcessor.create()
        BuildDriver.buildContentType srcDir outputDir {
            Name = ContentTypes.Media
            SourceDir = [ "media" ]
            OutputDir = [ "media" ]
            Processor = processor
            Slug = fun album -> album.FileName
            ItemView = fun album _ ->
                mediaPageView album.Metadata.Title (processor.Render album |> convertMdToHtml) album.Metadata.Date album.FileName album.Metadata.Tags
            ItemTitle = fun album -> $"{album.Metadata.Title} | Media | Luis Quintanilla"
            Layout = "defaultindex"
            Index = Some { View = albumsPageView; Title = "Media | Luis Quintanilla"; Sort = Some (Array.sortByDescending (fun x -> DateTimeOffset.Parse(x.Metadata.Date))) }
        }

    let buildAlbumCollections() =
        let processor = GenericBuilder.AlbumCollectionProcessor.create()
        BuildDriver.buildContentType srcDir outputDir {
            Name = ContentTypes.AlbumCollection
            SourceDir = [ "albums" ]
            OutputDir = [ "collections"; "albums" ]
            Processor = processor
            Slug = fun albumCollection -> albumCollection.FileName
            ItemView = fun albumCollection _ ->
                albumCollectionDetailView albumCollection (processor.Render albumCollection |> convertMdToHtml)
            ItemTitle = fun albumCollection -> $"{albumCollection.Metadata.Title} | Albums | Luis Quintanilla"
            Layout = "defaultindex"
            Index = Some { View = albumCollectionsPageView; Title = "Albums | Luis Quintanilla"; Sort = None }
        }

    let buildPlaylistCollections() =
        let processor = GenericBuilder.PlaylistCollectionProcessor.create()
        BuildDriver.buildContentType srcDir outputDir {
            Name = ContentTypes.PlaylistCollection
            SourceDir = [ "playlists" ]
            OutputDir = [ "collections"; "playlists" ]
            Processor = processor
            Slug = fun playlistCollection -> playlistCollection.FileName
            ItemView = fun playlistCollection _ ->
                playlistCollectionDetailView playlistCollection (processor.Render playlistCollection |> convertMdToHtml)
            ItemTitle = fun playlistCollection -> $"{playlistCollection.Metadata.Title} | Playlists | Luis Quintanilla"
            Layout = "defaultindex"
            Index = Some { View = playlistCollectionsPageView; Title = "Playlists | Luis Quintanilla"; Sort = Some (Array.sortByDescending (fun x -> DateTimeOffset.Parse(x.Metadata.Date))) }
        }

    // Build unified feed HTML page with all content types
    let buildUnifiedFeedPage (allUnifiedItems: (string * UnifiedFeeds.UnifiedFeedItem list) list) =
        // Flatten all feed items and sort chronologically
        let flattenedItems = 
            allUnifiedItems
            |> List.collect snd
            |> List.sortByDescending (fun item -> DateTimeOffset.Parse(item.Date))
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
        // Bookmarks are Response objects; they have no index of their own — the
        // /bookmarks/ landing page is built by buildBookmarksLandingPage from
        // bookmark-type responses. Hence Index = None.
        BuildDriver.buildContentType srcDir outputDir {
            Name = ContentTypes.Bookmarks
            SourceDir = [ "bookmarks" ]
            OutputDir = [ "bookmarks" ]
            Processor = GenericBuilder.ResponseProcessor.create()
            Slug = fun response -> response.FileName
            ItemView = fun response _ ->
                LayoutViews.responsePostView response.Metadata.Title (response.Content |> convertMdToHtml) response.Metadata.DatePublished response.FileName response.Metadata.TargetUrl response.Metadata.Tags response.Metadata.ReadingTimeMinutes response.Metadata.ResponseType response.Metadata.RsvpStatus
            ItemTitle = fun response -> response.Metadata.Title
            Layout = "defaultindex"
            Index = None
        }
