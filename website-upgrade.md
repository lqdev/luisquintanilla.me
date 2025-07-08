# Website Architecture Upgrade Specification

## Overview
Refactor the existing static site generator to eliminate repetitive code patterns and implement a unified content processing system using generic `ContentProcessor` patterns, AST-based parsing, and custom block support.

## Current Problems
- **Repetitive Build Functions**: 20+ nearly identical `build*Page` functions in Builder.fs
- **Inconsistent Metadata**: Different date fields, required fields, and naming conventions across content types
- **Manual String Parsing**: Current `getContentAndMetadata` uses string manipulation instead of AST
- **Limited Media Support**: Albums are clunky, no support for mixed media content
- **Scattered Logic**: Parsing, rendering, and output logic duplicated across content types
- **Manual Feed Generation**: Separate passes for individual pages, HTML feeds, and RSS feeds

## Proposed Solution Architecture

### Core Types
```fsharp
// Generic content processor
type ContentProcessor<'T> = {
    Parse: string -> 'T
    Render: 'T -> string
    OutputPath: 'T -> string
}

// Unified content interface
type ITaggable = 
    abstract member Tags: string array
    abstract member Title: string
    abstract member Date: string
    abstract member FileName: string
    abstract member ContentType: string

// AST-based parsing
type ParsedDocument = {
    Metadata: 'TMetadata option
    TextContent: string
    CustomBlocks: Map<string, obj list>
    RawMarkdown: string
}

// Custom block types (from IndieWeb repo)
type Media = {
    MediaType: MediaType
    Uri: string
    AltText: string
    Caption: string option
    AspectRatio: AspectRatio
    Location: Location option
}

type ReviewData = {
    Item: string
    ItemType: string
    Rating: float
    Scale: float
    Pros: string list
    Cons: string list
    AdditionalFields: Map<string, obj>
}

// Feed generation
type FeedData = {
    Card: XmlNode
    RssItem: RssItem
    ContentType: string
    Date: string
}
```

### Key Functions
```fsharp
// Single-pass content processing
val buildContentWithFeeds<'T> : 
    processor: ContentProcessor<'T> ->
    cardRenderer: ('T -> XmlNode) ->
    rssItemRenderer: ('T -> RssItem) ->
    contentType: string ->
    sourceDir: string ->
    FeedData list

// AST-based parsing (replaces getContentAndMetadata)
val parseDocumentFromAst : 
    pipeline: MarkdownPipeline ->
    content: string ->
    Result<ParsedDocument, ParseError>

// Custom block parsing
val parseCustomBlocks : 
    blockParsers: Map<string, string -> obj list> ->
    doc: MarkdownDocument ->
    Map<string, obj list>

// Unified feed generation
val buildMainFeeds : FeedData list -> unit

// Card-based rendering
val renderCard : XmlNode -> XmlNode -> XmlNode option -> XmlNode
```

## Migration Strategy

### Phase 1: Implement Alongside Existing System (No Breaking Changes)
**Goal**: Build complete new infrastructure without touching existing functionality

**Add New Modules**:
- `ASTParsing.fs` - AST-based document parsing (replaces string manipulation)
- `CustomBlocks.fs` - `:::media`, `:::review`, `:::venue`, `:::rsvp` block parsing
- `MediaTypes.fs` - Port IndieWeb media system (MediaType, AspectRatio, Location)
- `GenericBuilder.fs` - `buildContentWithFeeds`, `buildMainFeeds`, card renderers
- `BlockRenderers.fs` - Rendering for all custom block types

**Enhance Domain.fs**:
- Add `ITaggable` interface
- Add new content types with custom block support
- Keep all existing types unchanged

**Create Enhanced Pipeline**:
- Custom markdown pipeline with block parser registration
- Block renderer registry for extensible rendering
- AST-based front-matter extraction

**Validation**: New modules compile, existing build process unchanged, no breaking changes

---

### Phase 2: Migrate One Content Type at a Time to New Processors
**Goal**: Prove new system works by migrating content types individually

**Migration Order**: Snippets → Wiki → Presentations → Books → Posts → Responses → Albums

**Per Content Type**:
1. **Standardize Metadata**: Add `published_date`, `tags` array (backward compatible)
2. **Implement ITaggable**: Enable unified tag processing
3. **Create Processor**: Using AST parsing and custom block support
4. **Add Feature Flag**: Switch between old/new without breaking builds
5. **Validate Output**: Ensure identical results

