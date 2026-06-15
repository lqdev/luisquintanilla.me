module GenericBuilder

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

/// Data structure for feed generation with both card and RSS item
type FeedData<'T> = {
    Content: 'T
    CardHtml: string
    RssXml: XElement option
}

/// URL normalization for RSS feeds to ensure absolute URLs
let normalizeUrlsForRss (content: string) (baseUrl: string) =
    let baseUrl = baseUrl.TrimEnd('/')
    
    // Normalize relative image src attributes
    let imgPattern = @"src\s*=\s*""(/[^""]*)""|src\s*=\s*'(/[^']*)'"
    let normalizedImages = 
        System.Text.RegularExpressions.Regex.Replace(content, imgPattern, fun m ->
            let quote = if m.Value.Contains("\"") then "\"" else "'"
            let path = if m.Groups.[1].Success then m.Groups.[1].Value else m.Groups.[2].Value
            sprintf "src=%s%s%s%s" quote baseUrl path quote)
    
    // Normalize relative link href attributes  
    let linkPattern = @"href\s*=\s*""(/[^""]*)""|href\s*=\s*'(/[^']*)'"
    let normalizedLinks =
        System.Text.RegularExpressions.Regex.Replace(normalizedImages, linkPattern, fun m ->
            let quote = if m.Value.Contains("\"") then "\"" else "'"
            let path = if m.Groups.[1].Success then m.Groups.[1].Value else m.Groups.[2].Value
            sprintf "href=%s%s%s%s" quote baseUrl path quote)
    
    normalizedLinks

/// Generate source:markdown element for RSS feeds
let generateSourceMarkdown (markdownSource: string option) : XElement option =
    match markdownSource with
    | Some md when not (String.IsNullOrWhiteSpace md) ->
        // Escape any ]]> that might break CDATA
        let escaped = md.Replace("]]>", "]]]]><![CDATA[>")
        // Create XElement with source namespace
        let sourceNs = XNamespace.Get("http://source.scripting.com/")
        Some (XElement(sourceNs + "markdown", XCData(escaped)))
    | _ -> None

/// Generic content processor pattern for consistent content handling
type ContentProcessor<'T> = {
    /// Parse content from file path to domain type, transporting a typed
    /// `ContentError` on failure (F8 — the rail is no longer severed to Option).
    Parse: string -> Result<'T, Diagnostics.ContentError>
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
    // F8 full railway (plan 2.8): parse every file, accumulating failures instead
    // of short-circuiting. Successes render and flow to the feeds; failures are
    // transported as typed `ContentError`s to the diagnostics reporter (which
    // prints a self-contained block and records them for strict-mode exit). One
    // bad file does not block publishing the rest, so `_public/` is byte-identical
    // whenever nothing fails — a failed parse is dropped here exactly as the old
    // `| Error _ -> None` dropped it, only now it is reported instead of silent.
    let parsed =
        filePaths
        |> List.map (fun filePath -> processor.Parse filePath)
    parsed
    |> List.choose (function Error e -> Some e | Ok _ -> None)
    |> List.iter Diagnostics.report
    parsed
    |> List.choose (function
        | Ok content ->
            let cardHtml = processor.RenderCard content
            let rssXml = processor.RenderRss content
            Some { Content = content; CardHtml = cardHtml; RssXml = rssXml }
        | Error _ -> None)

/// Content processors for existing domain types

