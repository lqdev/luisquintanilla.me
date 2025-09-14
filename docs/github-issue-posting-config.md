# GitHub Issue Template Posting Configuration

## Supported Post Types

### Notes
- **Issue Template**: `.github/ISSUE_TEMPLATE/post-note.yml`
- **Target Directory**: `_src/notes/`
- **Post Type**: `note`
- **File Pattern**: `{slug}-{date}.md`
- **Required Fields**: title, content
- **Optional Fields**: tags

## Future Post Type Support

The system is designed to be extensible. Here's how to add support for additional post types:

### Adding a New Post Type

1. **Create Issue Template**: 
   - Copy `post-note.yml` to `post-{type}.yml`
   - Update labels, description, and form fields
   - Customize validation rules

2. **Update Workflow**:
   - Modify `process-post-issue.yml` to detect new post type
   - Add conditional logic for different post types
   - Update file paths and frontmatter structure

3. **Test Configuration**:
   - Validate issue template renders correctly
   - Test workflow with new post type
   - Ensure proper file generation and PR creation

### Post Type Templates

**Blog Posts** (Future):
```yaml
post_type: post
target_directory: _src/posts/
required_fields: [title, content, description]
optional_fields: [tags, category]
```

**Responses** (Future):
```yaml
post_type: response
target_directory: _src/responses/
required_fields: [title, targeturl, response_type]
optional_fields: [tags, content]
```

**Reviews** (Future):
```yaml
post_type: review
target_directory: _src/reviews/
required_fields: [title, content, rating]
optional_fields: [tags, category, recommendation]
```

## Configuration Guidelines

### Frontmatter Standards
All post types should include:
- `title`: String
- `post_type`: String (matching directory)
- `published_date`: ISO format with timezone
- `tags`: Array of strings (optional)

### File Naming Convention
- Use slug generated from title
- Include date for uniqueness: `{slug}-{YYYY-MM-DD}.md`
- Ensure valid filesystem characters only

### Validation Rules
- Title: Required, 1-100 characters
- Content: Required, minimum 10 non-whitespace characters
- Tags: Optional, comma-separated, auto-converted to lowercase
- URLs: Required for response types, must be valid HTTP/HTTPS

### Error Handling
- Clear error messages for validation failures
- Keep issues open for user correction
- Provide helpful guidance in error comments
- Log detailed information for debugging

## Workflow Architecture

The GitHub Action workflow is structured for extensibility:

```javascript
// Detect post type from issue labels
const postType = getPostTypeFromLabels(issue.labels);

// Get configuration for post type
const config = getPostTypeConfig(postType);

// Process according to configuration
const result = processPost(issueBody, config);
```

This allows adding new post types without major workflow changes.