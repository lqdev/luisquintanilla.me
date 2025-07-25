# Unified Feed HTML Page Project

**Project**: Create `/feed/index.html` Page  
**Complexity**: Medium  
**Started**: 2025-07-25  
**Status**: 🎯 Active  

## Problem Statement

Currently, `/feed/` only contains RSS feeds but no `index.html` page. Users visiting `/feed/` get a directory listing instead of a unified content feed. This creates a gap in user experience for discovering content.

## Requirements

### Core Functionality
- Create `/feed/index.html` that displays all content types in card layout
- Use existing card layout patterns from notes and responses
- Include all 8 content types: posts, notes, responses, snippets, wiki, presentations, reviews, media
- Sort chronologically (newest first) 
- Limit to reasonable number of items (20-30 for performance)

### Technical Requirements
- Leverage existing unified feed system in `GenericBuilder.UnifiedFeeds`
- Reuse existing card view patterns from `ContentViews.fs`
- Use `feedView` pattern from `CollectionViews.fs` as base
- Follow existing page generation patterns in `Builder.fs`
- Use `defaultindex` layout for consistency

### Success Criteria
- [x] `/feed/index.html` page exists and displays content
- [x] All content types represented in unified timeline
- [x] Cards use consistent layout with existing patterns
- [x] Page loads quickly with reasonable content limit
- [x] Clicking cards navigates to individual content pages
- [x] Visual consistency with notes and responses card layouts

## ✅ COMPLETED - Implementation Complete

### What Was Built
1. **Unified Feed Page**: Successfully created `/feed/index.html` displaying all content types
2. **Card Layout**: Implemented unified card view using existing patterns from notes/responses
3. **Content Type Badges**: Added visual indicators for each content type (Posts, Notes, Responses, etc.)
4. **Proper Permalinks**: Fixed URL mapping for all 8 content types with correct paths
5. **Clean Content Rendering**: Resolved CDATA display issues for proper HTML rendering

### Technical Achievements  
- Added `unifiedFeedView` function to `CollectionViews.fs`
- Added `buildUnifiedFeedPage` function to `Builder.fs`
- Integrated with existing unified feed infrastructure
- Proper content cleaning and permalink generation
- 30 items displayed chronologically (newest first)

### Issues Resolved
- ✅ **CDATA Rendering Bug**: Stripped `<![CDATA[...]]>` markup from displaying in content
- ✅ **Permalink Mapping**: Fixed incorrect URL paths using proper content type mapping
- ✅ **Content Type Display**: Added badges to distinguish different content types
- ✅ **RSS Content Cleaning**: Removed "See original post at..." prefixes from RSS descriptions

### Final Result
- **URL**: `/feed/index.html` now exists and displays unified content feed
- **Performance**: Limited to 30 most recent items across all content types
- **Visual Design**: Consistent card layout matching existing site patterns
- **Content Types**: All 8 types included (posts, notes, responses, snippets, wiki, presentations, reviews, media)
- **Navigation**: Proper permalinks to individual content pages

## Implementation Plan

### Phase 1: Create Unified Card View Function
- Add `unifiedFeedView` function to `CollectionViews.fs`
- Take array of `UnifiedFeedItem` and render as cards
- Use existing card patterns as foundation

### Phase 2: Add Content-Type-Specific Card Rendering  
- Create dispatcher function to render cards based on content type
- Reuse existing `feedPostView`, `notePostView`, `responsePostView` patterns
- Handle all 8 content types with appropriate card layouts

### Phase 3: Build Function Implementation
- Add `buildUnifiedFeedPage` function to `Builder.fs`  
- Leverage existing `allUnifiedItems` from Program.fs
- Generate page at `/feed/index.html`
- Use standard page generation pattern

### Phase 4: Integration & Testing
- Integrate with main build process in `Program.fs`
- Test with representative content from all types
- Validate card rendering and navigation
- Performance testing with current content volume

## Technical Notes

### Existing Infrastructure to Leverage
- `GenericBuilder.UnifiedFeeds.UnifiedFeedItem` type
- `allUnifiedItems` data structure from Program.fs
- Card view patterns from `ContentViews.fs`
- `feedView` pattern from `CollectionViews.fs`
- Page generation patterns from `Builder.fs`

### Architecture Decisions
- **Single Page Approach**: Generate one `/feed/index.html` instead of pagination
- **Content Limit**: Limit to 20-30 items for performance
- **Card Layout Reuse**: Leverage existing card patterns for consistency
- **Type-Agnostic Display**: Unified timeline regardless of content type

## Timeline

**Estimated**: 2-3 hours
- 1 hour: Create unified card view and content-type dispatcher
- 1 hour: Implement build function and integration  
- 30-60 minutes: Testing, validation, and refinement

This leverages the completed unified feed infrastructure and existing card layout patterns, making it primarily an integration task rather than new architecture.
