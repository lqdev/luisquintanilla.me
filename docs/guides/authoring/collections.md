# How to Create Collections

This guide explains how to create new collections in the Composable Starter Pack System. Collections are JSON-driven, automatically generate multiple output formats, and integrate seamlessly into the site navigation.

## Quick Start

Creating a new collection requires only two steps:

1. **Create a JSON data file** in `_src/Data/` 
2. **Add collection configuration** to `Collections.fs`

The system automatically handles:
- HTML page generation with proper styling
- OPML file generation for RSS reader imports
- RSS feed generation for subscription
- Navigation menu integration
- Text-only accessibility versions

## Collection Data Format

### JSON Structure

Create a JSON file in `_src/Data/` following this structure:

```json
{
  "title": "My Collection",
  "htmlUrl": "https://example.com",
  "description": "Description of this collection",
  "language": "en-us",
  "outlines": [
    {
      "text": "Resource Name",
      "title": "Resource Title", 
      "type": "rss",
      "htmlUrl": "https://example.com",
      "xmlUrl": "https://example.com/feed.xml"
    }
  ]
}
```

### Required Fields

- **title**: Collection name (displayed in navigation and page titles)
- **htmlUrl**: Homepage URL for the collection
- **description**: Brief description for the collection page
- **language**: Language code (typically "en-us")
- **outlines**: Array of resources in the collection

### Resource Fields

Each resource in the `outlines` array should include:

- **text**: Display name for the resource
- **title**: Full title of the resource
- **type**: Usually "rss" for feeds
- **htmlUrl**: Website URL
- **xmlUrl**: RSS feed URL (if available)

## Collection Configuration

### Add to Collections.fs

In the `Collections.fs` file, add your collection to the `allCollections` list:

```fsharp
{
    Id = "my-collection"
    Title = "My Collection"
    Description = "Description matching your JSON file"
    CollectionType = MediumFocused "my-medium"  // or TopicFocused "my-topic"
    UrlPath = "/collections/my-collection/"
    DataFile = "my-collection.json"
    Tags = [| "tag1"; "tag2"; "tag3" |]
}
```

### Configuration Fields

- **Id**: Unique identifier (lowercase, hyphenated)
- **Title**: Display name (matches JSON title)
- **Description**: Brief description
- **CollectionType**: 
  - `MediumFocused "medium"` for content-type collections (blogs, podcasts, etc.)
  - `TopicFocused "topic"` for subject-focused collections
- **UrlPath**: URL where the collection will be accessible
- **DataFile**: Name of your JSON file in `_src/Data/`
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

**File**: `_src/Data/blogroll.json`
```json
{
  "title": "Blogroll",
  "htmlUrl": "https://www.luisquintanilla.me/collections/blogroll/",
  "description": "Websites and blogs I follow regularly",
  "language": "en-us",
  "outlines": [
    {
      "text": "Julia Evans",
      "title": "Julia Evans",
      "type": "rss",
      "htmlUrl": "https://jvns.ca/",
      "xmlUrl": "https://jvns.ca/atom.xml"
    }
  ]
}
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
    Tags = [| "blogs"; "websites"; "reading" |]
}
```

### Topic-Focused Collection

**File**: `_src/Data/ai-tools.json`
```json
{
  "title": "AI Tools Starter Pack",
  "htmlUrl": "https://www.luisquintanilla.me/collections/ai-tools/",
  "description": "Essential AI tools and resources for developers",
  "language": "en-us",
  "outlines": [
    {
      "text": "Hugging Face",
      "title": "Hugging Face - The AI community building the future",
      "type": "rss",
      "htmlUrl": "https://huggingface.co/",
      "xmlUrl": "https://huggingface.co/blog/feed.xml"
    }
  ]
}
```

**Configuration**:
```fsharp
{
    Id = "ai-tools"
    Title = "AI Tools Starter Pack"
    Description = "Essential AI tools and resources for developers"
    CollectionType = TopicFocused "artificial-intelligence"
    UrlPath = "/collections/ai-tools/"
    DataFile = "ai-tools.json"
    Tags = [| "ai"; "tools"; "development"; "machine-learning" |]
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
- **Builder.fs**: Integration with main build process
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
