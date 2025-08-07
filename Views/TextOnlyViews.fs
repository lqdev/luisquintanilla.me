module TextOnlyViews

open Giraffe.ViewEngine
open Domain
open Layouts
open GenericBuilder.UnifiedFeeds
open System

// Helper function to extract slug from URL
let extractSlugFromUrl (url: string) =
    let parts = url.Split('/')
    if parts.Length >= 2 then
        parts.[parts.Length - 2] // Get second-to-last part (before trailing slash)
    else
        "content"

// Helper function to parse date string
let parseItemDate (dateString: string) =
    match DateTime.TryParse(dateString) with
    | (true, date) -> date
    | (false, _) -> DateTime.MinValue

// Text-Only Homepage
let textOnlyHomepage (recentContent: UnifiedFeedItem list) =
    let recentItems = recentContent |> List.take (min 20 recentContent.Length)
    
    let contentHtml = 
        div [] [
            h1 [] [Text "Luis Quintanilla - Text-Only Site"]
            
            p [] [
                Text "Welcome to the text-only, accessibility-optimized version of my website. "
                Text "This site is designed for 2G networks, flip phones, screen readers, and anyone preferring a minimal, fast-loading experience."
            ]
            
            h2 [] [Text "Recent Content"]
            
            if recentItems.IsEmpty then
                p [] [Text "No recent content available."]
            else
                ul [_class "content-list"] [
                    for item in recentItems do
                        let slug = extractSlugFromUrl item.Url
                        let itemDate = parseItemDate item.Date
                        li [] [
                            div [_class "content-type"] [Text item.ContentType]
                            h3 [] [
                                a [_href $"/text/content/{item.ContentType.ToLower()}/{slug}/"] [
                                    Text item.Title
                                ]
                            ]
                            div [_class "content-meta"] [
                                time [attr "datetime" (itemDate.ToString("yyyy-MM-dd"))] [
                                    Text (itemDate.ToString("MMMM d, yyyy"))
                                ]
                                if item.Tags.Length > 0 then
                                    Text " | Tags: "
                                    Text (String.concat ", " item.Tags)
                            ]
                        ]
                ]
            
            h2 [] [Text "Browse Content"]
            ul [] [
                li [] [a [_href "/text/content/posts/"] [Text "Blog Posts"]]
                li [] [a [_href "/text/content/notes/"] [Text "Notes & Microblog"]]
                li [] [a [_href "/text/content/responses/"] [Text "Responses & Bookmarks"]]
                li [] [a [_href "/text/content/snippets/"] [Text "Code Snippets"]]
                li [] [a [_href "/text/content/wiki/"] [Text "Wiki & Knowledge Base"]]
                li [] [a [_href "/text/content/presentations/"] [Text "Presentations"]]
                li [] [a [_href "/text/content/reviews/"] [Text "Book Reviews"]]
                li [] [a [_href "/text/content/albums/"] [Text "Photo Albums"]]
            ]
            
            h2 [] [Text "Quick Navigation"]
            ul [] [
                li [] [a [_href "/text/browse/recent/"] [Text "All Recent Content"]]
                li [] [a [_href "/text/browse/topics/"] [Text "Browse by Topic"]]
                li [] [a [_href "/text/browse/archives/"] [Text "Archives by Date"]]
                li [] [a [_href "/text/feeds/"] [Text "RSS Feeds"]]
                li [] [a [_href "/"] [Text "Full Website (with graphics)"]]
            ]
        ]
    
    textOnlyLayout "Home" (RenderView.AsString.xmlNode contentHtml)

// Content Type Listing Page
let textOnlyContentTypePage (contentType: string) (content: UnifiedFeedItem list) =
    let filteredContent = 
        content 
        |> List.filter (fun item -> item.ContentType.ToLower() = contentType.ToLower())
        |> List.sortByDescending (fun item -> parseItemDate item.Date)
    
    let pageTitle = 
        match contentType.ToLower() with
        | "posts" -> "Blog Posts"
        | "notes" -> "Notes & Microblog"
        | "responses" -> "Responses & Bookmarks"
        | "snippets" -> "Code Snippets"
        | "wiki" -> "Wiki & Knowledge Base"
        | "presentations" -> "Presentations"
        | "reviews" -> "Book Reviews"
        | "albums" -> "Photo Albums"
        | _ -> $"{contentType} Content"
    
    let contentHtml =
        div [] [
            h1 [] [Text pageTitle]
            
            p [] [
                a [_href "/text/"] [Text "← Back to Home"]
                Text " | "
                a [_href "/text/content/"] [Text "All Content Types"]
            ]
            
            if filteredContent.IsEmpty then
                p [] [Text $"No {pageTitle.ToLower()} available."]
            else
                p [] [Text $"Found {filteredContent.Length} {pageTitle.ToLower()}."]
                
                ul [_class "content-list"] [
                    for item in filteredContent do
                        let slug = extractSlugFromUrl item.Url
                        let itemDate = parseItemDate item.Date
                        li [] [
                            h3 [] [
                                a [_href $"/text/content/{item.ContentType.ToLower()}/{slug}/"] [
                                    Text item.Title
                                ]
                            ]
                            div [_class "content-meta"] [
                                time [attr "datetime" (itemDate.ToString("yyyy-MM-dd"))] [
                                    Text (itemDate.ToString("MMMM d, yyyy"))
                                ]
                                if item.Tags.Length > 0 then
                                    Text " | Tags: "
                                    Text (String.concat ", " item.Tags)
                            ]
                        ]
                ]
        ]
    
    textOnlyLayout pageTitle (RenderView.AsString.xmlNode contentHtml)

