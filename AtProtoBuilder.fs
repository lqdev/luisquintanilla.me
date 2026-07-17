/// AT Protocol (ATmosphere) integration — Standard.site documents + native Bluesky posts.
///
/// Mirrors `ActivityPubBuilder.fs`'s type-per-lexicon shape. This module is the Phase 1
/// "Domain Enhancement" layer of issue #2574 / ADR-0009: the record types, static config,
/// content-hash helper, and the deterministic TID record-key derivation. Staging, routing,
/// and the sync script (`Scripts/sync-atproto.fsx`) build on top of these in later phases.
module AtProtoBuilder

open System
open System.Globalization
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open System.Text.RegularExpressions
open Giraffe.ViewEngine

// ---------------------------------------------------------------------------
// Record types (one per lexicon we write)
//
// These are F# DOMAIN MODELS, not System.Text.Json DTOs. The on-the-wire records are built
// explicitly as JsonObject nodes — see `buildDocumentRecordJson` (Phase 2 processor) and
// `Scripts/create-atproto-publication.fsx` (the live Part A bootstrap) — which is what emits the
// lexicon-required "$type" discriminator, uses camelCase wire keys, nests fields correctly (e.g.
// preferences.showInDiscover), and omits absent optionals. Do NOT hand these records to
// `JsonSerializer.Serialize` as-is: they carry F#-friendly names (`Type`, not `$type`) and a flat
// shape that does not match the wire format. Finalizing a single serialization strategy (typed
// DTOs vs. these explicit builders) is tracked in issue #2639.
// ---------------------------------------------------------------------------

/// site.standard.publication — the blog/site itself. Created exactly once during the
/// one-time manual setup (Part A, already live); modelled here for completeness.
type AtProtoPublication =
    { Type: string                 // "site.standard.publication"
      Url: string                  // canonical site URL
      Name: string
      Description: string option
      ShowInDiscover: bool }       // preferences.showInDiscover

/// app.bsky.embed.external card — link preview attached to a native post (Track B).
type AtProtoExternalEmbed =
    { Uri: string
      Title: string
      Description: string }

/// site.standard.document — one record per Post (Track A).
type AtProtoDocument =
    { Type: string                 // "site.standard.document"
      Site: string                 // at:// URI of the publication record
      Title: string
      Path: string                 // "/posts/{slug}/" — must match the real site URL exactly
      Description: string option
      TextContent: string option
      Tags: string list
      PublishedAt: string          // ISO 8601
      UpdatedAt: string option
      SourceHash: string }         // EXTENSION FIELD — change detection + write-scope guard;
                                    // the rkey itself comes from deriveTid (below)

/// app.bsky.feed.post — one record per Note, created only after the activation cutoff (Track B).
type AtProtoPost =
    { Type: string                 // "app.bsky.feed.post"
      Text: string                 // POSSE excerpt, truncated to <=300 graphemes
      CreatedAt: string            // ISO 8601
      Embed: AtProtoExternalEmbed option
      SourceHash: string }         // same change-detection / write-scope role as AtProtoDocument

// ---------------------------------------------------------------------------
// Static configuration
// ---------------------------------------------------------------------------

module Config =
    /// Existing AT Protocol identity — reused, no second identity is minted. Domain-verified via
    /// DNS TXT and hosted on Bluesky's own PDS. NOTE: the PDS endpoint MUST be resolved
    /// dynamically from the DID document (plc.directory) at sync time — Bluesky migrates accounts
    /// between hosts, so hardcoding the "*.host.bsky.network" hostname would silently break.
    let did = "did:plc:pme7qquljcdx6i4zyawoxypd"
    let handle = "lqdev.me"

    /// The site.standard.publication record created once during Part A (see docs/adr/0009).
    let publicationAtUri =
        "at://did:plc:pme7qquljcdx6i4zyawoxypd/site.standard.publication/3mqs7sgylil2w"

    /// Publication metadata — single source of truth in Constants (ADR-0009 refinement).
    let canonicalUrl = Constants.Urls.canonical
    let publicationName = Constants.Site.title
    let publicationDescription = Constants.Pwa.description
    let showInDiscover = true

// ---------------------------------------------------------------------------
// Content hash (mirrors ActivityPubBuilder.generateHash — MD5, machine-stable)
// ---------------------------------------------------------------------------

