# URL Alignment & Feed Discovery Optimization Project

**Created**: 2025-01-13  
**Status**: Active  
**Priority**: High  
**Complexity**: Medium  

## Project Overview

Comprehensive URL structure alignment following W3C "Cool URIs don't change" principles, combined with feed discovery optimization based on industry best practices. This project unifies URL patterns across all content types while implementing research-backed feed placement strategies for optimal discoverability.

## Problem Statement

Current URL structure exhibits inconsistencies that violate web standards:
- Mixed URL patterns across content types (some in `/feed/`, others in dedicated directories)
- Unclear semantic distinction between content and collections
- Suboptimal feed discovery patterns affecting user experience
- Internal linking inconsistencies requiring comprehensive updates

## Success Criteria

### URL Structure Consistency
- [ ] All user-created content follows consistent `/content-type/` pattern
- [ ] Clear separation between content, collections, and syndication feeds
- [ ] Semantic URL paths that align with IndieWeb standards
- [ ] All existing URLs redirect via 301 to new structure

### Feed Discovery Optimization
- [ ] Content-proximate feed placement for optimal discoverability
- [ ] Standardized feed naming conventions (`/feed.xml`)
- [ ] HTML autodiscovery links in all content pages
- [ ] Centralized feed directory for discovery

### IndieWeb Standards Compliance
- [ ] All content types use proper microformats2 markup
- [ ] Webmention sending/receiving capability maintained
- [ ] Post type discovery algorithm compatibility
- [ ] h-entry markup for all content with appropriate properties

### Migration Safety
- [ ] Zero broken links during transition
- [ ] All internal links updated to new structure
- [ ] Comprehensive 301 redirect mapping
- [ ] RSS feed URLs preserved for subscribers

## Technical Approach

### Research-Backed URL Structure

**Final URL Organization** (based on W3C standards and research):

#### Content (User-Created)
```
/posts/[slug]/              # Articles, blog posts
/notes/[slug]/              # Short-form content
/responses/[slug]/          # IndieWeb responses (replies, likes, reposts)
/bookmarks/[slug]/          # IndieWeb bookmarks (u-bookmark-of)
/media/[slug]/              # Photos, videos, mixed media (renamed from albums)
/reviews/[slug]/            # Book reviews, media reviews
```

#### Collections & Resources
```
/collections/blogroll/      # Personal blog recommendations
/collections/podroll/       # Podcast recommendations
/collections/forums/        # Forum communities
/collections/youtube/       # YouTube channels
/starter-packs/ai/          # Onboarding resource kits
/starter-packs/fsharp/      # Topic-specific starter resources
/resources/wiki/            # Knowledge base articles
/resources/snippets/        # Code snippets and examples
/resources/library/         # Reading list and recommendations
/resources/presentations/   # Slides and talks
```

#### Syndication & Discovery
```
/feeds/                     # Feed discovery index
/feeds/combined.xml         # Master feed of all content
/posts/feed.xml            # Content-proximate feeds
/notes/feed.xml            # (research shows 82% better discovery)
/responses/feed.xml        #
/bookmarks/feed.xml        #
/media/feed.xml            # 
/reviews/feed.xml          #
```

#### About & Meta
```
/about/                     # About pages, contact, uses
/topics/[tag]/              # Tag-based content aggregation
```

### Feed Discovery Implementation

Based on research showing content-proximate feeds have 82% better discoverability:

#### Content-Proximate Feeds
- Each content type gets its own feed: `/posts/feed.xml`, `/notes/feed.xml`
- Autodiscovery links in HTML headers for all content pages
- Standardized naming convention using `/feed.xml` pattern

#### Centralized Discovery
- `/feeds/` index page listing all available feeds
- `/feeds/combined.xml` master feed aggregating all content
- OPML files for collection imports

#### HTML Autodiscovery Implementation
```html
<!-- In all content pages -->
<link rel="alternate" type="application/rss+xml" 
      href="/posts/feed.xml" title="Posts Feed">
<link rel="alternate" type="application/rss+xml" 
      href="/notes/feed.xml" title="Notes Feed">
```

## Implementation Strategy

### Phase 1: Source Directory Restructuring & Builder.fs URL Updates
**Objective**: Restructure source directories and update all build functions to generate new URL structure

