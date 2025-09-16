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

// Validate media type
let validMediaTypes = ["image"; "video"; "audio"]
if not (List.contains mediaType validMediaTypes) then
    printfn "❌ Error: Media type must be one of: %s" (String.concat ", " validMediaTypes)
    exit 1

if String.IsNullOrWhiteSpace(title) then
    printfn "❌ Error: Title is required and cannot be empty"
    exit 1

if String.IsNullOrWhiteSpace(contentWithAttachments) then
    printfn "❌ Error: Content with attachments is required and cannot be empty"
    exit 1

// Parse markdown images from content using regex
// Pattern: ![alt-text](URL) or ![](URL)
let parseMarkdownImages (content: string) =
    let imagePattern = @"!\[([^\]]*)\]\(([^)]+)\)"
    let matches = Regex.Matches(content, imagePattern)
    
    [| for m in matches do
        let altText = m.Groups.[1].Value.Trim()
        let url = m.Groups.[2].Value.Trim()
        // Use filename as alt text if alt text is empty
        let finalAltText = 
            if String.IsNullOrWhiteSpace(altText) then 
                try
                    let uri = Uri(url)
                    Path.GetFileNameWithoutExtension(uri.LocalPath)
                with
                | _ -> "media"
            else altText
        (url, finalAltText) |]

// Extract images and clean content
let imageAttachments = parseMarkdownImages contentWithAttachments
let cleanContent = Regex.Replace(contentWithAttachments, @"!\[([^\]]*)\]\(([^)]+)\)", "").Trim()

// Validate that we have at least one image attachment
if imageAttachments.Length = 0 then
    printfn "❌ Error: No media attachments found. Please drag and drop media files into the content field."
    exit 1

// Validate URLs
let urlPattern = @"^https?://.+"
for (url, _) in imageAttachments do
    if not (Regex.IsMatch(url, urlPattern)) then
        printfn "❌ Error: Invalid attachment URL: %s" url
        exit 1

printfn "📁 Found %d media attachment(s)" imageAttachments.Length
for (url, alt) in imageAttachments do
    printfn "  - %s (alt: %s)" url alt

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

// Create the :::media::: block with attachments using their alt text as captions
let generateMediaItem (url: string, altText: string) =
    sprintf "- url: \"%s\"\n  mediaType: \"%s\"\n  aspectRatio: \"%s\"\n  caption: \"%s\"" 
        url mediaType aspectRatio (altText.Replace("\"", "\\\""))

let mediaItems = imageAttachments |> Array.map generateMediaItem |> String.concat "\n"
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
printfn "📊 Attachments: %d" imageAttachments.Length
printfn ""
printfn "📄 Generated markdown file content:"
printfn "==========================================="
printfn "%s" fullContent
printfn "==========================================="
printfn ""
printfn "💾 File has been persisted to: %s" (Path.GetFullPath(filePath))
printfn "📊 File size: %d bytes" (FileInfo(filePath).Length)