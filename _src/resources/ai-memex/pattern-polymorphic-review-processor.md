---
title: "Polymorphic Review Processor"
description: "Use a single Review type and processor to handle books, movies, music, businesses, and products when source files already carry an itemType discriminator."
entry_type: pattern
published_date: "2026-07-02 22:33 -05:00"
last_updated_date: "2026-07-02 22:33 -05:00"
tags: "fsharp, dotnet, architecture, patterns, web"
related_skill: "write-ai-memex"
source_project: "lqdev-me"
---

## Discovery

A movie review added via a GitHub issue workflow landed in `_src/reviews/movies/` but was invisible on `/reviews/`, the RSS feed, and its direct permalink. The generator only read `_src/reviews/library/` and parsed every file as a `BookDetails` record, so the new file was never ingested. Even if it had been read, the frontmatter schema (`title`, `published_date`, `tags`) did not match the book schema (`author`, `isbn`, `cover`, `rating`, `source`, `date_published`).

## Root Cause

The publishing workflow and the static-site generator had diverged:

1. **Directory mismatch** — `Loaders.loadBooks` and `ContentTypePagesBuilder.buildBooks` hard-coded `reviews/library`.
2. **Schema mismatch** — new reviews used post-like frontmatter and a `:::review` block with `itemType` and `additionalFields`.
3. **Type explosion avoided** — the differences between book, movie, music, business, and product reviews are cosmetic (which metadata fields to render), not structural enough to justify five parallel processors.

## Solution

Introduce a single `Review` domain type and a single `ReviewProcessor` that scans all review subdirectories and normalizes every file into the same shape.

### Domain model

```fsharp
// Domain.fs
type ReviewDetails = {
    Title: string
    PublishedDate: string
    Tags: string array
    // Backward-compat fields for legacy book reviews
    Author: string
    Isbn: string
    Cover: string
    Source: string
    Rating: float
    DatePublished: string
}

type Review = {
    FileName: string
    Metadata: ReviewDetails
    Content: string
    MarkdownSource: string option
    ItemType: string
    AdditionalFields: Map<string, string>
}
    interface ITaggable with
        member this.Tags = this.Metadata.Tags
```

### Processor

```fsharp
// Processors/ReviewProcessor.fs
let create() : ContentProcessor<Review> = {
    Parse = fun filePath ->
        // 1. Parse frontmatter into ReviewDetails
        // 2. Parse :::review block if present
        // 3. Normalize ItemType from review block (default "book")
        // 4. Build AdditionalFields map
        // 5. Cache ReviewMetadata for ActivityPub / Schema.org
    Render = // ...
    OutputPath = fun review -> sprintf "reviews/%s.html" review.FileName
    RenderCard = // ...
    RenderRss = // ...
}
```

The processor scans `_src/reviews/` recursively, so `library/`, `movies/`, `music/`, `business/`, and `products/` are all picked up automatically.

### Backward compatibility

Legacy book files without a `:::review` block are synthesized from frontmatter:

```fsharp
let mutable fields = Map.empty
if not (String.IsNullOrWhiteSpace(metadata.Author)) then
    fields <- fields.Add("author", metadata.Author)
// ... isbn, cover, source, rating, datePublished
("book", fields)
```

### View-layer specialization

Views pattern-match on `Review.ItemType` to render type-specific metadata instead of maintaining separate page types:

```fsharp
match review.ItemType with
| "book" ->
    // render author / ISBN
| "movie" ->
    // render director / year / genre from AdditionalFields
| "music" ->
    // render artist / genre
| _ ->
    // render any additionalFields as key/value pairs
```

## Prevention

- When a publishing workflow writes to new subdirectories, update the generator's source paths at the same time.
- If source files already carry a discriminator (`itemType`), prefer one polymorphic processor over N parallel processors. It keeps `ITaggable`, feeds, tags, and search integration trivial.
- Validate that a sample file from each new subdirectory actually appears in the generated output before considering the feature shipped.

## Known Gotcha: Nested YAML in Custom Blocks

The `:::review` block uses `additionalFields` with nested key/value pairs:

```yaml
additionalFields:
  director: "Stephen Cognetti"
  year: "2025"
  genre: "horror,mystery"
```

Markdig's `CustomBlockParser.TryContinue` collects block content via `processor.Line`, whose slice has already had leading indentation stripped. The collected `RawContent` therefore flattens the nested map:

```yaml
additionalFields:
director: "Stephen Cognetti"
year: "2025"
genre: "horror,mystery"
```

YamlDotNet ignores the orphaned keys, so `additionalFields` ends up empty. Fixing this requires preserving the original source indentation when collecting custom-block lines. Tracked in GitHub issue #2578.

## Related

- `Processors/ReviewProcessor.fs`
- `Domain.fs` (`ReviewDetails`, `Review`)
- `Builders/ContentTypePages.fs` (`buildReviews`)
- `UnifiedFeeds.fs` (`convertReviewsToUnified`)
- `Views/LayoutViews.fs` (`reviewPageView`)
