# GitHub Actions Dual Triggering Fix

## Problem
When a note or response issue was created via GitHub Issue Forms, both the note processing workflow (`process-post-issue.yml`) and response processing workflow (`process-response-issue.yml`) were being triggered, even though only one would actually run based on the issue labels.

This caused:
- Unnecessary workflow runs appearing in the Actions tab
- Potential confusion about which workflow was supposed to run
- Inefficient use of GitHub Actions quota

## Root Cause
Both workflows used identical triggers:
```yaml
on:
  issues:
    types: [opened]
```

This meant **every** issue creation triggered **both** workflows. The workflows then used job-level `if` conditions to filter by labels:

- **Note workflow**: `if: contains(github.event.issue.labels.*.name, 'note') && github.event.issue.user.login == 'lqdev'`
- **Response workflow**: `if: contains(github.event.issue.labels.*.name, 'response') && github.event.issue.user.login == 'lqdev'`

While this prevented both from running simultaneously, it still caused both to be **triggered** and appear in the Actions tab.

## Solution
Consolidated both workflows into a single workflow file (`process-content-issue.yml`) with two conditional jobs:

1. **`process-note`** job - Runs only when issue has "note" label
2. **`process-response`** job - Runs only when issue has "response" label

### Benefits
- ✅ **Single trigger**: Only one workflow is triggered per issue
- ✅ **Mutual exclusivity**: Jobs have mutually exclusive conditions  
- ✅ **Same functionality**: All existing behavior preserved
- ✅ **Cleaner Actions tab**: No more confusing dual workflow runs
- ✅ **Efficient quota usage**: No unnecessary workflow triggers

### Key Changes
- **Added**: `.github/workflows/process-content-issue.yml` - Single consolidated workflow
- **Removed**: `.github/workflows/process-post-issue.yml` - Old note processing workflow  
- **Removed**: `.github/workflows/process-response-issue.yml` - Old response processing workflow

## Validation
The solution was validated to ensure:
- Note issues trigger only the note job
- Response issues trigger only the response job  
- Unauthorized users trigger no jobs
- Issues without content labels trigger no jobs
- No issue can trigger both jobs simultaneously

## Impact
This fix resolves issue #257 by ensuring that only the appropriate workflow job runs for each content type, eliminating the dual triggering problem while maintaining all existing functionality.