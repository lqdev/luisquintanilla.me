module GenericBuilder

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
open System.Globalization
open System.Text.RegularExpressions
open Giraffe.ViewEngine
open Giraffe.ViewEngine.HtmlElements
open Markdig
open Markdig.Syntax
open AngleSharp.Dom
open AngleSharp.Html.Parser

/// Data structure for feed generation with both card and RSS item
type FeedData<'T> = {
    Content: 'T
    CardHtml: string
    RssXml: XElement option
}

/// URL normalization for RSS feeds to ensure absolute URLs
let normalizeUrlsForRss (content: string) (baseUrl: string) =
    let baseUrl = baseUrl.TrimEnd('/')
    
    // Normalize relative image src attributes
    let imgPattern = @"src\s*=\s*""(/[^""]*)""|src\s*=\s*'(/[^']*)'"
    let normalizedImages = 
        System.Text.RegularExpressions.Regex.Replace(content, imgPattern, fun m ->
            let quote = if m.Value.Contains("\"") then "\"" else "'"
            let path = if m.Groups.[1].Success then m.Groups.[1].Value else m.Groups.[2].Value
            sprintf "src=%s%s%s%s" quote baseUrl path quote)
    
    // Normalize relative link href attributes  
    let linkPattern = @"href\s*=\s*""(/[^""]*)""|href\s*=\s*'(/[^']*)'"
    let normalizedLinks =
        System.Text.RegularExpressions.Regex.Replace(normalizedImages, linkPattern, fun m ->
            let quote = if m.Value.Contains("\"") then "\"" else "'"
            let path = if m.Groups.[1].Success then m.Groups.[1].Value else m.Groups.[2].Value
            sprintf "href=%s%s%s%s" quote baseUrl path quote)
    
    normalizedLinks

/// Generate source:markdown element for RSS feeds
let generateSourceMarkdown (markdownSource: string option) : XElement option =
    match markdownSource with
    | Some md when not (String.IsNullOrWhiteSpace md) ->
        let sourceNs = XNamespace.Get("http://source.scripting.com/")
        Some (XElement(sourceNs + "markdown", XCData(md)))
    | _ -> None

/// Parse authored dates only when their timezone is explicit. RSS readers require
/// a wire format, not merely a string that DateTimeOffset.Parse happens to accept.
let tryParseRssDate (value: string) : DateTimeOffset option =
    if String.IsNullOrWhiteSpace(value) then None
    else
        let trimmed = value.Trim()
        let hasExplicitZone =
            Regex.IsMatch(
                trimmed,
                @"(?:Z|GMT|UTC|[+-]\d{2}:?\d{2})$",
                RegexOptions.IgnoreCase ||| RegexOptions.CultureInvariant)
        let mutable parsed = DateTimeOffset.MinValue
        if hasExplicitZone
           && DateTimeOffset.TryParse(
                trimmed,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                &parsed) then
            Some parsed
        else
            None

/// Format a DateTimeOffset as the invariant, four-digit-year RSS date form.
let formatRssDate (value: DateTimeOffset) : string =
    value.ToUniversalTime().ToString("R", CultureInfo.InvariantCulture)

/// Add a valid RSS pubDate, reporting malformed or timezone-ambiguous source data.
let rssPubDateElement (context: string) (value: string) : XElement option =
    match tryParseRssDate value with
    | Some date -> Some (XElement(XName.Get "pubDate", formatRssDate date))
    | None ->
        let detail =
            if String.IsNullOrWhiteSpace(value) then
                "publication date is blank; provide a date with an explicit timezone"
            else
                sprintf "invalid RSS publication date %A; provide a parseable date with an explicit timezone" value
        Diagnostics.ContentError.ParseFailure(context, "rss-pubDate", detail)
        |> Diagnostics.report
        None

/// Resolve a feed URL against a content permalink while rejecting non-web schemes.
let resolveRssHttpUrl (baseUrl: string) (value: string) : Uri option =
    if String.IsNullOrWhiteSpace(value) then None
    else
        let mutable baseUri = Unchecked.defaultof<Uri>
        if not (Uri.TryCreate(baseUrl, UriKind.Absolute, &baseUri))
           || (baseUri.Scheme <> Uri.UriSchemeHttp && baseUri.Scheme <> Uri.UriSchemeHttps) then
            None
        else
            let basePath =
                if baseUri.AbsolutePath.EndsWith("/", StringComparison.Ordinal) then baseUri.AbsolutePath
                else baseUri.AbsolutePath + "/"
            let directoryUri =
                Uri(baseUri.GetLeftPart(UriPartial.Authority) + basePath)
            let hasExplicitScheme = Regex.IsMatch(value, @"^[A-Za-z][A-Za-z0-9+.-]*:")
            let mutable absoluteUri = Unchecked.defaultof<Uri>
            let resolved =
                if hasExplicitScheme then
                    if Uri.TryCreate(value, UriKind.Absolute, &absoluteUri) then absoluteUri
                    else null
                else
                    try Uri(directoryUri, value)
                    with :? UriFormatException -> null
            if isNull resolved
               || (resolved.Scheme <> Uri.UriSchemeHttp && resolved.Scheme <> Uri.UriSchemeHttps) then
                None
            else
                Some resolved

