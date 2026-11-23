# Reviews schema

The review pipeline now uses one shared review schema for every review type.

## Unified review block shape

```yaml
:::review
item: "The item being reviewed"
itemType: "movie"
rating: 4.5
scale: 5.0
summary: "Short summary"
pros:
  - "Pros"
cons:
  - "Cons"
itemUrl: "https://example.com"
imageUrl: "https://example.com/cover.jpg"
additionalFields:
  director: "Director Name"
  year: 2024
  genre: "Thriller"
:::
```

The page hero, listing cards, RSS title generation, and ActivityPub Schema.org output all read from the same normalized shape.

## Registry of supported fields

`ReviewSchema.fs` is the single source of truth. The table below matches the current registry:

| Review type | Supported fields |
| --- | --- |
| book | author, isbn, genre |
| movie | director, year, genre |
| music | artist, music_type, release_year, genre, label |
| business | business_type, location, price_range |
| product | manufacturer, product_category, model_version, price |

## Rendering pipeline

1. The issue workflow and snippets emit `additionalFields` for type-specific metadata.
2. `CustomBlocks.fs` parses the review block into `BaseReviewData`.
3. `ReviewProcessor.fs` normalizes the block into `ReviewMetadata` for the page hero, RSS, and ActivityPub.
4. `Views/LayoutViews.fs` renders one hero-based review page and strips the raw review block from the main prose content.
5. `Views/ContentViews.fs` uses the same registry to render the listing-card subtitle.

## Notes

- The registry controls display labels and Schema.org role extraction.
- Book reviews no longer need top-level `author`, `isbn`, or `datePublished` fields in the review block.
- The generic `additionalFields` map is the canonical storage location for per-type metadata.
