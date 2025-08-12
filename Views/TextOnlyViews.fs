module TextOnlyViews

open Giraffe.ViewEngine
open Domain
open Layouts
open GenericBuilder.UnifiedFeeds
open System
open System.IO
open System.Text.RegularExpressions
open MarkdownService

// Text-Only Content Processing
module TextOnlyContentProcessor =
    
    // Helper function to load and process markdown files
    let loadMarkdownContent (fileName: string) =
        let filePath = Path.Join("_src", fileName)
        if File.Exists(filePath) then
            try
                let content = File.ReadAllText(filePath)
                // Remove YAML front matter if present
                let content = 
                    if content.StartsWith("---") then
                        let lines = content.Split('\n')
                        let endIndex = lines |> Array.skip 1 |> Array.findIndex (fun line -> line.Trim() = "---")
                        String.Join("\n", lines |> Array.skip (endIndex + 2))
                    else
                        content
                convertMdToHtml content
            with
            | ex -> 
                printfn $"Error loading markdown from {filePath}: {ex.Message}"
                $"<p>Content not available. Error: {ex.Message}</p>"
        else
            printfn $"Markdown file not found: {filePath}"
            "<p>Content not available.</p>"
    
    // Replace only images with clickable text descriptions, keeping all other HTML
    let replaceImagesWithText (content: string) =
        if String.IsNullOrWhiteSpace(content) then ""
        else
            let mutable result = content
            
            // Replace images with alt text first (more specific pattern)
            let imgWithAltPattern = @"<img[^>]*src\s*=\s*[""']([^""']*)[""'][^>]*alt\s*=\s*[""']([^""']*)[""'][^>]*/?>"
            result <- Regex.Replace(result, imgWithAltPattern, fun m ->
                let src = m.Groups.[1].Value
                let alt = m.Groups.[2].Value
                let description = if String.IsNullOrWhiteSpace(alt) then "Image" else alt
                let fullUrl = if src.StartsWith("http") then src else $"https://www.luisquintanilla.me{src}"
                $"""<a href="{fullUrl}" target="_blank">[Image: {description}]</a>"""
            )
            
            // Handle images without alt text (catch remaining images)
            let imgWithoutAltPattern = @"<img[^>]*src\s*=\s*[""']([^""']*)[""'][^>]*/?>"
            result <- Regex.Replace(result, imgWithoutAltPattern, fun m ->
                let src = m.Groups.[1].Value
                let fullUrl = if src.StartsWith("http") then src else $"https://www.luisquintanilla.me{src}"
                $"""<a href="{fullUrl}" target="_blank">[Image]</a>"""
            )
            
            result

    // Convert certain internal links to text-only equivalents
    let convertLinksToTextOnly (content: string) =
        if String.IsNullOrWhiteSpace(content) then ""
        else
            let mutable result = content
            
            // Define mappings for internal pages that have text-only equivalents
            let linkMappings = [
                ("/uses", "/text/uses/")
                ("/colophon", "/text/colophon/")
                ("/contact", "/text/contact/")
                ("/about", "/text/about/")
                ("/feed", "/text/feeds/")
            ]
            
            // Replace each mapping
            for (originalPath, textOnlyPath) in linkMappings do
                // Pattern to match href attributes with the original path
                let pattern = $@"href\s*=\s*[""']{Regex.Escape(originalPath)}[""']"
                let replacement = $"href=\"{textOnlyPath}\""
                result <- Regex.Replace(result, pattern, replacement, RegexOptions.IgnoreCase)
            
            result

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
    let parts = url.Split('/', System.StringSplitOptions.RemoveEmptyEntries)
    if parts.Length >= 2 then
        parts.[parts.Length - 1] // Get the last part (the actual slug)
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
                li [] [a [_href "/text/content/bookmarks/"] [Text "Bookmarks"]]
                li [] [a [_href "/text/content/snippets/"] [Text "Code Snippets"]]
                li [] [a [_href "/text/content/wiki/"] [Text "Wiki & Knowledge Base"]]
                li [] [a [_href "/text/content/presentations/"] [Text "Presentations"]]
                li [] [a [_href "/text/content/reviews/"] [Text "Book Reviews"]]
                li [] [a [_href "/text/content/albums/"] [Text "Photo Albums"]]
                li [] [a [_href "/text/content/media/"] [Text "Media & Files"]]
            ]
            
            h2 [] [Text "Quick Navigation"]
            ul [] [
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
        | "bookmarks" -> "Bookmarks"
        | "media" -> "Media & Files"
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

// Individual Content Page - Simplified to match main website pattern
let textOnlyContentPage (content: UnifiedFeedItem) (htmlContent: string) =
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
            
            // Text-only content rendering with image replacement only
            div [_class "content"] [
                rawText (TextOnlyContentProcessor.replaceImagesWithText htmlContent)
            ]
        ]
    
    textOnlyLayout content.Title (RenderView.AsString.xmlNode contentHtml)

