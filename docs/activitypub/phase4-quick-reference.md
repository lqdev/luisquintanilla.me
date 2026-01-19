# Phase 4 Quick Reference

Quick reference for Phase 4 implementation with essential commands and code patterns.

---

## Azure Resource Creation

### Create Resources

```powershell
cd c:\Dev\website

# Dry run first
.\scripts\setup-activitypub-azure-resources.ps1 -DryRun

# Create resources
.\scripts\setup-activitypub-azure-resources.ps1

# Custom resource group/location
.\scripts\setup-activitypub-azure-resources.ps1 -ResourceGroup "my-rg" -Location "westus2"
```

### Verify Resources

```powershell
# List storage account
az storage account show --name lqdevactivitypub --resource-group lqdev-website-rg

# List tables
az storage table list --account-name lqdevactivitypub

# List queues
az storage queue list --account-name lqdevactivitypub

# Show Application Insights
az monitor app-insights component show --app lqdev-activitypub-insights --resource-group lqdev-website-rg
```

---

## GitHub Secrets Configuration

After running the setup script, add these secrets to your GitHub repository:

1. Go to: `https://github.com/YOUR_USERNAME/YOUR_REPO/settings/secrets/actions`
2. Click **"New repository secret"**
3. Add each secret with the value from script output:

| Secret Name | Value Source |
|-------------|--------------|
| `ACTIVITYPUB_STORAGE_CONNECTION` | Connection string from script output |
| `APPINSIGHTS_CONNECTION_STRING` | Connection string from script output |
| `APPINSIGHTS_INSTRUMENTATION_KEY` | Instrumentation key from script output |

---

## Table Storage Schemas

### followers Table

```fsharp
type FollowerEntity = {
    PartitionKey: string        // Always "follower"
    RowKey: string              // Actor URI (e.g., "https://mastodon.social/users/alice")
    ActorUri: string            // Actor URI
    InboxUrl: string            // Personal inbox URL
    SharedInboxUrl: string option  // Shared inbox URL (preferred)
    Domain: string              // Server domain (e.g., "mastodon.social")
    FollowedAt: DateTime        // Timestamp when followed
    LastDeliveryAttempt: DateTime option  // Last delivery attempt
    LastDeliveryStatus: string option     // "success" | "failed" | "pending"
}
```

### deliverystatus Table

```fsharp
type DeliveryStatusEntity = {
    PartitionKey: string        // Activity ID (e.g., "post-12345")
    RowKey: string              // Follower actor URI
    ActivityId: string          // Activity ID
    ActorUri: string            // Follower actor URI
    Status: string              // "pending" | "delivered" | "failed"
    AttemptCount: int           // Number of delivery attempts
    LastAttempt: DateTime option  // Last attempt timestamp
    NextRetry: DateTime option    // Next retry timestamp (exponential backoff)
    ErrorMessage: string option   // Error details for failed deliveries
}
```

---

## Queue Message Formats

### accept-delivery Queue

```json
{
  "actorUri": "https://mastodon.social/users/alice",
  "inboxUrl": "https://mastodon.social/users/alice/inbox",
  "followActivityId": "https://mastodon.social/users/alice/activities/123",
  "timestamp": "2026-01-19T15:30:00Z"
}
```

### activitypub-delivery Queue

```json
{
  "activityId": "post-12345",
  "activityUri": "https://lqdev.me/api/activitypub/outbox/post-12345",
  "actorUri": "https://mastodon.social/users/alice",
  "inboxUrl": "https://mastodon.social/users/alice/inbox",
  "useSharedInbox": true,
  "attemptCount": 0,
  "timestamp": "2026-01-19T15:30:00Z"
}
```

---

## HTTP Signature Format

### Signature Header (RFC 9421 / cavage-12)

```http
Signature: keyId="https://lqdev.me/api/activitypub/actor#main-key",
           algorithm="rsa-sha256",
           headers="(request-target) host date digest",
           signature="base64encodedstring"
```

### Digest Header

```http
Digest: SHA-256=base64(sha256(body))
```

### F# Signature Generation Pattern

