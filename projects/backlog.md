# Website Development Backlog

*Last Updated: 2025-07-08*

This backlog drives the systematic architectural upgrade of the indieweb content management system, transforming from a collection of 20+ repetitive build functions to a unified, generic content processing system with custom block support.

## High Priority (Critical Infrastructure)

### [✅] Phase 1: Core Infrastructure Implementation - COMPLETE
**Project**: Website Architecture Upgrade - Foundation  
**Complexity**: Large  
**Completed**: 2025-07-08 (1 day - all 4 phases)
**Status**: ✅ Complete - Archived in `projects/archive/core-infrastructure.md`

**Achievements**:
- ✅ All new modules compile without errors
- ✅ Existing build process unchanged and functional
- ✅ No breaking changes to current content
- ✅ Custom markdown pipeline processes blocks correctly
- ✅ AST parsing validated against string manipulation
- ✅ Comprehensive testing and documentation complete
- ✅ Feature flag pattern prepared for Phase 2

---

### [✅] Feature Flag Infrastructure - COMPLETE
**Project**: Website Architecture Upgrade - Migration Control  
**Complexity**: Small  
**Completed**: 2025-07-08 (1 day - all 3 phases)  
**Status**: ✅ Complete - Archived in `projects/archive/feature-flag-infrastructure.md`

**Achievements**:
- ✅ Environment variable-based feature flags for all content types
- ✅ Parallel old/new processor execution framework
- ✅ Output validation and comparison tools
- ✅ Migration progress tracking and reporting
- ✅ Automated documentation generation
- ✅ Program.fs integration with zero build impact
- ✅ Comprehensive testing and validation complete

**Success Criteria Met**:
- ✅ Feature flags control old vs new processing per content type
- ✅ Both systems can run in parallel without conflicts
- ✅ Output validation validates identical results
- ✅ Clear migration progress visibility
- ✅ Ready for Phase 2 content migrations

---

## Medium Priority (Content Migration & Core Features)

### [✅] Phase 2: Snippets Migration (Pilot Content Type) - COMPLETE
**Project**: Website Architecture Upgrade - Snippets Processor  
**Complexity**: Medium  
**Completed**: 2025-01-08 (1 day)  
**Status**: ✅ Complete - Archived in `projects/archive/snippets-migration.md`

**Achievements**:
- ✅ 100% output compatibility validated (13/13 files identical)
- ✅ AST-based parsing replaced string manipulation
- ✅ Feature flag migration pattern proven successful
- ✅ ITaggable interface implemented for unified tag processing
- ✅ New processor deployed as production default
- ✅ Old code removed, codebase simplified
- ✅ Migration pattern validated for future content types

---

### [ ] Phase 2: Wiki Content Migration
**Project**: Website Architecture Upgrade - Wiki Processor  
**Complexity**: Medium  
**Estimated Effort**: 1 week  
**Dependencies**: ✅ Snippets Migration Success

Migrate wiki content to new processor:
- Standardize wiki metadata format
- Implement wiki-specific custom blocks
- Create wiki processor with AST parsing
- Build wiki card and RSS renderers
- Add `NEW_WIKI=true` feature flag

**Success Criteria**:
- [ ] Wiki uses new generic processor pattern
- [ ] Wiki metadata standardized and validated
- [ ] Custom blocks functional in wiki content
- [ ] Feature flag enables safe migration
- [ ] Output validation passes

---

### [ ] Phase 2: Presentations Migration
**Project**: Website Architecture Upgrade - Presentations Processor  
**Complexity**: Medium  
**Estimated Effort**: 1 week  
**Dependencies**: Wiki Migration Success

Migrate presentations to new processor:
- Enhance presentation metadata with custom blocks
- Add `:::venue` blocks for presentation locations
- Implement presentation processor
- Create presentation card and RSS renderers
- Add `NEW_PRESENTATIONS=true` feature flag

**Success Criteria**:
- [ ] Presentations use AST-based processing
- [ ] `:::venue` blocks render presentation locations
- [ ] Presentation feeds generated automatically
- [ ] Migration validated through feature flag

---

### [ ] Phase 2: Books/Library Migration
**Project**: Website Architecture Upgrade - Books with Review Blocks  
**Complexity**: Medium  
**Estimated Effort**: 1 week  
**Dependencies**: Presentations Migration Success

