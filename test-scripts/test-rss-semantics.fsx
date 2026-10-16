// Run after dotnet build. Optional: --capture-fixtures DIR or --baseline-fixtures DIR.
#r "../bin/Debug/net10.0/PersonalSite.dll"

open System
open System.IO
open System.Xml.Linq
open Domain
open GenericBuilder

let atom = XNamespace.Get "http://www.w3.org/2005/Atom"
let source = XNamespace.Get "http://source.scripting.com/"
let thread = XNamespace.Get "http://purl.org/syndication/thread/1.0"
let schema = "https://schema.org/"
let mutable failures = 0
let check (name: string) (condition: bool) =
    if condition then printfn "  PASS  %s" name
    else failures <- failures + 1; printfn "  FAIL  %s" name

let parse (processor: ContentProcessor<'T>) (path: string) : 'T =
    processor.Parse path |> Result.defaultWith (Diagnostics.render >> failwith)
let render (processor: ContentProcessor<'T>) (content: 'T) : XElement =
    processor.RenderRss content |> Option.defaultWith (fun () -> failwith "Missing RSS item")
let postProcessor = PostProcessor.create()
let noteProcessor = NoteProcessor.create()
let responseProcessor = ResponseProcessor.create()
let postPath = Path.Combine("_src", "posts", "hello-world.md")
let notePath = Path.Combine("_src", "notes", "happy-new-year-2026.md")
let post = parse postProcessor postPath
let note = parse noteProcessor notePath
let reply = parse responseProcessor (Path.Combine("_src", "responses", "2000-poops-2025-09-23.md"))
let star = parse responseProcessor (Path.Combine("_src", "responses", "404-media-full-text-rss.md"))
let bookmark = parse responseProcessor (Path.Combine("_src", "bookmarks", "1-percent-rule.md"))
let target = "https://example.org/original-post?x=1&y=two#discussion"
let responseWith (kind: string) (url: string) : Response =
    { reply with
        FileName = "semantics-fixture"
        Metadata = { reply.Metadata with ResponseType = kind; TargetUrl = url }
        Content = "<p>A &amp; B, ]]> and <strong>markup</strong>.</p>"
        MarkdownSource = Some "**A & B** ]]> source\n" }

let rawFixtures () = [
    "post", render postProcessor post
    "note", render noteProcessor note
    "reply", render responseProcessor reply
    "star", render responseProcessor star
    "bookmark", render responseProcessor bookmark
    "short-post", render postProcessor { post with Content = "**Short** ]]>"; MarkdownSource = Some "**Short** ]]>" }
    "long-note", render noteProcessor { note with Content = String.replicate 300 "Long note. " }
    for kind in [ "reply"; "star"; "bookmark"; "RePlY"; "STAR"; "BOOKMARK"; "reshare"; "share"; "rsvp"; "unknown"; "review"; "rating"; " reply " ] do
        kind.Trim() + "-" + string kind.Length, render responseProcessor (responseWith kind (" " + target + " "))
]

let argument (flag: string) =
    let args = fsi.CommandLineArgs |> Array.toList
    args |> List.tryFindIndex ((=) flag) |> Option.map (fun i -> args.[i + 1])

let fixtures = rawFixtures()
// Keep the last case variant when writing snapshots on case-insensitive systems.
let snapshotFixtures =
    fixtures |> List.groupBy (fun (name, _) -> name.ToLowerInvariant()) |> List.map (snd >> List.last)
match argument "--capture-fixtures" with
| Some directory ->
    Directory.CreateDirectory directory |> ignore
    for name, item in snapshotFixtures do
        File.WriteAllText(Path.Combine(directory, name + ".xml"), item.ToString(SaveOptions.DisableFormatting))
    printfn "Captured %d prechange processor fixtures" snapshotFixtures.Length
    exit 0
| None -> ()

// Independent consumer: no producer mapping/helper references, and namespace URI
// selection works regardless of prefixes or declaration placement.
let consume (item: XElement) =
    let categories = item.Elements(atom + "category") |> Seq.toList
    let links = item.Elements(atom + "link") |> Seq.toList
    match categories with
    | [ category ] ->
        let term = category.Attribute(XName.Get "term").Value
        let profileRole, targetRole =
            match term with
            | "https://schema.org/BlogPosting"
            | "https://schema.org/SocialMediaPosting" -> "author", None
            | "https://schema.org/Comment" -> "author", Some "parentItem"
            | "https://schema.org/LikeAction"
            | "https://schema.org/BookmarkAction" -> "agent", Some "object"
            | _ -> failwith ("Unrecognized semantic category: " + term)
        let href role =
            links
            |> List.filter (fun link -> link.Attribute(XName.Get "rel").Value = schema + role)
            |> List.exactlyOne
            |> fun link -> link.Attribute(XName.Get "href").Value
        check "consumer: exact link cardinality" (links.Length = if targetRole.IsSome then 2 else 1)
        check "consumer: public profile" (href profileRole = "https://lqdev.me/")
        Some(term, targetRole |> Option.map href)
    | [] ->
        check "no partial semantic links" links.IsEmpty
        None
    | _ -> failwith "Duplicate semantic categories"

let assertKind (label: string) (kind: string) (expectedTarget: string option) (item: XElement) =
    let wire = item.ToString(SaveOptions.DisableFormatting)
    let parsed = XElement.Parse wire
    check label (consume parsed = Some(schema + kind, expectedTarget))
    let threads = parsed.Elements(thread + "in-reply-to") |> Seq.toArray
    check (label + ": reply-only threading") (
        if kind = "Comment" then
            threads.Length = 1
            && threads.[0].Attribute(XName.Get "ref").Value = expectedTarget.Value
            && threads.[0].Attribute(XName.Get "href").Value = expectedTarget.Value
        else threads.Length = 0)
    check (label + ": no double escaping") (not (wire.Contains("&amp;amp;y=two")))

for name, item in fixtures |> List.take 7 do
    let kind, expectedTarget =
        match name with
        | "post" | "short-post" -> "BlogPosting", None
        | "note" | "long-note" -> "SocialMediaPosting", None
        | "reply" -> "Comment", Some(reply.Metadata.TargetUrl.Trim())
        | "star" -> "LikeAction", Some(star.Metadata.TargetUrl.Trim())
        | _ -> "BookmarkAction", Some(bookmark.Metadata.TargetUrl.Trim())
    assertKind name kind expectedTarget item

for kind, expected in [ "reply", "Comment"; "RePlY", "Comment"; "star", "LikeAction"; "STAR", "LikeAction"; "bookmark", "BookmarkAction"; "BOOKMARK", "BookmarkAction" ] do
    assertKind (kind + ": trimmed exact target") expected (Some target) (render responseProcessor (responseWith kind (" " + target + " ")))

for url in [ "http://example.org/Mixed/Case/%2f?x=A%20B#Fragment"; "https://example.org/path/" ] do
    assertKind "target validation preserves exact HTTP(S) escaping/case/trailing slash" "Comment" (Some url)
        (render responseProcessor (responseWith "reply" (" " + url + " ")))

for kind in [ "reshare"; "share"; "rsvp"; "unknown"; "review"; "rating"; " reply "; "" ] do
    let before = Diagnostics.errorCount()
    check (kind + ": unsupported kind unclassified") (consume (render responseProcessor (responseWith kind target)) = None)
    check (kind + ": unsupported kind has no new diagnostic") (Diagnostics.errorCount() = before)

let reviewProcessor = ReviewProcessor.create()
let review = parse reviewProcessor (Path.Combine("_src", "reviews", "library", "agency-gibson.md"))
check "actual review is outside semantic profile" (consume (render reviewProcessor review) = None)
let snippetProcessor = SnippetProcessor.create()
let snippet = parse snippetProcessor (Path.Combine("_src", "resources", "snippets", "fsharp-data-rss-parser.md"))
check "actual resource is outside semantic profile" (consume (render snippetProcessor snippet) = None)
let albumProcessor = AlbumProcessor.create()
let album = parse albumProcessor (Path.Combine("_src", "media", "fall-mountains.md"))
let albumItem = render albumProcessor album
check "actual media item is outside semantic profile and keeps Media RSS" (
    consume albumItem = None
    && albumItem.Elements(XNamespace.Get("http://search.yahoo.com/mrss/") + "content") |> Seq.length = 2)

for kind in [ "reply"; "star"; "bookmark" ] do
    for invalid in [ null; ""; " "; "/relative"; "ftp://example.org/a"; "https://user:pass@example.org/a"; "https://example.org/%zz"; "https://example.org/a b"; "http://" ] do
        let before = Diagnostics.errorCount()
        use output = new StringWriter()
        let original = Console.Out
        let item =
            try
                Console.SetOut output
                render responseProcessor (responseWith kind invalid)
            finally Console.SetOut original
        check (sprintf "%s %A: whole block omitted" kind invalid) (consume item = None)
        check "invalid target retains readable item and omits threading" (
            item.Element(XName.Get "description").Value.Contains("markup")
            && item.Element(XName.Get "link").Value.EndsWith("/semantics-fixture")
            && item.Elements(thread + "in-reply-to") |> Seq.isEmpty)
        check "diagnostic identifies source, subtype, rejected value and fix" (
            Diagnostics.errorCount() = before + 1
            && output.ToString().Contains("rss-relationship-target")
            && output.ToString().Contains("semantics-fixture.md")
            && output.ToString().Contains(kind)
            && output.ToString().Contains(sprintf "%A" invalid)
            && output.ToString().Contains("fix:"))
check "diagnostics participate in existing strict policy" (Diagnostics.isStrict [| "--strict" |] && Diagnostics.errorCount() > 0)
check "ordinary mode is not promoted to strict" (
    Environment.GetEnvironmentVariable("STRICT_CONTENT") = "1" || not (Diagnostics.isStrict [||]))

let legacy (item: XElement) =
    let copy = XElement item
    copy.Elements() |> Seq.filter (fun e -> e.Name.Namespace = atom) |> Seq.toArray |> Array.iter (fun e -> e.Remove())
    copy.Attributes() |> Seq.filter (fun a -> a.IsNamespaceDeclaration && a.Value = atom.NamespaceName) |> Seq.toArray |> Array.iter (fun a -> a.Remove())
    copy.ToString(SaveOptions.DisableFormatting)

match argument "--baseline-fixtures" with
| Some directory ->
    for name, item in snapshotFixtures do
        let baseline = XElement.Load(Path.Combine(directory, name + ".xml"))
        check (name + ": all original fields, attributes, CDATA and order identical") (legacy item = legacy baseline)
| None -> ()

let edge = render responseProcessor (responseWith "reply" target)
check "source CDATA roundtrip keeps exact embedded terminator" (
    XElement.Parse(edge.ToString()).Element(source + "markdown").Value = "**A & B** ]]> source\n")
check "topic categories remain unqualified" (
    edge.Elements(XName.Get "category") |> Seq.map (fun e -> e.Value) |> Seq.toArray = reply.Metadata.Tags)
check "no new source reply/comments/likes or HTTPS source namespace" (
    edge.Elements() |> Seq.forall (fun e ->
        e.Name.NamespaceName <> "https://source.scripting.com/"
        && (e.Name.Namespace <> source || e.Name.LocalName = "markdown")))

let normalizeDeclarations (item: XElement) =
    let parsed = XElement.Parse(item.ToString())
    for element in parsed.DescendantsAndSelf() do
        element.Attributes() |> Seq.filter (fun a -> a.IsNamespaceDeclaration)
        |> Seq.toArray |> Array.iter (fun a -> a.Remove())
    parsed

let sample = XDocument.Load(Path.Combine(__SOURCE_DIRECTORY__, "fixtures", "rss-semantics.xml"))
let sampleItems = sample.Descendants(XName.Get "item") |> Seq.toList
check "issue sample has exactly five entries" (sampleItems.Length = 5)
for item, (kind, expectedTarget) in List.zip sampleItems [
    "BlogPosting", None
    "SocialMediaPosting", None
    "Comment", Some target
    "LikeAction", Some "https://example.org/liked-article"
    "BookmarkAction", Some "https://example.org/article?x=1&y=two" ] do
    assertKind ("exact issue sample: " + kind) kind expectedTarget item
    check "issue sample keeps topic and HTTP source distinct" (
        item.Elements(XName.Get "category") |> Seq.exactlyOne |> fun e -> e.Value = "feeds"
        && item.Elements(source + "markdown") |> Seq.length = 1)
    check "issue sample retains own link and historical GUID without attributes" (
        item.Element(XName.Get "link").Value.StartsWith("https://lqdev.me/")
        && item.Element(XName.Get "guid").Value.StartsWith("https://www.lqdev.me/")
        && not (item.Element(XName.Get "guid").HasAttributes))

for guidText in [ "https://lqdev.me/posts/identity"; "https://www.lqdev.me/posts/identity"; "urn:uuid:opaque-identity" ] do
    for permalink in [ None; Some "true"; Some "false" ] do
        let item = render postProcessor post
        let guid = item.Element(XName.Get "guid")
        guid.Value <- guidText
        permalink |> Option.iter (fun value -> guid.Add(XAttribute(XName.Get "isPermaLink", value)))
        let raw = XElement item
        let processor =
            { postProcessor with
                Parse = fun _ -> Ok post
                RenderRss = fun _ -> Some(XElement item) }
        let built = buildContentWithFeeds processor [ "identity-fixture" ] |> List.exactlyOne |> fun f -> f.RssXml.Value
        let expectedGuid = guidText.Replace("https://lqdev.me/", "https://www.lqdev.me/")
        check "GUID attributes and raw/built identity boundary remain unchanged" (
            raw.Element(XName.Get "guid").Value = guidText
            && built.Element(XName.Get "guid").Value = expectedGuid
            && (built.Element(XName.Get "guid").Attributes() |> Seq.map (fun a -> a.Name, a.Value) |> Seq.toList)
                = (guid.Attributes() |> Seq.map (fun a -> a.Name, a.Value) |> Seq.toList))

let temporary = Path.Combine(Path.GetTempPath(), "rss-semantics-" + Guid.NewGuid().ToString("N"))
Directory.CreateDirectory temporary |> ignore
try
    let builtPost = buildContentWithFeeds postProcessor [ postPath ]
    let builtNote = buildContentWithFeeds noteProcessor [ notePath ]
    let responsePaths = [
        Path.Combine("_src", "responses", "2000-poops-2025-09-23.md")
        Path.Combine("_src", "responses", "404-media-full-text-rss.md")
        Path.Combine("_src", "bookmarks", "1-percent-rule.md") ]
    let builtResponses = buildContentWithFeeds responseProcessor responsePaths
    let datasets = [
        "posts", UnifiedFeeds.convertPostsToUnified builtPost
        "notes", UnifiedFeeds.convertNotesToUnified builtNote
        "responses", UnifiedFeeds.convertResponsesToUnified builtResponses
        "bookmarks", UnifiedFeeds.convertBookmarkResponsesToUnified builtResponses ]
    let unified = datasets |> List.collect snd
    for entry in unified do
        check "built GUID preserves historical www identity without added attributes" (
            entry.RssXml.Element(XName.Get "guid").Value.StartsWith("https://www.lqdev.me/")
            && not (entry.RssXml.Element(XName.Get "guid").HasAttributes))
        check "built entry identity differs from relationship target" (
            consume entry.RssXml |> Option.get |> snd |> Option.forall ((<>) entry.Url))
    UnifiedFeeds.buildAllFeeds datasets temporary
    UnifiedFeeds.buildTagFeeds datasets temporary
    let expectedOrder =
        unified |> List.sortByDescending (fun i -> DateTimeOffset.Parse i.Date)
        |> List.map (fun i -> i.RssXml.Element(XName.Get "guid").Value)
    let feed = XDocument.Load(Path.Combine(temporary, "feed", "feed.xml"))
    let items = feed.Descendants(XName.Get "item") |> Seq.toList
    check "controlled five-kind feed preserves membership and chronological order" (
        items |> List.map (fun i -> i.Element(XName.Get "guid").Value) = expectedOrder)
    check "consumer extracts all five kinds from controlled generated feed" (
        items |> List.choose consume |> List.map fst |> Set.ofList =
            Set.ofList [ schema + "BlogPosting"; schema + "SocialMediaPosting"; schema + "Comment"; schema + "LikeAction"; schema + "BookmarkAction" ])
    for path in Directory.GetFiles(temporary, "*.xml", SearchOption.AllDirectories) do
        for item in XDocument.Load(path).Descendants(XName.Get "item") do
            let guid = item.Element(XName.Get "guid").Value
            let original = unified |> List.find (fun i -> i.RssXml.Element(XName.Get "guid").Value = guid)
            check "type/unified/tag feeds reuse item XML independent of namespace declarations" (
                XNode.DeepEquals(normalizeDeclarations item, normalizeDeclarations original.RssXml))
    check "semantic vocabulary never becomes a topic feed" (
        Directory.GetDirectories(Path.Combine(temporary, "tags"))
        |> Array.forall (fun p -> not (p.Contains("schema.org"))))
    let otherFormats () =
        UnifiedFeeds.generateJsonFeedString "Fixture" "https://lqdev.me/" "https://lqdev.me/feed.json" "Fixture" unified true
    let before = otherFormats()
    for entry in unified do
        entry.RssXml.Elements() |> Seq.filter (fun e -> e.Name.Namespace = atom) |> Seq.toArray |> Array.iter (fun e -> e.Remove())
    check "JSON Feed does not consume annotations" (before = otherFormats())
finally
    Directory.Delete(temporary, true)

if fsi.CommandLineArgs |> Array.contains "--generated-output" then
    for relative in [
        Path.Combine("feed", "feed.xml"); Path.Combine("posts", "feed.xml")
        Path.Combine("notes", "feed.xml"); Path.Combine("responses", "feed.xml")
        Path.Combine("bookmarks", "feed.xml"); Path.Combine("tags", "indieweb", "feed.xml") ] do
        let items =
            XDocument.Load(Path.Combine("_public", relative)).Descendants(XName.Get "item") |> Seq.toList
        for item in items do
            let path = Uri(item.Element(XName.Get "link").Value).AbsolutePath.Trim('/').Split('/')
            let expected =
                match path with
                | [| "posts"; _ |] -> Some(schema + "BlogPosting", None)
                | [| "notes"; _ |] -> Some(schema + "SocialMediaPosting", None)
                | [| ("responses" | "bookmarks"); slug |] ->
                    let sourcePath =
                        [ Path.Combine("_src", "responses", slug + ".md"); Path.Combine("_src", "bookmarks", slug + ".md") ]
                        |> List.filter File.Exists |> List.exactlyOne
                    let metadata = (parse responseProcessor sourcePath).Metadata
                    match metadata.ResponseType.ToLowerInvariant() with
                    | "reply" -> Some(schema + "Comment", Some(metadata.TargetUrl.Trim()))
                    | "star" -> Some(schema + "LikeAction", Some(metadata.TargetUrl.Trim()))
                    | "bookmark" -> Some(schema + "BookmarkAction", Some(metadata.TargetUrl.Trim()))
                    | _ -> None
                | _ -> None
            check (relative + ": independent consumer matches source kind and target") (consume item = expected)
            check "conventional consumer still reads title, body, own link, GUID and date" (
                [ "title"; "description"; "link"; "guid"; "pubDate" ]
                |> List.forall (fun name -> not (isNull (item.Element(XName.Get name)))))

if failures > 0 then failwithf "%d semantics checks failed" failures
printfn "ALL RSS SEMANTICS CHECKS PASSED"