/// Resolve only scalar URL-bearing HTML attributes in RSS description fragments.
/// The shared normalizeUrlsForRss helper remains unchanged for JSON Feed callers.
let normalizeRssHtmlUrls (html: string) (itemUrl: string) : string =
    if String.IsNullOrEmpty(html) then html
    else
        match resolveRssHttpUrl itemUrl "." with
        | None ->
            Diagnostics.ContentError.ParseFailure(
                itemUrl,
                "rss-html-url",
                "item permalink must be an absolute HTTP(S) URL to resolve relative HTML references")
            |> Diagnostics.report
            html
        | Some itemUri ->
            let mutable failedAttribute: string option = None
            let resolveAttribute (name: string) (value: string) =
                if not (
                    String.Equals(name, "src", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(name, "href", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(name, "poster", StringComparison.OrdinalIgnoreCase))
                   || String.IsNullOrWhiteSpace(value) then
                    None
                else
                    match resolveRssHttpUrl itemUrl value with
                    | Some resolved ->
                        let hasExplicitScheme = Regex.IsMatch(value, @"^[A-Za-z][A-Za-z0-9+.-]*:")
                        if hasExplicitScheme then
                            None
                        else
                            Some resolved.AbsoluteUri
                    | None ->
                        let hasExplicitScheme = Regex.IsMatch(value, @"^[A-Za-z][A-Za-z0-9+.-]*:")
                        if hasExplicitScheme then None
                        else
                            failedAttribute <- Some value
                            None

            let parser = HtmlParser()
            let document = parser.ParseDocument("<html><body>" + html + "</body></html>")
            let mutable changed = false
            for element in document.QuerySelectorAll("[src], [href], [poster]") do
                for attributeName in [ "src"; "href"; "poster" ] do
                    let value = element.GetAttribute(attributeName)
                    if not (isNull value) then
                        match resolveAttribute attributeName value with
                        | Some rewritten ->
                            element.SetAttribute(attributeName, rewritten)
                            changed <- true
                        | None -> ()
            match failedAttribute with
            | Some value ->
                Diagnostics.ContentError.ParseFailure(
                    itemUrl,
                    "rss-html-url",
                    sprintf "could not resolve relative HTML URL %A" value)
                |> Diagnostics.report
            | None -> ()
            if changed then document.Body.InnerHtml
            else
                html

/// Extract direct media assets in authored AST order and emit supported Media RSS
/// elements. Provider pages, unsupported schemes, and unknown extensions are ignored.
let mediaRssContentElements (markdown: string) (itemUrl: string) : XElement list =
    let mimeTypes =
        dict [
            ".avif", "image/avif"; ".bmp", "image/bmp"; ".gif", "image/gif"
            ".jpeg", "image/jpeg"; ".jpg", "image/jpeg"; ".png", "image/png"
            ".tif", "image/tiff"; ".tiff", "image/tiff"; ".webp", "image/webp"
            ".m4v", "video/x-m4v"; ".mov", "video/quicktime"; ".mp4", "video/mp4"
            ".ogv", "video/ogg"; ".webm", "video/webm"
            ".aac", "audio/aac"; ".flac", "audio/flac"; ".m4a", "audio/mp4"
            ".mp3", "audio/mpeg"; ".oga", "audio/ogg"; ".ogg", "audio/ogg"
            ".opus", "audio/ogg"; ".wav", "audio/wav"
        ]
    let rec mediaItemsInOrder (container: ContainerBlock) =
        seq {
            for block in container do
                match block with
                | :? MediaBlock as mediaBlock ->
                    yield! mediaBlock.MediaItems
                | :? ContainerBlock as nested ->
                    yield! mediaItemsInOrder nested
                | _ -> ()
        }
    let markdownAst = ASTParsing.parseMarkdownAst markdown
    let mediaNamespace = XNamespace.Get("http://search.yahoo.com/mrss/")
    let seen = System.Collections.Generic.HashSet<string>(StringComparer.Ordinal)
    mediaItemsInOrder markdownAst
    |> Seq.choose (fun mediaItem ->
        match resolveRssHttpUrl itemUrl mediaItem.uri with
        | None -> None
        | Some mediaUri ->
            let extension = Path.GetExtension(mediaUri.AbsolutePath).ToLowerInvariant()
            match mimeTypes.TryGetValue(extension) with
            | false, _ -> None
            | true, mimeType when seen.Add(mediaUri.AbsoluteUri) ->
                Some (
                    XElement(
                        mediaNamespace + "content",
                        XAttribute(XName.Get "url", mediaUri.AbsoluteUri),
                        XAttribute(XName.Get "type", mimeType)))
            | true, _ -> None)
    |> Seq.toList

/// Build an RSS <description> element containing a genuine CDATA section.
/// Passing a plain string to XElement would XML-escape it (emitting the literal
/// "&lt;![CDATA[...]]&gt;"); an XCData node emits a real "<![CDATA[ ... ]]>" section.
/// When serialized, XmlWriter.WriteCData auto-splits any embedded "]]>" across CDATA
/// sections (e.g. "]]]]><![CDATA[>"), so the feed stays well-formed (verified on .NET 10).
let rssDescriptionElement (content: string) : XElement =
    let safe = if isNull content then "" else content
    XElement(XName.Get "description", XCData(safe))

/// Preserve the historical RSS identifier while allowing published links to move
/// to the canonical apex origin. Existing subscribers use <guid> as the item
/// identity, so changing it would make every existing item look new.
let private preserveHistoricalRssGuid (rssXml: XElement) =
    let guid = rssXml.Element(XName.Get "guid")
    if not (isNull guid)
       && guid.Value.StartsWith(Constants.Urls.canonical, StringComparison.OrdinalIgnoreCase) then
        guid.Value <- Constants.Urls.legacyWww + guid.Value.Substring(Constants.Urls.canonical.Length)
    rssXml

/// Generic content processor pattern for consistent content handling
type ContentProcessor<'T> = {
    /// Parse content from file path to domain type, transporting a typed
    /// `ContentError` on failure (F8 — the rail is no longer severed to Option).
    Parse: string -> Result<'T, Diagnostics.ContentError>
    /// Render content to final HTML output
    Render: 'T -> string
    /// Generate output file path from content
    OutputPath: 'T -> string
    /// Generate card HTML for index pages
    RenderCard: 'T -> string
    /// Generate RSS XML element
    RenderRss: 'T -> XElement option
}

/// Build content and generate feed data in a single pass
let buildContentWithFeeds<'T> (processor: ContentProcessor<'T>) (filePaths: string list) : FeedData<'T> list =
    // F8 full railway (plan 2.8): parse every file, accumulating failures instead
    // of short-circuiting. Successes render and flow to the feeds; failures are
    // transported as typed `ContentError`s to the diagnostics reporter (which
    // prints a self-contained block and records them for strict-mode exit). One
    // bad file does not block publishing the rest, so `_public/` is byte-identical
    // whenever nothing fails — a failed parse is dropped here exactly as the old
    // `| Error _ -> None` dropped it, only now it is reported instead of silent.
    let parsed =
        filePaths
        |> List.map (fun filePath -> processor.Parse filePath)
    parsed
    |> List.choose (function Error e -> Some e | Ok _ -> None)
    |> List.iter Diagnostics.report
    parsed
    |> List.choose (function
        | Ok content ->
            let cardHtml = processor.RenderCard content
            let rssXml = processor.RenderRss content |> Option.map preserveHistoricalRssGuid
            Some { Content = content; CardHtml = cardHtml; RssXml = rssXml }
        | Error _ -> None)

