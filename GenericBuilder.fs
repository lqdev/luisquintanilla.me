module GenericBuilder

open Domain
open ASTParsing
open CustomBlocks
open BlockRenderers
open System.Xml.Linq
open System
open System.IO

/// Data structure for feed generation with both card and RSS item
type FeedData<'T> = {
    Content: 'T
    CardHtml: string
    RssXml: XElement option
}

/// Generic content processor pattern for consistent content handling
type ContentProcessor<'T> = {
    /// Parse content from file path to domain type
    Parse: string -> 'T option
    /// Render content to final HTML output
    Render: 'T -> string
    /// Generate output file path from content
    OutputPath: 'T -> string
    /// Generate card HTML for index pages
    RenderCard: 'T -> string
    /// Generate RSS XML element
    RenderRss: 'T -> XElement option
}

/// Build content and generate feed data in a single pass
let buildContentWithFeeds<'T> (processor: ContentProcessor<'T>) (filePaths: string list) : FeedData<'T> list =
    filePaths
    |> List.choose (fun filePath ->
        match processor.Parse filePath with
        | Some content ->
            let cardHtml = processor.RenderCard content
            let rssXml = processor.RenderRss content
            Some { Content = content; CardHtml = cardHtml; RssXml = rssXml }
        | None -> None)

/// Content processors for existing domain types

/// Post content processor
module PostProcessor =
    let create() : ContentProcessor<Post> = {
        Parse = fun filePath ->
            match parsePostFromFile filePath with
            | Ok parsedDoc -> 
                match parsedDoc.Metadata with
                | Some metadata -> 
                    Some {
                        FileName = Path.GetFileNameWithoutExtension(filePath)
                        Metadata = metadata
                        Content = parsedDoc.TextContent  // Raw markdown content
                    }
                | None -> None
            | Error _ -> None
        
        Render = fun post ->
            // Convert markdown to HTML - will be handled by calling code
            // This matches the pattern from existing processors
            sprintf "<article>%s</article>" post.Content
        
        OutputPath = fun post ->
            sprintf "posts/%s/index.html" post.FileName
        
        RenderCard = fun post ->
            let title = Html.escapeHtml post.Metadata.Title
            let description = 
                if isNull post.Metadata.Description then 
                    "No description available"
                else 
                    Html.escapeHtml post.Metadata.Description
            let url = sprintf "/posts/%s" post.FileName
            let date = post.Metadata.Date
            
            Html.element "article" (Html.attribute "class" "post-card")
                (Html.element "h2" "" (Html.element "a" (Html.attribute "href" url) title) +
                 Html.element "p" "" description +
                 Html.element "time" "" (Html.escapeHtml date))
        
        RenderRss = fun post ->
            // Create RSS item for post using existing pattern
            let url = sprintf "https://www.luisquintanilla.me/posts/%s" post.FileName
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", post.Metadata.Title),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" post.Content),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url),
                    XElement(XName.Get "pubDate", post.Metadata.Date))
            Some item
    }

/// Note content processor - notes are Post objects with post_type: "note"
module NoteProcessor =
    let create() : ContentProcessor<Post> = {
        Parse = fun filePath ->
            match parsePostFromFile filePath with
            | Ok parsedDoc -> 
                match parsedDoc.Metadata with
                | Some metadata -> 
                    Some {
                        FileName = Path.GetFileNameWithoutExtension(filePath)
                        Metadata = metadata
                        Content = parsedDoc.TextContent  // Raw markdown content
                    }
                | None -> None
            | Error _ -> None
        
        Render = fun note ->
            // Convert markdown to HTML - will be handled by calling code
            sprintf "<article class=\"note\">%s</article>" note.Content
        
        OutputPath = fun note ->
            sprintf "feed/%s/index.html" note.FileName
        
        RenderCard = fun note ->
            let title = Html.escapeHtml note.Metadata.Title
            let description = 
                if isNull note.Metadata.Description then 
                    "No description available"
                else 
                    Html.escapeHtml note.Metadata.Description
            let url = sprintf "/feed/%s" note.FileName
            let date = note.Metadata.Date
            
            Html.element "article" (Html.attribute "class" "note-card")
                (Html.element "h2" "" (Html.element "a" (Html.attribute "href" url) title) +
                 Html.element "p" "" description +
                 Html.element "time" "" (Html.escapeHtml date))
        
        RenderRss = fun note ->
            // Create RSS item for note using existing pattern
            let url = sprintf "https://www.luisquintanilla.me/feed/%s" note.FileName
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", note.Metadata.Title),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" note.Content),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url),
                    XElement(XName.Get "pubDate", note.Metadata.Date))
            Some item
    }