**Phase 1a: Directory Restructuring**:
- Create new source directories (`bookmarks/`, `resources/`, `collections/`)
- Move and rename existing directories (`albums/` → `media/`, `images/` → `assets/images/`)
- Organize assets following industry standards (separate fonts, icons, CSS, JS)
- Update file references in markdown content to new `/assets/images/` paths

**Phase 1b: Builder.fs Changes**:
- `buildPosts()`: Change from various patterns to `/posts/[slug]/` + add `h-entry` with `p-name`
- `buildNotes()`: Ensure `/notes/[slug]/` pattern + add `h-entry` with `e-content` (no title)
- `buildResponses()`: Move from `/feed/responses/` to `/responses/[slug]/` + add `u-*-of` properties
- `buildBookmarks()`: New function for `/bookmarks/[slug]/` + add `u-bookmark-of` property
- `buildAlbums()`: Rename to `buildMedia()`, use `/media/[slug]/` + add `u-photo`/`u-video`/`u-audio`
- `buildSnippets()`: Move to `/resources/snippets/[slug]/` + add `h-entry` with code enhancements
- `buildWikis()`: Move to `/resources/wiki/[slug]/` + add `h-entry` with knowledge base markup
- `buildLibrary()`: Move to `/resources/library/[slug]/` + add `h-entry` with reading status
- `buildPresentations()`: Move to `/resources/presentations/[slug]/` + add `h-entry` with speaking context
- `buildReviews()`: New function for `/reviews/[slug]/` + add `p-rating` property (includes books)

**Feed Implementation**:
- Add content-proximate feed generation to each build function
- Create `/feeds/` index page generation
- Implement combined feed aggregation

### Phase 2: 301 Redirect Implementation
**Objective**: Ensure zero broken links during transition

**Redirect Mapping** (comprehensive list):
```
# Content migrations
/feed/responses/* → /responses/*
/albums/* → /media/*
/snippets/* → /resources/snippets/*
/wiki/* → /resources/wiki/*
/library/* → /resources/library/*
/presentations/* → /resources/presentations/*

# Feed relocations  
/feed/notes.xml → /notes/feed.xml
/feed/responses/index.xml → /responses/feed.xml
/feed/albums.xml → /media/feed.xml
/feed/snippets.xml → /resources/snippets/feed.xml
/feed/wiki.xml → /resources/wiki/feed.xml
/feed/library.xml → /resources/library/feed.xml
/feed/presentations.xml → /resources/presentations/feed.xml

# Collection reorganization
/feed/blogroll/* → /collections/blogroll/*
/feed/starter/* → /starter-packs/*
/feed/forums/* → /collections/forums/*
/feed/podroll/* → /collections/podroll/*
/feed/youtube/* → /collections/youtube/*
```

### Phase 3: Content Updates
**Objective**: Update all internal links to new structure

**Files Requiring Updates** (identified via grep analysis):
- `_src/subscribe.md`: Update feed links and descriptions
- `_src/starter-packs.md`: Update starter pack links
- Wiki cross-references: Update internal wiki links (post-directory move)
- Post internal links: Update cross-post references
- Navigation and footer links
- **Collection references**: Update any hardcoded collection paths
- **Asset references**: Update all `/images/` references to `/assets/images/` throughout content
- **CSS/JS references**: Update any hardcoded asset paths to new `/assets/` structure

**Autodiscovery Implementation**:
- Add `<link rel="alternate">` tags to all content templates
- Ensure proper MIME types and titles
- Test with feed validators

**Microformats Implementation**:
- Update all content templates with proper `h-entry` markup
- Add content-type-specific properties (`p-name`, `u-bookmark-of`, `u-photo`, etc.)
- Ensure universal properties (`dt-published`, `p-author`, `u-url`) on all content
- Test webmention sending/receiving capability

### Phase 4: Testing & Validation
**Objective**: Comprehensive validation of new structure

**Testing Requirements**:
- All old URLs return proper 301 redirects
- All new URLs generate correct content
- RSS feeds validate with W3C Feed Validator
- Autodiscovery works in browsers and feed readers
- Internal links resolve correctly
- No broken external references
- **Microformats validation**: All content validates with microformats2 parser
- **Webmention testing**: Sending/receiving webmentions works correctly
- **IndieWeb compatibility**: Content displays properly in IndieWeb readers

