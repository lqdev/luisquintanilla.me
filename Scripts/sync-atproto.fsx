// Sync AT Protocol records (POSSE) — Track A documents + Track B native posts
//
// Reads the staging records generated during the build and upserts them into the site's
// Bluesky-hosted repo via com.atproto.repo.putRecord. The collection is selected with
// --collection (default site.standard.document):
//   * Track A: site.standard.document  <- AtProtoBuilder.buildAtProtoStaging
//              (_public/api/data/atproto/documents/{rkey}.json)  [Posts]
//   * Track B: app.bsky.feed.post       <- AtProtoBuilder.buildAtProtoNotesStaging
//              (_public/api/data/atproto/posts/{rkey}.json)      [Notes]
//
// Response POSSE (#2574 / ADR-0009 amendment) reuses the SAME script with different --dir/--collection:
//   * Bookmark link posts   --dir _public/api/data/atproto/bookmarks --collection app.bsky.feed.post
//   * Reshare link posts    --dir _public/api/data/atproto/reshares  --collection app.bsky.feed.post
//   * Quote posts           --dir _public/api/data/atproto/quotes    --collection app.bsky.feed.post
//   * Reposts               --dir _public/api/data/atproto/reposts   --collection app.bsky.feed.repost
//   Link posts sync exactly like Notes (sourceHash guard). Quote/repost staging carries a `targetRef`
//   sidecar {actor, rkey}; the script resolves it (handle→DID, app.bsky.feed.getPosts) to a real
//   subject strongRef and REFUSES to write any record whose target is unresolved. Reposts carry NO
//   sourceHash and are guarded by their natural subject (URI+CID): created once, never overwritten.
//
// This is the outbound half of #2574 / ADR-0009. It mirrors the shape of
// Scripts/send-webmentions.fsx (a small post-build dotnet-fsi side effect, not an Azure
// Function) and reuses the auth/XRPC pattern proven in Scripts/create-atproto-publication.fsx.
//
// ─── SAFETY MODEL ────────────────────────────────────────────────────────────
//  * DRY RUN BY DEFAULT. Without `--commit`, the script only READS (resolves the PDS,
//    lists existing records, computes and prints the plan) and NEVER authenticates or
//    writes. A live write requires BOTH `--commit` AND the ATPROTO_APP_PASSWORD secret.
//  * COLLECTION-SCOPED. The target collection is a parameter (--collection, default
//    `site.standard.document`). Track A owns its entire collection. Track B writes to the
//    SHARED `app.bsky.feed.post` collection where the ~14 hand-authored posts also live —
//    there the sourceHash write-scope guard below is load-bearing: any remote record WITHOUT
//    our sourceHash marker is classified `left-untouched` and is never updated or deleted, so
//    hand-authored posts are structurally safe even in a shared collection.
//  * CREATE / UPDATE ONLY, NEVER DELETE. Records are upserted by their deterministic TID
//    rkey. Orphaned remote records (rkey no longer produced by a build) are left alone.
//  * IDEMPOTENT. A record whose remote `sourceHash` already matches the staged one is
//    skipped, so re-running is cheap and safe.
//  * SECRET HYGIENE. The app password and session JWT are never printed.
//
// Usage:
//   dotnet fsi Scripts/sync-atproto.fsx                 # dry run (read-only, no secret needed)
//   dotnet fsi Scripts/sync-atproto.fsx --commit        # live upsert (needs ATPROTO_APP_PASSWORD)
//   dotnet fsi Scripts/sync-atproto.fsx --commit --limit 3   # write only the first 3 pending (cautious first run)
//   dotnet fsi Scripts/sync-atproto.fsx --dir <path>    # override staging dir (default _public/...)
//   dotnet fsi Scripts/sync-atproto.fsx --collection app.bsky.feed.post --dir <notes staging dir>
//                                                       # Track B: POSSE Notes as native posts
//   dotnet fsi Scripts/sync-atproto.fsx --collection app.bsky.feed.post --dir <media staging root>
//       --media-kind images|videos                         # Track C: select media phase