// Individual Content Page
let textOnlyContentPage (content: UnifiedFeedItem) (htmlContent: string) =
    // Convert HTML content to plain text (simple approach for Phase 1)
    let plainTextContent = 
        let text = 
            htmlContent
                .Replace("<p>", "\n\n")
                .Replace("</p>", "")
                .Replace("<br>", "\n")
                .Replace("<br/>", "\n")
                .Replace("<br />", "\n")
                .Replace("&nbsp;", " ")
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&quot;", "\"")
        System.Text.RegularExpressions.Regex.Replace(text, "<[^>]*>", "").Trim()
    
    let slug = extractSlugFromUrl content.Url
    let itemDate = parseItemDate content.Date
    
    let contentHtml =
        div [] [
            h1 [] [Text content.Title]
            
            div [_class "content-meta"] [
                div [_class "content-type"] [Text content.ContentType]
                time [attr "datetime" (itemDate.ToString("yyyy-MM-dd"))] [
                    Text (itemDate.ToString("MMMM d, yyyy"))
                ]
                if content.Tags.Length > 0 then
                    p [] [
                        Text "Tags: "
                        Text (String.concat ", " content.Tags)
                    ]
            ]
            
            p [] [
                a [_href "/text/"] [Text "← Home"]
                Text " | "
                a [_href $"/text/content/{content.ContentType.ToLower()}/"] [Text $"← All {content.ContentType}"]
                Text " | "
                a [_href content.Url] [Text "View Full Version"]
            ]
            
            // Main content - preserve paragraph structure
            if not (System.String.IsNullOrWhiteSpace(plainTextContent)) then
                let paragraphs = plainTextContent.Split([|"\n\n"|], System.StringSplitOptions.RemoveEmptyEntries)
                for paragraph in paragraphs do
                    if not (System.String.IsNullOrWhiteSpace(paragraph)) then
                        p [] [Text (paragraph.Trim())]
            else
                p [] [Text "Content not available in text format."]
        ]
    
    textOnlyLayout content.Title (RenderView.AsString.xmlNode contentHtml)

// All Content Types Directory
let textOnlyAllContentPage (content: UnifiedFeedItem list) =
    let groupedContent = 
        content
        |> List.groupBy (fun item -> item.ContentType)
        |> List.sortBy fst
    
    let contentHtml =
        div [] [
            h1 [] [Text "All Content Types"]
            
            p [] [
                a [_href "/text/"] [Text "← Back to Home"]
            ]
            
            p [] [Text "Browse all content organized by type:"]
            
            for (contentType, items) in groupedContent do
                let itemCount = List.length items
                div [] [
                    h2 [] [
                        a [_href $"/text/content/{contentType.ToLower()}/"] [
                            Text contentType
                        ]
                    ]
                    p [] [Text $"{itemCount} items available"]
                ]
        ]
    
    textOnlyLayout "All Content Types" (RenderView.AsString.xmlNode contentHtml)

// About Page
let textOnlyAboutPage =
    let contentHtml =
        div [] [
            h1 [] [Text "About Luis Quintanilla"]
            
            p [] [
                a [_href "/text/"] [Text "← Back to Home"]
                Text " | "
                a [_href "/about"] [Text "View Full About Page"]
            ]
            
            p [] [
                Text "I'm a Program Manager on the .NET ML team at Microsoft working on ML.NET. "
                Text "I'm passionate about democratizing AI through open source technologies and education."
            ]
            
            h2 [] [Text "Background"]
            p [] [
                Text "I work on ML.NET, an open source machine learning framework for .NET developers. "
                Text "I enjoy building tools and creating content that helps developers get started with AI and machine learning."
            ]
            
            h2 [] [Text "This Text-Only Site"]
            p [] [
                Text "This text-only version of my website is designed for universal accessibility. "
                Text "It works on 2G networks, flip phones, screen readers, and provides a distraction-free reading experience."
            ]
            
            p [] [
                Text "The site includes the same content as the full website but optimized for minimal bandwidth usage "
                Text "and maximum compatibility with assistive technologies."
            ]
            
            h2 [] [Text "Contact"]
            p [] [
                Text "Email: "
                a [_href "mailto:lqdev@outlook.com"] [Text "lqdev@outlook.com"]
            ]
            
            p [] [
                Text "Mastodon: "
                a [_href "https://toot.lqdev.tech/@lqdev"] [Text "@lqdev@toot.lqdev.tech"]
            ]
        ]
    
    textOnlyLayout "About" (RenderView.AsString.xmlNode contentHtml)

