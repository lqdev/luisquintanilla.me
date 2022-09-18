module PartialViews

open Giraffe.ViewEngine
open System
open System.IO
open Domain
open MarkdownService

let emptyView () = 
    div [] []

let underConstructionView () =
    div [] [
        img [ _src "images/under-construction.png" ]
    ]

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

let homeView content =
    div [ _class "mr-auto" ] [
        h2 [ _class "text-dark" ] [ Text "Recent Posts" ]
        rawText content
        div [ _class "mt-2 text-center" ] [
            a [ _href "/posts/2"
                _class "btn btn-secondary" ] [
                Text "Read More"
            ]
        ]
    ]

let aboutView content =
    div [ _class "mr-auto" ] [
        rawText content
    ]

let contactView content =
    div [ _class "mr-auto" ] [
        rawText content
    ]

let blogRollView (links:Outline array) = 
    div [ _class "mr-auto" ] [
        h2 [] [ Text "Blogroll" ]
        p [] [ Text "What is a blogroll you ask? At a high level, it's a list of links to blogs I find interesting."]
        p [] [
            str "Check out the article "
            a [_href "https://blogroll.org/what-are-blogrolls/"] [Text "What are blogrolls?"]
            str " for more details."

        ]
        p [] [
            str "You can subscribe to any of the individual feeds in your preferred RSS reader using the RSS feed links below. Want to subscribe to all of them? Use the "
            a [ _href "/feed/blogroll/index.opml"] [Text "OPML file"]
            str " if your RSS reader client supports "
            a [_href "http://opml.org/"] [Text "OPML."]
        ]        
        ul [] [
            for link in links do
                li [] [
                    strong [] [
                        str $"{link.Title} - "
                    ]
                    a [ _href link.HtmlUrl ] [ Text "Website"]
                    str " / "
                    a [ _href link.XmlUrl ] [ Text "RSS Feed"]                    
                ]
        ]
    ]

let podRollView (links:Outline array) = 
    div [ _class "mr-auto" ] [
        h2 [] [ Text "Podroll" ]
        p [] [ 
            str "I took the podroll concept from blogrolls. In short, this list of podcasts I find interesting. If you're interested in the blogroll, you can find it "
            a [_href "/feed/blogroll"] [Text "here"]            
            str "."
        ]
        p [] [
            str "You can subscribe to any of the individual feeds in your preferred RSS reader or podcast client using the RSS feed links below. Want to subscribe to all of them? Use the "
            a [ _href "/feed/pordoll/index.opml"] [Text "OPML file"]
            str " if your RSS reader or podcast client supports "
            a [_href "http://opml.org/"] [Text "OPML."]
        ]
        ul [] [
            for link in links do
                li [] [
                    strong [] [
                        str $"{link.Title} - "
                    ]
                    a [ _href link.HtmlUrl ] [ Text "Website"]
                    str " / "
                    a [ _href link.XmlUrl ] [ Text "RSS Feed"]                    
                ]
        ]
    ]

let irlStackView content = 
    div [ _class "mr-auto" ] [
        rawText content
    ]    

let colophonView content = 
    div [ _class "mr-auto" ] [
        rawText content
    ]

let subscribeView content = 
    div [ _class "mr-auto" ] [
        rawText content
    ]     

let onlineRadioView content = 
    div [ _class "mr-auto" ] [
        rawText content
    ]            

let postView title content =
    div [ _class "mr-auto" ] [
        h1 [] [Text title]
        rawText content
    ]

let snippetsView (snippets: Snippet array) = 
    div [ _class "d-grip gap-3" ] [
        h2[] [Text "Snippets"]
        p [] [Text "List of code snippets"]
        ul [] [
            for snippet in snippets do
                li [] [
                    a [ _href $"/snippets/{snippet.FileName}"] [ Text snippet.Metadata.Title ]
                ]
        ]
    ]

let snippetView (snippet:Snippet) =
    div [ _class "mr-auto" ] [
        h1 [] [Text snippet.Metadata.Title]
        rawText snippet.Content
    ]

