# Retroactive Date Assignment Project

**Project**: Fix Date Handling for Content Without Explicit Dates  
**Complexity**: Medium-High  
**Started**: 2025-07-25  
**Status**: 🎯 Active  

## Problem Statement

Snippets and Wiki content are showing current date (2025-07-25) in the unified feed because they lack explicit date fields in their frontmatter. When RSS generation encounters missing `pubDate`, it defaults to `DateTime.Now`, causing chronological ordering issues and misleading feed timestamps.

**Root Cause**: 
- `SnippetProcessor.RenderRss` and `WikiProcessor.RenderRss` don't include `pubDate` elements
- `UnifiedFeeds.convertSnippetsToUnified` and `convertWikisToUnified` default to current date when `pubDate` is null
- Domain model: `Snippet.Date = ""` and `Wiki` doesn't implement `ITaggable` directly

## Requirements

### Core Functionality
- **Git History Integration**: Extract actual creation/modification dates from Git logs
- **Frontmatter Enhancement**: Add `created_date` and `last_updated_date` fields where missing
- **RSS Date Fixing**: Include proper `pubDate` elements in RSS generation
- **Retroactive Application**: Apply historical dates to existing content without manual editing

### Technical Requirements
- **Git Integration**: Use `git log --follow --format=%aI --reverse -- <file>` to get creation dates
- **Batch Processing**: Handle all snippets and wikis systematically
- **Frontmatter Preservation**: Maintain existing YAML structure while adding dates
- **RSS Compatibility**: Ensure proper RFC 2822 date formatting
- **Fallback Strategy**: Handle files not in Git history gracefully

### Success Criteria
- [ ] All snippets have proper creation dates in frontmatter
- [ ] All wikis have proper last_updated_date in frontmatter
- [ ] RSS feeds show historical dates instead of current date
- [ ] Unified feed chronological ordering reflects actual content age
- [ ] Solution handles both existing and future content automatically

## Implementation Plan

### Phase 1: Git History Analysis & Date Extraction
**Duration**: 2-3 hours

#### Research & Analysis
- Analyze Git history patterns for snippets and wikis
- Identify files with/without Git history
- Research F#/.NET Git integration options (LibGit2Sharp vs process execution)
- Create date extraction utility functions

#### Implementation Tasks
- Create `GitHistoryService` module for date extraction
- Implement `getFileCreationDate` function using Git log
- Implement `getFileLastModificationDate` function
- Add error handling for files not in Git history
- Test with sample snippet and wiki files

### Phase 2: Frontmatter Enhancement System  
**Duration**: 2-3 hours

#### Frontmatter Processing
- Create `FrontMatterEnhancer` module for YAML manipulation
- Implement `addMissingDates` function for snippets (created_date)
- Implement `addMissingDates` function for wikis (last_updated_date)  
- Preserve existing frontmatter structure and formatting
- Handle edge cases (files without frontmatter, malformed YAML)

#### Batch Processing
- Create `enhanceAllSnippets` function for batch processing
- Create `enhanceAllWikis` function for batch processing
- Add dry-run capability for testing
- Generate summary reports of changes made

### Phase 3: RSS Generation Updates
**Duration**: 1-2 hours

#### Snippet RSS Enhancement
- Update `SnippetProcessor.RenderRss` to include `pubDate` from `created_date`
- Handle date parsing and RFC 2822 formatting
- Add fallback logic for missing dates

#### Wiki RSS Enhancement  
- Update `WikiProcessor.RenderRss` to include `pubDate` from `last_updated_date`
- Ensure consistent date formatting across processors
- Test RSS feed validation

### Phase 4: Domain Model & Interface Updates
**Duration**: 1 hour

#### Domain Updates
- Add `CreatedDate` field to `SnippetDetails` type
- Ensure `Wiki` properly implements date handling
- Update `ITaggable` implementations as needed
- Maintain backward compatibility

#### Integration Testing
- Test unified feed date ordering with enhanced content
- Verify RSS feed compliance and parsing
- Test build process end-to-end

## Technical Architecture

### Git Integration Strategy
```fsharp
module GitHistoryService =
    let getFileCreationDate (filePath: string) : DateTime option
    let getFileLastModificationDate (filePath: string) : DateTime option
    let formatDateForFrontmatter (date: DateTime) : string
```

### Frontmatter Enhancement Strategy
```fsharp
module FrontMatterEnhancer =
    let addCreatedDateToSnippet (filePath: string) (createdDate: DateTime) : unit
    let addLastUpdatedDateToWiki (filePath: string) (lastUpdatedDate: DateTime) : unit
    let enhanceAllSnippets (snippetDir: string) : unit
    let enhanceAllWikis (wikiDir: string) : unit
```

### RSS Enhancement Strategy
- Add `pubDate` elements to both SnippetProcessor and WikiProcessor
- Use consistent date formatting: `ddd, dd MMM yyyy HH:mm:ss zzz`
- Handle timezone conversion appropriately

## Risk Assessment & Mitigation

### **HIGH RISK**: Frontmatter Corruption
- **Mitigation**: Create backup copies before modification
- **Validation**: Test YAML parsing after each change
- **Recovery**: Git history provides rollback capability

### **MEDIUM RISK**: Date Accuracy
- **Mitigation**: Git log provides creation dates, but first commit may not be actual creation
- **Validation**: Manual spot-checking of questionable dates
- **Fallback**: Use file system dates as last resort

### **LOW RISK**: Build Process Impact
- **Mitigation**: Phase implementation with continuous testing
- **Validation**: Ensure RSS feeds remain valid throughout
- **Rollback**: Feature flag pattern for RSS date inclusion

## Timeline & Milestones

**Total Estimated Duration**: 6-8 hours across 2-3 sessions

**Session 1** (2-3 hours):
- [ ] Git history analysis and extraction system
- [ ] Test date extraction on sample files
- [ ] Create frontmatter enhancement foundation

**Session 2** (2-3 hours):
- [ ] Batch processing implementation
- [ ] RSS generation updates
- [ ] Integration testing

**Session 3** (1-2 hours):
- [ ] Domain model updates
- [ ] End-to-end validation
- [ ] Production deployment and verification

## Success Metrics

### Quantitative Metrics
- **Content Coverage**: 100% of snippets and wikis have proper dates
- **RSS Compliance**: All RSS feeds validate with proper pubDate elements
- **Chronological Accuracy**: Feed ordering reflects historical content creation

### Qualitative Metrics  
- **User Experience**: Unified feed shows logical chronological progression
- **Maintainability**: Future content automatically includes proper dates
- **Reliability**: No more default "current date" fallbacks in feeds

This project addresses the immediate date issue while establishing infrastructure for proper date handling in future content creation.
