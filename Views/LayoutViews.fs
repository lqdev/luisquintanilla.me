module LayoutViews

open Giraffe.ViewEngine
open System
open System.IO
open Domain
open ComponentViews
open CollectionViews

// New timeline homepage view for feed-as-homepage interface
let timelineHomeView (items: GenericBuilder.UnifiedFeeds.UnifiedFeedItem array) =
    // Initial load: Show first 100 items (Performance optimization for large datasets)
    // TODO: Implement progressive loading via JavaScript for remaining items
    let initialItems = items |> Array.take (min 100 items.Length)
    printfn "Debug: Showing %d items initially (%d total available)" initialItems.Length items.Length
    
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
                button [ _class "filter-btn"; attr "data-filter" "bookmarks"; _type "button" ] [ Text "Bookmarks" ]
                button [ _class "filter-btn"; attr "data-filter" "reviews"; _type "button" ] [ Text "Reviews" ]
                button [ _class "filter-btn"; attr "data-filter" "streams"; _type "button" ] [ Text "Streams" ]
                button [ _class "filter-btn"; attr "data-filter" "media"; _type "button" ] [ Text "Media" ]
            ]
        ]
        
        // Timeline content area
        main [ _class "timeline-content" ] [
            // Render timeline cards with desert theme and content type data attributes
            for item in initialItems do
                let fileName = Path.GetFileNameWithoutExtension(item.Url)
                let getProperPermalink (contentType: string) (fileName: string) =
                    match contentType with
                    | "posts" -> $"/posts/{fileName}/"
                    | "notes" -> $"/notes/{fileName}/"
                    | "responses" -> $"/responses/{fileName}/"
                    | "bookmarks" -> $"/responses/{fileName}/"  // Bookmarks are responses but filtered separately
                    | "reviews" -> $"/reviews/{fileName}/"
                    | "streams" -> $"/streams/{fileName}/"
                    | "media" -> $"/media/{fileName}/"
                    | _ -> $"/{contentType}/{fileName}/"
                
                let properPermalink = getProperPermalink item.ContentType fileName
                
                // Content card with desert theme and filtering attributes
                article [ 
                    _class "h-entry content-card"
                    attr "data-type" item.ContentType
                    attr "data-date" item.Date
                ] [
                    header [ _class "card-header" ] [
                        div [ _class "h-card author-info" ] [
                            img [ _class "u-photo author-avatar"; _src "/avatar.png"; _alt "Luis Quintanilla" ]
                            span [ _class "p-name author-name" ] [ Text "Luis Quintanilla" ]
                            time [ _class "dt-published publication-date"; attr "datetime" item.Date ] [
                                Text (DateTime.Parse(item.Date).ToString("MMM dd, yyyy"))
                            ]
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
                                      | _ -> item.ContentType)
                            ]
                        ]
                    ]
                    
                    div [ _class "card-body" ] [
                        h2 [ _class "p-name card-title" ] [
                            a [ _class "u-url title-link"; _href properPermalink ] [ Text item.Title ]
                        ]
                        div [ _class "e-content card-content" ] [
                            rawText item.Content
                        ]
                    ]
                    
                    footer [ _class "card-footer" ] [
                        div [ _class "card-meta" ] [
                            a [ _class "u-url permalink-link"; _href properPermalink ] [ Text "Read more →" ]
                            if item.Tags.Length > 0 then
                                div [ _class "p-category tags" ] [
                                    for tag in item.Tags do
                                        a [ _class "tag-link"; _href $"/tags/{tag}/" ] [ Text $"#{tag}" ]
                                ]
                        ]
                    ]
                ]
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

let liveStreamView (title:string) = 
    div [ _class "mr-auto" ] [
        h1 [] [Text title]
        iframe [
            _src "https://test-owncast2.jollybeach-ea688d6c.northcentralus.azurecontainerapps.io/embed/video"
            _title "lqdev Live Stream"
            _height "350px"
            _width "550px"
            attr "referrerpolicy" "origin"
            flag "allowfullscreen"
        ] []
    ]

let blogPostView (title:string) (content:string) = 
    div [ _class "mr-auto" ] [
        h1 [] [Text title]
        rawText content
        hr []
        webmentionForm
    ]

let notePostView (title:string) (content:string) = 
    div [ _class "mr-auto" ] [
        h1 [] [Text title]
        rawText content
        hr []
        webmentionForm
    ]

let responsePostView (title:string) (content:string) = 
    div [ _class "mr-auto" ] [
        h1 [] [Text title]
        rawText content
        hr []
        webmentionForm
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
                            Text(DateTime.Parse(event.Date).ToString("f"))
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
