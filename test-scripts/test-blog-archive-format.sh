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
  jq -e '
    .version == "https://jsonfeed.org/version/1.1"
    and (.title | type == "string" and length > 0)
    and (.home_page_url | type == "string" and length > 0)
    and (.feed_url | type == "string" and length > 0)
    and (.items | type == "array")
    and ((.items | length) > 0)
    and (.items[0].id | type == "string" and length > 0)
    and (.items[0].url | type == "string" and length > 0)
    and (.items[0].content_html | type == "string")
  ' "$json_file" >/dev/null
done

for bar_file in \
  "$PUBLIC_DIR/archive/posts.bar" \
  "$PUBLIC_DIR/archive/notes.bar" \
  "$PUBLIC_DIR/archive/responses.bar" \
  "$PUBLIC_DIR/archive/all.bar"; do
  unzip -Z1 "$bar_file" | grep -q "^index.html$"
  unzip -Z1 "$bar_file" | grep -q "^feed.json$"

  index_html="$(unzip -p "$bar_file" index.html)"
  feed_json="$(unzip -p "$bar_file" feed.json)"

  grep -q 'class="h-feed"' <<< "$index_html"
  grep -q 'class="h-entry"' <<< "$index_html"
  jq -e '
    .version == "https://jsonfeed.org/version/1.1"
    and (.items | type == "array")
  ' <<< "$feed_json" >/dev/null

  if grep -q 'src="uploads/' <<< "$index_html"; then
    archive_entries="$(unzip -Z1 "$bar_file")"
    grep -q "^uploads/" <<< "$archive_entries"

    mapfile -t referenced_uploads < <(grep -o 'src="uploads/[^"]*"' <<< "$index_html" | sed 's/src="//; s/"$//' | sort -u)
    for upload_path in "${referenced_uploads[@]}"; do
      grep -qx "$upload_path" <<< "$archive_entries"
    done
  fi
done

echo "✅ Blog archive format outputs validated."
