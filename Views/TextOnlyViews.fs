module TextOnlyViews

open Giraffe.ViewEngine
open Domain
open Layouts
open GenericBuilder.UnifiedFeeds
open System

// Helper function to sanitize tag names for URLs (matching TextOnlyBuilder)
let sanitizeTagForPath (tag: string) =
    let invalid = System.IO.Path.GetInvalidFileNameChars()
    let sanitized = 
        tag.ToCharArray()
        |> Array.map (fun c -> if Array.contains c invalid then '-' else c)
        |> System.String
    sanitized.Replace("\"", "").Replace("'", "").Replace(" ", "-").ToLower()

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
                li [] [a [_href "/text/search/"] [Text "Search Content"]]
                li [] [a [_href "/text/tags/"] [Text "Browse by Tags"]]
                li [] [a [_href "/text/archive/"] [Text "Archives by Date"]]
                li [] [a [_href "/text/content/"] [Text "All Content Types"]]
                li [] [a [_href "/text/feeds/"] [Text "RSS Feeds"]]
                li [] [a [_href "/text/help/"] [Text "Help & About"]]
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
    // Enhanced HTML-to-text conversion preserving semantic structure (Phase 2)
    let plainTextContent = 
        let text = 
            htmlContent
                // Preserve heading structure with clear markers
                .Replace("<h1>", "\n\n# ")
                .Replace("</h1>", "\n")
                .Replace("<h2>", "\n\n## ")
                .Replace("</h2>", "\n")
                .Replace("<h3>", "\n\n### ")
                .Replace("</h3>", "\n")
                .Replace("<h4>", "\n\n#### ")
                .Replace("</h4>", "\n")
                .Replace("<h5>", "\n\n##### ")
                .Replace("</h5>", "\n")
                .Replace("<h6>", "\n\n###### ")
                .Replace("</h6>", "\n")
                // Preserve list structures
                .Replace("<ul>", "\n")
                .Replace("</ul>", "\n")
                .Replace("<ol>", "\n")
                .Replace("</ol>", "\n")
                .Replace("<li>", "\n• ")
                .Replace("</li>", "")
                // Preserve paragraph structure
                .Replace("<p>", "\n\n")
                .Replace("</p>", "")
                // Preserve code blocks
                .Replace("<pre>", "\n\n```\n")
                .Replace("</pre>", "\n```\n")
                .Replace("<code>", "`")
                .Replace("</code>", "`")
                // Preserve blockquotes
                .Replace("<blockquote>", "\n\n> ")
                .Replace("</blockquote>", "\n")
                // Preserve emphasis
                .Replace("<strong>", "**")
                .Replace("</strong>", "**")
                .Replace("<em>", "*")
                .Replace("</em>", "*")
                .Replace("<b>", "**")
                .Replace("</b>", "**")
                .Replace("<i>", "*")
                .Replace("</i>", "*")
                // Preserve line breaks
                .Replace("<br>", "\n")
                .Replace("<br/>", "\n")
                .Replace("<br />", "\n")
                // Handle HTML entities
                .Replace("&nbsp;", " ")
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&quot;", "\"")
                .Replace("&#39;", "'")
        // Remove remaining HTML tags while preserving structure
        let cleaned = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]*>", "")
        // Clean up extra whitespace while preserving intentional spacing
        System.Text.RegularExpressions.Regex.Replace(cleaned, "\n{3,}", "\n\n").Trim()
    
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

