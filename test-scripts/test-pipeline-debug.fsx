#r "nuget: Markdig"
#r "nuget: YamlDotNet" 
#load "../Domain.fs"
#load "../CustomBlocks.fs"

open System
open System.IO
open Markdig
open Markdig.Syntax
open CustomBlocks

// Test if Markdig pipeline correctly parses custom blocks

let testContent = """---
title: "Test"
tags: ["test"]
date: "2025-07-08"
---

# Test Content

:::review
title: "Test Review"
item: "Test Item"
rating: 4.5
scale: 5.0
:::

Regular content here.
"""

printfn "=== Testing Custom Block Parsing in Pipeline ==="
printfn ""

// Test 1: Create pipeline with custom blocks
printfn "1. Creating Markdig pipeline with custom blocks..."
let pipeline = 
    MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        |> useCustomBlocks
        |> fun builder -> builder.Build()

printfn "Pipeline created successfully"
printfn ""

// Test 2: Parse document
printfn "2. Parsing test document..."
let doc = Markdown.Parse(testContent, pipeline)

printfn "Document parsed successfully"
printfn "Total blocks in document: %d" (doc.Count)
printfn ""

// Test 3: Examine all blocks
printfn "3. Examining all blocks in document..."
let mutable blockIndex = 0
for block in doc do
    blockIndex <- blockIndex + 1
    let blockType = block.GetType().Name
    printfn "  Block %d: %s" blockIndex blockType
    
    // Check if it's a custom block
    match block with
    | :? MediaBlock as mediaBlock ->
        printfn "    → Media Block found! Content: %s" (mediaBlock.RawContent.Substring(0, min 50 mediaBlock.RawContent.Length))
    | :? ReviewBlock as reviewBlock ->
        printfn "    → Review Block found! Content: %s" (reviewBlock.RawContent.Substring(0, min 50 reviewBlock.RawContent.Length))
    | :? VenueBlock as venueBlock ->
        printfn "    → Venue Block found! Content: %s" (venueBlock.RawContent.Substring(0, min 50 venueBlock.RawContent.Length))
    | :? RsvpBlock as rsvpBlock ->
        printfn "    → RSVP Block found! Content: %s" (rsvpBlock.RawContent.Substring(0, min 50 rsvpBlock.RawContent.Length))
    | _ ->
        printfn "    → Standard block: %s" blockType

printfn ""

// Test 4: Examine descendants
printfn "4. Examining all descendants..."
let descendants : Block list = doc.Descendants() |> Seq.cast<Block> |> Seq.toList
printfn "Total descendants: %d" descendants.Length

let mutable customBlockCount = 0
for desc in descendants do
    let blockType = desc.GetType().Name
    if blockType.Contains("Block") && not (blockType = "Block") then
        printfn "  Found block type: %s" blockType
        if blockType = "MediaBlock" || blockType = "ReviewBlock" || blockType = "VenueBlock" || blockType = "RsvpBlock" then
            customBlockCount <- customBlockCount + 1

printfn "Custom blocks found: %d" customBlockCount

printfn ""
printfn "=== Custom Block Parsing Test Complete ==="
