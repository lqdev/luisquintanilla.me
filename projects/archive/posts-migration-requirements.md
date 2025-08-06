# Posts Migration Requirements

**Project**: Website Architecture Upgrade - Posts Processor  
**Status**: Active  
**Created**: 2025-07-10  
**Content Type**: Articles (`post_type: "article"`)

## Problem Statement

Posts (articles) are currently processed using legacy string manipulation methods instead of the modern AST-based processing system. This migration will convert 70+ blog posts to use the new unified content processing architecture, enabling custom block support and RSS feed generation while maintaining 100% output compatibility.

## Context

Posts represent the largest and most complex content type in the system:
- **Content Location**: `_src/posts/*.md` (70+ files)
- **Post Type**: `post_type: "article"` (long-form blog posts)
- **Current Processing**: Legacy string manipulation via `convertMdToHtml`
- **Custom Blocks**: None currently used (all posts are plain markdown)
- **Output**: Individual post pages + `/posts/` index + RSS feeds

**Migration Scope**:
- Convert from legacy markdown processing to AST-based system
- Enable custom block support for future enhancement
- Add RSS feed generation capability
- Preserve all existing functionality and metadata
- Maintain identical HTML output

## Success Criteria

### Functional Requirements
- [ ] All 70+ posts render identically to current output
- [ ] Posts index page (`/posts/`) functions correctly
- [ ] RSS feed generation works for posts (`/posts/feed/index.xml`)
- [ ] Post metadata (title, description, date, tags) preserved
- [ ] Post URLs and permalinks unchanged
- [ ] Tag system integration works correctly
- [ ] Home page recent posts display functions correctly

### Technical Requirements
- [ ] Posts use AST-based processing via `GenericBuilder`
- [ ] Custom block infrastructure available (even if unused)
- [ ] Feature flag pattern implemented (`NEW_POSTS` environment variable)
- [ ] Output validation confirms 100% compatibility
- [ ] Legacy code removal after successful migration
- [ ] Build performance maintained or improved

### Quality Requirements
- [ ] No regression in existing functionality
- [ ] Comprehensive test coverage with validation scripts
- [ ] Complete documentation of migration process
- [ ] Clean code with consistent patterns from previous migrations

## Technical Approach

### Architecture Pattern
Following the proven 4-phase migration pattern from Books, Wiki, Snippets, and Presentations:

**Phase 1**: Domain Enhancement
- Enhance `Post` type with any missing interfaces
- Implement validation helpers
- Create test scripts for posts validation

**Phase 2**: Processor Implementation
- Implement `PostProcessor` in `GenericBuilder.fs`
- Add `buildPosts()` function to `Builder.fs`
- Integrate `NEW_POSTS` feature flag in `Program.fs`
- Validate with comprehensive testing

**Phase 3**: Migration Validation
- Create output comparison scripts
- Validate RSS feed generation
- Test feature flag on/off behavior
- Confirm zero regression

**Phase 4**: Production Deployment
- Deploy new processor as default (remove feature flag dependency)
- Remove legacy functions systematically
- Clean up unused imports and dependencies
- Archive project documentation

### Implementation Details

**Post Processing Flow**:
```fsharp
// Current: Direct markdown conversion
post.Content |> convertMdToHtml

// Target: AST-based processing
post |> ASTParsing.parseMarkdown |> GenericBuilder.PostProcessor.renderContent
```

**RSS Feed Integration**:
- Posts RSS feed: `/posts/feed/index.xml`
- Channel metadata from post collection
- Individual post items with full content

**Feature Flag Logic**:
```fsharp
if FeatureFlags.NEW_POSTS then
    buildPosts posts // New AST-based system
else
    buildPostsLegacy posts // Current system
```

## Constraints & Considerations

### Technical Constraints
- Must maintain existing URL structure
- Cannot break existing RSS feed consumers
- Build time should not significantly increase
- No changes to post source files required

### Migration Constraints
- Feature flag must allow safe rollback
- Both systems must produce identical output during transition
- No downtime during migration
- Legacy system must remain functional until cutover

### Content Considerations
- 70+ posts varying in length and complexity
- Diverse metadata patterns across years of content
- Some posts may have complex markdown structures
- Tag system integration critical for site navigation

## Timeline & Milestones

**Estimated Duration**: 1-2 days  
**Dependencies**: Books Migration Success ✅

### Phase 1: Domain Enhancement (Day 1)
- [ ] Analyze current `Post` type and enhance if needed
- [ ] Create comprehensive test scripts
- [ ] Validate AST parsing works for all posts

### Phase 2: Processor Implementation (Day 1)
- [ ] Implement `PostProcessor` with custom block support
- [ ] Add posts build orchestration
- [ ] Integrate feature flag logic
- [ ] Test build output and RSS generation

### Phase 3: Migration Validation (Day 1-2)
- [ ] Output comparison testing (HTML + RSS)
- [ ] Feature flag safety validation
- [ ] Performance and regression testing
- [ ] Stakeholder validation

### Phase 4: Production Deployment (Day 2)
- [ ] Remove feature flag dependency
- [ ] Clean up legacy code
- [ ] Archive project files
- [ ] Update documentation

## Risks & Mitigation

### High Risk
- **Large content volume**: 70+ posts to validate
  - *Mitigation*: Automated validation scripts and sampling

### Medium Risk
- **Complex metadata patterns**: Years of varied frontmatter
  - *Mitigation*: Comprehensive metadata analysis first

### Low Risk
- **RSS feed compatibility**: Feed consumer impact
  - *Mitigation*: Feature flag allows immediate rollback

## Dependencies

### Completed Dependencies ✅
- Core Infrastructure Implementation ✅
- Feature Flag Infrastructure ✅  
- Books Migration (architecture validation) ✅

### Blocking Dependencies
- None

## Acceptance Criteria

**Migration Complete When**:
- [ ] All 70+ posts render identically via new system
- [ ] Posts RSS feed generates correctly
- [ ] Feature flag removed and new system is default
- [ ] Legacy code completely removed
- [ ] Zero regression in any existing functionality
- [ ] Project files archived and backlog updated

**Quality Gates**:
- [ ] 100% output validation passes
- [ ] Build performance maintained
- [ ] No compile errors or warnings
- [ ] Documentation complete and accurate

---

*This requirements document follows the proven migration pattern from Books, Wiki, Snippets, and Presentations migrations.*