/// Post content processor
module PostProcessor =
    // Helper to extract markdown content without frontmatter
    let private extractContentWithoutFrontMatter (rawMarkdown: string) : string =
        let lines = rawMarkdown.Split([|'\n'|], StringSplitOptions.None)
        if lines.Length > 0 && lines.[0].Trim() = "---" then
            // Find the closing ---
            let closingIndex = 
                lines 
                |> Array.skip 1
                |> Array.findIndex (fun line -> line.Trim() = "---")
            // Return everything after the second ---
            lines 
            |> Array.skip (closingIndex + 2)
            |> String.concat "\n"
        else
            rawMarkdown

    let create() : ContentProcessor<Post> = {
        Parse = fun filePath ->
            match parsePostFromFile filePath with
            | Ok parsedDoc -> 
                match parsedDoc.Metadata with
                | Some metadata -> 
                    let contentWithoutFrontmatter = extractContentWithoutFrontMatter parsedDoc.RawMarkdown
                    let readingTime = ReadingTimeService.calculateReadingTime contentWithoutFrontmatter
                    Ok {
                        FileName = Path.GetFileNameWithoutExtension(filePath)
                        Metadata = { metadata with ReadingTimeMinutes = readingTime }
                        Content = contentWithoutFrontmatter  // Use raw markdown without frontmatter
                        MarkdownSource = Some contentWithoutFrontmatter
                    }
                | None -> Error (Diagnostics.ContentError.ParseFailure(filePath, "frontmatter", "no front-matter block found (expected a leading '---' fence)"))
            | Error e -> Error (Diagnostics.ofParseError filePath e)
        
        Render = fun post ->
            // Return raw markdown content to be processed by the calling code
            // The Builder will apply MarkdownService.convertMdToHtml to this content
            post.Content
        
        OutputPath = fun post ->
            sprintf "posts/%s/index.html" post.FileName
        
        RenderCard = fun post ->
            let title = Html.escapeHtml post.Metadata.Title
            let description = 
                if isNull post.Metadata.Description then 
                    "No description available"
                else 
                    Html.escapeHtml post.Metadata.Description
            let url = sprintf "/posts/%s/" post.FileName
            let date = post.Metadata.Date
            
            Html.element "article" (Html.attribute "class" "post-card")
                (Html.element "h2" "" (Html.element "a" (Html.attribute "href" url) title) +
                 Html.element "p" "" description)
        
        RenderRss = fun post ->
            // Create RSS item for post using existing pattern
            let url = sprintf "https://www.lqdev.me/posts/%s" post.FileName
            let categories = 
                if isNull post.Metadata.Tags then []
                else post.Metadata.Tags |> Array.map (fun tag -> XElement(XName.Get "category", tag)) |> Array.toList
            
            // Normalize URLs in content for RSS compatibility
            let normalizedContent = normalizeUrlsForRss post.Content "https://www.lqdev.me"
            
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", post.Metadata.Title),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" normalizedContent),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url),
                    XElement(XName.Get "pubDate", post.Metadata.Date))
            
            // Add categories if they exist
            if not (List.isEmpty categories) then
                item.Add(categories |> List.toArray)
            
            // Add source:markdown if available
            match generateSourceMarkdown post.MarkdownSource with
            | Some sourceElement -> item.Add(sourceElement)
            | None -> ()
            
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
                    let readingTime = ReadingTimeService.calculateReadingTime parsedDoc.TextContent
                    Ok {
                        FileName = Path.GetFileNameWithoutExtension(filePath)
                        Metadata = { metadata with ReadingTimeMinutes = readingTime }
                        Content = parsedDoc.TextContent  // Raw markdown content
                        MarkdownSource = Some parsedDoc.RawMarkdown
                    }
                | None -> Error (Diagnostics.ContentError.ParseFailure(filePath, "frontmatter", "no front-matter block found (expected a leading '---' fence)"))
            | Error e -> Error (Diagnostics.ofParseError filePath e)
        
        Render = fun note ->
            // Return ViewEngine node rendered to HTML string
            let viewNode = article [ _class "note" ] [ rawText note.Content ]
            RenderView.AsString.xmlNode viewNode
        
        OutputPath = fun note ->
            sprintf "notes/%s/index.html" note.FileName
        
        RenderCard = fun note ->
            let title = Html.escapeHtml note.Metadata.Title
            let description = 
                if isNull note.Metadata.Description then 
                    "No description available"
                else 
                    Html.escapeHtml note.Metadata.Description
            let url = sprintf "/notes/%s/" note.FileName
            let date = note.Metadata.Date
            
            // Include content excerpt like other processors
            let contentExcerpt = 
                if note.Content.Length > 200 then
                    note.Content.Substring(0, 200) + "..."
                else
                    note.Content
            
            // Use ViewEngine for consistency with Render function
            let viewNode = 
                article [ _class "note-card" ] [
                    h2 [] [ a [ _href url ] [ Text title ] ]
                    p [ _class "description" ] [ Text description ]
                    div [ _class "content-excerpt" ] [ Text contentExcerpt ]
                ]
            RenderView.AsString.xmlNode viewNode
        
        RenderRss = fun note ->
            // Create RSS item for note using existing pattern
            let url = sprintf "https://www.lqdev.me/notes/%s" note.FileName
            let categories = 
                if isNull note.Metadata.Tags then []
                else note.Metadata.Tags |> Array.map (fun tag -> XElement(XName.Get "category", tag)) |> Array.toList
            
            // Normalize URLs in content for RSS compatibility
            let normalizedContent = normalizeUrlsForRss note.Content "https://www.lqdev.me"
            
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", note.Metadata.Title),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" normalizedContent),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url),
                    XElement(XName.Get "pubDate", note.Metadata.Date))
            
            // Add categories if they exist
            if not (List.isEmpty categories) then
                item.Add(categories |> List.toArray)
            
            // Add source:markdown if available
            match generateSourceMarkdown note.MarkdownSource with
            | Some sourceElement -> item.Add(sourceElement)
            | None -> ()
            
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
                    Ok {
                        FileName = Path.GetFileNameWithoutExtension(filePath)
                        Metadata = metadata
                        Content = parsedDoc.TextContent
                        MarkdownSource = Some parsedDoc.RawMarkdown
                    }
                | None -> Error (Diagnostics.ContentError.ParseFailure(filePath, "frontmatter", "no front-matter block found (expected a leading '---' fence)"))
            | Error e -> Error (Diagnostics.ofParseError filePath e)
        
        Render = fun snippet ->
            let viewNode = article [] [ rawText snippet.Content ]
            RenderView.AsString.xmlNode viewNode
        
        OutputPath = fun snippet ->
            sprintf "snippets/%s.html" snippet.FileName
        
        RenderCard = fun snippet ->
            let title = Html.escapeHtml snippet.Metadata.Title
            let excerpt = Html.escapeHtml (snippet.Content.Substring(0, min 150 snippet.Content.Length) + "...")
            let url = sprintf "/resources/snippets/%s/" snippet.FileName
            
            Html.element "article" (Html.attribute "class" "snippet-card")
                (Html.element "h2" "" (Html.element "a" (Html.attribute "href" url) title) +
                 Html.element "p" "" excerpt)
        
        RenderRss = fun snippet ->
            // Create RSS item for snippet similar to post
            let url = sprintf "https://www.lqdev.me/resources/snippets/%s" snippet.FileName
            let categories = 
                if String.IsNullOrEmpty(snippet.Metadata.Tags) then []
                else snippet.Metadata.Tags.Split(',') 
                     |> Array.map (fun tag -> XElement(XName.Get "category", tag.Trim())) 
                     |> Array.toList
            
            // Normalize URLs in content for RSS compatibility
            let normalizedContent = normalizeUrlsForRss snippet.Content "https://www.lqdev.me"
            
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", snippet.Metadata.Title),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" normalizedContent),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url))
            
            // Add pubDate if created_date exists
            if not (String.IsNullOrEmpty(snippet.Metadata.CreatedDate)) then
                item.Add(XElement(XName.Get "pubDate", snippet.Metadata.CreatedDate))
            
            // Add categories if they exist
            if not (List.isEmpty categories) then
                item.Add(categories |> List.toArray)
            
            // Add source:markdown if available
            match generateSourceMarkdown snippet.MarkdownSource with
            | Some sourceElement -> item.Add(sourceElement)
            | None -> ()
                
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
                    Ok {
                        FileName = Path.GetFileNameWithoutExtension(filePath)
                        Metadata = metadata
                        Content = parsedDoc.TextContent
                        MarkdownSource = Some parsedDoc.RawMarkdown
                    }
                | None -> Error (Diagnostics.ContentError.ParseFailure(filePath, "frontmatter", "no front-matter block found (expected a leading '---' fence)"))
            | Error e -> Error (Diagnostics.ofParseError filePath e)
        
        Render = fun wiki ->
            let viewNode = article [] [ rawText wiki.Content ]
            RenderView.AsString.xmlNode viewNode
        
        OutputPath = fun wiki ->
            sprintf "wiki/%s.html" wiki.FileName
        
        RenderCard = fun wiki ->
            let title = Html.escapeHtml wiki.Metadata.Title
            let excerpt = Html.escapeHtml (wiki.Content.Substring(0, min 150 wiki.Content.Length) + "...")
            let url = sprintf "/resources/wiki/%s/" wiki.FileName
            
            Html.element "article" (Html.attribute "class" "wiki-card")
                (Html.element "h2" "" (Html.element "a" (Html.attribute "href" url) title) +
                 Html.element "p" "" excerpt)
        
        RenderRss = fun wiki ->
            // Create RSS item for wiki similar to post
            let url = sprintf "https://www.lqdev.me/resources/wiki/%s" wiki.FileName
            let categories = 
                if String.IsNullOrEmpty(wiki.Metadata.Tags) then []
                else wiki.Metadata.Tags.Split(',') 
                     |> Array.map (fun tag -> XElement(XName.Get "category", tag.Trim())) 
                     |> Array.toList
            
            // Normalize URLs in content for RSS compatibility
            let normalizedContent = normalizeUrlsForRss wiki.Content "https://www.lqdev.me"
            
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", wiki.Metadata.Title),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" normalizedContent),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url))
            
            // Add pubDate if last_updated_date exists
            if not (String.IsNullOrEmpty(wiki.Metadata.LastUpdatedDate)) then
                item.Add(XElement(XName.Get "pubDate", wiki.Metadata.LastUpdatedDate))
            
            // Add categories if they exist
            if not (List.isEmpty categories) then
                item.Add(categories |> List.toArray)
            
            // Add source:markdown if available
            match generateSourceMarkdown wiki.MarkdownSource with
            | Some sourceElement -> item.Add(sourceElement)
            | None -> ()
                
            Some item
    }

