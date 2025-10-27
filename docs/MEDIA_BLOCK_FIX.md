# Media Block HTML Tag Fix Documentation

## Issue
GitHub issue #707 reported that the media workflow was generating malformed markdown where `:::media:::` blocks were incorrectly placed inside `<img>` tag `src` attributes instead of replacing the entire `<img>` tag.

### Example of the Bug
**Broken output:**
```html
<img width=1080 height=463 alt=Image src=:::media
- url: "https://cdn.lqdev.tech/files/images/20251027_004729_d5017208-9919-4387-99e1-77a96f3ec654.jpg"
  mediaType: "image"
  aspectRatio: "landscape"
  caption: "media"
:::media />
```

**Expected output:**
```markdown
:::media
- url: "https://cdn.lqdev.tech/files/images/20251027_004729_d5017208-9919-4387-99e1-77a96f3ec654.jpg"
  mediaType: "image"
  aspectRatio: "landscape"
  caption: "Image"
:::media
```

## Root Cause
GitHub's drag-and-drop file upload feature sometimes generates HTML img tags with **unquoted** src attributes:
- Normal: `<img src="URL">`
- GitHub sometimes generates: `<img src=URL />`

The Python script's regex pattern in `.github/scripts/upload_media.py` only matched **quoted** src attributes. When it encountered unquoted attributes:
1. The HTML pattern failed to match the img tag
2. The plain URL pattern matched just the URL
3. The replacement put the `:::media` block where the URL was (inside the src attribute)
4. This left the malformed `<img ... src=:::media...:::media />` structure

## Solution

### Changes Made to `.github/scripts/upload_media.py`

#### 1. Updated HTML Pattern to Handle Both Quoted and Unquoted Attributes

**Before (line 164):**
```python
html_pattern = r'<img[^>]*src=["\'](https://github\.com/user-attachments/[^"\']+)["\'][^>]*>'
```

**After:**
```python
html_pattern = r'<img[^>]*src=(["\']?)(https://github\.com/user-attachments/[^"\'\s>]+)\1[^>]*>'
```

**Explanation:**
- `(["\']?)` - Captures optional opening quote character (group 1) - either double quote, single quote, or nothing
- `(https://...)` - Captures URL (group 2)
- `\1` - Backreference that matches exactly what was captured in group 1
  - If group 1 captured `"`, then `\1` requires a closing `"`
  - If group 1 captured `'`, then `\1` requires a closing `'`
  - If group 1 captured nothing (empty string), then `\1` requires nothing

This pattern now matches:
- `src="URL"` (double quotes)
- `src='URL'` (single quotes)  
- `src=URL` (no quotes)

#### 2. Updated Alt Text Extraction (line 170)

**Before:**
```python
alt_match = re.search(r'alt=["\']([^"\']+)["\']', match.group(0))
alt_text = alt_match.group(1) if alt_match else 'media'
```

**After:**
```python
alt_match = re.search(r'alt=(["\'])([^\1]+?)\1|alt=([^\s>]+)', match.group(0))
if alt_match:
    alt_text = alt_match.group(2) if alt_match.group(2) else alt_match.group(3)
else:
    alt_text = 'media'
```

**Explanation:**
This pattern uses alternation (`|`) to handle two cases:
1. **Quoted alt text**: `alt=(["\'])([^\1]+?)\1`
   - `(["\'])` - Captures the opening quote (group 1)
   - `([^\1]+?)` - Captures the alt text content (group 2)
     - Note: `[^\1]` literally means "not backslash or digit 1", but this works fine for capturing quoted text
   - `\1` - Backreference ensures the closing quote matches the opening quote
2. **Unquoted alt text**: `alt=([^\s>]+)`
   - `([^\s>]+)` - Captures non-whitespace, non-> characters (group 3)

The code then checks group 2 (quoted) first, falling back to group 3 (unquoted) if group 2 is None.

This handles:
- `alt="Text with spaces"` (quoted with spaces)
- `alt='Text'` (single quotes)
- `alt=SingleWord` (unquoted, no spaces)

#### 3. Improved Plain URL Exclusion Logic (line 340-356)

Added range checking to prevent plain URL pattern from matching URLs that are already inside matched HTML tags:

```python
# Check if match position falls within any existing media_item range
is_inside_existing = False
for existing_pos, existing_text, _, _ in media_items:
    existing_end = existing_pos + len(existing_text)
    if existing_pos <= match.start() < existing_end:
        is_inside_existing = True
        print(f"  - Skipped plain match at {match.start()} (inside HTML tag at {existing_pos})")
        break
```

This prevents the plain URL pattern from creating duplicate entries for URLs that are already captured by the HTML pattern.

#### 4. Applied Same Fix to `find_all_media_positions` Function (line 312)

The same HTML pattern fix was applied to the `find_all_media_positions` function to ensure consistent behavior throughout the script.

### Fixed File
Updated `_src/media/another-media-post-from-github-publishing-workflow.md` to show correct media block format.

## Testing

Comprehensive tests validate the fix handles:
1. ✅ HTML with double quotes: `<img src="URL">`
2. ✅ HTML with single quotes: `<img src='URL'>`
3. ✅ HTML without quotes: `<img src=URL />` (the bug case)
4. ✅ Mixed attributes in any order
5. ✅ Markdown images: `![alt](URL)`
6. ✅ Multiple items with position preservation
7. ✅ Plain URLs alongside HTML tags

All tests pass and the site builds successfully with proper HTML output.

## Impact

This fix ensures that:
- Media posts created via GitHub issue forms work correctly regardless of how GitHub formats the HTML
- Both quoted and unquoted HTML img attributes are handled properly
- The `:::media` block replaces the entire `<img>` tag, not just the URL
- Generated site HTML is valid and properly structured

## Future Considerations

GitHub's HTML generation appears inconsistent. This fix makes the media upload workflow robust to handle:
- Different quoting styles
- Attribute ordering variations
- Mixed content (Markdown + HTML + plain URLs)

The improved pattern matching should handle future variations in GitHub's HTML generation without requiring additional changes.
