// Regression guard for RFC 4685 threading vocabulary in RSS response items.
// Run from the project root, after `dotnet build`:
//   dotnet fsi test-scripts/test-rss-reply-threading.fsx

#r "../bin/Debug/net10.0/PersonalSite.dll"

open System
open System.IO
open System.Xml.Linq
open Domain
open GenericBuilder

let threadNamespace = XNamespace.Get("http://purl.org/syndication/thread/1.0")
let mutable failures = 0
let check name condition =
    if condition then printfn "  PASS  %s" name
    else failures <- failures + 1; printfn "  FAIL  %s" name

let responseType = ResponseProcessor.create()
let target = "https://rsc.example/posts/parent?x=1&y=two"
let responseWith (responseKind: string) (targetUrl: string) : Response =
    {
        FileName = "reply-fixture"
        Metadata =
            ({
                Title = "Reply fixture"
                TargetUrl = targetUrl
                ResponseType = responseKind
                RsvpStatus = None
                DatePublished = "2026-10-15 12:34 -05:00"
                DateUpdated = ""
                Tags = [| "interop"; "fsharp" |]
                ReadingTimeMinutes = None
            } : ResponseDetails)
        Content = "A deterministic reply body."
        MarkdownSource = Some "---\ntitle: Reply fixture\n---\nA deterministic reply body."
    }

let render (response: Response) =
    responseType.RenderRss response
    |> Option.defaultWith (fun () -> failwith "Expected response RSS item")

let threadElements (item: XElement) = item.Elements(threadNamespace + "in-reply-to") |> Seq.toArray
let parseThreadFromItem (item: XElement) =
    let serialized = item.ToString(SaveOptions.DisableFormatting)
    let reparsed = XElement.Parse(serialized)
    serialized, threadElements reparsed

printfn "=== RSS reply threading regression checks ==="

let item = render (responseWith "reply" target)
let serialized, roundTrippedThreads = parseThreadFromItem item
check "reply emits exactly one RFC 4685 element" (roundTrippedThreads.Length = 1)
check "element uses the RFC 4685 namespace" (roundTrippedThreads.Length = 1 && roundTrippedThreads.[0].Name.Namespace = threadNamespace)
check "ref and href identify the selected parent permalink" (
    roundTrippedThreads.Length = 1
    && roundTrippedThreads.[0].Attribute(XName.Get "ref").Value = target
    && roundTrippedThreads.[0].Attribute(XName.Get "href").Value = target)
check "thread prefix is declared and query ampersand is XML-escaped" (
    serialized.Contains("xmlns:thr=\"http://purl.org/syndication/thread/1.0\"")
    && serialized.Contains("x=1&amp;y=two"))
check "XML roundtrip preserves the original target" (
    roundTrippedThreads.Length = 1
    && roundTrippedThreads.[0].Attribute(XName.Get "ref").Value = target)
let trimmedTargetItem = render (responseWith "reply" (" " + target + " "))
let trimmedTargetThreads = threadElements trimmedTargetItem
check "surrounding target whitespace is trimmed" (
    trimmedTargetThreads.Length = 1
    && trimmedTargetThreads.[0].Attribute(XName.Get "ref").Value = target)
check "existing item fields and source:markdown remain intact" (
    item.Element(XName.Get "title").Value = "Reply fixture"
    && item.Element(XName.Get "link").Value = "https://lqdev.me/responses/reply-fixture"
    && item.Element(XName.Get "guid").Value = "https://lqdev.me/responses/reply-fixture"
    && item.Element(XName.Get "pubDate").Value = "Thu, 15 Oct 2026 17:34:00 GMT"
    && item.Element(XName.Get "description").Value.Contains("A deterministic reply body.")
    && (item.Elements(XName.Get "category") |> Seq.map (fun e -> e.Value) |> Seq.contains "interop")
    && (item.Elements(XNamespace.Get("http://source.scripting.com/") + "markdown") |> Seq.length = 1))

for nonReplyType in [ "star"; "reshare"; "bookmark"; "rsvp"; "other" ] do
    let nonReplyItem = render (responseWith nonReplyType target)
    check (sprintf "%s response has no threading element" nonReplyType) (threadElements nonReplyItem |> Seq.isEmpty)

for invalidTarget in [ null; ""; "   "; "not a URL"; "ftp://rsc.example/posts/parent"; "https://rsc.example/a b"; "https://rsc.example/%zz"; "http://" ] do
    let invalidItem = render (responseWith "reply" invalidTarget)
    check (sprintf "invalid target %A is omitted without an empty ref" invalidTarget) (threadElements invalidItem |> Seq.isEmpty)
    check (sprintf "invalid target %A preserves the item link and guid" invalidTarget) (
        invalidItem.Element(XName.Get "link").Value = "https://lqdev.me/responses/reply-fixture"
        && invalidItem.Element(XName.Get "guid").Value = "https://lqdev.me/responses/reply-fixture")

let fixtureDirectory = Path.Combine(Path.GetTempPath(), "rss-reply-threading-" + Guid.NewGuid().ToString("N"))
Directory.CreateDirectory(fixtureDirectory) |> ignore
let fixturePath = Path.Combine(fixtureDirectory, "reply-fixture.md")
let fixtureMarkdown =
    [ "---"
      "title: Reply fixture"
      $"targeturl: {target}"
      "response_type: reply"
      "dt_published: \"2026-10-15 12:34 -05:00\""
      "dt_updated: \"2026-10-15 12:34 -05:00\""
      "tags: [\"interop\", \"fsharp\"]"
      "---"
      ""
      "A deterministic reply body."
      "" ]
    |> String.concat Environment.NewLine
File.WriteAllText(fixturePath, fixtureMarkdown)

try
    let feedData = buildContentWithFeeds responseType [ fixturePath ] |> List.exactlyOne
    let historicalGuid = Constants.Urls.legacyWww + "/responses/reply-fixture"
    check "content pipeline retains historical RSS GUID identity" (feedData.RssXml.Value.Element(XName.Get "guid").Value = historicalGuid)

    let unified = UnifiedFeeds.convertResponsesToUnified [ feedData ]
    let outputDirectory = Path.Combine(fixtureDirectory, "public")
    UnifiedFeeds.buildAllFeeds [ ("responses", unified) ] outputDirectory
    UnifiedFeeds.buildTagFeeds [ ("responses", unified) ] outputDirectory

    let checkFeed path =
        let document = XDocument.Load(Path.Combine(outputDirectory, path))
        let feedItem = document.Descendants(XName.Get "item") |> Seq.exactlyOne
        let threads = feedItem.Elements(threadNamespace + "in-reply-to") |> Seq.toArray
        threads.Length = 1
        && threads.[0].Attribute(XName.Get "ref").Value = target
        && feedItem.Attribute(XNamespace.Xmlns + "thr").Value = threadNamespace.NamespaceName

    check "type-specific responses feed retains threading XML" (checkFeed "responses/feed.xml")
    check "unified feed retains threading XML" (checkFeed "feed/feed.xml")
    check "tag feed retains threading XML" (checkFeed "tags/interop/feed.xml")
finally
    Directory.Delete(fixtureDirectory, true)

printfn ""
if failures = 0 then
    printfn "ALL CHECKS PASSED"
    exit 0
else
    printfn "%d CHECK(S) FAILED" failures
    exit 1
