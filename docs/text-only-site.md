# Text-Only Accessibility Site

## Overview

The Text-Only Accessibility Site is a complete parallel version of the website optimized for universal access, providing full content parity while targeting 2G networks, flip phones, screen readers, and users preferring minimal interfaces. Every page is under 50KB with zero JavaScript dependencies for core functionality.

## Quick Access

- **Text-Only Site**: [https://www.lqdev.me/text/](https://www.lqdev.me/text/)
- **Main Site**: [https://www.lqdev.me](https://www.lqdev.me)

Easy toggle between versions available on every page.

## Features

### Universal Compatibility
- **2G Network Optimized**: All pages under 50KB load target
- **Flip Phone Support**: Core functionality works on basic mobile browsers
- **Screen Reader Optimized**: Semantic HTML with proper ARIA labels
- **Zero JavaScript Required**: All core features work without scripting
- **WCAG 2.1 AA Compliant**: Full accessibility standards compliance

### Complete Content Parity
- **All Content Types**: Posts, notes, responses, wiki, presentations, media, albums
- **1,130+ Pages**: Every piece of content has a text-only version
- **True Text-Only**: Images converted to clickable descriptive links
- **Metadata Preserved**: All frontmatter, tags, dates fully accessible
- **Collections Included**: Starter packs, travel guides, all collections

### Navigation & Discovery
- **Tag System**: Complete tag browsing with 1,195+ tags
- **Chronological Archives**: Year/month browsing for temporal navigation
- **Content Type Navigation**: Easy filtering by content type
- **Cross-Site Links**: Seamless transitions to full site when desired
- **Breadcrumb Navigation**: Clear hierarchical orientation

### Performance Excellence
- **Homepage**: 7.6KB total size
- **Average Page**: Under 30KB including all content
- **Maximum Page**: Under 50KB even for long posts
- **Zero External Dependencies**: No CDN calls, no web fonts, no analytics
- **Instant Loading**: Sub-second load times even on 2G

## Architecture

### URL Structure

```
/text/                              # Text-only homepage
/text/content/{type}/               # Content type listings
/text/content/{type}/{slug}/        # Individual content pages
/text/tags/                         # Tag index
/text/tags/{tag}/                   # Tag-filtered content
/text/archives/                     # Chronological archives
/text/archives/{year}/              # Year archives
/text/archives/{year}/{month}/      # Month archives
/text/collections/                  # Collections index
/text/collections/{collection}/     # Individual collections
```

### File Locations

**F# Source:**
- `TextOnlyBuilder.fs` - Main build orchestration
- `Views/TextOnlyViews.fs` - All view functions for text-only pages
- `Services/Tag.fs` - Tag processing shared with main site

**Generated Output:**
- `_public/text/` - Complete text-only site
- Semantic HTML structure mirrors main site
- Independent CSS file under 5KB

### CSS Approach

Minimal stylesheet (`_public/text/style.css`) under 5KB:
- System fonts only (no web fonts)
- High contrast for readability
- Large tap targets (44px+) for mobile
- Dark and light mode via `prefers-color-scheme`
- Zero animations or transitions
- Print-optimized styles included

## Content Processing

### Image to Text Conversion

All images converted to clickable descriptive links:

**Before (Main Site):**
```html
<img src="/images/example.jpg" alt="Mountain sunrise">
```

**After (Text-Only):**
```html
<a href="https://www.lqdev.me/images/example.jpg" target="_blank">[Image: Mountain sunrise]</a>
```

**Benefits:**
- True text-only content compatible with any device
- Images still accessible via click-through
- Alt text preserved as descriptive link text
- New tab preserves user's place in content

### HTML Processing

**Preserved Elements:**
- Headings (h1-h6) with proper hierarchy
- Paragraphs and line breaks
- Links with full URLs in parentheses
- Lists (ordered and unordered)
- Emphasis (strong, em)
- Code blocks and inline code

**Removed Elements:**
- Images (converted to links)
- Complex layouts
- JavaScript-dependent content
- Decorative elements
- Icon fonts

### Metadata Processing

All frontmatter fully accessible:
- Publication dates with proper formatting
- Tags as plain text with links
- Content types clearly labeled
- Author information when present
- All custom fields displayed

## User Experience

### Navigation Pattern

**Skip Links:**
```html
<a href="#main-content" class="skip-link">Skip to main content</a>
<a href="#navigation" class="skip-link">Skip to navigation</a>
```

**Breadcrumbs:**
```
← Back to Text-Only Home
View Full [Page Type] Page →
```

**Content Type Indicators:**
Simple text labels without icons:
- "Blog Post"
- "Note"
- "Response: Bookmark"
- "Wiki Entry"

### Tag Browsing

**Main Tag Index** (`/text/tags/`):
- Alphabetically organized
- Occurrence count for each tag
- Clean, scannable list format
- No tag cloud or visual complexity

**Individual Tag Pages** (`/text/tags/{tag}/`):
- Chronologically ordered content
- Content type labels
- Clear date formatting
- Direct links to content

### Archive Navigation

**Year Index** (`/text/archives/`):
- List of all years with content
- Item counts per year
- Chronological ordering

**Month Archives** (`/text/archives/{year}/{month}/`):
- All content from specific month
- Day-by-day chronological listing
- Content type indicators

## Accessibility Features

### Semantic HTML

```html
<header role="banner">
  <nav role="navigation" aria-label="Main navigation">
  </nav>
</header>

<main id="main-content" role="main">
  <article>
    <header>
      <h1>Article Title</h1>
    </header>
  </article>
</main>

<footer role="contentinfo">
</footer>
```

### ARIA Labels

- Navigation landmarks properly labeled
- Form inputs with clear labels
- Links with descriptive text
- Headings in logical hierarchy

### Keyboard Navigation

- Logical tab order
- Focus indicators on all interactive elements
- Skip links for efficient navigation
- No keyboard traps

### Screen Reader Support

- Alt text for all images (even as links)
- Proper heading hierarchy (h1 → h2 → h3)
- Landmark regions for easy navigation
- Clear link text (no "click here")

## Build Process

### Generation Steps

1. **Content Loading**: Reuse main site's GenericBuilder AST parsing
2. **HTML Processing**: `TextOnlyContentProcessor.replaceImagesWithText`
3. **View Generation**: `TextOnlyViews` functions for all page types
4. **Directory Creation**: Automatic directory structure creation
5. **File Writing**: Individual HTML files for all content

### Build Integration

**Program.fs:**
```fsharp
// After main site build
buildTextOnlySite outputDir unifiedContent presentationsFeedData
```

**Zero Impact:**
- Build time increase: <0.5 seconds
- No dependencies on external services
- Self-contained generation process
- Parallel to main site build

### Content Processing Pipeline

```
Source Content (Markdown)
  ↓
AST Parsing (shared with main site)
  ↓
Unified Content System
  ↓
Image to Text Conversion
  ↓
Text-Only View Functions
  ↓
HTML Output (<50KB pages)
```

## Use Cases

### Primary Audiences

**Low-Bandwidth Users:**
- 2G/3G mobile networks
- Satellite internet connections
- Limited data plans
- Rural/remote areas

**Accessibility Users:**
- Screen reader users (NVDA, JAWS, VoiceOver)
- Keyboard-only navigation
- High contrast requirements
- Reduced motion preferences

**Device Constraints:**
- Flip phones with basic browsers
- E-readers with web browsing
- Text-based browsers (Lynx, w3m)
- Old smartphones with limited resources

**Preference-Based:**
- Minimalist interface preferences
- Distraction-free reading
- Print-optimized content
- Privacy-conscious users (no JavaScript)

### Secondary Benefits

**Developer Testing:**
- Semantic HTML validation
- Content structure verification
- Accessibility compliance testing
- SEO optimization check

**Emergency Access:**
- Backup when main site unavailable
- Low-bandwidth disaster scenarios
- Network congestion situations
- Service degradation fallback

**Content Archival:**
- Long-term preservation format
- Future-proof HTML structure
- No external dependencies
- Simple maintenance

## Performance Metrics

### Page Size Breakdown

**Homepage (7.6KB):**
- HTML: 6.2KB
- CSS: 1.4KB (cached)
- Total: 7.6KB

**Average Content Page (25-30KB):**
- HTML: 24-29KB
- CSS: 1.4KB (cached)
- Images: 0KB (links only)
- JavaScript: 0KB
- Total: 25-30KB

**Maximum Content Page (<50KB):**
- HTML: <48KB (long-form content)
- CSS: 1.4KB (cached)
- Total: <50KB

### Load Times

**2G Network (50 Kbps):**
- Homepage: 1.2 seconds
- Average page: 5 seconds
- Max page: 8 seconds

**3G Network (400 Kbps):**
- Homepage: 0.15 seconds
- Average page: 0.6 seconds
- Max page: 1 second

**4G/WiFi:**
- All pages: <0.1 seconds

### Comparison with Main Site

| Metric | Main Site | Text-Only Site | Improvement |
|--------|-----------|----------------|-------------|
| Homepage Size | 150KB+ | 7.6KB | 95% smaller |
| JavaScript | 80KB+ | 0KB | 100% reduction |
| External Calls | 10+ | 0 | 100% reduction |
| 2G Load Time | 15-20s | 1-2s | 90% faster |
| Accessibility | Good | Excellent | WCAG 2.1 AA |

## Best Practices

### When to Use Text-Only

**Recommended:**
- Slow/unrestricted network connections
- Basic mobile devices or flip phones
- Screen reader navigation
- Battery-conscious browsing
- Print-optimized reading
- Privacy-focused access

**Optional:**
- Fast connections (main site provides richer experience)
- Modern devices (unless preferred)
- Visual content consumption (images as links may be limiting)

### Content Creation Considerations

**Already Optimized:**
- All existing content automatically included
- No special formatting required
- Standard markdown works perfectly
- Frontmatter fully supported

**Enhancement Opportunities:**
- Descriptive alt text for images (becomes link text)
- Clear link text (important for screen readers)
- Logical heading hierarchy
- Well-structured lists

### Maintenance

**Zero Special Maintenance:**
- Automatically updates with main site
- No separate content management
- Build process handles everything
- CSS rarely needs changes

**Monitoring:**
- Check page sizes stay under 50KB
- Verify links remain functional
- Test accessibility compliance
- Validate HTML structure

## Technical Implementation

### Key Functions

**TextOnlyBuilder.fs:**
```fsharp
buildTextOnlySite outputDir unifiedContent presentationsFeedData
buildTextOnlyHomepage outputDir unifiedContent
buildTextOnlyIndividualPages outputDir unifiedContent presentationsFeedData
buildTextOnlyTagSystem outputDir unifiedContent
buildTextOnlyArchives outputDir unifiedContent
buildTextOnlyCollections outputDir
```

**Views/TextOnlyViews.fs:**
```fsharp
textOnlyLayout title content
textOnlyHomePage items
textOnlyContentPage item htmlContent
textOnlyPresentationPage presentation htmlContent
textOnlyTagIndexPage tags
textOnlyTagPage tag items
textOnlyArchiveIndexPage years
```

**Content Processing:**
```fsharp
module TextOnlyContentProcessor =
    let replaceImagesWithText (content: string) : string
    let processContent (htmlContent: string) : string
```

### URL Normalization

Matching unified feed URLs with original content:
```fsharp
let actualPath = content.Url.Replace("https://www.lqdev.me", "")
let actualPathNormalized = 
    if actualPath.EndsWith("/") then actualPath 
    else actualPath + "/"
```

### Path Sanitization

URL-safe tag paths:
```fsharp
let sanitizeTagForPath (tag: string) : string =
    tag.ToLowerInvariant()
        .Replace(" ", "-")
        .Replace("#", "sharp")
        .Replace("+", "plus")
        .Replace("/", "-")
```

## Future Enhancements

### Potential Additions

**Planned:**
- Offline service worker (optional enhancement)
- Local storage for reading list
- Dark mode toggle (currently system-based)

**Under Consideration:**
- Text size controls
- Font family selection
- Reading time estimates
- Bookmark sync across visits

**Not Planned:**
- JavaScript-based features
- Complex interactions
- Visual enhancements
- Animated content

### Architectural Extensibility

The text-only system is designed for easy enhancement:
- Add new view functions to `TextOnlyViews.fs`
- Extend `buildTextOnlySite` orchestration
- Modify CSS for styling changes
- All changes isolated from main site

## Related Documentation

- [Enhanced Content Discovery](enhanced-content-discovery-implementation.md) - Main site search (not in text-only)
- [Feed Architecture](feed-architecture.md) - RSS feeds work across both sites
- [Core Infrastructure](core-infrastructure-architecture.md) - Shared content pipeline
- [Collections System](how-to-create-collections.md) - Collections in text-only format

## Resources

- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)
- [WebAIM Screen Reader Testing](https://webaim.org/articles/screenreader_testing/)
- [MDN Accessibility](https://developer.mozilla.org/en-US/docs/Web/Accessibility)
- [A11y Project](https://www.a11yproject.com/)

## Success Metrics

Since implementation (August 2025):
- **Content Parity**: 1,134 pages with zero information loss
- **Performance**: 7.6KB homepage, all pages under 50KB
- **Accessibility**: WCAG 2.1 AA compliant, tested with screen readers
- **Universal Access**: Works on flip phones, 2G networks, text browsers
- **Zero Regressions**: Main site unchanged, text-only is pure addition
- **Build Impact**: <0.5s build time increase for complete parallel site