/// Snippet content processor  
module SnippetProcessor =
    let create() : ContentProcessor<Snippet> = {
        Parse = fun filePath ->
            match parseSnippetFromFile filePath with
            | Ok parsedDoc -> 
                match parsedDoc.Metadata with
                | Some metadata -> 
                    Some {
                        FileName = Path.GetFileNameWithoutExtension(filePath)
                        Metadata = metadata
                        Content = parsedDoc.TextContent
                    }
                | None -> None
            | Error _ -> None
        
        Render = fun snippet ->
            sprintf "<article>%s</article>" snippet.Content
        
        OutputPath = fun snippet ->
            sprintf "snippets/%s.html" snippet.FileName
        
        RenderCard = fun snippet ->
            let title = Html.escapeHtml snippet.Metadata.Title
            let excerpt = Html.escapeHtml (snippet.Content.Substring(0, min 150 snippet.Content.Length) + "...")
            let url = sprintf "/snippets/%s" snippet.FileName
            
            Html.element "article" (Html.attribute "class" "snippet-card")
                (Html.element "h2" "" (Html.element "a" (Html.attribute "href" url) title) +
                 Html.element "p" "" excerpt)
        
        RenderRss = fun snippet ->
            // Create RSS item for snippet similar to post
            let url = sprintf "https://www.luisquintanilla.me/snippets/%s" snippet.FileName
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", snippet.Metadata.Title),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" snippet.Content),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url))
            Some item
    }

/// Wiki content processor
module WikiProcessor =
    let create() : ContentProcessor<Wiki> = {
        Parse = fun filePath ->
            match parseWikiFromFile filePath with
            | Ok parsedDoc -> 
                match parsedDoc.Metadata with
                | Some metadata -> 
                    Some {
                        FileName = Path.GetFileNameWithoutExtension(filePath)
                        Metadata = metadata
                        Content = parsedDoc.TextContent
                    }
                | None -> None
            | Error _ -> None
        
        Render = fun wiki ->
            sprintf "<article>%s</article>" wiki.Content
        
        OutputPath = fun wiki ->
            sprintf "wiki/%s.html" wiki.FileName
        
        RenderCard = fun wiki ->
            let title = Html.escapeHtml wiki.Metadata.Title
            let excerpt = Html.escapeHtml (wiki.Content.Substring(0, min 150 wiki.Content.Length) + "...")
            let url = sprintf "/wiki/%s" wiki.FileName
            
            Html.element "article" (Html.attribute "class" "wiki-card")
                (Html.element "h2" "" (Html.element "a" (Html.attribute "href" url) title) +
                 Html.element "p" "" excerpt)
        
        RenderRss = fun wiki ->
            // Create RSS item for wiki similar to post
            let url = sprintf "https://www.luisquintanilla.me/wiki/%s" wiki.FileName
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", wiki.Metadata.Title),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" wiki.Content),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url))
            Some item
    }

