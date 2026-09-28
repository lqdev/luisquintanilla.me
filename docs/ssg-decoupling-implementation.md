# SSG.Core Decoupling Implementation

## Overview

This document outlines the implementation of SSG.Core, a decoupled static site generator that separates the core framework from site-specific content and configuration.

## Architecture

### Current State (Coupled)
```
PersonalSite.exe
├── Domain.fs (site-specific types)
├── Program.fs (hardcoded build logic)
├── Builder.fs (mixed site/framework code)
├── GenericBuilder.fs (reusable core)
└── _src/ (content tightly coupled)
```

### Target State (Decoupled)
```
SSG.Core.dll (Reusable Framework)
├── Configuration.fs (JSON config loading)
├── PluginRegistry.fs (plugin discovery)
├── Builder.fs (config-driven build)
└── Program.fs (CLI entry point)

Site Project
├── site-config.json (declarative config)
├── _src/ (content repository)
├── Data/ (data files)
└── plugins/ (custom plugins)
```

## Key Components

### 1. Configuration System (`SSG.Core.Configuration.fs`)

**Purpose**: JSON-driven site configuration replacing hardcoded Program.fs logic

**Core Types**:
- `SiteConfiguration`: Complete site definition  
- `ContentTypeConfig`: Content type processors and settings
- `FeatureConfig`: Toggleable features (RSS, search, tags)
- `PluginConfig`: Plugin discovery and registration

**Usage**:
```fsharp
// Load configuration
let config = loadSiteConfiguration "site-config.json"

// Check feature status
let rssEnabled = isFeatureEnabled config "rss"

// Get enabled content types  
let contentTypes = getEnabledContentTypes config
```

### 2. Plugin Registry (`SSG.Core.PluginRegistry.fs`)

**Purpose**: Plugin discovery and registration system enabling extensibility

**Plugin Interfaces**:
- `IContentProcessor<'T>`: Process content types (posts, notes, etc.)
- `ICustomBlockProcessor`: Handle custom Markdown blocks  
- `IViewPlugin`: Custom layout rendering
- `IBuildPlugin`: Build pipeline hooks

**Usage**:
```fsharp
// Register plugins
let registry = PluginRegistry()
registry.RegisterContentProcessor(BlogPostProcessor())
registry.RegisterCustomBlockProcessor(QuoteBlockProcessor())

// Use registered plugins
let processor = registry.GetContentProcessor<BlogPost>("BlogPostProcessor")
```

### 3. Configuration-Driven Builder (`SSG.Core.Builder.fs`)

**Purpose**: Build orchestration driven by configuration instead of hardcoded logic

**Features**:
- Configuration-based feature toggling
- Plugin-based content processing
- Extensible build pipeline with pre/post hooks
- Comprehensive error handling and reporting

**Usage**:
```fsharp
// Build from configuration
let result = buildFromConfiguration "site-config.json" (Some "./plugins")

// Check build status
if result.Success then
    printfn "Built %d items in %O" result.ItemsProcessed result.Duration
```

### 4. Example Plugins (`SSG.Core.ExamplePlugins.fs`)

**Purpose**: Demonstrates plugin development patterns and provides built-in functionality

**Included Plugins**:
- `BlogPostProcessor`: Full-featured blog post processing
- `NoteProcessor`: Microblog note handling
- `QuoteBlockProcessor`: Custom quote block rendering
- `ExampleViewPlugin`: Layout customization
- `ExampleBuildPlugin`: Build pipeline hooks

## Configuration File Structure

### Complete Example (`site-config.json`):
```json
{
  "site": {
    "title": "My Site",
    "description": "A decoupled static site",
    "baseUrl": "https://example.com",
    "author": {
      "name": "Site Author",
      "email": "author@example.com", 
      "url": "https://example.com"
    }
  },
  "directories": {
    "source": "_src",
    "output": "_public", 
    "data": "Data",
    "assets": "_src/assets"
  },
  "contentTypes": [
    {
      "name": "posts",
      "sourceDirectory": "posts",
      "processor": "BlogPostProcessor",
      "enabled": true,
      "urlPattern": "/posts/{slug}/",
      "feedEnabled": true,
      "archiveEnabled": true
    },
    {
      "name": "notes", 
      "sourceDirectory": "notes",
      "processor": "NoteProcessor",
      "enabled": true,
      "urlPattern": "/notes/{slug}/",
      "feedEnabled": true,
      "archiveEnabled": false
    }
  ],
  "plugins": {
    "customBlocks": [
      {
        "name": "quote",
        "enabled": true,
        "parser": "QuoteBlockProcessor",
        "renderer": "QuoteBlockHtmlRenderer"
      }
    ],
    "markdownExtensions": [
      "UsePipeTables",
      "UseGenericAttributes", 
      "UseAutoLinks"
    ]
  },
  "features": {
    "rss": {
      "enabled": true,
      "unified": true,
      "tagFeeds": true,
      "itemLimit": 20
    },
    "tags": {
      "enabled": true,
      "generatePages": true,
      "cloudEnabled": true
    },
    "search": {
      "enabled": true,
      "generateIndex": true,
      "includeContent": true
    },
    "timeline": {
      "enabled": true,
      "progressiveLoading": true,
      "initialItems": 50,
      "chunkSize": 25
    },
    "collections": {
      "enabled": true,
      "starterPacks": true
    }
  },
  "theme": {
    "name": "desert",
    "cssFramework": "bootstrap", 
    "viewEngine": "giraffe",
    "customCss": ["desert-theme.css"],
    "customJs": ["timeline.js", "search.js"]
  },
  "build": {
    "cleanOutput": true,
    "copyStaticFiles": true,
    "enableValidation": true,
    "parallel": false
  }
}
```