let wikisView (wikis: Wiki array) = 
    div [ _class "d-grip gap-3" ] [
        h2[] [Text "Wikis"]
        p [] [Text "Personal wiki articles"]
        ul [] [
            for wiki in wikis do
                li [] [
                    a [ _href $"/wiki/{wiki.FileName}"] [ Text wiki.Metadata.Title ]
                ]
        ]
    ]

let wikiView (wiki:Wiki) =
    div [ _class "mr-auto" ] [
        h1 [] [Text wiki.Metadata.Title]
        rawText wiki.Content
    ]

let bookView (book:Book) =
    div [ _class "mr-auto" ] [
        h1 [] [Text book.Metadata.Title]
        rawText book.Content
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


let cardHeader (date:string) =
    let dt = 
        DateTime.Parse(date)


    div [_class "card-header u-author h-card"] [
        img [_src "/avatar.png"; _height "32"; _width "32"; _class "d-inline-block align-top rounded-circle u-photo"; _style "margin-right:5px"; attr "loading" "lazy"]
        a [ _href "http://lqdev.me"; _class "p-name"] [Text "lqdev"]
        tag "svg" [
            _style "margin-right:5px;margin-left:5px"
            _class "bi bi-shield-fill-check" 
            attr "fill" "green"
            attr "viewBox" "0 0 16 16"
            _height "16"
            _width "16"
        ] [
            tag "path" [
                attr "fill-rule" "evenodd"
                attr "d" "M8 0c-.69 0-1.843.265-2.928.56-1.11.3-2.229.655-2.887.87a1.54 1.54 0 0 0-1.044 1.262c-.596 4.477.787 7.795 2.465 9.99a11.777 11.777 0 0 0 2.517 2.453c.386.273.744.482 1.048.625.28.132.581.24.829.24s.548-.108.829-.24a7.159 7.159 0 0 0 1.048-.625 11.775 11.775 0 0 0 2.517-2.453c1.678-2.195 3.061-5.513 2.465-9.99a1.541 1.541 0 0 0-1.044-1.263 62.467 62.467 0 0 0-2.887-.87C9.843.266 8.69 0 8 0zm2.146 5.146a.5.5 0 0 1 .708.708l-3 3a.5.5 0 0 1-.708 0l-1.5-1.5a.5.5 0 1 1 .708-.708L7.5 7.793l2.646-2.647z"] []
        ]
        span [_class "float-right"] [
            time [_class "dt-published"; _datetime $"{post.Metadata.DatePublished}";] [Text $"{dt.ToShortDateString()}"]
        ] 
    ]    

let feedPostView (post:Post) = 

    let date =
        DateTime
            .Parse(post.Metadata.Date)
            .ToShortDateString()

    div [ _class "card rounded m-2 w-75 mx-auto" ] [
        div [_class "card-header"] [
            img [_src "/avatar.png"; _height "32"; _width "32"; _class "d-inline-block align-top rounded-circle"; _style "margin-right:5px"; attr "loading" "lazy"]
            Text $"Luis Quintanilla"
            tag "svg" [
                _style "margin-right:5px;margin-left:5px"
                _class "bi bi-shield-fill-check" 
                attr "fill" "green"
                attr "viewBox" "0 0 16 16"
                _height "16"
                _width "16"
            ] [
                tag "path" [
                    attr "fill-rule" "evenodd"
                    attr "d" "M8 0c-.69 0-1.843.265-2.928.56-1.11.3-2.229.655-2.887.87a1.54 1.54 0 0 0-1.044 1.262c-.596 4.477.787 7.795 2.465 9.99a11.777 11.777 0 0 0 2.517 2.453c.386.273.744.482 1.048.625.28.132.581.24.829.24s.548-.108.829-.24a7.159 7.159 0 0 0 1.048-.625 11.775 11.775 0 0 0 2.517-2.453c1.678-2.195 3.061-5.513 2.465-9.99a1.541 1.541 0 0 0-1.044-1.263 62.467 62.467 0 0 0-2.887-.87C9.843.266 8.69 0 8 0zm2.146 5.146a.5.5 0 0 1 .708.708l-3 3a.5.5 0 0 1-.708 0l-1.5-1.5a.5.5 0 1 1 .708-.708L7.5 7.793l2.646-2.647z"] []
            ]
            span [_class "float-right"] [ Text date ] 
        ]
        div [ _class "card-body" ] [
            rawText post.Content
        ]
        div [_class "card-footer"] [
            let permalink = $"/feed/{post.FileName}/" 
            Text "Permalink: " 
            a [_href permalink] [Text $"https://luisquintanilla.me{permalink}"]
        ]
    ]

let feedView (posts: Post array) =
    div [ _class "d-grip gap-3" ] [
        for post in posts do
            feedPostView post
    ]

let bookPostView (book: Book) = 
    div [_class "card mb-4 mx-auto"] [
        div [_class "row"] [
            div [_class "col-md-4"] [
                img [_src book.Metadata.Cover]
            ]
            div [_class "col-md-8"] [
                div [_class "card-body"] [
                    a [_href $"/library/{book.FileName}"] [
                        h5 [_class "card-title"] [Text book.Metadata.Title]
                    ]
                    p [_class "card-text"] [Text book.Metadata.Author]
                    p [_class "card-text"] [Text $"{book.Metadata.Rating}/5"]
                ]                
            ]
        ]
    ]

let libraryView (books:Book array) = 
    div [ _class "d-grip gap-3" ] [
        for book in books do
            bookPostView book
    ]

let presentationsView (presentations: Presentation array) = 
    div [ _class "d-grip gap-3" ] [
        h2[] [Text "Presentations"]
        p [] [Text "List of presentations and associated resources"]
        ul [] [
            for presentation in presentations do
                li [] [
                    a [ _href $"/presentations/{presentation.FileName}"] [ Text presentation.Metadata.Title ]
                ]
        ]
    ]

let presentationPageView (presentation:Presentation) = 
    div [_class "presentation-container"] [
        h2 [] [Text presentation.Metadata.Title]
        div [ _class "reveal"] [
            div [ _class "slides"] [
                section [ flag "data-markdown"] [
                    textarea [ flag "data-template" ] [
                        rawText presentation.Content
                    ]
                ]
            ]
        ]
        hr []
        h3 [] [Text "Resources"]
        ul [] [
            for resource in presentation.Metadata.Resources do
                li [] [a [_href $"{resource.Url}"] [Text resource.Text]]
        ]  
    ]

let albumsPageView (images:Album array) = 
    let albumGroups = images |> Array.chunkBySize 3
    div [_class "mr-auto"] [
        for group in albumGroups do
            div [_class "row"] [
                for album in group do
                    div [_class "col-md-4"] [
                        div [_class "img-thumbnail"] [
                            a [_href $"/albums/{album.FileName}"; _target "blank"] [    
                                img [_src album.Metadata.MainImage]
                                p [] [Text album.Metadata.Title]
                            ]                                
                        ]
                    ]
            ]
    ]

let albumPageView (images:AlbumImage array) = 
    let imgGroups = images |> Array.chunkBySize 3
    div [_class "mr-auto"] [
        for group in imgGroups do
            div [_class "row"] [
                for image in group do
                    div [_class "col-md-4"] [
                        div [_class "img-thumbnail"] [
                            a [_href image.ImagePath; _target "blank"] [    
                                img [_src $"{image.ImagePath}"; _alt $"{image.AltText}" ]
                                p [] [Text image.Description]
                            ]                                
                        ]
                    ]
            ]
    ]

let responsePostView (post: Response) = 

    let header = cardHeader post.Metadata.DatePublished
        
    div [ _class "card rounded m-2 w-75 mx-auto h-entry" ] [
        header
        div [ _class "card-body" ] [
            p [] [
                Text "In Reply To: "
                a [_class "u-in-reply-to"; _href $"{post.Metadata.TargetUrl}"] [Text post.Metadata.TargetUrl]
            ]
            div [_class "e-content"] [
                rawText post.Content
            ]
        ]
        div [_class "card-footer"] [
            let permalink = $"/feed/{post.FileName}/" 
            Text "Permalink: " 
            a [_href permalink; _class "u-url"] [Text $"http://lqdev.me{permalink}"]
        ]
    ]

let responseView (posts: Response array) =
    div [ _class "d-grip gap-3" ] [
        for post in posts do
            responsePostView post
    ]    