// Tag Browsing Page
let textOnlyTagPage (tag: string) (content: UnifiedFeedItem list) =
    let taggedContent = 
        content 
        |> List.filter (fun item -> item.Tags |> Array.exists (fun t -> t.ToLower() = tag.ToLower()))
        |> List.sortByDescending (fun item -> parseItemDate item.Date)
    
    let contentHtml =
        div [] [
            h1 [] [Text $"Content tagged with \"{tag}\""]
            
            p [] [
                a [_href "/text/"] [Text "← Back to Home"]
                Text " | "
                a [_href "/text/tags/"] [Text "All Tags"]
            ]
            
            if taggedContent.IsEmpty then
                p [] [Text $"No content found with tag \"{tag}\"."]
            else
                p [] [Text $"Found {taggedContent.Length} items tagged with \"{tag}\"."]
                
                ul [_class "content-list"] [
                    for item in taggedContent do
                        let slug = extractSlugFromUrl item.Url
                        let itemDate = parseItemDate item.Date
                        li [] [
                            h3 [] [
                                a [_href $"/text/content/{item.ContentType.ToLower()}/{slug}/"] [
                                    Text item.Title
                                ]
                            ]
                            p [] [
                                span [_class "content-type"] [Text item.ContentType]
                                Text " • "
                                time [attr "datetime" (itemDate.ToString("yyyy-MM-dd"))] [
                                    Text (itemDate.ToString("MMMM d, yyyy"))
                                ]
                            ]
                            if item.Tags.Length > 1 then
                                p [] [
                                    Text "Other tags: "
                                    Text (item.Tags |> Array.filter (fun t -> t.ToLower() <> tag.ToLower()) |> String.concat ", ")
                                ]
                        ]
                ]
        ]
    
    textOnlyLayout $"Tag: {tag}" (RenderView.AsString.xmlNode contentHtml)

// All Tags Page
let textOnlyAllTagsPage (content: UnifiedFeedItem list) =
    let allTags = 
        content
        |> List.collect (fun item -> item.Tags |> Array.toList)
        |> List.groupBy (fun tag -> tag.ToLower())
        |> List.map (fun (lowerTag, tags) -> (tags |> List.head, tags |> List.length))
        |> List.sortByDescending snd
    
    let contentHtml =
        div [] [
            h1 [] [Text "All Tags"]
            
            p [] [
                a [_href "/text/"] [Text "← Back to Home"]
            ]
            
            p [] [Text $"Browse content by tags. Found {allTags.Length} unique tags."]
            
            div [_class "tag-cloud"] [
                for (tag, count) in allTags do
                    let sanitizedTag = sanitizeTagForPath tag
                    p [] [
                        a [_href $"/text/tags/{sanitizedTag}/"] [
                            Text $"{tag} ({count})"
                        ]
                    ]
            ]
        ]
    
    textOnlyLayout "All Tags" (RenderView.AsString.xmlNode contentHtml)

// Archive Page (by year/month)
let textOnlyArchivePage (content: UnifiedFeedItem list) =
    let archiveData = 
        content
        |> List.map (fun item -> (item, parseItemDate item.Date))
        |> List.filter (fun (_, date) -> date <> DateTime.MinValue)
        |> List.groupBy (fun (_, date) -> date.Year)
        |> List.sortByDescending fst
    
    let contentHtml =
        div [] [
            h1 [] [Text "Content Archive"]
            
            p [] [
                a [_href "/text/"] [Text "← Back to Home"]
            ]
            
            p [] [Text "Browse content by publication date."]
            
            for (year, yearItems) in archiveData do
                let monthGroups = 
                    yearItems
                    |> List.groupBy (fun (_, date) -> date.Month)
                    |> List.sortByDescending fst
                
                div [] [
                    h2 [] [Text (year.ToString())]
                    ul [] [
                        for (month, monthItems) in monthGroups do
                            let monthName = DateTime(year, month, 1).ToString("MMMM")
                            li [] [
                                a [_href $"/text/archive/{year}/{month:D2}/"] [
                                    Text $"{monthName} ({monthItems.Length} items)"
                                ]
                            ]
                    ]
                ]
        ]
    
    textOnlyLayout "Archive" (RenderView.AsString.xmlNode contentHtml)

