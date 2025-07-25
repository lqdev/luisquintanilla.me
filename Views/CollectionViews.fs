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
            feedPostView post
    ]

let notesView (posts: Post array) =
    div [ _class "d-grip gap-3" ] [
        for post in posts do
            notePostView post
    ]

let responseView (posts: Response array) =
    div [ _class "d-grip gap-3" ] [
        for post in posts do
            responsePostView post
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
        // Content is now clean HTML from CardHtml, no need for CDATA stripping
        let cleanContent = item.Content
        
        // Create a custom footer with proper permalink
        let customFooter = 
            div [_class "card-footer"] [
                Text "Permalink: " 
                a [_href properPermalink; _class "u-url"] [Text properPermalink] 
                
                div [] [
                    str "Tags: "
                    for tag in item.Tags do
                        a [_href $"/tags/{tag}"; _class "p-category"] [Text $"#{tag}"]
                        Text " "
                ]
            ]
        
        div [ _class "card rounded m-2 w-75 mx-auto h-entry" ] [
            header
            div [ _class "card-body" ] [
                // Add content type indicator
                div [ _class "mb-2" ] [
                    span [ _class "badge badge-secondary" ] [ 
                        Text (item.ContentType |> fun ct -> ct.Substring(0, 1).ToUpper() + ct.Substring(1))
                    ]
                ]
                // Smart content display with length-based logic
                div [ _class "card-text" ] [
                    let contentLength = cleanContent.Length
                    let shouldTruncate = 
                        match item.ContentType.ToLower() with
                        | "response" -> contentLength > 300  // Responses: show full if under 300 chars
                        | "post" -> contentLength > 800      // Posts: show full if under 800 chars  
                        | "snippet" -> contentLength > 600   // Snippets: show full if under 600 chars
                        | "notes" -> contentLength > 400     // Notes: show full if under 400 chars
                        | _ -> contentLength > 500           // Others: show full if under 500 chars
                    
                    match item.ContentType.ToLower() with
                    | "response" ->
                        // Clean up response content and show with icon
                        let responseTypeIcon, cleanedContent = 
                            if cleanContent.Contains("reply") || cleanContent.Contains("Reply") then
                                let cleaned = cleanContent.Replace("reply", "").Replace("Reply", "").Replace("<div class=\"response-type\">reply</div>", "").Replace("<div class=\"response-type\">Reply</div>", "")
                                (span [_class "bi bi-reply-fill me-2"; _style "color:#3F5576;"] []), cleaned
                            elif cleanContent.Contains("reshare") || cleanContent.Contains("Share") then
                                let cleaned = cleanContent.Replace("reshare", "").Replace("Share", "").Replace("<div class=\"response-type\">reshare</div>", "").Replace("<div class=\"response-type\">Share</div>", "")
                                (span [_class "bi bi-share-fill me-2"; _style "color:#C0587E;"] []), cleaned
                            elif cleanContent.Contains("star") || cleanContent.Contains("Star") then
                                let cleaned = cleanContent.Replace("star", "").Replace("Star", "").Replace("<div class=\"response-type\">star</div>", "").Replace("<div class=\"response-type\">Star</div>", "")
                                (span [_class "bi bi-star-fill me-2"; _style "color:#ff7518;"] []), cleaned
                            elif cleanContent.Contains("bookmark") || cleanContent.Contains("Bookmark") then
                                let cleaned = cleanContent.Replace("bookmark", "").Replace("Bookmark", "").Replace("<div class=\"response-type\">bookmark</div>", "").Replace("<div class=\"response-type\">Bookmark</div>", "")
                                (span [_class "bi bi-journal-bookmark-fill me-2"; _style "color:#4a60b6;"] []), cleaned
                            else
                                (span [] []), cleanContent
                        
                        div [] [
                            responseTypeIcon
                            if shouldTruncate then
                                // Preview with read more
                                div [ _class "content-preview"; _style "max-height: 200px; overflow: hidden; position: relative;" ] [
                                    rawText cleanedContent
                                ]
                                div [ _class "mt-2" ] [
                                    a [ _href properPermalink; _class "btn btn-sm btn-outline-primary" ] [ Text "Read More →" ]
                                ]
                            else
                                // Show full content, no button needed
                                div [] [
                                    rawText cleanedContent
                                ]
                        ]
                    | "post" ->
                        // Better post handling - extract meaningful content 
                        let processedContent = 
                            if cleanContent.Contains("No description available") then
                                // More aggressive cleaning of "No description available"
                                let contentParts = cleanContent.Split([|"No description available"|], StringSplitOptions.RemoveEmptyEntries)
                                let meaningfulContent = contentParts |> Array.filter (fun part -> 
                                    not (String.IsNullOrWhiteSpace(part)) && 
                                    not (part.Trim() = "<p></p>") &&
                                    not (part.Trim() = "<p>") &&
                                    not (part.Trim() = "</p>"))
                                if meaningfulContent.Length > 0 then
                                    String.concat "" meaningfulContent
                                else
                                    // Create a description from the title
                                    $"<p>Read more about <strong>{item.Title}</strong></p>"
                            else
                                cleanContent
                        
                        div [] [
                            if shouldTruncate then
                                // Preview with read more
                                div [ _class "content-preview"; _style "max-height: 400px; overflow: hidden; position: relative;" ] [
                                    rawText processedContent
                                ]
                                div [ _class "mt-2" ] [
                                    a [ _href properPermalink; _class "btn btn-sm btn-outline-primary" ] [ Text "Read More →" ]
                                ]
                            else
                                // Show full content, no button needed
                                div [] [
                                    rawText processedContent
                                ]
                        ]
                    | "snippet" ->
                        // Code snippets with syntax highlighting preservation
                        div [] [
                            if shouldTruncate then
                                div [ _class "content-preview"; _style "max-height: 300px; overflow: hidden;" ] [
                                    rawText cleanContent
                                ]
                                div [ _class "mt-2" ] [
                                    a [ _href properPermalink; _class "btn btn-sm btn-outline-secondary" ] [ Text "Read More →" ]
                                ]
                            else
                                div [] [
                                    rawText cleanContent
                                ]
                        ]
                    | "notes" ->
                        // Notes - often shorter, show more content
                        div [] [
                            if shouldTruncate then
                                div [ _class "content-preview"; _style "max-height: 250px; overflow: hidden;" ] [
                                    rawText cleanContent
                                ]
                                div [ _class "mt-2" ] [
                                    a [ _href properPermalink; _class "btn btn-sm btn-outline-info" ] [ Text "Read More →" ]
                                ]
                            else
                                div [] [
                                    rawText cleanContent
                                ]
                        ]
                    | "wiki" ->
                        // Wiki pages - structured content
                        div [] [
                            if shouldTruncate then
                                div [ _class "content-preview"; _style "max-height: 350px; overflow: hidden;" ] [
                                    rawText cleanContent
                                ]
                                div [ _class "mt-2" ] [
                                    a [ _href properPermalink; _class "btn btn-sm btn-outline-info" ] [ Text "Read More →" ]
                                ]
                            else
                                div [] [
                                    rawText cleanContent
                                ]
                        ]
                    | _ ->
                        // Default handling for other content types
                        div [] [
                            if shouldTruncate then
                                div [ _class "content-preview"; _style "max-height: 300px; overflow: hidden;" ] [
                                    rawText cleanContent
                                ]
                                div [ _class "mt-2" ] [
                                    a [ _href properPermalink; _class "btn btn-sm btn-outline-primary" ] [ Text "Read More →" ]
                                ]
                            else
                                div [] [
                                    rawText cleanContent
                                ]
                        ]
                ]
                hr []
                webmentionForm
            ]
            customFooter
        ]
    
    div [ _class "d-grip gap-3" ] [
        for item in items do
            renderUnifiedCard item
    ]
