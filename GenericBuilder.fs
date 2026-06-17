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
open Giraffe.ViewEngine
open Giraffe.ViewEngine.HtmlElements
open Markdig
open Markdig.Syntax

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
        // Escape any ]]> that might break CDATA
        let escaped = md.Replace("]]>", "]]]]><![CDATA[>")
        // Create XElement with source namespace
        let sourceNs = XNamespace.Get("http://source.scripting.com/")
        Some (XElement(sourceNs + "markdown", XCData(escaped)))
    | _ -> None

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
            let rssXml = processor.RenderRss content
            Some { Content = content; CardHtml = cardHtml; RssXml = rssXml }
        | Error _ -> None)

/// Phase 5C: Review metadata for Schema.org Review vocabulary in ActivityPub
/// Defined at module level so it can be used by both BookProcessor and UnifiedFeeds
type ReviewMetadata = {
    ItemName: string          // Name of the reviewed item (book title, movie name, etc.)
    ItemType: string          // Type of item: "book", "movie", "music", "product", "business"
    Rating: float             // Rating value (e.g., 4.5)
    Scale: float              // Rating scale max (e.g., 5.0)
    Summary: string option    // Brief review summary
    ItemUrl: string option    // URL of the reviewed item
    ImageUrl: string option   // Cover/thumbnail image URL
    Author: string option     // For books: author name
    Isbn: string option       // For books: ISBN
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
