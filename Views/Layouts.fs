module Layouts

    open Giraffe.ViewEngine
  
    let defaultNavBar = 
        nav [_class "navbar navbar-expand-md navbar-dark fixed-top bg-dark"] [
            a [_class "navbar-brand"; _href "#"] [ Text "Luis Quintanilla" ]
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
                        a [_class "nav-link"; _href "/posts/1/1.html"] [ Text "Writing" ]
                    ]
                    li [_class "nav-item"] [
                        a [_class "nav-link"; _href "#"] [ Text "Events" ]
                    ]                                        
                ]
            ]
        ]

    let defaultLayout (pageTitle:string) (content:string) =
        html [_lang "en"] [
            head [] [
                link [_rel "stylesheet";_href "https://cdn.jsdelivr.net/npm/bootstrap@4.6.0/dist/css/bootstrap.min.css"]
                link [_rel "stylesheet";_href "https://cdnjs.cloudflare.com/ajax/libs/highlight.js/10.5.0/styles/dark.min.css"]
                link [_rel "stylesheet";_href "/main.css"]
              
                meta [_name "viewport"; _content "width=device-width, initial-scale=1, shrink-to-fit=no"]
                title [] [Text pageTitle]
            ]
            body [] [
                defaultNavBar
                main [attr "role" "main"; _class "container"] [
                    rawText content
                ]
                script [_src "https://code.jquery.com/jquery-3.5.1.slim.min.js"] []
                script [_src "https://cdn.jsdelivr.net/npm/bootstrap@4.6.0/dist/js/bootstrap.min.js"] []
                script [_src "https://cdnjs.cloudflare.com/ajax/libs/highlight.js/10.5.0/highlight.min.js"] [] 
                script [_src "https://cdnjs.cloudflare.com/ajax/libs/highlight.js/10.5.0/languages/fsharp.min.js"] []  
                script [_type "application/javascript"] [
                    rawText "hljs.initHighlightingOnLoad();"
                ]
            ]
        ]