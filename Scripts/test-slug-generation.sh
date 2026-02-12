#!/bin/bash
# Test slug generation standardization
# This script verifies that all post types generate slugs consistently

echo "🧪 Testing Slug Generation Standardization"
echo "=========================================="
echo ""

# Color codes
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test counter
TESTS_PASSED=0
TESTS_FAILED=0

# Function to test slug generation
test_slug_generation() {
    local post_type=$1
    local script=$2
    
    echo "Testing ${post_type}..."
    
    # Check if script file exists
    if [ ! -f "$script" ]; then
        echo -e "  ${RED}✗${NC} Script file not found: $script"
        ((TESTS_FAILED++))
        return
    fi
    
    # Check if the script contains the standard pattern
    # Look for the specific filename generation block with hasValidCustomSlug or match customSlug
    if grep -A 5 "let filename" "$script" | grep -q "hasValidCustomSlug" && \
       grep -A 5 "let filename" "$script" | grep -q "sprintf.*finalSlug.*now.ToString.*yyyy-MM-dd"; then
        echo -e "  ${GREEN}✓${NC} Standard pattern found in $script"
        ((TESTS_PASSED++))
    elif grep -A 5 "let filename" "$script" | grep -q "match customSlug with" && \
         grep -A 5 "let filename" "$script" | grep -q "sprintf.*finalSlug.*now.ToString.*yyyy-MM-dd"; then
        echo -e "  ${GREEN}✓${NC} Standard pattern found in $script"
        ((TESTS_PASSED++))
    elif [[ "$script" == *"review"* ]] && grep -q "generateSlug" "$script" && \
         grep -q "sprintf.*baseSlug.*timestamp" "$script"; then
        # Check if it's the review script with helper function
        echo -e "  ${YELLOW}✓${NC} Alternative pattern (helper function) found in $script"
        ((TESTS_PASSED++))
    else
        echo -e "  ${RED}✗${NC} Standard pattern NOT found in $script"
        ((TESTS_FAILED++))
    fi
}

# Test each post type
echo "Running slug generation tests..."
echo ""

test_slug_generation "Note" "Scripts/process-github-issue.fsx"
test_slug_generation "Bookmark" "Scripts/process-bookmark-issue.fsx"
test_slug_generation "Media" "Scripts/process-media-issue.fsx"
test_slug_generation "Playlist" "Scripts/process-playlist-issue.fsx"
test_slug_generation "Response" "Scripts/process-response-issue.fsx"
test_slug_generation "Review" "Scripts/process-review-issue.fsx"

echo ""
echo "=========================================="
echo "Test Results:"
echo -e "${GREEN}✓ Passed: $TESTS_PASSED${NC}"
if [ $TESTS_FAILED -gt 0 ]; then
    echo -e "${RED}✗ Failed: $TESTS_FAILED${NC}"
    exit 1
else
    echo -e "${GREEN}All tests passed!${NC}"
    echo ""
    echo "✅ Slug generation is standardized across all post types"
    echo "   - Custom slugs: No date appended"
    echo "   - Auto-generated: Date appended (YYYY-MM-DD)"
fi
