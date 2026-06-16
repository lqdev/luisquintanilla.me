# ADR-0006: Generic Build Driver for Content-Type Page Generation

## Status
Accepted

## Context

[ADR-0001](0001-unified-content-processing.md) unified the *parse → render → feed*
pipeline behind `GenericBuilder.ContentProcessor<'T>` and `buildContentWithFeeds`.
But the layer above it — the per-content-type `buildX` functions in `Builder.fs`
(`buildSnippets`, `buildWikis`, `buildPosts`, `buildNotes`, `buildResponses`,
`buildBookmarks`, `buildPresentations`, …) — was still ~40 lines of near-identical
boilerplate each. Every one repeated the same shape:

1. `Directory.GetFiles(...)` + filter `.md` → list
2. create processor → `buildContentWithFeeds`
3. iterate `FeedData`, build `outputDir/.../{FileName}/index.html` via a per-type view
4. write an index page (sometimes sorted)
5. return `FeedData` for downstream feeds

This was finding **F1** in `docs/architecture-assessment-2026.md`: duplicated build
drivers. The differences between content types are small and *data-shaped*: source/output
directory, which processor, which date/title/view to use, and whether the index is sorted.

## Decision

Introduce `BuildDriver.fs` with a declarative `ContentTypeBuild<'T>` record and a single
`buildContentType` driver that performs the shared I/O. Per-type rendering stays in the
caller (`Builder.fs`) as `ItemView`/`Index.View`/`ItemTitle` closures, because only
`Builder.fs` has access to the Views layer, `convertMdToHtml`, and
`RelatedContentService.findRelatedContent`. The driver only orchestrates enumeration,
page writing, optional index sorting, and returns `FeedData` unchanged.

```fsharp
type IndexConfig<'T> = {
    View: 'T array -> XmlNode
    Title: string
    Sort: ('T array -> 'T array) option        // None = source/feed order
}

type ContentTypeBuild<'T> = {
    Name: string                          // ContentTypes literal (identity/diagnostics)
    SourceDir: string list                // segments under srcDir
    OutputDir: string list                // segments under outputDir
    Processor: GenericBuilder.ContentProcessor<'T>
    Slug: 'T -> string                    // page directory name (typically FileName)
    ItemView: 'T -> 'T array -> XmlNode   // siblings passed for related-content
    ItemTitle: 'T -> string
    Layout: string                        // ViewGenerator layout key
    Index: IndexConfig<'T> option         // None = build individual pages only
}
```

The index-page fields are grouped into an **optional** `IndexConfig` so a content type
can declare it owns no index — `buildBookmarks` does this (`Index = None`): the
`/bookmarks/` landing page is built separately by `buildBookmarksLandingPage` from
bookmark-type *responses*, not from the `/bookmarks/` source folder. The driver also
guards the source dir with `Directory.Exists` (returning `[]` when absent), preserving
the safety the `albums`/`playlists` builders had.

**Compile order:** `BuildDriver.fs` sits *after* `Views/Generator.fs` (it calls
`ViewGenerator.generate`) and *before* `Builder.fs` (the consumer) — later than
`GenericBuilder.fs`, a deliberate deviation from the assessment's first sketch.

**Pilots (byte-identical verified, 0 diffs):**
- `buildSnippets` — `CreatedDate`, title `Snippet | {t} | …`, no index sort.
- `buildWikis` — `LastUpdatedDate`, title `{t} | Wiki | …`, index sorted by title
  (`Sort = Some (Array.sortBy (_.Metadata.Title))`).

These two prove the date/title/sort axes vary cleanly via config alone. The
`media`/`album-collection`/`playlist-collection` builders additionally proved that an
AST-rendered `ItemView` (`processor.Render content |> convertMdToHtml`) fits by closing
over a pre-bound `processor`.

## Consequences

**Easier:**
- Each migrated `buildX` shrinks from ~40 lines to a ~15-line config record.
- New content types get correct page+index structure by filling in one record.
- The shared path/write logic lives in one place; output structure can't drift per type.

**Outcome:** All **11/11** content-type builders migrated to the driver, each verified
byte-identical against the baseline `_public/` (excluding the nondeterministic
`ai-memex/graph.json` timestamp). The 9/11 target in the assessment was exceeded; the
STOP rule (leave a builder hand-written if it needs >2 new config fields) was never
triggered — the single optional `Index` field and the `Directory.Exists` guard were
enough to absorb every variation.

**More difficult / constraints:**
- The driver assumes the common shape (one source dir of `.md`, one page dir per item,
  at most one index). A genuinely different shape would still warrant a hand-written
  builder under the STOP rule.
- `ContentTypeBuild.Name` carries a `ContentTypes` literal for diagnostics and
  forward-compatibility with the closed-DU upgrade (2.7) and railway diagnostics (2.8);
  it is **not** output-affecting today.
