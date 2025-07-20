# Website Development Backlog

*Last Updated: 2025-07-13*

This backlog drives the systematic architectural upgrade of the indieweb content management system. **Core infrastructure complete** - all 8 content types successfully migrated to unified GenericBuilder pattern with AST-based processing and custom block support. Focus now shifts to URL structure optimization, legacy code cleanup, and enhancement features.

## 🎯 Current Status: Architecture Complete ✅

**Infrastructure Achievement**: All 8 content types successfully migrated to unified GenericBuilder pattern:
- ✅ **Completed Migrations**: Snippets, Wiki, Presentations, Books, Posts, Notes, Responses, Albums
- ✅ **Infrastructure**: Unified Feed System with single-pass processing 
- ✅ **Critical Fixes**: All functionality restored (presentations render as reveal.js slideshows, media blocks display correctly)
- ✅ **ViewEngine Integration**: Type-safe HTML generation throughout GenericBuilder replacing sprintf string concatenation
- 🎯 **Current Focus**: URL structure optimization and legacy code cleanup

**Recent Achievement (2025-01-22)**: Fixed media content rendering (:::media blocks now display semantic HTML instead of raw YAML) and completed ViewEngine conversion for improved type safety and maintainability.

**Migration Success**: Feature flag pattern proven across 8 consecutive deployments. All critical functionality preserved with zero regressions. System now uses type-safe HTML generation throughout. Ready for enhancement phase.

## High Priority (URL Structure & Cleanup)

### [>] URL Alignment & Feed Discovery Optimization - ACTIVE
**Project**: Website URL Structure & Feed Discovery  
**Complexity**: Medium  
**Started**: 2025-01-13  
**Status**: 🎯 Active - `projects/active/url-alignment-comprehensive.md`

**Objectives**:
- Align all URLs with W3C "Cool URIs don't change" principles
- Implement research-backed feed discovery optimization (content-proximate placement)
- Create semantic separation between content, collections, and resources
- **IndieWeb compliance**: Full microformats2 markup and webmention compatibility
- Ensure zero broken links with comprehensive 301 redirect strategy

**Key Changes**:
- Move `/albums/` → `/media/`, `/library/` → `/resources/library/`
- Add new content types: `/bookmarks/`, `/reviews/` 
- Reorganize static assets: `/images/` → `/assets/images/`
- Implement content-proximate feeds for 82% better discoverability

### [ ] Legacy Code Removal & Builder.fs Cleanup
**Project**: Website Architecture Upgrade - Final Cleanup  
**Complexity**: Medium  
**Estimated Effort**: 2-3 days  
**Dependencies**: URL Alignment Complete

**Immediate Objectives** (post URL alignment):
- Remove unused build functions and legacy code from Builder.fs
- Clean up old feed generation functions (replaced by unified system)
- Simplify main build orchestration
- Remove unused domain types and imports

**Success Criteria**:
- [ ] Builder.fs simplified to core orchestration functions only
- [ ] All unused functions and imports removed
- [ ] Build process streamlined and documented
- [ ] Zero technical debt remaining from migration phase

---

## ✅ Completed Infrastructure (Archived)

### Core Architecture Migrations - ALL COMPLETE
All foundational infrastructure successfully implemented and deployed:

- ✅ **Phase 1**: Core Infrastructure (GenericBuilder, AST processing, custom blocks)
- ✅ **Phase 2**: All 8 Content Type Migrations (Snippets → Wiki → Presentations → Books → Posts → Notes → Responses → Albums)
- ✅ **Phase 3**: Unified Feed System (single-pass processing, RSS 2.0 compliance)
- ✅ **Phase 3.5**: Critical Fixes (presentation reveal.js slideshow restoration)

**Architecture Achievement**: Builder.fs transformed from 20+ repetitive functions to unified GenericBuilder pattern. All content types process consistently through AST-based system with custom block support.

