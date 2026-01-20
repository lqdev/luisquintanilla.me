# ActivityPub Follow/Accept Investigation

**Created**: 2026-01-19  
**Status**: Active Investigation  
**Branch**: `investigate-activitypub-follow-accept`

## 🎯 Executive Summary

**Root Cause Identified**: ActivityPub Follow/Accept functionality is 90% working but failing at the final step due to **missing cryptographic key configuration**.

**Critical Missing Configuration**:
1. ❌ Code architecture doesn't match Free tier capabilities
2. ❌ Functions are trying to sign outgoing activities (requires Standard tier)
3. ✅ Key exists in Key Vault but Functions on Free tier can't access it

**Impact**: Follow requests are received and stored, but Accept activities cannot be delivered back to remote servers because the Azure Function is trying to sign them (which requires managed identity support only available in Standard tier $9/month).

**Fix Complexity**: Medium - Code changes needed to remove signing from Functions  
**Estimated Fix Time**: 30 minutes

---

## Problem Statement

### Issue 1: Follow Activities Not Working
- Follow request sent from @lqdev@toot.lqdev.tech to website's ActivityPub endpoints
- UI shows 0 followers (screenshot provided)
- Azure Table Storage `followers` table shows 0 items
- Application Insights not showing requests (configuration issue?)

