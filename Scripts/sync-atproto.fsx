// Sync AT Protocol Documents (Part B Phase 3 — POSSE, Track A)
//
// Reads the `site.standard.document` staging records generated during the build
// (AtProtoBuilder.buildAtProtoStaging -> _public/api/data/atproto/documents/{rkey}.json)
// and upserts them into the site's Bluesky-hosted repo via com.atproto.repo.putRecord.
//
// This is the outbound half of #2574 / ADR-0009 for Posts. It mirrors the shape of
// Scripts/send-webmentions.fsx (a small post-build dotnet-fsi side effect, not an Azure
// Function) and reuses the auth/XRPC pattern proven in Scripts/create-atproto-publication.fsx.
//
// ─── SAFETY MODEL ────────────────────────────────────────────────────────────
//  * DRY RUN BY DEFAULT. Without `--commit`, the script only READS (resolves the PDS,
//    lists existing records, computes and prints the plan) and NEVER authenticates or
//    writes. A live write requires BOTH `--commit` AND the ATPROTO_APP_PASSWORD secret.
//  * COLLECTION-SCOPED. It only ever touches the `site.standard.document` collection.
//    The ~14 hand-authored posts live in `app.bsky.feed.post`, a DIFFERENT collection
//    this script never names — so they are structurally untouchable here. (When Track B
//    later writes to that shared collection, the sourceHash write-scope guard below
//    becomes load-bearing; for Track A the whole collection is ours.)
//  * CREATE / UPDATE ONLY, NEVER DELETE. Records are upserted by their deterministic TID
//    rkey. Orphaned remote records (rkey no longer produced by a build) are left alone.
//  * IDEMPOTENT. A record whose remote `sourceHash` already matches the staged one is
//    skipped, so re-running is cheap and safe.
//  * SECRET HYGIENE. The app password and session JWT are never printed.
//
// Usage:
//   dotnet fsi Scripts/sync-atproto.fsx                 # dry run (read-only, no secret needed)
//   dotnet fsi Scripts/sync-atproto.fsx --commit        # live upsert (needs ATPROTO_APP_PASSWORD)
//   dotnet fsi Scripts/sync-atproto.fsx --dir <path>    # override staging dir (default _public/...)

open System
open System.IO
open System.Net.Http
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open System.Threading

// ---- Args ------------------------------------------------------------------
let argv = Environment.GetCommandLineArgs()
let hasFlag name = argv |> Array.exists (fun a -> a.Equals(name, StringComparison.OrdinalIgnoreCase))
let argValue name =
    argv
    |> Array.tryFindIndex (fun a -> a.Equals(name, StringComparison.OrdinalIgnoreCase))
    |> Option.bind (fun i -> if i + 1 < argv.Length then Some argv.[i + 1] else None)

let commit    = hasFlag "--commit"
let stagingDir = argValue "--dir" |> Option.defaultValue (Path.Combine("_public", "api", "data", "atproto", "documents"))

// ---- Configuration (public, non-secret) — mirrors AtProtoBuilder.Config -----
let handle      = "lqdev.me"
let did         = "did:plc:pme7qquljcdx6i4zyawoxypd"
let pdsFallback = "https://amanita.us-east.host.bsky.network"
let collection  = "site.standard.document"

// ---- HTTP helpers (same shape as create-atproto-publication.fsx) -----------
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

/// Soft GET for the PDS-resolution path: unlike getJson (which hard-EXITS on any non-2xx via sendReq),
/// this returns None on ANY failure — network error, non-2xx, or parse error — so resolvePds's
/// pdsFallback is actually reachable. getJson's `exit 1` calls Environment.Exit, which terminates the
/// process and bypasses resolvePds's try/with, turning a transient plc.directory blip into a whole-sync
/// abort. Only used for the (unauthenticated, read-only) DID-document lookup.
let tryGetJson (url: string) : JsonNode option =
    try
        use req = new HttpRequestMessage(HttpMethod.Get, url)
        let resp = http.SendAsync(req).Result
        if resp.IsSuccessStatusCode then Some(JsonNode.Parse(resp.Content.ReadAsStringAsync().Result))
        else None
    with _ -> None