open System
open System.IO
open System.Net.Http
open System.Net.Http.Headers
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open System.Threading
#load "../Services/AtProtoMediaValidation.fs"
open AtProtoMediaValidation

// ---- Args ------------------------------------------------------------------
let argv = Environment.GetCommandLineArgs()
let hasFlag name = argv |> Array.exists (fun a -> a.Equals(name, StringComparison.OrdinalIgnoreCase))
let argValue name =
    argv
    |> Array.tryFindIndex (fun a -> a.Equals(name, StringComparison.OrdinalIgnoreCase))
    |> Option.bind (fun i -> if i + 1 < argv.Length then Some argv.[i + 1] else None)

let commit    = hasFlag "--commit"
// Target collection (Track A default). Track B passes `--collection app.bsky.feed.post`. Resolved
// here (not in the config block below) because the default staging dir depends on it.
let collection = argValue "--collection" |> Option.defaultValue "site.standard.document"
// The default staging dir TRACKS the collection, so `--collection app.bsky.feed.post` on its own
// can't silently load Track A document staging and try to write it into the posts collection
// (a footgun). Pass --dir to override.
let defaultStagingDir =
    match collection with
    | "app.bsky.feed.post"   -> Path.Combine("_public", "api", "data", "atproto", "posts")
    | "app.bsky.feed.repost" -> Path.Combine("_public", "api", "data", "atproto", "reposts")
    | _                      -> Path.Combine("_public", "api", "data", "atproto", "documents")
let stagingDir = argValue "--dir" |> Option.defaultValue defaultStagingDir
let mediaKindFilter = argValue "--media-kind" |> Option.map (fun value -> value.Trim().ToLowerInvariant())

match mediaKindFilter with
| None
| Some ("note" | "notes" | "image" | "images" | "gallery" | "galleries" | "video" | "videos") -> ()
| Some invalid ->
    eprintfn "ERROR: unsupported --media-kind '%s'. Expected notes, images, galleries, or videos." invalid
    exit 1

// Optional cap on how many records a single run will write (create+update). Used for a cautious
// first activation: write a small batch, verify end-to-end, then re-run without --limit to backfill.
let limitOpt =
    argValue "--limit"
    |> Option.bind (fun v ->
        match Int32.TryParse v with
        | true, n when n > 0 -> Some n
        | _ -> eprintfn "WARN: ignoring invalid --limit '%s' (expected a positive integer)." v; None)

// ---- Configuration (public, non-secret) — mirrors AtProtoBuilder.Config -----
let handle      = "lqdev.me"
let did         = "did:plc:pme7qquljcdx6i4zyawoxypd"
let pdsFallback = "https://amanita.us-east.host.bsky.network"
// NOTE: `collection` is resolved up in the Args section (the default staging dir depends on it).
// Everything below — listRecords, the write-scope plan, and putRecord — reads that `collection`
// variable, so the script is fully collection-agnostic; only the `validate` param (below) differs
// by collection family.

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
type Staged =
    { Rkey: string
      SourceHash: string option
      Record: JsonNode
      StagingKind: string option
      TargetRef: JsonNode option
      Media: JsonArray option
      MediaKind: string option }

let mediaStagingDirs =
    if collection = "app.bsky.feed.post" && mediaKindFilter.IsSome then
        // A CI artifact may be downloaded as a media root containing images/,
        // galleries/, and videos/. The build's default layout keeps media next
        // to posts/, so support both layouts without making callers rearrange
        // generated artifacts.
        let childMediaDirs =
            [ "images"; "galleries"; "videos" ]
            |> List.map (fun child -> Path.Combine(stagingDir, child))
        let mediaRoot =
            if childMediaDirs |> List.exists Directory.Exists then
                stagingDir
            else
                let parent = Path.GetDirectoryName stagingDir
                if String.IsNullOrEmpty parent then Path.Combine(stagingDir, "media")
                else Path.Combine(parent, "media")
        [ Path.Combine(mediaRoot, "images")
          Path.Combine(mediaRoot, "galleries")
          Path.Combine(mediaRoot, "videos") ]
    else []

let stagingDirs =
    stagingDir :: mediaStagingDirs
    |> List.distinct

