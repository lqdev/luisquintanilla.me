/// AT Protocol (ATmosphere) integration — Standard.site documents + native Bluesky posts.
///
/// Mirrors `ActivityPubBuilder.fs`'s type-per-lexicon shape. This module is the Phase 1
/// "Domain Enhancement" layer of issue #2574 / ADR-0009: the record types, static config,
/// content-hash helper, and the deterministic TID record-key derivation. Staging, routing,
/// and the sync script (`Scripts/sync-atproto.fsx`) build on top of these in later phases.
module AtProtoBuilder

open System
open System.Security.Cryptography
open System.Text
open System.Text.RegularExpressions

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
