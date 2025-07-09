# Copilot Instructions for Project Development Workflow

## Overview

This document outlines the systematic project development workflow for the indieweb content management system. It captures proven methodologies for managing projects from backlog through completion, supporting both single-focus and multi-project development approaches.

**Key Achievement**: Evolved from refactoring-specific workflow to comprehensive project management system supporting diverse development tasks while maintaining quality and documentation standards.

## Core Development Principles

1. **Work Out Loud**: Document every step with verbose explanations in daily logs
2. **Incremental Implementation**: Make changes in small, testable steps
3. **Preserve Functionality**: Ensure existing behavior is maintained throughout
4. **Evidence-Based Planning**: Update plans based on actual implementation findings
5. **Clear Completion Criteria**: Define success metrics before starting work
6. **Context Preservation**: Maintain project continuity across sessions

## Project Workflow Management

### Backlog-Driven Development

1. **Project Selection**
   - Review `projects/backlog.md` for prioritized features and improvements
   - Select appropriate items based on current capacity and dependencies
   - Priority levels: **High** (critical), **Medium** (important), **Low** (nice to have), **Research** (exploratory)

2. **Moving Items to Active**
   - **Requirements Phase**: Create requirements document using `projects/templates/requirements-template.md`
   - **Collaborative Planning**: Work together to define problem statement, success criteria, and approach
   - **Create Project Plan**: Create detailed implementation plan in `projects/active/[project-name].md`
   - **Update Backlog Status**: Mark as in progress: `[ ]` → `[>]`
   - **Begin Documentation**: Start daily logging in date-based log files

### Project State Management

- **Backlog**: `[ ]` - Ideas and planned work in `projects/backlog.md`
- **Active**: `[>]` - Currently in progress with project plan in `projects/active/[project-name].md`
- **Complete**: Removed from backlog, archived in `projects/archive/`, and deleted from `projects/active/`
- **Archive**: Completed projects in `projects/archive/` with completion summary

**Active Directory Rule**: The `projects/active/` directory must only contain projects currently being worked on. Completed projects must be removed from active and moved to archive to maintain clear visibility of current work.

### Documentation Hierarchy

1. **Daily Logs** (`logs/YYYY-MM-DD-log.md`) - Implementation details and decisions
2. **Project Plans** (`projects/active/*.md`) - Project scope, objectives, and progress
3. **Backlog** (`projects/backlog.md`) - Strategic overview of planned work
4. **Changelog** (`changelog.md`) - High-level site evolution summary

## Development Lifecycle

### Project Initiation

1. **Analyze Current State**
   - Examine relevant files completely using large chunk reads
   - Identify specific patterns, dependencies, and constraints
   - Document findings in daily log with specific references
   - **Best Practice**: Large file reads are more efficient than multiple small reads

2. **Define Implementation Plan**
   - Break down work into logical phases or steps
   - Identify dependencies and potential constraints
   - Set clear success criteria and testing approach
   - Document plan in project file and reference in daily logs

### Active Development

3. **Daily Work Sessions**
   - Create phase-specific logs for focused work: `logs/YYYY-MM-DD-[project-phase]-log.md`
   - Document session objectives and planned approach for the specific phase
   - Reference active project plan for context and progress tracking
   - **Focus Principle**: One log per active project phase enables better context and completion tracking

4. **Step-by-Step Implementation**
   - Make one logical change at a time
   - Document each change in daily log before making it
   - Use appropriate edit tools with sufficient context
   - **Critical**: Consider module dependencies and system constraints

5. **Continuous Testing and Validation**
   - Test functionality after each significant change
   - Document test results in daily log
   - Fix issues immediately before proceeding
   - Validate against project success criteria
   - **Best Practice**: Compile and test continuously, not just at end

6. **Error Handling and Learning**
   - When errors occur, document the problem and solution
   - Update approach based on technical constraints discovered
   - Capture discoveries for future reference
   - Update project plan if significant deviations occur

### Project Completion

7. **Comprehensive Testing**
   - Test all existing functionality works correctly
   - Verify all expected outputs are generated properly
   - Check for regressions in existing features

8. **Impact Analysis and Documentation**
   - Document architectural improvements achieved
   - Measure code quality improvements (lines reduced, complexity, etc.)
   - Identify performance benefits and note any edge cases discovered

9. **Finalize and Archive**
   - Complete final summary in daily log
   - Update project plan with completion status and lessons learned
   - Copy project plan from `projects/active/` to `projects/archive/`
   - Delete project plan from `projects/active/` (only active projects should remain in active directory)
   - Remove completed project from `backlog.md`
   - Add entry to `changelog.md` with project summary

## Technical Implementation Guidelines

### Code Quality Standards

