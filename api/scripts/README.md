# ActivityPub Scripts

Scripts for ActivityPub functionality that run in GitHub Actions.

## deliver-accepts.js

Delivers queued Accept activities with HTTP signatures using Azure Key Vault.

**Purpose**: Free tier architecture requires GitHub Actions (not Azure Functions) to sign outgoing activities because Functions don't have managed identity support on Free tier.

**Environment Variables Required**:
- `ACTIVITYPUB_STORAGE_CONNECTION`: Azure Table Storage connection string
- `KEY_VAULT_KEY_ID`: Full Azure Key Vault key identifier (e.g., `https://lqdev-activitypub-kv.vault.azure.net/keys/activitypub-signing-key/abc123`)
- `AZURE_CLIENT_ID`: Azure app registration client ID (for OIDC auth)
- `AZURE_TENANT_ID`: Azure tenant ID
- `AZURE_SUBSCRIPTION_ID`: Azure subscription ID

**How It Works**:
1. Queries `pendingaccepts` table for Accept activities with status='pending'
2. For each pending Accept:
   - Generates HTTP signature using Azure Key Vault (RSA-SHA256)
   - Delivers signed Accept to remote server's inbox
   - Updates status to 'delivered' or 'failed' with error details
3. Reports summary of successful/failed deliveries

**Workflow**: `.github/workflows/deliver-activitypub-accepts.yml` runs this script every 5 minutes

**Testing Locally**:
```bash
# Set environment variables
export ACTIVITYPUB_STORAGE_CONNECTION="..."
export KEY_VAULT_KEY_ID="https://..."
export AZURE_CLIENT_ID="..."
export AZURE_TENANT_ID="..."
export AZURE_SUBSCRIPTION_ID="..."

# Authenticate to Azure
az login

# Run script
node api/scripts/deliver-accepts.js
```

**Error Handling**:
- Failed deliveries are marked with status='failed' and retry count incremented
- Error messages stored in table for debugging
- Script continues processing remaining Accepts even if one fails
