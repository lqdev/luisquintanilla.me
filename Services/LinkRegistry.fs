module LinkRegistry

open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open System.Text.RegularExpressions
open AngleSharp.Dom
open AngleSharp.Html.Parser
open UnifiedFeeds

type ScannedLink =
    { Href: string
      Text: string }

type ScannedPage =
    { Item: UnifiedFeedItem
      PathKey: string
      SummaryText: string
      Links: ScannedLink list }

type SiteRoute =
    { Pattern: string
      Redirect: string option
      Rewrite: string option }

type SiteLinkConfiguration =
    { InternalRedirects: Map<string, string>
      Routes: SiteRoute list }

[<CLIMutable>]
type RegistryEdge =
    { Source: string
      SourceType: string
      Href: string
      Resolved: string
      Kind: string
      Host: string
      TargetType: string
      Broken: bool }

[<CLIMutable>]
type RegistryDocument =
    { Generated: string
      EdgeCount: int
      Edges: RegistryEdge list }

type RegistryStats =
    { EdgeCount: int
      BrokenCount: int
      SourcePageCount: int }

let private tryGetString (element: JsonElement) (name: string) =
    let mutable value = Unchecked.defaultof<JsonElement>
    if element.TryGetProperty(name, &value) && value.ValueKind = JsonValueKind.String then
        Some(value.GetString())
    else
        None

let loadSiteConfiguration (configPath: string) =
    if not (File.Exists(configPath)) then
        raise (FileNotFoundException("Static Web Apps configuration is required for link resolution.", configPath))

    use document = JsonDocument.Parse(File.ReadAllText(configPath))
    let routes =
        document.RootElement.GetProperty("routes").EnumerateArray()
        |> Seq.toList

    let configuredRoutes =
        routes
        |> List.choose (fun route ->
            tryGetString route "route"
            |> Option.map (fun pattern ->
                { Pattern = pattern
                  Redirect = tryGetString route "redirect"
                  Rewrite = tryGetString route "rewrite" }))

    let internalRedirects =
        configuredRoutes
        |> List.choose (fun route ->
            match route.Redirect with
            | Some target
                when not (route.Pattern.Contains("*", StringComparison.Ordinal))
                     && not (target.Contains(":splat", StringComparison.Ordinal)) ->
                match LinkClassification.normalizeKey route.Pattern, LinkClassification.normalizeKey target with
                | Some sourceKey, Some targetKey -> Some(sourceKey, targetKey)
                | _ -> None
            | _ -> None)
        |> Map.ofList

    { InternalRedirects = internalRedirects
      Routes = configuredRoutes }

let resolveRedirectPath (redirects: Map<string, string>) (pathKey: string) =
    let rec resolve seen path =
        if Set.contains path seen then
            path
        else
            match Map.tryFind path redirects with
            | Some target -> resolve (Set.add path seen) target
            | None -> path
    resolve Set.empty pathKey

let private rawPathFromUrl (url: string) =
    let mutable uri = Unchecked.defaultof<Uri>
    if Uri.TryCreate(url, UriKind.Absolute, &uri) then
        uri.AbsolutePath
    else
        url.Split([| '?'; '#' |], 2)[0]

