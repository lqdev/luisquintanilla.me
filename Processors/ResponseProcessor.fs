module ResponseProcessor

    open Domain
    open ASTParsing
    open CustomBlocks
    open BlockRenderers
    open TagService
    open MarkdownService
    open ReadingTimeService
    open System.Xml.Linq
    open System
    open System.IO
    open System.Text.Json
    open System.Text.Json.Nodes
    open Giraffe.ViewEngine
    open Giraffe.ViewEngine.HtmlElements
    open Markdig
    open Markdig.Syntax
    open GenericBuilder

    /// RSVP status → (Bootstrap icon class, colour). Mirrors LayoutViews.responsePostView
    /// so the timeline card matches the individual post page.
    let private rsvpIconAndColor (status: string) =
        match status.ToLowerInvariant() with
        | "yes" -> ("bi bi-check-circle-fill", "#28a745")
        | "no" -> ("bi bi-x-circle-fill", "#dc3545")
        | "maybe" -> ("bi bi-question-circle-fill", "#ffc107")
        | "interested" -> ("bi bi-calendar-check-fill", "#6c757d")
        | _ -> ("bi bi-calendar-event-fill", "#4a60b6")

    /// The response-target div. For RSVPs it renders a status-aware prefix
    /// (icon + p-rsvp status + "to" + event link) so the card conveys yes/no/maybe/
    /// interested. All other response subtypes keep the canonical "→ {url}" form.
    let private renderResponseTarget (response: Response) =
        let targetUrl = Html.escapeHtml response.Metadata.TargetUrl
        match response.Metadata.ResponseType.ToLowerInvariant(), response.Metadata.RsvpStatus with
        | "rsvp", Some status ->
            let (iconClass, color) = rsvpIconAndColor status
            let icon =
                Html.element "span"
                    (Html.attribute "class" iconClass + Html.attribute "style" (sprintf "margin-right:5px;color:%s;" color))
                    ""
            let statusSpan = Html.element "span" (Html.attribute "class" "p-rsvp") (Html.escapeHtml status)
            let link = Html.element "a" (Html.attribute "href" targetUrl + Html.attribute "class" "u-in-reply-to") targetUrl
            Html.element "div" (Html.attribute "class" "response-target")
                (sprintf "%s%s to %s" icon statusSpan link)
        | _ ->
            Html.element "div" (Html.attribute "class" "response-target")
                (sprintf "→ %s" (Html.element "a" (Html.attribute "href" targetUrl) targetUrl))

    /// Chrome-free card body (B2 / F7): the response-target + response-content divs
    /// WITHOUT the <article> wrapper or the <h2><a>title</a></h2> heading. The timeline
    /// composes this directly — its own card-body/title wrapper makes the standalone
    /// CardHtml chrome redundant (historically regex-stripped by `cleanCardHtml`).
    /// `RenderCard` wraps this with the standalone-card chrome, so CardHtml is unchanged.
    let renderResponseCardBody (response: Response) =
        renderResponseTarget response +
        Html.element "div" (Html.attribute "class" "response-content") response.Content

    /// B2 (RENDER_V2) clean card-body seam. Like renderResponseCardBody, but renders the
    /// body from the response's MARKDOWN source via ASTParsing.renderCardHtmlFromMarkdown so
    /// the AST card renderer can drop headings the card's own title duplicates (level-1 and
    /// bare-link level-2). renderResponseCardBody embeds already-rendered HTML, where an
    /// embedded <h2><a> sits inside a single HtmlBlock and cannot be removed structurally.
    /// renderCardHtmlFromMarkdown reuses the canonical bare renderer (Media + Review object
    /// renderers, no pipeline.Setup), so the body is byte-identical to response.Content apart
    /// from the intended heading removal.
    let renderResponseCardBodyClean (response: Response) =
        let bodyHtml =
            match response.MarkdownSource with
            | Some raw -> ASTParsing.renderCardHtmlFromMarkdown (ASTParsing.stripFrontMatter raw)
            | None -> response.Content
        renderResponseTarget response +
        Html.element "div" (Html.attribute "class" "response-content") bodyHtml

    let create() : ContentProcessor<Response> = {
        Parse = fun filePath ->
            match parseResponseFromFile filePath with
            | Ok parsedDoc -> 
                match parsedDoc.Metadata with
                | Some metadata -> 
                    let readingTime = ReadingTimeService.calculateReadingTime parsedDoc.TextContent
                    Ok {
                        FileName = Path.GetFileNameWithoutExtension(filePath)
                        Metadata = { metadata with ReadingTimeMinutes = readingTime }
                        Content = parsedDoc.TextContent
                        MarkdownSource = Some parsedDoc.RawMarkdown
                    }
                | None -> Error (Diagnostics.ContentError.ParseFailure(filePath, "frontmatter", "no front-matter block found (expected a leading '---' fence)"))
            | Error e -> Error (Diagnostics.ofParseError filePath e)
        
        Render = fun response ->
            // Response rendering with IndieWeb microformat support and target URL
            let responseClass = sprintf "h-entry response response-%s" (response.Metadata.ResponseType.ToLower())
            let targetUrlDisplay = 
                div [ _class "response-target mb-3" ] [
                    p [] [
                        span [ _class "bi bi-link-45deg"; _style "margin-right:5px;color:#6c757d;" ] []
                        Text "→ "
                        a [ _class "u-bookmark-of"; _href response.Metadata.TargetUrl; _target "_blank" ] [ 
                            Text response.Metadata.TargetUrl 
                        ]
                    ]
                ]
            let viewNode = article [ _class responseClass ] [ 
                targetUrlDisplay
                div [ _class "e-content" ] [ rawText response.Content ]
            ]
            RenderView.AsString.xmlNode viewNode
        
        OutputPath = fun response ->
            let pathPrefix = 
                match response.Metadata.ResponseType with
                | "bookmark" -> "bookmarks"
                | _ -> "responses"
            sprintf "%s/%s.html" pathPrefix response.FileName
        
        RenderCard = fun response ->
            let title = Html.escapeHtml response.Metadata.Title
            // Use correct path based on response type 
            let urlPath = 
                match response.Metadata.ResponseType with
                | "bookmark" -> "bookmarks"
                | _ -> "responses"
            let url = sprintf "/%s/%s/" urlPath response.FileName
            
            // Include title, target URL, and content
            Html.element "article" (Html.attribute "class" "response-card h-entry")
                (Html.element "h2" "" (Html.element "a" (Html.attribute "href" url) title) +
                 renderResponseCardBody response)
        
        RenderRss = fun response ->
            // Create RSS item for response with correct path based on response type
            let urlPath = 
                match response.Metadata.ResponseType with
                | "bookmark" -> "bookmarks"
                | _ -> "responses"
            let url = sprintf "https://www.lqdev.me/%s/%s" urlPath response.FileName
            let description = sprintf "[%s] %s" response.Metadata.ResponseType response.Content
            
            // Normalize URLs in description for RSS compatibility
            let normalizedDescription = normalizeUrlsForRss description "https://www.lqdev.me"
            
            let categories = 
                if isNull response.Metadata.Tags then []
                else response.Metadata.Tags |> Array.map (fun tag -> XElement(XName.Get "category", tag)) |> Array.toList
            
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", response.Metadata.Title),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" normalizedDescription),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url),
                    XElement(XName.Get "pubDate", response.Metadata.DatePublished))
            
            // Add categories if they exist
            if not (List.isEmpty categories) then
                item.Add(categories |> List.toArray)
            
            // Add source:markdown if available
            match generateSourceMarkdown response.MarkdownSource with
            | Some sourceElement -> item.Add(sourceElement)
            | None -> ()
                
            Some item
    }