// Special presentation page with resources
let textOnlyPresentationPage (presentation: Presentation) (htmlContent: string) =
    let publishDate = DateTimeOffset.Parse(presentation.Metadata.Date)
    let slug = extractSlugFromUrl $"/resources/presentations/{Path.GetFileNameWithoutExtension(presentation.FileName)}/"
    
    let contentHtml =
        div [] [
            h1 [] [Text presentation.Metadata.Title]
            
            div [_class "content-meta"] [
                div [_class "content-type"] [Text "presentations"]
                time [attr "datetime" (publishDate.ToString("yyyy-MM-dd"))] [
                    Text (publishDate.ToString("MMMM d, yyyy"))
                ]
                if not (String.IsNullOrEmpty(presentation.Metadata.Tags)) then
                    let tags = presentation.Metadata.Tags.Split(',') |> Array.map (fun s -> s.Trim())
                    if tags.Length > 0 then
                        p [] [
                            Text "Tags: "
                            Text (String.concat ", " tags)
                        ]
            ]
            
            p [] [
                a [_href "/text/"] [Text "← Home"]
                Text " | "
                a [_href "/text/content/presentations/"] [Text "← All Presentations"]
                Text " | "
                a [_href $"/resources/presentations/{Path.GetFileNameWithoutExtension(presentation.FileName)}/"] [Text "View Full Version"]
            ]
            
            // Text-only content rendering with image replacement only
            div [_class "content"] [
                rawText (TextOnlyContentProcessor.replaceImagesWithText htmlContent)
            ]
            
            // Resources section - matching main site structure
            if presentation.Metadata.Resources.Length > 0 then
                hr []
                h2 [] [Text "Resources"]
                ul [] [
                    for resource in presentation.Metadata.Resources do
                        li [] [
                            a [_href resource.Url; _target "_blank"] [Text resource.Text]
                        ]
                ]
        ]
    
    textOnlyLayout presentation.Metadata.Title (RenderView.AsString.xmlNode contentHtml)

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
    let markdownHtml = TextOnlyContentProcessor.loadMarkdownContent "about.md"
    let processedHtml = 
        markdownHtml
        |> TextOnlyContentProcessor.replaceImagesWithText 
        |> TextOnlyContentProcessor.convertLinksToTextOnly
    
    let contentHtml =
        div [] [
            p [] [
                a [_href "/text/"] [Text "← Back to Home"]
                Text " | "
                a [_href "/about"] [Text "View Full About Page"]
            ]
            
            // Rendered markdown content with text-only processing
            div [_class "content"] [
                rawText processedHtml
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
                Text "Want to get in touch? Visit my "
                a [_href "/text/contact"] [Text "contact page"]
                Text " for all the ways to reach me."
            ]
        ]
    
    textOnlyLayout "About" (RenderView.AsString.xmlNode contentHtml)

