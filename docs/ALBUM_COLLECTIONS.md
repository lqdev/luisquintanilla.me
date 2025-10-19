# Album Collections Documentation

## Overview

Album Collections are curated photo and media groupings organized by events, themes, projects, or any freeform organization. They complement the existing media timeline posts by providing a way to group related media together in a dedicated collection.

## Architecture

- **Content Type**: `AlbumCollection` in Domain.fs
- **Storage Location**: `_src/albums/*.md`
- **Output Path**: `/collections/albums/{album-slug}/`
- **RSS Feed**: `/collections/albums/feed.xml`
- **Content Format**: YAML frontmatter + `:::media:::` blocks

## Key Features

1. **Flexible Organization**: Albums can represent events, themes, locations, projects, series, or any custom grouping
2. **Location Support**: Optional latitude/longitude metadata for geolocation
3. **Timeline Integration**: Albums appear in the unified timeline feed
4. **Search Integration**: Fully searchable by title and tags
5. **RSS Feeds**: Dedicated RSS feed for album updates
6. **Native Browser Components**: No external JavaScript dependencies for viewing

## Content Structure

### Frontmatter Fields

```yaml
---
title: "Album Title"                    # Required: Album name
description: "Album description"        # Required: Brief description
date: "2024-10-15 14:30 -05:00"        # Required: Album date (publish or event date)
tags: ["photography", "events"]         # Required: Array of tags
location:                               # Optional: Geographic location
  lat: 41.8781                          # Latitude
  lon: -87.6298                         # Longitude
---
```

### Media Content

Albums use the existing `:::media:::` block syntax for content:

```markdown
:::media
- url: "http://cdn.lqdev.tech/files/images/image1.jpg"
  alt: "Descriptive alt text"
  mediaType: "image"
  aspectRatio: "landscape"
  caption: "Optional caption"
- url: "http://cdn.lqdev.tech/files/videos/video1.mp4"
  alt: "Video description"
  mediaType: "video"
  aspectRatio: "landscape"
  caption: "Video caption"
:::media
```

## Creating an Album

### Using VS Code Snippets

1. Create a new file in `_src/albums/your-album-name.md`
2. Type `album-collection` to trigger the "Album Collection metadata" snippet (defined in `.vscode/metadata.code-snippets`)
3. Fill in the required fields
4. Add additional media items to the `:::media:::` block as needed

### Manual Creation

1. Copy the template from `.templates/album-collection.txt`
2. Customize the frontmatter fields
3. Add your media items
4. Save the file in `_src/albums/`

### Example

```markdown
---
title: "Summer Vacation 2024"
description: "Photos from our amazing summer trip to the mountains"
date: "2024-07-15 10:00 -05:00"
tags: ["travel", "vacation", "mountains", "summer"]
location:
  lat: 39.5501
  lon: -105.7821
---

:::media
- url: "http://cdn.lqdev.tech/files/images/mountain-sunrise.jpg"
  alt: "Sunrise over the Rocky Mountains"
  mediaType: "image"
  aspectRatio: "landscape"
  caption: "Early morning hike to catch the sunrise"
- url: "http://cdn.lqdev.tech/files/images/campfire.jpg"
  alt: "Evening campfire"
  mediaType: "image"
  aspectRatio: "landscape"
  caption: "Cozy evening by the fire"
:::media
```

## Album Announcements

To announce an album on your timeline, create a media post with a standardized pattern:

```markdown
---
post_type: "media"
title: "Album: Summer Vacation 2024"
published_date: "2024-07-20 15:00 -05:00"
tags: ["albums", "travel"]
---

Check out photos from our summer vacation!

[View full album](/collections/albums/summer-vacation-2024/)
```

## Technical Implementation

### Domain Types

```fsharp
type AlbumCollectionLocation = {
    Latitude: float
    Longitude: float
}

type AlbumCollectionDetails = {
    Title: string
    Description: string
    Date: string
    Location: AlbumCollectionLocation option
    Tags: string array
}

type AlbumCollection = {
    FileName: string
    Metadata: AlbumCollectionDetails
    Content: string
}
```

### Processing Pipeline

1. **Load**: `loadAlbumCollections` reads markdown files from `_src/albums/`
2. **Parse**: `AlbumCollectionProcessor.create()` parses frontmatter and content
3. **Render**: Custom blocks are processed by `MediaBlock` renderer
4. **Output**: Individual pages and listing page generated
5. **RSS**: Feed items created for each album
6. **Search**: Indexed for full-text search

### Build Process

Albums are integrated into the main build pipeline:

1. `buildAlbumCollections()` generates individual album pages
2. Album collections listing page created at `/collections/albums/`
3. RSS feed generated at `/collections/albums/feed.xml`
4. Albums included in unified timeline feed
5. Search index updated with album content

## URL Structure

- **Collections Index**: `/collections/`
- **Albums Index**: `/collections/albums/`
- **Individual Album**: `/collections/albums/{album-slug}/`
- **RSS Feed**: `/collections/albums/feed.xml`

## Best Practices

### Naming Conventions

- Use descriptive, URL-friendly filenames: `summer-vacation-2024.md`
- Avoid spaces, special characters, and uppercase in filenames
- Use hyphens to separate words

### Date Field Usage

The `date` field is flexible and can represent:
- **Publish Date**: When you're publishing the album
- **Event Date**: When the event/trip occurred
- **Backdate**: Set to historical date for past events

Choose the approach that makes sense for your content organization.

### Tags

- Use consistent tags across albums for better discoverability
- Common tag categories: location, event type, theme, year
- Examples: `travel`, `events`, `photography`, `family`, `2024`

### Location Data

Location is optional but recommended for:
- Travel albums
- Event documentation
- Location-based collections

Future enhancements may include map visualization for albums with location data.

### Image Organization

- Use consistent aspect ratios for cohesive galleries
- Provide descriptive alt text for accessibility
- Add captions to provide context
- Consider image loading order (first image used as preview)

## Integration Points

### Collections Landing Page

Albums are listed in the "Media Collections" section at `/collections/`

### Timeline

Album collections appear in the main timeline feed alongside posts, notes, and other content types.

### Search

Albums are fully searchable by:
- Title
- Description
- Tags
- Media captions and alt text

### RSS Feeds

- **Album-specific**: `/collections/albums/feed.xml`
- **Unified feed**: Includes all content types including albums

## Future Enhancements

Potential future features (not currently implemented):

- Map view integration for albums with location data
- Gallery navigation improvements
- Album-to-album linking
- Media metadata extraction (EXIF data)
- Automatic thumbnail generation
- Album statistics and analytics

## Related Documentation

- [Media Publishing Workflow](media-publishing-workflow.md) - Publishing individual media posts
- [Collections System](how-to-create-collections.md) - Overall collections architecture
- [Enhanced Content Discovery](enhanced-content-discovery-implementation.md) - Search functionality