**Revised Approach**: Convert books to use existing `:::review` blocks for consistency.

**Key Insight**: Books are reviews, and the system already has review blocks! Use existing proven architecture.

**Current Books Structure**:
- Metadata: `rating: 5.0`, `status: "Read"`, `author`, `title`, `isbn`
- Content: `## Review` sections with review text

**Migration Strategy**:
- Convert `## Review` sections to `:::review` blocks for metadata only (rating, item title)
- Keep review text as the post's main content, not inside the review block
- Map book metadata to review block structure (`rating`, `item_title`) 
- Preserve `isbn`, `author`, `cover` as book-specific metadata outside the block
- Use existing `ReviewRenderer` for consistent metadata output and microformats
- Add `NEW_BOOKS=true` feature flag

**Success Criteria**:
- [ ] Books use existing `:::review` blocks for review content
- [ ] Book metadata properly mapped to `ReviewData` structure
- [ ] Library pages render with consistent review formatting
- [ ] Microformats support inherited from existing review blocks
- [ ] All existing book functionality preserved
- [ ] Migration validated through feature flag and output comparison

---

### [ ] Phase 2: Posts Migration
**Project**: Website Architecture Upgrade - Posts Processor  
**Complexity**: Large  
**Estimated Effort**: 1-2 weeks  
**Dependencies**: Books Migration Success

Migrate main blog posts to new processor:
- Enhance post metadata standardization
- Add custom block support to posts
- Implement post processor with full feature set
- Create post card and RSS renderers
- Add `NEW_POSTS=true` feature flag

**Success Criteria**:
- [ ] Posts use AST-based processing
- [ ] Custom blocks functional in post content
- [ ] Post feeds generated through unified system
- [ ] All existing post functionality preserved

---

### [ ] Phase 2: Responses Migration
**Project**: Website Architecture Upgrade - Responses Processor  
**Complexity**: Medium  
**Estimated Effort**: 1 week  
**Dependencies**: Posts Migration Success

Migrate response content with IndieWeb features:
- Add `:::rsvp` blocks for event responses
- Standardize response metadata
- Implement response processor
- Create response card and RSS renderers
- Add `NEW_RESPONSES=true` feature flag

**Success Criteria**:
- [ ] Responses use new processor pattern
- [ ] `:::rsvp` blocks enable structured event responses
- [ ] Response feeds generated automatically
- [ ] IndieWeb response types supported

---

### [ ] Phase 2: Albums Migration (Final Content Type)
**Project**: Website Architecture Upgrade - Albums Processor  
**Complexity**: Large  
**Estimated Effort**: 1-2 weeks  
**Dependencies**: Responses Migration Success

Migrate albums with advanced media support:
- Convert albums to `:::media` blocks
- Implement rich media metadata
- Support mixed media content types
- Create album processor with media rendering
- Add `NEW_ALBUMS=true` feature flag

**Success Criteria**:
- [ ] Albums use `:::media` blocks exclusively
- [ ] Mixed media content supported
- [ ] Album feeds generated automatically
- [ ] Advanced media metadata preserved

---

### [ ] Phase 3: Unified Feed System
**Project**: Website Architecture Upgrade - Feed Consolidation  
**Complexity**: Large  
**Estimated Effort**: 1-2 weeks  
**Dependencies**: All Content Types Migrated

Replace multiple feed generation with unified system:
- Remove individual feed functions (`buildFeedPage`, `buildFeedRssPage`, etc.)
- Implement `buildMainFeeds(allData)` single-pass generation
- Generate fire-hose and type-specific feeds automatically
- Consolidate RSS and HTML feed generation

**Success Criteria**:
- [ ] Single `buildMainFeeds` call generates all feeds
- [ ] Fire-hose feed includes all content types
- [ ] Type-specific feeds generated automatically
- [ ] RSS and HTML feeds stay synchronized
- [ ] Feed generation performance improved

---

## Low Priority (Code Cleanup & Optimization)

### [ ] Phase 4: Legacy Code Removal
**Project**: Website Architecture Upgrade - Cleanup  
**Complexity**: Medium  
**Estimated Effort**: 1 week  
**Dependencies**: Unified Feed System Complete

Remove old build functions and clean up codebase:
- Remove 20+ old `build*Page` functions from `Builder.fs`
- Remove old feed generation functions
- Simplify main builder to single `buildAllSite()` function
- Clean up unused domain types

