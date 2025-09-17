module LayoutViews

open Giraffe.ViewEngine
open System
open System.IO
open Domain
open ComponentViews
open CollectionViews
open MarkdownService

/// Sanitize tag names for URL usage while preserving display text
let private sanitizeTagForUrl (tag: string) =
    tag.Replace("#", "sharp").Replace("/", "-").Replace(" ", "-").Replace("\"", "")

// New stratified timeline homepage view - takes 5 items from each content type initially
// Progressive loading is content-type aware for better filtering experience
let timelineHomeViewStratified (initialItems: GenericBuilder.UnifiedFeeds.UnifiedFeedItem array) (remainingItemsByType: (string * GenericBuilder.UnifiedFeeds.UnifiedFeedItem list) list) =
    
    div [ _class "h-feed unified-timeline" ] [
        // Header with personal intro and content filters
        header [ _class "timeline-header text-center p-4" ] [
            div [ _class "avatar-section mb-3" ] [
                img [ _src "/avatar.png"; _alt "Luis Quintanilla Avatar Image"; _class "rounded-circle"; _height "150"; _width "150" ]
                div [ _class "mt-2" ] [
                    h1 [ _class "p-name" ] [
                        str "Hi, I'm "
                        a [ _href "/about"; _class "author-link" ] [ Text "Luis" ]
                        span [] [ Text " &#x1F44B;" ]
                    ]
                    p [ _class "tagline" ] [ Text "Latest updates from across the site" ]
                ]
            ]
            
            // Content type filters (will be styled as buttons in CSS)
            div [ _class "content-filters mt-3"; _id "contentFilters" ] [
                button [ _class "filter-btn active"; attr "data-filter" "all"; _type "button" ] [ Text "All" ]
                button [ _class "filter-btn"; attr "data-filter" "posts"; _type "button" ] [ Text "Blog Posts" ]
                button [ _class "filter-btn"; attr "data-filter" "notes"; _type "button" ] [ Text "Notes" ]
                button [ _class "filter-btn"; attr "data-filter" "responses"; _type "button" ] [ Text "Responses" ]
                button [ _class "filter-btn"; attr "data-filter" "reviews"; _type "button" ] [ Text "Reviews" ]
                button [ _class "filter-btn"; attr "data-filter" "bookmarks"; _type "button" ] [ Text "Bookmarks" ]
                button [ _class "filter-btn"; attr "data-filter" "media"; _type "button" ] [ Text "Media" ]
            ]
        ]
        
        // Timeline content area with stratified loading
        main [ _class "timeline-content" ] [
            // Initial stratified content (5 items from each content type)
            div [ _class "initial-content"; _id "initialContent" ] [
                for item in initialItems do
                    let fileName = Path.GetFileNameWithoutExtension(item.Url)
                    let getProperPermalink (contentType: string) (fileName: string) =
                        match contentType with
                        | "posts" -> $"/posts/{fileName}/"
                        | "notes" -> $"/notes/{fileName}/"
                        | "responses" -> $"/responses/{fileName}/"
                        | "bookmarks" -> $"/bookmarks/{fileName}/"  // Bookmarks are responses but filtered separately
                        | "snippets" -> $"/resources/snippets/{fileName}/"
                        | "wiki" -> $"/resources/wiki/{fileName}/"
                        | "presentations" -> $"/resources/presentations/{fileName}/"
                        | "reviews" -> $"/reviews/{fileName}/"
                        | "streams" -> $"/streams/{fileName}/"
                        | "media" -> $"/media/{fileName}/"
                        // Specific response types also route to /responses/
                        | "star" | "reply" | "reshare" -> $"/responses/{fileName}/"
                        | "bookmark" -> $"/bookmarks/{fileName}/"
                        | _ -> $"/{contentType}/{fileName}/"
                    
                    let properPermalink = getProperPermalink item.ContentType fileName
                    
                    // Content card with desert theme and filtering attributes
                    article [ 
                        _class "h-entry content-card"
                        attr "data-type" item.ContentType
                        attr "data-date" item.Date
                    ] [
                        header [ _class "card-header" ] [
                            time [ _class "dt-published publication-date"; attr "datetime" item.Date ] [
                                Text (DateTimeOffset.Parse(item.Date).ToString("MMM dd, yyyy"))
                            ]
                            div [ _class "content-type-info" ] [
                                span [ _class "content-type-badge"; attr "data-type" item.ContentType ] [
                                    Text (match item.ContentType with
                                          | "posts" -> "Blog Post"
                                          | "notes" -> "Note"
                                          | "responses" -> "Response"
                                          | "bookmarks" -> "Bookmark"
                                          | "reviews" -> "Review"
                                          | "streams" -> "Stream Recording"
                                          | "media" -> "Media"
                                          // Specific response types
                                          | "star" -> "Star"
                                          | "reply" -> "Reply"
                                          | "reshare" -> "Reshare"
                                          | "bookmark" -> "Bookmark"
                                          | _ -> item.ContentType)
                                ]
                            ]
                        ]
                        
                        div [ _class "card-body" ] [
                            h2 [ _class "p-name card-title" ] [
                                a [ _class "u-url title-link"; _href properPermalink ] [ Text item.Title ]
                            ]
                            div [ _class "e-content card-content" ] [
                                // Convert markdown content to HTML and clean it
                                let cleanedContent = 
                                    let content = convertMdToHtml item.Content  // Convert markdown to HTML first
                                    // Remove all article opening tags with any class
                                    let removeArticleStart = System.Text.RegularExpressions.Regex.Replace(content, @"<article[^>]*>", "")
                                    // Remove all article closing tags
                                    let removeArticleEnd = removeArticleStart.Replace("</article>", "")
                                    // Remove duplicate h1/h2 titles (common source of duplication)
                                    let removeTitles = System.Text.RegularExpressions.Regex.Replace(removeArticleEnd, @"<h1[^>]*>.*?</h1>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                                    // Additional cleaning to prevent HTML parsing issues
                                    let safeCleaned = removeTitles.Replace("<script", "&lt;script").Replace("</script>", "&lt;/script&gt;")
                                    safeCleaned
                                rawText cleanedContent
                            ]
                        ]
                        
                        footer [ _class "card-footer" ] [
                            div [ _class "card-meta" ] [
                                if item.Tags.Length > 0 then
                                    div [ _class "p-category tags" ] [
                                        for tag in item.Tags do
                                            a [ _class "tag-link"; _href $"/tags/{sanitizeTagForUrl tag}/" ] [ Text $"#{tag}" ]
                                    ]
                            ]
                        ]
                    ]
            ]
            
            // Progressive loading container for additional content chunks per type
            div [ 
                _class "progressive-content"
                _id "progressiveContent"
            ] []
            
            // Type-aware progressive loading data and controls
            if not remainingItemsByType.IsEmpty then
                // Generate JSON data for each content type separately
                for (contentType, items) in remainingItemsByType do
                    if not items.IsEmpty then
                        // Proper JSON escaping function
                        let escapeJson (text: string) =
                            text.Replace("\\", "\\\\")
                                .Replace("\"", "\\\"")
                                .Replace("\b", "\\b")
                                .Replace("\f", "\\f")
                                .Replace("\n", "\\n")
                                .Replace("\r", "\\r")
                                .Replace("\t", "\\t")
                        
                        let contentTypeItemsJson = 
                            items 
                            |> List.map (fun item ->
                                let fileName = Path.GetFileNameWithoutExtension(item.Url)
                                let getProperPermalink (contentType: string) (fileName: string) =
                                    match contentType with
                                    | "posts" -> $"/posts/{fileName}/"
                                    | "notes" -> $"/notes/{fileName}/"
                                    | "responses" -> $"/responses/{fileName}/"
                                    | "bookmarks" -> $"/bookmarks/{fileName}/"
                                    | "snippets" -> $"/resources/snippets/{fileName}/"
                                    | "wiki" -> $"/resources/wiki/{fileName}/"
                                    | "presentations" -> $"/resources/presentations/{fileName}/"
                                    | "reviews" -> $"/reviews/{fileName}/"
                                    | "streams" -> $"/streams/{fileName}/"
                                    | "media" -> $"/media/{fileName}/"
                                    | _ -> $"/{contentType}/{fileName}/"
                                let properPermalink = getProperPermalink item.ContentType fileName
                                
                                // Clean content safely for JSON without truncation - full content display
                                let safeContent = 
                                    let content = convertMdToHtml item.Content  // Convert markdown to HTML first
                                    // Clean content similar to initial loading to ensure consistency
                                    let removeArticleStart = System.Text.RegularExpressions.Regex.Replace(content, @"<article[^>]*>", "")
                                    let removeArticleEnd = removeArticleStart.Replace("</article>", "")
                                    let removeTitles = System.Text.RegularExpressions.Regex.Replace(removeArticleEnd, @"<h1[^>]*>.*?</h1>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                                    let safeCleaned = removeTitles.Replace("<script", "&lt;script").Replace("</script>", "&lt;/script&gt;")
                                    escapeJson safeCleaned
                                
                                sprintf """{"title":"%s","contentType":"%s","date":"%s","url":"%s","content":"%s","tags":[%s]}"""
                                    (escapeJson item.Title)
                                    (escapeJson item.ContentType)
                                    (escapeJson item.Date)
                                    (escapeJson properPermalink)
                                    safeContent
                                    (item.Tags |> Array.map (fun tag -> sprintf "\"%s\"" (escapeJson tag)) |> String.concat ",")
                            )
                            |> String.concat ","
                        
                        script [ _type "application/json"; _id $"remainingContentData-{contentType}" ] [
                            rawText $"[{contentTypeItemsJson}]"
                        ]
                
                // Progressive loading controls (JavaScript will handle type-specific loading)
                div [ _class "load-more-section"; _id "loadMoreSection" ] [
                    let totalRemaining = remainingItemsByType |> List.sumBy (fun (_, items) -> items.Length)
                    button [ 
                        _class "load-more-btn"
                        _id "loadMoreBtn"
                        _type "button"
                        attr "data-chunk-size" "10"
                    ] [
                        Text $"Load More ({totalRemaining} items remaining)"
                    ]
                    div [ _class "loading-indicator hidden"; _id "loadingIndicator" ] [
                        div [ _class "loading-spinner" ] []
                        Text "Loading more content..."
                    ]
                ]
        ]
        
        // Back to top button for long content scrolling (UX best practice)
        button [ 
            _class "back-to-top"
            _id "backToTopBtn"
            _type "button"
            _title "Back to top"
            attr "aria-label" "Scroll back to top of page"
        ] [
            // Using simple up arrow for universal recognition
            span [ _class "icon"; attr "aria-hidden" "true" ] [ Text "↑" ]
        ]
    ]

// New timeline homepage view for feed-as-homepage interface - Progressive Loading Implementation
let timelineHomeView (items: GenericBuilder.UnifiedFeeds.UnifiedFeedItem array) =
    // Research-backed progressive loading: Start with safe 50 items, load more on demand
    
    div [ _class "h-feed unified-timeline" ] [
        // Header with personal intro and content filters
        header [ _class "timeline-header text-center p-4" ] [
            div [ _class "avatar-section mb-3" ] [
                img [ _src "/avatar.png"; _alt "Luis Quintanilla Avatar Image"; _class "rounded-circle"; _height "150"; _width "150" ]
                div [ _class "mt-2" ] [
                    h1 [ _class "p-name" ] [
                        str "Hi, I'm "
                        a [ _href "/about"; _class "author-link" ] [ Text "Luis" ]
                        span [] [ Text " &#x1F44B;" ]
                    ]
                    p [ _class "tagline" ] [ Text "Latest updates from across the site" ]
                ]
            ]
            
            // Content type filters (will be styled as buttons in CSS)
            div [ _class "content-filters mt-3"; _id "contentFilters" ] [
                button [ _class "filter-btn active"; attr "data-filter" "all"; _type "button" ] [ Text "All" ]
                button [ _class "filter-btn"; attr "data-filter" "posts"; _type "button" ] [ Text "Blog Posts" ]
                button [ _class "filter-btn"; attr "data-filter" "notes"; _type "button" ] [ Text "Notes" ]
                button [ _class "filter-btn"; attr "data-filter" "responses"; _type "button" ] [ Text "Responses" ]
                button [ _class "filter-btn"; attr "data-filter" "reviews"; _type "button" ] [ Text "Reviews" ]
                button [ _class "filter-btn"; attr "data-filter" "bookmarks"; _type "button" ] [ Text "Bookmarks" ]
                button [ _class "filter-btn"; attr "data-filter" "media"; _type "button" ] [ Text "Media" ]
            ]
        ]
        
        // Timeline content area with progressive loading
        main [ _class "timeline-content" ] [
            // Initial content batch (safe loading threshold based on content-volume-html-parsing-discovery.md)
            div [ _class "initial-content"; _id "initialContent" ] [
                for item in (items |> Array.take (min 50 items.Length)) do
                    let fileName = Path.GetFileNameWithoutExtension(item.Url)
                    let getProperPermalink (contentType: string) (fileName: string) =
                        match contentType with
                        | "posts" -> $"/posts/{fileName}/"
                        | "notes" -> $"/notes/{fileName}/"
                        | "responses" -> $"/responses/{fileName}/"
                        | "bookmarks" -> $"/bookmarks/{fileName}/"  // Bookmarks are responses but filtered separately
                        | "snippets" -> $"/resources/snippets/{fileName}/"
                        | "wiki" -> $"/resources/wiki/{fileName}/"
                        | "presentations" -> $"/resources/presentations/{fileName}/"
                        | "reviews" -> $"/reviews/{fileName}/"
                        | "streams" -> $"/streams/{fileName}/"
                        | "media" -> $"/media/{fileName}/"
                        // Specific response types also route to /responses/
                        | "star" | "reply" | "reshare" -> $"/responses/{fileName}/"
                        | "bookmark" -> $"/bookmarks/{fileName}/"
                        | _ -> $"/{contentType}/{fileName}/"
                    
                    let properPermalink = getProperPermalink item.ContentType fileName
                    
                    // Content card with desert theme and filtering attributes
                    article [ 
                        _class "h-entry content-card"
                        attr "data-type" item.ContentType
                        attr "data-date" item.Date
                    ] [
                        header [ _class "card-header" ] [
                            time [ _class "dt-published publication-date"; attr "datetime" item.Date ] [
                                Text (DateTimeOffset.Parse(item.Date).ToString("MMM dd, yyyy"))
                            ]
                            div [ _class "content-type-info" ] [
                                span [ _class "content-type-badge"; attr "data-type" item.ContentType ] [
                                    Text (match item.ContentType with
                                          | "posts" -> "Blog Post"
                                          | "notes" -> "Note"
                                          | "responses" -> "Response"
                                          | "bookmarks" -> "Bookmark"
                                          | "reviews" -> "Review"
                                          | "streams" -> "Stream Recording"
                                          | "media" -> "Media"
                                          // Specific response types
                                          | "star" -> "Star"
                                          | "reply" -> "Reply"
                                          | "reshare" -> "Reshare"
                                          | "bookmark" -> "Bookmark"
                                          | _ -> item.ContentType)
                                ]
                            ]
                        ]
                        
                        div [ _class "card-body" ] [
                            h2 [ _class "p-name card-title" ] [
                                a [ _class "u-url title-link"; _href properPermalink ] [ Text item.Title ]
                            ]
                            div [ _class "e-content card-content" ] [
                                // Convert markdown content to HTML and clean it
                                let cleanedContent = 
                                    let content = convertMdToHtml item.Content  // Convert markdown to HTML first
                                    // Remove all article opening tags with any class
                                    let removeArticleStart = System.Text.RegularExpressions.Regex.Replace(content, @"<article[^>]*>", "")
                                    // Remove all article closing tags
                                    let removeArticleEnd = removeArticleStart.Replace("</article>", "")
                                    // Remove duplicate h1/h2 titles (common source of duplication)
                                    let removeTitles = System.Text.RegularExpressions.Regex.Replace(removeArticleEnd, @"<h1[^>]*>.*?</h1>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                                    // Additional cleaning to prevent HTML parsing issues
                                    let safeCleaned = removeTitles.Replace("<script", "&lt;script").Replace("</script>", "&lt;/script&gt;")
                                    safeCleaned
                                rawText cleanedContent
                            ]
                        ]
                        
                        footer [ _class "card-footer" ] [
                            div [ _class "card-meta" ] [
                                if item.Tags.Length > 0 then
                                    div [ _class "p-category tags" ] [
                                        for tag in item.Tags do
                                            a [ _class "tag-link"; _href $"/tags/{sanitizeTagForUrl tag}/" ] [ Text $"#{tag}" ]
                                    ]
                            ]
                        ]
                    ]
            ]
            
            // Progressive loading container for additional content chunks
            div [ 
                _class "progressive-content"
                _id "progressiveContent"
                attr "data-total-items" (items.Length.ToString())
            ] []
            
            // Progressive loading trigger - JavaScript will load remaining content chunks
            if items.Length > 50 then
                // Pass remaining items as JSON for JavaScript progressive loading
                let remainingItems = items |> Array.skip 50
                
                // Proper JSON escaping function
                let escapeJson (text: string) =
                    text.Replace("\\", "\\\\")
                        .Replace("\"", "\\\"")
                        .Replace("\b", "\\b")
                        .Replace("\f", "\\f")
                        .Replace("\n", "\\n")
                        .Replace("\r", "\\r")
                        .Replace("\t", "\\t")
                
                let remainingItemsJson = 
                    remainingItems 
                    |> Array.map (fun item ->
                        let fileName = Path.GetFileNameWithoutExtension(item.Url)
                        let getProperPermalink (contentType: string) (fileName: string) =
                            match contentType with
                            | "posts" -> $"/posts/{fileName}/"
                            | "notes" -> $"/notes/{fileName}/"
                            | "responses" -> $"/responses/{fileName}/"
                            | "bookmarks" -> $"/bookmarks/{fileName}/"
                            | "snippets" -> $"/resources/snippets/{fileName}/"
                            | "wiki" -> $"/resources/wiki/{fileName}/"
                            | "presentations" -> $"/resources/presentations/{fileName}/"
                            | "reviews" -> $"/reviews/{fileName}/"
                            | "streams" -> $"/streams/{fileName}/"
                            | "media" -> $"/media/{fileName}/"
                            | _ -> $"/{contentType}/{fileName}/"
                        let properPermalink = getProperPermalink item.ContentType fileName
                        
                        // Clean content safely for JSON without truncation - full content display
                        let safeContent = 
                            let content = convertMdToHtml item.Content  // Convert markdown to HTML first
                            // Clean content similar to initial loading to ensure consistency
                            let removeArticleStart = System.Text.RegularExpressions.Regex.Replace(content, @"<article[^>]*>", "")
                            let removeArticleEnd = removeArticleStart.Replace("</article>", "")
                            let removeTitles = System.Text.RegularExpressions.Regex.Replace(removeArticleEnd, @"<h1[^>]*>.*?</h1>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                            let safeCleaned = removeTitles.Replace("<script", "&lt;script").Replace("</script>", "&lt;/script&gt;")
                            escapeJson safeCleaned
                        
                        sprintf """{"title":"%s","contentType":"%s","date":"%s","url":"%s","content":"%s","tags":[%s]}"""
                            (escapeJson item.Title)
                            (escapeJson item.ContentType)
                            (escapeJson item.Date)
                            (escapeJson properPermalink)
                            safeContent
                            (item.Tags |> Array.map (fun tag -> sprintf "\"%s\"" (escapeJson tag)) |> String.concat ",")
                    )
                    |> String.concat ","
                
                script [ _type "application/json"; _id "remainingContentData" ] [
                    rawText $"[{remainingItemsJson}]"
                ]
                
                div [ _class "load-more-section"; _id "loadMoreSection" ] [
                    button [ 
                        _class "load-more-btn"
                        _id "loadMoreBtn"
                        _type "button"
                        attr "data-total-items" (items.Length.ToString())
                        attr "data-loaded-items" "50"
                        attr "data-chunk-size" "25"
                    ] [
                        Text $"Load More ({items.Length - 50} items remaining)"
                    ]
                    div [ _class "loading-indicator hidden"; _id "loadingIndicator" ] [
                        div [ _class "loading-spinner" ] []
                        Text "Loading more content..."
                    ]
                ]
        ]
        
        // Back to top button for long content scrolling (UX best practice)
        button [ 
            _class "back-to-top"
            _id "backToTopBtn"
            _type "button"
            _title "Back to top"
            attr "aria-label" "Scroll back to top of page"
        ] [
            // Using simple up arrow for universal recognition
            span [ _class "icon"; attr "aria-hidden" "true" ] [ Text "↑" ]
        ]
    ]

let homeView (blog:Post) (microblog:Post) (response:Response) =
    div [ _class "mr-auto" ] [
        div [_class "row mx-auto p-2"] [
            div [_class "col d-flex align-self-center justify-content-center"] [
                div [_class "p-2"] [
                    img [_src "/avatar.png"; _alt "Luis Quintanilla Avatar Image"; _class "rounded-circle"; _height "200"; _width "200" ]
                    div [_class "mt-2 align-self-center text-center"] [
                        h2 [] [
                            str "Hi, I'm "
                            a [_href "/about"] [Text "Luis"]
                            span [] [Text " &#x1F44B;"]
                        ]
                    ]
                ]
            ]
        ]
        div [_class "row mx-auto p-2 text-center"] [
            div [_class "col align-self-center justify-content-center"] [
                h2 [] [
                    str "Latest Microblog Note"
                ]
                a [_href $"/notes/{Path.GetFileNameWithoutExtension(microblog.FileName)}"] [Text microblog.Metadata.Title]                
            ]
        ]
        div [_class "row mx-auto p-2 text-center"] [
            div [_class "col align-self-center justify-content-center"] [
                h2 [] [
                    str "Latest Response"
                ]
                a [_href $"/responses/{Path.GetFileNameWithoutExtension(response.FileName)}"] [Text response.Metadata.Title]                
            ]
        ]
        div [_class "row mx-auto p-2 text-center"] [
            div [_class "col align-self-center justify-content-center"] [
                h2 [] [
                    str "Latest Blog Post"
                ]
                a [_href $"/posts/{Path.GetFileNameWithoutExtension(blog.FileName)}"] [Text blog.Metadata.Title]
            ]
        ]
    ]

let contentView (content:string) = 
    div [ _class "mr-auto" ] [
        rawText content
    ]

let contentViewWithTitle (title:string) (content:string) = 
    div [ _class "mr-auto" ] [
        h1 [] [Text title]
        rawText content
    ]    

let snippetPageView (title:string) (content:string) (date:string) (fileName:string) = 
    let publishDate = DateTimeOffset.Parse(date)
    div [ _class "mr-auto" ] [
        article [ _class "h-entry individual-post" ] [
            header [ _class "post-header" ] [
                h1 [ _class "p-name post-title" ] [ Text title ]
                div [ _class "post-meta" ] [
                    time [ _class "dt-published"; attr "datetime" date ] [
                        Text (publishDate.ToString("MMMM d, yyyy"))
                    ]
                ]
                // Hidden IndieWeb author information for microformats compliance
                div [ _class "u-author h-card microformat-hidden" ] [
                    img [ _src "/avatar.png"; _class "u-photo"; _alt "Luis Quintanilla" ]
                    a [ _href "/about"; _class "u-url p-name" ] [ Text "Luis Quintanilla" ]
                ]
            ]
            
            div [ _class "e-content post-content" ] [
                rawText content
            ]
            
            footer [ _class "post-footer" ] [
                div [ _class "permalink-info" ] [
                    Text "Permalink: "
                    a [ _class "u-url permalink-link"; _href $"/resources/snippets/{Path.GetFileNameWithoutExtension(fileName)}/" ] [
                        Text $"/resources/snippets/{Path.GetFileNameWithoutExtension(fileName)}/"
                    ]
                ]
                webmentionForm
            ]
        ]
    ]

let wikiPageView (title:string) (content:string) (date:string) (fileName:string) = 
    let publishDate = DateTimeOffset.Parse(date)
    div [ _class "mr-auto" ] [
        article [ _class "h-entry individual-post" ] [
            header [ _class "post-header" ] [
                h1 [ _class "p-name post-title" ] [ Text title ]
                div [ _class "post-meta" ] [
                    time [ _class "dt-published"; attr "datetime" date ] [
                        Text (publishDate.ToString("MMMM d, yyyy"))
                    ]
                ]
                // Hidden IndieWeb author information for microformats compliance
                div [ _class "u-author h-card microformat-hidden" ] [
                    img [ _src "/avatar.png"; _class "u-photo"; _alt "Luis Quintanilla" ]
                    a [ _href "/about"; _class "u-url p-name" ] [ Text "Luis Quintanilla" ]
                ]
            ]
            
            div [ _class "e-content post-content" ] [
                rawText content
            ]
            
            footer [ _class "post-footer" ] [
                div [ _class "permalink-info" ] [
                    Text "Permalink: "
                    a [ _class "u-url permalink-link"; _href $"/resources/wiki/{Path.GetFileNameWithoutExtension(fileName)}/" ] [
                        Text $"/resources/wiki/{Path.GetFileNameWithoutExtension(fileName)}/"
                    ]
                ]
                webmentionForm
            ]
        ]
    ]

let reviewPageView (title:string) (content:string) (date:string) (fileName:string) = 
    let publishDate = DateTimeOffset.Parse(date)
    div [ _class "mr-auto" ] [
        article [ _class "h-entry individual-post" ] [
            header [ _class "post-header" ] [
                h1 [ _class "p-name post-title" ] [ Text title ]
                div [ _class "post-meta" ] [
                    time [ _class "dt-published"; attr "datetime" date ] [
                        Text (publishDate.ToString("MMMM d, yyyy"))
                    ]
                ]
                // Hidden IndieWeb author information for microformats compliance
                div [ _class "u-author h-card microformat-hidden" ] [
                    img [ _src "/avatar.png"; _class "u-photo"; _alt "Luis Quintanilla" ]
                    a [ _href "/about"; _class "u-url p-name" ] [ Text "Luis Quintanilla" ]
                ]
            ]
            
            div [ _class "e-content post-content" ] [
                rawText content
            ]
            
            footer [ _class "post-footer" ] [
                div [ _class "permalink-info" ] [
                    Text "Permalink: "
                    a [ _class "u-url permalink-link"; _href $"/reviews/{Path.GetFileNameWithoutExtension(fileName)}/" ] [
                        Text $"/reviews/{Path.GetFileNameWithoutExtension(fileName)}/"
                    ]
                ]
                webmentionForm
            ]
        ]
    ]

let presentationPageView (presentation:Presentation) = 
    let publishDate = DateTimeOffset.Parse(presentation.Metadata.Date)
    div [ _class "mr-auto" ] [
        article [ _class "h-entry individual-post" ] [
            header [ _class "post-header" ] [
                h1 [ _class "p-name post-title" ] [ Text presentation.Metadata.Title ]
                div [ _class "post-meta" ] [
                    time [ _class "dt-published"; attr "datetime" presentation.Metadata.Date ] [
                        Text (publishDate.ToString("MMMM d, yyyy"))
                    ]
                ]
                // Hidden IndieWeb author information for microformats compliance
                div [ _class "u-author h-card microformat-hidden" ] [
                    img [ _src "/avatar.png"; _class "u-photo"; _alt "Luis Quintanilla" ]
                    a [ _href "/about"; _class "u-url p-name" ] [ Text "Luis Quintanilla" ]
                ]
            ]
            
            div [ _class "e-content post-content" ] [
                // Embed Reveal.js presentation in the post content
                div [ _class "presentation-container" ] [
                    div [ _class "reveal" ] [
                        div [ _class "slides" ] [
                            section [ flag "data-markdown" ] [
                                textarea [ flag "data-template" ] [
                                    rawText presentation.Content
                                ]
                            ]
                        ]
                    ]
                ]
                
                // Resources section as part of the content
                hr []
                h3 [] [Text "Resources"]
                ul [] [
                    for resource in presentation.Metadata.Resources do
                        li [] [a [_href $"{resource.Url}"] [Text resource.Text]]
                ]
            ]
            
            footer [ _class "post-footer" ] [
                div [ _class "permalink-info" ] [
                    Text "Permalink: "
                    a [ _class "u-url permalink-link"; _href $"/resources/presentations/{Path.GetFileNameWithoutExtension(presentation.FileName)}/" ] [
                        Text $"/resources/presentations/{Path.GetFileNameWithoutExtension(presentation.FileName)}/"
                    ]
                ]
                webmentionForm
            ]
        ]
    ]


let liveStreamView (title:string) = 
    div [ _class "mr-auto" ] [
        h1 [] [Text title]
        iframe [
            _src "https://owncast-app.jollybeach-ea688d6c.northcentralus.azurecontainerapps.io/embed/video"
            _title "lqdev Live Stream"
            _height "350px"
            _width "550px"
            attr "referrerpolicy" "origin"
            flag "allowfullscreen"
        ] []
    ]

let blogPostView (title:string) (content:string) (date:string) (fileName:string) = 
    let publishDate = DateTimeOffset.Parse(date)
    div [ _class "mr-auto" ] [
        article [ _class "h-entry individual-post" ] [
            header [ _class "post-header" ] [
                h1 [ _class "p-name post-title" ] [ Text title ]
                div [ _class "post-meta" ] [
                    time [ _class "dt-published"; attr "datetime" date ] [
                        Text (publishDate.ToString("MMMM d, yyyy"))
                    ]
                ]
                // Hidden IndieWeb author information for microformats compliance
                div [ _class "u-author h-card microformat-hidden" ] [
                    img [ _src "/avatar.png"; _class "u-photo"; _alt "Luis Quintanilla" ]
                    a [ _href "/about"; _class "u-url p-name" ] [ Text "Luis Quintanilla" ]
                ]
            ]
            
            div [ _class "e-content post-content" ] [
                rawText content
            ]
            
            footer [ _class "post-footer" ] [
                div [ _class "permalink-info" ] [
                    Text "Permalink: "
                    a [ _class "u-url permalink-link"; _href $"/posts/{Path.GetFileNameWithoutExtension(fileName)}/" ] [
                        Text $"/posts/{Path.GetFileNameWithoutExtension(fileName)}/"
                    ]
                ]
                webmentionForm
            ]
        ]
    ]

let notePostView (title:string) (content:string) (date:string) (fileName:string) = 
    let publishDate = DateTimeOffset.Parse(date)
    div [ _class "mr-auto" ] [
        article [ _class "h-entry individual-post" ] [
            header [ _class "post-header" ] [
                h1 [ _class "p-name post-title" ] [ Text title ]
                div [ _class "post-meta" ] [
                    time [ _class "dt-published"; attr "datetime" date ] [
                        Text (publishDate.ToString("MMMM d, yyyy"))
                    ]
                ]
                // Hidden IndieWeb author information for microformats compliance
                div [ _class "u-author h-card microformat-hidden" ] [
                    img [ _src "/avatar.png"; _class "u-photo"; _alt "Luis Quintanilla" ]
                    a [ _href "/about"; _class "u-url p-name" ] [ Text "Luis Quintanilla" ]
                ]
            ]
            
            div [ _class "e-content post-content" ] [
                rawText content
            ]
            
            footer [ _class "post-footer" ] [
                div [ _class "permalink-info" ] [
                    Text "Permalink: "
                    a [ _class "u-url permalink-link"; _href $"/notes/{Path.GetFileNameWithoutExtension(fileName)}/" ] [
                        Text $"/notes/{Path.GetFileNameWithoutExtension(fileName)}/"
                    ]
                ]
                webmentionForm
            ]
        ]
    ]

let responsePostView (title:string) (content:string) (date:string) (fileName:string) (targetUrl:string) = 
    let publishDate = DateTimeOffset.Parse(date)
    div [ _class "mr-auto" ] [
        article [ _class "h-entry individual-post" ] [
            header [ _class "post-header" ] [
                h1 [ _class "p-name post-title" ] [ Text title ]
                div [ _class "post-meta" ] [
                    time [ _class "dt-published"; attr "datetime" date ] [
                        Text (publishDate.ToString("MMMM d, yyyy"))
                    ]
                ]
                // Hidden IndieWeb author information for microformats compliance
                div [ _class "u-author h-card microformat-hidden" ] [
                    img [ _src "/avatar.png"; _class "u-photo"; _alt "Luis Quintanilla" ]
                    a [ _href "/about"; _class "u-url p-name" ] [ Text "Luis Quintanilla" ]
                ]
            ]
            
            // Target URL display for responses
            div [ _class "response-target mb-3" ] [
                p [] [
                    span [ _class "bi bi-link-45deg"; _style "margin-right:5px;color:#6c757d;" ] []
                    Text "→ "
                    a [ _class "u-bookmark-of"; _href targetUrl; _target "_blank" ] [ 
                        Text targetUrl 
                    ]
                ]
            ]
            
            div [ _class "e-content post-content" ] [
                rawText content
            ]
            
            footer [ _class "post-footer" ] [
                div [ _class "permalink-info" ] [
                    Text "Permalink: "
                    a [ _class "u-url permalink-link"; _href $"/responses/{Path.GetFileNameWithoutExtension(fileName)}/" ] [
                        Text $"/responses/{Path.GetFileNameWithoutExtension(fileName)}/"
                    ]
                ]
                webmentionForm
            ]
        ]
    ]

let postPaginationView (currentPage: int) (lastPage: int) (posts: Post array) =
    let nextPage = currentPage + 1
    let previousPage = currentPage - 1

    div [ _class "mr-auto" ] [
        div [] [ recentPostsView posts ]

        div [ _class "mt-2 text-center" ] [
            if (lastPage > 1) then
                if (currentPage = 1) then
                    div [ _class "btn-group" ] [
                        a [ _href "/posts/1"
                            _class "btn btn-secondary" ] [
                            Text "First"
                        ]
                        a [ _href (sprintf "/posts/%i" nextPage)
                            _class "btn btn-secondary" ] [
                            Text "Next"
                        ]
                        a [ _href (sprintf "/posts/%i" lastPage)
                            _class "btn btn-secondary" ] [
                            Text "Last"
                        ]
                    ]
                else if (currentPage = lastPage) then
                    div [ _class "btn-group" ] [
                        a [ _href "/posts/1"
                            _class "btn btn-secondary" ] [
                            Text "First"
                        ]
                        a [ _href (sprintf "/posts/%i" previousPage)
                            _class "btn btn-secondary" ] [
                            Text "Back"
                        ]
                        a [ _href (sprintf "/posts/%i" lastPage)
                            _class "btn btn-secondary" ] [
                            Text "Last"
                        ]
                    ]
                else if (currentPage > 1) then
                    div [ _class "btn-group" ] [
                        a [ _href "/posts/1"
                            _class "btn btn-secondary" ] [
                            Text "First"
                        ]
                        a [ _href (sprintf "/posts/%i" previousPage)
                            _class "btn btn-secondary" ] [
                            Text "Back"
                        ]
                        a [ _href (sprintf "/posts/%i" nextPage)
                            _class "btn btn-secondary" ] [
                            Text "Next"
                        ]
                        a [ _href (sprintf "/posts/%i" lastPage)
                            _class "btn btn-secondary" ] [
                            Text "Last"
                        ]
                    ]
        ]
    ]

let eventView (events: Event array) =
    div [ _class "mr-auto" ] [
        h2 [] [ Text "Upcoming events" ]
        p [] [
            Text "All times are in EST (UTC -5) unless otherwise stated"
        ]
        table [ _class "table" ] [
            thead [] [
                tr [] [
                    th [] [ Text "Date/Time" ]
                    th [] [ Text "Name" ]
                    th [] [ Text "Url" ]
                ]
            ]
            tbody [] [
                for event in events do
                    tr [] [
                        td [] [
                            Text(DateTimeOffset.Parse(event.Date).ToString("f"))
                        ]
                        td [] [ Text event.Name ]
                        td [] [
                            a [ _href event.Url ] [ Text "Url" ]
                        ]
                    ]
            ]
        ]
    ]

let linkView (links: Link array) = 
    div [ _class "mr-auto" ] [
        h2 [] [ Text "Linkblog" ]
        p [] [Text "Link aggregator"]
        ul [] [
            for link in links do 
                li [] [
                    a [_href link.Url] [Text link.Title]
                ]
        ]
     ]
