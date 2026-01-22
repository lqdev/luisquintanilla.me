# Azure Static Web Apps Queue Trigger Fix

**Date**: January 21, 2026  
**Issue**: PR #1858 deployment failure - queue-triggered functions incompatible with Azure Static Web Apps  
**Solution**: GitHub Actions worker pattern (aligns with Phase 4A architecture)

## Problem

After merging PR #1858 (ActivityPub Phase 4B/4C), deployment failed with:

```
Error in processing api build artifacts: the file 'ProcessDelivery/function.json' has 
specified an invalid trigger of type 'queueTrigger' and direction 'in'.
Currently, only httpTriggers are supported.
```

**Root Cause**: Azure Static Web Apps (free tier) only supports HTTP-triggered Azure Functions, not queue-triggered functions.

## Solution Architecture

Converted to **GitHub Actions worker pattern** matching Phase 4A (deliver-accepts.yml):

### Before (Broken)
- `api/ProcessDelivery/` - Azure Function with queue trigger ❌
- Azure Queue automatically processes messages via function binding ❌

### After (Working)
- `api/scripts/process-delivery.js` - Node.js worker script ✅
- `.github/workflows/process-activitypub-deliveries.yml` - Scheduled GitHub Actions ✅
- Runs every 5 minutes via cron schedule ✅

## Implementation Details

### 1. Worker Script (`api/scripts/process-delivery.js`)
- **Pattern**: Based on `deliver-accepts.js` (Phase 4A proven pattern)
- **Queue Polling**: Uses enhanced `queueStorage.receiveMessages()` API
- **Message Processing**: 
  - Batch of up to 32 messages per run
  - HTTP signature generation via Azure Key Vault
  - SSRF protection for inbox URLs
  - Delivery status tracking in Azure Table Storage
- **Error Handling**:
  - Permanent failures (4xx except 429): Delete from queue
  - Temporary failures (5xx, network errors): Leave in queue for retry
  - Message visibility timeout: 300 seconds (5 minutes)

### 2. Enhanced Queue Storage Utilities (`api/utils/queueStorage.js`)
Added three new functions:
- `receiveMessages(queueName, maxMessages, visibilityTimeout)` - Poll queue for messages
- `deleteMessage(queueName, messageId, popReceipt)` - Remove processed messages
- Base64 decoding of message text for compatibility

### 3. GitHub Actions Workflow (`.github/workflows/process-activitypub-deliveries.yml`)
- **Trigger**: Scheduled cron (`*/5 * * * *`) + manual dispatch
- **Runtime**: Node.js 18 with Azure login
- **Environment**: 
  - `ACTIVITYPUB_STORAGE_CONNECTION` - Queue/Table storage
  - `KEY_VAULT_KEY_ID` - HTTP signature signing
- **Pattern**: Identical to `deliver-activitypub-accepts.yml`

### 4. Removed Incompatible Code
- Deleted `api/ProcessDelivery/` directory entirely
- Updated deployment workflow comments to reflect new architecture

### 5. Integration Flow
```
New Post Published
    ↓
Build & Deploy (publish-azure-static-web-apps.yml)
    ↓
Trigger ActivityPub Post Delivery Step
    ↓
Call QueueDeliveryTasks HTTP endpoint
    ↓
Messages added to 'activitypub-delivery' queue
    ↓
GitHub Actions cron (every 5 minutes)
    ↓
process-delivery.js polls queue
    ↓
Process batch of 32 messages
    ↓
Deliver to follower inboxes with HTTP signatures
    ↓
Update delivery status in Azure Table Storage
    ↓
Delete successful/permanent-fail messages from queue
```

## Benefits

1. **Azure Static Web Apps Compatible** - Uses only HTTP functions
2. **Proven Architecture** - Matches working Phase 4A pattern
3. **Cost Effective** - No Azure Functions consumption costs
4. **Reliable** - GitHub Actions provides robust scheduling
5. **Scalable** - Processes 32 messages per run (288 per hour)
6. **Observable** - Full logging in GitHub Actions runs
7. **Testable** - Manual workflow_dispatch trigger for testing

## Testing Plan

1. **Local Testing**:
   ```bash
   cd api
   npm install
   node scripts/process-delivery.js
   ```

2. **Deployment Validation**:
   - Commit and push changes
   - Monitor deployment workflow
   - Verify no queue trigger errors

3. **End-to-End Testing**:
   - Publish new content
   - Verify QueueDeliveryTasks queues messages
   - Wait 5 minutes for cron trigger
   - Check GitHub Actions run logs
   - Verify delivery status in Azure Table Storage

## Files Changed

**Created**:
- `api/scripts/process-delivery.js` (428 lines)
- `.github/workflows/process-activitypub-deliveries.yml` (44 lines)

**Modified**:
- `api/utils/queueStorage.js` - Added receiveMessages, deleteMessage functions
- `.github/workflows/publish-azure-static-web-apps.yml` - Updated comments

**Deleted**:
- `api/ProcessDelivery/index.js` (279 lines)
- `api/ProcessDelivery/function.json` (14 lines)

## Success Metrics

- ✅ Deployment succeeds without queue trigger errors
- ✅ Scheduled workflow runs every 5 minutes
- ✅ Messages successfully polled from queue
- ✅ Activities delivered to follower inboxes
- ✅ Delivery status tracked in table storage
- ✅ Zero Azure Functions consumption charges

## Rollback Plan

If issues arise:
1. Revert this commit
2. Consider Azure Functions upgrade to support queue triggers
3. Or implement HTTP polling pattern with longer intervals

## Related

- **Working Pattern**: `.github/workflows/deliver-activitypub-accepts.yml` (Phase 4A)
- **Original PR**: #1858 (ActivityPub Phase 4B/4C)
- **Error Logs**: GitHub Actions run #21214518998
