#!/bin/bash
set -e

# ActivityPub Production Deployment Testing Script
# Tests all deployed ActivityPub endpoints on production domain
# Based on phase4a-testing-guide.md and implementation-status.md

echo "=========================================="
echo "ActivityPub Production Deployment Tests"
echo "Testing Domain: https://lqdev.me"
echo "=========================================="
echo ""

PRODUCTION_URL="https://lqdev.me"
SUCCESS=0
FAILED=0
WARNINGS=0

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Test tracking
TESTS_RUN=()
TESTS_PASSED=()
TESTS_FAILED=()
TESTS_WARNED=()

log_test() {
    local name="$1"
    TESTS_RUN+=("$name")
}

log_pass() {
    local name="$1"
    local message="${2:-}"
    echo -e "${GREEN}✓ PASS${NC}: $name"
    [ -n "$message" ] && echo "  $message"
    TESTS_PASSED+=("$name")
    SUCCESS=$((SUCCESS + 1))
}

log_fail() {
    local name="$1"
    local message="$2"
    echo -e "${RED}✗ FAIL${NC}: $name"
    echo "  $message"
    TESTS_FAILED+=("$name")
    FAILED=$((FAILED + 1))
}

log_warn() {
    local name="$1"
    local message="$2"
    echo -e "${YELLOW}⚠ WARN${NC}: $name"
    echo "  $message"
    TESTS_WARNED+=("$name")
    WARNINGS=$((WARNINGS + 1))
}

test_endpoint() {
    local name="$1"
    local url="$2"
    local expected_type="${3:-}"
    local accept_header="${4:-application/activity+json}"
    
    log_test "$name"
    echo -n "Testing $name... "
    
    # Make request with timeout
    response=$(curl -s -w "\n%{http_code}" -H "Accept: $accept_header" --max-time 10 "$url" 2>&1)
    curl_exit_code=$?
    
    # Check if curl succeeded
    if [ $curl_exit_code -ne 0 ]; then
        echo ""
        log_fail "$name" "Connection failed (curl exit code: $curl_exit_code). Endpoint may not be deployed."
        return 1
    fi
    
    http_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | sed '$d')
    
    if [ "$http_code" -eq 200 ]; then
        # Check if response is valid JSON
        if echo "$body" | jq empty 2>/dev/null; then
            # Check for expected type if provided
            if [ -n "$expected_type" ]; then
                actual_type=$(echo "$body" | jq -r '.type // ."@type" // "unknown"')
                if [ "$actual_type" = "$expected_type" ]; then
                    echo ""
                    log_pass "$name" "HTTP 200, type: $actual_type"
                else
                    echo ""
                    log_warn "$name" "Type mismatch: expected $expected_type, got $actual_type"
                fi
            else
                echo ""
                log_pass "$name" "HTTP 200, valid JSON"
            fi
        else
            echo ""
            log_fail "$name" "HTTP 200 but invalid JSON response"
        fi
    elif [ "$http_code" -eq 404 ]; then
        echo ""
        log_fail "$name" "HTTP 404 - Endpoint not found. May not be deployed yet."
    elif [ "$http_code" -eq 500 ]; then
        echo ""
        log_fail "$name" "HTTP 500 - Server error. Check Azure Functions logs."
    else
        echo ""
        log_fail "$name" "HTTP $http_code"
    fi
}

test_webfinger() {
    local resource="$1"
    local display_name="$2"
    
    log_test "WebFinger: $display_name"
    echo -n "Testing WebFinger for $resource... "
    
    # URL encode the resource parameter
    encoded_resource=$(echo "$resource" | sed 's/@/%40/g' | sed 's/:/%3A/g')
    url="$PRODUCTION_URL/.well-known/webfinger?resource=$encoded_resource"
    
    response=$(curl -s -w "\n%{http_code}" -H "Accept: application/jrd+json" --max-time 10 "$url" 2>&1)
    curl_exit_code=$?
    
    if [ $curl_exit_code -ne 0 ]; then
        echo ""
        log_fail "WebFinger: $display_name" "Connection failed"
        return 1
    fi
    
    http_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | sed '$d')
    
    if [ "$http_code" -eq 200 ]; then
        if echo "$body" | jq empty 2>/dev/null; then
            subject=$(echo "$body" | jq -r '.subject // "unknown"')
            links=$(echo "$body" | jq -r '.links // [] | length')
            echo ""
            log_pass "WebFinger: $display_name" "subject: $subject, links: $links"
        else
            echo ""
            log_fail "WebFinger: $display_name" "Invalid JSON response"
        fi
    elif [ "$http_code" -eq 404 ]; then
        echo ""
        log_fail "WebFinger: $display_name" "HTTP 404 - Resource not found"
    else
        echo ""
        log_fail "WebFinger: $display_name" "HTTP $http_code"
    fi
}

