(*
    Process GitHub Issue Template for Following a Site

    Usage:
      dotnet fsi Scripts/process-follow-issue.fsx -- "feed_url" "site_url" "title" "blogroll|podroll"

    Exit codes:
      0  A single feed was validated and appended.
      1  Invalid input or another hard processing error.
      2  Multiple feeds were found; CANDIDATES_JSON contains the bounded choice list.
      3  No feed was found; no roll file was changed.

    The feed argument may be empty. In that case, the site URL is checked directly
    and its static HTML is searched using the browser extension's RSS/Atom/JSON
    Feed selectors. Common feed paths are then probed.

    Environment:
      FOLLOW_SKIP_FEED_CHECK=1   Skip validation for an explicitly supplied feed (local testing only).
*)

open System
open System.Collections.Generic
open System.IO
open System.Net
open System.Net.Http
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open System.Text.RegularExpressions

let args = fsi.CommandLineArgs |> Array.skip 1

let fail (message: string) =
    printfn "Error: %s" message
    exit 1

if args.Length < 4 then
    printfn "Error: Missing required arguments"
    printfn "Usage: dotnet fsi process-follow-issue.fsx -- \"feed_url\" \"site_url\" \"title\" \"blogroll|podroll\""
    exit 1

let feedInput = args.[0].Trim()
let siteInput = args.[1].Trim()
let title = args.[2].Trim()
let roll = args.[3].Trim().ToLowerInvariant()

let parseAbsoluteHttpUrl (label: string) (input: string) : Uri =
    if String.IsNullOrWhiteSpace(input) then fail $"{label} is required and cannot be empty"
    match Uri.TryCreate(input, UriKind.Absolute) with
    | true, uri when uri.Scheme = Uri.UriSchemeHttp || uri.Scheme = Uri.UriSchemeHttps -> uri
    | _ -> fail $"{label} must be an absolute http(s) URL (got: {input})"

let siteUri = parseAbsoluteHttpUrl "Site URL" siteInput

if String.IsNullOrWhiteSpace(title) then fail "Title is required and cannot be empty"

let rollFile =
    match roll with
    | "blogroll" -> Path.Combine("Data", "blogroll.json")
    | "podroll" -> Path.Combine("Data", "podroll.json")
    | other -> fail $"Roll must be 'blogroll' or 'podroll' (got: {other})"

let siteUrl = siteUri.AbsoluteUri

let normalizeForCompare (url: string) =
    match Uri.TryCreate(url.Trim(), UriKind.Absolute) with
    | true, uri when uri.Scheme = Uri.UriSchemeHttp || uri.Scheme = Uri.UriSchemeHttps ->
        let host =
            if uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) then
                uri.Host.Substring(4)
            else
                uri.Host
        let userInfo = if String.IsNullOrEmpty(uri.UserInfo) then "" else $"{uri.UserInfo}@"
        let port = if uri.IsDefaultPort then "" else $":{uri.Port}"
        let path = uri.AbsolutePath.TrimEnd('/')
        $"{uri.Scheme.ToLowerInvariant()}://{userInfo}{host}{port}{path}{uri.Query}{uri.Fragment}"
    | _ -> url.Trim()

let readRoll (path: string) : JsonArray =
    if not (File.Exists(path)) then fail $"Roll file not found: {path}"
    match JsonNode.Parse(File.ReadAllText(path)) with
    | :? JsonArray as arr -> arr
    | _ -> fail $"Roll file is not a JSON array: {path}"

let entryField (entry: JsonNode) (name: string) =
    match entry.[name] with
    | null -> ""
    | node -> node.GetValue<string>()

let allRolls =
    [ "blogroll", Path.Combine("Data", "blogroll.json")
      "podroll", Path.Combine("Data", "podroll.json") ]
    |> List.map (fun (name, path) -> name, readRoll path)

let siteKey = normalizeForCompare siteUrl

for (rollName, entries) in allRolls do
    for entry in entries do
        let existingSite = entryField entry "HtmlUrl"
        let existingFeed = entryField entry "XmlUrl"
        let added = match entryField entry "Added" with "" -> "date unknown" | d -> $"added {d}"
        let existingTitle = entryField entry "Title"
        if normalizeForCompare existingSite = siteKey then
            fail $"Already following '{existingTitle}' in {rollName} ({added}). Site: {existingSite} Feed: {existingFeed}"

let supportedFeedTypes =
    set [
        "application/rss+xml"
        "application/atom+xml"
        "application/feed+json"
    ]

type FetchResult =
    {
        FinalUri: Uri
        Status: int
        ContentType: string
        Body: string
        Truncated: bool
        Error: string option
    }

