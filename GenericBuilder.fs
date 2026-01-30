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
                    Some {
                        FileName = Path.GetFileNameWithoutExtension(filePath)
                        Metadata = { metadata with ReadingTimeMinutes = readingTime }
                        Content = contentWithoutFrontmatter  // Use raw markdown without frontmatter
                        MarkdownSource = Some contentWithoutFrontmatter
                    }
                | None -> None
            | Error _ -> None
        
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
                    Some {
                        FileName = Path.GetFileNameWithoutExtension(filePath)
                        Metadata = { metadata with ReadingTimeMinutes = readingTime }
                        Content = parsedDoc.TextContent  // Raw markdown content
                        MarkdownSource = Some parsedDoc.RawMarkdown
                    }
                | None -> None
            | Error _ -> None
        
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
                    Some {
                        FileName = Path.GetFileNameWithoutExtension(filePath)
                        Metadata = metadata
                        Content = parsedDoc.TextContent
                        MarkdownSource = Some parsedDoc.RawMarkdown
                    }
                | None -> None
            | Error _ -> None
        
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
                    Some {
                        FileName = Path.GetFileNameWithoutExtension(filePath)
                        Metadata = metadata
                        Content = parsedDoc.TextContent
                        MarkdownSource = Some parsedDoc.RawMarkdown
                    }
                | None -> None
            | Error _ -> None
        
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
                    
                    Some {
                        FileName = System.IO.Path.GetFileNameWithoutExtension(filePath)
                        Metadata = metadata
                        Content = markdownContent  // Store raw markdown for reveal.js
                        MarkdownSource = Some markdownContent
                    }
                | None -> None
            | Error _ -> None
        
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

/// Book content processor
module BookProcessor =
    // Cache for review data extracted during parsing - now includes item type
    let private reviewDataCache = System.Collections.Concurrent.ConcurrentDictionary<string, (string option * float * float * string option)>()
    
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
                    
                    // Store review data in cache for later use in rendering
                    match reviewDataOpt with
                    | Some reviewData ->
                        let reviewImageUrl = reviewData.ImageUrl
                        let reviewRating = reviewData.Rating
                        let reviewScale = reviewData.Scale
                        let reviewItemType = Some reviewData.ItemType
                        reviewDataCache.[fileName] <- (reviewImageUrl, reviewRating, reviewScale, reviewItemType)
                    | None ->
                        // Use old extraction method for backward compatibility
                        let (reviewImageUrl, reviewRating, reviewScale, reviewItemType) = ReviewDataExtractor.extractReviewData parsedDoc.RawMarkdown
                        reviewDataCache.[fileName] <- (reviewImageUrl, reviewRating, reviewScale, reviewItemType)
                    
                    Some {
                        FileName = fileName
                        Metadata = finalMetadata
                        Content = parsedDoc.TextContent
                        MarkdownSource = Some parsedDoc.RawMarkdown
                    }
                | None -> None
            | Error _ -> None
        
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
            let (reviewImageUrlOpt, reviewRating, reviewScale, reviewItemType) = 
                match reviewDataCache.TryGetValue(book.FileName) with
                | (true, data) -> data
                | _ -> (None, book.Metadata.Rating, 5.0, None)
            
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
            
            // Display rating with scale if available
            let ratingHtml = 
                if ratingValue > 0.0 then
                    sprintf "<div class=\"rating\">Rating: %.1f/%.1f</div>" ratingValue ratingScaleValue
                else ""
            
            // Create item type badge for timeline if available
            let itemTypeBadgeHtml = 
                match reviewItemType with
                | Some itemType when not (String.IsNullOrWhiteSpace(itemType)) ->
                    let capitalizedType = itemType.ToUpper()
                    sprintf "<span class=\"item-type-badge badge bg-secondary\">%s</span>" capitalizedType
                | _ -> ""
            
            // Create simplified timeline card with only: image, rating, item type badge (no duplicate title, no status, no author)
            let coverHtml = 
                if not (String.IsNullOrEmpty(imageUrl)) then
                    sprintf "<img src=\"%s\" alt=\"%s cover\" class=\"review-image img-fluid\">" 
                        (Html.escapeHtml imageUrl) (Html.escapeHtml book.Metadata.Title)
                else ""
            
            // Simple content div without duplicate title links or extra metadata
            sprintf "<div class=\"review-timeline-card\">%s%s%s</div>" itemTypeBadgeHtml coverHtml ratingHtml
        
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

