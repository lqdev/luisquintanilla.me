// Test MarkdownService directly
#r "nuget:Markdig"
#r "nuget:YamlDotNet"
#load "Domain.fs"
#load "CustomBlocks.fs"
#load "Services/Markdown.fs"

open MarkdownService

let testContent = """:::media
- url: "/test.jpg"
  alt: "test"
  mediaType: "image"
  caption: "test"
:::media"""

printfn "Testing MarkdownService.convertMdToHtml..."
printfn "Input: %s" testContent
printfn ""

let result = convertMdToHtml testContent
printfn "Output: %s" result
