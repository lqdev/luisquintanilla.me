# Website Development Backlog

*Last Updated: 2025-01-13*

This backlog drives the systematic architectural upgrade of the indieweb content management system, transforming from a collection of 20+ repetitive build functions to a unified, generic content processing system with custom block support.

## 🎯 Current Status: Critical Functionality Fix Required 🚨

**Migration Progress**: 8/8 content types successfully migrated to AST-based GenericBuilder infrastructure
- ✅ **Completed**: Snippets, Wiki, Presentations, Books, Posts, Notes, Responses, Albums
- ✅ **Infrastructure**: Unified Feed System Complete
- 🚨 **Critical Issue**: Presentation rendering broken - slideshows display as static content
- 🎯 **Next Priority**: Phase 3.5 - Presentation Rendering Fix (restore reveal.js functionality)

**Architecture Achievement**: All content types unified under GenericBuilder pattern with custom block support. Feature flag migration pattern proven across 8 consecutive successful deployments. **Critical regression**: Presentations need reveal.js slideshow restoration.

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

### [✅] Phase 2: Wiki Content Migration - COMPLETE
**Project**: Website Architecture Upgrade - Wiki Processor  
**Complexity**: Medium  
**Completed**: 2025-01-08 (1 day - all 3 phases)  
**Status**: ✅ Complete - Archived in `projects/archive/wiki-migration.md`

**Achievements**:
- ✅ 100% output compatibility validated (28/28 files identical)
- ✅ AST-based parsing replaced string manipulation for wiki content
- ✅ Feature flag migration pattern proven successful for second content type
- ✅ New processor deployed as production default
- ✅ Legacy code removed, codebase simplified (~20 lines reduction)
- ✅ Migration pattern validated for future content types

---

### [✅] Phase 2: Presentations Migration - COMPLETE
**Project**: Website Architecture Upgrade - Presentations Processor  
**Complexity**: Medium  
**Completed**: 2025-01-09 (1 day - all 4 phases)  
**Status**: ✅ Complete - Archived in `projects/archive/presentations-migration.md`

**Achievements**:
- ✅ 3/3 presentations migrated to AST-based processing
- ✅ RSS feed generation added (presentations/feed/index.xml)
- ✅ Feature flag migration pattern proven for third content type
- ✅ Legacy code removed (~28 lines eliminated)
- ✅ New processor deployed as production default
- ✅ ITaggable interface implemented for unified tag processing
- ✅ Zero regression - all existing functionality preserved

---

### [✅] Phase 2: Books/Library Migration - COMPLETE
**Project**: Website Architecture Upgrade - Books with Review Blocks  
**Complexity**: Medium  
**Completed**: 2025-07-10 (2 days - all 4 phases)  
**Status**: ✅ Complete - Archived in `projects/archive/books-migration.md`

**Achievements**:
- ✅ 37 books migrated to AST-based processing with full metadata preservation
- ✅ RSS feed generation added (library/feed/index.xml) 
- ✅ Feature flag migration pattern proven for fourth content type
- ✅ Legacy loading-only state converted to full content generation
- ✅ New processor deployed as production default (no environment variables needed)
- ✅ Zero regression - all existing functionality preserved
- ✅ Review block insight validated: "books are reviews" architecture reuse successful

**Key Insight Applied**: Books are reviews, leveraging existing `:::review` block infrastructure instead of creating new custom blocks.

**Migration Strategy Success**:
- Book metadata mapped to existing review infrastructure
- Complete content preservation (title, author, rating, status, ISBN, cover)
- Library pages render with consistent formatting
- Microformats support inherited from review block architecture
- All 37 book files processed successfully with individual pages and RSS feed

---

### [✅] Phase 2: Posts Migration - COMPLETE
**Project**: Website Architecture Upgrade - Posts Processor  
**Complexity**: Large  
**Completed**: 2025-07-10 (1 day - all 4 phases)  
**Status**: ✅ Complete - Archived in `projects/archive/posts-migration.md`

**Achievements**:
- ✅ 81 posts migrated to AST-based processing with full metadata preservation
- ✅ RSS feed generation added (posts/feed/index.xml) with DNS redirect compatibility
- ✅ Feature flag migration pattern proven for fifth content type
- ✅ Legacy code cleanup (~35 lines eliminated from Builder.fs)
- ✅ New processor deployed as production default (no environment variables needed)
- ✅ Zero regression - all existing functionality preserved
- ✅ Largest content migration completed: main blog post processing fully modernized

**Migration Strategy Success**:
- 100% output compatibility validated across 90 generated files (81 posts + 9 archive pages)
- Legacy RSS feed preserved for DNS redirects while adding new AST-based RSS
- Complete content preservation (title, description, date, tags, rich content)
- Archive pages and pagination working correctly with new processor
- Posts join Books, Wiki, Snippets, and Presentations as fully migrated content types

---

### [✅] Phase 2: Notes Migration - COMPLETE
**Project**: Website Architecture Upgrade - Notes Processor  
**Complexity**: Medium  
**Completed**: 2025-07-12 (2 days - all 4 phases)  
**Status**: ✅ Complete - Archived in `projects/archive/notes-migration.md`

**Achievements**:
- ✅ 243 notes migrated to AST-based processing with full content preservation
- ✅ RSS feed generation added (feed/index.xml) with 38% performance improvement 
- ✅ Feature flag migration pattern proven for sixth content type
- ✅ Legacy code cleanup (~50 lines eliminated: loadFeed, buildFeedPage, buildFeedRssPage)
- ✅ New processor deployed as production default (no environment variables needed)
- ✅ Critical AST parsing bug discovered and fixed (affects all content types)
- ✅ Zero regression - all existing functionality preserved

