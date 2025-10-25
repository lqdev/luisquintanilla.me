#!/usr/bin/env dotnet fsi

#r "nuget: Markdig, 0.37.0"
#r "nuget: YamlDotNet, 16.1.3"

open System
open System.IO
open Markdig
open Markdig.Parsers

// Load the resume content
let resumePath = "_src/resume/resume.md"
let content = File.ReadAllText(resumePath)

// Split frontmatter from content
let lines = content.Split('\n')
let mutable inFrontmatter = false
let mutable frontmatterEnd = 0

for i in 0 .. lines.Length - 1 do
    if lines.[i].Trim() = "---" then
        if not inFrontmatter then
            inFrontmatter <- true
        else
            frontmatterEnd <- i
            inFrontmatter <- false

let markdownContent = String.Join("\n", lines |> Array.skip (frontmatterEnd + 1))

printfn "Markdown content (first 500 chars):"
printfn "%s" (markdownContent.Substring(0, min 500 markdownContent.Length))
printfn "\n---\n"

// Parse the markdown
let pipeline = 
    MarkdownPipelineBuilder()
        .UseYamlFrontMatter()
        .UsePipeTables()
        .UseTaskLists()
        .UseBootstrap()
        .Build()

let doc = Markdown.Parse(markdownContent, pipeline)

printfn "Parsed document type: %s" (doc.GetType().Name)
printfn "Number of blocks: %d" (doc |> Seq.length)

// Find headings
let headings = 
    Markdig.Syntax.MarkdownObjectExtensions.Descendants<Markdig.Syntax.HeadingBlock>(doc)
    |> Seq.toList

printfn "\nHeadings found: %d" headings.Length
for h in headings do
    let text = if h.Inline <> null then h.Inline.ToString() else "[No content]"
    printfn "  Level %d: %s" h.Level text

// Try to extract About section
printfn "\n--- Extracting 'About' section ---"

let aboutHeading = 
    headings 
    |> List.tryFind (fun h -> 
        let inlineContent = h.Inline
        if inlineContent <> null then
            let text = inlineContent.ToString()
            printfn "  Comparing heading '%s' with 'About'" text
            text.Trim().Equals("About", StringComparison.OrdinalIgnoreCase)
        else
            false)

match aboutHeading with
| None -> printfn "About heading NOT found"
| Some heading -> 
    printfn "About heading found at level %d" heading.Level
    
    // Get all blocks
    let allBlocks = doc |> Seq.toList
    let headingIndex = allBlocks |> List.findIndex (fun b -> Object.ReferenceEquals(b, heading))
    printfn "Heading is at index %d of %d total blocks" headingIndex allBlocks.Length
    
    // Get blocks until next heading
    let contentBlocks = 
        allBlocks
        |> List.skip (headingIndex + 1)
        |> List.takeWhile (fun block ->
            match block with
            | :? Markdig.Syntax.HeadingBlock as h -> h.Level > heading.Level
            | _ -> true)
    
    printfn "Found %d content blocks after About heading" contentBlocks.Length
    
    if not contentBlocks.IsEmpty then
        use writer = new System.IO.StringWriter()
        let renderer = Markdig.Renderers.HtmlRenderer(writer)
        for block in contentBlocks do
            printfn "  Block type: %s" (block.GetType().Name)
            renderer.Write(block)
        let html = writer.ToString().Trim()
        printfn "\nRendered HTML length: %d" html.Length
        printfn "HTML content (first 300 chars):"
        printfn "%s" (if html.Length > 300 then html.Substring(0, 300) else html)
