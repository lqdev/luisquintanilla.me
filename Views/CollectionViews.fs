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
                // Render content with Tumblr-inspired smart preview approach
                div [ _class "card-text" ] [
                    match item.ContentType.ToLower() with
                    | "response" ->
                        // Show response icon and preserve rich content formatting
                        let responseTypeIcon = 
                            if cleanContent.Contains("reply") || cleanContent.Contains("Reply") then
                                span [_class "bi bi-reply-fill me-2"; _style "color:#3F5576;"] []
                            elif cleanContent.Contains("reshare") || cleanContent.Contains("Share") then
                                span [_class "bi bi-share-fill me-2"; _style "color:#C0587E;"] []
                            elif cleanContent.Contains("star") || cleanContent.Contains("Star") then
                                span [_class "bi bi-star-fill me-2"; _style "color:#ff7518;"] []
                            elif cleanContent.Contains("bookmark") || cleanContent.Contains("Bookmark") then
                                span [_class "bi bi-journal-bookmark-fill me-2"; _style "color:#4a60b6;"] []
                            else
                                span [] []
                        
                        div [] [
                            responseTypeIcon
                            // Create a responsive content preview with "Read More" for responses
                            div [ _class "content-preview"; _style "max-height: 200px; overflow: hidden; position: relative;" ] [
                                rawText cleanContent
                            ]
                            // Add "Read More" link for longer content
                            div [ _class "mt-2" ] [
                                a [ _href item.Url; _class "btn btn-sm btn-outline-primary" ] [ Text "View Response →" ]
                            ]
                        ]
                    | "post" ->
                        // For posts, create smart preview preserving formatting
                        let processedContent = 
                            if cleanContent.Contains("No description available") then
                                // Remove the "No description available" but keep all other HTML
                                cleanContent.Replace("<p>No description available</p>", "")
                                           .Replace("No description available", "")
                            else
                                cleanContent
                        
                        // Use CSS to handle truncation while preserving HTML structure
                        div [] [
                            div [ _class "content-preview"; _style "max-height: 400px; overflow: hidden; position: relative;" ] [
                                rawText processedContent
                            ]
                            // Add gradient fade and "Read More" for longer posts
                            div [ _class "mt-2" ] [
                                a [ _href item.Url; _class "btn btn-sm btn-outline-primary" ] [ Text "Read Full Post →" ]
                            ]
                        ]
                    | "snippet" ->
                        // For code snippets, show with syntax highlighting preservation
                        div [] [
                            div [ _class "content-preview"; _style "max-height: 300px; overflow: hidden;" ] [
                                rawText cleanContent
                            ]
                            div [ _class "mt-2" ] [
                                a [ _href item.Url; _class "btn btn-sm btn-outline-secondary" ] [ Text "View Snippet →" ]
                            ]
                        ]
                    | "wiki" ->
                        // Wiki pages get a structured preview
                        div [] [
                            div [ _class "content-preview"; _style "max-height: 350px; overflow: hidden;" ] [
                                rawText cleanContent
                            ]
                            div [ _class "mt-2" ] [
                                a [ _href item.Url; _class "btn btn-sm btn-outline-info" ] [ Text "Read More →" ]
                            ]
                        ]
                    | _ ->
                        // Default handling for other content types
                        div [] [
                            div [ _class "content-preview"; _style "max-height: 300px; overflow: hidden;" ] [
                                rawText cleanContent
                            ]
                            div [ _class "mt-2" ] [
                                a [ _href item.Url; _class "btn btn-sm btn-outline-primary" ] [ Text "View →" ]
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