```fsharp
let generateSignature (privateKey: string) (targetUrl: string) (body: string) : string =
    let date = DateTime.UtcNow.ToString("R")  // RFC 1123 format
    let host = Uri(targetUrl).Host
    let path = Uri(targetUrl).PathAndQuery
    
    // Compute digest
    let bodyBytes = Encoding.UTF8.GetBytes(body)
    let digest = SHA256.HashData(bodyBytes) |> Convert.ToBase64String
    
    // Build signing string
    let signingString = 
        $"(request-target): post {path}\n" +
        $"host: {host}\n" +
        $"date: {date}\n" +
        $"digest: SHA-256={digest}"
    
    // Sign with RSA-SHA256
    let signature = signWithRSA privateKey signingString
    
    // Build signature header
    $"keyId=\"https://lqdev.me/api/activitypub/actor#main-key\"," +
    $"algorithm=\"rsa-sha256\"," +
    $"headers=\"(request-target) host date digest\"," +
    $"signature=\"{signature}\""
```

---

## F# Service Module Templates

### Services/HttpSignature.fs

```fsharp
module Services.HttpSignature

open System
open System.Security.Cryptography
open System.Text

type SignatureParams = {
    KeyId: string
    PrivateKey: string
    TargetUrl: string
    Body: string
    Date: DateTime option
}

let generateSignature (params: SignatureParams) : Result<string * string * string, string> =
    // Returns (signatureHeader, dateHeader, digestHeader)
    // Implementation with RSA-SHA256 signing
    Ok ("Signature: ...", "Date: ...", "Digest: ...")

let validateSignature (publicKey: string) (signature: string) (signingString: string) : bool =
    // Validate incoming signatures from remote servers
    true
```

### Services/FollowerStore.fs

```fsharp
module Services.FollowerStore

open Azure.Data.Tables
open System

type Follower = {
    ActorUri: string
    InboxUrl: string
    SharedInboxUrl: string option
    Domain: string
    FollowedAt: DateTime
}

let addFollower (connectionString: string) (follower: Follower) : Async<Result<unit, string>> =
    async {
        // Add follower to Table Storage
        return Ok ()
    }

let removeFollower (connectionString: string) (actorUri: string) : Async<Result<unit, string>> =
    async {
        // Remove follower from Table Storage
        return Ok ()
    }

let getFollowers (connectionString: string) : Async<Follower list> =
    async {
        // Get all followers from Table Storage
        return []
    }

let getFollowersByDomain (connectionString: string) : Async<Map<string, Follower list>> =
    async {
        // Group followers by domain for shared inbox optimization
        return Map.empty
    }
```

### Services/ActivityQueue.fs

```fsharp
module Services.ActivityQueue

open Azure.Storage.Queues
open System

type QueueMessage = {
    MessageId: string
    Content: string
    EnqueuedAt: DateTime
}

let enqueue (connectionString: string) (queueName: string) (message: string) : Async<Result<unit, string>> =
    async {
        // Add message to queue
        return Ok ()
    }

let dequeue (connectionString: string) (queueName: string) : Async<QueueMessage option> =
    async {
        // Get next message from queue
        return None
    }

let deleteMessage (connectionString: string) (queueName: string) (messageId: string) : Async<Result<unit, string>> =
    async {
        // Delete processed message
        return Ok ()
    }
```

---

## Azure Function Templates

### Functions/InboxHandler.fs

```fsharp
module Functions.InboxHandler

open Microsoft.Azure.Functions.Worker
open Microsoft.Azure.Functions.Worker.Http
open System.Net

[<Function("InboxHandler")>]
let run 
    ([<HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "activitypub/inbox")>] req: HttpRequestData)
    (context: FunctionContext)
    : Async<HttpResponseData> =
    async {
        // 1. Parse incoming activity
        // 2. Validate HTTP signature
        // 3. Handle Follow/Unfollow activities
        // 4. Store follower in Table Storage
        // 5. Queue Accept activity
        
        let response = req.CreateResponse(HttpStatusCode.Accepted)
        return response
    }
```

### Functions/ProcessAccept.fs

```fsharp
module Functions.ProcessAccept

open Microsoft.Azure.Functions.Worker
open Azure.Storage.Queues.Models

[<Function("ProcessAccept")>]
let run
    ([<QueueTrigger("accept-delivery")>] message: QueueMessage)
    (context: FunctionContext)
    : Async<unit> =
    async {
        // 1. Parse queue message (actorUri, inboxUrl, followActivityId)
        // 2. Generate Accept activity
        // 3. Sign with HTTP signature
        // 4. POST to follower's inbox
        // 5. Handle response (success/retry/fail)
        
        return ()
    }
```

---

## Testing Commands

### Test Inbox Handler (Local)

