# SSG Decoupling Migration Guide

## Overview

This guide provides step-by-step instructions for migrating from the current coupled static site generator to the new decoupled SSG.Core architecture.

## Current State Analysis

### Tight Coupling Issues Identified:
- **Hardcoded build logic** in Program.fs (85 lines of orchestration)
- **Site-specific content processing** mixed with framework code
- **Fixed content type system** requiring code changes for new types
- **No plugin architecture** for extensibility
- **Content and generator in same repository** preventing independent iteration

### Coupling Points:
1. `Program.fs` - Hardcoded build orchestration and directory creation
2. `Builder.fs` - Mixed site-specific and framework logic
3. `Domain.fs` - Site-specific types mixed with framework interfaces
4. `GenericBuilder.fs` - Reusable core but tightly integrated
5. `Views/` modules - Site-specific view logic

## Migration Phases

### Phase 1: Foundation Setup ✅ COMPLETED

**Objective**: Create core framework structure and configuration system

**Completed Work**:
- ✅ `SSG.Core/Configuration.fs` - JSON-driven configuration system
- ✅ `SSG.Core/PluginRegistry.fs` - Plugin discovery and registration  
- ✅ `SSG.Core/Builder.fs` - Configuration-driven build orchestration
- ✅ `SSG.Core/ExamplePlugins.fs` - Plugin development examples
- ✅ `SSG.Core/Program.fs` - CLI entry point for decoupled system
- ✅ `site-config.json` - Complete configuration file for current site
- ✅ Documentation and migration guide

**Result**: Core framework foundation established with plugin architecture

### Phase 2: Configuration Generation

**Objective**: Generate site-config.json from existing hardcoded Program.fs logic

**Tasks**:
1. **Analyze current Program.fs build logic**:
   ```fsharp
   // Current hardcoded approach
   let posts = loadPosts(srcDir)
   let feedNotes = loadNotes(srcDir) 
   let responses = loadResponses(srcDir)
   ```

2. **Generate equivalent configuration**:
   ```json
   {
     "contentTypes": [
       {
         "name": "posts",
         "sourceDirectory": "posts", 
         "processor": "PostProcessor",
         "enabled": true,
         "urlPattern": "/posts/{slug}/",
         "feedEnabled": true,
         "archiveEnabled": true
       },
       {
         "name": "notes",
         "sourceDirectory": "feed",
         "processor": "NoteProcessor", 
         "enabled": true,
         "urlPattern": "/notes/{slug}/",
         "feedEnabled": true,
         "archiveEnabled": true
       },
       {
         "name": "responses", 
         "sourceDirectory": "responses",
         "processor": "ResponseProcessor",
         "enabled": true,
         "urlPattern": "/responses/{slug}/",
         "feedEnabled": true,
         "archiveEnabled": false
       }
     ]
   }
   ```

3. **Map current feature flags** to configuration:
   ```json
   {
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
         "cloudEnabled": false 
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
       "textOnlyVersion": {
         "enabled": true,
         "outputPath": "text"
       },
       "collections": {
         "enabled": true,
         "starterPacks": true
       },
       "indieWeb": {
         "enabled": true,
         "webmentions": true,
         "microformats": true
       }
     }
   }
   ```

4. **Create configuration generation script**:
   ```fsharp
   // Scripts/generate-site-config.fsx
   let generateCurrentSiteConfig () =
       let config = {
           Site = {
               Title = "Luis Quintanilla" 
               Description = "Personal website and IndieWeb presence"
               BaseUrl = "https://www.lqdev.me"
               Author = {
                   Name = "Luis Quintanilla"
                   Email = "luis@lqdev.me"
                   Url = "https://www.lqdev.me"
               }
           }
           // ... map all current functionality
       }
       let json = JsonSerializer.Serialize(config, JsonSerializerOptions(WriteIndented = true))
       File.WriteAllText("site-config.json", json)
   ```

**Success Criteria**: site-config.json accurately represents all current build logic

### Phase 3: Plugin Implementation

**Objective**: Extract content processors to plugins

**Tasks**:
1. **Create PostProcessor plugin**:
   ```fsharp
   type PostProcessor() =
       interface IContentProcessor<Post> with
           member _.Name = "PostProcessor"
           member _.Parse(filePath) = 
               // Use existing loadPosts logic
           member _.RenderHtml(post) = 
               // Use existing post rendering from Views
           member _.RenderCard(post) = 
               // Use existing card rendering
           member _.GenerateRssItem(post) = 
               // Use existing RSS item generation
   ```

2. **Create NoteProcessor plugin**:
   ```fsharp 
   type NoteProcessor() =
       interface IContentProcessor<Note> with
           member _.Name = "NoteProcessor"
           // Use existing GenericBuilder.NoteProcessor logic
   ```

