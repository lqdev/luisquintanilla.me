# Subdirectory HTML Index Generation Fix

## Problem
When visiting `/playlists/` or `/notes/` in the unix-ssg-mvp build, there were no `index.html` files, resulting in directory listings instead of proper collection pages.

## Solution
Created a new bash script `bin/generate-collection-indexes.sh` that:

1. **Discovers content items**: Finds all subdirectories in each content type folder (e.g., `/playlists/*/index.html`)
2. **Extracts metadata**: Parses HTML titles and dates from individual item pages
3. **Generates collection index**: Creates a styled index page at `/playlists/index.html`, `/notes/index.html`, etc.
4. **Provides navigation**: Includes links back to home and other sections

## Changes Made

### 1. New Script: `bin/generate-collection-indexes.sh`
- Processes all content types: posts, playlists, notes, reviews
- Generates responsive HTML collection pages
- Handles empty collections gracefully
- Extracts metadata from generated HTML files
- Properly escapes special characters in titles

### 2. Updated `Makefile`
- Added `generate-collection-indexes` target
- Integrated into main `build` target
- Updated `.PHONY` declaration to include new target

## How It Works

```
Build Process:
1. process-content        → Creates /playlists/item-name/index.html for each playlist
2. generate-feeds        → Creates RSS feeds
3. generate-tags         → Creates tag pages
4. generate-index        → Creates root /index.html
5. generate-collection-indexes ← NEW: Creates /playlists/index.html, /notes/index.html, etc.
```

## Testing

To verify the fix:
```bash
cd unix-ssg-mvp
make clean build
# Check that these files now exist:
# - build/playlists/index.html
# - build/notes/index.html
# - build/posts/index.html
# - build/reviews/index.html
```

Then visit in browser:
- http://localhost:8000/playlists/
- http://localhost:8000/notes/
- http://localhost:8000/reviews/

## Technical Details

### Directory Structure After Build
```
build/
├── playlists/
│   ├── index.html                    ← NEW: Collection index
│   ├── crate-finds-january/
│   │   └── index.html                ← Existing: Individual item
│   └── another-playlist/
│       └── index.html
├── notes/
│   ├── index.html                    ← NEW: Collection index
│   └── note-items/
│       └── index.html
└── posts/
    ├── index.html                    ← NEW: Collection index
    └── post-items/
        └── index.html
```

### HTML Features
- Responsive design matching existing site aesthetics
- Emoji indicators for content types (🎵 for playlists, 📓 for notes, etc.)
- Chronological ordering of items
- Metadata extraction (dates, titles)
- Graceful fallback for missing metadata
- Empty collection message when no items found

## Next Steps
- Run `make build` to regenerate site with collection indexes
- Serve and test collection pages load correctly
- Verify navigation works from root to collections to individual items