let postJson (url: string) (bearer: string option) (payload: JsonNode) : JsonNode =
    use req = new HttpRequestMessage(HttpMethod.Post, url)
    req.Content <- new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json")
    bearer |> Option.iter (fun jwt ->
        req.Headers.Authorization <- Headers.AuthenticationHeaderValue("Bearer", jwt))
    JsonNode.Parse(sendReq req)

// ---- 0) Load staged records (this is also the flag-off no-op guard) ---------
// With useAtProtoSync = false the build writes no staging files, so this list is
// empty and the script exits without ever touching the network.
type Staged = { Rkey: string; SourceHash: string; Record: JsonNode }

if not (Directory.Exists stagingDir) then
    printfn "No staging directory (%s). Nothing to sync (AtProto staging is off)." stagingDir
    exit 0

let staged =
    Directory.GetFiles(stagingDir, "*.json")
    |> Array.map (fun path ->
        let root = JsonNode.Parse(File.ReadAllText path)
        let rkey = root.["rkey"].GetValue<string>()
        let record = root.["record"]
        let sourceHash =
            match record.["sourceHash"] |> Option.ofObj |> Option.map (fun n -> n.GetValue<string>()) with
            | Some sh when not (String.IsNullOrWhiteSpace sh) -> sh
            | _ ->
                // The builder ALWAYS emits sourceHash (change-detection + write-scope guard). A missing or
                // blank one means a corrupt staging file — fail loudly rather than default to "" (which
                // matches no remote hash and would rewrite every record on every run, masking the corruption).
                eprintfn "ERROR: staged file '%s' has no non-blank 'sourceHash'. Corrupt staging; aborting without syncing." path
                exit 1
        { Rkey = rkey; SourceHash = sourceHash; Record = record })
    |> Array.sortBy (fun s -> s.Rkey)

printfn "Loaded %d staged %s record(s) from %s" staged.Length collection stagingDir
if staged.Length = 0 then exit 0

// ---- 1) Resolve the PDS endpoint from the DID document (no auth) ------------
let resolvePds () =
    try
        match tryGetJson (sprintf "https://plc.directory/%s" did) with
        | None -> pdsFallback   // plc.directory unreachable/errored → fall back to the last-known host
        | Some doc ->
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

// ---- 2) List existing remote records (no auth) → rkey -> sourceHash ---------
// Paginated. We read the remote sourceHash purely to skip unchanged records.
let fetchRemote () =
    let map = System.Collections.Generic.Dictionary<string, string option>()
    let mutable cursor : string option = None
    let mutable more = true
    while more do
        let url =
            let baseUrl =
                sprintf "%s/xrpc/com.atproto.repo.listRecords?repo=%s&collection=%s&limit=100" pds did collection
            match cursor with Some c -> sprintf "%s&cursor=%s" baseUrl (Uri.EscapeDataString c) | None -> baseUrl
        let page = getJson url
        for recNode in page.["records"].AsArray() do
            // uri looks like at://did/collection/<rkey> — take the last path segment.
            let uri = recNode.["uri"].GetValue<string>()
            let rkey = uri.Substring(uri.LastIndexOf('/') + 1)
            let value = recNode.["value"]
            let sh =
                value.["sourceHash"] |> Option.ofObj
                |> Option.map (fun n -> n.GetValue<string>())
            map.[rkey] <- sh
        match page.["cursor"] |> Option.ofObj |> Option.map (fun n -> n.GetValue<string>()) with
        | Some c when page.["records"].AsArray().Count > 0 -> cursor <- Some c
        | _ -> more <- false
    map

let remote = fetchRemote ()
printfn "Remote already holds %d %s record(s)." remote.Count collection

