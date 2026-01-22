# ActivityPub Delivery Investigation

**Branch**: `activitypub-followers-investigation`  
**Date**: January 21, 2026  
**Status**: Critical issues identified

## Problem Statement

1. **No followers showing** in database table or Mastodon UI despite previous confirmation
2. **PR #1870 response post** (reshare about Android phone) not delivered to followers
3. **Accept activities** appear to be queued but show 0 pending
4. **Post deliveries** show 0 in queue

## Investigation Findings

### Workflow Status (✅ Working)
Both GitHub Actions workflows are running successfully every 5 minutes:
- `deliver-activitypub-accepts.yml` - ✅ Running but finding 0 pending accepts
- `process-activitypub-deliveries.yml` - ✅ Running but finding 0 delivery queue messages

### Critical Issue #1: Missing Post Delivery Queueing

**Problem**: `ActivityPubBuilder.fs` generates static ActivityPub files but **NEVER queues posts for delivery** to followers.

**What exists**:
```fsharp
// ActivityPubBuilder.fs
let buildNotes (unifiedItems) (outputDir) =
    // ✅ Generates individual note JSON files
    // ❌ Does NOT queue for delivery

let buildOutbox (unifiedItems) (outputDir) =
    // ✅ Generates outbox.json
    // ❌ Does NOT queue for delivery
```

**What's missing**:
- No function to queue Create activities to delivery queue/table
- No integration between build process and delivery system
- Posts generate static files but never reach followers

**Expected flow**:
1. Build generates ActivityPub notes ✅
2. Build should queue notes for delivery ❌ **MISSING**
3. Worker processes queue and delivers ✅ (but queue is empty)

### Critical Issue #2: No Followers Data

**Problem**: Followers table appears to be empty, but we confirmed it was working previously.

**Possible causes**:
1. Followers were never actually stored (Accept delivery failed silently)
2. Table was cleared/reset
3. Connection string issue preventing reads
4. Different storage account being used

**Evidence needed**:
- Direct Azure Portal check of `followers` table
- Check `pendingaccepts` table for queued accepts
- Verify storage account connection string
- Check Azure Function logs for Follow processing

### Critical Issue #3: Accept Activity Flow

**Follow → Accept Flow Status**:
1. Inbox receives Follow ✅ (logs show activities received)
2. Follower added to table ✅ (code exists in `inbox/index.js`)
3. Accept queued ✅ (code calls `tableStorage.queueAcceptActivity`)
4. Worker finds 0 pending ❌ **Something wrong**
5. Accept never delivered ❌

**Questions**:
- Are Accepts actually being added to `pendingaccepts` table?
- Is there a table name mismatch?
- Is the query filter working correctly?
- Are old accepts being left in 'delivered' state?

## Files Examined

### Core Implementation Files
- `api/inbox/index.js` - Processes Follow activities, queues Accepts
- `api/followers/index.js` - Builds followers collection from table
- `api/utils/tableStorage.js` - Table storage operations
- `ActivityPubBuilder.fs` - Generates static ActivityPub files
- `.github/workflows/deliver-activitypub-accepts.yml` - Delivers queued accepts
- `.github/workflows/process-activitypub-deliveries.yml` - Delivers posts

### Workflow Logs Analyzed
- Accept delivery workflow (21233960285): "Found 0 pending Accept activities"
- Post delivery workflow (21233414364): "Found 0 messages in delivery queue"

## Missing Components

### 1. Post Delivery Queue System
**Need to create**:
- `tableStorage.queuePostDelivery(noteId, createActivity)` function
- Integration in `ActivityPubBuilder.fs` to call queue function
- Build step to queue ALL new posts after static file generation

### 2. Investigation Tools
**Need to create**:
- Script to directly query Azure Table Storage
- Script to list followers from table
- Script to list pending accepts from table
- Script to manually test Follow → Accept → Delivery flow

### 3. Monitoring & Debugging
**Need to add**:
- Logging in ActivityPubBuilder for delivery queueing
- Storage table query result logging in workers
- Accept delivery success/failure tracking
- Post delivery success/failure tracking

## ✅ Phase 1 Complete: Diagnosis & Immediate Fix

### Diagnostic Results
**Created**: `api/scripts/diagnose-table-storage.js` - Complete table inspection tool
**Created**: `api/scripts/fix-followers-table.js` - Follower data recovery tool

**Table State (Before Fix)**:
- ✅ `followers`: **0 entries** (EMPTY - Critical issue!)
- ⚠️  `pendingaccepts`: 8 entries (1 failed, 7 delivered)
- ⚠️  `deliverystatus`: **0 entries** (EMPTY - No posts delivered)

