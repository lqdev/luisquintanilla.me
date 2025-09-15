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

// Function to detect and format code blocks within content
let enhanceCodeBlocks (content: string) =
    let lines = content.Split([|'\n'|], StringSplitOptions.None)
    let mutable result = []
    let mutable i = 0
    
    // Patterns to detect different programming languages
    let detectLanguage (line: string) =
        let trimmedLine = line.Trim()
        if trimmedLine.StartsWith("let ") || trimmedLine.StartsWith("printfn ") || trimmedLine.Contains(" -> ") || trimmedLine.StartsWith("open ") || trimmedLine.StartsWith("module ") then
            "fsharp"
        elif trimmedLine.StartsWith("var ") || trimmedLine.StartsWith("const ") || trimmedLine.StartsWith("function ") || trimmedLine.Contains("console.log") || trimmedLine.StartsWith("let ") && trimmedLine.Contains("=") then
            "javascript"
        elif trimmedLine.StartsWith("def ") || trimmedLine.StartsWith("import ") || trimmedLine.StartsWith("from ") || trimmedLine.Contains("print(") || trimmedLine.Contains("return ") && trimmedLine.TrimEnd().EndsWith(":") = false then
            "python"
        elif trimmedLine.StartsWith("using ") || trimmedLine.StartsWith("public ") || trimmedLine.StartsWith("private ") || trimmedLine.Contains("Console.WriteLine") then
            "csharp"
        elif trimmedLine.StartsWith("SELECT ") || trimmedLine.StartsWith("INSERT ") || trimmedLine.StartsWith("UPDATE ") || trimmedLine.StartsWith("DELETE ") then
            "sql"
        elif trimmedLine.StartsWith("<!DOCTYPE") || trimmedLine.StartsWith("<html") || trimmedLine.StartsWith("<div") then
            "html"
        elif trimmedLine.StartsWith(".") || trimmedLine.Contains("display:") || trimmedLine.Contains("background:") then
            "css"
        else
            ""
    
    // Check if a line looks like code (indented or has programming patterns)
    let looksLikeCode (line: string) =
        let trimmed = line.Trim()
        if String.IsNullOrWhiteSpace(trimmed) then false
        else
            // Check for common code patterns
            let startsWithIndent = line.StartsWith("    ") || line.StartsWith("\t")
            let hasCodePatterns = 
                trimmed.Contains("(") && trimmed.Contains(")") ||
                trimmed.Contains("{") && trimmed.Contains("}") ||
                trimmed.Contains("[") && trimmed.Contains("]") ||
                trimmed.EndsWith(":") ||
                trimmed.EndsWith(";") ||
                detectLanguage line <> ""
            
            startsWithIndent || hasCodePatterns
    
    // Process each line
    while i < lines.Length do
        let line = lines.[i]
        let trimmedLine = line.Trim()
        
        // Skip lines that are already in markdown codeblocks
        if trimmedLine.StartsWith("```") then
            result <- line :: result
            i <- i + 1
        // Check if this line looks like code and is not empty
        elif not (String.IsNullOrWhiteSpace(trimmedLine)) then
            let language = detectLanguage line
            
            // If this looks like code, check the next lines to see if we should create a code block
            if language <> "" || looksLikeCode line then
                let mutable codeLines = [line]
                let mutable j = i + 1
                let mutable foundMoreCode = false
                
                // Look ahead for more code lines (allowing for one empty line)
                while j < lines.Length && j < i + 4 do
                    let nextLine = lines.[j]
                    let nextTrimmed = nextLine.Trim()
                    
                    if String.IsNullOrWhiteSpace(nextTrimmed) then
                        // Empty line, check if the line after has code
                        if j + 1 < lines.Length then
                            let lineAfter = lines.[j + 1].Trim()
                            if not (String.IsNullOrWhiteSpace(lineAfter)) && looksLikeCode lines.[j + 1] then
                                codeLines <- nextLine :: codeLines  // Include the empty line
                                foundMoreCode <- true
                        j <- j + 1
                    elif looksLikeCode nextLine then
                        codeLines <- nextLine :: codeLines
                        foundMoreCode <- true
                        j <- j + 1
                    else
                        // Found non-code line, stop looking
                        j <- lines.Length
                
                if foundMoreCode then
                    // Create a code block - use specific language if detected, otherwise try to detect from all lines
                    let finalLanguage = 
                        if language <> "" then language
                        else
                            // Try to detect language from any line in the block
                            codeLines 
                            |> List.rev 
                            |> List.tryPick (fun l -> let lang = detectLanguage l in if lang <> "" then Some lang else None)
                            |> Option.defaultValue ""
                    
                    result <- sprintf "```%s" finalLanguage :: result
                    for codeLine in List.rev codeLines do
                        result <- codeLine :: result
                    result <- "```" :: result
                    i <- i + codeLines.Length
                else
                    // Just a single line that looks like code, treat as normal text
                    result <- line :: result
                    i <- i + 1
            else
                result <- line :: result
                i <- i + 1
        else
            result <- line :: result
            i <- i + 1
    
    // Return the enhanced content
    String.Join("\n", List.rev result)

// Function to unwrap content from code blocks
let unwrapCodeBlock (content: string) =
    let trimmed = content.Trim()
    
    // Check if content is wrapped in code blocks (```text, ```markdown, or just ```)
    // Use greedy matching and ensure we match the last ``` at the end of string
    let codeBlockPattern = @"^```(?:text|markdown)?\s*\n([\s\S]*)\n```$"
    let match' = Regex.Match(trimmed, codeBlockPattern, RegexOptions.Multiline)
    
    if match'.Success then
        // Extract the inner content from the code block and enhance it
        let innerContent = match'.Groups.[1].Value
        enhanceCodeBlocks innerContent
    else
        // No code block wrapping, return content as-is
        trimmed

// Process the content to unwrap any code block formatting
let content = unwrapCodeBlock rawContent

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