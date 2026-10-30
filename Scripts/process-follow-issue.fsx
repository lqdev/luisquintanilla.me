(*
    Process GitHub Issue Template for Following a Site

    Appends an entry to Data/blogroll.json or Data/podroll.json from the "Follow a Site"
    issue form. The issue carries the feed URL, site URL, and title explicitly; this script
    does NOT perform feed autodiscovery. It performs a single defensive GET on the feed URL
    to confirm it is reachable and looks like a feed before writing anything.

    Usage: dotnet fsi process-follow-issue.fsx -- "feed_url" "site_url" "title" "blogroll|podroll"

    Environment:
      FOLLOW_SKIP_FEED_CHECK=1   skip the network check (local testing only)
*)

open System
open System.IO
open System.Net.Http
open System.Threading.Tasks
open System.Text.Json
open System.Text.Json.Nodes

let args = fsi.CommandLineArgs |> Array.skip 1

let fail (message: string) =
    printfn "❌ Error: %s" message
    exit 1

if args.Length < 4 then
    printfn "❌ Error: Missing required arguments"
    printfn "Usage: dotnet fsi process-follow-issue.fsx -- \"feed_url\" \"site_url\" \"title\" \"blogroll|podroll\""
    exit 1

let feedInput = args.[0].Trim()
let siteInput = args.[1].Trim()
let title = args.[2].Trim()
let roll = args.[3].Trim().ToLowerInvariant()

// ---------------------------------------------------------------------------
// Validation
// ---------------------------------------------------------------------------

let parseAbsoluteHttpUrl (label: string) (input: string) : Uri =
    if String.IsNullOrWhiteSpace(input) then fail $"{label} is required and cannot be empty"
    match Uri.TryCreate(input, UriKind.Absolute) with
    | true, uri when uri.Scheme = Uri.UriSchemeHttp || uri.Scheme = Uri.UriSchemeHttps -> uri
    | _ -> fail $"{label} must be an absolute http(s) URL (got: {input})"

let feedUri = parseAbsoluteHttpUrl "Feed URL" feedInput
let siteUri = parseAbsoluteHttpUrl "Site URL" siteInput

if String.IsNullOrWhiteSpace(title) then fail "Title is required and cannot be empty"

let rollFile =
    match roll with
    | "blogroll" -> Path.Combine("Data", "blogroll.json")
    | "podroll" -> Path.Combine("Data", "podroll.json")
    | other -> fail $"Roll must be 'blogroll' or 'podroll' (got: {other})"

// Site URL is the follow target: lowercase host, trailing slash on a bare origin.
// Uri.AbsoluteUri does both without touching path case.
let siteUrl = siteUri.AbsoluteUri
let feedUrl = feedUri.AbsoluteUri

// ---------------------------------------------------------------------------
// Duplicate detection across both rolls
// ---------------------------------------------------------------------------

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
let feedKey = normalizeForCompare feedUrl

for (rollName, entries) in allRolls do
    for entry in entries do
        let existingSite = entryField entry "HtmlUrl"
        let existingFeed = entryField entry "XmlUrl"
        let added = match entryField entry "Added" with "" -> "date unknown" | d -> $"added {d}"
        let existingTitle = entryField entry "Title"
        if normalizeForCompare existingSite = siteKey || normalizeForCompare existingFeed = feedKey then
            fail $"Already following '{existingTitle}' in {rollName} ({added}). Site: {existingSite} Feed: {existingFeed}"

// ---------------------------------------------------------------------------
// Defensive feed check: one GET, is it reachable and does it look like a feed?
// Hard failure: DNS/connection errors, 404/410, or 2xx with non-feed content.
// Soft warning: 401/403/429/5xx (likely bot-blocking or transient) — proceed.
// ---------------------------------------------------------------------------

let looksLikeFeed (contentType: string) (body: string) =
    let ct = contentType.ToLowerInvariant()
    let trimmed = body.TrimStart()
    let head = trimmed.Substring(0, min 4096 trimmed.Length)
    ct.Contains("rss+xml") || ct.Contains("atom+xml") || ct.Contains("feed+json")
    || head.Contains("<rss") || head.Contains("<feed") || head.Contains("<rdf:RDF")
    || (head.StartsWith("{") && head.Contains("jsonfeed.org"))

let checkFeed () =
    if Environment.GetEnvironmentVariable("FOLLOW_SKIP_FEED_CHECK") = "1" then
        printfn "⚠️  Feed check skipped (FOLLOW_SKIP_FEED_CHECK=1)"
    else
        use handler = new HttpClientHandler(AllowAutoRedirect = true, MaxAutomaticRedirections = 5)
        use client = new HttpClient(handler, Timeout = System.Threading.Timeout.InfiniteTimeSpan)
        use cancellation = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(20.0))
        client.DefaultRequestHeaders.UserAgent.ParseAdd("lqdev.me-follow-check/1.0 (+https://lqdev.me/collections/blogroll/)")
        client.DefaultRequestHeaders.Accept.ParseAdd("application/rss+xml, application/atom+xml, application/feed+json, application/xml;q=0.9, */*;q=0.5")
        try
            use response = client.GetAsync(feedUri, HttpCompletionOption.ResponseHeadersRead, cancellation.Token).GetAwaiter().GetResult()
            let status = int response.StatusCode
            let contentType =
                match response.Content.Headers.ContentType with
                | null -> ""
                | ct -> ct.ToString()
            if response.IsSuccessStatusCode then
                use stream = response.Content.ReadAsStreamAsync(cancellation.Token).GetAwaiter().GetResult()
                let buffer = Array.zeroCreate<byte> 8192
                let mutable bytesRead = 0
                let mutable reachedEnd = false
                while bytesRead < buffer.Length && not reachedEnd do
                    let read = stream.ReadAsync(buffer, bytesRead, buffer.Length - bytesRead, cancellation.Token).GetAwaiter().GetResult()
                    if read = 0 then reachedEnd <- true else bytesRead <- bytesRead + read
                let body = Text.Encoding.UTF8.GetString(buffer, 0, bytesRead)
                if looksLikeFeed contentType body then
                    printfn "✅ Feed check: %d %s" status contentType
                else
                    fail $"Feed URL responded {status} but the content does not look like RSS/Atom/JSON Feed (Content-Type: {contentType}). Double-check the Feed URL."
            elif status = 404 || status = 410 then
                fail $"Feed URL returned HTTP {status}. Double-check the Feed URL."
            else
                printfn "⚠️  Feed check: HTTP %d — could not verify feed content (possible bot-blocking or transient error). Proceeding." status
        with
        | :? HttpRequestException as ex -> fail $"Feed URL is not reachable: {ex.Message}"
        | :? OperationCanceledException -> fail "Feed URL timed out after 20 seconds"

checkFeed ()

// ---------------------------------------------------------------------------
// Append entry
// ---------------------------------------------------------------------------

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

let writeOptions = JsonSerializerOptions(WriteIndented = true, IndentSize = 4, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping)
File.WriteAllText(rollFile, targetRoll.ToJsonString(writeOptions) + "\n")

printfn "✅ Follow entry added successfully!"
printfn "📁 File: %s" rollFile
printfn "🏷️  Title: %s" title
printfn "🌐 Site: %s" siteUrl
printfn "📡 Feed: %s" feedUrl
printfn "📚 Roll: %s" roll
printfn "📅 Added: %s" today
printfn "📊 Entries in %s: %d" roll targetRoll.Count
