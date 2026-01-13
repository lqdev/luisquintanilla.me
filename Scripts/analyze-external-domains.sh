#!/bin/bash

# External Domain Analysis Script
# Analyzes all markdown files in the repository and generates a TSV report
# of external domains linked to from those files.
#
# Usage:
#   ./Scripts/analyze-external-domains.sh [options]
#
# Options:
#   -h, --help    Show this help message
#
# Output:
#   TSV format with two columns: COUNT and DOMAIN
#   Progress messages are sent to stderr
#
# Examples:
#   # View results in terminal
#   ./Scripts/analyze-external-domains.sh
#
#   # Save to file (progress messages still shown)
#   ./Scripts/analyze-external-domains.sh > domains.tsv
#
#   # Save to file without progress messages
#   ./Scripts/analyze-external-domains.sh > domains.tsv 2>/dev/null
#
#   # View top 10 domains
#   ./Scripts/analyze-external-domains.sh 2>/dev/null | head -11

# Check for help flag
if [ "$1" = "-h" ] || [ "$1" = "--help" ]; then
    echo "External Domain Analysis Script"
    echo ""
    echo "Analyzes all markdown files in the repository and generates a TSV report"
    echo "of external domains linked to from those files."
    echo ""
    echo "Usage:"
    echo "  ./Scripts/analyze-external-domains.sh [options]"
    echo ""
    echo "Options:"
    echo "  -h, --help    Show this help message"
    echo ""
    echo "Output:"
    echo "  TSV format with two columns: COUNT and DOMAIN"
    echo "  Progress messages are sent to stderr"
    echo ""
    echo "Examples:"
    echo "  # View results in terminal"
    echo "  ./Scripts/analyze-external-domains.sh"
    echo ""
    echo "  # Save to file (progress messages still shown)"
    echo "  ./Scripts/analyze-external-domains.sh > domains.tsv"
    echo ""
    echo "  # Save to file without progress messages"
    echo "  ./Scripts/analyze-external-domains.sh > domains.tsv 2>/dev/null"
    echo ""
    echo "  # View top 10 domains"
    echo "  ./Scripts/analyze-external-domains.sh 2>/dev/null | head -11"
    exit 0
fi

echo "🔍 External Domain Analysis" >&2
echo "============================" >&2
echo "" >&2

# Navigate to repository root (in case script is run from elsewhere)
cd "$(dirname "$0")/.."

echo "📁 Finding markdown files..." >&2
# Find all .md files recursively
MARKDOWN_FILES=$(find . -name "*.md" -type f)
FILE_COUNT=$(echo "$MARKDOWN_FILES" | wc -l)
echo "   Found $FILE_COUNT markdown files" >&2
echo "" >&2

echo "🔗 Extracting URLs..." >&2
# Extract all HTTP and HTTPS URLs from markdown files
# This pattern matches:
# - Inline links: [text](https://example.com/path)
# - Reference links: [text]: https://example.com/path
# - Plain URLs: https://example.com/path
# - HTML links: <a href="https://example.com">text</a>

# Create temporary file for URLs
TEMP_URLS=$(mktemp)

# Extract URLs using grep with extended regex
# Pattern explanation:
# - Match http:// or https://
# - Capture domain and path until whitespace, ), ], >, or quote
echo "$MARKDOWN_FILES" | while read -r file; do
    if [ -f "$file" ]; then
        # Extract URLs from various markdown formats
        grep -oE 'https?://[^][()<>\"'\'' ]+' "$file" 2>/dev/null || true
    fi
done > "$TEMP_URLS"

URL_COUNT=$(wc -l < "$TEMP_URLS")
echo "   Extracted $URL_COUNT URLs" >&2
echo "" >&2

if [ $URL_COUNT -eq 0 ]; then
    echo "⚠️  No URLs found in markdown files" >&2
    rm "$TEMP_URLS"
    exit 0
fi

echo "🌐 Parsing domains..." >&2
# Parse domains from URLs
# Remove protocol (http:// or https://)
# Extract domain part (everything before first /)
# Handle ports properly
TEMP_DOMAINS=$(mktemp)

# Parse domains using sed and awk
# 1. Remove http:// or https://
# 2. Remove everything after first / (path)
# 3. Remove port numbers if present
sed 's|https\?://||' "$TEMP_URLS" | \
    sed 's|/.*||' | \
    sed 's|:.*||' | \
    grep -v '^$' > "$TEMP_DOMAINS"

DOMAIN_COUNT=$(wc -l < "$TEMP_DOMAINS")
echo "   Parsed $DOMAIN_COUNT domain entries" >&2
echo "" >&2

echo "📊 Counting occurrences..." >&2
# Count occurrences of each domain and sort by count (descending)
TEMP_COUNTS=$(mktemp)

# Use sort and uniq to count occurrences
# Then sort numerically in reverse order
sort "$TEMP_DOMAINS" | uniq -c | sort -rn > "$TEMP_COUNTS"

UNIQUE_DOMAINS=$(wc -l < "$TEMP_COUNTS")
echo "   Found $UNIQUE_DOMAINS unique domains" >&2
echo "" >&2

echo "✅ Generating TSV report..." >&2
# Format as TSV: COUNT<tab>DOMAIN
# First output header
echo -e "COUNT\tDOMAIN"

# Process counts and format as TSV
# awk: print count first, then domain
awk '{printf "%s\t%s\n", $1, $2}' "$TEMP_COUNTS"

# Clean up temporary files
rm "$TEMP_URLS" "$TEMP_DOMAINS" "$TEMP_COUNTS"

echo "" >&2
echo "🎉 Analysis complete!" >&2
