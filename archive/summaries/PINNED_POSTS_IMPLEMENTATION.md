# Pinned Posts Feature - Implementation Summary

## Visual Example

The pinned post appears at the top of the timeline with a prominent pin indicator:

```
┌─────────────────────────────────────────────────────────────┐
│ 📌 Pinned                                                    │ ← Pin indicator
├─────────────────────────────────────────────────────────────┤
│ Oct 04, 2021                            Blog Post            │
│                                                              │
│ Automate YAML front-matter generation with custom           │
│ Visual Studio Code snippets                                 │
│                                                              │
│ [Content preview...]                                         │
│                                                              │
│ #blogging #tooling #visual-studio #visual-studio-code      │
└─────────────────────────────────────────────────────────────┘
  ^ Note the left accent border and subtle background

┌─────────────────────────────────────────────────────────────┐
│ Oct 12, 2024                            Note                 │ ← Regular post
│                                                              │
│ [Most recent unpinned content...]                           │
└─────────────────────────────────────────────────────────────┘
```

## Implementation Details

### Configuration File
Location: `Data/pinned-posts.json`

```json
[
    {
        "fileName": "automate-yaml-front-matter-vs-code-snippets",
        "contentType": "posts",
        "order": 1,
        "note": "Popular VS Code snippets guide"
    }
]
```

### How It Works

1. **Build Time**: Site generator loads `Data/pinned-posts.json`
2. **Matching**: Extracts filename from URLs and matches against config
3. **Ordering**: Pinned posts appear first (by order), then chronological items
4. **Rendering**: Pinned posts get special CSS class and visual indicator

### Files Modified

- `Domain.fs` - Added PinnedPost type
- `Loaders.fs` - Added loadPinnedPosts function
- `Builder.fs` - Updated buildTimelineHomePage with pinned logic
- `Views/LayoutViews.fs` - Added pin indicator rendering
- `_src/css/custom/timeline.css` - Added pinned post styles

### Build Output

```
✅ Timeline homepage created with 1 pinned posts, 50 chronological items from 1198 total items across all content types
```

## Usage Instructions

### To Pin a Post

1. Find the post's markdown filename (without `.md`)
   - Example: `_src/posts/my-awesome-post.md` → `my-awesome-post`

2. Add to `Data/pinned-posts.json`:
   ```json
   {
       "fileName": "my-awesome-post",
       "contentType": "posts",
       "order": 1,
       "note": "Why this is pinned"
   }
   ```

3. Rebuild the site: `dotnet run`

### To Pin Multiple Posts

Order determines display order (1 = first, 2 = second, etc.):

```json
[
    {
        "fileName": "most-important-post",
        "contentType": "posts",
        "order": 1
    },
    {
        "fileName": "second-most-important",
        "contentType": "posts",
        "order": 2
    },
    {
        "fileName": "important-note",
        "contentType": "notes",
        "order": 3
    }
]
```

### To Pin Other Content Types

The feature supports all content types:
- `posts` - Blog posts
- `notes` - Short notes
- `responses` - Responses (stars, replies, reshares)
- `bookmarks` - Bookmarks
- `reviews` - Book/media reviews
- `media` - Media items

### To Unpin a Post

Simply remove it from `Data/pinned-posts.json` and rebuild.

## Benefits

✅ **No Duplicate Management**: Filenames are stable, URLs can change
✅ **Centralized Control**: One JSON file manages all pinned posts
✅ **Visual Clarity**: Pin indicator makes pinned posts obvious
✅ **Flexible**: Can pin any content type across the unified timeline
✅ **Version Controlled**: Git tracks when posts are pinned/unpinned
✅ **Theme Integrated**: Works seamlessly with desert theme (light/dark)