if not (stagingDirs |> List.exists Directory.Exists) then
    printfn "No staging directory (%s). Nothing to sync (AtProto staging is off)." stagingDir
    exit 0

let staged =
    stagingDirs
    |> List.collect (fun dir ->
        if Directory.Exists dir then Directory.GetFiles(dir, "*.json") |> Array.toList else [])
    |> List.toArray
    |> Array.map (fun path ->
        let root = JsonNode.Parse(File.ReadAllText path)
        // Cross-check the wrapper's collection against --collection: refuse to write records staged
        // for one collection into another (e.g. document staging + `--collection app.bsky.feed.post`).
        let stagedCollection =
            root.["collection"] |> Option.ofObj |> Option.map (fun n -> n.GetValue<string>()) |> Option.defaultValue ""
        if stagedCollection <> collection then
            eprintfn "ERROR: staged file '%s' is for collection '%s' but --collection is '%s'. Refusing to write into the wrong collection (check --dir/--collection). Aborting." path stagedCollection collection
            exit 1
        let rkey = root.["rkey"].GetValue<string>()
        let record = root.["record"]
        let sourceHash =
            match record.["sourceHash"] |> Option.ofObj |> Option.map (fun n -> n.GetValue<string>()) with
            | Some sh when not (String.IsNullOrWhiteSpace sh) -> Some sh
            | _ ->
                // app.bsky.feed.repost records intentionally carry NO sourceHash: the repost lexicon has
                // no room for our extension field, so reposts are guarded by their natural subject
                // (URI + CID) instead (see the repost plan path below). For EVERY OTHER collection the
                // builder ALWAYS emits sourceHash (change-detection + write-scope guard), so a missing or
                // blank one means a corrupt staging file — fail loudly rather than default to "" (which
                // matches no remote hash and would rewrite every record on every run, masking the corruption).
                if collection = "app.bsky.feed.repost" then None
                else
                    eprintfn "ERROR: staged file '%s' has no non-blank 'sourceHash'. Corrupt staging; aborting without syncing." path
                    exit 1
        // Optional sidecar (quote/repost staging): { actor, rkey, kind } identifying the native post
        // to quote/repost. Resolved to a real subject strongRef (uri + cid) before any write.
        let stagingKind =
            root.["stagingKind"] |> Option.ofObj |> Option.map (fun n -> n.GetValue<string>())
        let targetRef = root.["targetRef"] |> Option.ofObj
        let hasUsableTargetRef =
            match targetRef with
            | Some target ->
                match target.["actor"], target.["rkey"] with
                | actor, rkey when not (isNull actor) && not (isNull rkey) ->
                    not (String.IsNullOrWhiteSpace(actor.GetValue<string>()))
                    && not (String.IsNullOrWhiteSpace(rkey.GetValue<string>()))
                | _ -> false
            | None -> false
        match stagingKind with
        | Some ("quote" | "repost") when not hasUsableTargetRef ->
            eprintfn "ERROR: staged file '%s' is a %s response record without a usable targetRef. Refusing to write an unresolved subject; aborting." path stagingKind.Value
            exit 1
        | _ -> ()
        let media =
            match root.["media"] with
            | :? JsonArray as values -> Some values
            | _ -> None
        let mediaKind =
            root.["mediaKind"] |> Option.ofObj |> Option.map (fun n -> n.GetValue<string>())
        { Rkey = rkey
          SourceHash = sourceHash
          Record = record
          StagingKind = stagingKind
          TargetRef = targetRef
          Media = media
          MediaKind = mediaKind })
    |> Array.filter (fun s ->
        match mediaKindFilter, s.MediaKind with
        | Some "note", None
        | Some "notes", None -> true
        | None, _ -> true
        | Some "image", Some "image"
        | Some "images", Some "image"
        | Some "image", Some "gallery"
        | Some "images", Some "gallery"
        | Some "gallery", Some "gallery"
        | Some "galleries", Some "gallery"
        | Some "video", Some "video"
        | Some "videos", Some "video" -> true
        | Some _, _ -> false)
    |> Array.sortBy (fun s -> s.Rkey)