/// AI Memex content processor
module AiMemexProcessor =
    let private extractContentWithoutFrontMatter (rawMarkdown: string) : string =
        let lines = rawMarkdown.Split([|'\n'|], StringSplitOptions.None)
        if lines.Length > 0 && lines.[0].Trim() = "---" then
            let closingIndex = 
                lines 
                |> Array.skip 1
                |> Array.findIndex (fun line -> line.Trim() = "---")
            lines 
            |> Array.skip (closingIndex + 2)
            |> String.concat "\n"
        else
            rawMarkdown

    let create() : ContentProcessor<AiMemex> = {
        Parse = fun filePath ->
            match parseAiMemexFromFile filePath with
            | Ok parsedDoc -> 
                match parsedDoc.Metadata with
                | Some metadata -> 
                    Ok {
                        FileName = Path.GetFileNameWithoutExtension(filePath)
                        Metadata = metadata
                        Content = parsedDoc.TextContent
                        MarkdownSource = Some (extractContentWithoutFrontMatter parsedDoc.RawMarkdown)
                    }
                | None -> Error (Diagnostics.ContentError.ParseFailure(filePath, "frontmatter", "no front-matter block found (expected a leading '---' fence)"))
            | Error e -> Error (Diagnostics.ofParseError filePath e)
        
        Render = fun aiMemex ->
            let viewNode = article [] [ rawText aiMemex.Content ]
            RenderView.AsString.xmlNode viewNode
        
        OutputPath = fun aiMemex ->
            sprintf "resources/ai-memex/%s.html" aiMemex.FileName
        
        RenderCard = fun aiMemex ->
            let title = Html.escapeHtml aiMemex.Metadata.Title
            let excerpt = Html.escapeHtml (aiMemex.Content.Substring(0, min 150 aiMemex.Content.Length) + "...")
            let url = sprintf "/resources/ai-memex/%s/" aiMemex.FileName
            
            Html.element "article" (Html.attribute "class" "ai-memex-card")
                (Html.element "h2" "" (Html.element "a" (Html.attribute "href" url) title) +
                 Html.element "p" "" excerpt)
        
        RenderRss = fun aiMemex ->
            let url = sprintf "https://www.lqdev.me/resources/ai-memex/%s" aiMemex.FileName
            let categories = 
                if String.IsNullOrEmpty(aiMemex.Metadata.Tags) then []
                else aiMemex.Metadata.Tags.Split(',') 
                     |> Array.map (fun tag -> XElement(XName.Get "category", tag.Trim())) 
                     |> Array.toList
            
            // Normalize URLs in content for RSS compatibility
            let normalizedContent = normalizeUrlsForRss aiMemex.Content "https://www.lqdev.me"
            
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", aiMemex.Metadata.Title),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" normalizedContent),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url))
            
            // Add pubDate if published_date exists
            if not (String.IsNullOrEmpty(aiMemex.Metadata.PublishedDate)) then
                item.Add(XElement(XName.Get "pubDate", aiMemex.Metadata.PublishedDate))
            
            // Add categories if they exist
            if not (List.isEmpty categories) then
                item.Add(categories |> List.toArray)
            
            // Add source:markdown if available
            match generateSourceMarkdown aiMemex.MarkdownSource with
            | Some sourceElement -> item.Add(sourceElement)
            | None -> ()
                
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
                    // For presentations, we need raw markdown content (not HTML) for reveal.js
                    // Extract the content without frontmatter from the already parsed raw markdown
                    let lines = parsedDoc.RawMarkdown.Split([|'\n'|], StringSplitOptions.None)
                    
                    let markdownContent = 
                        if lines.Length > 0 && lines.[0].Trim() = "---" then
                            let endIdx = 
                                lines 
                                |> Array.skip 1
                                |> Array.tryFindIndex (fun line -> line.Trim() = "---")
                            match endIdx with
                            | Some idx -> 
                                lines.[(idx + 2)..]
                                |> String.concat "\n"
                            | None -> parsedDoc.RawMarkdown
                        else parsedDoc.RawMarkdown
                    
                    Ok {
                        FileName = System.IO.Path.GetFileNameWithoutExtension(filePath)
                        Metadata = metadata
                        Content = markdownContent  // Store raw markdown for reveal.js
                        MarkdownSource = Some markdownContent
                    }
                | None -> Error (Diagnostics.ContentError.ParseFailure(filePath, "frontmatter", "no front-matter block found (expected a leading '---' fence)"))
            | Error e -> Error (Diagnostics.ofParseError filePath e)
        
        Render = fun presentation ->
            // Return raw content for reveal.js processing in presentationPageView
            presentation.Content
        
        OutputPath = fun presentation ->
            sprintf "presentations/%s.html" presentation.FileName
        
        RenderCard = fun presentation ->
            let title = Html.escapeHtml presentation.Metadata.Title
            let excerpt = Html.escapeHtml (presentation.Content.Substring(0, min 150 presentation.Content.Length) + "...")
            let url = sprintf "/resources/presentations/%s/" presentation.FileName
            
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
            let url = sprintf "https://www.lqdev.me/resources/presentations/%s" presentation.FileName
            let categories = 
                if String.IsNullOrEmpty(presentation.Metadata.Tags) then []
                else presentation.Metadata.Tags.Split(',') 
                     |> Array.map (fun tag -> XElement(XName.Get "category", tag.Trim())) 
                     |> Array.toList
            
            // Normalize URLs in content for RSS compatibility
            let normalizedContent = normalizeUrlsForRss presentation.Content "https://www.lqdev.me"
            
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", presentation.Metadata.Title),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" normalizedContent),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url))
            
            // Add pubDate if date exists
            if not (String.IsNullOrEmpty(presentation.Metadata.Date)) then
                item.Add(XElement(XName.Get "pubDate", presentation.Metadata.Date))
            
            // Add categories if they exist
            if not (List.isEmpty categories) then
                item.Add(categories |> List.toArray)
            
            // Add source:markdown if available
            match generateSourceMarkdown presentation.MarkdownSource with
            | Some sourceElement -> item.Add(sourceElement)
            | None -> ()
                
            Some item
    }

/// Review data extractor for processing review blocks during parsing
module ReviewDataExtractor =
    /// Extract review data from raw markdown content including item type for badge display
    let extractReviewData (rawMarkdown: string) : (string option * float * float * string option) =
        if not (String.IsNullOrWhiteSpace(rawMarkdown)) && rawMarkdown.Contains(":::review") then
            try
                let pipeline = 
                    MarkdownPipelineBuilder()
                        |> useCustomBlocks
                        |> fun builder -> builder.Build()
                let document = Markdown.Parse(rawMarkdown, pipeline)
                let customBlocks = extractCustomBlocks document
                
                match customBlocks.TryGetValue("review") with
                | true, reviewList when reviewList.Length > 0 ->
                    match reviewList.[0] with
                    | :? CustomBlocks.ReviewData as reviewData -> 
                        (reviewData.ImageUrl, reviewData.Rating, reviewData.Scale, Some reviewData.ItemType)
                    | _ -> (None, 0.0, 5.0, None)
                | _ -> (None, 0.0, 5.0, None)
            with
            | _ -> (None, 0.0, 5.0, None)
        else
            (None, 0.0, 5.0, None)

/// Phase 5C: Review metadata for Schema.org Review vocabulary in ActivityPub
/// Defined at module level so it can be used by both BookProcessor and UnifiedFeeds
type ReviewMetadata = {
    ItemName: string          // Name of the reviewed item (book title, movie name, etc.)
    ItemType: string          // Type of item: "book", "movie", "music", "product", "business"
    Rating: float             // Rating value (e.g., 4.5)
    Scale: float              // Rating scale max (e.g., 5.0)
    Summary: string option    // Brief review summary
    ItemUrl: string option    // URL of the reviewed item
    ImageUrl: string option   // Cover/thumbnail image URL
    Author: string option     // For books: author name
    Isbn: string option       // For books: ISBN
}

