<#
.SYNOPSIS
    Automatically configures GitHub Secrets and Azure Static Web App settings for ActivityPub.

.DESCRIPTION
    This script retrieves connection strings from Azure resources and automatically:
    - Sets GitHub repository secrets using GitHub CLI (gh)
    - Configures Azure Static Web App application settings using Azure CLI
    
    Requires both Azure CLI (az) and GitHub CLI (gh) to be authenticated.

.PARAMETER ResourceGroup
    Azure resource group name containing ActivityPub resources.
    Default: "luisquintanillameblog-rg"

.PARAMETER StorageAccountName
    Storage account name.
    Default: "lqdevactivitypub"

.PARAMETER AppInsightsName
    Application Insights resource name.
    Default: "lqdev-activitypub-insights"

.PARAMETER StaticWebAppName
    Azure Static Web App name.
    Default: "luisquintanillame-static"

.PARAMETER GitHubRepo
    GitHub repository in format "owner/repo".
    Default: "lqdev/luisquintanilla.me"

.PARAMETER SkipGitHub
    Skip GitHub secrets configuration (only configure Azure).

.PARAMETER SkipAzure
    Skip Azure Static Web App configuration (only configure GitHub).

.EXAMPLE
    .\configure-activitypub-secrets.ps1
    Configures both GitHub secrets and Azure Static Web App settings with default parameters.

.EXAMPLE
    .\configure-activitypub-secrets.ps1 -SkipAzure
    Only configures GitHub secrets, skips Azure Static Web App settings.

.NOTES
    Prerequisites:
    - Azure CLI authenticated (az login)
    - GitHub CLI authenticated (gh auth login)
    - Resources already created (run setup-activitypub-azure-resources.ps1 first)
#>

param(
    [string]$ResourceGroup = "luisquintanillameblog-rg",
    [string]$StorageAccountName = "lqdevactivitypub",
    [string]$AppInsightsName = "lqdev-activitypub-insights",
    [string]$StaticWebAppName = "luisquintanillame-static",
    [string]$GitHubRepo = "lqdev/luisquintanilla.me",
    [switch]$SkipGitHub = $false,
    [switch]$SkipAzure = $false
)

Write-Host @"
╔══════════════════════════════════════════════════════════════════════╗
║     ActivityPub Secrets Configuration                                ║
║     Automated GitHub & Azure Static Web App Setup                    ║
╚══════════════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

# ══════════════════════════════════════════════════════════════════════
# Verify Prerequisites
# ══════════════════════════════════════════════════════════════════════

Write-Host "`n📋 Verifying prerequisites..." -ForegroundColor Cyan

# Verify Azure CLI
try {
    $azVersion = az version --output json | ConvertFrom-Json
    Write-Host "✓ Azure CLI version: $($azVersion.'azure-cli')" -ForegroundColor Green
} catch {
    Write-Host "✗ Azure CLI not found. Install from: https://aka.ms/installazurecli" -ForegroundColor Red
    exit 1
}

# Verify Azure authentication
try {
    $account = az account show --output json | ConvertFrom-Json
    Write-Host "✓ Azure authenticated as: $($account.user.name)" -ForegroundColor Green
} catch {
    Write-Host "✗ Not authenticated to Azure. Run: az login" -ForegroundColor Red
    exit 1
}

# Verify GitHub CLI (only if not skipping GitHub)
if (-not $SkipGitHub) {
    try {
        $ghVersion = gh --version | Select-Object -First 1
        Write-Host "✓ GitHub CLI: $ghVersion" -ForegroundColor Green
    } catch {
        Write-Host "✗ GitHub CLI not found. Install from: https://cli.github.com" -ForegroundColor Red
        exit 1
    }
    
    # Verify GitHub authentication
    try {
        $ghAuth = gh auth status 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ GitHub CLI authenticated" -ForegroundColor Green
        } else {
            throw "Not authenticated"
        }
    } catch {
        Write-Host "✗ Not authenticated to GitHub. Run: gh auth login" -ForegroundColor Red
        exit 1
    }
}

# ══════════════════════════════════════════════════════════════════════
# Retrieve Connection Strings
# ══════════════════════════════════════════════════════════════════════

Write-Host "`n🔑 Retrieving connection strings from Azure..." -ForegroundColor Cyan