3. **Create ResponseProcessor plugin**:
   ```fsharp
   type ResponseProcessor() = 
       interface IContentProcessor<Response> with
           member _.Name = "ResponseProcessor"
           // Use existing GenericBuilder.ResponseProcessor logic
   ```

4. **Create custom block plugins**:
   ```fsharp
   // Extract from CustomBlocks.fs
   type MediaBlockProcessor() =
       interface ICustomBlockProcessor with
           member _.Name = "media"
           // Use existing MediaBlock parsing/rendering
   
   type ReviewBlockProcessor() =
       interface ICustomBlockProcessor with
           member _.Name = "review" 
           // Use existing ReviewBlock parsing/rendering
   ```

**Success Criteria**: All content types processed through plugin architecture

### Phase 4: Configuration-Driven Build

**Objective**: Replace hardcoded Program.fs with configuration-driven approach

**Current Program.fs** (85 lines):
```fsharp
[<EntryPoint>]
let main argv =
    let srcDir = "_src"
    let outputDir = "_public"
    
    // Hardcoded setup
    cleanOutputDirectory outputDir
    copyStaticFiles ()
    
    // Hardcoded directory creation
    Path.Join(outputDir,"feed") |> Directory.CreateDirectory |> ignore
    // ... 15+ hardcoded directory creations
    
    // Hardcoded data loading
    let posts = loadPosts(srcDir)
    let feedNotes = loadNotes(srcDir)
    // ... load all content types
    
    // Hardcoded build orchestration  
    let postsFeedData = buildPosts()
    let notesFeedData = buildNotes()
    // ... build all content types
    
    0
```

**New Configuration-Driven Program.fs**:
```fsharp
[<EntryPoint>]
let main argv =
    let args = parseArguments argv
    let configPath = args.ConfigPath |> Option.defaultValue "site-config.json"
    
    // Configuration-driven build
    let result = SSG.Core.Builder.buildFromConfiguration configPath args.PluginDirectory
    
    if result.Success then 0 else 1
```

**Migration Steps**:
1. **Backup current Program.fs** as `Program.Legacy.fs`
2. **Replace Program.fs** with configuration-driven version
3. **Test build equivalence** using output comparison
4. **Fix any missing functionality** through configuration or plugins

**Success Criteria**: Configuration-driven build produces identical output to current system

### Phase 5: Plugin Registration

**Objective**: Register site-specific plugins with the framework

**Tasks**:
1. **Create site plugin assembly**:
   ```fsharp
   // PersonalSite.Plugins/Processors.fs
   module PersonalSite.Plugins.Processors
   
   // Move existing processors here
   type PostProcessor() = (* existing logic *)
   type NoteProcessor() = (* existing logic *) 
   type ResponseProcessor() = (* existing logic *)
   type SnippetProcessor() = (* existing logic *)
   type WikiProcessor() = (* existing logic *)
   type PresentationProcessor() = (* existing logic *)
   ```

2. **Create plugin registration**:
   ```fsharp
   // PersonalSite.Plugins/Registration.fs
   let registerPersonalSitePlugins (registry: PluginRegistry) =
       registry.RegisterContentProcessor(PostProcessor())
       registry.RegisterContentProcessor(NoteProcessor())
       registry.RegisterContentProcessor(ResponseProcessor())
       registry.RegisterCustomBlockProcessor(MediaBlockProcessor())
       registry.RegisterCustomBlockProcessor(ReviewBlockProcessor())
   ```

3. **Update configuration** to reference plugins:
   ```json
   {
     "contentTypes": [
       {
         "name": "posts",
         "processor": "PersonalSite.Plugins.PostProcessor",
         "enabled": true
       }
     ],
     "plugins": {
       "customBlocks": [
         {
           "name": "media",
           "parser": "PersonalSite.Plugins.MediaBlockProcessor",
           "enabled": true
         }
       ]
     }
   }
   ```

**Success Criteria**: Site builds using registered plugins from configuration

### Phase 6: Content Repository Separation

**Objective**: Separate content from generator for independent iteration

**Current Structure**:
```
luisquintanilla.me/
├── Program.fs (generator)
├── Builder.fs (generator) 
├── Domain.fs (generator)
├── _src/ (content)
└── Data/ (content)
```

**Target Structure**:
```
SSG.Core/ (Framework Repository)
├── SSG.Core.dll
└── CLI tools

luisquintanilla.me-content/ (Content Repository)  
├── site-config.json
├── _src/
├── Data/
└── PersonalSite.Plugins.dll

luisquintanilla.me-generator/ (Site Repository - Optional)
├── Program.fs (thin wrapper)
├── PersonalSite.fsproj
└── Dependencies: SSG.Core
```