let duplicateRkeys =
    staged
    |> Array.groupBy (fun s -> s.Rkey)
    |> Array.choose (fun (rkey, records) -> if records.Length > 1 then Some rkey else None)
if duplicateRkeys.Length > 0 then
    eprintfn "ERROR: duplicate staged native rkey(s): %s. Refusing to write colliding content." (String.concat ", " duplicateRkeys)
    exit 1

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

// ---- 2) List existing remote records (no auth) → rkey -> value node ---------
// Paginated. We keep the whole `value` node so the plan can read either the sourceHash
// (posts/documents write-scope guard) or the subject strongRef (reposts, which have no sourceHash).
let fetchRemote () =
    let map = System.Collections.Generic.Dictionary<string, JsonNode>()
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
            map.[rkey] <- recNode.["value"]
        match page.["cursor"] |> Option.ofObj |> Option.map (fun n -> n.GetValue<string>()) with
        | Some c when page.["records"].AsArray().Count > 0 -> cursor <- Some c
        | _ -> more <- false
    map

let remote = fetchRemote ()
printfn "Remote already holds %d %s record(s)." remote.Count collection

// ---- 2b) Resolve quote/repost targets → real subject strongRefs (UNAUTHENTICATED reads) ------
// Quote (embed.record) and repost (subject) staging carries a PLACEHOLDER {uri="",cid=""} plus a
// `targetRef` sidecar {actor, rkey}. Before ANY write, resolve each actor handle→DID and batch-fetch
// the quoted posts (app.bsky.feed.getPosts, ≤25 per call) to obtain their canonical uri + cid, then
// fill the placeholder in-place. This runs in the read-only phase so a dry run surfaces resolution
// problems, and it REFUSES (exit 1) on any unresolved handle or missing post BEFORE writing anything.
let private stringField (node: JsonNode) (name: string) =
    if isNull node then None
    else node.[name] |> Option.ofObj |> Option.map (fun n -> n.GetValue<string>())

let resolveHandleToDid (actor: string) : string option =
    if actor.StartsWith("did:", StringComparison.OrdinalIgnoreCase) then Some actor
    else
        let tryEndpoint host =
            tryGetJson (sprintf "%s/xrpc/com.atproto.identity.resolveHandle?handle=%s" host (Uri.EscapeDataString actor))
            |> Option.bind (fun node -> stringField node "did")
        match tryEndpoint "https://public.api.bsky.app" with
        | Some did -> Some did
        | None -> tryEndpoint "https://bsky.social"   // entryway fallback

