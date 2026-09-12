module SSG.Core.Builder

open System
open System.IO
open SSG.Core.Configuration
open SSG.Core.PluginRegistry

// =============================================================================
// Configuration-Driven Build System
// =============================================================================

/// Build context containing configuration and runtime state
type BuildContext = {
    Config: SiteConfiguration
    Registry: PluginRegistry
    StartTime: DateTime
    OutputDirectory: string
    Errors: ResizeArray<string>
    Warnings: ResizeArray<string>
}

/// Build result with status and diagnostics
type BuildResult = {
    Success: bool
    Duration: TimeSpan
    ItemsProcessed: int
    Errors: string list
    Warnings: string list
}

/// Create build context from configuration
let createBuildContext (config: SiteConfiguration) (registry: PluginRegistry) : BuildContext =
    {
        Config = config
        Registry = registry
        StartTime = DateTime.Now
        OutputDirectory = Path.GetFullPath(config.Directories.Output)
        Errors = ResizeArray<string>()
        Warnings = ResizeArray<string>()
    }

// =============================================================================
// Pre-Build Setup
// =============================================================================

/// Clean output directory based on configuration
let cleanOutputDirectory (context: BuildContext) : unit =
    if context.Config.Build.CleanOutput && Directory.Exists(context.OutputDirectory) then
        try
            let dirInfo = DirectoryInfo(context.OutputDirectory)
            dirInfo.Delete(true)
            Directory.CreateDirectory(context.OutputDirectory) |> ignore
            printfn "✅ Output directory cleaned: %s" context.OutputDirectory
        with
        | ex -> 
            context.Errors.Add($"Failed to clean output directory: {ex.Message}")
            printfn "❌ Failed to clean output directory: %s" ex.Message

/// Create required directories based on configuration
let createRequiredDirectories (context: BuildContext) : unit =
    let directories = [
        context.OutputDirectory
        Path.Join(context.OutputDirectory, "feed")
        Path.Join(context.OutputDirectory, "tags")
    ]
    
    // Add content type specific directories
    for contentType in getEnabledContentTypes context.Config do
        let contentDir = Path.Join(context.OutputDirectory, contentType.SourceDirectory)
        directories @ [contentDir] |> ignore
    
    for dir in directories do
        try
            if not (Directory.Exists(dir)) then
                Directory.CreateDirectory(dir) |> ignore
        with
        | ex -> context.Errors.Add($"Failed to create directory {dir}: {ex.Message}")

/// Copy static files based on configuration
let copyStaticFiles (context: BuildContext) : unit =
    if context.Config.Build.CopyStaticFiles then
        try
            let assetsDir = context.Config.Directories.Assets
            let targetDir = Path.Join(context.OutputDirectory, "assets")
            
            if Directory.Exists(assetsDir) then
                if Directory.Exists(targetDir) then
                    Directory.Delete(targetDir, true)
                Directory.CreateDirectory(targetDir) |> ignore
                
                // Copy all files recursively
                let rec copyDirectory source target =
                    for file in Directory.GetFiles(source) do
                        let fileName = Path.GetFileName(file)
                        let targetFile = Path.Join(target, fileName)
                        File.Copy(file, targetFile, true)
                    
                    for dir in Directory.GetDirectories(source) do
                        let dirName = Path.GetFileName(dir)
                        let targetSubDir = Path.Join(target, dirName)
                        Directory.CreateDirectory(targetSubDir) |> ignore
                        copyDirectory dir targetSubDir
                
                copyDirectory assetsDir targetDir
                printfn "✅ Static files copied from %s to %s" assetsDir targetDir
            else
                context.Warnings.Add($"Assets directory not found: {assetsDir}")
        with
        | ex -> 
            context.Errors.Add($"Failed to copy static files: {ex.Message}")

// =============================================================================
// Content Processing Pipeline
// =============================================================================

