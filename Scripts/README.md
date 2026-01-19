# ActivityPub Azure Resources Setup

This directory contains scripts for setting up Azure resources required for ActivityPub federation on static websites.

## Overview

The ActivityPub implementation uses Azure's free tier services to enable social media federation:

- **Storage Account**: Table Storage for follower data, Queue Storage for async delivery
- **Application Insights**: Monitoring, logging, and performance tracking
- **Estimated Cost**: ~$0.01-0.02/month (free tier covers typical usage)

## Scripts

### 1. `setup-activitypub-azure-resources.ps1`

Automated setup script that creates all required Azure resources.

### 2. `configure-activitypub-secrets.ps1`

Automated configuration script that sets up GitHub Secrets and Azure Static Web App settings using CLI tools.

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

---

### 2. `configure-activitypub-secrets.ps1`

**Prerequisites:**
- Azure CLI installed and authenticated (`az login`)
- GitHub CLI installed and authenticated (`gh auth login`)
- Resources already created (run `setup-activitypub-azure-resources.ps1` first)

**Usage:**

```powershell
# Configure both GitHub secrets and Azure Static Web App settings
.\configure-activitypub-secrets.ps1

# Only configure GitHub secrets (skip Azure)
.\configure-activitypub-secrets.ps1 -SkipAzure

# Only configure Azure Static Web App (skip GitHub)
.\configure-activitypub-secrets.ps1 -SkipGitHub

# Custom parameters
.\configure-activitypub-secrets.ps1 `
    -StaticWebAppName "my-static-web-app" `
    -GitHubRepo "myuser/myrepo"
```

**Parameters:**

| Parameter | Description | Default |
|-----------|-------------|---------|
| `ResourceGroup` | Azure resource group | `luisquintanillameblog-rg` |
| `StorageAccountName` | Storage account name | `lqdevactivitypub` |
| `AppInsightsName` | Application Insights name | `lqdev-activitypub-insights` |
| `StaticWebAppName` | Azure Static Web App name | `luisquintanillame-static` |
| `GitHubRepo` | GitHub repository (`owner/repo`) | `lqdev/luisquintanilla.me` |
| `SkipGitHub` | Skip GitHub secrets configuration | `$false` |
| `SkipAzure` | Skip Azure Static Web App configuration | `$false` |

**What It Configures:**

1. **GitHub Repository Secrets** (via `gh secret set`):
   - `ACTIVITYPUB_STORAGE_CONNECTION`
   - `APPINSIGHTS_CONNECTION_STRING`
   - `APPINSIGHTS_INSTRUMENTATION_KEY`

2. **Azure Static Web App Settings** (via `az staticwebapp appsettings set`):
   - `ACTIVITYPUB_STORAGE_CONNECTION`
   - `APPINSIGHTS_CONNECTION_STRING`
   - `APPINSIGHTS_INSTRUMENTATION_KEY`

**Output:**

The script automatically:
- Retrieves connection strings from Azure resources
- Sets GitHub repository secrets using GitHub CLI
- Configures Azure Static Web App application settings
- Provides verification URLs for both platforms

## Post-Setup Configuration

### Automated Configuration (Recommended)

Use the automated script to configure both GitHub secrets and Azure Static Web App settings:

```powershell
.\configure-activitypub-secrets.ps1
```

This will:
1. ✅ Retrieve connection strings from Azure resources
2. ✅ Set GitHub repository secrets automatically
3. ✅ Configure Azure Static Web App application settings

### Manual Configuration (Alternative)

If you prefer manual setup or the automated script fails:

#### 1. GitHub Secrets

Add the connection strings to your repository secrets:

1. Go to: `https://github.com/<owner>/<repo>/settings/secrets/actions`
2. Click "New repository secret"
3. Add each of the three secrets from the `setup-activitypub-azure-resources.ps1` output:
   - `ACTIVITYPUB_STORAGE_CONNECTION`
   - `APPINSIGHTS_CONNECTION_STRING`
   - `APPINSIGHTS_INSTRUMENTATION_KEY`

#### 2. Azure Static Web App Settings

Add connection strings to your Static Web App configuration:

**Via Azure Portal:**
1. Azure Portal → Static Web Apps → Your App → Configuration → Application settings
2. Add each connection string as a new application setting

**Via Azure CLI:**
```powershell
az staticwebapp appsettings set `
    --name <your-static-web-app> `
    --resource-group <your-resource-group> `
    --setting-names `
        "ACTIVITYPUB_STORAGE_CONNECTION=<connection-string>" `
        "APPINSIGHTS_CONNECTION_STRING=<connection-string>" `
        "APPINSIGHTS_INSTRUMENTATION_KEY=<key>"
```

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

After running these scripts successfully:

1. ✅ Azure resources created (`setup-activitypub-azure-resources.ps1`)
2. ✅ Secrets configured (`configure-activitypub-secrets.ps1`)
3. 🔄 **Proceed to Phase 4A: Implement Inbox Handler**
   - Create F# service modules (`Services/HttpSignature.fs`, `Services/FollowerStore.fs`, `Services/ActivityQueue.fs`)
   - Implement Azure Functions (`api/InboxHandler/index.js` or F# equivalent)
   - Set up HTTP signature validation
4. 🔄 **Phase 4B: Implement Post Delivery**
   - Queue-based async delivery to all followers
   - Implement delivery status tracking
5. 🔄 **Phase 4C: Integration Testing**
   - Test Follow workflow with real Mastodon instance
   - Validate delivery and monitoring

## Reference

- [ActivityPub Specification](https://www.w3.org/TR/activitypub/)
- [Azure Storage Documentation](https://docs.microsoft.com/azure/storage/)
- [Azure Functions Documentation](https://docs.microsoft.com/azure/azure-functions/)
- [Project Repository](https://github.com/lqdev/luisquintanilla.me)