let resolveTargets (items: Staged[]) : unit =
    let withTarget = items |> Array.filter (fun s -> s.TargetRef.IsSome)
    if withTarget.Length = 0 then ()
    else
        printfn "Resolving %d quote/repost target(s)…" withTarget.Length
        let actorOf (s: Staged) = s.TargetRef.Value.["actor"].GetValue<string>()
        let rkeyOf  (s: Staged) = s.TargetRef.Value.["rkey"].GetValue<string>()
        // 1) actor (handle or DID) → DID. Refuse any that cannot be resolved.
        let didMap = System.Collections.Generic.Dictionary<string, string>()
        for actor in withTarget |> Array.map actorOf |> Array.distinct do
            match resolveHandleToDid actor with
            | Some did -> didMap.[actor] <- did
            | None ->
                eprintfn "ERROR: could not resolve ATProto actor '%s' to a DID. Refusing to write an unresolved quote/repost. Aborting." actor
                exit 1
        let atUriOf (s: Staged) = sprintf "at://%s/app.bsky.feed.post/%s" didMap.[actorOf s] (rkeyOf s)
        // 2) batch getPosts (≤25 uris/call) → uri → cid.
        let cidMap = System.Collections.Generic.Dictionary<string, string>()
        let uris = withTarget |> Array.map atUriOf |> Array.distinct
        for batch in uris |> Array.chunkBySize 25 do
            let query = batch |> Array.map (fun u -> "uris=" + Uri.EscapeDataString u) |> String.concat "&"
            let page = getJson (sprintf "https://public.api.bsky.app/xrpc/app.bsky.feed.getPosts?%s" query)
            match page.["posts"] with
            | :? JsonArray as posts ->
                for p in posts do
                    match stringField p "uri", stringField p "cid" with
                    | Some u, Some c -> cidMap.[u] <- c
                    | _ -> ()
            | _ -> ()
        // 3) refuse any target the AppView did not return (deleted / not public / bad rkey).
        let missing = uris |> Array.filter (fun u -> not (cidMap.ContainsKey u)) |> Array.distinct
        if missing.Length > 0 then
            eprintfn "ERROR: %d ATProto quote/repost target(s) could not be resolved to a post (deleted, private, or wrong rkey):" missing.Length
            for u in missing do eprintfn "        · %s" u
            eprintfn "       Refusing to write records with an unresolved subject. Aborting before any write."
            exit 1
        // 4) fill the placeholder subject strongRef in-place.
        for s in withTarget do
            let uri = atUriOf s
            let strongRef = JsonObject()
            strongRef.["uri"] <- JsonValue.Create uri
            strongRef.["cid"] <- JsonValue.Create cidMap.[uri]
            match s.StagingKind with
            | Some "quote" ->
                match s.Record.["embed"] with
                | embed when not (isNull embed) && not (isNull embed.["record"]) ->
                    embed.["record"] <- strongRef        // quote post: app.bsky.embed.record
                | _ ->
                    failwithf "staged quote '%s' has no embed.record placeholder; aborting before writes" s.Rkey
            | Some "repost" ->
                if isNull s.Record.["subject"] then
                    failwithf "staged repost '%s' has no subject placeholder; aborting before writes" s.Rkey
                else
                    s.Record.["subject"] <- strongRef    // repost: app.bsky.feed.repost subject
            | _ ->
                // Preserve compatibility with older targetRef wrappers while still requiring a
                // concrete placeholder shape before any write.
                match s.Record.["embed"] with
                | embed when not (isNull embed) && not (isNull embed.["record"]) ->
                    embed.["record"] <- strongRef
                | _ when not (isNull s.Record.["subject"]) ->
                    s.Record.["subject"] <- strongRef
                | _ ->
                    failwithf "staged target '%s' has neither embed.record nor subject placeholder; aborting before writes" s.Rkey

resolveTargets staged

// ---- 3) Compute the plan ----------------------------------------------------
// Two guard families:
//  * sourceHash collections (site.standard.document, app.bsky.feed.post — Posts/Notes/media/bookmark/
//    reshare/quote): CREATE if absent; UPDATE if remote bears OUR sourceHash but it differs; SKIP if it
//    matches; LEAVE untouched if the remote record has NO sourceHash (not ours).
//  * app.bsky.feed.repost: no sourceHash exists, so guard by the natural subject (URI+CID): CREATE if
//    absent; SKIP if a record already exists at our rkey with the SAME subject; LEAVE untouched if a
//    record exists at our rkey with a DIFFERENT/absent subject (never overwrite; reposts aren't updated).
let creates = ResizeArray<Staged>()
let updates = ResizeArray<Staged>()
let unmanaged = ResizeArray<string>()   // remote rkey exists but is not ours to touch → leave alone
let mutable skips = 0

let subjectKey (node: JsonNode) =
    if isNull node then ""
    else (stringField node "uri" |> Option.defaultValue "") + "\u0000" + (stringField node "cid" |> Option.defaultValue "")

if collection = "app.bsky.feed.repost" then
    for s in staged do
        match remote.TryGetValue s.Rkey with
        | true, value ->
            match value.["subject"] with
            | null -> unmanaged.Add s.Rkey                                   // present but no subject → leave
            | remoteSubject ->
                if subjectKey remoteSubject = subjectKey s.Record.["subject"]
                then skips <- skips + 1                                      // same subject → already reposted
                else unmanaged.Add s.Rkey                                    // different subject at our rkey → leave
        | false, _ -> creates.Add s
