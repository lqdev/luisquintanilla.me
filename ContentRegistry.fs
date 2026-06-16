module ContentRegistry

// =============================================================================
// Content-type roster (B1 — capstone of F1 + F5).
//
// One declarative row per content type describing how it participates in the
// site's UNIFIED-FEED surface. The three hand-maintained membership lists that
// used to live inline in Program.fs (the timeline feed, the all-content feed,
// and the blog-archive/JSON-feed scope) now DERIVE from this single table via
// per-row booleans — so adding a type to one feed but forgetting another (the
// bug class behind AI Memex `pattern-content-type-taxonomy-mismatch`) becomes
// structurally impossible.
//
// Design note (why this is a *projection*, not a driver):
//   The roster carries each type's already-computed `UnifiedFeedItem list`
//   (the result of `convertXToUnified (buildX())`). It is built AFTER the typed
//   builds run, in their existing explicit order. The registry never drives the
//   build loop, so global render counters (e.g. StarRating gradient IDs) are not
//   reordered — output stays byte-identical. A type still needs its concrete `'T`
//   for the few type-specific consumers (bookmarks landing, AI Memex pages,
//   text-only presentations); those keep their typed handles. This is why the
//   roster is homogeneous (no erased `'T`) and no interface hierarchy is added —
//   honoring the composition-over-inheritance doctrine the 2.1 driver set.
//
// Deliberately NOT modelled here (would be false unification — see 2.6):
//   - Desktop navigation: its menu grouping is editorial and mixes links that
//     are not content types at all (Tools, Radio, Blogroll, Read Later). It is
//     the single source in `Views/Navigation.fs`; it does not derive from membership.
//   - The tag-page list (`allTaggableContent`): it merges responses + bookmarks
//     into one entry and uses a plural `"wikis"` key, so it is not a clean
//     per-type projection. It stays explicit in Program.fs.
// =============================================================================

/// One content type's participation in the unified-feed surface. `Unified` is the
/// type's already-projected feed items; the booleans select which feed lists it
/// joins. Identity is the closed `ContentTypes.ContentType` (F5).
type ContentTypeRoster =
    { Identity: ContentTypes.ContentType
      Unified: UnifiedFeeds.UnifiedFeedItem list
      InTimeline: bool
      InAllFeeds: bool
      InBlogArchive: bool }

/// The wire key carried in feed lists (byte-identical to the `ContentTypes.*`
/// literals the inline lists used): `serialize` is the single boundary (F5).
let key (r: ContentTypeRoster) : string = ContentTypes.serialize r.Identity

let private project (pred: ContentTypeRoster -> bool) (roster: ContentTypeRoster list) =
    roster
    |> List.filter pred
    |> List.map (fun r -> key r, r.Unified)

/// Timeline (homepage) feed scope.
let timeline (roster: ContentTypeRoster list) = project (fun r -> r.InTimeline) roster

/// All-content scope (RSS fire-hose, search, text-only, ActivityPub source).
let allFeeds (roster: ContentTypeRoster list) = project (fun r -> r.InAllFeeds) roster

/// Blog-archive / JSON-feed scope (posts + notes + responses).
let blogArchive (roster: ContentTypeRoster list) = project (fun r -> r.InBlogArchive) roster
