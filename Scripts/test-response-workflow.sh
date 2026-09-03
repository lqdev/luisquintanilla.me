#!/bin/bash
set -eu

cleanup() {
    rm -f _src/responses/empty-response-test.md \
        _src/responses/content-response-test.md
}
trap cleanup EXIT

echo "🧪 Testing response issue processing"

OUTPUT=$(dotnet fsi Scripts/process-response-issue.fsx -- \
    'reply' 'https://example.com' 'Empty Response' '' 'empty-response-test' '' 2>&1)
echo "$OUTPUT" | grep -q "Response post created successfully"
test -f "_src/responses/empty-response-test.md"
if grep -q "Error: Content is required" "_src/responses/empty-response-test.md"; then
    echo "❌ Empty response content was rejected"
    exit 1
fi

OUTPUT=$(dotnet fsi Scripts/process-response-issue.fsx -- \
    'reply' 'https://example.com' 'Content Response' 'Additional commentary' 'content-response-test' '' 2>&1)
echo "$OUTPUT" | grep -q "Response post created successfully"
grep -q "Additional commentary" "_src/responses/content-response-test.md"

echo "✅ Empty and non-empty response content are processed successfully"
