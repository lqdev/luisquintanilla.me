#!/usr/bin/env dotnet fsi

#r "../bin/Debug/net10.0/PersonalSite.dll"
#r "../bin/Debug/net10.0/Giraffe.ViewEngine.dll"

open System
open System.IO
open System.Net
open System.Net.Http
open System.Text
open System.Text.Json
open System.Text.RegularExpressions
open System.Xml.Linq
open Collections
open Domain
open Giraffe.ViewEngine
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
let generatedFollowingPath = Path.Combine(projectRoot, "_public", ".well-known", "following.opml")
let generatedBlogrollPath = Path.Combine(projectRoot, "_public", "collections", "blogroll", "index.opml")
let generatedPodrollPath = Path.Combine(projectRoot, "_public", "collections", "podroll", "index.opml")
let generatedYouTubePath = Path.Combine(projectRoot, "_public", "collections", "youtube", "index.opml")
let generatedForumsPath = Path.Combine(projectRoot, "_public", "collections", "forums", "index.opml")
let generatedAiStarterPackPath = Path.Combine(projectRoot, "_public", "collections", "starter-packs", "ai", "index.opml")
let generatedVCardPath = Path.Combine(projectRoot, "_public", "vcard.vcf")
let generatedProfilePath = Path.Combine(projectRoot, "_public", ".well-known", "profile.json")
let generatedProfileSchemaPath = Path.Combine(projectRoot, "_public", ".well-known", "profile.schema.v1.json")
let generatedUnifiedRssPath = Path.Combine(projectRoot, "_public", "feed", "feed.xml")
let generatedUnifiedJsonFeedPath = Path.Combine(projectRoot, "_public", "feed", "index.json")
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
let forumsOutlines = checkOpml "Forums" generatedForumsPath
let aiStarterPackOutlines = checkOpml "AI Starter Pack" generatedAiStarterPackPath
check "Blogroll has the expected number of entries" (blogrollOutlines.Length = (loadBlogrollLinks ()).Length)
check "Podroll has the expected number of entries" (podrollOutlines.Length = (loadPodrollLinks ()).Length)
check "YouTube has the expected number of entries" (youtubeOutlines.Length = (loadYouTubeLinks ()).Length)
check "Forums has the expected number of entries" (forumsOutlines.Length = (loadForumsLinks ()).Length)
check "AI Starter Pack has the expected number of entries" (aiStarterPackOutlines.Length = (loadAIStarterPackLinks ()).Length)

let followingDocument = XDocument.Load(generatedFollowingPath)
let followingRoot = followingDocument.Root
let followingHead = followingRoot.Element(XName.Get "head")
let followingBody = followingRoot.Element(XName.Get "body")
let followingOutlines = followingBody.Elements(XName.Get "outline") |> Seq.toArray
let followingAttribute (name: string) (outline: XElement) =
    let attribute = outline.Attribute(XName.Get name)
    if isNull attribute then "" else attribute.Value
let expectedFollowingIncludes = FollowingDirectory.getIncludes ()
let expectedFollowingUrls = expectedFollowingIncludes |> Array.map _.Url
let actualFollowingUrls = followingOutlines |> Array.map (followingAttribute "url")
check "Following directory has the expected OPML metadata" (
    followingRoot.Name.LocalName = "opml"
    && followingRoot.Attribute(XName.Get "version").Value = "2.0"
    && followingHead.Element(XName.Get "title").Value = "Luis Quintanilla Following"
    && followingHead.Element(XName.Get "ownerId").Value = "https://lqdev.me"
    && followingHead.Element(XName.Get "docs").Value = "https://lqdev.me/collections/")
check "Following directory contains exactly the configured child OPMLs" (
    followingOutlines.Length = expectedFollowingIncludes.Length
    && (followingOutlines |> Array.map (followingAttribute "text")) = (expectedFollowingIncludes |> Array.map _.Title)
    && actualFollowingUrls = expectedFollowingUrls)
