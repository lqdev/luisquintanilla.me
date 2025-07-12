# Copilot Development Partner Instructions

## Purpose & Partnership

As your coding partner, I follow this systematic workflow to deliver high-quality, documented development that preserves functionality while improving architecture. This guide captures proven methodologies from successful migrations (Snippets, Wiki, Presentations) and establishes clear patterns for future work.

**Partnership Principles**: Work out loud, preserve functionality, validate continuously, document comprehensively, and maintain clean project state.

## 🔍 Research & Documentation Tools

### MCP Server Integration
When working on projects, use these specialized tools based on task context:

#### **Library & Framework Research**
- **Context7**: Use `mcp_context7_resolve-library-id` to find libraries, then `mcp_context7_get-library-docs` for current documentation
- **Microsoft Documentation**: Use `mcp_microsoft_doc_microsoft_docs_search` for F#/.NET best practices, Azure guidance, and official patterns

#### **Architecture & Pattern Research**  
- **DeepWiki**: Use `mcp_deepwiki_ask_question` to analyze successful F# projects and learn from similar implementations
- **Perplexity Research**: Use `mcp_perplexity-as_perplexity_research` for comprehensive technology comparisons and architectural analysis
- **Perplexity Reasoning**: Use `mcp_perplexity-as_perplexity_reason` for complex architectural trade-off analysis

### Context-Aware Tool Selection
Automatically select appropriate tools based on current project phase:

**Project Planning Phase** (when `projects/active/` contains new plans):
- Use research tools (Perplexity, DeepWiki) for architectural decisions
- Use Microsoft docs for F#/.NET best practices validation
- Use Context7 for library evaluation and documentation

**Active Development Phase** (when implementing features):
- Use Context7 for immediate library documentation needs
- Use Microsoft docs for implementation guidance
- Focus on code-level tools (codebase, search, usages)

**Migration Phase** (when converting legacy systems):
- Use Microsoft docs for migration best practices
- Use DeepWiki to research successful migration patterns
- Use validation tools for output comparison

**Architecture Review Phase** (when analyzing design):
- Use Perplexity reasoning for architectural trade-offs
- Use DeepWiki for pattern analysis from similar projects
- Use Context7 for library architecture understanding

## 🚀 Essential Workflow

### Project Lifecycle
1. **Start**: Clean active state → Create project plan → Begin phase logging
2. **Develop**: One change at a time → Test continuously → Document decisions
3. **Complete**: Comprehensive testing → Archive project → Update changelog → Clean up

### Critical Rules
- **Active Directory**: Only current work in `projects/active/`
- **Phase Logs**: Create, use, summarize in changelog, then delete immediately
- **Testing**: Build and test after each significant change
- **Documentation**: Capture all decisions and discoveries for future reference

## 📋 Project Management

### State Management
| State | Symbol | Location | Action |
|-------|--------|----------|---------|
| **Backlog** | `[ ]` | `projects/backlog.md` | Planning and priorities |
| **Active** | `[>]` | `projects/active/[name].md` | Current work only |
| **Complete** | ✅ | `projects/archive/[name].md` | Archived with cleanup |

### Documentation Flow
1. **Backlog** → Strategic planning
2. **Active Plans** → Current scope and progress
3. **Phase Logs** → Implementation details (temporary)
4. **Changelog** → Permanent achievement record
5. **Archive** → Completed project history

## ⚙️ Development Process

### 1. Project Initiation
- **Analyze Current State**: Large file reads, identify patterns/dependencies
- **Research Context**: Use MCP tools to research best practices and similar implementations
  - Query DeepWiki for similar F# projects and their architectural decisions
  - Use Perplexity to research technology trade-offs and industry patterns
  - Check Microsoft docs for official F#/.NET guidance
- **Create Implementation Plan**: Break into phases, set success criteria with research backing
- **Begin Documentation**: Phase-specific logs with clear objectives and research references

### 2. Active Development
- **One Change at a Time**: Small, testable changes with documentation
- **Continuous Validation**: Build/test after each change, fix issues immediately
- **Real-time Guidance**: Use Context7 for immediate library documentation, Microsoft docs for best practices
- **Document Everything**: Decisions, discoveries, and solutions for future reference

### 3. Project Completion
- **Comprehensive Testing**: All functionality + regression testing
- **Research Validation**: Verify final architecture against industry best practices using research tools
- **Document Learnings**: Update `.github/copilot-instructions.md` with proven patterns in appropriate sections
- **Archive Completely**: Move ALL project files to archive, clean active directory
- **Update Records**: Changelog entry, backlog cleanup, delete temporary logs

## 🔧 Technical Standards

### Code Quality
- **Type-First Design**: Define types before functions, use them to drive API design
- **Module Responsibility**: Each module handles one concern (parsing, rendering, generation)
- **Function Sizing**: Consider refactoring if functions exceed 20 lines
- **Centralized Entry Points**: Single functions that handle complete workflows

### F#-Specific Best Practices
- **Type Qualification**: Always use fully qualified types (`MediaType.Unknown` not `Unknown`)
- **Continuous Compilation**: Test build after each significant change
- **Module Dependencies**: Add new modules to project file immediately
- **Type Annotations**: Use explicit type annotations in test scripts to prevent inference issues
- **Research-Backed Decisions**: Use Microsoft docs to validate F# patterns and idioms

