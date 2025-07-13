# Unified Feed System Requirements

## Overview

**Feature Name**: Unified Feed System  
**Priority**: High  
**Complexity**: Large  
**Estimated Effort**: 1-2 weeks  

## Problem Statement

### What Problem Are We Solving?
Currently, the website has multiple scattered feed generation functions that are inefficient and hard to maintain:
- Individual feed functions (`buildFeedPage`, `buildFeedRssPage`, etc.) for each content type
- Duplicate logic for RSS and HTML feed generation
- Multiple passes through content data for different feed types
- Inconsistent feed structures and metadata across content types
- Performance impact from repeated content processing

### Who Is This For?
- **Site visitors**: Better feed performance and consistency
- **Content creators**: Unified feed management and better discoverability
- **Developers**: Simplified, maintainable feed architecture
- **RSS readers**: More reliable and standardized RSS feeds

### Why Now?
All 8 content types have been successfully migrated to AST-based GenericBuilder processing. This creates the foundation needed to implement a unified feed system that can process all content types in a single pass, eliminating the current scattered approach.

## Success Criteria

### Must Have (Core Requirements)
- [x] Single `buildMainFeeds(allData)` function generates all feeds
- [x] Fire-hose feed includes all content types chronologically
- [x] Type-specific feeds generated automatically (posts, notes, responses, etc.)
- [x] RSS and HTML feeds stay synchronized with identical content
- [x] All existing feed URLs continue to work (no breaking changes)
- [x] Feed generation performance improved (single pass vs multiple passes)
- [x] IndieWeb h-feed microformat compliance maintained

### Should Have (Important Features)
- [x] Consistent feed metadata across all content types
- [x] Configurable feed limits (number of items per feed)
- [x] Feed directory structure automatically created
- [x] Error handling for missing content or malformed data
- [x] Validation tools for feed integrity

### Could Have (Nice to Have)
- [ ] Feed analytics and metrics
- [ ] Advanced filtering options (by tags, date ranges)
- [ ] Custom feed templates
- [ ] Feed caching mechanisms

### Won't Have (Explicitly Out of Scope)
- [ ] Major changes to existing feed URLs or structure
- [ ] New feed formats beyond RSS and HTML
- [ ] Real-time feed updates or dynamic content

## User Stories

### Primary User Flow
**As a** site visitor  
**I want** to access consistent, up-to-date feeds for all content types  
**So that** I can follow the site's content through RSS readers or direct browsing

### Developer Experience
**As a** developer maintaining the site  
**I want** a single, unified feed generation system  
**So that** I can easily maintain and extend feed functionality without duplicating logic

### Content Discovery
**As a** content consumer  
**I want** a comprehensive fire-hose feed that includes all content types  
**So that** I don't miss any updates regardless of content type

## Current State Analysis

### Existing Feed Functions (To Be Replaced)
Based on successful migrations, these individual functions exist:
- Posts: RSS and HTML feeds
- Notes: RSS and HTML feeds  
- Responses: RSS and HTML feeds
- Snippets: RSS feeds
- Wiki: RSS feeds
- Presentations: RSS feeds
- Books/Library: RSS feeds
- Albums: RSS feeds

### Target Architecture
```
buildMainFeeds(allData) ->
  ├── Fire-hose feed (all content types)
  ├── Type-specific feeds (posts, notes, responses, etc.)
  ├── RSS generation (unified)
  └── HTML generation (unified)
```

## Implementation Phases

### Phase 1: Architecture Research
- [ ] Research F# feed processing patterns using Microsoft docs
- [ ] Analyze existing feed generation logic across content types
- [ ] Design unified data structure for feed items
- [ ] Plan feed generation pipeline architecture

### Phase 2: Core Implementation ✅
- [x] Create unified feed data types
- [x] Implement `buildMainFeeds` function
- [x] Create fire-hose feed generation
- [x] Implement type-specific feed filtering

### Phase 3: Integration & Testing ✅
- [x] Replace individual feed calls with unified system
- [x] Validate output compatibility with existing feeds
- [x] Performance testing and optimization
- [x] RSS validation and HTML structure verification

### Phase 4: Deployment & Cleanup ✅
- [x] Deploy unified system with feature flags
- [x] Remove legacy feed functions
- [x] Update documentation and architecture notes
- [x] Performance metrics and validation

## Technical Constraints

- Must maintain IndieWeb h-feed microformat standards
- All existing feed URLs must continue working
- RSS feeds must validate against RSS 2.0 specification
- No breaking changes to feed structure or content
- Must integrate with existing GenericBuilder AST infrastructure

## Dependencies

- ✅ All 8 content types migrated to GenericBuilder
- ✅ AST-based processing infrastructure in place
- ✅ Consistent content type interfaces (ITaggable, etc.)
- ✅ Feature flag infrastructure for safe deployment

## Validation Criteria

- [x] All existing feeds continue to work
- [x] Fire-hose feed contains items from all content types
- [x] RSS feeds validate with online RSS validators
- [x] HTML feeds maintain proper h-feed microformat structure
- [x] Performance improvement measurable (reduced build time)
- [x] Zero content regression across all feed types
