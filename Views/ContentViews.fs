module ContentViews

open Giraffe.ViewEngine
open System
open Domain
open MarkdownService
open ComponentViews

// Response body views for different IndieWeb response types
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

// Individual content type views
let feedPostView (post:Post) = 
    let header = cardHeader post.Metadata.Date
    let footer = cardFooter "posts" post.FileName post.Metadata.Tags

    div [ _class "card rounded m-2 w-75 mx-auto" ] [
        header
        div [ _class "card-body" ] [
            rawText post.Content
            hr []
            webmentionForm
        ]
        footer
    ]

let notePostView (post:Post) = 
    let header = cardHeader post.Metadata.Date
    let footer = cardFooter "notes" post.FileName post.Metadata.Tags

    div [ _class "card rounded m-2 w-75 mx-auto" ] [
        header
        div [ _class "card-body" ] [
            rawText post.Content
            hr []
            webmentionForm
        ]
        footer
    ]

let responsePostView (post: Response) = 
    let header = cardHeader post.Metadata.DatePublished
    let footer = cardFooter "responses" post.FileName post.Metadata.Tags
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

let bookmarkPostView (bookmark: Bookmark) = 
    let header = cardHeader bookmark.Metadata.DatePublished
    let footer = cardFooter "bookmarks" bookmark.FileName bookmark.Metadata.Tags
    
    div [ _class "card rounded m-2 w-75 mx-auto h-entry" ] [
        header
        div [ _class "card-body" ] [
            a [ _href bookmark.Metadata.BookmarkOf; _class "u-bookmark-of" ] [
                h5 [ _class "card-title p-name" ] [ Text bookmark.Metadata.Title ]
            ]
            p [ _class "card-text p-summary" ] [ Text bookmark.Metadata.Description ]
        ]
        footer
    ] 

let bookPostView (book: Book) = 
    div [_class "card mb-4 mx-auto"] [
        div [_class "row"] [
            div [_class "col-md-4"] [
                img [_src book.Metadata.Cover]
            ]
            div [_class "col-md-8"] [
                div [_class "card-body"] [
                    a [_href $"/reviews/{book.FileName}"] [
                        h5 [_class "card-title"] [Text book.Metadata.Title]
                    ]
                    p [_class "card-text"] [Text $"Author: {book.Metadata.Author}"]
                    p [_class "card-text"] [Text $"Status: {book.Metadata.Status}"]
                    p [_class "card-text"] [Text $"Rating: {book.Metadata.Rating}/5"]
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
            
            // Handle both legacy format (with Images) and new format (with :::media blocks)
            if isNull album.Metadata.Images || Array.isEmpty album.Metadata.Images then
                // New format - content already includes processed :::media blocks
                div [] [rawText "<!-- Content will be processed by :::media blocks -->"]
            else
                // Legacy format - convert album to markdown with :::media block and render
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

// Content views with backlinks
let feedPostViewWithBacklink (feedPostView:XmlNode) = 
    let mainFeedBacklink = feedBacklink "/posts"
    div [] [
        feedPostView        
        mainFeedBacklink
    ]

let notePostViewWithBacklink (notePostView:XmlNode) = 
    let notesBacklink = feedBacklink "/notes"
    div [] [
        notePostView        
        notesBacklink
    ]

let reponsePostViewWithBacklink (responsePostView:XmlNode) = 
    let responseBacklink = feedBacklink "/responses"
    div [] [
        responsePostView
        responseBacklink
    ]

let albumPostViewWithBacklink (albumPostView:XmlNode) = 
    let albumBacklink = feedBacklink "/media"
    div [] [
        albumPostView        
        albumBacklink
    ]
