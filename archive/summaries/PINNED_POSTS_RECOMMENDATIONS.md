# Pinned Posts Implementation Recommendations

## Overview
This document provides recommendations for implementing pinned posts on the homepage timeline of the Luis Quintanilla personal website.

## Current Architecture Analysis

### Timeline System
- **Homepage**: Built using `buildTimelineHomePage` function in `Builder.fs`
- **Timeline View**: Uses `timelineHomeViewStratified` in `Views/LayoutViews.fs`
- **Content Structure**: Uses `UnifiedFeedItem` from `GenericBuilder.UnifiedFeeds` module
- **Initial Load**: Shows 50 most recent items chronologically across all content types
- **Progressive Loading**: Remaining items loaded by type in 25-item chunks

### Unified Feed System
The site uses a unified feed architecture where different content types (posts, notes, responses, bookmarks, reviews, media) are converted to a common `UnifiedFeedItem` structure:

```fsharp
type UnifiedFeedItem = {
    Title: string
    Content: string
    Url: string
    Date: string
    ContentType: string
    Tags: string array
    RssXml: XElement
}
```

## Implementation Options

### Option 1: JSON Configuration File (RECOMMENDED ⭐)

**Approach**: Create a `Data/pinned-posts.json` file with an ordered list of posts to pin using **filenames** instead of URLs.

#### Advantages
✅ **Centralized Management**: Single location to manage all pinned posts
✅ **Easy Maintenance**: Simple to add, remove, or reorder pinned posts
✅ **Version Control**: JSON file tracks in Git history
✅ **No Frontmatter Pollution**: Doesn't clutter individual post files
✅ **Flexible Ordering**: Explicit index-based ordering
✅ **Multi-Content-Type Support**: Can pin any content type (posts, notes, responses)
✅ **Aligns with Existing Patterns**: Follows the site's pattern of using JSON configs in `Data/` directory
✅ **Permalink-Safe**: Uses filename matching, so changing permalinks doesn't require config updates

#### Implementation Details

**Configuration File Structure** (`Data/pinned-posts.json`):
```json
[
    {
        "fileName": "automate-yaml-front-matter-vs-code-snippets",
        "contentType": "posts",
        "order": 1,
        "note": "Popular VS Code snippets post"
    },
    {
        "fileName": "authorization-code-flow-python",
        "contentType": "posts",
        "order": 2
    }
]
```

**Why filename instead of URL?**
- Filenames are stable and don't change (they're the markdown file names without `.md` extension)
- Permalinks are generated from filenames at build time, so no duplication
- If you ever change the URL structure (e.g., from `/posts/filename/` to `/blog/filename/`), the config doesn't need updates
- The filename is what's stored in the `Post.FileName` property anyway

**Domain Type** (add to `Domain.fs`):
```fsharp
[<CLIMutable>]
type PinnedPost = {
    [<JsonPropertyName("fileName")>] FileName: string
    [<JsonPropertyName("contentType")>] ContentType: string
    [<JsonPropertyName("order")>] Order: int
    [<JsonPropertyName("note")>] Note: string option
}
```

**Loading Function** (add to `Loaders.fs`):
```fsharp
let loadPinnedPosts () = 
    let pinnedPostsPath = Path.Join("Data", "pinned-posts.json")
    if File.Exists(pinnedPostsPath) then
        File.ReadAllText(pinnedPostsPath)
        |> JsonSerializer.Deserialize<PinnedPost array>
        |> Array.sortBy (fun p -> p.Order)
    else
        [||]
```

