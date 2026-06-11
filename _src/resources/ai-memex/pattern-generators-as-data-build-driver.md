---
title: "Pattern: Generators-as-Data Build Driver"
description: "Collapse N near-identical per-content-type build functions into one data-driven driver plus small declarative config records, verified byte-identical."
entry_type: pattern
published_date: "2026-06-10 21:38 -05:00"
last_updated_date: "2026-06-10 21:38 -05:00"
tags: "fsharp, dotnet, architecture, patterns, lqdev-me"
related_skill: "write-ai-memex"
source_project: "lqdev-me"
related_entries: pattern-generic-builder-content-processor, pattern-long-lived-umbrella-branch-merge-strategy, pattern-content-type-taxonomy-mismatch
---

## Discovery

The static-site generator had ~11 hand-written `buildX` functions in `Builder.fs`
(`buildSnippets`, `buildWikis`, `buildPosts`, `buildNotes`, `buildResponses`,
`buildBookmarks`, `buildPresentations`, `buildBooks`, `buildMedia`,
`buildAlbumCollections`, `buildPlaylistCollections`). Each was ~40 lines of the *same*
shape:

1. `Directory.GetFiles(srcDir/…)` → filter `.md` → list
2. create a `ContentProcessor<'T>` → `buildContentWithFeeds`
3. iterate, writing `outputDir/…/{FileName}/index.html` via a per-type view
4. write an index page (sometimes sorted)
5. return `FeedData<'T> list` for downstream feeds

This is assessment finding **F1** (duplicated build drivers). The [[pattern-generic-builder-content-processor]]
layer already unified *parse → render → feed*; the layer **above** it was still copy-paste.

## Root Cause

The differences between content types are small and **data-shaped**, not control-shaped:
which directory, which processor, which date/title/view, whether the index sorts. When the
only variation is data, N functions that differ only in data are N − 1 functions too many.
The duplication persisted because each `buildX` *reads* like it's doing something bespoke —
but the bespoke part is three or four values, not the algorithm.

## Solution

A single `buildContentType` driver that owns the algorithm, plus a declarative
`ContentTypeBuild<'T>` record per type. Per-type **rendering** stays as caller-supplied
closures, because only `Builder.fs` can see the Views layer / `convertMdToHtml` /
`RelatedContentService`. The driver (`BuildDriver.fs`) only orchestrates I/O.

```fsharp
type IndexConfig<'T> = {
    View: 'T array -> XmlNode
    Title: string
    Sort: ('T array -> 'T array) option        // None = source/feed order
}

type ContentTypeBuild<'T> = {
    Name: string                          // ContentTypes literal — identity/diagnostics
    SourceDir: string list                // segments under srcDir
    OutputDir: string list                // segments under outputDir
    Processor: GenericBuilder.ContentProcessor<'T>
    Slug: 'T -> string                    // page dir name (typically FileName)
    ItemView: 'T -> 'T array -> XmlNode   // item + siblings (for related content)
    ItemTitle: 'T -> string
    Layout: string
    Index: IndexConfig<'T> option         // None = individual pages only
}
```

A `buildSnippets` becomes ~15 declarative lines:

```fsharp
let buildSnippets() =
    BuildDriver.buildContentType srcDir outputDir {
        Name = ContentTypes.Snippets
        SourceDir = [ "resources"; "snippets" ]
        OutputDir = [ "resources"; "snippets" ]
        Processor = GenericBuilder.SnippetProcessor.create()
        Slug = fun s -> s.FileName
        ItemView = fun s siblings ->
            snippetPageView s.Metadata.Title (s.Content |> convertMdToHtml)
                s.Metadata.CreatedDate s.FileName (tagsOf s)
                (RelatedContentService.findRelatedContent s siblings 5)
        ItemTitle = fun s -> $"Snippet | {s.Metadata.Title} | Luis Quintanilla"
        Layout = "defaultindex"
        Index = Some { View = snippetsView; Title = "Snippets | Luis Quintanilla"; Sort = None }
    }
```

Three design moves let the **one** driver absorb **all 11** types (the 9/11 STOP rule was
never triggered):

- **Optional `Index`.** `buildBookmarks` sets `Index = None` — bookmarks own no listing
  page; the `/bookmarks/` landing page is built separately from bookmark-type *responses*
  (a taxonomy quirk; see [[pattern-content-type-taxonomy-mismatch]]). Grouping the three
  index fields into one optional record captured the only no-index case without a new
  top-level field.
- **`Directory.Exists` guard** in the driver (returns `[]` when the source dir is absent),
  preserving the safety the `albums`/`playlists` builders had.
- **AST-rendered `ItemView` via a pre-bound processor closure.** media/album/playlist render
  through `processor.Render content |> convertMdToHtml` (custom blocks), not the raw
  `.Content`. Binding `let processor = …create()` *before* the record and capturing it in
  the closure made these fit with zero new fields:

  ```fsharp
  let buildMedia() =
      let processor = GenericBuilder.AlbumProcessor.create()
      BuildDriver.buildContentType srcDir outputDir {
          ...
          Processor = processor
          ItemView = fun album _ ->
              mediaPageView album.Metadata.Title
                  (processor.Render album |> convertMdToHtml)
                  album.Metadata.Date album.FileName album.Metadata.Tags
          ...
      }
  ```

### Two gotchas worth remembering

- **Compile order.** The driver calls `ViewGenerator.generate`, so `BuildDriver.fs` must
  compile **after** `Views/Generator.fs` and **before** `Builder.fs` — i.e. *later* than the
  `GenericBuilder.fs` slot the assessment first sketched. F#'s top-down compilation makes
  module order part of the design, not an afterthought.
- **Path identity.** To stay byte-identical, fold segments through the 2-arg
  `Path.Join` (`List.fold (fun acc s -> Path.Join(acc, s)) head tail`) rather than relying
  on params-array overloads — same separator behavior as the old `Path.Join(a, b, c)` calls.

## Prevention

When you see N functions whose diff is "a directory string, a date field, a title format,
and maybe a sort," that's a **generators-as-data** smell — lift the algorithm into one
driver and make each call site a config value. Prior art: ionide/Fornax drives generation
from `GeneratorConfig` records; the difference here is compiled domain types per content
type instead of stringly-typed generators.

**Migrate under a byte-identical contract.** Each type was: add config → swap the call →
hash-diff fresh `_public/` against a baseline (excluding the one nondeterministic
`graph.json` timestamp) → commit → next. Migrating one (or one group) at a time behind a
verified-identical gate means a regression is always isolated to the last swap. This rode
on the [[pattern-long-lived-umbrella-branch-merge-strategy]] workflow: per-type commits on a
work branch, squash-merged into the umbrella as a single revertable unit.

**Leave the abstraction honest.** Keep a STOP rule (here: "if a builder needs >2 new config
fields, leave it hand-written"). The fact that it was *never* triggered is the signal the
abstraction matched reality — not a license to have forced it. Outcome: 11/11 migrated,
`Builder.fs` shrank ~390 lines, output unchanged. The `Name` literal is carried for
forward-compat with a future closed `ContentType` DU; it does not affect output today.
