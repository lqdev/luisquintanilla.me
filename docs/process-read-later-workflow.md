# Process Read Later Workflow

## Overview
This workflow automatically processes "Read Later" issues, creates PRs with the changes, and automatically merges them when there are no conflicts. It also includes automatic cleanup functionality to remove entries older than 14 days.

## Workflow Components

### 1. Add New Entry (Issue-Triggered)

**Trigger**: When an issue is opened with the `read-later` label by `@lqdev`

**Process**:
1. **Parse Issue**: Extracts URL and optional title from the issue form
2. **Fetch Title**: If no title provided, attempts to fetch from the URL
3. **Check Duplicates**: Verifies the URL doesn't already exist in `Data/read-later.json`
4. **Create PR**: Creates a pull request with the new entry
5. **Auto-merge**: Automatically merges the PR if there are no conflicts
6. **Close Issue**: Closes the original issue with a success message

### 2. Automatic Cleanup (Scheduled)

**Trigger**: Daily at midnight UTC (cron: `0 0 * * *`) or manual workflow dispatch

**Process**:
1. **Calculate Cutoff**: Determines the date 14 days ago
2. **Filter Entries**: Keeps only entries with `dateAdded` within the last 14 days
3. **Create PR**: If entries were removed, creates a cleanup PR with statistics
4. **Auto-merge**: Automatically merges the cleanup PR if there are no conflicts
5. **Log Summary**: Outputs cleanup statistics to GitHub Actions summary

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

## Automatic Cleanup Feature

### Overview
The cleanup job automatically removes read later entries older than 14 days to keep the list fresh and relevant.

### How It Works

1. **Scheduled Execution**: Runs daily at midnight UTC
2. **Age Calculation**: Computes cutoff date (current date - 14 days)
3. **Entry Filtering**: Uses jq to filter entries where `dateAdded >= cutoff`
4. **Conditional PR**: Only creates PR if entries were actually removed
5. **Auto-merge**: Merges cleanup PR automatically like add PRs
6. **Logging**: Outputs detailed summary to GitHub Actions

### Testing Cleanup

#### Manual Trigger:
1. Go to GitHub Actions tab
2. Select "Process Read Later Issue" workflow
3. Click "Run workflow" button (workflow_dispatch)
4. Monitor the cleanup-old-entries job

#### Test Script:
```bash
# Run the cleanup test script
./Scripts/test-read-later-cleanup.sh
```

This validates:
- Workflow YAML syntax
- Cleanup job structure
- Schedule and manual triggers
- 14-day filtering logic with mock data
- Job conditions

### Cleanup Statistics

Each cleanup run logs:
- **Original entry count**: Total entries before cleanup
- **Entries removed**: Number of entries older than 14 days
- **Remaining entries**: Entries kept (< 14 days old)

### Customizing Cleanup Period

To change the 14-day period:

1. Edit `.github/workflows/process-read-later.yml`
2. Find the `cleanup-old-entries` job
3. Modify the cutoff calculation:
   ```bash
   # Change '14 days ago' to your desired period
   CUTOFF_DATE=$(date -u -d '14 days ago' +"%Y-%m-%dT%H:%M:%SZ")
   ```
4. Update PR body text to reflect new period

### Cleanup Behavior

- **No old entries**: If no entries are older than 14 days, no PR is created
- **Has old entries**: PR created with detailed statistics and auto-merged
- **Merge conflicts**: Manual intervention required (rare)
- **Job summary**: Always generated regardless of whether cleanup occurred

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
