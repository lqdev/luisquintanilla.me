#!/usr/bin/env dotnet fsi

// Feed Compatibility Testing Script - Phase 3
// Compare unified system output with legacy system
// Run with: dotnet fsi test-feed-compatibility.fsx

open System
open System.IO
open System.Xml

printfn "=== Feed Compatibility Testing ==="
printfn "Comparing unified system output with legacy feeds"
printfn ""

let compareFeedStructure (currentPath: string) (oldPath: string) (feedName: string) =
    printfn "🔍 Testing %s compatibility..." feedName
    
    if File.Exists(currentPath) && File.Exists(oldPath) then
        try
            // Parse both feeds
            let currentDoc = XmlDocument()
            currentDoc.LoadXml(File.ReadAllText(currentPath))
            
            let oldDoc = XmlDocument()
            oldDoc.LoadXml(File.ReadAllText(oldPath))
            
            // Compare structure
            let currentItems = currentDoc.SelectNodes("//item")
            let oldItems = oldDoc.SelectNodes("//item")
            
            let currentTitleNode = currentDoc.SelectSingleNode("//channel/title")
            let oldTitleNode = oldDoc.SelectSingleNode("//channel/title")
            let currentTitle = if currentTitleNode <> null then currentTitleNode.InnerText else ""
            let oldTitle = if oldTitleNode <> null then oldTitleNode.InnerText else ""
            
            let currentLinkNode = currentDoc.SelectSingleNode("//channel/link")
            let oldLinkNode = oldDoc.SelectSingleNode("//channel/link")
            let currentLink = if currentLinkNode <> null then currentLinkNode.InnerText else ""
            let oldLink = if oldLinkNode <> null then oldLinkNode.InnerText else ""
            
            // Check metadata compatibility
            let titleMatch = (currentTitle = oldTitle)
            let linkMatch = (currentLink = oldLink)
            
            printfn "   📊 Items: Current=%d, Legacy=%d" currentItems.Count oldItems.Count
            printfn "   📝 Title: %s %s" (if titleMatch then "✅" else "⚠️") (currentTitle |> Option.ofObj |> Option.defaultValue "N/A")
            printfn "   🔗 Link: %s %s" (if linkMatch then "✅" else "⚠️") (currentLink |> Option.ofObj |> Option.defaultValue "N/A")
            
            // Sample first few items for structure comparison
            let sampleSize = min 3 (min currentItems.Count oldItems.Count)
            let mutable structuralMatches = 0
            
            for i in 0 .. sampleSize - 1 do
                let currentItem = currentItems.[i]
                let oldItem = oldItems.[i]
                
                let currentItemTitleNode = currentItem.SelectSingleNode("title")
                let oldItemTitleNode = oldItem.SelectSingleNode("title")
                let currentItemTitle = if currentItemTitleNode <> null then currentItemTitleNode.InnerText else ""
                let oldItemTitle = if oldItemTitleNode <> null then oldItemTitleNode.InnerText else ""
                
                let currentItemLinkNode = currentItem.SelectSingleNode("link")
                let oldItemLinkNode = oldItem.SelectSingleNode("link")
                let currentItemLink = if currentItemLinkNode <> null then currentItemLinkNode.InnerText else ""
                let oldItemLink = if oldItemLinkNode <> null then oldItemLinkNode.InnerText else ""
                
                if (currentItemTitle = oldItemTitle) && (currentItemLink = oldItemLink) then
                    structuralMatches <- structuralMatches + 1
            
            let compatibilityScore = float structuralMatches / float sampleSize * 100.0
            printfn "   📈 Structure compatibility: %.0f%% (%d/%d samples match)" compatibilityScore structuralMatches sampleSize
            
            if compatibilityScore >= 100.0 && titleMatch && linkMatch then
                printfn "   ✅ %s: FULLY COMPATIBLE" feedName
                true
            else
                printfn "   ⚠️  %s: PARTIAL COMPATIBILITY" feedName
                false
                
        with
        | ex -> 
            printfn "   ❌ %s: Error comparing feeds - %s" feedName ex.Message
            false
    else
        let currentExists = File.Exists(currentPath)
        let oldExists = File.Exists(oldPath)
        printfn "   ❌ %s: Missing feeds - Current:%b, Legacy:%b" feedName currentExists oldExists
        false