/// Presentation content processor
module PresentationProcessor =
    let create() : ContentProcessor<Presentation> = {
        Parse = fun filePath ->
            match parsePresentationFromFile filePath with
            | Ok parsedDoc -> 
                match parsedDoc.Metadata with
                | Some metadata -> 
                    Some {
                        FileName = Path.GetFileNameWithoutExtension(filePath)
                        Metadata = metadata
                        Content = parsedDoc.TextContent
                    }
                | None -> None
            | Error _ -> None
        
        Render = fun presentation ->
            // Preserve existing Reveal.js integration - return content as-is for now
            sprintf "<article>%s</article>" presentation.Content
        
        OutputPath = fun presentation ->
            sprintf "presentations/%s.html" presentation.FileName
        
        RenderCard = fun presentation ->
            let title = Html.escapeHtml presentation.Metadata.Title
            let excerpt = Html.escapeHtml (presentation.Content.Substring(0, min 150 presentation.Content.Length) + "...")
            let url = sprintf "/presentations/%s" presentation.FileName
            
            // Add resources display
            let resourcesHtml = 
                if presentation.Metadata.Resources.Length > 0 then
                    let resourceLinks = 
                        presentation.Metadata.Resources
                        |> Array.map (fun resource -> 
                            Html.element "a" (Html.attribute "href" resource.Url) (Html.escapeHtml resource.Text))
                        |> String.concat " | "
                    Html.element "div" (Html.attribute "class" "resources") resourceLinks
                else ""
            
            Html.element "article" (Html.attribute "class" "presentation-card")
                (Html.element "h2" "" (Html.element "a" (Html.attribute "href" url) title) +
                 Html.element "p" "" excerpt +
                 resourcesHtml)
        
        RenderRss = fun presentation ->
            // Create RSS item for presentation
            let url = sprintf "https://www.luisquintanilla.me/presentations/%s" presentation.FileName
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", presentation.Metadata.Title),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" presentation.Content),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url))
            Some item
    }

/// Book content processor
module BookProcessor =
    let create() : ContentProcessor<Book> = {
        Parse = fun filePath ->
            match parseBookFromFile filePath with
            | Ok parsedDoc -> 
                match parsedDoc.Metadata with
                | Some metadata -> 
                    Some {
                        FileName = Path.GetFileNameWithoutExtension(filePath)
                        Metadata = metadata
                        Content = parsedDoc.TextContent
                    }
                | None -> None
            | Error _ -> None
        
        Render = fun book ->
            // For now, return content as-is. Later integrate with existing Views.Generator
            sprintf "<article>%s</article>" book.Content
        
        OutputPath = fun book ->
            sprintf "library/%s.html" book.FileName
        
        RenderCard = fun book ->
            let title = Html.escapeHtml book.Metadata.Title
            let author = Html.escapeHtml book.Metadata.Author
            let status = Html.escapeHtml book.Metadata.Status
            let url = sprintf "/library/%s" book.FileName
            
            // Display rating if available
            let ratingHtml = 
                if book.Metadata.Rating > 0.0 then
                    sprintf "<div class=\"rating\">Rating: %.1f/5</div>" book.Metadata.Rating
                else ""
            
            // Create book card with cover, title, author, status
            let coverHtml = 
                if not (String.IsNullOrEmpty(book.Metadata.Cover)) then
                    sprintf "<img src=\"%s\" alt=\"%s cover\" class=\"book-cover\">" 
                        (Html.escapeHtml book.Metadata.Cover) (Html.escapeHtml book.Metadata.Title)
                else ""
            
            Html.element "article" (Html.attribute "class" "book-card")
                (coverHtml +
                 Html.element "h2" "" (Html.element "a" (Html.attribute "href" url) title) +
                 Html.element "div" (Html.attribute "class" "author") ("by " + author) +
                 Html.element "div" (Html.attribute "class" "status") status +
                 ratingHtml)
        
        RenderRss = fun book ->
            // Create RSS item for book
            let url = sprintf "https://www.luisquintanilla.me/library/%s" book.FileName
            let pubDate = 
                if not (String.IsNullOrEmpty(book.Metadata.DatePublished)) then
                    book.Metadata.DatePublished
                else
                    DateTime.Now.ToString("ddd, dd MMM yyyy HH:mm:ss zzz")
            
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", sprintf "%s by %s" book.Metadata.Title book.Metadata.Author),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" book.Content),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url),
                    XElement(XName.Get "pubDate", pubDate))
            Some item
    }