// Help Page
let textOnlyHelpPage =
    let contentHtml =
        div [] [
            h1 [] [Text "Text-Only Site Help"]
            
            p [] [
                a [_href "/text/"] [Text "← Back to Home"]
            ]
            
            h2 [] [Text "About This Site"]
            p [] [
                Text "This is a text-only, accessibility-optimized version of Luis Quintanilla's website. "
                Text "It's designed to work on any device and connection, including:"
            ]
            
            ul [] [
                li [] [Text "2G mobile networks and slow connections"]
                li [] [Text "Flip phones and basic mobile browsers"]
                li [] [Text "Screen readers and assistive technologies"]
                li [] [Text "Devices with limited memory or processing power"]
                li [] [Text "Users who prefer minimal, distraction-free interfaces"]
            ]
            
            h2 [] [Text "Navigation Tips"]
            ul [] [
                li [] [Text "Use Tab and Shift+Tab to navigate between links"]
                li [] [Text "Press Enter to follow a link"]
                li [] [Text "Use arrow keys to scroll on most devices"]
                li [] [Text "Skip links are available for screen reader users"]
            ]
            
            h2 [] [Text "Content Organization"]
            p [] [Text "Content is organized by type:"]
            ul [] [
                li [] [Text "Posts: Blog articles and longer content"]
                li [] [Text "Notes: Short updates and microblog content"]
                li [] [Text "Responses: Replies and bookmarks to other content"]
                li [] [Text "Snippets: Code examples and technical notes"]
                li [] [Text "Wiki: Knowledge base and reference material"]
                li [] [Text "Presentations: Slides and presentation materials"]
                li [] [Text "Reviews: Book reviews and recommendations"]
                li [] [Text "Albums: Photo collections and visual content"]
            ]
            
            h2 [] [Text "RSS Feeds"]
            p [] [
                Text "RSS feeds are available for all content types at "
                a [_href "/text/feeds/"] [Text "/text/feeds/"]
                Text ". These feeds contain the same content as the full site feeds."
            ]
            
            h2 [] [Text "Full Website"]
            p [] [
                Text "To access the full website with graphics and interactive features, visit "
                a [_href "/"] [Text "the main site"]
                Text "."
            ]
            
            h2 [] [Text "Accessibility Features"]
            ul [] [
                li [] [Text "Semantic HTML structure for screen readers"]
                li [] [Text "High contrast text and background colors"]
                li [] [Text "Keyboard navigation support"]
                li [] [Text "Skip links for efficient navigation"]
                li [] [Text "WCAG 2.1 AA compliance"]
                li [] [Text "Works without CSS or JavaScript"]
            ]
            
            h2 [] [Text "Performance"]
            p [] [
                Text "This site is optimized for minimal data usage. Most pages load in under 50KB, "
                Text "making them suitable for very slow connections and limited data plans."
            ]
        ]
    
    textOnlyLayout "Help" (RenderView.AsString.xmlNode contentHtml)

// RSS Feeds Directory  
let textOnlyFeedsPage =
    let contentHtml =
        div [] [
            h1 [] [Text "RSS Feeds"]
            
            p [] [
                a [_href "/text/"] [Text "← Back to Home"]
            ]
            
            p [] [
                Text "Subscribe to RSS feeds to stay updated with new content. "
                Text "All feeds are available in standard RSS 2.0 format."
            ]
            
            h2 [] [Text "Main Feeds"]
            ul [] [
                li [] [
                    a [_href "/all.rss"] [Text "Everything Feed"]
                    Text " - All content from all types"
                ]
                li [] [
                    a [_href "/blog.rss"] [Text "Blog Posts"]
                    Text " - Long-form articles and blog posts"
                ]
                li [] [
                    a [_href "/microblog.rss"] [Text "Microblog"]
                    Text " - Short notes and updates"
                ]
                li [] [
                    a [_href "/responses.rss"] [Text "Responses"]
                    Text " - Replies and bookmarks"
                ]
            ]
            
            h2 [] [Text "Specialized Feeds"]
            ul [] [
                li [] [
                    a [_href "/snippets/feed.xml"] [Text "Code Snippets"]
                    Text " - Programming examples and technical notes"
                ]
                li [] [
                    a [_href "/wiki/feed.xml"] [Text "Wiki Updates"]
                    Text " - Knowledge base articles"
                ]
                li [] [
                    a [_href "/presentations/feed.xml"] [Text "Presentations"]
                    Text " - Slides and presentation materials"
                ]
                li [] [
                    a [_href "/reviews/feed.xml"] [Text "Book Reviews"]
                    Text " - Reading recommendations"
                ]
            ]
            
            h2 [] [Text "How to Subscribe"]
            p [] [
                Text "Copy any feed URL above and add it to your RSS reader. "
                Text "Popular RSS readers include:"
            ]
            
            ul [] [
                li [] [Text "Feedly (web-based)"]
                li [] [Text "NewsBlur (web-based)"]
                li [] [Text "NetNewsWire (Mac/iOS)"]
                li [] [Text "FeedReader (Windows)"]
                li [] [Text "Many email clients also support RSS"]
            ]
        ]
    
    textOnlyLayout "RSS Feeds" (RenderView.AsString.xmlNode contentHtml)
