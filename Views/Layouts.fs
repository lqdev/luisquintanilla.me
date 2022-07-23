module Layouts

    open Giraffe.ViewEngine
  
    let defaultNavBar = 
        nav [_class "navbar navbar-expand-md navbar-dark fixed-top bg-dark"] [
            a [_class "navbar-brand"; _href "/"] [ 
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


                    // About dropdown
                    li [_class "nav-item dropdown"] [
                        a [
                            _class "nav-link dropdown-toggle"
                            _href "#"
                            _id "aboutDropdown"
                            attr "role" "button"
                            attr "data-toggle" "dropdown"
                            attr "aria-haspopup" "true"
                            attr "aria-expanded" "false"
                        ] [
                            Text "About"
                        ]
                        div [
                            _class "dropdown-menu"
                            attr "aria-labelledby" "aboutDropdown"
                            ] [
                                a [_class "dropdown-item"; _href "/about.html"] [Text "About Me"]
                                a [_class "dropdown-item"; _href "/irl-stack.html"] [Text "IRL Stack"]
                                a [_class "dropdown-item"; _href "/colophon.html"] [Text "Colophon"]
                        ]
                    ]

                    li [_class "nav-item"] [
                        a [_class "nav-link"; _href "/contact.html"] [ Text "Contact" ]
                    ]

                    // Feeds dropdown
                    li [_class "nav-item dropdown"] [
                        a [
                            _class "nav-link dropdown-toggle"
                            _href "#"
                            _id "feedDropdown"
                            attr "role" "button"
                            attr "data-toggle" "dropdown"
                            attr "aria-haspopup" "true"
                            attr "aria-expanded" "false"
                        ] [
                            Text "Feeds"
                        ]
                        div [
                            _class "dropdown-menu"
                            attr "aria-labelledby" "feedDropdown"
                            ] [
                                a [_class "dropdown-item"; _href "/feed/index.html"] [Text "Main"]
                                a [_class "dropdown-item"; _href "/feed/notes.html"] [Text "Notes"]
                                a [_class "dropdown-item"; _href "/feed/photos.html"] [Text "Photos"]                                
                                a [_class "dropdown-item"; _href "/feed/videos.html"] [Text "Videos"]
                                div [_class "dropdown-divider"] []
                                a [_class "dropdown-item"; _href "/feed/blogroll.html"] [Text "Blogroll"]
                                a [_class "dropdown-item"; _href "/feed/podroll.html"] [Text "Podroll"]
                        ]
                    ]

                    li [_class "nav-item"] [
                        a [_class "nav-link"; _href "/posts/1"] [ Text "Blog" ]
                    ]
                    
                    li [_class "nav-item"] [
                        a [_class "nav-link"; _href "/subscribe.html"] [ Text "Subscribe" ]
                    ]

                    li [_class "nav-item"] [
                        a [_class "nav-link"; _href "/radio.html"] [ Text "Radio" ]
                    ]

                    //Events dropdown
                    li [_class "nav-item dropdown"] [
                        a [
                            _class "nav-link dropdown-toggle"
                            _href "#"
                            _id "eventsDropdown"
                            attr "role" "button"
                            attr "data-toggle" "dropdown"
                            attr "aria-haspopup" "true"
                            attr "aria-expanded" "false"
                        ] [
                            Text "Events"
                        ]
                        div [
                            _class "dropdown-menu"
                            attr "aria-labelledby" "eventsDropdown"
                            ] [
                                a [_class "nav-link"; _href "/events.html"] [ Text "Event List" ]
                                a [_class "nav-link"; _href "/presentations.html"] [ Text "Presentations" ]
                        ]
                    ]
                ]

                a [_href "/subscribe.html"] [
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

    let stats = script [ _async; attr "data-goatcounter" "https://stats.luisquintanilla.me/count"; _src "//stats.luisquintanilla.me/count.js" ] []
    
    let styleSheets = [
        link [_rel "stylesheet";_href "/css/bootstrap.min.css"] //4.6.0
        link [_rel "stylesheet";_href "/css/bootstrap-icons-1.5.0/bootstrap-icons.css"]
        link [_rel "stylesheet";_href "/css/highlight-dark.min.css"] //10.5.0
        link [_rel "stylesheet";_href "/css/main.css"]        
    ]

    let rssFeeds = [
        link [_rel "alternate"; _type "application/rss+xml" ; _title "Luis Quintanilla Blog RSS Feed"; _href "https://www.luisquintanilla.me/posts/index.xml"]
        link [_rel "alternate"; _type "application/rss+xml" ; _title "Luis Quintanilla Main Feed (Microblog) RSS"; _href "https://www.luisquintanilla.me/feed/index.xml"]
        link [_rel "alternate"; _type "application/rss+xml" ; _title "Luis Quintanilla Notes Feed RSS"; _href "https://www.luisquintanilla.me/feed/notes.xml"]
        link [_rel "alternate"; _type "application/rss+xml" ; _title "Luis Quintanilla Photos Feed RSS"; _href "https://www.luisquintanilla.me/feed/photos.xml"]        
        link [_rel "alternate"; _type "application/rss+xml" ; _title "Luis Quintanilla Videos Feed RSS"; _href "https://www.luisquintanilla.me/feed/videos.xml"]
    ]

    let defaultLayout (pageTitle:string) (content:string) =
        html [_lang "en"] [
            head [] [
                meta [_charset "UTF-8"]    
                meta [_name "viewport"; _content "width=device-width, initial-scale=1, shrink-to-fit=no"]

                stats

                // Stylesheets
                for sheet in styleSheets do
                    sheet

                // Opengraph
                meta [_property "og:title"; _content pageTitle]
                meta [_property "og:type"; _content "website"]
                meta [_property "og:image"; _content "https://www.luisquintanilla.me/avatar.png"]
                meta [_property "og:image:secure_url"; _content "https://www.luisquintanilla.me/avatar.png"]
                meta [_property "og:image:type"; _content "image/png"]
                meta [_property "og:image:width"; _content "200"]
                meta [_property "og:image:height"; _content "200"] 
                meta [_property "og:site_name"; _content "Luis Quintanilla Personal Website"]
                meta [_property "og:locale"; _content "en_US"]
                meta [_property "twitter:image"; _content "https://www.luisquintanilla.me/avatar.png"]

                // RSS Feeds
                for feed in rssFeeds do
                    feed

                // Robots
                meta [_name "robots"; _content "noindex,nofollow,nosnippet"]
                title [] [Text pageTitle]
            ]
            body [] [
                defaultNavBar

                main [attr "role" "main"; _class "container"] [
                    rawText content
                ]
                script [_src "/lib/jquery/jquery.slim.min.js"] [] // 3.5.1
                script [_src "/lib/boostrap/bootstrap.min.js"] [] // 4.6.0
                script [_src "/lib/highlight/highlight.min.js"] [] // 10.5.0
                script [_src "/lib/highlight/highlight.fsharp.min.js"] [] // 10.5.0
                
                script [_type "application/javascript"] [
                    rawText "hljs.initHighlightingOnLoad();"
                ]   
            ]
            footer [] [
                a [_rel "me"; _href "https://toot.lqdev.tech/@lqdev"] []
            ]
        ]

    let presentationLayout (pageTitle:string) (content:string) =
        html [_lang "en"] [
            head [] [
                meta [_charset "UTF-8"]    
                meta [_name "viewport"; _content "width=device-width, initial-scale=1, shrink-to-fit=no"]

                stats

                // Stylesheets
                for sheet in styleSheets do
                    sheet

                link [_rel "stylesheet"; _href "/lib/revealjs/dist/reveal.css"]
                link [_rel "stylesheet"; _href "/lib/revealjs/dist/theme/black.css"]

                // Opengraph
                meta [_property "og:title"; _content pageTitle]
                meta [_property "og:type"; _content "website"]
                meta [_property "og:image"; _content "https://www.luisquintanilla.me/avatar.png"]
                meta [_property "og:image:secure_url"; _content "https://www.luisquintanilla.me/avatar.png"]
                meta [_property "og:image:type"; _content "image/png"]
                meta [_property "og:image:width"; _content "200"]
                meta [_property "og:image:height"; _content "200"] 
                meta [_property "og:site_name"; _content "Luis Quintanilla Personal Website"]
                meta [_property "og:locale"; _content "en_US"]
                meta [_property "twitter:image"; _content "https://www.luisquintanilla.me/avatar.png"]
                
                // RSS Feeds
                for feed in rssFeeds do
                    feed
                
                // Robots                
                meta [_name "robots"; _content "noindex,nofollow,nosnippet"]
                
                title [] [Text pageTitle]
            ]
            body [] [
                defaultNavBar

                main [attr "role" "main"; _class "container"] [
                    rawText content
                ]
                script [_src "/lib/jquery/jquery.slim.min.js"] [] // 3.5.1
                script [_src "/lib/boostrap/bootstrap.min.js"] [] // 4.6.0
                script [_src "/lib/highlight/highlight.min.js"] [] // 10.5.0
                script [_src "/lib/highlight/highlight.fsharp.min.js"] [] // 10.5.0
                
                script [_type "application/javascript"] [
                    rawText "hljs.initHighlightingOnLoad();"
                ]

                // Revealjs (As of 10/20/2021)
                script [_src "/lib/revealjs/dist/reveal.js"] []
                script [_src "/lib/revealjs/plugin/markdown/markdown.js"] []
                script [_type "application/javascript"] [
                    rawText """
                    Reveal.initialize({
                        plugins: [ RevealMarkdown ],
                        embedded: true
                    });
                    """
                ]   
            ]
            footer [] [
                a [_rel "me"; _href "https://toot.lqdev.tech/@lqdev"] []
            ]
        ]