// Contact Page
let textOnlyContactPage =
    let markdownHtml = TextOnlyContentProcessor.loadMarkdownContent "contact.md"
    let processedHtml = 
        markdownHtml
        |> TextOnlyContentProcessor.replaceImagesWithText 
        |> TextOnlyContentProcessor.convertLinksToTextOnly
    
    let contentHtml =
        div [] [
            p [] [
                a [_href "/text/"] [Text "← Back to Home"]
                Text " | "
                a [_href "/contact"] [Text "View Full Contact Page"]
            ]
            
            // Rendered markdown content with text-only processing
            div [_class "content"] [
                rawText processedHtml
            ]
            
            h2 [] [Text "Response Time"]
            p [] [
                Text "I typically respond to emails within 24-48 hours. "
                Text "For urgent matters, Mastodon or LinkedIn may be faster."
            ]
        ]
    
    textOnlyLayout "Contact" (RenderView.AsString.xmlNode contentHtml)

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
            h1 [] [Text "Feeds & Content Discovery"]
            
            p [] [
                a [_href "/text/"] [Text "← Back to Home"]
                Text " | "
                a [_href "/feed"] [Text "View Full Feeds Page"]
            ]
            
            p [] [
                Text "Subscribe to RSS feeds to stay updated with new content. "
                Text "All feeds are available in standard RSS 2.0 format. "
                Text "You can also browse recent content on the homepage to get a sense of what I publish."
            ]
            
            p [] [
                a [_href "/feed/index.opml"] [Text "📄 Download All Feeds (OPML)"]
            ]
            
            h2 [] [Text "Featured Feeds"]
            
            h3 [] [Text "Everything Feed"]
            p [] [
                Text "All content updates in one feed - blog posts, notes, responses, bookmarks, and more."
            ]
            p [] [
                Text "Feed URL: "
                a [_href "/all.rss"] [Text "/all.rss"]
                Text " (20 most recent items, all content types)"
            ]
            
            h3 [] [Text "Blog"]
            p [] [Text "Long-form posts. Mainly around tech topics."]
            p [] [
                Text "Feed URL: "
                a [_href "/blog.rss"] [Text "/blog.rss"]
            ]
            
            h3 [] [Text "Microblog Feed"]
            p [] [Text "Microblog-like short posts containing different types of content such as notes."]
            p [] [
                Text "Feed URL: "
                a [_href "/microblog.rss"] [Text "/microblog.rss"]
            ]
            
            h3 [] [Text "Response Feed"]
            p [] [Text "Microblog-like short posts containing replies, reshares (repost), and favorites (likes)."]
            p [] [
                Text "Feed URL: "
                a [_href "/responses.rss"] [Text "/responses.rss"]
            ]
            
            h2 [] [Text "All Site Feeds"]
            p [] [Text "Subscribe to specific content types:"]
            
            h3 [] [Text "Bookmarks"]
            p [] [Text "Links to interesting articles, tools, and resources I come across."]
            p [] [
                Text "Feed URL: "
                a [_href "/bookmarks.rss"] [Text "/bookmarks.rss"]
                Text " (filter by bookmark type)"
            ]
            
            h3 [] [Text "Media"]
            p [] [Text "Photo albums and media collections."]
            p [] [
                Text "Feed URL: "
                a [_href "/media.rss"] [Text "/media.rss"]
                Text " (includes media posts)"
            ]
            
            h3 [] [Text "Reviews"]
            p [] [Text "Book reviews and other content critiques."]
            p [] [
                Text "Feed URL: "
                a [_href "/reviews.rss"] [Text "/reviews.rss"]
                Text " (includes review posts)"
            ]
            
            h3 [] [Text "Individual Topic Feeds"]
            p [] [
                Text "Every tag on this site has its own RSS feed. Browse "
                a [_href "/text/tags/"] [Text "all available tags"]
                Text " and click on any tag to access its dedicated feed."
            ]
            p [] [
                Text "Example: "
                a [_href "/tags/rss/feed.xml"] [Text "/tags/rss/feed.xml"]
            ]
            
            h2 [] [Text "External Platform Feeds"]
            p [] [Text "I also publish content on other platforms. These feeds help you follow my activity there:"]
            
            h3 [] [Text "Mastodon"]
            p [] [Text "RSS Feed for Mastodon posts."]
            p [] [
                Text "Feed URL: "
                a [_href "/mastodon.rss"] [Text "/mastodon.rss"]
            ]
            
            h3 [] [Text "Bluesky"]
            p [] [
                Text "Feed URL: "
                a [_href "/bluesky.rss"] [Text "/bluesky.rss"]
            ]
            
            h3 [] [Text "YouTube Channel"]
            p [] [Text "Video posts on YouTube"]
            p [] [
                Text "Feed URL: "
                a [_href "/youtube.rss"] [Text "/youtube.rss"]
            ]
            
            h2 [] [Text "New to RSS?"]
            p [] [
                Text "RSS (Really Simple Syndication) lets you follow websites and content creators "
                Text "without relying on social media algorithms or email newsletters cluttering your inbox. "
                Text "It works with everything from blogs and podcasts to YouTube channels, news sites, "
                Text "newsletters, forums, and social platforms like Mastodon and Bluesky."
            ]
            
            h3 [] [Text "Quick Start Guide"]
            ol [] [
                li [] [
                    strong [] [Text "Choose a Feed Reader: "]
                    Text "Popular options include "
                    a [_href "https://newsblur.com/"] [Text "NewsBlur"]
                    Text ", "
                    a [_href "https://www.inoreader.com/"] [Text "Inoreader"]
                    Text ", "
                    a [_href "https://netnewswire.com/"] [Text "NetNewsWire"]
                    Text " (Mac), or "
                    a [_href "https://feedly.com/"] [Text "Feedly"]
                    Text ". Most have free tiers to get started."
                ]
                li [] [
                    strong [] [Text "Subscribe to Feeds: "]
                    Text "Copy any feed URL from this page and paste it into your reader. "
                    Text "Or use the OPML file above to subscribe to everything at once."
                ]
                li [] [
                    strong [] [Text "Start Reading: "]
                    Text "New posts appear in your feed reader automatically. "
                    Text "Read what interests you, skip what doesn't - you're in complete control."
                ]
            ]
            
            h3 [] [Text "About OPML Files"]
            p [] [
                Text "OPML (Outline Processor Markup Language) is like a playlist for RSS feeds. "
                Text "Instead of adding feeds one by one, you can import an OPML file to subscribe to "
                Text "entire collections with one click. That's why I provide an "
                a [_href "/feed/index.opml"] [Text "OPML file with all my feeds"]
                Text " and use it for my "
                a [_href "/text/collections/starter-packs/"] [Text "starter packs"]
                Text " - curated feed collections around specific topics."
            ]
            
            p [] [
                Text "Learn more: "
                a [_href "https://aboutfeeds.com/"] [Text "About Feeds"]
                Text " • "
                a [_href "/text/content/posts/rediscovering-rss-user-freedom/"] [Text "Why I Use RSS"]
                Text " • "
                a [_href "/text/collections/starter-packs/"] [Text "Browse Starter Packs"]
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

// Text-Only Starter Packs Pages
let textOnlyStarterPacksPage =
    let contentHtml =
        div [] [
            h1 [] [Text "Starter Packs"]
            
            p [] [
                a [_href "/text/"] [Text "← Back to Home"]
                Text " | "
                a [_href "/collections/starter-packs"] [Text "View Full Starter Packs Page"]
            ]
            
            p [] [Text "Welcome to my curated collection of RSS Starter Packs."]
            
            p [] [
                Text "Inspired by "
                a [_href "https://bsky.social/about/blog/06-26-2024-starter-packs"] [Text "BlueSky's Starter Pack feature"]
                Text ", I've created a set of OPML bundles to help you discover and follow content across the open web. "
                Text "Whether it's a newsletter, blog, podcast, YouTube channel, Fediverse instance, or BlueSky profile—"
                Text "if it has an RSS feed and aligns with the topic, it's included."
            ]
            
            p [] [Text "By leveraging open standards like RSS and OPML, my goal is to:"]
            
            ul [] [
                li [] [Text "Make it easy and convenient for others to discover, explore, and subscribe to content using their own feed readers"]
                li [] [
                    Text "Promote the open web and open standards. When building on open standards, it doesn't matter what platform or software you use. "
                    a [_href "/posts/rediscovering-rss-user-freedom"] [Text "Both publishers and subscribers have the freedom to choose"]
                    Text "."
                ]
            ]
            
            h2 [] [Text "Available Packs"]
            ul [] [
                li [] [
                    a [_href "/text/collections/starter-packs/ai/"] [Text "AI Starter Pack"]
                    Text " - AI resources, news, and research feeds"
                ]
            ]
        ]
    
    textOnlyLayout "Starter Packs" (RenderView.AsString.xmlNode contentHtml)

let textOnlyAIStarterPackPage =
    let contentHtml =
        div [] [
            h1 [] [Text "AI Starter Pack"]
            
            p [] [
                a [_href "/text/collections/starter-packs/"] [Text "← Back to Starter Packs"]
                Text " | "
                a [_href "/collections/starter-packs/ai"] [Text "View Full AI Pack Page"]
            ]
            
            p [] [Text "This is a list of AI resources I use to stay on top of AI news."]
            
            p [] [
                Text "You can subscribe to any of the individual feeds in your preferred RSS reader using the RSS feed links below. "
                Text "Want to subscribe to all of them? Use the "
                a [_href "/collections/starter-packs/ai/index.opml"] [Text "OPML file"]
                Text " if your RSS reader supports "
                a [_href "http://opml.org/"] [Text "OPML"]
                Text "."
            ]
            
            h2 [] [Text "Included Feeds"]
            ul [] [
                li [] [
                    strong [] [Text "Latent Space"]
                    Text " - "
                    a [_href "https://www.latent.space/"] [Text "Website"]
                    Text " / "
                    a [_href "https://www.latent.space/feed/"] [Text "RSS Feed"]
                ]
                li [] [
                    strong [] [Text "OpenAI AI Blog"]
                    Text " - "
                    a [_href "https://openai.com/news/"] [Text "Website"]
                    Text " / "
                    a [_href "https://openai.com/news/rss.xml"] [Text "RSS Feed"]
                ]
                li [] [
                    strong [] [Text "Google AI Blog"]
                    Text " - "
                    a [_href "https://blog.google/technology/ai/"] [Text "Website"]
                    Text " / "
                    a [_href "https://blog.google/technology/ai/rss/"] [Text "RSS Feed"]
                ]
                li [] [
                    strong [] [Text "AWS Machine Learning Blog"]
                    Text " - "
                    a [_href "https://aws.amazon.com/blogs/machine-learning/"] [Text "Website"]
                    Text " / "
                    a [_href "https://aws.amazon.com/blogs/machine-learning/feed/"] [Text "RSS Feed"]
                ]
                li [] [
                    strong [] [Text "Microsoft Research Blog"]
                    Text " - "
                    a [_href "https://www.microsoft.com/en-us/research/blog/"] [Text "Website"]
                    Text " / "
                    a [_href "https://www.microsoft.com/en-us/research/feed/"] [Text "RSS Feed"]
                ]
                li [] [
                    strong [] [Text "Google Research Blog"]
                    Text " - "
                    a [_href "https://research.google/blog/"] [Text "Website"]
                    Text " / "
                    a [_href "https://research.google/blog/rss/"] [Text "RSS Feed"]
                ]
                li [] [
                    strong [] [Text "Simon Willison's Weblog"]
                    Text " - "
                    a [_href "https://simonwillison.net/"] [Text "Website"]
                    Text " / "
                    a [_href "https://simonwillison.net/atom/everything/"] [Text "RSS Feed"]
                ]
                li [] [
                    strong [] [Text "The AI Daily Brief"]
                    Text " - "
                    a [_href "https://www.podchaser.com/podcasts/the-ai-daily-brief-formerly-th-5260567"] [Text "Website"]
                    Text " / "
                    a [_href "https://anchor.fm/s/f7cac464/podcast/rss"] [Text "RSS Feed"]
                ]
                li [] [
                    strong [] [Text "The TWIML AI Podcast"]
                    Text " - "
                    a [_href "https://twimlai.com/"] [Text "Website"]
                    Text " / "
                    a [_href "https://feeds.megaphone.fm/MLN2155636147"] [Text "RSS Feed"]
                ]
                li [] [
                    strong [] [Text "Practical AI"]
                    Text " - "
                    a [_href "https://changelog.com/practicalai"] [Text "Website"]
                    Text " / "
                    a [_href "https://changelog.com/practicalai/feed"] [Text "RSS Feed"]
                ]
            ]
            
            h2 [] [Text "How to Use This Pack"]
            p [] [Text "Option 1: Subscribe to individual feeds by clicking the RSS Feed links above."]
            p [] [
                Text "Option 2: Download the "
                a [_href "/collections/starter-packs/ai/index.opml"] [Text "OPML file"]
                Text " and import it into your RSS reader to subscribe to all feeds at once."
            ]
        ]
    
    textOnlyLayout "AI Starter Pack" (RenderView.AsString.xmlNode contentHtml)

// Uses Page
let textOnlyUsesPage =
    let markdownHtml = TextOnlyContentProcessor.loadMarkdownContent "uses.md"
    let processedHtml = 
        markdownHtml
        |> TextOnlyContentProcessor.replaceImagesWithText 
        |> TextOnlyContentProcessor.convertLinksToTextOnly
    
    let contentHtml =
        div [] [
            p [] [
                a [_href "/text/"] [Text "← Back to Home"]
                Text " | "
                a [_href "/uses"] [Text "View Full Uses Page"]
            ]
            
            // Rendered markdown content with text-only processing
            div [_class "content"] [
                rawText processedHtml
            ]
        ]
    
    textOnlyLayout "Uses" (RenderView.AsString.xmlNode contentHtml)

// Colophon Page
let textOnlyColophonPage =
    let markdownHtml = TextOnlyContentProcessor.loadMarkdownContent "colophon.md"
    let processedHtml = 
        markdownHtml
        |> TextOnlyContentProcessor.replaceImagesWithText 
        |> TextOnlyContentProcessor.convertLinksToTextOnly
    
    let contentHtml =
        div [] [
            p [] [
                a [_href "/text/"] [Text "← Back to Home"]
                Text " | "
                a [_href "/colophon"] [Text "View Full Colophon"]
            ]
            
            // Rendered markdown content with text-only processing
            div [_class "content"] [
                rawText processedHtml
            ]
        ]
    
    textOnlyLayout "Colophon" (RenderView.AsString.xmlNode contentHtml)