// Monthly Archive Page
let textOnlyMonthlyArchivePage (year: int) (month: int) (content: UnifiedFeedItem list) =
    let monthlyContent = 
        content
        |> List.map (fun item -> (item, parseItemDate item.Date))
        |> List.filter (fun (_, date) -> date.Year = year && date.Month = month)
        |> List.sortByDescending (fun (_, date) -> date)
        |> List.map fst
    
    let monthName = DateTime(year, month, 1).ToString("MMMM yyyy")
    
    let contentHtml =
        div [] [
            h1 [] [Text $"Archive: {monthName}"]
            
            p [] [
                a [_href "/text/"] [Text "← Back to Home"]
                Text " | "
                a [_href "/text/archive/"] [Text "All Archives"]
            ]
            
            if monthlyContent.IsEmpty then
                p [] [Text $"No content found for {monthName}."]
            else
                p [] [Text $"Found {monthlyContent.Length} items from {monthName}."]
                
                ul [_class "content-list"] [
                    for item in monthlyContent do
                        let slug = extractSlugFromUrl item.Url
                        let itemDate = parseItemDate item.Date
                        li [] [
                            h3 [] [
                                a [_href $"/text/content/{item.ContentType.ToLower()}/{slug}/"] [
                                    Text item.Title
                                ]
                            ]
                            p [] [
                                span [_class "content-type"] [Text item.ContentType]
                                Text " • "
                                time [attr "datetime" (itemDate.ToString("yyyy-MM-dd"))] [
                                    Text (itemDate.ToString("MMMM d, yyyy"))
                                ]
                            ]
                            if item.Tags.Length > 0 then
                                p [] [
                                    Text "Tags: "
                                    Text (item.Tags |> String.concat ", ")
                                ]
                        ]
                ]
        ]
    
    textOnlyLayout $"{monthName} Archive" (RenderView.AsString.xmlNode contentHtml)

// Basic Search Page
let textOnlySearchPage (searchQuery: string option) (searchResults: UnifiedFeedItem list option) =
    let contentHtml =
        div [] [
            h1 [] [Text "Search Content"]
            
            p [] [
                a [_href "/text/"] [Text "← Back to Home"]
            ]
            
            // Basic search form (works without JavaScript)
            form [_action "/text/search/"; _method "GET"] [
                label [_for "q"] [Text "Search for content:"]
                br []
                input [_type "text"; _id "q"; _name "q"; _placeholder "Enter search terms..."; 
                       attr "value" (defaultArg searchQuery "")]
                br []
                input [_type "submit"; attr "value" "Search"]
            ]
            
            match searchQuery with
            | None -> 
                div [] [
                    h2 [] [Text "How to Search"]
                    ul [] [
                        li [] [Text "Enter keywords to search across all content"]
                        li [] [Text "Search includes titles, content, and tags"]
                        li [] [Text "Search is case-insensitive"]
                        li [] [Text "Multiple words will find content containing all terms"]
                    ]
                ]
            | Some query when System.String.IsNullOrWhiteSpace(query) ->
                p [] [Text "Please enter a search term."]
            | Some query ->
                match searchResults with
                | None | Some [] ->
                    div [] [
                        h2 [] [Text $"No results found for \"{query}\""]
                        p [] [Text "Try different keywords or check your spelling."]
                    ]
                | Some results ->
                    div [] [
                        h2 [] [Text $"Search Results for \"{query}\""]
                        p [] [Text $"Found {results.Length} results."]
                        
                        ul [_class "content-list"] [
                            for item in results do
                                let slug = extractSlugFromUrl item.Url
                                let itemDate = parseItemDate item.Date
                                li [] [
                                    h3 [] [
                                        a [_href $"/text/content/{item.ContentType.ToLower()}/{slug}/"] [
                                            Text item.Title
                                        ]
                                    ]
                                    p [] [
                                        span [_class "content-type"] [Text item.ContentType]
                                        Text " • "
                                        time [attr "datetime" (itemDate.ToString("yyyy-MM-dd"))] [
                                            Text (itemDate.ToString("MMMM d, yyyy"))
                                        ]
                                    ]
                                    if item.Tags.Length > 0 then
                                        p [] [
                                            Text "Tags: "
                                            Text (item.Tags |> String.concat ", ")
                                        ]
                                ]
                        ]
                    ]
        ]
    
    let pageTitle = 
        match searchQuery with
        | Some query when not (System.String.IsNullOrWhiteSpace(query)) -> $"Search: {query}"
        | _ -> "Search"
    
    textOnlyLayout pageTitle (RenderView.AsString.xmlNode contentHtml)