else
    for s in staged do
        match remote.TryGetValue s.Rkey with
        | true, value ->
            match (stringField value "sourceHash"), s.SourceHash with
            | Some rsh, Some ssh when rsh = ssh -> skips <- skips + 1   // unchanged → skip
            | Some _, Some _ -> updates.Add s                           // ours, changed → update
            | _ -> unmanaged.Add s.Rkey                                 // no marker → not ours → never touch
        | false, _ -> creates.Add s

printfn ""
printfn "PLAN: %d create, %d update, %d unchanged, %d left-untouched (of %d staged)"
    creates.Count updates.Count skips unmanaged.Count staged.Length
if unmanaged.Count > 0 then
    if collection = "app.bsky.feed.repost" then
        printfn "  ⚠️  %d remote record(s) share a staged rkey but have a DIFFERENT/absent subject — leaving them" unmanaged.Count
        printfn "      UNTOUCHED (reposts are guarded by their natural subject URI+CID, never overwritten):"
    else
        printfn "  ⚠️  %d remote record(s) share a staged rkey but carry NO sourceHash marker — leaving them" unmanaged.Count
        printfn "      UNTOUCHED to honour the write-scope invariant (only records bearing our sourceHash are ours):"
    for rk in unmanaged do printfn "        · %s" rk

let plannedAll =
    Seq.append (creates |> Seq.map (fun s -> "CREATE", s)) (updates |> Seq.map (fun s -> "UPDATE", s))
    |> Seq.toArray
if plannedAll.Length = 0 then
    printfn "Nothing to do — remote is already in sync."
    exit 0

// --limit caps how many records this run writes (create+update). Cautious first activation:
// write a small batch, verify, then re-run WITHOUT --limit to backfill the rest — the already
// written records skip idempotently via their matching sourceHash, so nothing is written twice.
let planned =
    match limitOpt with
    | Some n when n < plannedAll.Length ->
        printfn "  ⚠️  --limit %d: writing only the first %d of %d pending record(s) this run." n n plannedAll.Length
        Array.truncate n plannedAll
    | _ -> plannedAll

for (op, s) in planned do
    printfn "  %s %s" (if op = "CREATE" then "+ CREATE" else "~ UPDATE") s.Rkey

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

// ---- 7) Media uploads (only after the plan and auth gates) -------------------
// No media URL is fetched before this point.  The complete upload phase runs
// before the first putRecord, so a failed upload cannot leave a half-written
// native post behind.
let private postBytesRaw (url: string) (bearer: string) (mimeType: string) (bytes: byte[]) =
    use req = new HttpRequestMessage(HttpMethod.Post, url)
    let content = new ByteArrayContent(bytes)
    content.Headers.ContentType <- MediaTypeHeaderValue(mimeType)
    req.Content <- content
    req.Headers.Authorization <- Headers.AuthenticationHeaderValue("Bearer", bearer)
    let resp = http.SendAsync(req).Result
    int resp.StatusCode, resp.Content.ReadAsStringAsync().Result

let private postBytes (url: string) (bearer: string) (mimeType: string) (bytes: byte[]) : JsonNode =
    let statusCode, body = postBytesRaw url bearer mimeType bytes
    if statusCode >= 200 && statusCode < 300 then
        JsonNode.Parse body
    else
        // uploadBlob can report an already-existing blob as an error response.
        // It is safe to reuse that blob, and avoids a needless second upload.
        let errorOpt =
            try Some(JsonNode.Parse body)
            with _ -> None
        match errorOpt with
        | Some error when
            error.["error"] |> Option.ofObj |> Option.map (fun n -> n.GetValue<string>())
            = Some "already_exists"
            && not (isNull error.["blob"]) -> error
        | _ -> failwithf "media upload failed (HTTP %d): %s" statusCode body

let private blobFromResponse (response: JsonNode) =
    match response.["blob"] with
    | null -> failwith "media upload response did not contain a blob"
    | blob -> JsonNode.Parse(blob.ToJsonString())

let private downloadMedia (url: string) =
    if String.IsNullOrWhiteSpace url then failwith "media manifest contains a blank URL"
    try http.GetByteArrayAsync(url).Result
    with ex -> failwithf "could not download media '%s': %s" url ex.Message

