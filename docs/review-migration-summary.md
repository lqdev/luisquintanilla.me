# Review migration summary

The review system now uses a single normalized schema for every review type.

## What changed

- The legacy book-only review shape was removed from the rendering path.
- Review metadata now flows through `ReviewSchema.fs` and `BaseReviewData`.
- Book reviews use the same `additionalFields` map as movies, music, business, and product reviews.
- The individual review page now renders one hero block with title, badge, date, rating, metadata fields, summary, pros/cons, and a view-item link.
- The raw review block is stripped from prose content so the title appears only once.

## Canonical field map

- `book`: `author`, `isbn`, `genre`
- `movie`: `director`, `year`, `genre`
- `music`: `artist`, `music_type`, `release_year`, `genre`, `label`
- `business`: `business_type`, `location`, `price_range`
- `product`: `manufacturer`, `product_category`, `model_version`, `price`

## Example

```yaml
:::review
item: "The Matrix"
itemType: "movie"
rating: 4.5
scale: 5.0
summary: "A landmark sci-fi film."
itemUrl: "https://www.imdb.com/title/tt0133093/"
imageUrl: "https://example.com/poster.jpg"
additionalFields:
  director: "Lana Wachowski"
  year: 1999
  genre: "Sci-Fi"
:::
```
