# GitHub Actions Workflow Optimization

## Overview
This document explains the optimization made to the `publish-azure-static-web-apps.yml` workflow to decouple post-deployment notification jobs.

## Problem Statement
Previously, the workflow had a sequential structure where:
1. Build and Deploy executed
2. Send Webmentions executed (blocked if previous failed)
3. Queue ActivityPub Posts executed (blocked if previous failed)

This meant if webmentions failed, ActivityPub queuing would never run, even though they are independent notification mechanisms.

## Solution: Job Parallelization

### New Architecture
The workflow now uses **3 independent jobs** with proper dependency management:

```
build_and_deploy_job
        ├─> send_webmentions_job (parallel)
        └─> queue_activitypub_job (parallel)
```

### Job Breakdown

#### 1. `build_and_deploy_job` (Main Job)
**Responsibilities:**
- Checkout repository
- Setup .NET SDK 10.x
- Restore dependencies
- Build F# project
- Generate static website
- Sync ActivityPub data
- Deploy to Azure Static Web Apps
- **Upload artifacts** for downstream jobs

**Artifacts Uploaded:**
- `webmention-artifacts`: Contains source files and compiled binaries needed for webmention processing
  - `_src/responses/` - Response markdown files
  - `bin/Debug/net10.0/PersonalSite.dll` - Compiled assembly
  - `bin/Debug/net10.0/*.deps.json` - Dependency manifests
  - `bin/Debug/net10.0/*.runtimeconfig.json` - Runtime configuration

- `activitypub-artifacts`: Contains generated data for ActivityPub delivery
  - `_public/api/data/outbox/index.json` - Outbox feed with recent posts

#### 2. `send_webmentions_job` (Independent)
**Responsibilities:**
- Checkout repository (for Scripts/)
- Setup .NET SDK 10.x
- Download `webmention-artifacts`
- Execute `Scripts/send-webmentions.fsx`

**Dependencies:**
- **Needs:** `build_and_deploy_job` (must complete successfully)
- **Independent of:** `queue_activitypub_job` (runs in parallel)

**Conditions:**
- Runs on all trigger types (push, workflow_dispatch)
- No branch restrictions

**What it does:**
- Loads response content from `_src/responses/`
- Uses compiled `PersonalSite.dll` to process responses
- Sends webmentions to sites referenced in responses
- Notifies other IndieWeb sites of new content

#### 3. `queue_activitypub_job` (Independent)
**Responsibilities:**
- Download `activitypub-artifacts`
- Parse `outbox/index.json` to get 2 most recent posts
- Queue posts to Azure Table Storage for federated delivery
- Use Azure REST API for queuing

**Dependencies:**
- **Needs:** `build_and_deploy_job` (must complete successfully)
- **Independent of:** `send_webmentions_job` (runs in parallel)

**Conditions:**
- Only runs on: `push` events to `main` branch
- Skipped for: `workflow_dispatch` and pull requests

**What it does:**
- Extracts recent Create activities from outbox
- Generates queue entries with timestamps and IDs
- Inserts into Azure Table Storage `deliveryqueue`
- Scheduled workers (separate workflow) handle actual delivery

## Key Benefits

### 1. **Parallelization**
Both notification jobs run simultaneously after deployment completes, reducing total workflow time.

### 2. **Fault Isolation**
If webmentions fail, ActivityPub delivery still runs. Each job's success/failure is independent.

### 3. **Clear Observability**
Each job has its own:
- Status indicator (✅/❌)
- Execution logs
- Error messages
- Timing information

### 4. **Efficient Artifact Sharing**
Artifacts are uploaded once and downloaded only by jobs that need them:
- Webmentions job gets source files + binaries
- ActivityPub job gets only the outbox JSON
- Artifacts auto-expire after 1 day (no storage waste)

### 5. **Conditional Execution**
ActivityPub queuing only runs on production deployments (main branch pushes), while webmentions run on all deployments.

## Resiliency Features

### Job-Level Failure Handling
- If `send_webmentions_job` fails: `queue_activitypub_job` still runs
- If `queue_activitypub_job` fails: `send_webmentions_job` still runs
- Both jobs show independent status in GitHub Actions UI

### Graceful Degradation
- If no recent items in outbox: ActivityPub job exits with success (exit 0)
- If webmention sending fails: Logged but doesn't fail the deployment
- Each job logs its progress for debugging

### Retry Capability
Because jobs are independent, you can:
- Re-run only the failed job from GitHub Actions UI
- Trigger the entire workflow again without redeploying

## Workflow Execution Flow

