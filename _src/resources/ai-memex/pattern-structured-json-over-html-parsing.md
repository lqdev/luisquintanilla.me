---
title: "Pattern: Surface structured metadata instead of parsing HTML twice"
description: "When the same semantic value is rendered server-side and consumed client-side, expose it as structured JSON rather than duplicating HTML parsing logic in JavaScript."
entry_type: pattern
published_date: "2026-07-03 15:25 -05:00"
last_updated_date: "2026-07-03 15:25 -05:00"
tags: "fsharp, javascript, web, patterns"
related_skill: ""
source_project: "lqdev-me"
---

## Discovery

On the lqdev.me homepage timeline, review cards showed the correct specific type for a recent movie ("Movie") but fell back to the generic "Review" for an older book ("We"). Both reviews use the same `:::review` block with `itemType: "movie"` / `itemType: "book"`, so the data was clearly present somewhere.

## Root Cause

The homepage has two rendering paths for timeline cards:

1. **Server-side (F# / Giraffe ViewEngine)** — the newest N items are rendered as HTML by `Views/TimelineViews.fs`. Its `extractReviewItemType` parses the hidden `data-item-type="…"` span emitted by `ReviewProcessor.RenderCard`, so recent reviews showed the right badge.
2. **Client-side (JavaScript)** — older items are embedded as JSON (`<script id="remainingContentData-reviews">`) and rendered by `_src/js/timeline.js`. The JS version of `extractReviewItemType` only parsed the *legacy* `item-type-badge badge bg-secondary">` pattern. The server had switched to the new `data-item-type` hidden span, so the JS parser returned `null` and the badge fell back to `"Review"`.

The real bug was **two divergent HTML parsers** that had to be kept in sync. The recent movie happened to be server-rendered; the older book was client-rendered, exposing the mismatch.

## Solution

Stop parsing HTML in the client. Add a structured field to the progressive-loading JSON model and let the server compute the value once using the existing (correct) parser.

### F# model change

`Views/TimelineViews.fs`:

```fsharp
type ProgressiveContentItem =
    { title: string
      contentType: string
      date: string
      url: string
      content: string
      rsvpStatus: string
      reviewItemType: string   // "Book", "Movie", "" for non-reviews
      tags: string[] }
```

Populate it when serializing remaining items:

```fsharp
{ title = item.Title
  contentType = item.ContentType
  date = item.Date
  url = properPermalink
  content = item.BodyHtml.Value
  rsvpStatus = (match item.RsvpStatus with Some s -> s | None -> "")
  reviewItemType =
      (if item.ContentType = "reviews" then
           extractReviewItemType item.Content |> Option.defaultValue ""
       else "")
  tags = item.Tags }
```

### JavaScript change

`_src/js/timeline.js`:

```js
const contentTypeBadge = (() => {
    if (item.contentType === 'reviews') {
        // Structured field emitted server-side
        return item.reviewItemType || 'Review';
    }
    // ... other content types
})();
```

Then remove the now-unused `extractReviewItemType` method entirely.

## Prevention

- **One source of truth**: compute metadata server-side and ship it as data, not as markup to be reverse-engineered.
- **Avoid duplicating parsers**: if F# and JS both need the same value, put it in the JSON payload rather than parsing the same HTML twice in two languages.
- **Audit existing HTML scraping in JS**: search for `indexOf`, `innerHTML` slicing, or regex parsing in `_src/js/*.js`; these are candidates for structured fields.
- **When markup must carry data**, use `data-*` attributes *and* expose the same value in JSON, so the client never needs to parse the DOM string.