**Table State (After Fix)**:
- ✅ `followers`: **2 entries**
  - `https://toot.lqdev.tech/users/lqdev` (lqdev) - ✅ Followed 2026-01-20
  - `https://toot.lqdev.tech/users/luis` (no inbox) - ⚠️  Accept failed
- ⚠️  `pendingaccepts`: 8 entries (need cleanup)
- ⚠️  `deliverystatus`: 0 entries (still no posts delivered)

### Root Cause Identified

**Problem 1: Followers Table Was Empty**
- Inbox endpoint calls `tableStorage.addFollower()` ✅
- But followers table was empty ❌
- **Fix Applied**: Reconstructed from `pendingaccepts` history
- **Status**: ✅ **FIXED** - 2 followers recovered

**Problem 2: Post Delivery Never Implemented**
- `ActivityPubBuilder.fs` generates static ActivityPub files ✅
- But **NEVER queues posts for delivery** ❌
- No integration between build and delivery workers
- **Status**: ⚠️  **NEEDS IMPLEMENTATION**

**Problem 3: Accept Delivery Worker Confusion**
- Worker shows "Found 0 pending Accept activities"
- But `pendingaccepts` table has 8 entries
- Query filter: `PartitionKey eq 'pending' and status eq 'pending'`
- All accepts are marked 'delivered' or 'failed', not 'pending'
- **Status**: ⚠️  **NEEDS FIX** - Filter logic issue

## Next Steps

### Phase 2: Fix Accept Delivery Worker 🔧
**Priority**: High (Accept delivery broken)

1. **Fix pending accepts query** in `api/scripts/deliver-accepts.js`
   - Current filter only finds status='pending'
   - All accepts are marked 'delivered' or 'failed'
   - Need to either:
     - Change query to only fetch status='pending' (correct approach)
     - Fix code to set status='pending' initially (current approach is wrong)

2. **Clean up delivered accepts** from `pendingaccepts` table
   - 7 entries marked 'delivered' should be removed or archived
   - Only truly pending accepts should remain

3. **Re-test Follow → Accept flow**
   - Send new Follow from Mastodon
   - Verify Accept is queued with status='pending'
   - Verify worker picks it up and delivers
   - Verify Accept reaches follower

### Phase 3: Implement Post Delivery 🚀
**Priority**: Critical (Feature completely missing)

1. **Add post queueing to tableStorage.js**
```javascript
/**
 * Queue a post for delivery to all followers
 * @param {string} noteId - ActivityPub Note ID
 * @param {object} createActivity - Create activity to deliver
 * @returns {Promise<string>} Queue message ID
 */
async function queuePostDelivery(noteId, createActivity) {
    // Use deliverystatus table to track per-follower delivery
    // OR create new postdeliveryqueue table
}
```

2. **Integrate with ActivityPubBuilder.fs**
```fsharp
/// Queue new posts for delivery after build
let queuePostsForDelivery (notes: ActivityPubNote list) : unit =
    // Call Node.js script or Azure SDK to queue posts
    // Pass note IDs and Create activities to delivery system
```

3. **Update process-delivery.js worker**
   - Currently finds 0 messages because nothing is queued
   - Implement logic to:
     - Fetch pending post deliveries
     - Get all followers from table
     - Deliver to each follower inbox
     - Track delivery status

4. **Test end-to-end**
   - Build site with new post
   - Verify post is queued for delivery
   - Verify worker processes queue
   - Verify followers receive post
   - Check deliverystatus table for results

### Phase 4: Testing & Monitoring ✅
1. Complete Follow → Accept → Post cycle test
2. Verify with multiple Mastodon accounts
3. Monitor delivery logs for 24 hours
4. Create dashboard for delivery status

## Questions for User

1. **Did you test Follow from Mastodon recently?** When was it last confirmed working?
2. **Can you access Azure Portal** to check the actual table data?
3. **Do you have multiple storage accounts?** Could there be a mismatch?
4. **Were there any Azure resource changes** (resets, deletions, recreations)?

## Technical Debt Identified

1. **No post delivery queue implementation** - Critical missing feature
2. **No direct table query tools** - Makes debugging very hard
3. **No delivery monitoring dashboard** - Can't see what's happening
4. **Silent failures possible** - Accepts/posts could fail without visibility
5. **No retry logic for failed deliveries** - Posts could be lost forever

## References

- PR #1870: Reshare response that should have been delivered
- PR #1871: Documentation PR (not relevant to delivery)
- Table Storage: `followers`, `pendingaccepts`, `deliverystatus`
- GitHub Actions: 5-minute cron schedule for both workflows