# Get Storage Account connection string
Write-Host "Retrieving storage connection string..." -ForegroundColor White
$storageConnectionString = az storage account show-connection-string `
    --name $StorageAccountName `
    --resource-group $ResourceGroup `
    --query connectionString `
    --output tsv

if (-not $storageConnectionString) {
    Write-Host "✗ Failed to retrieve storage connection string" -ForegroundColor Red
    Write-Host "  Verify storage account exists: $StorageAccountName" -ForegroundColor Yellow
    exit 1
}
Write-Host "✓ Storage connection string retrieved" -ForegroundColor Green

# Get Application Insights connection string
Write-Host "Retrieving Application Insights connection string..." -ForegroundColor White
$appInsightsConnectionString = az monitor app-insights component show `
    --app $AppInsightsName `
    --resource-group $ResourceGroup `
    --query connectionString `
    --output tsv

if (-not $appInsightsConnectionString) {
    Write-Host "✗ Failed to retrieve Application Insights connection string" -ForegroundColor Red
    Write-Host "  Verify Application Insights exists: $AppInsightsName" -ForegroundColor Yellow
    exit 1
}
Write-Host "✓ Application Insights connection string retrieved" -ForegroundColor Green

# Get Application Insights instrumentation key
Write-Host "Retrieving Application Insights instrumentation key..." -ForegroundColor White
$appInsightsKey = az monitor app-insights component show `
    --app $AppInsightsName `
    --resource-group $ResourceGroup `
    --query instrumentationKey `
    --output tsv

if (-not $appInsightsKey) {
    Write-Host "✗ Failed to retrieve Application Insights key" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Application Insights key retrieved" -ForegroundColor Green

# ══════════════════════════════════════════════════════════════════════
# Configure GitHub Secrets
# ══════════════════════════════════════════════════════════════════════

if (-not $SkipGitHub) {
    Write-Host "`n╔══════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║ Step 1: Configuring GitHub Repository Secrets                       ║" -ForegroundColor Cyan
    Write-Host "╚══════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    
    Write-Host "Repository: $GitHubRepo" -ForegroundColor White
    
    try {
        # Set ACTIVITYPUB_STORAGE_CONNECTION
        Write-Host "`nSetting ACTIVITYPUB_STORAGE_CONNECTION..." -ForegroundColor White
        $storageConnectionString | gh secret set ACTIVITYPUB_STORAGE_CONNECTION --repo $GitHubRepo
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ ACTIVITYPUB_STORAGE_CONNECTION configured" -ForegroundColor Green
        } else {
            throw "Failed to set storage connection secret"
        }
        
        # Set APPINSIGHTS_CONNECTION_STRING
        Write-Host "Setting APPINSIGHTS_CONNECTION_STRING..." -ForegroundColor White
        $appInsightsConnectionString | gh secret set APPINSIGHTS_CONNECTION_STRING --repo $GitHubRepo
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ APPINSIGHTS_CONNECTION_STRING configured" -ForegroundColor Green
        } else {
            throw "Failed to set App Insights connection secret"
        }
        
        # Set APPINSIGHTS_INSTRUMENTATION_KEY
        Write-Host "Setting APPINSIGHTS_INSTRUMENTATION_KEY..." -ForegroundColor White
        $appInsightsKey | gh secret set APPINSIGHTS_INSTRUMENTATION_KEY --repo $GitHubRepo
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ APPINSIGHTS_INSTRUMENTATION_KEY configured" -ForegroundColor Green
        } else {
            throw "Failed to set App Insights key secret"
        }
        
        Write-Host "`n✅ GitHub secrets configured successfully" -ForegroundColor Green
        
    } catch {
        Write-Host "✗ Failed to configure GitHub secrets: $_" -ForegroundColor Red
        Write-Host "  You may need to configure manually at: https://github.com/$GitHubRepo/settings/secrets/actions" -ForegroundColor Yellow
        if (-not $SkipAzure) {
            Write-Host "  Continuing with Azure configuration..." -ForegroundColor Yellow
        } else {
            exit 1
        }
    }
} else {
    Write-Host "`n⏭️  Skipping GitHub secrets configuration" -ForegroundColor Yellow
}

# ══════════════════════════════════════════════════════════════════════
# Configure Azure Static Web App Settings
# ══════════════════════════════════════════════════════════════════════