**Timeline Integration** (modify `Builder.fs`):
```fsharp
let buildTimelineHomePage (allUnifiedItems: (string * GenericBuilder.UnifiedFeeds.UnifiedFeedItem list) list) =
    // Load pinned posts configuration
    let pinnedPostsConfig = loadPinnedPosts()
    
    // Flatten all items
    let allItemsFlattened = 
        allUnifiedItems
        |> List.collect snd
        |> List.sortByDescending (fun item -> DateTimeOffset.Parse(item.Date))
    
    // Extract pinned items based on configuration (using filename matching)
    let pinnedItems = 
        pinnedPostsConfig
        |> Array.choose (fun config ->
            allItemsFlattened 
            |> List.tryFind (fun item -> 
                // Extract filename from URL: /posts/my-post/ -> my-post
                let urlFileName = 
                    item.Url.TrimEnd('/').Split('/')
                    |> Array.last
                urlFileName = config.FileName && 
                item.ContentType = config.ContentType))
        |> Array.toList
    
    // Remove pinned items from chronological list
    let unpinnedItemsFlattened = 
        allItemsFlattened
        |> List.filter (fun item ->
            not (pinnedItems |> List.exists (fun pinned -> pinned.Url = item.Url)))
    
    // Take initial items from unpinned list
    let chronologicalInitialItems = 
        unpinnedItemsFlattened
        |> List.take (min 50 unpinnedItemsFlattened.Length)
    
    // Combine: pinned first, then chronological
    let initialItems = pinnedItems @ chronologicalInitialItems
    
    // ... rest of existing logic
```

**UI Enhancement** (modify `Views/LayoutViews.fs`):
Add a visual indicator for pinned posts in the timeline:
```fsharp
article [ 
    _class (if isPinned then "h-entry content-card pinned-post" else "h-entry content-card")
    attr "data-type" item.ContentType
    attr "data-date" item.Date
] [
    // Add pin indicator if pinned
    if isPinned then
        div [ _class "pin-indicator" ] [
            span [ _class "pin-icon" ] [ Text "📌" ]
            span [ _class "pin-label" ] [ Text "Pinned" ]
        ]
    // ... rest of card content
]
```

**CSS Styling**:
```css
.pinned-post {
    border-left: 4px solid var(--primary-color);
    background-color: var(--pinned-bg);
}

.pin-indicator {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.5rem;
    background-color: var(--accent-color);
    border-radius: 4px;
    margin-bottom: 1rem;
}

.pin-icon {
    font-size: 1.2rem;
}

.pin-label {
    font-size: 0.875rem;
    font-weight: 600;
    text-transform: uppercase;
}
```

#### Estimated Effort
- **Implementation Time**: 2-3 hours
- **Files Modified**: 5 files (Domain.fs, Loaders.fs, Builder.fs, LayoutViews.fs, timeline.css)
- **Testing Complexity**: Low (straightforward JSON loading and list manipulation)

---

### Option 2: Frontmatter YAML Property

**Approach**: Add a `pinned` or `pin_order` property to post frontmatter.

#### Configuration
```yaml
---
post_type: "article"
title: "My Important Post"
published_date: "10/04/2021 19:58 -05:00"
tags: [blogging,tooling]
pinned: true
pin_order: 1
---
```

#### Advantages
✅ **Self-Contained**: Pin metadata lives with the post
✅ **No External Dependencies**: No need to manage separate config files

#### Disadvantages
❌ **Scattered Configuration**: Must search through all posts to find pinned ones
❌ **Management Difficulty**: Hard to see all pinned posts at once
❌ **Manual Order Management**: Risk of duplicate order numbers
❌ **Type System Changes**: Requires modifying `PostDetails` type and all parsing logic
❌ **Limited to Posts**: Would need similar changes for notes, responses, etc.
❌ **Frontmatter Clutter**: Adds metadata to individual files

#### Implementation Details

Would require:
1. Modify `PostDetails` type in `Domain.fs` to add `Pinned` and `PinOrder` properties
2. Update post parsing in `MarkdownService`
3. Filter and sort logic in `Builder.fs`
4. No centralized view of pinned posts

#### Estimated Effort
- **Implementation Time**: 3-4 hours
- **Files Modified**: 8-10 files
- **Testing Complexity**: Medium (type system changes across multiple modules)

---

### Option 3: Hybrid Approach

**Approach**: Use frontmatter flag with JSON override configuration.

#### Configuration
- Posts have `pinnable: true` in frontmatter (optional)
- `Data/pinned-posts.json` specifies which pinnable posts are currently pinned

