# ActivityPub Azure Key Vault Setup Guide

This guide provides instructions for setting up Azure Key Vault to securely manage ActivityPub signing keys for both GitHub Actions workflows and Azure Functions.

## Overview

**Architecture:**
```
GitHub Actions → Azure Key Vault → Sign content → Publish
Azure Functions → Azure Key Vault → Verify signatures → Process activities
```

**Benefits:**
- ✅ Keys never leave Azure infrastructure
- ✅ Centralized signing authority for both workflows and Functions
- ✅ Audit logs for all signing operations
- ✅ Easy key rotation without code changes
- ✅ Secure key storage with RBAC access control

---

## Prerequisites

- Azure CLI installed (`az` command)
- Azure subscription: **Pay-As-You-Go**
- Resource group: **luisquintanillameblog-rg**
- Appropriate permissions to create Key Vault and assign roles

---

## Setup Steps

### 1. Create Key Vault and Signing Key

Run the following script to create the Key Vault and RSA signing key:

```bash
#!/bin/bash
# Azure Key Vault Setup for ActivityPub Signing

RESOURCE_GROUP="luisquintanillameblog-rg"
VAULT_NAME="lqdev-activitypub-kv"
KEY_NAME="activitypub-signing-key"
LOCATION="eastus"  # or your preferred region

echo "Creating Key Vault..."
az keyvault create \
  --name $VAULT_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --enable-rbac-authorization true

echo "Creating RSA signing key (2048-bit for ActivityPub)..."
az keyvault key create \
  --vault-name $VAULT_NAME \
  --name $KEY_NAME \
  --kty RSA \
  --size 2048 \
  --ops sign verify

echo "Retrieving Key ID..."
KEY_ID=$(az keyvault key show \
  --vault-name $VAULT_NAME \
  --name $KEY_NAME \
  --query key.kid -o tsv)

echo ""
echo "✅ Key Vault and signing key created successfully!"
echo ""
echo "Key ID: $KEY_ID"
echo ""
echo "Save this Key ID for the next steps."
```

**Important:** Save the `KEY_ID` output - you'll need it for Azure Functions configuration.

---

### 2. Configure Azure Functions Access

Grant your Azure Functions app access to sign and verify with the key:

```bash
#!/bin/bash
RESOURCE_GROUP="luisquintanillameblog-rg"
VAULT_NAME="lqdev-activitypub-kv"
FUNCTION_APP_NAME="<your-function-app-name>"  # Replace with your actual Function App name

# Get the subscription ID
SUBSCRIPTION_ID=$(az account show --query id -o tsv)

# Enable system-assigned managed identity for the Function App (if not already enabled)
echo "Enabling managed identity for Function App..."
az functionapp identity assign \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP

# Get the managed identity principal ID
PRINCIPAL_ID=$(az functionapp identity show \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query principalId -o tsv)

echo "Managed Identity Principal ID: $PRINCIPAL_ID"

# Grant the Function App "Key Vault Crypto User" role
echo "Granting Key Vault Crypto User role..."
az role assignment create \
  --role "Key Vault Crypto User" \
  --assignee $PRINCIPAL_ID \
  --scope "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$VAULT_NAME"

echo ""
echo "✅ Azure Functions access configured!"
```

**Add Key ID to Function App Settings:**

```bash
# Replace KEY_ID with the value from step 1
az functionapp config appsettings set \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings KEY_VAULT_KEY_ID="<your-key-id>"
```

---

### 3. Configure GitHub Actions Access

Create a service principal for GitHub Actions to sign content during build/publish workflows:

```bash
#!/bin/bash
RESOURCE_GROUP="luisquintanillameblog-rg"
VAULT_NAME="lqdev-activitypub-kv"

# Get the subscription ID
SUBSCRIPTION_ID=$(az account show --query id -o tsv)

# Create service principal with Key Vault Crypto User role
echo "Creating service principal for GitHub Actions..."
az ad sp create-for-rbac \
  --name "github-activitypub-signer" \
  --role "Key Vault Crypto User" \
  --scopes "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$VAULT_NAME" \
  --sdk-auth

echo ""
echo "✅ Service principal created!"
echo ""
echo "IMPORTANT: Copy the entire JSON output above and add it as a GitHub repository secret named 'AZURE_CREDENTIALS'"
```

**Add to GitHub Repository Secrets:**
1. Go to your GitHub repository
2. Navigate to Settings → Secrets and variables → Actions
3. Click "New repository secret"
4. Name: `AZURE_CREDENTIALS`
5. Value: Paste the entire JSON output from the command above
6. Click "Add secret"

---

## Implementation in Azure Functions

### Install Dependencies

```bash
cd api
npm install @azure/identity @azure/keyvault-keys
```

### Sample Code for Signature Verification

Create `api/utils/keyvault.js`:

```javascript
const { DefaultAzureCredential } = require('@azure/identity');
const { CryptographyClient } = require('@azure/keyvault-keys');

class KeyVaultSigner {
  constructor(keyId) {
    this.keyId = keyId || process.env.KEY_VAULT_KEY_ID;
    if (!this.keyId) {
      throw new Error('KEY_VAULT_KEY_ID environment variable not set');
    }
    
    this.credential = new DefaultAzureCredential();
    this.client = new CryptographyClient(this.keyId, this.credential);
  }

  /**
   * Sign data with Key Vault key
   * @param {Buffer} data - Data to sign (typically a hash)
   * @returns {Promise<Buffer>} Signature
   */
  async sign(data) {
    const result = await this.client.sign('RS256', data);
    return Buffer.from(result.result);
  }

  /**
   * Verify signature with Key Vault key
   * @param {Buffer} data - Original data (typically a hash)
   * @param {Buffer} signature - Signature to verify
   * @returns {Promise<boolean>} True if valid
   */
  async verify(data, signature) {
    const result = await this.client.verify('RS256', data, signature);
    return result.result;
  }

  /**
   * Get public key for actor profile
   * @returns {Promise<string>} PEM-encoded public key
   */
  async getPublicKeyPem() {
    const key = await this.client.getKey();
    // Convert JWK to PEM format
    // Implementation depends on key format needs
    return key; // Return appropriate format for ActivityPub
  }
}

module.exports = { KeyVaultSigner };
```

