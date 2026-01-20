# ProcessDelivery Function

Queue-triggered Azure Function that delivers ActivityPub activities to follower inboxes with HTTP signatures.

## Trigger

```
Queue: activitypub-delivery
Connection: ACTIVITYPUB_STORAGE_CONNECTION
```

## Purpose

Automatically triggered when messages appear in the `activitypub-delivery` queue. For each message:
1. Parses delivery task
2. Signs activity with Key Vault private key
3. POSTs activity to follower's inbox
4. Handles HTTP response and retry logic
5. Tracks delivery status in Table Storage

## Queue Message Format

```json
{
  "activityId": "https://lqdev.me/api/activitypub/notes/abc123",
  "activityJson": "{...complete Create activity...}",
  "targetInbox": "https://mastodon.social/inbox",
  "followerActor": "https://mastodon.social/users/alice",
  "attemptCount": 0,
  "queuedAt": "2026-01-20T12:00:00Z"
}
```

## Configuration

### Environment Variables

- `ACTIVITYPUB_STORAGE_CONNECTION`: Connection string for Azure Storage
- Key Vault configuration (inherited from `utils/keyvault.js`):
  - `AZURE_KEYVAULT_NAME`: Key Vault name
  - `AZURE_KEY_NAME`: Private key name

### Dependencies

- `../utils/signatures.js`: HTTP signature generation with Key Vault
- `../utils/tableStorage.js`: Track delivery status

## Delivery Flow

```
Azure Queue (activitypub-delivery)
    └─> ProcessDelivery function triggered
        ├─> Parse queue message
        ├─> Parse activity JSON
        ├─> Sign activity with Key Vault
        │   ├─> Generate digest (SHA-256 of body)
        │   ├─> Build signing string
        │   └─> Sign with RSA-SHA256
        ├─> POST to follower inbox with signature
        ├─> Handle response:
        │   ├─> 2xx: Success → mark delivered
        │   ├─> 4xx: Permanent failure → mark failed, don't retry
        │   └─> 5xx: Temporary failure → throw error, auto-retry
        └─> Update delivery status in Table Storage
```

## HTTP Request Format

### Headers

```http
POST /inbox HTTP/1.1
Host: mastodon.social
Date: Mon, 20 Jan 2026 12:00:00 GMT
Content-Type: application/activity+json
Content-Length: 1234
User-Agent: lqdev.me ActivityPub/1.0
Digest: SHA-256=base64encodeddigest
Signature: keyId="https://lqdev.me/api/activitypub/actor#main-key",
           headers="(request-target) host date digest",
           signature="base64encodedsignature",
           algorithm="rsa-sha256"
```

### Body

```json
{
  "@context": "https://www.w3.org/ns/activitystreams",
  "id": "https://lqdev.me/api/activitypub/notes/abc123",
  "type": "Create",
  "actor": "https://lqdev.me/api/activitypub/actor",
  "published": "2026-01-20T12:00:00Z",
  "to": ["https://www.w3.org/ns/activitystreams#Public"],
  "cc": ["https://lqdev.me/api/activitypub/followers"],
  "object": {
    "id": "https://lqdev.me/notes/my-post",
    "type": "Note",
    "content": "Hello, Fediverse!",
    "attributedTo": "https://lqdev.me/api/activitypub/actor",
    "published": "2026-01-20T12:00:00Z"
  }
}
```

## Response Handling

### Success (2xx)

- Status: `delivered`
- Action: Mark as delivered in Table Storage
- Retry: No
- Queue: Message deleted automatically

### Permanent Failure (4xx except 429)

- Status: `failed`
- Action: Mark as failed in Table Storage
- Retry: No
- Queue: Message deleted automatically

Common 4xx codes:
- **401**: Signature verification failed
- **404**: Inbox not found
- **410**: Account deleted/gone

### Temporary Failure (5xx, 429, network errors)

- Status: `pending`
- Action: Update attempt count in Table Storage
- Retry: Yes (automatic by Azure Queue)
- Queue: Message returned to queue with exponential backoff

Common 5xx codes:
- **500**: Internal server error
- **502**: Bad gateway
- **503**: Service unavailable

## Retry Strategy

Azure Queue Storage handles retries automatically:
- **Initial delay**: Immediate
- **Max retries**: 5 attempts (configurable)
- **Backoff**: Exponential (1s, 2s, 4s, 8s, 16s)
- **Dead letter**: After 5 failures, message moved to poison queue

## Table Storage Updates

### Delivery Status Table

After each attempt, updates `deliverystatus` table:

**Schema**:
```
PartitionKey: {activityId}
RowKey: {targetInbox} (base64 encoded)
Fields:
  - activityId: string
  - targetInbox: string
  - followerActor: string
  - status: "pending" | "delivered" | "failed"
  - attemptCount: number
  - lastAttempt: ISO date string
  - httpStatusCode: number
  - errorMessage: string
  - deliveredAt: ISO date string (if delivered)
```

## Error Handling

### Malformed Queue Message

- **Action**: Log error, don't retry
- **Reason**: Message will never be valid
- **Queue**: Message deleted

### Malformed Activity JSON

- **Action**: Mark failed, don't retry
- **Reason**: Activity will never be valid
- **Queue**: Message deleted

### Signature Generation Failure

- **Action**: Log error, don't retry
- **Reason**: Key Vault issue (not transient)
- **Queue**: Message deleted

### Network Timeout (30s)

- **Action**: Throw error, retry
- **Reason**: May succeed on next attempt
- **Queue**: Message returned for retry

## Performance

- **Execution time**: 1-5 seconds per delivery
- **Network timeout**: 30 seconds
- **Memory usage**: ~128MB typical
- **Concurrency**: 1 per function instance (default)
- **Scale**: Auto-scales with queue depth

## Security

### HTTP Signature Verification

Remote servers verify signatures using:
1. Fetch public key from `https://lqdev.me/api/activitypub/actor`
2. Extract signature from `Signature` header
3. Reconstruct signing string
4. Verify signature with public key (RSA-SHA256)

### HTTPS Only

All deliveries use HTTPS. HTTP endpoints are rejected by QueueDeliveryTasks.

## Monitoring

### Function Logs

View in Azure Portal:
1. Static Web App → Functions → ProcessDelivery → Monitor
2. See invocation count, success rate, errors

### Application Insights Queries

**Delivery Success Rate**:
```kusto
traces
| where message contains "Delivery successful" or message contains "Permanent failure"
| summarize Success = countif(message contains "successful"), 
            Failed = countif(message contains "Permanent") 
  by bin(timestamp, 1h)
```

**Failed Deliveries by Status Code**:
```kusto
traces
| where message contains "HTTP" and (message contains "Permanent" or message contains "Temporary")
| parse message with * "HTTP " StatusCode:int *
| summarize Count = count() by StatusCode
```

### Delivery Status Query

```bash
# Check all deliveries for an activity
az storage entity query \
  --account-name lqdevactivitypub \
  --table-name deliverystatus \
  --query-filter "PartitionKey eq 'https://lqdev.me/api/activitypub/notes/abc123'"

# Check deliveries to specific inbox
az storage entity query \
  --account-name lqdevactivitypub \
  --table-name deliverystatus \
  --query-filter "targetInbox eq 'https://mastodon.social/inbox'"
```

## Troubleshooting

### Delivery marked as failed with 401

**Cause**: HTTP signature verification failed at remote server

**Debug**:
1. Verify Key Vault key matches actor public key
2. Check signature header format
3. Test with different ActivityPub server

### All deliveries failing with network errors

**Cause**: Network connectivity issue or DNS problem

**Debug**:
1. Check Azure Functions network configuration
2. Verify outbound connectivity
3. Check remote server availability

### Queue depth growing without processing

**Cause**: Function not triggered or crashing

**Debug**:
1. Check Function App status in Azure Portal
2. Review function logs for errors
3. Verify ACTIVITYPUB_STORAGE_CONNECTION is set
4. Check Extension Bundle version (requires 3.x for queue trigger)

## Testing

### Manual Queue Message

```bash
# Add test message to queue
az storage message put \
  --account-name lqdevactivitypub \
  --queue-name activitypub-delivery \
  --content "$(echo '{"activityId":"test","activityJson":"{}","targetInbox":"https://example.com/inbox","followerActor":"https://example.com/actor","attemptCount":0,"queuedAt":"'$(date -u +%Y-%m-%dT%H:%M:%SZ)'"}' | base64)"

# Check queue for message
az storage queue stats \
  --account-name lqdevactivitypub \
  --name activitypub-delivery
```

## Related Functions

- **QueueDeliveryTasks**: Queues messages to this function
- **inbox**: Receives activities from remote servers

## References

- HTTP Signatures: https://datatracker.ietf.org/doc/html/draft-cavage-http-signatures-12
- ActivityPub: https://www.w3.org/TR/activitypub/
- Phase 4B/4C docs: `docs/activitypub/phase4b-4c-complete-summary.md`
