# RSS Feed Architecture Documentation

## Overview

The website implements a comprehensive RSS feed system providing multiple types of feeds for content discovery and syndication. All feeds follow RSS 2.0 specification with proper category metadata for filtering and organization. **Historical date accuracy** ensures all feeds display creation/publication dates from Git history instead of current date fallbacks.

## Feed Quality Standards

### Date Accuracy (Enhanced 2025-07-25)
All RSS feeds use historical dates extracted from Git history to ensure accurate timeline representation:

- **pubDate Elements**: Every feed item includes proper `<pubDate>` with historical creation/publication dates
- **Git History Integration**: Retroactive date extraction for content without metadata using `git log --all --full-history`
- **Date Field Schema**: Content-appropriate date fields (created_date, last_updated_date, date_published, date)
- **Timezone Consistency**: All dates formatted with `-05:00` timezone specification
- **No Current Date Fallbacks**: Zero instances of `DateTime.Now` in RSS generation

### Content Type Date Sources
| Content Type | Date Field | Source | Range |
|--------------|------------|--------|-------|
| Snippets | `created_date` | Git history | 2022-2025 |
| Wiki | `last_updated_date` | Git history | 2022-2025 |
| Presentations | `date` | Git history | 2021-2022 |
| Books/Reviews | `date_published` | Git history | 2022-2025 |
| Posts | `date` | Frontmatter | Various |
| Notes | `date` | Frontmatter | Various |
| Responses | `date` | Frontmatter | Various |
| Media | `date` | Frontmatter | Various |

## Feed Types

### 1. Content Type Feeds
Each content type has a dedicated RSS feed located content-proximate to the content:

| Content Type | Feed URL | Content Included |
|--------------|----------|------------------|
| Posts | `/posts/feed.xml` | Blog posts and articles |
| Notes | `/notes/feed.xml` | Short-form content and microblog entries |
| Responses | `/responses/feed.xml` | IndieWeb responses and webmentions |
| Snippets | `/resources/snippets/feed.xml` | Code snippets and technical examples |
| Wiki | `/resources/wiki/feed.xml` | Knowledge base articles |
| Presentations | `/resources/presentations/feed.xml` | Slide decks and talks |
| Reviews | `/reviews/feed.xml` | Book reviews and recommendations |
| Media | `/media/feed.xml` | Photo albums and visual content |

### 2. Unified Feeds
- **Unified Feed**: `/feed/feed.xml` - Fire-hose feed including all content types chronologically
- **User-Friendly Alias**: `/all.rss` - Easy-to-remember URL for unified feed subscription
- **Legacy Support**: `/feed/index.xml` - Maintained for backward compatibility
- **Individual Aliases**: `/blog.rss`, `/microblog.rss`, `/responses.rss` - Content-specific feed aliases

**Pattern Consistency (Enhanced 2025-08-05)**:
All feeds follow uniform URL structure `/[type]/feed.xml → /[alias].rss` for consistency:
- Unified feed moved from `/feed/index.xml` to `/feed/feed.xml` following established pattern
- Prominent subscription hub placement as "Everything Feed" for improved discoverability
- OPML integration with unified feed as first entry in feeds.json
- Backward compatibility maintained through dual file generation

### 3. Tag-Based Feeds
**Implementation**: 2025-07-25  
**Coverage**: 1,187 individual tag feeds  
**URL Pattern**: `/tags/{tagname}/feed.xml`

Each tag feed includes:
- All content types that have been tagged with the specific tag
- Chronological ordering (newest first)
- RSS 2.0 compliance with proper category metadata
- 20-item limit for performance optimization

**Example Tag Feeds**:
- `/tags/fsharp/feed.xml` - All F# related content
- `/tags/ai/feed.xml` - All AI/ML related content
- `/tags/indieweb/feed.xml` - All IndieWeb related content

## Technical Implementation

### RSS Category Metadata
All RSS feeds include proper `<category>` elements for content organization:

```xml
<item>
    <title>Example Post Title</title>
    <link>https://example.com/posts/example-post/</link>
    <description>Post content...</description>
    <category>fsharp</category>
    <category>programming</category>
    <category>dotnet</category>
    <pubDate>Thu, 25 Jul 2025 10:00:00 GMT</pubDate>
</item>
```

### Reply Threading Metadata
RSS items for responses whose `response_type` is `reply` and whose `targeturl` is a well-formed absolute HTTP(S) URL include one `thr:in-reply-to` element in the namespace `http://purl.org/syndication/thread/1.0`. The item's target URL is written to both the required `ref` attribute and the optional `href` attribute. Other response types do not receive this element. Invalid or blank targets are omitted without changing the rest of the item.

