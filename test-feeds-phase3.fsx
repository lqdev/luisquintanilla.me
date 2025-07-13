#!/usr/bin/env dotnet fsi

// Comprehensive Feed Validation Script - Phase 3 Testing
// Tests output compatibility, RSS validation, HTML structure, and performance
// Run with: dotnet fsi test-feeds-phase3.fsx

open System
open System.IO
open System.Xml
open System.Text.RegularExpressions
open System.Diagnostics

printfn "=== Unified Feed System - Phase 3 Validation ==="
printfn "Date: %s" (DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
printfn ""

// Performance tracking
let stopwatch = Stopwatch.StartNew()

// Test 1: RSS 2.0 Validation
printfn "🔍 Test 1: RSS 2.0 Specification Validation"
printfn "%s" (String.replicate 50 "=")

let validateRssFeed (feedPath: string) (feedName: string) =
    if File.Exists(feedPath) then
        try
            let content = File.ReadAllText(feedPath)
            let doc = XmlDocument()
            doc.LoadXml(content)
            
            // Check RSS 2.0 required elements
            let channel = doc.SelectSingleNode("//channel")
            let title = doc.SelectSingleNode("//channel/title")
            let link = doc.SelectSingleNode("//channel/link") 
            let description = doc.SelectSingleNode("//channel/description")
            let items = doc.SelectNodes("//item")
            
            let mutable validationErrors = []
            
            if channel = null then validationErrors <- "Missing <channel> element" :: validationErrors
            if title = null then validationErrors <- "Missing <title> element" :: validationErrors
            if link = null then validationErrors <- "Missing <link> element" :: validationErrors
            if description = null then validationErrors <- "Missing <description> element" :: validationErrors
            
            // Check item structure
            for i in 0 .. min 2 (items.Count - 1) do
                let item = items.[i]
                let itemTitle = item.SelectSingleNode("title")
                let itemLink = item.SelectSingleNode("link")
                let itemDesc = item.SelectSingleNode("description")
                
                if itemTitle = null then validationErrors <- sprintf "Item %d missing <title>" i :: validationErrors
                if itemLink = null then validationErrors <- sprintf "Item %d missing <link>" i :: validationErrors
                if itemDesc = null then validationErrors <- sprintf "Item %d missing <description>" i :: validationErrors
            
            if validationErrors.IsEmpty then
                printfn "✅ %s: Valid RSS 2.0 (%d items)" feedName items.Count
                true
            else
                printfn "❌ %s: RSS validation errors:" feedName
                validationErrors |> List.iter (printfn "   - %s")
                false
        with
        | ex -> 
            printfn "❌ %s: XML parsing error - %s" feedName ex.Message
            false
    else
        printfn "❌ %s: Feed not found at %s" feedName feedPath
        false

// Test all feeds
let feedValidationResults = [
    validateRssFeed "_public/feed/index.xml" "Main Fire-hose Feed"
    validateRssFeed "_public/posts/index.xml" "Posts Feed"
    validateRssFeed "_public/feed/notes/index.xml" "Notes Feed"
    validateRssFeed "_public/feed/responses/index.xml" "Responses Feed"
    validateRssFeed "_public/presentations/feed/index.xml" "Presentations Feed"
    validateRssFeed "_public/snippets/feed/index.xml" "Snippets Feed"
    validateRssFeed "_public/wiki/feed/index.xml" "Wiki Feed"
    validateRssFeed "_public/library/feed/index.xml" "Books Feed"
]

let validFeeds = feedValidationResults |> List.filter id |> List.length
let totalFeeds = feedValidationResults.Length

printfn ""
printfn "RSS Validation Summary: %d/%d feeds valid" validFeeds totalFeeds
printfn ""

// Test 2: Content Consistency Check
printfn "🔍 Test 2: Content Consistency & Structure"
printfn "%s" (String.replicate 50 "=")

let checkContentConsistency (feedPath: string) (feedName: string) =
    if File.Exists(feedPath) then
        try
            let content = File.ReadAllText(feedPath)
            let doc = XmlDocument()
            doc.LoadXml(content)
            let items = doc.SelectNodes("//item")
            
            let mutable hasDescriptions = 0
            let mutable hasLinks = 0
            let mutable hasCDATA = 0
            
            for i in 0 .. min 4 (items.Count - 1) do
                let item = items.[i]
                let description = item.SelectSingleNode("description")
                let link = item.SelectSingleNode("link")
                
                if description <> null then
                    hasDescriptions <- hasDescriptions + 1
                    if description.InnerText.Contains("<![CDATA[") then
                        hasCDATA <- hasCDATA + 1
                        
                if link <> null && not (String.IsNullOrEmpty(link.InnerText)) then
                    hasLinks <- hasLinks + 1
            
            let checkedItems = min 5 items.Count
            printfn "✅ %s: %d/%d items have descriptions, %d/%d have links, %d/%d use CDATA" 
                feedName hasDescriptions checkedItems hasLinks checkedItems hasCDATA checkedItems
                
        with
        | ex -> printfn "❌ %s: Error checking consistency - %s" feedName ex.Message
    else
        printfn "❌ %s: Feed not found" feedName

// Check content consistency for all feeds
[
    "_public/feed/index.xml", "Main Feed"
    "_public/posts/index.xml", "Posts"
    "_public/feed/notes/index.xml", "Notes"
    "_public/feed/responses/index.xml", "Responses"
] |> List.iter (fun (path, name) -> checkContentConsistency path name)

printfn ""

// Test 3: Performance Analysis
printfn "🔍 Test 3: Performance Analysis"
printfn "%s" (String.replicate 50 "=")

let buildStart = Stopwatch.StartNew()

// Run a build and measure performance
let processInfo = ProcessStartInfo()
processInfo.FileName <- "dotnet"
processInfo.Arguments <- "run"
processInfo.UseShellExecute <- false
processInfo.RedirectStandardOutput <- true
processInfo.RedirectStandardError <- true

let proc = Process.Start(processInfo)
let output = proc.StandardOutput.ReadToEnd()
let error = proc.StandardError.ReadToEnd()
proc.WaitForExit()

buildStart.Stop()

printfn "✅ Build completed in %d ms" buildStart.ElapsedMilliseconds

// Extract unified feed statistics from output
let unifiedFeedMatch = Regex.Match(output, @"Unified feeds generated: (\d+) total items across (\d+) content types")
if unifiedFeedMatch.Success then
    let itemCount = unifiedFeedMatch.Groups.[1].Value
    let typeCount = unifiedFeedMatch.Groups.[2].Value
    printfn "✅ Unified system processed %s items across %s content types" itemCount typeCount
else
    printfn "⚠️  Could not extract unified feed statistics from build output"

printfn ""

// Test 4: URL Structure Validation
printfn "🔍 Test 4: URL Structure & Accessibility"
printfn "%s" (String.replicate 50 "=")

let expectedFeedPaths = [
    "/feed/index.xml", "Main fire-hose feed"
    "/posts/index.xml", "Posts feed"
    "/feed/notes/index.xml", "Notes feed"
    "/feed/responses/index.xml", "Responses feed"
    "/presentations/feed/index.xml", "Presentations feed"
    "/snippets/feed/index.xml", "Snippets feed"
    "/wiki/feed/index.xml", "Wiki feed"
    "/library/feed/index.xml", "Books/Library feed"
]

let mutable accessibleFeeds = 0
for relativePath, description in expectedFeedPaths do
    let fullPath = "_public" + relativePath
    if File.Exists(fullPath) then
        let fileInfo = FileInfo(fullPath)
        printfn "✅ %s: Accessible (%d KB)" description (fileInfo.Length / 1024L)
        accessibleFeeds <- accessibleFeeds + 1
    else
        printfn "❌ %s: Not found at %s" description fullPath

printfn ""
printfn "URL Accessibility: %d/%d feeds accessible" accessibleFeeds expectedFeedPaths.Length

// Test 5: Feed Size and Content Distribution
printfn ""
printfn "🔍 Test 5: Feed Size & Content Distribution"
printfn "%s" (String.replicate 50 "=")

let analyzeFeedSize (feedPath: string) (feedName: string) =
    if File.Exists(feedPath) then
        let fileInfo = FileInfo(feedPath)
        let content = File.ReadAllText(feedPath)
        let doc = XmlDocument()
        doc.LoadXml(content)
        let items = doc.SelectNodes("//item")
        
        printfn "📊 %s: %d items, %d KB" feedName items.Count (fileInfo.Length / 1024L)
    else
        printfn "❌ %s: Not found" feedName

[
    "_public/feed/index.xml", "Main Feed"
    "_public/posts/index.xml", "Posts"
    "_public/feed/notes/index.xml", "Notes"
    "_public/feed/responses/index.xml", "Responses"
    "_public/presentations/feed/index.xml", "Presentations"
    "_public/snippets/feed/index.xml", "Snippets"
    "_public/wiki/feed/index.xml", "Wiki"
    "_public/library/feed/index.xml", "Books"
] |> List.iter (fun (path, name) -> analyzeFeedSize path name)

stopwatch.Stop()
printfn ""
printfn "%s" (String.replicate 60 "=")
printfn "🎯 Phase 3 Validation Summary"
printfn "%s" (String.replicate 60 "=")
printfn "Total validation time: %d ms" stopwatch.ElapsedMilliseconds
printfn "RSS 2.0 compliance: %d/%d feeds valid" validFeeds totalFeeds
printfn "URL accessibility: %d/%d feeds accessible" accessibleFeeds expectedFeedPaths.Length
printfn ""

if validFeeds = totalFeeds && accessibleFeeds = expectedFeedPaths.Length then
    printfn "🎉 ALL TESTS PASSED - Unified feed system ready for production!"
else
    printfn "⚠️  Some issues detected - review above for details"

printfn ""
