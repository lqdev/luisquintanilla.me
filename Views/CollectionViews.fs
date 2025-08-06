module CollectionViews

open Giraffe.ViewEngine
open System
open System.IO
open Domain
open ContentViews
open ComponentViews

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

let feedView (posts: Post array) =
    div [ _class "d-grip gap-3" ] [
        h2[] [Text "Posts"]
        p [] [Text "Long-form articles and blog posts"]
        ul [] [
            for post in posts do
                li [] [
                    a [ _href $"/posts/{post.FileName}"] [ Text post.Metadata.Title ]
                    Text " • "
                    Text (DateTime.Parse(post.Metadata.Date).ToString("MMM dd, yyyy"))
                ]
        ]
    ]

let notesView (posts: Post array) =
    div [ _class "d-grip gap-3" ] [
        h2[] [Text "Notes"]
        p [] [Text "Personal notes and short-form thoughts"]
        ul [] [
            for post in posts do
                li [] [
                    a [ _href $"/notes/{post.FileName}"] [ Text post.Metadata.Title ]
                    Text " • "
                    Text (DateTime.Parse(post.Metadata.Date).ToString("MMM dd, yyyy"))
                ]
        ]
    ]

let responseView (posts: Response array) =
    div [ _class "d-grip gap-3" ] [
        h2[] [Text "Responses"]
        p [] [Text "Replies, bookmarks, and reactions to other content"]
        ul [] [
            for post in posts do
                li [] [
                    a [ _href $"/responses/{post.FileName}"] [ Text post.Metadata.Title ]
                    Text " • "
                    Text (DateTime.Parse(post.Metadata.DatePublished).ToString("MMM dd, yyyy"))
                ]
        ]
    ]    

let bookmarkView (bookmarks: Bookmark array) =
    div [ _class "d-grip gap-3" ] [
        h2[] [Text "Bookmarks"]
        p [] [Text "Links to interesting articles, tools, and resources"]
        ul [] [
            for bookmark in bookmarks do
                li [] [
                    a [ _href $"/bookmarks/{bookmark.FileName}"] [ Text bookmark.Metadata.Title ]
                    Text " • "
                    Text (DateTime.Parse(bookmark.Metadata.DatePublished).ToString("MMM dd, yyyy"))
                ]
        ]
    ]

let libraryView (books:Book array) = 
    div [ _class "d-grip gap-3" ] [
        for book in books do
            bookPostView book
    ]

let snippetsView (snippets: Snippet array) = 
    div [ _class "d-grip gap-3" ] [
        h2[] [Text "Snippets"]
        p [] [Text "Code snippets and scripts"]
        ul [] [
            for snippet in snippets do
                li [] [
                    a [ _href $"/resources/snippets/{snippet.FileName}"] [ Text snippet.Metadata.Title ]
                    if not (String.IsNullOrEmpty(snippet.Metadata.CreatedDate)) then
                        Text " • "
                        Text (DateTime.Parse(snippet.Metadata.CreatedDate).ToString("MMM dd, yyyy"))
                ]
        ]
    ]

let wikisView (wikis: Wiki array) = 
    div [ _class "d-grip gap-3" ] [
        h2[] [Text "Wiki"]
        p [] [Text "Personal knowledge base and notes"]
        ul [] [
            for wiki in wikis do
                li [] [
                    a [ _href $"/resources/wiki/{wiki.FileName}"] [ Text wiki.Metadata.Title ]
                    if not (String.IsNullOrEmpty(wiki.Metadata.LastUpdatedDate)) then
                        Text " • "
                        Text (DateTime.Parse(wiki.Metadata.LastUpdatedDate).ToString("MMM dd, yyyy"))
                ]
        ]
    ]

