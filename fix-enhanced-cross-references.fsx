(*
    Enhanced Content Cross-Reference URL Fixing Script
    
    This script addresses the remaining 268 broken /feed/ URLs that are cross-references
    within content files by checking ALL content directories for matching files.
*)

#r "bin/Debug/net9.0/PersonalSite.dll"

open System
open System.IO
open System.Text.RegularExpressions
open Domain

let remainingFeedLinks = File.ReadAllLines("remaining-feed-links.txt")

printfn "Found %d remaining broken /feed/ URLs to fix" remainingFeedLinks.Length

// Load content from ALL directories to create comprehensive mapping
let loadAllContent () =
    let contentDirs = [
        ("_src/feed", "notes")
        ("_src/responses", "responses") 
        ("_src/posts", "posts")
        ("_src/reviews", "reviews")
        ("_src/wiki", "wiki")
        ("_src/snippets", "snippets")
        ("_src/library", "library")
    ]
    
    contentDirs
    |> List.collect (fun (dir, defaultType) ->
        if Directory.Exists(dir) then
            let mdFiles = Directory.GetFiles(dir, "*.md")
            mdFiles
            |> Array.map (fun filePath ->
                let fileName = Path.GetFileNameWithoutExtension(filePath)
                let content = File.ReadAllText(filePath)
                
                // Extract actual content type from front matter
                let postTypeMatch = Regex.Match(content, @"post_type:\s*""([^""]+)""")
                let responseTypeMatch = Regex.Match(content, @"response_type:\s*([^\r\n]+)")
                
                let actualType = 
                    if postTypeMatch.Success then
                        match postTypeMatch.Groups.[1].Value with
                        | "note" -> "notes"
                        | "response" -> "responses" 
                        | "post" -> "posts"
                        | "review" -> "reviews"
                        | "wiki" -> "wiki"
                        | "snippet" -> "snippets"
                        | other -> defaultType
                    elif responseTypeMatch.Success then
                        "responses"
                    elif content.Contains("targeturl:") then
                        "responses"
                    else
                        defaultType
                
                (fileName, actualType, dir)
            )
            |> Array.toList
        else
            []
    )

let allContentMapping = loadAllContent()
printfn "Analyzed %d content files across all directories" allContentMapping.Length

// Create comprehensive URL mapping 
let createComprehensiveMapping () =
    remainingFeedLinks
    |> Array.map (fun url ->
        // Extract slug from URL (remove /feed/ prefix and trailing slash)
        let slug = url.Replace("/feed/", "").TrimEnd('/')
        
        // Find exact match first
        let exactMatch = 
            allContentMapping
            |> List.tryFind (fun (fileName, _, _) -> fileName = slug)
        
        match exactMatch with
        | Some (fileName, contentType, sourceDir) ->
            let newUrl = sprintf "/%s/%s" contentType fileName
            (url, newUrl, sprintf "EXACT: %s" sourceDir)
        | None ->
            // Try fuzzy matching for variations
            let fuzzyMatch = 
                allContentMapping
                |> List.tryFind (fun (fileName, _, _) -> 
                    fileName.Replace("-", "").Replace("_", "").ToLower() = slug.Replace("-", "").Replace("_", "").ToLower())
            
            match fuzzyMatch with
            | Some (fileName, contentType, sourceDir) ->
                let newUrl = sprintf "/%s/%s" contentType fileName
                (url, newUrl, sprintf "FUZZY: %s" sourceDir)
            | None ->
                // Check if it's a special case (like ending with .html or .md)
                let cleanSlug = slug.Replace(".html", "").Replace(".md", "")
                let specialMatch = 
                    allContentMapping
                    |> List.tryFind (fun (fileName, _, _) -> fileName = cleanSlug)
                
                match specialMatch with
                | Some (fileName, contentType, sourceDir) ->
                    let newUrl = sprintf "/%s/%s" contentType fileName
                    (url, newUrl, sprintf "CLEANED: %s" sourceDir)
                | None ->
                    printfn "WARNING: No matching content found for URL: %s" url
                    // Default to notes for unmatched URLs
                    let newUrl = sprintf "/notes/%s" slug
                    (url, newUrl, "DEFAULT")
    )