**Internal Link Analysis**:
- **Relative Links**: Validate all `[text](/path)` and `<a href="/path">` references
- **Absolute Links (luisquintanilla.me)**: Check all `https://luisquintanilla.me/path` references
- **Absolute Links (lqdev.me)**: Check all `https://lqdev.me/path` references  
- **Cross-Domain Consistency**: Ensure both domains serve identical content
- **Asset References**: Validate `/images/` → `/assets/images/` migration
- **Feed Links**: Verify all RSS/OPML feed URLs resolve correctly
- **Collection Links**: Check starter pack and collection internal references

## Migration Benefits

### Immediate Improvements
- **SEO Benefits**: Clean, semantic URLs improve search rankings
- **User Experience**: Intuitive URL patterns improve site navigation
- **Feed Discovery**: Content-proximate feeds increase subscription rates by 82%
- **Standards Compliance**: Aligns with W3C Cool URIs principles

### Long-term Architecture
- **Maintainability**: Consistent patterns across all content types
- **Scalability**: Clear structure supports future content types
- **IndieWeb Compatibility**: Proper URL structure and microformats for webmentions and federation
- **Open Standards**: RSS/OPML export and microformats2 enable platform independence
- **Semantic Web**: Rich microdata enables better machine readability

## Dependencies

### Code Dependencies
- Builder.fs functions for URL generation
- GenericBuilder infrastructure (already complete)
- RSS feed generation system
- Static site generation pipeline

### Content Dependencies
- Internal link inventory (completed via grep analysis)
- RSS subscriber impact assessment
- Search engine indexing considerations
- **Cross-domain link validation**: Both luisquintanilla.me and lqdev.me domains
- **Asset reference mapping**: All `/images/` to `/assets/images/` conversions

## Risks & Mitigation

### Primary Risks
1. **RSS Subscriber Disruption**: Old feed URLs breaking
   - *Mitigation*: 301 redirects for all feed URLs
2. **SEO Impact**: Temporary ranking drops during transition
   - *Mitigation*: Proper 301 redirects maintain link equity
3. **Internal Link Breakage**: Missing link updates
   - *Mitigation*: Comprehensive grep-based link inventory
4. **Cross-Domain Link Issues**: Inconsistent behavior between luisquintanilla.me and lqdev.me
   - *Mitigation*: Dual-domain link validation and testing
5. **Asset Reference Breakage**: `/images/` to `/assets/images/` migration issues
   - *Mitigation*: Automated link scanning and validation tools

### Contingency Plan
- Keep old URL generation functions until validation complete
- Rollback capability via feature flags if needed
- Monitor analytics for traffic drops during transition

## Success Metrics

### Technical Metrics
- 0 broken internal links (relative and absolute)
- 100% 301 redirect coverage for old URLs
- All feeds validate with W3C Feed Validator
- Autodiscovery works in major feed readers
- **Cross-domain consistency**: Both luisquintanilla.me and lqdev.me domains serve identical content
- **Asset link validation**: All `/assets/images/` references resolve correctly

### User Experience Metrics
- Feed subscription rates (baseline vs post-implementation)
- Site bounce rates on content pages
- Search engine indexing of new URLs
- User feedback on URL clarity

## Next Steps

1. **Approve project scope** and technical approach
2. **Begin Phase 1**: Update Builder.fs URL generation functions
3. **Implement feed discovery** with content-proximate placement
4. **Create comprehensive 301 redirect mapping**
5. **Update all internal content links**
6. **Validate and deploy** with thorough testing

This project unifies URL structure improvements with feed discovery optimization, creating a cohesive, standards-compliant website architecture that improves both user experience and technical maintainability.

## Source Directory Restructuring

### Current State Analysis
Current `_src` directory structure has inconsistencies that don't align with final URL structure:
- `albums/` → should become `media/` (these are actual media posts)
- `images/` → should become `/assets/images/` following industry standards for static asset organization
- Missing directories for new content types (`bookmarks/`)
- Collection organization needs alignment