### Issue 2: Posts Not Visible in Mastodon UI
- Direct URLs work (e.g., https://toot.lqdev.tech/@lqdev@lqdev.me/115923438819723026)
- Posts don't appear automatically in Mastodon's Web UI
- Need to understand post delivery flow

## Investigation Areas

### 1. ActivityPub Protocol Flow
- [ ] Understand Mastodon → Website follow request flow
- [ ] Understand Accept activity response flow
- [ ] Verify inbox endpoint is receiving requests
- [ ] Verify outbox endpoint is delivering posts

### 2. Azure Function Implementation
- [ ] Review PR #1851 changes
- [ ] Check inbox function proxy implementation
- [ ] Verify Azure Functions are deployed correctly
- [ ] Check Application Insights configuration

### 3. Azure Storage
- [ ] Verify table storage connection strings
- [ ] Check followers table structure
- [ ] Verify write permissions

### 4. Logging & Monitoring
- [ ] Check Application Insights logs
- [ ] Review Azure Functions logs
- [ ] Check for errors in deployments

## Related PR
- PR #1851: Attempted fix for follow/accept activities

## Findings

### Environment Configuration ✅
- **Storage Account**: lqdevactivitypub (configured)
- **App Insights**: lqdev-activitypub-insights (configured)
- **Static Web App**: luisquintanillame-static
- **Environment Variables**: 
  - `ACTIVITYPUB_STORAGE_CONNECTION`: ✅ Configured
  - `APPINSIGHTS_CONNECTION_STRING`: ✅ Configured
- **Followers Table**: ✅ Exists in Azure Table Storage

### API Routing ✅
All ActivityPub endpoints are properly configured in function.json files:
- `/api/activitypub/inbox` → [inbox/function.json](../../api/inbox/function.json)
- `/api/activitypub/outbox` → [outbox/function.json](../../api/outbox/function.json)
- `/api/activitypub/actor` → [actor/function.json](../../api/actor/function.json)
- `/api/activitypub/followers` → [followers/function.json](../../api/followers/function.json)
- `/api/activitypub/following` → [following/function.json](../../api/following/function.json)
- `/api/activitypub/notes/{noteId}` → [activitypub-notes/function.json](../../api/activitypub-notes/function.json)

### Key Research Findings from Perplexity

#### ActivityPub Follow/Accept Protocol Flow
1. **Follow Request**: Remote server POSTs Follow activity to `/api/activitypub/inbox` with HTTP Signature
2. **Signature Verification**: Receiving server must fetch public key and verify signature
3. **Follow Processing**: Add follower to table storage, update followers collection
4. **Accept Delivery**: POST Accept activity back to remote actor's inbox (also signed)

#### Critical Requirements
- **HTTP Signatures**: Must use cavage-12 standard with RSA-SHA256
- **Headers to Sign**: `(request-target)`, `host`, `date`, `digest`, `content-type`
- **Content-Type**: `application/ld+json; profile="https://www.w3.org/ns/activitystreams"` or `application/activity+json`
- **Digest Header**: SHA-256 hash of request body in format `SHA-256=base64`
- **Public Key**: Must be retrievable from keyId in Signature header

#### Common Silent Failure Modes
1. **Public Key Resolution Failures**: Can't fetch public key from remote server
2. **Signature Verification Failures**: Digest mismatch, wrong headers signed, encoding issues
3. **Content-Type Mismatches**: Strict validation on some servers
4. **Missing HTTP 2xx Response**: Server not returning success even when activity received
5. **Authorized Fetch Mode**: Circular dependency when fetching public keys
6. **Activity Logging But Not Processing**: Server accepts request but doesn't update storage

### Investigation Status

#### ❓ Key Questions
1. **Are Follow requests reaching the inbox?** Need to check Application Insights logs
2. **Is HTTP Signature verification passing?** Check for signature validation logs
3. **Is table storage being updated?** Followers table appears empty
4. **Are Accept activities being delivered?** Need to verify delivery logic
5. **Is the deployment current?** When was last deployment to Static Web App?

#### ✅ Confirmed Working
- [x] Inbox endpoint is accessible and responding (HTTP 200 for GET, 202 for POST)
- [x] Followers table exists in Azure Table Storage
- [x] Environment variables are properly configured
- [x] Latest deployment was successful (3 hours ago)
- [x] Test Follow request successfully added follower to table storage!

#### ❌ Identified Issues

**Issue #1: Accept Activity Delivery Failure - ARCHITECTURAL MISMATCH ⚠️**
- Inbox processes Follow requests correctly
- Follower is added to table storage successfully
- BUT: Accept activity delivery is failing
- Response: `{"message": "Follow accepted but failed to deliver Accept activity"}`
- **Root Cause IDENTIFIED**: ARCHITECTURAL MISMATCH FOR FREE TIER!
  - Code is trying to sign outgoing Accept activities in Azure Functions
  - **Azure Static Web Apps Free tier does NOT support managed identities**
  - Per docs/activitypub/keyvault-setup.md:
    - Free tier: Functions should ONLY verify incoming signatures (using sender's public keys)
    - Free tier: Outgoing content signing happens in GitHub Actions, not Functions
  - Current code incorrectly attempts to sign Accept activities in the Function
  - This is incompatible with the Free tier architecture

**Issue #2: Real Follow from Mastodon Not Working**
- Test Follow (without signature) works fine
- Real Mastodon Follow probably includes HTTP Signature
- May be failing signature verification OR failing Accept delivery
- No visible error to user - appears as 0 followers

#### 🔍 Next Investigation Steps
- [ ] Check Key Vault configuration for private key used to sign Accept activities
- [ ] Review HTTP Signature generation code in `api/utils/signatures.js`
- [ ] Test with real Mastodon Follow to see actual error logs
- [ ] Check if Azure Function has permissions to access Key Vault
- [ ] Verify outbox endpoint is exposing followers collection properly

## Action Items

### ✅ Fix #1: Update Accept Delivery to Free Tier Architecture
**Status**: ✅ IMPLEMENTED (Option B)  
**Priority**: CRITICAL - Blocks all Follow/Accept functionality

**Implementation**: Queued Accept Delivery with GitHub Actions Signing

**Changes Made**:
1. **Table Storage (`api/utils/tableStorage.js`)**:
   - Added `pendingaccepts` table support
   - New functions: `queueAcceptActivity()`, `getPendingAccepts()`, `markAcceptDelivered()`, `markAcceptFailed()`
   - Accept activities stored with metadata (actorUrl, inbox, acceptActivity JSON, queuedAt timestamp)

2. **Inbox Function (`api/inbox/index.js`)**:
   - Modified to queue Accept activities instead of attempting delivery
   - Follow requests add follower to `followers` table
   - Accept activity queued to `pendingaccepts` table
   - Response returns acceptId for tracking

3. **GitHub Actions Workflow (`.github/workflows/deliver-activitypub-accepts.yml`)**:
   - Runs every 5 minutes to process pending Accepts
   - Uses Azure OIDC authentication for Key Vault access
   - Scheduled cron job + manual dispatch support

4. **Delivery Script (`api/scripts/deliver-accepts.js`)**:
   - Fetches pending Accepts from table storage
   - Generates HTTP signatures using Azure Key Vault (RSA-SHA256)
   - Delivers signed Accept activities to remote inboxes
   - Updates status (delivered/failed) with error tracking

**Architecture Alignment**:
- ✅ Functions only verify incoming signatures (Free tier compatible)
- ✅ GitHub Actions signs outgoing activities (proper Free tier pattern)
- ✅ Follows documented architecture in docs/activitypub/keyvault-setup.md

### 🧪 Next Steps: Test Follow/Accept Flow End-to-End
**Status**: Ready for testing after deployment  

**Required GitHub Secrets** (for workflow):
- `AZURE_CLIENT_ID`: Azure app registration client ID
- `AZURE_TENANT_ID`: Azure tenant ID
- `AZURE_SUBSCRIPTION_ID`: Azure subscription ID
- `ACTIVITYPUB_STORAGE_CONNECTION`: Table storage connection string
- `KEY_VAULT_KEY_ID`: Full key identifier (e.g., `https://lqdev-activitypub-kv.vault.azure.net/keys/activitypub-signing-key/version`)

**Test Steps**:
1. ✅ Merge this branch and deploy
2. Configure GitHub secrets for workflow
3. Send test Follow request from Mastodon
4. Verify follower added to `followers` table
5. Verify Accept queued in `pendingaccepts` table
6. Wait for workflow execution (5 min or manual trigger)
7. Verify Accept delivered and status updated to 'delivered'
8. Verify follower count shows correctly in Mastodon UI

### 📝 Fix #4: Investigate Post Visibility Issue
**Status**: Secondary priority (after Follow/Accept fixed)  
**Investigation Areas**:
- Outbox endpoint exposing posts correctly
- Posts being delivered to follower inboxes
- Create activity structure and addressing
- Webfinger resolution for remote posts
