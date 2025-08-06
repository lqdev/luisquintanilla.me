# Snippets Migration Project Plan

**Project**: Phase 2 Snippets Migration (Pilot Content Type)  
**Start Date**: 2025-01-08  
**Status**: Active  
**Dependencies**: ✅ Core Infrastructure, ✅ Feature Flag Infrastructure

## Project Overview

Migrate snippets from existing string-based processing to new AST-based infrastructure using feature flags for safe validation. This serves as the pilot migration to prove the approach before proceeding with other content types.

## Success Criteria

- ✅ Snippets use AST parsing instead of string manipulation
- ✅ Feature flag switches between old/new snippet processing (`NEW_SNIPPETS=true`)
- ✅ Output validation confirms identical results between old and new systems
- ✅ Snippets implement `ITaggable` interface for unified tag processing
- ✅ Custom blocks work correctly in snippet content

**Status**: ✅ **ALL CRITERIA MET** - Migration technically complete, awaiting deployment approval

## Implementation Phases

### Phase 1: Analysis and Current State Assessment
**Objective**: Understand existing snippet structure and processing

#### Tasks:
- [ ] Analyze existing snippet files in `_src/snippets/`
- [ ] Document current snippet metadata patterns
- [ ] Identify existing snippet processing functions in `Builder.fs`
- [ ] Map snippet processing flow and dependencies
- [ ] Test existing snippet build to establish baseline

**Deliverables**:
- Current state analysis documentation
- Snippet metadata standardization plan
- Existing vs new system comparison plan

### Phase 2: Processor Implementation
**Objective**: Create new snippet processor using AST infrastructure

#### Tasks:
- [ ] Create snippet processor function using `GenericBuilder.buildContentWithFeeds`
- [ ] Implement snippet-specific card renderer
- [ ] Implement snippet-specific RSS renderer
- [ ] Ensure snippets implement `ITaggable` interface
- [ ] Test new processor with sample snippet content

**Deliverables**:
- Snippet processor implementation
- Snippet card and RSS renderers
- Test scripts for new processor validation

### Phase 3: Integration and Feature Flag Setup
**Objective**: Integrate new processor with feature flag system

#### Tasks:
- [ ] Add snippet feature flag logic to `Program.fs`
- [ ] Implement conditional execution between old/new systems
- [ ] Create test scripts for feature flag validation
- [ ] Test build process with both flag states
- [ ] Validate zero impact when flag disabled

**Deliverables**:
- Feature flag integration in `Program.fs`
- Test scripts for flag switching
- Build validation for both states

### Phase 4: Output Validation and Testing
**Objective**: Ensure new system produces identical output

#### Tasks:
- [ ] Run output comparison between old and new systems
- [ ] Validate snippet HTML pages are identical
- [ ] Validate snippet RSS feeds are identical
- [ ] Test custom blocks in snippet content
- [ ] Create comprehensive snippet test content

**Deliverables**:
- Output validation results
- Test content files for snippets
- Validation report comparing old vs new

### Phase 5: Documentation and Completion
**Objective**: Document migration and prepare for next content type

#### Tasks:
- [ ] Update migration documentation with lessons learned
- [ ] Create snippet migration guide
- [ ] Update test scripts and validation tools
- [ ] Document any architecture improvements discovered
- [ ] Prepare recommendations for next content type migration

**Deliverables**:
- Updated migration documentation
- Snippet migration guide
- Architecture improvement recommendations
- Project completion summary

## Technical Implementation Details

### New Snippet Processor Pattern
```fsharp
let buildSnippetsNew() = 
    let snippetData = GenericBuilder.buildContentWithFeeds 
        createSnippetProcessor 
        renderSnippetCard 
        renderSnippetRssItem 
        "snippets" 
        (Path.Join(srcDir, "snippets"))
    snippetData
```

### Feature Flag Integration
```fsharp
// In Program.fs
if FeatureFlags.isEnabled ContentType.Snippets then
    printfn "Using NEW snippet processor (experimental)"
    let _ = buildSnippetsNew()
    ()
else
    buildSnippetPage snippets
    buildSnippetPages snippets
```

