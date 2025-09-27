module ComponentViews

open Giraffe.ViewEngine
open System
open Domain
open TagService

// Utility and empty views
let emptyView () = 
    div [] []

let underConstructionView () =
    div [] [
        img [ _src "images/assets/under-construction.png" ]
    ]

// Seasonal emoji for headers
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

// Card components
let cardHeader (date:string) =
    try
        let dt = DateTimeOffset.Parse(date)
        div [_class "card-header u-author h-card"] [
            img [_src "/avatar.png"; _height "32"; _width "32"; _class "d-inline-block align-top rounded-circle u-photo"; _style "margin-right:5px"; attr "loading" "lazy"]
            a [ _href "/about"; _class "u-url p-name"] [Text "lqdev"]
            seasonalCheckmarkEmoji
            span [_class "float-right"] [
                Text (dt.ToString("MMM dd, yyyy"))
            ] 
        ]    
    with
    | ex ->
        printfn $"Error parsing date '{date}': {ex.Message}"
        div [_class "card-header u-author h-card"] [
            img [_src "/avatar.png"; _height "32"; _width "32"; _class "d-inline-block align-top rounded-circle u-photo"; _style "margin-right:5px"; attr "loading" "lazy"]
            a [ _href "/about"; _class "u-url p-name"] [Text "lqdev"]
            seasonalCheckmarkEmoji
            span [_class "float-right"] [
                time [_class "dt-published"; _datetime date] [Text date]
            ] 
        ]

/// Sanitize tag names for URL usage while preserving display text
let private sanitizeTagForUrl (tag: string) =
    tag.Replace("#", "sharp").Replace("/", "-").Replace(" ", "-").Replace("\"", "")

/// Generate copy-to-clipboard button for permalinks
let copyPermalinkButton (relativeUrl: string) =
    let fullUrl = $"https://www.luisquintanilla.me{relativeUrl}"
    button [
        _class "copy-permalink-btn btn btn-sm btn-outline-secondary ms-2"
        _type "button"
        _title "Copy to clipboard"
        attr "data-url" fullUrl
        attr "aria-label" $"Copy {fullUrl} to clipboard"
    ] [
        tag "i" [_class "copy-icon bi bi-clipboard"; attr "aria-hidden" "true"] []
    ]

let cardFooter (contentType:string) (fileName:string) (tags: string array)= 
    let tagElements = 
        tags
        |> cleanTags
        |> Array.map(fun tag -> a [_href $"/tags/{sanitizeTagForUrl tag}"; _class "tag-link"] [Text $"#{tag}"])

    div [_class "card-footer"] [
        let permalink = $"/{contentType}/{fileName}/" 
        div [_class "permalink-section d-flex align-items-center"] [
            Text "Permalink: " 
            a [_href permalink; _class "u-url"] [Text $"{permalink}"]
            copyPermalinkButton permalink
        ]
        
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
        |> Array.map(fun tag -> a [_href $"/tags/{sanitizeTagForUrl tag}"; _class "tag-link"] [Text $"#{tag}"])

    div [_class "card-footer"] [
        let permalink = $"/media/{fileName}/" 
        div [_class "permalink-section d-flex align-items-center"] [
            Text "Permalink: " 
            a [_href permalink; _class "u-url"] [Text $"{permalink}"]
            copyPermalinkButton permalink
        ]
        
        div [] [
            str "Tags: "
            for tag in tagElements do
                tag
                Text " "
        ]
    ]

/// Tags section for individual post pages - shows tags as clickable hashtags
let postTagsSection (tags: string array) =
    if not (isNull tags) && tags.Length > 0 then
        let tagElements = 
            tags
            |> cleanTags
            |> Array.map(fun tag -> a [_href $"/tags/{sanitizeTagForUrl tag}"; _class "tag-link"] [Text $"#{tag}"])
        
        div [_class "post-tags-section"] [
            str "Tags: "
            for tag in tagElements do
                tag
                Text " "
        ]
    else
        div [] [] // Empty div if no tags

// Navigation and backlink components
let feedBacklink (url:string) = 
    div [_class "text-center"] [
        b [] [
            str "Back to "
            a [_href url] [Text "feed"]
        ]
    ]

// Webmention form component
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
                        _type "url"
                        _class "form-control"
                        _name "source"
                        _placeholder "Your URL (webmention source)"
                        _required
                    ]
                ]
                div [_class "col-auto"] [
                    input [_type "submit"; _class "btn btn-primary"; _value "Send"] 
                ]
                input [
                    _type "hidden"
                    _readonly
                    _name "target"
                    _id "webmention-target"
                ]
            ]
        ]
    ]
