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
open AtProtoResponseMapping
#if !INTERACTIVE
open Markdig
open CustomBlocks
#endif

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

/// A media descriptor is deliberately independent of rendered HTML.  It is the
/// normalized input used by both the build manifest and the upload step.
type AtProtoMediaDescriptor =
    { Url: string
      MimeType: string
      Alt: string
      Width: int
      Height: int }

type AtProtoRawMediaItem =
    { MediaType: string
      Url: string
      Alt: string
      Caption: string
      Aspect: string
      Width: int option
      Height: int option }

type AtProtoMediaKind =
    | Image
    | Gallery
    | Video

type MediaValidationError =
    | NoMedia
    | BlankMediaUrl
    | UnsupportedMediaType of string
    | MixedImageAndVideo
    | MultipleVideos
    | TooManyImages of int

type AtProtoMediaStaging =
    { Rkey: string
      Kind: AtProtoMediaKind
      Published: DateTimeOffset
      Slug: string
      Descriptors: AtProtoMediaDescriptor list }

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

/// Master feature flag for AT Protocol Track A (Posts -> site.standard.document). When false, NO
/// staging records are written and NO verification <link> tags are emitted, so generated _public
/// output stays byte-identical to the pre-integration baseline; when true (with the app-password
/// secret wired into CI), document staging + per-post verification tags are produced.
let useAtProtoSync = true

/// Track B feature flag — native Bluesky posts for Notes (app.bsky.feed.post). Independent of
/// useAtProtoSync (Track A / documents). When THIS flag (useAtProtoNotesSync) is false, NO note
/// staging records are written, so generated _public output stays byte-identical to the baseline;
/// when it is true (with ATPROTO_APP_PASSWORD wired into CI), post-cutoff Notes are POSSE'd to the
/// real bsky.app timeline.
let useAtProtoNotesSync = true

/// Forward-only activation cutoff for Track B. Only Notes published on/after this instant are
/// POSSE'd. Bluesky feeds sort by ingest time (indexedAt), not createdAt, so a bulk backfill of
/// historical notes would flood followers' timelines — hence forward-only from an explicit cutoff
/// (ADR-0009 / issue #2574). Set to catch the first seed note (lumpen-radio, 2026-07-13) and newer.
let notesActivationCutoff = DateTimeOffset(2026, 7, 13, 0, 0, 0, TimeSpan.FromHours -5.0)

/// Part C rolls out images first.  Image and gallery activation are separate
/// switches so a small image rollout can be enabled without enabling video.
/// Video is intentionally independent because it has a different upload
/// service, quota, and failure mode.
let useAtProtoMediaImageSync = true
let useAtProtoMediaGallerySync = false
let useAtProtoMediaVideoSync = false
let useAtProtoMediaSync = false

/// Explicit forward-only activation cutoffs.  Keep these values in source
/// control: changing a gate must never silently backfill old media into feeds.
let mediaImagesActivationCutoff = DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.FromHours -5.0)
let mediaVideoActivationCutoff = DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.FromHours -5.0)
let mediaActivationCutoff = mediaImagesActivationCutoff

/// Grapheme-safe truncation (AT Proto/lexicon length caps are counted in graphemes, not chars).
let private truncateGraphemes (maxGraphemes: int) (value: string) : string =
    if String.IsNullOrEmpty value then value
    else
        let si = StringInfo value
        if si.LengthInTextElements <= maxGraphemes then value
        else si.SubstringByTextElements(0, maxGraphemes)

// Plural aliases make the rollout intent clear at call sites and keep the
// flags easy to discover for operators.
let useAtProtoMediaImagesSync = useAtProtoMediaImageSync
let useAtProtoMediaVideosSync = useAtProtoMediaVideoSync

let private nonNull (value: string) = if isNull value then "" else value

let private detectMediaMimeType (url: string) =
    match Path.GetExtension(nonNull url).ToLowerInvariant() with
    | ".jpg" | ".jpeg" -> "image/jpeg"
    | ".png" -> "image/png"
    | ".gif" -> "image/gif"
    | ".webp" -> "image/webp"
    | ".avif" -> "image/avif"
    | ".mp4" -> "video/mp4"
    | ".webm" -> "video/webm"
    | ".mov" -> "video/quicktime"
    | _ -> "application/octet-stream"

let private parseAspectRatio (aspect: string) =
    let value = nonNull aspect |> fun s -> s.Trim().ToLowerInvariant()
    let named =
        match value with
        | "landscape" -> Some (16, 9)
        | "portrait" -> Some (9, 16)
        | "square" -> Some (1, 1)
        | _ -> None
    match named with
    | Some dimensions -> dimensions
    | None ->
        let parts = value.Split([| ':'; '/' |], StringSplitOptions.RemoveEmptyEntries)
        if parts.Length = 2 then
            match Int32.TryParse parts.[0], Int32.TryParse parts.[1] with
            | (true, w), (true, h) when w > 0 && h > 0 -> (w, h)
            | _ -> (1, 1)
        else (1, 1)

/// Read the structured `:::media` custom blocks from source markdown.  The
/// compiled generator uses the existing Markdig/custom-block AST.  The
/// standalone FSI branch below mirrors that source grammar so the focused
/// tests can load this file without the complete project compile graph.
#if !INTERACTIVE
let extractMediaItemsFromMarkdown (markdown: string) : AtProtoRawMediaItem list =
    if String.IsNullOrWhiteSpace markdown then []
    else
        let pipeline =
            MarkdownPipelineBuilder()
            |> CustomBlocks.useCustomBlocks
            |> fun builder -> builder.Build()
        let document = Markdown.Parse(markdown, pipeline)
        CustomBlocks.extractCustomBlocks document
        |> List.collect (function
            | CustomBlock.Media items ->
                items
                |> List.map (fun item ->
                    { MediaType = item.media_type
                      Url = item.uri
                      Alt = item.alt_text
                      Caption = item.caption
                      Aspect = item.aspect
                      Width = None
                      Height = None })
            | _ -> [])
