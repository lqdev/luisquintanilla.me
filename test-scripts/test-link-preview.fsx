// Focused checks for link classification, preview bucketing, redirects, and Markdown decoration.
// Run from the project root after `dotnet build -c Release`:
//   dotnet fsi test-scripts/test-link-preview.fsx

#r "../bin/Release/net11.0/PersonalSite.dll"
#r "../bin/Release/net11.0/Markdig.dll"
#r "../bin/Release/net11.0/AngleSharp.dll"

open System
open LinkClassification
open LinkRegistry
open MarkdownService

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

if failures > 0 then
    failwithf "%d link preview check(s) failed." failures
else
    printfn "All link preview checks passed."
