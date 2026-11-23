# Phase 4 Implementation Plan - Production-Ready ActivityPub Delivery

**Date**: January 18, 2026  
**Status**: Implementation Ready  
**Approach**: Production-Ready with Phased Rollout  
**Architecture**: Option A (Static followers.json + Table Storage)

---

## Executive Summary

Implementing full ActivityPub federation with inbox handler and post delivery capabilities using Azure Functions, Table Storage, Queue Storage, and Application Insights. Follower state managed in Table Storage with static followers.json regenerated during builds.

---

## Architectural Decisions

### Decision 1: Production-Ready Architecture

**Decision**: Implement queue-based asynchronous delivery with full monitoring from the start.

**Rationale**:
- Prevents blocking on slow remote servers
- Enables proper retry logic with exponential backoff
- Provides production-quality monitoring from day one
- Scales naturally with follower growth
- Minimal cost difference vs. minimal approach (~$0.02/month)

**Components**:
- Azure Functions (already have)
- Azure Key Vault (already have)
- **Azure Table Storage** (new - follower state)
- **Azure Queue Storage** (new - async delivery)
- **Application Insights** (new - monitoring)

---

### Decision 2: Static followers.json + Table Storage (Option A)

**Decision**: Maintain static `followers.json` file regenerated from Table Storage during builds.

**Rationale**:
- **Public accessibility**: Static file remains accessible for federation spec compliance
- **Source of truth**: Table Storage handles dynamic updates from Follow activities
- **Build integration**: Regenerate during normal build process
- **Zero sync issues**: Single direction flow (Table Storage → static file)
- **Debugging**: Static file provides visible follower list for troubleshooting

**Implementation**:
```fsharp
// During build: Read from Table Storage, write to static file
let buildFollowersCollection (tableStorage: IFollowerStore) (outputDir: string) = async {
    let! followers = tableStorage.GetAllFollowers()
    let collection = ActivityPubFollowersCollection.create followers
    do! writeJsonFile (Path.Combine(outputDir, "api/activitypub/followers/index.json")) collection
}
```

---

### Decision 3: URL Pattern - `/api/activitypub/*`

**Decision**: All ActivityPub endpoints follow `/api/activitypub/*` pattern.

**Rationale**:
- **Consistency**: Aligns with established architecture in `/docs/activitypub/implementation-status.md`
- **Future-proof**: Enables other `/api/*` functionality for non-ActivityPub features
- **Logical grouping**: All ActivityPub endpoints organized under single prefix
- **Explicit**: Clear distinction between ActivityPub and other API features

**URL Structure**:
```
/.well-known/webfinger              → Discovery (Azure Function)
/api/activitypub/actor             → Actor profile (Azure Function)
/api/activitypub/inbox             → Receive activities (Azure Function - NEW)
/api/activitypub/outbox            → Public activities (Static file)
/api/activitypub/followers         → Followers collection (Static file)
/api/activitypub/following         → Following collection (Static file)
```

---

### Decision 4: Phased Implementation Approach

**Decision**: Implement in 3 phases with validation between each phase.

**Rationale**:
- **Risk mitigation**: Test each component independently
- **Incremental value**: Accept followers before implementing delivery
- **Debugging**: Isolate issues to specific phase
- **Flexibility**: Can pause between phases for production validation

**Phases**:
1. **Phase 4A**: Inbox handler + follower management (1-2 days)
2. **Phase 4B**: Delivery queue + worker infrastructure (1-2 days)
3. **Phase 4C**: Full delivery integration + monitoring (1-2 days)

---

## Azure Resources Required

### New Resources to Create

| Resource | Name | Purpose | Estimated Cost |
|----------|------|---------|----------------|
| **Storage Account** | `lqdevactivitypub` | Table + Queue storage | $0.01-0.02/month |
| **Table** | `followers` | Follower state storage | Included in storage account |
| **Table** | `deliverystatus` | Delivery tracking | Included in storage account |
| **Queue** | `activitypub-delivery` | Async delivery tasks | Included in storage account |
| **Application Insights** | `lqdev-activitypub-insights` | Monitoring & logging | $0/month (free tier) |

**Total New Monthly Cost**: ~$0.01-0.02/month (essentially free)

### Existing Resources (Already Have)