/// Phase 5D: Media metadata for ActivityPub Image/Video/Audio objects
/// Defined at module level so it can be used by both media processing and UnifiedFeeds
type MediaAPData = {
    MediaUrl: string          // URL of the media file
    MediaType: string         // MIME type (e.g., "image/jpeg", "video/mp4")
    ObjectType: string        // ActivityPub type: "Image", "Video", "Audio"
    AltText: string option    // Alt text for accessibility
    Caption: string option    // Caption/description
}

/// Phase 5D: Media extractor for detecting and extracting media from content
module MediaExtractor =
    open System.Text.RegularExpressions
    
    /// Detect MIME type from URL file extension
    let detectMimeType (url: string) : string =
        let ext = Path.GetExtension(url).ToLowerInvariant()
        match ext with
        | ".jpg" | ".jpeg" -> "image/jpeg"
        | ".png" -> "image/png"
        | ".gif" -> "image/gif"
        | ".webp" -> "image/webp"
        | ".mp4" -> "video/mp4"
        | ".webm" -> "video/webm"
        | ".mov" -> "video/quicktime"
        | ".mp3" -> "audio/mpeg"
        | ".wav" -> "audio/wav"
        | ".ogg" -> "audio/ogg"
        | ".m4a" -> "audio/mp4"
        | _ -> "application/octet-stream"
    
    /// Determine ActivityPub object type from MIME type
    let detectObjectType (mimeType: string) : string =
        if mimeType.StartsWith("image/") then "Image"
        elif mimeType.StartsWith("video/") then "Video"
        elif mimeType.StartsWith("audio/") then "Audio"
        else "Document"
    
    /// Extract first media item from :::media block for media-primary content
    /// Returns Some MediaAPData if content has a media block, None otherwise
    let extractPrimaryMedia (content: string) : MediaAPData option =
        // Defensive null check to prevent NullReferenceException
        if String.IsNullOrWhiteSpace(content) then None
        else
            let mediaPattern = @":::media\s*([\s\S]*?):::(?:media)?"
            let matches = Regex.Matches(content, mediaPattern)
            
            if matches.Count = 0 then None
            else
                let firstMediaContent = matches.[0].Groups.[1].Value
                
                // Extract URL
                let urlMatch = Regex.Match(firstMediaContent, @"url:\s*[""']([^""']+)[""']")
                if not urlMatch.Success then None
                else
                    let url = urlMatch.Groups.[1].Value
                    let mimeType = detectMimeType url
                    let objectType = detectObjectType mimeType
                    
                    // Extract caption and alt text
                    let captionMatch = Regex.Match(firstMediaContent, @"caption:\s*[""']([^""']+)[""']")
                    let altMatch = Regex.Match(firstMediaContent, @"alt:\s*[""']([^""']+)[""']")
                    
                    let caption = if captionMatch.Success then Some captionMatch.Groups.[1].Value else None
                    let altText = if altMatch.Success then Some altMatch.Groups.[1].Value else None
                    
                    Some {
                        MediaUrl = url
                        MediaType = mimeType
                        ObjectType = objectType
                        AltText = altText
                        Caption = caption
                    }

