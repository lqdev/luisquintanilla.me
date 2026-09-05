# Third-Party Plugin Development and NuGet Distribution

## Overview

This guide demonstrates how to create, distribute, and consume SSG.Core plugins as third-party NuGet packages without requiring any plugin code in your site repository.

## Plugin Development Workflow

### 1. Create Plugin Project (Separate Repository)

```bash
# Create new plugin project in separate repository
mkdir SSG.Plugins.Tutorial
cd SSG.Plugins.Tutorial

dotnet new classlib -n SSG.Plugins.Tutorial
cd SSG.Plugins.Tutorial

# Add SSG.Core dependency (when published as NuGet package)
dotnet add package SSG.Core --version 1.0.0
```

### 2. Plugin Project Structure

```
SSG.Plugins.Tutorial/
├── SSG.Plugins.Tutorial.csproj
├── TutorialProcessor.fs
├── CodeExerciseBlockProcessor.fs
├── README.md
└── plugin.manifest.json
```

### 3. Plugin Manifest (plugin.manifest.json)

```json
{
  "pluginInfo": {
    "id": "SSG.Plugins.Tutorial",
    "name": "Tutorial Content Plugin",
    "version": "1.0.0",
    "description": "Adds tutorial content type support with code exercises",
    "author": "Plugin Developer",
    "website": "https://github.com/author/ssg-plugins-tutorial"
  },
  "contentProcessors": [
    {
      "name": "TutorialProcessor",
      "typeName": "SSG.Plugins.Tutorial.TutorialProcessor",
      "contentType": "tutorials",
      "supportedExtensions": [".md", ".markdown"]
    }
  ],
  "customBlocks": [
    {
      "name": "code-exercise",
      "typeName": "SSG.Plugins.Tutorial.CodeExerciseBlockProcessor",
      "description": "Interactive code exercises for tutorials"
    }
  ],
  "dependencies": {
    "SSG.Core": ">=1.0.0"
  }
}
```

### 4. Project File Configuration

```xml
<!-- SSG.Plugins.Tutorial.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <PackageId>SSG.Plugins.Tutorial</PackageId>
    <PackageVersion>1.0.0</PackageVersion>
    <PackageDescription>Tutorial content type plugin for SSG.Core</PackageDescription>
    <PackageTags>static-site-generator;ssg;plugin;tutorial</PackageTags>
    <PackageAuthor>Plugin Developer</PackageAuthor>
    <PackageProjectUrl>https://github.com/author/ssg-plugins-tutorial</PackageProjectUrl>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <RepositoryUrl>https://github.com/author/ssg-plugins-tutorial</RepositoryUrl>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="SSG.Core" Version="1.0.0" />
  </ItemGroup>

  <ItemGroup>
    <Compile Include="TutorialProcessor.fs" />
    <Compile Include="CodeExerciseBlockProcessor.fs" />
  </ItemGroup>

  <ItemGroup>
    <Content Include="plugin.manifest.json">
      <PackagePath>contentFiles/any/any/plugin.manifest.json</PackagePath>
      <BuildAction>Content</BuildAction>
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </Content>
  </ItemGroup>

</Project>
```

### 5. Implement Plugin (TutorialProcessor.fs)

