module CollectionViews

open Giraffe.ViewEngine
open System
open System.IO
open Domain
open ContentViews
open ComponentViews

let recentPostsView (posts: Post array) =
    div [ _class "d-grip gap-3" ] [
        for post in posts do
            let date =
                DateTime
                    .Parse(post.Metadata.Date)
                    .ToLongDateString()

            let url = sprintf "/posts/%s/" post.FileName

            div [ _class "card rounded m-2 w-75 mx-auto" ] [
                p [ _class "card-header" ] [ Text date ]
                div [ _class "card-body" ] [
                    a [ _href url ] [
                        h5 [ _class "card-text" ] [
                            Text post.Metadata.Title
                        ]
                    ]
                ]
            ]
    ]

let feedView (posts: Post array) =
    div [ _class "d-grip gap-3" ] [
        for post in posts do
            div [ _class "mb-5 border-bottom pb-4" ] [
                // Add content type indicator and date
                div [ _class "mb-3" ] [
                    span [ _class "badge badge-light border" ] [ Text "Posts" ]
                    Text " • "
                    Text (DateTime.Parse(post.Metadata.Date).ToString("MMM dd, yyyy"))
                ]
                
                // Post title and content
                h2 [] [
                    a [_href $"/posts/{post.FileName}/"] [Text post.Metadata.Title]
                ]
                div [] [
                    rawText post.Content
                ]
                
                // Footer with permalink and tags
                div [ _class "mt-3 pt-2 border-top text-muted small" ] [
                    Text "Permalink: " 
                    a [_href $"/posts/{post.FileName}/"; _class "text-decoration-none"] [Text $"/posts/{post.FileName}/"] 
                    
                    div [ _class "mt-1" ] [
                        str "Tags: "
                        let tags = if isNull post.Metadata.Tags then [||] else post.Metadata.Tags
                        for tag in tags do
                            a [_href $"/tags/{tag}"; _class "text-decoration-none me-2"] [Text $"#{tag}"]
                    ]
                ]
            ]
    ]

let notesView (posts: Post array) =
    div [ _class "d-grip gap-3" ] [
        for post in posts do
            div [ _class "mb-5 border-bottom pb-4" ] [
                // Add content type indicator and date
                div [ _class "mb-3" ] [
                    span [ _class "badge badge-light border" ] [ Text "Notes" ]
                    Text " • "
                    Text (DateTime.Parse(post.Metadata.Date).ToString("MMM dd, yyyy"))
                ]
                
                // Post title and content
                h2 [] [
                    a [_href $"/notes/{post.FileName}/"] [Text post.Metadata.Title]
                ]
                div [] [
                    rawText post.Content
                ]
                
                // Footer with permalink and tags
                div [ _class "mt-3 pt-2 border-top text-muted small" ] [
                    Text "Permalink: " 
                    a [_href $"/notes/{post.FileName}/"; _class "text-decoration-none"] [Text $"/notes/{post.FileName}/"] 
                    
                    div [ _class "mt-1" ] [
                        str "Tags: "
                        let tags = if isNull post.Metadata.Tags then [||] else post.Metadata.Tags
                        for tag in tags do
                            a [_href $"/tags/{tag}"; _class "text-decoration-none me-2"] [Text $"#{tag}"]
                    ]
                ]
            ]
    ]

let responseView (posts: Response array) =
    div [ _class "d-grip gap-3" ] [
        for post in posts do
            div [ _class "mb-5 border-bottom pb-4" ] [
                // Add content type indicator and date
                div [ _class "mb-3" ] [
                    span [ _class "badge badge-light border" ] [ Text "Responses" ]
                    Text " • "
                    Text (DateTime.Parse(post.Metadata.DatePublished).ToString("MMM dd, yyyy"))
                ]
                
                // Response type icon and target URL
                div [ _class "mb-2" ] [
                    let (icon, color) = 
                        match post.Metadata.ResponseType with
                        | "reply" -> ("bi-reply-fill", "#3F5576")
                        | "reshare" -> ("bi-share-fill", "#C0587E")
                        | "star" -> ("bi-star-fill", "#ff7518")
                        | "bookmark" -> ("bi-journal-bookmark-fill", "#4a60b6")
                        | _ -> ("bi-chat-dots", "#666")
                    
                    span [_class $"bi {icon}"; _style $"margin-right:5px;color:{color};"] []
                    a [_href post.Metadata.TargetUrl; _class "text-decoration-none"] [Text post.Metadata.TargetUrl]
                ]
                
                // Post title and content
                h2 [] [
                    a [_href $"/responses/{post.FileName}/"] [Text post.Metadata.Title]
                ]
                div [] [
                    let cleanContent = 
                        post.Content
                            .Replace("No description available", "")
                            .Replace("<p></p>", "")
                    let timestampPattern = System.Text.RegularExpressions.Regex(@"^\s*\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}\s*$", System.Text.RegularExpressions.RegexOptions.Multiline)
                    let cleanedContent = timestampPattern.Replace(cleanContent, "").Trim()
                    rawText cleanedContent
                ]
                
                // Footer with permalink and tags
                div [ _class "mt-3 pt-2 border-top text-muted small" ] [
                    Text "Permalink: " 
                    a [_href $"/responses/{post.FileName}/"; _class "text-decoration-none"] [Text $"/responses/{post.FileName}/"] 
                    
                    div [ _class "mt-1" ] [
                        str "Tags: "
                        let tags = if isNull post.Metadata.Tags then [||] else post.Metadata.Tags
                        for tag in tags do
                            a [_href $"/tags/{tag}"; _class "text-decoration-none me-2"] [Text $"#{tag}"]
                    ]
                ]
            ]
    ]    