### Research-Driven Architecture
- **Pattern Validation**: Before implementing new patterns, research similar approaches using DeepWiki
- **Technology Evaluation**: Use Perplexity research for comprehensive technology comparisons
- **Best Practice Verification**: Cross-reference decisions with Microsoft documentation
- **Industry Context**: Consider broader industry trends and proven patterns

### Implementation Pattern (Proven for Snippets, Wiki, Presentations)
1. **Enhance Domain** → Add types and interfaces (research similar domain models)
2. **Implement Processor** → Create new AST-based processor (validate patterns with Microsoft docs)
3. **Replace Usage** → Update calling code with feature flags (research migration strategies)
4. **Remove Legacy** → Clean up old functions and dependencies (document learnings)

## 📝 Documentation Standards

### Phase Log Structure
```markdown
# Development Log - [Date] - [Project-Phase]

## Session Objectives
[What you plan to accomplish this session]

## Current State Analysis
[Detailed analysis of existing code/situation]

## Implementation Steps
### Step N: [Description]
[What you're doing and why]

### Step N Complete: [Achievement]
[What was accomplished, issues fixed, metrics]

## Session Summary
[Analysis of achievements and next steps]
```

### Project Plan Template
Use `projects/templates/requirements-template.md` including:
- Problem statement and context
- Success criteria and objectives  
- Technical approach and constraints
- Implementation timeline and milestones

### Changelog Entry Format
```markdown
## YYYY-MM-DD - [Project Name] ✅

**Project**: [Link to archived project plan]  
**Duration**: [Start] - [End]  
**Status**: Complete

### What Changed
[High-level description]

### Technical Improvements  
[Bullet points of achievements]

### Architecture Impact
[How this affects the overall system]
```

## 🧪 Testing & Validation

### Test Script Organization
- **Core Validation Scripts**: Keep scripts that validate fundamental functionality and enable regression testing
- **Temporary Debug Scripts**: Delete scripts created for specific issues after resolution
- **Test Directory**: Use `/test-scripts/` for organized validation and testing scripts
- **Script Naming**: Use descriptive names like `test-[feature].fsx` or `test-[phase].fsx`

### Migration Testing Strategy
- **Output Comparison**: Use hash-based validation to ensure identical output between old and new systems
- **Feature Flag Testing**: Test both systems with feature flag switching before production
- **Integration Testing**: Verify new components don't break existing build processes
- **RSS Feed Validation**: Include XML validation, item counts, and proper channel structure
- **End-to-End Testing**: Validate complete user workflows, not just individual components

### Continuous Validation
- **Build After Changes**: Compile and test after each significant change
- **Incremental Testing**: Test each module individually before integration
- **Error Documentation**: Document problems and solutions for future reference
- **Regression Prevention**: Validate all existing functionality continues to work

## 🗂️ Project Management Best Practices

### Active Directory Hygiene
**Critical Rule**: `projects/active/` contains ONLY current work.

- **Complete Archival**: Move ALL project files (plan + requirements) to archive together
- **Immediate Cleanup**: Delete from active directory immediately after completion
- **No Placeholder Files**: Never leave empty files in active directory
- **Clean State Verification**: Active directory should clearly show current work focus

### Log Management
- **Phase-Specific Logs**: Create `logs/YYYY-MM-DD-[project-phase]-log.md` for focused work
- **Immediate Cleanup**: Summarize in changelog and delete logs immediately upon completion
- **No Accumulation**: Only keep logs for currently active work
- **Changelog as Record**: Use `changelog.md` as permanent record with links to archived plans

### Documentation Lifecycle
1. **Phase Completion**: Summarize achievements in changelog, delete phase log immediately
2. **Project Completion**: Archive project plan, remove from active, update references
3. **Reference Validation**: Ensure all links point to correct archived/current locations

## 🚀 Migration Pattern (Proven Success)

### Research-Enhanced Migration Process

#### **Pre-Migration Research**
Before starting any migration:
- **Pattern Research**: Use DeepWiki to find similar F# migration projects
- **Best Practice Validation**: Use Microsoft docs to confirm migration approaches
- **Risk Assessment**: Use Perplexity reasoning to analyze potential migration risks
- **Library Research**: Use Context7 to understand current library capabilities

### Phase 1: Domain Enhancement
- Enhance existing types with new fields (tags, date, etc.)
- Implement required interfaces (ITaggable)
- **Research Integration**: Validate type design against F# best practices using Microsoft docs
- Create comprehensive test scripts for validation

### Phase 2: Processor Implementation  
- Implement new AST-based processor in GenericBuilder
- Add new build function to Builder.fs
- **Pattern Validation**: Research AST processing patterns using available tools
- Integrate feature flag logic in Program.fs
- Validate with test scripts and builds

### Phase 3: Migration Validation
- Create output comparison test scripts
- **Validation Research**: Use Microsoft docs for testing best practices
- Validate RSS feed generation
- Test integration and regression scenarios
- Confirm 100% output compatibility

