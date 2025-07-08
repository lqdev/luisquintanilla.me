#r "nuget: Markdig"
#r "nuget: YamlDotNet" 
#load "../Domain.fs"
#load "../CustomBlocks.fs"

open System
open System.IO
open Markdig
open Markdig.Syntax
open CustomBlocks

// Test different pipeline configurations

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

printfn "=== Testing Pipeline Configurations ==="
printfn ""

// Test 1: Pipeline WITHOUT UseCustomContainers
printfn "1. Testing pipeline WITHOUT UseCustomContainers..."
let pipeline1 = 
    MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        |> useCustomBlocks
        |> fun builder -> builder.Build()

let doc1 = Markdown.Parse(testContent, pipeline1)
printfn "Blocks found:"
for i, block in doc1 |> Seq.mapi (fun i b -> i, b) do
    printfn "  %d: %s" i (block.GetType().Name)

printfn ""

// Test 2: Pipeline WITH UseCustomContainers  
printfn "2. Testing pipeline WITH UseCustomContainers..."
let pipeline2 = 
    MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseCustomContainers()
        |> useCustomBlocks
        |> fun builder -> builder.Build()

let doc2 = Markdown.Parse(testContent, pipeline2)
printfn "Blocks found:"
for i, block in doc2 |> Seq.mapi (fun i b -> i, b) do
    printfn "  %d: %s" i (block.GetType().Name)

printfn ""

// Test 3: Only custom blocks, no other extensions
printfn "3. Testing ONLY custom blocks..."
let pipeline3 = 
    MarkdownPipelineBuilder()
        |> useCustomBlocks
        |> fun builder -> builder.Build()

let doc3 = Markdown.Parse(testContent, pipeline3)
printfn "Blocks found:"
for i, block in doc3 |> Seq.mapi (fun i b -> i, b) do
    printfn "  %d: %s" i (block.GetType().Name)

printfn ""
printfn "=== Pipeline Configuration Test Complete ==="
