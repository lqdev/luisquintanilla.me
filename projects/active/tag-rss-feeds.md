# Tag RSS Feeds Implementation

## Problem Statement

Currently, tags have HTML pages (`/tags/{tagname}/index.html`) but no RSS feeds. Users who want to follow content by specific tags cannot subscribe to RSS feeds for individual tags.

## Requirements

### Core Requirements
- Generate RSS feeds for all tags at `/tags/{tagname}/feed.xml`
- Include all content types in tag feeds (posts, notes, responses, snippets, wiki, presentations, reviews, media)
- Content should be sorted chronologically (newest first)
- Standard RSS 2.0 format with proper XML headers
- Consistent with existing feed architecture

### Content Types to Include
Based on the unified feed system, tag feeds should include:
- Posts
- Notes  
- Responses
- Snippets
- Wiki pages
- Presentations  
- Reviews (books)
- Media (photo albums)

## Technical Approach

### Integration with Unified Feed System
The tag RSS feeds will leverage the existing unified feed system in `GenericBuilder.fs` to:
1. Collect all tagged content from unified feed items
2. Filter by specific tag
3. Generate RSS feeds using existing RSS generation infrastructure

### Implementation Plan

#### Phase 1: Extend Builder.fs Tag Processing
- Modify `buildTagsPages` function to also generate RSS feeds
- Create tag-specific RSS feeds alongside HTML pages
- Use existing RSS generation patterns

#### Phase 2: Integration with Unified Feeds
- Leverage `GenericBuilder.UnifiedFeeds` module
- Filter unified feed items by tag
- Generate RSS feeds using existing feed infrastructure

#### Phase 3: Testing & Validation
- Verify RSS feeds validate against RSS 2.0 spec
- Test with various RSS readers
- Ensure proper content aggregation across content types

## Success Criteria

- [ ] RSS feeds generated for all existing tags
- [ ] Tag feeds include content from all 8 content types
- [ ] Feeds validate against RSS 2.0 specification
- [ ] Consistent feed metadata and structure
- [ ] Performance impact minimal (leverage existing processing)
- [ ] Tag feeds accessible at `/tags/{tagname}/feed.xml`

## File Locations

### Target Feed URLs
```
/tags/ai/feed.xml
/tags/fsharp/feed.xml  
/tags/dotnet/feed.xml
...etc for all tags
```

### Implementation Files
- `Builder.fs` - Extend `buildTagsPages` function
- `GenericBuilder.fs` - Leverage existing unified feed system
- No new modules required - use existing infrastructure

## Timeline

**Estimated**: 2-3 hours
- 1 hour: Implement tag RSS feed generation
- 1 hour: Testing and validation  
- 30 minutes: Documentation and completion

This leverages the existing unified feed system infrastructure, making it a straightforward extension rather than new development.