type FeedVerdict =
    | ValidFeed of Uri
    | NotFeed
    | UnverifiableFeed of string

let looksLikeFeed (contentType: string) (body: string) =
    let ct = contentType.ToLowerInvariant()
    let trimmed = body.TrimStart()
    let head = trimmed.Substring(0, min 4096 trimmed.Length)
    ct.Contains("rss+xml", StringComparison.Ordinal)
    || ct.Contains("atom+xml", StringComparison.Ordinal)
    || ct.Contains("feed+json", StringComparison.Ordinal)
    || head.Contains("<rss", StringComparison.OrdinalIgnoreCase)
    || head.Contains("<feed", StringComparison.OrdinalIgnoreCase)
    || head.Contains("<rdf:RDF", StringComparison.OrdinalIgnoreCase)
    || (head.StartsWith("{", StringComparison.Ordinal) && head.Contains("jsonfeed.org", StringComparison.OrdinalIgnoreCase))

let fetch (client: HttpClient) (maxBytes: int) (uri: Uri) =
    try
        use cancellation = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(20.0))
        use response = client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellation.Token).GetAwaiter().GetResult()
        let contentType =
            match response.Content.Headers.ContentType with
            | null -> ""
            | ct -> ct.ToString()
        use stream = response.Content.ReadAsStreamAsync(cancellation.Token).GetAwaiter().GetResult()
        let buffer = Array.zeroCreate<byte> (maxBytes + 1)
        let mutable bytesRead = 0
        let mutable reachedEnd = false
        while bytesRead < buffer.Length && not reachedEnd do
            let read = stream.ReadAsync(buffer, bytesRead, buffer.Length - bytesRead, cancellation.Token).GetAwaiter().GetResult()
            if read = 0 then reachedEnd <- true else bytesRead <- bytesRead + read
        {
            FinalUri =
                match response.RequestMessage with
                | null -> uri
                | request ->
                    match request.RequestUri with
                    | null -> uri
                    | finalUri -> finalUri
            Status = int response.StatusCode
            ContentType = contentType
            Body = Encoding.UTF8.GetString(buffer, 0, min maxBytes bytesRead)
            Truncated = bytesRead > maxBytes
            Error = None
        }
    with
    | :? HttpRequestException as ex ->
        { FinalUri = uri; Status = 0; ContentType = ""; Body = ""; Truncated = false; Error = Some $"request failed: {ex.Message}" }
    | :? OperationCanceledException ->
        { FinalUri = uri; Status = 0; ContentType = ""; Body = ""; Truncated = false; Error = Some "request timed out after 20 seconds" }

let isSoftFeedStatus status =
    status = 401 || status = 403 || status = 429 || status >= 500

let validateFeedResult (allowSoftFailure: bool) (result: FetchResult) =
    match result.Error with
    | Some error -> UnverifiableFeed error
    | None when result.Status >= 200 && result.Status < 300 ->
        if looksLikeFeed result.ContentType result.Body then
            printfn "Feed check passed: %d %s %s" result.Status result.ContentType result.FinalUri.AbsoluteUri
            ValidFeed result.FinalUri
        elif result.Truncated then
            UnverifiableFeed $"response was truncated by the bounded read before feed discovery could confirm its content ({result.FinalUri.AbsoluteUri})"
        else
            NotFeed
    | None when result.Status = 404 || result.Status = 410 -> NotFeed
    | None when isSoftFeedStatus result.Status && allowSoftFailure ->
        printfn "Feed check warning: HTTP %d for %s; proceeding because an explicitly supplied feed may block automated requests." result.Status result.FinalUri.AbsoluteUri
        ValidFeed result.FinalUri
    | None when isSoftFeedStatus result.Status ->
        UnverifiableFeed $"returned HTTP {result.Status} ({result.FinalUri.AbsoluteUri})"
    | None -> NotFeed

let getAttribute (name: string) (tag: string) =
    let pattern =
        @"(?:^|\s)"
        + Regex.Escape(name)
        + @"\s*=\s*(?:""(?<double>[^""]*)""|'(?<single>[^']*)'|(?<bare>[^\s""'=<>`]+))"
    let matchResult = Regex.Match(tag, pattern, RegexOptions.IgnoreCase)
    if not matchResult.Success then
        None
    else
        let raw =
            [ "double"; "single"; "bare" ]
            |> List.map (fun group -> matchResult.Groups.[group].Value)
            |> List.tryFind (String.IsNullOrEmpty >> not)
            |> Option.defaultValue ""
        Some(WebUtility.HtmlDecode(raw).Trim())

