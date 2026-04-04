---
title: "Pattern: GenericBuilder ContentProcessor"
description: "A generic record type providing a consistent interface for parsing, rendering, and feed generation across all content types in the F# static site generator."
entry_type: pattern
published_date: "2026-04-01 00:00 -05:00"
last_updated_date: "2026-04-01 00:00 -05:00"
tags: fsharp, architecture, patterns, lqdev-me
related_skill: ""
source_project: lqdev-me
related_entries: pattern-viewengine-integration, pattern-content-type-landing-page, pattern-feed-architecture-consistency, codebase-context
---

## Discovery

As the site grew from a few content types (posts, notes) to over a dozen (responses, snippets, wiki entries, albums, playlists, bookmarks, AI Memex entries, presentations, books), the build pipeline accumulated repetitive code. Each content type needed the same sequence of operations: parse a markdown file into a domain type, render it to HTML, generate a card for index pages, produce an RSS feed entry, and determine the output file path. Without a shared abstraction, each content type reimplemented this sequence with slight variations, leading to inconsistencies and maintenance burden.

## Root Cause / Problem

Static site generators with multiple content types face a fundamental tension: each type has unique domain data (a post has reading time; an album has GPS coordinates; a response has a target URL), but the processing pipeline is structurally identical. Parse the file. Render the page. Generate the card. Build the feed. Write the output.

Without a generic interface, adding a new content type means copying the build logic from an existing type and modifying it — a process that's error-prone and creates N copies of essentially the same pipeline code. When the pipeline needs to change (e.g., adding text-only rendering or search index generation), every content type's builder must be updated individually.

## Solution

The `ContentProcessor<'T>` record type in GenericBuilder.fs provides a generic interface that every content type implements:

```fsharp
type ContentProcessor<'T> = {
    /// Parse content from file path to domain type
    Parse: string -> 'T option
    /// Render content to final HTML output
    Render: 'T -> string
    /// Generate output file path from content
    OutputPath: 'T -> string
    /// Generate card HTML for index pages
    RenderCard: 'T -> string
    /// Generate RSS XML element
    RenderRss: 'T -> XElement option
}
```

Each content type creates its processor through a module with a `create()` function. For example, the Post processor:

```fsharp
module PostProcessor =
    let create() : ContentProcessor<Post> = {
        Parse = fun filePath ->
            match parsePostFromFile filePath with
            | Ok parsedDoc -> Some parsedDoc
            | Error _ -> None
        Render = fun post -> renderPostToHtml post
        OutputPath = fun post -> post.Slug + "/index.html"
        RenderCard = fun post -> renderPostCard post
        RenderRss = fun post -> Some (generatePostRssItem post)
    }
```

The generic `buildContentWithFeeds` function then processes any content type uniformly:

```fsharp
let buildContentWithFeeds<'T> 
    (processor: ContentProcessor<'T>) 
    (filePaths: string list) : FeedData<'T> list =
    filePaths
    |> List.choose (fun filePath ->
        match processor.Parse filePath with
        | Some content ->
            let cardHtml = processor.RenderCard content
            let rssXml = processor.RenderRss content
            Some { Content = content; CardHtml = cardHtml; RssXml = rssXml }
        | None -> None)
```

The `ContentPipeline` module provides an even higher-level abstraction that handles directory scanning, file generation, and output management:

```fsharp
module ContentPipeline =
    let processAllContent<'T> 
        (processor: ContentProcessor<'T>) 
        (sourceDirectory: string) 
        (outputDirectory: string) =
        let files = Directory.GetFiles(sourceDirectory, "*.md")
        let feedData = buildContentWithFeeds processor (Array.toList files)
        // Generate individual HTML files
        // Collect feed data for RSS generation
        feedData
```

### Adding a New Content Type

Adding a new content type follows a predictable sequence:

1. Define the domain type in `Domain.fs` (e.g., `type Recipe = { Title: string; PrepTime: int; ... }`)
2. Create a parser in `Loaders.fs` that reads YAML frontmatter into the domain type
3. Create a processor module in `GenericBuilder.fs` with `create() : ContentProcessor<Recipe>`
4. Add view functions in the appropriate Views module for rendering
5. Call `ContentPipeline.processAllContent` in `Program.fs`

The processor module is the integration point — it connects the domain type, parser, views, and feed generation into a single record that the generic pipeline consumes.

## Key Components

- **GenericBuilder.fs**: Defines `ContentProcessor<'T>`, `FeedData<'T>`, `buildContentWithFeeds`, and the `ContentPipeline` module
- **Domain.fs**: All content type definitions that serve as the `'T` parameter
- **Loaders.fs**: File parsing functions that each processor's `Parse` field calls
- **Views/ modules**: Rendering functions that each processor's `Render`, `RenderCard`, and `RenderRss` fields call
- **Program.fs**: Build orchestration that creates processors and feeds them to the pipeline

Currently implemented processors: `PostProcessor`, `NoteProcessor`, `SnippetProcessor`, `WikiProcessor`, `AiMemexProcessor`, `PresentationProcessor`, `BookProcessor`, `ResponseProcessor`, `AlbumProcessor`, `AlbumCollectionProcessor`, `PlaylistCollectionProcessor`, `BookmarkProcessor`.

## Results

- **12+ content types** share a single processing pipeline with zero duplicated orchestration code
- **Consistent behavior**: Every content type gets HTML pages, card rendering, and RSS feed generation through the same code path
- **Type safety**: The F# type system ensures each processor's functions are compatible with its domain type — a `ContentProcessor<Post>` cannot accidentally use an `Album` renderer
- **Rapid addition**: New content types (like AI Memex entries and bookmarks) were added by implementing a single processor module, not by copying and modifying an entire build pipeline

## Benefits

The ContentProcessor pattern is the architectural backbone of the site's build system. It transforms the problem of "N content types × M pipeline stages" from N×M separate implementations into N processor definitions plus one shared pipeline. When a new pipeline stage is needed (text-only rendering, search indexing), it can be added to the generic pipeline and immediately benefit all content types. When a new content type is added, it gets all existing pipeline capabilities for free. The pattern also makes the codebase navigable — understanding one processor module means understanding them all, because they share the same structure.