| Resource | Purpose | Status |
|----------|---------|--------|
| Azure Functions App | Serverless compute | ✅ Existing |
| Azure Key Vault | Private key storage | ✅ Existing |
| Azure Blob Storage | Static site hosting | ✅ Existing |

---

## Implementation Phases

### Phase 4A: Inbox Handler & Follower Management (1-2 days)

**Objective**: Accept Follow requests and build follower list in Table Storage.

#### Azure Functions to Create

**Function 1: `InboxHandler`**
- **Trigger**: HTTP POST `/api/activitypub/inbox`
- **Purpose**: Process incoming Follow/Unfollow activities
- **Operations**:
  1. Validate HTTP signature
  2. Parse Follow/Unfollow activity
  3. Store/remove follower in Table Storage
  4. Queue Accept activity for delivery
  5. Return 200 OK immediately

**Function 2: `ProcessAccept`**
- **Trigger**: Azure Queue `accept-delivery`
- **Purpose**: Send Accept activities to new followers
- **Operations**:
  1. Retrieve Accept activity from queue
  2. Fetch remote actor inbox URL
  3. Generate HTTP signature with Key Vault
  4. POST Accept activity to follower inbox
  5. Handle retry on failure (automatic via queue)
  6. Log to Application Insights

#### Table Storage Schema: `followers`

```
PartitionKey: "follower"
RowKey: {actorUri}  // e.g., "https://mastodon.social/users/alice"
Fields:
  - ActorUri: string
  - InboxUrl: string
  - SharedInbox: string (optional)
  - DisplayName: string
  - Domain: string
  - FollowedAt: DateTime
  - PublicKeyPem: string (cached)
  - AvatarUrl: string (optional)
  - LastActivityAt: DateTime (optional)
```

#### Queue Storage: `accept-delivery`

**Message Format**:
```json
{
  "activityType": "Accept",
  "targetInbox": "https://mastodon.social/users/alice/inbox",
  "activityJson": "{...}",  // Complete Accept activity
  "attemptCount": 0,
  "queuedAt": "2026-01-18T15:30:00Z"
}
```

#### F# Modules to Create

1. **`Services/HttpSignature.fs`**: HTTP signature generation/validation
2. **`Services/FollowerStore.fs`**: Table Storage abstraction for followers
3. **`Services/ActivityQueue.fs`**: Queue Storage abstraction for delivery
4. **Functions/InboxHandler.fs`**: Inbox HTTP endpoint logic
5. **`Functions/ProcessAccept.fs`**: Accept delivery worker

#### Success Criteria

- ✅ Receive Follow activity from test Mastodon account
- ✅ Follower stored in Table Storage
- ✅ Accept activity delivered to follower
- ✅ Follower sees "Following" status on Mastodon
- ✅ Table Storage queryable via Azure Portal

---

### Phase 4B: Post Delivery Infrastructure (1-2 days)

**Objective**: Build delivery queue system for broadcasting posts to followers.

#### Azure Functions to Create

**Function 3: `QueueDeliveryTasks`**
- **Trigger**: HTTP POST `/api/activitypub/trigger-delivery` (called by GitHub Actions)
- **Purpose**: Queue delivery tasks for new content
- **Operations**:
  1. Read new activities from outbox
  2. Retrieve all followers from Table Storage
  3. Group followers by shared inbox
  4. Create delivery task for each unique inbox
  5. Queue tasks to `activitypub-delivery`
  6. Return delivery summary

**Function 4: `ProcessDelivery`**
- **Trigger**: Azure Queue `activitypub-delivery`
- **Purpose**: Deliver activities to follower inboxes
- **Operations**:
  1. Retrieve delivery task from queue
  2. Generate HTTP signature with Key Vault
  3. POST activity to remote inbox
  4. Handle status codes (retry 5xx, don't retry 4xx)
  5. Update delivery status in Table Storage
  6. Log to Application Insights

#### Table Storage Schema: `deliverystatus`

```
PartitionKey: {activityId}  // e.g., "https://lqdev.me/api/activitypub/notes/abc123"
RowKey: {targetInbox}       // e.g., "https://mastodon.social/inbox"
Fields:
  - ActivityId: string
  - TargetInbox: string
  - Status: string (pending/delivered/failed)
  - AttemptCount: int
  - LastAttempt: DateTime
  - NextRetry: DateTime (optional)
  - HttpStatusCode: int (optional)
  - ErrorMessage: string (optional)
  - DeliveredAt: DateTime (optional)
