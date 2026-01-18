# ActivityPub Azure Resources Setup

This directory contains scripts for setting up Azure resources required for ActivityPub federation on static websites.

## Overview

The ActivityPub implementation uses Azure's free tier services to enable social media federation:

- **Storage Account**: Table Storage for follower data, Queue Storage for async delivery
- **Application Insights**: Monitoring, logging, and performance tracking
- **Estimated Cost**: ~$0.01-0.02/month (free tier covers typical usage)

## Scripts

### `setup-activitypub-azure-resources.ps1`

Automated setup script that creates all required Azure resources.

**Prerequisites:**
- Azure CLI installed ([Download](https://aka.ms/installazurecli))
- Authenticated to Azure (`az login`)
- Existing resource group

**Usage:**

```powershell
# Basic usage (default parameters)
.\setup-activitypub-azure-resources.ps1

# Dry run (preview changes without creating resources)
.\setup-activitypub-azure-resources.ps1 -DryRun

# Custom parameters
.\setup-activitypub-azure-resources.ps1 `
    -ResourceGroup "my-resource-group" `
    -StorageAccountName "myactivitypub" `
    -Location "westus2"
```

**Parameters:**

| Parameter | Description | Default |
|-----------|-------------|---------|
| `ResourceGroup` | Azure resource group (must exist) | `luisquintanillameblog-rg` |
| `Location` | Azure region | `eastus` |
| `StorageAccountName` | Storage account name (3-24 chars, lowercase alphanumeric) | `lqdevactivitypub` |
| `AppInsightsName` | Application Insights name | `lqdev-activitypub-insights` |
| `DryRun` | Preview mode without creating resources | `$false` |

**Resources Created:**

1. **Storage Account** (`Standard_LRS`, TLS 1.2 minimum)
   - Table: `followers` - Stores follower actor URIs and inbox URLs
   - Table: `deliverystatus` - Tracks post delivery status
   - Queue: `accept-delivery` - Async Accept activity delivery
   - Queue: `activitypub-delivery` - Async post distribution

2. **Application Insights** (Web application type)
   - Connection string for Azure Functions
   - Instrumentation key for telemetry

**Output:**

The script displays three connection strings needed for GitHub Secrets and Azure Function configuration:

```
ACTIVITYPUB_STORAGE_CONNECTION: DefaultEndpointsProtocol=https;...
APPINSIGHTS_CONNECTION_STRING: InstrumentationKey=...;IngestionEndpoint=...
APPINSIGHTS_INSTRUMENTATION_KEY: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
```

## Post-Setup Configuration

### 1. GitHub Secrets

Add the connection strings to your repository secrets:

1. Go to: `https://github.com/<owner>/<repo>/settings/secrets/actions`
2. Click "New repository secret"
3. Add each of the three secrets:
   - `ACTIVITYPUB_STORAGE_CONNECTION`
   - `APPINSIGHTS_CONNECTION_STRING`
   - `APPINSIGHTS_INSTRUMENTATION_KEY`

### 2. Azure Function App Settings

Add connection strings to your Function App configuration:

1. Azure Portal → Function App → Configuration → Application settings
2. Add each connection string as a new application setting

## Cost Management

**Free Tier Limits:**

- Storage Account: 5GB storage + 20,000 table operations/month
- Queue Storage: 1GB storage + 2,000,000 operations/month
- Application Insights: 5GB data ingestion/month

**Typical Personal Blog Usage:**

- Followers table: ~50-100 KB (1,000 followers)
- Queue operations: <10,000/month (unless content goes viral)
- App Insights: <1GB/month (normal logging)

**Monitoring Costs:**

Check actual usage in Azure Portal:
- Storage Account → Metrics → Transactions
- Application Insights → Usage and estimated costs

## Troubleshooting

**Resource group not found:**
```powershell
az group create --name luisquintanillameblog-rg --location eastus
```

**Storage account name already taken:**
Storage account names must be globally unique. Use a different name:
```powershell
.\setup-activitypub-azure-resources.ps1 -StorageAccountName "myuniquename123"
```

**Retrieve connection strings manually:**
```powershell
# Storage connection string
az storage account show-connection-string `
    --name lqdevactivitypub `
    --resource-group luisquintanillameblog-rg `
    --query connectionString `
    --output tsv

# Application Insights connection string
az monitor app-insights component show `
    --app lqdev-activitypub-insights `
    --resource-group luisquintanillameblog-rg `
    --query connectionString `
    --output tsv
```

## Resource Cleanup

To delete all resources (if needed):

```powershell
# Delete storage account
az storage account delete `
    --name lqdevactivitypub `
    --resource-group luisquintanillameblog-rg `
    --yes

# Delete Application Insights
az monitor app-insights component delete `
    --app lqdev-activitypub-insights `
    --resource-group luisquintanillameblog-rg
```

## Next Steps

After running this script successfully:

1. ✅ Copy connection strings to GitHub Secrets
2. ✅ Add connection strings to Azure Function App settings
3. 🔄 Proceed to Phase 4A: Implement Inbox Handler
   - Create F# service modules (HttpSignature, FollowerStore, ActivityQueue)
   - Implement Azure Functions (InboxHandler, ProcessAccept)
4. 🔄 Phase 4B: Implement Post Delivery
   - Queue-based async delivery to all followers
5. 🔄 Phase 4C: Integration Testing
   - Test Follow workflow with real Mastodon instance
   - Validate delivery and monitoring

## Reference

- [ActivityPub Specification](https://www.w3.org/TR/activitypub/)
- [Azure Storage Documentation](https://docs.microsoft.com/azure/storage/)
- [Azure Functions Documentation](https://docs.microsoft.com/azure/azure-functions/)
- [Project Repository](https://github.com/lqdev/luisquintanilla.me)