check "Following directory outlines are include-only" (
    followingOutlines
    |> Array.forall (fun outline ->
        followingAttribute "type" outline = "include"
        && not (isNull (outline.Attribute(XName.Get "title")))
        && isNull (outline.Attribute(XName.Get "xmlUrl"))
        && isNull (outline.Attribute(XName.Get "htmlUrl"))
        && (outline.Elements(XName.Get "outline") |> Seq.isEmpty)))
check "Following directory contains no direct RSS outlines" (
    followingRoot.Descendants(XName.Get "outline")
    |> Seq.forall (fun outline -> followingAttribute "type" outline <> "rss"))

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
let specialDirectory =
    OpmlService.buildOpmlDocument
        {
            Title = "Directory & <collection>"
            OwnerId = "https://example.test"
            DocsUrl = Some "https://example.test/docs"
        }
        [| IncludeOutline { Title = "Feeds & Friends"; Url = "https://example.test/feeds.opml" } |]
        |> fun document -> XDocument.Parse(document.ToString())
let specialInclude = specialDirectory.Descendants(XName.Get "outline") |> Seq.exactlyOne
check "OPML include outlines use include-specific attributes" (
    specialInclude.Attribute(XName.Get "text").Value = "Feeds & Friends"
    && specialInclude.Attribute(XName.Get "title").Value = "Feeds & Friends"
    && specialInclude.Attribute(XName.Get "type").Value = "include"
    && specialInclude.Attribute(XName.Get "url").Value = "https://example.test/feeds.opml"
    && isNull (specialInclude.Attribute(XName.Get "xmlUrl"))
    && specialDirectory.Root.Element(XName.Get "head").Element(XName.Get "docs").Value = "https://example.test/docs")

printfn "\nvCard output"
let vcard = readUtf8 generatedVCardPath
check "Generated vCard preserves vCard 3.0" (vcard.Contains("VERSION:3.0", StringComparison.Ordinal))
check "Generated vCard has a formatted name" (vcard.Contains("FN:Luis Quintanilla", StringComparison.Ordinal))
check "Generated vCard preserves its source fields" (
    Convert.ToBase64String(File.ReadAllBytes(Path.Combine(projectRoot, "_src", "vcard.vcf"))) = Convert.ToBase64String(File.ReadAllBytes(generatedVCardPath)))

printfn "\nProfile manifest"
let profileText = readUtf8 generatedProfilePath
let profile = JsonDocument.Parse(profileText).RootElement
let profileSchemaText = readUtf8 generatedProfileSchemaPath
let profileSchema = JsonDocument.Parse(profileSchemaText).RootElement
let profileSchemaId = profileSchema.GetProperty("$id").GetString()
let profileSchemaDefs = profileSchema.GetProperty("$defs")
let profileSchemaProfile = profileSchemaDefs.GetProperty("profile")
let profileSchemaFeeds = profileSchema.GetProperty("properties").GetProperty("feeds")
let profileSchemaServices = profileSchema.GetProperty("properties").GetProperty("services")
let hasJsonProperty (element: JsonElement) (name: string) =
    let mutable value = Unchecked.defaultof<JsonElement>
    element.TryGetProperty(name, &value)
let requiredProperties (schemaObject: JsonElement) =
    schemaObject.GetProperty("required").EnumerateArray()
    |> Seq.map _.GetString()
    |> Seq.toArray
let checkRequiredProperties (name: string) (instance: JsonElement) (schemaObject: JsonElement) =
    let required = requiredProperties schemaObject
    check (name + " declares all required properties") (
        required |> Array.forall (hasJsonProperty instance))
check "Profile schema is published with a stable identifier" (
    profileSchemaId = "https://lqdev.me/.well-known/profile.schema.v1.json"
    && (profileSchema.GetProperty("$schema").GetString()) = "https://json-schema.org/draft/2020-12/schema")
check "Profile manifest declares a versioned site schema" (
    (profile.GetProperty("schema").GetString()) = profileSchemaId
    && (profile.GetProperty("version").GetString()) = "1.0")