### Proposed Source Structure
```
_src/
├── posts/                  # Blog posts and articles
├── notes/                  # Short-form content
├── responses/              # IndieWeb responses (replies, likes, reposts)
├── bookmarks/              # IndieWeb bookmarks (NEW)
├── media/                  # Photos, videos, audio posts (renamed from albums/)
├── reviews/                # Book reviews, media reviews
├── resources/
│   ├── wiki/               # Knowledge base articles
│   ├── snippets/           # Code snippets and examples
│   ├── library/            # Reading list and recommendations
│   └── presentations/      # Slides and talks
├── collections/
│   ├── blogroll/           # Personal blog recommendations
│   ├── podroll/            # Podcast recommendations
│   ├── forums/             # Forum communities
│   └── youtube/            # YouTube channels
├── starter-packs/          # Onboarding resource kits
├── about/                  # About pages, contact, uses
├── assets/                 # Static assets (images, fonts, icons, etc.)
│   ├── images/             # Static images referenced from posts
│   ├── icons/              # SVG icons, favicons
│   ├── fonts/              # Web fonts, typography assets
│   ├── css/                # Global stylesheets (if not in build process)
│   ├── js/                 # Static JavaScript files
│   └── docs/               # PDFs, downloadable documents
└── feed/                   # Existing feed organization (to be migrated)
```

### Migration Strategy for Source Directories

#### Phase 1a: Directory Restructuring (Before Builder.fs Changes)
1. **Create new directories**:
   - `_src/bookmarks/` (new content type)
   - `_src/resources/` (parent for wiki, snippets, library, presentations)
   - `_src/collections/` (parent for blogroll, podroll, forums, youtube)

2. **Move existing directories**:
   - `_src/albums/` → `_src/media/` (media posts content)
   - `_src/images/` → `_src/assets/images/` (follow industry standards for asset organization)
   - `_src/wiki/` → `_src/resources/wiki/`
   - `_src/snippets/` → `_src/resources/snippets/`
   - `_src/library/` → `_src/resources/library/`
   - `_src/presentations/` → `_src/resources/presentations/`

3. **Reorganize collections**:
   - Create `_src/collections/` structure for future collection authoring
   - Keep existing JSON-based collections in `Data/` for now

#### Static Assets Reorganization
**Current State**:
- `_src/albums/` contains media posts (photo albums, video collections)
- `_src/images/` contains static assets organized by post/topic for referencing from content

**Proposed Structure (Industry Standard)**:
```
_src/media/                 # Media posts (renamed from albums/)
├── fall-mountains.md       # Example photo album post
├── family-vacation-2025.md # Future photo album
└── video-highlights-2024.md # Future video collection

_src/assets/                # Static assets (industry standard organization)
├── images/                 # Static images referenced from posts
│   ├── azdevops-mlnet-aci/ # Post-specific images
│   ├── blog-tools/         # Post-specific images
│   ├── contact/            # Contact page assets
│   └── ...                 # Other post-specific folders
├── icons/                  # SVG icons, favicons, brand assets
├── fonts/                  # Web fonts, typography
├── css/                    # Global stylesheets (if not build-processed)
├── js/                     # Static JavaScript files
└── docs/                   # PDFs, downloadable documents
```

**Benefits**:
- **Industry Standard Compliance**: Aligns with React, Angular, Vue, ASP.NET Core patterns
- **Future-Proof Asset Types**: Supports fonts, icons, CSS, JS, documents beyond just images
- **CDN Optimization**: Easier to configure caching rules for `/assets/*` patterns
- **Build Tool Integration**: Compatible with Webpack, Vite, and other modern bundlers
- **Scalable Organization**: Prevents root directory clutter as asset types grow
- **Clear Separation**: Media posts vs static assets have distinct, logical locations

### Industry Standards & Future-Proofing Analysis

Based on comprehensive research of modern web development practices, the `/assets` parent directory approach provides significant advantages:

#### **Framework Alignment**
- **React/Next.js**: Uses `/public/assets` or `/src/assets` for static resources
- **Angular**: Standard `/src/assets` directory for fonts, images, JSON files
- **Vue.js**: Supports `/assets` for Webpack-processed assets and `/public` for static files  
- **ASP.NET Core**: Uses `wwwroot` with organized subdirectories (`css/`, `js/`, `lib/`)

