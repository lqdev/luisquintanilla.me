# Phase 4A-5: Testing & Validation Guide

**Status**: Ready for Execution  
**Prerequisites**: All Phase 4A-1 through 4A-4 components complete  
**Estimated Duration**: 2-3 hours

## 📋 Overview

Phase 4A-5 validates the complete ActivityPub follower management workflow with end-to-end testing using a real Mastodon account.

### Success Criteria
- [ ] Follow request successfully processed and stored in Table Storage
- [ ] Accept activity delivered to follower inbox with valid HTTP signature
- [ ] Follower appears in static followers.json after build
- [ ] Unfollow (Undo) request removes follower from Table Storage
- [ ] Idempotent processing prevents duplicate follows
- [ ] All logs appear correctly in Application Insights

## 🧪 Test Plan

### Test 1: Basic Follow Workflow

**Objective**: Verify complete Follow → Accept → followers.json flow

**Steps**:
1. **Initiate Follow from Mastodon**:
   - Login to your Mastodon test account
   - Search for `@lqdev@lqdev.me`
   - Click "Follow" button
   - Mastodon sends POST to `https://lqdev.me/api/activitypub/inbox`

2. **Verify Inbox Handler Processing**:
   ```bash
   # Check Application Insights logs
   az monitor app-insights query \
     --app <app-insights-name> \
     --analytics-query "traces | where message contains 'Follow' | order by timestamp desc | take 20"
   ```
   
   Expected log entries:
   - `Received Follow activity from [actor]`
   - `HTTP signature verified successfully`
   - `Follower added to Table Storage: [actor]`
   - `Queued Accept activity for delivery to [inbox]`

3. **Verify Table Storage**:
   ```bash
   # Run test script to check follower was added
   cd api
   node test-table-storage.js
   ```
   
   Expected: Follower appears in list with correct actor URL, inbox, and timestamp

4. **Verify Accept Delivery**:
   - Check Application Insights for ProcessAccept function logs
   - Expected: `Accept delivered successfully: 200`
   - Verify on Mastodon: You should now be following @lqdev@lqdev.me

5. **Verify Static File Generation**:
   ```bash
   # Rebuild site
   cd /home/runner/work/luisquintanilla.me/luisquintanilla.me
   dotnet run
   
   # Check followers.json
   cat _public/api/data/followers.json
   ```
   
   Expected: File contains your Mastodon actor URL in `orderedItems` array

### Test 2: Idempotent Processing

**Objective**: Verify duplicate Follow requests don't create multiple entries

**Steps**:
1. From Mastodon, unfollow @lqdev@lqdev.me
2. Follow @lqdev@lqdev.me again (second Follow request)
3. Verify Table Storage has only one entry for your actor
4. Check Application Insights for idempotency message: `Follow activity already processed`

### Test 3: Unfollow Workflow

**Objective**: Verify Undo (unfollow) removes follower from Table Storage

**Steps**:
1. From Mastodon, unfollow @lqdev@lqdev.me
2. Mastodon sends Undo activity to inbox
3. Verify Application Insights logs:
   - `Received Undo activity from [actor]`
   - `Follower removed from Table Storage: [actor]`
4. Verify Table Storage: Follower should be removed
5. Rebuild site and verify followers.json no longer contains your actor

### Test 4: HTTP Signature Verification

**Objective**: Verify inbox rejects requests without valid signatures

**Steps**:
1. Send test POST to inbox without signature:
   ```bash
   curl -X POST https://lqdev.me/api/activitypub/inbox \
     -H "Content-Type: application/activity+json" \
     -d '{"type":"Follow","actor":"https://example.com/users/test"}'
   ```
2. Expected: HTTP 401 Unauthorized
3. Application Insights should log: `HTTP signature verification failed`

### Test 5: Accept Delivery with HTTP Signature

**Objective**: Verify Accept activities are signed correctly

**Steps**:
1. Follow from Mastodon
2. Check remote server logs (if accessible) or use packet capture
3. Verify Accept activity includes HTTP Signature header
4. Verify signature components: (request-target), host, date, digest

### Test 6: Queue Retry Logic

**Objective**: Verify automatic retry on delivery failures