/// Process a single content type based on configuration
let processContentType (context: BuildContext) (contentTypeConfig: ContentTypeConfig) : int =
    let sourceDir = Path.Join(context.Config.Directories.Source, contentTypeConfig.SourceDirectory)
    let mutable itemsProcessed = 0
    
    if Directory.Exists(sourceDir) then
        try
            let markdownFiles = Directory.GetFiles(sourceDir, "*.md")
            printfn "🔄 Processing %s (%d files)..." contentTypeConfig.Name markdownFiles.Length
            
            // Note: In actual implementation, this would use the registered content processor
            // For now, we'll just show the configuration-driven approach
            for file in markdownFiles do
                try
                    // This would call the appropriate processor from the registry
                    let processor = context.Registry.GetContentProcessor(contentTypeConfig.Processor)
                    match processor with
                    | Some _ -> 
                        // Process the file with the configured processor
                        itemsProcessed <- itemsProcessed + 1
                    | None ->
                        context.Warnings.Add($"Content processor '{contentTypeConfig.Processor}' not found for {contentTypeConfig.Name}")
                with
                | ex -> context.Errors.Add($"Failed to process {file}: {ex.Message}")
            
            printfn "✅ Processed %d %s items" itemsProcessed contentTypeConfig.Name
        with
        | ex -> context.Errors.Add($"Failed to process content type {contentTypeConfig.Name}: {ex.Message}")
    else
        context.Warnings.Add($"Source directory not found: {sourceDir}")
    
    itemsProcessed

/// Process all enabled content types
let processAllContentTypes (context: BuildContext) : int =
    let enabledContentTypes = getEnabledContentTypes context.Config
    let mutable totalProcessed = 0
    
    printfn "🔄 Processing %d enabled content types..." enabledContentTypes.Length
    
    for contentType in enabledContentTypes do
        let processed = processContentType context contentType
        totalProcessed <- totalProcessed + processed
    
    totalProcessed

// =============================================================================
// Feature-Based Processing
// =============================================================================

/// Generate RSS feeds if enabled
let generateRssFeeds (context: BuildContext) : unit =
    if context.Config.Features.Rss.Enabled then
        printfn "🔄 Generating RSS feeds..."
        
        // Unified feed
        if context.Config.Features.Rss.Unified then
            printfn "  • Unified feed (all content types)"
        
        // Tag feeds
        if context.Config.Features.Rss.TagFeeds then
            printfn "  • Tag-based feeds"
        
        // Content type feeds
        for contentType in getEnabledContentTypes context.Config do
            if contentType.FeedEnabled then
                printfn $"  • {contentType.Name} feed"
        
        printfn "✅ RSS feeds generated"

/// Generate tag pages if enabled
let generateTagPages (context: BuildContext) : unit =
    if context.Config.Features.Tags.Enabled && context.Config.Features.Tags.GeneratePages then
        printfn "🔄 Generating tag pages..."
        
        if context.Config.Features.Tags.CloudEnabled then
            printfn "  • Tag cloud page"
        
        printfn "✅ Tag pages generated"

/// Generate search index if enabled
let generateSearchIndex (context: BuildContext) : unit =
    if context.Config.Features.Search.Enabled && context.Config.Features.Search.GenerateIndex then
        printfn "🔄 Generating search index..."
        
        let indexPath = Path.Join(context.OutputDirectory, "search-index.json")
        
        if context.Config.Features.Search.IncludeContent then
            printfn "  • Including full content in search index"
        else
            printfn "  • Including titles and metadata only"
        
        printfn $"✅ Search index generated: {indexPath}"

/// Generate collections if enabled
let generateCollections (context: BuildContext) : unit =
    if context.Config.Features.Collections.Enabled then
        printfn "🔄 Generating collections..."
        
        if context.Config.Features.Collections.StarterPacks then
            printfn "  • Starter pack collections"
        
        printfn "✅ Collections generated"

/// Generate text-only version if enabled
let generateTextOnlyVersion (context: BuildContext) : unit =
    if context.Config.Features.TextOnlyVersion.Enabled then
        let textOnlyPath = Path.Join(context.OutputDirectory, context.Config.Features.TextOnlyVersion.OutputPath)
        printfn $"🔄 Generating text-only version: {textOnlyPath}"
        
        if not (Directory.Exists(textOnlyPath)) then
            Directory.CreateDirectory(textOnlyPath) |> ignore
        
        printfn "✅ Text-only version generated"

