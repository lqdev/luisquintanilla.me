#!/usr/bin/env dotnet fsi

#r "../bin/Debug/net10.0/PersonalSite.dll"

open System
open System.IO
open System.Net
open System.Net.Http
open System.Text
open System.Text.Json
open System.Xml.Linq
open Collections
open Domain
open Loaders
open MarkdownService

let mutable failures = 0

let check name condition =
    if condition then
        printfn "  PASS  %s" name
    else
        failures <- failures + 1
        printfn "  FAIL  %s" name

let expectFailure name action =
    try
        action ()
        check name false
    with
    | :? ArgumentException
    | :? InvalidOperationException ->
        check name true
    | ex ->
        printfn "  INFO  %s raised %s: %s" name (ex.GetType().Name) ex.Message
        check name false

let projectRoot = Directory.GetCurrentDirectory()
let sourceAboutPath = Path.Combine(projectRoot, "_src", "about.md")
let generatedAboutPath = Path.Combine(projectRoot, "_public", ".well-known", "about.md")
let generatedAboutPagePath = Path.Combine(projectRoot, "_public", "about", "index.html")
let generatedTextAboutPagePath = Path.Combine(projectRoot, "_public", "text", "about", "index.html")
let generatedBlogrollPath = Path.Combine(projectRoot, "_public", "collections", "blogroll", "index.opml")
let generatedPodrollPath = Path.Combine(projectRoot, "_public", "collections", "podroll", "index.opml")
let generatedYouTubePath = Path.Combine(projectRoot, "_public", "collections", "youtube", "index.opml")
let generatedVCardPath = Path.Combine(projectRoot, "_public", "vcard.vcf")
let configPath = Path.Combine(projectRoot, "staticwebapp.config.json")

let readUtf8 path = File.ReadAllText(path, Encoding.UTF8)
let sourceAbout = readUtf8 sourceAboutPath

printfn "=== Social discovery validation ==="

printfn "\nAbout source and rendering"
let parsedAbout = parseAbout sourceAboutPath
check "About summary is non-empty" (not (String.IsNullOrWhiteSpace(parsedAbout.Yaml.Summary)))
check "About body starts with the existing heading" (parsedAbout.Content.TrimStart().StartsWith("# Hi!", StringComparison.Ordinal))
check "Generated About Markdown preserves the source bytes" (
    File.Exists(generatedAboutPath)
    && Convert.ToBase64String(File.ReadAllBytes(sourceAboutPath))
       = Convert.ToBase64String(File.ReadAllBytes(generatedAboutPath)))

let aboutPage = readUtf8 generatedAboutPagePath
let textAboutPage = readUtf8 generatedTextAboutPagePath
check "Main About page retains its heading" (aboutPage.Contains("Hi!", StringComparison.Ordinal))
check "Main About page retains ProfilePage structured data" (aboutPage.Contains("ProfilePage", StringComparison.Ordinal))
check "Main About page does not expose YAML frontmatter" (not (aboutPage.Contains("summary:", StringComparison.Ordinal)))
check "Text-only About page retains its heading" (textAboutPage.Contains("Hi!", StringComparison.Ordinal))
check "Text-only About page does not expose YAML frontmatter" (not (textAboutPage.Contains("summary:", StringComparison.Ordinal)))

let tempDir = Path.Combine(Path.GetTempPath(), "lqdev-social-discovery-" + Guid.NewGuid().ToString("N"))
Directory.CreateDirectory(tempDir) |> ignore
try
    let fixture (name: string) (content: string) =
        let path = Path.Combine(tempDir, name)
        File.WriteAllText(path, content, Encoding.UTF8)
        path

    expectFailure "About without frontmatter is rejected" (fun () ->
        parseAbout (fixture "missing.md" "# About") |> ignore)
    expectFailure "About with blank summary is rejected" (fun () ->
        parseAbout (fixture "blank.md" "---\nsummary: \"\"\n---\n# About") |> ignore)
    expectFailure "About with malformed YAML is rejected" (fun () ->
        parseAbout (fixture "malformed.md" "---\nsummary: [unterminated\n---\n# About") |> ignore)
    expectFailure "About with structured summary is rejected" (fun () ->
        parseAbout (fixture "structured.md" "---\nsummary:\n  - not-a-string\n---\n# About") |> ignore)
finally
    if Directory.Exists(tempDir) then
        Directory.Delete(tempDir, true)