printfn ""

// Test main feeds
let mainFeedCompatible = compareFeedStructure "_public/feed/index.xml" "_public_old/feed/index.xml" "Main Feed"

printfn ""

// Test individual feed existence and basic structure
let testFeedExists (feedPath: string) (feedName: string) =
    if File.Exists(feedPath) then
        try
            let content = File.ReadAllText(feedPath)
            let doc = XmlDocument()
            doc.LoadXml(content)
            let items = doc.SelectNodes("//item")
            printfn "✅ %s: %d items, well-formed XML" feedName items.Count
            true
        with
        | ex ->
            printfn "❌ %s: XML validation failed - %s" feedName ex.Message
            false
    else
        printfn "❌ %s: Feed not found at %s" feedName feedPath
        false

printfn "🔍 Individual Feed Validation..."
let individualFeedResults = [
    testFeedExists "_public/posts/index.xml" "Posts Feed"
    testFeedExists "_public/feed/notes/index.xml" "Notes Feed"
    testFeedExists "_public/feed/responses/index.xml" "Responses Feed"
    testFeedExists "_public/presentations/feed/index.xml" "Presentations Feed"
    testFeedExists "_public/snippets/feed/index.xml" "Snippets Feed"
    testFeedExists "_public/wiki/feed/index.xml" "Wiki Feed"
    testFeedExists "_public/library/feed/index.xml" "Books Feed"
]

let validIndividualFeeds = individualFeedResults |> List.filter id |> List.length
let totalIndividualFeeds = individualFeedResults.Length

printfn ""
printfn "=== Compatibility Summary ==="
printfn "Main feed compatibility: %s" (if mainFeedCompatible then "✅ COMPATIBLE" else "⚠️ PARTIAL")
printfn "Individual feeds: %d/%d valid" validIndividualFeeds totalIndividualFeeds
printfn ""

// Test URL patterns
printfn "🔍 URL Pattern Validation..."
let expectedUrls = [
    "/feed/index.xml"
    "/posts/index.xml"
    "/feed/notes/index.xml"
    "/feed/responses/index.xml"
    "/presentations/feed/index.xml"
    "/snippets/feed/index.xml"
    "/wiki/feed/index.xml"
    "/library/feed/index.xml"
]

let mutable urlPatternScore = 0
for url in expectedUrls do
    let localPath = "_public" + url
    if File.Exists(localPath) then
        urlPatternScore <- urlPatternScore + 1
        printfn "   ✅ %s" url
    else
        printfn "   ❌ %s" url

let urlCompatibility = float urlPatternScore / float expectedUrls.Length * 100.0
printfn ""
printfn "URL pattern compatibility: %.0f%% (%d/%d)" urlCompatibility urlPatternScore expectedUrls.Length

printfn ""
printfn "=== Final Compatibility Assessment ==="
if mainFeedCompatible && validIndividualFeeds = totalIndividualFeeds && urlCompatibility = 100.0 then
    printfn "🎉 UNIFIED SYSTEM FULLY COMPATIBLE WITH LEGACY SYSTEM!"
    printfn "✅ Ready for production deployment"
else
    printfn "⚠️  Some compatibility issues detected:"
    if not mainFeedCompatible then printfn "   - Main feed structure differences"
    if validIndividualFeeds <> totalIndividualFeeds then printfn "   - Individual feed issues"
    if urlCompatibility <> 100.0 then printfn "   - URL pattern mismatches"

printfn ""
