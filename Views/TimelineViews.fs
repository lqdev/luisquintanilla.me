module TimelineViews

open Giraffe.ViewEngine
open System
open System.IO
open Domain
open ComponentViews
open CollectionViews
open MarkdownService
open Markdig
open Markdig.Syntax
open CustomBlocks
open HtmlHelpers

/// Sanitize tag names for URL usage while preserving display text
let private sanitizeTagForUrl (tag: string) =
    tag.Replace("#", "sharp").Replace("/", "-").Replace(" ", "-").Replace("\"", "")

/// F7 (cheap slice): single copy of the timeline card-HTML cleanup that was duplicated
/// across the initial-render and progressive-loading paths (4 copies). Takes already-rendered
/// HTML and strips the article wrapper and the title h1 / h2-link (already shown by the card
/// header), then neutralizes <script> tags. Callers convert markdown → HTML first (some use
/// createSimplifiedReviewContent for reviews). The full structural fix (a RenderedContent
/// product so consumers compose instead of regex-strip) is deferred to B2.
let private cleanCardHtml (html: string) =
    // Remove all article opening tags with any class
    let removeArticleStart = System.Text.RegularExpressions.Regex.Replace(html, @"<article[^>]*>", "")
    // Remove all article closing tags
    let removeArticleEnd = removeArticleStart.Replace("</article>", "")
    // Remove duplicate h1/h2 titles (common source of duplication)
    let removeTitles = System.Text.RegularExpressions.Regex.Replace(removeArticleEnd, @"<h1[^>]*>.*?</h1>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
    // Remove h2 title links from CardHtml (fixes duplicate headings while preserving content h2s)
    let removeTitleLinks = System.Text.RegularExpressions.Regex.Replace(removeTitles, @"<h2[^>]*><a[^>]*>.*?</a></h2>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
    // Additional cleaning to prevent HTML parsing issues
    removeTitleLinks.Replace("<script", "&lt;script").Replace("</script>", "&lt;/script&gt;")

/// Extract item type from review content for badge display
let private extractReviewItemType (content: string) =
    try
        // First try: extract from data-item-type attribute in hidden span (new pattern)
        if content.Contains("data-item-type=") then
            let startTag = "data-item-type=\""
            let startIndex = content.IndexOf(startTag)
            if startIndex >= 0 then
                let startIndex = startIndex + startTag.Length
                let endIndex = content.IndexOf("\"", startIndex)
                if endIndex > startIndex then
                    let itemType = content.Substring(startIndex, endIndex - startIndex).Trim()
                    // Guard against empty string before Substring call
                    if itemType.Length > 0 then
                        Some (itemType.Substring(0, 1).ToUpper() + itemType.Substring(1).ToLower())
                    else None
                else None
            else None
        // Fallback: try old pattern with visible badge (for backwards compatibility)
        elif content.Contains("item-type-badge") then
            let startTag = "item-type-badge badge bg-secondary\">"
            let endTag = "</span>"
            let startIndex = content.IndexOf(startTag)
            if startIndex >= 0 then
                let startIndex = startIndex + startTag.Length
                let endIndex = content.IndexOf(endTag, startIndex)
                if endIndex > startIndex then
                    let itemType = content.Substring(startIndex, endIndex - startIndex).Trim()
                    // Guard against empty string before Substring call
                    if itemType.Length > 0 then
                        Some (itemType.Substring(0, 1).ToUpper() + itemType.Substring(1).ToLower())
                    else None
                else None
            else None
        else None
    with
    | ex -> 
        None

/// Extract simplified review content for timeline cards
/// Shows only: title, image, rating, scale, and summary extracted from already-rendered HTML
let private createSimplifiedReviewContent (content: string) =
    try
        // The content is already HTML with rendered custom-review-block
        if content.Contains("custom-review-block") then
            // Extract key information from the rendered HTML
            let extractFromHtml (startPattern: string) (endPattern: string) =
                let startIndex = content.IndexOf(startPattern)
                if startIndex >= 0 then
                    let startIndex = startIndex + startPattern.Length
                    let endIndex = content.IndexOf(endPattern, startIndex)
                    if endIndex > startIndex then
                        Some (content.Substring(startIndex, endIndex - startIndex).Trim())
                    else None
                else None
            
            // Extract title
            let title = extractFromHtml "review-title p-name\">" "</h3>"
            
            // Extract image
            let imageHtml = 
                if content.Contains("review-image") then
                    extractFromHtml "review-image\"><img src=\"" "\" alt="
                    |> Option.map (fun imgUrl -> 
                        let altText = title |> Option.defaultValue "Review Item"
                        $"""<div class="simplified-review-image">
                <img src="{imgUrl}" alt="{altText}" class="review-cover" style="max-width: 150px; height: auto;" />
            </div>""")
                    |> Option.defaultValue ""
                else ""
            
            // Extract rating
            let ratingHtml = 
                if content.Contains("review-rating") then
                    extractFromHtml "review-rating p-rating\"><strong>Rating:</strong> " "</div>"
                    |> Option.map (fun rating -> 
                        $"""<div class="simplified-review-rating">
                <strong>Rating:</strong> {rating}
            </div>""")
                    |> Option.defaultValue ""
                else ""
            
            // Extract summary
            let summaryHtml = 
                if content.Contains("review-summary") then
                    extractFromHtml "review-summary p-summary\">" "</div>"
                    |> Option.map (fun summary -> 
                        $"""<div class="simplified-review-summary">
                {summary}
            </div>""")
                    |> Option.defaultValue ""
                else ""
            
            let titleText = title |> Option.defaultValue "Review"
            
            // Construct the simplified review card
            $"""<div class="simplified-review-card">
            <h3 class="simplified-review-title">{titleText}</h3>
            {imageHtml}
            {ratingHtml}
            {summaryHtml}
        </div>"""
        else
            // No custom review block found, return empty
            ""
    with
    | ex -> 
        // On any error, return empty content to avoid breaking the page
        ""

// Reusable avatar flip-card component (pure CSS, native disclosure).
//
// Renders the avatar as the front face of a 3D card built on a native
// `<details>`/`<summary>` element. Clicking, tapping, or activating the
// avatar via Enter/Space (native disclosure semantics) flips the card to
// reveal a pre-rendered QR code on the back face. Clicking the avatar a
// second time flips back. The flip is driven entirely by CSS via the
// `details[open]` selector — no JavaScript and no runtime QR generation.
// Phase 3 (post-#2389) extended this pattern to every content page, so the
// layout no longer loads `qr-code-styling` from a CDN at all — per-page
// QRs are now pre-rendered SVGs revealed by a `<details>` disclosure (see
// `ComponentViews.qrCodeButton`).
//
// Architecture:
//   - The QR SVG is pre-rendered at build time by `Services/QRStyled.fs`,
//     invoked from `Builder.copyStaticFiles` (see `Builder.fs:144-149`).
//   - Output lands at `_public/assets/images/contact/qr-home.svg`, served
//     directly as a static asset (path embedded by `avatarFlipCard` below,
//     with a `?v=<contentHash>` cache-buster from `QRStyled.HomeCacheKey`).
//   - 3D-flip + shape-morph styles live in `_src/css/custom/timeline.css`
//     (selectors prefixed with `details.avatar-flip`).
//   - The card visually morphs from circle (front, avatar) to rounded-square
//     (back, QR) during the flip, which gives the QR its natural square real
//     estate and avoids clipping the corner finder patterns.
//
// Accessibility:
//   - Native `<details>`/`<summary>` provides `aria-expanded` automatically,
//     keyboard activation (Enter/Space) for free, and inert closed content
//     for screen readers — no custom ARIA management required.
//   - Works fully without JavaScript: the flip, the QR, and the dismiss-by-
//     second-click all behave as expected.
//
// Trade-offs (vs. the previous JS implementation):
//   - LOST: Escape-key dismissal and click-outside dismissal — pure CSS
//     cannot read keyboard events, and the only pure-CSS outside-click
//     workaround intercepts all page clicks (unacceptable).
//   - PRESERVED: click avatar again to dismiss (standard disclosure pattern).
//
// Parameters:
//   altText      : alt text for the avatar image (front face)
//   flipUrl      : kept for API compatibility with existing call sites; the
//                  QR destination is baked into the pre-rendered SVG, so this
//                  parameter is currently unused for the home QR. Kept so the
//                  function signature stays stable if we later support multiple
//                  pre-rendered destinations.
//   ariaLabel    : accessible label applied to the <summary> element so screen
//                  readers announce the affordance (e.g. "Show QR code for homepage").
//   destinationSr: human-readable destination name used in the QR's alt text
//                  (e.g., "Luis Quintanilla's homepage").
let avatarFlipCard (altText: string) (flipUrl: string) (ariaLabel: string) (destinationSr: string) =
    // `flipUrl` is intentionally unused here — the QR SVG is pre-rendered for
    // the homepage. The parameter is preserved so call sites don't need to
    // change and so we can extend to multiple destinations later.
    ignore flipUrl
    tag "details" [ _class "avatar-flip" ] [
        tag "summary" [ _class "avatar-flip-summary"; attr "aria-label" ariaLabel ] [
            div [ _class "avatar-flip-inner" ] [
                // Front face: the avatar image
                div [ _class "avatar-flip-front" ] [
                    img [ _src "/avatar.png"
                          _alt altText
                          _class "rounded-circle avatar-flip-image"
                          _height "150"
                          _width "150" ]
                ]
                // Back face: pre-rendered QR code. Decorative `aria-hidden`
                // because the QR is described by the summary's aria-label and
                // by the `<details>` disclosure semantics.
                //
                // Cache-buster (`?v=<hash>`): the PWA service worker uses a
                // `cacheFirstStaleWhileRevalidate` strategy for SVG assets,
                // which would otherwise serve a stale homepage QR on first
                // load after a style/URL change. The hash comes from the
                // generated SVG content, so it only changes when the SVG
                // actually changes.
                div [ _class "avatar-flip-back" ] [
                    let cacheKey =
                        if System.String.IsNullOrEmpty Services.QRStyled.HomeCacheKey
                        then ""
                        else "?v=" + Services.QRStyled.HomeCacheKey
                    img [ _src ("/assets/images/contact/qr-home.svg" + cacheKey)
                          _alt (sprintf "QR code linking to %s" destinationSr)
                          _class "avatar-flip-qr" ]
                ]
            ]
        ]
    ]

// New stratified timeline homepage view - takes 5 items from each content type initially
// Progressive loading is content-type aware for better filtering experience
let timelineHomeViewStratified (initialItems: UnifiedFeeds.UnifiedFeedItem array) (remainingItemsByType: (string * UnifiedFeeds.UnifiedFeedItem list) list) (pinnedUrls: Set<string>) =
    
    div [ _class "h-feed unified-timeline" ] [
        // Header with personal intro and content filters
        header [ _class "timeline-header text-center p-4" ] [
            div [ _class "avatar-section mb-3" ] [
                avatarFlipCard "Luis Quintanilla Avatar Image" "/" "Show QR code for homepage" "Luis Quintanilla's homepage"
                div [ _class "mt-2" ] [
                    h1 [ _class "p-name" ] [
                        str "Hi, I'm "
                        a [ _href "/about"; _class "author-link" ] [ Text "Luis" ]
                        span [] [ Text " &#x1F44B;" ]
                    ]
                    p [ _class "tagline" ] [ Text "Latest updates from across the site" ]
                ]
            ]
            
            // Content type filters (will be styled as buttons in CSS)
            div [ _class "content-filters mt-3"; _id "contentFilters" ] [
                button [ _class "filter-btn active"; attr "data-filter" "all"; _type "button" ] [ Text "All" ]
                button [ _class "filter-btn"; attr "data-filter" "posts"; _type "button" ] [ Text "Blog Posts" ]
                button [ _class "filter-btn"; attr "data-filter" "notes"; _type "button" ] [ Text "Notes" ]
                button [ _class "filter-btn"; attr "data-filter" "responses"; _type "button" ] [ Text "Responses" ]
                button [ _class "filter-btn"; attr "data-filter" "reviews"; _type "button" ] [ Text "Reviews" ]
                button [ _class "filter-btn"; attr "data-filter" "bookmarks"; _type "button" ] [ Text "Bookmarks" ]
                button [ _class "filter-btn"; attr "data-filter" "media"; _type "button" ] [ Text "Media" ]
            ]
        ]
        
        // Timeline content area with stratified loading
        main [ _class "timeline-content" ] [
            // Initial stratified content (5 items from each content type)
            div [ _class "initial-content"; _id "initialContent" ] [
                for item in initialItems do
                    let fileName = Path.GetFileNameWithoutExtension(item.Url)
                    let getProperPermalink (contentType: string) (fileName: string) =
                        match contentType with
                        | "posts" -> $"/posts/{fileName}/"
                        | "notes" -> $"/notes/{fileName}/"
                        | "responses" -> $"/responses/{fileName}/"
                        | "bookmarks" -> $"/bookmarks/{fileName}/"  // Bookmarks are responses but filtered separately
                        | "snippets" -> $"/resources/snippets/{fileName}/"
                        | "wiki" -> $"/resources/wiki/{fileName}/"
                        | "presentations" -> $"/resources/presentations/{fileName}/"
                        | "reviews" -> $"/reviews/{fileName}/"
                        | "streams" -> $"/streams/{fileName}/"
                        | "media" -> $"/media/{fileName}/"
                        // Specific response types also route to /responses/
                        | "star" | "reply" | "reshare" | "rsvp" -> $"/responses/{fileName}/"
                        | "bookmark" -> $"/bookmarks/{fileName}/"
                        | "ai-memex" -> $"/resources/ai-memex/{fileName}/"
                        | _ -> $"/{contentType}/{fileName}/"
                    
                    let properPermalink = getProperPermalink item.ContentType fileName
                    
                    // Check if this item is pinned
                    let isPinned = pinnedUrls.Contains(item.Url)
                    
                    // Content card with desert theme and filtering attributes
                    article [ 
                        _class (if isPinned then "h-entry content-card pinned-post" else "h-entry content-card")
                        attr "data-type" item.ContentType
                        attr "data-date" item.Date
                    ] [
                        // Add pin indicator if pinned
                        if isPinned then
                            div [ _class "pin-indicator" ] [
                                span [ _class "pin-icon" ] [ Text "📌" ]
                                span [ _class "pin-label" ] [ Text "Pinned" ]
                            ]
                        
                        header [ _class "card-header" ] [
                            time [ _class "dt-published publication-date"; attr "datetime" item.Date ] [
                                Text (DateTimeOffset.Parse(item.Date).ToString("MMM dd, yyyy"))
                            ]
                            div [ _class "content-type-info" ] [
                                span [ _class "content-type-badge"; attr "data-type" item.ContentType ] [
                                    Text (match item.ContentType with
                                          | "posts" -> "Blog Post"
                                          | "notes" -> "Note"
                                          | "responses" -> "Response"
                                          | "bookmarks" -> "Bookmark"
                                          | "reviews" -> 
                                              // For reviews, try to extract the specific item type (Book, Movie, etc.)
                                              match extractReviewItemType item.Content with
                                              | Some itemType -> itemType
                                              | None -> "Review"  // Fallback to generic "Review"
                                          | "streams" -> "Stream Recording"
                                          | "media" -> "Media"
                                          // Specific response types
                                          | "star" -> "Star"
                                          | "reply" -> "Reply"
                                          | "reshare" -> "Reshare"
                                          | "rsvp" -> "RSVP"
                                          | "bookmark" -> "Bookmark"
                                          | "ai-memex" -> "AI Memex"
                                          | _ -> item.ContentType)
                                ]
                            ]
                        ]
                        
                        div [ _class "card-body" ] [
                            h2 [ _class "p-name card-title" ] [
                                a [ _class "u-url title-link"; _href properPermalink ] [ Text item.Title ]
                            ]
                            div [ _class "e-content card-content" ] [
                                // Special handling for reviews - they already use simplified CardHtml from GenericBuilder
                                if item.ContentType = "reviews" then
                                    // Reviews are already simplified in the unified feed processing, display as-is
                                    rawText item.Content
                                else
                                    // Convert markdown content to HTML and clean it for other content types
                                    let cleanedContent = cleanCardHtml (convertMdToHtml item.Content)
                                    rawText cleanedContent
                            ]
                        ]
                        
                        footer [ _class "card-footer" ] [
                            div [ _class "card-meta" ] [
                                if item.Tags.Length > 0 then
                                    div [ _class "p-category tags" ] [
                                        for tag in item.Tags do
                                            a [ _class "tag-link"; _href $"/tags/{sanitizeTagForUrl tag}/" ] [ Text $"#{tag}" ]
                                    ]
                            ]
                        ]
                    ]
            ]
            
            // Progressive loading container for additional content chunks per type
            div [ 
                _class "progressive-content"
                _id "progressiveContent"
            ] []
            
            // Type-aware progressive loading data and controls
            if not remainingItemsByType.IsEmpty then
                // Generate JSON data for each content type separately
                for (contentType, items) in remainingItemsByType do
                    if not items.IsEmpty then
                        // Proper JSON escaping function
                        let escapeJson (text: string) =
                            text.Replace("\\", "\\\\")
                                .Replace("\"", "\\\"")
                                .Replace("\b", "\\b")
                                .Replace("\f", "\\f")
                                .Replace("\n", "\\n")
                                .Replace("\r", "\\r")
                                .Replace("\t", "\\t")
                        
                        let contentTypeItemsJson = 
                            items 
                            |> List.map (fun item ->
                                let fileName = Path.GetFileNameWithoutExtension(item.Url)
                                let getProperPermalink (contentType: string) (fileName: string) =
                                    sprintf "%s%s/" (ContentTypes.urlPrefix contentType) fileName
                                let properPermalink = getProperPermalink item.ContentType fileName
                                
                                // Clean content safely for JSON without truncation - full content display
                                let safeContent = escapeJson (cleanCardHtml (convertMdToHtml item.Content))
                                
                                sprintf """{"title":"%s","contentType":"%s","date":"%s","url":"%s","content":"%s","tags":[%s]}"""
                                    (escapeJson item.Title)
                                    (escapeJson item.ContentType)
                                    (escapeJson item.Date)
                                    (escapeJson properPermalink)
                                    safeContent
                                    (item.Tags |> Array.map (fun tag -> sprintf "\"%s\"" (escapeJson tag)) |> String.concat ",")
                            )
                            |> String.concat ","
                        
                        script [ _type "application/json"; _id $"remainingContentData-{contentType}" ] [
                            rawText $"[{contentTypeItemsJson}]"
                        ]
                
                // Progressive loading controls (JavaScript will handle type-specific loading)
                div [ _class "load-more-section"; _id "loadMoreSection" ] [
                    let totalRemaining = remainingItemsByType |> List.sumBy (fun (_, items) -> items.Length)
                    button [ 
                        _class "load-more-btn"
                        _id "loadMoreBtn"
                        _type "button"
                        attr "data-chunk-size" "10"
                    ] [
                        Text $"Load More ({totalRemaining} items remaining)"
                    ]
                    div [ _class "loading-indicator hidden"; _id "loadingIndicator" ] [
                        div [ _class "loading-spinner" ] []
                        Text "Loading more content..."
                    ]
                ]
        ]
        
        // Back to top button for long content scrolling (UX best practice)
        button [ 
            _class "back-to-top"
            _id "backToTopBtn"
            _type "button"
            _title "Back to top"
            attr "aria-label" "Scroll back to top of page"
        ] [
            // Using simple up arrow for universal recognition
            span [ _class "icon"; attr "aria-hidden" "true" ] [ Text "↑" ]
        ]
    ]

// New timeline homepage view for feed-as-homepage interface - Progressive Loading Implementation
let timelineHomeView (items: UnifiedFeeds.UnifiedFeedItem array) =
    // Research-backed progressive loading: Start with safe 50 items, load more on demand
    
    div [ _class "h-feed unified-timeline" ] [
        // Header with personal intro and content filters
        header [ _class "timeline-header text-center p-4" ] [
            div [ _class "avatar-section mb-3" ] [
                avatarFlipCard "Luis Quintanilla Avatar Image" "/" "Show QR code for homepage" "Luis Quintanilla's homepage"
                div [ _class "mt-2" ] [
                    h1 [ _class "p-name" ] [
                        str "Hi, I'm "
                        a [ _href "/about"; _class "author-link" ] [ Text "Luis" ]
                        span [] [ Text " &#x1F44B;" ]
                    ]
                    p [ _class "tagline" ] [ Text "Latest updates from across the site" ]
                ]
            ]
            
            // Content type filters (will be styled as buttons in CSS)
            div [ _class "content-filters mt-3"; _id "contentFilters" ] [
                button [ _class "filter-btn active"; attr "data-filter" "all"; _type "button" ] [ Text "All" ]
                button [ _class "filter-btn"; attr "data-filter" "posts"; _type "button" ] [ Text "Blog Posts" ]
                button [ _class "filter-btn"; attr "data-filter" "notes"; _type "button" ] [ Text "Notes" ]
                button [ _class "filter-btn"; attr "data-filter" "responses"; _type "button" ] [ Text "Responses" ]
                button [ _class "filter-btn"; attr "data-filter" "reviews"; _type "button" ] [ Text "Reviews" ]
                button [ _class "filter-btn"; attr "data-filter" "bookmarks"; _type "button" ] [ Text "Bookmarks" ]
                button [ _class "filter-btn"; attr "data-filter" "media"; _type "button" ] [ Text "Media" ]
            ]
        ]
        
        // Timeline content area with progressive loading
        main [ _class "timeline-content" ] [
            // Initial content batch (safe loading threshold based on content-volume-html-parsing-discovery.md)
            div [ _class "initial-content"; _id "initialContent" ] [
                for item in (items |> Array.take (min 50 items.Length)) do
                    let fileName = Path.GetFileNameWithoutExtension(item.Url)
                    let getProperPermalink (contentType: string) (fileName: string) =
                        match contentType with
                        | "posts" -> $"/posts/{fileName}/"
                        | "notes" -> $"/notes/{fileName}/"
                        | "responses" -> $"/responses/{fileName}/"
                        | "bookmarks" -> $"/bookmarks/{fileName}/"  // Bookmarks are responses but filtered separately
                        | "snippets" -> $"/resources/snippets/{fileName}/"
                        | "wiki" -> $"/resources/wiki/{fileName}/"
                        | "presentations" -> $"/resources/presentations/{fileName}/"
                        | "reviews" -> $"/reviews/{fileName}/"
                        | "streams" -> $"/streams/{fileName}/"
                        | "media" -> $"/media/{fileName}/"
                        // Specific response types also route to /responses/
                        | "star" | "reply" | "reshare" | "rsvp" -> $"/responses/{fileName}/"
                        | "bookmark" -> $"/bookmarks/{fileName}/"
                        | "ai-memex" -> $"/resources/ai-memex/{fileName}/"
                        | _ -> $"/{contentType}/{fileName}/"
                    
                    let properPermalink = getProperPermalink item.ContentType fileName
                    
                    // Content card with desert theme and filtering attributes
                    article [ 
                        _class "h-entry content-card"
                        attr "data-type" item.ContentType
                        attr "data-date" item.Date
                    ] [
                        header [ _class "card-header" ] [
                            time [ _class "dt-published publication-date"; attr "datetime" item.Date ] [
                                Text (DateTimeOffset.Parse(item.Date).ToString("MMM dd, yyyy"))
                            ]
                            div [ _class "content-type-info" ] [
                                span [ _class "content-type-badge"; attr "data-type" item.ContentType ] [
                                    Text (match item.ContentType with
                                          | "posts" -> "Blog Post"
                                          | "notes" -> "Note"
                                          | "responses" -> "Response"
                                          | "bookmarks" -> "Bookmark"
                                          | "reviews" -> 
                                              // For reviews, try to extract the specific item type (Book, Movie, etc.)
                                              match extractReviewItemType item.Content with
                                              | Some itemType -> itemType
                                              | None -> "Review"  // Fallback to generic "Review"
                                          | "streams" -> "Stream Recording"
                                          | "media" -> "Media"
                                          // Specific response types
                                          | "star" -> "Star"
                                          | "reply" -> "Reply"
                                          | "reshare" -> "Reshare"
                                          | "rsvp" -> "RSVP"
                                          | "bookmark" -> "Bookmark"
                                          | "ai-memex" -> "AI Memex"
                                          | _ -> item.ContentType)
                                ]
                            ]
                        ]
                        
                        div [ _class "card-body" ] [
                            h2 [ _class "p-name card-title" ] [
                                a [ _class "u-url title-link"; _href properPermalink ] [ Text item.Title ]
                            ]
                            div [ _class "e-content card-content" ] [
                                // Special handling for reviews - they already use simplified CardHtml from GenericBuilder
                                if item.ContentType = "reviews" then
                                    // Reviews are already simplified in the unified feed processing, display as-is
                                    rawText item.Content
                                    // Convert markdown content to HTML and clean it for other content types
                                    let cleanedContent = cleanCardHtml (convertMdToHtml item.Content)
                                    rawText cleanedContent
                            ]
                        ]
                        
                        footer [ _class "card-footer" ] [
                            div [ _class "card-meta" ] [
                                if item.Tags.Length > 0 then
                                    div [ _class "p-category tags" ] [
                                        for tag in item.Tags do
                                            a [ _class "tag-link"; _href $"/tags/{sanitizeTagForUrl tag}/" ] [ Text $"#{tag}" ]
                                    ]
                            ]
                        ]
                    ]
            ]
            
            // Progressive loading container for additional content chunks
            div [ 
                _class "progressive-content"
                _id "progressiveContent"
                attr "data-total-items" (items.Length.ToString())
            ] []
            
            // Progressive loading trigger - JavaScript will load remaining content chunks
            if items.Length > 50 then
                // Pass remaining items as JSON for JavaScript progressive loading
                let remainingItems = items |> Array.skip 50
                
                // Proper JSON escaping function
                let escapeJson (text: string) =
                    text.Replace("\\", "\\\\")
                        .Replace("\"", "\\\"")
                        .Replace("\b", "\\b")
                        .Replace("\f", "\\f")
                        .Replace("\n", "\\n")
                        .Replace("\r", "\\r")
                        .Replace("\t", "\\t")
                
                let remainingItemsJson = 
                    remainingItems 
                    |> Array.map (fun item ->
                        let fileName = Path.GetFileNameWithoutExtension(item.Url)
                        let getProperPermalink (contentType: string) (fileName: string) =
                            sprintf "%s%s/" (ContentTypes.urlPrefix contentType) fileName
                        let properPermalink = getProperPermalink item.ContentType fileName
                        
                        // Clean content safely for JSON without truncation - full content display
                        let safeContent = 
                            let content = 
                                if item.ContentType = "reviews" then
                                    // For reviews, use simplified content just like the initial timeline
                                    createSimplifiedReviewContent item.Content
                                else
                                    // For other content types, use the standard processing
                                    convertMdToHtml item.Content  // Convert markdown to HTML first
                            // Clean content similar to initial loading to ensure consistency
                            escapeJson (cleanCardHtml content)
                        
                        sprintf """{"title":"%s","contentType":"%s","date":"%s","url":"%s","content":"%s","tags":[%s]}"""
                            (escapeJson item.Title)
                            (escapeJson item.ContentType)
                            (escapeJson item.Date)
                            (escapeJson properPermalink)
                            safeContent
                            (item.Tags |> Array.map (fun tag -> sprintf "\"%s\"" (escapeJson tag)) |> String.concat ",")
                    )
                    |> String.concat ","
                
                script [ _type "application/json"; _id "remainingContentData" ] [
                    rawText $"[{remainingItemsJson}]"
                ]
                
                div [ _class "load-more-section"; _id "loadMoreSection" ] [
                    button [ 
                        _class "load-more-btn"
                        _id "loadMoreBtn"
                        _type "button"
                        attr "data-total-items" (items.Length.ToString())
                        attr "data-loaded-items" "50"
                        attr "data-chunk-size" "25"
                    ] [
                        Text $"Load More ({items.Length - 50} items remaining)"
                    ]
                    div [ _class "loading-indicator hidden"; _id "loadingIndicator" ] [
                        div [ _class "loading-spinner" ] []
                        Text "Loading more content..."
                    ]
                ]
        ]
        
        // Back to top button for long content scrolling (UX best practice)
        button [ 
            _class "back-to-top"
            _id "backToTopBtn"
            _type "button"
            _title "Back to top"
            attr "aria-label" "Scroll back to top of page"
        ] [
            // Using simple up arrow for universal recognition
            span [ _class "icon"; attr "aria-hidden" "true" ] [ Text "↑" ]
        ]
    ]
