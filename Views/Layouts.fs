module Layouts

    open Giraffe.ViewEngine
  
    let defaultNavBar = 
        nav [_class "navbar navbar-expand-md navbar-dark fixed-top bg-dark"] [
            a [_class "navbar-brand"; _href "/"] [ 
                img [_src "/art-profile.png"; _height "32"; _width "32"; _class "d-inline-block align-top rounded-circle"; _style "margin-right:5px"; attr "loading" "lazy"]
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
                                a [_class "dropdown-item"; _href "/about"] [Text "Profile"]
                                a [_class "dropdown-item"; _href "/irl-stack"] [Text "IRL Stack"]
                                a [_class "dropdown-item"; _href "/colophon"] [Text "Colophon"]
                        ]
                    ]

                    li [_class "nav-item"] [
                        a [_class "nav-link"; _href "/contact"] [ Text "Contact" ]
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
                                a [_class "dropdown-item"; _href "/feed/responses.html"] [Text "Responses"]                                
                                div [_class "dropdown-divider"] []
                                a [_class "dropdown-item"; _href "/feed/blogroll"] [Text "Blogroll"]
                                a [_class "dropdown-item"; _href "/feed/podroll"] [Text "Podroll"]
                        ]
                    ]

                    li [_class "nav-item"] [
                        a [_class "nav-link"; _href "/posts/1"] [ Text "Blog" ]
                    ]
                    
                    li [_class "nav-item"] [
                        a [_class "nav-link"; _href "/subscribe"] [ Text "Subscribe" ]
                    ]

                    li [_class "nav-item"] [
                        a [_class "nav-link"; _href "/radio"] [ Text "Radio" ]
                    ]


                    //Knowledgebase dropdown
                    li [_class "nav-item dropdown"] [
                        a [
                            _class "nav-link dropdown-toggle"
                            _href "#"
                            _id "kbDropdown"
                            attr "role" "button"
                            attr "data-toggle" "dropdown"
                            attr "aria-haspopup" "true"
                            attr "aria-expanded" "false"
                        ] [
                            Text "KB"
                        ]
                        div [
                            _class "dropdown-menu"
                            attr "aria-labelledby" "kbDropdown"
                            ] [
                                a [_class "dropdown-item"; _href "/snippets"] [ Text "Snippets" ]
                                a [_class "dropdown-item"; _href "/wiki"] [ Text "Wiki" ]
                                a [_class "dropdown-item"; _href "/library"] [ Text "Library" ]
                                a [_class "dropdown-item"; _href "/presentations"] [ Text "Presentations" ]
                        ]
                    ]
                    
                    li [_class "nav-item"] [
                        a [_class "nav-link"; _href "/events"] [ Text "Events" ]
                    ]                    
                ]

                span [
                    _href "#"
                    _onclick "switchTheme()"
                    ] [
                    tag "svg" [
                        _class "bi bi-brightness-high-fill" 
                        attr "fill" "currentColor"
                        attr "viewBox" "0 0 16 16"
                        _height "32"
                        _width "32"] [
                        
                        tag "path" [attr "d" "M12 8a4 4 0 1 1-8 0 4 4 0 0 1 8 0zM8 0a.5.5 0 0 1 .5.5v2a.5.5 0 0 1-1 0v-2A.5.5 0 0 1 8 0zm0 13a.5.5 0 0 1 .5.5v2a.5.5 0 0 1-1 0v-2A.5.5 0 0 1 8 13zm8-5a.5.5 0 0 1-.5.5h-2a.5.5 0 0 1 0-1h2a.5.5 0 0 1 .5.5zM3 8a.5.5 0 0 1-.5.5h-2a.5.5 0 0 1 0-1h2A.5.5 0 0 1 3 8zm10.657-5.657a.5.5 0 0 1 0 .707l-1.414 1.415a.5.5 0 1 1-.707-.708l1.414-1.414a.5.5 0 0 1 .707 0zm-9.193 9.193a.5.5 0 0 1 0 .707L3.05 13.657a.5.5 0 0 1-.707-.707l1.414-1.414a.5.5 0 0 1 .707 0zm9.193 2.121a.5.5 0 0 1-.707 0l-1.414-1.414a.5.5 0 0 1 .707-.707l1.414 1.414a.5.5 0 0 1 0 .707zM4.464 4.465a.5.5 0 0 1-.707 0L2.343 3.05a.5.5 0 1 1 .707-.707l1.414 1.414a.5.5 0 0 1 0 .708z"] []

                    ]
                ]


                a [_href "/subscribe"] [
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
        link [_rel "stylesheet";_href "/css/customthemes.css"]
    ]

    let buildOpenGraphElements (pageTitle:string)= 
        let ogElements = [
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
            
        ]

        ogElements

    let rssFeeds = [
        link [_rel "alternate"; _type "application/rss+xml" ; _title "Luis Quintanilla Blog RSS Feed"; _href "https://www.luisquintanilla.me/posts/index.xml"]
        link [_rel "alternate"; _type "application/rss+xml" ; _title "Luis Quintanilla Main Feed (Microblog) RSS"; _href "https://www.luisquintanilla.me/feed/index.xml"]
        link [_rel "alternate"; _type "application/rss+xml" ; _title "Luis Quintanilla Notes Feed RSS"; _href "https://www.luisquintanilla.me/feed/notes.xml"]
        link [_rel "alternate"; _type "application/rss+xml" ; _title "Luis Quintanilla Photos Feed RSS"; _href "https://www.luisquintanilla.me/feed/photos.xml"]        
        link [_rel "alternate"; _type "application/rss+xml" ; _title "Luis Quintanilla Videos Feed RSS"; _href "https://www.luisquintanilla.me/feed/videos.xml"]
    ]

    let scripts = [
        script [_src "/lib/jquery/jquery.slim.min.js"] [] // 3.5.1
        script [_src "/lib/boostrap/bootstrap.min.js"] [] // 4.6.0
        script [_src "/lib/highlight/highlight.min.js"] [] // 10.5.0
        script [_src "/lib/highlight/highlight.fsharp.min.js"] [] // 10.5.0

        script [_src "/js/main.js"] []    

        script [_src "https://cdn.jsdelivr.net/npm/mermaid/dist/mermaid.min.js"] []

        script [_type "application/javascript"] [
            rawText "mermaid.initialize({startOnLoad:true});"
        ]

        script [_type "application/javascript"] [
            rawText "hljs.initHighlightingOnLoad();"
        ]
    ]

    let footerContent = 
        footer [] [
            a [_rel "me"; _href "https://toot.lqdev.tech/@lqdev"] []
            a [_rel "me"; _href "https://github.com/lqdev"] []
            a [_rel "me"; _href "https://twitter.com/ljquintanilla"] []      
            a [_rel "me"; _href "https://www.linkedin.com/in/lquintanilla01/"] []
            a [_rel "me"; _href "mailto:lqdev@outlook.com"] []            
        ]

    let redirectLayout (pageUrl:string) (pageTitle:string) = 
        html [_lang "en"] [
            head [] [
                meta [
                    attr "http-equiv" "refresh"
                    _content $"0; url={pageUrl}"
                ]
                
                // Robots
                meta [_name "robots"; _content "nosnippet"]
                title [] [Text pageTitle]]
            body [] []
            
            footerContent
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
                let ogElements = buildOpenGraphElements pageTitle

                for el in ogElements do
                    el

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

                for scr in scripts do
                    scr

            ]
            footerContent
        ]

    // Similar to default layout but allows indexing by search engines
    let defaultIndexedLayout (pageTitle:string) (content:string) =
        html [_lang "en"] [
            head [] [
                meta [_charset "UTF-8"]    
                meta [_name "viewport"; _content "width=device-width, initial-scale=1, shrink-to-fit=no"]

                stats

                // Stylesheets
                for sheet in styleSheets do
                    sheet

                // Opengraph
                let ogElements = buildOpenGraphElements pageTitle

                for el in ogElements do
                    el

                // RSS Feeds
                for feed in rssFeeds do
                    feed

                // Robots
                meta [_name "robots"; _content "nofollow,nosnippet"]
                title [] [Text pageTitle]
            ]
            body [] [
                defaultNavBar

                main [attr "role" "main"; _class "container"] [
                    rawText content
                ]

                for scr in scripts do
                    scr

            ]
            footerContent
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
                let ogElements = buildOpenGraphElements pageTitle

                for el in ogElements do
                    el
                                
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

                for scr in scripts do
                    scr

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
            footerContent
        ]