// ---- 3) Compute the plan ----------------------------------------------------
// CREATE = rkey absent remotely.
// UPDATE = rkey present AND the remote record carries OUR sourceHash but it differs from the staged one.
// SKIP   = rkey present AND remote sourceHash matches the staged one (idempotent no-op).
// LEAVE  = rkey present but the remote record has NO sourceHash → it does not bear our write-scope
//          marker, so we never touch it (honours the "only touch records we created" invariant).
let creates = ResizeArray<Staged>()
let updates = ResizeArray<Staged>()
let unmanaged = ResizeArray<string>()   // remote rkey exists but lacks our sourceHash marker → leave alone
let mutable skips = 0
for s in staged do
    match remote.TryGetValue s.Rkey with
    | true, remoteSh ->
        match remoteSh with
        | Some rsh when rsh = s.SourceHash -> skips <- skips + 1   // unchanged → skip
        | Some _ -> updates.Add s                                  // ours, changed → update
        | None -> unmanaged.Add s.Rkey                             // no marker → not ours → never touch
    | false, _ -> creates.Add s

printfn ""
printfn "PLAN: %d create, %d update, %d unchanged, %d left-untouched (of %d staged)"
    creates.Count updates.Count skips unmanaged.Count staged.Length
if unmanaged.Count > 0 then
    printfn "  ⚠️  %d remote record(s) share a staged rkey but carry NO sourceHash marker — leaving them" unmanaged.Count
    printfn "      UNTOUCHED to honour the write-scope invariant (only records bearing our sourceHash are ours):"
    for rk in unmanaged do printfn "        · %s" rk

let planned = Seq.append creates updates |> Seq.toArray
if planned.Length = 0 then
    printfn "Nothing to do — remote is already in sync."
    exit 0

for s in creates do printfn "  + CREATE %s" s.Rkey
for s in updates do printfn "  ~ UPDATE %s" s.Rkey

// ---- 4) Dry-run gate --------------------------------------------------------
if not commit then
    printfn ""
    printfn "DRY RUN — no records written. Re-run with --commit (and ATPROTO_APP_PASSWORD set) to apply."
    exit 0

// ---- 5) Read the app password ONLY when actually writing --------------------
let appPassword =
    match Environment.GetEnvironmentVariable "ATPROTO_APP_PASSWORD" with
    | null | "" ->
        eprintfn "ERROR: --commit was given but ATPROTO_APP_PASSWORD is not set. Aborting without writing."
        eprintfn "       In CI this comes from the ATPROTO_APP_PASSWORD repository secret."
        exit 1
    | v -> v.Trim()

// ---- 6) Authenticate --------------------------------------------------------
let session =
    let payload = JsonObject()
    payload.["identifier"] <- JsonValue.Create handle
    payload.["password"]   <- JsonValue.Create appPassword
    postJson (sprintf "%s/xrpc/com.atproto.server.createSession" pds) None payload
let accessJwt = session.["accessJwt"].GetValue<string>()   // NEVER printed

// ---- 7) Upsert each planned record (putRecord = create-or-update by rkey) ---
// validate:false → custom lexicon stored with validationStatus "unknown" regardless
// of whether the PDS can resolve the site.standard.* schema.
let mutable ok = 0
for s in planned do
    // JsonNode instances have a parent; detach a fresh copy before re-parenting into the body.
    let recordCopy = JsonNode.Parse(s.Record.ToJsonString())
    let body = JsonObject()
    body.["repo"]       <- JsonValue.Create did
    body.["collection"] <- JsonValue.Create collection
    body.["rkey"]       <- JsonValue.Create s.Rkey
    body.["validate"]   <- JsonValue.Create false
    body.["record"]     <- recordCopy
    let result = postJson (sprintf "%s/xrpc/com.atproto.repo.putRecord" pds) (Some accessJwt) body
    let cid = result.["cid"] |> Option.ofObj |> Option.map (fun n -> n.GetValue<string>()) |> Option.defaultValue "?"
    printfn "  ✓ put %s (cid=%s)" s.Rkey cid
    ok <- ok + 1
    Thread.Sleep 150   // gentle spacing; well within PDS write rate limits

printfn ""
printfn "DONE: upserted %d/%d record(s)." ok planned.Length