let presentationsView (presentations: Presentation array) = 
    div [ _class "d-grip gap-3" ] [
        h2[] [Text "Presentations"]
        p [] [Text "List of presentations and associated resources"]
        ul [] [
            for presentation in presentations do
                li [] [
                    a [ _href $"/resources/presentations/{presentation.FileName}"] [ Text presentation.Metadata.Title ]
                ]
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

let albumsPageView (albums:Album array) = 
    div [ _class "d-grip gap-3" ] [
        h2[] [Text "Media"]
        p [] [Text "Photo albums and media collections"]
        ul [] [
            for album in albums do
                li [] [
                    a [ _href $"/media/{album.FileName}"] [ Text album.Metadata.Title ]
                    if not (String.IsNullOrEmpty(album.Metadata.Date)) then
                        Text " • "
                        Text (DateTime.Parse(album.Metadata.Date).ToString("MMM dd, yyyy"))
                ]
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

// Enhanced subscription hub with content discovery
let enhancedSubscriptionHubView (items: GenericBuilder.UnifiedFeeds.UnifiedFeedItem array) =
    // Take recent 8-10 items for preview
    let recentItems = items |> Array.take (min 10 items.Length)
    
    let getProperPermalink (contentType: string) (fileName: string) =
        match contentType with
        | "posts" -> $"/posts/{fileName}/"
        | "notes" -> $"/notes/{fileName}/"
        | "responses" -> $"/responses/{fileName}/"
        | "bookmarks" -> $"/responses/{fileName}/"
        | "snippets" -> $"/resources/snippets/{fileName}/"
        | "wiki" -> $"/resources/wiki/{fileName}/"
        | "presentations" -> $"/resources/presentations/{fileName}/"
        | "reviews" -> $"/reviews/{fileName}/"
        | "media" -> $"/media/{fileName}/"
        | _ -> $"/{contentType}/{fileName}/"
    
    div [ _class "content-wrapper" ] [
        div [ _class "mr-auto" ] [
            // Header & Introduction
            h2 [] [ Text "Feeds & Content Discovery" ]
            p [] [
                Text "I'm a big fan of RSS and have written about it in posts like "
                a [ _href "/posts/rediscovering-rss-user-freedom" ] [ Text "Rediscovering the RSS Protocol" ]
                Text ". Therefore, I've made it easy to subscribe to content on this site through various RSS feeds. You can also browse recent content below to get a sense of what I publish."
            ]
            p [] [
                a [ _href "/feed/index.opml"; _class "btn btn-primary me-3" ] [ Text "📄 Download All Feeds (OPML)" ]
                a [ _href "#recent-content"; _class "btn btn-outline-secondary" ] [ Text "📖 Browse Recent Posts ↓" ]
            ]
            
            // Primary Feed Subscriptions
            h2 [] [ Text "Featured Feeds" ]
            
            // Blog Feed
            h3 [] [ a [ _href "/posts/1/" ] [ Text "Blog" ] ]
            p [] [ Text "Long-form posts. Mainly around tech topics." ]
            p [] [ 
                Text "Feed URL: "
                a [ _href "/blog.rss" ] [ Text "/blog.rss" ]
            ]
            
            // Microblog Feed  
            h3 [] [ a [ _href "/notes/" ] [ Text "Microblog Feed" ] ]
            p [] [ Text "Microblog-like short posts containing different types of content such as notes, photos, and videos." ]
            p [] [
                Text "Feed URL: "
                a [ _href "/microblog.rss" ] [ Text "/microblog.rss" ]
            ]
            
            // Response Feed
            h3 [] [ a [ _href "/responses/" ] [ Text "Response Feed" ] ]
            p [] [ Text "Microblog-like short posts containing replies, reshares (repost), favorites (likes), and bookmarks." ]
            p [] [
                Text "Feed URL: "
                a [ _href "/responses.rss" ] [ Text "/responses.rss" ]
            ]
            
            // Recent Content Highlights
            h2 [ _id "recent-content" ] [ Text "Recent Updates" ]
            p [] [ Text "Get a taste of what you'll receive by subscribing:" ]
            
            // Simple list like other landing pages
            ul [] [
                for item in recentItems do
                    let fileName = System.IO.Path.GetFileNameWithoutExtension(item.Url)
                    let properPermalink = getProperPermalink item.ContentType fileName
                    li [] [
                        a [ _href properPermalink ] [ Text item.Title ]
                        Text " • "
                        Text (DateTime.Parse(item.Date).ToString("MMM dd, yyyy"))
                    ]
            ]
            
            // Additional Site Feeds
            h2 [] [ Text "All Site Feeds" ]
            p [] [ Text "Subscribe to specific content types:" ]
            
            // Bookmarks Feed
            h3 [] [ a [ _href "/bookmarks/" ] [ Text "Bookmarks" ] ]
            p [] [ Text "Links to interesting articles, tools, and resources I come across." ]
            p [] [ 
                Text "Feed URL: "
                a [ _href "/responses.rss" ] [ Text "/responses.rss" ]
                Text " (filter by bookmark type)"
            ]
            
            // Media Feed  
            h3 [] [ a [ _href "/media/" ] [ Text "Media" ] ]
            p [] [ Text "Photo albums and media collections." ]
            p [] [
                Text "Feed URL: "
                a [ _href "/microblog.rss" ] [ Text "/microblog.rss" ]
                Text " (includes media posts)"
            ]
            
            // Reviews Feed
            h3 [] [ a [ _href "/reviews/" ] [ Text "Reviews" ] ]
            p [] [ Text "Book reviews and other content critiques." ]
            p [] [
                Text "Feed URL: "
                a [ _href "/microblog.rss" ] [ Text "/microblog.rss" ]
                Text " (includes review posts)"
            ]
            
            // Tags Information
            h3 [] [ a [ _href "/tags/" ] [ Text "Individual Topic Feeds" ] ]
            p [] [ Text "Every tag on this site has its own RSS feed. Browse " ]
            a [ _href "/tags/" ] [ Text "all available tags" ]
            p [] [ Text " and click on any tag to access its dedicated feed." ]
            p [] [ 
                Text "Example: "
                a [ _href "/tags/rss/feed.xml" ] [ Text "/tags/rss/feed.xml" ]
            ]
            
            // External Platform Feeds (Collapsible)
            h2 [] [ Text "External Platform Feeds" ]
            details [ _class "mb-4" ] [
                summary [ _class "btn btn-outline-secondary" ] [ Text "View External Platform Feeds" ]
                div [ _class "mt-3" ] [
                    p [] [ Text "I also publish content on other platforms. These feeds help you follow my activity there:" ]
                    
                    h4 [] [ a [ _href "/mastodon" ] [ Text "Mastodon" ] ]
                    p [] [ Text "RSS Feed for Mastodon posts." ]
                    p [] [ 
                        Text "Feed URL: "
                        a [ _href "http://toot.lqdev.tech/@lqdev.rss" ] [ Text "/mastodon.rss" ]
                    ]
                    
                    h4 [] [ a [ _href "/bluesky" ] [ Text "Bluesky" ] ]
                    p [] [
                        Text "Feed URL: "
                        a [ _href "/bluesky.rss" ] [ Text "/bluesky.rss" ]
                    ]
                    
                    h4 [] [ a [ _href "/youtube" ] [ Text "YouTube Channel" ] ]
                    p [] [ Text "Video posts on YouTube" ]
                    p [] [
                        Text "Feed URL: "
                        a [ _href "/youtube.rss" ] [ Text "/youtube.rss" ]
                    ]
                ]
            ]
            
            // Getting Started with RSS
            h2 [] [ Text "New to RSS?" ]
            p [] [
                Text "RSS (Really Simple Syndication) lets you follow websites and content creators without "
                Text "relying on social media algorithms or email newsletters cluttering your inbox. "
                Text "It works with everything from blogs and podcasts to YouTube channels, news sites, "
                Text "newsletters, forums, and social platforms like Mastodon and Bluesky."
            ]
            
            h3 [] [ Text "Quick Start Guide" ]
            ol [] [
                li [] [
                    strong [] [ Text "Choose a Feed Reader: " ]
                    Text "Popular options include "
                    a [ _href "https://www.inoreader.com/" ] [ Text "Inoreader" ]
                    Text ", "
                    a [ _href "https://newsblur.com/" ] [ Text "NewsBlur" ]
                    Text ", "
                    a [ _href "https://netnewswire.com/" ] [ Text "NetNewsWire" ]
                    Text " (Mac), or "
                    a [ _href "https://feedly.com/" ] [ Text "Feedly" ]
                    Text ". Most have free tiers to get started."
                ]
                li [] [
                    strong [] [ Text "Subscribe to Feeds: " ]
                    Text "Copy any feed URL from this page and paste it into your reader. "
                    Text "Or use the OPML file above to subscribe to everything at once."
                ]
                li [] [
                    strong [] [ Text "Start Reading: " ]
                    Text "New posts appear in your feed reader automatically. "
                    Text "Read what interests you, skip what doesn't - you're in complete control."
                ]
            ]
            
            // OPML Information
            h3 [] [ Text "About OPML Files" ]
            p [] [
                Text "OPML (Outline Processor Markup Language) is like a playlist for RSS feeds. Instead of "
                Text "adding feeds one by one, you can import an OPML file to subscribe to entire collections "
                Text "with one click. That's why I provide an "
                a [ _href "/feed/index.opml" ] [ Text "OPML file with all my feeds" ]
                Text " and use it for my "
                a [ _href "/collections/starter-packs/" ] [ Text "starter packs" ]
                Text " - curated feed collections around specific topics."
            ]
            
            p [] [
                Text "Learn more: "
                a [ _href "https://aboutfeeds.com/" ] [ Text "About Feeds" ]
                Text " • "
                a [ _href "/posts/rediscovering-rss-user-freedom" ] [ Text "Why I Use RSS" ]
                Text " • "
                a [ _href "/collections/starter-packs/" ] [ Text "Browse Starter Packs" ]
            ]
        ]
    ]

// Unified feed view for aggregated content across all types
let unifiedFeedView (items: GenericBuilder.UnifiedFeeds.UnifiedFeedItem array) =
    let getProperPermalink (contentType: string) (fileName: string) =
        match contentType with
        | "posts" -> $"/posts/{fileName}/"
        | "notes" -> $"/notes/{fileName}/"
        | "responses" -> $"/responses/{fileName}/"
        | "bookmarks" -> $"/responses/{fileName}/"  // Bookmarks are responses but filtered separately
        | "snippets" -> $"/resources/snippets/{fileName}/"
        | "wiki" -> $"/resources/wiki/{fileName}/"
        | "presentations" -> $"/resources/presentations/{fileName}/"
        | "reviews" -> $"/reviews/{fileName}/"
        | "media" -> $"/media/{fileName}/"
        | _ -> $"/{contentType}/{fileName}/"
    
    let renderUnifiedCard (item: GenericBuilder.UnifiedFeeds.UnifiedFeedItem) =
        let header = cardHeader item.Date
        let fileName = Path.GetFileNameWithoutExtension(item.Url)
        let properPermalink = getProperPermalink item.ContentType fileName
        
        // Clean content function to remove "No description available" and other placeholder text
        let cleanPlaceholderContent (content: string) =
            if String.IsNullOrWhiteSpace(content) then
                $"<p>Read more about <strong>{item.Title}</strong></p>"
            else
                let cleaned = 
                    content
                        .Replace("No description available", "")
                        .Replace("<p></p>", "")
                        .Replace("<p> </p>", "")
                        .Trim()
                
                // Remove timestamp patterns like "2025-07-06 20:09" that appear in content
                let timestampPattern = System.Text.RegularExpressions.Regex(@"^\s*\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}\s*$", System.Text.RegularExpressions.RegexOptions.Multiline)
                let cleanedWithoutTimestamp = timestampPattern.Replace(cleaned, "").Trim()
                
                if String.IsNullOrWhiteSpace(cleanedWithoutTimestamp) || cleanedWithoutTimestamp = "<p></p>" then
                    $"<p>Read more about <strong>{item.Title}</strong></p>"
                else
                    cleanedWithoutTimestamp
        
        // Function to extract content from RenderCard HTML structure
        let extractContentFromCardHtml (html: string) =
            // Remove article wrapper and extract just the content inside
            let articlePattern = System.Text.RegularExpressions.Regex(@"<article[^>]*>(.*?)</article>", System.Text.RegularExpressions.RegexOptions.Singleline)
            let articleMatch = articlePattern.Match(html)
            
            if articleMatch.Success then
                let innerContent = articleMatch.Groups.[1].Value
                
                // Remove H2 title elements (they're duplicated now)
                let titlePattern = System.Text.RegularExpressions.Regex(@"<h2[^>]*>.*?</h2>", System.Text.RegularExpressions.RegexOptions.Singleline)
                let contentWithoutTitle = titlePattern.Replace(innerContent, "").Trim()
                
                // If only whitespace left, provide fallback
                if String.IsNullOrWhiteSpace(contentWithoutTitle) then
                    $"<p>Read more about <strong>{item.Title}</strong></p>"
                else
                    contentWithoutTitle
            else
                // If not wrapped in article, try to remove any H2 titles
                let titlePattern = System.Text.RegularExpressions.Regex(@"<h2[^>]*>.*?</h2>", System.Text.RegularExpressions.RegexOptions.Singleline)
                let contentWithoutTitle = titlePattern.Replace(html, "").Trim()
                
                if String.IsNullOrWhiteSpace(contentWithoutTitle) then
                    $"<p>Read more about <strong>{item.Title}</strong></p>"
                else
                    contentWithoutTitle
        
        // Content is now clean HTML from CardHtml, extract content without title duplication
        let rawContent = extractContentFromCardHtml item.Content
        let cleanContent = cleanPlaceholderContent rawContent
        
        // Instead of creating a new card wrapper, just render the content directly
        // The RenderCard functions already generate proper article elements
        div [ _class "mb-5 post-separator pb-4 h-entry" ] [
            // Add content type indicator above the article
            div [ _class "mb-3" ] [
                span [ _class "badge badge-light border" ] [ 
                    Text (item.ContentType |> fun ct -> ct.Substring(0, 1).ToUpper() + ct.Substring(1))
                ]
                Text " • "
                Text (DateTime.Parse(item.Date).ToString("MMM dd, yyyy"))
            ]
            
            // Render the original content directly - it's already a complete article
            rawText item.Content
            
            // Add "Read More" button if not already present in content
            let needsReadMoreButton = not (item.Content.Contains("Read More →"))
            if needsReadMoreButton then
                div [ _class "mt-2" ] [
                    a [ _href properPermalink; _class "btn btn-outline-primary btn-sm" ] [ Text "Read More →" ]
                ]
            
            // Add footer with permalink and tags
            div [ _class "mt-3 pt-2 border-top text-muted small" ] [
                Text "Permalink: " 
                a [_href properPermalink; _class "u-url text-decoration-none"] [Text properPermalink] 
                
                div [ _class "mt-1" ] [
                    str "Tags: "
                    for tag in item.Tags do
                        let sanitizedTag = tag.Replace("#", "sharp").Replace("/", "-").Replace(" ", "-").Replace("\"", "")
                        a [_href $"/tags/{sanitizedTag}"; _class "p-category text-decoration-none me-2"] [Text $"#{tag}"]
                ]
            ]
        ]
    
    div [ _class "d-grip gap-3" ] [
        for item in items do
            renderUnifiedCard item
    ]
