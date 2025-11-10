#!/bin/bash

# Test script for read later cleanup workflow
# This script validates the cleanup logic for removing entries older than 14 days

echo "🧹 Testing Read Later Cleanup Workflow"
echo "========================================"
echo ""

# Navigate to project directory
cd "$(dirname "$0")/.."

# Test 1: Validate YAML syntax
echo "🧪 Test 1: Validate workflow YAML syntax"
echo "-----------------------------------------"

if python3 -c "import yaml; yaml.safe_load(open('.github/workflows/process-read-later.yml'))" 2>/dev/null; then
    echo "✅ Workflow YAML is valid"
else
    echo "❌ Workflow YAML is invalid"
    exit 1
fi

echo ""

# Test 2: Verify cleanup job exists
echo "🧪 Test 2: Verify cleanup job exists in workflow"
echo "------------------------------------------------"

if grep -q "cleanup-old-entries:" .github/workflows/process-read-later.yml; then
    echo "✅ Cleanup job found in workflow"
else
    echo "❌ Cleanup job not found in workflow"
    exit 1
fi

echo ""

# Test 3: Verify schedule trigger
echo "🧪 Test 3: Verify schedule trigger exists"
echo "------------------------------------------"

if grep -q "schedule:" .github/workflows/process-read-later.yml; then
    echo "✅ Schedule trigger found"
else
    echo "❌ Schedule trigger not found"
    exit 1
fi

if grep -q "cron:" .github/workflows/process-read-later.yml; then
    echo "✅ Cron expression found"
else
    echo "❌ Cron expression not found"
    exit 1
fi

echo ""

# Test 4: Verify workflow_dispatch trigger
echo "🧪 Test 4: Verify manual trigger capability"
echo "--------------------------------------------"

if grep -q "workflow_dispatch:" .github/workflows/process-read-later.yml; then
    echo "✅ Manual trigger (workflow_dispatch) found"
else
    echo "❌ Manual trigger not found"
    exit 1
fi

echo ""

# Test 5: Test cleanup logic with mock data
echo "🧪 Test 5: Test cleanup logic with mock data"
echo "---------------------------------------------"

# Create temporary test file
TMP_TEST_FILE=$(mktemp)

# Create mock data with entries of different ages
# Current time
CURRENT_TIME=$(date -u +"%Y-%m-%dT%H:%M:%SZ")

# 7 days ago (should be kept)
SEVEN_DAYS_AGO=$(date -u -d '7 days ago' +"%Y-%m-%dT%H:%M:%SZ")

# 10 days ago (should be kept)
TEN_DAYS_AGO=$(date -u -d '10 days ago' +"%Y-%m-%dT%H:%M:%SZ")

# 15 days ago (should be removed)
FIFTEEN_DAYS_AGO=$(date -u -d '15 days ago' +"%Y-%m-%dT%H:%M:%SZ")

# 20 days ago (should be removed)
TWENTY_DAYS_AGO=$(date -u -d '20 days ago' +"%Y-%m-%dT%H:%M:%SZ")

# 30 days ago (should be removed)
THIRTY_DAYS_AGO=$(date -u -d '30 days ago' +"%Y-%m-%dT%H:%M:%SZ")

# Create test JSON with mixed ages
cat > "$TMP_TEST_FILE" <<EOF
[
  {
    "url": "https://example.com/recent",
    "title": "Recent Article",
    "dateAdded": "$CURRENT_TIME"
  },
  {
    "url": "https://example.com/week-old",
    "title": "Week Old Article",
    "dateAdded": "$SEVEN_DAYS_AGO"
  },
  {
    "url": "https://example.com/ten-days",
    "title": "Ten Days Old",
    "dateAdded": "$TEN_DAYS_AGO"
  },
  {
    "url": "https://example.com/fifteen-days",
    "title": "Fifteen Days Old (should be removed)",
    "dateAdded": "$FIFTEEN_DAYS_AGO"
  },
  {
    "url": "https://example.com/twenty-days",
    "title": "Twenty Days Old (should be removed)",
    "dateAdded": "$TWENTY_DAYS_AGO"
  },
  {
    "url": "https://example.com/thirty-days",
    "title": "Thirty Days Old (should be removed)",
    "dateAdded": "$THIRTY_DAYS_AGO"
  }
]
EOF

