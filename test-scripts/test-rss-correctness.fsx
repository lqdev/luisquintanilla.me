// Regression checks for RSS dates, HTML references, source CDATA, and Media RSS.
// Run from the project root, after `dotnet build`:
//   dotnet fsi test-scripts/test-rss-correctness.fsx

#r "../bin/Debug/net10.0/PersonalSite.dll"

open System
open System.Globalization
open System.IO
open System.Xml.Linq
open Domain
open GenericBuilder

let mutable failures = 0
let check name condition =
    if condition then printfn "  PASS  %s" name
    else failures <- failures + 1; printfn "  FAIL  %s" name

printfn "=== RSS correctness and Media RSS checks ==="

let originalCulture = CultureInfo.CurrentCulture
try
    CultureInfo.CurrentCulture <- CultureInfo("fr-FR")
    let parsed = tryParseRssDate "2026-10-15 12:34 -05:00"
    check "offset date parses with invariant culture" (
        parsed = Some(DateTimeOffset(2026, 10, 15, 12, 34, 0, TimeSpan.FromHours(-5))))
    check "wire date is RFC 1123 GMT" (
            (parsed |> Option.map formatRssDate) = Some "Thu, 15 Oct 2026 17:34:00 GMT")
finally
    CultureInfo.CurrentCulture <- originalCulture

check "U.S. AM/PM date with explicit offset parses" (
    tryParseRssDate "10/15/2026 12:34 PM -05:00" |> Option.isSome)
check "blank RSS date is omitted" (tryParseRssDate " " |> Option.isNone)
check "date without explicit timezone is omitted" (
    tryParseRssDate "2026-10-15 12:34" |> Option.isNone)
let normalizedPubDate =
    rssPubDateElement "fixture" "2026-10-15 12:34 -05:00"
    |> Option.map (fun date -> date.Value)
check "RSS pubDate is valid RFC 1123 text" (
    normalizedPubDate = Some "Thu, 15 Oct 2026 17:34:00 GMT")

let html =
    """<p><a href="guide.html">Guide</a><img src="/images/photo.jpg" alt="Photo"><video poster="../poster.png"></video><a href="https://external.example/a">External</a><img srcset="/small.jpg 1x"></p>"""
let normalized = normalizeRssHtmlUrls html "https://www.example.test/posts/entry"
check "relative HTML href resolves against item permalink" (
    normalized.Contains("https://www.example.test/posts/entry/guide.html"))
check "root-relative HTML src resolves against the origin" (
    normalized.Contains("https://www.example.test/images/photo.jpg"))
check "parent-relative poster resolves against item permalink" (
    normalized.Contains("https://www.example.test/posts/poster.png"))
check "absolute URLs and srcset are not rewritten" (
    normalized.Contains("https://external.example/a")
    && normalized.Contains("srcset=")
    && normalized.Contains("/small.jpg 1x"))
check "custom-scheme links are retained without URL-resolution diagnostics" (
    normalizeRssHtmlUrls """<a href="lqvlc://radio.example/stream.pls">Radio</a>"""
        "https://www.example.test/posts/entry"
    |> fun result -> result.Contains("lqvlc://radio.example/stream.pls"))
let htmlWithEntitiesAndLineBreak =
    """<p>List&lt;AIBookmark&gt; &amp; other values<br>Next line <a href="guide.html" title="A &quot;quoted&quot; title">Guide</a></p>"""
let normalizedEntitiesAndLineBreak =
    normalizeRssHtmlUrls htmlWithEntitiesAndLineBreak "https://www.example.test/posts/entry"
check "HTML rewriting retains br elements and escapes decoded text entities" (
    normalizedEntitiesAndLineBreak.Contains("<br>")
    && normalizedEntitiesAndLineBreak.Contains("List&lt;AIBookmark&gt;")
    && normalizedEntitiesAndLineBreak.Contains("&amp; other values"))
check "HTML rewriting safely re-escapes decoded attribute quotes" (
    normalizedEntitiesAndLineBreak.Contains("title=\"A &quot;quoted&quot; title\"")
    && normalizedEntitiesAndLineBreak.Contains("href=\"https://www.example.test/posts/entry/guide.html\""))

let sourceMarkdown = "text with ]]> marker"
let sourceElement = generateSourceMarkdown (Some sourceMarkdown) |> Option.get
let sourceReparsed = XElement.Parse(sourceElement.ToString())
check "source markdown preserves embedded CDATA terminator" (
    sourceReparsed.Value = sourceMarkdown)

let mediaMarkdown =
    """:::media
- url: "https://cdn.example.test/images/one.jpg"
  alt: "First"
  mediaType: "image"
- url: "/audio/clip.mp3"
  alt: "Second"
  mediaType: "audio"
- url: "https://cdn.example.test/images/one.jpg"
  alt: "Duplicate"
  mediaType: "image"
- url: "https://provider.example.test/watch?id=7"
  alt: "Provider page"
  mediaType: "video"
- url: "javascript:alert(1)"
  alt: "Unsupported"
  mediaType: "image"
- url: "https://cdn.example.test/files/data.bin"
  alt: "Unknown extension"
  mediaType: "video"
:::media

:::media
- url: "https://cdn.example.test/audio/voice.opus?download=1"
  alt: "Audio"
  mediaType: "audio"
:::media
"""
let mediaElements =
    mediaRssContentElements mediaMarkdown "https://www.example.test/media/fixture"
