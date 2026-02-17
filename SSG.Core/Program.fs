module SSG.Core.Program

open System
open System.IO
open SSG.Core.Configuration
open SSG.Core.PluginRegistry  
open SSG.Core.Builder
open SSG.Core.ExamplePlugins

// =============================================================================
// Configuration-Driven SSG Entry Point
// =============================================================================

/// Command line arguments for the SSG
type Arguments = {
    ConfigPath: string option
    PluginDirectory: string option
    Verbose: bool
    Help: bool
}

/// Parse command line arguments
let parseArguments (args: string array) : Arguments =
    let rec loop acc remaining =
        match remaining with
        | [] -> acc
        | "--config" :: path :: rest -> loop { acc with ConfigPath = Some path } rest
        | "-c" :: path :: rest -> loop { acc with ConfigPath = Some path } rest
        | "--plugins" :: dir :: rest -> loop { acc with PluginDirectory = Some dir } rest
        | "-p" :: dir :: rest -> loop { acc with PluginDirectory = Some dir } rest
        | "--verbose" :: rest -> loop { acc with Verbose = true } rest
        | "-v" :: rest -> loop { acc with Verbose = true } rest
        | "--help" :: rest -> loop { acc with Help = true } rest
        | "-h" :: rest -> loop { acc with Help = true } rest
        | unknown :: rest -> 
            printfn "Warning: Unknown argument %s" unknown
            loop acc rest
    
    loop { ConfigPath = None; PluginDirectory = None; Verbose = false; Help = false } (Array.toList args)

/// Print usage information
let printUsage () =
    printfn """SSG.Core - Configuration-Driven Static Site Generator

Usage:
    ssg [options]

Options:
    -c, --config <path>     Path to site configuration JSON file (default: site-config.json)
    -p, --plugins <dir>     Directory containing plugin assemblies
    -v, --verbose          Enable verbose output
    -h, --help             Show this help message

Configuration File Format:
    The configuration file should be a JSON file with the following structure:
    {
      "site": {
        "title": "My Site",
        "baseUrl": "https://example.com"
      },
      "directories": {
        "source": "_src",
        "output": "_public"
      },
      "contentTypes": [
        {
          "name": "posts",
          "processor": "BlogPostProcessor",
          "enabled": true
        }
      ],
      "features": {
        "rss": { "enabled": true }
      }
    }

Plugin Development:
    Create .NET assemblies implementing the SSG.Core plugin interfaces:
    - IContentProcessor<T> for custom content types
    - ICustomBlockProcessor for custom Markdown blocks
    - IViewPlugin for custom layouts
    - IBuildPlugin for build pipeline hooks

Examples:
    ssg --config my-site.json
    ssg --config my-site.json --plugins ./plugins
    ssg --verbose"""

/// Initialize the SSG with built-in plugins
let initializeSSG (config: SiteConfiguration) (pluginDir: string option) : Result<PluginRegistry, string list> =
    match initializePluginRegistry config pluginDir with
    | Ok registry ->
        // Register built-in example plugins
        registerExamplePlugins registry
        printfn "✅ SSG.Core initialized with built-in plugins"
        Ok registry
    | Error errors -> Error errors

/// Main entry point demonstrating configuration-driven approach
let runSSG (args: Arguments) : int =
    if args.Help then
        printUsage()
        0
    else
        // Determine configuration path
        let configPath = args.ConfigPath |> Option.defaultValue "site-config.json"
        
        if not (File.Exists(configPath)) then
            printfn "❌ Configuration file not found: %s" configPath
            printfn "Use --help for usage information"
            1
        else
            printfn "🔧 Configuration-Driven Static Site Generator"
            printfn $"   Config: {configPath}"
            
            match args.PluginDirectory with
            | Some dir -> printfn $"   Plugins: {dir}"
            | None -> printfn "   Plugins: Built-in only"
            
            // Load and validate configuration
            match loadSiteConfiguration configPath with
            | Error error ->
                printfn "❌ Configuration error: %s" error
                1
            | Ok config ->
                match validateConfiguration config with
                | Error errors ->
                    printfn "❌ Configuration validation failed:"
                    errors |> List.iter (printfn "   • %s")
                    1
                | Ok () ->
                    // Initialize SSG with plugins
                    match initializeSSG config args.PluginDirectory with
                    | Error errors ->
                        printfn "❌ SSG initialization failed:"
                        errors |> List.iter (printfn "   • %s")
                        1
                    | Ok registry ->
                        // Build the site
                        let result = buildSite config registry
                        
                        if args.Verbose then
                            if result.Warnings.Length > 0 then
                                printfn "\nWarnings:"
                                result.Warnings |> List.iter (printfn "  ⚠️  %s")
                            
                            if result.Errors.Length > 0 then
                                printfn "\nErrors:"
                                result.Errors |> List.iter (printfn "  ❌ %s")
                        
                        if result.Success then 0 else 1

