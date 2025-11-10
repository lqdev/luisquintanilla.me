# Testing Read Later Cleanup Workflow

## Quick Start

### Automated Testing

Run the comprehensive test script:

```bash
./Scripts/test-read-later-cleanup.sh
```

This validates:
- ✅ Workflow YAML syntax
- ✅ Cleanup job structure and conditions
- ✅ Schedule and manual triggers
- ✅ 14-day filtering logic with mock data

### Manual GitHub Actions Testing

1. **Navigate to Actions**:
   - Go to your repository on GitHub
   - Click the "Actions" tab

2. **Select Workflow**:
   - Find "Process Read Later Issue" workflow in the list
   - Click on it

3. **Trigger Manually**:
   - Click "Run workflow" button (top right)
   - Select branch (usually `main`)
   - Click "Run workflow" to confirm

4. **Monitor Execution**:
   - Watch the workflow run appear in the list
   - Click on it to see job details
   - Check the `cleanup-old-entries` job

5. **Review Results**:
   - Check job summary for statistics
   - Look for PR creation if entries were removed
   - Verify auto-merge if PR was created

## Expected Behavior

### No Old Entries Scenario

If all entries in `Data/read-later.json` are less than 14 days old:

```
Original entry count: 6
Remaining entry count: 6
Entries removed: 0

✅ No old entries to remove
```

**Result**: No PR created, workflow completes successfully

### With Old Entries Scenario

If some entries are older than 14 days:

```
Original entry count: 10
Remaining entry count: 7
Entries removed: 3

✅ Removed 3 old entries
```

**Result**: 
- PR created with title "🧹 Clean up old read later entries"
- PR body includes removal statistics
- PR auto-merged if no conflicts
- Branch automatically deleted

## Testing with Mock Data

### Create Test Data

To test the cleanup with controlled data:

1. **Backup Current Data**:
   ```bash
   cp Data/read-later.json Data/read-later.json.backup
   ```

2. **Add Old Test Entry** (manually edit `Data/read-later.json`):
   ```json
   [
     {
       "url": "https://example.com/old-article",
       "title": "Old Test Article",
       "dateAdded": "2025-01-01T00:00:00Z"
     },
     {
       "url": "https://example.com/recent-article",
       "title": "Recent Test Article",
       "dateAdded": "2025-11-09T00:00:00Z"
     }
   ]
   ```

3. **Commit and Push**:
   ```bash
   git add Data/read-later.json
   git commit -m "Add test data for cleanup workflow"
   git push
   ```

4. **Trigger Workflow**:
   - Use manual trigger as described above

5. **Restore Backup** (if needed):
   ```bash
   cp Data/read-later.json.backup Data/read-later.json
   git add Data/read-later.json
   git commit -m "Restore read later data"
   git push
   ```

## Debugging

### Check Job Logs

1. Go to the workflow run in GitHub Actions
2. Click on the `cleanup-old-entries` job
3. Expand each step to see detailed logs:
   - Date calculations
   - Entry counts
   - Filtering results

### Common Issues

#### Workflow Doesn't Run on Schedule

- **Cause**: GitHub Actions schedules can have delays
- **Solution**: Use manual trigger (`workflow_dispatch`) for immediate testing

#### No PR Created When Expected

- **Check**: Verify entries are actually older than 14 days
- **Check**: Look at job logs for `has_changes` output
- **Check**: Ensure cutoff date calculation is correct

#### Auto-merge Fails

- **Cause**: Merge conflicts with main branch
- **Solution**: Manually resolve conflicts in the PR
- **Prevention**: Ensure main branch is up to date

## Customization

### Change Cleanup Period

To modify the 14-day period:

1. Edit `.github/workflows/process-read-later.yml`
2. Find line in `cleanup-old-entries` job:
   ```bash
   CUTOFF_DATE=$(date -u -d '14 days ago' +"%Y-%m-%dT%H:%M:%SZ")
   ```
3. Change `14 days ago` to desired period (e.g., `7 days ago`, `30 days ago`)
4. Update PR body text to reflect new period
5. Update documentation

### Change Schedule

To modify the cleanup frequency:

1. Edit `.github/workflows/process-read-later.yml`
2. Find the `schedule` section:
   ```yaml
   schedule:
     - cron: '0 0 * * *'  # Daily at midnight UTC
   ```
3. Modify cron expression:
   - `0 */6 * * *` - Every 6 hours
   - `0 12 * * *` - Daily at noon UTC
   - `0 0 * * 0` - Weekly on Sunday at midnight UTC

## Monitoring

### View Cleanup History

1. **GitHub PRs**:
   - Filter PRs by title: "🧹 Clean up old read later entries"
   - Review merge dates and statistics

2. **Workflow Runs**:
   - Check Actions tab for scheduled runs
   - Review job summaries for statistics

3. **Git History**:
   ```bash
   git log --all --grep="Clean up read later"
   ```

## Validation Checklist

Before deploying:

- [ ] Test script passes (`./Scripts/test-read-later-cleanup.sh`)
- [ ] YAML syntax is valid
- [ ] Manual workflow trigger works
- [ ] Cleanup logic filters correctly
- [ ] PR creation works when entries removed
- [ ] Auto-merge completes successfully
- [ ] Job summary displays statistics
- [ ] Documentation is updated

## Support

For issues or questions:
- Check workflow logs in GitHub Actions
- Review documentation in `docs/process-read-later-workflow.md`
- Run test script for validation: `./Scripts/test-read-later-cleanup.sh`