/// Response content processor
module ResponseProcessor =
    let create() : ContentProcessor<Response> = {
        Parse = fun filePath ->
            match parseResponseFromFile filePath with
            | Ok parsedDoc -> 
                match parsedDoc.Metadata with
                | Some metadata -> 
                    Some {
                        FileName = Path.GetFileNameWithoutExtension(filePath)
                        Metadata = metadata
                        Content = parsedDoc.TextContent
                    }
                | None -> None
            | Error _ -> None
        
        Render = fun response ->
            // Response rendering with IndieWeb microformat support
            sprintf "<article class=\"h-entry response response-%s\">%s</article>" 
                (response.Metadata.ResponseType.ToLower()) response.Content
        
        OutputPath = fun response ->
            sprintf "responses/%s.html" response.FileName
        
        RenderCard = fun response ->
            let title = Html.escapeHtml response.Metadata.Title
            let targetUrl = Html.escapeHtml response.Metadata.TargetUrl
            let responseType = Html.escapeHtml response.Metadata.ResponseType
            let url = sprintf "/responses/%s" response.FileName
            let date = response.Metadata.DatePublished
            
            // IndieWeb microformat card with response type indication
            Html.element "article" (Html.attribute "class" "response-card h-entry")
                (Html.element "div" (Html.attribute "class" "response-type") responseType +
                 Html.element "h2" "" (Html.element "a" (Html.attribute "href" url) title) +
                 Html.element "div" (Html.attribute "class" "response-target") 
                    (sprintf "→ %s" (Html.element "a" (Html.attribute "href" targetUrl) targetUrl)) +
                 Html.element "time" (Html.attribute "class" "dt-published") (Html.escapeHtml date))
        
        RenderRss = fun response ->
            // Create RSS item for response
            let url = sprintf "https://www.luisquintanilla.me/responses/%s" response.FileName
            let description = sprintf "[%s] %s" response.Metadata.ResponseType response.Content
            
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", response.Metadata.Title),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" description),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url),
                    XElement(XName.Get "pubDate", response.Metadata.DatePublished))
            Some item
    }

/// Album content processor with :::media block conversion
module AlbumProcessor =
    
    /// Convert AlbumImage to :::media block markdown syntax
    let private convertImageToMediaBlock (image: AlbumImage) : string =
        sprintf ":::media\nmedia_type: image\nuri: %s\nalt_text: %s\ncaption: %s\naspect: \"\"\n:::" 
            image.ImagePath image.AltText image.Description
    
    /// Convert album images to :::media blocks and combine with existing content
    let private convertAlbumToMarkdown (album: Album) (existingContent: string) : string =
        let mediaBlocks = 
            album.Metadata.Images
            |> Array.map convertImageToMediaBlock
            |> String.concat "\n\n"
        
        // Combine any existing markdown content with media blocks
        match existingContent.Trim() with
        | "" -> mediaBlocks
        | content -> sprintf "%s\n\n%s" content mediaBlocks
    
    let create() : ContentProcessor<Album> = {
        Parse = fun filePath ->
            match parseAlbumFromFile filePath with
            | Ok parsedDoc -> 
                match parsedDoc.Metadata with
                | Some metadata -> 
                    Some {
                        FileName = Path.GetFileNameWithoutExtension(filePath)
                        Metadata = metadata
                    }
                | None -> None
            | Error _ -> None
        
        Render = fun album ->
            // Album rendering with IndieWeb h-entry microformat
            // Content will include :::media blocks converted from album images
            let contentWithMedia = convertAlbumToMarkdown album ""
            // Return content that will be processed by calling code
            contentWithMedia
        
        OutputPath = fun album ->
            sprintf "media/%s/index.html" album.FileName
        
        RenderCard = fun album ->
            let title = Html.escapeHtml album.Metadata.Title
            let url = sprintf "/media/%s" album.FileName
            let date = album.Metadata.Date
            let imageCount = Array.length album.Metadata.Images
            
            // Album card with first image and photo count
            let firstImageSrc = 
                if imageCount > 0 then album.Metadata.Images.[0].ImagePath
                else "/images/default-album.jpg"
            
            Html.element "article" (Html.attribute "class" "album-card h-entry")
                (Html.element "div" (Html.attribute "class" "album-thumbnail") 
                    (Html.element "a" (Html.attribute "href" url) 
                        (Html.element "img" (Html.attribute "src" firstImageSrc + Html.attribute "alt" title + Html.attribute "loading" "lazy") "")) +
                 Html.element "div" (Html.attribute "class" "album-info") 
                    (Html.element "h2" "" (Html.element "a" (Html.attribute "href" url) title) +
                     Html.element "div" (Html.attribute "class" "album-meta") 
                        (sprintf "%d photos" imageCount) +
                     Html.element "time" (Html.attribute "class" "dt-published") (Html.escapeHtml date)))
        
        RenderRss = fun album ->
            // Create RSS item for album with all images included
            let url = sprintf "https://www.luisquintanilla.me/media/%s" album.FileName
            let imageCount = Array.length album.Metadata.Images
            let description = sprintf "Album containing %d photos" imageCount
            
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", album.Metadata.Title),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" description),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url),
                    XElement(XName.Get "pubDate", album.Metadata.Date))
            Some item
    }

