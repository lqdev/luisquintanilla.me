# Pinned Posts Feature

## Overview

The Pinned Posts feature allows you to pin important content to the top of your timeline, ensuring key posts remain visible regardless of publication date. Pinned posts appear before all chronological content with a distinctive visual indicator and can be easily managed through a JSON configuration file.

## Features

- **Timeline Prominence**: Pinned posts always appear at the top of the homepage
- **Visual Distinction**: Pin indicator (📌) and subtle styling differentiate pinned content
- **Multiple Posts**: Pin as many posts as needed with customizable ordering
- **Any Content Type**: Pin posts, notes, responses, bookmarks, or any unified timeline content
- **Zero Content Duplication**: Uses filename matching, not URLs, for stability
- **Version Controlled**: Git tracks when posts are pinned/unpinned
- **Theme Integrated**: Works seamlessly with desert theme (light/dark modes)

## Visual Design

### Pin Indicator

Pinned posts display with:
- 📌 "Pinned" badge at the top of the card
- Subtle left accent border in theme color
- Slightly different background for visual distinction
- All original content preserved (title, date, content, tags)

### Example Timeline View

```
┌─────────────────────────────────────────────────────────────┐
│ 📌 Pinned                                                    │
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
│ Oct 12, 2024                            Note                 │
│                                                              │
│ [Most recent unpinned content...]                           │
└─────────────────────────────────────────────────────────────┘
```

## Configuration

### Configuration File

**Location**: `Data/pinned-posts.json`

**Format**:
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

### Configuration Fields

**Required:**
- `fileName` - Markdown filename without `.md` extension
- `contentType` - Content type directory name (posts, notes, responses, etc.)
- `order` - Display order (1 = first, 2 = second, etc.)

**Optional:**
- `note` - Personal note about why this post is pinned (not displayed publicly)

### Supported Content Types

Any content type in the unified timeline system:
- `posts` - Long-form blog posts
- `notes` - Short notes and updates
- `responses` - All response types (bookmarks, stars, replies, reshares)
- `bookmarks` - Specifically bookmark responses
- `stars` - Star responses
- `replies` - Reply responses
- `reshares` - Reshare responses
- `reviews` - Book/media reviews
- `media` - Media posts

## Usage Instructions

### Finding the Filename

The filename is the markdown file name without the `.md` extension:

```bash
# Example file path
_src/posts/automate-yaml-front-matter-vs-code-snippets.md

# Filename to use
automate-yaml-front-matter-vs-code-snippets
```

### Pinning a Single Post

1. Identify the content you want to pin
2. Find its markdown filename
3. Edit `Data/pinned-posts.json`:

```json
[
    {
        "fileName": "your-post-filename",
        "contentType": "posts",
        "order": 1,
        "note": "Important announcement"
    }
]
```

4. Rebuild the site: `dotnet run`

### Pinning Multiple Posts

Order determines display sequence (lower numbers appear first):

```json
[
    {
        "fileName": "most-important-post",
        "contentType": "posts",
        "order": 1,
        "note": "Primary announcement"
    },
    {
        "fileName": "second-important-post",
        "contentType": "posts",
        "order": 2,
        "note": "Secondary feature"
    },
    {
        "fileName": "important-note",
        "contentType": "notes",
        "order": 3,
        "note": "Quick update pinned"
    }
]
```

### Pinning Different Content Types

Mix any content types on your timeline:

```json
[
    {
        "fileName": "welcome-post",
        "contentType": "posts",
        "order": 1,
        "note": "Welcome message for new visitors"
    },
    {
        "fileName": "project-announcement",
        "contentType": "notes",
        "order": 2,
        "note": "Current project status"
    },
    {
        "fileName": "important-bookmark",
        "contentType": "responses",
        "order": 3,
        "note": "Essential resource"
    }
]
```

### Unpinning Posts

Simply remove the entry from `Data/pinned-posts.json` and rebuild:

```json
[
    // Remove the post object completely
    // The post will return to chronological position
]
```

### Reordering Pins

