/// Unified feed system for consistent feed generation across all content types.
/// Extracted from GenericBuilder.fs in refactor step 2.5 (F9 module split); pure move.
module UnifiedFeeds

    open Domain
    open ASTParsing
    open CustomBlocks
    open BlockRenderers
    open TagService
    open MarkdownService
    open ReadingTimeService
    open System.Xml.Linq
    open System
    open System.IO
    open System.Text.Json
    open System.Text.Json.Nodes
    open Giraffe.ViewEngine
    open Giraffe.ViewEngine.HtmlElements
    open Markdig
    open Markdig.Syntax
    open GenericBuilder

    // Phase 5C: ReviewMetadata is now defined at GenericBuilder module level for use by BookProcessor
    
    /// Unified feed item representation
    type UnifiedFeedItem = {
        Title: string
        Content: string
        Url: string
        Date: string
        ContentType: string
        Tags: string array
        RssXml: XElement
        // Phase 5A: Response semantics for ActivityPub
        ResponseType: string option  // "star", "reply", "reshare", "bookmark", "rsvp"
        TargetUrl: string option     // URL being responded to
        UpdatedDate: string option   // For edit tracking
        // Phase 6A: RSVP status for event responses
        RsvpStatus: string option    // "yes", "no", "maybe", "interested"
        // Phase 5C: Review metadata for Schema.org Review vocabulary
        ReviewData: ReviewMetadata option
        // Phase 5D: Media metadata for native Image/Video/Audio objects
        MediaData: MediaAPData option
        // B2 (F7): chrome-free body HTML the timeline composes structurally instead of
        // regex-stripping CardHtml. LAZY on purpose — forced only by the RENDER_V2 path
        // for items actually rendered, so flag-off triggers no extra convertMdToHtml
        // (no global-counter perturbation; output stays byte-identical).
        BodyHtml: Lazy<string>
    }

    /// Feed configuration for different feed types
    type FeedConfiguration = {
        Title: string
        Link: string
        Description: string
        OutputPath: string
        ContentType: string option  // None for fire-hose, Some("posts") for type-specific
    }
    
    /// Convert FeedData to UnifiedFeedItem
    let private convertToUnifiedItem<'T> (contentType: string) (feedData: FeedData<'T>) : UnifiedFeedItem option =
        match feedData.RssXml with
        | Some rssXml ->
            let title = 
                match rssXml.Element(XName.Get "title") with
                | null -> "Untitled"
                | titleElement -> titleElement.Value
            
            let url = 
                match rssXml.Element(XName.Get "link") with
                | null -> ""
                | linkElement -> linkElement.Value
            
            let content = 
                match rssXml.Element(XName.Get "description") with
                | null -> ""
                | descElement -> descElement.Value
            
            let date = 
                match rssXml.Element(XName.Get "pubDate") with
                | null -> DateTime.Now.ToString("ddd, dd MMM yyyy HH:mm:ss zzz")
                | dateElement -> dateElement.Value
            
            // Extract tags from RSS categories and apply sanitization
            let tags = 
                rssXml.Elements(XName.Get "category")
                |> Seq.map (fun cat -> TagService.processTagName cat.Value)
                |> Seq.filter (fun tag -> tag <> "untagged")  // Remove untagged from individual items
                |> Seq.toArray
            
            Some {
                Title = title
                Content = content
                Url = url
                Date = date
                ContentType = contentType
                Tags = tags
                RssXml = rssXml
                // Phase 5A: Default to None for non-response content
                ResponseType = None
                TargetUrl = None
                UpdatedDate = None
                // Phase 6A: Default to None for non-RSVP content
                RsvpStatus = None
                // Phase 5C: Default to None for non-review content
                ReviewData = None
                // Phase 5D: Default to None for non-media-primary content
                MediaData = None
                BodyHtml = lazy (MarkdownService.convertMdToCardHtml content)
            }
        | None -> None
    
    /// Generate RSS feed for given items and configuration
    let private generateRssFeed (items: UnifiedFeedItem list) (config: FeedConfiguration) : string =
        let latestDate = 
            if items.IsEmpty then 
                DateTime.Now.ToString("ddd, dd MMM yyyy HH:mm:ss zzz")
            else 
                items |> List.head |> fun item -> item.Date
        
        let channel = 
            XElement(XName.Get "rss",
                XAttribute(XName.Get "version", "2.0"),
                XAttribute(XName.Get("{http://www.w3.org/2000/xmlns/}source"), "http://source.scripting.com/"),
                XElement(XName.Get "channel",
                    XElement(XName.Get "title", config.Title),
                    XElement(XName.Get "link", config.Link),
                    XElement(XName.Get "description", config.Description),
                    XElement(XName.Get "lastBuildDate", latestDate),
                    XElement(XName.Get "language", "en")))
        
        // Add RSS items to channel
        let channelElement = channel.Descendants(XName.Get "channel") |> Seq.head
        let rssElements = items |> List.map (fun item -> item.RssXml) |> List.toArray
        channelElement.Add(rssElements)
        
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + Environment.NewLine + channel.ToString()

    // Canonical set of unified ContentType values that belong to the response
    // stream. Response items carry their SUBTYPE as ContentType (reply/reshare/
    // star/rsvp) rather than a generic "responses", so every subtype must be
    // listed here or it is silently dropped from JSON feeds and BAR exports.
    // "bookmark" is intentionally excluded (separate stream). Single source of
    // truth shared with Builder.fs to prevent drift.
    let responseStreamContentTypes = set [ ContentTypes.Responses; ContentTypes.Reply; ContentTypes.Reshare; ContentTypes.Star; ContentTypes.Rsvp ]
    let private jsonFeedItemLimit = 20

    let private toJsonFeedContent (item: UnifiedFeedItem) =
        match ContentTypes.parse item.ContentType with
        | Some ContentTypes.ContentType.Posts
        | Some ContentTypes.ContentType.Notes
        | Some ContentTypes.ContentType.Snippets
        | Some ContentTypes.ContentType.Wiki
        | Some ContentTypes.ContentType.Presentations
        | Some ContentTypes.ContentType.Reviews
        | Some ContentTypes.ContentType.AiMemex -> MarkdownService.convertMdToHtml item.Content
        | Some ContentTypes.ContentType.Responses
        | Some ContentTypes.ContentType.Bookmarks
        | Some ContentTypes.ContentType.Media
        | Some ContentTypes.ContentType.Streams
        | Some ContentTypes.ContentType.AlbumCollection
        | Some ContentTypes.ContentType.PlaylistCollection
        | None -> item.Content

    let private toIso8601Date (value: string) =
        if String.IsNullOrWhiteSpace(value) then DateTimeOffset.UtcNow.ToString("o")
        else
            try
                DateTimeOffset.Parse(value).ToString("o")
            with
            | _ -> DateTimeOffset.UtcNow.ToString("o")

    let private sortByDateDesc (items: UnifiedFeedItem list) =
        items
        |> List.sortByDescending (fun item ->
            if String.IsNullOrWhiteSpace(item.Date) then DateTimeOffset.MinValue
            else
                try DateTimeOffset.Parse(item.Date)
                with _ -> DateTimeOffset.MinValue)

    let generateJsonFeedString (title: string) (homePageUrl: string) (feedUrl: string) (description: string) (items: UnifiedFeedItem list) (useAbsoluteUrls: bool) =
        let jsonItems = JsonArray()

        items
        |> List.iter (fun item ->
            let contentHtml =
                let baseContent = toJsonFeedContent item
                if useAbsoluteUrls then normalizeUrlsForRss baseContent "https://www.lqdev.me"
                else baseContent

            let jsonItem = JsonObject()
            jsonItem["id"] <- item.Url
            jsonItem["url"] <- item.Url
            jsonItem["title"] <- item.Title
            jsonItem["content_html"] <- contentHtml
            jsonItem["date_published"] <- toIso8601Date item.Date

            if not (isNull item.Tags) && item.Tags.Length > 0 then
                let tags = JsonArray()
                item.Tags
                |> Array.iter (fun tag -> tags.Add(tag))
                jsonItem["tags"] <- tags

            jsonItems.Add(jsonItem)
        )

        let author = JsonObject()
        author["name"] <- "Luis Quintanilla"
        author["url"] <- "https://www.lqdev.me"

        let authors = JsonArray()
        authors.Add(author)

        let root = JsonObject()
        root["version"] <- "https://jsonfeed.org/version/1.1"
        root["title"] <- title
        root["home_page_url"] <- homePageUrl
        root["feed_url"] <- feedUrl
        root["description"] <- description
        root["authors"] <- authors
        root["items"] <- jsonItems

        root.ToJsonString(JsonSerializerOptions(WriteIndented = true))

    let buildJsonFeeds (feedDataSets: (string * (UnifiedFeedItem list)) list) (outputDirectory: string) =
        let allUnifiedItems = feedDataSets |> List.collect snd |> sortByDateDesc

        let writeFeed (outputPath: string) (feedContent: string) =
            let fullPath = Path.Combine(outputDirectory, outputPath)
            let fullDir = Path.GetDirectoryName(fullPath)
            Directory.CreateDirectory(fullDir) |> ignore
            File.WriteAllText(fullPath, feedContent)

        let typeConfigs = [
            (ContentTypes.Posts, "Luis Quintanilla - Posts", "https://www.lqdev.me/posts", "https://www.lqdev.me/posts/feed.json", "Blog posts by Luis Quintanilla", fun (item: UnifiedFeedItem) -> item.ContentType = ContentTypes.Posts)
            (ContentTypes.Notes, "Luis Quintanilla - Notes", "https://www.lqdev.me/notes", "https://www.lqdev.me/notes/feed.json", "Notes and micro-posts by Luis Quintanilla", fun (item: UnifiedFeedItem) -> item.ContentType = ContentTypes.Notes)
            (ContentTypes.Responses, "Luis Quintanilla - Responses", "https://www.lqdev.me/responses", "https://www.lqdev.me/responses/feed.json", "IndieWeb responses by Luis Quintanilla", fun (item: UnifiedFeedItem) -> responseStreamContentTypes.Contains(item.ContentType))
        ]

        typeConfigs
        |> List.iter (fun (outputPathPrefix, title, homePageUrl, feedUrl, description, filter) ->
            let matchingItems = allUnifiedItems |> List.filter filter
            let typeItems = matchingItems |> List.take (min jsonFeedItemLimit matchingItems.Length)

            if not typeItems.IsEmpty then
                let jsonFeed = generateJsonFeedString title homePageUrl feedUrl description typeItems true
                writeFeed (Path.Combine(outputPathPrefix, "feed.json")) jsonFeed
        )

        let allMatchingStreamItems =
            allUnifiedItems
            |> List.filter (fun item ->
                item.ContentType = ContentTypes.Posts
                || item.ContentType = ContentTypes.Notes
                || responseStreamContentTypes.Contains(item.ContentType))
        let allStreamItems = allMatchingStreamItems |> List.take (min jsonFeedItemLimit allMatchingStreamItems.Length)

        if not allStreamItems.IsEmpty then
            let allJsonFeed =
                generateJsonFeedString
                    "Luis Quintanilla - All Updates"
                    "https://www.lqdev.me/feed"
                    "https://www.lqdev.me/feed/index.json"
                    "Posts, notes, and responses by Luis Quintanilla"
                    allStreamItems
                    true

            writeFeed (Path.Combine("feed", "index.json")) allJsonFeed

        printfn "✅ JSON feeds generated: posts, notes, responses, and combined feed"
    
    /// Build unified feeds from all content types with proper type conversion
    let buildAllFeeds (feedDataSets: (string * (UnifiedFeedItem list)) list) (outputDirectory: string) =
        // Flatten all feed items and sort chronologically
        // Handle null/empty dates gracefully by using a very old date for items with missing dates
        let allUnifiedItems = 
            feedDataSets
            |> List.collect snd
            |> List.sortByDescending (fun item -> 
                if String.IsNullOrWhiteSpace(item.Date) then
                    DateTimeOffset.MinValue
                else
                    try
                        DateTimeOffset.Parse(item.Date)
                    with
                    | _ -> DateTimeOffset.MinValue)
        
        // Fire-hose feed configuration (all content types)
        let fireHoseConfig = {
            Title = "Luis Quintanilla - All Updates"
            Link = "https://www.lqdev.me/feed"
            Description = "All content updates from Luis Quintanilla's website"
            OutputPath = "feed/feed.xml"
            ContentType = None
        }
        
        // Generate fire-hose feed (exclude AI Memex from public syndication)
        let publicItems = allUnifiedItems |> List.filter (fun item -> item.ContentType <> ContentTypes.AiMemex)
        let fireHoseFeed = generateRssFeed (publicItems |> List.take (min 20 publicItems.Length)) fireHoseConfig
        let fireHoseDir = Path.Combine(outputDirectory, "feed")
        Directory.CreateDirectory(fireHoseDir) |> ignore
        File.WriteAllText(Path.Combine(fireHoseDir, "feed.xml"), fireHoseFeed)
        
        // Also create backward compatibility copy at old location
        File.WriteAllText(Path.Combine(fireHoseDir, "index.xml"), fireHoseFeed)
        
        // Type-specific feed configurations
        let typeConfigurations = [
            (ContentTypes.Posts, {
                Title = "Luis Quintanilla - Posts"
                Link = "https://www.lqdev.me/posts"
                Description = "Blog posts by Luis Quintanilla"
                OutputPath = "posts/feed.xml"
                ContentType = Some ContentTypes.Posts
            })
            (ContentTypes.Notes, {
                Title = "Luis Quintanilla - Notes"
                Link = "https://www.lqdev.me/notes"
                Description = "Notes and micro-posts by Luis Quintanilla"
                OutputPath = "notes/feed.xml"
                ContentType = Some ContentTypes.Notes
            })
            (ContentTypes.Responses, {
                Title = "Luis Quintanilla - Responses"
                Link = "https://www.lqdev.me/responses"
                Description = "IndieWeb responses by Luis Quintanilla"
                OutputPath = "responses/feed.xml"
                ContentType = Some ContentTypes.Responses
            })
            (ContentTypes.Bookmarks, {
                Title = "Luis Quintanilla - Bookmarks"
                Link = "https://www.lqdev.me/bookmarks"
                Description = "IndieWeb bookmarks by Luis Quintanilla"
                OutputPath = "bookmarks/feed.xml"
                ContentType = Some ContentTypes.Bookmarks
            })
            (ContentTypes.Rsvp, {
                Title = "Luis Quintanilla - RSVPs"
                Link = "https://www.lqdev.me/rsvp"
                Description = "Events Luis Quintanilla has responded to (yes/no/maybe/interested)"
                OutputPath = "rsvp/feed.xml"
                ContentType = Some ContentTypes.Rsvp
            })
            (ContentTypes.Snippets, {
                Title = "Luis Quintanilla - Snippets"
                Link = "https://www.lqdev.me/resources/snippets"
                Description = "Code snippets by Luis Quintanilla"
                OutputPath = "resources/snippets/feed.xml"
                ContentType = Some ContentTypes.Snippets
            })
            (ContentTypes.Wiki, {
                Title = "Luis Quintanilla - Wiki"
                Link = "https://www.lqdev.me/resources/wiki"
                Description = "Wiki articles by Luis Quintanilla"
                OutputPath = "resources/wiki/feed.xml"
                ContentType = Some ContentTypes.Wiki
            })
            (ContentTypes.Presentations, {
                Title = "Luis Quintanilla - Presentations"
                Link = "https://www.lqdev.me/resources/presentations"
                Description = "Presentations by Luis Quintanilla"
                OutputPath = "resources/presentations/feed.xml"
                ContentType = Some ContentTypes.Presentations
            })
            (ContentTypes.Reviews, {
                Title = "Luis Quintanilla - Reviews"
                Link = "https://www.lqdev.me/reviews"
                Description = "Book reviews by Luis Quintanilla"
                OutputPath = "reviews/feed.xml"
                ContentType = Some ContentTypes.Reviews
            })
            (ContentTypes.Media, {
                Title = "Luis Quintanilla - Media"
                Link = "https://www.lqdev.me/media"
                Description = "Photo albums and media by Luis Quintanilla"
                OutputPath = "media/feed.xml"
                ContentType = Some ContentTypes.Media
            })
            (ContentTypes.AlbumCollection, {
                Title = "Luis Quintanilla - Albums"
                Link = "https://www.lqdev.me/collections/albums"
                Description = "Photo album collections by Luis Quintanilla"
                OutputPath = "collections/albums/feed.xml"
                ContentType = Some ContentTypes.AlbumCollection
            })
            (ContentTypes.PlaylistCollection, {
                Title = "Luis Quintanilla - Playlists"
                Link = "https://www.lqdev.me/collections/playlists"
                Description = "Music playlist collections by Luis Quintanilla"
                OutputPath = "collections/playlists/feed.xml"
                ContentType = Some ContentTypes.PlaylistCollection
            })
            (ContentTypes.AiMemex, {
                Title = "Luis Quintanilla - AI Memex"
                Link = "https://www.lqdev.me/resources/ai-memex"
                Description = "AI-authored content: project reports, research, patterns, and blog posts"
                OutputPath = "resources/ai-memex/feed.xml"
                ContentType = Some ContentTypes.AiMemex
            })
        ]
        
        // Generate type-specific feeds
        typeConfigurations
        |> List.iter (fun (contentType, config) ->
            let typeItems = 
                allUnifiedItems 
                |> List.filter (fun item -> 
                    if contentType = ContentTypes.Responses then
                        // For responses feed, include all response subtypes (incl. rsvp)
                        [ ContentTypes.Star; ContentTypes.Reply; ContentTypes.Reshare; ContentTypes.Rsvp; ContentTypes.Responses ] |> List.contains item.ContentType
                    else
                        item.ContentType = contentType)
                |> List.take (min 20 (allUnifiedItems |> List.filter (fun item -> 
                    if contentType = ContentTypes.Responses then
                        [ ContentTypes.Star; ContentTypes.Reply; ContentTypes.Reshare; ContentTypes.Rsvp; ContentTypes.Responses ] |> List.contains item.ContentType
                    else
                        item.ContentType = contentType) |> List.length))
            
            if not (List.isEmpty typeItems) then
                let typeFeed = generateRssFeed typeItems config
                let feedDir = Path.Combine(outputDirectory, Path.GetDirectoryName(config.OutputPath))
                Directory.CreateDirectory(feedDir) |> ignore
                File.WriteAllText(Path.Combine(outputDirectory, config.OutputPath), typeFeed)
        )
        
        printfn "✅ Unified feeds generated: %d total items across %d content types" allUnifiedItems.Length (feedDataSets |> List.length)
    
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

    /// Generate RSS feeds for individual tags
    let buildTagFeeds (feedDataSets: (string * (UnifiedFeedItem list)) list) (outputDirectory: string) =
        // Flatten all feed items (exclude AI Memex from tag syndication feeds)
        let allUnifiedItems = 
            feedDataSets
            |> List.collect snd
            |> List.filter (fun item -> item.ContentType <> ContentTypes.AiMemex)
            |> List.sortByDescending (fun item -> DateTimeOffset.Parse(item.Date))
        
        // Extract all canonical tags (processTagName consolidates plurals, gerunds, etc.)
        let allTags = 
            allUnifiedItems
            |> List.collect (fun item -> 
                if isNull item.Tags then [] 
                else item.Tags |> Array.map TagService.processTagName |> Array.toList)
            |> List.distinct
            |> List.sort
        
        printfn "Generating RSS feeds for %d tags..." allTags.Length
        
        // Generate RSS feed for each canonical tag
        allTags
        |> List.iter (fun tag ->
            let matchesTag (item: UnifiedFeedItem) =
                not (isNull item.Tags) && item.Tags |> Array.exists (fun t -> TagService.processTagName t = tag)
            let tagItems = 
                allUnifiedItems
                |> List.filter matchesTag
                |> List.take (min 20 (allUnifiedItems |> List.filter matchesTag |> List.length))
            
            if not (List.isEmpty tagItems) then
                let sanitizedTag = sanitizeTagForPath tag
                let tagConfig = {
                    Title = sprintf "Luis Quintanilla - %s" tag
                    Link = sprintf "https://www.lqdev.me/tags/%s" sanitizedTag
                    Description = sprintf "All content tagged with '%s' by Luis Quintanilla" tag
                    OutputPath = sprintf "tags/%s/feed.xml" sanitizedTag
                    ContentType = None  // Tag feeds include all content types
                }
                
                let tagFeed = generateRssFeed tagItems tagConfig
                let feedDir = Path.Combine(outputDirectory, "tags", sanitizedTag)
                Directory.CreateDirectory(feedDir) |> ignore
                File.WriteAllText(Path.Combine(feedDir, "feed.xml"), tagFeed)
        )
        
        printfn "✅ Tag RSS feeds generated for %d tags" allTags.Length
    
    /// Convert FeedData to UnifiedFeedItem - helper functions for each content type

    /// Optional extras on a UnifiedFeedItem beyond the common core. Most content
    /// types carry none (`defaultExtras`); the genuinely divergent types
    /// (responses, books, albums, bookmarks) supply their own.
    type private UnifiedExtras = {
        ResponseType: string option
        TargetUrl: string option
        UpdatedDate: string option
        RsvpStatus: string option
        ReviewData: ReviewMetadata option
        MediaData: MediaAPData option
    }

    let private defaultExtras =
        { ResponseType = None; TargetUrl = None; UpdatedDate = None
          RsvpStatus = None; ReviewData = None; MediaData = None }

    let private arrayTags (tags: string array) = if isNull tags then [||] else tags

    let private splitTags (tags: string) =
        if String.IsNullOrEmpty(tags) then [||]
        else tags.Split(',') |> Array.map (fun s -> s.Trim())

    /// Generic projection shared by all content types (assessment F2). The skeleton
    /// — choose over RssXml, extract the `link`, build the record — lives here once;
    /// callers supply the content-type identity, the core fields, and any extras.
    let private toUnified
        (contentType: string)
        (getCore: FeedData<'T> -> string * string * string * string array) // title, content, date, tags
        (getExtras: FeedData<'T> -> UnifiedExtras)
        (feedDataList: FeedData<'T> list) : UnifiedFeedItem list =
        feedDataList |> List.choose (fun feedData ->
            match feedData.RssXml with
            | Some rssXml ->
                let (title, content, date, tags) = getCore feedData
                let url = match rssXml.Element(XName.Get "link") with | null -> "" | e -> e.Value
                let extras = getExtras feedData
                Some { Title = title; Content = content; Url = url; Date = date; ContentType = contentType; Tags = tags; RssXml = rssXml; ResponseType = extras.ResponseType; TargetUrl = extras.TargetUrl; UpdatedDate = extras.UpdatedDate; RsvpStatus = extras.RsvpStatus; ReviewData = extras.ReviewData; MediaData = extras.MediaData; BodyHtml = lazy (MarkdownService.convertMdToCardHtml content) }
            | None -> None)

    let convertPostsToUnified (feedDataList: FeedData<Post> list) : UnifiedFeedItem list =
        toUnified ContentTypes.Posts
            (fun (fd: FeedData<Post>) -> fd.Content.Metadata.Title, fd.Content.Content, fd.Content.Metadata.Date, arrayTags fd.Content.Metadata.Tags)
            (fun _ -> defaultExtras)
            feedDataList

    let convertNotesToUnified (feedDataList: FeedData<Post> list) : UnifiedFeedItem list =
        toUnified ContentTypes.Notes
            (fun (fd: FeedData<Post>) -> fd.Content.Metadata.Title, fd.Content.Content, fd.Content.Metadata.Date, arrayTags fd.Content.Metadata.Tags)
            (fun _ -> defaultExtras)
            feedDataList
    
    let convertResponsesToUnified (feedDataList: FeedData<Response> list) : UnifiedFeedItem list =
        feedDataList 
        |> List.filter (fun feedData -> feedData.Content.Metadata.ResponseType <> ContentTypes.Bookmark) // Exclude bookmarks
        |> List.choose (fun feedData ->
            match feedData.RssXml with
            | Some rssXml ->
                // Use CardHtml for responses to include target URL information
                let title = feedData.Content.Metadata.Title
                let url = match rssXml.Element(XName.Get "link") with | null -> "" | e -> e.Value
                let content = feedData.CardHtml  // Use CardHtml to include target URL display
                let date = feedData.Content.Metadata.DatePublished
                let tags = if isNull feedData.Content.Metadata.Tags then [||] else feedData.Content.Metadata.Tags
                // Use specific response type instead of generic "responses"
                let contentType = feedData.Content.Metadata.ResponseType
                // Phase 5A: Extract response semantics for ActivityPub
                let responseType = 
                    if String.IsNullOrWhiteSpace(feedData.Content.Metadata.ResponseType) then None
                    else Some feedData.Content.Metadata.ResponseType
                let targetUrl = 
                    if String.IsNullOrWhiteSpace(feedData.Content.Metadata.TargetUrl) then None
                    else Some feedData.Content.Metadata.TargetUrl
                let updatedDate = if String.IsNullOrWhiteSpace(feedData.Content.Metadata.DateUpdated) then None else Some feedData.Content.Metadata.DateUpdated
                // Phase 6A: Extract RSVP status for event responses
                let rsvpStatus = feedData.Content.Metadata.RsvpStatus
                Some { Title = title; Content = content; Url = url; Date = date; ContentType = contentType; Tags = tags; RssXml = rssXml; ResponseType = responseType; TargetUrl = targetUrl; UpdatedDate = updatedDate; RsvpStatus = rsvpStatus; ReviewData = None; MediaData = None; BodyHtml = lazy (ResponseProcessor.renderResponseCardBodyClean feedData.Content) }
            | None -> None)
    
    let convertResponseBookmarksToUnified (feedDataList: FeedData<Response> list) : UnifiedFeedItem list =
        feedDataList 
        |> List.filter (fun feedData -> feedData.Content.Metadata.ResponseType = ContentTypes.Bookmark) // Only bookmarks
        |> List.choose (fun feedData ->
            match feedData.RssXml with
            | Some rssXml ->
                let title = feedData.Content.Metadata.Title
                let url = match rssXml.Element(XName.Get "link") with | null -> "" | e -> e.Value
                let content = feedData.CardHtml  // Use CardHtml to include target URL display
                let date = feedData.Content.Metadata.DatePublished
                let tags = if isNull feedData.Content.Metadata.Tags then [||] else feedData.Content.Metadata.Tags
                // Phase 5A: Extract response semantics for ActivityPub
                let responseType = Some ContentTypes.Bookmark
                let targetUrl = Some feedData.Content.Metadata.TargetUrl
                let updatedDate = if String.IsNullOrWhiteSpace(feedData.Content.Metadata.DateUpdated) then None else Some feedData.Content.Metadata.DateUpdated
                Some { Title = title; Content = content; Url = url; Date = date; ContentType = ContentTypes.Bookmarks; Tags = tags; RssXml = rssXml; ResponseType = responseType; TargetUrl = targetUrl; UpdatedDate = updatedDate; RsvpStatus = None; ReviewData = None; MediaData = None; BodyHtml = lazy (ResponseProcessor.renderResponseCardBodyClean feedData.Content) }
            | None -> None)
    
    let convertSnippetsToUnified (feedDataList: FeedData<Snippet> list) : UnifiedFeedItem list =
        toUnified ContentTypes.Snippets
            (fun (fd: FeedData<Snippet>) -> fd.Content.Metadata.Title, fd.Content.Content, fd.Content.Metadata.CreatedDate, splitTags fd.Content.Metadata.Tags)
            (fun _ -> defaultExtras)
            feedDataList

    let convertWikisToUnified (feedDataList: FeedData<Wiki> list) : UnifiedFeedItem list =
        toUnified ContentTypes.Wiki
            (fun (fd: FeedData<Wiki>) -> fd.Content.Metadata.Title, fd.Content.Content, fd.Content.Metadata.LastUpdatedDate, splitTags fd.Content.Metadata.Tags)
            (fun _ -> defaultExtras)
            feedDataList

    let convertAiMemexToUnified (feedDataList: FeedData<AiMemex> list) : UnifiedFeedItem list =
        toUnified ContentTypes.AiMemex
            (fun (fd: FeedData<AiMemex>) -> fd.Content.Metadata.Title, fd.Content.Content, fd.Content.Metadata.PublishedDate, splitTags fd.Content.Metadata.Tags)
            (fun _ -> defaultExtras)
            feedDataList

    let convertPresentationsToUnified (feedDataList: FeedData<Presentation> list) : UnifiedFeedItem list =
        toUnified ContentTypes.Presentations
            (fun (fd: FeedData<Presentation>) -> fd.Content.Metadata.Title, fd.Content.Content, fd.Content.Metadata.Date, splitTags fd.Content.Metadata.Tags)
            (fun _ -> defaultExtras)
            feedDataList
    
    let convertBooksToUnified (feedDataList: FeedData<Book> list) : UnifiedFeedItem list =
        feedDataList |> List.choose (fun feedData ->
            match feedData.RssXml with
            | Some rssXml ->
                // Use clean CardHtml instead of RSS description
                let title = feedData.Content.Metadata.Title
                let url = match rssXml.Element(XName.Get "link") with | null -> "" | e -> e.Value
                // For reviews timeline display, use simplified CardHtml instead of full content
                let content = feedData.CardHtml
                let date = feedData.Content.Metadata.DatePublished
                let tags = [||]  // Books don't have explicit tags
                // Phase 5C: Get review metadata from cache for Schema.org integration in ActivityPub
                let reviewData = BookProcessor.getReviewMetadata feedData.Content.FileName
                Some { Title = title; Content = content; Url = url; Date = date; ContentType = ContentTypes.Reviews; Tags = tags; RssXml = rssXml; ResponseType = None; TargetUrl = None; UpdatedDate = None; RsvpStatus = None; ReviewData = reviewData; MediaData = None; BodyHtml = lazy content }
            | None -> None)
    
    let convertAlbumsToUnified (feedDataList: FeedData<Album> list) : UnifiedFeedItem list =
        feedDataList |> List.choose (fun feedData ->
            match feedData.RssXml with
            | Some rssXml ->
                let title = feedData.Content.Metadata.Title
                let url = match rssXml.Element(XName.Get "link") with | null -> "" | e -> e.Value
                let content = feedData.Content.Content  // Use full content instead of CardHtml
                let date = feedData.Content.Metadata.Date
                let tags = if isNull feedData.Content.Metadata.Tags then [||] else feedData.Content.Metadata.Tags
                // Phase 5D: Extract media data for media-primary content
                let mediaData = MediaExtractor.extractPrimaryMedia content
                Some { Title = title; Content = content; Url = url; Date = date; ContentType = ContentTypes.Media; Tags = tags; RssXml = rssXml; ResponseType = None; TargetUrl = None; UpdatedDate = None; RsvpStatus = None; ReviewData = None; MediaData = mediaData; BodyHtml = lazy (MarkdownService.convertMdToCardHtml content) }
            | None -> None)
    
    let convertAlbumCollectionsToUnified (feedDataList: FeedData<AlbumCollection> list) : UnifiedFeedItem list =
        toUnified ContentTypes.AlbumCollection
            (fun (fd: FeedData<AlbumCollection>) -> fd.Content.Metadata.Title, fd.Content.Content, fd.Content.Metadata.Date, arrayTags fd.Content.Metadata.Tags)
            (fun _ -> defaultExtras)
            feedDataList

    let convertPlaylistCollectionsToUnified (feedDataList: FeedData<PlaylistCollection> list) : UnifiedFeedItem list =
        toUnified ContentTypes.PlaylistCollection
            (fun (fd: FeedData<PlaylistCollection>) -> fd.Content.Metadata.Title, fd.Content.Content, fd.Content.Metadata.Date, arrayTags fd.Content.Metadata.Tags)
            (fun _ -> defaultExtras)
            feedDataList
    
    let convertBookmarksToUnified (feedDataList: FeedData<Bookmark> list) : UnifiedFeedItem list =
        feedDataList |> List.choose (fun feedData ->
            match feedData.RssXml with
            | Some rssXml ->
                let title = feedData.Content.Metadata.Title
                let url = match rssXml.Element(XName.Get "link") with | null -> "" | e -> e.Value
                let content = feedData.Content.Content  // Use full content instead of CardHtml
                let date = feedData.Content.Metadata.DatePublished
                let tags = if isNull feedData.Content.Metadata.Tags then [||] else feedData.Content.Metadata.Tags
                let targetUrl = 
                    if String.IsNullOrWhiteSpace(feedData.Content.Metadata.BookmarkOf) then None
                    else Some feedData.Content.Metadata.BookmarkOf
                Some { Title = title; Content = content; Url = url; Date = date; ContentType = ContentTypes.Bookmarks; Tags = tags; RssXml = rssXml; ResponseType = None; TargetUrl = targetUrl; UpdatedDate = None; RsvpStatus = None; ReviewData = None; MediaData = None; BodyHtml = lazy (MarkdownService.convertMdToCardHtml content) }
            | None -> None)

    // Convert bookmark responses (Response objects with bookmark type) to unified feed
    let convertBookmarkResponsesToUnified (feedDataList: FeedData<Response> list) : UnifiedFeedItem list =
        feedDataList 
        |> List.filter (fun feedData -> feedData.Content.Metadata.ResponseType = ContentTypes.Bookmark) // Include only bookmarks
        |> List.choose (fun feedData ->
            match feedData.RssXml with
            | Some rssXml ->
                let title = feedData.Content.Metadata.Title
                let url = match rssXml.Element(XName.Get "link") with | null -> "" | e -> e.Value
                let content = feedData.CardHtml  // Use CardHtml to include target URL display
                let date = feedData.Content.Metadata.DatePublished
                let tags = if isNull feedData.Content.Metadata.Tags then [||] else feedData.Content.Metadata.Tags
                Some { Title = title; Content = content; Url = url; Date = date; ContentType = ContentTypes.Bookmarks; Tags = tags; RssXml = rssXml; ResponseType = Some ContentTypes.Bookmark; TargetUrl = Some feedData.Content.Metadata.TargetUrl; UpdatedDate = None; RsvpStatus = None; ReviewData = None; MediaData = None; BodyHtml = lazy (ResponseProcessor.renderResponseCardBodyClean feedData.Content) }
            | None -> None)