/// MD5 hex of a stable input string. Used for the `sourceHash` extension field, which both
/// drives skip-if-unchanged checks and scopes writes to records we created.
let generateHash (input: string) : string =
    use md5 = MD5.Create()
    md5.ComputeHash(Encoding.UTF8.GetBytes input)
    |> Array.map (fun b -> b.ToString("x2"))
    |> String.concat ""

// ---------------------------------------------------------------------------
// Deterministic TID record keys
//
// AT Protocol mandates "key": "tid" for these lexicons, so the rkey cannot be a content hash.
// A TID is a 64-bit integer (top bit 0, next 53 bits = microseconds since the UNIX epoch, final
// 10 bits = a clock identifier) encoded as 13 chars of base32-sortable. It is normally
// clock-derived but is client-generatable, so we derive it deterministically from each item's
// published date + a slug hash. That yields spec-valid, rebuild-stable rkeys, which makes
// AT-URIs precomputable at build time and putRecord a stateless idempotent upsert.
// Spec: https://atproto.com/specs/tid
// ---------------------------------------------------------------------------

/// base32-sortable alphabet (no padding).
let private tidAlphabet = "234567abcdefghijklmnopqrstuvwxyz"

/// Encode a 64-bit unsigned integer as a 13-character base32-sortable TID.
let encodeTid (value: uint64) : string =
    let chars = Array.zeroCreate<char> 13
    let mutable v = value
    for i in 12 .. -1 .. 0 do
        chars.[i] <- tidAlphabet.[int (v &&& 31UL)]
        v <- v >>> 5
    String(chars)

/// Machine-stable 64-bit value from a string (first 8 MD5 bytes, big-endian). MD5 is used
/// rather than String.GetHashCode() because GetHashCode is randomized per process and would
/// produce different rkeys on every build.
let private stableHash64 (s: string) : uint64 =
    use md5 = MD5.Create()
    let bytes = md5.ComputeHash(Encoding.UTF8.GetBytes s)
    let mutable acc = 0UL
    for i in 0 .. 7 do
        acc <- (acc <<< 8) ||| uint64 bytes.[i]
    acc

/// Derive a deterministic, spec-valid TID rkey from an item's published date + slug.
/// The published minute anchors the TID (so records sort by publish time); a slug-derived
/// sub-minute microsecond offset plus a 10-bit clock identifier make same-minute items
/// collision-resistant while remaining rebuild-stable.
///
/// The anchor is FLOORED to the minute before the sub-minute offset is added. Source dates carry
/// varying precision (some minute-only, some with seconds), and the offset spans a full minute;
/// without flooring, a post at e.g. :27s plus an up-to-60s offset could bleed past the next minute
/// boundary and invert order relative to a later post. Flooring keeps every derived instant inside
/// its own minute — [minute, minute+60s) — so chronological/string order can never cross-invert
/// between minutes. Within a shared minute the sub-minute position is intentionally hash-ordered
/// (no real sub-minute signal exists to preserve, and the rkey is an identifier, not a sort key).
let deriveTid (publishedDate: DateTimeOffset) (slug: string) : string =
    let ms = publishedDate.ToUnixTimeMilliseconds()
    let minuteMs = if ms < 0L then 0L else (ms / 60_000L) * 60_000L   // floor to minute; clamp pre-epoch
    let epochMicros = uint64 minuteMs * 1000UL
    let h = stableHash64 slug
    let subMinuteMicros = h % 60_000_000UL            // in [0,60s): stays inside the floored minute
    let clockId = (h >>> 20) &&& 1023UL               // 10-bit clock identifier
    let micros53 = (epochMicros + subMinuteMicros) &&& 0x1FFFFFFFFFFFFFUL   // clamp to 53 bits
    let tidInt = (micros53 <<< 10) ||| clockId
    encodeTid tidInt

/// TID syntax: 13 chars, base32-sortable, first char restricted (top bit of the integer is 0).
let private tidRegex =
    Regex(@"^[234567abcdefghij][234567abcdefghijklmnopqrstuvwxyz]{12}$", RegexOptions.Compiled)

/// True when a string is a syntactically valid TID.
let isValidTid (s: string) : bool = not (isNull s) && tidRegex.IsMatch s