```

#### Queue Storage: `activitypub-delivery`

**Message Format**:
```json
{
  "activityId": "https://lqdev.me/api/activitypub/notes/abc123",
  "activityJson": "{...}",  // Complete Create activity
  "targetInbox": "https://mastodon.social/inbox",
  "followerActors": ["https://mastodon.social/users/alice", "..."],
  "attemptCount": 0,
  "queuedAt": "2026-01-18T15:30:00Z"
}
```

#### F# Modules to Create

1. **`Services/DeliveryQueue.fs`**: Delivery queue management
2. **`Services/DeliveryStatus.fs`**: Table Storage for delivery tracking
3. **`Services/InboxDiscovery.fs`**: Inbox URL extraction and caching
4. **`Functions/QueueDeliveryTasks.fs`**: Delivery queueing endpoint
5. **`Functions/ProcessDelivery.fs`**: Delivery worker

#### Success Criteria

- ✅ Trigger delivery queue from GitHub Actions
- ✅ Delivery tasks queued for all followers
- ✅ Activities delivered to test follower inboxes
- ✅ Test follower sees post in timeline
- ✅ Delivery status tracked in Table Storage
- ✅ Errors logged to Application Insights

---

### Phase 4C: Full Integration & Monitoring (1-2 days)

**Objective**: Complete integration with build process and production monitoring.

#### GitHub Actions Updates

**Current Workflow** (`.github/workflows/publish-azure-static-web-apps.yml`):
```yaml
- name: Generate ActivityPub files
  run: |
    dotnet fsi ./Scripts/rss-to-activitypub.fsx -- \
    --rss-path "./_public/feed/feed.xml" \
    --static-path "./_public" \
    --notes-path "social/notes" \
    --outbox-path "social/outbox"
```

**New Workflow** (Updated):
```yaml
- name: Generate ActivityPub outbox
  run: dotnet run  # Phase 3 already does this

- name: Regenerate followers.json from Table Storage
  env:
    AZURE_STORAGE_CONNECTION_STRING: ${{ secrets.ACTIVITYPUB_STORAGE_CONNECTION }}
  run: dotnet fsi ./Scripts/regenerate-followers.fsx

- name: Trigger activity delivery to followers
  env:
    AZURE_FUNCTION_URL: ${{ secrets.ACTIVITYPUB_DELIVERY_FUNCTION_URL }}
    AZURE_FUNCTION_KEY: ${{ secrets.ACTIVITYPUB_DELIVERY_FUNCTION_KEY }}
  run: |
    # Call Azure Function to queue delivery tasks
    curl -X POST "$AZURE_FUNCTION_URL/api/activitypub/trigger-delivery" \
      -H "Content-Type: application/json" \
      -H "x-functions-key: $AZURE_FUNCTION_KEY" \
      -d '{"trigger":"github-actions"}'
```

#### F# Scripts to Create

1. **`Scripts/regenerate-followers.fsx`**: Read Table Storage, write followers.json
2. **`Scripts/test-inbox-handler.fsx`**: Test Follow/Accept workflow
3. **`Scripts/test-delivery.fsx`**: Test post delivery to test account

#### Application Insights Dashboards

**Monitoring Queries**:

1. **Follower Activity**:
```kusto
customEvents
| where name == "FollowerAdded" or name == "FollowerRemoved"
| project timestamp, name, customDimensions.ActorUri, customDimensions.Domain
| order by timestamp desc
```

2. **Delivery Success Rate**:
```kusto
customEvents
| where name == "ActivityDelivered" or name == "DeliveryFailed"
| summarize 
    Total = count(),
    Successful = countif(name == "ActivityDelivered"),
    Failed = countif(name == "DeliveryFailed")
  by bin(timestamp, 1h)
| extend SuccessRate = (Successful * 100.0) / Total
```

3. **Delivery Latency**:
```kusto
dependencies
| where type == "Http" and target contains "inbox"
| summarize 
    avg(duration),
    percentile(duration, 50),
    percentile(duration, 95),
    percentile(duration, 99)
  by bin(timestamp, 1h)