```fsharp
namespace SSG.Plugins.Tutorial

open System
open System.IO
open System.Xml.Linq
open SSG.Core.PluginRegistry

type Tutorial = {
    Title: string
    Content: string
    Author: string
    Date: DateTime
    Difficulty: string
    Tags: string array
    Slug: string
    Duration: int option
}

type TutorialProcessor() =
    interface IContentProcessor<Tutorial> with
        member _.Name = "TutorialProcessor"
        
        member _.Parse(filePath: string) : Tutorial option =
            // Implementation here
            Some {
                Title = "Sample Tutorial"
                Content = File.ReadAllText(filePath)
                Author = "Tutorial Author"
                Date = DateTime.Now
                Difficulty = "Intermediate"
                Tags = [|"tutorial"|]
                Slug = Path.GetFileNameWithoutExtension(filePath)
                Duration = Some 30
            }
        
        member _.RenderHtml(tutorial: Tutorial) : string =
            $"""<article class="tutorial">
                <h1>{tutorial.Title}</h1>
                <div class="tutorial-meta">
                    <span class="difficulty {tutorial.Difficulty.ToLower()}">{tutorial.Difficulty}</span>
                    <span class="duration">{tutorial.Duration |> Option.map (fun d -> $"{d} min") |> Option.defaultValue ""}</span>
                </div>
                <div class="content">{tutorial.Content}</div>
            </article>"""
        
        member _.RenderCard(tutorial: Tutorial) : string =
            $"""<div class="card tutorial-card">
                <div class="card-body">
                    <h5>{tutorial.Title}</h5>
                    <p class="difficulty">{tutorial.Difficulty}</p>
                    <small>{tutorial.Date:MMMM dd, yyyy}</small>
                </div>
            </div>"""
        
        member _.GenerateRssItem(tutorial: Tutorial) : XElement option =
            Some (XElement(XName.Get("item"),
                XElement(XName.Get("title"), tutorial.Title),
                XElement(XName.Get("description"), tutorial.Content),
                XElement(XName.Get("pubDate"), tutorial.Date.ToString("R"))
            ))
        
        member _.GetOutputPath(tutorial: Tutorial) : string =
            $"tutorials/{tutorial.Slug}/index.html"
        
        member _.GetSlug(tutorial: Tutorial) : string = tutorial.Slug
        member _.GetTags(tutorial: Tutorial) : string array = tutorial.Tags
        member _.GetDate(tutorial: Tutorial) : string = tutorial.Date.ToString("yyyy-MM-dd HH:mm zzz")
```

## Publishing Plugin as NuGet Package

### 1. Build and Package

```bash
# Build the plugin
dotnet build --configuration Release

# Create NuGet package
dotnet pack --configuration Release

# Publish to NuGet (requires API key)
dotnet nuget push bin/Release/SSG.Plugins.Tutorial.1.0.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

### 2. Alternative Publishing (Private Feed)

```bash
# Publish to private NuGet feed
dotnet nuget push bin/Release/SSG.Plugins.Tutorial.1.0.0.nupkg --source https://your-private-feed.com/nuget
```

## Consuming Third-Party Plugins (No Code in Site Repo)

### 1. Site Configuration Only

Your site repository contains only:
```
my-site/
├── site-config.json
├── _src/
│   └── tutorials/
│       └── my-tutorial.md
└── packages.config (optional)
```

### 2. Enhanced Site Configuration (site-config.json)

```json
{
  "site": {
    "title": "My Site",
    "baseUrl": "https://example.com"
  },
  "plugins": {
    "sources": [
      {
        "type": "nuget",
        "packages": [
          {
            "id": "SSG.Plugins.Tutorial",
            "version": "1.0.0"
          },
          {
            "id": "SSG.Plugins.Gallery",
            "version": "2.1.0"
          }
        ]
      },
      {
        "type": "directory",
        "path": "./local-plugins"
      }
    ]
  },
  "contentTypes": [
    {
      "name": "tutorials",
      "processor": "TutorialProcessor",
      "enabled": true,
      "urlPattern": "/tutorials/{slug}/",
      "feedEnabled": true
    }
  ],
  "customBlocks": [
    {
      "name": "code-exercise",
      "enabled": true,
      "parser": "CodeExerciseBlockProcessor"
    }
  ]
}
```

### 3. Plugin Resolution and Loading

The SSG.Core framework automatically:

1. **Downloads NuGet packages** to local cache
2. **Discovers plugin assemblies** from packages
3. **Loads plugin manifest files** to understand capabilities
4. **Registers processors and blocks** based on configuration
5. **Resolves dependencies** between plugins

### 4. Build Commands (No Local Plugin Code)

```bash
# Build site with NuGet plugin resolution
ssg build --config site-config.json

# Restore plugins first, then build
ssg restore-plugins --config site-config.json
ssg build --config site-config.json