test_static_file() {
    local name="$1"
    local path="$2"
    
    log_test "Static File: $name"
    echo -n "Testing static file: $name... "
    
    url="$PRODUCTION_URL$path"
    response=$(curl -s -w "\n%{http_code}" --max-time 10 "$url" 2>&1)
    curl_exit_code=$?
    
    if [ $curl_exit_code -ne 0 ]; then
        echo ""
        log_warn "Static File: $name" "Connection failed - file may not be deployed yet"
        return 1
    fi
    
    http_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | sed '$d')
    
    if [ "$http_code" -eq 200 ]; then
        if echo "$body" | jq empty 2>/dev/null; then
            echo ""
            log_pass "Static File: $name" "Valid JSON file deployed"
        else
            echo ""
            log_warn "Static File: $name" "File exists but not valid JSON"
        fi
    else
        echo ""
        log_warn "Static File: $name" "HTTP $http_code - File may not be deployed"
    fi
}

echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}Phase 1: WebFinger Discovery Tests${NC}"
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

test_webfinger "acct:lqdev@lqdev.me" "Primary Handle"
test_webfinger "acct:lqdev@www.lqdev.me" "WWW Subdomain"

echo ""
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}Phase 2: Actor Profile Tests${NC}"
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

test_endpoint "Actor Profile" "$PRODUCTION_URL/api/activitypub/actor" "Person" "application/activity+json"

echo ""
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}Phase 3: Collection Endpoints Tests${NC}"
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

test_endpoint "Outbox Collection" "$PRODUCTION_URL/api/activitypub/outbox" "OrderedCollection" "application/activity+json"
test_endpoint "Followers Collection" "$PRODUCTION_URL/api/activitypub/followers" "OrderedCollection" "application/activity+json"
test_endpoint "Following Collection" "$PRODUCTION_URL/api/activitypub/following" "OrderedCollection" "application/activity+json"

echo ""
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}Phase 4: Inbox Endpoint Tests${NC}"
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

# Inbox should accept GET (returns empty collection) and POST (processes activities)
test_endpoint "Inbox (GET)" "$PRODUCTION_URL/api/activitypub/inbox" "OrderedCollection" "application/activity+json"

echo ""
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}Phase 5: Static Data Files Tests${NC}"
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

test_static_file "Actor Data" "/api/data/actor.json"
test_static_file "WebFinger Data" "/api/data/webfinger.json"
test_static_file "Followers Data" "/api/data/followers.json"
test_static_file "Outbox Data" "/api/data/outbox/index.json"

echo ""
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}Phase 6: URL Structure Validation${NC}"
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

log_test "URL Pattern Consistency"
echo "Checking actor.json URLs for pattern consistency..."

actor_json=$(curl -s "$PRODUCTION_URL/api/data/actor.json")
actor_id=$(echo "$actor_json" | jq -r '.id // "missing"')
inbox_url=$(echo "$actor_json" | jq -r '.inbox // "missing"')
outbox_url=$(echo "$actor_json" | jq -r '.outbox // "missing"')

if [[ "$actor_id" == *"/api/activitypub/"* ]] && \
   [[ "$inbox_url" == *"/api/activitypub/"* ]] && \
   [[ "$outbox_url" == *"/api/activitypub/"* ]]; then
    log_pass "URL Pattern Consistency" "All URLs use /api/activitypub/* pattern"
else
    log_warn "URL Pattern Consistency" "Some URLs may not follow /api/activitypub/* pattern"
fi

echo ""
echo "=========================================="
echo "Test Summary"
echo "=========================================="
echo ""
echo "Total Tests Run: ${#TESTS_RUN[@]}"
echo -e "${GREEN}Passed: $SUCCESS${NC}"
echo -e "${YELLOW}Warnings: $WARNINGS${NC}"
echo -e "${RED}Failed: $FAILED${NC}"
echo ""

if [ $FAILED -eq 0 ] && [ $WARNINGS -eq 0 ]; then
    echo -e "${GREEN}✅ All tests passed! ActivityPub deployment is fully functional.${NC}"
    exit 0
elif [ $FAILED -eq 0 ]; then
    echo -e "${YELLOW}⚠️  All critical tests passed, but there are warnings.${NC}"
    echo "Review warnings above for potential issues."
    exit 0
else
    echo -e "${RED}❌ Some tests failed.${NC}"
    echo ""
    echo "Failed Tests:"
    for test in "${TESTS_FAILED[@]}"; do
        echo "  • $test"
    done
    echo ""
    echo "Next Steps:"
    echo "1. Check Azure Functions deployment status"
    echo "2. Verify Azure Static Web Apps configuration"
    echo "3. Review Application Insights logs for errors"
    echo "4. Consult /docs/activitypub/deployment-guide.md"
    exit 1
fi
