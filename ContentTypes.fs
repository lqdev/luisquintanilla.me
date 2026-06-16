module ContentTypes

// =============================================================================
// Single authority for content-type identity (F5 — closed DU, step 2 of 2).
//
// Two layers live here, on purpose:
//   1. The `[<Literal>]` strings below — the WIRE form of a content type. They
//      are carried verbatim in `UnifiedFeedItem.ContentType`, used as feed/tag
//      keys, emitted into timeline JSON, and matched for URL routing. They MUST
//      remain byte-for-byte what the converters/feeds/URLs already emit.
//   2. The closed `ContentType` DU — the IDENTITY form. `parse`/`serialize` are
//      the single boundary pair between the two. Genuine dispatch (URL routing,
//      JSON-feed body decision) matches the DU exhaustively, so adding a case
//      forces the compiler to enumerate every site that must handle it.
//
// Taxonomy decision (the choice F5 forces): response SUBTYPES
// (reply/reshare/star/rsvp/bookmark) are NOT content types — they belong to
// `Domain.ResponseType`. The DU deliberately excludes them; they survive only as
// strings in the `UnifiedFeedItem.ContentType` wire field, where they act as the
// timeline grouping key (see AI Memex `pattern-content-type-taxonomy-mismatch`).
// That is why `parse` returns `None` for them and the URL shim keeps a fallback.
//
// Why ~90 equality/key sites stay string-based: they read or set the wire field
// (serialization), not dispatch. The DU's enforcement value lands at the two
// match sites; converting the per-type setters/keys would be churn with no
// exhaustiveness gain. `serialize` is the forward path the B1 content-type
// registry will key off of.
// =============================================================================

/// Canonical content-type identifiers, as carried in `UnifiedFeedItem.ContentType`,
/// used as feed/tag keys, and matched for URL routing.
[<Literal>]
let Posts = "posts"
[<Literal>]
let Notes = "notes"
[<Literal>]
let Responses = "responses"
[<Literal>]
let Bookmarks = "bookmarks"
[<Literal>]
let Snippets = "snippets"
[<Literal>]
let Wiki = "wiki"
[<Literal>]
let Presentations = "presentations"
[<Literal>]
let Reviews = "reviews"
[<Literal>]
let Media = "media"
[<Literal>]
let Streams = "streams"
[<Literal>]
let AiMemex = "ai-memex"
[<Literal>]
let AlbumCollection = "album-collection"
[<Literal>]
let PlaylistCollection = "playlist-collection"

/// Response subtypes carried in `UnifiedFeedItem.ContentType` instead of the
/// generic `Responses` (see AI Memex `pattern-content-type-taxonomy-mismatch`).
[<Literal>]
let Reply = "reply"
[<Literal>]
let Reshare = "reshare"
[<Literal>]
let Star = "star"
[<Literal>]
let Rsvp = "rsvp"
[<Literal>]
let Bookmark = "bookmark"

/// The full set of response subtypes that should normalize back to `Responses`
/// for routing/filtering/grouping by downstream consumers.
let ResponseSubtypes = set [ Reply; Reshare; Star; Rsvp ]

/// Normalize a response subtype back to the canonical `Responses` identity.
let normalizeResponseSubtype (contentType: string) =
    if ResponseSubtypes.Contains contentType then Responses else contentType

/// Closed set of canonical content-type identities. Response subtypes are
/// intentionally absent (they are `Domain.ResponseType` values — see header).
[<RequireQualifiedAccess>]
type ContentType =
    | Posts
    | Notes
    | Responses
    | Bookmarks
    | Snippets
    | Wiki
    | Presentations
    | Reviews
    | Media
    | Streams
    | AiMemex
    | AlbumCollection
    | PlaylistCollection

/// Identity -> wire string. Exhaustive over the DU (no wildcard): adding a case
/// will not compile until its wire string is chosen here. This is the forward
/// half of the boundary pair the B1 content-type registry keys off of.
let serialize (ct: ContentType) =
    match ct with
    | ContentType.Posts -> Posts
    | ContentType.Notes -> Notes
    | ContentType.Responses -> Responses
    | ContentType.Bookmarks -> Bookmarks
    | ContentType.Snippets -> Snippets
    | ContentType.Wiki -> Wiki
    | ContentType.Presentations -> Presentations
    | ContentType.Reviews -> Reviews
    | ContentType.Media -> Media
    | ContentType.Streams -> Streams
    | ContentType.AiMemex -> AiMemex
    | ContentType.AlbumCollection -> AlbumCollection
    | ContentType.PlaylistCollection -> PlaylistCollection

/// Wire string -> identity. The intake boundary: returns `None` for response
/// subtypes and any value outside the closed set. Matching on an arbitrary
/// string, so a final wildcard is correct here (the no-wildcard rule applies to
/// matches ON the DU, not to string intake).
let parse (s: string) : ContentType option =
    match s with
    | Posts -> Some ContentType.Posts
    | Notes -> Some ContentType.Notes
    | Responses -> Some ContentType.Responses
    | Bookmarks -> Some ContentType.Bookmarks
    | Snippets -> Some ContentType.Snippets
    | Wiki -> Some ContentType.Wiki
    | Presentations -> Some ContentType.Presentations
    | Reviews -> Some ContentType.Reviews
    | Media -> Some ContentType.Media
    | Streams -> Some ContentType.Streams
    | AiMemex -> Some ContentType.AiMemex
    | AlbumCollection -> Some ContentType.AlbumCollection
    | PlaylistCollection -> Some ContentType.PlaylistCollection
    | _ -> None

/// Permalink directory prefix per canonical content type. Exhaustive over the
/// closed DU (no wildcard) — adding a `ContentType` case will not compile until
/// a prefix is chosen here. AlbumCollection/PlaylistCollection are now explicit
/// (previously they fell through the `/{other}/` fallback to the same value).
let urlPrefix (ct: ContentType) =
    match ct with
    | ContentType.Posts -> "/posts/"
    | ContentType.Notes -> "/notes/"
    | ContentType.Responses -> "/responses/"
    | ContentType.Bookmarks -> "/bookmarks/"
    | ContentType.Snippets -> "/resources/snippets/"
    | ContentType.Wiki -> "/resources/wiki/"
    | ContentType.Presentations -> "/resources/presentations/"
    | ContentType.Reviews -> "/reviews/"
    | ContentType.Streams -> "/streams/"
    | ContentType.Media -> "/media/"
    | ContentType.AiMemex -> "/resources/ai-memex/"
    | ContentType.AlbumCollection -> "/album-collection/"
    | ContentType.PlaylistCollection -> "/playlist-collection/"

/// String-boundary shim for URL prefixing. Parses the wire string; response
/// subtypes carry their subtype as the key but their detail pages live under the
/// parent route (replies/reshares/stars/rsvps → /responses/, bookmark → /bookmarks/),
/// matching TimelineViews' initial-content `getProperPermalink`. Any other unmapped
/// value preserves the previous `| other -> sprintf "/%s/" other` behavior.
let urlPrefixForKey (contentType: string) =
    match parse contentType with
    | Some ct -> urlPrefix ct
    | None ->
        match contentType with
        | Reply | Reshare | Star | Rsvp -> "/responses/"
        | Bookmark -> "/bookmarks/"
        | other -> sprintf "/%s/" other
