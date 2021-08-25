module Layouts

    open Giraffe.ViewEngine
  
    let defaultNavBar = 
        nav [_class "navbar navbar-expand-md navbar-dark fixed-top bg-dark"] [
            a [_class "navbar-brand"; _href "#"] [ 
                img [_src "/avatar.png"; _height "32"; _width "32"; _class "d-inline-block align-top rounded-circle"; _style "margin-right:5px"]
                Text "Luis Quintanilla" ]
            button [
                _type "button"
                _class "navbar-toggler collapsed"
                attr "data-toggle" "collapse"
                attr "data-target" "#navbarCollapse"
                attr "aria-controls" "navbarCollapse"
                attr "aria-expanded" "false"
                attr "aria-label" "Toggle navigation"] [
                span [_class "navbar-toggler-icon"] []
            ]
            div [_class "collapse navbar-collapse"; _id "navbarCollapse"] [
                ul [_class "navbar-nav mr-auto"] [
                    li [_class "nav-item active"] [
                        a [_class "nav-link"; _href "/"] [ Text "Home" ]
                    ]
                    li [_class "nav-item"] [
                        a [_class "nav-link"; _href "/about.html"] [ Text "About" ]
                    ]
                    li [_class "nav-item"] [
                        a [_class "nav-link"; _href "/contact.html"] [ Text "Contact" ]
                    ]
                    li [_class "nav-item"] [
                        a [_class "nav-link"; _href "/posts/1"] [ Text "Blog" ]
                    ]
                    li [_class "nav-item"] [
                        a [_class "nav-link"; _href "/events.html"] [ Text "Events" ]
                    ]                                        
                ]
                a [_href "/feed.rss"] [
                    tag "svg" [
                        _class "bi bi-rss text-secondary" 
                        attr "fill" "currentColor"
                        attr "viewBox" "0 0 16 16"
                        _height "32"
                        _width "32"] [
                        
                        tag "path" [attr "d" "M14 1a1 1 0 0 1 1 1v12a1 1 0 0 1-1 1H2a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1h12zM2 0a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V2a2 2 0 0 0-2-2H2z"] []
                        tag "path" [attr "d" "M5.5 12a1.5 1.5 0 1 1-3 0 1.5 1.5 0 0 1 3 0zm-3-8.5a1 1 0 0 1 1-1c5.523 0 10 4.477 10 10a1 1 0 1 1-2 0 8 8 0 0 0-8-8 1 1 0 0 1-1-1zm0 4a1 1 0 0 1 1-1 6 6 0 0 1 6 6 1 1 0 1 1-2 0 4 4 0 0 0-4-4 1 1 0 0 1-1-1z"] []
                    ]
                ]
            ]
        ]

    let defaultLayout (pageTitle:string) (content:string) =
        html [_lang "en"] [
            head [] [
                link [_rel "stylesheet";_href "/css/bootstrap.min.css"] //4.6.0
                link [_rel "stylesheet";_href "/css/bootstrap-icons-1.5.0/bootstrap-icons.css"]
                link [_rel "stylesheet";_href "/css/highlight-dark.min.css"] //10.5.0
                link [_rel "stylesheet";_href "/css/main.css"]
                

                meta [_name "viewport"; _content "width=device-width, initial-scale=1, shrink-to-fit=no"]
                meta [_property "og:title"; _content pageTitle]
                meta [_property "og:type"; _content "website"]
                meta [_property "og:image"; _content "https://www.luisquintanilla.me/avatar.png"]
                meta [_property "og:image:secure_url"; _content "https://www.luisquintanilla.me/avatar.png"]
                meta [_property "og:image:type"; _content "image/png"]
                meta [_property "og:image:width"; _content "200"]
                meta [_property "og:image:height"; _content "200"] 
                meta [_property "og:site_name"; _content "Luis Quintanilla Personal Website"]
                meta [_property "og:locale"; _content "en_US"]
                meta [_name "robots"; _content "noindex,nofollow,nosnippet"]
                title [] [Text pageTitle]
            ]
            body [] [
                defaultNavBar
                main [attr "role" "main"; _class "container"] [
                    rawText content
                ]
                script [_src "/js/jquery.slim.min.js"] [] // 3.5.1
                script [_src "/js/bootstrap.min.js"] [] // 4.6.0
                script [_src "/js/highlight.min.js"] [] // 10.5.0
                script [_src "/js/highlight.fsharp.min.js"] []  // 10.5.0
                script [_type "application/javascript"] [
                    rawText "hljs.initHighlightingOnLoad();"
                ]
            ]
            footer [] [
                a [_rel "me"; _href "https://toot.lqdev.tech/@lqdev"] []
            ]
        ]