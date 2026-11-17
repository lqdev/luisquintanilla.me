module LinkClassification

open System
open System.IO
open System.Text

[<RequireQualifiedAccess>]
type LinkKind =
    | Internal
    | External
    | Mailto
    | Anchor
    | Asset
    | Other

type ClassifiedLink =
    { Kind: LinkKind
      ResolvedUrl: string option
      Host: string option
      Path: string option }

let private siteHosts = Constants.Urls.siteHosts
let private assetExtensions =
    set
        [ ".activitypub"; ".bar"; ".css"; ".csv"; ".gif"; ".gpx"; ".ico"; ".ics"
          ".jpeg"; ".jpg"; ".js"; ".json"; ".m3u"; ".md"; ".mp3"; ".mp4"; ".opml"
          ".pdf"; ".png"; ".svg"; ".txt"; ".vcf"; ".webmanifest"; ".webp"; ".xml"
          ".zip"; ".woff"; ".woff2"; ".ttf"; ".eot" ]

let private isSiteHost (host: string) =
    not (String.IsNullOrWhiteSpace(host))
    && siteHosts.Contains(host.ToLowerInvariant())

let private normalizePath (path: string) =
    if String.IsNullOrWhiteSpace(path) then
        None
    else
        let segments =
            path.Split([| '/' |], StringSplitOptions.RemoveEmptyEntries)
            |> Array.filter (fun segment -> not (segment.Equals("index.html", StringComparison.OrdinalIgnoreCase)))
        if segments.Length = 0 then
            None
        else
            Some("/" + String.Join("/", segments).ToLowerInvariant())

/// Return a canonical, origin-free path key for site URLs and route paths.
let normalizeKey (urlOrPath: string) =
    if String.IsNullOrWhiteSpace(urlOrPath) then
        None
    elif urlOrPath.StartsWith("/", StringComparison.Ordinal) then
        normalizePath (urlOrPath.Split([| '?'; '#' |], 2)[0])
    else
        let mutable uri = Unchecked.defaultof<Uri>
        if Uri.TryCreate(urlOrPath.Trim(), UriKind.Absolute, &uri) && isSiteHost uri.Host then
            normalizePath uri.AbsolutePath
        else
            None

let private isAssetPath (path: string) =
    path.StartsWith("/assets/", StringComparison.OrdinalIgnoreCase)
    || path.StartsWith("/images/", StringComparison.OrdinalIgnoreCase)
    || Set.contains (Path.GetExtension(path).ToLowerInvariant()) assetExtensions

let private classifyUri (uri: Uri) =
    if uri.Scheme.Equals("mailto", StringComparison.OrdinalIgnoreCase) then
        { Kind = LinkKind.Mailto; ResolvedUrl = Some uri.AbsoluteUri; Host = None; Path = None }
    elif uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase)
         || uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) then
        let host = uri.Host.ToLowerInvariant()
        if isSiteHost host then
            let path = normalizePath uri.AbsolutePath
            let kind =
                match path with
                | Some value when isAssetPath value -> LinkKind.Asset
                | _ -> LinkKind.Internal
            { Kind = kind; ResolvedUrl = Some uri.AbsoluteUri; Host = Some host; Path = path }
        else
            { Kind = LinkKind.External; ResolvedUrl = Some uri.AbsoluteUri; Host = Some host; Path = None }
    else
        { Kind = LinkKind.Other; ResolvedUrl = Some uri.AbsoluteUri; Host = Some(uri.Host.ToLowerInvariant()); Path = None }

/// Classify a link relative to the page URL that contains it.
let classify (baseUrl: string) (href: string) =
    if String.IsNullOrWhiteSpace(href) then
        { Kind = LinkKind.Other; ResolvedUrl = None; Host = None; Path = None }
    else
        let trimmed = href.Trim()
        if trimmed.StartsWith("#", StringComparison.Ordinal) then
            { Kind = LinkKind.Anchor; ResolvedUrl = Some trimmed; Host = None; Path = None }
        else
            let mutable absoluteUri = Unchecked.defaultof<Uri>
            let isWebAbsoluteUri =
                Uri.TryCreate(trimmed, UriKind.Absolute, &absoluteUri)
                && (absoluteUri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase)
                    || absoluteUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)
                    || absoluteUri.Scheme.Equals("mailto", StringComparison.OrdinalIgnoreCase))
            if isWebAbsoluteUri then
                classifyUri absoluteUri
            else
                let mutable baseUri = Unchecked.defaultof<Uri>
                let mutable resolvedUri = Unchecked.defaultof<Uri>
                let basePageUrl =
                    if baseUrl.EndsWith("/", StringComparison.Ordinal) then baseUrl
                    else baseUrl + "/"
                if Uri.TryCreate(basePageUrl, UriKind.Absolute, &baseUri)
                   && Uri.TryCreate(baseUri, trimmed, &resolvedUri) then
                    classifyUri resolvedUri
                else
                    { Kind = LinkKind.Other; ResolvedUrl = None; Host = None; Path = None }

/// Stable FNV-1a hash shared with the browser implementation.
let fnv1a (value: string) =
    let mutable hash = 2166136261u
    for byteValue in Encoding.UTF8.GetBytes(value) do
        let mixed = hash ^^^ uint32 byteValue
        hash <- uint32 ((uint64 mixed * 16777619UL) &&& 0xFFFFFFFFUL)
    hash

let bucketFor (pathKey: string) =
    int (fnv1a pathKey % uint32 Constants.LinkPreview.bucketCount)

let previewBucketFile (pathKey: string) =
    sprintf "/assets/previews/b%02d.json" (bucketFor pathKey)

let kindName (kind: LinkKind) =
    match kind with
    | LinkKind.Internal -> "Internal"
    | LinkKind.External -> "External"
    | LinkKind.Mailto -> "Mailto"
    | LinkKind.Anchor -> "Anchor"
    | LinkKind.Asset -> "Asset"
    | LinkKind.Other -> "Other"
