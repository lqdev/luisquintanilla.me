module TextOnlyContent

open Giraffe.ViewEngine
open Domain
open Layouts
open UnifiedFeeds
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
                let fullUrl = if src.StartsWith("http") then src else $"https://www.lqdev.me{src}"
                $"""<a href="{fullUrl}" target="_blank">[Image: {description}]</a>"""
            )
            
            // Handle images without alt text (catch remaining images)
            let imgWithoutAltPattern = @"<img[^>]*src\s*=\s*[""']([^""']*)[""'][^>]*/?>"
            result <- Regex.Replace(result, imgWithoutAltPattern, fun m ->
                let src = m.Groups.[1].Value
                let fullUrl = if src.StartsWith("http") then src else $"https://www.lqdev.me{src}"
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
                ("/tools", "/text/tools/")
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

// Normalize content-type values for text-only routing, filtering, and grouping.
// The unified feed assigns response items their *subtype* (reply/reshare/star/rsvp)
// as ContentType. For the text-only site we collapse those into a single "responses"
// section (matching the main site) so individual pages, landing pages, and nav links
// all resolve to the same consistent path.
let normalizeContentType (contentType: string) =
    if String.IsNullOrWhiteSpace(contentType) then ""
    else
        match contentType.Trim().ToLowerInvariant() with
        | "reply" | "reshare" | "star" | "rsvp" -> "responses"
        | other -> other
