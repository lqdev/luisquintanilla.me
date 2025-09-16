(*
    Process GitHub Issue Template for Media Creation
    
    This script processes GitHub issue template data to create a media post.
    It parses markdown images from GitHub's attachment format and creates media blocks.
    Usage: dotnet fsi process-media-issue.fsx -- "media_type" "title" "content_with_attachments" "orientation" "optional-slug" "optional,tags"
*)

#r "../bin/Debug/net9.0/PersonalSite.dll"

open System
open System.IO
open System.Text.RegularExpressions
open System.Net.Http
open System.Threading.Tasks

// Get command line arguments
let args = fsi.CommandLineArgs |> Array.skip 1

// Validate arguments
if args.Length < 3 then
    printfn "❌ Error: Missing required arguments"
    printfn "Usage: dotnet fsi process-media-issue.fsx -- \"media_type\" \"title\" \"content_with_attachments\" \"orientation\" \"optional-slug\" \"optional,tags\""
    printfn "Example: dotnet fsi process-media-issue.fsx -- \"image\" \"Beautiful Sunset\" \"Here's my photo: ![sunset](https://github.com/user/repo/assets/123/sunset.jpg)\" \"landscape\" \"sunset-photo\" \"photography,nature\""
    exit 1

let mediaType = args.[0]
let title = args.[1]
let contentWithAttachments = args.[2]
let orientation = if args.Length > 3 && not (String.IsNullOrWhiteSpace(args.[3])) then Some(args.[3]) else None
let customSlug = if args.Length > 4 && not (String.IsNullOrWhiteSpace(args.[4])) then Some(args.[4]) else None
let tagsInput = if args.Length > 5 && not (String.IsNullOrWhiteSpace(args.[5])) then Some(args.[5]) else None

// Validate required fields
if String.IsNullOrWhiteSpace(mediaType) then
    printfn "❌ Error: Media type is required and cannot be empty"
    exit 1

// Validate media type (allow mixed types for automatic detection)
let validMediaTypes = ["image"; "video"; "audio"; "mixed"]
if not (List.contains mediaType validMediaTypes) then
    printfn "❌ Error: Media type must be one of: %s" (String.concat ", " validMediaTypes)
    printfn "Use 'mixed' to allow different media types in the same post"
    exit 1

if String.IsNullOrWhiteSpace(title) then
    printfn "❌ Error: Title is required and cannot be empty"
    exit 1

if String.IsNullOrWhiteSpace(contentWithAttachments) then
    printfn "❌ Error: Content with attachments is required and cannot be empty"
    exit 1

open System.Net.Http
open System.Threading.Tasks

