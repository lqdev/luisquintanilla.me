#!/usr/bin/env dotnet fsi

// Debug what book.Content actually contains
#r "bin/Debug/net9.0/PersonalSite.dll"

open System.IO
open Domain

// Load the books to see what Content actually contains
let loadBooks() =
    let files = Directory.GetFiles("_src/reviews/library", "*.md")
    files
    |> Array.map (fun file ->
        printfn "Loading file: %s" file
        let content = File.ReadAllText(file)
        printfn "Raw content (first 500 chars): %s" (content.Substring(0, min 500 content.Length))
        let frontmatter, bodyContent = MarkdownService.extractFrontmatter content
        printfn "Body content (first 500 chars): %s" (bodyContent.Substring(0, min 500 bodyContent.Length))
        let metadata = MarkdownService.parseBookMetadata frontmatter file
        let processedContent = MarkdownService.processMarkdown bodyContent
        printfn "Processed content (first 500 chars): %s" (processedContent.Substring(0, min 500 processedContent.Length))
        { 
            FileName = Path.GetFileNameWithoutExtension(file)
            Metadata = metadata
            Content = processedContent  // This is what gets to the view
        }
    )
    |> Array.take 2  // Just test first 2 books

loadBooks() |> ignore