**Steps**:
1. Temporarily break Accept delivery (modify target inbox URL in queue message)
2. Observe Azure Queue retry behavior:
   - Message becomes visible again after visibility timeout
   - ProcessAccept function retries delivery
   - After 5 attempts, message moves to poison queue
3. Check Application Insights for retry logs

## 🔍 Validation Checklist

### Azure Resources
- [ ] Storage Account `lqdevactivitypub` exists and accessible
- [ ] Table `followers` exists and has correct schema
- [ ] Queue `accept-delivery` exists and messages are processed
- [ ] Application Insights receiving logs from all functions

### API Endpoints
- [ ] `GET /.well-known/webfinger` returns correct actor reference
- [ ] `GET /api/activitypub/actor` returns actor profile with public key
- [ ] `POST /api/activitypub/inbox` accepts Follow activities
- [ ] `GET /api/activitypub/followers` returns followers collection
- [ ] `GET /api/data/followers.json` serves static followers file

### Data Flow
- [ ] Follow request → Table Storage (follower added)
- [ ] Follow request → Queue (Accept queued)
- [ ] Queue → ProcessAccept (Accept delivered with signature)
- [ ] Build → Static File (followers.json regenerated)
- [ ] Undo request → Table Storage (follower removed)

### Security
- [ ] HTTP signature verification on incoming requests
- [ ] HTTP signature generation on outgoing Accept activities
- [ ] Key Vault integration working correctly
- [ ] No credentials exposed in logs

## 📊 Expected Metrics

After successful testing:
- **Table Storage**: 1+ followers (depending on test accounts)
- **Queue Messages**: 0 pending (all processed)
- **Application Insights**:
  - Inbox handler invocations: 2+ (Follow + Undo)
  - ProcessAccept invocations: 1+ (one per Follow)
  - Success rate: 100%
  - Average processing time: < 2 seconds

## 🐛 Troubleshooting

### Issue: Follow not appearing in Table Storage

**Possible Causes**:
- HTTP signature verification failed
- ACTIVITYPUB_STORAGE_CONNECTION not configured
- Table Storage permissions issue

**Debug Steps**:
1. Check Application Insights for error messages
2. Verify environment variable is set correctly
3. Test Table Storage connection with test-table-storage.js

### Issue: Accept not delivered to follower

**Possible Causes**:
- Queue message malformed
- Key Vault access denied
- Remote inbox URL incorrect

**Debug Steps**:
1. Check Application Insights for ProcessAccept errors
2. Verify queue message format
3. Test Key Vault access manually

### Issue: followers.json not updated

**Possible Causes**:
- Build process not running
- ACTIVITYPUB_STORAGE_CONNECTION not set in build environment
- FollowersSync.fs not integrated correctly

**Debug Steps**:
1. Run `dotnet run` and check console output
2. Look for "Building ActivityPub followers collection" message
3. Verify `_public/api/data/followers.json` was created

## 📝 Documentation Updates

After successful testing, update:
- [ ] `docs/activitypub/implementation-status.md` - Mark Phase 4A as complete
- [ ] `docs/activitypub/phase4-implementation-plan.md` - Update progress
- [ ] `api/ACTIVITYPUB.md` - Document any API changes
- [ ] `projects/active/phase-4a-inbox-handler.md` - Final status update

## 🎯 Success Criteria Summary

Phase 4A-5 is complete when:
1. ✅ All 6 tests pass successfully
2. ✅ Validation checklist 100% complete
3. ✅ No errors in Application Insights
4. ✅ Documentation updated
5. ✅ Lessons learned captured
6. ✅ Ready to proceed to Phase 4B (Post Delivery)

## 📚 Reference Materials

- **ActivityPub Spec**: https://www.w3.org/TR/activitypub/
- **HTTP Signatures**: https://www.rfc-editor.org/rfc/rfc9421
- **Azure Table Storage Docs**: https://learn.microsoft.com/en-us/azure/storage/tables/
- **Azure Queue Storage Docs**: https://learn.microsoft.com/en-us/azure/storage/queues/

---

**Next Phase**: Phase 4B - Post Delivery (After Phase 4A-5 completion)