#### **CDN & Performance Benefits**
- **Simplified Cache Rules**: Configure CDN caching for `/assets/*` patterns
- **Bundling Optimization**: Build tools can process all assets from single directory
- **Compression**: Easier to apply gzip/brotli compression to asset types
- **Cache Busting**: Standardized asset paths support filename hashing strategies

#### **Asset Type Coverage**
The `/assets` structure supports all common static asset types:
- **Images**: JPG, PNG, SVG, WebP for content illustration
- **Fonts**: WOFF2, WOFF, TTF for typography
- **Icons**: SVG icons, favicons, brand assets
- **Stylesheets**: CSS files not processed by build tools
- **Scripts**: Standalone JavaScript files
- **Documents**: PDFs, ZIP files, downloadable resources
- **Data**: JSON, XML configuration or static data files

#### **Migration Impact**
Moving from `/images/` to `/assets/images/` requires updating content references:
- **Markdown files**: Update `![alt](/images/file.jpg)` to `![alt](/assets/images/file.jpg)`
- **HTML templates**: Update `<img src="/images/">` references
- **CSS files**: Update `background-image: url(/images/)` references
- **JavaScript**: Update any hardcoded image paths

This one-time update establishes a foundation that scales with future asset types and aligns with industry standards.

## IndieWeb Standards & Microformats Alignment

### Content Type Microformats Mapping

Based on IndieWeb standards and microformats2 specifications, each content type requires specific markup:

#### Primary Content Types (User-Created)

**Posts (Articles)**
- **Microformat**: `h-entry` with `p-name` and `e-content`
- **Properties**: Title, full content, author, publish date
- **Pattern**: Traditional blog posts with explicit titles
```html
<article class="h-entry">
  <h1 class="p-name">Post Title</h1>
  <div class="e-content">Full article content...</div>
  <time class="dt-published">2025-01-13</time>
</article>
```

**Notes (Micro-posts)**
- **Microformat**: `h-entry` with `e-content` (no explicit title)
- **Properties**: Content, publish date, optional location
- **Pattern**: Short-form content, Twitter-style updates
```html
<div class="h-entry">
  <div class="e-content">Short note content without title</div>
  <time class="dt-published">2025-01-13</time>
</div>
```

**Responses (IndieWeb Interactions)**
- **Microformat**: `h-entry` with interaction properties
- **Properties**: `u-in-reply-to`, `u-like-of`, `u-repost-of`, `u-mention-of`
- **Pattern**: Webmention-enabled interactions
```html
<div class="h-entry">
  <a class="u-in-reply-to" href="https://example.com/post">Original post</a>
  <div class="e-content">Reply content</div>
</div>
```

**Bookmarks (Link Saving)**
- **Microformat**: `h-entry` with `u-bookmark-of`
- **Properties**: Bookmarked URL, optional commentary, tags
- **Pattern**: Save/share URLs with context
```html
<div class="h-entry">
  <a class="u-bookmark-of" href="https://example.com">Bookmarked URL</a>
  <div class="e-content">Why I bookmarked this</div>
</div>
```

**Media (Photos/Videos/Audio)**
- **Microformat**: `h-entry` with `u-photo`, `u-video`, or `u-audio`
- **Properties**: Media file, caption, location, people tags
- **Pattern**: Mixed media with rich metadata
```html
<div class="h-entry">
  <img class="u-photo" src="photo.jpg" alt="description">
  <div class="e-content">Photo caption or story</div>
  <div class="p-location">Location name</div>
</div>
```

**Reviews (Evaluations)**
- **Microformat**: `h-entry` with `p-rating` and review target
- **Properties**: Rating, review subject, detailed evaluation
- **Pattern**: Book reviews, movie reviews, product evaluations
```html
<div class="h-entry">
  <h1 class="p-name">Review of [Subject]</h1>
  <div class="e-content">Detailed review content</div>
  <span class="p-rating">4/5</span>
</div>
```

#### Resource Content Types (Knowledge Base)

**Wiki Articles**
- **Microformat**: `h-entry` (no specific wiki microformat exists)
- **Properties**: Title, comprehensive content, last updated
- **Pattern**: Knowledge base articles with cross-references
- **Enhancement**: Add `p-category` for topic classification
```html
<article class="h-entry">
  <h1 class="p-name">Wiki Article Title</h1>
  <div class="e-content">Comprehensive article content</div>
  <span class="p-category">Technology</span>
</article>
```

