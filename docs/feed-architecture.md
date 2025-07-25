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
- **Main Feed**: `/feed/index.xml` - Fire-hose feed including all content types chronologically
- **Individual RSS**: `/blog.rss`, `/microblog.rss`, `/responses.rss` - Legacy feed URLs with redirects

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
      title="Main Feed" href="/feed/index.xml">
```

### IndieWeb Compliance
- **Microformats2**: All feed content includes h-entry markup
- **Webmention**: Response feeds maintain webmention compatibility
- **Cool URIs**: Feed URLs follow W3C persistence principles

## Migration History

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
- **All Content**: `/feed/index.xml`
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
