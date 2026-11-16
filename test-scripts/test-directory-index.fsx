#!/usr/bin/env dotnet fsi

#r "../bin/Debug/net11.0/PersonalSite.dll"

open System
open Domain

let mutable failures = 0

let check name condition =
    if condition then
        printfn "  PASS  %s" name
    else
        failures <- failures + 1
        printfn "  FAIL  %s" name

let collection
    (id: string)
    (title: string)
    (urlPath: string)
    (tags: string array) : Collection =
    {
        Id = id
        Title = title
        Description = title + " follow list"
        CollectionType = CollectionType.MediumFocused "blogs"
        UrlPath = urlPath
        DataFile = id + ".json"
        Tags = tags
        LastUpdated = "2026-01-01"
        ItemCount = None
    }

let item
    (title: string)
    (htmlUrl: string)
    (xmlUrl: string)
    (description: string option)
    (tags: string array option) : CollectionItem =
    {
        Title = title
        Type = "rss"
        HtmlUrl = htmlUrl
        XmlUrl = xmlUrl
        Description = description
        Tags = tags
        Added = None
    }

let loaded (source: Collection) (items: CollectionItem array) =
    source, { Metadata = source; Items = items }

let blogroll = collection "blogroll" "Blogroll" "/collections/blogroll" [| "Open web" |]
let forums = collection "forums" "Forums" "/collections/forums/" [| "Community"; "Open Web" |]
let excluded = collection "travel" "Travel" "/collections/travel/" [||]

let blogrollItems =
    [|
        item
            "Example Journal"
            "HTTPS://EXAMPLE.COM:443"
            "HTTPS://FEEDS.EXAMPLE.COM:443/rss"
            (Some "  A useful site.  ")
            (Some [| "Open web"; "Accessibility"; " " |])
        item
            "Podcast Feed"
            ""
            "https://podcasts.example/feed.xml"
            None
            None
    |]

let forumItems =
    [|
        item
            "Example Journal"
            "https://example.com/"
            "https://feeds.example.com:443/atom.xml"
            None
            (Some [| "Accessibility"; "Community" |])
    |]

let excludedItems =
    [|
        item "Must not be indexed" "javascript:alert(1)" "" None None
    |]

let includedCollections = [| blogroll; forums |]
let loadedCollections =
    [|
        loaded blogroll blogrollItems
        loaded forums forumItems
        loaded excluded excludedItems
    |]

printfn "=== Directory index validation ==="

let index = DirectoryIndexBuilder.buildIndex includedCollections loadedCollections
let serialized = DirectoryIndexBuilder.serialize index
let serializedAgain =
    DirectoryIndexBuilder.buildIndex includedCollections loadedCollections
    |> DirectoryIndexBuilder.serialize

check "Index includes only the selected source collections" (index.Entries.Length = 2)
check "Repeated builds serialize byte-identically" (serialized = serializedAgain)

let merged =
    index.Entries
    |> Array.find (fun entry -> entry.HomepageUrl = "https://example.com/")
let expectedFeeds =
    set [ "https://feeds.example.com/rss"; "https://feeds.example.com/atom.xml" ]
check "URL normalization removes scheme casing and default ports" (
    merged.Id = "https://example.com/"
    && merged.HomepageUrl = "https://example.com/")
check "Duplicate URLs merge and retain distinct feed URLs" (
    merged.FeedUrls.Length = 2
    && Set.ofArray merged.FeedUrls = expectedFeeds)
check "Merged entry retains both source memberships" (
    merged.Collections.Length = 2
    && (merged.Collections |> Array.map _.Id |> Set.ofArray) = set [ "blogroll"; "forums" ])
check "Source collection links include the page and OPML paths" (
    merged.Collections
    |> Array.forall (fun source ->
        source.Url.StartsWith("/collections/", StringComparison.Ordinal)
        && source.OpmlUrl = source.Url + "index.opml"))
check "Item and collection tags are trimmed and de-duplicated" (
    Set.ofArray merged.Tags = set [ "Accessibility"; "Community"; "Open web" ])
check "Description is trimmed and retained when present" (
    merged.Description = Some "A useful site.")

let fallback =
    index.Entries
    |> Array.find (fun entry -> entry.Title = "Podcast Feed")
check "Feed URL is used as homepage when the HTML URL is missing" (
    fallback.HomepageUrl = "https://podcasts.example/feed.xml"
    && fallback.FeedUrls = [| "https://podcasts.example/feed.xml" |])

let invalidCollection = collection "invalid" "Invalid" "/collections/invalid/" [||]
let invalidItem = item "Broken source" "ftp://example.com" "javascript:alert(1)" None None
let invalidBuild =
    try
        DirectoryIndexBuilder.buildIndex
            [| invalidCollection |]
            [| loaded invalidCollection [| invalidItem |] |]
        |> ignore
        None
    with
    | :? InvalidOperationException as error -> Some error.Message

check "Entry without an HTTP(S) homepage or feed is rejected with source context" (
    match invalidBuild with
    | Some message ->
        message.Contains("Invalid", StringComparison.Ordinal)
        && message.Contains("Broken source", StringComparison.Ordinal)
    | None -> false)

printfn ""
if failures = 0 then
    printfn "ALL DIRECTORY INDEX CHECKS PASSED"
    exit 0
else
    printfn "%d DIRECTORY INDEX CHECK(S) FAILED" failures
    exit 1
