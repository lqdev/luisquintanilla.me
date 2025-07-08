# Core Infrastructure Implementation - Requirements

## Overview

**Feature Name**: Website Architecture Upgrade - Core Infrastructure Implementation  
**Priority**: High  
**Complexity**: Large  
**Estimated Effort**: 2-3 weeks

## Problem Statement

### What Problem Are We Solving?
The current website build system uses 20+ repetitive build functions in `Builder.fs` (581 lines) with string-based content parsing and no support for custom blocks. This creates massive code duplication, inconsistent content processing, and limited extensibility for IndieWeb features.

### Who Is This For?
- **Content Creators**: Need rich custom block support (media, reviews, venues, RSVPs)
- **Developers**: Need maintainable, extensible content processing system
- **Site Visitors**: Will benefit from improved content presentation and IndieWeb features

### Why Now?
This is the foundational phase that enables all subsequent migrations. Without this infrastructure, we cannot implement AST-based parsing, custom blocks, or unified content processing that the entire architectural upgrade depends on.

## Success Criteria

### Must Have (Core Requirements)
- [ ] `ASTParsing.fs` module with AST-based document parsing functions
- [ ] `CustomBlocks.fs` with support for `:::media`, `:::review`, `:::venue`, `:::rsvp` blocks
- [ ] `MediaTypes.fs` with IndieWeb media system (MediaType, AspectRatio, Location)
- [ ] `GenericBuilder.fs` with `buildContentWithFeeds` unified processing function
- [ ] `BlockRenderers.fs` with extensible custom block rendering system
- [ ] Enhanced `Domain.fs` with `ITaggable` interface and new content types
- [ ] Custom markdown pipeline with block parser registration
- [ ] All new modules compile without errors
- [ ] Existing build process remains unchanged and functional

### Should Have (Important Features)
- [ ] AST parsing replaces string manipulation for test content files
- [ ] Custom blocks render correctly in test scenarios
- [ ] Block renderer system supports extensibility for future block types
- [ ] Generic content processor pattern established for content type migrations

### Could Have (Nice to Have)
- [ ] Advanced block validation and error handling
- [ ] Debug output for block processing pipeline

### Won't Have (Explicitly Out of Scope)
- [ ] Migration of existing content types (separate projects)
- [ ] Removal of old build functions (Phase 4 project)
- [ ] Production deployment of new processors (waiting for content migrations)
- [ ] Advanced block features (galleries, mapping integration - Phase 5)

## User Stories

### Primary User Flow
**As a** content creator  
**I want** to use custom blocks in my markdown content  
**So that** I can create rich, structured content with media, reviews, venues, and RSVPs

### Edge Cases & Secondary Flows
- Custom blocks with malformed syntax should degrade gracefully
- Mixed content with both standard markdown and custom blocks
- Nested block structures and complex content hierarchies
- Block rendering in different contexts (cards, feeds, full pages)

## Technical Considerations

### Dependencies
- **Internal**: Current `Domain.fs`, `Builder.fs`, and content processing system
- **External**: Existing markdown processing pipeline and HTML generation
- **Blocking**: None - this is foundational infrastructure

### Integration Points
- Integrate with existing `Builder.fs` without breaking current functionality
- Extend current markdown pipeline with custom block parsing
- Maintain compatibility with existing content file formats
- Support parallel operation of old and new systems

### Technical Constraints
- Must not break existing build process during implementation
- New modules must compile alongside existing F# codebase
- Custom blocks must render to valid HTML output
- Performance must not degrade from current string-based processing

## Design & User Experience

### User Interface Requirements
- Custom blocks should render with consistent, accessible HTML structure
- Block content should be visually distinct from regular markdown
- Mobile-responsive rendering for all custom block types
- Integration with existing site styling and themes

### Content Strategy
- Custom blocks enable structured content authoring
- Support for IndieWeb microformats in block output
- Consistent metadata extraction across all content types
- Enhanced feed generation with structured block data

## Implementation Approach

