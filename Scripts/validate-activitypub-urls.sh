#!/bin/bash

# ActivityPub Endpoint Validation Script
# Validates that all ActivityPub endpoints and files use correct /api/activitypub/* pattern
# Run this script to verify no legacy URL patterns exist

echo "=========================================="
echo "ActivityPub Endpoint Validation"
echo "=========================================="
echo ""

SUCCESS=0
FAILED=0

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

validate_file() {
    local file="$1"
    local check_name="$2"
    
    if [ ! -f "$file" ]; then
        echo -e "${YELLOW}⚠ SKIP${NC}: $check_name - File not found: $file"
        return
    fi
    
    # Check for legacy patterns (excluding activitypub subdirectory references)
    legacy_patterns=$(grep -E "lqdev\.me/api/(actor|outbox|inbox|followers|following)\"" "$file" | grep -v "activitypub" || true)
    
    if [ -z "$legacy_patterns" ]; then
        echo -e "${GREEN}✓ PASS${NC}: $check_name"
        SUCCESS=$((SUCCESS + 1))
    else
        echo -e "${RED}✗ FAIL${NC}: $check_name"
        echo "  Legacy patterns found in $file:"
        echo "$legacy_patterns" | sed 's/^/    /'
        FAILED=$((FAILED + 1))
    fi
}

validate_function_route() {
    local file="$1"
    local expected_route="$2"
    local check_name="$3"
    
    if [ ! -f "$file" ]; then
        echo -e "${YELLOW}⚠ SKIP${NC}: $check_name - File not found: $file"
        return
    fi
    
    actual_route=$(grep -o '"route":\s*"[^"]*"' "$file" | sed 's/"route":\s*"\([^"]*\)"/\1/' || echo "NO_ROUTE")
    
    if [ "$actual_route" = "$expected_route" ]; then
        echo -e "${GREEN}✓ PASS${NC}: $check_name (route: $actual_route)"
        SUCCESS=$((SUCCESS + 1))
    elif [ "$actual_route" = "NO_ROUTE" ]; then
        echo -e "${YELLOW}⚠ WARN${NC}: $check_name - No explicit route found"
        SUCCESS=$((SUCCESS + 1))
    else
        echo -e "${RED}✗ FAIL${NC}: $check_name"
        echo "  Expected: $expected_route"
        echo "  Found: $actual_route"
        FAILED=$((FAILED + 1))
    fi
}

echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}Phase 1: Azure Functions Configuration${NC}"
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

validate_function_route "api/actor/function.json" "activitypub/actor" "Actor Function Route"
validate_function_route "api/webfinger/function.json" "webfinger" "WebFinger Function Route"
validate_function_route "api/outbox/function.json" "activitypub/outbox" "Outbox Function Route"
validate_function_route "api/inbox/function.json" "activitypub/inbox" "Inbox Function Route"
validate_function_route "api/followers/function.json" "activitypub/followers" "Followers Function Route"
validate_function_route "api/following/function.json" "activitypub/following" "Following Function Route"

echo ""
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}Phase 2: Data Files${NC}"
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

validate_file "api/data/actor.json" "Actor Data File"
validate_file "api/data/webfinger.json" "WebFinger Data File"

echo ""
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}Phase 3: JavaScript Functions${NC}"
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

validate_file "api/actor/index.js" "Actor Function Code"
validate_file "api/inbox/index.js" "Inbox Function Code"
validate_file "api/outbox/index.js" "Outbox Function Code"
validate_file "api/followers/index.js" "Followers Function Code"
validate_file "api/following/index.js" "Following Function Code"
validate_file "api/utils/signatures.js" "Signatures Utility"
validate_file "api/utils/tableStorage.js" "Table Storage Utility"
validate_file "api/utils/followers.js" "Followers Utility"

echo ""
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}Phase 4: Documentation${NC}"
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

validate_file "api/ACTIVITYPUB.md" "API Documentation"
validate_file "docs/activitypub/implementation-status.md" "Implementation Status"
validate_file "docs/activitypub/deployment-guide.md" "Deployment Guide"
validate_file "docs/activitypub/ACTIVITYPUB-DOCS.md" "ActivityPub Docs"

echo ""
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}Phase 5: Test Scripts${NC}"
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

validate_file "Scripts/test-activitypub.sh" "Local Test Script"
validate_file "Scripts/test-activitypub-production.sh" "Production Test Script"

echo ""
echo "=========================================="
echo "Validation Summary"
echo "=========================================="
echo ""
echo -e "${GREEN}Passed: $SUCCESS${NC}"
echo -e "${RED}Failed: $FAILED${NC}"
echo ""

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}✅ All validations passed!${NC}"
    echo "ActivityPub endpoints are correctly configured."
    exit 0
else
    echo -e "${RED}❌ Some validations failed.${NC}"
    echo "Please review the failed checks above."
    exit 1
fi