Change the `order` field values and rebuild:

```json
[
    {
        "fileName": "now-most-important",
        "contentType": "posts",
        "order": 1  // Changed from 2 to 1
    },
    {
        "fileName": "now-second",
        "contentType": "posts",
        "order": 2  // Changed from 1 to 2
    }
]
```

## Technical Implementation

### How It Works

1. **Build Time**: Site generator loads `Data/pinned-posts.json`
2. **Matching**: Extracts filename from unified content URLs
3. **Comparison**: Matches against configuration entries
4. **Ordering**: Pinned posts placed first (by order), then chronological content
5. **Rendering**: Pinned posts get special CSS class and visual indicator

### Files Modified

**Core Application:**
- `Domain.fs` - `PinnedPost` type definition
- `Loaders.fs` - `loadPinnedPosts()` function
- `Builder.fs` - `buildTimelineHomePage()` with pinning logic

**View Layer:**
- `Views/LayoutViews.fs` - Pin indicator rendering
- `Views/ComponentViews.fs` - Card rendering with pin support

**Styling:**
- `_src/css/custom/timeline.css` - Pinned post visual styles

### Build Output

When site builds successfully with pinned posts:

```
✅ Timeline homepage created with 1 pinned posts, 50 chronological items 
   from 1198 total items across all content types
```

### Filename Matching Logic

```fsharp
let isPinned (item: UnifiedFeedItem) (pinnedPosts: PinnedPost list) : bool =
    let itemFileName = 
        item.Url
        |> Path.GetFileNameWithoutExtension
        
    pinnedPosts 
    |> List.exists (fun pin -> 
        pin.FileName = itemFileName && 
        pin.ContentType = item.ContentType)
```

**Why Filename-Based?**
- URLs can change (permalink structure updates)
- Filenames remain stable across builds
- No duplicate content management needed
- Simple, reliable matching

## Best Practices

### When to Pin

**Good Use Cases:**
- Welcome messages for first-time visitors
- Current project announcements
- Important site updates
- Popular evergreen content
- Critical resources or guides
- Breaking news or urgent updates

**Avoid:**
- Pinning too many posts (3-5 maximum recommended)
- Pinning outdated content
- Pinning without a specific purpose
- Leaving posts pinned indefinitely

### Content Selection

**Consider:**
- Is this content relevant to most visitors?
- Does it provide immediate value?
- Is it time-sensitive or evergreen?
- Will it still be relevant in a month?

**Rotation Strategy:**
- Review pinned posts monthly
- Unpin outdated announcements
- Replace with current priorities
- Keep pins fresh and relevant

### Visual Design Considerations

**Current Design:**
- Subtle enough not to overwhelm
- Clear enough to indicate importance
- Consistent with overall theme
- Accessible color contrast

**Theme Compatibility:**
- Works in light and dark modes
- Uses theme accent color for border
- Maintains readability standards
- Preserves all interactive elements

## Troubleshooting

### Post Not Pinning

**Check:**
1. Is the filename correct (without .md extension)?
2. Is the content type correct?
3. Did you rebuild the site after editing JSON?
4. Does the content file actually exist?
5. Is the JSON syntax valid?

**Verify Filename:**
```bash
# List all markdown files to find the exact name
ls -la _src/posts/ | grep "your-post"
```

**Validate JSON:**
```bash
# Check JSON syntax
cat Data/pinned-posts.json | jq '.'
```

### Wrong Order

**Issue**: Pinned posts appear in wrong sequence

**Solution**: Check `order` field values:
- Lower numbers appear first
- Numbers should be sequential (1, 2, 3...)
- Rebuild after changing order

### Missing Pin Indicator

**Issue**: Post is pinned but indicator not showing

**Solution**: 
1. Clear browser cache
2. Check CSS file is being loaded
3. Verify `timeline.css` includes pinned styles
4. Rebuild site completely

### Pin Not Persisting

**Issue**: Pin disappears after rebuild

