# GitHub Actions Scheduled Workflows - Important Notes

## Overview

This repository uses GitHub Actions scheduled workflows (cron) to automate periodic tasks like processing ActivityPub post deliveries and Accept activities.

## Critical GitHub Actions Scheduler Behavior

### Scheduler Activation Requirement

**IMPORTANT**: GitHub Actions scheduled workflows (`on: schedule`) require a **push to the default branch AFTER the workflow file exists** to activate the cron scheduler.

#### Why This Matters:
- Adding a new workflow file with `on: schedule` does NOT immediately activate the scheduler
- Editing an existing scheduled workflow does NOT immediately update the scheduler
- The scheduler only detects changes when a **subsequent push occurs** to the default branch after the workflow file is modified

#### Solution:
After modifying any scheduled workflow file:
1. Ensure the workflow file is committed to the `main` branch
2. Make ANY subsequent commit and push to `main` to activate/update the scheduler
3. The scheduler will then begin executing according to the cron schedule

### Minimum Frequency Limit

GitHub Actions enforces a **minimum frequency of 5 minutes** for scheduled workflows:
- Valid: `*/5 * * * *` (every 5 minutes)
- Invalid: `* * * * *` (every minute - not supported)

More frequent intervals (e.g., every minute) will be treated as if they were set to 5 minutes.

### Timing and Reliability

- **All cron schedules use UTC timezone**
- **Delays are common**: Scheduled jobs may be delayed by several minutes during high load
- **No guarantees**: GitHub does not guarantee exact timing for scheduled workflows
- **Peak time delays**: Jobs scheduled exactly on the hour (`0 * * * *`) face more delays

#### Best Practices:
- Schedule jobs at off-peak times (e.g., `:05`, `:17`, `:33` instead of `:00`)
- Don't rely on scheduled workflows for time-critical operations requiring precision
- Implement backup trigger mechanisms for critical workflows

## Scheduled Workflows in This Repository

### 1. Process ActivityPub Post Deliveries
**File**: `process-activitypub-deliveries.yml`  
**Schedule**: Every 5 minutes (`*/5 * * * *`)  
**Purpose**: Processes queued ActivityPub post deliveries to Mastodon followers

**Triggers**:
- `schedule`: Every 5 minutes via cron
- `workflow_run`: After successful deployment (backup trigger)
- `workflow_dispatch`: Manual triggering for testing

**Backup Mechanism**: This workflow includes a `workflow_run` trigger that runs after successful deployments. This ensures posts get delivered even if the cron schedule experiences delays.

### 2. Deliver ActivityPub Accept Activities
**File**: `deliver-activitypub-accepts.yml`  
**Schedule**: Every 5 minutes (`*/5 * * * *`)  
**Purpose**: Processes and delivers pending Accept activities to new followers

**Triggers**:
- `schedule`: Every 5 minutes via cron
- `workflow_dispatch`: Manual triggering for testing

### 3. Process Read Later Items
**File**: `process-read-later.yml`  
**Schedule**: Daily at midnight UTC (`0 0 * * *`)  
**Purpose**: Processes read-later items from GitHub issues

### 4. Weekly Wrap-up
**File**: `weekly-wrapup.yml`  
**Schedule**: Weekly on Sundays at 11 PM UTC (`0 23 * * 0`)  
**Purpose**: Generates weekly content summaries

### 5. Check Broken Links
**File**: `check-broken-links.yml`  
**Schedule**: Weekly on Mondays at 8 AM UTC (`0 8 * * MON`)  
**Purpose**: Checks for broken links across the site

### 6. Statistics
**File**: `stats.yml`  
**Schedule**: Monthly on the 1st at 12:30 AM UTC (`30 0 1 * *`)  
**Purpose**: Generates monthly statistics

### 7. Test ActivityPub Deployment
**File**: `test-activitypub-deployment.yml`  
**Schedule**: Weekly on Mondays at 9 AM UTC (`0 9 * * 1`)  
**Purpose**: Health monitoring for ActivityPub endpoints

## Troubleshooting Scheduled Workflows

### Workflow Not Triggering

1. **Verify workflow is on default branch**: Check that the workflow file exists in `.github/workflows/` on the `main` branch
2. **Check recent commits**: Ensure there has been at least one push to `main` after the workflow file was added/modified
3. **Review syntax**: Validate cron expression at [crontab.guru](https://crontab.guru/)
4. **Check timing**: Convert UTC schedule to your local time to verify when the workflow should run
5. **Wait for delays**: GitHub may delay scheduled runs by several minutes
6. **Use manual trigger**: Test the workflow using `workflow_dispatch` to verify it works

### Activating a Modified Workflow

If you've modified a scheduled workflow and it's not updating:

```bash
# Make a trivial change to trigger the scheduler
git commit --allow-empty -m "Activate workflow scheduler"
git push origin main
```

### Monitoring Scheduled Workflows

- Check workflow runs in the **Actions** tab of the repository
- Filter by workflow name to see scheduled vs manual runs
- Look for the event trigger type in the workflow run details
- Set up notifications for failed workflow runs

## Additional Resources

- [GitHub Actions: Events that trigger workflows](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows)
- [GitHub Actions: Scheduled workflows](https://docs.github.com/en/actions/using-workflows/events-that-trigger-workflows#schedule)
- [Crontab Expression Validator](https://crontab.guru/)

## Summary of 2026-01-22 Fix

**Problem**: The `process-activitypub-deliveries.yml` workflow was not triggering automatically despite valid cron syntax.

**Root Cause**: The workflow file was updated on 2026-01-22 but no subsequent push occurred to activate the GitHub Actions scheduler.

**Solution**:
1. Updated workflow file with enhanced documentation
2. Added `workflow_run` backup trigger to ensure deliveries after deployment
3. Made commit and push to activate the scheduler
4. Created this documentation for future reference

**Expected Result**: Cron scheduler should now trigger the workflow every 5 minutes, with backup triggers ensuring delivery even during cron delays.
