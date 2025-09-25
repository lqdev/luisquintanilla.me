#r "nuget: Giraffe.ViewEngine"
#r "bin/Debug/net9.0/PersonalSite.dll"

open Domain
open ComponentViews
open LayoutViews
open Giraffe.ViewEngine

// Test with empty tags array
let emptyTags = [||]
let emptyTagsSection = postTagsSection emptyTags

printfn "=== Empty Tags Section Output ==="
printfn "%s" (RenderView.AsString.xmlNode emptyTagsSection)
printfn ""

// Test with single tag
let singleTag = [| "machine-learning" |]
let singleTagSection = postTagsSection singleTag

printfn "=== Single Tag Section Output ==="
printfn "%s" (RenderView.AsString.xmlNode singleTagSection)