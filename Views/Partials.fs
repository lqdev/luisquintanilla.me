module PartialViews

    open Giraffe.ViewEngine
    open Domain

    let homeView = 
        div [_class "mr-auto"] [
            h1 [ ] [ Text "Hello" ]
            p [ ] [ Text "This is my app"]    
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
            ul [] [
                for post in posts do
                    li [ ] [
                        a [_href (sprintf "/posts/%s.html" post.FileName)] [
                            h3 [] [Text post.Metadata.Title]
                        ]
                    ]
            ]

            if(lastPage > 1) then
                if(currentPage = 1) then
                    a [_href "/posts/1/1.html"] [Text "First"]
                    a [_href (sprintf "/posts/%i/%i.html" nextPage nextPage)] [Text "Next"]
                    a [_href (sprintf "/posts/%i/%i.html" lastPage lastPage)] [Text "Last"]
                else if(currentPage = lastPage) then
                    a [_href "/posts/1/1.html"] [Text "First"]
                    a [_href (sprintf "/posts/%i/%i.html" previousPage previousPage)] [Text "Back"]
                    a [_href (sprintf "/posts/%i/%i.html" lastPage lastPage)] [Text "Last"]
                else if(currentPage > 1) then
                    a [_href "/posts/1/1.html"] [Text "First"]
                    a [_href (sprintf "/posts/%i/%i.html" previousPage previousPage)] [Text "Back"]
                    a [_href (sprintf "/posts/%i/%i.html" nextPage nextPage)] [Text "Next"]
                    a [_href (sprintf "/posts/%i/%i.html" lastPage lastPage)] [Text "Last"]        
        ]
