#!/usr/bin/env bash
set -euo pipefail

# Validates the Blog Archive Format (.bar) and JSON Feed v1.1 outputs.
#
# JSON parsing prefers `jq` and zip inspection prefers `unzip`, but both fall
# back to Python (json + zipfile) when those tools are unavailable, so the
# script also runs in minimal environments (e.g. Windows / Git Bash / WSL
# without extra packages). Requirements: bash plus {jq OR python} for JSON and
# {unzip OR python} for archives.

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PUBLIC_DIR="$ROOT_DIR/_public"

have() { command -v "$1" >/dev/null 2>&1; }

PYBIN=""
if have python3; then
  PYBIN="python3"
elif have python; then
  PYBIN="python"
fi

if have jq; then
  JSON_MODE="jq"
elif [[ -n "$PYBIN" ]]; then
  JSON_MODE="python"
else
  echo "Need either 'jq' or Python (python3/python) to validate JSON feeds." >&2
  exit 1
fi

if have unzip; then
  ZIP_MODE="unzip"
elif [[ -n "$PYBIN" ]]; then
  ZIP_MODE="python"
else
  echo "Need either 'unzip' or Python (python3/python) to inspect .bar archives." >&2
  exit 1
fi

echo "Validating outputs (json=$JSON_MODE, zip=$ZIP_MODE${PYBIN:+, python=$PYBIN})"

# --- helpers -----------------------------------------------------------------

zip_list() { # <archive> -> prints entry names, one per line
  if [[ "$ZIP_MODE" == "unzip" ]]; then
    unzip -Z1 "$1"
  else
    "$PYBIN" - "$1" <<'PY'
import sys, zipfile
with zipfile.ZipFile(sys.argv[1]) as z:
    for name in z.namelist():
        print(name)
PY
  fi
}

zip_read() { # <archive> <entry> -> writes entry bytes to stdout
  if [[ "$ZIP_MODE" == "unzip" ]]; then
    unzip -p "$1" "$2"
  else
    "$PYBIN" - "$1" "$2" <<'PY'
import sys, zipfile
with zipfile.ZipFile(sys.argv[1]) as z:
    sys.stdout.buffer.write(z.read(sys.argv[2]))
PY
  fi
}

validate_feed_full() { # <feed.json path> -> exit 0 if a complete JSON Feed v1.1
  if [[ "$JSON_MODE" == "jq" ]]; then
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
    ' "$1" >/dev/null
  else
    "$PYBIN" - "$1" <<'PY'
import sys, json
def nonempty_str(x):
    return isinstance(x, str) and len(x) > 0
with open(sys.argv[1], encoding="utf-8") as f:
    d = json.load(f)
items = d.get("items")
ok = (
    d.get("version") == "https://jsonfeed.org/version/1.1"
    and nonempty_str(d.get("title"))
    and nonempty_str(d.get("home_page_url"))
    and nonempty_str(d.get("feed_url"))
    and isinstance(items, list)
    and len(items) > 0
    and isinstance(items[0], dict)
    and nonempty_str(items[0].get("id"))
    and nonempty_str(items[0].get("url"))
    and isinstance(items[0].get("content_html"), str)
)
sys.exit(0 if ok else 1)
PY
  fi
}

validate_feed_min() { # <feed.json path> -> exit 0 if minimally valid JSON Feed v1.1
  if [[ "$JSON_MODE" == "jq" ]]; then
    jq -e '
      .version == "https://jsonfeed.org/version/1.1"
      and (.items | type == "array")
    ' "$1" >/dev/null
  else
    "$PYBIN" - "$1" <<'PY'
import sys, json
with open(sys.argv[1], encoding="utf-8") as f:
    d = json.load(f)
ok = (
    d.get("version") == "https://jsonfeed.org/version/1.1"
    and isinstance(d.get("items"), list)
)
sys.exit(0 if ok else 1)
PY
  fi
}

# --- required files ----------------------------------------------------------

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
    echo "Missing expected file: $file" >&2
    exit 1
  fi
done

# --- JSON Feed v1.1 endpoints ------------------------------------------------

for json_file in \
  "$PUBLIC_DIR/posts/feed.json" \
  "$PUBLIC_DIR/notes/feed.json" \
  "$PUBLIC_DIR/responses/feed.json" \
  "$PUBLIC_DIR/feed/index.json"; do
  if ! validate_feed_full "$json_file"; then
    echo "Invalid JSON Feed v1.1 document: $json_file" >&2
    exit 1
  fi
done

# --- BAR archives ------------------------------------------------------------

for bar_file in \
  "$PUBLIC_DIR/archive/posts.bar" \
  "$PUBLIC_DIR/archive/notes.bar" \
  "$PUBLIC_DIR/archive/responses.bar" \
  "$PUBLIC_DIR/archive/all.bar"; do
  entries="$(zip_list "$bar_file")"

  if ! grep -q '^index.html$' <<< "$entries"; then
    echo "Archive missing index.html: $bar_file" >&2
    exit 1
  fi
  if ! grep -q '^feed.json$' <<< "$entries"; then
    echo "Archive missing feed.json: $bar_file" >&2
    exit 1
  fi

  index_html="$(zip_read "$bar_file" index.html)"

  feed_json_tmp="$(mktemp)"
  zip_read "$bar_file" feed.json > "$feed_json_tmp"

  if ! grep -q 'class="h-feed"' <<< "$index_html"; then
    echo "Archive index.html missing h-feed: $bar_file" >&2
    rm -f "$feed_json_tmp"
    exit 1
  fi
  if ! grep -q 'class="h-entry"' <<< "$index_html"; then
    echo "Archive index.html missing h-entry: $bar_file" >&2
    rm -f "$feed_json_tmp"
    exit 1
  fi

  if ! validate_feed_min "$feed_json_tmp"; then
    echo "Invalid JSON Feed v1.1 document in archive: $bar_file" >&2
    rm -f "$feed_json_tmp"
    exit 1
  fi
  rm -f "$feed_json_tmp"

  if grep -q 'src="uploads/' <<< "$index_html"; then
    if ! grep -q '^uploads/' <<< "$entries"; then
      echo "Archive references uploads/ but contains none: $bar_file" >&2
      exit 1
    fi

    mapfile -t referenced_uploads < <(grep -o 'src="uploads/[^"]*"' <<< "$index_html" | sed 's/src="//; s/"$//' | sort -u)
    for upload_path in "${referenced_uploads[@]}"; do
      if ! grep -qx "$upload_path" <<< "$entries"; then
        echo "Referenced upload missing from archive ($bar_file): $upload_path" >&2
        exit 1
      fi
    done
  fi
done

echo "✅ Blog archive format outputs validated."
