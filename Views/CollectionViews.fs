module CollectionViews

open Giraffe.ViewEngine
open System
open System.IO
open Domain
open ContentViews

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
            // let header = ComponentViews.cardHeader album.Metadata.Date
            
            div [ _class "card rounded m-2 w-75 mx-auto h-entry" ] [
                // header
                
                div [ _class "card-body" ] [
                    h5 [_class "card-title"] [
                        a [_href $"/media/{album.FileName}"; _class "text-decoration-none"] [
                            Text album.Metadata.Title
                        ]
                    ]
                    div [_class "album-preview mb-3"] [
                        if not (isNull album.Metadata.Images) && album.Metadata.Images.Length > 0 then
                            img [
                                _src album.Metadata.Images.[0].ImagePath
                                _alt album.Metadata.Images.[0].AltText
                                _class "img-fluid rounded"
                                _style "max-height: 200px; width: 100%; object-fit: cover;"
                                attr "loading" "lazy"
                            ]
                        else
                            div [_class "placeholder-img"; _style "height: 200px; background-color: #f8f9fa; display: flex; align-items: center; justify-content: center;"] [
                                Text "No preview available"
                            ]
                    ]
                    p [_class "text-muted"] [
                        if not (isNull album.Metadata.Images) then
                            Text $"{Array.length album.Metadata.Images} photos"
                        else
                            Text "0 photos"
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
