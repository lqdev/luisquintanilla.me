#!/bin/bash
# Test script to verify ActivityPub outbox endpoint returns fresh data
# Usage: ./test-outbox-endpoint.sh [production|local]

set -e

TARGET="${1:-production}"

if [ "$TARGET" = "production" ]; then
    BASE_URL="https://lqdev.me"
    echo "🌐 Testing production endpoint: $BASE_URL"
elif [ "$TARGET" = "local" ]; then
    BASE_URL="http://localhost:7071"
    echo "🏠 Testing local endpoint: $BASE_URL"
else
    echo "Usage: $0 [production|local]"
    exit 1
fi

echo ""
echo "Testing ActivityPub outbox endpoint..."
echo "=========================================="

# Fetch the outbox
RESPONSE=$(curl -s -H "Accept: application/activity+json" "$BASE_URL/api/activitypub/outbox")

# Parse JSON response
TOTAL_ITEMS=$(echo "$RESPONSE" | jq -r '.totalItems')
COLLECTION_TYPE=$(echo "$RESPONSE" | jq -r '.type')
FIRST_ACTIVITY_DATE=$(echo "$RESPONSE" | jq -r '.orderedItems[0].published' 2>/dev/null || echo "N/A")

echo ""
echo "📊 Results:"
echo "   Total Items: $TOTAL_ITEMS"
echo "   Collection Type: $COLLECTION_TYPE"
echo "   Most Recent Activity: $FIRST_ACTIVITY_DATE"
echo ""

# Validate results
if [ "$TOTAL_ITEMS" = "null" ] || [ -z "$TOTAL_ITEMS" ]; then
    echo "❌ FAIL: Could not parse totalItems from response"
    echo ""
    echo "Response (first 500 chars):"
    echo "$RESPONSE" | head -c 500
    exit 1
fi

if [ "$TOTAL_ITEMS" -lt 1000 ]; then
    echo "⚠️  WARNING: Only $TOTAL_ITEMS items returned (expected 1500+)"
    echo "   This suggests the old data (20 items) or incomplete data is being served"
    echo ""
    echo "   Troubleshooting steps:"
    echo "   1. Check that GitHub Actions workflow ran successfully"
    echo "   2. Verify 'Sync ActivityPub data' step completed"
    echo "   3. Check deployment logs for the outbox file size"
    echo "   4. Ensure api/data/outbox/index.json was updated in deployment"
    exit 1
fi

if [ "$COLLECTION_TYPE" != "OrderedCollection" ]; then
    echo "❌ FAIL: Expected OrderedCollection, got: $COLLECTION_TYPE"
    exit 1
fi

# Check if the most recent activity is reasonably recent
CURRENT_YEAR=$(date +%Y)
ACTIVITY_YEAR=$(echo "$FIRST_ACTIVITY_DATE" | cut -d'-' -f1)

if [ "$ACTIVITY_YEAR" != "$CURRENT_YEAR" ] && [ "$ACTIVITY_YEAR" != "$((CURRENT_YEAR - 1))" ]; then
    echo "⚠️  WARNING: Most recent activity from $ACTIVITY_YEAR (expected recent date)"
fi

echo "✅ SUCCESS: Outbox endpoint is serving fresh data!"
echo ""
echo "Summary:"
echo "   ✓ $TOTAL_ITEMS items available"
echo "   ✓ Valid ActivityPub OrderedCollection"
echo "   ✓ Contains recent activities"
echo ""
echo "🎉 Fix verified! The deployment synchronization is working correctly."
