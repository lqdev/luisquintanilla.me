// Create AT Protocol Publication Record (ONE-TIME setup)
//
// Part A / step A3 of the AT Protocol integration (issue #2574). Creates the
// singleton `site.standard.publication` record for lqdev.me on its Bluesky-hosted
// PDS. This is what makes the site a discoverable publication in the ATmosphere
// (Standard.site + Bluesky timeline).
//
// SAFE TO RE-RUN: if a publication record already exists, the script prints the
// existing AT-URI and exits WITHOUT creating a duplicate. There is no other undo,
// so the duplicate guard matters.
//
// Auth: reads the Bluesky app password from the ATPROTO_APP_PASSWORD environment
//       variable (in CI this is the ATPROTO_APP_PASSWORD repository secret). The
//       password and the derived session token are NEVER printed.
//
// Output: only non-secret lines. The machine-readable result is the final line:
//       PUBLICATION_URI=at://did:plc:.../site.standard.publication/<rkey>
//
// Usage:  dotnet fsi Scripts/create-atproto-publication.fsx

open System
open System.Net.Http
open System.Text
open System.Text.Json
open System.Text.Json.Nodes

// ---- Configuration (public, non-secret) ------------------------------------
// These mirror Constants.fs; kept literal here so the script is self-contained.
let handle      = "lqdev.me"
let did         = "did:plc:pme7qquljcdx6i4zyawoxypd"
let pdsFallback = "https://amanita.us-east.host.bsky.network"
let collection  = "site.standard.publication"

let publicationUrl         = "https://lqdev.me"                    // Constants.Urls.canonical (NO trailing slash)
let publicationName        = "Luis Quintanilla Personal Website"   // Constants.Site.title
let publicationDescription =                                       // Constants.Pwa.description
    "Personal website and blog by Luis Quintanilla - Software Engineer, "
    + "ML Enthusiast, and Tech Content Creator"

let http = new HttpClient()

let private sendReq (req: HttpRequestMessage) : string =
    let resp = http.SendAsync(req).Result
    let body = resp.Content.ReadAsStringAsync().Result
    if not resp.IsSuccessStatusCode then
        eprintfn "ERROR: %A %s -> HTTP %d" req.Method (req.RequestUri.ToString()) (int resp.StatusCode)
        eprintfn "%s" body   // server error JSON is safe to echo (never contains our secret)
        exit 1
    body

let getJson (url: string) : JsonNode =
    use req = new HttpRequestMessage(HttpMethod.Get, url)
    JsonNode.Parse(sendReq req)

let postJson (url: string) (bearer: string option) (payload: JsonNode) : JsonNode =
    use req = new HttpRequestMessage(HttpMethod.Post, url)
    req.Content <- new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json")
    bearer |> Option.iter (fun jwt ->
        req.Headers.Authorization <- Headers.AuthenticationHeaderValue("Bearer", jwt))
    JsonNode.Parse(sendReq req)

// ---- 1) Resolve the PDS endpoint from the DID document (no auth) ------------
let resolvePds () =
    try
        let doc = getJson (sprintf "https://plc.directory/%s" did)
        doc.["service"].AsArray()
        |> Seq.tryPick (fun s ->
            let isPds =
                s.["type"] |> Option.ofObj
                |> Option.map (fun n -> n.GetValue<string>() = "AtprotoPersonalDataServer")
                |> Option.defaultValue false
            if isPds then
                s.["serviceEndpoint"] |> Option.ofObj |> Option.map (fun n -> n.GetValue<string>())
            else None)
        |> Option.defaultValue pdsFallback
    with _ -> pdsFallback

let pds = resolvePds ()
printfn "PDS=%s" pds

// ---- 2) Duplicate guard: never create a second publication (no auth) --------
let existing =
    getJson (sprintf "%s/xrpc/com.atproto.repo.listRecords?repo=%s&collection=%s" pds did collection)
let existingRecords = existing.["records"].AsArray()
if existingRecords.Count > 0 then
    let uri = existingRecords.[0].["uri"].GetValue<string>()
    printfn "ALREADY_EXISTS: a %s record already exists; NOT creating a duplicate." collection
    printfn "PUBLICATION_URI=%s" uri
    exit 0

// ---- 3) Read the app password ONLY when we are actually going to write ------
let appPassword =
    match Environment.GetEnvironmentVariable "ATPROTO_APP_PASSWORD" with
    | null | "" ->
        eprintfn "ERROR: ATPROTO_APP_PASSWORD is not set. No publication exists yet, so a"
        eprintfn "       write is required, but the app password is unavailable. In CI this"
        eprintfn "       comes from the ATPROTO_APP_PASSWORD repository secret."
        exit 1
    | v -> v.Trim()

// ---- 4) Authenticate (create a session with the app password) --------------
let session =
    let payload = JsonObject()
    payload.["identifier"] <- JsonValue.Create handle
    payload.["password"]   <- JsonValue.Create appPassword
    postJson (sprintf "%s/xrpc/com.atproto.server.createSession" pds) None payload
let accessJwt = session.["accessJwt"].GetValue<string>()   // NEVER printed

// ---- 5) Create the publication record (validate:false) ---------------------
printfn "Creating %s:" collection
printfn "  url         = %s" publicationUrl
printfn "  name        = %s" publicationName
printfn "  description = %s" publicationDescription
printfn "  preferences.showInDiscover = true"

let record = JsonObject()
record.["$type"]       <- JsonValue.Create "site.standard.publication"
record.["url"]         <- JsonValue.Create publicationUrl
record.["name"]        <- JsonValue.Create publicationName
record.["description"] <- JsonValue.Create publicationDescription
record.["createdAt"]   <- JsonValue.Create (DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
let prefs = JsonObject()
prefs.["showInDiscover"] <- JsonValue.Create true
record.["preferences"] <- prefs

let createBody = JsonObject()
createBody.["repo"]       <- JsonValue.Create did
createBody.["collection"] <- JsonValue.Create collection
createBody.["validate"]   <- JsonValue.Create false   // custom lexicon -> store with validationStatus "unknown"
createBody.["record"]     <- record

let result = postJson (sprintf "%s/xrpc/com.atproto.repo.createRecord" pds) (Some accessJwt) createBody
let uri = result.["uri"].GetValue<string>()
let cid = result.["cid"].GetValue<string>()
let validationStatus =
    result.["validationStatus"] |> Option.ofObj
    |> Option.map (fun n -> n.GetValue<string>()) |> Option.defaultValue "(none)"

printfn "CREATED: cid=%s validationStatus=%s" cid validationStatus
printfn "PUBLICATION_URI=%s" uri
