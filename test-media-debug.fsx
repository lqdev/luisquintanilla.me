#r "nuget: Markdig"
#r "nuget: YamlDotNet"
#load "CustomBlocks.fs"

open System
open Markdig
open Markdig.Extensions.CustomContainers
open CustomBlocks

// Test the actual parsing pipeline with debug output
let testMarkdown = """
# Test Page

Some content before.

:::media
- media_type: image
  uri: /images/fall-mountains/sunrise.jpg
  alt_text: "Sunrise over mountains in fall"
  caption: "Beautiful fall morning sunrise"
  aspect: landscape
- media_type: image  
  uri: /images/fall-mountains/valley.jpg
  alt_text: "Valley view in fall colors"
  caption: "Fall colors in the valley"
  aspect: landscape
:::

Some content after.
"""

// Create pipeline with custom blocks
let pipeline = 
    MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Use<CustomBlockExtension>()
        .Build()

// Parse the markdown
let document = Markdown.Parse(testMarkdown, pipeline)

printfn "Document parsed. Block count: %d" document.Count
document |> Seq.iteri (fun i block ->
    printfn "Block %d: %s" i (block.GetType().Name)
    match block with
    | :? Markdig.Syntax.FencedCodeBlock as fcb ->
        printfn "  Fenced code block - Info: %s" fcb.Info
    | :? Markdig.Syntax.ParagraphBlock as p ->
        printfn "  Paragraph content: %s" (p.Inline.ToString())
    | :? Markdig.Extensions.CustomContainers.CustomContainer as cc ->
        printfn "  CustomContainer - Info: %s, Arguments: %s" cc.Info cc.Arguments
        printfn "  CustomContainer has %d child blocks" cc.Count
        cc |> Seq.iteri (fun j child ->
            printfn "    Child %d: %s" j (child.GetType().Name))
    | _ -> ()
)

// Find our MediaBlock and examine its content
let rec findMediaBlocks (blocks: seq<Markdig.Syntax.Block>) =
    blocks |> Seq.iter (fun block ->
        match block with
        | :? MediaBlock as mediaBlock ->
            printfn "Found MediaBlock:"
            printfn "RawContent length: %d" (if mediaBlock.RawContent = null then 0 else mediaBlock.RawContent.Length)
            printfn "RawContent: [%s]" (if mediaBlock.RawContent = null then "NULL" else mediaBlock.RawContent)
            printfn "MediaItems count: %d" (mediaBlock.MediaItems |> List.length)
            if mediaBlock.MediaItems |> List.isEmpty |> not then
                mediaBlock.MediaItems |> List.iteri (fun i item ->
                    printfn "  Item %d: %s -> %s" i item.media_type item.uri)
        | :? Markdig.Syntax.ContainerBlock as container ->
            findMediaBlocks container
        | _ -> ()
    )

findMediaBlocks document

printfn "Done."
