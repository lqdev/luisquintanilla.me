# ADR-0007: Content-Type Roster Registry for Unified-Feed Membership

## Status
Accepted

## Context

[ADR-0006](0006-generic-build-driver.md) collapsed the per-type *page* generation into one
declarative `ContentTypeBuild<'T>` driver. But the layer above — *which content types appear
in which site-wide feeds* — was still three hand-maintained lists inline in `Program.fs`:

- `timelineFeedItems` — the homepage timeline (7 of 12 types)
- `allUnifiedItems` — the RSS fire-hose, search index, text-only, and ActivityPub source (all 12)
- `blogArchiveFeedItems` — the blog-archive / JSON-feed scope (posts + notes + responses)

Each was a literal `(ContentTypes.<key>, <type>Unified)` list. Adding a content type meant editing
two or three of these lists *and remembering which*. Forgetting one silently dropped the type from
that surface — exactly the bug class documented in AI Memex `pattern-content-type-taxonomy-mismatch`.
This was the unfinished half of findings **F1** (one driver, many configs) and **F5** (one identity
authority): identity and page-building were unified (1.3 / 2.1 / 2.7), but feed *participation* was
still scattered.

This ADR is bet **B1** in `docs/architecture-assessment-2026.md`.

## Decision

Introduce `ContentRegistry.fs` (compiled after `UnifiedFeeds.fs`) with one homogeneous row per
content type and three derivations:

```fsharp
type ContentTypeRoster =
    { Identity: ContentTypes.ContentType
      Unified: UnifiedFeeds.UnifiedFeedItem list
      InTimeline: bool
      InAllFeeds: bool
      InBlogArchive: bool }

let timeline    roster = ... // rows where InTimeline,    as (serialize Identity, Unified)
let allFeeds    roster = ... // rows where InAllFeeds
let blogArchive roster = ... // rows where InBlogArchive
```

`Program.fs` builds **one ordered `contentRoster` value** from the already-typed
`convertX (buildX())` results, and the three feed lists become one-line derivations. Adding a type
to a feed surface is now a per-row boolean, not an edit to a separate list.

### Why a *projection*, not a driver (the key constraint)

The roster carries each type's **already-computed** `UnifiedFeedItem list`. It is built *after* the
typed builds run, in their existing explicit order. The registry never drives the build loop, so
global render counters (notably the StarRating gradient IDs at `BlockRenderers.fs`, whose ordering
the intentional second `buildBooks()` depends on — Program.fs:213) are **not reordered**. Output is
therefore **byte-identical**: verified 0 diffs across 13,518 files vs baseline.

### Why not the literal "one record incl. processor + views"

The assessment's sketch implied a single table holding each type's generic processor and views.
That requires erasing `'T` across heterogeneous types, which in practice means
`{ new IContentType with … }` object expressions — reintroducing the interface hierarchy ADR-0006
deliberately rejected (a regression against the composition-over-inheritance doctrine, assessment
§8.1). It also does not pay off: **four** types have type-specific consumers that need the concrete
`'T` downstream — `buildBookmarksLandingPage` (`FeedData<Response>`), `buildAiMemexPages`
(`FeedData<AiMemex>`), `buildTextOnlySite` (`FeedData<Presentation>`), and the byte-identity-load-bearing
second `buildBooks()`. Those keep their typed handles regardless, so an erased registry would be a
redundant *parallel* structure that also reorders builds (byte-identity risk). The roster stays
homogeneous (no erased `'T`, no new interface).

### Deliberately out of scope (would be false unification — see 2.6)

- **Desktop navigation** (`Views/Navigation.fs`): its menu grouping is editorial and mixes links that
  are not content types (Tools, Radio, Blogroll, Read Later). It is not derivable from feed membership.
- **Tag-page list** (`allTaggableContent`): it merges responses + bookmarks into a single entry and
  uses a plural `"wikis"` key — not a clean per-type projection. It stays explicit in `Program.fs`.

## Consequences

**Easier:**
- Feed participation for all content types is one greppable table instead of three parallel lists.
- The "added to one feed list but forgot another" bug class is structurally impossible.
- Adding a type's feed membership is a one-row edit with boolean flags.

**Constraints / unchanged:**
- The per-type `buildX()`/`convertX()` bindings remain (they must — typed consumers need them); the
  registry projects their results, it does not replace them.
- Nav and tag-page membership remain explicit, by design.
- `ContentRegistry` depends only on `ContentTypes` (identity) and `UnifiedFeeds` (the item type); no
  generic `'T` leaks into the registry, and no interface is introduced.

**Exit:** deleting `ContentRegistry.fs` and inlining the three lists returns to the Phase-2 end-state
with no loss — the registry is additive structure, not a new contract.
