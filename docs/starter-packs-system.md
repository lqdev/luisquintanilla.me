# Starter Packs System

## Overview

The Starter Packs System provides curated collections of RSS feeds organized by topic, making it easy for others to discover and subscribe to quality content across the open web. Inspired by BlueSky's Starter Pack feature, this implementation uses open standards (RSS and OPML) that work with any RSS reader.

## Features

- **Open Standards**: Based on RSS 2.0 and OPML for universal compatibility
- **Cross-Platform**: Works with any RSS reader (Feedly, Inoreader, NewsBlur, etc.)
- **Bulk Subscription**: OPML files enable one-click subscription to entire collections
- **Discoverable**: Integrated into site navigation at `/collections/starter-packs/`
- **Flexible**: Can include feeds from blogs, podcasts, YouTube, forums, social platforms
- **Automated Generation**: F# build system generates HTML pages, OPML files, and RSS feeds

## Architecture

### File Structure

```
Data/
  ai-starter-pack.json          # Data file with feed URLs
  [topic]-starter-pack.json     # Additional starter packs

_public/collections/starter-packs/
  index.html                     # Starter packs index page
  ai/
    index.html                   # Individual starter pack page
    index.opml                   # OPML file for bulk subscription
    index.xml                    # RSS feed for the collection
```

### Generated Outputs

Each starter pack generates three files:
1. **HTML Page** - Human-readable feed listing with descriptions
2. **OPML File** - Machine-readable feed list for RSS readers
3. **RSS Feed** - Subscribe to updates about the collection itself

## Creating a Starter Pack

### Step 1: Create Data File

Create a JSON file in the `Data/` directory with your feed collection:

```json
[
    {
        "Title": "Blog Name",
        "Type": "rss",
        "HtmlUrl": "https://example.com",
        "XmlUrl": "https://example.com/feed.xml"
    },
    {
        "Title": "Another Blog",
        "Type": "rss",
        "HtmlUrl": "https://another.com",
        "XmlUrl": "https://another.com/rss"
    }
]
```

**Naming Convention**: Use format `[topic]-starter-pack.json` (e.g., `ai-starter-pack.json`, `fsharp-starter-pack.json`)

### Step 2: Verify Feed URLs

Always verify that feeds are active before adding:

```bash
# Test feed availability
curl -I https://example.com/feed.xml

# Should return: 200 OK with content-type: application/xml
```

### Step 3: Add Configuration

Add your starter pack to `StarterPackSystem.fs` in the `loadStarterPackConfigs` function:

```fsharp
{
    Id = "my-topic"
    Title = "My Topic Starter Pack"
    Description = "Curated feeds about my topic"
    DataFile = "Data/my-topic-starter-pack.json"
    IconEmoji = "🎯"  // Optional: emoji for visual identification
}
```

### Step 4: Build the Site

Run the build command to generate all starter pack files:

```bash
dotnet run
```

The system will:
- Generate HTML page at `/collections/starter-packs/my-topic/`
- Create OPML file at `/collections/starter-packs/my-topic/index.opml`
- Generate RSS feed at `/collections/starter-packs/my-topic/index.xml`
- Update the main starter packs index page

## Data File Format

### Required Fields

- **Title**: Display name for the feed (used in HTML and OPML)
- **Type**: Feed type (typically "rss" for RSS/Atom feeds)
- **HtmlUrl**: Website homepage URL
- **XmlUrl**: Direct feed URL (RSS/Atom/JSON feed)

### Optional Fields

While the basic structure only requires the four fields above, you can add custom metadata for future enhancements:

```json
{
    "Title": "Advanced Example",
    "Type": "rss",
    "HtmlUrl": "https://example.com",
    "XmlUrl": "https://example.com/feed.xml",
    "Description": "Optional feed description",
    "Language": "en",
    "Category": "Technology"
}
```

## Configuration Options

### StarterPackConfig Type

```fsharp
type StarterPackConfig = {
    Id: string              // URL-friendly identifier (no spaces)
    Title: string           // Display title
    Description: string     // Brief description of the collection
    DataFile: string        // Path to JSON data file
    IconEmoji: string option // Optional emoji for visual identification
}
```

### Best Practices

**ID Selection:**
- Use lowercase, hyphenated format
- Make it descriptive and memorable
- Examples: "ai", "fsharp", "indieweb", "photography"

**Title Format:**
- Clear and concise
- Include "Starter Pack" for consistency
- Examples: "AI Starter Pack", "F# Development Starter Pack"

**Description:**
- One-sentence explanation of the collection
- Mention the topic and type of content
- Examples: "Essential AI research blogs and papers", "F# development resources and community blogs"

## Using Starter Packs

### For End Users

#### Subscribe via OPML (Recommended)

1. Download the OPML file: `/collections/starter-packs/[topic]/index.opml`
2. Import into your RSS reader:
   - **Feedly**: Settings → Import OPML
   - **Inoreader**: Settings → Import/Export → Import from OPML
   - **NewsBlur**: Settings → Import → Choose OPML file
   - **NetNewsWire**: File → Import Subscriptions

#### Subscribe Individually

Visit the HTML page and subscribe to feeds one by one using the provided RSS links.

### For Developers

#### Access the RSS Feed

Subscribe to the starter pack's RSS feed to get updates when the collection changes:

```
https://www.lqdev.me/collections/starter-packs/[topic]/index.xml
```

