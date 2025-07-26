module LayoutViews

open Giraffe.ViewEngine
open System
open System.IO
open Domain
open ComponentViews
open CollectionViews

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