**Critical Infrastructure Fix**:
- ASTParsing.fs updated to render HTML instead of storing raw markdown
- Fix affects all content types using AST infrastructure
- Ensures consistent markdown-to-HTML conversion across entire system

**Migration Strategy Success**:
- 100% content integrity maintained across all 243 notes  
- Notes index page and individual note pages working correctly
- RSS feed generation more efficient (280KB vs 442KB - 38% improvement)
- Notes join Books, Posts, Wiki, Snippets, and Presentations as fully migrated content types
- **6th Successful Content Migration**: Completes major content type unification

---

### [✅] Phase 2: Responses Migration - COMPLETE
**Project**: Website Architecture Upgrade - Responses Processor  
**Complexity**: Medium  
**Completed**: 2025-07-12 (1 day)  
**Status**: ✅ Complete - Archived in `projects/archive/responses-migration.md`

**Achievements**:
- ✅ Responses use new AST-based processor pattern
- ✅ IndieWeb h-entry microformat support preserved
- ✅ Response feeds generated automatically (HTML + RSS)
- ✅ Feature flag migration pattern proven for seventh content type
- ✅ Critical post-deployment fix: HTML index page generation
- ✅ Legacy code cleanup: 40+ lines eliminated
- ✅ Zero regression deployment with immediate production fixes

---

### [✅] Phase 2: Albums Migration (Final Content Type) - COMPLETE
**Project**: Website Architecture Upgrade - Albums Processor  
**Complexity**: Large  
**Completed**: 2025-01-13 (Media card consistency improvements)  
**Status**: ✅ Complete - Archived in `projects/archive/albums-migration-requirements.md`

**Achievements**:
- ✅ Albums use `:::media` blocks exclusively via albumPostView
- ✅ Advanced media metadata preserved with proper aspect ratios
- ✅ Album feeds generated automatically (RSS + HTML index)
- ✅ Unified card-based visual consistency across all content types
- ✅ IndieWeb h-entry microformat and webmention integration maintained
- ✅ Feature flag migration pattern applied successfully (8th content type)

**Migration Strategy Success**:
- Albums successfully integrated with GenericBuilder AST-based processing
- Visual consistency unified across all content types (notes, responses, albums)
- Individual album pages now use proper card layout with working permalinks
- **8th Successful Content Migration**: Completes all content type migrations

---

### [✅] Phase 3: Unified Feed System - COMPLETE
**Project**: Website Architecture Upgrade - Feed Consolidation  
**Complexity**: Large  
**Completed**: 2025-07-13 (1 day - all 4 phases)  
**Status**: ✅ Complete - Archived in `projects/archive/unified-feed-system.md`

**Achievements**:
- ✅ Single `buildMainFeeds` call generates all feeds
- ✅ Fire-hose feed includes all content types
- ✅ Type-specific feeds generated automatically  
- ✅ RSS and HTML feeds synchronized with 20-item optimization
- ✅ Feed generation performance improved (single-pass processing)
- ✅ All existing feed URLs preserved with zero breaking changes
- ✅ RSS 2.0 compliance validated across 8 content types

**Migration Strategy Success**:
- Unified feed processing eliminates code duplication
- 1129 items processed across 8 content types efficiently
- Feed throughput: ~65.6 items/sec with 17.2s total processing
- All legacy RSS functions removed and replaced with unified system

---

## High Priority (Critical Functionality Fixes)

### [ ] Phase 3.5: Presentation Rendering Fix
**Project**: Website Architecture Upgrade - Presentation Slideshow Restoration  
**Priority**: Critical  
**Complexity**: Medium  
**Estimated Effort**: 3-4 days  
**Dependencies**: Unified Feed System Complete ✅

**Problem**: Presentations are currently rendering as static markdown content instead of interactive reveal.js slideshows, causing significant functionality regression.

**Root Cause**: Current GenericBuilder presentation processing uses `convertMdToHtml` instead of reveal.js integration, treating slideshow content as regular markdown.

**Solution**: Implement `:::presentation` custom block system:
- Create new `:::presentation` custom block type
- Move YAML resources into presentation block metadata
- Integrate with existing reveal.js infrastructure
- Preserve slide separators (`---`) and reveal.js features
- Ensure proper slideshow navigation, themes, and interactivity

**Current vs Target**:
```markdown
<!-- Current (broken) -->
---
title: "Hello World!"
resources: [...]
---
## Slide 1
Content...
---
## Slide 2

<!-- Target (working) -->
---
title: "Hello World!"
---
:::presentation
resources:
  - text: "Personal Site"
    url: "https://luisquintanilla.me"
## Slide 1
Content...
---
## Slide 2
:::
```

**Success Criteria**:
- [ ] Presentations render as interactive reveal.js slideshows
- [ ] Slide navigation works (arrow keys, click navigation)
- [ ] Resources section displays properly
- [ ] All existing presentations work without content changes
- [ ] Integration with existing reveal.js themes and plugins
- [ ] Presentation index page shows proper slideshow previews

**Technical Requirements**:
- [ ] Create PresentationBlock.fs for `:::presentation` custom block
- [ ] Update GenericBuilder.PresentationProcessor to use custom blocks
- [ ] Integrate reveal.js rendering pipeline with AST processing
- [ ] Preserve existing reveal.js features (speaker notes, themes, etc.)
- [ ] Test with all 3 existing presentations

---

## Medium Priority (Code Cleanup & Optimization)

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