// Enhanced function to parse all media attachment formats from GitHub issues
let parseAllMediaAttachments (content: string) =
    let attachments = ResizeArray<string * string>()
    
    // 1. Parse markdown images: ![alt-text](URL)
    let markdownPattern = @"!\[([^\]]*)\]\(([^)]+)\)"
    let markdownMatches = Regex.Matches(content, markdownPattern)
    for m in markdownMatches do
        let altText = m.Groups.[1].Value.Trim()
        let url = m.Groups.[2].Value.Trim()
        let finalAltText = 
            if String.IsNullOrWhiteSpace(altText) then 
                try
                    let uri = Uri(url)
                    Path.GetFileNameWithoutExtension(uri.LocalPath)
                with
                | _ -> "media"
            else altText
        attachments.Add((url, finalAltText))
    
    // 2. Parse HTML img tags: <img alt="..." src="...">
    let htmlImgPattern = @"<img[^>]*\salt\s*=\s*[""']([^""']*)[""'][^>]*\ssrc\s*=\s*[""']([^""']*)[""'][^>]*/?>"
    let htmlMatches = Regex.Matches(content, htmlImgPattern)
    for m in htmlMatches do
        let altText = m.Groups.[1].Value.Trim()
        let url = m.Groups.[2].Value.Trim()
        let finalAltText = 
            if String.IsNullOrWhiteSpace(altText) then 
                try
                    let uri = Uri(url)
                    Path.GetFileNameWithoutExtension(uri.LocalPath)
                with
                | _ -> "media"
            else altText
        attachments.Add((url, finalAltText))
    
    // 3. Parse HTML img tags without alt attribute: <img src="...">
    let htmlImgNoAltPattern = @"<img[^>]*\ssrc\s*=\s*[""']([^""']*)[""'][^>]*/?>"
    let htmlNoAltMatches = Regex.Matches(content, htmlImgNoAltPattern)
    for m in htmlNoAltMatches do
        let url = m.Groups.[1].Value.Trim()
        // Skip if already found with alt attribute
        if not (attachments |> Seq.exists (fun (existingUrl, _) -> existingUrl = url)) then
            let finalAltText = 
                try
                    let uri = Uri(url)
                    Path.GetFileNameWithoutExtension(uri.LocalPath)
                with
                | _ -> "media"
            attachments.Add((url, finalAltText))
    
    // 4. Parse plain GitHub attachment URLs (common for videos)
    let githubAttachmentPattern = @"https://github\.com/user-attachments/assets/[a-zA-Z0-9\-]+"
    let plainGithubMatches = Regex.Matches(content, githubAttachmentPattern)
    for m in plainGithubMatches do
        let url = m.Value.Trim()
        // Skip if already found in other formats
        if not (attachments |> Seq.exists (fun (existingUrl, _) -> existingUrl = url)) then
            let finalAltText = 
                try
                    let uri = Uri(url)
                    Path.GetFileNameWithoutExtension(uri.LocalPath)
                with
                | _ -> "media"
            attachments.Add((url, finalAltText))
    
    // 5. Parse other plain URLs that might be media files
    let plainUrlPattern = @"https?://[^\s<>""')\]]+"
    let plainUrlMatches = Regex.Matches(content, plainUrlPattern)
    for m in plainUrlMatches do
        let url = m.Value.Trim()
        // Skip if already found in other formats
        if not (attachments |> Seq.exists (fun (existingUrl, _) -> existingUrl = url)) then
            // Only include URLs that look like they could be media files
            let lowerUrl = url.ToLowerInvariant()
            if lowerUrl.Contains(".jpg") || lowerUrl.Contains(".jpeg") || lowerUrl.Contains(".png") || 
               lowerUrl.Contains(".gif") || lowerUrl.Contains(".webp") || lowerUrl.Contains(".mp4") || 
               lowerUrl.Contains(".webm") || lowerUrl.Contains(".mov") || lowerUrl.Contains(".mp3") || 
               lowerUrl.Contains(".wav") || lowerUrl.Contains(".ogg") then
                let finalAltText = 
                    try
                        let uri = Uri(url)
                        Path.GetFileNameWithoutExtension(uri.LocalPath)
                    with
                    | _ -> "media"
                attachments.Add((url, finalAltText))
    
    attachments |> Seq.toArray

// Function to detect media type using HTTP HEAD request when extension is ambiguous
let detectMediaTypeFromUrl (url: string) =
    async {
        try
            // First try basic extension detection
            let basicType = MediaTypes.MediaTypeHelpers.detectMediaTypeFromUri url
            if basicType <> MediaTypes.MediaType.Unknown && basicType <> MediaTypes.MediaType.Link then
                return basicType
            else
                // Special handling for GitHub attachment URLs that typically don't have extensions
                if url.Contains("github.com/user-attachments/assets/") then
                    // For GitHub attachment URLs, try HTTP HEAD request to follow redirect and get the actual file URL
                    try
                        use client = new HttpClient()
                        client.Timeout <- TimeSpan.FromSeconds(10.0)
                        
                        use! response = client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url)) |> Async.AwaitTask
                        
                        if response.IsSuccessStatusCode then
                            // Check if we got redirected and can detect type from final URL
                            let finalUrl = response.RequestMessage.RequestUri.ToString()
                            let finalUrlType = MediaTypes.MediaTypeHelpers.detectMediaTypeFromUri finalUrl
                            if finalUrlType <> MediaTypes.MediaType.Unknown && finalUrlType <> MediaTypes.MediaType.Link then
                                return finalUrlType
                            else
                                // Fallback to Content-Type header
                                if response.Content.Headers.ContentType <> null then
                                    let contentType = response.Content.Headers.ContentType.MediaType.ToLower()
                                    if contentType.StartsWith("image/") then
                                        return MediaTypes.MediaType.Image
                                    elif contentType.StartsWith("video/") then
                                        return MediaTypes.MediaType.Video
                                    elif contentType.StartsWith("audio/") then
                                        return MediaTypes.MediaType.Audio
                                    else
                                        return MediaTypes.MediaType.Unknown
                                else
                                    return MediaTypes.MediaType.Unknown
                        else
                            return MediaTypes.MediaType.Unknown
                    with
                    | _ -> 
                        // If HTTP detection fails for GitHub URLs, assume they could be any media type
                        // and let the user-specified media type guide us
                        return MediaTypes.MediaType.Unknown
                else
                    // For other URLs, try HTTP detection
                    use client = new HttpClient()
                    client.Timeout <- TimeSpan.FromSeconds(10.0)
                    use! response = client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url)) |> Async.AwaitTask
                    
                    if response.IsSuccessStatusCode && response.Content.Headers.ContentType <> null then
                        let contentType = response.Content.Headers.ContentType.MediaType.ToLower()
                        if contentType.StartsWith("image/") then
                            return MediaTypes.MediaType.Image
                        elif contentType.StartsWith("video/") then
                            return MediaTypes.MediaType.Video
                        elif contentType.StartsWith("audio/") then
                            return MediaTypes.MediaType.Audio
                        else
                            return MediaTypes.MediaType.Unknown
                    else
                        return MediaTypes.MediaType.Unknown
        with
        | _ -> 
            // Fallback to basic detection if HTTP request fails
            return MediaTypes.MediaTypeHelpers.detectMediaTypeFromUri url
    }

