// Focused checks for link classification, preview bucketing, redirects, and Markdown decoration.
// Run from the project root after `dotnet build -c Release`:
//   dotnet fsi test-scripts/test-link-preview.fsx

#r "../bin/Release/net11.0/PersonalSite.dll"
#r "../bin/Release/net11.0/Markdig.dll"
#r "../bin/Release/net11.0/AngleSharp.dll"

open System
open System.IO
open System.Text.Json
open ASTParsing
open LinkClassification
open LinkRegistry
open MarkdownService
open UnifiedFeeds

let mutable failures = 0
let check name condition =
    if condition then printfn "  PASS  %s" name
    else
        failures <- failures + 1
        printfn "  FAIL  %s" name

printfn "=== Link preview checks ==="

check "normalizes first-party URLs, index.html, query, and fragment" (
    normalizeKey "https://www.luisquintanilla.me/Posts/example/index.html?ref=x#section" = Some "/posts/example")

check "normalizes a route path" (normalizeKey "/Posts/example/?ref=x" = Some "/posts/example")
check "rejects a third-party canonical key" (normalizeKey "https://example.com/posts/example" = None)

let baseUrl = "https://www.luisquintanilla.me/posts/current/"
check "classifies a relative first-party content link" (
    (classify baseUrl "../notes/next/" ).Kind = LinkKind.Internal)
check "classifies a first-party asset separately" (
    (classify baseUrl "/assets/images/photo.webp").Kind = LinkKind.Asset)
check "classifies an external web link" (
    (classify baseUrl "https://example.com/article").Kind = LinkKind.External)
check "classifies mailto and fragment links" (
    (classify baseUrl "mailto:hello@example.com").Kind = LinkKind.Mailto
    && (classify baseUrl "#details").Kind = LinkKind.Anchor)

check "FNV-1a bucket matches the browser fixture" (
    fnv1a "/posts/example" = 3672533564u
    && bucketFor "/posts/example" = 60
    && previewBucketFile "/posts/example" = "/assets/previews/b60.json")

let redirects = Map.ofList [ "/old-path", "/middle-path"; "/middle-path", "/posts/example" ]
check "resolves redirect chains" (
    resolveRedirectPath redirects "/old-path" = "/posts/example")
let cyclicRedirects = Map.ofList [ "/one", "/two"; "/two", "/one" ]
check "stops safely when redirects contain a cycle" (
    resolveRedirectPath cyclicRedirects "/one" = "/one")

let markdown =
    """[Internal](/posts/example) [External](https://example.com/article)

![Image](https://example.com/photo.jpg) [![Linked image](https://example.com/inner.jpg)](https://example.com/photo-page)"""
let html = convertMdToHtml markdown
check "decorates external text links but not internal links" (
    html.Contains("href=\"https://example.com/article\" class=\"link-external\"")
    && html.Contains("href=\"/posts/example\"")
    && not (html.Contains("href=\"/posts/example\" class=\"link-external\"")))
check "does not decorate image-only links" (
    html.Contains("href=\"https://example.com/photo-page\"")
    && html.Split([| "link-external" |], StringSplitOptions.None).Length - 1 = 1)

let parsedResponse =
    parseResponse
        """---
title: "Parser fixture"
targeturl: "https://example.org/item"
response_type: "reply"
dt_published: "2026-01-01 12:00 -05:00"
tags: []
---

[External response link](https://example.com/resource)"""
check "AST-parsed response links receive external decoration" (
    match parsedResponse with
    | Ok parsed -> parsed.TextContent.Contains("class=\"link-external\"")
    | Error _ -> false)

let parsedBookmark =
    parseBookmark
        """---
title: "Bookmark fixture"
bookmark_of: "https://example.org/item"
description: "A bookmark fixture"
dt_published: "2026-01-01 12:00 -05:00"
dt_updated: ""
tags: []
---

[External bookmark link](https://example.com/resource)"""
check "AST-parsed bookmark links receive external decoration" (
    match parsedBookmark with
    | Ok parsed -> parsed.TextContent.Contains("class=\"link-external\"")
    | Error _ -> false)
let responseCardHtml = renderCardHtmlFromMarkdown "[External card link](https://example.com/resource)"
check "AST-rendered response card links receive external decoration" (
    responseCardHtml.Contains("class=\"link-external\""))

let fixtureItem (url: string) (contentType: string) (content: string) : UnifiedFeedItem =
    { Title = "Link preview fixture"
      Content = content
      Url = url
      Date = "2026-01-01"
      ContentType = contentType
      Tags = [||]
      RssXml = System.Xml.Linq.XElement(System.Xml.Linq.XName.Get("item"))
      ResponseType = None
      TargetUrl = None
      UpdatedDate = None
      RsvpStatus = None
      ReviewData = None
      MediaData = None
      BodyHtml = lazy "" }

let tempOutput =
    Path.Combine(Path.GetTempPath(), "link-preview-test-" + Guid.NewGuid().ToString("N"))
Directory.CreateDirectory(tempOutput) |> ignore