### Recommended Strategy
**Parallel Development**: Build new infrastructure alongside existing system without breaking changes, using feature flags and gradual adoption pattern.

**Reference Specification**: See `website-upgrade.md` for detailed architecture and code examples.

**Core Types to Implement** (from specification):
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

**Key Functions to Implement** (from specification):
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

**Module Architecture**:
1. **ASTParsing.fs**: Core document parsing with AST generation
2. **CustomBlocks.fs**: Block type definitions and parsing logic
3. **MediaTypes.fs**: Rich media system for IndieWeb support
4. **GenericBuilder.fs**: Unified content processing pattern
5. **BlockRenderers.fs**: Extensible rendering system
6. **Domain.fs Enhancement**: `ITaggable` interface and type extensions

### Phases/Milestones
1. **Phase 1A**: Core module creation and compilation (ASTParsing, CustomBlocks, MediaTypes)
2. **Phase 1B**: Generic builder pattern and block renderers
3. **Phase 1C**: Domain enhancements and markdown pipeline integration
4. **Phase 1D**: Testing and validation with sample content

### Alternative Approaches Considered
- **Big Bang Migration**: Rejected due to high risk and breaking changes
- **String-Based Custom Blocks**: Rejected due to parsing complexity and maintenance burden
- **External Block Processing**: Rejected due to build pipeline complexity

## Testing Strategy

### Testing Requirements
- [ ] Unit tests for AST parsing functions
- [ ] Integration tests for custom block processing with Markdig
- [ ] Validation tests comparing new vs existing output
- [ ] YamlDotNet parsing tests for block content

### Test Cases
- Parse documents with various custom block combinations
- Render custom blocks in isolation and within complex documents
- Process real content files through new AST pipeline
- Validate HTML output structure and accessibility

## Documentation Requirements

### User Documentation
- [ ] Custom block syntax reference and examples
- [ ] Migration guide for content creators (future phase)
- [ ] Best practices for structured content authoring

### Technical Documentation
- [ ] Module architecture and API documentation
- [ ] Markdig extension integration guide for developers
- [ ] Integration points with existing system
- [ ] Custom block syntax and YamlDotNet usage patterns

## Success Metrics

### How Will We Measure Success?
- **Compilation**: All new modules compile without errors
- **Functionality**: Custom blocks render correctly in test scenarios
- **Stability**: Existing build process remains unchanged
- **Extensibility**: Block renderer system supports new block types
- **Integration**: Markdig pipeline processes custom blocks correctly

### Definition of Done
- [ ] All core infrastructure modules implemented and tested
- [ ] Custom block pipeline processes test content correctly
- [ ] No regression in existing build functionality
- [ ] Documentation covers module architecture and usage
- [ ] Foundation ready for content type migration projects

## Risks & Assumptions

### Key Risks
- **Complexity Risk**: AST parsing more complex than anticipated - Mitigation: Start with simple block types
- **Integration Risk**: Breaking existing functionality - Mitigation: Parallel development with feature flags
- **Markdig Extension Risk**: Custom block integration challenges - Mitigation: Research Markdig extension patterns early

### Assumptions
- Markdig provides sufficient functionality for custom block extensions
- YamlDotNet can handle structured content parsing within custom blocks
- Custom block syntax can be integrated with Markdig's markdown processing pipeline
- Existing content structure supports enhanced metadata extraction
- Block rendering system will handle all identified IndieWeb use cases

## Open Questions

- How should custom block validation errors be reported to content creators?
- What is the best pattern for integrating custom block parsers with Markdig's pipeline?
- How should complex nested block structures be handled?
- Should YamlDotNet parsing within blocks use the same configuration as front-matter parsing?

---

## Sign-off

**Requirements Author**: GitHub Copilot  
**Date Created**: 2025-07-08  
**Last Updated**: 2025-07-08  
**Approved By**: [User approved with Markdig/YamlDotNet technology choices]  
**Ready for Implementation**: [x] Yes / [ ] No

---

*This document will be used to create the detailed project plan in `projects/active/core-infrastructure.md` once approved*
