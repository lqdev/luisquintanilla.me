module PartialViews

    open Giraffe.ViewEngine
    open Domain
    open MarkdownService

    let recentPostsView (posts:Post array) = 
        div [_class "mr-auto"] [
            h2 [] [Text "Recent Posts"]
            ul [_class "list-group"] [
                for post in posts do
                    li [_class "list-group-item"] [
                        a [_href (sprintf "/posts/%s.html" post.FileName)] [
                            h3 [] [Text post.Metadata.Title]
                        ]
                        rawText (post.Content |> summarizePost  |> ConvertMdToHtml)
                    ]                    
            ]
        ]

    let homeView content = 
        div [_class "mr-auto"] [
            rawText content    
        ]

    let aboutView content =  
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
            div [_class "mr-auto"] [
                ul [_class "list-group"] [
                    for post in posts do
                        li [_class "list-group-item"] [
                            a [_href (sprintf "/posts/%s.html" post.FileName)] [
                                h3 [] [Text post.Metadata.Title]
                            ]
                            rawText (post.Content |> summarizePost  |> ConvertMdToHtml)
                        ]
                ]
            ]

            if(lastPage > 1) then
                if(currentPage = 1) then
                    div [_class "btn-group justify-content-end mr-auto"] [
                        a [_href "/posts/1/1.html"; _class "btn btn-secondary"] [Text "First"]
                        a [_href (sprintf "/posts/%i/%i.html" nextPage nextPage); _class "btn btn-secondary"] [Text "Next"]
                        a [_href (sprintf "/posts/%i/%i.html" lastPage lastPage); _class "btn btn-secondary"] [Text "Last"]
                    ]
                else if(currentPage = lastPage) then
                    div [_class "btn-group justify-content-end mr-auto"] [
                        a [_href "/posts/1/1.html"; _class "btn btn-secondary"] [Text "First"]
                        a [_href (sprintf "/posts/%i/%i.html" previousPage previousPage); _class "btn btn-secondary"] [Text "Back"]
                        a [_href (sprintf "/posts/%i/%i.html" lastPage lastPage); _class "btn btn-secondary"] [Text "Last"]
                    ]
                else if(currentPage > 1) then
                    div [_class "btn-group justify-content-end mr-auto"] [
                        a [_href "/posts/1/1.html"; _class "btn btn-secondary"] [Text "First"]
                        a [_href (sprintf "/posts/%i/%i.html" previousPage previousPage); _class "btn btn-secondary"] [Text "Back"]
                        a [_href (sprintf "/posts/%i/%i.html" nextPage nextPage); _class "btn btn-secondary"] [Text "Next"]
                        a [_href (sprintf "/posts/%i/%i.html" lastPage lastPage); _class "btn btn-secondary"] [Text "Last"] 
                    ]
        ]

    let eventView (events:Event array) = 
        div [_class "mr-auto"] [
            h2 [] [Text "Upcoming events"]
            table [_class "table"] [
                thead [] [
                    tr [] [
                        th [] [Text "Date"]
                        th [] [Text "Name"]
                        th [] [Text "Url"]
                    ]
                ]
                tbody [] [
                    for event in events do
                        tr [] [
                            td [] [Text event.Date]
                            td [] [Text event.Name]
                            td [] [ a[ _href event.Url] [Text "Url"]]
                        ]
                ]
            ]
        ]