// =============================================================================
// Demonstration Functions
// =============================================================================

/// Create example configuration for demonstration
let createExampleConfiguration (outputPath: string) : unit =
    let exampleConfig = createDefaultConfiguration()
    let configWithPlugins = { 
        exampleConfig with 
            Site = { exampleConfig.Site with Title = "Example SSG Site"; BaseUrl = "https://example-ssg.com" }
            ContentTypes = [|
                { Name = "posts"; SourceDirectory = "posts"; Processor = "BlogPostProcessor"; Enabled = true; UrlPattern = "/posts/{slug}/"; FeedEnabled = true; ArchiveEnabled = true }
                { Name = "notes"; SourceDirectory = "notes"; Processor = "NoteProcessor"; Enabled = true; UrlPattern = "/notes/{slug}/"; FeedEnabled = true; ArchiveEnabled = false }
            |]
            Plugins = {
                CustomBlocks = [| { Name = "quote"; Enabled = true; Parser = "QuoteBlockProcessor"; Renderer = "QuoteBlockHtmlRenderer" } |]
                MarkdownExtensions = [| "UsePipeTables"; "UseGenericAttributes"; "UseAutoLinks" |]
            }
            Features = {
                exampleConfig.Features with
                    Rss = { Enabled = true; Unified = true; TagFeeds = true; ItemLimit = 20 }
                    Search = { Enabled = true; GenerateIndex = true; IncludeContent = true }
                    Tags = { Enabled = true; GeneratePages = true; CloudEnabled = true }
            }
    }
    
    let json = System.Text.Json.JsonSerializer.Serialize(configWithPlugins, System.Text.Json.JsonSerializerOptions(WriteIndented = true))
    File.WriteAllText(outputPath, json)
    printfn $"✅ Example configuration created: {outputPath}"

/// Demonstrate the configuration-driven approach
let demonstrateDecoupling () : unit =
    printfn """
🎯 SSG.Core Decoupling Demonstration

This example shows how the static site generator has been decoupled into:

1. 📋 Configuration-Driven Core
   • Site configuration loaded from JSON
   • Content types defined in configuration
   • Features enabled/disabled through config
   • Plugin discovery and registration

2. 🔌 Plugin Architecture  
   • Content processors as plugins (BlogPostProcessor, NoteProcessor)
   • Custom block processors (QuoteBlockProcessor)
   • View plugins for layout customization
   • Build pipeline plugins for extensibility

3. 🏗️ Extensibility Patterns
   • Add new content types without code changes
   • Register custom block types through configuration
   • Hook into build pipeline with plugins
   • Customize output through view plugins

4. 🚀 Reusability Benefits
   • Core framework can be packaged as NuGet library
   • Multiple sites can use same SSG with different configs
   • Plugin ecosystem enables community contributions
   • Clean separation of concerns

Next Steps for Full Decoupling:
   • Extract Domain.fs, ASTParsing.fs to SSG.Core library
   • Move site-specific logic to configuration
   • Create plugin packages for content types
   • Separate content repository from generator
"""

/// CLI entry point
[<EntryPoint>]
let main argv =
    try
        let args = parseArguments argv
        
        // Special commands
        if Array.contains "--demo" argv then
            demonstrateDecoupling()
            0
        elif Array.contains "--create-config" argv then
            createExampleConfiguration "example-site-config.json"
            0
        else
            runSSG args
    with
    | ex ->
        printfn "❌ Unexpected error: %s" ex.Message
        if ex.InnerException <> null then
            printfn "   Inner: %s" ex.InnerException.Message
        1