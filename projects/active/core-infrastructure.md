# Core Infrastructure Implementation - Project Plan

## Project Overview

**Status**: Active  
**Start Date**: 2025-07-08  
**Estimated Completion**: 2025-07-25 (3 weeks)  
**Priority**: High (Critical Infrastructure)

**Goals**: Implement foundational infrastructure for website architecture upgrade using Markdig for AST parsing and YamlDotNet for structured content within custom blocks.

**Success Metrics**:
- All 6 core modules implemented and compiling
- Custom blocks (`:::media`, `:::review`, `:::venue`, `:::rsvp`) parsing and rendering
- Existing build process unchanged during parallel development
- Foundation ready for content type migration projects

## Technical Approach

### Technology Stack
- **AST Parsing**: Markdig for markdown processing with custom extensions
- **Structured Content**: YamlDotNet for parsing YAML within custom blocks
- **Architecture**: Parallel development alongside existing system
- **Integration**: Custom Markdig pipeline with block parser registration

### Reference Architecture
**Complete Specification**: `website-upgrade.md` contains detailed architecture, migration strategy, and code examples.

**Target Architecture** (from specification):
- Replace current `getContentAndMetadata` string manipulation with AST-based parsing
- Implement `ContentProcessor<'T>` pattern for unified content processing
- Create `ITaggable` interface for consistent tag handling across all content types
- Build `buildContentWithFeeds` for single-pass content processing and feed generation
- Support custom blocks: `:::media`, `:::review`, `:::venue`, `:::rsvp`

### Module Architecture

1. **ASTParsing.fs** - Core document parsing with Markdig AST generation
2. **CustomBlocks.fs** - Block type definitions and Markdig extension integration  
3. **MediaTypes.fs** - IndieWeb media system (MediaType, AspectRatio, Location)
4. **GenericBuilder.fs** - Unified `buildContentWithFeeds` processing pattern
5. **BlockRenderers.fs** - Extensible rendering system for all block types
6. **Domain.fs Enhancement** - `ITaggable` interface and unified content types

## Implementation Phases

### Phase 1A: Core Module Foundation (Days 1-5) ✅ COMPLETE
**Objective**: Create core modules and establish Markdig integration

**Reference**: `website-upgrade.md` Phase 1 - AST-based parsing foundation

**Tasks**:
- [x] Create `ASTParsing.fs` with Markdig pipeline setup
- [x] Implement `parseDocumentFromAst` function (replaces current `getContentAndMetadata` string manipulation)
- [x] Create `CustomBlocks.fs` with block type definitions (`Media`, `ReviewData`, etc.)
- [x] Research and implement Markdig custom extension pattern
- [x] Create `MediaTypes.fs` with IndieWeb media types (MediaType, AspectRatio, Location)
- [x] Ensure all modules compile without errors

**Validation**: ✅ PASSED
- New modules compile alongside existing code
- Basic Markdig pipeline processes standard markdown
- Custom block type definitions are well-formed

**Completion Date**: 2025-07-08
**Tested**: Via `test-ast-parsing.fsx` - all parsing functions working correctly

### Phase 1B: Generic Builder and Renderers (Days 6-10) ✅ COMPLETE
**Objective**: Implement unified content processing and rendering system

**Reference**: `website-upgrade.md` Core Types - `ContentProcessor<'T>` and `FeedData`

**Tasks**:
- [x] Create `GenericBuilder.fs` with `buildContentWithFeeds` function (from specification)
- [x] Implement `ContentProcessor<'T>` pattern exactly as defined in specification
- [x] Create `BlockRenderers.fs` with extensible rendering system
- [x] Implement block-specific renderers for each custom block type
- [x] Add YamlDotNet integration for structured block content
- [x] Create card and RSS rendering functions (`renderCard` from specification)

**Validation**: ✅ PASSED
- Generic content processor pattern functional
- Block renderers produce valid HTML output
- YamlDotNet correctly parses block content

**Completion Date**: 2025-07-08
**Tested**: Via `test-phase1b.fsx` - all renderers functional, HTML output verified
- YamlDotNet correctly parses block content

### Phase 1C: Domain Enhancement and Pipeline Integration (Days 11-15) ✅ COMPLETE
**Objective**: Enhance domain types and integrate custom markdown pipeline

**Reference**: `website-upgrade.md` ITaggable interface and Phase 1 pipeline requirements

**Tasks**:
- [x] Add `ITaggable` interface to `Domain.fs` (exact specification from upgrade doc)
- [x] Enhance existing content types to implement `ITaggable`
- [x] Register custom block parsers with Markdig pipeline
- [x] Implement complete markdown processing with custom blocks
- [x] Create pipeline configuration for block registration
- [x] Add `parseCustomBlocks` function from specification

**Validation**:
- ✅ `ITaggable` interface works with existing content types
- ✅ Custom blocks parse correctly within markdown documents
- ✅ Pipeline processes mixed content (standard markdown + custom blocks)

