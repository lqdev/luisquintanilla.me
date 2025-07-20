// Test custom blocks processing for media
#r "nuget:YamlDotNet"
#r "nuget:Markdig"
#load "../Domain.fs"
#load "../CustomBlocks.fs"
#load "../Services/Markdown.fs"

open System
open MarkdownService

printfn "=== Testing Media Block Processing ==="

let testContent = """:::media
- url: "/assets/images/fall-mountains/sunrise.jpg"
  alt: "Sunrise at mountain summit with mist and fall foliage"
  mediaType: "image"
  aspectRatio: "landscape"
  caption: "Sunrise"
- url: "/assets/images/fall-mountains/sunset.jpg"
  alt: "Sunset at lake with mountains in the background"
  mediaType: "image"
  aspectRatio: "landscape"
  caption: "Sunset"
:::media"""

printfn "Original content:"
printfn "%s" testContent
printfn ""

let convertedHtml = convertMdToHtml testContent

printfn "Converted HTML:"
printfn "%s" convertedHtml
printfn ""

printfn "=== Testing Complete ==="
