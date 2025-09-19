#!/usr/bin/env dotnet fsi

#r "bin/Debug/net9.0/PersonalSite.dll"

open System
open System.IO
open MarkdownService

// Read the actual Four Agreements file
let filePath = "_src/reviews/library/four-agreements-ruiz.md"
let fileContent = File.ReadAllText(filePath)

printfn "Reading file: %s" filePath
printfn "\nFile content preview (first 500 chars):"
printfn "%s" (fileContent.Substring(0, min 500 fileContent.Length))

// Convert to HTML using the same pipeline as the builder
let htmlResult = convertMdToHtml fileContent

printfn "\nHTML output preview (first 1000 chars):"
printfn "%s" (htmlResult.Substring(0, min 1000 htmlResult.Length))

// Check if it contains custom review block HTML
if htmlResult.Contains("custom-review-block") then
    printfn "\n✅ Custom review block rendered successfully!"
else
    printfn "\n❌ Custom review block NOT rendered - showing as plain text"
    
if htmlResult.Contains("item: \"The Four Agreements\"") then
    printfn "❌ Raw YAML content found in output"
else
    printfn "✅ Raw YAML content processed correctly"