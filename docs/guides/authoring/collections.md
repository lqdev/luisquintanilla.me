# Create a collection

A collection is a list of feeds or links. Its items live in a JSON file in `Data/` and its settings live in `Collections.fs`. The build writes a page, an OPML file, and an RSS feed for each collection.

## Quick Start

Creating a new collection requires only two steps:

1. **Create a JSON data file** in `Data/`
2. **Add collection configuration** to `Collections.fs`

The system automatically handles:
- HTML page generation with proper styling
- OPML file generation for RSS reader imports
- RSS feed generation for subscription
- Navigation menu integration
- Text-only accessibility versions

## Collection Data Format

### JSON Structure

Create a JSON file in `Data/` (at the repo root). It is an array of items, one per resource:

```json
[
  {
    "Title": "Resource Title",
    "Type": "rss",
    "HtmlUrl": "https://example.com",
    "XmlUrl": "https://example.com/feed.xml",
    "Added": "2024-01-15"
  }
]
```

`Collections.fs` reads it as a `CollectionItem array` (defined in `Domain.fs`). The collection's
title, description and URL come from its configuration in `Collections.fs`, not from the JSON.

### Item Fields

- **Title**: Display name for the resource
- **Type**: Usually "rss" for feeds
- **HtmlUrl**: Website URL
- **XmlUrl**: RSS feed URL (if available)
- **Added** (optional): Date the item was added
- **Description**, **Tags** (optional): Extra metadata; fields left out are read as empty

## Collection Configuration

### Add to Collections.fs

In the `Collections.fs` file, add your collection to the array returned by `getDefaultCollections`:

```fsharp
{
    Id = "my-collection"
    Title = "My Collection"
    Description = "Description of this collection"
    CollectionType = MediumFocused "my-medium"  // or TopicFocused "my-topic"
    UrlPath = "/collections/my-collection/"
    DataFile = "my-collection.json"
    Tags = [| "tag1"; "tag2"; "tag3" |]
    LastUpdated = DateTime.Now.ToString("yyyy-MM-dd")
    ItemCount = None
}
```

### Configuration Fields

- **Id**: Unique identifier (lowercase, hyphenated)
- **Title**: Display name
- **Description**: Brief description
- **CollectionType**: 
  - `MediumFocused "medium"` for content-type collections (blogs, podcasts, etc.)
  - `TopicFocused "topic"` for subject-focused collections
- **UrlPath**: URL where the collection will be accessible
- **DataFile**: Name of your JSON file in `Data/`
- **LastUpdated**: Date string shown on the page
- **ItemCount**: Leave as `None`; the build fills it in
- **Tags**: Array of relevant tags for discovery

## Navigation Integration

### Automatic Integration

Once configured, collections automatically appear in the site navigation under the "Collections" dropdown, organized by type:

- **Medium-focused collections** appear under the main collection items
- **Topic-focused collections** appear in appropriate sections

### Manual Navigation Updates

If you need custom navigation placement, edit `Views/Layouts.fs` in the collections dropdown section.

## File Outputs

The system automatically generates:

1. **HTML Page**: `/collections/{id}/index.html`
   - Responsive design with desert theme integration
   - Individual resource links with website and RSS feed URLs
   - Collection metadata and subscription information

2. **OPML File**: `/collections/{id}/index.opml`
   - RSS reader import format
   - Contains all feed URLs for bulk subscription
   - Proper XML structure with collection metadata

3. **RSS Feed**: `/collections/{id}/index.rss`
   - Standard RSS format for the collection itself
   - Useful for tracking collection updates

## Examples

### Blogroll Collection

**File**: `Data/blogroll.json`
```json
[
  {
    "Title": "Blogroll.org",
    "Type": "rss",
    "HtmlUrl": "https://blogroll.org/",
    "XmlUrl": "https://blogroll.org/feed/",
    "Added": "2022-08-14"
  }
]
```

**Configuration**:
```fsharp
{
    Id = "blogroll"
    Title = "Blogroll"
    Description = "Websites and blogs I follow regularly"
    CollectionType = MediumFocused "blogs"
    UrlPath = "/collections/blogroll/"
    DataFile = "blogroll.json"
    Tags = [| "blogs"; "reading"; "indieweb" |]
    LastUpdated = DateTime.Now.ToString("yyyy-MM-dd")
    ItemCount = None
}
```

