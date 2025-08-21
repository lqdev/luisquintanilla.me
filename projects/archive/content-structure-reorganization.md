# Content Structure Reorganization Project

## Project Overview
**Goal**: Align source directory structure with output structure to eliminate content type mismatches and improve maintainability.

## Current State Analysis
**Date**: August 20, 2025
**Issue Identified**: Content types are stored in wrong source directories, causing confusion and maintenance complexity.

### Discovered Misalignments:
1. **Notes Content**: All notes are in `_src/feed/` instead of `_src/notes/` (which is empty)
2. **Bookmarks Content**: All bookmarks are in `_src/responses/` instead of `_src/bookmarks/` (which is empty)

## Implementation Plan

### Phase 1: Content Audit
- Scan `_src/feed/` for files with `post_type: "note"`
- Scan `_src/responses/` for files with `response_type: "bookmark"`
- Generate file lists for systematic migration
- Validate content types and counts

### Phase 2: Directory Migration
- Move note files from `_src/feed/` to `_src/notes/`
- Move bookmark files from `_src/responses/` to `_src/bookmarks/`
- Update any hardcoded file paths in Builder.fs

### Phase 3: Validation & Testing
- Test build process for any broken references
- Verify RSS feeds generate correctly
- Validate output structure matches expectations

## Success Criteria
- All content types stored in appropriate source directories
- Build process works without errors
- RSS feed generation maintains existing functionality
- URL structure and output remain unchanged