let bookmarkView (bookmarks: Bookmark array) =
    div [ _class "d-grip gap-3" ] [
        for bookmark in bookmarks do
            bookmarkPostView bookmark
    ]

let libraryView (books:Book array) = 
    div [ _class "d-grip gap-3" ] [
        for book in books do
            bookPostView book
    ]

let snippetsView (snippets: Snippet array) = 
    div [ _class "d-grip gap-3" ] [
        h2[] [Text "Snippets"]
        p [] [Text "List of code snippets"]
        ul [] [
            for snippet in snippets do
                li [] [
                    a [ _href $"/resources/snippets/{snippet.FileName}"] [ Text snippet.Metadata.Title ]
                ]
        ]
    ]

let wikisView (wikis: Wiki array) = 
    div [ _class "d-grip gap-3" ] [
        h2[] [Text "Wikis"]
        p [] [Text "Personal wiki articles"]
        ul [] [
            for wiki in wikis do
                li [] [
                    a [ _href $"/resources/wiki/{wiki.FileName}"] [ Text wiki.Metadata.Title ]
                ]
        ]
    ]

let presentationsView (presentations: Presentation array) = 
    div [ _class "d-grip gap-3" ] [
        h2[] [Text "Presentations"]
        p [] [Text "List of presentations and associated resources"]
        ul [] [
            for presentation in presentations do
                li [] [
                    a [ _href $"/resources/presentations/{presentation.FileName}"] [ Text presentation.Metadata.Title ]
                ]
        ]
    ]

let liveStreamsView (livestreams: Livestream array) = 
    div [ _class "d-grip gap-3" ] [
        h2[] [Text "Live Stream Recordings"]
        p [] [Text "List of live stream recordings and associated resources"]
        ul [] [
            for stream in livestreams do
                li [] [
                    a [ _href $"/streams/{stream.FileName}"] [ Text stream.Metadata.Title ]
                ]
        ]
    ]

let albumsPageView (albums:Album array) = 
    div [ _class "d-grip gap-3" ] [
        for album in albums do
            let tags = if isNull album.Metadata.Tags then [||] else album.Metadata.Tags
            let footer = ComponentViews.albumCardFooter album.FileName tags
            
            // Process the album content through MarkdownService to render :::media blocks
            let processedContent = 
                try
                    MarkdownService.convertMdToHtml album.Content
                with
                | ex -> 
                    printfn "Warning: Failed to process media content for album %s: %s" album.FileName ex.Message
                    "<p>Content processing failed</p>"
            
            div [ _class "card rounded m-2 w-75 mx-auto h-entry" ] [
                div [ _class "card-body" ] [
                    h5 [_class "card-title"] [
                        a [_href $"/media/{album.FileName}"; _class "text-decoration-none"] [
                            Text album.Metadata.Title
                        ]
                    ]
                    div [_class "album-content mb-3"] [
                        // Display the processed :::media blocks content
                        rawText processedContent
                    ]
                    p [_class "text-muted"] [
                        Text $"Permalink: /media/{album.FileName}/"
                    ]
                ]
                
                footer
            ]
    ]

let albumPageView (images:AlbumImage array) = 
    let imgGroups = images |> Array.chunkBySize 3
    div [_class "mr-auto"] [
        for group in imgGroups do
            div [_class "row"; _style "margin-bottom:5px;"] [
                for image in group do
                    div [_class "col-md-4"] [
                        div [_class "img-thumbnail"; _style "object-fit:cover;height:100%;" ] [
                            a [_href image.ImagePath; _target "blank"] [    
                                img [_src $"{image.ImagePath}"; _alt $"{image.AltText}"; _style "object-fit:cover;object-position:50% 50%;"; attr "loading" "lazy"]
                                p [] [Text image.Description]
                            ]                                
                        ]
                    ]
            ]
    ]