**Solution**:
1. Verify `Data/pinned-posts.json` is committed to git
2. Check file permissions
3. Ensure JSON syntax is valid
4. Rebuild site after any changes

## Advanced Usage

### Conditional Pinning

While not built-in, you could implement conditional pinning by:

1. Creating multiple JSON files (e.g., `pinned-posts-events.json`, `pinned-posts-normal.json`)
2. Modifying `Loaders.fs` to load conditionally
3. Using environment variables to switch configs
4. Implementing date-based auto-unpinning logic

### Analytics Integration

Track pinned post performance by:

1. Adding UTM parameters to pinned post links
2. Using `data-*` attributes for tracking
3. Monitoring click-through rates
4. A/B testing different pinning strategies

### Automated Management

Automate pinning with:

1. GitHub Actions to update JSON
2. Scheduled pin rotation
3. Event-triggered pinning
4. API-driven pin management

## Related Features

### Timeline System

Pinned posts integrate with:
- Progressive loading (pinned posts in initial load)
- Content type filtering (pins respect filters)
- Tag filtering (pins appear in filtered views)
- Desktop theme toggle (pins styled in all modes)

### RSS Feeds

**Note**: Pinned posts appear in timeline HTML only, not in RSS feeds. RSS feeds maintain strict chronological order for feed reader compatibility.

### Text-Only Site

Pinned posts also appear in text-only version at `/text/` with accessible pin indicator.

## Comparison with Other Platforms

### Twitter/X Pinned Tweets
- **Similar**: Single pinned item at profile top
- **Advantage**: We support multiple pins with ordering

### Reddit Pinned Posts
- **Similar**: Multiple pins in subreddits
- **Advantage**: More flexible content type support

### Blog Sticky Posts
- **Similar**: Posts stuck to top of blog homepage
- **Advantage**: JSON configuration vs database flags

## Performance Impact

### Build Time
- Negligible increase (~5-10ms)
- JSON parsing is fast
- Matching algorithm is O(n×m) where n=content items, m=pinned posts
- Typically m is small (1-5), so effectively O(n)

### Page Load
- No additional HTTP requests
- Pinned styling in existing CSS
- No JavaScript required
- Zero performance impact

### User Experience
- Instant visual feedback
- No loading delays
- Works with progressive loading
- Compatible with all browsers

## Future Enhancements

### Potential Features

**Under Consideration:**
- Date-based auto-expiry
- Pin categories (announcements, features, etc.)
- A/B testing framework
- Analytics dashboard
- User preferences for hiding pins

**Not Planned:**
- Per-user pin customization
- Dynamic server-side pinning
- Real-time pin updates
- Complex pin algorithms

## Related Documentation

- [Timeline System](../README.md#performance--ux) - Overall timeline architecture
- [Feed Architecture](feed-architecture.md) - RSS feed generation
- [Content Types](../README.md#content-structure) - All supported content types
- [VS Code Snippets](vs-code-snippets-modernization.md) - Content creation workflow

## Examples in the Wild

Current pinned posts on the site:
- [VS Code Snippets Guide](https://www.lqdev.me/posts/automate-yaml-front-matter-vs-code-snippets/) - Popular evergreen tutorial

## Migration Notes

### From Manual Pinning

If you previously manually ordered posts by changing dates:

1. Restore original publication dates
2. Add posts to `Data/pinned-posts.json`
3. Rebuild site
4. Verify pins appear correctly

### From Other Systems

If migrating from another platform:

1. Identify pinned/featured content
2. Find corresponding markdown filenames
3. Create configuration JSON
4. Test with a few posts first
5. Roll out complete pin list

## Summary

The Pinned Posts feature provides a simple, flexible way to highlight important content while maintaining clean separation between configuration and content. Its filename-based matching ensures stability across site updates, and the minimal visual design keeps the focus on content while clearly indicating pinned status.

**Key Benefits:**
- ✅ No content duplication needed
- ✅ Simple JSON configuration
- ✅ Version controlled changes
- ✅ Theme integrated design
- ✅ Zero performance impact
- ✅ Flexible content type support
