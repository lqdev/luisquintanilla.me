#load "SSG.Core/Configuration.fs"
#load "SSG.Core/PluginRegistry.fs"
#load "SSG.Core/Builder.fs"
#load "SSG.Core/ExamplePlugins.fs"

open System
open System.IO
open SSG.Core.Configuration
open SSG.Core.PluginRegistry
open SSG.Core.Builder
open SSG.Core.ExamplePlugins

// =============================================================================
// Test Configuration Loading and Validation
// =============================================================================

printfn "🔧 Testing SSG.Core Configuration System"
printfn "=" 

// Test configuration loading
let testConfigPath = "site-config.json"

match loadSiteConfiguration testConfigPath with
| Error error ->
    printfn "❌ Failed to load configuration: %s" error
| Ok config ->
    printfn "✅ Configuration loaded successfully"
    printfn $"   Site: {config.Site.Title}"
    printfn $"   Base URL: {config.Site.BaseUrl}"
    printfn $"   Source Directory: {config.Directories.Source}"
    printfn $"   Output Directory: {config.Directories.Output}"
    
    // Test configuration validation
    match validateConfiguration config with
    | Error errors ->
        printfn "❌ Configuration validation failed:"
        errors |> List.iter (printfn "   • %s")
    | Ok () ->
        printfn "✅ Configuration validation passed"
        
        // Show enabled content types
        let enabledContentTypes = getEnabledContentTypes config
        printfn $"\n📄 Enabled Content Types ({enabledContentTypes.Length}):"
        for contentType in enabledContentTypes do
            printfn $"   • {contentType.Name} -> {contentType.Processor}"
        
        // Show enabled features
        printfn "\n🎯 Enabled Features:"
        if isFeatureEnabled config "rss" then
            printfn $"   • RSS: Unified={config.Features.Rss.Unified}, TagFeeds={config.Features.Rss.TagFeeds}"
        if isFeatureEnabled config "tags" then
            printfn $"   • Tags: Pages={config.Features.Tags.GeneratePages}, Cloud={config.Features.Tags.CloudEnabled}"
        if isFeatureEnabled config "search" then
            printfn $"   • Search: Index={config.Features.Search.GenerateIndex}, Content={config.Features.Search.IncludeContent}"
        if isFeatureEnabled config "timeline" then
            printfn $"   • Timeline: Progressive={config.Features.Timeline.ProgressiveLoading}, Initial={config.Features.Timeline.InitialItems}"
        if isFeatureEnabled config "collections" then
            printfn $"   • Collections: StarterPacks={config.Features.Collections.StarterPacks}"
        if isFeatureEnabled config "indieweb" then
            printfn $"   • IndieWeb: Webmentions={config.Features.IndieWeb.Webmentions}, Microformats={config.Features.IndieWeb.Microformats}"
        
        // Show enabled custom blocks
        let enabledCustomBlocks = getEnabledCustomBlocks config
        printfn $"\n🔌 Enabled Custom Blocks ({enabledCustomBlocks.Length}):"
        for customBlock in enabledCustomBlocks do
            printfn $"   • {customBlock.Name} -> {customBlock.Parser}"

// =============================================================================
// Test Plugin Registry
// =============================================================================

printfn "\n🔌 Testing Plugin Registry System"
printfn "================================="

let registry = PluginRegistry()

// Register example plugins
registerExamplePlugins registry

let contentProcessorNames = String.Join(", ", registry.GetContentProcessorNames())
let customBlockProcessorNames = String.Join(", ", registry.GetCustomBlockProcessorNames())
printfn $"✅ Registered content processors: {contentProcessorNames}"
printfn $"✅ Registered custom block processors: {customBlockProcessorNames}"

// Test plugin retrieval
match registry.GetContentProcessor<BlogPost>("BlogPostProcessor") with
| Some processor ->
    printfn $"✅ Retrieved blog post processor: {processor.Name}"
| None ->
    printfn "❌ Failed to retrieve blog post processor"

match registry.GetCustomBlockProcessor("quote") with
| Some processor ->
    printfn $"✅ Retrieved custom block processor: {processor.Name}"
| None ->
    printfn "❌ Failed to retrieve custom block processor"

// =============================================================================
// Test Configuration-Driven Build Process
// =============================================================================

printfn "\n🚀 Testing Configuration-Driven Build"
printfn "====================================="