- **Function Sizing**: If function >20 lines, consider separation of concerns
- **Module Responsibility**: Each module should handle one concern (parsing, rendering, generation)
- **Type-First Design**: Define types before functions, use them to drive API design
- **Centralized Entry Points**: Create single functions that handle complete workflows

### Implementation Patterns

1. **Enhance Core Module** (Add types and functions)
2. **Replace Usage** (Update calling code to use new functions)
3. **Remove Old Code** (Clean up deprecated functions)
4. **Test and Validate** (Ensure everything works)

### Error Handling Evolution

- **Early Phases**: Focus on functionality, basic error handling
- **Later Phases**: Introduce comprehensive validation and error types
- **Incremental Improvement**: Don't over-engineer early phases

## Documentation Standards

### Daily Log Structure

```markdown
# Development Log - [Date]

## Session Objectives
[What you plan to accomplish]

## Current State Analysis
[Detailed analysis of existing code/situation]

## Implementation Steps

### Step N: [Step Description]
[What you're doing and why]

### Step N Complete: [Achievement Summary]
[What was accomplished, issues fixed, metrics]

## Session Summary
[Complete analysis of achievements and next steps]
```

### Project Plan Structure

Use the template in `projects/templates/requirements-template.md` for consistent project documentation including:
- Problem statement and context
- Success criteria and objectives
- Technical approach and constraints
- Implementation timeline and milestones

### Commit Message Standards

- Use clear, descriptive commit messages
- Reference project and step when relevant
- Include file changes and their purpose

## Quality Metrics and Success Indicators

- **Reduced Complexity**: Fewer lines of code doing the same work
- **Eliminated Duplication**: Single source of truth for common operations
- **Improved Performance**: More efficient algorithms and fewer redundant operations
- **Better Separation**: Clear module boundaries and responsibilities
- **Enhanced Maintainability**: Code that is easier to understand and modify

## Multi-Project Support

- Support concurrent projects when appropriate
- Maintain context switching discipline with proper documentation
- Ensure each project has clear boundaries and deliverables
- Use project plans in `projects/active/` for focused work tracking

## Project Transition Protocol

### Before Moving to Next Phase/Project

1. **Complete Current Work Documentation**
   - Finalize log entries with metrics and analysis
   - Update project plan with progress and any deviations
   - Document lessons learned and discoveries

2. **Architecture Readiness Check**
   - Verify foundation is solid for next work
   - Ensure no technical debt that would complicate future work
   - Confirm all existing functionality works

3. **Explicit Completion Declaration**
   - Mark work as complete in project plan
   - State readiness for next phase or project
   - Get explicit approval before proceeding to new work

### Never Do

- Don't proceed to next phase/project without explicit instruction
- Don't skip testing intermediate steps
- Don't make assumptions about user intent
- Don't combine multiple phases in single implementation

## Changelog Entry Template

When adding entries to `changelog.md`, use this template:

```markdown
## YYYY-MM-DD - [Project Name] [Status Icon]

**Project**: [Link to project plan]  
**Duration**: [Start] - [End]  
**Status**: [Complete/In Progress/Cancelled]

### What Changed
[High-level description of what was accomplished]

### Technical Improvements  
[Bullet points of technical achievements]

### Features Added/Removed
[List of user-facing changes]

### Architecture Impact
[How this affects the overall system]

### Documentation Created/Updated
[Links to relevant documentation]
```

## Project Completion Checklist

- [ ] All project objectives implemented and tested
- [ ] No regression in existing functionality
- [ ] Code quality metrics improved (when applicable)
- [ ] Architecture foundation solid for future work
- [ ] All changes documented in daily logs
- [ ] Project plan updated with completion status and lessons learned
- [ ] Project plan copied from `projects/active/` to `projects/archive/`
- [ ] Project plan deleted from `projects/active/` (only active projects should remain in active directory)
- [ ] Project removed from backlog and moved to archive
- [ ] Changelog updated with project summary
- [ ] Explicit completion declaration made

This workflow ensures systematic, documented, and quality-focused development that preserves functionality while improving architecture incrementally.

## Workflow Improvements and Lessons Learned

### Log Management Best Practices

**Key Learning**: Phase-specific logs should be summarized in changelog and deleted immediately to maintain clean documentation structure.

- **Phase-Specific Logs**: Create `logs/YYYY-MM-DD-[project-phase]-log.md` for each active project phase
- **Immediate Cleanup Protocol**: Summarize achievements in `changelog.md` and delete phase logs immediately upon completion
- **No Log Accumulation**: Only keep logs for currently active work - completed logs should never accumulate
- **Changelog as Permanent Record**: Use `changelog.md` as the permanent record with links to archived project plans
- **Focus Benefit**: Phase-specific logs provide better context and completion tracking than single accumulated logs