if (-not $SkipAzure) {
    Write-Host "`n╔══════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║ Step 2: Configuring Azure Static Web App Settings                   ║" -ForegroundColor Cyan
    Write-Host "╚══════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    
    Write-Host "Static Web App: $StaticWebAppName" -ForegroundColor White
    
    try {
        # Verify Static Web App exists
        Write-Host "`nVerifying Static Web App..." -ForegroundColor White
        $swa = az staticwebapp show `
            --name $StaticWebAppName `
            --resource-group $ResourceGroup `
            --query "name" `
            --output tsv 2>$null
        
        if (-not $swa) {
            Write-Host "✗ Static Web App not found: $StaticWebAppName" -ForegroundColor Red
            Write-Host "  Available Static Web Apps in resource group:" -ForegroundColor Yellow
            az staticwebapp list --resource-group $ResourceGroup --query "[].name" --output tsv
            throw "Static Web App not found"
        }
        
        Write-Host "✓ Static Web App found: $swa" -ForegroundColor Green
        
        # Set application settings
        Write-Host "`nConfiguring application settings..." -ForegroundColor White
        
        az staticwebapp appsettings set `
            --name $StaticWebAppName `
            --resource-group $ResourceGroup `
            --setting-names `
                "ACTIVITYPUB_STORAGE_CONNECTION=$storageConnectionString" `
                "APPINSIGHTS_CONNECTION_STRING=$appInsightsConnectionString" `
                "APPINSIGHTS_INSTRUMENTATION_KEY=$appInsightsKey" `
            --output none
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ Application settings configured" -ForegroundColor Green
        } else {
            throw "Failed to configure application settings"
        }
        
        Write-Host "`n✅ Azure Static Web App configured successfully" -ForegroundColor Green
        
    } catch {
        Write-Host "✗ Failed to configure Azure Static Web App: $_" -ForegroundColor Red
        Write-Host "  You may need to configure manually in Azure Portal:" -ForegroundColor Yellow
        Write-Host "  Portal → Static Web Apps → $StaticWebAppName → Configuration" -ForegroundColor Gray
        if (-not $SkipGitHub) {
            Write-Host "  Note: GitHub secrets were configured successfully" -ForegroundColor Green
        }
        exit 1
    }
} else {
    Write-Host "`n⏭️  Skipping Azure Static Web App configuration" -ForegroundColor Yellow
}

# ══════════════════════════════════════════════════════════════════════
# Summary
# ══════════════════════════════════════════════════════════════════════

Write-Host "`n╔══════════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║ ✓ Configuration Complete                                            ║" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════════════════════════════╝" -ForegroundColor Green

Write-Host "`n📊 Configuration Summary:" -ForegroundColor Yellow

if (-not $SkipGitHub) {
    Write-Host "  ✓ GitHub Secrets ($GitHubRepo)" -ForegroundColor Green
    Write-Host "    • ACTIVITYPUB_STORAGE_CONNECTION" -ForegroundColor Green
    Write-Host "    • APPINSIGHTS_CONNECTION_STRING" -ForegroundColor Green
    Write-Host "    • APPINSIGHTS_INSTRUMENTATION_KEY" -ForegroundColor Green
}

if (-not $SkipAzure) {
    Write-Host "  ✓ Azure Static Web App ($StaticWebAppName)" -ForegroundColor Green
    Write-Host "    • ACTIVITYPUB_STORAGE_CONNECTION" -ForegroundColor Green
    Write-Host "    • APPINSIGHTS_CONNECTION_STRING" -ForegroundColor Green
    Write-Host "    • APPINSIGHTS_INSTRUMENTATION_KEY" -ForegroundColor Green
}

Write-Host "`n🔍 Verify Configuration:" -ForegroundColor Yellow
if (-not $SkipGitHub) {
    Write-Host "  GitHub Secrets: https://github.com/$GitHubRepo/settings/secrets/actions" -ForegroundColor Gray
}
if (-not $SkipAzure) {
    Write-Host "  Azure Portal: https://portal.azure.com/#resource/subscriptions/$($account.id)/resourceGroups/$ResourceGroup/providers/Microsoft.Web/staticSites/$StaticWebAppName/configuration" -ForegroundColor Gray
}

Write-Host "`n✅ Ready for Phase 4A: Inbox Handler Implementation" -ForegroundColor Cyan
Write-Host "   Next: Create F# service modules and Azure Functions" -ForegroundColor White

Write-Host ""