### Phase 4: Production Deployment
- Deploy new processor as default (remove feature flag dependency)
- Remove legacy functions systematically
- **Cleanup Guidance**: Research deprecation patterns and cleanup strategies
- Clean up unused imports and dependencies
- Archive project and update documentation

### Success Metrics
- **Code Reduction**: Track lines of code eliminated
- **Function Cleanup**: Count deprecated functions removed
- **New Capabilities**: Document features enabled (RSS feeds, unified processing)
- **Architecture Consistency**: Validate pattern reuse across content types
- **Research Documentation**: Capture key insights from research that informed decisions

### Books Migration Pattern Learnings (2025-07-10)
- **Architecture Reuse Insight**: Successfully applied "books are reviews" insight to reuse existing review block infrastructure instead of creating new custom blocks - demonstrates value of analyzing domain relationships before implementation
- **Review Block Validation**: Confirmed that existing `:::review` blocks are robust and can handle different content types (books) while maintaining their original purpose
- **Feature Flag Safety**: Fourth consecutive successful migration using feature flag pattern - confirms this is a reliable, low-risk approach for production migrations
- **Content Metadata Preservation**: Books migration successfully preserved all complex metadata (title, author, rating, status, ISBN, cover) while converting from loading-only to full processing
- **RSS Feed Integration**: Adding RSS feeds during migration continues to be valuable and straightforward using existing infrastructure

### Notes Migration Pattern Learnings (2025-07-12)
- **AST Parsing Critical Fix**: Discovered ASTParsing.fs was storing raw markdown instead of rendered HTML, affecting all content types - essential to verify rendered output post-deployment
- **Cross-Content Impact**: Single AST infrastructure change affects all content types (notes, posts, snippets, wiki, presentations, books) - demonstrates importance of comprehensive testing
- **Production Validation**: Post-deployment content verification caught critical regression not visible in build process - validates need for actual output testing
- **Immediate Fix Protocol**: Critical rendering issues require immediate resolution before project archival - maintains production quality standards
- **Architecture Consolidation**: 6th successful migration completes major content type unification under GenericBuilder pattern - establishes this as proven standard approach

## 🔄 Production Deployment Best Practices

### Feature Flag Removal
- **Environment Independence**: Production should not depend on environment variables for core functionality
- **Default Selection**: Code should default to new system with old system completely removed
- **Systematic Removal**: Remove legacy functions in logical order (Program.fs → supporting functions → imports)
- **Build Validation**: Test compilation after each removal step

### Post-Deployment Validation
- **Immediate Testing**: Test actual rendered output after deployment to catch regressions
- **Content Verification**: Verify content renders correctly (HTML vs raw markdown)
- **Cross-Content Impact**: Check if changes affect other content types using same infrastructure
- **Quick Fix Protocol**: Apply immediate fixes for critical rendering issues before final archival

### Legacy Code Cleanup
- **Complete Identification**: Systematically find all components (processors, loaders, parsers, integrations)
- **Incremental Removal**: Remove in dependency order to maintain build state
- **Functionality Preservation**: Ensure new system maintains exact external behavior
- **Immediate Cleanup**: Perform all cleanup immediately after successful deployment

### RSS Feed Integration
- **XML Standards**: Include proper XML declaration for RSS validation
- **Directory Management**: Ensure feed directories are created programmatically
- **Content Consistency**: Use same URL patterns and metadata as existing content types
- **Validation Testing**: Test feeds with XML validation and proper structure

## ✅ Project Completion Protocol

### Transition Requirements
- **Explicit User Approval**: Never proceed to next phase/project without instruction
- **Architecture Readiness**: Verify foundation is solid for future work
- **Complete Documentation**: Finalize all logs with metrics and lessons learned
- **No Technical Debt**: Ensure no issues that would complicate future work

### Workflow Evolution Documentation
- **Capture Learnings**: Document any workflow improvements, new patterns, or process refinements discovered during the project
- **Update Instructions**: Add proven methodologies and best practices to `.github/copilot-instructions.md` in the appropriate sections:
  - New technical patterns → `🔧 Technical Standards` section
  - Testing improvements → `🧪 Testing & Validation` section  
  - Process refinements → `⚙️ Development Process` section
  - Documentation patterns → `📝 Documentation Standards` section
  - Migration learnings → `🚀 Migration Pattern` section
- **Pattern Validation**: Note when existing patterns work well or need refinement across different project types
- **Process Improvements**: Document any efficiency gains, error prevention techniques, or quality improvements found

### Completion Checklist
- [ ] All objectives implemented and tested
- [ ] No regression in existing functionality  
- [ ] Code quality improvements documented
- [ ] All project files archived (plan + requirements)
- [ ] Active directory cleaned (only current work remains)
- [ ] Backlog updated (item marked complete and removed)
- [ ] Changelog entry created with project summary
- [ ] Phase logs summarized and deleted
- [ ] Workflow improvements documented in `.github/copilot-instructions.md` appropriate sections
- [ ] Explicit completion declaration made

This workflow ensures systematic, quality-focused development that preserves functionality while improving architecture incrementally.
