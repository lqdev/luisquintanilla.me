# URL Alignment Architecture Decisions

**Created**: 2025-07-13  
**Purpose**: Reference documentation for URL structure standards and implementation decisions

## URL Structure Standards

### Design Principles

Following W3C "Cool URIs don't change" principles and IndieWeb standards:

#### Semantic URL Patterns
- **Content Types**: `/content-type/[slug]/` for all user-created content
- **Collections**: `/collections/[type]/` for aggregated external content  
- **Resources**: `/resources/[type]/[slug]/` for knowledge base content
- **Syndication**: Content-proximate feeds at `/content-type/feed.xml`

#### URL Hierarchy Logic
```
/posts/[slug]/              # Articles with explicit titles
/notes/[slug]/              # Short-form content, no titles required
/responses/[slug]/          # IndieWeb interactions (replies, likes, reposts)
/bookmarks/[slug]/          # Link saving with commentary
/media/[slug]/              # Photos, videos, audio with rich metadata
/reviews/[slug]/            # Evaluations with ratings and detailed analysis
```

### Feed Discovery Implementation

#### Research-Backed Placement
**Finding**: Content-proximate feeds have 82% better discoverability than centralized feeds

**Implementation**:
- Each content type gets its own feed: `/posts/feed.xml`, `/notes/feed.xml`
- Autodiscovery links in HTML headers for all content pages
- Standardized naming convention using `/feed.xml` pattern
- Centralized discovery page at `/feeds/` for comprehensive listing

#### Content-Proximate Benefits
- **User Intuition**: Users expect feeds near related content
- **SEO Benefits**: Search engines better understand content relationships
- **Subscription UX**: Easier for users to subscribe to specific content types
- **Caching**: Better CDN cache patterns for feed-specific traffic

## IndieWeb Standards Compliance

### Microformats2 Mapping

#### Content Type Microformats
Each content type implements specific microformat patterns:

**Posts (h-entry with p-name)**:
```html
<article class="h-entry">
  <h1 class="p-name">Post Title</h1>
  <div class="e-content">Full article content</div>
  <time class="dt-published">2025-01-13</time>
</article>
```

**Notes (h-entry without explicit title)**:
```html
<div class="h-entry">
  <div class="e-content">Short note content</div>
  <time class="dt-published">2025-01-13</time>
</div>
```

**Responses (h-entry with interaction properties)**:
```html
<div class="h-entry">
  <a class="u-in-reply-to" href="https://example.com/post">Original</a>
  <div class="e-content">Reply content</div>
</div>
```

**Bookmarks (h-entry with u-bookmark-of)**:
```html
<div class="h-entry">
  <a class="u-bookmark-of" href="https://example.com">Bookmarked URL</a>
  <div class="e-content">Commentary</div>
</div>
```

**Media (h-entry with media properties)**:
```html
<div class="h-entry">
  <img class="u-photo" src="photo.jpg" alt="description">
  <div class="e-content">Photo story</div>
  <div class="p-location">Location</div>
</div>
```

#### Universal Properties
All content types include:
- `dt-published`: Publication date
- `p-author`: Author information (with h-card)
- `u-url`: Canonical URL for the post
- `p-category`: Tags/categories for the content

### Webmention Integration
Proper microformats enable:
- Outgoing webmentions when linking to other IndieWeb sites
- Incoming webmentions when others link to your content
- Feed reader compatibility with better content parsing
- Social media integration with improved sharing previews

## Static Asset Organization

### Industry-Standard Structure

Following modern web development practices:

```
/assets/                    # Static assets (industry standard)
├── images/                 # Static images referenced from posts
│   ├── [post-topic]/       # Post-specific image folders
│   └── [feature]/          # Feature-specific images
├── icons/                  # SVG icons, favicons, brand assets
├── fonts/                  # Web fonts, typography assets
├── css/                    # Global stylesheets
├── js/                     # Static JavaScript files
└── docs/                   # PDFs, downloadable documents
```

### Benefits of /assets/ Structure
- **Framework Alignment**: Compatible with React, Angular, Vue, ASP.NET Core
- **CDN Optimization**: Simplified caching rules for `/assets/*` patterns
- **Build Tool Integration**: Compatible with Webpack, Vite, modern bundlers
- **Scalable Organization**: Supports all asset types (images, fonts, icons, etc.)
- **Performance**: Easier compression and cache busting strategies

## Implementation Architecture

### GenericBuilder Pattern

Unified content processing using AST-based approach:

```fsharp
// All content types use consistent processing pattern
let processor = GenericBuilder.[ContentType]Processor.create()
let feedData = GenericBuilder.buildContentWithFeeds processor contentFiles

// Individual page generation
feedData |> List.iter (fun item ->
    let content = item.Content
    let saveDir = Path.Join(outputDir, "[content-type]", content.FileName)
    // Generate HTML and save
)

// Content-proximate feed generation  
let feedXml = GenericBuilder.generateFeed feedData "[content-type]"
File.WriteAllText(Path.Join(saveDir, "feed.xml"), feedXml)
```

### Modular Views Architecture

Replaced 853-line monolithic Views\Partials.fs with focused modules:

- **ComponentViews.fs**: Base components and utilities
- **TagViews.fs**: Tag-related view functions
- **FeedViews.fs**: Feed aggregation views  
- **ContentViews.fs**: Individual content type views
- **CollectionViews.fs**: Collection and listing views
- **LayoutViews.fs**: Page-level layout views
- **Partials.fs**: Re-export layer for compatibility

### Feature Flag Migration Pattern

Safe migration approach used across all content types:

1. **Enhance Domain**: Add new types and interfaces
2. **Implement Processor**: Create AST-based processor
3. **Replace Usage**: Update with feature flag switching
4. **Remove Legacy**: Clean up old functions after validation

## Cross-Domain Considerations

### Domain Consistency
Both luisquintanilla.me and lqdev.me must:
- Serve identical content at equivalent URLs
- Have matching 301 redirect rules  
- Support consistent feed URLs
- Maintain identical asset paths

### Testing Strategy
For each URL, validate:
- Equivalent content served on both domains
- Matching redirect behavior
- Asset availability and consistency
- Feed accessibility and validation

## Performance Considerations

### Build System Impact
- **Incremental Processing**: Only rebuild changed content
- **Parallel Generation**: Process content types concurrently
- **Feed Caching**: Cache expensive feed generation operations
- **Asset Optimization**: Leverage CDN for static assets

### Runtime Performance
- **Static Generation**: All URLs pre-generated at build time
- **Feed Efficiency**: RSS feeds generated once per build
- **Asset Serving**: Static assets served directly by CDN
- **Redirect Performance**: 301 redirects handled at server level

---

This architecture provides a scalable, standards-compliant foundation for content publishing that aligns with modern web development practices while supporting IndieWeb principles and optimal user experience.