// Extract all media attachments and clean content
let mediaAttachments = parseAllMediaAttachments contentWithAttachments

// Clean content by removing all detected attachment patterns
let mutable cleanContent = contentWithAttachments
// Remove markdown images
cleanContent <- Regex.Replace(cleanContent, @"!\[([^\]]*)\]\(([^)]+)\)", "")
// Remove HTML img tags
cleanContent <- Regex.Replace(cleanContent, @"<img[^>]*>", "")
// Remove plain GitHub attachment URLs
cleanContent <- Regex.Replace(cleanContent, @"https://github\.com/user-attachments/assets/[a-zA-Z0-9\-]+", "")
// Remove other media URLs that were detected as attachments
for (url, _) in mediaAttachments do
    cleanContent <- cleanContent.Replace(url, "")
cleanContent <- cleanContent.Trim()

// Validate that we have at least one media attachment
if mediaAttachments.Length = 0 then
    printfn "❌ Error: No media attachments found. Please drag and drop media files into the content field."
    exit 1

// Validate URLs and detect actual media types
let urlPattern = @"^https?://.+"
let validatedAttachments = ResizeArray<string * string * MediaTypes.MediaType>()

for (url, altText) in mediaAttachments do
    if not (Regex.IsMatch(url, urlPattern)) then
        printfn "❌ Error: Invalid attachment URL: %s" url
        exit 1
    
    // Detect actual media type for each URL
    let detectedType = detectMediaTypeFromUrl url |> Async.RunSynchronously
    
    // For GitHub attachment URLs where we can't determine the type definitively,
    // make intelligent guesses based on context and patterns
    let finalType = 
        if detectedType = MediaTypes.MediaType.Unknown && url.Contains("github.com/user-attachments/assets/") then
            // For GitHub attachment URLs, try to determine type from attachment patterns
            // Look for clues in the original content to see how this URL was referenced
            let isReferencedAsMarkdownImage = contentWithAttachments.Contains($"![") && contentWithAttachments.Contains(url) && contentWithAttachments.Contains($"]({url})")
            let isPlainUrl = not isReferencedAsMarkdownImage && contentWithAttachments.Contains(url)
            
            match mediaType with
            | "image" -> MediaTypes.MediaType.Image
            | "video" -> MediaTypes.MediaType.Video  
            | "audio" -> MediaTypes.MediaType.Audio
            | "mixed" -> 
                // For mixed media, be smarter about detection
                if isReferencedAsMarkdownImage then
                    MediaTypes.MediaType.Image
                elif isPlainUrl then
                    // Plain URLs in GitHub issues are often videos/non-image content
                    MediaTypes.MediaType.Video
                else
                    // Default fallback for unknown mixed media
                    MediaTypes.MediaType.Image
            | _ -> detectedType
        else
            detectedType
    
    validatedAttachments.Add((url, altText, finalType))

printfn "📁 Found %d media attachment(s)" validatedAttachments.Count
for (url, alt, finalType) in validatedAttachments do
    printfn "  - %s (alt: %s, type: %A)" url alt finalType

// Only validate compatibility for specific media types (not mixed)
let incompatibleAttachments = 
    if mediaType = "mixed" then
        [] // Allow any media types for mixed posts
    else
        validatedAttachments 
        |> Seq.filter (fun (url, _, detectedType) -> 
            // Skip validation for GitHub attachment URLs where we used user-specified type
            if url.Contains("github.com/user-attachments/assets/") then
                false
            else
                let compatibleTypes = 
                    match mediaType with
                    | "image" -> [MediaTypes.MediaType.Image]
                    | "video" -> [MediaTypes.MediaType.Video]
                    | "audio" -> [MediaTypes.MediaType.Audio]
                    | _ -> [MediaTypes.MediaType.Image; MediaTypes.MediaType.Video; MediaTypes.MediaType.Audio]
                not (List.contains detectedType compatibleTypes))
        |> Seq.toList