let parseFeedLinks (documentUri: Uri) (html: string) =
    let searchableHtml =
        Regex.Replace(
            html,
            @"(?is)<!--.*?-->|<(script|style|template)\b[^>]*>.*?</\1>",
            ""
        )
    let tags =
        Regex.Matches(searchableHtml, @"<(?<tag>link|a|base)\b[^>]*>", RegexOptions.IgnoreCase)
        |> Seq.cast<Match>
        |> Seq.toArray

    let baseUri =
        tags
        |> Array.tryPick (fun tag ->
            if tag.Groups.["tag"].Value.Equals("base", StringComparison.OrdinalIgnoreCase) then
                getAttribute "href" tag.Value
                |> Option.bind (fun href ->
                    match Uri.TryCreate(documentUri, href) with
                    | true, uri when uri.Scheme = Uri.UriSchemeHttp || uri.Scheme = Uri.UriSchemeHttps -> Some uri
                    | _ -> None)
            else
                None)
        |> Option.defaultValue documentUri

    tags
    |> Array.choose (fun tag ->
        let tagName = tag.Groups.["tag"].Value.ToLowerInvariant()
        let typeValue =
            getAttribute "type" tag.Value
            |> Option.map _.ToLowerInvariant()
            |> Option.defaultValue ""
        let isFeedType = supportedFeedTypes.Contains(typeValue)
        let hasAlternateRel =
            getAttribute "rel" tag.Value
            |> Option.exists (fun rel ->
                rel.Split([| ' '; '\t'; '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries)
                |> Array.exists (fun value -> value.Equals("alternate", StringComparison.OrdinalIgnoreCase)))
        if not isFeedType || (tagName = "link" && not hasAlternateRel) || (tagName <> "link" && tagName <> "a") then
            None
        else
            getAttribute "href" tag.Value
            |> Option.bind (fun href ->
                match Uri.TryCreate(baseUri, href) with
                | true, uri when uri.Scheme = Uri.UriSchemeHttp || uri.Scheme = Uri.UriSchemeHttps -> Some uri
                | _ -> None))
    |> Array.toList

let pathProbes (documentUri: Uri) =
    [ "/feed"; "/feed/"; "/rss.xml"; "/atom.xml"; "/index.xml"; "/feed.xml"; "/rss"; "/feed.json" ]
    |> List.choose (fun path ->
        match Uri.TryCreate(documentUri, path) with
        | true, uri -> Some uri
        | _ -> None)

let isCommentFeed (uri: Uri) =
    Regex.IsMatch(
        uri.AbsoluteUri,
        @"(?:^|[^a-z])comments?(?:[^a-z]|$)",
        RegexOptions.IgnoreCase
    )

let handler = new HttpClientHandler(AllowAutoRedirect = true, MaxAutomaticRedirections = 5)
let client = new HttpClient(handler, Timeout = System.Threading.Timeout.InfiniteTimeSpan)
client.DefaultRequestHeaders.UserAgent.ParseAdd("lqdev.me-follow-check/1.0 (+https://lqdev.me/collections/blogroll/)")
client.DefaultRequestHeaders.Accept.ParseAdd("application/rss+xml, application/atom+xml, application/feed+json, text/html;q=0.9, application/xml;q=0.8, */*;q=0.5")

let feedReadLimit = 8192
let htmlReadLimit = 1024 * 1024
let maxCandidateChecks = 15

let ensureNotDuplicateFeed (candidate: Uri) =
    let candidateKey = normalizeForCompare candidate.AbsoluteUri
    for (rollName, entries) in allRolls do
        for entry in entries do
            let existingFeed = entryField entry "XmlUrl"
            if normalizeForCompare existingFeed = candidateKey then
                let existingSite = entryField entry "HtmlUrl"
                let existingTitle = entryField entry "Title"
                fail $"Already following '{existingTitle}' in {rollName} with feed {existingFeed}. Site: {existingSite}"

let mutable discoveredFeeds: Uri list = []

if not (String.IsNullOrWhiteSpace(feedInput)) then
    let feedUri = parseAbsoluteHttpUrl "Feed URL" feedInput
    ensureNotDuplicateFeed feedUri
    if Environment.GetEnvironmentVariable("FOLLOW_SKIP_FEED_CHECK") = "1" then
        printfn "Feed check skipped: %s (FOLLOW_SKIP_FEED_CHECK=1)" feedUri.AbsoluteUri
        discoveredFeeds <- [ feedUri ]
    else
        match validateFeedResult true (fetch client feedReadLimit feedUri) with
        | ValidFeed finalUri -> discoveredFeeds <- [ finalUri ]
        | NotFeed -> fail "Feed URL responded successfully but the content does not look like RSS/Atom/JSON Feed."
        | UnverifiableFeed reason -> fail $"Feed URL could not be verified: {reason}."
else
    let siteResult = fetch client htmlReadLimit siteUri
    match validateFeedResult false siteResult with
    | ValidFeed finalUri ->
        discoveredFeeds <- [ finalUri ]
    | UnverifiableFeed reason ->
        fail $"Feed discovery could not be completed for the Site URL: {reason}."
    | NotFeed ->
        let parsed =
            if siteResult.Status >= 200 && siteResult.Status < 300 && siteResult.Error.IsNone then
                parseFeedLinks siteResult.FinalUri siteResult.Body
            else
                []

        let rawCandidates = parsed @ pathProbes siteResult.FinalUri
        let seen = HashSet<string>(StringComparer.Ordinal)
        let deduped =
            rawCandidates
            |> List.filter (fun uri -> seen.Add(uri.AbsoluteUri))

        if deduped.Length > maxCandidateChecks then
            fail $"Feed discovery exceeded its {maxCandidateChecks}-request validation budget after URL deduplication. Supply a specific Feed URL to continue."

        let validatedSeen = HashSet<string>(StringComparer.Ordinal)
        let mutable validated: Uri list = []
        let mutable unverifiableReasons: string list = []
        for candidate in deduped do
            match validateFeedResult false (fetch client feedReadLimit candidate) with
            | ValidFeed finalUri when validatedSeen.Add(finalUri.AbsoluteUri) ->
                validated <- finalUri :: validated
            | ValidFeed _ -> ()
            | NotFeed -> ()
            | UnverifiableFeed reason ->
                unverifiableReasons <- $"{candidate.AbsoluteUri}: {reason}" :: unverifiableReasons

        if validated.IsEmpty && not unverifiableReasons.IsEmpty then
            let details = unverifiableReasons |> List.rev |> String.concat "; "
            fail $"Feed discovery could not be completed because no candidate was verified: {details}."

        discoveredFeeds <-
            validated
            |> List.rev
            |> List.sortBy (fun uri -> if isCommentFeed uri then 1 else 0)
            |> List.truncate maxCandidateChecks

let candidateUrls =
    discoveredFeeds
    |> List.fold (fun (seen, urls) (uri: Uri) ->
        if (seen: HashSet<string>).Add(uri.AbsoluteUri) then seen, uri.AbsoluteUri :: urls
        else seen, urls
    ) (HashSet<string>(StringComparer.Ordinal), [])
    |> snd
    |> List.rev
    |> List.toArray

if candidateUrls.Length = 0 then
    printfn "No feed could be found for %s." siteUrl
    exit 3

if candidateUrls.Length > 1 then
    let candidatesJson = JsonSerializer.Serialize(candidateUrls)
    printfn "Multiple feed candidates found (%d). Choose exactly one before continuing." candidateUrls.Length
    printfn "CANDIDATES_JSON=%s" candidatesJson
    exit 2

let selectedFeedUri = Uri(candidateUrls.[0], UriKind.Absolute)
ensureNotDuplicateFeed selectedFeedUri
let feedUrl = selectedFeedUri.AbsoluteUri

let estTimeZone =
    try TimeZoneInfo.FindSystemTimeZoneById("America/New_York")
    with _ -> TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")

let today = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, estTimeZone).ToString("yyyy-MM-dd")
let entry = JsonObject()
entry.["Title"] <- JsonValue.Create(title)
entry.["Type"] <- JsonValue.Create("rss")
entry.["HtmlUrl"] <- JsonValue.Create(siteUrl)
entry.["XmlUrl"] <- JsonValue.Create(feedUrl)
entry.["Added"] <- JsonValue.Create(today)

let targetRoll = allRolls |> List.find (fun (name, _) -> name = roll) |> snd
targetRoll.Add(entry)

let writeOptions =
    JsonSerializerOptions(
        WriteIndented = true,
        IndentSize = 4,
        Encoder = Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    )
File.WriteAllText(rollFile, targetRoll.ToJsonString(writeOptions) + "\n")

printfn "Follow entry added successfully."
printfn "File: %s" rollFile
printfn "Title: %s" title
printfn "Site: %s" siteUrl
printfn "Feed: %s" feedUrl
printfn "Roll: %s" roll
printfn "Added: %s" today
printfn "Entries in %s: %d" roll targetRoll.Count
printfn "SELECTED_FEED_URL=%s" feedUrl
