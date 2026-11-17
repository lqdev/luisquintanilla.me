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

type SiteLinkConfiguration =
    { InternalRedirects: Map<string, string>
      RoutePatterns: string list }

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

    let routePatterns =
        routes
        |> List.choose (fun route -> tryGetString route "route")

    let internalRedirects =
        routes
        |> List.choose (fun route ->
            match tryGetString route "route", tryGetString route "redirect" with
            | Some source, Some target
                when not (source.Contains("*", StringComparison.Ordinal))
                     && not (target.Contains(":splat", StringComparison.Ordinal)) ->
                match LinkClassification.normalizeKey source, LinkClassification.normalizeKey target with
                | Some sourceKey, Some targetKey -> Some(sourceKey, targetKey)
                | _ -> None
            | _ -> None)
        |> Map.ofList

    { InternalRedirects = internalRedirects
      RoutePatterns = routePatterns }

let resolveRedirectPath (redirects: Map<string, string>) (pathKey: string) =
    let rec resolve seen path =
        if Set.contains path seen then
            path
        else
            match Map.tryFind path redirects with
            | Some target -> resolve (Set.add path seen) target
            | None -> path
    resolve Set.empty pathKey

let private pageFilePath (outputDir: string) (pathKey: string) =
    let segments =
        pathKey.Trim('/')
            .Split([| '/' |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.map Uri.UnescapeDataString
    Path.Combine(Array.concat [ [| outputDir |]; segments; [| "index.html" |] ])

let scanContentPages (outputDir: string) (items: UnifiedFeedItem list) =
    let parser = HtmlParser()

    items
    |> List.choose (fun item ->
        match LinkClassification.normalizeKey item.Url with
        | None -> None
        | Some pathKey ->
            let htmlPath = pageFilePath outputDir pathKey
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

            let links =
                body
                |> Option.map (fun content ->
                    content.QuerySelectorAll("a[href]")
                    |> Seq.map (fun anchor ->
                        { Href = anchor.GetAttribute("href")
                          Text = anchor.TextContent.Trim() })
                    |> Seq.toList)
                |> Option.defaultValue []

            let summaryText =
                match explicitSummary, body with
                | Some summary, _ -> summary
                | None, Some content -> content.TextContent
                | None, None -> SearchIndex.ContentProcessor.stripHtml item.Content

            Some
                { Item = item
                  PathKey = pathKey
                  SummaryText = summaryText
                  Links = links })
    |> List.distinctBy (fun page -> page.PathKey)

let private outputPathExists (outputDir: string) (pathKey: string) =
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

let private routeMatches (patterns: string list) (pathKey: string) =
    patterns
    |> List.exists (fun pattern ->
        let regex =
            "^"
            + Regex.Escape(pattern.ToLowerInvariant()).Replace("\\*", ".*")
            + "$"
        Regex.IsMatch(pathKey, regex, RegexOptions.CultureInvariant))

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
                        not (outputPathExists outputDir targetPath)
                        && not (routeMatches configuration.RoutePatterns targetPath)
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
