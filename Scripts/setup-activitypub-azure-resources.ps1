# Azure ActivityPub Resources Setup Script
# Prerequisites: Azure CLI installed and authenticated (az login)
# Purpose: Create Azure resources for ActivityPub implementation

param(
    [string]$ResourceGroup = "luisquintanillameblog-rg",
    [string]$Location = "eastus",
    [string]$StorageAccountName = "lqdevactivitypub",
    [string]$AppInsightsName = "lqdev-activitypub-insights",
    [switch]$DryRun = $false
)

Write-Host @"
╔══════════════════════════════════════════════════════════════════════╗
║     Azure ActivityPub Resources Setup                                ║
║     Phase 4 Implementation - Production-Ready Approach               ║
╚══════════════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

if ($DryRun) {
    Write-Host "`n🔍 DRY RUN MODE - No resources will be created`n" -ForegroundColor Yellow
}

# Verify Azure CLI is installed
try {
    $azVersion = az version --output json | ConvertFrom-Json
    Write-Host "✓ Azure CLI version: $($azVersion.'azure-cli')" -ForegroundColor Green
} catch {
    Write-Host "✗ Azure CLI not found. Please install from: https://aka.ms/installazurecli" -ForegroundColor Red
    exit 1
}

# Verify authentication
Write-Host "`nVerifying Azure authentication..." -ForegroundColor Cyan
try {
    $account = az account show --output json | ConvertFrom-Json
    Write-Host "✓ Authenticated as: $($account.user.name)" -ForegroundColor Green
    Write-Host "✓ Subscription: $($account.name) ($($account.id))" -ForegroundColor Green
} catch {
    Write-Host "✗ Not authenticated. Please run: az login" -ForegroundColor Red
    exit 1
}

# Verify resource group exists
Write-Host "`nVerifying resource group..." -ForegroundColor Cyan
try {
    $rg = az group show --name $ResourceGroup --output json 2>$null | ConvertFrom-Json
    if ($rg) {
        Write-Host "✓ Resource group exists: $ResourceGroup" -ForegroundColor Green
    }
} catch {
    Write-Host "✗ Resource group not found: $ResourceGroup" -ForegroundColor Red
    Write-Host "  Please create it or specify an existing resource group with -ResourceGroup parameter" -ForegroundColor Yellow
    exit 1
}

# ══════════════════════════════════════════════════════════════════════
# Resource Creation
# ══════════════════════════════════════════════════════════════════════

Write-Host "`n╔══════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║ Step 1: Creating Storage Account                                    ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

Write-Host "Storage Account: $StorageAccountName" -ForegroundColor White
Write-Host "Purpose: Table Storage (followers, delivery status) + Queue Storage (delivery tasks)" -ForegroundColor Gray