/// Response content processor
module ResponseProcessor =
    let create() : ContentProcessor<Response> = {
        Parse = fun filePath ->
            match parseResponseFromFile filePath with
            | Ok parsedDoc -> 
                match parsedDoc.Metadata with
                | Some metadata -> 
                    let readingTime = ReadingTimeService.calculateReadingTime parsedDoc.TextContent
                    Some {
                        FileName = Path.GetFileNameWithoutExtension(filePath)
                        Metadata = { metadata with ReadingTimeMinutes = readingTime }
                        Content = parsedDoc.TextContent
                        MarkdownSource = Some parsedDoc.RawMarkdown
                    }
                | None -> None
            | Error _ -> None
        
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
            let targetUrl = Html.escapeHtml response.Metadata.TargetUrl
            // Use correct path based on response type 
            let urlPath = 
                match response.Metadata.ResponseType with
                | "bookmark" -> "bookmarks"
                | _ -> "responses"
            let url = sprintf "/%s/%s/" urlPath response.FileName
            
            // Include title, target URL, and content
            Html.element "article" (Html.attribute "class" "response-card h-entry")
                (Html.element "h2" "" (Html.element "a" (Html.attribute "href" url) title) +
                 Html.element "div" (Html.attribute "class" "response-target") 
                    (sprintf "→ %s" (Html.element "a" (Html.attribute "href" targetUrl) targetUrl)) +
                 Html.element "div" (Html.attribute "class" "response-content") response.Content)
        
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
                    
                    Some {
                        FileName = fileName
                        Metadata = metadata
                        Content = extractContentWithoutFrontMatter parsedDoc.RawMarkdown  // Use raw markdown without frontmatter
                        MarkdownSource = Some (extractContentWithoutFrontMatter parsedDoc.RawMarkdown)
                    }
                | None -> None
            | Error _ -> None
        
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
                    
                    Some {
                        FileName = fileName
                        Metadata = metadata
                        Content = extractContentWithoutFrontMatter parsedDoc.RawMarkdown
                        MarkdownSource = Some (extractContentWithoutFrontMatter parsedDoc.RawMarkdown)
                    }
                | None -> None
            | Error _ -> None
        
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
                    
                    Some {
                        FileName = fileName
                        Metadata = metadata
                        Content = extractContentWithoutFrontMatter parsedDoc.RawMarkdown
                        MarkdownSource = Some (extractContentWithoutFrontMatter parsedDoc.RawMarkdown)
                    }
                | None -> None
            | Error _ -> None
        
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
                    Some {
                        FileName = Path.GetFileNameWithoutExtension(filePath)
                        Metadata = metadata
                        Content = parsedDoc.TextContent
                        MarkdownSource = Some parsedDoc.RawMarkdown
                    }
                | None -> None
            | Error _ -> None
        
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
        // Phase 5A: Response semantics for ActivityPub
        ResponseType: string option  // "star", "reply", "reshare", "bookmark"
        TargetUrl: string option     // URL being responded to
        UpdatedDate: string option   // For edit tracking
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
        
        // Generate fire-hose feed
        let fireHoseFeed = generateRssFeed (allUnifiedItems |> List.take (min 20 allUnifiedItems.Length)) fireHoseConfig
        let fireHoseDir = Path.Combine(outputDirectory, "feed")
        Directory.CreateDirectory(fireHoseDir) |> ignore
        File.WriteAllText(Path.Combine(fireHoseDir, "feed.xml"), fireHoseFeed)
        
        // Also create backward compatibility copy at old location
        File.WriteAllText(Path.Combine(fireHoseDir, "index.xml"), fireHoseFeed)
        
        // Type-specific feed configurations
        let typeConfigurations = [
            ("posts", {
                Title = "Luis Quintanilla - Posts"
                Link = "https://www.lqdev.me/posts"
                Description = "Blog posts by Luis Quintanilla"
                OutputPath = "posts/feed.xml"
                ContentType = Some "posts"
            })
            ("notes", {
                Title = "Luis Quintanilla - Notes"
                Link = "https://www.lqdev.me/notes"
                Description = "Notes and micro-posts by Luis Quintanilla"
                OutputPath = "notes/feed.xml"
                ContentType = Some "notes"
            })
            ("responses", {
                Title = "Luis Quintanilla - Responses"
                Link = "https://www.lqdev.me/responses"
                Description = "IndieWeb responses by Luis Quintanilla"
                OutputPath = "responses/feed.xml"
                ContentType = Some "responses"
            })
            ("bookmarks", {
                Title = "Luis Quintanilla - Bookmarks"
                Link = "https://www.lqdev.me/bookmarks"
                Description = "IndieWeb bookmarks by Luis Quintanilla"
                OutputPath = "bookmarks/feed.xml"
                ContentType = Some "bookmarks"
            })
            ("snippets", {
                Title = "Luis Quintanilla - Snippets"
                Link = "https://www.lqdev.me/resources/snippets"
                Description = "Code snippets by Luis Quintanilla"
                OutputPath = "resources/snippets/feed.xml"
                ContentType = Some "snippets"
            })
            ("wiki", {
                Title = "Luis Quintanilla - Wiki"
                Link = "https://www.lqdev.me/resources/wiki"
                Description = "Wiki articles by Luis Quintanilla"
                OutputPath = "resources/wiki/feed.xml"
                ContentType = Some "wiki"
            })
            ("presentations", {
                Title = "Luis Quintanilla - Presentations"
                Link = "https://www.lqdev.me/resources/presentations"
                Description = "Presentations by Luis Quintanilla"
                OutputPath = "resources/presentations/feed.xml"
                ContentType = Some "presentations"
            })
            ("reviews", {
                Title = "Luis Quintanilla - Reviews"
                Link = "https://www.lqdev.me/reviews"
                Description = "Book reviews by Luis Quintanilla"
                OutputPath = "reviews/feed.xml"
                ContentType = Some "reviews"
            })
            ("media", {
                Title = "Luis Quintanilla - Media"
                Link = "https://www.lqdev.me/media"
                Description = "Photo albums and media by Luis Quintanilla"
                OutputPath = "media/feed.xml"
                ContentType = Some "media"
            })
            ("album-collection", {
                Title = "Luis Quintanilla - Albums"
                Link = "https://www.lqdev.me/collections/albums"
                Description = "Photo album collections by Luis Quintanilla"
                OutputPath = "collections/albums/feed.xml"
                ContentType = Some "album-collection"
            })
            ("playlist-collection", {
                Title = "Luis Quintanilla - Playlists"
                Link = "https://www.lqdev.me/collections/playlists"
                Description = "Music playlist collections by Luis Quintanilla"
                OutputPath = "collections/playlists/feed.xml"
                ContentType = Some "playlist-collection"
            })
        ]
        
        // Generate type-specific feeds
        typeConfigurations
        |> List.iter (fun (contentType, config) ->
            let typeItems = 
                allUnifiedItems 
                |> List.filter (fun item -> 
                    if contentType = "responses" then
                        // For responses feed, include all response subtypes
                        ["star"; "reply"; "reshare"; "responses"] |> List.contains item.ContentType
                    else
                        item.ContentType = contentType)
                |> List.take (min 20 (allUnifiedItems |> List.filter (fun item -> 
                    if contentType = "responses" then
                        ["star"; "reply"; "reshare"; "responses"] |> List.contains item.ContentType
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
        // Flatten all feed items
        let allUnifiedItems = 
            feedDataSets
            |> List.collect snd
            |> List.sortByDescending (fun item -> DateTimeOffset.Parse(item.Date))
        
        // Extract all unique tags
        let allTags = 
            allUnifiedItems
            |> List.collect (fun item -> 
                if isNull item.Tags then [] 
                else item.Tags |> Array.toList)
            |> List.distinct
            |> List.sort
        
        printfn "Generating RSS feeds for %d tags..." allTags.Length
        
        // Generate RSS feed for each tag
        allTags
        |> List.iter (fun tag ->
            let tagItems = 
                allUnifiedItems
                |> List.filter (fun item -> 
                    if isNull item.Tags then false 
                    else item.Tags |> Array.contains tag)
                |> List.take (min 20 (allUnifiedItems |> List.filter (fun item -> 
                    if isNull item.Tags then false 
                    else item.Tags |> Array.contains tag) |> List.length))
            
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
    let convertPostsToUnified (feedDataList: FeedData<Post> list) : UnifiedFeedItem list =
        feedDataList |> List.choose (fun feedData ->
            match feedData.RssXml with
            | Some rssXml ->
                // Use full content instead of CardHtml for complete content display
                let title = feedData.Content.Metadata.Title
                let url = match rssXml.Element(XName.Get "link") with | null -> "" | e -> e.Value
                let content = feedData.Content.Content  // Full raw content - will be processed by timeline view
                let date = feedData.Content.Metadata.Date
                let tags = if isNull feedData.Content.Metadata.Tags then [||] else feedData.Content.Metadata.Tags
                Some { Title = title; Content = content; Url = url; Date = date; ContentType = "posts"; Tags = tags; RssXml = rssXml; ResponseType = None; TargetUrl = None; UpdatedDate = None }
            | None -> None)
    
    let convertNotesToUnified (feedDataList: FeedData<Post> list) : UnifiedFeedItem list =
        feedDataList |> List.choose (fun feedData ->
            match feedData.RssXml with
            | Some rssXml ->
                // Use full content instead of CardHtml for complete content display
                let title = feedData.Content.Metadata.Title
                let url = match rssXml.Element(XName.Get "link") with | null -> "" | e -> e.Value
                let content = feedData.Content.Content  // Full raw content - will be processed by timeline view
                let date = feedData.Content.Metadata.Date
                let tags = if isNull feedData.Content.Metadata.Tags then [||] else feedData.Content.Metadata.Tags
                Some { Title = title; Content = content; Url = url; Date = date; ContentType = "notes"; Tags = tags; RssXml = rssXml; ResponseType = None; TargetUrl = None; UpdatedDate = None }
            | None -> None)
    
    let convertResponsesToUnified (feedDataList: FeedData<Response> list) : UnifiedFeedItem list =
        feedDataList 
        |> List.filter (fun feedData -> feedData.Content.Metadata.ResponseType <> "bookmark") // Exclude bookmarks
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
                Some { Title = title; Content = content; Url = url; Date = date; ContentType = contentType; Tags = tags; RssXml = rssXml; ResponseType = responseType; TargetUrl = targetUrl; UpdatedDate = updatedDate }
            | None -> None)
    
    let convertResponseBookmarksToUnified (feedDataList: FeedData<Response> list) : UnifiedFeedItem list =
        feedDataList 
        |> List.filter (fun feedData -> feedData.Content.Metadata.ResponseType = "bookmark") // Only bookmarks
        |> List.choose (fun feedData ->
            match feedData.RssXml with
            | Some rssXml ->
                let title = feedData.Content.Metadata.Title
                let url = match rssXml.Element(XName.Get "link") with | null -> "" | e -> e.Value
                let content = feedData.CardHtml  // Use CardHtml to include target URL display
                let date = feedData.Content.Metadata.DatePublished
                let tags = if isNull feedData.Content.Metadata.Tags then [||] else feedData.Content.Metadata.Tags
                // Phase 5A: Extract response semantics for ActivityPub
                let responseType = Some "bookmark"
                let targetUrl = Some feedData.Content.Metadata.TargetUrl
                let updatedDate = if String.IsNullOrWhiteSpace(feedData.Content.Metadata.DateUpdated) then None else Some feedData.Content.Metadata.DateUpdated
                Some { Title = title; Content = content; Url = url; Date = date; ContentType = "bookmarks"; Tags = tags; RssXml = rssXml; ResponseType = responseType; TargetUrl = targetUrl; UpdatedDate = updatedDate }
            | None -> None)
    
    let convertSnippetsToUnified (feedDataList: FeedData<Snippet> list) : UnifiedFeedItem list =
        feedDataList |> List.choose (fun feedData ->
            match feedData.RssXml with
            | Some rssXml ->
                let title = feedData.Content.Metadata.Title
                let url = match rssXml.Element(XName.Get "link") with | null -> "" | e -> e.Value
                let content = feedData.Content.Content  // Use full content instead of CardHtml
                let date = feedData.Content.Metadata.CreatedDate
                let tags = 
                    if String.IsNullOrEmpty(feedData.Content.Metadata.Tags) then [||]
                    else feedData.Content.Metadata.Tags.Split(',') |> Array.map (fun s -> s.Trim())
                Some { Title = title; Content = content; Url = url; Date = date; ContentType = "snippets"; Tags = tags; RssXml = rssXml; ResponseType = None; TargetUrl = None; UpdatedDate = None }
            | None -> None)
    
    let convertWikisToUnified (feedDataList: FeedData<Wiki> list) : UnifiedFeedItem list =
        feedDataList |> List.choose (fun feedData ->
            match feedData.RssXml with
            | Some rssXml ->
                let title = feedData.Content.Metadata.Title
                let url = match rssXml.Element(XName.Get "link") with | null -> "" | e -> e.Value
                let content = feedData.Content.Content  // Use full content instead of CardHtml
                let date = feedData.Content.Metadata.LastUpdatedDate
                let tags = 
                    if String.IsNullOrEmpty(feedData.Content.Metadata.Tags) then [||]
                    else feedData.Content.Metadata.Tags.Split(',') |> Array.map (fun s -> s.Trim())
                Some { Title = title; Content = content; Url = url; Date = date; ContentType = "wiki"; Tags = tags; RssXml = rssXml; ResponseType = None; TargetUrl = None; UpdatedDate = None }
            | None -> None)
    
    let convertPresentationsToUnified (feedDataList: FeedData<Presentation> list) : UnifiedFeedItem list =
        feedDataList |> List.choose (fun feedData ->
            match feedData.RssXml with
            | Some rssXml ->
                let title = feedData.Content.Metadata.Title
                let url = match rssXml.Element(XName.Get "link") with | null -> "" | e -> e.Value
                let content = feedData.Content.Content  // Use full content instead of CardHtml
                let date = feedData.Content.Metadata.Date
                let tags = 
                    if String.IsNullOrEmpty(feedData.Content.Metadata.Tags) then [||]
                    else feedData.Content.Metadata.Tags.Split(',') |> Array.map (fun s -> s.Trim())
                Some { Title = title; Content = content; Url = url; Date = date; ContentType = "presentations"; Tags = tags; RssXml = rssXml; ResponseType = None; TargetUrl = None; UpdatedDate = None }
            | None -> None)
    
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
                Some { Title = title; Content = content; Url = url; Date = date; ContentType = "reviews"; Tags = tags; RssXml = rssXml; ResponseType = None; TargetUrl = None; UpdatedDate = None }
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
                Some { Title = title; Content = content; Url = url; Date = date; ContentType = "media"; Tags = tags; RssXml = rssXml; ResponseType = None; TargetUrl = None; UpdatedDate = None }
            | None -> None)
    
    let convertAlbumCollectionsToUnified (feedDataList: FeedData<AlbumCollection> list) : UnifiedFeedItem list =
        feedDataList |> List.choose (fun feedData ->
            match feedData.RssXml with
            | Some rssXml ->
                let title = feedData.Content.Metadata.Title
                let url = match rssXml.Element(XName.Get "link") with | null -> "" | e -> e.Value
                let content = feedData.Content.Content  // Use full content
                let date = feedData.Content.Metadata.Date
                let tags = if isNull feedData.Content.Metadata.Tags then [||] else feedData.Content.Metadata.Tags
                Some { Title = title; Content = content; Url = url; Date = date; ContentType = "album-collection"; Tags = tags; RssXml = rssXml; ResponseType = None; TargetUrl = None; UpdatedDate = None }
            | None -> None)
    
    let convertPlaylistCollectionsToUnified (feedDataList: FeedData<PlaylistCollection> list) : UnifiedFeedItem list =
        feedDataList |> List.choose (fun feedData ->
            match feedData.RssXml with
            | Some rssXml ->
                let title = feedData.Content.Metadata.Title
                let url = match rssXml.Element(XName.Get "link") with | null -> "" | e -> e.Value
                let content = feedData.Content.Content  // Use full content
                let date = feedData.Content.Metadata.Date
                let tags = if isNull feedData.Content.Metadata.Tags then [||] else feedData.Content.Metadata.Tags
                Some { Title = title; Content = content; Url = url; Date = date; ContentType = "playlist-collection"; Tags = tags; RssXml = rssXml; ResponseType = None; TargetUrl = None; UpdatedDate = None }
            | None -> None)
    
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
                Some { Title = title; Content = content; Url = url; Date = date; ContentType = "bookmarks"; Tags = tags; RssXml = rssXml; ResponseType = None; TargetUrl = targetUrl; UpdatedDate = None }
            | None -> None)

    // Convert bookmark responses (Response objects with bookmark type) to unified feed
    let convertBookmarkResponsesToUnified (feedDataList: FeedData<Response> list) : UnifiedFeedItem list =
        feedDataList 
        |> List.filter (fun feedData -> feedData.Content.Metadata.ResponseType = "bookmark") // Include only bookmarks
        |> List.choose (fun feedData ->
            match feedData.RssXml with
            | Some rssXml ->
                let title = feedData.Content.Metadata.Title
                let url = match rssXml.Element(XName.Get "link") with | null -> "" | e -> e.Value
                let content = feedData.CardHtml  // Use CardHtml to include target URL display
                let date = feedData.Content.Metadata.DatePublished
                let tags = if isNull feedData.Content.Metadata.Tags then [||] else feedData.Content.Metadata.Tags
                Some { Title = title; Content = content; Url = url; Date = date; ContentType = "bookmarks"; Tags = tags; RssXml = rssXml; ResponseType = Some "bookmark"; TargetUrl = Some feedData.Content.Metadata.TargetUrl; UpdatedDate = None }
            | None -> None)