let private descriptorValues (staged: Staged) =
    match staged.Media with
    | None -> []
    | Some media ->
        media
        |> Seq.map (fun item ->
            let url = item.["url"].GetValue<string>()
            let mime = item.["mimeType"].GetValue<string>()
            let alt = item.["alt"].GetValue<string>()
            let width = item.["width"].GetValue<int>()
            let height = item.["height"].GetValue<int>()
            url, mime, alt, width, height)
        |> Seq.toList

let private uploadImage (url: string) (mime: string) =
    let bytes = downloadMedia url
    let dimensions = imageDimensions mime bytes
    let response =
        postBytes
            (sprintf "%s/xrpc/com.atproto.repo.uploadBlob" pds)
            accessJwt mime bytes
    blobFromResponse response, dimensions

let private serviceAuthForVideo () =
    // The video service token is minted by the account's PDS.  Its audience is
    // the PDS service DID, and its scope is uploadBlob because the video
    // service stores the processed blob back in that PDS.
    let pdsHost = Uri(pds).Host
    let audience = sprintf "did:web:%s" pdsHost
    let exp = DateTimeOffset.UtcNow.AddMinutes(30.0).ToUnixTimeSeconds()
    let url =
        sprintf "%s/xrpc/com.atproto.server.getServiceAuth?aud=%s&exp=%d&lxm=com.atproto.repo.uploadBlob"
            pds (Uri.EscapeDataString audience) exp
    use req = new HttpRequestMessage(HttpMethod.Get, url)
    req.Headers.Authorization <- Headers.AuthenticationHeaderValue("Bearer", accessJwt)
    JsonNode.Parse(sendReq req)
    |> fun response -> response.["token"].GetValue<string>()

let private videoJobStatus (serviceJwt: string) (jobId: string) =
    let url =
        sprintf "https://video.bsky.app/xrpc/app.bsky.video.getJobStatus?jobId=%s"
            (Uri.EscapeDataString jobId)
    use req = new HttpRequestMessage(HttpMethod.Get, url)
    req.Headers.Authorization <- Headers.AuthenticationHeaderValue("Bearer", serviceJwt)
    JsonNode.Parse(sendReq req)

let private videoStatusField (response: JsonNode) (name: string) =
    let status =
        match response.["jobStatus"] with
        | null -> response
        | node -> node
    match status.[name] with
    | null -> response.[name] |> Option.ofObj
    | value -> Some value

let private videoStatusString (response: JsonNode) (name: string) =
    videoStatusField response name
    |> Option.map (fun value -> value.GetValue<string>())
    |> Option.map (fun value -> value.ToLowerInvariant())

let private uploadVideo (url: string) (mime: string) (name: string) =
    let bytes = downloadMedia url
    validateVideo mime bytes
    let serviceJwt = serviceAuthForVideo ()
    let endpoint =
        sprintf "https://video.bsky.app/xrpc/app.bsky.video.uploadVideo?did=%s&name=%s"
            (Uri.EscapeDataString did) (Uri.EscapeDataString name)
    let initialStatusCode, initialBody = postBytesRaw endpoint serviceJwt mime bytes
    let initial =
        let parsed =
            try Some(JsonNode.Parse initialBody)
            with _ -> None
        match initialStatusCode, parsed with
        | code, Some response when code >= 200 && code < 300 -> response
        | _, Some response when videoStatusString response "error" = Some "already_exists" -> response
        | _, _ -> failwithf "video upload failed (HTTP %d): %s" initialStatusCode initialBody
    match videoStatusField initial "blob", videoStatusField initial "jobId" with
    | Some blob, _ -> JsonNode.Parse(blob.ToJsonString())
    | None, None ->
        let error = videoStatusString initial "error" |> Option.defaultValue "unknown"
        failwithf "video upload response contained neither a blob nor a jobId (error: %s)" error
    | None, Some jobIdNode ->
        let jobId = jobIdNode.GetValue<string>()
        let mutable completed : JsonNode option = None
        let mutable attempt = 0
        while completed.IsNone && attempt < 60 do
            Thread.Sleep 2000
            let status = videoJobStatus serviceJwt jobId
            let state = videoStatusString status "state" |> Option.defaultValue ""
            if state = "completed" || state = "job_state_completed" then
                match videoStatusField status "blob" with
                | Some blob -> completed <- Some(JsonNode.Parse(blob.ToJsonString()))
                | _ -> failwith "video job completed without a blob"
            elif state = "failed" || state = "job_state_failed" || state = "error" then
                let message = videoStatusField status "error" |> Option.map (fun n -> n.ToJsonString()) |> Option.defaultValue "unknown video processing error"
                failwithf "video processing failed for '%s': %s" url message
            attempt <- attempt + 1
        match completed with
        | Some blob -> blob
        | None -> failwithf "video processing timed out for '%s' after 60 polls" url

