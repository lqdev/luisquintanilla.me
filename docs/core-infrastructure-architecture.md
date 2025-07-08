# Core Infrastructure Module Architecture

## Overview

This document provides comprehensive documentation for the new core infrastructure modules implemented in Phase 1 of the website architecture upgrade. These modules provide AST-based parsing, custom block support, and unified content processing patterns to replace the existing repetitive build functions.

## Module Architecture

### 1. Domain.fs Enhancement
**Purpose**: Enhanced domain types with unified content interface  
**Status**: Enhanced existing module without breaking changes  
**Key Additions**:

```fsharp
// Unified content interface for tag processing
type ITaggable = 
    abstract member Tags: string array
    abstract member Title: string
    abstract member Date: string
    abstract member FileName: string
    abstract member ContentType: string
```

**Helper Functions**:
- `ITaggableHelpers.postToITaggable: Post -> ITaggable`
- `ITaggableHelpers.snippetToITaggable: Snippet -> ITaggable`
- `ITaggableHelpers.wikiToITaggable: Wiki -> ITaggable`
- `ITaggableHelpers.responseToITaggable: Response -> ITaggable`

**Integration**: All existing content types can now be processed uniformly through the ITaggable interface.

### 2. CustomBlocks.fs
**Purpose**: Custom block types and Markdig extension integration  
**Dependencies**: Domain.fs  
**Status**: New module

**Custom Block Types**:
```fsharp
type MediaBlock(parser: BlockParser) = inherit ContainerBlock(parser)
type ReviewBlock(parser: BlockParser) = inherit ContainerBlock(parser)
type VenueBlock(parser: BlockParser) = inherit ContainerBlock(parser)
type RsvpBlock(parser: BlockParser) = inherit ContainerBlock(parser)
```

**Data Types**:
```fsharp
type MediaItem = {
    media_type: string
    uri: string
    alt_text: string
    caption: string
    aspect: string
}

type ReviewData = {
    item_title: string
    rating: float
    max_rating: float
    review_text: string
    item_url: string option
    review_date: string option
}
```

**Key Functions**:
- `useCustomBlocks: MarkdownPipelineBuilder -> MarkdownPipelineBuilder`
- `parseCustomBlocks: Map<string, string -> obj list> -> MarkdownDocument -> Map<string, obj list>`

**Integration**: Integrates with Markdig pipeline to parse `:::block` syntax into structured objects.

### 3. MediaTypes.fs
**Purpose**: IndieWeb media system with comprehensive type support  
**Dependencies**: None  
**Status**: New module

**Core Types**:
```fsharp
type MediaType = Image | Video | Audio | Document | Link | Unknown
type AspectRatio = Landscape | Portrait | Square | Cinematic | Unknown
type Location = {
    latitude: float option
    longitude: float option
    name: string option
    address: string option
}
```

**Helper Functions**:
- `detectMediaType: string -> MediaType`
- `parseAspectRatio: string -> AspectRatio`
- `createLocation: lat:float option -> lng:float option -> name:string option -> Location`

**Integration**: Provides structured media metadata for custom blocks and content processing.

### 4. ASTParsing.fs
**Purpose**: Core document parsing with AST generation  
**Dependencies**: Domain.fs, CustomBlocks.fs  
**Status**: New module

**Core Types**:
```fsharp
type ParseError = 
    | YamlParseError of string
    | MarkdownParseError of string  
    | FileNotFound of string
    | InvalidMarkdownStructure of string
    | MissingRequiredField of field: string * context: string

type ParsedDocument<'TMetadata> = {
    Metadata: 'TMetadata option
    TextContent: string
    CustomBlocks: Map<string, obj list>
    RawMarkdown: string
    MarkdownAst: MarkdownDocument
}
```