/// Build-time invariant: assert no two (date, slug) items derive the same rkey. Phase 2 calls
/// this over the full set of records before any write, so a collision fails the build loudly
/// rather than silently overwriting a record.
let assertNoTidCollisions (items: (DateTimeOffset * string) list) : unit =
    let collisions =
        items
        |> List.map (fun (date, slug) -> deriveTid date slug, (date, slug))
        |> List.groupBy fst
        |> List.choose (fun (tid, group) ->
            let pairs = group |> List.map snd
            if List.length pairs > 1 then Some(tid, pairs) else None)
    if not (List.isEmpty collisions) then
        // Include the source date alongside each slug: the derivation domain is (date, slug), so a
        // diagnostic that named only slugs would be ambiguous when the same slug appears under
        // different dates (or when distinct slugs collide, the dates pinpoint which posts to fix).
        let fmt (date: DateTimeOffset, slug: string) =
            sprintf "%s@%s" slug
                (date.ToString("yyyy-MM-dd HH:mmzzz", System.Globalization.CultureInfo.InvariantCulture))
        let detail =
            collisions
            |> List.map (fun (tid, pairs) ->
                sprintf "%s <- [%s]" tid (pairs |> List.map fmt |> String.concat ", "))
            |> String.concat "; "
        failwithf "AtProtoBuilder.deriveTid produced colliding rkeys: %s" detail

// ---------------------------------------------------------------------------
// Phase 2 — Processor: feature flag, plaintext extraction, document record
// construction, verification <link> tags, and staged-record generation.
// ---------------------------------------------------------------------------

/// Master feature flag for AT Protocol Part B. While false (the committed default), NO staging
/// records are written and NO verification <link> tags are emitted, so generated _public output
/// stays byte-identical to the pre-integration baseline. Flip to true (with the app-password
/// secret wired into CI) to activate document staging + per-post verification tags.
let useAtProtoSync = false

/// Grapheme-safe truncation (AT Proto/lexicon length caps are counted in graphemes, not chars).
let private truncateGraphemes (maxGraphemes: int) (value: string) : string =
    if String.IsNullOrEmpty value then value
    else
        let si = StringInfo value
        if si.LengthInTextElements <= maxGraphemes then value
        else si.SubstringByTextElements(0, maxGraphemes)

/// Best-effort, dependency-free Markdown -> plaintext for the document's `textContent`
/// (the lexicon asks for plaintext with no markdown/formatting).
let stripToPlainText (markdown: string) : string =
    if String.IsNullOrWhiteSpace markdown then ""
    else
        let mutable t = markdown
        t <- Regex.Replace(t, @"```[\s\S]*?```", " ")          // fenced code blocks
        t <- Regex.Replace(t, @"`([^`]*)`", "$1")               // inline code
        t <- Regex.Replace(t, @"!\[([^\]]*)\]\([^)]*\)", "$1")  // images -> alt text
        t <- Regex.Replace(t, @"\[([^\]]*)\]\([^)]*\)", "$1")   // links -> link text
        t <- Regex.Replace(t, @"(?m)^\s{0,3}(#{1,6}|>|[-*+]|\d+\.)\s+", "")  // block markers
        t <- Regex.Replace(t, @"[*_]{1,3}([^*_]+)[*_]{1,3}", "$1")           // emphasis
        t <- Regex.Replace(t, @"<[^>]+>", "")                   // stray HTML tags
        t <- Regex.Replace(t, @"\s+", " ")                       // collapse whitespace
        t.Trim()

/// Canonical `path` for a post's site.standard.document record (leading + trailing slash,
/// matching the real site URL structure). Derives the prefix from `ContentTypes.urlPrefix` — the
/// single authority for permalink prefixes — so the staged `path` can never drift from the actual
/// published URL (Standard.site verification fetches `{publication.url}{path}` and looks for the
/// matching `<link>`, so any drift would silently break verification).
let postPath (slug: string) : string =
    sprintf "%s%s/" (ContentTypes.urlPrefix ContentTypes.ContentType.Posts) slug

/// AT-URI of the site.standard.document record for a given (published date, slug) pair.
/// Deterministic: identical inputs always yield the identical AT-URI, so the verification
/// <link> tag rendered into the page matches the record the sync script writes.
let documentAtUri (published: DateTimeOffset) (slug: string) : string =
    sprintf "at://%s/site.standard.document/%s" Config.did (deriveTid published slug)

/// AT-URI from a frontmatter date string; None when the date can't be parsed (the tag is
/// skipped rather than crashing the build).
let documentAtUriFromDateString (dateStr: string) (slug: string) : string option =
    match DateTimeOffset.TryParse dateStr with
    | true, d -> Some(documentAtUri d slug)
    | _ -> None

