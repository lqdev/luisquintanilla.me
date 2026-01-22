# ActivityPub Followers & Delivery Fix Summary

**Branch**: `activitypub-followers-investigation`  
**Date**: January 21, 2026

## 🔍 Problems Identified

### 1. ✅ Followers Table Empty (Partially Fixed)
**Problem**: The `followers` table appeared empty despite Accept activities being delivered.

**Root Cause**: Found via diagnostic that 2 followers DO exist:
- `https://toot.lqdev.tech/users/lqdev` (Accept delivered successfully)
- `https://toot.lqdev.tech/users/luis` (Accept failed to deliver)

**Status**: ✅ **Followers ARE working!** The inbox code correctly adds followers before queueing accepts.

### 2. ⚠️ One Failed Accept Activity
**Problem**: One follow from `@luis@toot.lqdev.tech` has status "failed" in pendingaccepts table.

**Impact**: This user won't receive posts until the Accept is successfully delivered.

**Next Step**: Debug why that specific Accept failed and retry manually.

### 3. 🚨 **CRITICAL**: No Post Delivery System
**Problem**: When you publish new content (like PR #1870), the ActivityPub builder generates static JSON files but **NEVER queues posts for delivery** to followers.

**Impact**: 
- Your 2 followers never received the PR #1870 response post
- `deliverystatus` table is completely empty
- Post delivery workflow finds 0 items to process
- Followers can see posts in your outbox but don't get push notifications

## 🔧 Fixes Implemented

### 1. Added Post Delivery Queue System

**File**: `api/utils/tableStorage.js`
- ✅ Added `deliveryqueue` table support
- ✅ Added `queuePostDelivery(noteId, createActivity)` function
- ✅ Added `getPendingDeliveries()` function
- ✅ Added `markDeliveryCompleted/Failed()` functions

### 2. Updated Delivery Worker

**File**: `api/scripts/process-delivery.js`
- ✅ Switched from Azure Queue Storage to Table Storage
- ✅ Now fetches pending deliveries from `deliveryqueue` table
- ✅ Gets all followers and delivers to each one
- ✅ Marks deliveries as completed after successful distribution

### 3. Created Queue Helper Script

**File**: `api/scripts/queue-post-delivery.js`
- ✅ Node.js script callable from F# build
- ✅ Accepts noteId and Create activity JSON path
- ✅ Queues post for delivery to followers

### 4. Integrated Queueing into Build

**File**: `ActivityPubBuilder.fs`
- ✅ Added `queueRecentPostsForDelivery()` function
- ✅ Queues posts from last 24 hours only (prevents re-delivering old content)
- ✅ Calls Node.js script to queue each recent post
- ✅ Gracefully handles missing ACTIVITYPUB_STORAGE_CONNECTION

**File**: `Program.fs`
- ✅ Added call to `queueRecentPostsForDelivery()` after outbox generation

## 📊 Current State

### Tables Status (From Diagnostic)
- **followers**: 2 entries ✅
- **pendingaccepts**: 8 entries (7 delivered, 1 failed) ⚠️
- **deliverystatus**: 0 entries (will populate after first delivery) 
- **deliveryqueue**: 0 entries (will populate on next build)

### Workflows Status
- **deliver-activitypub-accepts.yml**: ✅ Running (processes Accept queue)
- **process-activitypub-deliveries.yml**: ✅ Running (will now process delivery queue)

## 🚀 Testing Plan

### Phase 1: Test Post Queueing
1. Set `ACTIVITYPUB_STORAGE_CONNECTION` env var locally
2. Run the build with `dotnet run`
3. Verify posts from last 24 hours get queued
4. Check `deliveryqueue` table has entries

### Phase 2: Test Manual Delivery
1. Manually trigger `process-activitypub-deliveries` workflow
2. Verify posts get delivered to 2 followers
3. Check `deliverystatus` table for delivery records
4. Check Mastodon UI to see if posts appear

### Phase 3: Test End-to-End Flow
1. Create new response post via GitHub issue
2. Build should queue post automatically
3. Worker should deliver within 5 minutes
4. Verify followers receive the post

### Phase 4: Fix Failed Accept
1. Debug why `@luis` Accept failed
2. Manually retry or delete and re-follow
3. Verify Accept delivers successfully

## 🎯 Expected Behavior After Fix

### When You Publish New Content:
1. **F# Build runs** → Generates ActivityPub JSON files
2. **Queueing happens** → Recent posts added to `deliveryqueue` table
3. **Worker triggers** → Every 5 minutes via GitHub Actions
4. **Deliveries occur** → Posts delivered to all 2 followers
5. **Status tracked** → `deliverystatus` table shows delivery results

### What Your Followers Will See:
- New posts appear in their Mastodon home timeline
- They get push notifications for your posts
- Posts show proper attribution and formatting
- Links back to your site work correctly

## 🐛 Remaining Issues

### 1. Failed Accept for @luis
- **Status**: Failed with 1 retry
- **Action Needed**: Debug and retry or re-follow
- **Impact**: That user won't receive posts

### 2. No Retroactive Delivery
- **Limitation**: Old posts (>24 hours) won't be queued
- **Reason**: Prevents spamming followers with hundreds of old posts
- **Workaround**: Could manually queue specific posts if needed

### 3. No Delivery Confirmation UI
- **Limitation**: Can't easily see delivery status without querying Azure
- **Future Enhancement**: Add delivery dashboard or logging page

## 📝 Environment Requirements

### Local Development:
```bash
$env:ACTIVITYPUB_STORAGE_CONNECTION = "..."  # Azure Table Storage connection string
```

### GitHub Actions (Already Configured):
- ✅ `ACTIVITYPUB_STORAGE_CONNECTION` secret set
- ✅ `KEY_VAULT_KEY_ID` secret set for signing
- ✅ `AZURE_CREDENTIALS` secret set for Azure login

## 🎉 Success Criteria

- [ ] Build completes without errors
- [ ] New posts get queued automatically
- [ ] Delivery worker processes queue
- [ ] Followers receive posts
- [ ] No duplicate deliveries
- [ ] Failed Accept resolved

## 📚 Files Modified

### Core Implementation:
- `api/utils/tableStorage.js` - Added delivery queue functions
- `api/scripts/process-delivery.js` - Updated to use table queue
- `api/scripts/queue-post-delivery.js` - **NEW** queueing script
- `ActivityPubBuilder.fs` - Added queueing integration
- `Program.fs` - Added queueing call

### Diagnostic Tools:
- `api/scripts/diagnose-table-storage.js` - **EXISTING** diagnostic script
- `ACTIVITYPUB_DELIVERY_INVESTIGATION.md` - Investigation findings

## ⚡ Next Steps

1. **Commit and push changes** to `activitypub-followers-investigation` branch
2. **Create PR** with these fixes
3. **Test locally** with small post to verify queueing
4. **Merge to main** after testing
5. **Monitor first delivery** via workflow logs
6. **Verify on Mastodon** that posts arrive
7. **Debug failed Accept** for @luis user

## 💡 Key Learnings

1. **Diagnostic scripts are essential** - Without `diagnose-table-storage.js`, we wouldn't have found followers DO exist
2. **Missing features can look like bugs** - The real issue was missing delivery queueing, not broken Follow/Accept
3. **Table Storage works well for queues** - Simpler than Azure Queue Storage for this use case
4. **Build-time integration is key** - Queueing at build time ensures all new content gets delivered

---

**Status**: ✅ Ready for testing and deployment
