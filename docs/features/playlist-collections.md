# Playlist Collections

## Overview

Playlist Collections are curated music playlists organized by theme or time period (typically monthly "Crate Finds"). Each playlist provides YouTube and Spotify links for maximum accessibility, allowing listeners to enjoy music regardless of their preferred platform. The system generates individual pages for each playlist with proper formatting and links.

## Features

- **Monthly Music Discoveries**: Curated "Crate Finds" playlists featuring new discoveries and favorites
- **Dual Platform Links**: YouTube primary links with Spotify backups for accessibility
- **Track Metadata**: Includes artist, album, duration, and links for each track
- **Collection Index**: Organized listing of all playlists at `/collections/playlists/`
- **Tag System**: Playlists are fully integrated with the site's tag system
- **Markdown-Based**: Simple markdown format for easy playlist creation

## Content Structure

### Frontmatter Fields

```yaml
---
title: "Playlist Title"                     # Required: Playlist name
date: "2025-07-01 20:00 -05:00"            # Required: Publication date with timezone
tags: ["cratefinds", "music", "playlist"]   # Required: Array of tags
description: "Optional description"         # Optional: Brief playlist description
---
```

### Track List Format

Playlists use a standardized markdown format for tracks:

```markdown
## Tracks

1. **Track Title** by Artist Name
   - Album: *Album Name*
   - Duration: 4:42
   - [Listen on YouTube](https://youtube.com/watch?v=...)
   - [Backup: Listen on Spotify](https://open.spotify.com/track/...)

2. **Another Track** by Another Artist
   - Album: *Another Album*
   - Duration: 3:15
   - [Listen on YouTube](https://youtube.com/watch?v=...)
   - [Backup: Listen on Spotify](https://open.spotify.com/track/...)
```

## Creating a Playlist

### Using VS Code Snippets

1. Create a new file in `_src/playlists/playlist-name.md`
2. Type `playlist-collection` to trigger the snippet
3. Fill in the frontmatter fields
4. Add your track list

### Manual Creation

1. Copy the template structure
2. Customize the frontmatter
3. Add tracks with proper formatting
4. Save in `_src/playlists/`

### Example Playlist

```markdown
---
title: "Crate Finds - June 2025"
date: "2025-07-01 20:00 -05:00"
tags: ["cratefinds","music","playlist","spotify"]
---

Playlist from June 2025.

Some of my favorites:

- Claudia, Wilhelm R And Me by Roberto Musci
- Journey in Satchidananda by Alice Coltrane and Pharoah Sanders
- Little Sunflower by Dorothy Ashby

## Tracks

1. **Claudia, Wilhelm R And Me** by Roberto Musci
   - Album: *Tower of Silence*
   - Duration: 2:57
   - [Listen on YouTube](https://www.youtube.com/watch?v=g6-Wf79K_zE)
   - [Backup: Listen on Spotify](https://open.spotify.com/track/4jR8SmpJ76qsD1TlaD6TIY)

2. **Journey In Satchidananda** by Alice Coltrane, Pharoah Sanders
   - Album: *Journey in Satchidananda*
   - Duration: 6:36
   - [Listen on YouTube](https://www.youtube.com/watch?v=TQtEFdyhgdE)
   - [Backup: Listen on Spotify](https://open.spotify.com/track/2gG3ivmsfylVXLyIJvLXyN)

---

**Original Spotify Playlist:** [Listen on Spotify](https://open.spotify.com/playlist/...).
```

## Technical Implementation

### Domain Types

```fsharp
type PlaylistDetails = {
    Title: string
    Description: string option
    Date: string
    Tags: string array
}

type PlaylistCollection = {
    FileName: string
    Metadata: PlaylistDetails
    Content: string  // Raw markdown content with track lists
}
with
    interface ITaggable
```

### Processing Pipeline

1. **Load**: `loadPlaylistCollections` reads markdown files from `_src/playlists/`
2. **Parse**: YAML frontmatter parsed into `PlaylistDetails`
3. **Process**: Markdown content rendered to HTML
4. **Generate**: Individual pages created at `/collections/playlists/{slug}/`
5. **Index**: Collection index page created at `/collections/playlists/`

### Build Process

**Builder.fs Integration:**
```fsharp
let playlistCollectionFiles = 
    let playlistPath = Path.Join(srcDir, "playlists")
    if Directory.Exists(playlistPath) then
        Directory.GetFiles(playlistPath)

let feedData = GenericBuilder.buildContentWithFeeds processor playlistCollectionFiles

// Generate individual playlist pages
for item in feedData do
    let playlistCollection = item.Content
    let saveDir = Path.Join(outputDir, "collections", "playlists", playlistCollection.FileName)
    // ... generate page

// Generate playlist collections index
let playlistCollections = feedData |> Array.map (fun item -> item.Content)
let indexHtml = playlistCollectionsPageView playlistCollections
```

### View Functions