/// Generic content processing pipeline
module ContentPipeline =
    
    /// Process all content of a specific type and generate both HTML and feeds
    let processAllContent<'T> (processor: ContentProcessor<'T>) (sourceDirectory: string) (outputDirectory: string) =
        if Directory.Exists sourceDirectory then
            let files = Directory.GetFiles(sourceDirectory, "*.md")
            let feedData = buildContentWithFeeds processor (Array.toList files)
            
            // Generate individual content files
            feedData
            |> List.iter (fun data ->
                let outputPath = Path.Combine(outputDirectory, processor.OutputPath data.Content)
                let outputDir = Path.GetDirectoryName(outputPath)
                if not (Directory.Exists(outputDir)) then
                    Directory.CreateDirectory(outputDir) |> ignore
                
                let html = processor.Render data.Content
                File.WriteAllText(outputPath, html))
            
            // Return feed data for aggregation
            feedData
        else
            []
    
    /// Build main RSS feeds from all content types
    let buildMainFeeds (allFeedData: FeedData<obj> list) (outputDirectory: string) =
        let rssItems = 
            allFeedData
            |> List.choose (fun data -> data.RssXml)
            |> List.take (min 20 allFeedData.Length)  // Latest 20 items or fewer
        
        // TODO: Generate main RSS feed XML using existing RssService
        // TODO: Generate ATOM feed
        // TODO: Generate JSON feed
        
        ()

