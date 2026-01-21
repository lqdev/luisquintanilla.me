# QueueDeliveryTasks Function

HTTP-triggered Azure Function that queues delivery tasks for ActivityPub posts to all followers.

## Endpoint

```
POST /api/activitypub/trigger-delivery
```

## Purpose

When called (typically by GitHub Actions after deployment), this function:
1. Loads specified activities from the outbox
2. Retrieves all followers from Table Storage
3. Creates a delivery task for each follower
4. Queues tasks to Azure Queue Storage for processing

## Request Body

```json
{
  "activityIds": [
    "https://lqdev.me/api/activitypub/notes/abc123",
    "https://lqdev.me/api/activitypub/notes/def456"
  ]
}
```

- `activityIds`: Array of activity ID URLs to deliver
- Typically contains 1-5 most recent posts

## Response

### Success (200)
```json
{
  "success": true,
  "totalFollowers": 10,
  "tasksQueued": 10,
  "activitiesProcessed": 1,
  "message": "Successfully queued 10 delivery tasks for 1 activities"
}
```

### No Followers (200)
```json
{
  "success": true,
  "message": "No followers to deliver to",
  "totalFollowers": 0,
  "tasksQueued": 0,
  "activitiesProcessed": 1
}
```

### Error (400)
```json
{
  "error": "Invalid request",
  "message": "activityIds array is required and must not be empty"
}
```

### Error (500)
```json
{
  "error": "Internal server error",
  "message": "..."
}
```

## Configuration

### Environment Variables

- `ACTIVITYPUB_STORAGE_CONNECTION`: Connection string for Azure Storage (Table + Queue)

### Dependencies

- `../utils/tableStorage.js`: Get followers
- `../utils/queueStorage.js`: Queue delivery tasks
- `../data/outbox/index.json`: Outbox data (copied during deployment)

## Security Features

### SSRF Protection

Validates all inbox URLs before queuing:
- Only HTTPS allowed
- Blocks localhost, 127.0.0.1, ::1
- Blocks private IP ranges:
  - 192.168.0.0/16
  - 10.0.0.0/8
  - 172.16.0.0/12

Invalid inbox URLs are logged but don't fail the request.

## Invocation Flow

```
GitHub Actions
    └─> POST /api/activitypub/trigger-delivery
        └─> QueueDeliveryTasks function
            ├─> Load activities from outbox
            ├─> Get all followers from Table Storage
            ├─> Validate inbox URLs (SSRF protection)
            └─> Queue delivery task per follower
                └─> Azure Queue: activitypub-delivery
```

## Queue Message Format

Each queued message contains:
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

## Error Handling

- **Activity not found**: Logged as warning, continues with other activities
- **Invalid inbox URL**: Logged as warning, skips follower
- **Table Storage error**: Returns 500 error
- **Queue Storage error**: Returns 500 error

## Performance

- **Cold start**: ~2-5 seconds (Azure Functions warm-up)
- **Execution time**: ~100-500ms + (50ms × follower count)
- **Memory**: ~128MB typical usage
- **Timeout**: 5 minutes (Azure Functions default)

## Testing

```bash
# Test with empty array (should return 400)
curl -X POST "https://your-site.azurestaticapps.net/api/activitypub/trigger-delivery" \
  -H "Content-Type: application/json" \
  -d '{"activityIds": []}'

# Test with valid activity ID
curl -X POST "https://your-site.azurestaticapps.net/api/activitypub/trigger-delivery" \
  -H "Content-Type: application/json" \
  -d '{"activityIds": ["https://lqdev.me/api/activitypub/notes/test123"]}'
```

## Monitoring

Check function logs in Azure Portal:
1. Go to Static Web App
2. Click "Functions" in left sidebar
3. Select "QueueDeliveryTasks"
4. Click "Monitor" tab
5. View recent invocations and logs

## Related Functions

- **ProcessDelivery**: Consumes queued tasks and delivers activities
- **inbox**: Handles incoming ActivityPub requests

## References

- ActivityPub spec: https://www.w3.org/TR/activitypub/
- Phase 4B/4C documentation: `docs/activitypub/phase4b-4c-complete-summary.md`
