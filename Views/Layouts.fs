module Layouts

    open Giraffe.ViewEngine
  
    let defaultNavBar = 
        nav [_class "navbar navbar-expand-md navbar-dark fixed-top bg-dark"] [
            a [_class "navbar-brand"; _href "#"] [ 
                img [_src "/avatar.png"; _height "30"; _width "30"; _class "d-inline-block align-top rounded-circle"; _style "margin-right:5px"]
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
            ]
        ]

    let defaultLayout (pageTitle:string) (content:string) =
        html [_lang "en"] [
            head [] [
                link [_rel "stylesheet";_href "/css/bootstrap.min.css"] //4.6.0
                link [_rel "stylesheet";_href "/css/highlight-dark.min.css"] //10.5.0
                link [_rel "stylesheet";_href "/css/main.css"]
               
                meta [_name "viewport"; _content "width=device-width, initial-scale=1, shrink-to-fit=no"]
                meta [_property "og:title"; _content pageTitle]
                meta [_property "og:type"; _content "website"]
                meta [_property "og:image"; _content "/favicon.ico"]
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
        ]