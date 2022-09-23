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
        img [ _src "images/assets/under-construction.png" ]
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

let contentView (content:string) = 
    div [ _class "mr-auto" ] [
        rawText content
    ]

let contentViewWithTitle (title:string) (content:string) = 
    div [ _class "mr-auto" ] [
        h1 [] [Text title]
        rawText content
    ]    

let rollLinkView (links:Outline array) = 
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

let blogRollView (links:Outline array) = 
    
    let linkContent = rollLinkView links 

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

        linkContent
    ]

let podRollView (links:Outline array) = 

    let linkContent = rollLinkView links

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
        
        linkContent
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
        span [_class "bi bi-shield-fill-check"; _style "margin-right:5px;margin-left:5px;color:green"] []
        span [_class "float-right"] [
            time [_class "dt-published"; _datetime date] [Text $"{dt.ToShortDateString()}"]
        ] 
    ]    

let cardFooter (fileName:string) = 

    div [_class "card-footer"] [
        let permalink = $"/feed/{fileName}/" 
        Text "Permalink: " 
        a [_href permalink] [Text $"https://luisquintanilla.me{permalink}"]
    ]

let feedPostView (post:Post) = 

    let header = cardHeader post.Metadata.Date
    let footer = cardFooter post.FileName

    div [ _class "card rounded m-2 w-75 mx-auto" ] [

        header

        div [ _class "card-body" ] [
            rawText post.Content
        ]

        footer
    ]

let feedView (posts: Post array) =
    div [ _class "d-grip gap-3" ] [
        for post in posts do
            feedPostView post
    ]

let replyBodyView (post:Response) = 
    div [ _class "card-body" ] [
        p [] [
            span [_class "bi bi-reply-fill"; _style "margin-right:5px;margin-left:5px;"] []
            a [_class "u-in-reply-to"; _href $"{post.Metadata.TargetUrl}"] [Text post.Metadata.TargetUrl]
        ]
        div [_class "e-content"] [
            rawText post.Content
        ]
    ]        

// Repost
let reshareBodyView (post:Response) = 
    div [ _class "card-body" ] [
        p [] [
            span [_class "bi bi-share-fill"; _style "margin-right:5px;margin-left:5px;"] []
            a [_class "u-repost-of"; _href $"{post.Metadata.TargetUrl}"] [Text post.Metadata.TargetUrl]
        ]
        div [_class "e-content"] [
            rawText post.Content
        ]
    ]

// Like / Favorite
let starBodyView (post:Response) = 
    div [ _class "card-body" ] [
        p [] [
            span [_class "bi bi-star-fill"; _style "margin-right:5px;margin-left:5px;color:yellow"] []
            a [_class "u-like-of"; _href $"{post.Metadata.TargetUrl}"] [Text post.Metadata.TargetUrl]
        ]
        div [_class "e-content"] [
            rawText post.Content
        ]
    ]


let responsePostView (post: Response) = 

    let header = cardHeader post.Metadata.DatePublished
    let footer = cardFooter post.FileName
    let body = 
        match post.Metadata.ResponseType with
        | "reply" -> replyBodyView post
        | "reshare" -> reshareBodyView post
        | "star" -> starBodyView post
        | _ -> div [_class "card-body"] [p [] [Text "No content"]]
        
    div [ _class "card rounded m-2 w-75 mx-auto h-entry" ] [
        header

        body    

        footer
    ]

let responseView (posts: Response array) =
    div [ _class "d-grip gap-3" ] [
        for post in posts do
            responsePostView post
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
