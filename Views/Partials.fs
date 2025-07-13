module PartialViews

open Giraffe.ViewEngine
open System
open System.IO
open Domain
open MarkdownService
open TagService

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
                        ul [_class "list-group list-unstyled";] [
                            li [] [
                                span [_class "mr-1"] [Text "&#x1F4AC;"]
                                str "Scroll through my "
                                a [_href "/feed"] [Text "microblog"]
                                str " and "
                                a [_href "/feed/responses"] [Text "response"]
                                str " feeds"
                            ]
                            li [] [
                                span [_class "mr-1"] [Text "&#x1F4D6;"]
                                str "Check out my "
                                a [_href "/posts/1"] [Text "blog"]
                            ]
                            li [] [
                                span [_class "mr-1"] [Text "&#x1F514;"]
                                a [_href "/subscribe"] [Text "Subscribe"]
                                str " to content on this site."
                            ]
                            li [] [
                                span [_class "mr-1"] [Text "&#x1F4ED;"]
                                str "Find me "
                                a [_href "/contact"] [Text "elsewhere"]
                            ]                                                                                    
                        ]
                    ]
                ]
            ]
        ]
        div [_class "row mx-auto p-2 text-center"] [
            div [_class "col align-self-center justify-content-center"] [
                h2 [] [
                    str "Latest from the microblog"
                    span [_class "ml-1"] [Text "&#x1F4AC;"]
                ]
                a [_href $"/feed/{Path.GetFileNameWithoutExtension(microblog.FileName)}"] [Text microblog.Metadata.Title]
                br []
                a [_href $"/feed/{Path.GetFileNameWithoutExtension(response.FileName)}"] [Text response.Metadata.Title]                
            ]
        ]
        div [_class "row mx-auto p-2 text-center"] [
            div [_class "col align-self-center justify-content-center"] [
                h2 [] [
                    str "Latest blog post"
                    span [_class "ml-1"] [Text "&#x1F4D6;"]
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

let webmentionForm = 
    div [ ] [
        script [_type "application/javascript"] [
            rawText "window.onload = function() { document.getElementById('webmention-target').value = window.location.href }"
        ]
        form [
            _action "https://webmentions.lqdev.tech/api/inbox"
            _method "POST"
            _enctype "application/x-www-form-urlencoded"
        ] [
            h5 [_class "text-center"] [str "Send me a " ; a [_href "/contact"] [Text "message"];str " or ";a [_href "https://indieweb.org/webmentions"] [Text "webmention"]]
            div [_class "form-row justify-content-center"] [
                div [_class "w-75"] [
                    input [
                        _type "text"
                        _name "source"
                        _class "form-control"
                        _placeholder "Your URL (source)"
                    ]
                ]
                div [_class "col-auto"] [
                    input [_type "submit"; _class "btn btn-primary"; _value "Send"] 
                ]

                input [
                    _readonly
                    _class "form-control-plaintext"
                    _style "visibility:hidden"
                    _type "text"
                    _id "webmention-target"
                    _name "target"
                ]
            ]
        ]
    ]

let blogPostView (title:string) (content:string) = 
    div [ _class "mr-auto" ] [
        h1 [] [Text title]
        rawText content
        hr []
        webmentionForm
    ]    

let tagLinkView (tags: string array) = 
    ul [] [
        for tag in tags do
            li [] [
                a [_href $"/tags/{tag}"; _rel "tag"] [Text $"#{tag}"]
            ]
    ]

let tagPostLinkView (posts: Post array) (prefix:string) = 
    ul [] [
        for post in posts do
            li [] [
                a [_href $"/{prefix}/{post.FileName}"] [Text $"{post.Metadata.Title}"]
            ]
    ]

let tagResponseLinkView (responses: Response array) (prefix: string) = 
    ul [] [
        for post in responses do
            li [] [
                a [_href $"/{prefix}/{post.FileName}"] [Text $"{post.Metadata.Title}"]
            ]
    ]    

let allTagsView (tags: string array) = 
    
    let tagLinks = tagLinkView tags
    
    div [ _class "mr-auto" ] [ 
        h2 [] [Text "Tags"]
        p [] [Text "A list of tags for posts on this page"]

        tagLinks
    ]

let  individualTagView (tagName:string) (posts:Post array) (notes:Post array) (responses:Response array) = 
    let postLinks = tagPostLinkView posts "posts"
    let noteLinks = tagPostLinkView notes "feed"

    let responseLinks = tagResponseLinkView responses "feed"

    div [ _class "mr-auto" ] [ 
        h2 [] [Text $"{tagName}"]
        p [] [Text $"A list of posts tagged {tagName}"]

        h3 [] [Text "Blogs"]
        postLinks

        h3 [] [Text "Notes"]
        noteLinks

        h3 [] [Text "Responses"]
        responseLinks
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
            a [ _href "/feed/podroll/index.opml"] [Text "OPML file"]
            str " if your RSS reader or podcast client supports "
            a [_href "http://opml.org/"] [Text "OPML."]
        ]
        
        linkContent
    ]

let forumsView (links:Outline array) = 

    let linkContent = rollLinkView links

    div [ _class "mr-auto" ] [
        h2 [] [ Text "Forums" ]
        p [] [ 
            str "This is a list of forums I find interesting. If you're interested, you can also check out my "
            a [_href "/feed/blogroll"] [Text "blogroll"]
            str " and "
            a [_href "/feed/podroll"] [Text "podroll"]            
            str "."
        ]
        p [] [
            str "You can subscribe to any of the individual forums in your preferred RSS reader using the RSS feed links below. Want to subscribe to all of them? Use the "
            a [ _href "/feed/forums/index.opml"] [Text "OPML file"]
            str " if your RSS reader or podcast client supports "
            a [_href "http://opml.org/"] [Text "OPML."]
        ]
        
        linkContent
    ]

let youTubeFeedView (links:Outline array) = 

    let linkContent = rollLinkView links

    div [ _class "mr-auto" ] [
        h2 [] [ Text "YouTube" ]
        p [] [ 
            str "This is a list of YouTube channels I find interesting. If you're interested, you can also check out my "
            a [_href "/feed/blogroll"] [Text "blogroll"]
            str ", "
            a [_href "/feed/podroll"] [Text "podroll"]            
            str ", and "
            a [_href "/feed/forums"] [Text "forums"]
            str "."
        ]
        p [] [
            str "You can subscribe to any of the individual channels in your preferred RSS reader using the RSS feed links below. Want to subscribe to all of them? Use the "
            a [ _href "/feed/youtube/index.opml"] [Text "OPML file"]
            str " if your RSS reader or podcast client supports "
            a [_href "http://opml.org/"] [Text "OPML."]
        ]
        
        linkContent
    ]

let aiStarterPackFeedView (links:Outline array) = 

    let linkContent = rollLinkView links

    div [ _class "mr-auto" ] [
        h2 [] [ Text "AI Starter Pack" ]
        p [] [ 
            str "This is a list of AI resources I use to stay on top of AI news."
        ]
        p [] [
            str "You can subscribe to any of the individual feeds in your preferred RSS reader using the RSS feed links below. Want to subscribe to all of them? Use the "
            a [ _href "/feed/starter/ai/index.opml"] [Text "OPML file"]
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


let seasonalCheckmarkEmoji = 
    match DateTime.Now.Month with
    | 1 -> span [_style "margin-right:5px;margin-left:5px;"] [Text "&#x26C4;"]
    | 2 -> span [_style "margin-right:5px;margin-left:5px;"] [Text "&#x1F9AB;"]
    | 3 -> span [_style "margin-right:5px;margin-left:5px;"] [Text "&#x1F33B;"]
    | 4 -> span [_style "margin-right:5px;margin-left:5px;"] [Text "&#x2614;"]
    | 5 -> span [_style "margin-right:5px;margin-left:5px;"] [Text "&#x1F33C;"]
    | 6 -> span [_style "margin-right:5px;margin-left:5px;"] [Text "&#x2600;"]
    | 7 -> span [_style "margin-right:5px;margin-left:5px;"] [Text "&#x1F47D;"]
    | 8 -> span [_style "margin-right:5px;margin-left:5px;"] [Text "&#x1F427;"]
    | 9 -> span [_style "margin-right:5px;margin-left:5px;"] [Text "&#x1F342;"]
    | 10 -> span [_style "margin-right:5px;margin-left:5px;"] [Text "&#x1F383;"]
    | 11 -> span [_style "margin-right:5px;margin-left:5px;"] [Text "&#x1F983;"]
    | 12 -> span [_style "margin-right:5px;margin-left:5px;"] [Text "&#x1F384;"]
    | _ -> span [_style "margin-right:5px;margin-left:5px;"] [Text "&#x1F4CE;"]

let cardHeader (date:string) =
    
    let dt = 
        DateTime.Parse(date)

    div [_class "card-header u-author h-card"] [
        img [_src "/avatar.png"; _height "32"; _width "32"; _class "d-inline-block align-top rounded-circle u-photo"; _style "margin-right:5px"; attr "loading" "lazy"]
        a [ _href "/about"; _class "u-url p-name"] [Text "lqdev"]
        seasonalCheckmarkEmoji
        span [_class "float-right"] [
            time [_class "dt-published"; _datetime date] [Text $"{dt.ToShortDateString()}"]
        ] 
    ]    

let cardFooter (fileName:string) (tags: string array)= 

    let tagElements = 
        tags
        |> cleanTags
        |> Array.map(fun tag -> a [_href $"/tags/{tag}"; _class "p-category"] [Text $"#{tag}"])

    div [_class "card-footer"] [
        let permalink = $"/feed/{fileName}/" 
        Text "Permalink: " 
        a [_href permalink; _class "u-url"] [Text $"{permalink}"] 
        
        div [] [
            str "Tags: "
            for tag in tagElements do
                tag
                Text " "
        ]
    ]

let albumCardFooter (fileName:string) (tags: string array)= 

    let tagElements = 
        tags
        |> cleanTags
        |> Array.map(fun tag -> a [_href $"/tags/{tag}"; _class "p-category"] [Text $"#{tag}"])

    div [_class "card-footer"] [
        let permalink = $"/media/{fileName}/" 
        Text "Permalink: " 
        a [_href permalink; _class "u-url"] [Text $"{permalink}"] 
        
        div [] [
            str "Tags: "
            for tag in tagElements do
                tag
                Text " "
        ]
    ]

let feedBacklink (url:string) = 
    div [_class "text-center"] [
        b [] [
            str "Back to "
            a [_href url] [Text "feed"]
        ]
    ]

let feedPostView (post:Post) = 

    let header = cardHeader post.Metadata.Date
    let footer = cardFooter post.FileName post.Metadata.Tags

    div [ _class "card rounded m-2 w-75 mx-auto" ] [

        header

        div [ _class "card-body" ] [
            rawText post.Content
            hr []
            webmentionForm
        ]

        footer
    ]


let feedPostViewWithBacklink (feedPostView:XmlNode) = 
    
    let mainFeedBacklink = feedBacklink "/feed"

    div [] [
        feedPostView        
        mainFeedBacklink
    ]

let feedView (posts: Post array) =
    div [ _class "d-grip gap-3" ] [
        for post in posts do
            feedPostView post
    ]

let replyBodyView (post:Response) = 
    div [ _class "card-body" ] [
        p [] [
            span [_class "bi bi-reply-fill"; _style "margin-right:5px;margin-left:5px;color:#3F5576;"] []
            a [_class "u-in-reply-to"; _href $"{post.Metadata.TargetUrl}"] [Text post.Metadata.TargetUrl]
        ]
        div [_class "e-content"] [
            rawText post.Content
        ]
        hr []
        webmentionForm
    ]        

// Repost
let reshareBodyView (post:Response) = 
    div [ _class "card-body" ] [
        p [] [
            span [_class "bi bi-share-fill"; _style "margin-right:5px;margin-left:5px;color:#C0587E;"] []
            a [_class "u-repost-of"; _href $"{post.Metadata.TargetUrl}"] [Text post.Metadata.TargetUrl]
        ]
        div [_class "e-content"] [
            rawText post.Content
        ]
        hr []
        webmentionForm
    ]

// Star / Like / Favorite
let starBodyView (post:Response) = 
    div [ _class "card-body" ] [
        p [] [
            span [_class "bi bi-star-fill"; _style "margin-right:5px;margin-left:5px;color:#ff7518;"] []
            a [_class "u-like-of"; _href $"{post.Metadata.TargetUrl}"] [Text post.Metadata.TargetUrl]
        ]
        div [_class "e-content"] [
            rawText post.Content
        ]
        hr []
        webmentionForm
    ]

let bookmarkBodyView (post:Response) = 
    div [ _class "card-body" ] [
        p [] [
            span [_class "bi bi-journal-bookmark-fill"; _style "margin-right:5px;margin-left:5px;color:#4a60b6;"] []
            a [_class "u-bookmark-of"; _href $"{post.Metadata.TargetUrl}"] [Text post.Metadata.TargetUrl]
        ]
        div [_class "e-content"] [
            rawText post.Content
        ]
        hr []
        webmentionForm
    ]


let responsePostView (post: Response) = 

    let header = cardHeader post.Metadata.DatePublished
    let footer = cardFooter post.FileName post.Metadata.Tags
    let body = 
        match post.Metadata.ResponseType with
        | "reply" -> replyBodyView post
        | "reshare" -> reshareBodyView post
        | "star" -> starBodyView post
        | "bookmark" -> bookmarkBodyView post
        | _ -> div [_class "card-body"] [p [] [Text "No content"]]

    
    div [ _class "card rounded m-2 w-75 mx-auto h-entry" ] [
        header

        body    

        footer
    ] 

let reponsePostViewWithBacklink (responsePostView:XmlNode) = 
    
    let responseBacklink = feedBacklink "/feed/responses"

    div [] [
        responsePostView
        responseBacklink
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
                    p [_class "card-text"] [Text $"Author: {book.Metadata.Author}"]
                    p [_class "card-text"] [Text $"Status: {book.Metadata.Status}"]
                    p [_class "card-text"] [Text $"Rating: {book.Metadata.Rating}/5"]
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

let liveStreamPageView (stream:Livestream) = 


    div [_class "presentation-container"] [
        h2 [] [Text stream.Metadata.Title]
        rawText stream.Content
        hr []
        h3 [] [Text "Resources"]
        ul [] [
            for resource in stream.Metadata.Resources do
                li [] [a [_href $"{resource.Url}"] [Text resource.Text]]
        ]  
    ]

let albumsPageView (albums:Album array) = 
    div [ _class "d-grip gap-3" ] [
        for album in albums do
            let header = cardHeader album.Metadata.Date
            let footer = albumCardFooter album.FileName album.Metadata.Tags
            
            div [ _class "card rounded m-2 w-75 mx-auto h-entry" ] [
                header
                
                div [ _class "card-body" ] [
                    h5 [_class "card-title"] [
                        a [_href $"/media/{album.FileName}"; _class "text-decoration-none"] [
                            Text album.Metadata.Title
                        ]
                    ]
                    div [_class "album-preview mb-3"] [
                        img [
                            _src album.Metadata.Images.[0].ImagePath
                            _alt album.Metadata.Images.[0].AltText
                            _class "img-fluid rounded"
                            _style "max-height: 200px; width: 100%; object-fit: cover;"
                            attr "loading" "lazy"
                        ]
                    ]
                    p [_class "text-muted"] [
                        Text $"{Array.length album.Metadata.Images} photos"
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

let albumPostView (album: Album) = 

    let header = cardHeader album.Metadata.Date
    let footer = albumCardFooter album.FileName album.Metadata.Tags

    div [ _class "card rounded m-2 w-75 mx-auto h-entry" ] [

        header

        div [ _class "card-body" ] [
            h5 [_class "card-title"] [Text album.Metadata.Title]
            
            // Convert album to markdown with :::media block and render
            let mediaItems = 
                album.Metadata.Images 
                |> Array.map (fun img -> 
                    sprintf "- media_type: image\n  uri: %s\n  alt_text: %s\n  caption: %s\n  aspect: \"16:9\"" 
                        img.ImagePath img.AltText img.Description)
                |> String.concat "\n"
            let mediaBlock = sprintf ":::media\n%s\n:::media" mediaItems
            
            rawText (mediaBlock |> convertMdToHtml)
            hr []
            webmentionForm
        ]

        footer
    ]

let albumPostViewWithBacklink (albumPostView:XmlNode) = 
    
    let albumBacklink = feedBacklink "/media"

    div [] [
        albumPostView        
        albumBacklink
    ]