try
    let casedUrl = Constants.Urls.canonical + "/responses/TinyAgent-function-calling-edge/"
    let casedPagePath = contentPageFilePath tempOutput casedUrl
    check "preserves source slug casing for filesystem lookups" (
        String.Equals(
            Path.GetRelativePath(tempOutput, casedPagePath),
            Path.Combine("responses", "TinyAgent-function-calling-edge", "index.html"),
            StringComparison.Ordinal))

    Directory.CreateDirectory(Path.GetDirectoryName(casedPagePath)) |> ignore
    File.WriteAllText(
        casedPagePath,
        """<article><div class="e-content"><a href="/responses/Missing-Target">Missing</a></div></article>"""
    )
    let casedItem = fixtureItem casedUrl ContentTypes.Responses ""
    let casedPage = scanContentPages tempOutput [ casedItem ] Map.empty |> List.head
    check "scans a mixed-case generated content path" (
        casedPage.Links |> List.exists (fun link -> link.Href = "/responses/Missing-Target"))

    let presentationUrl =
        Constants.Urls.canonical + "/resources/presentations/Link-Preview-Fixture/"
    let presentationPagePath = contentPageFilePath tempOutput presentationUrl
    Directory.CreateDirectory(Path.GetDirectoryName(presentationPagePath)) |> ignore
    File.WriteAllText(
        presentationPagePath,
        """<article><div class="e-content"><ul><li><a href="https://example.com/resource">Resource</a></li></ul></div></article>"""
    )
    let presentationItem = fixtureItem presentationUrl ContentTypes.Presentations ""
    let presentationKey = normalizeKey presentationUrl |> Option.get
    let presentationSource =
        """[Slide link](/responses/TinyAgent-function-calling-edge)

<a href="/responses/Raw-HTML-Slide-Link">Raw HTML slide link</a>"""
    let presentationPage =
        scanContentPages
            tempOutput
            [ presentationItem ]
            (Map.ofList [ (presentationKey, presentationSource) ])
        |> List.head
    check "includes Markdown slide links alongside rendered resource links" (
        (presentationPage.Links
         |> List.exists (fun link -> link.Href = "/responses/TinyAgent-function-calling-edge"))
        && (presentationPage.Links
            |> List.exists (fun link -> link.Href = "/responses/Raw-HTML-Slide-Link"))
        && (presentationPage.Links
            |> List.exists (fun link -> link.Href = "https://example.com/resource")))

    let mediaUrl = Constants.Urls.canonical + "/media/body-authored-fixture/"
    let mediaPagePath = contentPageFilePath tempOutput mediaUrl
    Directory.CreateDirectory(Path.GetDirectoryName(mediaPagePath)) |> ignore
    File.WriteAllText(
        mediaPagePath,
        """<article><div class="e-content"><p class="p-summary">Rendered caption</p><img src="/images/decorative.jpg" alt=""><img src="/images/fixture.jpg" alt="First image alt"><p>Body caption</p></div></article>"""
    )
    let mediaItem = fixtureItem mediaUrl ContentTypes.Media ""
    let mediaPage = scanContentPages tempOutput [ mediaItem ] Map.empty |> List.head
    check "uses body-authored media alt text before summary and caption" (
        mediaPage.SummaryText = "First image alt")

    LinkPreview.buildPreviewBuckets
        tempOutput
        [ mediaPage ]
        Map.empty
        (Map.ofList [ (mediaPage.PathKey, "Legacy media description") ])
    |> ignore
    let mediaBucketPath =
        Path.Combine(tempOutput, "assets", "previews", Path.GetFileName(previewBucketFile mediaPage.PathKey))
    use previewJson = JsonDocument.Parse(File.ReadAllText(mediaBucketPath))
    let previewSummary =
        previewJson.RootElement.GetProperty("entries").GetProperty(mediaPage.PathKey)
            .GetProperty("summary").GetString()
    check "keeps legacy media descriptions ahead of body-authored alt text" (
        previewSummary.Contains("Legacy media description", StringComparison.Ordinal))

    let siteConfiguration = loadSiteConfiguration "staticwebapp.config.json"
    check "header-only presentation route does not hide a missing destination" (
        not (
            isDestinationValid
                tempOutput
                Map.empty
                siteConfiguration
                "/resources/presentations/nonexistent-link-preview-fixture"))
    check "configured API route is accepted without a generated file" (
        isDestinationValid tempOutput Map.empty siteConfiguration "/api/activitypub/outbox")
    check "external redirect route is accepted" (
        isDestinationValid tempOutput Map.empty siteConfiguration "/github")

    let rewriteFile = Path.Combine(tempOutput, "nodeinfo", "2.1.json")
    Directory.CreateDirectory(Path.GetDirectoryName(rewriteFile)) |> ignore
    File.WriteAllText(rewriteFile, "{}")
    check "static rewrite validates its generated destination" (
        isDestinationValid tempOutput Map.empty siteConfiguration "/nodeinfo/2.1")
    check "static rewrite to a missing file remains broken" (
        not (isDestinationValid tempOutput Map.empty siteConfiguration "/nodeinfo/9.9"))
finally
    if Directory.Exists(tempOutput) then
        Directory.Delete(tempOutput, true)

if failures > 0 then
    failwithf "%d link preview check(s) failed." failures
else
    printfn "All link preview checks passed."