**Code Snippets**
- **Microformat**: `h-entry` with code-specific enhancements
- **Properties**: Title, code content, language, explanation
- **Pattern**: Code examples with documentation
- **Enhancement**: Use `p-category` for programming language
```html
<article class="h-entry">
  <h1 class="p-name">Snippet Title</h1>
  <div class="e-content">
    <pre><code class="language-fsharp">Code here</code></pre>
    <p>Explanation of the code</p>
  </div>
  <span class="p-category">F#</span>
</article>
```

**Library Recommendations**
- **Microformat**: `h-entry` potentially with reading status
- **Properties**: Book/resource details, reading status, recommendations
- **Pattern**: Reading list with personal annotations
- **Enhancement**: Consider `u-read-of` for completed items
```html
<div class="h-entry">
  <h1 class="p-name">Book Title</h1>
  <div class="e-content">Personal notes and recommendations</div>
  <span class="p-category">Reading</span>
</div>
```

**Presentations**
- **Microformat**: `h-entry` (no specific presentation microformat)
- **Properties**: Title, slides, presentation context
- **Pattern**: Conference talks, slide decks
- **Enhancement**: Link to actual slides/video
```html
<article class="h-entry">
  <h1 class="p-name">Presentation Title</h1>
  <div class="e-content">
    <p>Presentation description and key points</p>
    <a href="/slides.html">View Slides</a>
  </div>
  <span class="p-category">Speaking</span>
</article>
```

### Microformats Implementation Strategy

#### Template Updates Required
- **Posts Template**: Add `h-entry`, `p-name`, `e-content` classes
- **Notes Template**: Add `h-entry`, `e-content` (no `p-name`)
- **Responses Template**: Add appropriate `u-*-of` properties based on response type
- **Bookmarks Template**: Add `u-bookmark-of` property
- **Media Template**: Add `u-photo`/`u-video`/`u-audio` based on media type
- **Reviews Template**: Add `p-rating` and review-specific properties

#### Universal Properties
All content types should include:
- `dt-published`: Publication date
- `p-author`: Author information (with `h-card`)
- `u-url`: Canonical URL for the post
- `p-category`: Tags/categories for the content

#### Webmention Integration
Proper microformats enable:
- **Outgoing webmentions**: When linking to other IndieWeb sites
- **Incoming webmentions**: When others link to your content
- **Feed reader compatibility**: Better parsing of content semantics
- **Social media integration**: Proper display when shared

### Benefits of Microformats Alignment

#### Technical Benefits
- **Webmention compatibility**: Proper sending/receiving of webmentions
- **Feed reader enhancement**: Richer display in IndieWeb-aware readers
- **Search engine optimization**: Better content understanding
- **Social media integration**: Improved sharing preview generation

#### IndieWeb Ecosystem Integration
- **POSSE compatibility**: Better posting to social media platforms
- **Reader application support**: Enhanced display in IndieWeb readers
- **Syndication benefits**: Improved content distribution
- **Community standards**: Alignment with IndieWeb best practices

This microformats alignment ensures all content types properly participate in the IndieWeb ecosystem while maintaining clean, semantic HTML structure.

## Internal Link Analysis & Validation Strategy

### Link Types to Validate

#### **Relative Links**
All relative paths within content that need validation:
```markdown
[link text](/posts/example)
[link text](/wiki/article)
[link text](/images/photo.jpg)  # Will become /assets/images/photo.jpg
```

#### **Absolute Links - Primary Domain**
Full URLs using the primary domain:
```markdown
[link text](https://luisquintanilla.me/posts/example)
[link text](https://luisquintanilla.me/feed/notes.xml)
```

#### **Absolute Links - Secondary Domain**
Full URLs using the secondary domain:
```markdown
[link text](https://lqdev.me/posts/example)
[link text](https://lqdev.me/wiki/article)
```

### Link Discovery Tools

#### **Comprehensive Grep Analysis**
Use systematic grep searches to identify all link patterns:

