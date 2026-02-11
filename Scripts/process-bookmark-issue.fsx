(*
    Process GitHub Issue Template for Bookmark Creation
    
    This script processes GitHub issue template data to create a bookmark post.
    Usage: dotnet fsi process-bookmark-issue.fsx -- "target_url" "title" "content" "optional-slug" "optional,tags"
*)

#r "../bin/Debug/net10.0/PersonalSite.dll"

open System
open System.IO
open System.Text.RegularExpressions

// Get command line arguments
let args = fsi.CommandLineArgs |> Array.skip 1

// Validate arguments
if args.Length < 2 then
    printfn "❌ Error: Missing required arguments"
    printfn "Usage: dotnet fsi process-bookmark-issue.fsx -- \"target_url\" \"title\" \"content\" \"optional-slug\" \"optional,tags\""
    printfn "Example: dotnet fsi process-bookmark-issue.fsx -- \"https://example.com\" \"My Bookmark\" \"This is a great resource\" \"custom-slug\" \"tools,webdev\""
    exit 1

let targetUrl = args.[0]
let title = args.[1]
let content = if args.Length > 2 then args.[2].Trim() else ""
let customSlug = if args.Length > 3 && not (String.IsNullOrWhiteSpace(args.[3])) then Some(args.[3]) else None
let tagsInput = if args.Length > 4 && not (String.IsNullOrWhiteSpace(args.[4])) then Some(args.[4]) else None

// Validate required fields
if String.IsNullOrWhiteSpace(targetUrl) then
    printfn "❌ Error: Target URL is required and cannot be empty"
    exit 1

// Validate URL format
let urlPattern = @"^https?://.+"
if not (Regex.IsMatch(targetUrl, urlPattern)) then
    printfn "❌ Error: Target URL must be a valid HTTP/HTTPS URL"
    exit 1

if String.IsNullOrWhiteSpace(title) then
    printfn "❌ Error: Title is required and cannot be empty"
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
    if String.IsNullOrWhiteSpace(slug) then "untitled-bookmark"
    elif slug.Length > 50 then slug.Substring(0, 50).TrimEnd('-')
    else slug

// Determine final slug and track if we have a valid custom slug
let finalSlug, hasValidCustomSlug = 
    match customSlug with
    | Some slug -> 
        let sanitized = sanitizeSlug slug
        if String.IsNullOrWhiteSpace(sanitized) then 
            (generateSlugFromTitle title, false)  // Custom slug was invalid, treat as auto-generated
        else 
            (sanitized, true)  // Valid custom slug
    | None -> (generateSlugFromTitle title, false)  // No custom slug provided

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

// Generate frontmatter using bookmark format (following existing pattern)
let tagsString = 
    if tags.Length = 0 then ""
    else sprintf "\ntags: [%s]" (tags |> Array.map (sprintf "\"%s\"") |> String.concat ",")

let frontmatter = 
    sprintf """---
title: "%s"
targeturl: %s
response_type: bookmark
dt_published: "%s"
dt_updated: "%s"%s
---""" (title.Replace("\"", "\\\"")) targetUrl timestamp timestamp tagsString

// Generate filename - only append date when no valid custom slug provided
let filename = 
    if hasValidCustomSlug then
        sprintf "%s.md" finalSlug  // Use slug as-is when valid custom slug provided
    else
        sprintf "%s-%s.md" finalSlug (now.ToString("yyyy-MM-dd"))  // Append date when no valid custom slug

// Combine frontmatter and content
let fullContent = 
    if String.IsNullOrWhiteSpace(content) then
        frontmatter // Just frontmatter for bookmarks with no content
    else
        sprintf "%s\n\n%s" frontmatter content

// Ensure _src/bookmarks directory exists
let bookmarksDir = Path.Combine("_src", "bookmarks")
if not (Directory.Exists(bookmarksDir)) then
    Directory.CreateDirectory(bookmarksDir) |> ignore

// Write file
let filePath = Path.Combine(bookmarksDir, filename)
File.WriteAllText(filePath, fullContent)

// Output success information
printfn "✅ Bookmark post created successfully!"
printfn "📁 File: %s" filePath
printfn "🏷️  Title: %s" title
printfn "🎯 Target: %s" targetUrl
printfn "📝 Type: bookmark"
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