printfn "\nOPML output"
let checkOpml (name: string) (path: string) =
    let document = XDocument.Load(path)
    let root = document.Root
    let outlines = root.Descendants(XName.Get "outline") |> Seq.toArray
    check (name + " is OPML 2.0") (root.Name.LocalName = "opml" && root.Attribute(XName.Get "version").Value = "2.0")
    check (name + " contains outlines with text") (
        outlines.Length > 0
        && outlines |> Array.forall (fun outline ->
            let text = outline.Attribute(XName.Get "text")
            not (isNull text) && not (String.IsNullOrWhiteSpace(text.Value))))
    outlines

let blogrollOutlines = checkOpml "Blogroll" generatedBlogrollPath
let podrollOutlines = checkOpml "Podroll" generatedPodrollPath
let youtubeOutlines = checkOpml "YouTube" generatedYouTubePath
check "Blogroll has the expected number of entries" (blogrollOutlines.Length = (loadBlogrollLinks ()).Length)
check "Podroll has the expected number of entries" (podrollOutlines.Length = (loadPodrollLinks ()).Length)
check "YouTube has the expected number of entries" (youtubeOutlines.Length = (loadYouTubeLinks ()).Length)

let specialOpml =
    OpmlService.buildOpmlFeed
        "Special & <collection>"
        "https://example.test"
        [| { Title = "A & B"; Type = "rss"; HtmlUrl = "https://example.test/?a=1&b=2"; XmlUrl = "https://example.test/feed?name=\"x\"" } |]
        |> fun document -> XDocument.Parse(document.ToString())
let specialOutline = specialOpml.Descendants(XName.Get "outline") |> Seq.exactlyOne
check "OPML safely round-trips XML-sensitive values" (
    specialOutline.Attribute(XName.Get "text").Value = "A & B"
    && specialOutline.Attribute(XName.Get "htmlUrl").Value = "https://example.test/?a=1&b=2"
    && specialOutline.Attribute(XName.Get "xmlUrl").Value = "https://example.test/feed?name=\"x\"")

printfn "\nvCard output"
let vcard = readUtf8 generatedVCardPath
check "Generated vCard preserves vCard 3.0" (vcard.Contains("VERSION:3.0", StringComparison.Ordinal))
check "Generated vCard has a formatted name" (vcard.Contains("FN:Luis Quintanilla", StringComparison.Ordinal))
check "Generated vCard preserves its source fields" (
    Convert.ToBase64String(File.ReadAllBytes(Path.Combine(projectRoot, "_src", "vcard.vcf"))) = Convert.ToBase64String(File.ReadAllBytes(generatedVCardPath)))

printfn "\nStatic Web Apps configuration"
let config = JsonDocument.Parse(readUtf8 configPath)
let routes = config.RootElement.GetProperty("routes").EnumerateArray() |> Seq.toArray
let routeFor (path: string) =
    routes
    |> Array.find (fun route -> route.GetProperty("route").GetString() = path)
let header (route: JsonElement) (name: string) =
    route.GetProperty("headers").GetProperty(name).GetString()
let rewrite (route: JsonElement) =
    let mutable value = Unchecked.defaultof<JsonElement>
    if route.TryGetProperty("rewrite", &value) then Some(value.GetString()) else None

let followingRoute = routeFor "/.well-known/following.opml"
let aboutRoute = routeFor "/.well-known/about.md"
let contactRoute = routeFor "/.well-known/contact.vcf"
check "Following route rewrites to the existing blogroll OPML" (rewrite followingRoute = Some "/collections/blogroll/index.opml")
check "About route serves the direct generated file" (rewrite aboutRoute |> Option.isNone)
check "Contact route rewrites to the existing vCard" (rewrite contactRoute = Some "/vcard.vcf")
check "Following route has the SocialSpec media type and CORS" (
    header followingRoute "Content-Type" = "text/x-opml; charset=utf-8"
    && header followingRoute "Access-Control-Allow-Origin" = "*")
check "About route has the SocialSpec media type and CORS" (
    header aboutRoute "Content-Type" = "text/markdown; charset=utf-8"
    && header aboutRoute "Access-Control-Allow-Origin" = "*")
check "Contact route has the SocialSpec media type and CORS" (
    header contactRoute "Content-Type" = "text/vcard; charset=utf-8"
    && header contactRoute "Access-Control-Allow-Origin" = "*")
let fallback = config.RootElement.GetProperty("navigationFallback")
let excludes = fallback.GetProperty("exclude").EnumerateArray() |> Seq.map _.GetString() |> Seq.toArray
check "Well-known paths bypass homepage fallback" (excludes |> Array.contains "/.well-known/*")
check "Existing WebFinger route remains configured" (routes |> Array.exists (fun route -> route.GetProperty("route").GetString() = "/.well-known/webfinger"))

