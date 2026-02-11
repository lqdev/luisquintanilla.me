(*
    Process GitHub Issue Template for Playlist Creation
    
    This script processes GitHub issue template data to create a playlist post.
    Usage: dotnet fsi process-playlist-issue.fsx -- "title" "spotify-url" "optional-commentary" "optional-slug" "optional,tags" "playlist-content"
*)

#r "../bin/Debug/net10.0/PersonalSite.dll"

open System
open System.IO
open System.Text.RegularExpressions

// Get command line arguments
let args = fsi.CommandLineArgs |> Array.skip 1

// Validate arguments
if args.Length < 6 then
    printfn "❌ Error: Missing required arguments"
    printfn "Usage: dotnet fsi process-playlist-issue.fsx -- \"title\" \"spotify-url\" \"optional-commentary\" \"optional-slug\" \"optional,tags\" \"playlist-content\""
    exit 1

let title = args.[0]
let spotifyUrl = args.[1]
let commentary = if args.Length > 2 && not (String.IsNullOrWhiteSpace(args.[2])) then Some(args.[2].Trim()) else None
let customSlug = if args.Length > 3 && not (String.IsNullOrWhiteSpace(args.[3])) then Some(args.[3]) else None
let tagsInput = if args.Length > 4 && not (String.IsNullOrWhiteSpace(args.[4])) then Some(args.[4]) else None
let playlistContent = if args.Length > 5 && not (String.IsNullOrWhiteSpace(args.[5])) then args.[5].Trim() else ""

// Validate required fields
if String.IsNullOrWhiteSpace(title) then
    printfn "❌ Error: Title is required and cannot be empty"
    exit 1

if String.IsNullOrWhiteSpace(spotifyUrl) then
    printfn "❌ Error: Spotify URL is required and cannot be empty"
    exit 1

// Validate Spotify URL format
let spotifyUrlPattern = @"^https://open\.spotify\.com/playlist/[a-zA-Z0-9]+(\?.*)?$"
if not (Regex.IsMatch(spotifyUrl, spotifyUrlPattern)) then
    printfn "❌ Error: Invalid Spotify playlist URL format"
    printfn "Expected format: https://open.spotify.com/playlist/[playlist-id]"
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
    if String.IsNullOrWhiteSpace(slug) then "untitled-playlist"
    elif slug.Length > 60 then slug.Substring(0, 60).TrimEnd('-')
    else slug

// Determine final slug
let finalSlug = 
    match customSlug with
    | Some slug -> 
        let sanitized = sanitizeSlug slug
        if String.IsNullOrWhiteSpace(sanitized) then generateSlugFromTitle title
        else sanitized
    | None -> generateSlugFromTitle title

// Process tags - default tags + additional tags
let defaultTags = [| "playlist"; "music"; "spotify"; "youtube"; "cratefinds" |]
let additionalTags = 
    match tagsInput with
    | Some input -> 
        input.Split(',') 
        |> Array.map (fun tag -> tag.Trim().ToLowerInvariant())
        |> Array.filter (fun tag -> not (String.IsNullOrWhiteSpace(tag)))
    | None -> [||]

let allTags = 
    Array.append defaultTags additionalTags
    |> Array.distinct
    |> Array.sort

// Generate timestamp in EST (-05:00)
let now = DateTimeOffset.Now.ToOffset(TimeSpan.FromHours(-5.0))
let timestamp = now.ToString("yyyy-MM-dd HH:mm zzz")

// Generate frontmatter
let tagsString = sprintf "[%s]" (allTags |> Array.map (sprintf "\"%s\"") |> String.concat ",")

let frontmatter = 
    sprintf """---
title: "%s"
date: "%s"
tags: %s
---""" (title.Replace("\"", "\\\"")) timestamp tagsString

// Process playlist content: strip debug output before "## Tracks"
let cleanedPlaylistContent =
    if String.IsNullOrWhiteSpace(playlistContent) then
        ""
    else
        // Split lines but keep empty lines to preserve formatting
        let lines = playlistContent.Split([|"\r\n"; "\n"|], StringSplitOptions.None)
        let tracksIndex = 
            lines 
            |> Array.tryFindIndex (fun line -> line.TrimStart().StartsWith("## Tracks"))
        
        match tracksIndex with
        | Some idx ->
            // Include everything from "## Tracks" onwards, preserving empty lines
            lines.[idx..] |> String.concat "\n"
        | None ->
            // If no "## Tracks" header found, keep original content
            playlistContent

// Check if playlist content already has a footer with "Generated using..."
let hasGeneratedFooter = 
    not (String.IsNullOrWhiteSpace(cleanedPlaylistContent)) && 
    cleanedPlaylistContent.Contains("*Generated using")

// Check if playlist content already includes the Spotify link
let hasSpotifyLink = 
    not (String.IsNullOrWhiteSpace(cleanedPlaylistContent)) &&
    cleanedPlaylistContent.Contains("**Original Spotify Playlist:**")

// Build content body
let contentParts = 
    [
        // Add commentary if provided
        match commentary with
        | Some text -> yield text
        | None -> ()
        
        // Add cleaned playlist content if provided
        if not (String.IsNullOrWhiteSpace(cleanedPlaylistContent)) then
            yield ""
            yield cleanedPlaylistContent
        
        // Only add footer separator if playlist content doesn't already have one
        if not hasGeneratedFooter then
            yield ""
            yield "---"
            yield ""
            // Add the Spotify link only if it's not already in the playlist content
            yield sprintf "**Original Spotify Playlist:** [Listen on Spotify](%s)." spotifyUrl
        // If the footer exists but somehow doesn't have the Spotify link, add it
        elif not hasSpotifyLink then
            yield ""
            yield sprintf "**Original Spotify Playlist:** [Listen on Spotify](%s)." spotifyUrl
    ]

let contentBody = String.concat "\n" contentParts

// Combine frontmatter and content
let fullContent = sprintf "%s\n\n%s" frontmatter contentBody

// Generate filename - only append date when no custom slug provided
let filename = 
    match customSlug with
    | Some _ -> sprintf "%s.md" finalSlug  // Use slug as-is when custom slug provided
    | None -> sprintf "%s-%s.md" finalSlug (now.ToString("yyyy-MM-dd"))  // Append date when no custom slug

// Ensure _src/playlists directory exists
let playlistsDir = Path.Combine("_src", "playlists")
if not (Directory.Exists(playlistsDir)) then
    Directory.CreateDirectory(playlistsDir) |> ignore

// Write file
let filePath = Path.Combine(playlistsDir, filename)
File.WriteAllText(filePath, fullContent)

// Output success information
printfn "✅ Playlist post created successfully!"
printfn "📁 File: %s" filePath
printfn "🏷️  Title: %s" title
printfn "🔗 Slug: %s" finalSlug
printfn "📅 Date: %s" timestamp
printfn "🏷️  Tags: %s" (String.concat ", " allTags)
printfn "🎵 Spotify URL: %s" spotifyUrl
printfn ""
printfn "📄 Generated markdown file content:"
printfn "==========================================="
printfn "%s" fullContent
printfn "==========================================="
printfn ""
printfn "💾 File has been persisted to: %s" (Path.GetFullPath(filePath))
printfn "📊 File size: %d bytes" (FileInfo(filePath).Length)
