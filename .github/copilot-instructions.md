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
   - Start with current state analysis in `logs/YYYY-MM-DD-log.md`
   - Document session objectives and planned approach
   - Reference active project plan for context and progress tracking

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

## Specification Translation Workflow

When working with comprehensive specifications or upgrade documents, follow this proven pattern:

### 1. Specification Analysis and Decomposition
- **Read Complete Specification**: Use large chunk reads to understand full scope
- **Identify Phase Structure**: Look for natural breakpoints and dependencies
- **Map Technical Constraints**: Analyze current codebase to understand limitations
- **Document Analysis**: Capture findings in daily log with specific file references

### 2. Project Identification and Breakdown
- **Phase to Project Mapping**: Transform specification phases into discrete projects
- **Dependency Analysis**: Identify technical and logical dependencies between projects
- **Scope Validation**: Ensure each project has clear boundaries and deliverables
- **Effort Estimation**: Base estimates on similar completed work and complexity analysis

### 3. Priority Assignment Framework
- **Technical Dependencies First**: Infrastructure projects get high priority
- **Risk Mitigation**: Projects that enable rollback/validation get high priority
- **Feature Implementation**: Core functionality gets medium priority
- **Optimization Last**: Performance and cleanup get low priority
- **Exploration Separate**: Research items get research priority

### 4. Backlog Quality Validation
- **Completeness Check**: Verify all specification elements have corresponding projects
- **Dependency Verification**: Ensure prerequisite relationships are logical and achievable
- **Success Criteria Review**: Confirm each project has measurable outcomes
- **Effort Distribution**: Check that high-priority work is appropriately sized for capacity

## Cross-Reference Documentation Standards

### File Linking Patterns
When working across multiple related documents, use consistent cross-referencing:

```markdown
**Specification Reference**: `website-upgrade.md` Phase 1
**Implementation Plan**: `projects/active/core-infrastructure.md`
**Progress Log**: `logs/2025-07-08-log.md` Step 3
**Backlog Item**: `projects/backlog.md` Core Infrastructure Implementation
```

### Traceability Maintenance
- **Specification → Project**: Clear mapping from source requirements to implementation projects
- **Project → Tasks**: Breakdown from project goals to actionable steps
- **Tasks → Validation**: Connection from implementation to success criteria
- **Validation → Documentation**: Results feeding back to project progress

## Context Preservation Enhancements

### Session Continuity Patterns
- **Reference Previous Work**: Always cite relevant previous logs and project documentation
- **State Transition Documentation**: Explicitly document what changed between sessions
- **Decision Rationale**: Capture why specific approaches were chosen over alternatives
- **Learning Integration**: Update project plans based on implementation discoveries

### Multi-Document Workflow
When working with specifications, backlog, and project plans:
1. **Start with Specification Analysis**: Understand complete scope before decomposition
2. **Create Implementation Structure**: Establish project hierarchy and dependencies
3. **Validate Against Constraints**: Check technical feasibility and resource capacity
4. **Document Decision Path**: Capture reasoning for prioritization and scope decisions

## Quality Assurance Patterns

### Backlog Validation Checklist
When creating or reviewing backlogs from specifications:
- [ ] **Completeness**: All specification phases/requirements have corresponding projects
- [ ] **Dependencies**: Technical prerequisites clearly identified and ordered
- [ ] **Scope Boundaries**: Each project has clear start/end criteria
- [ ] **Success Metrics**: Measurable outcomes defined for each project
- [ ] **Effort Realism**: Time estimates based on complexity analysis and historical data
- [ ] **Priority Logic**: High priority items enable subsequent work or reduce risk
- [ ] **Rollback Strategy**: Each project includes safe fallback approach

### Implementation Validation Patterns
- **Continuous Compilation**: Test build after each significant change
- **Output Comparison**: Validate new implementations against existing behavior
- **Feature Flag Testing**: Verify both old and new systems work in parallel
- **Regression Prevention**: Test existing functionality before and after changes
- **Documentation Sync**: Update project documentation as implementation progresses

## Tool Usage Optimization Patterns

### File Analysis Strategy
- **Large Chunk Reads**: Read 100+ lines at once rather than multiple small reads
- **Pattern Recognition**: Use grep search to identify repetitive code structures
- **Context Building**: Read related files in parallel when dependencies exist
- **State Documentation**: Capture file analysis findings in daily logs immediately

### Edit Tool Selection
- **replace_string_in_file**: When exact string replacement with sufficient context
- **insert_edit_into_file**: When adding new functionality with minimal context hints
- **create_file**: When establishing new documentation or project structures
- **Avoid Manual Copy-Paste**: Always use appropriate tools rather than showing code blocks

### Terminal Usage Guidelines
- **Background Processes**: Use `isBackground=true` for servers, watch tasks, long-running builds
- **Compilation Testing**: Run build commands after significant code changes
- **Output Management**: Use head/tail for potentially large outputs
- **Pager Prevention**: Add `| cat` or `--no-pager` flags for git, less, man commands