**Key Functions**:
```fsharp
// Primary parsing function (replaces getContentAndMetadata)
val parseDocumentFromAst<'TMetadata> : string -> Result<ParsedDocument<'TMetadata>, ParseError>

// Specialized parsers for each content type
val parsePost : string -> Result<ParsedDocument<PostDetails>, ParseError>
val parseSnippet : string -> Result<ParsedDocument<SnippetDetails>, ParseError>
val parseWiki : string -> Result<ParsedDocument<WikiDetails>, ParseError>
```

**Pipeline Configuration**:
- Uses `UseAdvancedExtensions()` for comprehensive markdown support
- Integrates `useCustomBlocks` for custom block parsing
- Maintains compatibility with existing MarkdownService configuration

**Integration**: Replaces string-based `getContentAndMetadata` with AST-based parsing while maintaining API compatibility.

### 5. BlockRenderers.fs
**Purpose**: Extensible rendering system for custom blocks  
**Dependencies**: CustomBlocks.fs, MediaTypes.fs  
**Status**: New module

**Key Functions**:
```fsharp
// Core rendering function
val renderCustomBlock : string -> obj -> string

// Specialized renderers
val renderMediaBlock : MediaItem -> string
val renderReviewBlock : ReviewData -> string
val renderVenueBlock : VenueData -> string
val renderRsvpBlock : RsvpData -> string
```

**Rendering Features**:
- IndieWeb microformat support (h-card, h-entry, h-review)
- Responsive HTML output
- Accessibility features (alt text, ARIA labels)
- SEO-friendly structured data

**Integration**: Converts parsed custom blocks into HTML for different contexts (cards, feeds, full pages).

### 6. GenericBuilder.fs
**Purpose**: Unified content processing pattern  
**Dependencies**: ASTParsing.fs, BlockRenderers.fs  
**Status**: New module

**Core Types**:
```fsharp
type ContentProcessor<'T> = {
    Parse: string -> 'T
    Render: 'T -> string
    RenderCard: 'T -> XmlNode
    OutputPath: 'T -> string
}

type FeedData = {
    Card: XmlNode
    RssItem: RssItem
    ContentType: string
    Date: string
}
```

**Key Functions**:
```fsharp
// Unified content processing (replaces repetitive build functions)
val buildContentWithFeeds<'T> : 
    processor: ContentProcessor<'T> ->
    cardRenderer: ('T -> XmlNode) ->
    rssItemRenderer: ('T -> RssItem) ->
    contentType: string ->
    sourceDir: string ->
    FeedData list

// Unified feed generation
val buildMainFeeds : FeedData list -> unit
```

**Integration**: Provides single-pass content processing that generates both static files and feed data simultaneously.

## Data Flow Architecture

### Phase 1: AST-Based Parsing
```
Markdown File → ASTParsing.parseDocumentFromAst → ParsedDocument<'T>
    ├── Metadata: Structured front matter
    ├── TextContent: Processed markdown (excluding custom blocks)
    ├── CustomBlocks: Structured custom block data
    ├── RawMarkdown: Original content
    └── MarkdownAst: Markdig AST for advanced processing
```

### Phase 2: Content Processing
```
ParsedDocument<'T> → ContentProcessor<'T> → Multiple Outputs
    ├── Render → Static HTML files
    ├── RenderCard → Feed card entries
    └── RssItem → RSS feed entries
```

### Phase 3: Custom Block Rendering
```
CustomBlocks → BlockRenderers → HTML Output
    ├── MediaItem → renderMediaBlock → h-card microformat
    ├── ReviewData → renderReviewBlock → h-review microformat
    ├── VenueData → renderVenueBlock → h-card venue
    └── RsvpData → renderRsvpBlock → h-event response
```

## Integration Points

### With Existing System
- **Services/Markdown.fs**: Original functions preserved, new AST functions added
- **Domain.fs**: Enhanced with ITaggable, all existing types unchanged
- **Builder.fs**: Will integrate new ContentProcessor pattern in Phase 2

### With Content Types
- **Posts**: Use `parsePost` and `Post` type with ITaggable interface
- **Snippets**: Use `parseSnippet` and `Snippet` type with ITaggable interface  
- **Wiki**: Use `parseWiki` and `Wiki` type with ITaggable interface
- **Responses**: Use `parseResponse` and `Response` type with ITaggable interface