/// Unified feed system for consistent feed generation across all content types  
module UnifiedFeeds =
    
    /// Unified feed item representation
    type UnifiedFeedItem = {
        Title: string
        Content: string
        Url: string
        Date: string
        ContentType: string
        Tags: string array
        RssXml: XElement
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
            
            // Extract tags from RSS categories
            let tags = 
                rssXml.Elements(XName.Get "category")
                |> Seq.map (fun cat -> cat.Value)
                |> Seq.toArray
            
            Some {
                Title = title
                Content = content
                Url = url
                Date = date
                ContentType = contentType
                Tags = tags
                RssXml = rssXml
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
    
    /// Build unified feeds from all content types with proper type conversion
    let buildAllFeeds (feedDataSets: (string * (UnifiedFeedItem list)) list) (outputDirectory: string) =
        // Flatten all feed items and sort chronologically
        let allUnifiedItems = 
            feedDataSets
            |> List.collect snd
            |> List.sortByDescending (fun item -> DateTime.Parse(item.Date))
        
        // Fire-hose feed configuration (all content types)
        let fireHoseConfig = {
            Title = "Luis Quintanilla - All Updates"
            Link = "https://www.luisquintanilla.me/feed"
            Description = "All content updates from Luis Quintanilla's website"
            OutputPath = "feed/index.xml"
            ContentType = None
        }
        
        // Generate fire-hose feed
        let fireHoseFeed = generateRssFeed (allUnifiedItems |> List.take (min 20 allUnifiedItems.Length)) fireHoseConfig
        let fireHoseDir = Path.Combine(outputDirectory, "feed")
        Directory.CreateDirectory(fireHoseDir) |> ignore
        File.WriteAllText(Path.Combine(fireHoseDir, "index.xml"), fireHoseFeed)
        
        // Type-specific feed configurations
        let typeConfigurations = [
            ("posts", {
                Title = "Luis Quintanilla - Posts"
                Link = "https://www.luisquintanilla.me/posts"
                Description = "Blog posts by Luis Quintanilla"
                OutputPath = "posts/feed/index.xml"
                ContentType = Some "posts"
            })
            ("notes", {
                Title = "Luis Quintanilla - Notes"
                Link = "https://www.luisquintanilla.me/feed"
                Description = "Notes and micro-posts by Luis Quintanilla"
                OutputPath = "feed/notes/index.xml"
                ContentType = Some "notes"
            })
            ("responses", {
                Title = "Luis Quintanilla - Responses"
                Link = "https://www.luisquintanilla.me/feed/responses"
                Description = "IndieWeb responses by Luis Quintanilla"
                OutputPath = "feed/responses/index.xml"
                ContentType = Some "responses"
            })
            ("snippets", {
                Title = "Luis Quintanilla - Snippets"
                Link = "https://www.luisquintanilla.me/snippets"
                Description = "Code snippets by Luis Quintanilla"
                OutputPath = "snippets/feed/index.xml"
                ContentType = Some "snippets"
            })
            ("wiki", {
                Title = "Luis Quintanilla - Wiki"
                Link = "https://www.luisquintanilla.me/wiki"
                Description = "Wiki articles by Luis Quintanilla"
                OutputPath = "wiki/feed/index.xml"
                ContentType = Some "wiki"
            })
            ("presentations", {
                Title = "Luis Quintanilla - Presentations"
                Link = "https://www.luisquintanilla.me/presentations"
                Description = "Presentations by Luis Quintanilla"
                OutputPath = "presentations/feed/index.xml"
                ContentType = Some "presentations"
            })
            ("library", {
                Title = "Luis Quintanilla - Library"
                Link = "https://www.luisquintanilla.me/library"
                Description = "Book reviews and library updates by Luis Quintanilla"
                OutputPath = "library/feed/index.xml"
                ContentType = Some "library"
            })
            ("albums", {
                Title = "Luis Quintanilla - Media"
                Link = "https://www.luisquintanilla.me/media"
                Description = "Photo albums and media by Luis Quintanilla"
                OutputPath = "feed/media/index.xml"
                ContentType = Some "albums"
            })
        ]
        
        // Generate type-specific feeds
        typeConfigurations
        |> List.iter (fun (contentType, config) ->
            let typeItems = 
                allUnifiedItems 
                |> List.filter (fun item -> item.ContentType = contentType)
                |> List.take (min 20 (allUnifiedItems |> List.filter (fun item -> item.ContentType = contentType) |> List.length))
            
            if not (List.isEmpty typeItems) then
                let typeFeed = generateRssFeed typeItems config
                let feedDir = Path.Combine(outputDirectory, Path.GetDirectoryName(config.OutputPath))
                Directory.CreateDirectory(feedDir) |> ignore
                File.WriteAllText(Path.Combine(outputDirectory, config.OutputPath), typeFeed)
        )
        
        printfn "✅ Unified feeds generated: %d total items across %d content types" allUnifiedItems.Length (feedDataSets |> List.length)
    
    /// Convert FeedData to UnifiedFeedItem - helper functions for each content type
    let convertPostsToUnified (feedDataList: FeedData<Post> list) : UnifiedFeedItem list =
        feedDataList |> List.choose (fun feedData ->
            match feedData.RssXml with
            | Some rssXml ->
                let title = match rssXml.Element(XName.Get "title") with | null -> "Untitled" | e -> e.Value
                let url = match rssXml.Element(XName.Get "link") with | null -> "" | e -> e.Value
                let content = match rssXml.Element(XName.Get "description") with | null -> "" | e -> e.Value
                let date = match rssXml.Element(XName.Get "pubDate") with | null -> DateTime.Now.ToString("ddd, dd MMM yyyy HH:mm:ss zzz") | e -> e.Value
                let tags = rssXml.Elements(XName.Get "category") |> Seq.map (fun cat -> cat.Value) |> Seq.toArray
                Some { Title = title; Content = content; Url = url; Date = date; ContentType = "posts"; Tags = tags; RssXml = rssXml }
            | None -> None)
    
    let convertNotesToUnified (feedDataList: FeedData<Post> list) : UnifiedFeedItem list =
        feedDataList |> List.choose (fun feedData ->
            match feedData.RssXml with
            | Some rssXml ->
                let title = match rssXml.Element(XName.Get "title") with | null -> "Untitled" | e -> e.Value
                let url = match rssXml.Element(XName.Get "link") with | null -> "" | e -> e.Value
                let content = match rssXml.Element(XName.Get "description") with | null -> "" | e -> e.Value
                let date = match rssXml.Element(XName.Get "pubDate") with | null -> DateTime.Now.ToString("ddd, dd MMM yyyy HH:mm:ss zzz") | e -> e.Value
                let tags = rssXml.Elements(XName.Get "category") |> Seq.map (fun cat -> cat.Value) |> Seq.toArray
                Some { Title = title; Content = content; Url = url; Date = date; ContentType = "notes"; Tags = tags; RssXml = rssXml }
            | None -> None)
    
    let convertResponsesToUnified (feedDataList: FeedData<Response> list) : UnifiedFeedItem list =
        feedDataList |> List.choose (fun feedData ->
            match feedData.RssXml with
            | Some rssXml ->
                let title = match rssXml.Element(XName.Get "title") with | null -> "Untitled" | e -> e.Value
                let url = match rssXml.Element(XName.Get "link") with | null -> "" | e -> e.Value
                let content = match rssXml.Element(XName.Get "description") with | null -> "" | e -> e.Value
                let date = match rssXml.Element(XName.Get "pubDate") with | null -> DateTime.Now.ToString("ddd, dd MMM yyyy HH:mm:ss zzz") | e -> e.Value
                let tags = rssXml.Elements(XName.Get "category") |> Seq.map (fun cat -> cat.Value) |> Seq.toArray
                Some { Title = title; Content = content; Url = url; Date = date; ContentType = "responses"; Tags = tags; RssXml = rssXml }
            | None -> None)
    
    let convertSnippetsToUnified (feedDataList: FeedData<Snippet> list) : UnifiedFeedItem list =
        feedDataList |> List.choose (fun feedData ->
            match feedData.RssXml with
            | Some rssXml ->
                let title = match rssXml.Element(XName.Get "title") with | null -> "Untitled" | e -> e.Value
                let url = match rssXml.Element(XName.Get "link") with | null -> "" | e -> e.Value
                let content = match rssXml.Element(XName.Get "description") with | null -> "" | e -> e.Value
                let date = match rssXml.Element(XName.Get "pubDate") with | null -> DateTime.Now.ToString("ddd, dd MMM yyyy HH:mm:ss zzz") | e -> e.Value
                let tags = rssXml.Elements(XName.Get "category") |> Seq.map (fun cat -> cat.Value) |> Seq.toArray
                Some { Title = title; Content = content; Url = url; Date = date; ContentType = "snippets"; Tags = tags; RssXml = rssXml }
            | None -> None)
    
    let convertWikisToUnified (feedDataList: FeedData<Wiki> list) : UnifiedFeedItem list =
        feedDataList |> List.choose (fun feedData ->
            match feedData.RssXml with
            | Some rssXml ->
                let title = match rssXml.Element(XName.Get "title") with | null -> "Untitled" | e -> e.Value
                let url = match rssXml.Element(XName.Get "link") with | null -> "" | e -> e.Value
                let content = match rssXml.Element(XName.Get "description") with | null -> "" | e -> e.Value
                let date = match rssXml.Element(XName.Get "pubDate") with | null -> DateTime.Now.ToString("ddd, dd MMM yyyy HH:mm:ss zzz") | e -> e.Value
                let tags = rssXml.Elements(XName.Get "category") |> Seq.map (fun cat -> cat.Value) |> Seq.toArray
                Some { Title = title; Content = content; Url = url; Date = date; ContentType = "wiki"; Tags = tags; RssXml = rssXml }
            | None -> None)
    
    let convertPresentationsToUnified (feedDataList: FeedData<Presentation> list) : UnifiedFeedItem list =
        feedDataList |> List.choose (fun feedData ->
            match feedData.RssXml with
            | Some rssXml ->
                let title = match rssXml.Element(XName.Get "title") with | null -> "Untitled" | e -> e.Value
                let url = match rssXml.Element(XName.Get "link") with | null -> "" | e -> e.Value
                let content = match rssXml.Element(XName.Get "description") with | null -> "" | e -> e.Value
                let date = match rssXml.Element(XName.Get "pubDate") with | null -> DateTime.Now.ToString("ddd, dd MMM yyyy HH:mm:ss zzz") | e -> e.Value
                let tags = rssXml.Elements(XName.Get "category") |> Seq.map (fun cat -> cat.Value) |> Seq.toArray
                Some { Title = title; Content = content; Url = url; Date = date; ContentType = "presentations"; Tags = tags; RssXml = rssXml }
            | None -> None)
    
    let convertBooksToUnified (feedDataList: FeedData<Book> list) : UnifiedFeedItem list =
        feedDataList |> List.choose (fun feedData ->
            match feedData.RssXml with
            | Some rssXml ->
                let title = match rssXml.Element(XName.Get "title") with | null -> "Untitled" | e -> e.Value
                let url = match rssXml.Element(XName.Get "link") with | null -> "" | e -> e.Value
                let content = match rssXml.Element(XName.Get "description") with | null -> "" | e -> e.Value
                let date = match rssXml.Element(XName.Get "pubDate") with | null -> DateTime.Now.ToString("ddd, dd MMM yyyy HH:mm:ss zzz") | e -> e.Value
                let tags = rssXml.Elements(XName.Get "category") |> Seq.map (fun cat -> cat.Value) |> Seq.toArray
                Some { Title = title; Content = content; Url = url; Date = date; ContentType = "library"; Tags = tags; RssXml = rssXml }
            | None -> None)
    
    let convertAlbumsToUnified (feedDataList: FeedData<Album> list) : UnifiedFeedItem list =
        feedDataList |> List.choose (fun feedData ->
            match feedData.RssXml with
            | Some rssXml ->
                let title = match rssXml.Element(XName.Get "title") with | null -> "Untitled" | e -> e.Value
                let url = match rssXml.Element(XName.Get "link") with | null -> "" | e -> e.Value
                let content = match rssXml.Element(XName.Get "description") with | null -> "" | e -> e.Value
                let date = match rssXml.Element(XName.Get "pubDate") with | null -> DateTime.Now.ToString("ddd, dd MMM yyyy HH:mm:ss zzz") | e -> e.Value
                let tags = rssXml.Elements(XName.Get "category") |> Seq.map (fun cat -> cat.Value) |> Seq.toArray
                Some { Title = title; Content = content; Url = url; Date = date; ContentType = "albums"; Tags = tags; RssXml = rssXml }
            | None -> None)