#else
let extractMediaItemsFromMarkdown (markdown: string) : AtProtoRawMediaItem list =
    if String.IsNullOrWhiteSpace markdown then []
    else
        let lines = markdown.Replace("\r\n", "\n").Split('\n')
        let result = ResizeArray<AtProtoRawMediaItem>()
        let mutable inside = false
        let mutable current : Map<string, string> = Map.empty
        let valueOf (line: string) =
            let colon = line.IndexOf(':')
            if colon < 0 then None
            else
                let value = line.Substring(colon + 1).Trim().Trim([| '"'; '\'' |])
                Some(line.Substring(0, colon).Trim().ToLowerInvariant(), value)
        let flush () =
            if current.ContainsKey "url" || current.ContainsKey "uri" then
                let get key fallback = current |> Map.tryFind key |> Option.defaultValue fallback
                result.Add {
                    MediaType = get "mediatype" (get "media_type" "")
                    Url = get "url" (get "uri" "")
                    Alt = get "alt" (get "alt_text" "")
                    Caption = get "caption" ""
                    Aspect = get "aspectratio" (get "aspect" "")
                    Width = (match Int32.TryParse(get "width" "") with | true, n when n > 0 -> Some n | _ -> None)
                    Height = (match Int32.TryParse(get "height" "") with | true, n when n > 0 -> Some n | _ -> None) }
            current <- Map.empty
        for line in lines do
            let trimmed = line.Trim()
            if trimmed.Equals(":::media", StringComparison.OrdinalIgnoreCase) then
                if inside then
                    flush ()
                    inside <- false
                else
                    inside <- true
            elif inside && trimmed.StartsWith(":::") then
                flush ()
                inside <- false
            elif inside then
                if trimmed.StartsWith("- ") then
                    flush ()
                    match valueOf (trimmed.Substring(2)) with
                    | Some (key, value) -> current <- current.Add(key, value)
                    | None -> ()
                else
                    match valueOf trimmed with
                    | Some (key, value) -> current <- current.Add(key, value)
                    | None -> ()
        if inside then flush ()
        result |> List.ofSeq
#endif

let private descriptorForMediaItem (title: string) (item: AtProtoRawMediaItem) =
    let url = nonNull item.Url |> fun s -> s.Trim()
    let mime =
        let declared = nonNull item.MediaType |> fun s -> s.Trim().ToLowerInvariant()
        if declared.Contains "/" then declared
        elif declared = "image" then
            let detected = detectMediaMimeType url
            if detected.StartsWith("image/", StringComparison.Ordinal) then detected else "image/jpeg"
        elif declared = "video" then
            let detected = detectMediaMimeType url
            if detected.StartsWith("video/", StringComparison.Ordinal) then detected else "video/mp4"
        else detectMediaMimeType url
    let alt =
        [ nonNull item.Alt; nonNull item.Caption; nonNull title; "Media" ]
        |> List.map (fun s -> s.Trim())
        |> List.tryFind (String.IsNullOrWhiteSpace >> not)
        |> Option.defaultValue "Media"
    let aspectWidth, aspectHeight = parseAspectRatio item.Aspect
    let width = item.Width |> Option.defaultValue aspectWidth
    let height = item.Height |> Option.defaultValue aspectHeight
    { Url = url; MimeType = mime; Alt = truncateGraphemes 1000 alt; Width = width; Height = height }

let normalizeMediaDescriptors (title: string) (items: AtProtoRawMediaItem list) =
    items
    |> List.map (descriptorForMediaItem title)

let extractMediaDescriptorsFromMarkdown (title: string) (markdown: string) =
    extractMediaItemsFromMarkdown markdown |> normalizeMediaDescriptors title