### Usage in Inbox Function

Update `api/inbox/index.js` to verify incoming signatures:

```javascript
const { KeyVaultSigner } = require('../utils/keyvault');

module.exports = async function (context, req) {
  if (req.method === 'POST') {
    try {
      const signer = new KeyVaultSigner();
      
      // Extract signature from HTTP headers
      const signature = req.headers['signature'];
      const digest = req.headers['digest'];
      
      // Verify signature (simplified - actual implementation more complex)
      const isValid = await signer.verify(
        Buffer.from(digest, 'base64'),
        Buffer.from(signature, 'base64')
      );
      
      if (!isValid) {
        context.res = {
          status: 401,
          body: { error: 'Invalid signature' }
        };
        return;
      }
      
      // Process activity...
      
    } catch (error) {
      context.log.error(`Signature verification error: ${error.message}`);
      context.res = {
        status: 500,
        body: { error: 'Signature verification failed' }
      };
    }
  }
};
```

---

## Implementation in GitHub Actions

### Sample Workflow for Signing Content

Create `.github/workflows/publish-with-signing.yml`:

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
          # Get Key Vault details from secrets
          VAULT_NAME="lqdev-activitypub-kv"
          KEY_NAME="activitypub-signing-key"
          
          # Example: Sign outbox content
          for file in public/api/data/outbox/*.json; do
            # Create SHA-256 hash of content
            HASH=$(sha256sum "$file" | cut -d' ' -f1)
            
            # Sign hash with Key Vault
            SIGNATURE=$(az keyvault key sign \
              --vault-name $VAULT_NAME \
              --name $KEY_NAME \
              --algorithm RS256 \
              --value $HASH \
              --query result -o tsv)
            
            # Add signature to JSON (implementation depends on your format)
            echo "Signed: $file"
          done
      
      - name: Deploy to Azure Static Web Apps
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: "upload"
          app_location: "public"
```

---

## Security Best Practices

### Key Vault Configuration

1. **Enable RBAC Authorization**: Always use RBAC instead of access policies for better security
2. **Enable Soft Delete**: Protects against accidental key deletion
3. **Enable Purge Protection**: Prevents permanent deletion during retention period
4. **Enable Audit Logging**: Track all key operations via Azure Monitor

```bash
# Enable additional security features
az keyvault update \
  --name lqdev-activitypub-kv \
  --resource-group luisquintanillameblog-rg \
  --enable-soft-delete true \
  --enable-purge-protection true
```

### Role Assignments

- **GitHub Actions**: `Key Vault Crypto User` (sign only)
- **Azure Functions**: `Key Vault Crypto User` (sign and verify)
- **Administrators**: `Key Vault Administrator` (manage keys)

### Key Rotation

To rotate keys:

```bash
# Create new key version
az keyvault key create \
  --vault-name lqdev-activitypub-kv \
  --name activitypub-signing-key \
  --kty RSA \
  --size 2048 \
  --ops sign verify

# Old key versions remain accessible for verification
# Update actor.json publicKey when ready to switch
```

---

## Troubleshooting

### "Authentication failed" in Azure Functions

**Solution**: Ensure managed identity is enabled and has proper role assignment:

```bash
# Check if managed identity is enabled
az functionapp identity show \
  --name <function-app-name> \
  --resource-group luisquintanillameblog-rg

# Check role assignments
az role assignment list \
  --assignee <principal-id> \
  --scope /subscriptions/<subscription-id>/resourceGroups/luisquintanillameblog-rg/providers/Microsoft.KeyVault/vaults/lqdev-activitypub-kv
```

### "Key not found" Error

**Solution**: Verify KEY_VAULT_KEY_ID environment variable:

```bash
az functionapp config appsettings list \
  --name <function-app-name> \
  --resource-group luisquintanillameblog-rg \
  --query "[?name=='KEY_VAULT_KEY_ID']"
```

### GitHub Actions Authentication Fails

**Solution**: Verify service principal credentials in GitHub secrets:

1. Check that `AZURE_CREDENTIALS` secret exists
2. Ensure JSON format is valid
3. Verify service principal has `Key Vault Crypto User` role

---

## Cost Considerations

Azure Key Vault pricing (as of 2025):

- **Operations**: $0.03 per 10,000 operations
- **Key storage**: First 5 keys free
- **Estimated monthly cost**: < $1 for typical single-user ActivityPub usage

---

## References

- [Azure Key Vault Documentation](https://learn.microsoft.com/en-us/azure/key-vault/)
- [Azure Functions Managed Identity](https://learn.microsoft.com/en-us/azure/app-service/overview-managed-identity)
- [GitHub Actions Azure Login](https://github.com/Azure/login)
- [ActivityPub HTTP Signatures](https://docs.joinmastodon.org/spec/security/#http)

---

## Next Steps

1. Run the setup scripts to create Key Vault and keys
2. Configure Azure Functions managed identity and settings
3. Add GitHub Actions service principal credentials
4. Update Azure Functions code to use KeyVaultSigner
5. Test signature verification with incoming ActivityPub activities
6. Update GitHub Actions workflow to sign content on publish

For ActivityPub implementation details, see:
- `api/ACTIVITYPUB.md` - Complete endpoint documentation
- `docs/activitypub-fix-summary.md` - Architecture and implementation notes
