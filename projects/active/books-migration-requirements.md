# Books/Library Migration Requirements

## Overview

**Feature Name**: Books/Library Migration to AST-Based Processing  
**Priority**: Medium  
**Complexity**: Medium  
**Estimated Effort**: 1 week

## Problem Statement

### What Problem Are We Solving?
Books content type is currently loaded but not processed or displayed. The system has infrastructure for review blocks that perfectly matches book review use cases, but books aren't leveraging this proven architecture. Need to migrate books to use existing `:::review` blocks for consistency and enable proper book library functionality.

### Who Is This For?
- **Content creators**: Streamlined book review publishing workflow
- **Site visitors**: Access to book reviews and ratings in consistent format
- **Developers**: Unified content processing architecture across all content types

### Why Now?
- Books are the logical next migration after successful Snippets, Wiki, and Presentations migrations
- Existing review block infrastructure eliminates need for new custom block development
- Current books are loaded but not displayed, leaving functionality incomplete
- Architecture insight: "Books are reviews" - leverage existing proven patterns

## Success Criteria

### Must Have (Core Requirements)
- [ ] Books use existing `:::review` blocks for rating and metadata
- [ ] Book-specific metadata (ISBN, author, cover) preserved outside review blocks
- [ ] Library index page displays all books with review data
- [ ] Individual book pages render with consistent review formatting
- [ ] Feature flag controls migration (`NEW_BOOKS=true`)
- [ ] 100% output compatibility validation during migration
- [ ] RSS feed generation for books content type

### Should Have (Important Features)
- [ ] Microformats support inherited from existing review blocks
- [ ] Book status filtering (Read, Reading, Want to Read)
- [ ] Rating display and aggregation
- [ ] Review text preservation as main content
- [ ] Book cover image display integration

### Could Have (Nice to Have)
- [ ] ISBN-based metadata lookup integration
- [ ] Reading progress tracking
- [ ] Book recommendation system based on ratings
- [ ] Integration with external book APIs

### Won't Have (Explicitly Out of Scope)
- [ ] New custom block development (leverage existing review blocks)
- [ ] External API integrations (focus on migration first)
- [ ] Advanced reading tracking features
- [ ] Social features (sharing, following, etc.)

## User Stories

### Primary User Flow
**As a** content creator  
**I want** to publish book reviews using existing review block infrastructure  
**So that** I have consistent formatting and microformats support across all reviews

**As a** site visitor  
**I want** to browse book reviews with ratings and metadata  
**So that** I can discover books and read thoughtful reviews

**As a** developer  
**I want** books to use the same AST-based processing as other content types  
**So that** the architecture is consistent and maintainable

### Edge Cases & Secondary Flows
- Books without ratings (default to review without rating)
- Books with multiple review sections (preserve all content)
- Missing book metadata (graceful degradation)
- Long review content (proper content flow)

## Technical Approach

### Current State Analysis
- **Book Structure**: Metadata (rating, author, title, ISBN, cover, status) + Review sections
- **Infrastructure**: `:::review` blocks already implemented with rating, item_title, review_text
- **Loading**: Books loaded via `loadBooks` but not processed for display
- **Review Blocks**: ReviewData type with rating, item_title, review_text, item_url, review_date

### Migration Strategy
1. **Leverage Existing Review Blocks**: Map book reviews to proven `:::review` block structure
2. **Preserve Book Metadata**: Keep ISBN, author, cover, status as book-specific metadata
3. **Content Mapping**: Review text becomes main content, review block handles rating/title metadata
4. **Feature Flag Safety**: Use `NEW_BOOKS=true` for safe parallel processing
5. **Infrastructure Reuse**: Use existing ReviewRenderer for consistent output

### Data Mapping
```
Book Metadata -> Review Block + Book Metadata
- rating -> ReviewData.rating
- title -> ReviewData.item_title  
- Review content -> main content (outside block)
- ISBN, author, cover, status -> preserved as book metadata
```

## Architecture Considerations

### Review Block Integration
- **Existing ReviewData**: rating, item_title, max_rating, review_text, item_url, review_date
- **Book Mapping**: rating → rating, title → item_title, preserve other book fields
- **Content Flow**: Review blocks for metadata, main content for review text

### Migration Pattern Consistency
- **Phase 1**: Domain enhancement (BookDetails implements ITaggable)
- **Phase 2**: Processor implementation (BookProcessor using GenericBuilder)
- **Phase 3**: Migration validation (output comparison, feature flag testing)
- **Phase 4**: Production deployment (legacy code removal)

### Technical Dependencies
- **Existing Infrastructure**: AST parsing, GenericBuilder pattern, ReviewRenderer
- **Review Blocks**: Already implemented and proven in custom block system
- **Feature Flags**: NEW_BOOKS environment variable for safe migration
- **Testing Framework**: Output comparison scripts from previous migrations

## Risk Assessment

### Low Risk Factors
- **Proven Infrastructure**: Review blocks already implemented and working
- **Small Content Set**: ~37 book files, manageable scope
- **Established Pattern**: Fourth migration using validated approach
- **Existing Loader**: Books already loaded, just need processing

### Mitigation Strategies
- **Leverage Existing Architecture**: Use proven review blocks instead of new development
- **Feature Flag Safety**: Parallel old/new processing with rollback capability
- **Incremental Testing**: Validate each book file individually
- **Content Preservation**: Ensure all book metadata and review content preserved

## Acceptance Criteria

### Migration Validation
- [ ] All book files parse correctly with new processor
- [ ] Review blocks contain proper rating and title metadata
- [ ] Book-specific metadata (ISBN, author, cover) preserved
- [ ] Library index page displays all books with review data
- [ ] Individual book pages render correctly
- [ ] RSS feed generates with book review data
- [ ] Feature flag controls enable/disable new processing

### Quality Assurance
- [ ] Zero regression in existing functionality
- [ ] Output compatibility validation passes
- [ ] Review block microformats work correctly
- [ ] Book cover images display properly
- [ ] Rating display matches review block standards

### Performance & Reliability
- [ ] Build time impact minimal
- [ ] Memory usage within acceptable limits
- [ ] Error handling for missing/malformed book data
- [ ] Graceful degradation for incomplete book metadata

## Timeline & Milestones

### Phase 1: Domain Enhancement (Days 1-2)
- Enhance BookDetails to implement ITaggable interface
- Map book metadata to review block structure
- Create test scripts for validation

### Phase 2: Processor Implementation (Days 3-4)
- Implement BookProcessor using GenericBuilder pattern
- Add review block integration for book content
- Implement feature flag logic in Program.fs

### Phase 3: Migration Validation (Days 5-6)
- Create output comparison test scripts
- Validate review block integration
- Test library and individual book page generation
- Confirm RSS feed functionality

### Phase 4: Production Deployment (Day 7)
- Deploy new processor as default
- Remove legacy code if any exists
- Archive project and update documentation

## Definition of Done

- [ ] All acceptance criteria validated
- [ ] Migration pattern successfully applied to books
- [ ] Review block integration working correctly
- [ ] Library functionality fully operational
- [ ] Feature flag migration completed
- [ ] Documentation updated with lessons learned
- [ ] Project archived following workflow protocols

This migration leverages existing proven architecture (review blocks) to implement book functionality efficiently while maintaining consistency with the established migration pattern.
