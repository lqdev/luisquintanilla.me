# Media Block Position Preservation Fix

## Problem

In issue #693, media items (GitHub attachments, YouTube URLs, direct links) were being moved to the end of posts instead of staying in their original positions.

### Example from Issue #693

**User Input:**
```markdown
Here is another post

<img src="...github-attachment...">

I could also post YT

https://www.youtube.com/watch?v=fAV_J5-dMls

And direct links

https://cdn.lqdev.tech/files/images/...jpg
```

**Wrong Output (Before Fix):**
```markdown
Here is another post

I could also post YT

[![Video]...]

And direct links

:::media::: [all media at end]
```

**Correct Output (After Fix):**
```markdown
Here is another post

:::media::: [image here]

I could also post YT

[![Video]...]

And direct links

:::media::: [direct link here]
```

## Root Cause

The original implementation in `.github/scripts/upload_media.py`:
1. Extracted all media URLs from content
2. **Removed** all media URLs completely
3. **Appended** all media blocks at the end

This lost the position information of where each media item originally appeared.

## Solution

Refactored the transformation logic to preserve positions:

### New Approach

1. **Find All Media Positions** - Track position of each media item in original content
2. **Sort by Position** - Order media items by their appearance (descending for replacement)
3. **Replace In-Place** - Replace each media item with its corresponding block/syntax at its original position

### Implementation Details

#### New Function: `find_all_media_positions()`
```python
def find_all_media_positions(content, github_attachments, youtube_urls, direct_media_urls):
    """
    Find all media items in the content with their positions.
    Returns a list of (position, match_text, media_type, data) tuples sorted by position.
    """
```

This function:
- Finds all GitHub attachment references (markdown, HTML img tags, plain URLs)
- Finds all YouTube URLs
- Finds all direct media URLs
- Returns sorted list with position information

#### New Function: `transform_content_preserving_positions()`
```python
def transform_content_preserving_positions(content, url_mapping, youtube_urls, direct_media_urls):
    """
    Transform content by replacing media items in-place with their corresponding
    media blocks or formatted syntax, preserving the original position of each item.
    """
```

This function:
- Calls `find_all_media_positions()` to get all media with positions
- Replaces media items from end to start (to maintain string positions during replacement)
- Each item replaced with its corresponding:
  - `:::media:::` block for GitHub attachments and direct links
  - YouTube thumbnail markdown for YouTube URLs
- Cleans up any remaining empty img tags

### Backward Compatibility

Kept `transform_content_to_media_blocks()` function for existing test compatibility.

## Testing

Created comprehensive test suite:

### 1. Position Preservation Test (`test-media-position-ordering.py`)
- Documents expected behavior
- Validates current wrong behavior
- Defines correct expected output

### 2. Position Preservation Fix Test (`test-position-preservation-fix.py`)
- Tests exact scenario from issue #693
- Tests multiple mixed media items
- Validates correct position ordering

### 3. End-to-End Workflow Test (`test-end-to-end-workflow.py`)
- Complete workflow simulation from issue content to final markdown
- Tests extraction, transformation, and file writing
- Validates exact match with expected output

### 4. Existing Media Workflow Tests (`test-media-workflow.py`)
- All existing tests continue to pass
- Validates backward compatibility

## Test Results

✅ All 9 tests pass:
- 4/4 existing media workflow tests
- 2/2 position preservation fix tests
- 1/1 end-to-end workflow test
- 2/2 position ordering specification tests

## Files Changed

- `.github/scripts/upload_media.py` - Core fix implementation
- `test-scripts/test-media-position-ordering.py` - Position ordering spec
- `test-scripts/test-position-preservation-fix.py` - Fix validation tests
- `test-scripts/test-end-to-end-workflow.py` - Complete workflow test

## Impact

This fix ensures that when users create media posts through GitHub issue forms:
- Images appear where they uploaded/dragged them
- YouTube videos appear where they pasted the URL
- Direct media links appear where they placed them

The content flow and narrative structure is preserved exactly as the user intended.