/// Book content processor
module BookProcessor =
    // Cache for review data extracted during parsing
    // Phase 5C: Enhanced to store full ReviewMetadata for ActivityPub Schema.org integration
    let private reviewDataCache = System.Collections.Concurrent.ConcurrentDictionary<string, ReviewMetadata option>()
    
    // Helper function to extract rating from custom review blocks using regex (for backward compatibility)
    let private extractRatingFromContent (content: string) : float option =
        try
            // Use regex to find rating in :::review blocks
            let reviewBlockPattern = @":::review\s*\n(.*?)\n:::"
            let ratingPattern = @"rating:\s*([\d.]+)"
            
            let reviewMatches = System.Text.RegularExpressions.Regex.Matches(content, reviewBlockPattern, System.Text.RegularExpressions.RegexOptions.Singleline)
            
            if reviewMatches.Count > 0 then
                let reviewContent = reviewMatches.[0].Groups.[1].Value
                let ratingMatch = System.Text.RegularExpressions.Regex.Match(reviewContent, ratingPattern)
                
                if ratingMatch.Success then
                    match System.Double.TryParse(ratingMatch.Groups.[1].Value) with
                    | (true, rating) -> Some rating
                    | _ -> None
                else None
            else None
        with
        | _ -> None

    let create() : ContentProcessor<Book> = {
        Parse = fun filePath ->
            match parseBookFromFile filePath with
            | Ok parsedDoc -> 
                match parsedDoc.Metadata with
                | Some metadata -> 
                    let fileName = Path.GetFileNameWithoutExtension(filePath)
                    
                    // Try to extract review data from parsed custom blocks (new approach)
                    let reviewDataOpt = 
                        match parsedDoc.CustomBlocks.TryGetValue("review") with
                        | true, reviewList when reviewList.Length > 0 ->
                            match reviewList.[0] with
                            | :? CustomBlocks.ReviewData as reviewData -> Some reviewData
                            | _ -> None
                        | _ -> None
                    
                    // Populate metadata from review block if available, otherwise use frontmatter
                    let finalMetadata = 
                        match reviewDataOpt with
                        | Some reviewData ->
                            // Use review block as source of truth
                            // Note: Keep existing title from frontmatter to maintain consistency with views
                            // that expect "Book Title Review" format rather than just "Book Title"
                            { metadata with
                                Author = reviewData.GetAuthor()
                                Isbn = reviewData.GetIsbn()
                                Cover = reviewData.GetCover()
                                Rating = reviewData.Rating
                                DatePublished = reviewData.GetDatePublished()
                            }
                        | None ->
                            // Fallback to frontmatter (backward compatibility)
                            metadata
                    
                    // Phase 5C: Store full review metadata in cache for ActivityPub Schema.org integration
                    let reviewMetadata : ReviewMetadata option =
                        match reviewDataOpt with
                        | Some reviewData ->
                            Some {
                                ItemName = reviewData.Item  // The actual item name (book title)
                                ItemType = reviewData.ItemType  // "book", "movie", etc.
                                Rating = reviewData.Rating
                                Scale = reviewData.Scale
                                Summary = if String.IsNullOrWhiteSpace(reviewData.Summary) then None else Some reviewData.Summary
                                ItemUrl = reviewData.ItemUrl
                                ImageUrl = reviewData.ImageUrl
                                Author = Some (reviewData.GetAuthor())  // For books
                                Isbn = let isbn = reviewData.GetIsbn() in if String.IsNullOrWhiteSpace(isbn) then None else Some isbn
                            }
                        | None ->
                            // Fallback to frontmatter data (backward compatibility)
                            let (reviewImageUrl, reviewRating, reviewScale, reviewItemType) = ReviewDataExtractor.extractReviewData parsedDoc.RawMarkdown
                            if reviewRating > 0.0 then
                                Some {
                                    ItemName = metadata.Title  // Use frontmatter title as fallback
                                    ItemType = reviewItemType |> Option.defaultValue "book"
                                    Rating = reviewRating
                                    Scale = reviewScale
                                    Summary = None
                                    ItemUrl = None
                                    ImageUrl = reviewImageUrl
                                    Author = if String.IsNullOrWhiteSpace(metadata.Author) then None else Some metadata.Author
                                    Isbn = if String.IsNullOrWhiteSpace(metadata.Isbn) then None else Some metadata.Isbn
                                }
                            else None
                    
                    reviewDataCache.[fileName] <- reviewMetadata
                    
                    Ok {
                        FileName = fileName
                        Metadata = finalMetadata
                        Content = parsedDoc.TextContent
                        MarkdownSource = Some parsedDoc.RawMarkdown
                    }
                | None -> Error (Diagnostics.ContentError.ParseFailure(filePath, "frontmatter", "no front-matter block found (expected a leading '---' fence)"))
            | Error e -> Error (Diagnostics.ofParseError filePath e)
        
        Render = fun book ->
            // For now, return content as-is. Later integrate with existing Views.Generator
            let viewNode = article [] [ rawText book.Content ]
            RenderView.AsString.xmlNode viewNode
        
        OutputPath = fun book ->
            sprintf "reviews/%s.html" book.FileName
        
        RenderCard = fun book ->
            let title = Html.escapeHtml book.Metadata.Title
            let url = sprintf "/reviews/%s/" book.FileName
            
            // Get review data from cache (extracted during parsing)
            // Phase 5C: Cache now stores full ReviewMetadata
            let reviewMetadataOpt = 
                match reviewDataCache.TryGetValue(book.FileName) with
                | (true, data) -> data
                | _ -> None
            
            // Extract values from ReviewMetadata with fallbacks
            let (reviewImageUrlOpt, reviewRating, reviewScale, reviewItemType) =
                match reviewMetadataOpt with
                | Some rm -> (rm.ImageUrl, rm.Rating, rm.Scale, Some rm.ItemType)
                | None -> (None, book.Metadata.Rating, 5.0, None)
            
            // Determine image URL with proper fallbacks
            let imageUrl = 
                match reviewImageUrlOpt with
                | Some reviewImageUrl -> reviewImageUrl
                | None -> 
                    if String.IsNullOrEmpty(book.Metadata.Cover) then
                        "/assets/img/book-placeholder.png"
                    else
                        book.Metadata.Cover
            
            // Use review rating if available, otherwise use metadata rating
            let (ratingValue, ratingScaleValue) = 
                if reviewRating > 0.0 then (reviewRating, reviewScale)
                else (book.Metadata.Rating, 5.0)
            
            // Display rating with SVG stars + numeric value using shared helper
            let ratingHtml = 
                if ratingValue > 0.0 then
                    sprintf "<div class=\"rating\">%s</div>" (BlockRenderers.StarRating.render ratingValue ratingScaleValue)
                else ""
            
            // Note: Item type badge is shown in the timeline card header (not duplicated here)
            // The header badge is extracted from reviewItemType by timelineHomeView/timelineHomeViewStratified
            
            // Create simplified timeline card with only: image and rating (no duplicate badge, title, or author)
            let coverHtml = 
                if not (String.IsNullOrEmpty(imageUrl)) then
                    sprintf "<img src=\"%s\" alt=\"%s cover\" class=\"review-image img-fluid\">" 
                        (Html.escapeHtml imageUrl) (Html.escapeHtml book.Metadata.Title)
                else ""
            
            // Hidden span to carry review item type for header badge extraction (not displayed)
            let hiddenItemTypeHtml = 
                match reviewItemType with
                | Some itemType when not (String.IsNullOrWhiteSpace(itemType)) ->
                    sprintf "<span class=\"review-item-type\" style=\"display:none\" data-item-type=\"%s\"></span>" (Html.escapeHtml itemType)
                | _ -> ""
            
            // Simple content div without duplicate badge or title
            sprintf "<div class=\"review-timeline-card\">%s%s%s</div>" hiddenItemTypeHtml coverHtml ratingHtml
        
        RenderRss = fun book ->
            // Create RSS item for book
            let url = sprintf "https://www.lqdev.me/reviews/%s" book.FileName
            
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", sprintf "%s by %s" book.Metadata.Title book.Metadata.Author),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" (normalizeUrlsForRss book.Content "https://www.lqdev.me")),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url))
            
            // Add pubDate if date exists
            if not (String.IsNullOrEmpty(book.Metadata.DatePublished)) then
                item.Add(XElement(XName.Get "pubDate", book.Metadata.DatePublished))
            
            // Add source:markdown if available
            match generateSourceMarkdown book.MarkdownSource with
            | Some sourceElement -> item.Add(sourceElement)
            | None -> ()
                
            Some item
    }
    
    /// Phase 5C: Public accessor for review metadata cache (used by UnifiedFeeds.convertBooksToUnified)
    let getReviewMetadata (fileName: string) : ReviewMetadata option =
        match reviewDataCache.TryGetValue(fileName) with
        | (true, data) -> data
        | _ -> None