**Migration Success**: 8 consecutive successful deployments using proven feature flag pattern. Zero regressions, all functionality preserved.  
---

## Medium Priority (Performance & Enhancement)

### [ ] Performance Optimization
**Project**: Build Performance & Memory Optimization  
**Complexity**: Medium  
**Estimated Effort**: 1-2 weeks  
**Dependencies**: Legacy Code Removal Complete

**Objectives**:
- Implement parallel content processing where possible
- Optimize memory usage during large content builds
- Add build time metrics and performance monitoring
- Investigate incremental build capabilities

**Success Criteria**:
- [ ] Build time improvements through parallelization
- [ ] Memory usage optimized for 1000+ content items
- [ ] Performance metrics provide build visibility
- [ ] Development cycle time reduced

### [ ] Advanced Custom Blocks
**Project**: Enhanced Block Features  
**Complexity**: Medium  
**Estimated Effort**: 2-3 weeks  
**Dependencies**: Performance Optimization

**New Block Types**:
- Enhanced `:::media` blocks with gallery support
- Advanced `:::review` blocks with structured metadata
- `:::venue` blocks with location/mapping integration
- `:::event` blocks with calendar integration and RSVP support

**Success Criteria**:
- [ ] Gallery functionality in media blocks
- [ ] Structured review data for filtering/search
- [ ] Venue blocks integrate with mapping services
- [ ] Event blocks generate calendar files

---

## Research Priority (Future Exploration)

### [ ] Semantic Search Implementation
**Project**: Content Search & Discovery  
**Complexity**: Large  
**Estimated Effort**: 3-4 weeks  
**Dependencies**: Advanced Custom Blocks

**Capabilities**:
- Generate search indexes from all content types
- Prepare content for vector embedding generation
- Create fast client-side search interface
- Implement semantic content discovery

**Success Criteria**:
- [ ] Comprehensive search data generation
- [ ] Content prepared for embedding workflows
- [ ] Fast, responsive search interface
- [ ] Semantic content recommendations

### [ ] Dynamic Feed Management
**Project**: Client-Side Feed Customization  
**Complexity**: Medium  
**Estimated Effort**: 2-3 weeks  
**Dependencies**: Semantic Search Implementation

**Features**:
- Real-time feed filtering and switching
- User-customizable feed combinations
- Interactive content discovery interface
- Personalized content recommendations

**Success Criteria**:
- [ ] Dynamic feed filtering without page reloads
- [ ] User preferences persist across sessions
- [ ] Interactive content discovery tools
- [ ] Personalized content curation

---

## Backlog Management & Success Metrics

### Current Architecture State
**✅ Infrastructure Complete**: All 8 content types unified under GenericBuilder pattern with AST-based processing and custom block support. Feature flag migration pattern proven across consecutive deployments.

**🎯 Current Focus**: URL structure optimization (active project) followed by legacy code cleanup to complete the architectural transformation.

### Migration Methodology (Proven Pattern)
- **Safety First**: Feature flags enable parallel system operation during transitions
- **Validation Strategy**: Output comparison ensures identical results before legacy removal
- **Continuous Testing**: Build and test after each change with comprehensive regression testing
- **Documentation**: Complete project archival with lessons learned for future reference

### Success Metrics Achieved
- **Code Unification**: All content types process through single GenericBuilder pattern
- **Performance**: Single-pass feed processing (65.6 items/sec across 1129 items)
- **Architecture Consistency**: Custom block system enables rich content across all types
- **Zero Regressions**: All migrations completed with full functionality preservation
- **Documentation**: Complete project history archived with proven methodologies

### Next Phase Readiness
**URL Alignment Project**: Foundation complete for comprehensive URL restructuring with IndieWeb standards compliance, feed discovery optimization, and asset reorganization following industry best practices.

This backlog reflects the completed architectural transformation from repetitive build functions to unified, generic content processing with custom block support, ready for URL optimization and enhancement features.
