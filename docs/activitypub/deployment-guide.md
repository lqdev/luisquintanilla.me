# ActivityPub Deployment Guide

> **📋 Implementation Status**: For complete phase breakdown and current state, see [`activitypub-implementation-status.md`](activitypub-implementation-status.md)  
> **API Reference**: See [`/api/ACTIVITYPUB.md`](../../api/ACTIVITYPUB.md)

## Overview

This guide covers the post-merge steps to deploy and configure the ActivityPub federation implementation for your static site on Azure Static Web Apps.

**Current State**: Phases 1-2 Complete (Discovery + Follow/Accept Workflow + Key Vault Security)

## Prerequisites

- Azure CLI installed and authenticated
- Access to Azure Portal
- Resource Group: `luisquintanillameblog-rg`
- Subscription: `Pay-As-You-Go`

---

## Phase 1: Azure Key Vault Setup (~15 minutes)

### Step 1: Create Key Vault and RSA Key

Run the following commands to set up Azure Key Vault with the ActivityPub signing key:

```bash
#!/bin/bash
# Azure Key Vault Setup for ActivityPub Signing

RESOURCE_GROUP="luisquintanillameblog-rg"
VAULT_NAME="lqdev-activitypub-kv"
KEY_NAME="activitypub-signing-key"
LOCATION="eastus"  # or your preferred region

# 1. Create Key Vault with RBAC authorization
az keyvault create \
  --name $VAULT_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --enable-rbac-authorization true

# 2. Create RSA signing key (2048-bit for ActivityPub)
az keyvault key create \
  --vault-name $VAULT_NAME \
  --name $KEY_NAME \
  --kty RSA \
  --size 2048 \
  --ops sign verify

# 3. Get Key ID (save this for next steps)
KEY_ID=$(az keyvault key show \
  --vault-name $VAULT_NAME \
  --name $KEY_NAME \
  --query key.kid -o tsv)
  
echo "✅ Key Vault setup complete!"
echo "Key ID: $KEY_ID"
echo ""
echo "📋 Copy the Key ID above - you'll need it for Function App configuration"
```

**Save the Key ID output** - you'll need it in the next step.

### Step 2: Verify Key Vault Creation

In Azure Portal, navigate to:
1. Resource Groups → `luisquintanillameblog-rg`
2. Find the Key Vault: `lqdev-activitypub-kv`
3. Navigate to Keys → verify `activitypub-signing-key` exists

---

## Phase 2: Azure Function App Configuration (~10 minutes)

### Step 1: Enable Managed Identity

Your Azure Static Web App automatically creates an associated Function App. Find your Function App name (typically related to your Static Web App name).

```bash
# Get your Function App name (if you don't know it)
az functionapp list \
  --resource-group luisquintanillameblog-rg \
  --query "[].name" -o tsv

# Enable system-assigned managed identity
FUNCTION_APP_NAME="<your-function-app-name>"
az functionapp identity assign \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP

# Get the principal ID
PRINCIPAL_ID=$(az functionapp identity show \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query principalId -o tsv)
  
echo "Managed Identity Principal ID: $PRINCIPAL_ID"
```

### Step 2: Grant Key Vault Access

Grant the Function App's managed identity access to use the signing key:

```bash
# Get your subscription ID
SUBSCRIPTION_ID=$(az account show --query id -o tsv)

# Grant "Key Vault Crypto User" role
az role assignment create \
  --role "Key Vault Crypto User" \
  --assignee $PRINCIPAL_ID \
  --scope "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$VAULT_NAME"

echo "✅ Key Vault access granted to Function App"
```

### Step 3: Configure Application Settings

Add the Key Vault key ID to your Function App configuration:

**Option A: Via Azure CLI**
```bash
az functionapp config appsettings set \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings KEY_VAULT_KEY_ID="<paste-key-id-from-phase-1>"
```

**Option B: Via Azure Portal**
1. Navigate to Azure Portal → Function App → Configuration
2. Click "New application setting"
3. Name: `KEY_VAULT_KEY_ID`
4. Value: `<paste-key-id-from-phase-1>`
5. Click Save

### Step 4: Verify Configuration

```bash
# Verify the setting was added
az functionapp config appsettings list \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query "[?name=='KEY_VAULT_KEY_ID']"
```

---

## Phase 3: Deploy and Test (~5 minutes)

### Step 1: Trigger Deployment

The ActivityPub implementation will deploy automatically with your next commit. To trigger a manual deployment:

1. Push any commit to your main branch, or
2. Manually trigger the GitHub Actions workflow: `.github/workflows/publish-azure-static-web-apps.yml`

### Step 2: Verify Endpoints

Once deployed, test the ActivityPub endpoints:

```bash
# Test WebFinger discovery
curl "https://lqdev.me/.well-known/webfinger?resource=acct:lqdev@lqdev.me"

# Test Actor endpoint
curl -H "Accept: application/activity+json" "https://lqdev.me/api/activitypub/actor"

# Test Followers collection
curl -H "Accept: application/activity+json" "https://lqdev.me/api/activitypub/followers"
```

Expected responses:
- WebFinger: JSON with actor link
- Actor: Person object with public key
- Followers: OrderedCollection (initially empty)

### Step 3: Test Federation from Mastodon