```

#### Success Criteria

- ✅ Build triggers follower regeneration automatically
- ✅ Build triggers delivery queueing automatically
- ✅ New posts delivered within 5 minutes of publish
- ✅ Application Insights shows all deliveries
- ✅ Dashboard shows follower growth
- ✅ Dashboard shows delivery success rate
- ✅ Error alerts configured for delivery failures

---

## Azure Resource Creation Script

**Script**: `scripts/setup-activitypub-azure-resources.ps1`

```powershell
# Azure ActivityPub Resources Setup Script
# Prerequisites: Azure CLI installed and authenticated

param(
    [string]$ResourceGroup = "lqdev-website-rg",
    [string]$Location = "eastus",
    [string]$StorageAccountName = "lqdevactivitypub",
    [string]$AppInsightsName = "lqdev-activitypub-insights"
)

Write-Host "Creating Azure resources for ActivityPub implementation..." -ForegroundColor Green

# 1. Create Storage Account (if not exists)
Write-Host "`n1. Creating Storage Account: $StorageAccountName" -ForegroundColor Cyan
az storage account create `
    --name $StorageAccountName `
    --resource-group $ResourceGroup `
    --location $Location `
    --sku Standard_LRS `
    --kind StorageV2 `
    --min-tls-version TLS1_2

# 2. Get Storage Connection String
Write-Host "`n2. Retrieving Storage Connection String..." -ForegroundColor Cyan
$storageConnectionString = az storage account show-connection-string `
    --name $StorageAccountName `
    --resource-group $ResourceGroup `
    --query connectionString `
    --output tsv

# 3. Create Table Storage Tables
Write-Host "`n3. Creating Table Storage tables..." -ForegroundColor Cyan
az storage table create `
    --name followers `
    --connection-string $storageConnectionString

az storage table create `
    --name deliverystatus `
    --connection-string $storageConnectionString

# 4. Create Queue Storage Queues
Write-Host "`n4. Creating Queue Storage queues..." -ForegroundColor Cyan
az storage queue create `
    --name accept-delivery `
    --connection-string $storageConnectionString

az storage queue create `
    --name activitypub-delivery `
    --connection-string $storageConnectionString

# 5. Create Application Insights
Write-Host "`n5. Creating Application Insights..." -ForegroundColor Cyan
az monitor app-insights component create `
    --app $AppInsightsName `
    --location $Location `
    --resource-group $ResourceGroup `
    --application-type web

# 6. Get Application Insights Connection String
Write-Host "`n6. Retrieving Application Insights Connection String..." -ForegroundColor Cyan
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

Write-Host "`n✅ Azure resources created successfully!" -ForegroundColor Green
Write-Host "`n📋 Connection strings to add to GitHub Secrets:" -ForegroundColor Yellow
Write-Host "ACTIVITYPUB_STORAGE_CONNECTION: $storageConnectionString"
Write-Host "APPINSIGHTS_CONNECTION_STRING: $appInsightsConnectionString"
Write-Host "APPINSIGHTS_INSTRUMENTATION_KEY: $appInsightsKey"