if not incompatibleAttachments.IsEmpty then
    printfn "❌ Error: Some attachments don't match the specified media type '%s':" mediaType
    for (url, _, detectedType) in incompatibleAttachments do
        printfn "  - %s (detected as %A)" url detectedType
    exit 1

// Slug generation and sanitization functions
let sanitizeSlug (slug: string) =
    slug.ToLowerInvariant()
        .Replace(" ", "-")
        .Replace("_", "-")
    |> fun s -> Regex.Replace(s, @"[^a-z0-9\-]", "")
    |> fun s -> Regex.Replace(s, @"-+", "-")
    |> fun s -> s.Trim('-')

let generateSlugFromTitle (title: string) =
    let slug = sanitizeSlug title
    if String.IsNullOrWhiteSpace(slug) then "untitled-media"
    elif slug.Length > 50 then slug.Substring(0, 50).TrimEnd('-')
    else slug

// Determine final slug
let finalSlug = 
    match customSlug with
    | Some slug -> 
        let sanitized = sanitizeSlug slug
        if String.IsNullOrWhiteSpace(sanitized) then generateSlugFromTitle title
        else sanitized
    | None -> generateSlugFromTitle title

// Process tags
let tags = 
    match tagsInput with
    | Some input -> 
        input.Split(',') 
        |> Array.map (fun tag -> tag.Trim().ToLowerInvariant())
        |> Array.filter (fun tag -> not (String.IsNullOrWhiteSpace(tag)))
        |> Array.distinct
    | None -> [||]

// Generate timestamp in EST (-05:00)
let now = DateTimeOffset.Now.ToOffset(TimeSpan.FromHours(-5.0))
let timestamp = now.ToString("yyyy-MM-dd HH:mm zzz")

// Generate frontmatter using media format (following existing pattern)
let tagsString = 
    if tags.Length = 0 then ""
    else sprintf "\ntags: [%s]" (tags |> Array.map (sprintf "\"%s\"") |> String.concat ",")

let frontmatter = 
    sprintf """---
title: %s
post_type: media
published_date: "%s"%s
---""" (title.Replace("\"", "\\\"")) timestamp tagsString

// Generate media block based on fields
let aspectRatio = 
    match orientation with
    | Some value -> value
    | None -> "landscape" // Default to landscape if not specified

// Create the :::media::: block with attachments using their detected types and alt text as captions
let generateMediaItem (url: string, altText: string, detectedType: MediaTypes.MediaType) =
    let mediaTypeString = 
        match detectedType with
        | MediaTypes.MediaType.Image -> "image"
        | MediaTypes.MediaType.Video -> "video"
        | MediaTypes.MediaType.Audio -> "audio"
        | _ -> "image" // Default fallback
    
    sprintf "- url: \"%s\"\n  mediaType: \"%s\"\n  aspectRatio: \"%s\"\n  caption: \"%s\"" 
        url mediaTypeString aspectRatio (altText.Replace("\"", "\\\""))

let mediaItems = validatedAttachments |> Seq.map generateMediaItem |> String.concat "\n"
let mediaBlock = sprintf ":::media\n%s\n:::media" mediaItems

// Generate filename
let filename = sprintf "%s.md" finalSlug

// Combine frontmatter, content, and media block
let fullContent = 
    let contentSection = 
        if String.IsNullOrWhiteSpace(cleanContent) then ""
        else sprintf "\n\n%s" cleanContent
    
    sprintf "%s%s\n\n%s" frontmatter contentSection mediaBlock

// Ensure _src/media directory exists
let mediaDir = Path.Combine("_src", "media")
if not (Directory.Exists(mediaDir)) then
    Directory.CreateDirectory(mediaDir) |> ignore

// Write file
let filePath = Path.Combine(mediaDir, filename)
File.WriteAllText(filePath, fullContent)

// Output success information
printfn "✅ Media post created successfully!"
printfn "📁 File: %s" filePath
printfn "🏷️  Title: %s" title
printfn "📸 Media Type: %s" mediaType
printfn "📄 Content: %s" (if String.IsNullOrWhiteSpace(cleanContent) then "none" else "included")
printfn "📐 Aspect Ratio: %s" aspectRatio
printfn "🔗 Slug: %s" finalSlug
printfn "📅 Date: %s" timestamp
printfn "🏷️  Tags: %s" (if tags.Length = 0 then "none" else String.concat ", " tags)
printfn "📊 Attachments: %d" validatedAttachments.Count
printfn ""
printfn "📄 Generated markdown file content:"
printfn "==========================================="
printfn "%s" fullContent
printfn "==========================================="
printfn ""
printfn "💾 File has been persisted to: %s" (Path.GetFullPath(filePath))
printfn "📊 File size: %d bytes" (FileInfo(filePath).Length)