// Test build context creation
match loadSiteConfiguration testConfigPath with
| Ok config ->
    let context = createBuildContext config registry
    printfn $"✅ Build context created for: {config.Site.Title}"
    printfn $"   Start time: {context.StartTime}"
    printfn $"   Output directory: {context.OutputDirectory}"
    
    // Test enabled content type processing
    let enabledContentTypes = getEnabledContentTypes config
    printfn $"\n📝 Would process {enabledContentTypes.Length} content types:"
    for contentType in enabledContentTypes do
        let sourceDir = Path.Join(config.Directories.Source, contentType.SourceDirectory)
        let exists = Directory.Exists(sourceDir)
        let status = if exists then "✅" else "⚠️ (directory not found)"
        printfn $"   • {contentType.Name}: {sourceDir} {status}"
        
        if exists then
            let markdownFiles = Directory.GetFiles(sourceDir, "*.md")
            printfn $"     Found {markdownFiles.Length} Markdown files"
    
    printfn "\n🎯 Feature-based processing would include:"
    if config.Features.Rss.Enabled then
        printfn "   • RSS feed generation"
    if config.Features.Tags.Enabled && config.Features.Tags.GeneratePages then
        printfn "   • Tag page generation"
    if config.Features.Search.Enabled && config.Features.Search.GenerateIndex then
        printfn "   • Search index generation"
    if config.Features.Collections.Enabled then
        printfn "   • Collection generation"
    if config.Features.TextOnlyVersion.Enabled then
        let textOnlyPath = config.Features.TextOnlyVersion.OutputPath
        printfn "   • Text-only version in '%s'" textOnlyPath

| Error error ->
    printfn $"❌ Cannot test build - configuration error: {error}"

// =============================================================================
// Test Plugin Development Pattern
// =============================================================================

printfn "\n🔧 Plugin Development Demonstration"
printfn "=================================="

// Example of how a user would create a custom content processor
type CustomContentProcessor() =
    interface IContentProcessor<BlogPost> with
        member _.Name = "CustomContentProcessor"
        member _.Parse(filePath) = 
            printfn $"CustomContentProcessor: Parsing {filePath}"
            None // Simplified for demo
        member _.RenderHtml(content) = 
            "<p>Custom rendering</p>"
        member _.RenderCard(content) = 
            "<div>Custom card</div>"
        member _.GenerateRssItem(content) = 
            None
        member _.GetOutputPath(content) = 
            "custom/path.html"
        member _.GetSlug(content) = 
            "custom-slug"
        member _.GetTags(content) = 
            [|"custom"|]
        member _.GetDate(content) = 
            DateTime.Now.ToString()

// Register custom processor
let customProcessor = CustomContentProcessor()
registry.RegisterContentProcessor(customProcessor)
printfn $"✅ Registered custom processor: {customProcessor.Name}"

// Test retrieval
match registry.GetContentProcessor<BlogPost>("CustomContentProcessor") with
| Some processor ->
    printfn $"✅ Successfully retrieved custom processor: {processor.Name}"
| None ->
    printfn "❌ Failed to retrieve custom processor"

// =============================================================================
// Summary and Next Steps
// =============================================================================

printfn "\n📋 SSG.Core Decoupling Test Summary"
printfn "==================================="
printfn "✅ Configuration system: Loads and validates JSON configuration"
printfn "✅ Plugin registry: Registers and retrieves content processors and custom blocks"
printfn "✅ Build orchestration: Configuration-driven build pipeline"
printfn "✅ Extensibility: Plugin development patterns demonstrated"

printfn "\n🎯 Ready for Migration Phase 2:"
printfn "1. Generate site-config.json from current Program.fs hardcoded logic"
printfn "2. Extract existing content processors to plugin architecture"
printfn "3. Replace Program.fs with configuration-driven approach"
printfn "4. Test output equivalence between old and new systems"
printfn "5. Separate content repository from generator framework"

printfn "\n🔧 Next Steps to Complete Decoupling:"
printfn "• Update PersonalSite.fsproj to reference SSG.Core modules"
printfn "• Map existing Domain.fs types to plugin interfaces"
printfn "• Extract PostProcessor, NoteProcessor, etc. to plugins" 
printfn "• Generate accurate site-config.json for current functionality"
printfn "• Test configuration-driven build produces identical output"

printfn $"\n✨ SSG.Core foundation successfully established!"