/// Response content processor
module ResponseProcessor =
    /// Chrome-free card body (B2 / F7): the response-target + response-content divs
    /// WITHOUT the <article> wrapper or the <h2><a>title</a></h2> heading. The timeline
    /// composes this directly — its own card-body/title wrapper makes the standalone
    /// CardHtml chrome redundant (historically regex-stripped by `cleanCardHtml`).
    /// `RenderCard` wraps this with the standalone-card chrome, so CardHtml is unchanged.
    let renderResponseCardBody (response: Response) =
        let targetUrl = Html.escapeHtml response.Metadata.TargetUrl
        Html.element "div" (Html.attribute "class" "response-target")
            (sprintf "→ %s" (Html.element "a" (Html.attribute "href" targetUrl) targetUrl)) +
        Html.element "div" (Html.attribute "class" "response-content") response.Content

    /// B2 (RENDER_V2) clean card-body seam. Like renderResponseCardBody, but renders the
    /// body from the response's MARKDOWN source via ASTParsing.renderCardHtmlFromMarkdown so
    /// the AST card renderer can drop headings the card's own title duplicates (level-1 and
    /// bare-link level-2). renderResponseCardBody embeds already-rendered HTML, where an
    /// embedded <h2><a> sits inside a single HtmlBlock and cannot be removed structurally.
    /// renderCardHtmlFromMarkdown reuses the canonical bare renderer (Media + Review object
    /// renderers, no pipeline.Setup), so the body is byte-identical to response.Content apart
    /// from the intended heading removal.
    let renderResponseCardBodyClean (response: Response) =
        let targetUrl = Html.escapeHtml response.Metadata.TargetUrl
        let bodyHtml =
            match response.MarkdownSource with
            | Some raw -> ASTParsing.renderCardHtmlFromMarkdown (ASTParsing.stripFrontMatter raw)
            | None -> response.Content
        Html.element "div" (Html.attribute "class" "response-target")
            (sprintf "→ %s" (Html.element "a" (Html.attribute "href" targetUrl) targetUrl)) +
        Html.element "div" (Html.attribute "class" "response-content") bodyHtml

    let create() : ContentProcessor<Response> = {
        Parse = fun filePath ->
            match parseResponseFromFile filePath with
            | Ok parsedDoc -> 
                match parsedDoc.Metadata with
                | Some metadata -> 
                    let readingTime = ReadingTimeService.calculateReadingTime parsedDoc.TextContent
                    Ok {
                        FileName = Path.GetFileNameWithoutExtension(filePath)
                        Metadata = { metadata with ReadingTimeMinutes = readingTime }
                        Content = parsedDoc.TextContent
                        MarkdownSource = Some parsedDoc.RawMarkdown
                    }
                | None -> Error (Diagnostics.ContentError.ParseFailure(filePath, "frontmatter", "no front-matter block found (expected a leading '---' fence)"))
            | Error e -> Error (Diagnostics.ofParseError filePath e)
        
        Render = fun response ->
            // Response rendering with IndieWeb microformat support and target URL
            let responseClass = sprintf "h-entry response response-%s" (response.Metadata.ResponseType.ToLower())
            let targetUrlDisplay = 
                div [ _class "response-target mb-3" ] [
                    p [] [
                        span [ _class "bi bi-link-45deg"; _style "margin-right:5px;color:#6c757d;" ] []
                        Text "→ "
                        a [ _class "u-bookmark-of"; _href response.Metadata.TargetUrl; _target "_blank" ] [ 
                            Text response.Metadata.TargetUrl 
                        ]
                    ]
                ]
            let viewNode = article [ _class responseClass ] [ 
                targetUrlDisplay
                div [ _class "e-content" ] [ rawText response.Content ]
            ]
            RenderView.AsString.xmlNode viewNode
        
        OutputPath = fun response ->
            let pathPrefix = 
                match response.Metadata.ResponseType with
                | "bookmark" -> "bookmarks"
                | _ -> "responses"
            sprintf "%s/%s.html" pathPrefix response.FileName
        
        RenderCard = fun response ->
            let title = Html.escapeHtml response.Metadata.Title
            // Use correct path based on response type 
            let urlPath = 
                match response.Metadata.ResponseType with
                | "bookmark" -> "bookmarks"
                | _ -> "responses"
            let url = sprintf "/%s/%s/" urlPath response.FileName
            
            // Include title, target URL, and content
            Html.element "article" (Html.attribute "class" "response-card h-entry")
                (Html.element "h2" "" (Html.element "a" (Html.attribute "href" url) title) +
                 renderResponseCardBody response)
        
        RenderRss = fun response ->
            // Create RSS item for response with correct path based on response type
            let urlPath = 
                match response.Metadata.ResponseType with
                | "bookmark" -> "bookmarks"
                | _ -> "responses"
            let url = sprintf "https://www.lqdev.me/%s/%s" urlPath response.FileName
            let description = sprintf "[%s] %s" response.Metadata.ResponseType response.Content
            
            // Normalize URLs in description for RSS compatibility
            let normalizedDescription = normalizeUrlsForRss description "https://www.lqdev.me"
            
            let categories = 
                if isNull response.Metadata.Tags then []
                else response.Metadata.Tags |> Array.map (fun tag -> XElement(XName.Get "category", tag)) |> Array.toList
            
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", response.Metadata.Title),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" normalizedDescription),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url),
                    XElement(XName.Get "pubDate", response.Metadata.DatePublished))
            
            // Add categories if they exist
            if not (List.isEmpty categories) then
                item.Add(categories |> List.toArray)
            
            // Add source:markdown if available
            match generateSourceMarkdown response.MarkdownSource with
            | Some sourceElement -> item.Add(sourceElement)
            | None -> ()
                
            Some item
    }

/// Media data extracted from album content for efficient rendering
type AlbumMediaData = {
    FirstImageUrl: string option
    FirstImageAlt: string option
    MediaCount: int
}

/// Media data extractor for processing :::media blocks during parsing
module MediaDataExtractor =
    /// Extract media data from raw markdown content
    let extractAlbumMediaData (rawMarkdown: string) : AlbumMediaData =
        try
            // Parse the media block to extract the first media item
            let mediaBlockPattern = System.Text.RegularExpressions.Regex(@":::media\s*\n(.*?)\n:::(?:media)?", System.Text.RegularExpressions.RegexOptions.Singleline)
            let mediaMatches = mediaBlockPattern.Matches(rawMarkdown)
            
            if mediaMatches.Count > 0 then
                let firstMediaContent = mediaMatches.[0].Groups.[1].Value
                
                // Extract first URL and alt text from YAML-like structure
                let urlPattern = System.Text.RegularExpressions.Regex(@"uri:\s*[""']?([^""'\n]+)[""']?")
                let urlMatch = urlPattern.Match(firstMediaContent)
                let altPattern = System.Text.RegularExpressions.Regex(@"alt_text:\s*[""']?([^""'\n]+)[""']?")
                let altMatch = altPattern.Match(firstMediaContent)
                
                let firstImageUrl = if urlMatch.Success then Some (urlMatch.Groups.[1].Value.Trim()) else None
                let firstImageAlt = if altMatch.Success then Some (altMatch.Groups.[1].Value) else None
                
                {
                    FirstImageUrl = firstImageUrl
                    FirstImageAlt = firstImageAlt
                    MediaCount = mediaMatches.Count
                }
            else
                {
                    FirstImageUrl = None
                    FirstImageAlt = None
                    MediaCount = 0
                }
        with
        | _ -> 
            {
                FirstImageUrl = None
                FirstImageAlt = None
                MediaCount = 0
            }

