# Posts Migration Project Plan

**Project**: Website Architecture Upgrade - Posts Processor  
**Created**: 2025-07-10  
**Status**: Active  
**Content Type**: Articles (`post_type: "article"`)

## Project Overview

Migrate 70+ blog posts from legacy string manipulation to AST-based processing following the proven 4-phase migration pattern. This is the largest content migration in the project, representing the main blog content of the site.

## Implementation Strategy

### Content Analysis
- **Location**: `_src/posts/*.md` (70+ files)
- **Type**: Long-form articles (`post_type: "article"`)
- **Current State**: Legacy `convertMdToHtml` processing
- **Target State**: AST-based processing with custom block support
- **Complexity**: Large volume, varied metadata patterns, no custom blocks

### Migration Pattern Application

Following the proven pattern from Books, Wiki, Snippets, Presentations:

1. **Domain Enhancement** → Enhance `Post` type, add validation
2. **Processor Implementation** → Create `PostProcessor`, integrate feature flag
3. **Migration Validation** → Output comparison, RSS validation
4. **Production Deployment** → Default new system, remove legacy code

## Phase Breakdown

### Phase 1: Domain Enhancement ✅ COMPLETE
**Status**: ✅ Complete (2025-07-10)  
**Objective**: Prepare foundation for AST-based post processing

**Tasks**:
- ✅ Analyze current `Post` type in `Domain.fs`
- ✅ Implement `ITaggable` interface for unified tag processing
- ✅ Validate compilation with enhanced domain
- ✅ Confirm Post follows pattern from Books/Wiki/Snippets/Presentations

**Achievements**:
```fsharp
type Post = { FileName: string; Metadata: PostDetails; Content: string }
with
    interface ITaggable with
        member this.Tags = if isNull this.Metadata.Tags then [||] else this.Metadata.Tags
        member this.Title = this.Metadata.Title
        member this.Date = this.Metadata.Date  
        member this.FileName = this.FileName
        member this.ContentType = "post"
```

**Success Criteria**:
- ✅ `Post` type ready for new processor
- ✅ ITaggable interface implemented correctly
- ✅ Compilation succeeds with domain enhancement

### Phase 2: Processor Implementation  
**Objective**: Implement new post processor with feature flag safety

**Tasks**:
- [ ] Implement `PostProcessor` in `GenericBuilder.fs`
- [ ] Add `buildPosts()` function to `Builder.fs`
- [ ] Integrate `NEW_POSTS` feature flag in `Program.fs`
- [ ] Test build output (posts pages, index, RSS)
- [ ] Validate feature flag switching behavior

**Success Criteria**:
- [ ] New post processor generates identical output
- [ ] Posts RSS feed created correctly
- [ ] Feature flag enables safe switching
- [ ] Build performance maintained

### Phase 3: Migration Validation
**Objective**: Comprehensive validation of migration correctness

**Tasks**:
- [ ] Create output comparison scripts for all 70+ posts
- [ ] Validate RSS feed structure and content
- [ ] Test feature flag on/off behavior
- [ ] Performance testing and regression validation
- [ ] Directory structure and URL validation

**Success Criteria**:
- [ ] 100% output compatibility confirmed
- [ ] RSS feeds valid and complete
- [ ] Zero regression in existing functionality
- [ ] Feature flag safety validated

### Phase 4: Production Deployment
**Objective**: Deploy new system and clean up legacy code

**Tasks**:
- [ ] Remove feature flag dependency from `Program.fs`
- [ ] Update `FeatureFlags.fs` to default Posts to true
- [ ] Remove legacy post processing functions
- [ ] Clean up unused imports and dependencies
- [ ] Archive project files and update documentation

**Success Criteria**:
- [ ] New system running as production default
- [ ] Legacy code completely removed
- [ ] Codebase simplified and clean
- [ ] Project properly archived

## Technical Implementation

### Core Components

**PostProcessor Structure**:
```fsharp
module PostProcessor =
    let processPost (post: Post) =
        // AST-based content processing
        let ast = ASTParsing.parseMarkdown post.Content
        let renderedContent = renderWithCustomBlocks ast
        { post with Content = renderedContent }
    
    let renderPostPage (post: Post) =
        // Generate individual post page
        
    let renderPostIndex (posts: Post array) =
        // Generate posts index page
        
    let renderPostRss (posts: Post array) =
        // Generate posts RSS feed
```

**Build Integration**:
```fsharp
// In Builder.fs
let buildPosts (posts: Post array) =
    if FeatureFlags.NEW_POSTS then
        buildContentWithFeeds 
            posts 
            PostProcessor.processPost
            PostProcessor.renderPostPage
            PostProcessor.renderPostIndex
            PostProcessor.renderPostRss
            "posts"
            "Posts"
    else
        buildPostsLegacy posts
```

### RSS Feed Specification
- **Location**: `/posts/feed/index.xml`
- **Channel**: Main blog feed with post metadata
- **Items**: Full post content with proper escaping
- **Format**: RSS 2.0 with proper XML declaration

### Feature Flag Integration
```fsharp
// Environment variable control
NEW_POSTS=true|false

// Program.fs logic
let posts = loadPosts srcDir
if FeatureFlags.NEW_POSTS then
    buildPosts posts
else
    buildPostsLegacy posts
```

## Validation Strategy

### Output Comparison
- **HTML Pages**: Compare all 70+ individual post pages
- **Index Page**: Validate posts listing and navigation
- **RSS Feed**: Structure, content, and XML validity
- **URL Structure**: Ensure no permalink changes

### Test Scripts
- `test-posts-validation.fsx` - Core functionality validation
- `test-posts-output-comparison.fsx` - Old vs new comparison
- `test-posts-rss-validation.fsx` - RSS feed validation
- `test-posts-feature-flag.fsx` - Feature flag behavior

### Performance Metrics
- Build time comparison
- Memory usage analysis
- Output file size verification

## Risk Management

### High Priority Risks
1. **Volume Impact**: 70+ posts may reveal edge cases
   - *Mitigation*: Incremental testing, comprehensive validation scripts

2. **Metadata Complexity**: Years of varied frontmatter patterns
   - *Mitigation*: Thorough metadata analysis before implementation

### Medium Priority Risks
1. **RSS Compatibility**: Existing feed consumers
   - *Mitigation*: Feature flag allows immediate rollback

2. **Build Performance**: Large content volume
   - *Mitigation*: Performance monitoring and optimization

## Success Metrics

### Quantitative Goals
- [ ] 100% output compatibility (70+ posts)
- [ ] RSS feed generation successful
- [ ] Build time maintained or improved
- [ ] Zero regression test failures

### Qualitative Goals
- [ ] Code consistency with migration pattern
- [ ] Clean legacy code removal
- [ ] Comprehensive documentation
- [ ] Architecture pattern validation

## Dependencies & Prerequisites

### Completed ✅
- Core Infrastructure ✅
- Feature Flag Infrastructure ✅
- Books Migration (pattern validation) ✅

### Required
- Active development environment
- Test validation infrastructure
- Feature flag system operational

## Project Completion

### Definition of Done
- [ ] All posts migrate to AST-based processing
- [ ] Posts RSS feed operational
- [ ] Feature flag removed (new system default)
- [ ] Legacy code completely removed
- [ ] Project archived with comprehensive documentation

### Transition Criteria
- [ ] 100% output validation passed
- [ ] Stakeholder approval for production deployment
- [ ] Performance benchmarks met
- [ ] No blocking issues identified

---

**Next Phase**: After Posts completion, proceed to Responses migration per backlog priority order.

*This plan follows the proven migration workflow from Books, Wiki, Snippets, and Presentations.*
