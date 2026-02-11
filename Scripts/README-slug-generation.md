# Slug Generation in Processing Scripts

## Quick Reference

All post processing scripts follow a standardized slug generation pattern to ensure consistency and prevent filename conflicts.

## Standard Pattern

```fsharp
// Generate filename - only append date when no custom slug provided
let filename = 
    match customSlug with
    | Some _ -> sprintf "%s.md" finalSlug  // Use slug as-is when custom slug provided
    | None -> sprintf "%s-%s.md" finalSlug (now.ToString("yyyy-MM-dd"))  // Append date when no custom slug
```

## Behavior

### When Custom Slug is Provided
- Use the custom slug as-is (after sanitization)
- **No date is appended**
- Example: Custom slug "my-post" → `my-post.md`

### When No Custom Slug (Auto-Generated)
- Generate slug from title
- **Append current date in YYYY-MM-DD format**
- Example: Title "My Post" → `my-post-2024-01-15.md`

## Scripts Following This Pattern

| Script | Post Type | Status |
|--------|-----------|--------|
| `process-github-issue.fsx` | note | ✅ Standardized |
| `process-bookmark-issue.fsx` | bookmark | ✅ Standardized |
| `process-media-issue.fsx` | media | ✅ Standardized |
| `process-playlist-issue.fsx` | playlist | ✅ Standardized |
| `process-response-issue.fsx` | response | ✅ Standardized |
| `process-review-issue.fsx` | review | ✅ Standardized (helper) |

**Note**: The review script uses a helper function (`generateSlug`) but achieves the same result.

## Testing

Run the automated test suite to verify all scripts follow the pattern:

```bash
./Scripts/test-slug-generation.sh
```

Expected output:
```
Testing Note...       ✓ Standard pattern found
Testing Bookmark...   ✓ Standard pattern found
Testing Media...      ✓ Standard pattern found
Testing Playlist...   ✓ Standard pattern found
Testing Response...   ✓ Standard pattern found
Testing Review...     ✓ Alternative pattern found

✓ Passed: 6
All tests passed!
```

## Why This Matters

### Problem It Solves
Without date appending, posts with similar titles could overwrite each other:
- "My Thoughts" on Monday → `my-thoughts.md`
- "My Thoughts" on Tuesday → `my-thoughts.md` ❌ **CONFLICT!**

With date appending:
- "My Thoughts" on Monday → `my-thoughts-2024-01-15.md`
- "My Thoughts" on Tuesday → `my-thoughts-2024-01-16.md` ✅ **UNIQUE!**

### Benefits
1. **Prevents Overwrites**: Each auto-generated post gets a unique filename
2. **Maintains Consistency**: All post types follow the same rules
3. **Preserves Flexibility**: Custom slugs still work for permanent URLs
4. **Enables Tracking**: Date in filename shows when post was created

## Examples

### Bookmark Post
```fsharp
// User input:
let title = "Great Web Article"
let customSlug = None  // Left blank

// Output:
great-web-article-2024-01-15.md
```

### Media Post with Custom Slug
```fsharp
// User input:
let title = "Vacation Photos"
let customSlug = Some "hawaii-trip"

// Output:
hawaii-trip.md  // No date appended
```

### Playlist Post
```fsharp
// User input:
let title = "Coding Focus Music"
let customSlug = None

// Output:
coding-focus-music-2024-01-15.md
```

## Implementation Details

### Date Format
- Format: `yyyy-MM-dd` (ISO 8601)
- Example: `2024-01-15`
- Timezone: Fixed offset UTC-05:00 (EST-equivalent; daylight saving time is not applied)

### Slug Sanitization
All slugs (custom or auto-generated) are sanitized:
1. Convert to lowercase
2. Replace spaces and underscores with hyphens
3. Remove special characters (keep only a-z, 0-9, -)
4. Collapse multiple hyphens to one
5. Trim leading/trailing hyphens

## Adding New Post Types

When adding a new post type, ensure the processing script follows the standard pattern:

```fsharp
// 1. Parse customSlug from arguments
let customSlug = 
    if args.Length > X && not (String.IsNullOrWhiteSpace(args.[X])) 
    then Some(args.[X]) 
    else None

// 2. Generate slug using standard sanitization
let finalSlug = 
    match customSlug with
    | Some slug -> sanitizeSlug slug
    | None -> generateSlugFromTitle title

// 3. Apply standard filename pattern
let filename = 
    match customSlug with
    | Some _ -> sprintf "%s.md" finalSlug
    | None -> sprintf "%s-%s.md" finalSlug (now.ToString("yyyy-MM-dd"))
```

Then update `test-slug-generation.sh` to include the new post type.

## Troubleshooting

### Test Failing for New Script
1. Check that script has `match customSlug with` pattern
2. Verify it uses `now.ToString("yyyy-MM-dd")` for date
3. Ensure pattern matches exactly as shown above

### Date Not Appearing in Filename
1. Check if custom slug was provided (intentional)
2. Verify `customSlug` is correctly parsed as `None`
3. Check if script was updated with new pattern

### Filename Conflicts Still Occurring
1. Verify script uses the standard pattern
2. Run test suite: `./Scripts/test-slug-generation.sh`
3. Check if custom slugs are being provided unintentionally

## Related Documentation

- [SLUG_GENERATION_GUIDE.md](../docs/SLUG_GENERATION_GUIDE.md) - User guide
- [SLUG_GENERATION_AUDIT.md](../docs/SLUG_GENERATION_AUDIT.md) - Technical audit
- [SLUG_GENERATION_IMPLEMENTATION.md](../docs/SLUG_GENERATION_IMPLEMENTATION.md) - Implementation details

## History

- **2024-01**: Standardized across all post types
- **Before**: Inconsistent - only notes and responses had date appending

---

**Maintainer Note**: This pattern is critical for preventing filename conflicts. Do not modify without updating all post types and tests.
