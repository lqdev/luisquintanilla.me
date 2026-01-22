#!/bin/bash
# Queue a post for ActivityPub delivery using Azure Table Storage REST API
# Usage: ./queue-post-delivery.sh <noteId> <createActivityJsonPath>

set -e

NOTE_ID="$1"
CREATE_ACTIVITY_PATH="$2"

if [ -z "$NOTE_ID" ] || [ -z "$CREATE_ACTIVITY_PATH" ]; then
    echo "Usage: $0 <noteId> <createActivityJsonPath>"
    exit 1
fi

if [ -z "$ACTIVITYPUB_STORAGE_CONNECTION" ]; then
    echo "❌ ACTIVITYPUB_STORAGE_CONNECTION environment variable not set"
    exit 1
fi

# Read the Create activity JSON
if [ ! -f "$CREATE_ACTIVITY_PATH" ]; then
    echo "❌ File not found: $CREATE_ACTIVITY_PATH"
    exit 1
fi

CREATE_ACTIVITY=$(cat "$CREATE_ACTIVITY_PATH")

# Extract account name and key from connection string
ACCOUNT_NAME=$(echo "$ACTIVITYPUB_STORAGE_CONNECTION" | grep -oP 'AccountName=\K[^;]+')
ACCOUNT_KEY=$(echo "$ACTIVITYPUB_STORAGE_CONNECTION" | grep -oP 'AccountKey=\K[^;]+')

if [ -z "$ACCOUNT_NAME" ] || [ -z "$ACCOUNT_KEY" ]; then
    echo "❌ Failed to parse storage connection string"
    exit 1
fi

# Generate queue ID
TIMESTAMP=$(date +%s%3N)
RANDOM_HEX=$(openssl rand -hex 4)
QUEUE_ID="post-${TIMESTAMP}-${RANDOM_HEX}"

# Prepare entity JSON (escape quotes in CREATE_ACTIVITY)
CREATE_ACTIVITY_ESCAPED=$(echo "$CREATE_ACTIVITY" | jq -Rs .)
QUEUED_AT=$(date -u +"%Y-%m-%dT%H:%M:%S.%3NZ")

ENTITY_JSON=$(cat <<EOF
{
  "PartitionKey": "pending",
  "RowKey": "$QUEUE_ID",
  "noteId": "$NOTE_ID",
  "createActivity": $CREATE_ACTIVITY_ESCAPED,
  "status": "pending",
  "queuedAt": "$QUEUED_AT",
  "retryCount": 0
}
EOF
)

# Azure Table Storage REST API
TABLE_NAME="deliveryqueue"
URL="https://${ACCOUNT_NAME}.table.core.windows.net/${TABLE_NAME}"
DATE=$(TZ=UTC date -R)
CONTENT_TYPE="application/json"

# Calculate signature
STRING_TO_SIGN="$DATE"
SIGNATURE=$(echo -n "$STRING_TO_SIGN" | openssl dgst -sha256 -hmac "$(echo $ACCOUNT_KEY | base64 -d)" -binary | base64)

# Make request
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$URL" \
  -H "x-ms-date: $DATE" \
  -H "x-ms-version: 2019-02-02" \
  -H "Authorization: SharedKeyLite $ACCOUNT_NAME:$SIGNATURE" \
  -H "Content-Type: $CONTENT_TYPE" \
  -H "Accept: application/json;odata=nometadata" \
  -d "$ENTITY_JSON")

HTTP_CODE=$(echo "$RESPONSE" | tail -n 1)
BODY=$(echo "$RESPONSE" | sed '$d')

if [ "$HTTP_CODE" -eq 201 ] || [ "$HTTP_CODE" -eq 204 ]; then
    echo "✅ Queued: $NOTE_ID"
    exit 0
else
    echo "❌ Failed to queue (HTTP $HTTP_CODE): $BODY"
    exit 1
fi
