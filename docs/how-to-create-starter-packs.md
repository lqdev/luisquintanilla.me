# How to Create Starter Packs

This comprehensive guide explains how to author and publish RSS/OPML starter packs using the Starter Pack System. Starter packs are curated collections of RSS feeds that help others discover and subscribe to content across the open web.

## What Are Starter Packs?

Starter packs are collections of RSS feeds organized around a specific topic or interest area. Inspired by [BlueSky's Starter Pack feature](https://bsky.social/about/blog/06-26-2024-starter-packs), they leverage open web standards like RSS and OPML to enable cross-platform content discovery.

### Benefits of RSS/OPML Starter Packs

- **Open Standards**: Work with any RSS reader, not locked to a specific platform
- **Cross-Platform**: Include feeds from blogs, podcasts, YouTube, forums, social platforms
- **Easy Sharing**: Share via OPML files for bulk subscription
- **Discoverable**: Integrated into site navigation for organic discovery
- **Sustainable**: Built on decentralized, open web technologies

## Quick Start

Creating a new starter pack requires two steps:

1. **Create a JSON data file** in `Data/` directory
2. **Add configuration** to `StarterPackSystem.fs` 

### Visual Workflow

```
1. Create JSON data file
   Data/my-topic-starter-pack.json
   ↓
2. Add F# configuration 
   StarterPackSystem.fs → loadStarterPackConfigs()
   ↓
3. Build site
   dotnet run
   ↓
4. Generated outputs:
   • HTML page: /collections/starter-packs/my-topic/index.html
   • OPML file: /collections/starter-packs/my-topic/index.opml  
   • RSS feed: /collections/starter-packs/my-topic/index.xml
   • Text-only: /text/collections/starter-packs/my-topic/index.html
```

The system automatically handles:
- HTML page generation with feed listings
- OPML file generation for RSS reader imports  
- RSS feed generation for the collection itself
- Navigation integration and accessibility versions

## Data Structure and Format

### JSON File Structure

Create a JSON file in the `Data/` directory following this structure:

```json
[
    {
        "Title": "Resource Name",
        "Type": "rss",
        "HtmlUrl": "https://example.com",
        "XmlUrl": "https://example.com/feed.xml"
    },
    {
        "Title": "Another Resource",
        "Type": "rss", 
        "HtmlUrl": "https://another-example.com",
        "XmlUrl": "https://another-example.com/rss"
    }
]
```

### Required Fields for Each Feed

- **Title**: Display name for the feed (used in listings and OPML)
- **Type**: Feed type (typically "rss" for RSS feeds)
- **HtmlUrl**: Website homepage URL
- **XmlUrl**: Direct RSS/Atom feed URL

### Data Quality Guidelines

**Verify Feed URLs**: Always test that `XmlUrl` feeds are active and valid before adding:
```bash
# Test feed validity
curl -I https://example.com/feed.xml
# Should return 200 OK with XML content type
```

**Consistent Naming**: Use clear, descriptive titles that match the source website's branding.

**URL Format**: Ensure URLs include proper protocol (`https://`) and are accessible.

## Configuration Options

### Method 1: StarterPackSystem.fs Configuration (Current System)

The current system uses F# configuration in `StarterPackSystem.fs`. To add a new starter pack, modify the `loadStarterPackConfigs` function:

```fsharp
// In StarterPackSystem.fs, add to the list in loadStarterPackConfigs()
{
    Metadata = {
        Id = "your-topic"
        Title = "Your Topic Starter Pack"
        Description = "A curated collection of resources about your topic"
        Category = Some "Technology"
        Tags = ["topic"; "category"; "keywords"]
        Author = Some "Your Name"
        Created = Some (DateTime(2024, 1, 1))
        Updated = None
        Featured = true
        SortOrder = Some 1
    }
    DataFile = "your-topic-starter-pack.json"
    OutputPath = "collections/starter-packs/your-topic"
    UrlPath = "/collections/starter-packs/your-topic"
    OpmlFilename = Some "index.opml"
}
```

### Method 2: Registry File Configuration (Future Enhancement)

The system supports loading from `Data/starter-packs-registry.json` if the file exists, but currently uses hardcoded configuration for simplicity.

#### Configuration Fields Explained

**Metadata Fields**:
- **Id**: Unique identifier (lowercase, hyphenated: "ai", "web-development")
- **Title**: Display name for the starter pack
- **Description**: Brief description explaining the pack's purpose and scope
- **Category**: Optional category for organization ("Technology", "Development", "Content")
- **Tags**: Array of relevant keywords for discovery and filtering
- **Author**: Pack creator's name (defaults to site owner)
- **Created**: Creation date in ISO format
- **Updated**: Last update date (null if never updated)
- **Featured**: Whether to highlight in featured collections
- **SortOrder**: Display order for featured collections (lower numbers first)

**File Configuration**:
- **DataFile**: Name of your JSON file in `Data/` directory
- **OutputPath**: Relative path where HTML page will be generated
- **UrlPath**: Public URL path for the starter pack
- **OpmlFilename**: Name of OPML file (defaults to "index.opml")

### Method 2: Inline Configuration

For quick starter packs or testing, you can use the `createStarterPackTemplate` function:

```fsharp
// In F# interactive or build script
let newPack = StarterPackSystem.createStarterPackTemplate 
    "example-topic" 
    "Example Topic Starter Pack" 
    "A collection of resources about example topics"
```

This generates a configuration template that you can customize and add to the registry.

## Content Creation Workflow

### Step 1: Research and Curation

**Identify Topic Focus**: Choose a specific theme or area of interest with enough quality RSS feeds available.

**Collect Feed URLs**: Gather RSS feeds from various sources:
- Personal blogs and websites
- Podcasts (most have RSS feeds)
- YouTube channels (use `/feeds/videos.xml?channel_id=CHANNEL_ID`)
- Forums with RSS support
- News sites and publications
- Social platform feeds (Mastodon, some Twitter alternatives)

**Quality Assessment**: Ensure feeds are:
- Active and regularly updated
- High-quality content relevant to your topic
- Reliable and unlikely to disappear
- Diverse in perspective and voice

### Step 2: Data File Creation

Create your JSON file in the `Data/` directory:

```bash
# Example: Data/web-development-starter-pack.json
touch Data/web-development-starter-pack.json
```

Structure your data with consistent formatting:

```json
[
    {
        "Title": "CSS-Tricks",
        "Type": "rss",
        "HtmlUrl": "https://css-tricks.com/",
        "XmlUrl": "https://css-tricks.com/feed/"
    },
    {
        "Title": "Smashing Magazine",
        "Type": "rss", 
        "HtmlUrl": "https://www.smashingmagazine.com/",
        "XmlUrl": "https://www.smashingmagazine.com/feed/"
    },
    {
        "Title": "A List Apart",
        "Type": "rss",
        "HtmlUrl": "https://alistapart.com/",
        "XmlUrl": "https://alistapart.com/main/feed/"
    }
]
```

### Step 3: Configuration Setup

Add your starter pack configuration to `StarterPackSystem.fs`:

```fsharp
// Add to the list in loadStarterPackConfigs() function
{
    Metadata = {
        Id = "web-development"
        Title = "Web Development Starter Pack"
        Description = "Essential resources for modern web developers including CSS, JavaScript, and design techniques"
        Category = Some "Development"
        Tags = ["web-development"; "css"; "javascript"; "frontend"; "design"]
        Author = Some "Luis Quintanilla"
        Created = Some (DateTime(2024, 1, 15))
        Updated = None
        Featured = true
        SortOrder = Some 2
    }
    DataFile = "web-development-starter-pack.json"
    OutputPath = "collections/starter-packs/web-development"
    UrlPath = "/collections/starter-packs/web-development"
    OpmlFilename = Some "index.opml"
}
```

## Publishing Workflow

### Build Process

The starter pack system integrates with the main site build process:

```bash
# Build the entire site including all starter packs
dotnet run
```

This automatically:

1. **Loads Configuration**: Reads registry and validates data files
2. **Generates HTML Pages**: Creates styled pages with feed listings
3. **Creates OPML Files**: Generates RSS reader import files
4. **Builds RSS Feeds**: Creates feeds for the collections themselves
5. **Updates Navigation**: Integrates starter packs into site menus
6. **Creates Accessibility Versions**: Generates text-only versions

### Output Files Generated

For each starter pack, the system creates:

**HTML Page**: `/collections/starter-packs/{id}/index.html`
- Styled page with feed listings
- Individual website and RSS links
- OPML download link
- Collection metadata and description

**OPML File**: `/collections/starter-packs/{id}/index.opml`
- XML format for RSS reader bulk import
- Contains all feed URLs with metadata
- Compatible with most RSS readers

**RSS Feed**: `/collections/starter-packs/{id}/index.rss`
- RSS feed of the collection itself
- Useful for tracking collection updates
- Supports feed aggregation workflows

**Text-Only Version**: `/text/collections/starter-packs/{id}/index.html`
- Accessibility-optimized version
- Works on 2G networks and assistive technology
- Maintains full functionality

### Validation and Testing

**Build Validation**: Ensure the build completes without errors:
```bash
dotnet run
# Look for: "✅ Built N starter packs successfully"
```

**URL Testing**: Verify your starter pack is accessible:
- Visit `/collections/starter-packs/{your-id}/` 
- Check that all feed links work
- Download and test OPML file in an RSS reader

**Feed Validation**: Test individual feeds:
```bash
# Validate OPML structure
xmllint --format /path/to/index.opml

# Test RSS reader import
# Import OPML file into your preferred RSS reader
```

## Examples and Use Cases

### Example 1: Technology-Focused Starter Pack

**AI and Machine Learning Starter Pack**

```json
[
    {
        "Title": "OpenAI Blog",
        "Type": "rss",
        "HtmlUrl": "https://openai.com/news/",
        "XmlUrl": "https://openai.com/news/rss.xml"
    },
    {
        "Title": "Google AI Blog", 
        "Type": "rss",
        "HtmlUrl": "https://blog.google/technology/ai/",
        "XmlUrl": "https://blog.google/technology/ai/rss/"
    },
    {
        "Title": "Latent Space Podcast",
        "Type": "rss",
        "HtmlUrl": "https://www.latent.space/",
        "XmlUrl": "https://www.latent.space/feed/"
    }
]
```

Configuration:
```fsharp
// Add to StarterPackSystem.fs loadStarterPackConfigs() function
{
    Metadata = {
        Id = "ai-ml"
        Title = "AI & Machine Learning Starter Pack"
        Description = "Stay current with AI developments through blogs, research, and expert commentary"
        Category = Some "Technology"
        Tags = ["ai"; "machine-learning"; "research"; "technology"]
        Author = Some "Luis Quintanilla"
        Created = Some (DateTime(2024, 1, 1))
        Updated = None
        Featured = true
        SortOrder = Some 1
    }
    DataFile = "ai-ml-starter-pack.json"
    OutputPath = "collections/starter-packs/ai-ml"
    UrlPath = "/collections/starter-packs/ai-ml"
    OpmlFilename = Some "index.opml"
}
```

### Example 2: Community-Focused Starter Pack

**IndieWeb Community Starter Pack**

```json
[
    {
        "Title": "IndieWeb News",
        "Type": "rss",
        "HtmlUrl": "https://news.indieweb.org/",
        "XmlUrl": "https://news.indieweb.org/feed"
    },
    {
        "Title": "Tantek Çelik",
        "Type": "rss", 
        "HtmlUrl": "https://tantek.com/",
        "XmlUrl": "https://tantek.com/updates.atom"
    },
    {
        "Title": "Aaron Parecki",
        "Type": "rss",
        "HtmlUrl": "https://aaronparecki.com/",
        "XmlUrl": "https://aaronparecki.com/feed.xml"
    }
]
```

### Example 3: Mixed Media Starter Pack

**Creative Technology Starter Pack**

```json
[
    {
        "Title": "Creative Coding Podcast",
        "Type": "rss",
        "HtmlUrl": "https://creativecodingpodcast.com/",
        "XmlUrl": "https://creativecodingpodcast.com/feed.xml"
    },
    {
        "Title": "Processing Foundation Blog",
        "Type": "rss",
        "HtmlUrl": "https://processingfoundation.org/",
        "XmlUrl": "https://processingfoundation.org/feed.xml"
    },
    {
        "Title": "Daniel Shiffman (YouTube)",
        "Type": "rss",
        "HtmlUrl": "https://www.youtube.com/c/TheCodingTrain",
        "XmlUrl": "https://www.youtube.com/feeds/videos.xml?channel_id=UCvjgXvBlbQiydffZU7m1_aw"
    }
]
```

## Best Practices

### Curation Guidelines

**Quality Over Quantity**: 8-15 high-quality feeds are better than 30+ mediocre ones.

**Diverse Perspectives**: Include different viewpoints, backgrounds, and approaches to your topic.

**Update Frequency**: Choose feeds that publish regularly but not overwhelmingly (avoid hourly news feeds unless specifically needed).

**Longevity**: Prioritize established sources likely to remain active long-term.

### Organization Strategies

**Thematic Coherence**: Ensure all feeds relate clearly to your stated topic and description.

**Skill Level Mix**: Include resources for beginners, intermediates, and advanced practitioners.

**Content Type Variety**: Mix blogs, podcasts, news sites, and documentation where appropriate.

**Geographic Diversity**: Include international perspectives when relevant to your topic.

### Maintenance Practices

**Regular Review**: Check your starter packs quarterly for:
- Broken or inactive feeds
- New important sources to add
- Feeds that have changed focus or quality

**Version Control**: Update the `Updated` timestamp when making significant changes.

**Documentation**: Keep notes about why specific feeds were included or removed.

**Community Feedback**: Monitor how people use your starter packs and adjust accordingly.

## Technical Details

### File Locations and Structure

```
├── Data/
│   ├── your-topic-starter-pack.json     # Feed data
│   └── starter-packs-registry.json      # Configuration registry
├── _public/collections/starter-packs/
│   └── your-topic/
│       ├── index.html                   # Generated HTML page
│       ├── index.opml                   # OPML file for RSS readers
│       └── index.xml                    # RSS feed (compatibility)
└── text/collections/starter-packs/
    └── your-topic/
        └── index.html                   # Text-only accessibility version
```

### Build Integration

The starter pack system integrates with the main build pipeline through:

1. **StarterPackSystem.fs**: Core processing logic and type definitions
2. **Program.fs**: Build orchestration and integration point
3. **Views/**: HTML generation using F# ViewEngine
4. **Services/Opml.fs**: OPML generation utilities

### Type Definitions

The system uses F# types for compile-time safety:

```fsharp
type StarterPackMetadata = {
    Id: string
    Title: string
    Description: string
    Category: string option
    Tags: string list
    Author: string option
    Created: DateTime option
    Updated: DateTime option
    Featured: bool
    SortOrder: int option
}

type Outline = {
    Title: string
    Type: string
    HtmlUrl: string
    XmlUrl: string
}
```

## Troubleshooting

### Common Issues and Solutions

#### Starter Pack Not Appearing

**Problem**: Starter pack doesn't show up in navigation or on site

**Solutions**:
1. **Check JSON Syntax**: Validate your JSON file format using a JSON validator
2. **Verify Configuration**: Ensure F# configuration in StarterPackSystem.fs is correct
3. **Build Errors**: Look for compilation errors in `dotnet run` output
4. **File Paths**: Confirm `DataFile` field matches your actual JSON filename
5. **F# Syntax**: Validate that your configuration follows proper F# syntax

#### OPML Import Issues

**Problem**: OPML file doesn't work with RSS readers

**Solutions**:
1. **XML Validation**: Check OPML file structure with `xmllint --format file.opml`
2. **Feed URL Testing**: Verify all `XmlUrl` feeds are accessible
3. **RSS Reader Compatibility**: Test with multiple RSS readers (some have different OPML support)
4. **Encoding Issues**: Ensure all feed titles use proper UTF-8 encoding

#### Build Failures

**Problem**: Site build fails when adding starter pack

**Solutions**:
1. **F# Compilation**: Check for syntax errors in StarterPackSystem.fs configuration
2. **Data Validation**: Ensure all required fields are present in F# configuration
3. **File Permissions**: Verify write permissions to `_public/` directory
4. **Dependency Issues**: Run `dotnet restore` if needed

#### Navigation Problems

**Problem**: Starter pack appears but navigation is broken

**Solutions**:
1. **URL Path Conflicts**: Ensure `UrlPath` doesn't conflict with existing pages
2. **Cache Issues**: Hard refresh browser or clear cache
3. **Build Completion**: Ensure `dotnet run` completed successfully without errors
4. **Configuration Typos**: Double-check navigation field spelling in registry

### Debugging Techniques

**Verbose Build Output**: Enable detailed logging to see exactly what's happening:
```bash
dotnet run --verbosity detailed
```

**JSON Validation**: Use online validators or command-line tools:
```bash
# Validate JSON syntax
python -m json.tool your-file.json

# Or use jq
jq . your-file.json
```

**Feed Testing**: Verify individual feeds work:
```bash
# Test feed accessibility
curl -I https://example.com/feed.xml
curl -s https://example.com/feed.xml | head -20
```

**OPML Testing**: Validate generated OPML files:
```bash
# Check OPML structure
xmllint --noout --valid index.opml

# Or just format check
xmllint --format index.opml
```

## Integration with IndieWeb

### Webmention Support

Starter packs support IndieWeb discovery patterns:
- Each pack page includes proper microformats markup
- OPML files can be referenced in webmentions
- Pack updates can trigger webmention notifications

### RSS Feed Integration

The starter pack RSS feeds integrate with:
- Feed readers and aggregators
- IndieWeb feed discovery protocols
- Cross-site feed syndication networks

### Decentralized Discovery

Starter packs promote decentralized content discovery by:
- Enabling cross-platform feed sharing
- Supporting RSS/OPML standards
- Facilitating community-driven curation
- Reducing dependence on algorithmic recommendations

## Advanced Usage

### Programmatic Creation

For bulk starter pack creation, use the F# API:

```fsharp
// Create multiple starter packs from data
let createBulkStarterPacks topics =
    topics
    |> List.map (fun (id, title, description, feeds) ->
        let config = StarterPackSystem.createStarterPackTemplate id title description
        let pack = { Config = config; Links = feeds }
        StarterPackSystem.buildStarterPack "_public" pack
    )
```

### Custom Output Formats

Extend the system to generate additional formats:

```fsharp
// Add JSON feed support
let generateJsonFeed (pack: StarterPack) =
    let jsonFeed = // JSON Feed format implementation
    File.WriteAllText(outputPath + "feed.json", jsonFeed)
```

### Analytics Integration

Track starter pack usage with analytics:
- Monitor OPML download rates
- Track individual feed click-through rates
- Measure pack discovery patterns
- Analyze user engagement metrics

## Community and Sharing

### Distribution Strategies

**Social Media**: Share starter packs on relevant platforms with hashtags like #RSS #OPML #IndieWeb

**Community Forums**: Post in forums and communities related to your topic area

**Blog Posts**: Write about your curation process and featured sources

**Cross-Linking**: Reference other quality starter packs and encourage reciprocal linking

### Collaboration Approaches

**Open Source**: Consider making your curation process transparent with public repositories

**Community Input**: Invite suggestions and submissions from topic area experts

**Regular Updates**: Maintain active engagement with your starter pack's community

**Documentation**: Share your curation criteria and update processes

## Conclusion

RSS/OPML starter packs provide a powerful way to share curated content collections using open web standards. By following this guide, you can create, publish, and maintain high-quality starter packs that help others discover valuable content across the decentralized web.

The system's integration with modern web technologies while maintaining compatibility with open standards ensures that your starter packs will remain useful and accessible regardless of platform changes or technological shifts.

Remember that successful starter packs focus on quality curation, regular maintenance, and clear communication of their purpose and scope. By building on RSS and OPML standards, you're contributing to a more open, interoperable web where content discovery isn't controlled by algorithmic black boxes but by thoughtful human curation.

### Resources and Further Reading

- [RSS 2.0 Specification](https://www.rssboard.org/rss-specification)
- [OPML 2.0 Specification](http://opml.org/spec2.opml)
- [IndieWeb Getting Started](https://indieweb.org/Getting_Started)
- [BlueSky Starter Packs](https://bsky.social/about/blog/06-26-2024-starter-packs)
- [RSS Feed Discovery](https://indieweb.org/feed_discovery)
- [Feedland RSS Community](https://feedland.org/)

### Contributing to This Guide

This guide is part of an open source project. To contribute improvements, corrections, or additional examples:

1. Visit the project repository
2. Submit issues for problems or suggestions
3. Create pull requests for direct improvements
4. Share your starter pack examples for inclusion

The goal is to make starter pack creation accessible to anyone interested in promoting open web content discovery and community-driven curation.