### Topic-Focused Collection

**File**: `Data/ai-starter-pack.json`
```json
[
  {
    "Title": "Latent Space",
    "Type": "rss",
    "HtmlUrl": "https://www.latent.space/",
    "XmlUrl": "https://www.latent.space/feed/"
  }
]
```

**Configuration**:
```fsharp
{
    Id = "ai"
    Title = "AI Starter Pack"
    Description = "Everything you need to stay informed about AI developments"
    CollectionType = TopicFocused "ai"
    UrlPath = "/collections/starter-packs/ai/"
    DataFile = "ai-starter-pack.json"
    Tags = [| "ai"; "machine-learning"; "technology" |]
    LastUpdated = DateTime.Now.ToString("yyyy-MM-dd")
    ItemCount = None
}
```

## Building and Testing

### Build the Site

After creating your collection:

```bash
dotnet run
```

This automatically:
- Loads your JSON data
- Generates all output formats
- Updates navigation
- Creates text-only accessibility versions

### Verify Output

Check that your collection appears:

1. **Navigation**: Collections dropdown should include your new collection
2. **Page**: Visit `/collections/{your-collection}/` to see the generated page
3. **OPML**: Download `/collections/{your-collection}/index.opml` to test RSS reader import
4. **RSS**: Check `/collections/{your-collection}/index.rss` for feed validity

### Text-Only Version

Your collection automatically gets a text-only version at `/text/collections/{your-collection}/` optimized for:
- 2G networks
- Screen readers
- Flip phones
- Assistive technology

## Best Practices

### Data Quality

- **Verify RSS URLs**: Test that `xmlUrl` feeds are valid and active
- **Consistent Descriptions**: Keep descriptions concise but informative
- **Proper Categorization**: Choose `MediumFocused` vs `TopicFocused` appropriately

### URL Structure

- **Use hyphens**: Collection IDs should use hyphens, not underscores
- **Descriptive paths**: URLs should clearly indicate the collection purpose
- **Consistent naming**: Match collection ID, title, and file naming

### Maintenance

- **Regular updates**: Keep resource lists current
- **Broken link monitoring**: Periodically verify URLs are still valid
- **Description updates**: Keep collection descriptions accurate as they evolve

## Troubleshooting

### Collection Not Appearing

1. **Check JSON syntax**: Validate your JSON file format
2. **Verify configuration**: Ensure `Collections.fs` entry is correct
3. **Build errors**: Look for compilation errors in `dotnet run` output
4. **File paths**: Confirm `DataFile` matches your actual JSON filename

### Navigation Issues

1. **Cache**: Hard refresh your browser
2. **Build**: Ensure `dotnet run` completed successfully
3. **Configuration**: Check `Collections.fs` for typos in navigation fields

### Missing Outputs

1. **Permissions**: Ensure write permissions to `_public/collections/`
2. **Directory creation**: The system should auto-create directories
3. **Build process**: Check for errors in the collections build phase

## Architecture Overview

The Composable Starter Pack System uses:

- **Collections.fs**: Central configuration and processing logic
- **Domain.fs**: Type definitions for type-safe collection handling
- **Builders/CollectionPages.fs**: Integration with main build process
- **Views/**: F# ViewEngine templates for HTML generation
- **JSON Data**: Human-readable, version-controllable collection data

This architecture provides:
- **Type safety**: F# compiler catches configuration errors
- **Consistency**: Unified processing for all collections
- **Maintainability**: Single source of truth for collection logic
- **Extensibility**: Easy to add new output formats or features

## Contributing

When adding collections:

1. **Follow naming conventions**: Use clear, descriptive names
2. **Test thoroughly**: Verify all output formats work correctly
3. **Document decisions**: Add comments for non-obvious choices
4. **Consider accessibility**: Ensure collections work in text-only mode

The system is designed to be self-documenting through code and configuration, making it easy for future maintainers to understand and extend.
