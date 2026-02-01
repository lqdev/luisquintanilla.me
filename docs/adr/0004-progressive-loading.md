# Progressive Loading for Large Content Volumes

- **Status**: Accepted
- **Date**: 2025-07 (documented 2026-02-01)
- **Decision-makers**: Luis Quintanilla, GitHub Copilot
- **Consulted**: Performance testing

## Context and Problem Statement

The site has 1,130+ content items. Loading all items on the timeline page caused browser DOM parsing failures when content exceeded ~50 items with `rawText` rendering. How do we display large content volumes without breaking the browser?

## Decision Drivers

- Browser DOM parser limits with large HTML payloads
- Static site architecture (no server-side pagination)
- User experience for content browsing
- Filter functionality (content types)
- Performance on mobile devices

## Considered Options

1. **Server-side pagination with API calls**
2. **Artificial content limits (show only latest 50)**
3. **Progressive loading with intersection observer**
4. **Virtual scrolling with fixed viewport**

## Decision Outcome

**Chosen option**: "Progressive loading with intersection observer", because it maintains the static site architecture while providing excellent UX for browsing large content volumes.

### Consequences

**Good:**
- Handles any content volume (1,000+ items tested)
- Initial page load under 50KB
- Seamless infinite scroll experience
- Works with content type filters
- No API required (purely static)
- Maintains excellent performance

**Bad:**
- More complex JavaScript implementation
- JSON data must be properly escaped
- Progressive content must respect filter state

### Confirmation

- Timeline loads correctly with 1,130+ items
- No JavaScript loading failures
- Filter buttons work with progressively loaded content
- Mobile performance validated

## Implementation Details

### Architecture

```
+-------------------+     +----------------+     +------------------+
| F# Builder        | --> | JSON Data      | --> | JavaScript       |
| (Server-side)     |     | (Static file)  |     | (Client-side)    |
+-------------------+     +----------------+     +------------------+
                                |
                                v
                    +----------------------+
                    | Intersection Observer |
                    | + Manual Load Button |
                    +----------------------+
```

### Loading Strategy

1. **Initial Load**: 50 items rendered in HTML (server-side)
2. **JSON Remainder**: All other items as escaped JSON in script tag
3. **Chunked Loading**: 25 items per chunk via intersection observer
4. **Manual Trigger**: "Load More" button as fallback

### JSON Escaping (Critical)

All special characters must be escaped:
- `\"` for quotes
- `\n`, `\r`, `\t` for whitespace
- `\\` for backslashes

### Filter Integration

```javascript
const includeItem = ['star', 'reply', 'reshare', 'responses'].includes(cardType);
```

## Root Cause Discovery

**Symptom**: Scripts visible in HTML source but absent from browser Network tab, zero JavaScript execution.

**Cause**: Large content arrays with `rawText` rendering produce malformed HTML that exceeds browser parser limits, causing complete DOM parsing failure.

**Solution**: Server-side content limiting + client-side progressive loading.

## More Information

- Implementation in `Views/CollectionViews.fs`
- JavaScript in `_src/js/timeline.js`
- Documentation: `docs/content-volume-html-parsing-discovery.md`
