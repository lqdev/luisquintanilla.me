# ActivityPub Media Rendering Fix

## Summary

Fixed ActivityPub notes containing media to properly render images in federated clients like Mastodon instead of displaying raw `:::media` custom block syntax.

## Problem

Before this fix:
- Mastodon would show: `:::media - url: "..." mediaType: "image" ...:::media 🥶🥶🥶`
- Images were not displayed properly in federated clients
- Raw markdown syntax was visible to users

**Root Cause:**
1. `UnifiedFeedItem.Content` contained raw markdown with `:::media` blocks
2. `ActivityPubBuilder.convertToNote` passed this directly to the `content` field
3. Mastodon strips inline `<img>` tags from HTML and only renders images from the `attachment` array

## Solution

Implemented proper ActivityPub media handling following Mastodon federation best practices:

### 1. Added ActivityPubImage Type
```fsharp
type ActivityPubImage = {
    Type: string              // Always "Image"
    MediaType: string         // "image/jpeg", "image/png", etc.
    Url: string              // Image URL
    Name: string option      // Alt text/caption
}
```

### 2. Updated ActivityPubNote Type
Added `Attachment` field to hold image attachments array:
```fsharp
type ActivityPubNote = {
    // ... existing fields ...
    Attachment: ActivityPubImage array option
}
```

### 3. Implemented Media Extraction
Created `extractMediaAttachments` function that:
- Parses `:::media` blocks using regex
- Extracts ALL image URLs (handles single images and multi-image albums)
- Extracts captions/alt text for each image
- Removes `:::media` blocks from content
- Returns (cleaned content, Image attachments array)

### 4. Updated Note Conversion
Modified `convertToNote` to:
- Extract media attachments before generating note
- Use cleaned content (without `:::media` syntax)
- Populate `attachment` array with proper Image objects

## Testing

✅ **Test Results:**
- Single image media posts work correctly
- Multi-image albums work correctly (all images included)
- Posts without media work correctly (no attachment field)
- All required ActivityPub fields present
- Build succeeds without errors

### Example Output

**Before:**
```json
{
  "content": ":::media\n- url: \"https://cdn.lqdev.tech/files/images/photo.jpg\"\n  caption: \"Weather in Chicago\"\n:::media\n🥶🥶🥶",
  "attachment": null
}
```

**After:**
```json
{
  "content": "🥶🥶🥶",
  "attachment": [
    {
      "type": "Image",
      "mediaType": "image/jpeg",
      "url": "https://cdn.lqdev.tech/files/images/photo.jpg",
      "name": "Weather in Chicago"
    }
  ]
}
```

## Impact

- ✅ Fixed media rendering in Mastodon and other ActivityPub clients
- ✅ Images now display properly below posts (like native Mastodon posts)
- ✅ Alt text/captions preserved for accessibility
- ✅ Supports both single images and multi-image albums
- ✅ No impact on website HTML rendering or RSS feeds
- ✅ Backward compatible (posts without media unchanged)

## Files Modified

- `ActivityPubBuilder.fs` - Core implementation
  - Added `ActivityPubImage` type
  - Updated `ActivityPubNote` type with `Attachment` field
  - Added `detectMediaTypeFromUrl` function
  - Added `extractMediaAttachments` function
  - Updated `convertToNote` function
- `test_activitypub_media.sh` - Test script to verify functionality

## Technical Details

### Media Type Detection
Automatically detects proper MIME types from file extensions and sets correct ActivityPub types:
- **Images** (`Type: "Image"`): jpg, png, gif, webp
- **Videos** (`Type: "Video"`): mp4, webm
- **Audio** (`Type: "Audio"`): mp3, wav, ogg

The implementation correctly determines the ActivityPub `type` field based on the detected MIME type, ensuring proper rendering in federated clients.

### Regex Pattern
```fsharp
let mediaPattern = @":::media\s*([\s\S]*?):::(?:media)?"
let urlPattern = @"url:\s*[""']([^""']+)[""']"
let captionPattern = @"caption:\s*[""']([^""']+)[""']"
let altPattern = @"alt:\s*[""']([^""']+)[""']"
```

### Caption/Alt Text Priority
1. First tries `caption` field
2. Falls back to `alt` field
3. If neither exists, caption is `None`

## References

- [ActivityPub Spec](https://www.w3.org/TR/activitypub/)
- [ActivityStreams Vocabulary](https://www.w3.org/TR/activitystreams-vocabulary/)
- [Mastodon ActivityPub Implementation](https://docs.joinmastodon.org/spec/activitypub/)
- [Fediverse Best Practices](https://socialhub.activitypub.rocks/t/guide-for-new-activitypub-implementers/479)
