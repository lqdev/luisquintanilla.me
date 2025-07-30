(*
    Comprehensive Content Cross-Reference URL Fixing Script
    
    This script addresses the remaining 268 broken /feed/ URLs that are cross-references
    within content files (weekly summaries, posts, etc.)
    
    Strategy:
    1. Load all remaining broken /feed/ URLs
    2. Analyze actual content files to determine correct content type
    3. Create mapping from /feed/slug to correct path (/notes/, /responses/, /posts/, etc.)
    4. Apply fixes to all content files containing these cross-references
*)

#r "bin/Debug/net9.0/PersonalSite.dll"

open System
open System.IO
open System.Text.RegularExpressions
open Domain

let remainingFeedLinks = File.ReadAllLines("remaining-feed-links.txt")

printfn "Found %d remaining broken /feed/ URLs to fix" remainingFeedLinks.Length

// Load actual content from _src to determine content types
let loadFeedContent () =
    let feedDir = "_src/feed"
    let feedFiles = Directory.GetFiles(feedDir, "*.md")
    
    feedFiles
    |> Array.map (fun filePath ->
        let fileName = Path.GetFileNameWithoutExtension(filePath)
        let content = File.ReadAllText(filePath)
        
        // Extract post_type from front matter
        let postTypeMatch = Regex.Match(content, @"post_type:\s*""([^""]+)""")
        let responseTypeMatch = Regex.Match(content, @"response_type:\s*([^\r\n]+)")
        
        let contentType = 
            if postTypeMatch.Success then
                match postTypeMatch.Groups.[1].Value with
                | "note" -> "notes"
                | "response" -> "responses" 
                | "post" -> "posts"
                | "review" -> "reviews"
                | other -> 
                    printfn "Unknown post_type: %s for %s" other fileName
                    "notes" // default fallback
            elif responseTypeMatch.Success then
                "responses"
            else
                // Fallback: analyze content structure
                if content.Contains("targeturl:") || content.Contains("response_type:") then
                    "responses"
                else
                    "notes" // default for other content
        
        (fileName, contentType)
    )

let feedContentMapping = loadFeedContent()
printfn "Analyzed %d feed content files for type mapping" feedContentMapping.Length

// Create URL mapping from /feed/slug to correct content type path
let createUrlMapping () =
    remainingFeedLinks
    |> Array.map (fun url ->
        // Extract slug from URL (remove /feed/ prefix and trailing slash)
        let slug = url.Replace("/feed/", "").TrimEnd('/')
        
        // Find matching content file
        let matchingContent = 
            feedContentMapping
            |> Array.tryFind (fun (fileName, _) -> fileName = slug)
        
        match matchingContent with
        | Some (_, contentType) ->
            let newUrl = sprintf "/%s/%s" contentType slug
            (url, newUrl)
        | None ->
            // Handle cases where slug doesn't match filename exactly
            // This can happen with URL encoding, hyphens vs underscores, etc.
            let fuzzyMatch = 
                feedContentMapping
                |> Array.tryFind (fun (fileName, _) -> 
                    fileName.Replace("-", "").Replace("_", "").ToLower() = slug.Replace("-", "").Replace("_", "").ToLower())
            
            match fuzzyMatch with
            | Some (fileName, contentType) ->
                let newUrl = sprintf "/%s/%s" contentType fileName
                (url, newUrl)
            | None ->
                printfn "WARNING: No matching content found for URL: %s" url
                // Default to notes for unmatched URLs
                let newUrl = sprintf "/notes/%s" slug
                (url, newUrl)
    )

let urlMapping = createUrlMapping()

// Print mapping summary
printfn "\nURL Mapping Summary:"
let mappingByType = 
    urlMapping 
    |> Array.groupBy (fun (oldUrl, newUrl) -> 
        if newUrl.StartsWith("/notes/") then "notes"
        elif newUrl.StartsWith("/responses/") then "responses" 
        elif newUrl.StartsWith("/posts/") then "posts"
        elif newUrl.StartsWith("/reviews/") then "reviews"
        else "other")

mappingByType
|> Array.iter (fun (contentType, mappings) ->
    printfn "  %s: %d URLs" contentType mappings.Length)

// Find all markdown files that might contain cross-references
let findFilesWithCrossReferences () =
    let searchDirs = [
        "_src/feed"
        "_src/posts" 
        "_src/responses"
        "_src/wiki"
        "_src/library"
        "_src/snippets"
    ]
    
    searchDirs
    |> List.collect (fun dir ->
        if Directory.Exists(dir) then
            Directory.GetFiles(dir, "*.md", SearchOption.AllDirectories) |> Array.toList
        else
            []
    )

let contentFiles = findFilesWithCrossReferences()
printfn "\nFound %d content files to check for cross-references" contentFiles.Length

// Apply URL fixes to content files
let fixCrossReferences () =
    let mutable totalReplacements = 0
    let mutable filesModified = 0
    
    contentFiles
    |> List.iter (fun filePath ->
        let originalContent = File.ReadAllText(filePath)
        let mutable modifiedContent = originalContent
        let mutable fileReplacements = 0
        
        // Apply each URL mapping
        urlMapping
        |> Array.iter (fun (oldUrl, newUrl) ->
            // Match URLs in markdown links [text](url) and standalone URLs
            let patterns = [
                sprintf @"\](%s(\)|/))" (Regex.Escape(oldUrl))  // Markdown links
                sprintf @"\s%s(\s|$)" (Regex.Escape(oldUrl))   // Standalone URLs
            ]
            
            patterns
            |> List.iter (fun pattern ->
                let regex = Regex(pattern)
                if regex.IsMatch(modifiedContent) then
                    let replacementPattern = 
                        if pattern.Contains("]") then
                            sprintf "](%s)" newUrl
                        else
                            sprintf " %s " newUrl
                    modifiedContent <- regex.Replace(modifiedContent, replacementPattern)
                    fileReplacements <- fileReplacements + 1
            )
        )
        
        // Save modified content if changes were made
        if modifiedContent <> originalContent then
            File.WriteAllText(filePath, modifiedContent)
            filesModified <- filesModified + 1
            totalReplacements <- totalReplacements + fileReplacements
            printfn "Fixed %d cross-references in: %s" fileReplacements (Path.GetFileName(filePath))
    )
    
    printfn "\nCross-Reference Fix Summary:"
    printfn "  Files modified: %d" filesModified
    printfn "  Total replacements: %d" totalReplacements

// Save mapping for reference
let saveMappingReport () =
    let reportLines = [
        "# Content Cross-Reference URL Mapping Report"
        sprintf "Generated: %s" (DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
        ""
        sprintf "Total URLs mapped: %d" urlMapping.Length
        ""
        "## Mapping by Content Type"
    ]
    
    let mappingLines = 
        mappingByType
        |> Array.collect (fun (contentType, mappings) ->
            [|
                sprintf "### %s (%d URLs)" contentType mappings.Length
                ""
            |] |> Array.append (
                mappings 
                |> Array.map (fun (oldUrl, newUrl) -> sprintf "- `%s` → `%s`" oldUrl newUrl)
            ) |> Array.append [|""|]
        )
    
    let allLines = reportLines @ (Array.toList mappingLines)
    File.WriteAllLines("content-cross-reference-mapping.md", allLines)
    printfn "Saved mapping report to: content-cross-reference-mapping.md"

// Execute the fix process
printfn "Starting content cross-reference fixing process..."
saveMappingReport()
fixCrossReferences()
printfn "\nContent cross-reference fixing completed!"