/// Album content processor with :::media block conversion
module AlbumProcessor =
    // Cache for media data extracted during parsing
    let private mediaDataCache = System.Collections.Concurrent.ConcurrentDictionary<string, AlbumMediaData>()
    
    /// Helper to extract markdown content without frontmatter
    let private extractContentWithoutFrontMatter (rawMarkdown: string) : string =
        let lines = rawMarkdown.Split([|'\n'|], StringSplitOptions.None)
        if lines.Length > 0 && lines.[0].Trim() = "---" then
            // Find the closing ---
            let closingIndex = 
                lines 
                |> Array.skip 1
                |> Array.findIndex (fun line -> line.Trim() = "---")
            // Return everything after the second ---
            lines 
            |> Array.skip (closingIndex + 2)
            |> String.concat "\n"
        else
            rawMarkdown
    
    /// Convert AlbumImage to :::media block markdown syntax
    let private convertImageToMediaBlock (image: AlbumImage) : string =
        sprintf ":::media\nmedia_type: image\nuri: %s\nalt_text: %s\ncaption: %s\naspect: \"\"\n:::" 
            image.ImagePath image.AltText image.Description
    
    /// Convert album images to :::media blocks and combine with existing content
    let private convertAlbumToMarkdown (album: Album) (existingContent: string) : string =
        // For new :::media block format, just return the existing content
        // The AST parsing will handle :::media blocks automatically
        existingContent
    
    let create() : ContentProcessor<Album> = {
        Parse = fun filePath ->
            match parseAlbumFromFile filePath with
            | Ok parsedDoc -> 
                match parsedDoc.Metadata with
                | Some metadata -> 
                    let fileName = Path.GetFileNameWithoutExtension(filePath)
                    
                    // Extract media data from raw markdown during parsing
                    let mediaData = MediaDataExtractor.extractAlbumMediaData parsedDoc.RawMarkdown
                    
                    // Store media data in cache for later use in rendering
                    mediaDataCache.[fileName] <- mediaData
                    
                    Ok {
                        FileName = fileName
                        Metadata = metadata
                        Content = extractContentWithoutFrontMatter parsedDoc.RawMarkdown  // Use raw markdown without frontmatter
                        MarkdownSource = Some (extractContentWithoutFrontMatter parsedDoc.RawMarkdown)
                    }
                | None -> Error (Diagnostics.ContentError.ParseFailure(filePath, "frontmatter", "no front-matter block found (expected a leading '---' fence)"))
            | Error e -> Error (Diagnostics.ofParseError filePath e)
        
        Render = fun album ->
            // Return raw markdown content to be processed by Builder.fs through MarkdownService
            // This allows :::media blocks to be converted to proper HTML by custom block processors
            album.Content
        
        OutputPath = fun album ->
            sprintf "media/%s/index.html" album.FileName
        
        RenderCard = fun album ->
            let title = album.Metadata.Title
            let url = sprintf "/media/%s/" album.FileName
            let date = album.Metadata.Date
            
            // Get media data from cache (extracted during parsing)
            let mediaData = 
                match mediaDataCache.TryGetValue(album.FileName) with
                | (true, data) -> data
                | _ -> { FirstImageUrl = None; FirstImageAlt = None; MediaCount = 0 }
            
            // Generate content preview using cached data
            let contentPreview = 
                match mediaData.FirstImageUrl with
                | Some imageUrl ->
                    let altText = mediaData.FirstImageAlt |> Option.defaultValue "Media preview"
                    sprintf """<img src="%s" alt="%s" class="img-fluid" />""" imageUrl altText
                | None -> "Photo album"
            
            let viewNode = 
                article [ _class "album-card h-entry" ] [
                    h2 [] [ a [ _href url ] [ Text title ] ]
                    div [ _class "content-preview" ] [ rawText contentPreview ]
                    div [ _class "mt-2" ] [
                        a [ _href url; _class "btn btn-outline-primary btn-sm" ] [ Text "Read More →" ]
                    ]
                ]
            RenderView.AsString.xmlNode viewNode
        
        RenderRss = fun album ->
            // Create RSS item for album with all images included
            let url = sprintf "https://www.lqdev.me/media/%s" album.FileName
            let imageCount = 
                if isNull album.Metadata.Images then 0
                else Array.length album.Metadata.Images
            let description = sprintf "Album containing %d photos" imageCount
            
            // Normalize URLs in description for RSS compatibility
            let normalizedDescription = normalizeUrlsForRss description "https://www.lqdev.me"
            
            let categories = 
                if isNull album.Metadata.Tags then []
                else album.Metadata.Tags |> Array.map (fun tag -> XElement(XName.Get "category", tag)) |> Array.toList
            
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", album.Metadata.Title),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" normalizedDescription),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url),
                    XElement(XName.Get "pubDate", album.Metadata.Date))
            
            // Add categories if they exist
            if not (List.isEmpty categories) then
                item.Add(categories |> List.toArray)
            
            // Add source:markdown if available
            match generateSourceMarkdown album.MarkdownSource with
            | Some sourceElement -> item.Add(sourceElement)
            | None -> ()
                
            Some item
    }

/// Album Collection content processor - Curated media groupings
module AlbumCollectionProcessor =
    // Cache for media data extracted during parsing
    let private mediaDataCache = System.Collections.Concurrent.ConcurrentDictionary<string, AlbumMediaData>()
    
    /// Helper to extract markdown content without frontmatter
    let private extractContentWithoutFrontMatter (rawMarkdown: string) : string =
        let lines = rawMarkdown.Split([|'\n'|], StringSplitOptions.None)
        if lines.Length > 0 && lines.[0].Trim() = "---" then
            // Find the closing ---
            let closingIndex = 
                lines 
                |> Array.skip 1
                |> Array.findIndex (fun line -> line.Trim() = "---")
            // Return everything after the second ---
            lines 
            |> Array.skip (closingIndex + 2)
            |> String.concat "\n"
        else
            rawMarkdown
    
    let create() : ContentProcessor<AlbumCollection> = {
        Parse = fun filePath ->
            match parseAlbumCollectionFromFile filePath with
            | Ok parsedDoc -> 
                match parsedDoc.Metadata with
                | Some metadata -> 
                    let fileName = Path.GetFileNameWithoutExtension(filePath)
                    
                    // Extract media data from raw markdown during parsing
                    let mediaData = MediaDataExtractor.extractAlbumMediaData parsedDoc.RawMarkdown
                    
                    // Store media data in cache for later use in rendering
                    mediaDataCache.[fileName] <- mediaData
                    
                    Ok {
                        FileName = fileName
                        Metadata = metadata
                        Content = extractContentWithoutFrontMatter parsedDoc.RawMarkdown
                        MarkdownSource = Some (extractContentWithoutFrontMatter parsedDoc.RawMarkdown)
                    }
                | None -> Error (Diagnostics.ContentError.ParseFailure(filePath, "frontmatter", "no front-matter block found (expected a leading '---' fence)"))
            | Error e -> Error (Diagnostics.ofParseError filePath e)
        
        Render = fun albumCollection ->
            // Return raw markdown content to be processed by Builder.fs through MarkdownService
            // This allows :::media blocks to be converted to proper HTML by custom block processors
            albumCollection.Content
        
        OutputPath = fun albumCollection ->
            sprintf "collections/albums/%s/index.html" albumCollection.FileName
        
        RenderCard = fun albumCollection ->
            let title = albumCollection.Metadata.Title
            let description = albumCollection.Metadata.Description
            let url = sprintf "/collections/albums/%s/" albumCollection.FileName
            let date = albumCollection.Metadata.Date
            
            // Get media data from cache (extracted during parsing)
            let mediaData = 
                match mediaDataCache.TryGetValue(albumCollection.FileName) with
                | (true, data) -> data
                | _ -> { FirstImageUrl = None; FirstImageAlt = None; MediaCount = 0 }
            
            // Generate content preview using cached data
            let contentPreview = 
                match mediaData.FirstImageUrl with
                | Some imageUrl ->
                    let altText = mediaData.FirstImageAlt |> Option.defaultValue "Album preview"
                    sprintf """<div class="album-preview"><img src="%s" alt="%s" class="img-fluid" /><p class="mt-2">%s</p></div>""" imageUrl altText description
                | None -> sprintf "<p>%s</p>" description
            
            let viewNode = 
                article [ _class "album-collection-card h-entry" ] [
                    h2 [] [ a [ _href url ] [ Text title ] ]
                    div [ _class "content-preview" ] [ rawText contentPreview ]
                    if mediaData.MediaCount > 0 then
                        p [ _class "text-muted" ] [ Text (sprintf "%d items" mediaData.MediaCount) ]
                    div [ _class "mt-2" ] [
                        a [ _href url; _class "btn btn-outline-primary btn-sm" ] [ Text "View Album →" ]
                    ]
                ]
            RenderView.AsString.xmlNode viewNode
        
        RenderRss = fun albumCollection ->
            // Create RSS item for album collection
            let url = sprintf "https://www.lqdev.me/collections/albums/%s/" albumCollection.FileName
            let description = albumCollection.Metadata.Description
            
            // Get media count from cache
            let mediaData = 
                match mediaDataCache.TryGetValue(albumCollection.FileName) with
                | (true, data) -> data
                | _ -> { FirstImageUrl = None; FirstImageAlt = None; MediaCount = 0 }
            
            let enhancedDescription = 
                if mediaData.MediaCount > 0 then
                    sprintf "%s (%d items)" description mediaData.MediaCount
                else
                    description
            
            // Normalize URLs in description for RSS compatibility
            let normalizedDescription = normalizeUrlsForRss enhancedDescription "https://www.lqdev.me"
            
            // Create RSS category elements for tags
            let categories = 
                albumCollection.Metadata.Tags
                |> Array.map (fun tag -> XElement(XName.Get "category", tag))
                |> Array.toList
            
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", albumCollection.Metadata.Title),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" normalizedDescription),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url),
                    XElement(XName.Get "pubDate", albumCollection.Metadata.Date))
            
            // Add categories if they exist
            if not (List.isEmpty categories) then
                item.Add(categories |> List.toArray)
            
            // Add source:markdown if available
            match generateSourceMarkdown albumCollection.MarkdownSource with
            | Some sourceElement -> item.Add(sourceElement)
            | None -> ()
                
            Some item
    }