checkRequiredProperties "Profile manifest" profile profileSchema
let profileObject = profile.GetProperty("profile")
let feedsObject = profile.GetProperty("feeds")
let servicesObject = profile.GetProperty("services")
let followingEntries = profile.GetProperty("following").EnumerateArray() |> Seq.toArray
let followingUrls = followingEntries |> Array.map (fun entry -> entry.GetProperty("url").GetString())
let requiredFollowingUrls =
    set [
        "https://lqdev.me/.well-known/following.opml"
        "https://lqdev.me/collections/blogroll/index.opml"
        "https://lqdev.me/collections/podroll/index.opml"
        "https://lqdev.me/collections/forums/index.opml"
        "https://lqdev.me/collections/youtube/index.opml"
    ]
let actualFollowingUrlSet = followingUrls |> Set.ofArray
checkRequiredProperties "Profile manifest profile object" profileObject profileSchemaProfile
checkRequiredProperties "Profile manifest feeds object" feedsObject profileSchemaFeeds
checkRequiredProperties "Profile manifest services object" servicesObject profileSchemaServices
check "Profile manifest advertises aggregate and direct following OPMLs" (
    Set.isSubset requiredFollowingUrls actualFollowingUrlSet)
check "Profile manifest preserves the AI starter pack entry" (
    profile.GetProperty("starterPacks").EnumerateArray()
    |> Seq.exists (fun entry -> entry.GetProperty("url").GetString() = "https://lqdev.me/collections/starter-packs/ai/index.opml"))
check "Profile schema forbids undeclared root properties" (
    let declared = profileSchema.GetProperty("properties").EnumerateObject() |> Seq.map _.Name |> Set.ofSeq
    profile.EnumerateObject() |> Seq.forall (fun property -> declared.Contains(property.Name)))
check "Profile manifest uses the canonical origin" (
    not (profileText.Contains("https://www.lqdev.me", StringComparison.Ordinal)))
check "Profile manifest has no webmention typo" (
    not (profileText.Contains("webmebtion", StringComparison.Ordinal)))
check "Profile manifest links to standard discovery surfaces" (
    let services = profile.GetProperty("services")
    services.GetProperty("webfinger").GetString() = "https://lqdev.me/.well-known/webfinger"
    && services.GetProperty("activitypub").GetString() = "https://lqdev.me/api/activitypub/actor"
    && services.GetProperty("indieauth").GetString().StartsWith("https://", StringComparison.Ordinal))

printfn "\nPublished feed identifiers"
let unifiedRss = XDocument.Load(generatedUnifiedRssPath)
let firstRssItem = unifiedRss.Descendants(XName.Get "item") |> Seq.head
let firstRssLink = firstRssItem.Element(XName.Get "link").Value
let firstRssGuid = firstRssItem.Element(XName.Get "guid").Value
check "Published RSS links use the canonical origin" (
    firstRssLink.StartsWith("https://lqdev.me/", StringComparison.Ordinal))
check "Published RSS GUIDs retain the historical origin" (
    firstRssGuid.StartsWith("https://www.lqdev.me/", StringComparison.Ordinal))
let unifiedJsonFeed = JsonDocument.Parse(readUtf8 generatedUnifiedJsonFeedPath).RootElement
let firstJsonItem = unifiedJsonFeed.GetProperty("items").EnumerateArray() |> Seq.head
check "JSON Feed item IDs retain the historical origin" (
    firstJsonItem.GetProperty("id").GetString().StartsWith("https://www.lqdev.me/", StringComparison.Ordinal))
check "JSON Feed item URLs use the canonical origin" (
    firstJsonItem.GetProperty("url").GetString().StartsWith("https://lqdev.me/", StringComparison.Ordinal))

printfn "\nStatic Web Apps configuration"
let config = JsonDocument.Parse(readUtf8 configPath)
let routes = config.RootElement.GetProperty("routes").EnumerateArray() |> Seq.toArray
let routeFor (path: string) =
    routes
    |> Array.find (fun route -> route.GetProperty("route").GetString() = path)
