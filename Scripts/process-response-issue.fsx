(*
    Process GitHub Issue Template for Response Creation
    
    This script processes GitHub issue template data to create a response post.
    Usage: dotnet fsi process-response-issue.fsx -- "response_type" "target_url" "title" "content" "optional-slug" "optional,tags"
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
    printfn "Usage: dotnet fsi process-response-issue.fsx -- \"response_type\" \"target_url\" \"title\" \"content\" \"optional-slug\" \"optional,tags\""
    printfn "Example: dotnet fsi process-response-issue.fsx -- \"reply\" \"https://example.com\" \"My Response\" \"This is my response\" \"custom-slug\" \"indieweb,reply\""
    exit 1

let responseType = args.[0]
let targetUrl = args.[1]
let title = args.[2]
let content = if args.Length > 3 then args.[3].Trim() else ""
let customSlug = if args.Length > 4 && not (String.IsNullOrWhiteSpace(args.[4])) then Some(args.[4]) else None
let tagsInput = if args.Length > 5 && not (String.IsNullOrWhiteSpace(args.[5])) then Some(args.[5]) else None

// Validate required fields
if String.IsNullOrWhiteSpace(responseType) then
    printfn "❌ Error: Response type is required"
    exit 1

let validResponseTypes = ["reply"; "reshare"; "star"]
if not (List.contains responseType validResponseTypes) then
    printfn "❌ Error: Response type must be one of: %s" (String.concat ", " validResponseTypes)
    exit 1

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

// Content validation - allow empty content for star responses
if responseType <> "star" && String.IsNullOrWhiteSpace(content) then
    printfn "❌ Error: Content is required for %s responses" responseType
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
    if String.IsNullOrWhiteSpace(slug) then "untitled-response"
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

// Generate frontmatter using response format
let tagsString = 
    if tags.Length = 0 then ""
    else sprintf "\ntags: [%s]" (tags |> Array.map (sprintf "\"%s\"") |> String.concat ",")

let frontmatter = 
    sprintf """---
title: "%s"
targeturl: %s
response_type: %s
dt_published: "%s"
dt_updated: "%s"%s
---""" (title.Replace("\"", "\\\"")) targetUrl responseType timestamp timestamp tagsString

// Generate filename - only append date when no custom slug provided
let filename = 
    match customSlug with
    | Some _ -> sprintf "%s.md" finalSlug  // Use slug as-is when custom slug provided
    | None -> sprintf "%s-%s.md" finalSlug (now.ToString("yyyy-MM-dd"))  // Append date when no custom slug

// Combine frontmatter and content
let fullContent = 
    if String.IsNullOrWhiteSpace(content) then
        frontmatter // Just frontmatter for star responses with no content
    else
        sprintf "%s\n\n%s" frontmatter content

// Ensure _src/responses directory exists
let responsesDir = Path.Combine("_src", "responses")
if not (Directory.Exists(responsesDir)) then
    Directory.CreateDirectory(responsesDir) |> ignore

// Write file
let filePath = Path.Combine(responsesDir, filename)
File.WriteAllText(filePath, fullContent)

// Output success information
printfn "✅ Response post created successfully!"
printfn "📁 File: %s" filePath
printfn "🏷️  Title: %s" title
printfn "🎯 Target: %s" targetUrl
printfn "📝 Type: %s" responseType
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