// Unified feed view for aggregated content across all types
let unifiedFeedView (items: GenericBuilder.UnifiedFeeds.UnifiedFeedItem array) =
    let getProperPermalink (contentType: string) (fileName: string) =
        match contentType with
        | "posts" -> $"/posts/{fileName}/"
        | "notes" -> $"/notes/{fileName}/"
        | "responses" -> $"/responses/{fileName}/"
        | "snippets" -> $"/resources/snippets/{fileName}/"
        | "wiki" -> $"/resources/wiki/{fileName}/"
        | "presentations" -> $"/resources/presentations/{fileName}/"
        | "reviews" -> $"/reviews/{fileName}/"
        | "media" -> $"/media/{fileName}/"
        | _ -> $"/{contentType}/{fileName}/"
    
    let renderUnifiedCard (item: GenericBuilder.UnifiedFeeds.UnifiedFeedItem) =
        let header = cardHeader item.Date
        let fileName = Path.GetFileNameWithoutExtension(item.Url)
        let properPermalink = getProperPermalink item.ContentType fileName
        
        // Clean content function to remove "No description available" and other placeholder text
        let cleanPlaceholderContent (content: string) =
            if String.IsNullOrWhiteSpace(content) then
                $"<p>Read more about <strong>{item.Title}</strong></p>"
            else
                let cleaned = 
                    content
                        .Replace("No description available", "")
                        .Replace("<p></p>", "")
                        .Replace("<p> </p>", "")
                        .Trim()
                
                // Remove timestamp patterns like "2025-07-06 20:09" that appear in content
                let timestampPattern = System.Text.RegularExpressions.Regex(@"^\s*\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}\s*$", System.Text.RegularExpressions.RegexOptions.Multiline)
                let cleanedWithoutTimestamp = timestampPattern.Replace(cleaned, "").Trim()
                
                if String.IsNullOrWhiteSpace(cleanedWithoutTimestamp) || cleanedWithoutTimestamp = "<p></p>" then
                    $"<p>Read more about <strong>{item.Title}</strong></p>"
                else
                    cleanedWithoutTimestamp
        
        // Function to extract content from RenderCard HTML structure
        let extractContentFromCardHtml (html: string) =
            // Remove article wrapper and extract just the content inside
            let articlePattern = System.Text.RegularExpressions.Regex(@"<article[^>]*>(.*?)</article>", System.Text.RegularExpressions.RegexOptions.Singleline)
            let articleMatch = articlePattern.Match(html)
            
            if articleMatch.Success then
                let innerContent = articleMatch.Groups.[1].Value
                
                // Remove H2 title elements (they're duplicated now)
                let titlePattern = System.Text.RegularExpressions.Regex(@"<h2[^>]*>.*?</h2>", System.Text.RegularExpressions.RegexOptions.Singleline)
                let contentWithoutTitle = titlePattern.Replace(innerContent, "").Trim()
                
                // If only whitespace left, provide fallback
                if String.IsNullOrWhiteSpace(contentWithoutTitle) then
                    $"<p>Read more about <strong>{item.Title}</strong></p>"
                else
                    contentWithoutTitle
            else
                // If not wrapped in article, try to remove any H2 titles
                let titlePattern = System.Text.RegularExpressions.Regex(@"<h2[^>]*>.*?</h2>", System.Text.RegularExpressions.RegexOptions.Singleline)
                let contentWithoutTitle = titlePattern.Replace(html, "").Trim()
                
                if String.IsNullOrWhiteSpace(contentWithoutTitle) then
                    $"<p>Read more about <strong>{item.Title}</strong></p>"
                else
                    contentWithoutTitle
        
        // Content is now clean HTML from CardHtml, extract content without title duplication
        let rawContent = extractContentFromCardHtml item.Content
        let cleanContent = cleanPlaceholderContent rawContent
        
        // Instead of creating a new card wrapper, just render the content directly
        // The RenderCard functions already generate proper article elements
        div [ _class "mb-5 border-bottom pb-4 h-entry" ] [
            // Add content type indicator above the article
            div [ _class "mb-3" ] [
                span [ _class "badge badge-light border" ] [ 
                    Text (item.ContentType |> fun ct -> ct.Substring(0, 1).ToUpper() + ct.Substring(1))
                ]
                Text " • "
                Text (DateTime.Parse(item.Date).ToString("MMM dd, yyyy"))
            ]
            
            // Render the original content directly - it's already a complete article
            rawText item.Content
            
            // Add footer with permalink and tags
            div [ _class "mt-3 pt-2 border-top text-muted small" ] [
                Text "Permalink: " 
                a [_href properPermalink; _class "u-url text-decoration-none"] [Text properPermalink] 
                
                div [ _class "mt-1" ] [
                    str "Tags: "
                    for tag in item.Tags do
                        a [_href $"/tags/{tag}"; _class "p-category text-decoration-none me-2"] [Text $"#{tag}"]
                ]
            ]
        ]
    
    div [ _class "d-grip gap-3" ] [
        for item in items do
            renderUnifiedCard item
    ]