**Completion Date**: 2025-07-08
**Tested**: Via `test-phase1c.fsx` - all ITaggable functionality and parseCustomBlocks validated
- ITaggable interface working with Post, Snippet, Wiki types
- parseCustomBlocks function finding 4 block types successfully
- Integration with AST parsing system operational
- Bug in filterCustomBlocks identified and resolved

### Phase 1D: Testing and Validation (Days 16-21) ✅ COMPLETE
**Objective**: Comprehensive testing and preparation for content migrations

**Reference**: `website-upgrade.md` Phase 2 preparation - feature flag patterns

**Tasks**:
- [x] Create test content files with all custom block types
- [x] Test `parseDocumentFromAst` vs existing `getContentAndMetadata` (string-based)
- [x] Validate custom block rendering in different contexts
- [x] Test integration with existing build process (parallel development)
- [x] Create documentation for module architecture
- [x] Prepare for Phase 2 feature flag pattern (example from specification)

**Validation**: ✅ ALL CRITERIA MET
- All custom blocks render correctly in test scenarios
- No regression in existing build functionality  
- Foundation ready for snippets migration (Phase 2)

**Completion Results**:
- **Test Scripts Created**: Comprehensive validation of all infrastructure components
- **Documentation**: Complete module architecture and feature flag pattern guides
- **Integration**: Zero conflicts with existing build process
- **Quality**: All success criteria achieved, ready for Phase 2

## Dependencies and Integration Points

### Internal Dependencies
- **Domain.fs**: Enhance with `ITaggable`, maintain backward compatibility
- **Builder.fs**: Integrate new functions without breaking existing functionality
- **Existing Content Types**: Ensure compatibility with enhanced metadata

### External Dependencies
- **Markdig**: Custom extension development for block parsing
- **YamlDotNet**: Structured content parsing within blocks
- **Existing Markdown Pipeline**: Integration without breaking current processing

### Integration Strategy
- **Parallel Development**: Build alongside existing system
- **Backward Compatibility**: All existing functionality preserved
- **Gradual Adoption**: New infrastructure available but not required initially

## Risk Mitigation

### Technical Risks
1. **Markdig Extension Complexity**: Research extension patterns early, start with simple blocks
2. **Integration Breaking Changes**: Parallel development with comprehensive testing
3. **YamlDotNet Configuration**: Use consistent configuration with existing front-matter parsing

### Mitigation Strategies
- Start with simplest custom blocks (`:::review`) before complex ones (`:::media`)
- Continuous compilation testing throughout development
- Feature flag approach for gradual integration testing

## Testing Strategy

### Unit Testing
- AST parsing functions with various markdown inputs
- Custom block parsing with YamlDotNet
- Block renderer output validation
- `ITaggable` interface implementation verification

### Integration Testing
- Complete pipeline processing with mixed content
- Custom block rendering in different contexts (cards, feeds, pages)
- Compatibility with existing content processing

### Validation Testing
- Output comparison between new and existing systems
- Regression testing for existing functionality
- Custom block syntax validation and error handling

## Success Criteria

### Must Complete
- [ ] All 6 modules implemented and compiling
- [ ] Custom block pipeline processes test content correctly
- [ ] No regression in existing build functionality
- [ ] `ITaggable` interface unified tag processing
- [ ] Markdig pipeline with custom block support

### Documentation Deliverables
- [ ] Module architecture documentation
- [ ] Custom block syntax reference
- [ ] Markdig extension integration guide
- [ ] `ITaggable` interface usage patterns

### Readiness for Next Phase
- [ ] Foundation supports content type migrations
- [ ] Snippets migration can begin immediately
- [ ] Generic content processor pattern proven

## Next Steps After Completion

1. **Phase 2 Start**: Begin snippets migration using new infrastructure
2. **Feature Flag Setup**: Implement feature flag system for safe content migration
3. **Content Type Planning**: Prepare detailed migration plans for each content type

## Reference Documentation

### Primary Specification
**File**: `website-upgrade.md`
- **Core Types**: Lines defining `ContentProcessor<'T>`, `ITaggable`, `ParsedDocument`, custom block types
- **Key Functions**: `buildContentWithFeeds`, `parseDocumentFromAst`, `parseCustomBlocks`, `buildMainFeeds`
- **Migration Strategy**: Phase 1 parallel development approach
- **Feature Flag Pattern**: Phase 2 examples for content type migration

### Current Implementation Analysis
**File**: `Services/Markdown.fs` 
- **Current Problem**: `getContentAndMetadata` uses string manipulation (lines 56-66)
- **Target Replacement**: AST-based parsing with `parseDocumentFromAst`

**File**: `Domain.fs`
- **Current State**: Inconsistent tag handling across content types
- **Target Enhancement**: Unified `ITaggable` interface for all content types

### Implementation Context
This project implements the foundational infrastructure from `website-upgrade.md` Phase 1, enabling the subsequent content migration phases. All code samples and patterns should follow the specification exactly to ensure compatibility with the planned migration strategy.

---

**Project Log**: All progress documented in `logs/2025-07-08-log.md` and subsequent daily logs  
**Requirements**: Detailed in `projects/active/core-infrastructure-requirements.md`  
**Backlog Reference**: `projects/backlog.md` - Phase 1: Core Infrastructure Implementation