/// Playlist Collection content processor for curated music playlists
module PlaylistCollectionProcessor =
    /// Helper to extract markdown content without frontmatter
    let private extractContentWithoutFrontMatter (rawMarkdown: string) : string =
        let lines = rawMarkdown.Split([|'\n'|], StringSplitOptions.None)
        if lines.Length > 0 && lines.[0].Trim() = "---" then
            // Find the closing ---
            let closingIndex = 
                lines 
                |> Array.skip 1
                |> Array.findIndex (fun line -> line.Trim() = "---")
            // Return everything after the second ---
            lines 
            |> Array.skip (closingIndex + 2)
            |> String.concat "\n"
        else
            rawMarkdown
    
    let create() : ContentProcessor<PlaylistCollection> = {
        Parse = fun filePath ->
            match parsePlaylistCollectionFromFile filePath with
            | Ok parsedDoc -> 
                match parsedDoc.Metadata with
                | Some metadata -> 
                    let fileName = Path.GetFileNameWithoutExtension(filePath)
                    
                    Ok {
                        FileName = fileName
                        Metadata = metadata
                        Content = extractContentWithoutFrontMatter parsedDoc.RawMarkdown
                        MarkdownSource = Some (extractContentWithoutFrontMatter parsedDoc.RawMarkdown)
                    }
                | None -> Error (Diagnostics.ContentError.ParseFailure(filePath, "frontmatter", "no front-matter block found (expected a leading '---' fence)"))
            | Error e -> Error (Diagnostics.ofParseError filePath e)
        
        Render = fun playlistCollection ->
            // Return raw markdown content to be processed by Builder.fs through MarkdownService
            playlistCollection.Content
        
        OutputPath = fun playlistCollection ->
            sprintf "collections/playlists/%s/index.html" playlistCollection.FileName
        
        RenderCard = fun playlistCollection ->
            let title = playlistCollection.Metadata.Title
            let description = playlistCollection.Metadata.Description |> Option.defaultValue ""
            let url = sprintf "/collections/playlists/%s/" playlistCollection.FileName
            let date = playlistCollection.Metadata.Date
            
            let viewNode = 
                article [ _class "playlist-collection-card h-entry" ] [
                    h2 [] [ a [ _href url ] [ Text title ] ]
                    if not (String.IsNullOrEmpty(description)) then
                        p [ _class "content-preview" ] [ Text description ]
                    div [ _class "mt-2" ] [
                        a [ _href url; _class "btn btn-outline-primary btn-sm" ] [ Text "View Playlist →" ]
                    ]
                ]
            RenderView.AsString.xmlNode viewNode
        
        RenderRss = fun playlistCollection ->
            // Create RSS item for playlist collection
            let url = sprintf "https://www.lqdev.me/collections/playlists/%s/" playlistCollection.FileName
            let description = playlistCollection.Metadata.Description |> Option.defaultValue ""
            
            // Normalize URLs in description for RSS compatibility
            let normalizedDescription = normalizeUrlsForRss description "https://www.lqdev.me"
            
            // Create RSS category elements for tags
            let categories = 
                playlistCollection.Metadata.Tags
                |> Array.map (fun tag -> XElement(XName.Get "category", tag))
                |> Array.toList
            
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", playlistCollection.Metadata.Title),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" normalizedDescription),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url),
                    XElement(XName.Get "pubDate", playlistCollection.Metadata.Date))
            
            // Add categories if they exist
            if not (List.isEmpty categories) then
                item.Add(categories |> List.toArray)
            
            // Add source:markdown if available
            match generateSourceMarkdown playlistCollection.MarkdownSource with
            | Some sourceElement -> item.Add(sourceElement)
            | None -> ()
                
            Some item
    }

/// Bookmark content processor for IndieWeb bookmarks
module BookmarkProcessor =
    let create() : ContentProcessor<Bookmark> = {
        Parse = fun filePath ->
            match parseBookmarkFromFile filePath with
            | Ok parsedDoc -> 
                match parsedDoc.Metadata with
                | Some metadata -> 
                    Ok {
                        FileName = Path.GetFileNameWithoutExtension(filePath)
                        Metadata = metadata
                        Content = parsedDoc.TextContent
                        MarkdownSource = Some parsedDoc.RawMarkdown
                    }
                | None -> Error (Diagnostics.ContentError.ParseFailure(filePath, "frontmatter", "no front-matter block found (expected a leading '---' fence)"))
            | Error e -> Error (Diagnostics.ofParseError filePath e)
        
        Render = fun bookmark ->
            // Bookmark rendering with IndieWeb microformat support
            let viewNode = article [ _class "h-entry bookmark" ] [
                a [ _class "u-bookmark-of"; _href bookmark.Metadata.BookmarkOf ] [ str bookmark.Metadata.Title ]
                rawText bookmark.Content
            ]
            RenderView.AsString.xmlNode viewNode
        
        OutputPath = fun bookmark ->
            sprintf "bookmarks/%s.html" bookmark.FileName
        
        RenderCard = fun bookmark ->
            let title = Html.escapeHtml bookmark.Metadata.Title
            let bookmarkOf = Html.escapeHtml bookmark.Metadata.BookmarkOf
            let description = Html.escapeHtml bookmark.Metadata.Description
            let url = sprintf "/bookmarks/%s/" bookmark.FileName
            let date = bookmark.Metadata.DatePublished
            
            // IndieWeb microformat card for bookmarks
            Html.element "article" (Html.attribute "class" "bookmark-card h-entry")
                (Html.element "h2" "" (Html.element "a" (Html.attribute "href" url) title) +
                 Html.element "div" (Html.attribute "class" "bookmark-target") 
                    (sprintf "🔖 %s" (Html.element "a" (Html.attribute "href" bookmarkOf + Html.attribute "class" "u-bookmark-of") bookmarkOf)) +
                 Html.element "div" (Html.attribute "class" "bookmark-description") description)
        
        RenderRss = fun bookmark ->
            // Create RSS item for bookmark
            let url = sprintf "https://www.lqdev.me/bookmarks/%s" bookmark.FileName
            let description = sprintf "Bookmark: %s - %s" bookmark.Metadata.Description bookmark.Metadata.BookmarkOf
            
            // Normalize URLs in description for RSS compatibility
            let normalizedDescription = normalizeUrlsForRss description "https://www.lqdev.me"
            
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", bookmark.Metadata.Title),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" normalizedDescription),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url),
                    XElement(XName.Get "pubDate", bookmark.Metadata.DatePublished))
            
            // Add source:markdown if available
            match generateSourceMarkdown bookmark.MarkdownSource with
            | Some sourceElement -> item.Add(sourceElement)
            | None -> ()
            
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
