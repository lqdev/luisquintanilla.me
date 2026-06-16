module TimelineViews

open Giraffe.ViewEngine
open System
open System.IO
open System.Text.Json
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

/// B2.2: typed shape of a progressive-loading timeline item. Serialized with
/// System.Text.Json (field names map verbatim to JSON keys) under RENDER_V2,
/// replacing the hand-rolled sprintf-JSON + escapeJson. STJ's default HTML-safe
/// encoder escapes &lt;&gt;&amp; (e.g. &lt; → \u003C), which also closes the latent
/// &lt;/script&gt; break-out the old escapeJson relied on cleanCardHtml to neutralize.
/// NOTE: must be a public (non-private) record — System.Text.Json's reflection
/// serializer only reads properties of accessible types; a `private` record
/// serializes as empty `{}`.
type ProgressiveContentItem =
    { title: string
      contentType: string
      date: string
      url: string
      content: string
      tags: string[] }

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
                                // Compose the chrome-free structured body directly (B2 / F7).
                                rawText item.BodyHtml.Value
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
                        let contentTypeItemsJson =
                            // B2.2: structured serialization via System.Text.Json over a typed
                            // record. The clean body comes from item.BodyHtml (B2.1 seam).
                            items
                            |> List.map (fun item ->
                                let fileName = Path.GetFileNameWithoutExtension(item.Url)
                                let properPermalink = sprintf "%s%s/" (ContentTypes.urlPrefixForKey item.ContentType) fileName
                                { title = item.Title
                                  contentType = item.ContentType
                                  date = item.Date
                                  url = properPermalink
                                  content = item.BodyHtml.Value
                                  tags = item.Tags })
                            |> List.toArray
                            |> JsonSerializer.Serialize

                        script [ _type "application/json"; _id $"remainingContentData-{contentType}" ] [
                            rawText contentTypeItemsJson
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
