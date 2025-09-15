(*
    Process GitHub Issue Template for Note Creation
    
    This script processes GitHub issue template data to create a note post.
    Usage: dotnet fsi process-github-issue.fsx -- "title" "content" "optional-slug" "optional,tags"
*)

#r "../bin/Debug/net9.0/PersonalSite.dll"

open System
open System.IO
open System.Text.RegularExpressions

// Get command line arguments
let args = fsi.CommandLineArgs |> Array.skip 1

// Validate arguments
if args.Length < 2 then
    printfn "❌ Error: Missing required arguments"
    printfn "Usage: dotnet fsi process-github-issue.fsx -- \"title\" \"content\" \"optional-slug\" \"optional,tags\""
    printfn "Example: dotnet fsi process-github-issue.fsx -- \"My Note\" \"This is my content\" \"custom-slug\" \"tech,programming\""
    exit 1

let title = args.[0]
let rawContent = args.[1]
let customSlug = if args.Length > 2 && not (String.IsNullOrWhiteSpace(args.[2])) then Some(args.[2]) else None
let tagsInput = if args.Length > 3 && not (String.IsNullOrWhiteSpace(args.[3])) then Some(args.[3]) else None

// Use content directly - user is responsible for proper markdown formatting
let content = rawContent.Trim()

// Validate required fields
if String.IsNullOrWhiteSpace(title) then
    printfn "❌ Error: Title is required and cannot be empty"
    exit 1

if String.IsNullOrWhiteSpace(content) then
    printfn "❌ Error: Content is required and cannot be empty"
    exit 1

if content.Replace(" ", "").Replace("\n", "").Replace("\t", "").Length < 10 then
    printfn "❌ Error: Content must have at least 10 non-whitespace characters"
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
    if String.IsNullOrWhiteSpace(slug) then "untitled-note"
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
let timestamp = now.ToString("M/d/yyyy h:mm tt zzz")

// Generate frontmatter
let tagsString = 
    if tags.Length = 0 then "[]"
    else sprintf "[%s]" (tags |> Array.map (sprintf "\"%s\"") |> String.concat ",")

let frontmatter = 
    sprintf """---
post_type: "note"
title: "%s"
published_date: "%s"
tags: %s
---""" (title.Replace("\"", "\\\"")) timestamp tagsString

// Generate filename - only append date when no custom slug provided
let filename = 
    match customSlug with
    | Some _ -> sprintf "%s.md" finalSlug  // Use slug as-is when custom slug provided
    | None -> sprintf "%s-%s.md" finalSlug (now.ToString("yyyy-MM-dd"))  // Append date when no custom slug

// Combine frontmatter and content
let fullContent = sprintf "%s\n\n%s" frontmatter content

// Ensure _src/notes directory exists
let notesDir = Path.Combine("_src", "notes")
if not (Directory.Exists(notesDir)) then
    Directory.CreateDirectory(notesDir) |> ignore

// Write file
let filePath = Path.Combine(notesDir, filename)
File.WriteAllText(filePath, fullContent)

// Output success information
printfn "✅ Note post created successfully!"
printfn "📁 File: %s" filePath
printfn "🏷️  Title: %s" title
printfn "🔗 Slug: %s" finalSlug
printfn "📅 Date: %s" timestamp
printfn "🏷️  Tags: %s" (if tags.Length = 0 then "none" else String.concat ", " tags)
printfn ""
printfn "📄 Content preview:"
printfn "%s" fullContent