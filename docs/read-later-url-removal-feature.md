# Read Later URL Removal Feature

## Overview
This feature automatically removes URLs from the `Data/read-later.json` file when creating response or bookmark posts that reference those URLs. This provides seamless cleanup of your read-later list without manual intervention.

## How It Works

When you create a response or bookmark post via GitHub issue templates:

1. **Issue Created**: User fills out the response/bookmark issue template with a target URL
2. **Workflow Triggered**: GitHub Actions workflow processes the issue
3. **URL Check**: F# script checks if the target URL exists in `Data/read-later.json`
4. **Automatic Removal**: If found, the URL is removed from the read-later list using `jq`
5. **File Creation**: Response/bookmark markdown file is created as normal
6. **PR Generated**: Changes are committed and PR is created

## Implementation Details

### Modified Files
- `Scripts/process-response-issue.fsx` - Added `removeFromReadLater` function
- `Scripts/process-bookmark-issue.fsx` - Added `removeFromReadLater` function

### Technical Approach
- Uses standard Unix tool `jq` for JSON processing
- Invoked via bash shell for proper quoting and escaping
- Executed before file creation to ensure cleanup happens first
- Gracefully handles missing files and non-existent URLs

### Code Structure

```fsharp
// Function to remove URL from read-later.json using jq via shell
let removeFromReadLater (url: string) =
    let readLaterPath = Path.Combine("Data", "read-later.json")
    
    if File.Exists(readLaterPath) then
        try
            let tempFile = Path.GetTempFileName()
            let psi = ProcessStartInfo()
            psi.FileName <- "/bin/bash"
            psi.Arguments <- sprintf """-c "jq --arg url '%s' 'map(select(.url != $url))' '%s' > '%s' && mv '%s' '%s'" """ 
                                url readLaterPath tempFile tempFile readLaterPath
            // ... execute and handle result
        with ex ->
            printfn "⚠️  Error updating read-later.json: %s" ex.Message
            false
    else
        true
```

### jq Command Explanation

The core jq command used:
```bash
jq --arg url 'https://example.com' 'map(select(.url != $url))' read-later.json
```

- `--arg url 'https://example.com'` - Pass URL as a variable for safe handling
- `map(select(.url != $url))` - Filter array keeping only items where URL doesn't match
- Result is written to temp file, then moved to original file atomically

## User Experience

### Before This Feature
1. Create response/bookmark via GitHub issue
2. Response/bookmark file is created
3. URL remains in read-later list
4. Manual cleanup required later

### After This Feature
1. Create response/bookmark via GitHub issue
2. Response/bookmark file is created
3. ✨ URL automatically removed from read-later list
4. No manual cleanup needed!

## Examples

### Creating a Response
```bash
# User creates GitHub issue with:
# - Response Type: reply
# - Target URL: https://example.com/article
# - Title: My thoughts on this article
# - Content: Great insights about...

# When workflow runs:
# 1. Script processes response
# 2. Checks Data/read-later.json for https://example.com/article
# 3. If found, removes it
# 4. Creates _src/responses/my-thoughts-on-this-article.md
# 5. Commits changes (both response file and updated read-later.json)
```

### Creating a Bookmark
```bash
# User creates GitHub issue with:
# - Target URL: https://example.com/resource
# - Title: Useful web development tool
# - Content: This tool is great for...

# When workflow runs:
# 1. Script processes bookmark
# 2. Checks Data/read-later.json for https://example.com/resource
# 3. If found, removes it
# 4. Creates _src/bookmarks/useful-web-development-tool.md
# 5. Commits changes (both bookmark file and updated read-later.json)
```

## Error Handling

The feature handles various edge cases gracefully:

- **File Not Found**: If `Data/read-later.json` doesn't exist, continues without error
- **URL Not in List**: If URL isn't in read-later list, continues without error
- **jq Execution Failure**: Logs warning but doesn't fail the entire workflow
- **Invalid JSON**: jq will catch and report any JSON syntax issues

## Testing

### Manual Testing
```bash
# Run the integration test suite
chmod +x test-integration.sh
./test-integration.sh
```

### Test Coverage
- ✅ URL removal for response posts
- ✅ URL removal for bookmark posts
- ✅ Handling non-existent URLs
- ✅ Graceful error handling
- ✅ File creation verification
- ✅ JSON integrity preservation

## Benefits

1. **Automatic Cleanup**: No manual list maintenance required
2. **Seamless Integration**: Works transparently with existing workflows
3. **Standard Tools**: Uses reliable Unix tools (jq) for JSON processing
4. **Robust**: Handles errors gracefully without breaking workflows
5. **Atomic Operations**: Uses temp files for safe JSON updates
6. **No Side Effects**: Doesn't modify any other functionality

## Workflow Integration

### GitHub Actions Workflows
No changes required to workflow files:
- `.github/workflows/process-content-issue.yml` - Works as-is
- `.github/ISSUE_TEMPLATE/post-response.yml` - Works as-is
- `.github/ISSUE_TEMPLATE/post-bookmark.yml` - Works as-is

The feature is self-contained in the F# processing scripts.

## Future Enhancements

Potential improvements for future consideration:
- Add logging of removed URLs to PR description
- Track statistics on read-later cleanup
- Option to preserve URL in read-later with "completed" flag instead of removal
- Support for pattern matching (e.g., remove all URLs from same domain)

## Troubleshooting

### URL Not Being Removed

Check the following:
1. Is `Data/read-later.json` present in the repository?
2. Does the URL exactly match (including protocol and query parameters)?
3. Check workflow logs for any jq errors
4. Verify jq is installed in the GitHub Actions runner

### JSON Corruption

The implementation uses atomic operations (write to temp file, then move) to prevent JSON corruption. If corruption occurs:
1. Check the workflow logs for jq errors
2. Verify the original read-later.json was valid JSON
3. Restore from git history if needed

## Related Documentation

- [Process Read Later Workflow](process-read-later-workflow.md)
- [GitHub Issue Templates](.github/ISSUE_TEMPLATE/)
- [Process Content Issue Workflow](.github/workflows/process-content-issue.yml)
