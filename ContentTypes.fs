module ContentTypes

// =============================================================================
// Single authority for content-type identity (F5 — step 1 of 2).
//
// DO NOT scatter bare content-type string literals across the codebase — extend
// this module instead. Honest scope: these are [<Literal>] strings, so this is
// DOCUMENTATION, not enforcement. F# literals erase to `string`; the compiler
// still can't catch a tag passed where a content type belongs. The enforcement
// upgrade is the closed `ContentType` DU in Phase 2.7; this module's job is to
// collect the scatter into one place so that swap is mechanical.
//
// Values must remain byte-for-byte what the converters/feeds/URLs already emit.
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

/// Permalink directory prefix per content type, lifted verbatim from the
/// hand-maintained match in `Views/LayoutViews.fs`. The fallback mirrors the
/// previous `| _ -> $"/{contentType}/"` behavior for unmapped types.
let urlPrefix (contentType: string) =
    match contentType with
    | Posts -> "/posts/"
    | Notes -> "/notes/"
    | Responses -> "/responses/"
    | Bookmarks -> "/bookmarks/"
    | Snippets -> "/resources/snippets/"
    | Wiki -> "/resources/wiki/"
    | Presentations -> "/resources/presentations/"
    | Reviews -> "/reviews/"
    | Streams -> "/streams/"
    | Media -> "/media/"
    | AiMemex -> "/resources/ai-memex/"
    | other -> sprintf "/%s/" other
