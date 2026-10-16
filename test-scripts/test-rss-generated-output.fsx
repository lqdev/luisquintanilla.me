// Validate generated RSS documents after `dotnet run`.
// Run from the project root:
//   dotnet fsi test-scripts/test-rss-generated-output.fsx

#r "../bin/Debug/net10.0/PersonalSite.dll"

open System
open System.Globalization
open System.IO
open System.Xml.Linq

let mutable failures = 0
let check name condition =
    if condition then printfn "  PASS  %s" name
    else failures <- failures + 1; printfn "  FAIL  %s" name

let publicRoot = Path.GetFullPath("_public")
if not (Directory.Exists(publicRoot)) then
    failwith "Generated site output `_public` is missing; run `dotnet run` first."

let rssFiles =
    Directory.GetFiles(publicRoot, "*.xml", SearchOption.AllDirectories)
    |> Array.choose (fun path ->
        let document = XDocument.Load(path)
        if not (isNull document.Root) && document.Root.Name = XName.Get "rss" then
            Some(path, document)
        else
            None)
let rssAliasesAndCollections =
    Directory.GetFiles(publicRoot, "*.rss", SearchOption.AllDirectories)
    |> Array.map (fun path -> XDocument.Load(path))

let validRfc1123Date (value: string) =
    let mutable parsed = DateTimeOffset.MinValue
    DateTimeOffset.TryParseExact(
        value,
        "R",
        CultureInfo.InvariantCulture,
        DateTimeStyles.None,
        &parsed)

let invalidDates =
    rssFiles
    |> Array.collect (fun (_, document) ->
        document.Descendants(XName.Get "channel")
        |> Seq.collect (fun channel ->
            seq {
                let buildDate = channel.Element(XName.Get "lastBuildDate")
                if not (isNull buildDate) && not (validRfc1123Date buildDate.Value) then
                    yield "lastBuildDate=" + buildDate.Value
                for item in channel.Elements(XName.Get "item") do
                    let pubDate = item.Element(XName.Get "pubDate")
                    if not (isNull pubDate) && not (validRfc1123Date pubDate.Value) then
                        yield "pubDate=" + pubDate.Value
            })
        |> Seq.toArray)

check "generated RSS XML documents parse" (rssFiles.Length > 0)
check "legacy and collection RSS documents parse" (rssAliasesAndCollections.Length > 0)
check "all generated RSS dates use RFC 1123 format" (invalidDates.Length = 0)

let feedPath relativePath = Path.Combine(publicRoot, relativePath)
let aliasMatches alias target =
    File.Exists(feedPath alias)
    && File.Exists(feedPath target)
    && File.ReadAllBytes(feedPath alias) = File.ReadAllBytes(feedPath target)

check "legacy microblog alias copies the notes feed" (
    aliasMatches "microblog.rss" "notes/feed.xml")
check "legacy blog alias copies the posts feed" (
    aliasMatches "blog.rss" "posts/feed.xml")
check "legacy all-content alias copies the unified feed" (
    aliasMatches "all.rss" "feed/feed.xml")

let mediaNamespace = XNamespace.Get("http://search.yahoo.com/mrss/")
let mediaFeed = XDocument.Load(feedPath "media/feed.xml")
let directMedia =
    mediaFeed.Descendants(mediaNamespace + "content")
    |> Seq.toArray
check "generated media feed contains Media RSS resources" (directMedia.Length > 0)
check "Media RSS resource URLs are absolute HTTP(S)" (
    directMedia
    |> Array.forall (fun element ->
        let mutable uri = Unchecked.defaultof<Uri>
        Uri.TryCreate(element.Attribute(XName.Get "url").Value, UriKind.Absolute, &uri)
        && (uri.Scheme = Uri.UriSchemeHttp || uri.Scheme = Uri.UriSchemeHttps)))

let postFeed = XDocument.Load(feedPath "posts/feed.xml")
let postDescriptions =
    postFeed.Descendants(XName.Get "item")
    |> Seq.choose (fun item ->
        let description = item.Element(XName.Get "description")
        if isNull description then None else Some description.Value)
    |> Seq.toArray
check "post RSS descriptions contain rendered HTML" (
    postDescriptions |> Array.exists (fun value -> value.Contains("<p>") || value.Contains("<h")))

printfn ""
if failures = 0 then
    printfn "ALL CHECKS PASSED (%d RSS documents)" rssFiles.Length
    exit 0
else
    printfn "%d CHECK(S) FAILED (%d RSS documents)" failures rssFiles.Length
    if invalidDates.Length > 0 then
        invalidDates |> Array.truncate 10 |> Array.iter (printfn "  Invalid date: %s")
    exit 1