**Example - Snippets**:
```fsharp
// Add to Builder.fs (alongside existing buildSnippetPages)
let buildSnippetsNew() = 
    let snippetData = buildContentWithFeeds 
        createSnippetProcessor 
        renderSnippetCard 
        renderSnippetRssItem 
        "snippets" 
        (Path.Join(srcDir, "snippets"))
    snippetData

// Feature flag in Program.fs
let useNewSnippets = Environment.GetEnvironmentVariable("NEW_SNIPPETS") = "true"
if useNewSnippets then 
    let _ = buildSnippetsNew()
    ()
else 
    buildSnippetPages(snippets)
```

**Content Enhancement**: As types migrate, add custom blocks:
- Albums: Convert to `:::media` blocks
- Books: Add `:::review` blocks  
- Add `:::venue`, `:::rsvp` for new content types

---

### Phase 3: Switch Feed Generation to Unified System
**Goal**: Replace multiple feed generation passes with single unified system

**Replace Feed Functions**:
- Remove: `buildFeedPage`, `buildFeedRssPage`, `buildBlogRssFeed`, etc.
- Replace with: `buildMainFeeds(allData)` - generates all feeds automatically

**Single Feed Generation**:
```fsharp
let buildAllFeeds() =
    let allData = [
        buildSnippetsNew()    // Returns FeedData list
        buildWikiNew()        // Returns FeedData list  
        buildPostsNew()       // Returns FeedData list
        // ... all migrated content types
    ] |> List.concat
    
    buildMainFeeds allData  // Generates fire-hose + type-specific feeds
```

**Benefits**: Single pass, automatic RSS/HTML generation, consistent sorting

---

### Phase 4: Remove Old Build Functions Once All Content Migrated
**Goal**: Clean up Builder.fs after all content types use new processors

**Remove Old Functions** (20+ functions eliminated):
- `buildPostPages`, `buildSnippetPages`, `buildWikiPages`, etc.
- `buildFeedPage`, `buildResponsePage`, `buildTagsPages` (old version)
- All individual RSS generation functions

**Simplify Main Builder**:
```fsharp
let buildAllSite() =
    // All content in single pass
    let allData = [
        buildContentWithFeeds postProcessor renderPostCard renderPostRssItem "posts" (Path.Join(srcDir, "posts"))
        buildContentWithFeeds snippetProcessor renderSnippetCard renderSnippetRssItem "snippets" (Path.Join(srcDir, "snippets"))
        // ... all content types
    ] |> List.concat
    
    // Single feed generation
    buildMainFeeds allData
    
    // Other site assets
    buildAllStaticPages()
    buildAllDataPages()
    buildAllTags allData
```

**Result**: Builder.fs reduced from 580+ lines to ~200 lines

---

### Phase 5: Clean Up and Optimize
**Goal**: Polish new system and add advanced features

**Code Cleanup**:
- Remove unused old types and functions
- Consolidate domain types
- Optimize build performance (parallel processing, memory usage)

**Enhanced Content**:
- Migrate all albums to `:::media` blocks
- Convert book reviews to `:::review` blocks
- Add location data with `:::venue` blocks
- Create RSVP posts with `:::rsvp` blocks

**Advanced Features**:
- Search data generation (with embeddings preparation)
- Client-side feed switching with JSON data
- Microformats output for IndieWeb compliance
- Enhanced card rendering with custom blocks

**Documentation**: Update README with new content creation workflow and custom block examples

## Rollback Strategy
- **Feature Flags**: Environment variables control old vs new system per content type
- **Parallel Systems**: Old functions remain until all content migrated  
- **Output Validation**: Automated comparison ensures identical results
- **Git Tags**: Tag each phase completion for easy rollback
- **Gradual Migration**: Move content types individually, not all at once

## Success Criteria
- [ ] Builder.fs: 580+ lines → ~200 lines (65% reduction)
- [ ] Single `buildAllSite()` call builds everything
- [ ] All content types use AST parsing instead of string manipulation
- [ ] Custom blocks working: `:::media`, `:::review`, `:::venue`, `:::rsvp`
- [ ] Adding new content type: ~10 lines of code
- [ ] Unified tag system for all content
- [ ] Single-pass feed generation (HTML + RSS)
- [ ] All existing functionality preserved
- [ ] Build time improved through elimination of multiple file passes

This migration strategy ensures a safe, progressive upgrade to the new architecture while maintaining full backward compatibility and the ability to rollback at any point.