let urlMappingWithSource = createComprehensiveMapping()
let urlMapping = urlMappingWithSource |> Array.map (fun (oldUrl, newUrl, _) -> (oldUrl, newUrl))

// Print enhanced mapping summary
printfn "\nURL Mapping Summary:"
let mappingByType = 
    urlMapping 
    |> Array.groupBy (fun (oldUrl, newUrl) -> 
        if newUrl.StartsWith("/notes/") then "notes"
        elif newUrl.StartsWith("/responses/") then "responses" 
        elif newUrl.StartsWith("/posts/") then "posts"
        elif newUrl.StartsWith("/reviews/") then "reviews"
        elif newUrl.StartsWith("/wiki/") then "wiki"
        elif newUrl.StartsWith("/snippets/") then "snippets"
        elif newUrl.StartsWith("/library/") then "library"
        else "other")

mappingByType
|> Array.iter (fun (contentType, mappings) ->
    printfn "  %s: %d URLs" contentType mappings.Length)

// Show source analysis
printfn "\nMapping Source Analysis:"
let sourceStats = 
    urlMappingWithSource 
    |> Array.groupBy (fun (_, _, source) -> source)
    |> Array.sortByDescending (fun (_, mappings) -> mappings.Length)

sourceStats
|> Array.iter (fun (source, mappings) ->
    printfn "  %s: %d URLs" source mappings.Length)

// Find all content files that might contain cross-references
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

// Enhanced fix function with better pattern matching
let fixCrossReferences () =
    let mutable totalReplacements = 0
    let mutable filesModified = 0
    
    contentFiles
    |> List.iter (fun filePath ->
        let originalContent = File.ReadAllText(filePath)
        let mutable modifiedContent = originalContent
        let mutable fileReplacements = 0
        
        // Apply each URL mapping with improved pattern matching
        urlMapping
        |> Array.iter (fun (oldUrl, newUrl) ->
            // Pattern 1: Markdown links [text](url) or [text](url/)
            let markdownPattern = sprintf @"\]\(%s/?(\)|#[^)]*\))" (Regex.Escape(oldUrl))
            let markdownRegex = Regex(markdownPattern)
            if markdownRegex.IsMatch(modifiedContent) then
                modifiedContent <- markdownRegex.Replace(modifiedContent, sprintf "](%s)" newUrl)
                fileReplacements <- fileReplacements + 1
            
            // Pattern 2: Standalone URLs at word boundaries
            let standalonePattern = sprintf @"\b%s/?(\b|$)" (Regex.Escape(oldUrl))
            let standaloneRegex = Regex(standalonePattern)
            if standaloneRegex.IsMatch(modifiedContent) then
                modifiedContent <- standaloneRegex.Replace(modifiedContent, newUrl)
                fileReplacements <- fileReplacements + 1
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

// Enhanced mapping report
let saveMappingReport () =
    let reportLines = [
        "# Enhanced Content Cross-Reference URL Mapping Report"
        sprintf "Generated: %s" (DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
        ""
        sprintf "Total URLs mapped: %d" urlMapping.Length
        ""
        "## Mapping by Content Type"
    ]
    
    let typeLines = 
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
    
    let sourceLines = 
        sourceStats
        |> Array.collect (fun (source, mappings) ->
            [|
                sprintf "### %s (%d URLs)" source mappings.Length
                ""
            |] |> Array.append (
                mappings 
                |> Array.map (fun (oldUrl, newUrl, _) -> sprintf "- `%s` → `%s`" oldUrl newUrl)
            ) |> Array.append [|""|]
        )
    
    let allLines = reportLines @ (Array.toList typeLines) @ [""; "## Mapping by Source Analysis"; ""] @ (Array.toList sourceLines)
    File.WriteAllLines("enhanced-cross-reference-mapping.md", allLines)
    printfn "Saved enhanced mapping report to: enhanced-cross-reference-mapping.md"

// Execute the enhanced fix process
printfn "Starting enhanced content cross-reference fixing process..."
saveMappingReport()
fixCrossReferences()
printfn "\nEnhanced content cross-reference fixing completed!"