let tryHeader (route: JsonElement) (name: string) =
    let mutable headers = Unchecked.defaultof<JsonElement>
    let mutable value = Unchecked.defaultof<JsonElement>
    if route.TryGetProperty("headers", &headers) && headers.TryGetProperty(name, &value) then
        Some (value.GetString())
    else
        None
let rewrite (route: JsonElement) =
    let mutable value = Unchecked.defaultof<JsonElement>
    if route.TryGetProperty("rewrite", &value) then Some(value.GetString()) else None

let followingRoute = routeFor "/.well-known/following.opml"
let aboutRoute = routeFor "/.well-known/about.md"
let contactRoute = routeFor "/.well-known/contact.vcf"
let profileRoute = routeFor "/.well-known/profile"
let childOpmlPaths =
    [|
        "/collections/blogroll/index.opml"
        "/collections/podroll/index.opml"
        "/collections/youtube/index.opml"
        "/collections/forums/index.opml"
        "/collections/starter-packs/ai/index.opml"
    |]
check "Following route serves the generated directory directly" (rewrite followingRoute |> Option.isNone)
for path in childOpmlPaths do
    let childRoute = routeFor path
    check ("Child OPML route has CORS: " + path) (tryHeader childRoute "Access-Control-Allow-Origin" = Some "*")
check "About route serves the direct generated file" (rewrite aboutRoute |> Option.isNone)
check "Contact route rewrites to the existing vCard" (rewrite contactRoute = Some "/vcard.vcf")
check "Profile route rewrites to the versioned JSON manifest" (rewrite profileRoute = Some "/.well-known/profile.json")
check "Following route has CORS" (tryHeader followingRoute "Access-Control-Allow-Origin" = Some "*")
check "About route has CORS" (tryHeader aboutRoute "Access-Control-Allow-Origin" = Some "*")
check "Contact route has CORS" (tryHeader contactRoute "Access-Control-Allow-Origin" = Some "*")
check "Profile route has CORS" (tryHeader profileRoute "Access-Control-Allow-Origin" = Some "*")
let mimeTypes = config.RootElement.GetProperty("mimeTypes")
check "Static Web Apps maps OPML to text/x-opml" (
    mimeTypes.GetProperty(".opml").GetString() = "text/x-opml")
check "Static Web Apps maps Markdown to text/markdown" (
    mimeTypes.GetProperty(".md").GetString() = "text/markdown")
check "Static Web Apps maps vCard to text/vcard" (
    mimeTypes.GetProperty(".vcf").GetString() = "text/vcard")
check "Static Web Apps maps profile JSON to application/json" (
    mimeTypes.GetProperty(".json").GetString() = "application/json")
let fallback = config.RootElement.GetProperty("navigationFallback")
let excludes = fallback.GetProperty("exclude").EnumerateArray() |> Seq.map _.GetString() |> Seq.toArray
check "Well-known paths bypass homepage fallback" (excludes |> Array.contains "/.well-known/*")
check "Existing WebFinger route remains configured" (routes |> Array.exists (fun route -> route.GetProperty("route").GetString() = "/.well-known/webfinger"))

printfn "\nHTML discovery links"
let homepage = readUtf8 (Path.Combine(projectRoot, "_public", "index.html"))
let aboutHtml = readUtf8 generatedAboutPagePath
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
let hasMeta (html: string) (attribute: string) (key: string) (value: string) =
    let pattern = $"<meta[^>]*{attribute}=\"{Regex.Escape(key)}\"[^>]*content=\"{Regex.Escape(value)}\"[^>]*>"
    Regex.IsMatch(html, pattern, RegexOptions.IgnoreCase)