### Test Script Organization

**Key Learning**: Test scripts need dedicated organization separate from main codebase.

- **Dedicated Test Folder**: Create `/test-scripts/` directory for validation and testing scripts
- **Phase-Specific Tests**: Create test scripts for each major phase to validate functionality
- **Preserve Test Scripts**: Unlike logs, keep test scripts as they provide ongoing validation capability
- **Test Script Naming**: Use descriptive names like `test-[phase-name].fsx` or `test-[feature].fsx`

### Phase Transition Protocol Enhancement

**Key Learning**: Explicit approval protocol prevents scope creep and ensures quality completion.

- **Explicit User Approval Required**: Never proceed to next phase without explicit user instruction
- **Completion Validation**: Ensure all phase objectives are met and tested before requesting approval
- **Workflow Improvement Check**: After each phase, explicitly check for and document workflow/process improvements
- **Architecture Readiness**: Verify foundation is solid for next work before proceeding

### Multi-Phase Project Management

**Key Learning**: Large projects benefit from phase-based organization with clear boundaries.

- **Phase-Specific Planning**: Break large projects into phases with clear deliverables
- **Phase Completion Documentation**: Update project plan to mark completed phases with validation results
- **Progressive Architecture**: Build foundation phases first, then layer additional functionality
- **Test-Driven Validation**: Create test scripts for each phase to validate implementation

### Technical Implementation Refinements

**Key Learning**: Type qualification and compilation validation are critical for F# projects.

- **Type Qualification**: Always use fully qualified types (e.g., `MediaType.Unknown` not `Unknown`)
- **Continuous Compilation**: Test build after each significant change, not just at end
- **Module Dependencies**: Ensure new modules are properly added to project file immediately
- **Test Script Validation**: Run test scripts after implementation to validate functionality
- **Type Annotations in Scripts**: Use explicit type annotations (`: Post`, `: Snippet`) in test scripts to prevent F# type inference issues

### Documentation Quality Standards

**Key Learning**: Consistent documentation patterns improve project continuity.

- **Traceability**: Maintain clear links between specifications, project plans, implementation logs, and test results
- **Progress Tracking**: Update project plans with completion status and validation details
- **Decision Documentation**: Capture why specific approaches were chosen over alternatives
- **Learning Integration**: Update workflow instructions based on implementation discoveries

### Error Recovery Patterns

**Key Learning**: Systematic approach to fixing compilation and runtime errors.

- **Incremental Testing**: Test each module individually before integration
- **Type System Leverage**: Use F# type system to catch issues early
- **Context Preservation**: Document error fixes in logs for future reference
- **Rollback Capability**: Ensure each change can be safely reverted if needed

### Comprehensive Testing Strategies

**Key Learning**: Multi-layer testing approach ensures infrastructure reliability and compatibility.

- **Comparison Testing**: When replacing existing systems, create test scripts that compare old vs new output
- **Context Validation**: Test new functionality in different usage contexts (cards, feeds, pages)
- **Integration Testing**: Verify new components don't break existing build processes during parallel development
- **Regression Prevention**: Validate all existing functionality continues to work after infrastructure changes
- **Test Script Preservation**: Keep test scripts for ongoing validation, unlike phase-specific logs which should be archived
- **Documentation Testing**: Create comprehensive test content files that exercise all custom block types and edge cases

### Project Cleanup and Maintenance

**Key Learning**: Regular cleanup prevents documentation and test script bloat, maintaining clean development environment.

- **Completed Phase Log Deletion**: Delete phase-specific logs immediately after summarizing in changelog - don't accumulate
- **Project Archival Timing**: When all phases of a project are complete, archive immediately to keep active directory focused
- **Test Script Lifecycle**: Distinguish between core validation scripts (keep) vs temporary debug scripts (delete after issue resolution)
- **Cleanup Triggers**: Perform cleanup at project completion, not as separate maintenance tasks
- **Reference Accuracy**: Update all documentation references when archiving/deleting to prevent broken links

### Test Script Classification

**Keep - Core Validation Scripts**: 
- Scripts that validate fundamental functionality (AST parsing, domain integration, etc.)
- Scripts used for regression testing after future changes
- Scripts that test complete workflows end-to-end

**Remove - Temporary Debug Scripts**:
- Scripts created to debug specific issues during implementation
- Minimal test scripts superseded by comprehensive versions  
- Pipeline configuration debug scripts after issues are resolved

### Documentation Cleanup Protocol

1. **Phase Completion**: Summarize achievements in changelog, delete phase log immediately
2. **Project Completion**: Archive project plan, remove from active, update any references
3. **Test Scripts**: Remove debug/temporary scripts, update README to reflect current state
4. **Reference Validation**: Ensure all documentation links point to correct archived/current locations