**Collection Index** (`playlistCollectionsPageView`):
```fsharp
let playlistCollectionsPageView (playlistCollections: PlaylistCollection array) = 
    div [ _class "d-grip gap-3" ] [
        h2[] [Text "Playlists"]
        p [] [Text "Monthly music discoveries and favorites..."]
        ul [] [
            for playlist in playlistCollections do
                li [] [
                    a [ _href $"/collections/playlists/{playlist.FileName}"] [ Text playlist.Metadata.Title ]
                    // Description and date...
                ]
        ]
    ]
```

**Individual Playlist** (`playlistCollectionDetailView`):
```fsharp
let playlistCollectionDetailView (playlistCollection: PlaylistCollection) (processedContent: string) =
    div [ _class "playlist-collection-detail" ] [
        h1 [] [ Text playlistCollection.Metadata.Title ]
        // Description (optional)
        // Date
        // Tags
        div [ _class "playlist-content mt-4" ] [
            rawText processedContent  // Rendered track list
        ]
    ]
```

## URL Structure

```
/collections/playlists/                      # Playlist index
/collections/playlists/crate-finds-june-2025/  # Individual playlist
```

## Use Cases

### Primary Use Case: Monthly Music Discovery

**"Crate Finds" Series:**
- Monthly playlists featuring discovered music
- Mix of new releases and classic finds
- Organized chronologically for easy browsing

**Benefits:**
- Platform-agnostic listening (YouTube + Spotify)
- Track metadata preserved (artist, album, duration)
- Easy sharing via permalink
- Integrated with site's tag system

### Alternative Use Cases

**Themed Playlists:**
- Genre-specific collections
- Mood-based playlists
- Artist deep dives
- Seasonal compilations

**Event Playlists:**
- Party mixes
- Study/work music
- Travel soundtracks

## Platform Accessibility

### YouTube as Primary Platform

**Advantages:**
- No account required for listening
- Available globally
- Embedded player optional
- No tracking by default

**Implementation:**
```markdown
[Listen on YouTube](https://www.youtube.com/watch?v=...)
```

### Spotify as Backup

**Advantages:**
- High audio quality
- Playlist preservation
- Mobile app integration
- Offline listening (with subscription)

**Implementation:**
```markdown
[Backup: Listen on Spotify](https://open.spotify.com/track/...)
```

### Privacy Considerations

**Why Not Embedded Players:**
1. **No Spotify embeds**: Avoids analytics/tracking concerns
2. **YouTube links only**: User controls playback environment
3. **User choice**: Listeners choose their preferred platform
4. **Minimal footprint**: No third-party JavaScript on pages

## Automation Tools

### Playlist Creator Script

A companion tool generates properly formatted playlists:

**GitHub Repository:** [playlist-creator](https://github.com/lqdev/playlist-creator)

**Features:**
- Spotify playlist → Markdown conversion
- Automatic YouTube link discovery
- Track metadata extraction
- Formatted output ready for publishing

**Workflow:**
1. Create Spotify playlist
2. Run playlist-creator script
3. Review/edit generated markdown
4. Add to `_src/playlists/`
5. Build site to publish

## Best Practices

### Track Selection

**Quality over Quantity:**
- 15-25 tracks per playlist (manageable length)
- Cohesive theme or mood
- Mix of familiar and discovery

**Metadata Accuracy:**
- Verify artist names (handle featured artists)
- Include full album names
- Accurate track durations

### Link Verification

**Before Publishing:**
- Test YouTube links (ensure videos available)
- Verify Spotify links (check track availability)
- Confirm both links point to correct track

**Link Maintenance:**
- YouTube links may break over time
- Spotify links generally more stable
- Consider periodic link validation

### Content Organization

**Naming Convention:**
- Use consistent slug format: `crate-finds-month-year`
- Descriptive titles: "Crate Finds - June 2025"
- Clear date in frontmatter

**Tags:**
- Always include: `["cratefinds", "music", "playlist"]`
- Add genre tags for discoverability
- Include platform tags: `["spotify", "youtube"]`

### Description Guidelines

**Frontmatter Description:**
- Brief overview (1-2 sentences)
- Highlight standout tracks
- Set expectations for playlist mood

**Markdown Content:**
- Optional introduction paragraph
- Highlight personal favorites
- Context for playlist curation

## Integration Points

### Collections System

Playlists appear in the Collections section alongside:
- Album Collections
- Starter Packs
- Travel Collections
- Blogroll/Podroll

**Collections Landing Page:** `/collections/`

### Timeline Integration

Playlists can be announced on the timeline:
```markdown
---
post_type: "note"
title: "New Playlist: Crate Finds - June 2025"
published_date: "2025-07-01 20:00 -05:00"
tags: ["music", "playlist"]
---

June's music discoveries are up! Features Alice Coltrane, Dorothy Ashby, and more.

[Listen to Crate Finds - June 2025](/collections/playlists/crate-finds-june-2025/)
```

### Tag System

Playlists fully participate in the tag system:
- `/tags/cratefinds/` - All Crate Finds playlists
- `/tags/music/` - All music content
- `/tags/playlist/` - All playlists

### Search Integration

Playlists are indexed for site-wide search:
- Searchable by title
- Searchable by tags
- Searchable by track names in content

## File Locations

**Source Content:**
- `_src/playlists/*.md` - Playlist markdown files

**Domain Types:**
- `Domain.fs` - `PlaylistDetails` and `PlaylistCollection` types

**Builder Functions:**
- `Builder.fs` - Playlist processing and page generation

**View Functions:**
- `Views/CollectionViews.fs` - `playlistCollectionsPageView` and `playlistCollectionDetailView`

**Generated Output:**
- `_public/collections/playlists/` - Playlist pages
- `_public/collections/playlists/index.html` - Index page

## Example Workflow

### Creating a New Playlist

**Step 1: Curate Music**
- Create Spotify playlist throughout the month
- Discover and add tracks
- Organize in preferred order

**Step 2: Generate Markdown**
```bash
# Using playlist-creator tool
playlist-creator --spotify-url "https://open.spotify.com/playlist/..." --output crate-finds-august-2025.md
```

**Step 3: Review and Edit**
- Check generated track list
- Verify YouTube links work
- Add introduction paragraph
- Highlight favorite tracks

**Step 4: Publish**
- Save to `_src/playlists/crate-finds-august-2025.md`
- Update frontmatter (title, date, tags)
- Build site: `dotnet run`
- Verify at `/collections/playlists/crate-finds-august-2025/`

**Step 5: Announce (Optional)**
- Create timeline note
- Share on social media
- Link from RSS feed

## Comparison with Album Collections

### Similarities
- Both are collection types
- Both use markdown format
- Both have index and detail pages
- Both integrate with tags and search

### Differences

| Feature | Playlists | Albums |
|---------|-----------|---------|
| **Content Type** | Curated music lists | Photo/media galleries |
| **Primary Links** | YouTube/Spotify | Image files |
| **Format** | Track lists with metadata | Media blocks with images |
| **Update Frequency** | Monthly | Event-based |
| **Location Metadata** | No | Yes (lat/lon) |
| **Timeline Integration** | Via announcements | Direct media posts |

## Future Enhancements

### Potential Additions

**Planned:**
- RSS feed for playlists (`/collections/playlists/feed.xml`)
- M3U playlist file generation
- Embedded YouTube player (optional)
- Audio preview snippets

**Under Consideration:**
- Spotify Web Playback SDK integration
- YouTube Music playlist links
- Apple Music link support
- Bandcamp integration for indie artists
- SoundCloud links for mixtapes

**Not Planned:**
- Automatic playlist syncing
- Real-time YouTube link validation
- Spotify embed (privacy concerns)
- Music playback on site

## Troubleshooting

### Broken YouTube Links

**Problem:** YouTube video removed or unavailable

**Solution:**
1. Search for alternative video
2. Update YouTube link in markdown
3. Rebuild site
4. Spotify link serves as backup

### Missing Track Metadata

**Problem:** Track missing artist, album, or duration

**Solution:**
1. Check Spotify metadata
2. Manually add missing information
3. Use playlist-creator tool's updated output

### Playlist Not Appearing

**Problem:** Playlist not showing on index page

**Solution:**
1. Verify file in `_src/playlists/` directory
2. Check YAML frontmatter syntax
3. Ensure required fields present (title, date, tags)
4. Rebuild site: `dotnet run`
5. Check build output for errors

### Formatting Issues

**Problem:** Track list not rendering correctly

**Solution:**
1. Verify markdown formatting
2. Check indentation (3 spaces for nested items)
3. Ensure proper list numbering
4. Test with simple track first

## Related Documentation

- [Album Collections](ALBUM_COLLECTIONS.md) - Photo/media collections
- [Collections System](how-to-create-collections.md) - Overall collections architecture
- [Content Creation](../README.md#content-creation) - VS Code snippets
- [Tag System](../README.md#key-features) - Tag-based organization

## Success Metrics

Current implementation:
- **16 playlists** published since 2023
- **Monthly cadence** maintained
- **Platform diversity**: YouTube + Spotify links
- **Privacy-friendly**: No embedded players or tracking
- **Searchable**: Full text search integration
- **Tag integration**: Organized via site-wide tag system

## Summary

Playlist Collections provide a platform-agnostic way to share music discoveries while respecting user privacy and choice. The dual-link system (YouTube + Spotify) ensures maximum accessibility, while the markdown-based format keeps content simple and maintainable. Integration with the site's collection system, tags, and search makes playlists easily discoverable alongside other content types.

**Key Benefits:**
- ✅ Platform-agnostic music sharing
- ✅ Privacy-friendly (no embedded players)
- ✅ Dual links for accessibility
- ✅ Monthly discovery cadence
- ✅ Full tag and search integration
- ✅ Simple markdown format
- ✅ Automation-friendly with playlist-creator tool