check "Homepage exposes an explicit h-card identity" (
    homepage.Contains("h-card", StringComparison.Ordinal)
    && homepage.Contains("class=\"p-name microformat-hidden\"", StringComparison.Ordinal)
    && homepage.Contains("class=\"u-url u-uid microformat-hidden\"", StringComparison.Ordinal)
    && homepage.Contains("class=\"u-email microformat-hidden\"", StringComparison.Ordinal)
    && homepage.Contains("class=\"rounded-circle avatar-flip-image u-photo\"", StringComparison.Ordinal))
check "Homepage has canonical Open Graph metadata" (
    hasMeta homepage "property" "og:url" "https://lqdev.me/"
    && hasMeta homepage "property" "og:type" "profile"
    && hasMeta homepage "property" "og:image:alt" "Luis Quintanilla avatar")
check "About page has page-specific Open Graph metadata" (
    hasMeta aboutHtml "property" "og:url" "https://lqdev.me/about/"
    && hasMeta aboutHtml "property" "og:type" "profile"
    && hasMeta aboutHtml "name" "twitter:url" "https://lqdev.me/about/")
let hasCanonicalLink (html: string) (url: string) =
    let pattern = $"<link(?=[^>]*rel=\"canonical\")(?=[^>]*href=\"{Regex.Escape(url)}\")[^>]*>"
    Regex.IsMatch(html, pattern, RegexOptions.IgnoreCase)
check "Homepage emits its canonical link" (hasCanonicalLink homepage "https://lqdev.me/")
check "About page emits its canonical link" (hasCanonicalLink aboutHtml "https://lqdev.me/about/")
let legacyLayoutHtml =
    Layouts.defaultLayout "Legacy caller" "<p>legacy</p>"
    |> RenderView.AsString.htmlDocument
check "Legacy layout callers do not inherit a homepage canonical link" (
    not (legacyLayoutHtml.Contains("rel=\"canonical\"", StringComparison.Ordinal)))

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
            && contentType.MediaType = mediaType)
        check ("Live " + path + " allows cross-origin reads") (
            corsPresent
            && (corsValues |> Seq.contains "*"))
        body

    let liveFollowing = checkLive "/.well-known/following.opml" "text/x-opml" "text/x-opml"
    let liveAbout = checkLive "/.well-known/about.md" "text/markdown" "text/markdown"
    let liveContact = checkLive "/.well-known/contact.vcf" "text/vcard" "text/vcard"
    let liveProfile = checkLive "/.well-known/profile" "application/json" "application/json"
    check "Live following response parses as OPML" (try XDocument.Parse(liveFollowing) |> ignore; true with _ -> false)
    let liveFollowingDocument = XDocument.Parse(liveFollowing)
    let liveFollowingOutlines =
        liveFollowingDocument.Descendants(XName.Get "outline")
        |> Seq.toArray
    let actualLiveFollowingUrls =
        liveFollowingOutlines
        |> Array.map (followingAttribute "url")
        |> Array.sort
    let expectedLiveFollowingUrls = expectedFollowingUrls |> Array.sort
    check "Live following response contains the five include outlines" (
        liveFollowingOutlines.Length = 5
        && (liveFollowingOutlines |> Array.forall (fun outline -> followingAttribute "type" outline = "include"))
        && actualLiveFollowingUrls = expectedLiveFollowingUrls)
    for path in childOpmlPaths do
        let liveChild = checkLive path "text/x-opml" "text/x-opml"
        check ("Live child OPML parses: " + path) (
            try XDocument.Parse(liveChild) |> ignore; true with _ -> false)
    check "Live About response contains summary frontmatter" (liveAbout.Contains("summary:", StringComparison.Ordinal))
    check "Live contact response contains FN" (liveContact.Contains("FN:Luis Quintanilla", StringComparison.Ordinal))
    check "Live profile response parses as JSON" (try JsonDocument.Parse(liveProfile) |> ignore; true with _ -> false)
    check "Live profile response has no typo" (not (liveProfile.Contains("webmebtion", StringComparison.Ordinal)))

    for path in [| "/.well-known/following.opml"; "/.well-known/about.md"; "/.well-known/contact.vcf"; "/.well-known/profile" |] do
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