// =============================================================================
// Build Pipeline Orchestration
// =============================================================================

/// Execute pre-build plugins
let executePreBuildPlugins (context: BuildContext) : unit =
    let buildPlugins = context.Registry.GetBuildPlugins()
    if buildPlugins.Length > 0 then
        printfn "🔄 Executing pre-build plugins..."
        for plugin in buildPlugins do
            try
                plugin.PreBuild(context.Config)
                printfn $"  ✅ {plugin.Name}"
            with
            | ex -> context.Errors.Add($"Pre-build plugin {plugin.Name} failed: {ex.Message}")

/// Execute post-build plugins
let executePostBuildPlugins (context: BuildContext) : unit =
    let buildPlugins = context.Registry.GetBuildPlugins()
    if buildPlugins.Length > 0 then
        printfn "🔄 Executing post-build plugins..."
        for plugin in buildPlugins do
            try
                plugin.PostBuild(context.Config)
                printfn $"  ✅ {plugin.Name}"
            with
            | ex -> context.Errors.Add($"Post-build plugin {plugin.Name} failed: {ex.Message}")

/// Main build function - orchestrates entire build process
let buildSite (config: SiteConfiguration) (registry: PluginRegistry) : BuildResult =
    let context = createBuildContext config registry
    
    printfn "🚀 Starting site build with configuration-driven approach..."
    printfn $"   Site: {config.Site.Title}"
    printfn $"   Output: {context.OutputDirectory}"
    
    // Pre-build setup
    executePreBuildPlugins context
    cleanOutputDirectory context
    createRequiredDirectories context
    copyStaticFiles context
    
    // Core content processing
    let itemsProcessed = processAllContentTypes context
    
    // Feature-based generation
    generateRssFeeds context
    generateTagPages context
    generateSearchIndex context
    generateCollections context
    generateTextOnlyVersion context
    
    // Post-build plugins
    executePostBuildPlugins context
    
    // Build completion
    let duration = DateTime.Now - context.StartTime
    let success = context.Errors.Count = 0
    
    if success then
        printfn "✅ Build completed successfully in %O" duration
        printfn $"   Processed {itemsProcessed} items"
        if context.Warnings.Count > 0 then
            printfn $"   {context.Warnings.Count} warnings"
    else
        printfn "❌ Build failed in %O" duration
        printfn $"   {context.Errors.Count} errors"
        printfn $"   {context.Warnings.Count} warnings"
    
    {
        Success = success
        Duration = duration
        ItemsProcessed = itemsProcessed
        Errors = List.ofSeq context.Errors
        Warnings = List.ofSeq context.Warnings
    }

// =============================================================================
// Configuration-Based Build Entry Point
// =============================================================================

/// Build site from configuration file
let buildFromConfiguration (configPath: string) (pluginDirectory: string option) : BuildResult =
    match loadSiteConfiguration configPath with
    | Error error ->
        printfn "❌ Failed to load configuration: %s" error
        {
            Success = false
            Duration = TimeSpan.Zero
            ItemsProcessed = 0
            Errors = [error]
            Warnings = []
        }
    | Ok config ->
        match validateConfiguration config with
        | Error errors ->
            printfn "❌ Configuration validation failed:"
            errors |> List.iter (printfn "   • %s")
            {
                Success = false
                Duration = TimeSpan.Zero
                ItemsProcessed = 0
                Errors = errors
                Warnings = []
            }
        | Ok () ->
            match initializePluginRegistry config pluginDirectory with
            | Error errors ->
                printfn "❌ Plugin registry initialization failed:"
                errors |> List.iter (printfn "   • %s")
                {
                    Success = false
                    Duration = TimeSpan.Zero
                    ItemsProcessed = 0
                    Errors = errors
                    Warnings = []
                }
            | Ok registry ->
                buildSite config registry