let contentPageFilePath (outputDir: string) (url: string) =
    let segments =
        (rawPathFromUrl url).Trim('/')
            .Split([| '/' |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.filter (fun segment ->
            not (segment.Equals("index.html", StringComparison.OrdinalIgnoreCase)))
        |> Array.map Uri.UnescapeDataString
    Path.Combine(Array.concat [ [| outputDir |]; segments; [| "index.html" |] ])

let private contentPagePathsByKey (outputDir: string) (items: UnifiedFeedItem list) =
    items
    |> List.choose (fun item ->
        match LinkClassification.normalizeKey item.Url with
        | Some pathKey -> Some(pathKey, contentPageFilePath outputDir item.Url)
        | None -> None)
    |> Map.ofList

let private scanMarkdownLinks (parser: HtmlParser) (markdown: string) =
    let document = parser.ParseDocument(MarkdownService.convertMdToHtml markdown)
    document.QuerySelectorAll("a[href]")
    |> Seq.map (fun anchor ->
        { Href = anchor.GetAttribute("href")
          Text = anchor.TextContent.Trim() })
    |> Seq.toList

let scanContentPages
    (outputDir: string)
    (items: UnifiedFeedItem list)
    (markdownByPath: Map<string, string>)
    =
    let parser = HtmlParser()

    items
    |> List.choose (fun item ->
        match LinkClassification.normalizeKey item.Url with
        | None -> None
        | Some pathKey ->
            let htmlPath = contentPageFilePath outputDir item.Url
            let pageDocument =
                if File.Exists(htmlPath) then
                    Some(parser.ParseDocument(File.ReadAllText(htmlPath)))
                else
                    None

            let body =
                pageDocument
                |> Option.bind (fun page -> Option.ofObj (page.QuerySelector(".e-content")))

            let explicitSummary =
                pageDocument
                |> Option.bind (fun page -> Option.ofObj (page.QuerySelector(".p-summary")))
                |> Option.map (fun summary -> summary.TextContent)
                |> Option.filter (String.IsNullOrWhiteSpace >> not)

            let htmlLinks =
                body
                |> Option.map (fun content ->
                    content.QuerySelectorAll("a[href]")
                    |> Seq.map (fun anchor ->
                        { Href = anchor.GetAttribute("href")
                          Text = anchor.TextContent.Trim() })
                    |> Seq.toList)
                |> Option.defaultValue []

            let markdownLinks =
                markdownByPath
                |> Map.tryFind pathKey
                |> Option.map (scanMarkdownLinks parser)
                |> Option.defaultValue []

            let mediaImageAlt =
                if item.ContentType <> ContentTypes.Media then
                    None
                else
                    body
                    |> Option.bind (fun content ->
                        content.QuerySelectorAll("img[alt]")
                        |> Seq.tryPick (fun image ->
                            let alt = image.GetAttribute("alt")
                            if String.IsNullOrWhiteSpace(alt) then None else Some alt))

            let summaryText =
                match mediaImageAlt, explicitSummary, body with
                | Some alt, _, _ -> alt
                | None, Some summary, _ -> summary
                | None, None, Some content -> content.TextContent
                | None, None, None -> SearchIndex.ContentProcessor.stripHtml item.Content

            Some
                { Item = item
                  PathKey = pathKey
                  SummaryText = summaryText
                  Links = htmlLinks @ markdownLinks })
    |> List.distinctBy (fun page -> page.PathKey)

let private outputPathExists
    (outputDir: string)
    (contentPathsByKey: Map<string, string>)
    (pathKey: string)
    =
    match Map.tryFind pathKey contentPathsByKey with
    | Some pagePath -> File.Exists(pagePath)
    | None ->
        let pathSegments =
            pathKey.Trim('/')
                .Split([| '/' |], StringSplitOptions.RemoveEmptyEntries)
            |> Array.map Uri.UnescapeDataString
        if pathSegments.Length = 0 then
            File.Exists(Path.Combine(outputDir, "index.html"))
        else
            let target = Path.Combine(Array.append [| outputDir |] pathSegments)
            File.Exists(Path.Combine(target, "index.html"))
            || File.Exists(target + ".html")
            || File.Exists(target)

type private RouteDestination =
    | InternalPath of string
    | ExternalUrl
    | ApiEndpoint

let private normalizeRouteTarget (target: string) =
    let pathPart = target.Split([| '?'; '#' |], 2)[0]
    if pathPart = "/" then
        Some "/"
    else
        LinkClassification.normalizeKey target

let private routeMatch (pattern: string) (pathKey: string) =
    let regex =
        "^"
        + Regex.Escape(pattern.ToLowerInvariant()).Replace("\\*", "(?<splat>.*)")
        + "$"
    let matched = Regex.Match(pathKey, regex, RegexOptions.CultureInvariant)
    if matched.Success then Some matched else None

let private routeDestination (route: SiteRoute) (matched: Match) (pathKey: string) =
    let expandSplat (target: string) =
        let splat = matched.Groups["splat"].Value
        target.Replace(":splat", splat, StringComparison.OrdinalIgnoreCase)

    let destination target =
        let expanded = expandSplat target
        match normalizeRouteTarget expanded with
        | Some targetPath when targetPath.StartsWith("/api/", StringComparison.Ordinal) ->
            Some ApiEndpoint
        | Some targetPath -> Some(InternalPath targetPath)
        | None ->
            let mutable uri = Unchecked.defaultof<Uri>
            if Uri.TryCreate(expanded, UriKind.Absolute, &uri) then Some ExternalUrl
            else None

    match route.Redirect, route.Rewrite with
    | Some target, _ -> destination target
    | None, Some target -> destination target
    | None, None when route.Pattern.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) ->
        Some ApiEndpoint
    | _ -> None

let private tryRouteDestination (configuration: SiteLinkConfiguration) (pathKey: string) =
    configuration.Routes
    |> List.tryPick (fun route ->
        routeMatch route.Pattern pathKey
        |> Option.bind (fun matched -> routeDestination route matched pathKey))

let isDestinationValid
    (outputDir: string)
    (contentPathsByKey: Map<string, string>)
    (configuration: SiteLinkConfiguration)
    (pathKey: string)
    =
    let rec resolve seen path =
        if outputPathExists outputDir contentPathsByKey path then
            true
        elif Set.contains path seen then
            false
        else
            match tryRouteDestination configuration path with
            | Some(InternalPath targetPath) ->
                resolve (Set.add path seen) (resolveRedirectPath configuration.InternalRedirects targetPath)
            | Some ExternalUrl
            | Some ApiEndpoint -> true
            | None -> false
    resolve Set.empty pathKey

let private jsonOptions =
    JsonSerializerOptions(
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    )

let buildRegistry
    (outputDir: string)
    (items: UnifiedFeedItem list)
    (pages: ScannedPage list)
    (configuration: SiteLinkConfiguration)
    =
    let contentPathsByKey = contentPagePathsByKey outputDir items
    let targetTypes =
        items
        |> List.choose (fun item ->
            LinkClassification.normalizeKey item.Url
            |> Option.map (fun key -> key, item.ContentType))
        |> Map.ofList

    let edges =
        pages
        |> List.collect (fun page ->
            page.Links
            |> List.map (fun link ->
                let classified = LinkClassification.classify page.Item.Url link.Href
                let resolvedPath =
                    classified.Path
                    |> Option.map (resolveRedirectPath configuration.InternalRedirects)
                let broken =
                    match classified.Kind, resolvedPath with
                    | LinkClassification.LinkKind.Internal, Some targetPath ->
                        not (isDestinationValid outputDir contentPathsByKey configuration targetPath)
                    | _ -> false
                let resolved =
                    match resolvedPath, classified.ResolvedUrl with
                    | Some path, _ -> path
                    | None, Some url -> url
                    | None, None -> null
                let targetType =
                    resolvedPath
                    |> Option.bind (fun path -> Map.tryFind path targetTypes)
                    |> Option.defaultWith (fun () -> null)
                { Source = page.PathKey
                  SourceType = page.Item.ContentType
                  Href = link.Href
                  Resolved = resolved
                  Kind = LinkClassification.kindName classified.Kind
                  Host = classified.Host |> Option.defaultWith (fun () -> null)
                  TargetType = targetType
                  Broken = broken }))
        |> List.sortBy (fun edge -> edge.Source, edge.Href)

    let brokenEdges = edges |> List.filter (fun edge -> edge.Broken)
    let registry =
        { Generated = DateTimeOffset.UtcNow.ToString("O")
          EdgeCount = edges.Length
          Edges = edges }

    let registryDir = Path.Combine("obj", "links")
    Directory.CreateDirectory(registryDir) |> ignore
    File.WriteAllText(
        Path.Combine(registryDir, "registry.json"),
        JsonSerializer.Serialize(registry, jsonOptions)
    )

    if not brokenEdges.IsEmpty then
        printfn "⚠ %d broken internal link(s); first %d:" brokenEdges.Length (min 20 brokenEdges.Length)
        brokenEdges
        |> List.truncate 20
        |> List.iter (fun edge -> printfn "  %s → %s" edge.Source edge.Href)

    { EdgeCount = edges.Length
      BrokenCount = brokenEdges.Length
      SourcePageCount = pages.Length }