## Examples

### Example 1: AI Starter Pack

**Data File** (`Data/ai-starter-pack.json`):
```json
[
    {
        "Title": "OpenAI Blog",
        "Type": "rss",
        "HtmlUrl": "https://openai.com/blog",
        "XmlUrl": "https://openai.com/blog/rss.xml"
    },
    {
        "Title": "Anthropic News",
        "Type": "rss",
        "HtmlUrl": "https://www.anthropic.com/news",
        "XmlUrl": "https://www.anthropic.com/news/rss"
    }
]
```

**Configuration** (`StarterPackSystem.fs`):
```fsharp
{
    Id = "ai"
    Title = "AI Starter Pack"
    Description = "Essential AI research blogs, papers, and industry news"
    DataFile = "Data/ai-starter-pack.json"
    IconEmoji = Some "🤖"
}
```

### Example 2: IndieWeb Starter Pack

**Data File** (`Data/indieweb-starter-pack.json`):
```json
[
    {
        "Title": "IndieWeb News",
        "Type": "rss",
        "HtmlUrl": "https://news.indieweb.org",
        "XmlUrl": "https://news.indieweb.org/feed"
    },
    {
        "Title": "Tantek Çelik",
        "Type": "rss",
        "HtmlUrl": "https://tantek.com",
        "XmlUrl": "https://tantek.com/feed"
    }
]
```

**Configuration**:
```fsharp
{
    Id = "indieweb"
    Title = "IndieWeb Starter Pack"
    Description = "Blogs and resources from the IndieWeb community"
    DataFile = "Data/indieweb-starter-pack.json"
    IconEmoji = Some "🌐"
}
```

## Integration Points

### Collections System

Starter packs are part of the broader collections system at `/collections/`. They appear alongside:
- Blogroll
- Podroll
- Travel Collections
- Album Collections

### Navigation

Starter packs are accessible via:
- Main collections index: `/collections/`
- Direct URL: `/collections/starter-packs/`
- Individual pack: `/collections/starter-packs/[id]/`

### Text-Only Site

All starter packs have text-only versions at:
- `/text/collections/starter-packs/`
- Optimized for 2G networks and accessibility

## Technical Implementation

### Build Process

1. **Load Configurations**: `StarterPackSystem.loadStarterPackConfigs()` loads all pack definitions
2. **Read Data Files**: JSON files parsed into `FeedItem` records
3. **Generate HTML**: `Views/CollectionViews.fs` creates human-readable pages
4. **Generate OPML**: Standard OPML 2.0 format with all feeds
5. **Generate RSS**: RSS 2.0 feed for the collection itself
6. **Create Index**: Main listing page with all starter packs

### Key Functions

**StarterPackSystem.fs:**
- `loadStarterPackConfigs()` - Returns list of all starter pack configurations
- `loadStarterPackData()` - Reads JSON and parses into feed items

**Collections.fs:**
- `buildStarterPacks()` - Orchestrates generation of all starter pack files
- Integration with main build pipeline

**Views/CollectionViews.fs:**
- `generateStarterPackPage()` - HTML page generation
- `generateStarterPackOPML()` - OPML file generation
- `generateStarterPackRSS()` - RSS feed generation

## Comparison with BlueSky Starter Packs

### Similarities
- Curated topic-based collections
- Easy discovery and bulk subscription
- Shareable via simple links

### Advantages of RSS/OPML Approach
- **Open Standards**: Work with any RSS reader, not platform-locked
- **Cross-Platform**: Include content from anywhere with an RSS feed
- **Decentralized**: No single platform controls your subscriptions
- **Mature Ecosystem**: Leverage decades of RSS reader development
- **Universal Compatibility**: Podcasts, blogs, YouTube, forums, everything with RSS

### Use Cases
- **Content Discovery**: Help others find quality feeds on specific topics
- **Onboarding**: Quick setup for new community members
- **Knowledge Sharing**: Share your reading list with colleagues
- **Niche Communities**: Curate feeds for specialized interests

## Maintenance

### Updating a Starter Pack

1. Edit the JSON data file
2. Add, remove, or modify feed entries
3. Rebuild the site: `dotnet run`
4. All files automatically regenerate

### Removing a Starter Pack

1. Delete the JSON data file
2. Remove configuration from `StarterPackSystem.fs`
3. Rebuild the site

### Feed Validation

Regularly verify feed URLs are still active:

```bash
# Check all feeds in a starter pack
cat Data/my-starter-pack.json | jq '.[].XmlUrl' | xargs -I {} curl -I {}
```

## Future Enhancements

Potential future features:
- Automatic feed validation during build
- Feed health monitoring and dead link detection
- Category-based organization within starter packs
- Social sharing previews (Open Graph, Twitter Cards)
- Analytics for popular starter packs
- User-submitted starter packs via GitHub Issues

## Related Documentation

- [How to Create Collections](how-to-create-collections.md) - General collections system
- [Feed Architecture](feed-architecture.md) - RSS feed generation patterns
- [OPML Support](../Services/Opml.fs) - OPML generation implementation

## Resources

- [OPML 2.0 Specification](http://opml.org/spec2.opml)
- [RSS 2.0 Specification](https://www.rssboard.org/rss-specification)
- [IndieWeb Feed Discovery](https://indieweb.org/feed_discovery)