if (-not $DryRun) {
    try {
        # Check if storage account already exists
        $existingStorage = az storage account show --name $StorageAccountName --resource-group $ResourceGroup --output json 2>$null | ConvertFrom-Json
        
        if ($existingStorage) {
            Write-Host "✓ Storage account already exists" -ForegroundColor Yellow
        } else {
            Write-Host "Creating storage account..." -ForegroundColor White
            az storage account create `
                --name $StorageAccountName `
                --resource-group $ResourceGroup `
                --location $Location `
                --sku Standard_LRS `
                --kind StorageV2 `
                --min-tls-version TLS1_2 `
                --allow-blob-public-access false `
                --output none
            
            Write-Host "✓ Storage account created" -ForegroundColor Green
        }
    } catch {
        Write-Host "✗ Failed to create storage account: $_" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "[DRY RUN] Would create storage account: $StorageAccountName" -ForegroundColor Yellow
}

# ══════════════════════════════════════════════════════════════════════

Write-Host "`n╔══════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║ Step 2: Creating Table Storage Tables                               ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

$storageConnectionString = $null

if (-not $DryRun) {
    # Get storage connection string
    Write-Host "Retrieving storage connection string..." -ForegroundColor White
    $storageConnectionString = az storage account show-connection-string `
        --name $StorageAccountName `
        --resource-group $ResourceGroup `
        --query connectionString `
        --output tsv
    
    if (-not $storageConnectionString) {
        Write-Host "✗ Failed to retrieve storage connection string" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✓ Connection string retrieved" -ForegroundColor Green
    
    # Create followers table
    Write-Host "`nTable: followers" -ForegroundColor White
    Write-Host "Purpose: Store follower state (actor URI, inbox URL, metadata)" -ForegroundColor Gray
    
    try {
        $existingTable = az storage table exists --name followers --connection-string $storageConnectionString --query exists --output tsv
        
        if ($existingTable -eq "true") {
            Write-Host "✓ Table already exists" -ForegroundColor Yellow
        } else {
            az storage table create `
                --name followers `
                --connection-string $storageConnectionString `
                --output none
            
            Write-Host "✓ Table created: followers" -ForegroundColor Green
        }
    } catch {
        Write-Host "✗ Failed to create followers table: $_" -ForegroundColor Red
        exit 1
    }
    
    # Create deliverystatus table
    Write-Host "`nTable: deliverystatus" -ForegroundColor White
    Write-Host "Purpose: Track post delivery status (pending, delivered, failed)" -ForegroundColor Gray
    
    try {
        $existingTable = az storage table exists --name deliverystatus --connection-string $storageConnectionString --query exists --output tsv
        
        if ($existingTable -eq "true") {
            Write-Host "✓ Table already exists" -ForegroundColor Yellow
        } else {
            az storage table create `
                --name deliverystatus `
                --connection-string $storageConnectionString `
                --output none
            
            Write-Host "✓ Table created: deliverystatus" -ForegroundColor Green
        }
    } catch {
        Write-Host "✗ Failed to create deliverystatus table: $_" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "[DRY RUN] Would create tables: followers, deliverystatus" -ForegroundColor Yellow
}

# ══════════════════════════════════════════════════════════════════════

Write-Host "`n╔══════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║ Step 3: Creating Queue Storage Queues                               ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

if (-not $DryRun) {
    # Create accept-delivery queue
    Write-Host "`nQueue: accept-delivery" -ForegroundColor White
    Write-Host "Purpose: Async Accept activity delivery to new followers" -ForegroundColor Gray
    
    try {
        $existingQueue = az storage queue exists --name accept-delivery --connection-string $storageConnectionString --query exists --output tsv
        
        if ($existingQueue -eq "true") {
            Write-Host "✓ Queue already exists" -ForegroundColor Yellow
        } else {
            az storage queue create `
                --name accept-delivery `
                --connection-string $storageConnectionString `
                --output none
            
            Write-Host "✓ Queue created: accept-delivery" -ForegroundColor Green
        }
    } catch {
        Write-Host "✗ Failed to create accept-delivery queue: $_" -ForegroundColor Red
        exit 1
    }
    
    # Create activitypub-delivery queue
    Write-Host "`nQueue: activitypub-delivery" -ForegroundColor White
    Write-Host "Purpose: Async post delivery to all follower inboxes" -ForegroundColor Gray
    
    try {
        $existingQueue = az storage queue exists --name activitypub-delivery --connection-string $storageConnectionString --query exists --output tsv
        
        if ($existingQueue -eq "true") {
            Write-Host "✓ Queue already exists" -ForegroundColor Yellow
        } else {
            az storage queue create `
                --name activitypub-delivery `
                --connection-string $storageConnectionString `
                --output none
            
            Write-Host "✓ Queue created: activitypub-delivery" -ForegroundColor Green
        }
    } catch {
        Write-Host "✗ Failed to create activitypub-delivery queue: $_" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "[DRY RUN] Would create queues: accept-delivery, activitypub-delivery" -ForegroundColor Yellow
}

# ══════════════════════════════════════════════════════════════════════

Write-Host "`n╔══════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║ Step 4: Creating Application Insights                               ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

Write-Host "Application Insights: $AppInsightsName" -ForegroundColor White
Write-Host "Purpose: Monitoring, logging, and performance tracking" -ForegroundColor Gray

$appInsightsConnectionString = $null
$appInsightsKey = $null

if (-not $DryRun) {
    try {
        # Check if Application Insights already exists
        $existingAppInsights = az monitor app-insights component show `
            --app $AppInsightsName `
            --resource-group $ResourceGroup `
            --output json 2>$null | ConvertFrom-Json
        
        if ($existingAppInsights) {
            Write-Host "✓ Application Insights already exists" -ForegroundColor Yellow
        } else {
            Write-Host "Creating Application Insights..." -ForegroundColor White
            az monitor app-insights component create `
                --app $AppInsightsName `
                --location $Location `
                --resource-group $ResourceGroup `
                --application-type web `
                --output none
            
            Write-Host "✓ Application Insights created" -ForegroundColor Green
        }
        
        # Get Application Insights connection string and key
        Write-Host "Retrieving Application Insights connection string..." -ForegroundColor White
        $appInsightsConnectionString = az monitor app-insights component show `
            --app $AppInsightsName `
            --resource-group $ResourceGroup `
            --query connectionString `
            --output tsv
        
        $appInsightsKey = az monitor app-insights component show `
            --app $AppInsightsName `
            --resource-group $ResourceGroup `
            --query instrumentationKey `
            --output tsv
        
        Write-Host "✓ Application Insights configured" -ForegroundColor Green
    } catch {
        Write-Host "✗ Failed to create Application Insights: $_" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "[DRY RUN] Would create Application Insights: $AppInsightsName" -ForegroundColor Yellow
}

# ══════════════════════════════════════════════════════════════════════
# Summary
# ══════════════════════════════════════════════════════════════════════

Write-Host "`n╔══════════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║ ✓ Azure Resources Created Successfully                              ║" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════════════════════════════╝" -ForegroundColor Green

if (-not $DryRun) {
    Write-Host "`n📋 Connection Strings for GitHub Secrets:" -ForegroundColor Yellow
    Write-Host "─────────────────────────────────────────────────────────────────────" -ForegroundColor Gray
    Write-Host "`nACTIVITYPUB_STORAGE_CONNECTION:" -ForegroundColor Cyan
    Write-Host $storageConnectionString -ForegroundColor White
    
    Write-Host "`nAPPINSIGHTS_CONNECTION_STRING:" -ForegroundColor Cyan
    Write-Host $appInsightsConnectionString -ForegroundColor White
    
    Write-Host "`nAPPINSIGHTS_INSTRUMENTATION_KEY:" -ForegroundColor Cyan
    Write-Host $appInsightsKey -ForegroundColor White
    
    Write-Host "`n─────────────────────────────────────────────────────────────────────" -ForegroundColor Gray
    
    Write-Host "`n📝 Next Steps:" -ForegroundColor Yellow
    Write-Host "  1. Copy the connection strings above" -ForegroundColor White
    Write-Host "  2. Add them to GitHub repository secrets:" -ForegroundColor White
    Write-Host "     • Settings → Secrets and variables → Actions → New repository secret" -ForegroundColor Gray
    Write-Host "  3. Add connection strings to Azure Function App settings:" -ForegroundColor White
    Write-Host "     • Azure Portal → Function App → Configuration → Application settings" -ForegroundColor Gray
    Write-Host "  4. Proceed with Phase 4A implementation (Inbox Handler)" -ForegroundColor White
    
    Write-Host "`n📊 Resources Created:" -ForegroundColor Yellow
    Write-Host "  ✓ Storage Account: $StorageAccountName" -ForegroundColor Green
    Write-Host "    • Table: followers" -ForegroundColor Green
    Write-Host "    • Table: deliverystatus" -ForegroundColor Green
    Write-Host "    • Queue: accept-delivery" -ForegroundColor Green
    Write-Host "    • Queue: activitypub-delivery" -ForegroundColor Green
    Write-Host "  ✓ Application Insights: $AppInsightsName" -ForegroundColor Green
    
    Write-Host "`n💰 Estimated Monthly Cost: ~`$0.01-0.02" -ForegroundColor Cyan
    Write-Host "   (Free tier covers most usage for typical follower counts)" -ForegroundColor Gray
} else {
    Write-Host "`n[DRY RUN] No resources were created. Run without -DryRun to create resources." -ForegroundColor Yellow
}

Write-Host ""
