module PartialViews

    open Giraffe.ViewEngine
    open System
    open Domain
    open MarkdownService

    let underConstructionView () = 
        div [] [
            img [_src "images/under-construction.png"]
        ]

    let recentPostsView (posts:Post array) = 
        div [] [
            for post in posts do
                let date = DateTime.Parse(post.Metadata.Date).ToShortDateString()
                let url = sprintf "/posts/%s.html" post.FileName
                a [_href url] [
                    div [_class "card my-2"] [
                        div [_class "card-body"] [
                            h5 [_class "card-title"] [ Text post.Metadata.Title ]
                            h6 [_class "card-subtitle"] [ Text date ]
                        ]
                    ]
                ]
        ]

    let homeView content = 
        div [_class "mr-auto"] [
            h2 [] [Text "Recent Posts"]
            rawText content    
        ]

    let aboutView content =  
        div [_class "mr-auto"] [
            rawText content
        ]

    let contactView content =  
        div [_class "mr-auto"] [
            rawText content
        ]

    let postView content =     
        div [_class "mr-auto"] [
            rawText content
        ]        

    let postPaginationView (currentPage:int) (lastPage:int) (posts:Post array) = 
        let nextPage = currentPage + 1
        let previousPage = currentPage - 1
        div [_class "mr-auto"] [

            div [] [
                recentPostsView posts
            ]
            
            div [_class "mt-2 text-center"] [
                if(lastPage > 1) then
                    if(currentPage = 1) then
                        div [_class "btn-group"] [
                            a [_href "/posts/1"; _class "btn btn-secondary"] [Text "First"]
                            a [_href (sprintf "/posts/%i" nextPage); _class "btn btn-secondary"] [Text "Next"]
                            a [_href (sprintf "/posts/%i" lastPage); _class "btn btn-secondary"] [Text "Last"]
                        ]
                    else if(currentPage = lastPage) then
                        div [_class "btn-group"] [
                            a [_href "/posts/1"; _class "btn btn-secondary"] [Text "First"]
                            a [_href (sprintf "/posts/%i" previousPage ); _class "btn btn-secondary"] [Text "Back"]
                            a [_href (sprintf "/posts/%i" lastPage); _class "btn btn-secondary"] [Text "Last"]
                        ]
                    else if(currentPage > 1) then
                        div [_class "btn-group"] [
                            a [_href "/posts/1"; _class "btn btn-secondary"] [Text "First"]
                            a [_href (sprintf "/posts/%i" previousPage); _class "btn btn-secondary"] [Text "Back"]
                            a [_href (sprintf "/posts/%i" nextPage); _class "btn btn-secondary"] [Text "Next"]
                            a [_href (sprintf "/posts/%i" lastPage); _class "btn btn-secondary"] [Text "Last"] 
                        ]
            ]
        ]

    let eventView (events:Event array) = 
        div [_class "mr-auto"] [
            h2 [] [Text "Upcoming events"]
            p [] [Text "All times are in EST (UTC -5) unless otherwise stated"]
            table [_class "table"] [
                thead [] [
                    tr [] [
                        th [] [Text "Date/Time"]
                        th [] [Text "Name"]
                        th [] [Text "Url"]
                    ]
                ]
                tbody [] [
                    for event in events do
                        tr [] [
                            td [] [Text (DateTime.Parse(event.Date).ToString("f"))]
                            td [] [Text event.Name]
                            td [] [ a[ _href event.Url] [Text "Url"]]
                        ]
                ]
            ]
        ]