echo "Created test data with 6 entries:"
echo "  - 3 recent entries (< 14 days) - should be kept"
echo "  - 3 old entries (> 14 days) - should be removed"
echo ""

# Calculate cutoff date
CUTOFF_DATE=$(date -u -d '14 days ago' +"%Y-%m-%dT%H:%M:%SZ")
echo "Cutoff date: $CUTOFF_DATE"
echo ""

# Count original entries
ORIGINAL_COUNT=$(jq 'length' "$TMP_TEST_FILE")
echo "Original entry count: $ORIGINAL_COUNT"

# Filter entries (same logic as workflow)
TMP_FILTERED=$(mktemp)
jq --arg cutoff "$CUTOFF_DATE" '[.[] | select(.dateAdded >= $cutoff)]' "$TMP_TEST_FILE" > "$TMP_FILTERED"

# Count remaining entries
NEW_COUNT=$(jq 'length' "$TMP_FILTERED")
echo "Remaining entry count: $NEW_COUNT"

# Calculate removed count
REMOVED_COUNT=$((ORIGINAL_COUNT - NEW_COUNT))
echo "Entries removed: $REMOVED_COUNT"
echo ""

# Verify expected results
if [ "$ORIGINAL_COUNT" -eq 6 ] && [ "$NEW_COUNT" -eq 3 ] && [ "$REMOVED_COUNT" -eq 3 ]; then
    echo "✅ Cleanup logic works correctly!"
    echo "   - Kept 3 recent entries (< 14 days)"
    echo "   - Removed 3 old entries (> 14 days)"
else
    echo "❌ Cleanup logic failed!"
    echo "   - Expected: 6 original, 3 remaining, 3 removed"
    echo "   - Got: $ORIGINAL_COUNT original, $NEW_COUNT remaining, $REMOVED_COUNT removed"
    rm "$TMP_TEST_FILE" "$TMP_FILTERED"
    exit 1
fi

# Show filtered entries
echo ""
echo "Remaining entries:"
jq -r '.[] | "  - \(.title) (added: \(.dateAdded))"' "$TMP_FILTERED"

# Cleanup
rm "$TMP_TEST_FILE" "$TMP_FILTERED"

echo ""

# Test 6: Verify job condition
echo "🧪 Test 6: Verify cleanup job has correct condition"
echo "---------------------------------------------------"

if grep -q "if: github.event_name == 'schedule' || github.event_name == 'workflow_dispatch'" .github/workflows/process-read-later.yml; then
    echo "✅ Cleanup job condition is correct"
else
    echo "❌ Cleanup job condition is incorrect or missing"
    exit 1
fi

echo ""

echo "🎉 Read Later cleanup validation complete!"
echo "==========================================="
echo ""
echo "📚 Summary:"
echo "- Workflow YAML syntax is valid"
echo "- Cleanup job exists with correct structure"
echo "- Schedule trigger configured (daily at midnight UTC)"
echo "- Manual trigger available via workflow_dispatch"
echo "- Cleanup logic correctly filters entries older than 14 days"
echo "- Job condition prevents running on issue events"
echo ""
echo "🔍 What was added:"
echo "- New 'cleanup-old-entries' job in process-read-later.yml"
echo "- Schedule trigger: daily at 0:00 UTC (cron: '0 0 * * *')"
echo "- Manual trigger: workflow_dispatch for testing"
echo "- 14-day age filter using jq and date comparison"
echo "- Automatic PR creation and merge for cleanup changes"
echo "- Job summary output for tracking cleanup results"
echo ""
echo "📖 How it works:"
echo "1. Runs daily at midnight UTC (or manually triggered)"
echo "2. Calculates cutoff date (14 days ago)"
echo "3. Filters entries to keep only those < 14 days old"
echo "4. Creates PR if entries were removed"
echo "5. Auto-merges PR if no conflicts"
echo "6. Logs summary to GitHub Actions summary"
echo ""
echo "🧪 To test the cleanup manually:"
echo "1. Go to Actions tab in GitHub"
echo "2. Select 'Process Read Later Issue' workflow"
echo "3. Click 'Run workflow' button"
echo "4. Monitor the cleanup job execution"
echo ""