/// Per-post <head> nodes: the site.standard.document verification <link> tag. Flag-gated and
/// returns [] when disabled or when the date is unparseable, so callers stay byte-identical
/// with the feature off. Returns Giraffe ViewEngine nodes for injection via the layout.
let documentLinkHead (dateStr: string) (slug: string) : XmlNode list =
    if not useAtProtoSync then []
    else
        match documentAtUriFromDateString dateStr slug with
        | Some atUri -> [ link [ _rel "site.standard.document"; _href atUri ] ]
        | None -> []

/// Build the site.standard.document record JSON for one post. Optional lexicon fields are
/// omitted when empty; `sourceHash` is our extension field (change detection + write-scope guard).
let buildDocumentRecordJson (post: Domain.Post) (published: DateTimeOffset) (slug: string) : JsonObject =
    let o = JsonObject()
    o.Add("$type", JsonValue.Create "site.standard.document")
    o.Add("site", JsonValue.Create Config.publicationAtUri)          // no trailing slash (lexicon)
    o.Add("title", JsonValue.Create(truncateGraphemes 500 post.Metadata.Title))
    o.Add("path", JsonValue.Create(postPath slug))
    if not (String.IsNullOrWhiteSpace post.Metadata.Description) then
        o.Add("description", JsonValue.Create(truncateGraphemes 3000 (post.Metadata.Description.Trim())))
    let text = stripToPlainText post.Content
    if not (String.IsNullOrWhiteSpace text) then
        o.Add("textContent", JsonValue.Create text)
    let tags =
        if isNull post.Metadata.Tags then [||]
        else
            post.Metadata.Tags
            // Normalize through the site's single tag authority so the record's tags match the
            // canonical taxonomy the rest of the site publishes (tag pages, RSS tag feeds): e.g.
            // ".net"->"dotnet", "c#"->"csharp", spaces->hyphens, plural/variant consolidation.
            |> Array.map TagService.processTagName
            // Drop the internal "untagged" sentinel (processTagName returns it for empty input) and
            // any residual blanks — the lexicon's tags field is optional, so omit rather than inject.
            |> Array.filter (fun t -> not (String.IsNullOrWhiteSpace t) && t <> "untagged")
            // Normalization can collapse several distinct frontmatter tags onto one canonical tag
            // (e.g. "machine learning" and "machinelearning" both -> "machinelearning").
            |> Array.distinct
            |> Array.map (truncateGraphemes 128)
    if tags.Length > 0 then
        let arr = JsonArray()
        tags |> Array.iter (fun t -> arr.Add(JsonValue.Create t))
        o.Add("tags", arr)
    o.Add("publishedAt", JsonValue.Create(published.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz", CultureInfo.InvariantCulture)))
    o.Add("sourceHash", JsonValue.Create(generateHash (Config.canonicalUrl + postPath slug + "\u0000" + post.Content)))
    o

/// Generate AT Protocol staging records for Posts (Track A -> site.standard.document).
/// Pure/local: writes one self-describing JSON file (collection + rkey + record) per post under
/// {outputDir}/api/data/atproto/documents/{rkey}.json. The sync script consumes these; nothing
/// here touches the network. Fails the build loudly if two posts would derive the same rkey.
let buildAtProtoStaging (posts: Domain.Post list) (outputDir: string) : unit =
    printfn "  🌐 Generating AT Protocol staging records (site.standard.document)..."
    let docsDir = Path.Combine(outputDir, "api", "data", "atproto", "documents")
    Directory.CreateDirectory docsDir |> ignore
    let dated =
        posts
        |> List.choose (fun p ->
            match DateTimeOffset.TryParse p.Metadata.Date with
            | true, d -> Some(d, p.FileName, p)
            | _ ->
                eprintfn "  ⚠️  AtProto: skipping post with unparseable date '%s' (%s)" p.Metadata.Date p.FileName
                None)
    // Build-time invariant: no two posts may derive the same record key.
    assertNoTidCollisions (dated |> List.map (fun (d, s, _) -> d, s))
    let opts = JsonSerializerOptions()
    opts.WriteIndented <- true
    let mutable count = 0
    for (d, slug, post) in dated do
        let rkey = deriveTid d slug
        let wrapper = JsonObject()
        wrapper.Add("collection", JsonValue.Create "site.standard.document")
        wrapper.Add("rkey", JsonValue.Create rkey)
        wrapper.Add("record", buildDocumentRecordJson post d slug)
        File.WriteAllText(Path.Combine(docsDir, sprintf "%s.json" rkey), wrapper.ToJsonString opts)
        count <- count + 1
    printfn "  ✅ Generated %d AT Protocol document staging records" count