1. Open any Mastodon instance
2. Search for: `@lqdev@lqdev.me`
3. Your profile should appear with bio and avatar
4. Click "Follow"
5. You should receive the follow request

**What happens behind the scenes:**
1. Mastodon discovers you via WebFinger
2. Mastodon fetches your Actor profile
3. Mastodon sends signed Follow activity to `/api/inbox`
4. Your inbox verifies the signature with the follower's public key
5. Your inbox adds the follower to `api/data/followers.json`
6. Your inbox sends Accept activity back to Mastodon
7. Follow is complete!

### Step 4: Verify Follower Was Added

Check if the follower was added to your collection:

```bash
curl -H "Accept: application/activity+json" "https://lqdev.me/api/activitypub/followers"
```

You should see the Mastodon user in the `orderedItems` array.

---

## Phase 4: Monitoring and Troubleshooting

### View Activity Logs

Activity logs are stored in `api/data/activities/` (gitignored). To view them in production:

1. Azure Portal → Function App → App Service Editor
2. Navigate to `api/data/activities/`
3. View JSON files for incoming Follow/Undo activities

### Common Issues

#### Issue: "Follow" button doesn't work
**Symptoms**: Mastodon shows your profile but Follow button doesn't respond

**Check:**
1. Verify `KEY_VAULT_KEY_ID` is set in Function App settings
2. Verify managed identity has "Key Vault Crypto User" role
3. Check Function App logs for signature verification errors

**Solution:**
```bash
# Re-grant Key Vault access
az role assignment create \
  --role "Key Vault Crypto User" \
  --assignee $PRINCIPAL_ID \
  --scope "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$VAULT_NAME"
```

#### Issue: Profile not discoverable
**Symptoms**: Mastodon says "user not found" when searching

**Check:**
1. Verify WebFinger endpoint returns valid JSON
2. Check `staticwebapp.config.json` has WebFinger rewrite rule
3. Verify domain is `lqdev.me` (not `www.lqdev.me`)

**Test:**
```bash
curl -v "https://lqdev.me/.well-known/webfinger?resource=acct:lqdev@lqdev.me"
```

#### Issue: Signature verification failures
**Symptoms**: Activities logged but followers not added

**Check Function App logs:**
```bash
az functionapp log tail \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP
```

Look for signature verification errors. Ensure:
- Key Vault key ID is correct
- Managed identity has proper permissions
- Key Vault is in same subscription

### View Function App Logs

**Via Azure Portal:**
1. Function App → Monitor → Logs
2. Filter by function name: `inbox`
3. Look for errors or activity processing logs

**Via Azure CLI:**
```bash
az functionapp log tail \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP
```

---

## What's Working Now

After completing this deployment guide, you'll have:

✅ **Full Federation Support:**
- Discoverable from Mastodon via WebFinger
- Accept Follow requests with signature verification
- Maintain followers collection
- Handle Unfollow (Undo) activities
- Secure key management via Azure Key Vault

✅ **Production-Ready Security:**
- HTTP signature verification prevents spoofing
- Azure Key Vault manages signing keys
- Managed identity for secure access
- Comprehensive activity logging

---

## Future Enhancements (Phase 3 & 4)

The current implementation handles **discovery and following**. Future phases will add:

### Phase 3: Outbox Automation (Future PR)
**Goal:** Automatically generate ActivityPub activities from your content

**What it adds:**
- Generate ActivityPub `Create` activities from F# UnifiedFeedItems during build
- Replace placeholder outbox content with actual posts
- Auto-generate individual note JSON files
- Fix future dates in current outbox

**Impact:** Your existing posts will be visible in your ActivityPub outbox

**Estimated effort:** 1-2 weeks

### Phase 4: Activity Delivery (Future PR)
**Goal:** Push new content to follower timelines automatically

**What it adds:**
- Deliver `Create` activities to follower inboxes when new content published
- Sign outbound activities with Key Vault
- Handle delivery failures and retries
- Integrate with GitHub Actions publish workflow

**Impact:** When you publish new content, your followers will see it in their Mastodon timelines automatically

**Estimated effort:** 1-2 weeks

---

## Additional Resources

- **Implementation Status:** [`activitypub-implementation-status.md`](activitypub-implementation-status.md) - Current phase breakdown and roadmap
- **ActivityPub Specification:** https://www.w3.org/TR/activitypub/
- **WebFinger Specification (RFC 7033):** https://tools.ietf.org/html/rfc7033
- **HTTP Signatures:** https://datatracker.ietf.org/doc/html/draft-cavage-http-signatures
- **Mastodon ActivityPub Guide:** https://docs.joinmastodon.org/spec/activitypub/
- **Key Vault Setup Details:** See `activitypub/keyvault-setup.md`
- **API Documentation:** See `api/ACTIVITYPUB.md`
- **Testing Scripts:** See `Scripts/ACTIVITYPUB-SCRIPTS.md`

---

## Support and Feedback

If you encounter issues during deployment:

1. Check the "Common Issues" section above
2. Review Function App logs for specific errors
3. Verify all configuration settings match this guide
4. Test endpoints individually to isolate the problem

For questions about all implementation phases, refer to `ARCHITECTURE-OVERVIEW.md` for comprehensive architecture details, or `phase3-implementation-complete.md` and `phase4b-4c-complete-summary.md` for phase completion summaries.