Write-Host "`n📝 Next Steps:" -ForegroundColor Cyan
Write-Host "1. Add connection strings to GitHub repository secrets"
Write-Host "2. Add connection strings to Azure Function App settings"
Write-Host "3. Proceed with Phase 4A implementation"
```

---

## GitHub Secrets Required

**New Secrets to Add** (after running setup script):

| Secret Name | Source | Purpose |
|-------------|--------|---------|
| `ACTIVITYPUB_STORAGE_CONNECTION` | Setup script output | Table/Queue storage access |
| `APPINSIGHTS_CONNECTION_STRING` | Setup script output | Application Insights logging |
| `APPINSIGHTS_INSTRUMENTATION_KEY` | Setup script output | Application Insights SDK |
| `ACTIVITYPUB_DELIVERY_FUNCTION_URL` | Azure Portal (after deploy) | Trigger delivery endpoint |
| `ACTIVITYPUB_DELIVERY_FUNCTION_KEY` | Azure Portal (after deploy) | Function authentication |

**Existing Secrets** (already have):
- `AZURE_KEY_VAULT_NAME`: Key Vault for private key
- Azure Static Web Apps deployment token

---

## Implementation Checklist

### Phase 4A: Inbox Handler (Week 1)

- [ ] Run Azure resource creation script
- [ ] Add GitHub secrets
- [ ] Create `Services/HttpSignature.fs` module
- [ ] Create `Services/FollowerStore.fs` module
- [ ] Create `Services/ActivityQueue.fs` module
- [ ] Create `Functions/InboxHandler.fs` Azure Function
- [ ] Create `Functions/ProcessAccept.fs` Azure Function
- [ ] Update Azure Function App settings with connection strings
- [ ] Deploy Azure Functions
- [ ] Test Follow request from test Mastodon account
- [ ] Verify follower in Table Storage
- [ ] Verify Accept delivered
- [ ] Update documentation

### Phase 4B: Delivery Infrastructure (Week 2)

- [ ] Create `Services/DeliveryQueue.fs` module
- [ ] Create `Services/DeliveryStatus.fs` module
- [ ] Create `Services/InboxDiscovery.fs` module
- [ ] Create `Functions/QueueDeliveryTasks.fs` Azure Function
- [ ] Create `Functions/ProcessDelivery.fs` Azure Function
- [ ] Deploy updated Azure Functions
- [ ] Test delivery queueing manually
- [ ] Test delivery to test follower
- [ ] Verify delivery status in Table Storage
- [ ] Verify Application Insights logging
- [ ] Update documentation

### Phase 4C: Full Integration (Week 2-3)

- [ ] Create `Scripts/regenerate-followers.fsx` script
- [ ] Update GitHub Actions workflow
- [ ] Add workflow secrets
- [ ] Test end-to-end publish workflow
- [ ] Configure Application Insights dashboards
- [ ] Configure error alerts
- [ ] Test with real followers
- [ ] Monitor first week of production delivery
- [ ] Update documentation
- [ ] Archive Phase 4 implementation complete

---

## Success Metrics

**Phase 4 Complete When**:

- ✅ Can accept Follow requests from any Mastodon instance
- ✅ Followers stored in Table Storage
- ✅ Static followers.json regenerated during builds
- ✅ Accept activities delivered successfully
- ✅ New posts delivered to all followers within 5 minutes
- ✅ HTTP signatures validated by Mastodon
- ✅ Delivery success rate >95%
- ✅ Application Insights shows all activity
- ✅ GitHub Actions workflow updated and working
- ✅ Zero manual intervention required for normal operation

---

## Rollback Plan

If issues arise during implementation:

**Phase 4A Rollback**:
- Disable `/api/activitypub/inbox` endpoint
- Followers remain in Table Storage (no data loss)
- No impact on existing static site

**Phase 4B Rollback**:
- Disable delivery trigger in GitHub Actions
- Inbox continues accepting followers
- No deliveries until issue resolved

**Phase 4C Rollback**:
- Revert GitHub Actions workflow changes
- Manual trigger for delivery (if needed)
- Full inbox functionality preserved

---

## Cost Monitoring

**Expected Monthly Costs** (100 followers, 4 posts/month):

| Service | Usage | Cost |
|---------|-------|------|
| Azure Functions | ~1,000 executions/month | $0 (free tier) |
| Table Storage | ~100 entities, 10K reads | $0.01 |
| Queue Storage | ~400 messages | $0.01 |
| Application Insights | ~500MB data/month | $0 (free tier) |
| **Total** | | **~$0.02/month** |

**Scaling Projections** (1,000 followers, 4 posts/month):

| Service | Usage | Cost |
|---------|-------|------|
| Azure Functions | ~10,000 executions/month | $0 (free tier) |
| Table Storage | ~1,000 entities, 100K reads | $0.05 |
| Queue Storage | ~4,000 messages | $0.02 |
| Application Insights | ~2GB data/month | $0 (free tier) |
| **Total** | | **~$0.07/month** |

---

## Documentation Updates Required

1. **`/api/ACTIVITYPUB.md`**: Add inbox handler and delivery endpoints
2. **`/docs/activitypub/implementation-status.md`**: Update Phase 4 status
3. **`/docs/activitypub/README.md`**: Add Phase 4 completion
4. **`/.github/workflows/publish-azure-static-web-apps.yml`**: Update workflow
5. **`/docs/activitypub/follower-management-architecture.md`**: Update with Table Storage details
6. **This file**: Archive as Phase 4 complete document

---

**Next Action**: Run Azure resource creation script and proceed with Phase 4A implementation.
