# Azure Static Web Apps Free Tier Monitoring Guide

## Overview

Azure Static Web Apps **Free tier includes built-in metrics at no additional cost**. These metrics provide essential monitoring capabilities without requiring Application Insights or any paid services.

## Critical Distinction

| Service | Cost | Capabilities |
|---------|------|--------------|
| **Built-in Metrics** | **FREE** (included) | Request counts, error counts, latency, basic monitoring |
| **Application Insights** | **PAID** (separate service) | Detailed stack traces, root cause analysis, query logs |

**For most debugging scenarios, the FREE built-in metrics are sufficient.**

## Available Free Metrics

Azure Static Web Apps Free tier automatically tracks these metrics:

- **FunctionHits**: Count of successful API function invocations
- **FunctionErrors**: Count of failed API function invocations
- **SiteHits**: Count of successful requests to static content
- **SiteErrors**: Count of failed requests to static content
- **BytesSent**: Outgoing bytes from the application
- **CdnPercentageOf4XX**: Percentage of 4xx client errors
- **CdnPercentageOf5XX**: Percentage of 5xx server errors
- **CdnRequestCount**: Number of CDN requests (when enterprise edge enabled)
- **CdnResponseSize**: Size of CDN responses
- **CdnTotalLatency**: CDN latency in milliseconds
- **DataApiErrors**: Errors from Data API calls
- **DataApiHits**: Successful Data API hits

## Resource Information

> **Security Note**: Subscription IDs are **not secrets** and cannot be used to access resources by themselves. However, they should not be published in public documentation as they can aid reconnaissance. The values below are for **internal use only** in private repositories.

```bash
# Static Web App Details
Resource Name: luisquintanillame-static
Resource Group: luisquintanillameblog-rg
Subscription ID: [YOUR_SUBSCRIPTION_ID]  # Replace with actual ID when used internally
Region: eastus2
Hostname: nice-wave-017e3760f.2.azurestaticapps.net
```

## Essential Monitoring Commands

### 1. Check Function Error Rate (Daily)

```powershell
# Define your resource path once
$subscriptionId = "[YOUR_SUBSCRIPTION_ID]"
$resourcePath = "/subscriptions/$subscriptionId/resourceGroups/luisquintanillameblog-rg/providers/Microsoft.Web/staticSites/luisquintanillame-static"

# Query function errors
az monitor metrics list `
  --resource $resourcePath `
  --metric "FunctionErrors" `
  --interval P1D `
  --start-time 2026-01-22T00:00:00Z `
  --end-time 2026-01-30T00:00:00Z `
  --output json
```

# Query function successes
az monitor metrics list `
  --resource $resourcePath
### 2. Check Function Success Rate (Daily)

```powershell
az monitor metrics list `
  --resource /subscriptions/0ecbb599-849f-4d64-a97d-808abd3b8572/resourceGroups/luisquintanillameblog-rg/providers/Microsoft.Web/staticSites/luisquintanillame-static `
  --metric "FunctionHits" `
  --interval P1D `
  --start-time 2026-01-22T00:00:00Z `
  --end-time 2026-01-30T00:00:00Z `
  --output json
```

**Use this to**: Compare success rate vs error rate to identify catastrophic failures

### 3. Check Hourly Metrics (Fine-Grained)

# Query hourly for fine-grained analysis
az monitor metrics list `
  --resource $resourcePath
  --resource /subscriptions/0ecbb599-849f-4d64-a97d-808abd3b8572/resourceGroups/luisquintanillameblog-rg/providers/Microsoft.Web/staticSites/luisquintanillame-static `
  --metric "FunctionErrors" `
  --interval PT1H `
  --start-time 2026-01-29T00:00:00Z `
  --end-time 2026-01-29T23:59:59Z `
  --output json
```

**Use this to**: Pinpoint exact hour when failures started

### 4. Check All Metrics (Overview)

# List all available metrics
az monitor metrics list-definitions `
  --resource $resourcePath
  --resource /subscriptions/0ecbb599-849f-4d64-a97d-808abd3b8572/resourceGroups/luisquintanillameblog-rg/providers/Microsoft.Web/staticSites/luisquintanillame-static `
  --output table
```

**Use this to**: See all available metrics for the Static Web App

### 5. Check Recent Deployments

```powershell
# List recent deployment runs
gh run list --limit 10 --repo lqdev/luisquintanilla.me --workflow "Azure Static Web Apps CI/CD"

# View specific deployment details
gh run view <RUN_ID> --repo lqdev/luisquintanilla.me

# Check for failed deployments
gh run view <RUN_ID> --log-failed --repo lqdev/luisquintanilla.me
```

**Use this to**: Correlate failures with deployment timing

### 6. Test API Endpoints Directly

```powershell
# Test with status code
curl -sL -w "`nHTTP Status: %{http_code}" "https://www.lqdev.me/api/test-health"

# Test ActivityPub endpoints
curl -sL "https://www.lqdev.me/api/activitypub/actor" | Select-Object -First 20
curl -sL "https://www.lqdev.me/api/activitypub/outbox" | Select-Object -First 20
```

**Use this to**: Verify API functionality after fixes

## Diagnostic Workflow

### Step 1: Identify the Problem Window

1. Query FunctionErrors and FunctionHits for the past 7 days (daily interval)
2. Calculate error rate: `Errors / (Hits + Errors) * 100`
3. Identify the day when error rate spiked significantly

### Step 2: Narrow Down Timing

1. Use hourly interval (PT1H) for the identified problem day
2. Find the exact hour when failures started
3. Document timestamp for correlation

### Step 3: Correlate with Changes

```powershell
# Check commits around the failure timestamp
git log --oneline --format="%h %ad %s" --date=short --since="YYYY-MM-DD" --until="YYYY-MM-DD"