#### Advantages
✅ **Flexible Control**: Can mark posts as "pinnable" but control display separately
✅ **Safety**: Can't accidentally pin arbitrary posts

#### Disadvantages
❌ **Complexity**: More moving parts
❌ **Redundant Work**: Need to modify both frontmatter and config
❌ **Over-Engineering**: Adds complexity without clear benefits

---

## Final Recommendation

**Use Option 1: JSON Configuration File** ⭐

### Rationale
1. **Aligns with Existing Architecture**: The site already uses JSON config files in `Data/` directory for feeds, blogrolls, starter packs, etc.
2. **Centralized Management**: Single source of truth makes it easy to manage pinned posts
3. **Minimal Changes**: Clean implementation with limited code changes
4. **Flexible**: Can pin any content type, not just posts
5. **User-Friendly**: Easy to understand and maintain
6. **Version Controlled**: Git history shows when posts were pinned/unpinned

### Implementation Steps
1. Create `Domain.fs` type definition for `PinnedPost`
2. Add loader function in `Loaders.fs`
3. Create sample `Data/pinned-posts.json` file
4. Modify `buildTimelineHomePage` in `Builder.fs` to filter and order pinned posts
5. Update `timelineHomeViewStratified` in `Views/LayoutViews.fs` to add visual indicators
6. Add CSS styling for pinned posts
7. Update documentation

### Example Usage
User creates/edits `Data/pinned-posts.json`:
```json
[
    {
        "fileName": "automate-yaml-front-matter-vs-code-snippets",
        "contentType": "posts",
        "order": 1,
        "note": "VS Code snippets guide - popular"
    },
    {
        "fileName": "authorization-code-flow-python",
        "contentType": "posts",
        "order": 2,
        "note": "OAuth tutorial"
    }
]
```

**How to find the filename?**
- It's the markdown filename without the `.md` extension
- For `/posts/my-awesome-post/`, the filename is `my-awesome-post`
- Located in `_src/posts/my-awesome-post.md`

Then rebuild the site. The pinned posts will appear at the top of the timeline with a pin indicator.

### Addressing Permalink Concerns

**The Problem**: If URLs are used in the config, changing permalink structure requires updates in two places:
1. The URL generation logic in the code
2. The pinned posts JSON config

**The Solution**: Use **filename-based matching** instead:
- Filenames are stable and rarely change (they're the source markdown files)
- URLs are generated from filenames at build time
- If you change URL structure (e.g., `/posts/` → `/blog/`), only the code changes, not the config
- The config references the source of truth (the markdown file), not the derived URL

**Example scenario**:
- Filename: `automate-yaml-front-matter-vs-code-snippets.md`
- Current URL: `/posts/automate-yaml-front-matter-vs-code-snippets/`
- Config uses: `"fileName": "automate-yaml-front-matter-vs-code-snippets"`
- If you later decide URLs should be `/blog/...`, the config doesn't need updating!

### Alternative: Title-Based Matching

If you're concerned about filename changes, an alternative approach is **title-based matching**:

```json
[
    {
        "title": "Automate YAML front-matter generation with custom Visual Studio Code snippets",
        "contentType": "posts",
        "order": 1
    }
]
```

**Pros**: Titles are even more stable than filenames
**Cons**: Longer strings, potential for typos, title changes require config updates

However, **filename-based is still recommended** because:
1. Filenames are the actual source files and rarely change
2. Shorter and easier to type
3. Easy to find (just look at `_src/posts/` directory)
4. If you rename a file, you'd need to update Git history/redirects anyway

### Future Enhancements
- Admin UI for managing pinned posts (if needed)
- Pin expiration dates (auto-unpin after certain date)
- Pin to specific sections (e.g., "Featured Posts" vs "Pinned Posts")
- Per-content-type pinning (pin different items for different content filters)

## Questions?
Please review these recommendations and let me know:
1. Do you prefer Option 1 (JSON config)?
2. Should pinned posts have a visual indicator (📌 icon/label)?
3. How many posts would you typically want to pin (for initial load sizing)?
4. Should pinned posts count toward the initial 50 items, or be in addition to them?
