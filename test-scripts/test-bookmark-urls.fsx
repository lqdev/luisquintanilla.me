#!/usr/bin/env dotnet fsi

#r "../bin/Debug/net9.0/PersonalSite.dll"

open System
open System.IO
open System.Xml.Linq
open GenericBuilder
open Domain

// Test the Response processor with a bookmark response
let testBookmarkRssGeneration() =
    printfn "Testing bookmark RSS URL generation..."
    
    // Create a test bookmark response object
    let testResponse : Response = {
        FileName = "real-simple-licensing-open-standard"
        Metadata = {
            Title = "Real Simple Licensing"
            TargetUrl = "https://rslstandard.org/"
            ResponseType = "bookmark"
            DatePublished = "2025-09-12 20:48 -05:00"
            DateUpdated = "2025-09-12 20:48 -05:00"
            Tags = [| "rsl"; "ai"; "licensing"; "openstandard" |]
        }
        Content = "> The open content licensing standard for the AI-first Internet"
    }
    
    // Test the RSS generation
    let processor = ResponseProcessor.create()
    match processor.RenderRss testResponse with
    | Some rssItem ->
        let linkElement = rssItem.Element(XName.Get "link")
        if linkElement <> null then
            let url = linkElement.Value
            printfn "Generated RSS URL: %s" url
            
            if url.Contains("/bookmarks/") then
                printfn "✅ SUCCESS: Bookmark RSS URL contains '/bookmarks/' as expected"
                true
            else
                printfn "❌ FAILURE: Bookmark RSS URL contains '%s' instead of '/bookmarks/'" url
                false
        else
            printfn "❌ FAILURE: No link element found in RSS item"
            false
    | None ->
        printfn "❌ FAILURE: RSS generation returned None"
        false

// Test the Response processor with a non-bookmark response
let testNonBookmarkRssGeneration() =
    printfn "\nTesting non-bookmark RSS URL generation..."
    
    // Create a test star response object
    let testResponse : Response = {
        FileName = "test-star-response"
        Metadata = {
            Title = "Test Star Response"
            TargetUrl = "https://example.com/"
            ResponseType = "star"
            DatePublished = "2025-09-12 20:48 -05:00"
            DateUpdated = "2025-09-12 20:48 -05:00"
            Tags = [| "test" |]
        }
        Content = "Great article!"
    }
    
    // Test the RSS generation
    let processor = ResponseProcessor.create()
    match processor.RenderRss testResponse with
    | Some rssItem ->
        let linkElement = rssItem.Element(XName.Get "link")
        if linkElement <> null then
            let url = linkElement.Value
            printfn "Generated RSS URL: %s" url
            
            if url.Contains("/responses/") then
                printfn "✅ SUCCESS: Non-bookmark RSS URL contains '/responses/' as expected"
                true
            else
                printfn "❌ FAILURE: Non-bookmark RSS URL contains '%s' instead of '/responses/'" url
                false
        else
            printfn "❌ FAILURE: No link element found in RSS item"
            false
    | None ->
        printfn "❌ FAILURE: RSS generation returned None"
        false

// Run the tests
let bookmarkTest = testBookmarkRssGeneration()
let nonBookmarkTest = testNonBookmarkRssGeneration()

if bookmarkTest && nonBookmarkTest then
    printfn "\n🎉 All tests passed! Bookmark URLs are correctly generated."
    exit 0
else
    printfn "\n❌ Some tests failed."
    exit 1