# Check API-specific changes
git log --oneline --format="%h %ad %s" --date=short --since="YYYY-MM-DD" --until="YYYY-MM-DD" -- api/

# Check deployments
gh run list --created "YYYY-MM-DD" --repo lqdev/luisquintanilla.me
```

### Step 4: Analyze Pattern

| Error Pattern | Likely Cause |
|--------------|--------------|
| **Gradual increase** (13% → 35%) | Code issue, rate limiting building up |
| **Sudden cliff** (1753 hits → 14 hits) | Platform change, breaking deployment |
| **100% errors, 0 hits** | Complete system failure, config issue |
| **High errors but stable hits** | Intermittent issue, DDoS, specific endpoint failing |

## Real-World Example (Jan 28, 2026 Incident)

### The Data

| Date | Hits | Errors | Error Rate |
|------|------|--------|------------|
| Jan 22 | 2,661 | 169 | 6% |
| Jan 23 | 4,350 | 694 | 14% |
| Jan 24 | 5,814 | 902 | 13% |
| Jan 25 | 3,339 | 899 | 21% |
| Jan 26 | 1,265 | 686 | 35% |
| Jan 27 | 1,753 | 863 | 33% |
| **Jan 28** | **14** | **2,159** | **99.4%** |
| **Jan 29** | **0** | **2,630** | **100%** |

### The Analysis

1. **Pattern**: Gradual error increase (Jan 23-27) followed by catastrophic cliff (Jan 28)
2. **Not DDoS**: Total traffic (hits + errors) remained consistent ~1000-6000/day
3. **Platform-level failure**: Successful hits dropped to zero, not just increased errors
4. **Timing correlation**: No code changes between Jan 23 and Jan 28

### The Investigation

```powershell
# 1. Checked what changed
git log --oneline --since="2026-01-26" --until="2026-01-29" -- api/
# Result: No API changes after Jan 23

# 2. Checked Azure Functions configuration
# Discovered: Extension bundle v3 (deprecated) in host.json

# 3. Checked package.json
# Discovered: "main": "files/index.js" pointing to non-existent file

# 4. Root cause: Invalid package.json + stricter validation in extension bundle v4
```

### The Fix

1. Updated extension bundle to v4: `[4.0.0, 5.0.0)`
2. Removed invalid `"main"` entry from package.json
3. Deployed fixes → **100% recovery**

## Key Learnings

### When You See 100% Error Rate:
1. **Check package.json** for invalid entry points
2. **Check host.json** for deprecated extension bundles
3. **Check function.json** files for syntax errors
4. **Check Node.js version** constraints and runtime settings

### Common Free Tier Gotchas:
- Extension bundle v3 is **deprecated** - use v4: `[4.0.0, 5.0.0)`
- Node.js 22 has compatibility issues - constrain to `">=18.0.0 <22.0.0"`
- Invalid `"main"` in package.json can cause platform-level crashes
- staticwebapp.config.json needs `platform.apiRuntime: "node:20"`

### Monitoring Best Practices:
1. **Always check built-in metrics FIRST** - they're free and usually sufficient
2. **Only enable App Insights if you need detailed stack traces**
3. **Set up daily metric checks** to catch issues early
4. **Correlate metrics with deployment timing** using gh CLI
5. **Document baseline metrics** (typical hit/error rates) for comparison

## When to Upgrade Beyond Free Tier

Consider Application Insights (paid) if you need:
- Detailed error stack traces and exception messages
- Root cause analysis with full diagnostic context
- Query logs with Kusto (KQL) language
- Performance analytics beyond basic metrics
- Dependency tracking and application mapping
- Custom telemetry and tracing

**Cost Control**: If you enable App Insights, configure sampling and log levels in host.json to stay within the 5GB/month free tier:

```json
{
  "version": "2.0",
  "logging": {
    "applicationInsights": {
      "samplingSettings": {
        "isEnabled": true
      },
      "enableDependencyTracking": false
    },
    "logLevels": {
      "default": "Warning"
    }
  }
}
```

## Azure Portal Alternative

If CLI commands aren't working, use Azure Portal:

1. Navigate to: https://portal.azure.com
2. Find resource: `luisquintanillame-static`
3. Click **Monitoring** → **Metrics**
4. Select metric (e.g., "FunctionErrors")
5. Set time range and view chart
6. Use "Diagnose and solve problems" for 24-hour diagnostics (free)

## Quick Reference
Set your subscription ID and resource path (one-time setup)
$subscriptionId = "[YOUR_SUBSCRIPTION_ID]"
$resource = "/subscriptions/$subscriptionId
# Alias the resource path for convenience
$resource = "/subscriptions/0ecbb599-849f-4d64-a97d-808abd3b8572/resourceGroups/luisquintanillameblog-rg/providers/Microsoft.Web/staticSites/luisquintanillame-static"

# Get last 7 days of errors
az monitor metrics list --resource $resource --metric "FunctionErrors" --interval P1D --start-time (Get-Date).AddDays(-7).ToString("yyyy-MM-ddT00:00:00Z") --end-time (Get-Date).ToString("yyyy-MM-ddT00:00:00Z") --output json

# Get last 7 days of hits
az monitor metrics list --resource $resource --metric "FunctionHits" --interval P1D --start-time (Get-Date).AddDays(-7).ToString("yyyy-MM-ddT00:00:00Z") --end-time (Get-Date).ToString("yyyy-MM-ddT00:00:00Z") --output json
```

## Summary

**Remember**: Azure Static Web Apps Free tier includes robust monitoring capabilities at **zero additional cost**. The built-in metrics are accessible through Azure CLI and provide sufficient data for 95% of debugging scenarios. Only consider Application Insights if you specifically need detailed diagnostic traces and are willing to manage the separate billing.
