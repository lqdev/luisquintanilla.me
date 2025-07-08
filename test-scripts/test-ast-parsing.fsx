// Test script for ASTParsing module validation
// Run with: dotnet fsi test-ast-parsing.fsx

#r "../bin/Debug/net9.0/PersonalSite.dll"
#r "nuget: Markdig, 0.38.0"

open System
open ASTParsing
open Domain

// Test data - simple snippet content
let testSnippetContent = """---
title: "Test QR Code Generator"
language: "F#"
tags: dotnet,f#,script
---

## Description

Script to generate a QR Code and save as PNG image from a URL

## Usage

```bash
dotnet fsi qr-code-generator.fsx "my-qr-code.svg" "https://twitter.com/user-profile"
```

This is a test snippet with **markdown** formatting.
"""

printfn "=== ASTParsing Module Validation Test ==="
printfn ""

// Test 1: Parse snippet content using new ASTParsing module
printfn "Test 1: Parsing snippet content with ASTParsing.parseSnippet"
match parseSnippet testSnippetContent with
| Ok parsedDoc ->
    printfn "✅ Parsing successful!"
    printfn "Metadata:"
    match parsedDoc.Metadata with
    | Some meta ->
        printfn "  Title: %s" meta.Title
        printfn "  Language: %s" meta.Language
        printfn "  Tags: %s" meta.Tags
    | None ->
        printfn "  No metadata found"
    
    printfn ""
    printfn "Text Content (first 100 chars):"
    let preview = if parsedDoc.TextContent.Length > 100 then 
                     parsedDoc.TextContent.Substring(0, 100) + "..."
                  else 
                     parsedDoc.TextContent
    printfn "  %s" preview
    
    printfn ""
    printfn "Custom Blocks: %d" (Map.count parsedDoc.CustomBlocks)
    printfn "Raw Markdown Length: %d characters" parsedDoc.RawMarkdown.Length
    printfn "AST Type: %s" (parsedDoc.MarkdownAst.GetType().Name)
    
| Error parseError ->
    printfn "❌ Parsing failed: %A" parseError

printfn ""
printfn "=== Test 2: Parse from existing file ==="

// Test 2: Parse actual snippet file
let snippetPath = "_src/snippets/qr-code-generator.md"
printfn "Parsing file: %s" snippetPath

match parseSnippetFromFile snippetPath with
| Ok parsedDoc ->
    printfn "✅ File parsing successful!"
    printfn "Metadata:"
    match parsedDoc.Metadata with
    | Some meta ->
        printfn "  Title: %s" meta.Title
        printfn "  Language: %s" meta.Language
        printfn "  Tags: %s" meta.Tags
    | None ->
        printfn "  No metadata found"
    
    printfn ""
    printfn "Content preview: %s" (if parsedDoc.TextContent.Length > 200 then 
                                        parsedDoc.TextContent.Substring(0, 200) + "..."
                                   else 
                                        parsedDoc.TextContent)
    
| Error parseError ->
    printfn "❌ File parsing failed: %A" parseError

printfn ""
printfn "=== Test 3: Error Handling ==="

// Test 3: Test error handling with invalid content
let invalidContent = """---
title: "Test
invalid yaml
---
Content here"""

printfn "Testing invalid YAML front matter..."
match parseSnippet invalidContent with
| Ok _ ->
    printfn "❌ Should have failed with invalid YAML"
| Error parseError ->
    printfn "✅ Correctly caught error: %A" parseError

printfn ""
printfn "=== ASTParsing Module Validation Complete ==="
