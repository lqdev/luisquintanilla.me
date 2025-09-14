# Weekly Summary GitHub Action Test

This document explains how to test the weekly summary GitHub Action.

## Manual Testing

The workflow can be triggered manually for testing:

1. Go to the GitHub repository
2. Navigate to Actions tab
3. Select "Weekly Post Summary" workflow
4. Click "Run workflow" button
5. Confirm the workflow runs successfully

## Testing Steps Performed

✅ **Script Execution**: Verified `dotnet fsi Scripts/weekly-wrapup.fsx` runs successfully  
✅ **File Generation**: Confirmed script creates markdown files in `_src/notes/`  
✅ **Build Pipeline**: Tested .NET 9 build process works correctly  
✅ **YAML Validation**: Verified workflow syntax is valid  

## Workflow Features

- **Schedule**: Every Sunday at 7 PM EST (11 PM UTC)
- **Manual Trigger**: Available via `workflow_dispatch`
- **Change Detection**: Only creates PR if new content exists
- **Auto-cleanup**: Deletes feature branch after PR creation
- **Descriptive PRs**: Includes date, run number, and clear instructions

## Expected Behavior

When the workflow runs:
1. Builds the F# project
2. Executes weekly-wrapup.fsx script
3. Checks if new files were created
4. Creates PR with generated weekly summary (if content exists)
5. PR includes title like "Weekly Post Summary - 2025-09-14"

## Timezone Notes

The cron expression `0 23 * * 0` runs at 11 PM UTC every Sunday, which corresponds to:
- 7 PM EST (UTC-5) 
- 6 PM EST during daylight saving time (UTC-4)

This timing ensures the script captures a full week of content.