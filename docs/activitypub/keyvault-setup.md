# ActivityPub Azure Key Vault Setup Guide

**Setup Status**: ✅ **COMPLETE** (January 18, 2026)  
**AZURE_CREDENTIALS Secret**: ✅ Verified in GitHub repository  
**Next Phase**: Phase 3 (Outbox Automation) - No signing required  
**Signing Implementation**: Phase 4 (Activity Delivery)

---

This guide documents the completed Azure Key Vault setup for ActivityPub signing keys.

## Overview

**Architecture (Free Tier - Default):**
```
GitHub Actions → Azure Key Vault → Sign outgoing content → Publish
Azure Functions → Fetch sender's public key → Verify incoming signatures
```

**Architecture (Standard Tier - Optional):**
```
GitHub Actions → Azure Key Vault → Sign outgoing content → Publish
Azure Functions → Azure Key Vault → Sign/Verify with our own key
```

**Benefits:**
- ✅ Keys never leave Azure infrastructure
- ✅ Centralized signing authority for GitHub Actions
- ✅ Audit logs for all signing operations
- ✅ Easy key rotation without code changes
- ✅ Secure key storage with RBAC access control

---

## Prerequisites

- Azure CLI installed (`az` command)
- GitHub CLI installed (`gh` command) - [Install from cli.github.com](https://cli.github.com/)
- Node.js (already required for Azure Functions)
- Azure subscription: **Pay-As-You-Go**
- Resource group: **luisquintanillameblog-rg**
- Azure Static Web App: **luisquintanillame-static**
- Appropriate permissions to create Key Vault and assign roles
- GitHub authentication: Run `gh auth login` before setup

---

## ⚠️ Security Considerations

**What's Safe to Document:**
- ✅ Resource group names
- ✅ Key Vault names (globally unique but not sensitive)
- ✅ Static Web App names (already public via your domain)
- ✅ Azure region names
- ✅ Public keys (they're meant to be public!)

**What Should NEVER Be Committed:**
- ❌ Subscription IDs
- ❌ Tenant IDs  
- ❌ Service principal credentials (clientId, clientSecret, etc.)
- ❌ Key Vault key IDs (full URLs)
- ❌ Any authentication tokens or secrets

**Best Practices:**
1. Use placeholders (`<subscription-id>`, `<key-id>`) in documentation
2. Store sensitive values only in GitHub Secrets or Azure Key Vault
3. Clear terminal history after copying credentials: `Clear-History`
4. Never screenshot or log sensitive outputs
5. Rotate credentials immediately if accidentally exposed

---

## Setup Steps

### 1. Create Key Vault and Signing Key

**PowerShell (Windows):**

```powershell
$RESOURCE_GROUP = "luisquintanillameblog-rg"
$VAULT_NAME = "lqdev-activitypub-kv"
$KEY_NAME = "activitypub-signing-key"
$LOCATION = "eastus"

Write-Host "Creating Key Vault..."
az keyvault create `
  --name $VAULT_NAME `
  --resource-group $RESOURCE_GROUP `
  --location $LOCATION `
  --enable-rbac-authorization true `
  --enable-purge-protection true

Write-Host "Assigning Key Vault Administrator role to current user..."
$SUBSCRIPTION_ID = (az account show --query id -o tsv)
$MY_USER_ID = (az ad signed-in-user show --query id -o tsv)
az role assignment create `
  --role "Key Vault Administrator" `
  --assignee $MY_USER_ID `
  --scope "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$VAULT_NAME"

Write-Host "Waiting for role propagation (10 seconds)..."
Start-Sleep -Seconds 10

Write-Host "Creating RSA signing key (2048-bit for ActivityPub)..."
az keyvault key create `
  --vault-name $VAULT_NAME `
  --name $KEY_NAME `
  --kty RSA `
  --size 2048 `
  --ops sign verify

$KEY_ID = (az keyvault key show `
  --vault-name $VAULT_NAME `
  --name $KEY_NAME `
  --query key.kid -o tsv)

Write-Host ""
Write-Host "✅ Key Vault and signing key created successfully!"
Write-Host ""
Write-Host "Key ID: $KEY_ID"
Write-Host ""
Write-Host "This will be used in GitHub Actions."
```

**Bash (Linux/Mac):**

```bash
#!/bin/bash
RESOURCE_GROUP="luisquintanillameblog-rg"
VAULT_NAME="lqdev-activitypub-kv"
KEY_NAME="activitypub-signing-key"
LOCATION="eastus"

echo "Creating Key Vault..."
az keyvault create \
  --name $VAULT_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --enable-rbac-authorization true \
  --enable-purge-protection true

echo "Assigning Key Vault Administrator role to current user..."
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
MY_USER_ID=$(az ad signed-in-user show --query id -o tsv)
az role assignment create \
  --role "Key Vault Administrator" \
  --assignee $MY_USER_ID \
  --scope "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$VAULT_NAME"

echo "Waiting for role propagation (10 seconds)..."
sleep 10

echo "Creating RSA signing key (2048-bit for ActivityPub)..."
az keyvault key create \
  --vault-name $VAULT_NAME \
  --name $KEY_NAME \
  --kty RSA \
  --size 2048 \
  --ops sign verify

KEY_ID=$(az keyvault key show \
  --vault-name $VAULT_NAME \
  --name $KEY_NAME \
  --query key.kid -o tsv)

echo ""
echo "✅ Key Vault and signing key created successfully!"
echo ""
echo "Key ID: $KEY_ID"
echo ""
echo "This will be used in GitHub Actions."
```

**Important Notes:**
- The Key Vault Administrator role assignment is required when using RBAC authorization
- Wait 10 seconds for role propagation before creating keys
- The Key ID will be needed for GitHub Actions (stored in secrets)
- Soft delete is enabled by default (no need to specify explicitly)

---

### 2. Update Actor Public Key

Extract the public key from Key Vault and update your `api/data/actor.json`:

**PowerShell (Windows):**

```powershell
# Run the conversion script
.\Scripts\jwk-to-pem.ps1

# The script will output the PEM-formatted public key
# Copy the output and update api/data/actor.json "publicKeyPem" field
```

**Bash (Linux/Mac):**

```bash
# Run the conversion script
chmod +x Scripts/jwk-to-pem.sh
./Scripts/jwk-to-pem.sh

# The script will output the PEM-formatted public key
# Copy the output and update api/data/actor.json "publicKeyPem" field
```

**Manual update in `api/data/actor.json`:**

Replace the `publicKeyPem` value in the `publicKey` object with the output from the script above.

```json
{
  "publicKey": {
    "@type": "Key",
    "id": "https://lqdev.me/api/actor#main-key",
    "owner": "https://lqdev.me/api/actor",
    "publicKeyPem": "-----BEGIN PUBLIC KEY-----\n...\n-----END PUBLIC KEY-----"
  }
}
```

**⚠️ Important:** This public key is safe to commit to your repository - it's meant to be public!

---

### 3. Configure GitHub Actions Access

Create a service principal for GitHub Actions to sign content during build/publish workflows:

**PowerShell:**

```powershell
$RESOURCE_GROUP = "luisquintanillameblog-rg"
$VAULT_NAME = "lqdev-activitypub-kv"
$SUBSCRIPTION_ID = (az account show --query id -o tsv)

Write-Host "Creating service principal for GitHub Actions..."
az ad sp create-for-rbac `
  --name "github-activitypub-signer" `
  --role "Key Vault Crypto User" `
  --scopes "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$VAULT_NAME" `
  --sdk-auth

Write-Host ""
Write-Host "✅ Service principal created!"
Write-Host ""
Write-Host "SECURITY WARNING: The JSON output above contains SENSITIVE credentials!"
Write-Host "IMPORTANT: "
Write-Host "  1. Copy the entire JSON output"
Write-Host "  2. Add it as a GitHub repository secret named 'AZURE_CREDENTIALS'"
Write-Host "  3. NEVER commit these credentials to your repository"
Write-Host "  4. Clear your terminal history after copying"
```

**Bash:**

```bash
RESOURCE_GROUP="luisquintanillameblog-rg"
VAULT_NAME="lqdev-activitypub-kv"
SUBSCRIPTION_ID=$(az account show --query id -o tsv)

echo "Creating service principal for GitHub Actions..."
az ad sp create-for-rbac \
  --name "github-activitypub-signer" \
  --role "Key Vault Crypto User" \
  --scopes "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$VAULT_NAME" \
  --sdk-auth

echo ""
echo "✅ Service principal created!"
echo ""
echo "SECURITY WARNING: The JSON output above contains SENSITIVE credentials!"
echo "IMPORTANT:"
echo "  1. Copy the entire JSON output"
echo "  2. Add it as a GitHub repository secret named 'AZURE_CREDENTIALS'"
echo "  3. NEVER commit these credentials to your repository"
echo "  4. Clear your terminal history after copying"
```

**Add to GitHub Repository Secrets:**

**Option 1: Automated with GitHub CLI (Recommended)**

```powershell
# Prerequisite: Install GitHub CLI and authenticate
# winget install GitHub.cli
# gh auth login

# Create service principal and pipe directly to GitHub Secrets
$spOutput = az ad sp create-for-rbac `
  --name "github-activitypub-signer" `
  --role "Key Vault Crypto User" `
  --scopes "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$VAULT_NAME" `
  --sdk-auth

# Set the secret (credentials never touch clipboard or files)
$spOutput | gh secret set AZURE_CREDENTIALS --repo lqdev/luisquintanilla.me

Write-Host "✅ AZURE_CREDENTIALS secret set in GitHub repository"
Write-Host "Clearing sensitive data from terminal..."
Clear-History
```

**Option 2: Manual through GitHub UI**

1. Go to your GitHub repository
2. Navigate to Settings → Secrets and variables → Actions
3. Click "New repository secret"
4. Name: `AZURE_CREDENTIALS`
5. Value: Paste the entire JSON output from the command above
6. Click "Add secret"

**⚠️ CRITICAL SECURITY NOTE:**
- The service principal JSON contains sensitive authentication credentials
- Store this ONLY in GitHub Secrets, never commit to repository
- Use Option 1 (GitHub CLI) to avoid exposing credentials in terminal/clipboard
- Clear your terminal history after setup: `Clear-History` (PowerShell) or `history -c` (Bash)
- Rotate credentials immediately if accidentally exposed

---

### 4. Azure Static Web Apps Managed Identity (Optional - Standard Tier Only)

**⚠️ IMPORTANT:** Azure Static Web Apps **Free tier does NOT support managed identities**. This step is only needed if:
- You upgrade to Standard tier ($9/month)
- You want Functions to access Key Vault directly for signing

**For Free tier users:** Skip this step. Your Functions will verify incoming signatures using the sender's public keys (fetched from their servers), and GitHub Actions will sign your outgoing content using Key Vault.

**If using Standard tier:**

```powershell
$STATIC_WEB_APP_NAME = "luisquintanillame-static"
$SUBSCRIPTION_ID = (az account show --query id -o tsv)
$RESOURCE_GROUP = "luisquintanillameblog-rg"
$VAULT_NAME = "lqdev-activitypub-kv"

# Enable managed identity
az staticwebapp identity assign `
  --name $STATIC_WEB_APP_NAME `
  --resource-group $RESOURCE_GROUP

# Get principal ID
$PRINCIPAL_ID = (az staticwebapp identity show `
  --name $STATIC_WEB_APP_NAME `
  --resource-group $RESOURCE_GROUP `
  --query principalId -o tsv)

# Grant Key Vault access
az role assignment create `
  --role "Key Vault Crypto User" `
  --assignee $PRINCIPAL_ID `
  --scope "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$VAULT_NAME"

# Set Key ID in app settings
$KEY_ID = (az keyvault key show `
  --vault-name $VAULT_NAME `
  --name activitypub-signing-key `
  --query key.kid -o tsv)

az staticwebapp appsettings set `
  --name $STATIC_WEB_APP_NAME `
  --resource-group $RESOURCE_GROUP `
  --setting-names KEY_VAULT_KEY_ID="$KEY_ID"
```

---

## Implementation Guide

### For GitHub Actions (Required - All Tiers)

GitHub Actions will sign your outgoing ActivityPub content using Key Vault.

**Sample Workflow** (`.github/workflows/publish-with-signing.yml`):

```yaml
name: Publish with ActivityPub Signing

on:
  push:
    branches: [main]

jobs:
  build-and-sign:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Build static site
        run: |
          # Your build commands here
          dotnet run
      
      - name: Azure Login
        uses: azure/login@v1
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}
      
      - name: Sign ActivityPub Content
        run: |
          VAULT_NAME="lqdev-activitypub-kv"
          KEY_NAME="activitypub-signing-key"
          
          # Example: Sign outbox content
          for file in _public/api/data/outbox/*.json; do
            HASH=$(sha256sum "$file" | cut -d' ' -f1)
            
            SIGNATURE=$(az keyvault key sign \
              --vault-name $VAULT_NAME \
              --name $KEY_NAME \
              --algorithm RS256 \
              --value $HASH \
              --query result -o tsv)
            
            echo "Signed: $file with signature: ${SIGNATURE:0:20}..."
          done
      
      - name: Deploy to Azure Static Web Apps
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: "upload"
          app_location: "_public"
```

---

### For Azure Functions (Optional - Depends on Tier)

#### Free Tier (Default - No Key Vault Access Needed)

**Incoming signature verification:** Fetch the sender's public key from their server and verify their signature.

```javascript
// api/utils/activitypub-signatures.js
const crypto = require('crypto');
const https = require('https');

async function fetchPublicKey(keyId) {
  return new Promise((resolve, reject) => {
    https.get(keyId, (res) => {
      let data = '';
      res.on('data', chunk => data += chunk);
      res.on('end', () => {
        const actor = JSON.parse(data);
        resolve(actor.publicKey.publicKeyPem);
      });
    }).on('error', reject);
  });
}

async function verifySignature(req) {
  const signatureHeader = req.headers['signature'];
  
  // Parse signature header to extract keyId
  const keyIdMatch = signatureHeader.match(/keyId="([^"]+)"/);
  if (!keyIdMatch) throw new Error('No keyId in signature');
  
  const keyId = keyIdMatch[1];
  
  // Fetch the sender's public key from their server
  const publicKeyPem = await fetchPublicKey(keyId);
  
  // Build signing string from request
  const signingString = buildSigningString(req);
  
  // Extract signature from header
  const signatureMatch = signatureHeader.match(/signature="([^"]+)"/);
  const signature = Buffer.from(signatureMatch[1], 'base64');
  
  // Verify using the sender's public key
  const verify = crypto.createVerify('RSA-SHA256');
  verify.update(signingString);
  return verify.verify(publicKeyPem, signature);
}

function buildSigningString(req) {
  // Build signing string according to HTTP Signatures spec
  // This is simplified - actual implementation more complex
  return `(request-target): post ${req.url}\nhost: ${req.headers.host}\ndate: ${req.headers.date}`;
}

module.exports = { verifySignature };
```

**Usage in inbox:**

```javascript
// api/inbox/index.js
const { verifySignature } = require('../utils/activitypub-signatures');

module.exports = async function (context, req) {
  if (req.method === 'POST') {
    try {
      // Verify the sender's signature
      const isValid = await verifySignature(req);
      
      if (!isValid) {
        context.res = { status: 401, body: { error: 'Invalid signature' } };
        return;
      }
      
      // Process activity...
      context.res = { status: 202, body: { message: 'Accepted' } };
      
    } catch (error) {
      context.log.error(`Signature verification error: ${error.message}`);
      context.res = { status: 500, body: { error: 'Verification failed' } };
    }
  }
};
```

#### Standard Tier (Optional - Requires Upgrade)

If you upgrade to Standard tier ($9/month), you can use Key Vault in Functions for advanced signing scenarios.

**Install Dependencies:**

```powershell
cd api
npm install @azure/identity @azure/keyvault-keys
```

**Use Key Vault in Functions:**

```javascript
// api/utils/keyvault.js
const { DefaultAzureCredential } = require('@azure/identity');
const { CryptographyClient } = require('@azure/keyvault-keys');

class KeyVaultSigner {
  constructor() {
    this.keyId = process.env.KEY_VAULT_KEY_ID;
    if (!this.keyId) {
      throw new Error('KEY_VAULT_KEY_ID not configured');
    }
    this.credential = new DefaultAzureCredential();
    this.client = new CryptographyClient(this.keyId, this.credential);
  }

  async sign(data) {
    const result = await this.client.sign('RS256', data);
    return Buffer.from(result.result);
  }

  async verify(data, signature) {
    const result = await this.client.verify('RS256', data, signature);
    return result.result;
  }
}

module.exports = { KeyVaultSigner };
```

---

## Security Best Practices

### Key Vault Configuration

1. **Enable RBAC Authorization**: Always use RBAC instead of access policies
2. **Enable Purge Protection**: Prevents permanent deletion during retention period
3. **Enable Audit Logging**: Track all key operations via Azure Monitor
4. **Soft Delete**: Enabled by default, protects against accidental deletion

### Role Assignments

- **GitHub Actions**: `Key Vault Crypto User` (sign operations only)
- **Azure Functions** (Standard tier only): `Key Vault Crypto User` (sign and verify)
- **Administrators**: `Key Vault Administrator` (manage keys and policies)

### Key Rotation

To rotate keys and update actor.json:

```powershell
# Create new key version
az keyvault key create `
  --vault-name lqdev-activitypub-kv `
  --name activitypub-signing-key `
  --kty RSA `
  --size 2048 `
  --ops sign verify

# Run conversion script again
.\Scripts\jwk-to-pem.ps1

# Update api/data/actor.json with new public key
# Commit and deploy the updated actor.json
```

**Note:** Old key versions remain accessible for verification during transition period.

---

## Troubleshooting

### "Authentication failed" in Azure Functions (Standard Tier)

Check managed identity and role assignments:

```powershell
# Verify managed identity is enabled
az staticwebapp identity show `
  --name luisquintanillame-static `
  --resource-group luisquintanillameblog-rg

# Check role assignments (replace <principal-id> with output from above)
az role assignment list `
  --assignee <principal-id> `
  --all
```

### "Key not found" Error (Standard Tier)

Verify environment variable is set:

```powershell
az staticwebapp appsettings list `
  --name luisquintanillame-static `
  --resource-group luisquintanillameblog-rg
```

### GitHub Actions Authentication Fails

1. Verify `AZURE_CREDENTIALS` secret exists in GitHub
2. Check JSON format is valid
3. Verify service principal has `Key Vault Crypto User` role:

```powershell
# List service principals
az ad sp list --display-name "github-activitypub-signer" --query "[].{Name:displayName, AppId:appId}" -o table

# Check role assignments (replace <app-id> with output from above)
az role assignment list --assignee <app-id> --all -o table
```

---

## Cost Considerations

**Azure Key Vault Pricing (2025):**
- **Operations**: $0.03 per 10,000 operations
- **Key storage**: First 5 keys free
- **Estimated monthly cost**: < $1 for typical single-user ActivityPub usage

**Azure Static Web Apps Tiers:**
- **Free**: No managed identity support (use Free tier approach for Functions)
- **Standard**: $9/month, includes managed identity (optional for this setup)

---

## Summary

### What We've Set Up

- ✅ Azure Key Vault with RBAC and purge protection
- ✅ RSA-2048 signing key for ActivityPub
- ✅ Key Vault Administrator role for key management
- ✅ Updated `api/data/actor.json` with public key
- ✅ Service principal for GitHub Actions
- ✅ GitHub Secrets configured with credentials
- ✅ Conversion scripts for future key rotation

### How It Works

**Free Tier (Default):**
- **Outgoing**: GitHub Actions signs content with Key Vault before deployment
- **Incoming**: Functions verify signatures using sender's public keys (no Key Vault access needed)

**Standard Tier (Optional):**
- **Outgoing**: Same as Free tier
- **Incoming**: Functions can optionally use Key Vault for advanced signing scenarios

### Next Steps

1. **Commit changes:**
   ```bash
   git add api/data/actor.json Scripts/jwk-to-pem.ps1 Scripts/jwk-to-pem.sh
   git commit -m "feat: integrate Azure Key Vault for ActivityPub signing"
   ```

2. **Implement GitHub Actions workflow** (see GitHub Actions section)

3. **Implement signature verification** in Functions (see appropriate tier section)

4. **Test the setup:**
   - Trigger GitHub Actions build
   - Verify signed content in deployment
   - Send test ActivityPub activity to inbox
   - Verify signature verification works

---

## Key Management Commands

### View your keys

```powershell
az keyvault key list --vault-name lqdev-activitypub-kv -o table
```

### Show key details

```powershell
az keyvault key show --vault-name lqdev-activitypub-kv --name activitypub-signing-key
```

### Backup your key

```powershell
az keyvault key backup `
  --vault-name lqdev-activitypub-kv `
  --name activitypub-signing-key `
  --file activitypub-key-backup.bak

# Store backup file securely (NOT in git repository!)
```

### Restore from backup

```powershell
az keyvault key restore `
  --vault-name lqdev-activitypub-kv `
  --file activitypub-key-backup.bak
```

---

## References

- [Azure Key Vault Documentation](https://learn.microsoft.com/en-us/azure/key-vault/)
- [Azure Static Web Apps Managed Identity](https://learn.microsoft.com/en-us/azure/static-web-apps/authentication-authorization)
- [Azure Static Web Apps API Configuration](https://learn.microsoft.com/en-us/azure/static-web-apps/apis-functions)
- [GitHub Actions Azure Login](https://github.com/Azure/login)
- [GitHub CLI Secrets](https://cli.github.com/manual/gh_secret_set)
- [ActivityPub HTTP Signatures](https://docs.joinmastodon.org/spec/security/#http)
- [HTTP Signatures Specification](https://datatracker.ietf.org/doc/html/draft-cavage-http-signatures)

---

## Additional Resources

For ActivityPub implementation details, see:
- `api/ACTIVITYPUB.md` - Complete endpoint documentation
- `docs/activitypub/fix-summary.md` - Architecture and implementation notes
To rotate keys and update actor.json:

```powershell
# 1. Create new key version
az keyvault key create 
  --vault-name lqdev-activitypub-kv 
  --name activitypub-signing-key 
  --kty RSA 
  --size 2048

# 2. Extract new public key
.\Scripts\jwk-to-pem.ps1

# 3. Update api/data/actor.json with new public key
# 4. Commit and deploy changes
# 5. Old key version remains available during transition period
```

**⚠️ Important:** Update your published actor.json BEFORE rotating the key used by GitHub Actions to avoid signature verification failures during the transition period.

---

## Troubleshooting

### Role Assignment Propagation Issues

**Problem:** Forbidden error when creating keys immediately after assigning roles.

**Solution:**
```powershell
# Add a wait period after role assignment
Start-Sleep -Seconds 10
```

Role propagation can take up to 60 seconds in some cases. If issues persist, increase the wait time.

---

### Managed Identity Not Available

**Problem:** SkuCode 'Free' is invalid for property 'sku.name' when enabling managed identity.

**Solution:**
- Azure Static Web Apps **Free tier does NOT support managed identities**
- Either:
  1. Use the Free tier approach (GitHub Actions signs, Functions verify using sender's public keys)
  2. Upgrade to Standard tier (Lines9/month) if you need Functions to access Key Vault

**Verification:**
```powershell
az staticwebapp show 
  --name luisquintanillame-static 
  --resource-group luisquintanillameblog-rg 
  --query sku
```

---

### Service Principal Authentication Failures

**Problem:** GitHub Actions workflow fails with authentication errors.

**Solution:**
1. Verify AZURE_CREDENTIALS secret is set correctly in GitHub repository
2. Check service principal has Key Vault Crypto User role assigned
3. Confirm role assignment scope matches your Key Vault resource path
4. Test authentication locally:

```powershell
# Test with service principal credentials
az login --service-principal 
  --username <clientId> 
  --password <clientSecret> 
  --tenant <tenantId>

# Verify access
az keyvault key show 
  --vault-name lqdev-activitypub-kv 
  --name activitypub-signing-key
```

---

### Key Vault Access Denied

**Problem:** Access denied or Forbidden errors when accessing Key Vault.

**Solution:**
1. Verify RBAC role assignments:

```powershell
az role assignment list 
  --scope "/subscriptions/<subscription-id>/resourceGroups/luisquintanillameblog-rg/providers/Microsoft.KeyVault/vaults/lqdev-activitypub-kv"
```

2. Confirm you have one of these roles:
   - Key Vault Administrator (for managing keys)
   - Key Vault Crypto User (for signing operations)

3. Wait for role propagation (up to 60 seconds)

---

### Public Key Format Issues

**Problem:** ActivityPub servers reject your public key format.

**Solution:**
1. Verify PEM format matches ActivityPub expectations:
   - Must start with -----BEGIN PUBLIC KEY-----
   - Must end with -----END PUBLIC KEY-----
   - Must have exactly 64 characters per line (except header/footer)
   - Must include \n newline characters in JSON

2. Use the provided conversion scripts:
   - Windows: .\Scripts\jwk-to-pem.ps1
   - Linux/Mac: ./Scripts/jwk-to-pem.sh

3. Validate the public key:

```powershell
# Extract key and verify format
 = .\Scripts\jwk-to-pem.ps1
 -split "
" | ForEach-Object { .Length }
# Should show: 27, 64, 64, ..., 64, 25
```

---

## Cost Considerations

### Azure Key Vault Pricing

**Free Tier Operations:**
- First 10,000 operations/month: **FREE**
- Additional operations: ~.03 per 10,000

**Storage:**
- Keys: .015 per key per month

**Typical Usage (Free Tier ActivityPub Site):**
- GitHub Actions signing: ~100 operations/month
- **Estimated Monthly Cost: .02** (essentially free)

**Standard Tier Additional Costs (If Upgraded):**
- Azure Static Web Apps Standard: /month
- Additional Key Vault operations: Minimal (still within free tier for most sites)

**Cost Optimization Tips:**
1. Stay on Free tier unless you need Functions to access Key Vault
2. Use GitHub Actions for signing (covered by free operations)
3. Monitor usage via Azure Cost Management
4. Enable soft delete to prevent accidental deletion costs

---

## Summary

You have successfully configured:

✅ **Azure Key Vault** with RBAC authorization and purge protection  
✅ **RSA-2048 signing key** for ActivityPub signatures  
✅ **Public key extraction** and actor.json configuration  
✅ **Conversion scripts** (PowerShell and Bash) for JWK to PEM  
✅ **Security best practices** for credential management  
✅ **Free tier architecture** (GitHub Actions signs, Functions verify)  

**Free Tier Architecture (Default):**
- GitHub Actions accesses Key Vault to sign outgoing content
- Azure Functions verify incoming content using sender's public keys
- **Cost:** Essentially free (<.05/month)

**Standard Tier Architecture (Optional - /month):**
- Azure Functions can access Key Vault via managed identity
- Enables advanced signing scenarios in Functions
- Requires upgrade from Free to Standard tier

---

## Next Steps

### ✅ Setup Complete

All infrastructure is configured and verified:
- ✅ Azure Key Vault created with RSA-2048 signing key
- ✅ Service principal configured with Key Vault Crypto User role
- ✅ AZURE_CREDENTIALS secret verified in GitHub repository (updated 18 minutes ago)
- ✅ Public key extracted and ready for actor.json
- ✅ Conversion scripts available for future key rotation

### 📋 Phase 3: Outbox Automation (Next)

**No signing implementation needed for Phase 3.**

Phase 3 focuses on improving outbox generation from your F# build:
- Continue generating **unsigned** outbox JSON files (current approach is correct)
- Refine content quality and metadata
- Consider F# module integration
- Plan URL migration to `/api/activitypub/*`

**Why no signing?** ActivityPub signatures are HTTP request-level, not file-level. Static outbox files don't need signatures.

### 🚀 Phase 4: Activity Delivery (Future - Signing Happens Here)

When you implement Phase 4 delivery system, you'll use Key Vault to sign HTTP requests:

1. **Delivery System Implementation**
   - Load followers from `api/data/followers.json`
   - Generate Create activities for new posts
   - Sign each HTTP POST request with Key Vault (fresh signature per request)
   - Deliver to follower inboxes with Signature header
   - Handle delivery failures and retries

2. **GitHub Actions Signing Workflow** (Phase 4)
   - Store signatures with content metadata
   - Include signature headers in ActivityPub delivery

2. **Implement verification in Azure Functions**
   - Add signature verification to inbox endpoint
   - Fetch sender's public keys automatically
   - Log verification success/failure for debugging

3. **Monitor and optimize**
   - Set up Azure Monitor alerts for Key Vault access
   - Track signature verification failures
   - Review audit logs monthly

4. **Consider Standard tier upgrade** (optional)
   - If you need Functions to sign content directly
   - If you want centralized key management for all operations
   - Cost: Additional /month

---

## Key Management Commands

### View Key Versions
```powershell
az keyvault key list-versions 
  --vault-name lqdev-activitypub-kv 
  --name activitypub-signing-key
```

### Get Current Key
```powershell
az keyvault key show 
  --vault-name lqdev-activitypub-kv 
  --name activitypub-signing-key
```

### Rotate Key (Create New Version)
```powershell
az keyvault key create 
  --vault-name lqdev-activitypub-kv 
  --name activitypub-signing-key 
  --kty RSA 
  --size 2048
```

### Disable Old Key Version
```powershell
az keyvault key set-attributes 
  --vault-name lqdev-activitypub-kv 
  --name activitypub-signing-key 
  --version <old-version-id> 
  --enabled false
```

### View Audit Logs
```powershell
az monitor activity-log list 
  --resource-group luisquintanillameblog-rg 
  --offset 7d 
  --query "[?contains(authorization.action, 'Microsoft.KeyVault')]"
```

---

## References

### Azure Documentation
- [Azure Key Vault Overview](https://learn.microsoft.com/en-us/azure/key-vault/general/overview)
- [Key Vault Security Best Practices](https://learn.microsoft.com/en-us/azure/key-vault/general/security-features)
- [Key Vault RBAC Guide](https://learn.microsoft.com/en-us/azure/key-vault/general/rbac-guide)
- [Azure Static Web Apps Managed Identity](https://learn.microsoft.com/en-us/azure/static-web-apps/user-information)

### ActivityPub Standards
- [ActivityPub HTTP Signatures](https://www.w3.org/TR/activitypub/#security-considerations)
- [HTTP Signatures Draft Spec](https://datatracker.ietf.org/doc/html/draft-cavage-http-signatures)
- [ActivityPub Public Key Format](https://www.w3.org/TR/activitypub/#public-key)

### Related Tools
- [GitHub CLI Documentation](https://cli.github.com/manual/)
- [Azure CLI Reference](https://learn.microsoft.com/en-us/cli/azure/)
- [PowerShell Azure Modules](https://learn.microsoft.com/en-us/powershell/azure/)

### Security Resources
- [OWASP Key Management Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Key_Management_Cheat_Sheet.html)
- [Azure Security Baseline for Key Vault](https://learn.microsoft.com/en-us/security/benchmark/azure/baselines/key-vault-security-baseline)

---

## Additional Resources

### Conversion Scripts

**PowerShell Script** (Scripts/jwk-to-pem.ps1):
- Uses .NET System.Security.Cryptography.RSA
- Handles Base64url decoding
- Formats PEM with 64-char lines

**Bash Script** (Scripts/jwk-to-pem.sh):
- Uses Node.js crypto.createPublicKey
- Zero additional dependencies (jq + node already required)
- Cross-platform compatible

Both scripts are included in this repository and handle the JWK to PEM conversion automatically.

### Credential Safety

**Gitignore Patterns Added:**
```gitignore
# Azure credentials
**/azure-credentials.json
**/*subscription-id*
**/*tenant-id*
**/*service-principal*
**/*client-secret*
```

**What's Safe to Commit:**
- ✅ Public keys (PEM format)
- ✅ Key Vault names
- ✅ Resource group names
- ✅ Conversion scripts
- ✅ Documentation and setup guides

**Never Commit:**
- ❌ Subscription IDs
- ❌ Tenant IDs
- ❌ Service principal credentials
- ❌ Key IDs (full URLs)
- ❌ Authentication tokens

---

**Documentation Version:** 1.0  
**Last Updated:** 2025-01-29  
**Author:** GitHub Copilot with Luis Quintanilla  
**Project:** ActivityPub Integration for luisquintanilla.me
