# ActivityPub Post-Deployment Testing - Quick Start Guide

**Purpose**: Quick reference for running ActivityPub tests after deployment  
**Created**: January 19, 2026  
**Status**: Ready for Execution

---

## TL;DR - Run Tests Now

```bash
# 1. Clone repository (if needed)
git clone https://github.com/lqdev/luisquintanilla.me.git
cd luisquintanilla.me

# 2. Run production tests
./Scripts/test-activitypub-production.sh

# 3. If all pass, you're done! ✅
```

---

## What Gets Tested

### Automated Tests (test-activitypub-production.sh)

| Phase | What It Tests | Why It Matters |
|-------|---------------|----------------|
| **1. Discovery** | WebFinger endpoints (@lqdev.me, @www.lqdev.me) | Required for Mastodon to find you |
| **2. Profile** | Actor profile with publicKey | Your identity in the Fediverse |
| **3. Collections** | Outbox, Followers, Following | Content and social graph |
| **4. Inbox** | Inbox GET endpoint | Receives federation activities |
| **5. Static Files** | All JSON data files | Deployed correctly |
| **6. URL Validation** | Pattern consistency | Architecture compliance |

**Total Tests**: ~15 automated checks  
**Time to Run**: ~10-30 seconds  
**Prerequisites**: Internet connectivity, curl, jq

---

## Expected Outcomes

### ✅ Full Success

```
==========================================
ActivityPub Production Deployment Tests
==========================================

Phase 1: WebFinger Discovery Tests
✓ PASS: WebFinger: Primary Handle
✓ PASS: WebFinger: WWW Subdomain

Phase 2: Actor Profile Tests
✓ PASS: Actor Profile

Phase 3: Collection Endpoints Tests
✓ PASS: Outbox Collection
✓ PASS: Followers Collection
✓ PASS: Following Collection

Phase 4: Inbox Endpoint Tests
✓ PASS: Inbox (GET)

Phase 5: Static Data Files Tests
✓ PASS: Static File: Actor Data
✓ PASS: Static File: WebFinger Data
✓ PASS: Static File: Followers Data
✓ PASS: Static File: Outbox Data

Phase 6: URL Structure Validation
✓ PASS: URL Pattern Consistency

==========================================
Test Summary
==========================================

Total Tests Run: 15
Passed: 15
Warnings: 0
Failed: 0

✅ All tests passed! ActivityPub deployment is fully functional.
```

**Action**: You're done! Deployment verified. ✅

### ⚠️ Partial Success (Warnings)

Some tests passed but with warnings (e.g., type mismatches, missing optional fields).

**Action**: Review warnings, usually not critical but may indicate configuration issues.

### ❌ Failures

One or more tests failed (404 errors, 500 errors, invalid JSON).

**Action**: 
1. Check which tests failed
2. Consult troubleshooting guide: `/docs/activitypub/POST-DEPLOYMENT-TEST-RESULTS.md`
3. Review Azure Functions deployment status
4. Check Application Insights logs

---

## Test from Mastodon (Recommended)

After automated tests pass, verify real federation:

### Step 1: Search for Profile

1. Log into any Mastodon instance
2. Search for: `@lqdev@lqdev.me`
3. **Expected**: Profile appears with name "Luis Quintanilla"

### Step 2: Follow

1. Click "Follow" button
2. **Expected**: 
   - Follow request sent
   - Accept activity delivered back
   - You're now following @lqdev@lqdev.me

### Step 3: Verify Follower

```bash
curl -H "Accept: application/activity+json" \
  "https://lqdev.me/api/activitypub/followers" | jq
```

**Expected**: Your Mastodon actor URL appears in `orderedItems` array

---

## Automated Monitoring (Optional)

### Enable GitHub Actions

The repository includes a GitHub Actions workflow for automated monitoring:

**Features**:
- Manual trigger for on-demand testing
- Weekly scheduled health checks (Mondays 9 AM UTC)
- Auto-creates GitHub issue if problems detected
- Test result artifacts for review

**Usage**:

1. **Manual Trigger**:
   - Go to GitHub: Actions → "Test ActivityPub Deployment"
   - Click "Run workflow"
   - Review results

2. **Scheduled Health Checks**:
   - Already configured in workflow
   - Runs automatically every Monday
   - Creates issue if failures detected

---

## Troubleshooting Quick Reference

### Problem: Tests won't run

**Cause**: Missing dependencies or permissions

**Solution**:
```bash
# Install dependencies (Ubuntu/Debian)
sudo apt-get install curl jq

# Make script executable
chmod +x ./Scripts/test-activitypub-production.sh
```

### Problem: All tests fail with connection errors

**Cause**: Network connectivity or DNS issues

**Solution**:
```bash
# Test basic connectivity
ping lqdev.me

# Test HTTPS
curl -I https://lqdev.me

# Check DNS
nslookup lqdev.me
```

### Problem: 404 errors on all endpoints

**Cause**: Deployment not complete or staticwebapp.config.json not applied

**Solution**:
1. Verify Azure Static Web Apps deployment completed
2. Check GitHub Actions deployment logs
3. Verify staticwebapp.config.json in repository root
4. Redeploy if necessary

### Problem: 500 errors on dynamic endpoints

**Cause**: Azure Functions not deployed or configuration issues

**Solution**:
1. Check Azure Functions status in portal
2. Review Application Insights logs
3. Verify environment variables configured
4. See `/docs/activitypub/deployment-guide.md`

---

## Complete Documentation

For detailed information, see:

- **Test Results Template**: `/docs/activitypub/POST-DEPLOYMENT-TEST-RESULTS.md`
- **Implementation Status**: `/docs/activitypub/implementation-status.md`
- **Deployment Guide**: `/docs/activitypub/deployment-guide.md`
- **API Reference**: `/api/ACTIVITYPUB.md`
- **Scripts Documentation**: `/Scripts/ACTIVITYPUB-SCRIPTS.md`

---

## Support

If you encounter issues not covered in troubleshooting:

1. Review Application Insights logs in Azure Portal
2. Check GitHub Actions workflow logs
3. Consult `/docs/activitypub/` documentation
4. Create GitHub issue with test output

---

**Last Updated**: January 19, 2026  
**Quick Reference Version**: 1.0
