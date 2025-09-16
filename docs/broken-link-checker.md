# Broken Link Checker Documentation

This repository includes an automated broken link checker that scans all markdown files in the `_src` directory and identifies broken links.

## Overview

The broken link checker:
- Scans all `.md` files in the `_src` directory recursively
- Extracts markdown links using pattern `[text](url)`
- Converts relative links to absolute URLs using `https://www.lqdev.me` as the base domain
- Tests HTTP accessibility of all links with proper timeout handling
- Generates detailed reports with GitHub issue format
- Automatically creates GitHub issues when broken links are found

## Files

### Scripts
- **`Scripts/check-broken-links.fsx`** - Main production script with full concurrency control
- **`Scripts/check-broken-links-simple.fsx`** - Simplified version for testing (limited files)
- **`Scripts/test-link-checker.fsx`** - Basic link extraction testing

### GitHub Action
- **`.github/workflows/check-broken-links.yml`** - Automated workflow

## GitHub Action Configuration

### Schedule
The action runs automatically:
- **Weekly**: Every Monday at 8:00 AM UTC (3:00 AM EST)
- **Manual**: Can be triggered manually via GitHub Actions UI

### Workflow Steps
1. Checkout repository
2. Setup .NET 9
3. Build F# project
4. Run broken link checker script
5. Create GitHub issue if broken links found
6. Clean up temporary files

### GitHub Issue Creation
When broken links are detected, the action automatically:
- Creates a new GitHub issue with title: "Broken Links Report - YYYY-MM-DD"
- Includes checkboxes for each broken link
- Provides test links for verification
- Adds labels: `maintenance`, `broken-links`
- Includes summary statistics and instructions

## Manual Usage

### Run Full Check
```bash
cd /path/to/repository
dotnet build
dotnet fsi Scripts/check-broken-links.fsx
```

### Run Test Check (Limited Files)
```bash
dotnet fsi Scripts/check-broken-links-simple.fsx
```

### Basic Link Extraction Test
```bash
dotnet fsi Scripts/test-link-checker.fsx
```

## Configuration Options

### Main Script (`check-broken-links.fsx`)
```fsharp
let baseUrl = "https://www.lqdev.me"          // Base URL for relative links
let srcDirectory = "_src"                      // Directory to scan
let httpTimeout = TimeSpan.FromSeconds(10.0)  // HTTP request timeout
let maxConcurrentRequests = 10                 // Concurrent request limit
```

### Test Script (`check-broken-links-simple.fsx`)
```fsharp
let maxFilesToCheck = 3    // Limit files for testing
let httpTimeout = TimeSpan.FromSeconds(5.0)  // Shorter timeout for testing
```

## Link Detection

### Supported Link Formats
- **Markdown links**: `[text](url)`
- **Relative links**: `/posts/example` → `https://www.lqdev.me/posts/example`
- **Absolute links**: `https://example.com/page`

### Excluded Links
- `mailto:` URLs (email addresses)
- `#` fragment links (page anchors)
- `javascript:` URLs

### Link Classification
- **Relative**: Links starting with `/` (converted to absolute using base URL)
- **Absolute**: Links starting with `http://` or `https://`

## Report Format

### Console Output
```
=== BROKEN LINK CHECKER RESULTS ===
Total links checked: 150
Working links: 140
Broken links: 10

=== BROKEN LINKS BY FILE ===
📁 posts/example.md
  ❌ Line 15: [broken link](/invalid/path) Relative (HTTP 404)
  ❌ Line 20: [external](https://invalid.example.com) Absolute (DNS error)
```

### GitHub Issue Format
```markdown
## Broken Links Report - 2024-01-15

Found **10 broken links** out of 150 total links checked across 5 files.

### Summary
- **Total files with broken links:** 5
- **Total broken links:** 10
- **Relative links:** 6
- **Absolute links:** 4

### Broken Links by File

#### 📁 `posts/example.md`
- [ ] **Line 15:** `[broken link](/invalid/path)` → [Test Link](https://www.lqdev.me/invalid/path) _(HTTP 404)_
- [ ] **Line 20:** `[external](https://invalid.example.com)` → [Test Link](https://invalid.example.com) _(DNS error)_

### Instructions
1. **Review each broken link** by clicking the "Test Link" to verify it's actually broken
2. **Check the checkboxes** ☑️ for links you've fixed or verified
3. **Update the markdown files** in the `_src` directory to fix broken links
4. **For relative links** - ensure they point to valid content in the repository
5. **For absolute links** - update URLs or remove if the resource is permanently unavailable
```

## Error Handling

### HTTP Errors
- **Timeout**: Connection timeout (configurable)
- **DNS Errors**: Domain name resolution failures
- **HTTP Status Codes**: 404, 500, etc. with status code reporting
- **Connection Errors**: Network connectivity issues

### File Processing Errors
- **File Access**: Permissions or file not found
- **Content Parsing**: Invalid markdown or encoding issues
- **Regex Errors**: Malformed link patterns

## Performance Considerations

### Concurrency Control
- **Semaphore**: Limits concurrent HTTP requests to prevent overwhelming servers
- **Timeout**: Prevents hanging on slow/unresponsive links
- **Chunked Processing**: Processes files in batches

### Resource Management
- **HTTP Client**: Proper disposal with timeout configuration
- **Memory**: Efficient streaming for large files
- **Network**: Respectful rate limiting

## Troubleshooting

### Common Issues

#### High False Positives
- **Solution**: Increase HTTP timeout for slow servers
- **Check**: Network connectivity in GitHub Actions environment

#### Missing Links
- **Solution**: Verify regex patterns match markdown format
- **Check**: File encoding and special characters

#### Action Failures
- **Solution**: Check .NET 9 setup and NuGet package access
- **Check**: GitHub Actions permissions for issue creation

### Debug Options
- Run simplified test script first: `check-broken-links-simple.fsx`
- Check specific file: Modify `test-link-checker.fsx`
- Increase logging in main script for detailed output

## Integration with Existing Workflow

### Build Process
The broken link checker integrates with existing F# build infrastructure:
- Uses same .NET 9 setup as other scripts
- Follows existing script patterns in `Scripts/` directory
- Compatible with current GitHub Actions configuration

### Issue Management
- **Labels**: Automatically applies `maintenance` and `broken-links` labels
- **Assignees**: Can be configured in workflow file
- **Notifications**: Standard GitHub issue notifications apply
- **Tracking**: Checkboxes enable progress tracking

## Future Enhancements

### Potential Improvements
- **Link Caching**: Cache successful links to improve performance
- **Exclude Lists**: Configure domains/patterns to skip
- **Retry Logic**: Automatic retry for temporary failures
- **Diff Reporting**: Only report newly broken links
- **Integration**: Webhook notifications or Slack integration
- **Analytics**: Historical broken link trends and reporting