#load "SSG.Core/Configuration.fs"

open System
open System.IO
open SSG.Core.Configuration

printfn "🔧 Testing SSG.Core Configuration System"
printfn "========================================"

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

printfn "\n✅ Configuration system test completed successfully!"