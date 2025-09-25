#r "nuget: Giraffe.ViewEngine"
#r "bin/Debug/net9.0/PersonalSite.dll"

open Domain
open ComponentViews
open LayoutViews
open Giraffe.ViewEngine

// Test the postTagsSection function
let testTags = [| "dotnet"; "fsharp"; "web-development" |]
let tagsSection = postTagsSection testTags

// Test the blogPostView function with tags
let testTitle = "Test Blog Post"
let testContent = "<p>This is a test blog post content.</p>"
let testDate = "2024-01-15 10:30 -05:00"
let testFileName = "test-blog-post.md"

let blogPostWithTags = blogPostView testTitle testContent testDate testFileName testTags

// Render the HTML to see the output
let htmlOutput = RenderView.AsString.xmlNode blogPostWithTags

printfn "=== Tags Section Output ==="
printfn "%s" (RenderView.AsString.xmlNode tagsSection)
printfn ""

printfn "=== Blog Post with Tags (Footer portion) ==="
// Extract just the footer part for readability
let footerStartIndex = htmlOutput.IndexOf("<footer")
if footerStartIndex >= 0 then
    let footerPart = htmlOutput.Substring(footerStartIndex)
    let footerEndIndex = footerPart.IndexOf("</footer>") + 9
    printfn "%s" (footerPart.Substring(0, footerEndIndex))
else
    printfn "Full output:"
    printfn "%s" htmlOutput