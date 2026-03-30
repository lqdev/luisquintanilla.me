# ADR-0002: IndieWeb Microformats2 Compliance

## Status
Accepted

## Context

As a personal website, lqdev.me should be a first-class citizen of the IndieWeb ecosystem — discoverable by feed readers, able to send and receive webmentions, and machine-readable by IndieWeb tools like webmention.io, Bridgy, and Microsub servers. This requires structured markup that IndieWeb parsers can understand without relying on proprietary APIs.

## Decision

All content pages use Microformats2 markup rendered via Giraffe ViewEngine in `LayoutViews.fs`. The microformat vocabulary is applied consistently across every content type view function (`mediaPageView`, `snippetPageView`, `wikiPageView`, `bookPageView`, `presentationPageView`, `responsePostView`, and timeline card views).

**Entry markup** — every content item is wrapped in an `h-entry`:
- `article [ _class "h-entry individual-post" ]` — the entry container
- `h1 [ _class "p-name post-title" ]` — the entry title
- `time [ _class "dt-published"; attr "datetime" date ]` — publication date
- `div [ _class "e-content post-content" ]` — the entry content
- `a [ _class "u-url permalink-link" ]` — canonical permalink

**Author markup** — a hidden `h-card` is embedded in every entry for parser compliance:
```fsharp
div [ _class "u-author h-card microformat-hidden" ] [
    img [ _src "/avatar.png"; _class "u-photo"; _alt "Luis Quintanilla" ]
    a [ _href "/about"; _class "u-url p-name" ] [ Text "Luis Quintanilla" ]
]
```
The `microformat-hidden` CSS class hides the card visually while keeping it accessible to parsers.

**Response microformats** — the `responsePostView` function uses type-specific microformat classes for social interactions:
- `"reply"` → `a [ _class "u-in-reply-to" ]` — reply to another post
- `"reshare" | "share"` → `a [ _class "u-repost-of" ]` — repost/reshare
- `"star"` → `a [ _class "u-like-of" ]` — like/favorite
- `"bookmark"` → `a [ _class "u-bookmark-of" ]` — bookmark
- `"rsvp"` → `a [ _class "u-in-reply-to" ]` with `span [ _class "p-rsvp" ]` — event RSVP with status

**Timeline cards** also carry microformat classes:
- `_class "h-entry content-card"` on each card
- `_class "dt-published publication-date"` on timestamps
- `_class "p-name card-title"` on card titles
- `_class "u-url title-link"` on card links
- `_class "e-content card-content"` on card content
- `_class "p-category tags"` on tag containers

## Consequences

**Easier:**
- Content is machine-readable by any Microformats2 parser. Feed readers, webmention endpoints, and IndieWeb tools can extract structured data without custom integrations.
- Webmention support works out of the box — the `u-in-reply-to`, `u-like-of`, `u-repost-of`, and `u-bookmark-of` classes allow webmention senders to identify interaction types.
- The hidden `h-card` pattern provides author attribution without visual redundancy on every page.

**More difficult:**
- Every new view function must include the correct microformat classes — missing a `p-name` or `dt-published` breaks parser expectations.
- The hidden author `h-card` pattern requires maintaining the `microformat-hidden` CSS class and testing with parsers to ensure it remains discoverable.
- Changes to HTML structure must preserve microformat class placement, adding a constraint to frontend refactoring.