printfn "\nHTML discovery links"
let homepage = readUtf8 (Path.Combine(projectRoot, "_public", "index.html"))
let alternateMatches = System.Text.RegularExpressions.Regex.Matches(homepage, "<link[^>]+rel=\"alternate\"[^>]*>")
let alternateLinks =
    alternateMatches
    |> Seq.cast<System.Text.RegularExpressions.Match>
    |> Seq.map _.Value
    |> Seq.toArray
check "Unified RSS feed is advertised first" (
    alternateLinks.Length >= 4
    && alternateLinks[0].Contains("href=\"/feed/feed.xml\"", StringComparison.Ordinal))
check "Existing blog, microblog, and response feed links remain" (
    alternateLinks |> Array.exists (_.Contains("href=\"/blog.rss\"", StringComparison.Ordinal))
    && alternateLinks |> Array.exists (_.Contains("href=\"/microblog.rss\"", StringComparison.Ordinal))
    && alternateLinks |> Array.exists (_.Contains("href=\"/responses.rss\"", StringComparison.Ordinal)))
check "No non-specified rel=following link was introduced" (
    not (homepage.Contains("rel=\"following\"", StringComparison.Ordinal)))

let runLiveChecks (baseUrl: string) =
    use handler = new HttpClientHandler(AllowAutoRedirect = false)
    use client = new HttpClient(handler)
    let origin = baseUrl.TrimEnd('/')
    let send (methodName: string) (path: string) (accept: string) =
        let request = new HttpRequestMessage(HttpMethod(methodName), origin + path)
        request.Headers.Add("Origin", "https://subscriber.example")
        request.Headers.Accept.ParseAdd(accept)
        client.Send(request)

    let checkLive (path: string) (accept: string) (mediaType: string) =
        use response = send "GET" path accept
        let body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        let contentType = response.Content.Headers.ContentType
        let corsPresent, corsValues = response.Headers.TryGetValues("Access-Control-Allow-Origin")
        check ("Live " + path + " returns 200 without redirect") (response.StatusCode = HttpStatusCode.OK)
        check ("Live " + path + " has the expected media type") (
            not (isNull contentType)
            && contentType.MediaType = mediaType
            && contentType.CharSet = "utf-8")
        check ("Live " + path + " allows cross-origin reads") (
            corsPresent
            && (corsValues |> Seq.contains "*"))
        body

    let liveFollowing = checkLive "/.well-known/following.opml" "text/x-opml" "text/x-opml"
    let liveAbout = checkLive "/.well-known/about.md" "text/markdown" "text/markdown"
    let liveContact = checkLive "/.well-known/contact.vcf" "text/vcard" "text/vcard"
    check "Live following response parses as OPML" (try XDocument.Parse(liveFollowing) |> ignore; true with _ -> false)
    check "Live About response contains summary frontmatter" (liveAbout.Contains("summary:", StringComparison.Ordinal))
    check "Live contact response contains FN" (liveContact.Contains("FN:Luis Quintanilla", StringComparison.Ordinal))

    for path in [| "/.well-known/following.opml"; "/.well-known/about.md"; "/.well-known/contact.vcf" |] do
        use response = send "HEAD" path "*/*"
        check ("Live HEAD " + path + " returns 200") (response.StatusCode = HttpStatusCode.OK)
        check ("Live HEAD " + path + " has no body") (response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult().Length = 0)

    for accept in [| "text/x-opml"; "text/html" |] do
        use response = send "GET" "/.well-known/does-not-exist" accept
        check ("Unknown well-known path returns 404 for " + accept) (response.StatusCode = HttpStatusCode.NotFound)

let args = Environment.GetCommandLineArgs()
let baseUrl =
    args
    |> Array.tryFindIndex ((=) "--base-url")
    |> Option.bind (fun index ->
        if index + 1 < args.Length then Some args[index + 1] else None)

match baseUrl with
| Some url ->
    printfn "\nLive HTTP checks against %s" url
    runLiveChecks url
| None ->
    printfn "\nLive HTTP checks skipped (pass --base-url <origin> after deployment)."

printfn ""
if failures = 0 then
    printfn "ALL SOCIAL DISCOVERY CHECKS PASSED"
    exit 0
else
    printfn "%d SOCIAL DISCOVERY CHECK(S) FAILED" failures
    exit 1
