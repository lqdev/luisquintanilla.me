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
    let renderUnifiedCard (item: GenericBuilder.UnifiedFeeds.UnifiedFeedItem) =
        let header = cardHeader item.Date
        let footer = cardFooter item.ContentType (Path.GetFileNameWithoutExtension(item.Url)) item.Tags
        
        div [ _class "card rounded m-2 w-75 mx-auto h-entry" ] [
            header
            div [ _class "card-body" ] [
                // Add content type indicator
                div [ _class "mb-2" ] [
                    span [ _class "badge badge-secondary" ] [ 
                        Text (item.ContentType |> fun ct -> ct.Substring(0, 1).ToUpper() + ct.Substring(1))
                    ]
                ]
                // Render content
                rawText item.Content
                hr []
                webmentionForm
            ]
            footer
        ]
    
    div [ _class "d-grip gap-3" ] [
        for item in items do
            renderUnifiedCard item
    ]