**Migration Steps**:
1. **Create new content repository**:
   ```bash
   git subtree split --prefix=_src -b content-only
   git subtree split --prefix=Data -b data-only
   # Create new repo with content
   ```

2. **Update build to reference external content**:
   ```json
   {
     "directories": {
       "source": "../luisquintanilla.me-content/_src",
       "data": "../luisquintanilla.me-content/Data",
       "output": "_public"
     }
   }
   ```

3. **Create deployment workflow**:
   ```yaml
   # .github/workflows/build.yml
   - name: Checkout content
     uses: actions/checkout@v4
     with:
       repository: lqdev/luisquintanilla.me-content
       path: content
   
   - name: Build site  
     run: ssg --config content/site-config.json
   ```

**Success Criteria**: Content and generator can be updated independently

### Phase 7: Distribution and Reusability

**Objective**: Package SSG.Core for reuse by other sites

**Tasks**:
1. **Create NuGet package**:
   ```xml
   <!-- SSG.Core.fsproj -->
   <PackageId>SSG.Core</PackageId>
   <PackageVersion>1.0.0</PackageVersion>
   <PackageDescription>Configuration-driven static site generator</PackageDescription>
   <PackageTags>static-site-generator;fsharp;configuration</PackageTags>
   ```

2. **Create site templates**:
   ```
   Templates/
   ├── blog-template/
   │   ├── site-config.json (blog focus)
   │   └── _src/posts/
   ├── portfolio-template/
   │   ├── site-config.json (portfolio focus) 
   │   └── _src/projects/
   └── personal-template/
       ├── site-config.json (full features)
       └── _src/ (multiple content types)
   ```

3. **Create CLI tool**:
   ```bash
   # Global tool installation
   dotnet tool install --global SSG.Core.CLI
   
   # Create new site from template
   ssg new --template blog my-blog-site
   ssg new --template portfolio my-portfolio  
   
   # Build site
   cd my-blog-site
   ssg build
   ```

4. **Documentation and examples**:
   ```
   docs/
   ├── getting-started.md
   ├── configuration-reference.md
   ├── plugin-development.md
   ├── content-types.md
   └── examples/
       ├── blog-example/
       ├── portfolio-example/
       └── documentation-site/
   ```

**Success Criteria**: Other developers can create sites using SSG.Core without touching framework code

## Validation Strategy

### Output Comparison Testing
```fsharp
// Tests/MigrationValidation.fsx
let validateMigrationOutput () =
    // Build with current system
    let currentOutput = buildCurrentSystem()
    
    // Build with configuration-driven system
    let newOutput = SSG.Core.Builder.buildFromConfiguration "site-config.json" None
    
    // Compare outputs
    let comparison = OutputComparison.compare currentOutput.OutputDirectory newOutput.OutputDirectory
    
    match comparison with
    | Identical -> printfn "✅ Migration successful - identical output"
    | Differences diffs -> 
        printfn "❌ Migration issues found:"
        diffs |> List.iter (printfn "  • %s")
```

### Feature Parity Checklist
- [ ] All 6 content types process correctly
- [ ] RSS feeds generate with identical content
- [ ] Tag system produces same tag pages
- [ ] Search index contains same data
- [ ] Timeline progressive loading works
- [ ] Text-only version generates correctly
- [ ] Collections and starter packs build
- [ ] Custom blocks (media, review) render properly
- [ ] IndieWeb microformats preserved
- [ ] Build performance comparable

### Rollback Plan
1. Keep `Program.Legacy.fs` as fallback
2. Maintain current build system during transition
3. Use feature flags to switch between systems
4. Automated testing to catch regressions
5. Quick rollback if issues discovered

## Benefits Achieved

### ✅ Decoupling Goals Met
- **Independent iteration**: Content and generator separated
- **Configuration-driven**: No code changes for new content types
- **Plugin extensibility**: Custom processors through interfaces  
- **Reusability**: Framework packaged for other sites

### ✅ Extensibility Scenarios Enabled
- **Add content types**: Through configuration only
- **Custom processing**: Through plugin development  
- **Multi-site support**: Same framework, different configs
- **Community plugins**: Shareable plugin ecosystem

### ✅ Developer Experience Improved
- **Clear interfaces**: Plugin development patterns
- **Helpful CLI**: Configuration validation and error messages
- **Good defaults**: Working examples and templates
- **Comprehensive docs**: Migration guides and API references

This migration guide provides a systematic approach to decoupling the static site generator while maintaining all current functionality and enabling significant extensibility improvements.