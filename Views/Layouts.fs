module Layouts

    open Giraffe.ViewEngine
  
    let desertNavigation = Navigation.desertNavigation
    
    let styleSheets = [
        // Desert Theme CSS - Custom design system (Phase 1)
        link [_rel "stylesheet";_href "/assets/css/custom/main.css"]
        link [_rel "stylesheet";_href "/assets/css/custom/timeline.css"]  // Timeline styles for feed-as-homepage
        link [_rel "stylesheet";_href "/assets/css/custom/resume.css"]    // Resume page styles
        
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

        // Main JavaScript functionality (theme management, copy-to-clipboard, etc.)
        script [_src "/assets/js/main.js"] []
        // Timeline functionality (filtering, progressive loading, etc.)
        script [_src "/assets/js/timeline.js"] []
        // UFO Cursor enhancement (dynamic direction-based tilting)
        script [_src "/assets/js/ufo-cursor.js"] []
        
        // Web API enhancements (progressive enhancement pattern)
        script [_src "/assets/js/clipboard.js"] []    // Code snippet copy buttons
        script [_src "/assets/js/share.js"] []        // Native content sharing
        // QR codes are pre-rendered at build time into
        // `/assets/images/qr/<type>/<slug>.svg` by `Builder.buildPerPageQRs`
        // and revealed by a pure-CSS `<details>` disclosure
        // (see `Views/ComponentViews.qrCodeButton`). No runtime JS or CDN
        // library required — Phase 3.
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

    // Core site layout. `includeReveal` adds the Reveal.js presentation assets (CSS in head,
    // scripts + init in body); everything else is shared. Both variants emit the same
    // `<meta name="robots" content="nosnippet">` — nosnippet permits indexing but suppresses
    // search-result snippets; the only real difference between the two is the Reveal.js bundle.
    let private layoutCore (includeReveal: bool) (pageTitle:string) (content:string) =
        html [_lang "en"] [
            head [] [
                meta [_charset "UTF-8"]    
                meta [_name "viewport"; _content "width=device-width, initial-scale=1, shrink-to-fit=no"]

                // Stylesheets
                for sheet in styleSheets do
                    sheet

                // Reveal.js CSS for presentations
                if includeReveal then
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
                if includeReveal then
                    script [_src "/lib/revealjs/dist/reveal.js"] []
                    script [_src "/lib/revealjs/plugin/markdown/markdown.js"] []
                    script [_src "/lib/revealjs/plugin/highlight/highlight.js"] []
                    script [_src "/lib/revealjs/plugin/notes/notes.js"] []
                    script [_type "application/javascript"] [
                        rawText """
                    // Initialize Reveal.js when a presentation container is found on the page
                    document.addEventListener('DOMContentLoaded', function() {
                        const presentationContainer = document.querySelector('.presentation-container');
                        if (presentationContainer && typeof Reveal !== 'undefined') {
                            Reveal.initialize({
                                plugins: [RevealMarkdown, RevealHighlight, RevealNotes],
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

    let defaultLayout (pageTitle:string) (content:string) =
        layoutCore false pageTitle content

    // Same as defaultLayout plus the Reveal.js presentation assets (see layoutCore note).
    let defaultIndexedLayout (pageTitle:string) (content:string) =
        layoutCore true pageTitle content




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
                Navigation.renderTextOnlyNav
                
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
                script [_src "/assets/lib/revealjs/plugin/notes/notes.js"] []
                script [_type "application/javascript"] [
                    rawText """
                    Reveal.initialize({
                        plugins: [ RevealMarkdown, RevealNotes ],
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