### ITaggable Implementation
Ensure snippets properly implement the `ITaggable` interface for unified tag processing across all content types.

## Dependencies and Prerequisites

### Completed Infrastructure ✅
- Core Infrastructure (AST parsing, custom blocks, generic builder)
- Feature Flag Infrastructure (environment variables, validation, progress tracking)
- Output validation and comparison tools

### Required for Success
- Access to existing snippet content for testing
- Ability to run both old and new systems in parallel
- Comprehensive test coverage for validation

## Risk Management

### Identified Risks
1. **Snippet Metadata Variations**: Different metadata formats could cause parsing issues
   - **Mitigation**: Comprehensive analysis phase first
2. **Custom Block Compatibility**: Existing custom blocks in snippets might behave differently
   - **Mitigation**: Test with real content, create fallback handling
3. **Feed Generation Differences**: RSS/HTML feeds might have subtle differences
   - **Mitigation**: Use output validation tools extensively

### Success Indicators
- Feature flag can switch between systems without errors
- Output validation shows identical results
- All existing snippet functionality preserved
- Custom blocks work correctly in snippet content
- Migration pattern proven for other content types

## Timeline Estimates

- **Phase 1**: 1-2 days (Analysis and assessment)
- **Phase 2**: 2-3 days (Processor implementation)
- **Phase 3**: 1 day (Integration and feature flags)
- **Phase 4**: 1-2 days (Validation and testing)
- **Phase 5**: 1 day (Documentation and completion)

**Total Estimated Duration**: 6-9 days (1 week with testing and documentation)

## Next Steps After Completion

Upon successful completion of snippets migration:
1. Document lessons learned and architecture improvements
2. Update migration pattern based on discoveries
3. Proceed with Wiki Content Migration (next in backlog)
4. Apply proven migration pattern to remaining content types

This pilot migration is critical for validating the entire Phase 2 migration approach and infrastructure.

**Current Status**: ✅ **VALIDATION COMPLETED - MIGRATION SUCCESSFUL**

## Phase 4: Output Validation ❌ FAILED

**Status**: FAILED - Output differences detected
**Start**: 2025-01-08  
**End**: 2025-01-08

### Validation Results
- ❌ **Output Comparison**: 12/13 files different
- ❌ **Migration Validation**: FAILED
- ⚠️ **Critical Issues**: ID attributes and code processing differences

### Issues Identified
1. **Header ID Attributes**: New system adds `id` attributes to headers
2. **Code Block Processing**: Different markdown/HTML processing 
3. **AST vs String**: Fundamental processing differences

### Blocker Status
**BLOCKED**: Cannot proceed until output compatibility achieved

## Phase 5: Issue Resolution ✅ **COMPLETE**

**Status**: ✅ Complete  
**Start**: 2025-01-08  
**End**: 2025-01-08

### Root Cause Analysis ✅
**Issue Identified**: Double markdown processing in new AST system
- **Old System**: Raw markdown → `convertMdToHtml` (single conversion)
- **New System**: Raw markdown → AST → HTML → `convertMdToHtml` (double conversion!)

**Location**: `ASTParsing.fs::parseDocumentFromAst` function using `extractTextContentFromAst` instead of raw markdown

### Fix Implementation ✅
**Change Applied**: Modified `ASTParsing.fs::parseDocumentFromAst`
```fsharp
// OLD (caused double processing):
TextContent = extractTextContentFromAst doc

// NEW (fixed):  
TextContent = contentWithoutFrontMatter  // Raw markdown
```

### Validation Results ✅
**Re-ran Output Comparison**: 100% SUCCESS
- **Total files**: 13 snippet files
- **✅ Matching**: 13 (100%)
- **❌ Different**: 0 (0%)  
- **Missing files**: 0

**Migration Status**: ✅ **VALIDATION PASSED** - Ready for production deployment

## Phase 6: Migration Completion (READY)

**Status**: Ready to proceed (awaiting approval)
**Prerequisites**: ✅ All validation passed

### Completion Tasks
- [ ] Switch default to new processor (`NEW_SNIPPETS=true` by default)
- [ ] Remove old snippet processing code
- [ ] Update documentation
- [ ] Archive project plan
- [ ] Update changelog with success
