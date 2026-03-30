# ADR-0004: Progressive Loading for Large Content Sets

## Status
Accepted

## Context

With 1000+ content items, rendering all items as server-side HTML in a single timeline page caused browser HTML parser failures. The DOM parser would choke on the large HTML payload, preventing JavaScript from loading entirely — not just slow rendering, but complete failure where script tags present in the source never appeared in the browser's Network tab. No JavaScript executed at all, breaking filtering, theme switching, and all interactive features.

## Decision

Implement a server-side JSON generation + client-side progressive loading strategy in `LayoutViews.fs`. The first 50 items are rendered as full HTML in the page. Remaining items are serialized as a JSON array embedded in a `<script type="application/json">` tag and loaded progressively by client-side JavaScript.

**Server-side implementation** (in `timelineHomeView` and `timelineHomeViewStratified`):

1. Render initial items as full `h-entry` HTML cards (first 50 items):
   ```fsharp
   article [ _class "h-entry content-card" ] [ ... ]
   ```

2. Create a progressive loading container:
   ```fsharp
   div [ _class "progressive-content"; _id "progressiveContent" ] []
   ```

3. If items exceed 50, serialize remaining items as JSON with comprehensive escaping:
   ```fsharp
   let escapeJson (text: string) =
       text.Replace("\\", "\\\\")
           .Replace("\"", "\\\"")
           .Replace("\b", "\\b")
           .Replace("\f", "\\f")
           .Replace("\n", "\\n")
           .Replace("\r", "\\r")
           .Replace("\t", "\\t")
   ```

4. Embed the JSON data in the page:
   ```fsharp
   script [ _type "application/json"; _id "remainingContentData" ] [
       rawText $"[{remainingItemsJson}]"
   ]
   ```

5. Include a manual "Load More" button as fallback:
   ```fsharp
   div [ _class "load-more-section"; _id "loadMoreSection" ] [
       button [ _class "load-more-btn"; _id "loadMoreBtn" ] [ ... ]
   ]
   ```

**JSON item structure**: Each remaining item is serialized with `title`, `contentType`, `date`, `url`, `content` (HTML), and `tags` array. Content is processed through the same pipeline as initial items (markdown-to-HTML conversion, article wrapper removal, title stripping, script tag escaping).

**Client-side**: JavaScript reads the `#remainingContentData` JSON, renders items in chunks (25 at a time) via intersection observer, and integrates with the existing content type filter system.

**Stratified view** (`timelineHomeViewStratified`): Groups remaining items by content type, generating separate JSON blocks per type for type-aware progressive loading.

## Consequences

**Easier:**
- Handles any content volume — the initial HTML payload stays constant at ~50 items regardless of total content count.
- Filter integration works seamlessly — progressively loaded items carry `contentType` metadata and respect the active filter state.
- No external dependencies — the JSON is embedded in the page, requiring no API calls or separate data files.
- Graceful degradation — the first 50 items are always visible even without JavaScript.

**More difficult:**
- Content is processed twice in different formats — once as ViewEngine HTML for initial items, once as escaped JSON strings for remaining items.
- JSON escaping must be comprehensive — any unescaped character in markdown content can break the JSON parser and prevent loading.
- The "Load More" button and intersection observer add client-side complexity that must stay in sync with server-side card rendering.
- Review content requires special handling (`createSimplifiedReviewContent`) to produce consistent output across initial and progressive items.