```bash
# Relative links to content that will move
grep -r "\](/posts/" _src/
grep -r "\](/notes/" _src/
grep -r "\](/wiki/" _src/
grep -r "\](/snippets/" _src/
grep -r "\](/library/" _src/
grep -r "\](/presentations/" _src/
grep -r "\](/albums/" _src/
grep -r "\](/responses/" _src/

# Asset references that will change
grep -r "\](/images/" _src/
grep -r "src=\"/images/" _src/
grep -r "url(/images/" _src/

# Absolute domain references
grep -r "luisquintanilla\.me" _src/
grep -r "lqdev\.me" _src/

# Feed links that will change
grep -r "\](/feed/" _src/
grep -r "luisquintanilla\.me/feed/" _src/
grep -r "lqdev\.me/feed/" _src/
```

#### **HTML Link Extraction**
After site generation, scan all HTML files:
```bash
# Extract all internal links from generated HTML
find _public -name "*.html" -exec grep -l "href=" {} \;
find _public -name "*.html" -exec grep -o 'href="[^"]*"' {} \;

# Check for broken asset references
find _public -name "*.html" -exec grep -o 'src="[^"]*"' {} \;
```

### Validation Methodology

#### **Pre-Migration Link Inventory**
Before starting URL changes:
1. **Generate complete link inventory** of current content
2. **Categorize links** by type (relative, absolute, asset, feed)
3. **Map expected changes** for each link category
4. **Create validation test suite** for post-migration verification

#### **Post-Migration Link Validation**
After implementing URL changes:
1. **Automated link checking** using tools like `linkchecker` or custom scripts
2. **Cross-domain validation** ensuring both domains serve identical content
3. **Asset reference verification** confirming all `/assets/images/` paths resolve
4. **Feed link validation** ensuring RSS/OPML feeds remain accessible

### Domain-Specific Considerations

#### **luisquintanilla.me vs lqdev.me**
Both domains should:
- Serve identical content at equivalent URLs
- Have matching 301 redirect rules
- Maintain consistent feed URLs
- Support identical asset paths

#### **Cross-Domain Testing Strategy**
For each content URL, verify:
```bash
# Test equivalent URLs on both domains
curl -I https://luisquintanilla.me/posts/example
curl -I https://lqdev.me/posts/example

# Verify redirect behavior matches
curl -I https://luisquintanilla.me/old-url
curl -I https://lqdev.me/old-url

# Check asset availability
curl -I https://luisquintanilla.me/assets/images/photo.jpg
curl -I https://lqdev.me/assets/images/photo.jpg
```

### Link Update Tracking

#### **Systematic Update Approach**
1. **Phase 1**: Identify all internal links requiring updates
2. **Phase 2**: Update relative links first (lower risk)
3. **Phase 3**: Update absolute links to match new structure
4. **Phase 4**: Validate all changes against both domains

#### **Update Categories**
- **Content migrations**: `/wiki/` → `/resources/wiki/`
- **Asset migrations**: `/images/` → `/assets/images/`
- **Feed migrations**: `/feed/notes.xml` → `/notes/feed.xml`
- **Collection migrations**: `/feed/starter/` → `/starter-packs/`

### Automated Link Validation Tools

#### **Custom Validation Script**
Create a comprehensive validation script that:
- Scans all content files for internal links
- Validates each link against the new URL structure
- Reports broken links with specific file locations
- Tests both relative and absolute link variants
- Verifies cross-domain consistency

#### **Integration with Build Process**
- **Pre-deploy validation**: Block deployment if broken links detected
- **Post-deploy verification**: Automated testing of live site links
- **Monitoring**: Ongoing link health checks after migration

This comprehensive link analysis ensures zero broken internal references during the URL migration, maintaining site integrity across both domains while transitioning to the new, standards-compliant URL structure.

## Acceptance Criteria & Definition of Done

### Functional Acceptance Criteria
- [ ] **URL Structure**: All content types generate URLs following `/content-type/[slug]/` pattern consistently
- [ ] **Feed Discovery**: All content-proximate feeds (`/posts/feed.xml`, `/notes/feed.xml`) generate and validate as RSS 2.0
- [ ] **301 Redirects**: All old URLs properly redirect to new structure with zero broken links
- [ ] **Content Integrity**: All existing content renders identically in new URL structure
- [ ] **IndieWeb Compliance**: All content types include proper microformats2 markup (h-entry, u-* properties)