### With Feed Generation
- **RSS**: Custom blocks included as structured data in RSS items
- **JSON Feed**: Custom blocks provide rich metadata for JSON feed entries
- **Cards**: Custom blocks render as structured h-card entries

## Usage Patterns

### Basic Content Processing
```fsharp
// Replace getContentAndMetadata<PostDetails> filePath
let result = parseDocumentFromAst<PostDetails> (File.ReadAllText filePath)

match result with
| Ok parsedDoc ->
    // Access metadata
    let title = parsedDoc.Metadata.Value.Title
    // Access text content (excluding custom blocks)
    let content = parsedDoc.TextContent
    // Access custom blocks
    let reviews = parsedDoc.CustomBlocks.["review"]
| Error parseError ->
    // Handle parsing errors with detailed context
```

### Unified Content Processing
```fsharp
let postProcessor : ContentProcessor<Post> = {
    Parse = fun content -> (* parse logic *)
    Render = fun post -> (* HTML generation *)
    RenderCard = fun post -> (* h-card generation *)
    OutputPath = fun post -> sprintf "posts/%s.html" post.FileName
}

let feedData = buildContentWithFeeds postProcessor cardRenderer rssRenderer "post" "_src/posts"
```

### Custom Block Rendering
```fsharp
// In templates or rendering functions
parsedDoc.CustomBlocks
|> Map.iter (fun blockType objList ->
    objList |> List.iter (fun obj ->
        let html = renderCustomBlock blockType obj
        (* include in output *)))
```

## Performance Characteristics

### AST Parsing
- **Single Pass**: Markdown parsed once with all extensions
- **Memory Efficient**: AST reused for multiple operations
- **Type Safe**: Compile-time validation of metadata types
- **Error Handling**: Comprehensive error reporting with context

### Custom Blocks
- **Lazy Processing**: Blocks processed only when accessed
- **Cacheable**: Parsed block objects can be cached
- **Extensible**: New block types added without breaking changes
- **Validation**: YAML validation with clear error messages

### Content Processing
- **Batch Processing**: Multiple content types processed in single pass
- **Feed Optimization**: Cards and RSS items generated simultaneously
- **Parallelizable**: Content processing can be parallelized by type
- **Incremental**: Only changed content needs reprocessing

## Testing and Validation

### Test Coverage
- **Unit Tests**: Each module tested independently
- **Integration Tests**: Full pipeline tested with real content
- **Regression Tests**: Existing functionality validated
- **Performance Tests**: Baseline performance measurements

### Validation Scripts
- `test-ast-parsing.fsx`: AST parsing functionality
- `test-phase1b.fsx`: Block renderers and generic builder
- `test-phase1c.fsx`: ITaggable interface and parseCustomBlocks
- `test-comparison-phase1d.fsx`: Old vs new parsing comparison
- `test-context-validation.fsx`: Custom blocks in different contexts
- `test-integration-phase1d.fsx`: Parallel development validation

### Quality Metrics
- **Compilation**: All modules compile without warnings
- **Type Safety**: Full type safety with comprehensive error handling
- **Performance**: Equivalent or better than existing string-based parsing
- **Maintainability**: Clear separation of concerns and extensible design

## Future Enhancements

### Phase 2: Content Type Migrations
- Migrate snippets to use AST parsing and ContentProcessor pattern
- Implement feature flags for gradual rollout
- Create specialized content processors for each type

### Phase 3: Advanced Features
- Custom block galleries and complex layouts
- Real-time preview with custom block rendering
- Advanced IndieWeb microformat support
- Integration with external services (mapping, media)

### Phase 4: Legacy Cleanup
- Remove old build functions after complete migration
- Optimize performance with lessons learned
- Consolidate similar content processing patterns

---

**Created**: 2025-07-08  
**Phase**: 1D - Testing and Validation  
**Status**: Complete and Validated  
**Next Phase**: Ready for Phase 2 content migrations