### Scenario 1: Main Branch Push
```
push to main
    ↓
build_and_deploy_job ✅
    ├─> send_webmentions_job ✅ (parallel)
    └─> queue_activitypub_job ✅ (parallel)
```

### Scenario 2: Manual Workflow Dispatch
```
workflow_dispatch
    ↓
build_and_deploy_job ✅
    ├─> send_webmentions_job ✅ (parallel)
    └─> queue_activitypub_job ⊘ (skipped - not main push)
```

### Scenario 3: Webmention Failure
```
push to main
    ↓
build_and_deploy_job ✅
    ├─> send_webmentions_job ❌ (failed - but doesn't block others)
    └─> queue_activitypub_job ✅ (still runs successfully)
```

## Artifact Details

### Why Artifacts?
Jobs run in separate runner instances with fresh filesystems. Artifacts enable sharing data between jobs without rebuilding.

### Artifact Retention
- **Retention Period:** 1 day
- **Rationale:** These are ephemeral notification artifacts, not build artifacts for download
- **Storage Impact:** Minimal (~100KB for webmention artifacts, ~10KB for ActivityPub data)

### Artifact Download Paths
- **Webmentions:** Downloads to workspace root (matching build layout)
- **ActivityPub:** Downloads to `_public/api/data/outbox/` (matching script expectations)

## Testing the Workflow

### Validation Performed
✅ YAML syntax validation (Python yaml.safe_load)
✅ Job dependency graph verified
✅ Artifact upload/download paths confirmed
✅ Conditional logic preserved
✅ Environment variables and secrets maintained

### Recommended Testing
1. **Full workflow test:** Push to main branch
   - Verify all 3 jobs complete successfully
   - Check parallel execution in Actions UI
   - Confirm webmentions are sent
   - Verify posts are queued in Azure Table Storage

2. **Failure isolation test:** Temporarily break webmention script
   - Verify webmentions job fails
   - Confirm ActivityPub job still succeeds
   - Check deployment remains successful

3. **Conditional execution test:** Trigger `workflow_dispatch`
   - Verify webmentions job runs
   - Confirm ActivityPub job is skipped
   - Check conditional logic works correctly

## Migration Notes

### Breaking Changes
**None** - This is a refactoring of job structure, not functionality changes.

### Behavior Changes
- Jobs now run in **parallel** instead of sequentially
- Total workflow time should **decrease** by ~30-60 seconds
- Job failures are now **isolated** (one can fail without blocking the other)

### No Changes To
- Build process
- Deployment mechanism
- Webmention sending logic
- ActivityPub queuing logic
- Secret/environment variable usage
- Trigger conditions

## Troubleshooting

### If Webmentions Job Fails
**Check:**
1. Artifact download succeeded
2. `_src/responses/` contains markdown files
3. `PersonalSite.dll` exists in workspace
4. .NET SDK 10.x is available
5. Script execution logs for specific errors

### If ActivityPub Job Fails
**Check:**
1. Artifact download succeeded
2. `outbox/index.json` exists and is valid JSON
3. `ACTIVITYPUB_STORAGE_CONNECTION` secret is configured
4. Azure Table Storage `deliveryqueue` exists
5. Recent items exist in outbox (check logs)

### If Artifacts Upload Fails
**Check:**
1. Paths exist in build job workspace
2. Build completed successfully before artifact upload
3. actions/upload-artifact@v4 is functioning
4. GitHub Actions storage quota not exceeded

## Future Enhancements

### Potential Optimizations
1. **Caching:** Add .NET dependency caching to speed up webmentions job
2. **Failure Notifications:** Add Slack/email notifications for job failures
3. **Metrics:** Track timing and success rates for each job
4. **Conditional Artifacts:** Only upload artifacts if downstream jobs will run

### Monitoring Recommendations
- Track job duration trends (should be lower after parallelization)
- Monitor failure patterns (are failures isolated or correlated?)
- Review artifact storage usage (should be minimal with 1-day retention)

## References

- **GitHub Actions Documentation:** https://docs.github.com/en/actions
- **Job Dependencies:** https://docs.github.com/en/actions/using-workflows/workflow-syntax-for-github-actions#jobsjob_idneeds
- **Artifacts:** https://docs.github.com/en/actions/using-workflows/storing-workflow-data-as-artifacts
- **Workflow Visualization:** Available in Actions tab → Select workflow run → View graph

---

**Implementation Date:** 2025-01-24  
**Implemented By:** Issue Publisher Agent (GitHub Actions Specialist)  
**Related Issue:** Workflow Optimization - Decouple Post-Deployment Jobs