This reuses the threading vocabulary defined by [RFC 4685](https://www.rfc-editor.org/rfc/rfc4685.html) through RSS 2.0's [namespaced extension mechanism](https://www.rssboard.org/rss-specification). It is not a native RSS element, nor does RFC 4685 specify RSS threading. The `thr` prefix is only a serialization label; the namespace URI identifies the vocabulary.

The existing response metadata contains a target URL but no separate persistent identifier for the parent. Consequently, `ref` and `href` are mapped to that same URL. This is appropriate for interoperability when the parent's persistent identifier is its permalink; it does not establish that arbitrary webpage URLs are interchangeable with opaque parent identifiers. No identifier resolution or network requests are performed during the static build. Existing item links, historical GUIDs, dates, categories, content, and `source:markdown` remain unchanged.

#### Manual RSC interoperability smoke test
1. Choose an original public post on the target RSC instance whose persistent parent identifier is its permalink.
2. Have the instance subscribe to and approve the site's responses feed (`/responses/feed.xml`, also available at `/responses.rss`).
3. Publish a normal site response with `response_type: reply` and `targeturl` set to that permalink.
4. Allow or trigger feed acquisition, then verify the reply is attached under the correct original parent rather than appearing only as a standalone imported item.

The metadata expresses the relationship; it does not notify RSC or discover the publisher. Feed subscription and ingestion are separate requirements. This feed change does not receive replies back onto this site.

### Feed Generation Architecture
**Location**: `GenericBuilder.fs` - Unified feed processing system

**Processors with Category Support**:
- `PostProcessor` - Blog posts and articles
- `NoteProcessor` - Short-form content
- `SnippetProcessor` - Code examples
- `ResponseProcessor` - IndieWeb responses
- `AlbumProcessor` - Media content
- `WikiProcessor` - Knowledge base
- `PresentationProcessor` - Slide decks
- `BookProcessor` - Reviews and recommendations

### Performance Characteristics
- **Build Time Impact**: Minimal - feeds generated during content processing
- **Feed Size Optimization**: 20-item limits prevent oversized feeds
- **Memory Efficiency**: Single-pass processing through unified system
- **Tag Processing**: 1,187 tag feeds generated efficiently

## Feed Discovery

### Content-Proximate Strategy
Feeds are located with their content for intuitive discovery:
- **Content**: `/posts/example-post/`
- **Feed**: `/posts/feed.xml` (same directory level)

### HTML Meta Discovery
All content pages include feed discovery metadata:

```html
<link rel="alternate" type="application/rss+xml" 
      title="Posts Feed" href="/posts/feed.xml">
<link rel="alternate" type="application/rss+xml" 
      title="Unified Feed" href="/feed/feed.xml">
```

### IndieWeb Compliance
- **Microformats2**: All feed content includes h-entry markup
- **Webmention**: Response feeds maintain webmention compatibility
- **Cool URIs**: Feed URLs follow W3C persistence principles

### SocialSpec Discovery

The shared HTML layout advertises the unified feed first at `/feed/feed.xml`, followed by the existing `/blog.rss`, `/microblog.rss`, and `/responses.rss` aliases. This gives feed consumers one stable default subscription without breaking existing URLs.

The site also publishes profile and subscription artifacts at:

- `/.well-known/following.opml` - include-only OPML directory for the blogroll, podroll, YouTube, forums, and AI starter pack
- `/.well-known/about.md` - About Markdown with a machine-readable `summary`
- `/.well-known/contact.vcf` - vCard 3.0 contact artifact

See [Social discovery endpoints](social-discovery.md) for source ownership, Azure route behavior, media types, CORS, and validation commands.

## Migration History

### Unified Feed Pattern Consistency (2025-08-05)
- **Problem**: Unified feed used `/feed/index.xml` breaking established `/[type]/feed.xml` pattern
- **Solution**: Changed unified feed to `/feed/feed.xml` with user-friendly `/all.rss` alias
- **Enhancement**: Prominent "Everything Feed" placement in subscription hub
- **Implementation**: Dual file generation maintaining backward compatibility
- **Result**: Consistent URL patterns across all 10 feed types + improved discoverability

### Tag RSS Feeds (2025-07-25)
- **Problem**: Tags had HTML pages but no RSS feeds
- **Solution**: Extended GenericBuilder processors with category metadata
- **Implementation**: Added `<category>` elements to all RSS processors
- **Result**: 1,187 working tag feeds covering all content types

### Feed Architecture Consolidation (2025-07-25)
- **Problem**: Confusing "library" vs "reviews" terminology
- **Solution**: Consolidated to consistent "reviews" branding
- **Migration**: `/resources/library/feed.xml` → `/reviews/feed.xml`
- **Benefit**: Content-proximate placement and navigation consistency

### Unified Feed System (2025-07-13)
- **Problem**: Scattered RSS generation across 8 content types
- **Solution**: Centralized feed processing in GenericBuilder
- **Performance**: Single-pass processing (~65.6 items/sec)
- **Architecture**: Eliminated code duplication across content types

## Usage Examples

### RSS Reader Subscription
Users can subscribe to:
- **All Content**: `/feed/feed.xml` or user-friendly `/all.rss`
- **Specific Types**: `/posts/feed.xml`, `/notes/feed.xml`
- **Specific Tags**: `/tags/fsharp/feed.xml`, `/tags/ai/feed.xml`

### Content Filtering
RSS readers can filter content using category metadata:
- Filter by tag using `<category>` elements
- Combine multiple content types through main feed
- Focus on specific content types through dedicated feeds

### API Integration
Feeds provide structured data for:
- Content aggregation services
- Social media cross-posting
- Content recommendation systems
- Analytics and monitoring tools

## Maintenance

### Adding New Content Types
1. Create processor in `GenericBuilder.fs` following established pattern
2. Ensure RSS items include proper category metadata
3. Add feed URL to documentation and discovery metadata
4. Update unified feed system to include new content type

### Tag Management
- Tags are automatically extracted from content frontmatter
- Tag feeds are generated dynamically based on content tagging
- No manual tag feed management required
- Tag feeds update automatically when content is tagged

### Performance Monitoring
- Monitor feed generation time during builds
- Verify 20-item limits maintain reader performance
- Check tag feed count growth as content expands
- Validate RSS 2.0 compliance with feed validators

This feed architecture provides comprehensive content syndication while maintaining performance, discoverability, and standards compliance across all content types and organizational methods.