```bash
# Start Azure Functions locally
cd api
func start

# Send Follow activity
curl -X POST http://localhost:7071/api/activitypub/inbox \
  -H "Content-Type: application/activity+json" \
  -d '{
    "@context": "https://www.w3.org/ns/activitystreams",
    "type": "Follow",
    "actor": "https://mastodon.social/users/testuser",
    "object": "https://lqdev.me/api/activitypub/actor"
  }'
```

### Test with Real Mastodon

```bash
# Search for account on Mastodon
# Go to: https://mastodon.social
# Search: @lqdev@lqdev.me
# Click Follow

# Check Table Storage for new follower
az storage entity show \
  --account-name lqdevactivitypub \
  --table-name followers \
  --partition-key "follower" \
  --row-key "https://mastodon.social/users/YOUR_USERNAME"
```

---

## Monitoring Queries (Application Insights)

### Delivery Success Rate

```kusto
customEvents
| where name == "DeliveryAttempt"
| summarize 
    Total = count(),
    Success = countif(customDimensions.status == "success"),
    Failed = countif(customDimensions.status == "failed")
| extend SuccessRate = (Success * 100.0) / Total
```

### Failed Deliveries by Domain

```kusto
customEvents
| where name == "DeliveryAttempt" and customDimensions.status == "failed"
| summarize FailureCount = count() by tostring(customDimensions.domain)
| order by FailureCount desc
```

### Average Delivery Time

```kusto
customMetrics
| where name == "DeliveryDuration"
| summarize 
    AvgDuration = avg(value),
    P50Duration = percentile(value, 50),
    P95Duration = percentile(value, 95)
```

---

## Troubleshooting

### Check Follower Storage

```powershell
# List all followers
az storage entity query \
  --account-name lqdevactivitypub \
  --table-name followers \
  --query-filter "PartitionKey eq 'follower'"

# Check specific follower
az storage entity show \
  --account-name lqdevactivitypub \
  --table-name followers \
  --partition-key "follower" \
  --row-key "ACTOR_URI"
```

### Check Queue Messages

```powershell
# Peek accept-delivery queue
az storage message peek \
  --account-name lqdevactivitypub \
  --queue-name accept-delivery

# Peek activitypub-delivery queue
az storage message peek \
  --account-name lqdevactivitypub \
  --queue-name activitypub-delivery
```

### Check Application Insights Logs

```powershell
# Query recent errors
az monitor app-insights query \
  --app lqdev-activitypub-insights \
  --analytics-query "traces | where severityLevel >= 3 | order by timestamp desc | take 50"
```

---

## Common Issues

### HTTP Signature Validation Failures

**Symptom**: Mastodon rejects Follow activity with 401 Unauthorized

**Causes**:
- Incorrect date format (must be RFC 1123)
- Missing or incorrect digest header
- Wrong signing string format
- Private key mismatch with public key in actor profile

**Debug**:
```fsharp
// Log signing string and signature for comparison
printfn "Signing String:\n%s" signingString
printfn "Signature: %s" signature
```

### Accept Activity Not Delivered

**Symptom**: Follower shows pending in Table Storage but never receives Accept

**Causes**:
- Queue message not processed (check Azure Functions logs)
- HTTP signature validation fails on remote server
- Network connectivity issues to follower's inbox

**Debug**:
```powershell
# Check accept-delivery queue depth
az storage queue stats --account-name lqdevactivitypub --name accept-delivery

# Check Function logs
az monitor log-analytics query \
  --workspace YOUR_WORKSPACE_ID \
  --analytics-query "FunctionAppLogs | where FunctionName == 'ProcessAccept'"
```

---

## References

- **HTTP Signatures**: [RFC 9421](https://www.rfc-editor.org/rfc/rfc9421.html) and [cavage-12 draft](https://datatracker.ietf.org/doc/html/draft-cavage-http-signatures-12)
- **ActivityPub Spec**: [W3C Recommendation](https://www.w3.org/TR/activitypub/)
- **Azure Table Storage**: [.NET SDK Docs](https://learn.microsoft.com/en-us/azure/storage/tables/table-storage-overview)
- **Azure Queue Storage**: [.NET SDK Docs](https://learn.microsoft.com/en-us/azure/storage/queues/storage-dotnet-how-to-use-queues)
- **Application Insights**: [Query Language Reference](https://learn.microsoft.com/en-us/azure/azure-monitor/logs/log-query-overview)