## Usage Scenarios

### 1. Configuration-Only Customization

Add new content types without writing code:

```json
{
  "contentTypes": [
    {
      "name": "tutorials",
      "sourceDirectory": "tutorials", 
      "processor": "BlogPostProcessor",
      "enabled": true,
      "urlPattern": "/tutorials/{slug}/",
      "feedEnabled": true,
      "archiveEnabled": true
    }
  ]
}
```

### 2. Plugin-Based Extension

Create custom content processor:

```fsharp
type TutorialProcessor() =
    interface IContentProcessor<Tutorial> with
        member _.Name = "TutorialProcessor"
        member _.Parse(filePath) = (* custom parsing *)
        member _.RenderHtml(tutorial) = (* custom rendering *)
        // ... implement other methods
```

Register plugin:
```fsharp
registry.RegisterContentProcessor(TutorialProcessor())
```

Update configuration:
```json
{
  "contentTypes": [
    {
      "name": "tutorials",
      "processor": "TutorialProcessor",
      "enabled": true
    }
  ]
}
```

### 3. Multi-Site Reusability

Same SSG.Core, different configurations:

**Blog Site** (`blog-config.json`):
```json
{
  "site": { "title": "My Blog" },
  "contentTypes": [
    { "name": "posts", "processor": "BlogPostProcessor", "enabled": true }
  ],
  "features": { 
    "rss": { "enabled": true },
    "search": { "enabled": true }
  }
}
```

**Portfolio Site** (`portfolio-config.json`):
```json
{
  "site": { "title": "My Portfolio" },
  "contentTypes": [
    { "name": "projects", "processor": "ProjectProcessor", "enabled": true },
    { "name": "gallery", "processor": "GalleryProcessor", "enabled": true }
  ],
  "features": {
    "rss": { "enabled": false },
    "collections": { "enabled": true }
  }
}
```

Build different sites:
```bash
ssg --config blog-config.json
ssg --config portfolio-config.json
```

## Migration Strategy

### Phase 1: Extract Core Framework
1. **Create SSG.Core project** with Configuration, PluginRegistry, Builder modules
2. **Package as NuGet library** for reusability
3. **Maintain existing Program.fs** as compatibility layer

### Phase 2: Configuration-Driven Migration
1. **Generate site-config.json** from existing hardcoded logic
2. **Update Program.fs** to load configuration and use SSG.Core.Builder
3. **Test configuration-driven build** maintains identical output

### Phase 3: Plugin Migration
1. **Extract content processors** to plugins (PostProcessor, NoteProcessor, etc.)
2. **Register plugins** through configuration or discovery
3. **Test plugin-based processing** maintains functionality

### Phase 4: Complete Separation
1. **Move content to separate repository**
2. **Create site-specific plugin assemblies**
3. **Distribute SSG.Core** as standalone package

## Benefits Achieved

### ✅ Configuration-Driven Customization
- Add content types through JSON config
- Toggle features without code changes
- Customize build behavior declaratively

### ✅ Plugin Architecture Extensibility
- Custom content processors through interfaces
- Custom block types for Markdown extensions
- Build pipeline hooks for additional processing
- View plugins for layout customization

### ✅ Multi-Site Reusability
- Same framework, different configurations
- Plugin ecosystem enables sharing
- Clean separation of framework vs. site logic

### ✅ Developer Experience
- CLI with helpful error messages
- Built-in example plugins and documentation
- Clear interfaces for plugin development
- Configuration validation and helpful diagnostics

## Next Steps

1. **Extract existing modules** to SSG.Core library
2. **Generate configuration** from current hardcoded Program.fs
3. **Test configuration-driven build** against current output
4. **Package and distribute** SSG.Core as reusable library
5. **Create plugin ecosystem** for common content types
6. **Separate content** into dedicated repository

This implementation provides the foundation for a truly decoupled, reusable static site generator that can support multiple sites with different requirements through configuration and plugins rather than code modifications.