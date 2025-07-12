# Notes Migration Project Plan

**Project**: Website Architecture Upgrade - Notes Processor  
**Content Type**: Notes/Feed (Microblog)  
**Start Date**: 2025-07-11  
**Status**: [>] ACTIVE - Phase 1 Analysis

## Project Overview

Migrate notes/feed content from legacy string-based processing to AST-based infrastructure, enabling custom block support for rich microblog content while preserving all existing functionality.

## Success Criteria

### Phase 1: Domain Enhancement & Analysis
- [ ] Analyze current notes system (`loadFeed()`, `buildFeedPage()`)
- [ ] Research microblog best practices and patterns
- [ ] Enhance Note domain type with ITaggable interface
- [ ] Create comprehensive test scripts for validation
- [ ] Document current vs target architecture

### Phase 2: Processor Implementation
- [ ] Implement NoteProcessor in GenericBuilder following proven pattern
- [ ] Add `buildNotes()` function to Builder.fs 
- [ ] Integrate NEW_NOTES feature flag in Program.fs
- [ ] Enable custom block support for rich note content
- [ ] Validate AST-based processing with test content

### Phase 3: Migration Validation
- [ ] Create output comparison test scripts
- [ ] Validate 100% compatibility between old/new systems
- [ ] Test RSS feed generation and note feeds
- [ ] Verify microblog functionality preservation
- [ ] Confirm integration with other content types

### Phase 4: Production Deployment
- [ ] Deploy new processor as default (remove feature flag dependency)
- [ ] Remove legacy `loadFeed()` and `buildFeedPage()` functions
- [ ] Clean up unused imports and dependencies
- [ ] Archive project and update documentation
- [ ] Update changelog with completion metrics

## Current State Analysis

### Content Location & Structure
- **Source**: `_src/feed/*.md` (150+ note files)
- **Type**: Short-form content with `post_type: "note"`
- **Processing**: Legacy `loadFeed()` + `buildFeedPage()` functions
- **Output**: `/feed/` index page + RSS feeds
- **Features**: Microblog format, RSS generation, chronological listing

### Technical Context
- **Domain Type**: Note type exists but may need ITaggable enhancement
- **Legacy Functions**: `loadFeed()`, `buildFeedPage()` in current system
- **Dependencies**: 5 content types successfully migrated (Snippets, Wiki, Presentations, Books, Posts)
- **Infrastructure**: GenericBuilder and AST parsing proven and stable

## Migration Strategy

Following the proven 4-phase migration pattern that has succeeded for 5 consecutive content types:

1. **Domain Enhancement**: Add ITaggable interface to Note type, analyze current system
2. **Processor Implementation**: Create NoteProcessor using GenericBuilder pattern
3. **Migration Validation**: Parallel processing with feature flags, comprehensive testing
4. **Production Deployment**: Remove feature flags, clean up legacy code

## Research Context

### Microblog Patterns Research
- Research microblog processing patterns and IndieWeb standards
- Investigate note-specific custom blocks (status updates, photos, replies)
- Validate RSS feed requirements for microblog content
- Study successful F# microblog implementations

### Technical Dependencies
- **Completed Migrations**: Books, Posts, Wiki, Snippets, Presentations ✅
- **Infrastructure**: GenericBuilder, AST parsing, feature flags ✅  
- **Custom Blocks**: Media, review, venue, RSVP blocks available ✅
- **Test Framework**: Validation scripts and comparison tools ✅

## Implementation Timeline

- **Phase 1**: Analysis and domain enhancement (0.5 days)
- **Phase 2**: Processor implementation and feature flags (0.5 days)  
- **Phase 3**: Migration validation and testing (0.5 days)
- **Phase 4**: Production deployment and cleanup (0.5 days)

**Total Estimated Duration**: 1-2 days

## Documentation Plan

- Phase-specific development logs in `/logs/`
- Test scripts for validation and comparison
- Architecture documentation updates
- Changelog entry upon completion
- Project archival with lessons learned
