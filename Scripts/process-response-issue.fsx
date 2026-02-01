(*
    Process GitHub Issue Template for Response Creation
    
    This script processes GitHub issue template data to create a response post.
    Usage: dotnet fsi process-response-issue.fsx -- "response_type" "target_url" "title" "content" "optional-slug" "optional,tags"
*)

#r "../bin/Debug/net10.0/PersonalSite.dll"

open System
open System.IO
open System.Text.RegularExpressions

// Get command line arguments
let args = fsi.CommandLineArgs |> Array.skip 1

// Validate arguments
if args.Length < 3 then
    printfn "❌ Error: Missing required arguments"
    printfn "Usage: dotnet fsi process-response-issue.fsx -- \"response_type\" \"target_url\" \"title\" \"content\" \"optional-slug\" \"optional,tags\" \"optional-rsvp-status\""
    printfn "Example: dotnet fsi process-response-issue.fsx -- \"reply\" \"https://example.com\" \"My Response\" \"This is my response\" \"custom-slug\" \"indieweb,reply\" \"\""
    printfn "Example RSVP: dotnet fsi process-response-issue.fsx -- \"rsvp\" \"https://meetup.com/event\" \"RSVP to Event\" \"Looking forward to it!\" \"\" \"events\" \"yes\""
    exit 1

let responseType = args.[0]
let targetUrl = args.[1]
let title = args.[2]
let content = if args.Length > 3 then args.[3].Trim() else ""
let customSlug = if args.Length > 4 && not (String.IsNullOrWhiteSpace(args.[4])) then Some(args.[4]) else None
let tagsInput = if args.Length > 5 && not (String.IsNullOrWhiteSpace(args.[5])) then Some(args.[5]) else None
let rsvpStatusInput = if args.Length > 6 && not (String.IsNullOrWhiteSpace(args.[6])) && args.[6] <> "not applicable" then Some(args.[6]) else None

// Validate required fields
if String.IsNullOrWhiteSpace(responseType) then
    printfn "❌ Error: Response type is required"
    exit 1

let validResponseTypes = ["reply"; "reshare"; "star"; "rsvp"]
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

// Content validation - allow empty content for star and rsvp responses
if responseType <> "star" && responseType <> "rsvp" && String.IsNullOrWhiteSpace(content) then
    printfn "❌ Error: Content is required for %s responses" responseType
    exit 1

// RSVP status validation - required for RSVP responses
if responseType = "rsvp" && rsvpStatusInput.IsNone then
    printfn "❌ Error: RSVP status is required for RSVP responses (yes, no, maybe, or interested)"
    exit 1

let validRsvpStatuses = ["yes"; "no"; "maybe"; "interested"]
if rsvpStatusInput.IsSome && not (List.contains rsvpStatusInput.Value validRsvpStatuses) then
    printfn "❌ Error: RSVP status must be one of: %s" (String.concat ", " validRsvpStatuses)
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

// YouTube URL formatting functions
let isYouTubeUrl (url: string) =
    let patterns = [
        @"^https?://(www\.)?youtube\.com/watch\?v=[\w-]+.*$"
        @"^https?://(www\.)?youtu\.be/[\w-]+.*$"
        @"^https?://(www\.)?youtube\.com/embed/[\w-]+.*$"
    ]
    patterns |> List.exists (fun pattern -> Regex.IsMatch(url, pattern, RegexOptions.IgnoreCase))

let extractYouTubeVideoId (url: string) =
    let patterns = [
        (@"youtube\.com/watch\?v=([\w-]+)", 1)  // youtube.com/watch?v=VIDEO_ID
        (@"youtu\.be/([\w-]+)", 1)             // youtu.be/VIDEO_ID  
        (@"youtube\.com/embed/([\w-]+)", 1)    // youtube.com/embed/VIDEO_ID
    ]
    
    patterns
    |> List.tryPick (fun (pattern, groupIndex) ->
        let match' = Regex.Match(url, pattern, RegexOptions.IgnoreCase)
        if match'.Success && match'.Groups.Count > groupIndex then
            Some match'.Groups.[groupIndex].Value
        else
            None)

let formatYouTubeContent (title: string) (url: string) (content: string) =
    match extractYouTubeVideoId url with
    | Some videoId ->
        let thumbnailUrl = sprintf "http://img.youtube.com/vi/%s/0.jpg" videoId
        let youtubeMarkdown = sprintf "[![%s](%s)](%s \"%s\")" title thumbnailUrl url title
        
        // If there's additional content, add it after the YouTube thumbnail
        if String.IsNullOrWhiteSpace(content) then
            youtubeMarkdown
        else
            sprintf "%s\n\n%s" content youtubeMarkdown
    | None ->
        content // Return original content if video ID extraction fails

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

// Phase 6A: Include rsvp_status in frontmatter for RSVP responses
let rsvpStatusString =
    match rsvpStatusInput with
    | Some status -> sprintf "\nrsvp_status: %s" status
    | None -> ""

let frontmatter = 
    sprintf """---
title: "%s"
targeturl: %s
response_type: %s
dt_published: "%s"
dt_updated: "%s"%s%s
---""" (title.Replace("\"", "\\\"")) targetUrl responseType timestamp timestamp rsvpStatusString tagsString

// Generate filename - only append date when no custom slug provided
let filename = 
    match customSlug with
    | Some _ -> sprintf "%s.md" finalSlug  // Use slug as-is when custom slug provided
    | None -> sprintf "%s-%s.md" finalSlug (now.ToString("yyyy-MM-dd"))  // Append date when no custom slug

// Process content - apply YouTube formatting if target URL is a YouTube video
let processedContent = 
    if isYouTubeUrl targetUrl then
        formatYouTubeContent title targetUrl content
    else
        content

// Combine frontmatter and content
let fullContent = 
    if String.IsNullOrWhiteSpace(processedContent) then
        frontmatter // Just frontmatter for star responses with no content
    else
        sprintf "%s\n\n%s" frontmatter processedContent

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