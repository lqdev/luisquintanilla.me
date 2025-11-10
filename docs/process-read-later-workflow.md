# Process Read Later Workflow

## Overview
This workflow automatically processes "Read Later" issues, creates PRs with the changes, and automatically merges them when there are no conflicts.

## How It Works

1. **Trigger**: When an issue is opened with the `read-later` label by `@lqdev`
2. **Parse Issue**: Extracts URL and optional title from the issue form
3. **Fetch Title**: If no title provided, attempts to fetch from the URL
4. **Check Duplicates**: Verifies the URL doesn't already exist in `Data/read-later.json`
5. **Create PR**: Creates a pull request with the new entry
6. **Auto-merge**: Automatically merges the PR if there are no conflicts
7. **Close Issue**: Closes the original issue with a success message

## Auto-Merge Implementation

The workflow uses direct PR merging via the GitHub REST API instead of GitHub's native auto-merge feature (which requires branch protection rules).

### Merge Process:
1. Waits 2 seconds for GitHub to process the PR
2. Checks the PR mergeable state
3. If mergeable (no conflicts), merges using squash merge
4. Handles errors gracefully with informative comments

### Advantages:
- ✅ Works without branch protection rules
- ✅ Immediate merge when conditions are met
- ✅ Clean squash commits
- ✅ Graceful error handling

## Testing the Workflow

### Manual Testing Steps:

1. **Create a test issue** using the "Add to Read Later" issue template
2. **Fill in the form**:
   - URL: Any valid URL (e.g., https://example.com/test-article)
   - Title: (Optional) A descriptive title
3. **Submit the issue**
4. **Monitor the workflow**:
   - Go to Actions tab → Process Read Later Issue
   - Watch for the workflow run
5. **Verify outcomes**:
   - ✅ PR is created with the new entry
   - ✅ PR is automatically merged (if no conflicts)
   - ✅ Branch is automatically deleted
   - ✅ Issue is closed with success message

### Expected Behavior:

#### Success Case (No Conflicts):
- PR created immediately
- PR merged automatically after ~2 seconds
- Issue closed with: "🎉 Your link has been added to the Read Later list!"
- Link appears on `/resources/read-later` after site rebuild

#### Conflict Case:
- PR created successfully
- Comment added: "⚠️ This PR has conflicts and cannot be automatically merged."
- Manual merge required

#### Duplicate Case:
- No PR created
- Issue closed with: "⚠️ This URL already exists in your Read Later list."

## Troubleshooting

### If Auto-Merge Fails:
1. Check the workflow logs for error details
2. Verify the PR was created (it should still exist even if merge fails)
3. Manually merge the PR if needed
4. Check for:
   - Merge conflicts with main branch
   - API rate limits
   - Network connectivity issues

### Common Issues:
- **"PR has conflicts"**: Base branch has changed; resolve conflicts manually
- **"Could not automatically merge"**: GitHub API issue; try manual merge
- **"Protected branch rules not configured"**: This error should no longer occur with the direct merge approach

## Migration Notes

### Previous Implementation (v1):
- Used `enablePullRequestAutoMerge` GraphQL mutation
- Required branch protection rules
- Failed with: "Protected branch rules not configured for this branch"

### Current Implementation (v2):
- Uses `github.rest.pulls.merge` REST API
- No branch protection requirements
- Direct merge with proper error handling
- Maintains same user experience

## Related Files:
- Issue Template: `.github/ISSUE_TEMPLATE/add-read-later.yml`
- Data File: `Data/read-later.json`
- This Workflow: `.github/workflows/process-read-later.yml`