let mediaNamespace = XNamespace.Get("http://search.yahoo.com/mrss/")
let mediaUrls =
    mediaElements
    |> List.map (fun element -> element.Attribute(XName.Get "url").Value)
check "Media RSS emits direct supported assets only" (mediaElements.Length = 3)
check "Media RSS preserves AST and list order" (
    mediaUrls = [
        "https://cdn.example.test/images/one.jpg"
        "https://www.example.test/audio/clip.mp3"
        "https://cdn.example.test/audio/voice.opus?download=1"
    ])
let mediaMimeTypes =
    mediaElements
    |> List.map (fun element -> element.Attribute(XName.Get "type").Value)
check "Media RSS applies bounded extension MIME inference" (
    mediaMimeTypes = [ "image/jpeg"; "audio/mpeg"; "audio/ogg" ])
check "Media RSS elements use the required namespace" (
    mediaElements |> List.forall (fun element -> element.Name = mediaNamespace + "content"))

let albumProcessor = AlbumProcessor.create()
let albumPath = Path.Combine("_src", "media", "fall-mountains.md")
let album =
    albumProcessor.Parse albumPath
    |> Result.defaultWith (fun error -> failwith (Diagnostics.render error))
let albumItem =
    albumProcessor.RenderRss album
    |> Option.defaultWith (fun () -> failwith "Expected album RSS item")
let albumDescription = albumItem.Element(XName.Get "description").Value
let albumMedia = albumItem.Elements(mediaNamespace + "content") |> Seq.toArray
check "album RSS publishes fully rendered media HTML" (
    albumDescription.Contains("<img") && albumDescription.Contains("<figure"))
check "album RSS attaches exactly two ordered media resources" (
    albumMedia.Length = 2
    && albumMedia.[0].Attribute(XName.Get "url").Value.EndsWith("/sunrise.jpg")
    && albumMedia.[1].Attribute(XName.Get "url").Value.EndsWith("/sunset.jpg"))
check "album RSS preserves source markdown" (
    albumItem.Element(XNamespace.Get("http://source.scripting.com/") + "markdown")
        .Value.Contains("sunrise.jpg"))

let taggedAlbum =
    { album with Metadata = { album.Metadata with Tags = [| "rss-fixture" |] } }
let taggedAlbumItem =
    albumProcessor.RenderRss taggedAlbum
    |> Option.defaultWith (fun () -> failwith "Expected tagged album RSS item")
let albumFeedData : FeedData<Album> = {
    Content = taggedAlbum
    CardHtml = ""
    RssXml = Some taggedAlbumItem
}
let unifiedAlbum = UnifiedFeeds.convertAlbumsToUnified [ albumFeedData ]
let propagationDirectory =
    Path.Combine(Path.GetTempPath(), "rss-propagation-" + Guid.NewGuid().ToString("N"))
Directory.CreateDirectory(propagationDirectory) |> ignore
try
    UnifiedFeeds.buildAllFeeds [ ("media", unifiedAlbum) ] propagationDirectory
    UnifiedFeeds.buildTagFeeds [ ("media", unifiedAlbum) ] propagationDirectory
    let mediaPaths = [
        "media/feed.xml"
        "feed/feed.xml"
        "tags/rss-fixture/feed.xml"
    ]
    let resourcesByFeed =
        mediaPaths
        |> List.map (fun relativePath ->
            let document = XDocument.Load(Path.Combine(propagationDirectory, relativePath))
            document.Descendants(mediaNamespace + "content") |> Seq.length)
    check "Media RSS survives type, unified, and tag feed propagation" (
        resourcesByFeed = [ 2; 2; 2 ])
finally
    Directory.Delete(propagationDirectory, true)

let collectionData : CollectionData = {
    Metadata = {
        Id = "rss-fixture"
        Title = "Caf\u00e9 & Travel"
        Description = "A & B <C>"
        CollectionType = CollectionType.Other "fixture"
        UrlPath = "/collections/rss-fixture/"
        DataFile = "rss-fixture.json"
        Tags = [||]
        LastUpdated = "2026-10-15"
        ItemCount = Some 0
    }
    Items = [||]
}
let collectionXml = Collections.CollectionProcessor.generateCollectionRss collectionData
let collectionDocument = XDocument.Parse(collectionXml)
check "collection RSS escapes XML text and preserves empty channel semantics" (
    collectionDocument.Root.Element(XName.Get "channel").Element(XName.Get "title").Value = "Caf\u00e9 & Travel"
    && (collectionDocument.Descendants(XName.Get "item") |> Seq.isEmpty))

printfn ""
if failures = 0 then
    printfn "ALL CHECKS PASSED"
    exit 0
else
    printfn "%d CHECK(S) FAILED" failures
    exit 1
