# F# Static Site Generator Architecture

- **Status**: Accepted
- **Date**: 2024 (documented 2026-02-01)
- **Decision-makers**: Luis Quintanilla
- **Consulted**: F# community practices

## Context and Problem Statement

The website needs a static site generator that provides type safety, excellent performance, and full control over content processing. Should we use an existing SSG or build a custom one?

## Decision Drivers

- Type safety for complex content processing
- Custom block support (media galleries, reviews, venues, RSVP)
- Full control over HTML output and RSS feeds
- Integration with F# ecosystem (Giraffe ViewEngine, Markdig)
- Build performance for 1,000+ content items
- Flexibility for future feature additions

## Considered Options

1. **Hugo with custom shortcodes**
2. **Astro with F# preprocessor**
3. **Custom F# static site generator**
4. **Fornax (F# SSG)**

## Decision Outcome

**Chosen option**: "Custom F# static site generator", because it provides maximum flexibility for custom content processing, integrates with proven F# libraries, and allows complete control over the build pipeline.

### Consequences

**Good:**
- Full type safety across content processing pipeline
- Custom Markdig blocks for specialized content (reviews, media, venues)
- Giraffe ViewEngine for type-safe HTML generation
- Complete control over RSS feed structure
- Easy integration with Azure Static Web Apps
- Build times under 2 seconds for 1,130+ items

**Bad:**
- No community of pre-built themes
- All features must be implemented from scratch
- Higher maintenance burden
- Requires F# expertise

### Confirmation

- Site successfully builds and deploys
- All content types render correctly
- RSS feeds validate against standards
- Performance meets targets (~1.3s builds)

## Implementation Details

### Core Stack

| Component | Technology |
|-----------|------------|
| Language | F# on .NET 10.0 |
| Markdown | Markdig with custom extensions |
| HTML | Giraffe ViewEngine |
| Build | dotnet run |
| Hosting | Azure Static Web Apps |
| API | Azure Functions |

### Module Architecture

```
PersonalSite.fsproj
├── Domain.fs           # Core types, ITaggable interface
├── ASTParsing.fs       # Markdown AST parsing
├── CustomBlocks.fs     # Markdig extensions
├── BlockRenderers.fs   # HTML rendering for custom blocks
├── GenericBuilder.fs   # Unified content processing
├── Builder.fs          # High-level orchestration
├── Program.fs          # Entry point
├── Views/              # Modular view components
│   ├── Layouts.fs
│   ├── LayoutViews.fs
│   ├── ContentViews.fs
│   ├── CollectionViews.fs
│   └── ...
└── Services/           # Shared services
    ├── Markdown.fs
    ├── Tag.fs
    └── ...
```

### Build Pipeline

1. Load content from `_src/` directories
2. Parse YAML frontmatter
3. Process Markdown with custom blocks
4. Extract tags via ITaggable interface
5. Generate HTML pages via ViewEngine
6. Generate RSS feeds per content type and tag
7. Output to `_public/` for deployment

## More Information

- Giraffe ViewEngine: https://github.com/giraffe-fsharp/Giraffe.ViewEngine
- Markdig: https://github.com/xoofx/markdig
- Azure Static Web Apps: https://azure.microsoft.com/services/app-service/static/
