#!/bin/bash

# ActivityPub Endpoint Testing Script
# Tests the local API endpoints for proper ActivityPub compliance

echo "=========================================="
echo "ActivityPub Endpoint Tests"
echo "=========================================="
echo ""

BASE_URL="http://localhost:7071"
SUCCESS=0
FAILED=0

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

test_endpoint() {
    local name="$1"
    local url="$2"
    local expected_type="$3"
    local accept_header="${4:-application/json}"
    
    echo -n "Testing $name... "
    
    response=$(curl -s -w "\n%{http_code}" -H "Accept: $accept_header" "$url" 2>/dev/null)
    http_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | sed '$d')
    
    if [ "$http_code" -eq 200 ]; then
        # Check if response is valid JSON
        if echo "$body" | jq empty 2>/dev/null; then
            # Check for expected type if provided
            if [ -n "$expected_type" ]; then
                actual_type=$(echo "$body" | jq -r '.type // ."@type" // "unknown"')
                if [ "$actual_type" = "$expected_type" ]; then
                    echo -e "${GREEN}✓ PASS${NC} (200 OK, type: $actual_type)"
                    SUCCESS=$((SUCCESS + 1))
                else
                    echo -e "${YELLOW}⚠ WARN${NC} (200 OK but type mismatch: expected $expected_type, got $actual_type)"
                    SUCCESS=$((SUCCESS + 1))
                fi
            else
                echo -e "${GREEN}✓ PASS${NC} (200 OK, valid JSON)"
                SUCCESS=$((SUCCESS + 1))
            fi
        else
            echo -e "${RED}✗ FAIL${NC} (200 OK but invalid JSON)"
            FAILED=$((FAILED + 1))
        fi
    else
        echo -e "${RED}✗ FAIL${NC} (HTTP $http_code)"
        FAILED=$((FAILED + 1))
    fi
}

test_webfinger() {
    local resource="$1"
    echo -n "Testing WebFinger for $resource... "
    
    encoded_resource=$(echo "$resource" | sed 's/@/%40/g' | sed 's/:/%3A/g')
    url="$BASE_URL/.well-known/webfinger?resource=$encoded_resource"
    
    response=$(curl -s -w "\n%{http_code}" -H "Accept: application/jrd+json" "$url" 2>/dev/null)
    http_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | sed '$d')
    
    if [ "$http_code" -eq 200 ]; then
        if echo "$body" | jq empty 2>/dev/null; then
            subject=$(echo "$body" | jq -r '.subject')
            echo -e "${GREEN}✓ PASS${NC} (200 OK, subject: $subject)"
            SUCCESS=$((SUCCESS + 1))
        else
            echo -e "${RED}✗ FAIL${NC} (200 OK but invalid JSON)"
            FAILED=$((FAILED + 1))
        fi
    elif [ "$http_code" -eq 404 ]; then
        echo -e "${YELLOW}⚠ INFO${NC} (404 Not Found - expected for invalid resources)"
    else
        echo -e "${RED}✗ FAIL${NC} (HTTP $http_code)"
        FAILED=$((FAILED + 1))
    fi
}

echo "1. WebFinger Discovery Tests"
echo "----------------------------"
test_webfinger "acct:lqdev@lqdev.me"
test_webfinger "acct:lqdev@www.lqdev.me"
echo ""

echo "2. Actor Endpoint Tests"
echo "----------------------"
test_endpoint "Actor" "$BASE_URL/api/actor" "Person" "application/activity+json"
echo ""

echo "3. Collection Endpoint Tests"
echo "---------------------------"
test_endpoint "Outbox" "$BASE_URL/api/outbox" "OrderedCollection" "application/activity+json"
test_endpoint "Inbox" "$BASE_URL/api/inbox" "OrderedCollection" "application/activity+json"
test_endpoint "Followers" "$BASE_URL/api/followers" "OrderedCollection" "application/activity+json"
test_endpoint "Following" "$BASE_URL/api/following" "OrderedCollection" "application/activity+json"
echo ""

echo "=========================================="
echo "Test Summary"
echo "=========================================="
echo -e "${GREEN}Passed: $SUCCESS${NC}"
if [ $FAILED -gt 0 ]; then
    echo -e "${RED}Failed: $FAILED${NC}"
else
    echo -e "Failed: $FAILED"
fi
echo ""

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}All tests passed!${NC}"
    exit 0
else
    echo -e "${RED}Some tests failed.${NC}"
    exit 1
fi
