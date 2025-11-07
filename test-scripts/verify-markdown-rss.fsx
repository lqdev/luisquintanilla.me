#!/usr/bin/env dotnet fsi

open System
open System.IO
open System.Xml.Linq

let feedPath = "_public/feed/feed.xml"

if not (File.Exists feedPath) then
    printfn "❌ Feed not found at %s" feedPath
    exit 1

let doc = XDocument.Load(feedPath)
let sourceNs = XNamespace.Get("http://source.scripting.com/")

// Check namespace declaration
let rssElement = doc.Root
let hasNamespace = 
    rssElement.Attributes()
    |> Seq.exists (fun attr -> 
        attr.Name.LocalName = "source" && 
        attr.Value = "http://source.scripting.com/")

printfn "Namespace declared: %b" hasNamespace

// Count items with source:markdown
let items = doc.Descendants(XName.Get("item")) |> Seq.toList
let itemsWithMarkdown = 
    items
    |> Seq.filter (fun item -> 
        item.Element(sourceNs + "markdown") <> null)
    |> Seq.length

printfn "Total items: %d" (List.length items)
printfn "Items with source:markdown: %d" itemsWithMarkdown
printfn "Coverage: %.1f%%" (float itemsWithMarkdown / float items.Length * 100.0)

// Sample first item
match items with
| firstItem :: _ ->
    let title = firstItem.Element(XName.Get("title")).Value
    let hasMarkdown = firstItem.Element(sourceNs + "markdown") <> null
    let markdownLength = 
        if hasMarkdown then
            firstItem.Element(sourceNs + "markdown").Value.Length
        else 0
    
    printfn "\nFirst item:"
    printfn "  Title: %s" title
    printfn "  Has Markdown: %b" hasMarkdown
    printfn "  Markdown length: %d chars" markdownLength
| [] ->
    printfn "No items in feed"

// Check additional feeds
let feedsToCheck = [
    "_public/posts/feed.xml"
    "_public/notes/feed.xml"
    "_public/responses/feed.xml"
    "_public/bookmarks/feed.xml"
]

printfn "\n✅ Checking additional feeds..."
for feedPath in feedsToCheck do
    if File.Exists feedPath then
        let feedDoc = XDocument.Load(feedPath)
        let feedItems = feedDoc.Descendants(XName.Get("item")) |> Seq.toList
        let feedItemsWithMarkdown = 
            feedItems
            |> Seq.filter (fun item -> item.Element(sourceNs + "markdown") <> null)
            |> Seq.length
        let feedName = Path.GetFileName(Path.GetDirectoryName(feedPath))
        printfn "  %s: %d/%d items with source:markdown (%.1f%%)" 
            feedName 
            feedItemsWithMarkdown 
            (List.length feedItems)
            (if feedItems.Length > 0 then float feedItemsWithMarkdown / float feedItems.Length * 100.0 else 0.0)

if itemsWithMarkdown = items.Length then
    printfn "\n✓✓✓ All items have Markdown source! ✓✓✓"
    exit 0
else
    printfn "\n✗ Some items missing Markdown source"
    exit 1
