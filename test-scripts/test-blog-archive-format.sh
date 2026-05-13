#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PUBLIC_DIR="$ROOT_DIR/_public"

required_files=(
  "$PUBLIC_DIR/posts/feed.json"
  "$PUBLIC_DIR/notes/feed.json"
  "$PUBLIC_DIR/responses/feed.json"
  "$PUBLIC_DIR/feed/index.json"
  "$PUBLIC_DIR/archive/posts.bar"
  "$PUBLIC_DIR/archive/notes.bar"
  "$PUBLIC_DIR/archive/responses.bar"
  "$PUBLIC_DIR/archive/all.bar"
  "$PUBLIC_DIR/archive/index.html"
)

for file in "${required_files[@]}"; do
  if [[ ! -f "$file" ]]; then
    echo "Missing expected file: $file"
    exit 1
  fi
done

for json_file in \
  "$PUBLIC_DIR/posts/feed.json" \
  "$PUBLIC_DIR/notes/feed.json" \
  "$PUBLIC_DIR/responses/feed.json" \
  "$PUBLIC_DIR/feed/index.json"; do
  jq -e '.version == "https://jsonfeed.org/version/1.1" and (.items | type == "array")' "$json_file" >/dev/null
done

for bar_file in \
  "$PUBLIC_DIR/archive/posts.bar" \
  "$PUBLIC_DIR/archive/notes.bar" \
  "$PUBLIC_DIR/archive/responses.bar" \
  "$PUBLIC_DIR/archive/all.bar"; do
  unzip -Z1 "$bar_file" | grep -q "^index.html$"
  unzip -Z1 "$bar_file" | grep -q "^feed.json$"
done

echo "✅ Blog archive format outputs validated."