/// Phase 5C: Review metadata for Schema.org Review vocabulary in ActivityPub
/// Defined at module level so it can be used by both BookProcessor and UnifiedFeeds
type ReviewMetadata = {
    ItemName: string          // Name of the reviewed item (book title, movie name, etc.)
    ItemType: string          // Type of item: "book", "movie", "music", "product", "business"
    Rating: float             // Rating value (e.g., 4.5)
    Scale: float              // Rating scale max (e.g., 5.0)
    Description: string option // Short description of the item
    ItemUrl: string option    // URL of the reviewed item
    ImageUrl: string option   // Cover/thumbnail image URL
    Author: string option     // For books: author name
    Isbn: string option       // For books: ISBN
    Pros: string array option
    Cons: string array option
}

/// Phase 5D: Media metadata for ActivityPub Image/Video/Audio objects
/// Defined at module level so it can be used by both media processing and UnifiedFeeds
type MediaAPData = {
    MediaUrl: string          // URL of the media file
    MediaType: string         // MIME type (e.g., "image/jpeg", "video/mp4")
    ObjectType: string        // ActivityPub type: "Image", "Video", "Audio"
    AltText: string option    // Alt text for accessibility
    Caption: string option    // Caption/description
}

/// Media data extracted from album content for efficient rendering
type AlbumMediaData = {
    FirstImageUrl: string option
    FirstImageAlt: string option
    MediaCount: int
}

/// Generic content processing pipeline
module ContentPipeline =
    
    /// Process all content of a specific type and generate both HTML and feeds
    let processAllContent<'T> (processor: ContentProcessor<'T>) (sourceDirectory: string) (outputDirectory: string) =
        if Directory.Exists sourceDirectory then
            let files = Directory.GetFiles(sourceDirectory, "*.md")
            let feedData = buildContentWithFeeds processor (Array.toList files)
            
            // Generate individual content files
            feedData
            |> List.iter (fun data ->
                let outputPath = Path.Combine(outputDirectory, processor.OutputPath data.Content)
                let outputDir = Path.GetDirectoryName(outputPath)
                if not (Directory.Exists(outputDir)) then
                    Directory.CreateDirectory(outputDir) |> ignore
                
                let html = processor.Render data.Content
                File.WriteAllText(outputPath, html))
            
            // Return feed data for aggregation
            feedData
        else
            []