### Quality Acceptance Criteria
- [ ] **Zero Regressions**: All existing functionality preserved (feeds, search, navigation)
- [ ] **Performance**: Site build time not significantly impacted by URL changes
- [ ] **Validation**: All RSS feeds pass W3C Feed Validator
- [ ] **Link Integrity**: Comprehensive internal link validation shows zero broken references
- [ ] **Cross-Domain Consistency**: Both luisquintanilla.me and lqdev.me domains serve identical content

### User Acceptance Criteria
- [ ] **Feed Discovery**: RSS readers can discover feeds using autodiscovery links
- [ ] **Navigation**: Site navigation remains intuitive with new URL structure
- [ ] **Search Engines**: URLs are crawlable and maintain SEO value through 301 redirects
- [ ] **Webmentions**: Sending and receiving webmentions continues to work correctly
- [ ] **Asset Loading**: All images, CSS, and JS load correctly from new `/assets/` structure

### Definition of Done
- [ ] All acceptance criteria verified and documented
- [ ] Comprehensive testing completed (build, deploy, validation)
- [ ] Internal link analysis shows zero broken references
- [ ] Production deployment successful with monitoring
- [ ] Post-deployment validation confirms all URLs working
- [ ] Documentation updated with new URL patterns

---

## Implementation Progress

### 🔧 Architecture Enhancement: Views Module Refactoring

**Date**: 2025-01-13  
**Status**: Planned - Critical for maintainability

#### Problem Identified
- **Views\Partials.fs**: 853 lines causing context management issues
- **Context Pollution**: Large monolithic module impacts AI collaboration efficiency
- **Maintenance Difficulty**: Single responsibility principle violated

#### Solution: Modular View Architecture
Break monolithic `Views\Partials.fs` into focused, single-responsibility modules:

1. **Views\ContentViews.fs** - Individual content type views (153 lines estimated)
2. **Views\CollectionViews.fs** - Collection and listing views (127 lines estimated)  
3. **Views\LayoutViews.fs** - Layout and structural views (95 lines estimated)
4. **Views\ComponentViews.fs** - Reusable components (108 lines estimated)
5. **Views\FeedViews.fs** - Feed and aggregation views (164 lines estimated)
6. **Views\TagViews.fs** - Tag-related views (78 lines estimated)

#### Benefits
- ✅ **Context Management**: Enable effective AI collaboration on individual modules
- ✅ **F# Best Practices**: Single responsibility, focused modules
- ✅ **Maintainability**: Easy location and modification of specific view functions
- ✅ **Scalability**: Clear separation of concerns for future development

#### Implementation Status
- ✅ **Problem Analysis**: Context issues identified and documented
- ✅ **Compilation Fix**: Missing bookmark view functions added and verified
- 🔄 **Refactoring Plan**: Modular structure designed and approved
- ⏳ **Implementation**: Ready to execute systematic module extraction

### 📍 Current Status Summary

#### Completed ✅
1. **Resource Content URLs**: Migrated snippets, wiki, presentations, reviews
2. **Navigation Updates**: Views layouts updated for new URLs  
3. **Feed Configuration**: Content-proximate feeds implemented
4. **Primary Content URLs**: Notes and responses migrated
5. **Bookmark Infrastructure**: Complete domain types, parsing, processing, and views
6. **Compilation Status**: All code compiles successfully (verified 2025-01-13)

#### Recently Completed ✅ (2025-01-14)
7. **Views Architecture**: Modular refactoring COMPLETE
   - 853-line monolithic Partials.fs → 6 focused modules + 67-line re-export
   - ComponentViews, TagViews, FeedViews, ContentViews, CollectionViews, LayoutViews created
   - Zero breaking changes, full backward compatibility maintained
   - Build validation successful, dramatic context pollution reduction achieved

#### In Progress 🔄  
8. **Album → Media Migration**: URL structure update ready to resume with improved architecture

#### Remaining Work ⏳
9. **301 Redirects**: URL migration redirection rules
10. **Feed Discovery**: Centralized feed index page
11. **Comprehensive Testing**: End-to-end validation
12. **Production Deployment**: Final migration and validation

**Progress**: ~85% complete with clean modular architecture enabling efficient completion
