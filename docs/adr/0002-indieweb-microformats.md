# IndieWeb Microformats Integration

- **Status**: Accepted
- **Date**: 2024 (documented 2026-02-01)
- **Decision-makers**: Luis Quintanilla
- **Consulted**: IndieWeb community standards

## Context and Problem Statement

Personal websites need to participate in the decentralized social web while maintaining ownership of content. How do we structure HTML output to be machine-readable while remaining human-friendly?

## Decision Drivers

- Content ownership and POSSE (Publish Own Site, Syndicate Elsewhere) principles
- Interoperability with IndieWeb ecosystem (webmentions, readers, etc.)
- Federation with Mastodon and ActivityPub networks
- Semantic HTML structure for accessibility
- Machine-readable content for aggregators

## Considered Options

1. **Plain HTML without structured data**
2. **JSON-LD structured data**
3. **Microformats2 (h-entry, h-card, etc.)**
4. **Microdata (Schema.org)**

## Decision Outcome

**Chosen option**: "Microformats2", because it provides the widest compatibility with IndieWeb tools, requires minimal markup overhead, and integrates naturally with existing HTML structure.

### Consequences

**Good:**
- Full compatibility with IndieWeb parsers and readers
- Webmention support for distributed conversations
- Clean integration with existing HTML (class-based)
- Activity types map well to response types (likes, reposts, replies)
- RSVP support for event responses

**Bad:**
- Requires careful attention to markup structure
- Less mainstream than JSON-LD
- Some properties must be hidden for clean visual design

### Confirmation

- Content successfully parsed by IndieWeb parsers
- Webmentions sent and received correctly
- RSS readers properly interpret content
- ActivityPub federation working with Mastodon

## Implementation Details

### Core Microformats Used

- **h-entry**: Individual content entries (posts, notes, responses)
- **h-card**: Author information
- **h-feed**: Content feeds
- **u-url, u-uid**: Permalink and unique identifiers
- **dt-published**: Publication dates
- **p-name, e-content**: Titles and content
- **p-rsvp**: RSVP status for events

### Response Types

| Type | Microformat Properties |
|------|----------------------|
| Like | u-like-of |
| Reply | u-in-reply-to |
| Repost | u-repost-of |
| Bookmark | u-bookmark-of |
| RSVP | p-rsvp, u-in-reply-to |

### Hidden Microformats Pattern

```html
<span class="microformat-hidden">
  <a class="u-author h-card" href="...">
    <img class="u-photo" src="..." alt="">
    <span class="p-name">Author Name</span>
  </a>
</span>
```

## More Information

- IndieWeb wiki: https://indieweb.org/microformats
- Microformats2 specification: https://microformats.org/wiki/microformats2
- Implementation in `Views/LayoutViews.fs` and `Views/ContentViews.fs`