# Build with specific plugin source
ssg build --config site-config.json --plugin-source https://your-private-feed.com/nuget
```

## Enhanced Plugin Registry for NuGet Support

Update `SSG.Core/PluginRegistry.fs` to support NuGet package resolution:

```fsharp
// Enhanced plugin source types
type PluginSource =
    | Directory of string
    | NuGetPackage of packageId: string * version: string option
    | NuGetFeed of feedUrl: string

type PluginConfiguration = {
    Sources: PluginSource list
    CacheDirectory: string option
    RestoreTimeout: TimeSpan option
}

// NuGet plugin resolver
let resolveNuGetPlugin (packageId: string) (version: string option) (cacheDir: string) : Result<string, string> =
    try
        // Use NuGet.Core to download and extract plugin package
        // Return path to extracted plugin assembly
        let pluginPath = Path.Combine(cacheDir, packageId, "lib", "net9.0", $"{packageId}.dll")
        if File.Exists(pluginPath) then
            Ok pluginPath
        else
            Error $"Plugin assembly not found: {pluginPath}"
    with
    | ex -> Error $"Failed to resolve NuGet plugin {packageId}: {ex.Message}"

// Enhanced plugin discovery with NuGet support
let discoverPluginsFromSources (sources: PluginSource list) (registry: PluginRegistry) : Result<unit, string list> =
    let errors = ResizeArray<string>()
    
    for source in sources do
        match source with
        | Directory path ->
            match discoverPlugins path registry with
            | Ok () -> ()
            | Error sourceErrors -> errors.AddRange(sourceErrors)
            
        | NuGetPackage (packageId, version) ->
            let cacheDir = Path.Combine(Path.GetTempPath(), "ssg-plugins")
            match resolveNuGetPlugin packageId version cacheDir with
            | Ok pluginPath ->
                match discoverPlugins (Path.GetDirectoryName(pluginPath)) registry with
                | Ok () -> printfn $"✅ Loaded NuGet plugin: {packageId}"
                | Error pluginErrors -> errors.AddRange(pluginErrors)
            | Error error -> errors.Add(error)
            
        | NuGetFeed feedUrl ->
            // Discover all available plugins from feed
            printfn $"Discovering plugins from feed: {feedUrl}"
    
    if errors.Count > 0 then
        Error (List.ofSeq errors)
    else
        Ok ()
```

## Benefits of NuGet Plugin Distribution

### ✅ Zero Code in Site Repository
- Site contains only configuration and content
- Plugins installed via NuGet package manager
- No plugin source code in site repository

### ✅ Version Management
- Semantic versioning for plugin updates
- Dependency resolution between plugins
- Easy rollback to previous plugin versions

### ✅ Plugin Ecosystem
- Searchable on NuGet.org
- Community-contributed plugins
- Private feeds for organizational plugins

### ✅ Development Workflow
- Plugin development in separate repositories
- CI/CD for plugin publishing
- Independent plugin and site release cycles

## Example Usage Scenarios

### Scenario 1: Community Plugin Consumption
```bash
# Site developer finds useful plugin on NuGet
# No code changes needed, just configuration update

# Update site-config.json
{
  "plugins": {
    "sources": [
      {
        "type": "nuget",
        "packages": [
          { "id": "SSG.Plugins.SocialMedia", "version": "1.2.0" }
        ]
      }
    ]
  },
  "contentTypes": [
    {
      "name": "social-posts", 
      "processor": "SocialMediaProcessor",
      "enabled": true
    }
  ]
}

# Build site - plugin automatically downloaded and used
ssg build
```

### Scenario 2: Private Plugin Distribution
```bash
# Organization publishes internal plugins to private feed
# Teams consume without accessing plugin source code

# Corporate site configuration
{
  "plugins": {
    "sources": [
      {
        "type": "nuget",
        "packages": [
          { "id": "Corp.SSG.Plugins.ComplianceReporting", "version": "2.0.0" },
          { "id": "Corp.SSG.Plugins.BrandGuidelines", "version": "1.5.0" }
        ]
      }
    ]
  }
}
```

This approach ensures complete separation between plugin development and site development while enabling rich plugin ecosystems through standard NuGet package distribution.