let validateMediaDescriptors (descriptors: AtProtoMediaDescriptor list) : Result<AtProtoMediaKind, MediaValidationError> =
    if List.isEmpty descriptors then Error NoMedia
    else
        if descriptors |> List.exists (fun d -> String.IsNullOrWhiteSpace d.Url) then
            Error BlankMediaUrl
        else
            let mimeOf (descriptor: AtProtoMediaDescriptor) =
                if isNull descriptor.MimeType then "" else descriptor.MimeType.ToLowerInvariant()
            let videos = descriptors |> List.filter (fun d -> (mimeOf d).StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            let images = descriptors |> List.filter (fun d -> (mimeOf d).StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            let unsupported =
                descriptors
                |> List.tryFind (fun d ->
                    let mime = mimeOf d
                    not (mime = "image/jpeg" || mime = "image/png" || mime = "image/gif" || mime = "image/webp" || mime = "video/mp4"))
            if unsupported.IsSome then Error (UnsupportedMediaType (mimeOf unsupported.Value))
            elif List.length videos > 1 then Error MultipleVideos
            elif not (List.isEmpty videos) && not (List.isEmpty images) then Error MixedImageAndVideo
            elif not (List.isEmpty videos) then Ok Video
            elif List.length images > 10 then Error (TooManyImages (List.length images))
            elif List.isEmpty images then
                descriptors
                |> List.tryHead
                |> Option.map (fun d -> Error (UnsupportedMediaType d.MimeType))
                |> Option.defaultValue (Error NoMedia)
            elif List.length images < 5 then Ok Image
            else Ok Gallery

let mediaKindName kind =
    match kind with
    | Image -> "image"
    | Gallery -> "gallery"
    | Video -> "video"

let private mediaDirectoryName kind =
    match kind with
    | Image -> "images"
    | Gallery -> "galleries"
    | Video -> "videos"

/// Media rkeys are namespaced so a media record can never reuse a Note's
/// content-derived TID, while the existing Note deriveTid contract is unchanged.
/// The embed kind is intentionally not part of the seed: changing an album from
/// images to a gallery (or to video) must update one record rather than create a
/// second post and orphan the old one.
let deriveMediaTid (publishedDate: DateTimeOffset) (slug: string) (_kind: AtProtoMediaKind) =
    deriveTid publishedDate (sprintf "media:%s" (nonNull slug))

let mediaPath (slug: string) = sprintf "%s%s/" (ContentTypes.urlPrefix ContentTypes.ContentType.Media) slug
let mediaUrl (slug: string) = Config.canonicalUrl + mediaPath slug

let normalizeAtProtoTags (tags: string array) =
    if isNull tags then [||]
    else
        tags
        |> Array.map TagService.processTagName
        |> Array.filter (fun tag -> not (String.IsNullOrWhiteSpace tag) && tag <> "untagged")
        |> Array.distinct
        |> Array.map (truncateGraphemes 128)
        |> Array.truncate 8

let private mediaDescriptorHashPart (descriptor: AtProtoMediaDescriptor) =
    String.concat "\u0001"
        [ (nonNull descriptor.Url).Trim()
          (nonNull descriptor.MimeType).ToLowerInvariant()
          (nonNull descriptor.Alt).Trim()
          string descriptor.Width
          string descriptor.Height ]

/// Stable media source hash: canonical page URL, final post text, normalized
/// tags, and normalized descriptors are all part of change detection.
let generateMediaSourceHash (canonicalUrl: string) (text: string) (tags: string array) (descriptors: AtProtoMediaDescriptor list) =
    let tagPart =
        if isNull tags then ""
        else tags |> Array.toList |> String.concat "\u0001"
    let mediaPart = descriptors |> List.map mediaDescriptorHashPart |> String.concat "\u0002"
    generateHash (String.concat "\u0000" [ nonNull canonicalUrl; nonNull text; tagPart; mediaPart ])

let private utf8Length (value: string) = Encoding.UTF8.GetByteCount(nonNull value)
let private graphemeLength (value: string) = (StringInfo(nonNull value)).LengthInTextElements

let private truncateToBytesAndGraphemes maxBytes maxGraphemes (value: string) =
    let mutable result = truncateGraphemes maxGraphemes (nonNull value)
    while utf8Length result > maxBytes && graphemeLength result > 0 do
        result <- (StringInfo result).SubstringByTextElements(0, graphemeLength result - 1)
    result

let buildMediaLinkFacet (text: string) (url: string) : JsonObject =
    let start = text.LastIndexOf(url, StringComparison.Ordinal)
    if start < 0 then failwithf "Media URL '%s' is missing from post text" url
    let facet = JsonObject()
    let index = JsonObject()
    index.Add("byteStart", JsonValue.Create (utf8Length (text.Substring(0, start))))
    index.Add("byteEnd", JsonValue.Create (utf8Length (text.Substring(0, start)) + utf8Length url))
    let feature = JsonObject()
    feature.Add("$type", JsonValue.Create "app.bsky.richtext.facet#link")
    feature.Add("uri", JsonValue.Create url)
    let features = JsonArray()
    features.Add feature
    facet.Add("index", index)
    facet.Add("features", features)
    facet

let assertNoNativeTidCollisions (items: (string * string * string) list) : unit =
    let collisions =
        items
        |> List.groupBy (fun (collection, rkey, _) -> collection, rkey)
        |> List.choose (fun ((collection, rkey), group) ->
            if List.length group > 1 then
                Some (sprintf "%s/%s <- %s" collection rkey (group |> List.map (fun (_, _, source) -> source) |> String.concat ", "))
            else None)
    if not (List.isEmpty collisions) then
        failwithf "AtProtoBuilder native rkey collision: %s" (String.concat "; " collisions)

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

let private removeMediaBlocks (markdown: string) =
    let output = ResizeArray<string>()
    let mutable inside = false
    for line in (nonNull markdown).Replace("\r\n", "\n").Split('\n') do
        let trimmed = line.Trim()
        if trimmed.Equals(":::media", StringComparison.OrdinalIgnoreCase) then
            // The normal fence closes with `:::`, but the parser also accepts
            // another `:::media` marker as a compatibility fallback.
            if inside then inside <- false else inside <- true
        elif inside && trimmed = ":::" then
            inside <- false
        elif not inside then
            output.Add line
    String.concat "\n" output

/// Return post text with the canonical media page URL retained.  The URL is
/// deliberately appended after truncation and its facet is byte-indexed below.
let buildMediaPostText (album: Domain.Album) =
    let source = album.MarkdownSource |> Option.defaultValue album.Content
    let body = stripToPlainText (removeMediaBlocks source)
    let url = mediaUrl album.FileName
    let separator = if String.IsNullOrWhiteSpace body then "" else "\n\n"
    let suffix = separator + url
    if graphemeLength suffix > 300 || utf8Length suffix > 3000 then
        failwithf "Canonical media URL for '%s' exceeds app.bsky.feed.post limits" album.FileName
    let availableGraphemes = max 0 (300 - graphemeLength suffix)
    let availableBytes = max 0 (3000 - utf8Length suffix)
    let bodyPart = truncateToBytesAndGraphemes availableBytes availableGraphemes body |> fun s -> s.TrimEnd()
    bodyPart + suffix

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

// ---------------------------------------------------------------------------
// Track B — Notes -> native app.bsky.feed.post (timeline-visible).
//
// A Note becomes a real Bluesky post: a plaintext excerpt (<=300 graphemes) plus an
// app.bsky.embed.external link card pointing back to the canonical note URL on lqdev.me. Records
// carry our `sourceHash` extension field, so the sync script only ever manages posts IT created —
// the account's hand-authored app.bsky.feed.post records (no sourceHash) are structurally
// untouchable. Create-only, forward-only from notesActivationCutoff. rkeys reuse the Track A
// deterministic-TID derivation (deriveTid = published date + slug hash), keeping AT-URIs
// precomputable and putRecord a stateless idempotent upsert.
// ---------------------------------------------------------------------------

/// Canonical `/notes/{slug}/` path (leading + trailing slash) from the single permalink authority,
/// so the embed link card can never drift from the actual published note URL.
let notePath (slug: string) : string =
    sprintf "%s%s/" (ContentTypes.urlPrefix ContentTypes.ContentType.Notes) slug

/// Full canonical URL of a note (what the embed link card points at).
let noteUrl (slug: string) : string =
    Config.canonicalUrl + notePath slug

/// POSSE post text: the note body as plaintext, truncated to <=300 graphemes (Bluesky's hard cap),
/// with a trailing ellipsis when truncated. Falls back to the note title if the body is empty.
let buildNoteText (note: Domain.Post) : string =
    let plain = stripToPlainText note.Content
    let body =
        if String.IsNullOrWhiteSpace plain then
            (if isNull note.Metadata.Title then "" else note.Metadata.Title.Trim())
        else plain
    let si = StringInfo body
    if si.LengthInTextElements <= 300 then body
    else (si.SubstringByTextElements(0, 299)).TrimEnd() + "…"

/// Build the app.bsky.feed.post record JSON for one Note (Track B). Native Bluesky post: excerpt
/// text + an app.bsky.embed.external link card back to the canonical note. `langs` aids Bluesky's
/// language filtering; `sourceHash` is our write-scope / change-detection extension field (same
/// role as on documents). No facets are emitted (plaintext excerpt) — the embed card carries the
/// canonical link, so no UTF-8 byte-offset facet math is needed for the MVP.
let buildPostRecordJson (note: Domain.Post) (published: DateTimeOffset) (slug: string) : JsonObject =
    let o = JsonObject()
    o.Add("$type", JsonValue.Create "app.bsky.feed.post")
    o.Add("text", JsonValue.Create(buildNoteText note))
    o.Add("createdAt", JsonValue.Create(published.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz", CultureInfo.InvariantCulture)))
    let langs = JsonArray()
    langs.Add(JsonValue.Create "en")
    o.Add("langs", langs)
    // app.bsky.embed.external link card -> canonical note (title/description from frontmatter).
    let ext = JsonObject()
    ext.Add("uri", JsonValue.Create(noteUrl slug))
    let title =
        if isNull note.Metadata.Title || String.IsNullOrWhiteSpace note.Metadata.Title
        then Config.publicationName else note.Metadata.Title.Trim()
    ext.Add("title", JsonValue.Create(truncateGraphemes 300 title))
    let description =
        if not (isNull note.Metadata.Description) && not (String.IsNullOrWhiteSpace note.Metadata.Description)
        then note.Metadata.Description.Trim()
        else stripToPlainText note.Content
    ext.Add("description", JsonValue.Create(truncateGraphemes 1000 description))
    let embed = JsonObject()
    embed.Add("$type", JsonValue.Create "app.bsky.embed.external")
    embed.Add("external", ext)
    o.Add("embed", embed)
    // Extension field: scopes writes to records we created + detects content changes.
    o.Add("sourceHash", JsonValue.Create(generateHash (noteUrl slug + "\u0000" + note.Content)))
    o

/// Generate AT Protocol staging records for Notes (Track B -> app.bsky.feed.post). Flag-gated by the
/// caller (useAtProtoNotesSync) and forward-only: only notes on/after notesActivationCutoff are
/// staged. Writes one self-describing JSON file (collection + rkey + record) per eligible note under
/// {outputDir}/api/data/atproto/posts/{rkey}.json. Pure/local — the sync script does the network I/O.
/// Fails the build loudly if two notes would derive the same record key.
let buildAtProtoNotesStaging (notes: Domain.Post list) (outputDir: string) : unit =
    printfn "  🌐 Generating AT Protocol note staging records (app.bsky.feed.post)..."
    let postsDir = Path.Combine(outputDir, "api", "data", "atproto", "posts")
    Directory.CreateDirectory postsDir |> ignore
    let eligible =
        notes
        |> List.choose (fun n ->
            match DateTimeOffset.TryParse n.Metadata.Date with
            | true, d when d >= notesActivationCutoff -> Some(d, n.FileName, n)
            | true, _ -> None                                   // pre-cutoff -> forward-only, skip
            | _ ->
                eprintfn "  ⚠️  AtProto: skipping note with unparseable date '%s' (%s)" n.Metadata.Date n.FileName
                None)
    // Build-time invariant: no two eligible notes may derive the same record key.
    assertNoTidCollisions (eligible |> List.map (fun (d, s, _) -> d, s))
    let opts = JsonSerializerOptions()
    opts.WriteIndented <- true
    let mutable count = 0
    for (d, slug, note) in eligible do
        let rkey = deriveTid d slug
        let wrapper = JsonObject()
        wrapper.Add("collection", JsonValue.Create "app.bsky.feed.post")
        wrapper.Add("rkey", JsonValue.Create rkey)
        wrapper.Add("record", buildPostRecordJson note d slug)
        File.WriteAllText(Path.Combine(postsDir, sprintf "%s.json" rkey), wrapper.ToJsonString opts)
        count <- count + 1
    printfn "  ✅ Generated %d AT Protocol note staging records (post-cutoff)" count

// ---------------------------------------------------------------------------
// Part C — Media -> native app.bsky.feed.post manifests.
// ---------------------------------------------------------------------------

let private jsonAspect (descriptor: AtProtoMediaDescriptor) =
    let aspect = JsonObject()
    aspect.Add("width", JsonValue.Create descriptor.Width)
    aspect.Add("height", JsonValue.Create descriptor.Height)
    aspect

let private jsonMediaDescriptor (descriptor: AtProtoMediaDescriptor) =
    let image = JsonObject()
    // `url` is a build-time placeholder.  sync-atproto replaces it with the
    // uploaded blob ref after authentication and before putRecord.
    image.Add("url", JsonValue.Create descriptor.Url)
    image.Add("mimeType", JsonValue.Create descriptor.MimeType)
    image

let private buildMediaEmbedJson (kind: AtProtoMediaKind) (descriptors: AtProtoMediaDescriptor list) =
    let embed = JsonObject()
    let items = JsonArray()
    for descriptor in descriptors do
        let item = JsonObject()
        if kind = Gallery then
            item.Add("$type", JsonValue.Create "app.bsky.embed.gallery#image")
        item.Add("image", jsonMediaDescriptor descriptor)
        item.Add("alt", JsonValue.Create descriptor.Alt)
        item.Add("aspectRatio", jsonAspect descriptor)
        items.Add item
    match kind with
    | Image ->
        embed.Add("$type", JsonValue.Create "app.bsky.embed.images")
        embed.Add("images", items)
    | Gallery ->
        embed.Add("$type", JsonValue.Create "app.bsky.embed.gallery")
        embed.Add("items", items)
    | Video ->
        embed.Add("$type", JsonValue.Create "app.bsky.embed.video")
        let descriptor = List.head descriptors
        let video = JsonObject()
        video.Add("url", JsonValue.Create descriptor.Url)
        video.Add("mimeType", JsonValue.Create descriptor.MimeType)
        embed.Remove("images") |> ignore
        embed.Add("video", video)
        embed.Add("alt", JsonValue.Create descriptor.Alt)
        embed.Add("aspectRatio", jsonAspect descriptor)
    embed

let buildMediaPostRecordJson (album: Domain.Album) (published: DateTimeOffset)
                            (kind: AtProtoMediaKind) (descriptors: AtProtoMediaDescriptor list) : JsonObject =
    let text = buildMediaPostText album
    let tags = normalizeAtProtoTags album.Metadata.Tags
    let o = JsonObject()
    o.Add("$type", JsonValue.Create "app.bsky.feed.post")
    o.Add("text", JsonValue.Create text)
    o.Add("createdAt", JsonValue.Create(published.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz", CultureInfo.InvariantCulture)))
    let langs = JsonArray()
    langs.Add(JsonValue.Create "en")
    o.Add("langs", langs)
    let facets = JsonArray()
    facets.Add(buildMediaLinkFacet text (mediaUrl album.FileName))
    o.Add("facets", facets)
    if tags.Length > 0 then
        let tagArray = JsonArray()
        tags |> Array.iter (fun tag -> tagArray.Add(JsonValue.Create tag))
        o.Add("tags", tagArray)
    o.Add("embed", buildMediaEmbedJson kind descriptors)
    o.Add("sourceHash", JsonValue.Create(generateMediaSourceHash (mediaUrl album.FileName) text tags descriptors))
    o

let private mediaEnabled kind =
    match kind with
    | Image -> useAtProtoMediaSync || useAtProtoMediaImageSync
    | Gallery -> useAtProtoMediaSync || useAtProtoMediaGallerySync
    | Video -> useAtProtoMediaSync || useAtProtoMediaVideoSync

let private anyMediaGateEnabled =
    useAtProtoMediaSync || useAtProtoMediaImageSync || useAtProtoMediaGallerySync || useAtProtoMediaVideoSync

let private mediaAfterCutoff kind published =
    match kind with
    | Image | Gallery -> published >= mediaImagesActivationCutoff
    | Video -> published >= mediaVideoActivationCutoff

let isMediaAfterActivationCutoff kind published = mediaAfterCutoff kind published

let private stagingDirectory (outputDir: string) kind =
    Path.Combine(outputDir, "api", "data", "atproto", "media", mediaDirectoryName kind)

let private mediaErrorText error =
    match error with
    | NoMedia -> "no media items found"
    | BlankMediaUrl -> "one or more media items have a blank URL"
    | UnsupportedMediaType mime -> sprintf "unsupported media type '%s'" mime
    | MixedImageAndVideo -> "mixed image and video media is not supported"
    | MultipleVideos -> "multiple videos are not supported"
    | TooManyImages count -> sprintf "%d images supplied; the maximum is 10" count

let private eligibleMedia (albums: Domain.Album list) =
    if not anyMediaGateEnabled then []
    else albums
    |> List.choose (fun album ->
        match DateTimeOffset.TryParse album.Metadata.Date with
        | false, _ ->
            eprintfn "  ⚠️  AtProto: skipping media with unparseable date '%s' (%s)" album.Metadata.Date album.FileName
            None
        | true, published ->
            let source = album.MarkdownSource |> Option.defaultValue album.Content
            let descriptors = extractMediaDescriptorsFromMarkdown album.Metadata.Title source
            match validateMediaDescriptors descriptors with
            | Error error ->
                // A media file is content, not an optional decoration.  Reject
                // malformed combinations explicitly instead of staging a
                // misleading text-only post.
                failwithf "AtProto media '%s' rejected: %s" album.FileName (mediaErrorText error)
            | Ok kind when mediaEnabled kind && mediaAfterCutoff kind published ->
                Some (published, album.FileName, album, kind, descriptors)
            | Ok _ -> None)

/// Return all native staging keys (Notes plus media) for the cross-content
/// collision assertion performed by Program.fs and focused tests.
let nativeStagingKeys (notes: Domain.Post list) (albums: Domain.Album list) =
    let noteKeys =
        if not useAtProtoNotesSync then []
        else
            notes
            |> List.choose (fun note ->
                match DateTimeOffset.TryParse note.Metadata.Date with
                | true, date when date >= notesActivationCutoff ->
                    Some ("app.bsky.feed.post", deriveTid date note.FileName, "note:" + note.FileName)
                | _ -> None)
    let mediaKeys =
        if not anyMediaGateEnabled then []
        else
            eligibleMedia albums
            |> List.map (fun (date, slug, _, kind, _) ->
                "app.bsky.feed.post", deriveMediaTid date slug kind, "media:" + slug)
    noteKeys @ mediaKeys

/// Generate separate image/gallery/video manifests.  The wrappers contain
/// upload descriptors and a build-time embed placeholder; the sync script
/// performs uploads and substitutes blob refs only after the write plan and
/// authentication gates have passed.
let buildAtProtoMediaStaging (albums: Domain.Album list) (outputDir: string) : unit =
    printfn "  🌐 Generating AT Protocol rich-media staging records..."
    let eligible = eligibleMedia albums
    assertNoNativeTidCollisions (
        eligible
        |> List.map (fun (date, slug, _, kind, _) ->
            "app.bsky.feed.post", deriveMediaTid date slug kind, "media:" + slug))
    let opts = JsonSerializerOptions()
    opts.WriteIndented <- true
    let mutable count = 0
    for (published, slug, album, kind, descriptors) in eligible do
        let directory = stagingDirectory outputDir kind
        Directory.CreateDirectory directory |> ignore
        let rkey = deriveMediaTid published slug kind
        let wrapper = JsonObject()
        wrapper.Add("collection", JsonValue.Create "app.bsky.feed.post")
        wrapper.Add("rkey", JsonValue.Create rkey)
        wrapper.Add("mediaKind", JsonValue.Create (mediaKindName kind))
        let media = JsonArray()
        descriptors |> List.iter (fun descriptor ->
            let item = JsonObject()
            item.Add("url", JsonValue.Create descriptor.Url)
            item.Add("mimeType", JsonValue.Create descriptor.MimeType)
            item.Add("alt", JsonValue.Create descriptor.Alt)
            item.Add("width", JsonValue.Create descriptor.Width)
            item.Add("height", JsonValue.Create descriptor.Height)
            media.Add item)
        wrapper.Add("media", media)
        wrapper.Add("record", buildMediaPostRecordJson album published kind descriptors)
        File.WriteAllText(Path.Combine(directory, sprintf "%s.json" rkey), wrapper.ToJsonString opts)
        count <- count + 1
    printfn "  ✅ Generated %d AT Protocol rich-media staging records" count

// ---------------------------------------------------------------------------
// Response POSSE — bookmarks + reshares -> native Bluesky records.
//
// Scope (issue #2574 / ADR-0009 amendment):
//   * Public bookmark response targeting an ordinary URL  -> app.bsky.feed.post link post.
//   * Ordinary-web reshare response                        -> app.bsky.feed.post link post.
//   * ATProto-targeted reshare, NO authored commentary     -> app.bsky.feed.repost.
//   * ATProto-targeted reshare, WITH authored commentary   -> app.bsky.feed.post quote-post.
//
// Classification lives in AtProtoResponseMapping (pure). Everything below is the record + staging
// layer: it reuses the SAME deterministic-TID rkeys, sourceHash write-scope guard, grapheme/byte
// truncation, and link-facet math as Tracks A/B/C. rkey SEEDS are namespaced ("bookmark:",
// "reshare-link:", "quote:", "repost:") so a response record can never collide with a Post/Note/media
// record. Reposts write to the SEPARATE app.bsky.feed.repost collection and — per the ATProto
// repost lexicon, which has no room for an extension field — carry NO sourceHash; the sync script
// guards them by their natural subject (URI + CID) instead. Quote/repost records embed a build-time
// PLACEHOLDER for the quoted subject; the sync script resolves the target actor -> DID and fills the
// real uri/cid before putRecord, refusing to write if any target is unresolved.
// ---------------------------------------------------------------------------

/// Four dormant master flags — one per response POSSE mode. All false: no staging is produced and
/// _public stays byte-identical to the baseline until a mode is deliberately activated.
let useAtProtoBookmarkPostsSync = false
let useAtProtoResharePostsSync = false
let useAtProtoRepostsSync = false
let useAtProtoQuotePostsSync = false

/// Forward-only activation cutoffs, one per mode. The SENTINEL is DateTimeOffset.MaxValue: no
/// response is ever published on/after MaxValue, so even if a flag above is flipped to true WITHOUT
/// choosing a real cutoff, staging stays empty. Activation is therefore a deliberate two-part change
/// — set the flag AND replace the sentinel with a real forward-only date (mirrors the notes/media
/// cutoff discipline, which never silently backfills historical content into followers' timelines).
let bookmarkPostsActivationCutoff = DateTimeOffset.MaxValue
let resharePostsActivationCutoff = DateTimeOffset.MaxValue
let repostsActivationCutoff = DateTimeOffset.MaxValue
let quotePostsActivationCutoff = DateTimeOffset.MaxValue

let private responseActivationModes =
    [ "bookmarks", useAtProtoBookmarkPostsSync, bookmarkPostsActivationCutoff
      "reshare link posts", useAtProtoResharePostsSync, resharePostsActivationCutoff
      "reposts", useAtProtoRepostsSync, repostsActivationCutoff
      "quote-posts", useAtProtoQuotePostsSync, quotePostsActivationCutoff ]

/// Fail closed when an operator enables a response track without replacing its
/// sentinel cutoff. This prevents a flag-only change from silently changing the
/// forward-only rollout policy.
let validateResponseActivationConfiguration () : unit =
    match responseActivationModes |> List.tryFind (fun (_, enabled, cutoff) ->
        enabled && cutoff = DateTimeOffset.MaxValue) with
    | Some (mode, _, _) ->
        failwithf "ATProto response track '%s' is enabled but its activation cutoff is still DateTimeOffset.MaxValue" mode
    | None -> ()

let private anyReshareModeEnabled =
    useAtProtoResharePostsSync || useAtProtoRepostsSync || useAtProtoQuotePostsSync

/// Canonical `/bookmarks/{slug}/` and `/responses/{slug}/` paths + URLs, from the single permalink
/// authority (ContentTypes.urlPrefix) so the in-text canonical link can never drift from the real URL.
let bookmarkPath (slug: string) : string =
    sprintf "%s%s/" (ContentTypes.urlPrefix ContentTypes.ContentType.Bookmarks) slug
let bookmarkUrl (slug: string) : string = Config.canonicalUrl + bookmarkPath slug
let responsePath (slug: string) : string =
    sprintf "%s%s/" (ContentTypes.urlPrefix ContentTypes.ContentType.Responses) slug
let responseUrl (slug: string) : string = Config.canonicalUrl + responsePath slug

/// A response body with its front matter removed, for Markdown analysis (per the plan: analyse the
/// MARKDOWN source, not the rendered HTML in Response.Content).
let private responseBody (response: Domain.Response) : string =
    let raw = response.MarkdownSource |> Option.defaultValue ""
    ASTParsing.stripFrontMatter raw

let private isoTimestamp (published: DateTimeOffset) =
    published.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz", CultureInfo.InvariantCulture)

let private englishLangs () =
    let langs = JsonArray()
    langs.Add(JsonValue.Create "en")
    langs

let private addNormalizedTags (record: JsonObject) (tags: string array) =
    let normalized = normalizeAtProtoTags tags
    if normalized.Length > 0 then
        let tagArray = JsonArray()
        normalized |> Array.iter (fun tag -> tagArray.Add(JsonValue.Create tag))
        record.Add("tags", tagArray)

/// Compose native post text `"{header}\n\n{excerpt}\n\n{url}"` (or `"{header}\n\n{url}"` when the
/// excerpt is empty). The canonical URL is appended AFTER truncation — like buildMediaPostText — so
/// its byte-facet offsets are stable, and the excerpt is grapheme/byte-trimmed so the whole text
/// stays within Bluesky's 300-grapheme / 3000-byte caps with the header + URL reserved.
let private composeLinkPostText (header: string) (excerpt: string) (url: string) : string =
    let header = (nonNull header).Trim()
    let excerpt = (nonNull excerpt).Trim()
    let urlSuffix = "\n\n" + url
    let sep = "\n\n"
    if graphemeLength urlSuffix > 300 || utf8Length urlSuffix > 3000 then
        failwithf "Canonical response URL exceeds app.bsky.feed.post limits: %s" url
    elif graphemeLength header + graphemeLength urlSuffix > 300
       || utf8Length header + utf8Length urlSuffix > 3000 then
        // Even header + URL overflow — trim the header (title) to fit, keep the URL.
        let availG = max 0 (300 - graphemeLength urlSuffix)
        let availB = max 0 (3000 - utf8Length urlSuffix)
        (truncateToBytesAndGraphemes availB availG header).TrimEnd() + urlSuffix
    elif excerpt.Length = 0 then header + urlSuffix
    else
        let availG = max 0 (300 - graphemeLength header - graphemeLength sep - graphemeLength urlSuffix)
        let availB = max 0 (3000 - utf8Length header - utf8Length sep - utf8Length urlSuffix)
        if availG = 0 || availB = 0 then header + urlSuffix
        else
            let body = (truncateToBytesAndGraphemes availB availG excerpt).TrimEnd()
            if body.Length = 0 then header + urlSuffix else header + sep + body + urlSuffix

/// Compose a quote-post text `"{commentary}\n\n{url}"`. Commentary is present by construction (a
/// reshare only becomes a quote-post when it has authored commentary), but the empty guard keeps the
/// URL (and its facet) valid if plaintext extraction somehow collapses to nothing.
let private composeQuoteText (commentary: string) (url: string) : string =
    let c = (nonNull commentary).Trim()
    let urlSuffix = "\n\n" + url
    if graphemeLength urlSuffix > 300 || utf8Length urlSuffix > 3000 then
        failwithf "Canonical response URL exceeds app.bsky.feed.post limits: %s" url
    elif c.Length = 0 then url
    else
        let availG = max 0 (300 - graphemeLength urlSuffix)
        let availB = max 0 (3000 - utf8Length urlSuffix)
        let body = (truncateToBytesAndGraphemes availB availG c).TrimEnd()
        if body.Length = 0 then url else body + urlSuffix

/// app.bsky.embed.external card pointing at the EXTERNAL target URL (per the contract: bookmark and
/// reshare link cards point at the external target, while the canonical lqdev.me URL rides in the
/// post text via a byte facet).
let private externalCardJson (externalUrl: string) (title: string) (description: string) : JsonObject =
    let ext = JsonObject()
    ext.Add("uri", JsonValue.Create externalUrl)
    let title = if String.IsNullOrWhiteSpace title then Config.publicationName else title.Trim()
    ext.Add("title", JsonValue.Create(truncateGraphemes 300 title))
    ext.Add("description", JsonValue.Create(truncateGraphemes 1000 ((nonNull description).Trim())))
    let embed = JsonObject()
    embed.Add("$type", JsonValue.Create "app.bsky.embed.external")
    embed.Add("external", ext)
    embed

/// Build the app.bsky.feed.post record for a bookmark response (ordinary-URL target). Text:
/// `Bookmarked: {title}` + excerpt + canonical bookmark URL (facet); external card -> target URL.
let buildBookmarkPostRecordJson (response: Domain.Response) (published: DateTimeOffset)
                                (slug: string) (externalUrl: string) : JsonObject =
    let title = if isNull response.Metadata.Title then "" else response.Metadata.Title.Trim()
    let excerpt = stripToPlainText (responseBody response)
    let canonical = bookmarkUrl slug
    let text = composeLinkPostText (sprintf "Bookmarked: %s" title) excerpt canonical
    let o = JsonObject()
    o.Add("$type", JsonValue.Create "app.bsky.feed.post")
    o.Add("text", JsonValue.Create text)
    o.Add("createdAt", JsonValue.Create(isoTimestamp published))
    o.Add("langs", englishLangs ())
    addNormalizedTags o response.Metadata.Tags
    let facets = JsonArray()
    facets.Add(buildMediaLinkFacet text canonical)   // canonical lqdev.me URL as a link facet
    o.Add("facets", facets)
    o.Add("embed", externalCardJson externalUrl title excerpt)
    let tags = normalizeAtProtoTags response.Metadata.Tags
    o.Add("sourceHash", JsonValue.Create(generateHash (String.concat "\u0000" [ canonical; nonNull externalUrl; text; String.concat "\u0001" tags ])))
    o

/// Build the app.bsky.feed.post record for an ordinary-web reshare. Text: `Shared: {title}` +
/// authored commentary if present else quoted source excerpt + canonical response URL (facet);
/// external card -> the ordinary target URL.
let buildResharePostRecordJson (response: Domain.Response) (published: DateTimeOffset)
                               (slug: string) (externalUrl: string) : JsonObject =
    let title = if isNull response.Metadata.Title then "" else response.Metadata.Title.Trim()
    let analysis = analyzeResponseBody (responseBody response)
    let excerptSource =
        if analysis.HasAuthoredCommentary then analysis.AuthoredCommentaryMarkdown
        elif not (String.IsNullOrWhiteSpace analysis.QuotedExcerptMarkdown) then analysis.QuotedExcerptMarkdown
        else responseBody response
    let excerpt = stripToPlainText excerptSource
    let canonical = responseUrl slug
    let text = composeLinkPostText (sprintf "Shared: %s" title) excerpt canonical
    let o = JsonObject()
    o.Add("$type", JsonValue.Create "app.bsky.feed.post")
    o.Add("text", JsonValue.Create text)
    o.Add("createdAt", JsonValue.Create(isoTimestamp published))
    o.Add("langs", englishLangs ())
    addNormalizedTags o response.Metadata.Tags
    let facets = JsonArray()
    facets.Add(buildMediaLinkFacet text canonical)
    o.Add("facets", facets)
    o.Add("embed", externalCardJson externalUrl title excerpt)
    let tags = normalizeAtProtoTags response.Metadata.Tags
    o.Add("sourceHash", JsonValue.Create(generateHash (String.concat "\u0000" [ canonical; nonNull externalUrl; text; String.concat "\u0001" tags ])))
    o

/// Build the app.bsky.feed.post quote-post record for an ATProto-targeted reshare WITH commentary.
/// Text: authored commentary + canonical response URL (facet). embed.record carries a build-time
/// PLACEHOLDER {uri="",cid=""}; the sync script resolves the target and fills it before putRecord.
/// The quoted post is NOT duplicated in text — it rides in the embed.
let buildQuotePostRecordJson (response: Domain.Response) (published: DateTimeOffset)
                             (slug: string) (target: TargetRef) : JsonObject =
    match target with
    | AtProtoPost _ -> ()
    | OrdinaryUrl url -> failwithf "Cannot build a quote-post for ordinary target URL '%s'" url
    let analysis = analyzeResponseBody (responseBody response)
    let commentary = stripToPlainText analysis.AuthoredCommentaryMarkdown
    let canonical = responseUrl slug
    let text = composeQuoteText commentary canonical
    let targetUrl = (nonNull response.Metadata.TargetUrl).Trim()
    let o = JsonObject()
    o.Add("$type", JsonValue.Create "app.bsky.feed.post")
    o.Add("text", JsonValue.Create text)
    o.Add("createdAt", JsonValue.Create(isoTimestamp published))
    o.Add("langs", englishLangs ())
    addNormalizedTags o response.Metadata.Tags
    let facets = JsonArray()
    facets.Add(buildMediaLinkFacet text canonical)
    o.Add("facets", facets)
    let subject = JsonObject()
    subject.Add("uri", JsonValue.Create "")   // placeholder — filled by sync after target resolution
    subject.Add("cid", JsonValue.Create "")
    let embed = JsonObject()
    embed.Add("$type", JsonValue.Create "app.bsky.embed.record")
    embed.Add("record", subject)
    o.Add("embed", embed)
    // sourceHash covers the canonical URL, authored text, and original target URL. It deliberately does
    // NOT cover the resolved CID (unknown at build time), so a target's later edit does not rewrite the
    // quote; changing the local target reference does re-sync it.
    let tags = normalizeAtProtoTags response.Metadata.Tags
    o.Add("sourceHash", JsonValue.Create(generateHash (String.concat "\u0000" [ canonical; text; targetUrl; String.concat "\u0001" tags ])))
    o

/// Build the app.bsky.feed.repost record for an ATProto-targeted reshare with NO commentary. subject
/// is a strongRef PLACEHOLDER {uri="",cid=""} filled by the sync. NO text and — deliberately — NO
/// sourceHash: the repost lexicon has no extension slot, so the sync guards reposts by their natural
/// subject URI+CID instead of a sourceHash marker.
let buildRepostRecordJson (published: DateTimeOffset) : JsonObject =
    let o = JsonObject()
    o.Add("$type", JsonValue.Create "app.bsky.feed.repost")
    let subject = JsonObject()
    subject.Add("uri", JsonValue.Create "")   // placeholder — filled by sync after target resolution
    subject.Add("cid", JsonValue.Create "")
    o.Add("subject", subject)
    o.Add("createdAt", JsonValue.Create(isoTimestamp published))
    o

// --- Eligibility / routing ---------------------------------------------------

/// A single planned response record (post-flag, post-cutoff), before JSON construction.
type private ResponsePlan =
    { Published: DateTimeOffset
      Slug: string
      Response: Domain.Response
      Kind: ResponsePostKind
      Target: TargetRef }

let private parseResponseDate (r: Domain.Response) : DateTimeOffset option =
    match DateTimeOffset.TryParse r.Metadata.DatePublished with
    | true, d -> Some d
    | _ -> None

/// Namespaced deterministic rkey for a response record. The seed prefix ("bookmark", "reshare-link",
/// "quote", "repost") keeps response rkeys disjoint from Post/Note/media rkeys.
let responseRkey (seedPrefix: string) (published: DateTimeOffset) (slug: string) : string =
    deriveTid published (sprintf "%s:%s" seedPrefix (nonNull slug))

let private resharePrefix (kind: ResponsePostKind) =
    match kind with
    | LinkPost -> "reshare-link"
    | QuotePost -> "quote"
    | Repost -> "repost"

/// The AT Protocol collection a response record is written to. Reposts get their own collection, so
/// a repost rkey can never collide with a post rkey.
let responseCollection (kind: ResponsePostKind) : string =
    match kind with
    | Repost -> "app.bsky.feed.repost"
    | LinkPost | QuotePost -> "app.bsky.feed.post"

/// Bookmark responses eligible for POSSE: bookmark-type, ordinary-URL target, flag on, post-cutoff.
/// ATProto-targeted bookmarks are intentionally skipped (private app.bsky.bookmark procedures are
/// out of scope).
let private eligibleBookmarkPlans (bookmarks: Domain.Response list) : ResponsePlan list =
    if not useAtProtoBookmarkPostsSync then []
    else
        bookmarks
        |> List.filter (fun r -> String.Equals(r.Metadata.ResponseType, "bookmark", StringComparison.OrdinalIgnoreCase))
        |> List.choose (fun r ->
            match parseResponseDate r with
            | Some d when d >= bookmarkPostsActivationCutoff ->
                match parseTargetRef r.Metadata.TargetUrl with
                | OrdinaryUrl _ as t -> Some { Published = d; Slug = r.FileName; Response = r; Kind = LinkPost; Target = t }
                | AtProtoPost _ -> None
            | _ -> None)

/// Reshare responses eligible for POSSE, each routed to its kind (link/quote/repost) and gated by
/// that kind's own flag + cutoff. Body analysis (for the quote-vs-repost decision) only runs when at
/// least one reshare mode is enabled, so dormant builds pay no parsing cost.
let private eligibleResharePlans (responses: Domain.Response list) : ResponsePlan list =
    if not anyReshareModeEnabled then []
    else
        responses
        |> List.filter (fun r -> String.Equals(r.Metadata.ResponseType, "reshare", StringComparison.OrdinalIgnoreCase))
        |> List.choose (fun r ->
            match parseResponseDate r with
            | None -> None
            | Some d ->
                let target, kind = classifyResponse r.Metadata.TargetUrl (responseBody r)
                let enabled =
                    match kind with
                    | LinkPost -> useAtProtoResharePostsSync && d >= resharePostsActivationCutoff
                    | QuotePost -> useAtProtoQuotePostsSync && d >= quotePostsActivationCutoff
                    | Repost -> useAtProtoRepostsSync && d >= repostsActivationCutoff
                if enabled && kind = QuotePost && containsLocalMedia (responseBody r) then
                    failwithf "ATProto quote-post response '%s' contains local media; recordWithMedia is out of scope" r.FileName
                if enabled then Some { Published = d; Slug = r.FileName; Response = r; Kind = kind; Target = target }
                else None)

/// (collection, rkey, source) keys for all response-derived native records, for the cross-content
/// collision assertion in Program.fs. Respects every flag + cutoff, so dormant modes contribute
/// nothing.
let responseNativeStagingKeys (bookmarks: Domain.Response list) (responses: Domain.Response list)
                              : (string * string * string) list =
    validateResponseActivationConfiguration ()
    let bmKeys =
        eligibleBookmarkPlans bookmarks
        |> List.map (fun p -> responseCollection p.Kind, responseRkey "bookmark" p.Published p.Slug, "bookmark:" + p.Slug)
    let rsKeys =
        eligibleResharePlans responses
        |> List.map (fun p ->
            let prefix = resharePrefix p.Kind
            responseCollection p.Kind, responseRkey prefix p.Published p.Slug, prefix + ":" + p.Slug)
    bmKeys @ rsKeys

let private targetRefJson (kind: ResponsePostKind) (target: TargetRef) : JsonObject option =
    match target with
    | AtProtoPost(actor, rkey) ->
        let o = JsonObject()
        o.Add("actor", JsonValue.Create actor)
        o.Add("rkey", JsonValue.Create rkey)
        o.Add("kind", JsonValue.Create (match kind with | QuotePost -> "quote" | Repost -> "repost" | LinkPost -> "link"))
        Some o
    | OrdinaryUrl _ -> None

/// Generate bookmark staging records (app.bsky.feed.post) under
/// {outputDir}/api/data/atproto/bookmarks/{rkey}.json. Flag-gated + forward-only; sync consumes them
/// exactly like Notes (sourceHash write-scope guard, no media upload). Fails loudly on rkey collision.
let buildAtProtoBookmarksStaging (bookmarks: Domain.Response list) (outputDir: string) : unit =
    validateResponseActivationConfiguration ()
    printfn "  🌐 Generating AT Protocol bookmark staging records (app.bsky.feed.post)..."
    let dir = Path.Combine(outputDir, "api", "data", "atproto", "bookmarks")
    Directory.CreateDirectory dir |> ignore
    let plans = eligibleBookmarkPlans bookmarks
    assertNoNativeTidCollisions (
        plans |> List.map (fun p -> "app.bsky.feed.post", responseRkey "bookmark" p.Published p.Slug, "bookmark:" + p.Slug))
    let opts = JsonSerializerOptions(WriteIndented = true)
    let mutable count = 0
    for p in plans do
        let externalUrl = match p.Target with | OrdinaryUrl u -> u | AtProtoPost _ -> ""
        let rkey = responseRkey "bookmark" p.Published p.Slug
        let wrapper = JsonObject()
        wrapper.Add("collection", JsonValue.Create "app.bsky.feed.post")
        wrapper.Add("rkey", JsonValue.Create rkey)
        wrapper.Add("stagingKind", JsonValue.Create "bookmark")
        wrapper.Add("record", buildBookmarkPostRecordJson p.Response p.Published p.Slug externalUrl)
        File.WriteAllText(Path.Combine(dir, sprintf "%s.json" rkey), wrapper.ToJsonString opts)
        count <- count + 1
    printfn "  ✅ Generated %d AT Protocol bookmark staging records (post-cutoff)" count

/// Generate reshare staging records, routed per kind: link posts -> reshares/, quote posts ->
/// quotes/, reposts -> reposts/ (each only when its mode is enabled and post-cutoff). Quote/repost
/// wrappers carry a `targetRef` sidecar {actor, rkey, kind} the sync uses to resolve + fill the
/// embed/subject strongRef. Fails loudly on rkey collision (checked across all kinds together).
let buildAtProtoResharesStaging (responses: Domain.Response list) (outputDir: string) : unit =
    validateResponseActivationConfiguration ()
    printfn "  🌐 Generating AT Protocol reshare staging records..."
    let plans = eligibleResharePlans responses
    assertNoNativeTidCollisions (
        plans |> List.map (fun p ->
            let prefix = resharePrefix p.Kind
            responseCollection p.Kind, responseRkey prefix p.Published p.Slug, prefix + ":" + p.Slug))
    let opts = JsonSerializerOptions(WriteIndented = true)
    let mutable linkCount = 0
    let mutable quoteCount = 0
    let mutable repostCount = 0
    for p in plans do
        let prefix = resharePrefix p.Kind
        let rkey = responseRkey prefix p.Published p.Slug
        let collection = responseCollection p.Kind
        let subdir = match p.Kind with | LinkPost -> "reshares" | QuotePost -> "quotes" | Repost -> "reposts"
        let stagingKind =
            match p.Kind with
            | LinkPost -> "reshare-link"
            | QuotePost -> "quote"
            | Repost -> "repost"
        let dir = Path.Combine(outputDir, "api", "data", "atproto", subdir)
        Directory.CreateDirectory dir |> ignore
        let wrapper = JsonObject()
        wrapper.Add("collection", JsonValue.Create collection)
        wrapper.Add("rkey", JsonValue.Create rkey)
        wrapper.Add("stagingKind", JsonValue.Create stagingKind)
        match targetRefJson p.Kind p.Target with
        | Some t -> wrapper.Add("targetRef", t)
        | None -> ()
        let record =
            match p.Kind with
            | LinkPost ->
                let externalUrl = match p.Target with | OrdinaryUrl u -> u | AtProtoPost _ -> ""
                buildResharePostRecordJson p.Response p.Published p.Slug externalUrl
            | QuotePost -> buildQuotePostRecordJson p.Response p.Published p.Slug p.Target
            | Repost -> buildRepostRecordJson p.Published
        wrapper.Add("record", record)
        File.WriteAllText(Path.Combine(dir, sprintf "%s.json" rkey), wrapper.ToJsonString opts)
        match p.Kind with
        | LinkPost -> linkCount <- linkCount + 1
        | QuotePost -> quoteCount <- quoteCount + 1
        | Repost -> repostCount <- repostCount + 1
    printfn "  ✅ Generated %d reshare link, %d quote, %d repost staging records" linkCount quoteCount repostCount
