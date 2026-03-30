# ADR-0001: Unified Content Processing with GenericBuilder

## Status
Accepted

## Context

The site supports 11+ content types: posts, notes, snippets, wikis, presentations, books (reviews), responses, albums (media), album collections, playlist collections, and bookmarks. Each content type needs the same pipeline: parse markdown with YAML frontmatter, render to HTML, generate individual pages, produce RSS feed XML, and create card HTML for index/timeline views. Before the GenericBuilder pattern, each content type had its own bespoke build logic, leading to inconsistencies in RSS generation, card rendering, and output structure.

## Decision

Introduce a generic `ContentProcessor<'T>` record type in `GenericBuilder.fs` that defines the full processing pipeline for any content type:

```fsharp
type ContentProcessor<'T> = {
    Parse: string -> 'T option
    Render: 'T -> string
    OutputPath: 'T -> string
    RenderCard: 'T -> string
    RenderRss: 'T -> XElement option
}
```

A single function `buildContentWithFeeds<'T>` takes a processor and a list of file paths, returning `FeedData<'T> list` where each item bundles the parsed content, card HTML, and RSS XML:

```fsharp
type FeedData<'T> = {
    Content: 'T
    CardHtml: string
    RssXml: XElement option
}
```

Each content type defines a processor module (`PostProcessor`, `NoteProcessor`, `SnippetProcessor`, `WikiProcessor`, `PresentationProcessor`, `BookProcessor`, `ResponseProcessor`, `AlbumProcessor`, `AlbumCollectionProcessor`, `PlaylistCollectionProcessor`, `BookmarkProcessor`) with a `create()` function that returns a configured `ContentProcessor<'T>`.

A helper function `processAllContent<'T>` handles the full workflow of scanning a source directory, running `buildContentWithFeeds`, writing individual HTML files, and returning `FeedData` for downstream aggregation.

The `UnifiedFeeds` module then converts type-specific `FeedData` into `UnifiedFeedItem` records for cross-type operations like the fire-hose RSS feed, tag feeds, timeline rendering, search indexing, and ActivityPub content generation. Converter functions (`convertPostsToUnified`, `convertNotesToUnified`, `convertResponsesToUnified`, etc.) handle the mapping, while `buildAllFeeds` and `buildTagFeeds` generate the actual RSS XML output.

## Consequences

**Easier:**
- Adding a new content type requires only defining a new processor module with a `create()` function (~20 minutes of work). The rest of the pipeline — RSS, search indexing, timeline inclusion, tag feeds — comes automatically.
- All content types get consistent RSS feed structure, URL normalization (via `normalizeUrlsForRss`), and source markdown inclusion (via `generateSourceMarkdown`).
- The `UnifiedFeedItem` type provides a single representation for cross-cutting concerns like ActivityPub, search indexes, and the text-only site.

**More difficult:**
- Processor configuration requires understanding multiple rendering concerns (card HTML, RSS XML, output paths) upfront.
- The `UnifiedFeeds` module requires a new converter function for each content type, adding a small coordination cost.
- Domain types must implement `ITaggable` (defined in `Domain.fs`) to participate in the unified tag system.
