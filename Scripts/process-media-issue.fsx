(*
    Process GitHub Issue Template for Media Creation
    
    This script processes GitHub issue template data to create a media post.
    Usage: dotnet fsi process-media-issue.fsx -- "media_type" "title" "attachment_url" "caption" "orientation" "optional-slug" "optional,tags"
*)

#r "../bin/Debug/net9.0/PersonalSite.dll"

open System
open System.IO
open System.Text.RegularExpressions

// Get command line arguments
let args = fsi.CommandLineArgs |> Array.skip 1

// Validate arguments
if args.Length < 4 then
    printfn "❌ Error: Missing required arguments"
    printfn "Usage: dotnet fsi process-media-issue.fsx -- \"media_type\" \"title\" \"attachment_urls\" \"content\" \"caption\" \"orientation\" \"optional-slug\" \"optional,tags\""
    printfn "Example: dotnet fsi process-media-issue.fsx -- \"image\" \"Beautiful Sunset\" \"https://github.com/user/repo/assets/123/sunset.jpg\" \"Beautiful day at the beach\" \"Golden hour at the beach\" \"landscape\" \"sunset-photo\" \"photography,nature\""
    exit 1

let mediaType = args.[0]
let title = args.[1]
let attachmentUrls = args.[2]
let content = if args.Length > 3 then args.[3].Trim() else ""
let caption = if args.Length > 4 then args.[4].Trim() else ""
let orientation = if args.Length > 5 && not (String.IsNullOrWhiteSpace(args.[5])) then Some(args.[5]) else None
let customSlug = if args.Length > 6 && not (String.IsNullOrWhiteSpace(args.[6])) then Some(args.[6]) else None
let tagsInput = if args.Length > 7 && not (String.IsNullOrWhiteSpace(args.[7])) then Some(args.[7]) else None

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

if String.IsNullOrWhiteSpace(attachmentUrls) then
    printfn "❌ Error: Attachment URL(s) is required and cannot be empty"
    exit 1

// Parse multiple attachment URLs (newline or comma separated)
let parseAttachmentUrls (input: string) =
    input.Split([|'\n'; '\r'; ','; ';'|], StringSplitOptions.RemoveEmptyEntries)
    |> Array.map (fun url -> url.Trim())
    |> Array.filter (fun url -> not (String.IsNullOrWhiteSpace(url)))

let attachmentUrlList = parseAttachmentUrls attachmentUrls

if attachmentUrlList.Length = 0 then
    printfn "❌ Error: No valid attachment URLs found"
    exit 1

// Validate URL formats for all attachments
let urlPattern = @"^https?://.+"
for url in attachmentUrlList do
    if not (Regex.IsMatch(url, urlPattern)) then
        printfn "❌ Error: All attachment URLs must be valid HTTP/HTTPS URLs. Invalid: %s" url
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

// Create the :::media::: block with multiple attachments
let generateMediaItem (url: string) =
    let captionField = 
        if String.IsNullOrWhiteSpace(caption) then ""
        else sprintf "\n  caption: \"%s\"" (caption.Replace("\"", "\\\""))
    
    sprintf "- url: \"%s\"\n  mediaType: \"%s\"\n  aspectRatio: \"%s\"%s" url mediaType aspectRatio captionField

let mediaItems = attachmentUrlList |> Array.map generateMediaItem |> String.concat "\n"

let mediaBlock = sprintf ":::media\n%s\n:::media" mediaItems

// Generate filename
let filename = sprintf "%s.md" finalSlug

// Combine frontmatter, content, and media block
let fullContent = 
    let contentSection = 
        if String.IsNullOrWhiteSpace(content) then ""
        else sprintf "\n\n%s" content
    
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
printfn "🔗 URLs: %s" (String.concat ", " attachmentUrlList)
printfn "📄 Content: %s" (if String.IsNullOrWhiteSpace(content) then "none" else "included")
printfn "📐 Aspect Ratio: %s" aspectRatio
printfn "💬 Caption: %s" (if String.IsNullOrWhiteSpace(caption) then "none" else caption)
printfn "🔗 Slug: %s" finalSlug
printfn "📅 Date: %s" timestamp
printfn "🏷️  Tags: %s" (if tags.Length = 0 then "none" else String.concat ", " tags)
printfn "📊 Attachments: %d" attachmentUrlList.Length
printfn ""
printfn "📄 Generated markdown file content:"
printfn "==========================================="
printfn "%s" fullContent
printfn "==========================================="
printfn ""
printfn "💾 File has been persisted to: %s" (Path.GetFullPath(filePath))
printfn "📊 File size: %d bytes" (FileInfo(filePath).Length)