let private uploadAndAttach (stagedRecord: Staged) =
    let recordCopy = JsonNode.Parse(stagedRecord.Record.ToJsonString())
    match stagedRecord.Media with
    | None -> recordCopy
    | Some media ->
        if media.Count = 0 then failwithf "media staging for rkey %s has no descriptors" stagedRecord.Rkey
        let kind = stagedRecord.MediaKind |> Option.defaultValue ""
        let blobs =
            descriptorValues stagedRecord
            |> List.mapi (fun index (url, mime, _alt, width, height) ->
                if kind = "video" || kind = "videos" then
                    uploadVideo url mime (sprintf "%s-%d" stagedRecord.Rkey index), (width, height)
                else
                    uploadImage url mime)
        let embed = recordCopy.["embed"]
        if kind = "video" || kind = "videos" then
            embed.["video"] <- blobs.Head |> fst
        else
            let images =
                match embed.["images"], embed.["items"] with
                | images, _ when not (isNull images) -> images.AsArray()
                | _, items when not (isNull items) -> items.AsArray()
                | _ -> failwithf "media record %s has no image/gallery items" stagedRecord.Rkey
            for index in 0 .. images.Count - 1 do
                images.[index].["image"] <- blobs.[index] |> fst
                let width, height = blobs.[index] |> snd
                let aspectRatio = JsonObject()
                aspectRatio.["width"] <- JsonValue.Create width
                aspectRatio.["height"] <- JsonValue.Create height
                images.[index].["aspectRatio"] <- aspectRatio
        recordCopy

// ---- 8) Upsert each planned record (putRecord = create-or-update by rkey) ---
// validate:false → custom lexicon stored with validationStatus "unknown" regardless
// of whether the PDS can resolve the site.standard.* schema.
let mutable ok = 0
let prepared =
    planned
    |> Array.map (fun (op, s) -> op, s, uploadAndAttach s)

for (_op, s, recordCopy) in prepared do
    // JsonNode instances have a parent; detach a fresh copy before re-parenting into the body.
    let body = JsonObject()
    body.["repo"]       <- JsonValue.Create did
    body.["collection"] <- JsonValue.Create collection
    body.["rkey"]       <- JsonValue.Create s.Rkey
    // Custom site.standard.* lexicons: the PDS can't resolve the schema, so validate:false stores
    // the record with validationStatus "unknown". Known lexicons (app.bsky.*, e.g. Track B posts):
    // OMIT validate so the PDS validates against the real schema and REJECTS a malformed record at
    // write time (fail-fast) instead of silently failing to index/render on the AppView.
    if collection.StartsWith("site.standard", StringComparison.Ordinal) then
        body.["validate"] <- JsonValue.Create false
    body.["record"]     <- recordCopy
    let result = postJson (sprintf "%s/xrpc/com.atproto.repo.putRecord" pds) (Some accessJwt) body
    let cid = result.["cid"] |> Option.ofObj |> Option.map (fun n -> n.GetValue<string>()) |> Option.defaultValue "?"
    printfn "  ✓ put %s (cid=%s)" s.Rkey cid
    ok <- ok + 1
    Thread.Sleep 150   // gentle spacing; well within PDS write rate limits

printfn ""
printfn "DONE: upserted %d/%d record(s)." ok planned.Length
