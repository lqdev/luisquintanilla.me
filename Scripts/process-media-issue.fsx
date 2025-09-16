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
if args.Length < 3 then
    printfn "❌ Error: Missing required arguments"
    printfn "Usage: dotnet fsi process-media-issue.fsx -- \"media_type\" \"title\" \"attachment_url\" \"caption\" \"orientation\" \"optional-slug\" \"optional,tags\""
    printfn "Example: dotnet fsi process-media-issue.fsx -- \"image\" \"Beautiful Sunset\" \"https://github.com/user/repo/assets/123/sunset.jpg\" \"Golden hour at the beach\" \"landscape\" \"sunset-photo\" \"photography,nature\""
    exit 1

let mediaType = args.[0]
let title = args.[1]
let attachmentUrl = args.[2]
let caption = if args.Length > 3 then args.[3].Trim() else ""
let orientation = if args.Length > 4 && not (String.IsNullOrWhiteSpace(args.[4])) then Some(args.[4]) else None
let customSlug = if args.Length > 5 && not (String.IsNullOrWhiteSpace(args.[5])) then Some(args.[5]) else None
let tagsInput = if args.Length > 6 && not (String.IsNullOrWhiteSpace(args.[6])) then Some(args.[6]) else None

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

if String.IsNullOrWhiteSpace(attachmentUrl) then
    printfn "❌ Error: Attachment URL is required and cannot be empty"
    exit 1

// Validate URL format (should be a GitHub attachment URL or similar)
let urlPattern = @"^https?://.+"
if not (Regex.IsMatch(attachmentUrl, urlPattern)) then
    printfn "❌ Error: Attachment URL must be a valid HTTP/HTTPS URL"
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

// Create the :::media::: block
let mediaBlock = 
    let captionField = 
        if String.IsNullOrWhiteSpace(caption) then ""
        else sprintf "\n  caption: \"%s\"" (caption.Replace("\"", "\\\""))
    
    sprintf """:::media
- url: "%s"
  mediaType: "%s"
  aspectRatio: "%s"%s
:::media""" attachmentUrl mediaType aspectRatio captionField

// Generate filename
let filename = sprintf "%s.md" finalSlug

// Combine frontmatter and media block
let fullContent = sprintf "%s\n\n%s" frontmatter mediaBlock

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
printfn "🔗 URL: %s" attachmentUrl
printfn "📐 Aspect Ratio: %s" aspectRatio
printfn "💬 Caption: %s" (if String.IsNullOrWhiteSpace(caption) then "none" else caption)
printfn "🔗 Slug: %s" finalSlug
printfn "📅 Date: %s" timestamp
printfn "🏷️  Tags: %s" (if tags.Length = 0 then "none" else String.concat ", " tags)
printfn ""
printfn "📄 Generated markdown file content:"
printfn "==========================================="
printfn "%s" fullContent
printfn "==========================================="
printfn ""
printfn "💾 File has been persisted to: %s" (Path.GetFullPath(filePath))
printfn "📊 File size: %d bytes" (FileInfo(filePath).Length)