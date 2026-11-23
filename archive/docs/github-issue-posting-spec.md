# GitHub Issue Template Posting Specification

## Overview

This specification defines a workflow to replace Discord-based content publishing with GitHub Issue Templates, enabling direct posting to the website through GitHub Issues that automatically generate PRs with properly formatted content files.

## Current Discord Workflow Analysis

Based on PR #212 (Crawford vs. Canelo note), the current Discord workflow produces:

### Generated Content Structure
- **File Location**: `_src/notes/{slug}.md`
- **Frontmatter Format**:
  ```yaml
  ---
  title: Crawford vs. Canelo
  post_type: note
  published_date: "2025-09-14 00:51 -05:00"
  tags: ["boxing", "canelo", "crawford"]
  ---
  ```
- **Content**: Plain markdown text below frontmatter
- **PR Structure**: Descriptive title, content preview, frontmatter validation

## Proposed GitHub Issue Template Workflow

### 1. Issue Template Design

**Template Type**: Form-based issue template
**Filename**: `.github/ISSUE_TEMPLATE/post-note.yml`

**Form Fields**:
1. **Title** (required)
   - Input: text
   - Validation: Required, max 100 characters
   
2. **Content** (required)
   - Input: textarea
   - Validation: Required, supports markdown
   
3. **Tags** (optional)
   - Input: text
   - Format: Comma-separated values
   - Example: "boxing, sports, fighting"
   
4. **Post Type** (hidden, default: "note")
   - Used to determine content folder and processing

### 2. GitHub Action Workflow

**Trigger**: Issue opened with label "post"
**Workflow File**: `.github/workflows/process-post-issue.yml`

**Processing Steps**:
1. **Parse Issue Body**
   - Extract form responses using GitHub's issue forms API
   - Validate required fields are present
   
2. **Generate Content**
   - Create slug from title (lowercase, replace spaces/special chars with hyphens)
   - Generate timestamp in EST timezone format
   - Format tags as YAML array
   - Build frontmatter and combine with content
   
3. **Create Content File**
   - Generate file at `_src/notes/{slug}.md`
   - Use proper YAML frontmatter format matching existing content
   
4. **Create Pull Request**
   - Branch name: `content/issue-{issue-number}/note/{slug}`
   - PR title: "Add note post: {title}"
   - PR body: Include content preview and validation checklist
   - Auto-assign to repository owner
   
5. **Issue Management**
   - Close original issue automatically
   - Link to created PR in closing comment

### 3. Content Generation Rules

**Slug Generation**:
- Convert title to lowercase
- Replace spaces with hyphens
- Remove special characters except hyphens
- Ensure uniqueness (append timestamp if needed)

**Timestamp Format**: 
- Use EST timezone: "YYYY-MM-DD HH:MM -05:00"
- Generate at issue creation time

**Tag Processing**:
- Split comma-separated input
- Trim whitespace
- Convert to lowercase
- Generate YAML array format

### 4. Validation and Error Handling

**Content Validation**:
- Ensure title is not empty
- Verify content has substance (minimum character count)
- Validate tag format if provided

**Error Scenarios**:
- Invalid issue format → Comment with instructions, keep issue open
- Duplicate content → Comment with warning, proceed with timestamp suffix
- Processing failure → Comment with error details, label issue for manual review

### 5. Security Considerations

**Input Sanitization**:
- Escape special characters in title/content
- Validate file paths to prevent directory traversal
- Limit content size to prevent abuse

**Permission Model**:
- Only repository collaborators can create posts
- Action runs with limited permissions
- Generated content follows existing content structure

## Implementation Plan

### Phase 1: Basic Note Posts
- [ ] Create issue template for note posts
- [ ] Implement GitHub Action for basic processing
- [ ] Test with example note post
- [ ] Validate generated content format

### Phase 2: Enhanced Features  
- [ ] Add support for other post types (posts, responses)
- [ ] Implement content preview in PR description
- [ ] Add validation checks and error handling
- [ ] Create documentation for users

### Phase 3: Advanced Workflow
- [ ] Auto-merge for trusted users
- [ ] Content scheduling capabilities
- [ ] Integration with existing build pipeline
- [ ] Analytics and usage tracking

## Benefits vs Discord Workflow

**Advantages**:
- ✅ Native GitHub integration
- ✅ Version control for all content
- ✅ PR review workflow for quality control
- ✅ Accessible from any device with GitHub access
- ✅ No external service dependencies
- ✅ Audit trail and content history

**Considerations**:
- GitHub UI less optimized for mobile posting
- Requires GitHub account and repository access
- More steps compared to simple Discord message

## Success Criteria

1. **Functional Parity**: Issue template produces same output as Discord workflow
2. **User Experience**: Simple form interface for content creation
3. **Automation**: Zero manual intervention for standard note posts
4. **Integration**: Seamless fit with existing build and deployment pipeline
5. **Reliability**: Robust error handling and validation