**Success Criteria**:
- [ ] `Builder.fs` reduced from 581+ lines to ~200 lines (65% reduction)
- [ ] Single `buildAllSite()` function builds everything
- [ ] No unused functions or types remain
- [ ] Build process simplified and streamlined

---

### [ ] Phase 5: Performance Optimization
**Project**: Website Architecture Upgrade - Performance  
**Complexity**: Medium  
**Estimated Effort**: 1 week  
**Dependencies**: Legacy Code Removal

Optimize build performance and memory usage:
- Implement parallel content processing
- Optimize memory usage during builds
- Add build time metrics and monitoring
- Implement incremental builds where possible

**Success Criteria**:
- [ ] Build time improved through parallel processing
- [ ] Memory usage optimized for large content sets
- [ ] Build metrics provide performance visibility
- [ ] Incremental builds reduce development cycle time

---

### [ ] Phase 5: Advanced Custom Blocks
**Project**: Website Architecture Upgrade - Enhanced Blocks  
**Complexity**: Medium  
**Estimated Effort**: 1-2 weeks  
**Dependencies**: Performance Optimization

Add advanced custom block features:
- Enhanced `:::media` blocks with galleries
- Advanced `:::review` blocks with structured data
- `:::venue` blocks with mapping integration
- Complex `:::rsvp` blocks with calendar integration

**Success Criteria**:
- [ ] Gallery support in `:::media` blocks
- [ ] Structured review data for search/filtering
- [ ] Venue blocks integrate with mapping services
- [ ] RSVP blocks generate calendar events

---

## Research Priority (Future Exploration)

### [ ] Search Data Generation with Embeddings
**Project**: Website Architecture Upgrade - Search Enhancement  
**Complexity**: Large  
**Estimated Effort**: 2-3 weeks  
**Dependencies**: Advanced Custom Blocks

Implement semantic search capabilities:
- Generate search data from all content
- Prepare content for embedding generation
- Create search API endpoints
- Build client-side search interface

**Success Criteria**:
- [ ] Search data generated for all content types
- [ ] Content prepared for vector embeddings
- [ ] Search API functional and performant
- [ ] Client-side search interface responsive

---

### [ ] Client-Side Feed Switching
**Project**: Website Architecture Upgrade - Dynamic Feeds  
**Complexity**: Medium  
**Estimated Effort**: 1-2 weeks  
**Dependencies**: Search Data Generation

Enable dynamic feed filtering and switching:
- Generate JSON data for client-side processing
- Build interactive feed filtering interface
- Implement real-time feed switching
- Add feed customization features

**Success Criteria**:
- [ ] JSON feed data available for all content
- [ ] Interactive filtering interface functional
- [ ] Real-time feed updates without page reload
- [ ] User customization preferences persist

---

### [ ] Microformats IndieWeb Compliance
**Project**: Website Architecture Upgrade - IndieWeb Standards  
**Complexity**: Large  
**Estimated Effort**: 2-3 weeks  
**Dependencies**: Client-Side Feed Switching

Implement full IndieWeb microformats support:
- Add microformats markup to all content types
- Implement webmention support enhancements
- Create IndieWeb-compliant card rendering
- Add POSSE (Publish Own Site, Syndicate Elsewhere) support

**Success Criteria**:
- [ ] All content includes proper microformats
- [ ] Webmention support enhanced and tested
- [ ] IndieWeb validators pass for all pages
- [ ] POSSE integration functional

---

## Backlog Management Notes

**Migration Safety**: Each phase includes rollback capabilities through feature flags and parallel system operation.

**Validation Strategy**: Every migration step includes output validation to ensure identical results between old and new systems.

**Testing Approach**: Continuous compilation and testing throughout implementation, with comprehensive regression testing before legacy removal.

**Documentation**: Each completed phase updates project documentation and creates archived project plans with lessons learned.

**Success Metrics**: 
- Builder.fs: 580+ lines → ~200 lines (65% reduction)
- Single-pass content processing for all types
- Custom block system enabling rich content
- Unified feed generation replacing multiple functions
- Performance improvements through optimized processing

This backlog follows the proven development workflow ensuring systematic, documented, and quality-focused development while preserving functionality throughout the architectural upgrade.
