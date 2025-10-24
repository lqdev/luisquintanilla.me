module Layouts

    open Giraffe.ViewEngine
  
    let desertNavigation = 
        [
            // Mobile toggle button (hidden on desktop)
            button [
                _class "mobile-toggle"
                _id "mobile-nav-toggle"
                attr "aria-label" "Toggle navigation menu"
                attr "aria-expanded" "false"
            ] [
                div [_class "hamburger"] [
                    span [] []
                    span [] []
                    span [] []
                ]
            ]
            
            // Navigation overlay for mobile
            div [_class "nav-overlay"; _id "nav-overlay"] []
            
            // Always-visible navigation sidebar (desktop) / slide-out (mobile)
            nav [_class "desert-nav"; _id "sidebar-menu"; attr "role" "navigation"; attr "aria-label" "Main navigation"] [
                // Brand/Header
                div [_class "nav-brand"] [
                    a [_href "/"; _class "brand-text"] [
                        img [_src "/avatar.png"; _alt "Luis Quintanilla avatar"; attr "loading" "lazy"]
                        Text "Luis Quintanilla"
                    ]
                ]
                
                // Main Navigation with explicit Home button
                div [_class "nav-section"] [
                    a [_class "nav-link"; _href "/"] [
                        tag "svg" [_class "nav-icon"; attr "viewBox" "0 0 16 16"; attr "fill" "currentColor"] [
                            tag "path" [attr "d" "M8.354 1.146a.5.5 0 0 0-.708 0l-6 6A.5.5 0 0 0 1.5 7.5v7a.5.5 0 0 0 .5.5h4.5a.5.5 0 0 0 .5-.5v-4h2v4a.5.5 0 0 0 .5.5H14a.5.5 0 0 0 .5-.5v-7a.5.5 0 0 0-.146-.354L8.354 1.146zM2.5 14V7.707l5.5-5.5 5.5 5.5V14H10v-4a.5.5 0 0 0-.5-.5h-3a.5.5 0 0 0-.5.5v4H2.5z"] []
                        ]
                        Text "Home"
                    ]
                    a [_class "nav-link"; _href "/about"] [
                        tag "svg" [_class "nav-icon"; attr "viewBox" "0 0 16 16"; attr "fill" "currentColor"] [
                            tag "path" [attr "d" "M8 15A7 7 0 1 1 8 1a7 7 0 0 1 0 14zm0 1A8 8 0 1 0 8 0a8 8 0 0 0 0 16z"] []
                            tag "path" [attr "d" "m8.93 6.588-2.29.287-.082.38.45.083c.294.07.352.176.288.469l-.738 3.468c-.194.897.105 1.319.808 1.319.545 0 1.178-.252 1.465-.598l.088-.416c-.2.176-.492.246-.686.246-.275 0-.375-.193-.304-.533L8.93 6.588zM9 4.5a1 1 0 1 1-2 0 1 1 0 0 1 2 0z"] []
                        ]
                        Text "About"
                    ]
                    a [_class "nav-link"; _href "/contact"] [
                        tag "svg" [_class "nav-icon"; attr "viewBox" "0 0 16 16"; attr "fill" "currentColor"] [
                            tag "path" [attr "d" "M0 4a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2V4Zm2-1a1 1 0 0 0-1 1v.217l7 4.2 7-4.2V4a1 1 0 0 0-1-1H2Zm13 2.383-4.708 2.825L15 11.105V5.383Zm-.034 6.876-5.64-3.471L8 9.583l-1.326-.795-5.64 3.47A1 1 0 0 0 2 13h12a1 1 0 0 0 .966-.741ZM1 11.105l4.708-2.897L1 5.383v5.722Z"] []
                        ]
                        Text "Contact"
                    ]
                    a [_class "nav-link"; _href "/search"] [
                        tag "svg" [_class "nav-icon"; attr "viewBox" "0 0 16 16"; attr "fill" "currentColor"] [
                            tag "path" [attr "d" "M11.742 10.344a6.5 6.5 0 1 0-1.397 1.398h-.001c.03.04.062.078.098.115l3.85 3.85a1 1 0 0 0 1.415-1.414l-3.85-3.85a1.007 1.007 0 0 0-.115-.1zM12 6.5a5.5 5.5 0 1 1-11 0 5.5 5.5 0 0 1 11 0z"] []
                        ]
                        Text "Search"
                    ]
                    a [_class "nav-link"; _href "/feed"] [
                        tag "svg" [_class "nav-icon"; attr "viewBox" "0 0 16 16"; attr "fill" "currentColor"] [
                            tag "path" [attr "d" "M5.5 12a1.5 1.5 0 1 1-3 0 1.5 1.5 0 0 1 3 0zm-3-8.5a1 1 0 0 1 1-1c5.523 0 10 4.477 10 10a1 1 0 1 1-2 0 8 8 0 0 0-8-8 1 1 0 0 1-1-1zm0 4a1 1 0 0 1 1-1 6 6 0 0 1 6 6 1 1 0 1 1-2 0 4 4 0 0 0-4-4 1 1 0 0 1-1-1z"] []
                        ]
                        Text "Subscribe"
                    ]
                ]

                // Collections Dropdown
                div [_class "nav-section dropdown"] [
                    button [_class "nav-link dropdown-toggle"; attr "data-target" "collections-dropdown"] [
                        tag "svg" [_class "nav-icon"; attr "viewBox" "0 0 16 16"; attr "fill" "currentColor"] [
                            tag "path" [attr "d" "M2.5 3A1.5 1.5 0 0 0 1 4.5v.793c.026.009.051.02.076.032L7.674 8.51c.206.1.446.1.652 0l6.598-3.185A.755.755 0 0 1 15 5.293V4.5A1.5 1.5 0 0 0 13.5 3h-11Z"] []
                            tag "path" [attr "d" "M15 6.954 8.978 9.86a2.25 2.25 0 0 1-1.956 0L1 6.954V11.5A1.5 1.5 0 0 0 2.5 13h11a1.5 1.5 0 0 0 1.5-1.5V6.954Z"] []
                        ]
                        Text "Collections"
                        tag "svg" [_class "dropdown-arrow"; attr "viewBox" "0 0 16 16"; attr "fill" "currentColor"] [
                            tag "path" [attr "d" "M7.247 11.14 2.451 5.658C1.885 5.013 2.345 4 3.204 4h9.592a1 1 0 0 1 .753 1.659l-4.796 5.48a1 1 0 0 1-1.506 0z"] []
                        ]
                    ]
                    div [_class "dropdown-menu"; _id "collections-dropdown"] [
                        // Rolls (Content-Type) with spacing
                        a [_class "dropdown-item"; _href "/collections/blogroll"] [Text "Blogroll"]
                        a [_class "dropdown-item"; _href "/collections/podroll"] [Text "Podroll"]
                        a [_class "dropdown-item"; _href "/collections/youtube"] [Text "YouTube"]
                        a [_class "dropdown-item"; _href "/collections/forums"] [Text "Forums"]
                        div [_class "dropdown-divider"] []
                        // Starter Packs as direct link (like Radio/Tags)
                        a [_class "dropdown-item"; _href "/collections/starter-packs"] [Text "Starter Packs"]
                        a [_class "dropdown-item"; _href "/collections/travel-guides"] [Text "Travel Guides"]
                        div [_class "dropdown-divider"] []
                        // Media Collections
                        a [_class "dropdown-item"; _href "/collections/albums"] [Text "Albums"]
                        a [_class "dropdown-item"; _href "/collections/playlists"] [Text "Playlists"]
                        div [_class "dropdown-divider"] []
                        a [_class "dropdown-item"; _href "/radio"] [Text "Radio"]
                        a [_class "dropdown-item"; _href "/tags"] [Text "Tags"]
                    ]
                ]

                // Resources Dropdown  
                div [_class "nav-section dropdown"] [
                    button [_class "nav-link dropdown-toggle"; attr "data-target" "resources-dropdown"] [
                        tag "svg" [_class "nav-icon"; attr "viewBox" "0 0 16 16"; attr "fill" "currentColor"] [
                            tag "path" [attr "d" "M1 2.828c.885-.37 2.154-.769 3.388-.893 1.33-.134 2.458.063 3.112.752v9.746c-.935-.53-2.12-.603-3.213-.493-1.18.12-2.37.461-3.287.811V2.828zm7.5-.141c.654-.689 1.782-.886 3.112-.752 1.234.124 2.503.523 3.388.893v9.923c-.918-.35-2.107-.692-3.287-.81-1.094-.111-2.278-.039-3.213.492V2.687zM8 1.783C7.015.936 5.587.81 4.287.94c-1.514.153-3.042.672-3.994 1.105A.5.5 0 0 0 0 2.5v11a.5.5 0 0 0 .707.455c.882-.4 2.303-.881 3.68-1.02 1.409-.142 2.59.087 3.223.877a.5.5 0 0 0 .78 0c.633-.79 1.814-1.019 3.222-.877 1.378.139 2.8.62 3.681 1.02A.5.5 0 0 0 16 13.5v-11a.5.5 0 0 0-.293-.455c-.952-.433-2.48-.952-3.994-1.105C10.413.809 8.985.936 8 1.783z"] []
                        ]
                        Text "Resources"
                        tag "svg" [_class "dropdown-arrow"; attr "viewBox" "0 0 16 16"; attr "fill" "currentColor"] [
                            tag "path" [attr "d" "M7.247 11.14 2.451 5.658C1.885 5.013 2.345 4 3.204 4h9.592a1 1 0 0 1 .753 1.659l-4.796 5.48a1 1 0 0 1-1.506 0z"] []
                        ]
                    ]
                    div [_class "dropdown-menu"; _id "resources-dropdown"] [
                        a [_class "dropdown-item"; _href "/resources/snippets"] [Text "Snippets"]
                        a [_class "dropdown-item"; _href "/resources/wiki"] [Text "Wiki"]
                        a [_class "dropdown-item"; _href "/resources/presentations"] [Text "Presentations"]
                    ]
                ]
                
                // Theme Toggle
                div [_class "theme-toggle"] [
                    button [
                        _class "theme-toggle-btn"
                        _id "theme-toggle"
                        attr "aria-label" "Toggle dark mode"
                    ] [
                        span [_id "theme-toggle-icon"] [Text "☀️"]
                        span [] [Text "Theme"]
                    ]
                ]
            ]
        ]
    
    let defaultNavBar = 
        nav [_class "navbar navbar-expand-md navbar-dark fixed-top bg-dark"] [
            a [_class "navbar-brand"; _href "/"] [ 
                img [_src "/avatar.png"; _height "32"; _width "32"; _class "d-inline-block align-top rounded-circle"; _style "margin-right:5px"; attr "loading" "lazy"]
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
                                a [_class "dropdown-item"; _href "/contact"] [ Text "Contact" ]
                                a [_class "dropdown-item"; _href "/uses"] [Text "Uses"]
                                a [_class "dropdown-item"; _href "/colophon"] [Text "Colophon"]
                        ]
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
                                a [_class "dropdown-item"; _href "/feed"] [Text "Main"]
                                a [_class "dropdown-item"; _href "/feed/responses"] [Text "Responses"]
                                a [_class "dropdown-item"; _href "/posts/1"] [ Text "Blog" ]
                                div [_class "dropdown-divider"] []
                                a [_class "dropdown-item"; _href "/feed"] [ Text "Subscribe" ]
                        ]
                    ]
                    
                    //Collections dropdown
                    li [_class "nav-item dropdown"] [
                        a [
                            _class "nav-link dropdown-toggle"
                            _href "#"
                            _id "collectionDropdown"
                            attr "role" "button"
                            attr "data-toggle" "dropdown"
                            attr "aria-haspopup" "true"
                            attr "aria-expanded" "false"
                        ] [
                            Text "Collections"
                        ]
                        div [
                            _class "dropdown-menu"
                            attr "aria-labelledby" "collectionDropdown"
                            ] [
                                a [_class "dropdown-item"; _href "/radio"] [ Text "Radio" ]
                                a [_class "dropdown-item"; _href "/reviews"] [ Text "Books" ]
                                a [_class "dropdown-item"; _href "/tags"] [ Text "Tags" ]
                                div [_class "dropdown-divider"] []
                                a [_class "dropdown-item"; _href "/collections/starter-packs"] [Text "Starter Packs"]
                                a [_class "dropdown-item"; _href "/collections/travel-guides"] [Text "Travel Guides"]
                                a [_class "dropdown-item"; _href "/collections/blogroll"] [Text "Blogroll"]
                                a [_class "dropdown-item"; _href "/collections/podroll"] [Text "Podroll"]
                                a [_class "dropdown-item"; _href "/collections/forums"] [Text "Forums"]
                                a [_class "dropdown-item"; _href "/collections/youtube"] [Text "YouTube"]
                            ]
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
                            Text "Knowledgebase"
                        ]
                        div [
                            _class "dropdown-menu"
                            attr "aria-labelledby" "kbDropdown"
                            ] [
                                a [_class "dropdown-item"; _href "/resources/snippets"] [ Text "Snippets" ]
                                a [_class "dropdown-item"; _href "/resources/wiki"] [ Text "Wiki" ]
                                a [_class "dropdown-item"; _href "/resources/presentations"] [ Text "Presentations" ]
                        ]
                    ]

                    //Livestream dropdown
                    li [_class "nav-item dropdown"] [
                        a [
                            _class "nav-link dropdown-toggle"
                            _href "#"
                            _id "liveDropdown"
                            attr "role" "button"
                            attr "data-toggle" "dropdown"
                            attr "aria-haspopup" "true"
                            attr "aria-expanded" "false"
                        ] [
                            Text "Live"
                        ]
                        div [
                            _class "dropdown-menu"
                            attr "aria-labelledby" "liveDropdown"
                            ] [
                                a [_class "dropdown-item"; _href "/live"] [ Text "Stream" ]
                                a [_class "dropdown-item"; _href "/streams"] [ Text "Recordings" ]
                            ]
                    ]

                    li [_class "nav-item"] [
                        a [_class "nav-link"; _href "/events"] [ Text "Events" ]
                    ]

                 ]

                a [_href "/feed"] [
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
    
    let styleSheets = [
        // Desert Theme CSS - Custom design system (Phase 1)
        link [_rel "stylesheet";_href "/assets/css/custom/main.css"]
        link [_rel "stylesheet";_href "/assets/css/custom/timeline.css"]  // Timeline styles for feed-as-homepage
        
        // Keep essential external stylesheets
        link [_rel "stylesheet";_href "/assets/css/bootstrap-icons-1.5.0/bootstrap-icons.css"]
        link [_rel "stylesheet";_href "/assets/css/highlight.github-dark-dimmed.min.css"] //11.8.0
        
        // Preserve existing custom styles during transition
        link [_rel "stylesheet";_href "/assets/css/main.css"]
        
        // Web API enhancements
        link [_rel "stylesheet";_href "/assets/css/custom/permalink-buttons.css"]  // Permalink action buttons
        link [_rel "stylesheet";_href "/assets/css/custom/clipboard.css"]  // Code copy buttons
        link [_rel "stylesheet";_href "/assets/css/custom/share.css"]      // Content sharing
        link [_rel "stylesheet";_href "/assets/css/custom/qrcode.css"]     // QR code modal
        link [_rel "stylesheet";_href "/assets/css/pwa.css"]              // PWA notifications and install prompts
        
        // Note: Bootstrap removed in Phase 1 - replaced with desert theme CSS
        // Note: customthemes.css removed - functionality integrated into custom CSS
    ]

    let buildOpenGraphElements (pageTitle:string)= 
        let ogElements = [
                meta [_property "og:title"; _content pageTitle]
                meta [_property "og:type"; _content "website"]
                meta [_property "og:image"; _content "https://www.lqdev.me/avatar.png"]
                meta [_property "og:image:secure_url"; _content "https://www.lqdev.me/avatar.png"]
                meta [_property "og:image:type"; _content "image/png"]
                meta [_property "og:image:width"; _content "200"]
                meta [_property "og:image:height"; _content "200"] 
                meta [_property "og:site_name"; _content "Luis Quintanilla Personal Website"]
                meta [_property "og:locale"; _content "en_US"]
                meta [_property "twitter:image"; _content "https://www.lqdev.me/avatar.png"]
                meta [_property "fediverse:creator"; _content "@lqdev@toot.lqdev.tech"]
            
        ]

        ogElements

    let rssFeeds = [
        link [_rel "alternate"; _type "application/rss+xml" ; _title "Luis Quintanilla Blog RSS Feed"; _href "/blog.rss"]
        link [_rel "alternate"; _type "application/rss+xml" ; _title "Luis Quintanilla Microblog RSS Feed"; _href "/microblog.rss"]
        link [_rel "alternate"; _type "application/rss+xml" ; _title "Luis Quintanilla Response RSS Feed"; _href "/responses.rss"]
    ]

    let webmentionLink = 
        link [_rel "webmention"; _title "Luis Quintanilla Webmention Endpoint"; _href "https://webmentions.lqdev.tech/api/inbox"]


    let rollLinks = [
        link [_rel "feeds"; _type "text/xml" ; _title "Luis Quintanilla's Feeds"; _href "/feed/index.opml"]
        link [_rel "blogroll"; _type "text/xml" ; _title "Luis Quintanilla's Blogroll"; _href "/collections/blogroll/index.opml"]
        link [_rel "podroll"; _type "text/xml" ; _title "Luis Quintanilla's Podroll"; _href "/collections/podroll/index.opml"]
        link [_rel "youtuberoll"; _type "text/xml" ; _title "Luis Quintanilla's YouTube Roll"; _href "/collections/youtube/index.opml"]
    ]

    let scripts = [
        script [_src "/assets/lib/jquery/jquery.slim.min.js"] [] // 3.5.1
        script [_src "/assets/lib/boostrap/bootstrap.min.js"] [] // 4.6.0
        script [_src "/assets/lib/highlight/highlight.min.js"] [] // 11.8.0
        script [_src "/assets/lib/highlight/highlight.fsharp.min.js"] [] // 11.8.0
        script [_src "/assets/lib/highlight/highlight.nix.min.js"] [] // 11.8.0

        // QR Code generation library
        script [_src "https://cdn.jsdelivr.net/npm/qr-code-styling@1.5.0/lib/qr-code-styling.min.js"] []

        // Main JavaScript functionality (theme management, copy-to-clipboard, etc.)
        script [_src "/assets/js/main.js"] []
        // Timeline functionality (filtering, progressive loading, etc.)
        script [_src "/assets/js/timeline.js"] []
        // UFO Cursor enhancement (dynamic direction-based tilting)
        script [_src "/assets/js/ufo-cursor.js"] []
        
        // Web API enhancements (progressive enhancement pattern)
        script [_src "/assets/js/clipboard.js"] []    // Code snippet copy buttons
        script [_src "/assets/js/share.js"] []        // Native content sharing
        script [_src "/assets/js/qrcode.js"] []       // QR code generation
        script [_src "/assets/js/lazy-images.js"] []  // Image lazy loading
        script [_src "/assets/js/page-visibility.js"] []  // Resource optimization when tab hidden
        
        // PWA Service Worker registration (offline support, caching)
        script [_src "/assets/js/sw-registration.js"] []

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

    let defaultLayout (pageTitle:string) (content:string) =
        html [_lang "en"] [
            head [] [
                meta [_charset "UTF-8"]    
                meta [_name "viewport"; _content "width=device-width, initial-scale=1, shrink-to-fit=no"]

                // Stylesheets
                for sheet in styleSheets do
                    sheet

                // Leaflet.js CSS loaded from CDN in travel-map.js

                // Opengraph
                let ogElements = buildOpenGraphElements pageTitle

                for el in ogElements do
                    el

                // RSS Feeds
                for feed in rssFeeds do
                    feed

                // Webmentions
                webmentionLink

                // Rolls
                for roll in rollLinks do
                    roll
                
                // PWA Manifest
                link [_rel "manifest"; _href "/manifest.json"]
                meta [_name "theme-color"; _content "#2d4a5c"]
                meta [_name "apple-mobile-web-app-capable"; _content "yes"]
                meta [_name "apple-mobile-web-app-status-bar-style"; _content "black-translucent"]
                meta [_name "apple-mobile-web-app-title"; _content "Luis Quintanilla"]

                // Robots
                meta [_name "robots"; _content "nosnippet"]
                title [] [Text pageTitle]
            ]
            body [] [
                // Desert theme navigation
                for navElement in desertNavigation do
                    navElement

                main [attr "role" "main"; _class "main-content"; _id "main-content"] [
                    div [_class "content-wrapper"] [
                        rawText content
                    ]
                ]

                for scr in scripts do
                    scr

                // Travel map functionality (Leaflet.js loaded from CDN)
                script [_src "/assets/js/travel-map.js"] []

            ]
            footerContent
        ]

    // Similar to default layout but allows indexing by search engines
    let defaultIndexedLayout (pageTitle:string) (content:string) =
        html [_lang "en"] [
            head [] [
                meta [_charset "UTF-8"]    
                meta [_name "viewport"; _content "width=device-width, initial-scale=1, shrink-to-fit=no"]

                // Stylesheets
                for sheet in styleSheets do
                    sheet

                // Reveal.js CSS for presentations
                link [_rel "stylesheet"; _href "/lib/revealjs/dist/reveal.css"]
                link [_rel "stylesheet"; _href "/lib/revealjs/dist/theme/black.css"]
                link [_rel "stylesheet"; _href "/lib/revealjs/plugin/highlight/monokai.css"]
                link [_rel "stylesheet"; _href "/assets/css/presentation-layouts.css"]
                // Leaflet.js CSS loaded from CDN in travel-map.js

                // Opengraph
                let ogElements = buildOpenGraphElements pageTitle

                for el in ogElements do
                    el

                // RSS Feeds
                for feed in rssFeeds do
                    feed

                // Webmentions
                webmentionLink

                // Rolls
                for roll in rollLinks do
                    roll
                
                // PWA Manifest
                link [_rel "manifest"; _href "/manifest.json"]
                meta [_name "theme-color"; _content "#2d4a5c"]
                meta [_name "apple-mobile-web-app-capable"; _content "yes"]
                meta [_name "apple-mobile-web-app-status-bar-style"; _content "black-translucent"]
                meta [_name "apple-mobile-web-app-title"; _content "Luis Quintanilla"]

                // Robots
                meta [_name "robots"; _content "nosnippet"]
                title [] [Text pageTitle]
            ]
            body [] [
                // Desert theme navigation
                for navElement in desertNavigation do
                    navElement

                main [attr "role" "main"; _class "main-content"; _id "main-content"] [
                    div [_class "content-wrapper"] [
                        rawText content
                    ]
                ]

                for scr in scripts do
                    scr

                // Conditionally load Reveal.js for presentations
                script [_src "/lib/revealjs/dist/reveal.js"] []
                script [_src "/lib/revealjs/plugin/markdown/markdown.js"] []
                script [_src "/lib/revealjs/plugin/highlight/highlight.js"] []
                script [_type "application/javascript"] [
                    rawText """
                    // Initialize Reveal.js when a presentation container is found on the page
                    document.addEventListener('DOMContentLoaded', function() {
                        const presentationContainer = document.querySelector('.presentation-container');
                        if (presentationContainer && typeof Reveal !== 'undefined') {
                            Reveal.initialize({
                                plugins: [RevealMarkdown, RevealHighlight],
                                embedded: true
                            });
                        }
                    });
                    """
                ]

                // Travel map functionality (Leaflet.js loaded from CDN)
                script [_src "/assets/js/travel-map.js"] []

            ]
            footerContent
        ]


    // Text-Only Layout - Accessibility-First Universal Design
    let textOnlyLayout (pageTitle:string) (content:string) =
        html [_lang "en"] [
            head [] [
                meta [_charset "UTF-8"]
                meta [_name "viewport"; _content "width=device-width, initial-scale=1"]
                
                // Minimal CSS for text-only experience (<5KB target)
                link [_rel "stylesheet"; _href "/text/assets/text-only.css"]
                
                // Essential metadata
                meta [_name "description"; _content "Text-only accessible version of Luis Quintanilla's website"]
                meta [_name "robots"; _content "noindex, nofollow"]
                
                title [] [Text $"{pageTitle} - Text-Only Site"]
            ]
            body [] [
                // Skip link for screen readers
                a [_href "#main-content"; _class "skip-link"] [Text "Skip to main content"]
                
                // Text-only navigation header
                header [attr "role" "banner"] [
                    h1 [] [
                        a [_href "/text/"] [Text "Luis Quintanilla"]
                    ]
                    p [] [Text "Text-Only Accessible Website"]
                ]
                
                // Main navigation
                nav [attr "role" "navigation"; attr "aria-label" "Main navigation"] [
                    ul [] [
                        li [] [a [_href "/text/"] [Text "Home"]]
                        li [] [a [_href "/text/about/"] [Text "About"]]
                        li [] [a [_href "/text/contact/"] [Text "Contact"]]
                        li [] [a [_href "/text/content/"] [Text "All Content"]]
                        li [] [a [_href "/text/feeds/"] [Text "RSS Feeds"]]
                        li [] [a [_href "/text/help/"] [Text "Help"]]
                    ]
                ]
                
                // Main content
                main [attr "role" "main"; _id "main-content"] [
                    rawText content
                ]
                
                // Simple footer with essential links
                footer [attr "role" "contentinfo"] [
                    hr []
                    p [] [
                        a [_href "/"] [Text "Full Site"]
                        Text " | "
                        a [_href "/text/accessibility/"] [Text "Accessibility"]
                    ]
                ]
            ]
        ]

    let presentationLayout (pageTitle:string) (content:string) =
        html [_lang "en"] [
            head [] [
                meta [_charset "UTF-8"]    
                meta [_name "viewport"; _content "width=device-width, initial-scale=1, shrink-to-fit=no"]

                // Stylesheets
                for sheet in styleSheets do
                    sheet

                link [_rel "stylesheet"; _href "/assets/lib/revealjs/dist/reveal.css"]
                link [_rel "stylesheet"; _href "/assets/lib/revealjs/dist/theme/black.css"]
                link [_rel "stylesheet"; _href "/assets/css/presentation-layouts.css"]

                // Opengraph
                let ogElements = buildOpenGraphElements pageTitle

                for el in ogElements do
                    el
                                
                // RSS Feeds
                for feed in rssFeeds do
                    feed

                // Webmentions
                webmentionLink

                // Rolls
                for roll in rollLinks do
                    roll
                
                // PWA Manifest
                link [_rel "manifest"; _href "/manifest.json"]
                meta [_name "theme-color"; _content "#2d4a5c"]
                meta [_name "apple-mobile-web-app-capable"; _content "yes"]
                meta [_name "apple-mobile-web-app-status-bar-style"; _content "black-translucent"]
                meta [_name "apple-mobile-web-app-title"; _content "Luis Quintanilla"]

                // Robots                
                meta [_name "robots"; _content "nosnippet"]
                
                title [] [Text pageTitle]
            ]
            body [] [
                // Desert theme navigation (consistent with other content types)
                for navElement in desertNavigation do
                    navElement

                main [attr "role" "main"; _class "main-content"; _id "main-content"] [
                    div [_class "content-wrapper"] [
                        rawText content
                    ]
                ]

                for scr in scripts do
                    scr

                // Revealjs (As of 10/20/2021)
                script [_src "/assets/lib/revealjs/dist/reveal.js"] []
                script [_src "/assets/lib/revealjs/plugin/markdown/markdown.js"] []
                script [_type "application/javascript"] [
                    rawText """
                    Reveal.initialize({
                        plugins: [ RevealMarkdown ],
                        embedded: true,
                        width: 800,
                        height: 600,
                        minScale: 0.5,
                        maxScale: 1.0,
                        margin: 0.1,
                        controls: true,
                        progress: true,
                        center: true,
                        touch: true,
                        loop: false,
                        rtl: false,
                        fragments: true,
                        autoSlide: 0,
                        keyboard: true,
                        overview: true,
                        disableLayout: false
                    });
                    """
                ]   
            ]
            footerContent
        ]
