module SSG.Core.Configuration

open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization

// =============================================================================
// Core Configuration Types
// =============================================================================

[<CLIMutable>]
type SiteInfo = {
    Title: string
    Description: string
    BaseUrl: string
    Author: AuthorInfo
}
and [<CLIMutable>] AuthorInfo = {
    Name: string
    Email: string
    Url: string
}

[<CLIMutable>]
type DirectoryConfig = {
    Source: string
    Output: string
    Data: string
    Assets: string
}

[<CLIMutable>]
type ContentTypeConfig = {
    Name: string
    SourceDirectory: string
    Processor: string
    Enabled: bool
    UrlPattern: string
    FeedEnabled: bool
    ArchiveEnabled: bool
}

[<CLIMutable>]
type CustomBlockConfig = {
    Name: string
    Enabled: bool
    Parser: string
    Renderer: string
}

[<CLIMutable>]
type PluginConfig = {
    CustomBlocks: CustomBlockConfig array
    MarkdownExtensions: string array
}

[<CLIMutable>]
type RssConfig = {
    Enabled: bool
    Unified: bool
    TagFeeds: bool
    ItemLimit: int
}

[<CLIMutable>]
type TagConfig = {
    Enabled: bool
    GeneratePages: bool
    CloudEnabled: bool
}

[<CLIMutable>]
type SearchConfig = {
    Enabled: bool
    GenerateIndex: bool
    IncludeContent: bool
}

[<CLIMutable>]
type TimelineConfig = {
    Enabled: bool
    ProgressiveLoading: bool
    InitialItems: int
    ChunkSize: int
}

[<CLIMutable>]
type TextOnlyConfig = {
    Enabled: bool
    OutputPath: string
}

[<CLIMutable>]
type CollectionConfig = {
    Enabled: bool
    StarterPacks: bool
}

[<CLIMutable>]
type IndieWebConfig = {
    Enabled: bool
    Webmentions: bool
    Microformats: bool
}

[<CLIMutable>]
type FeatureConfig = {
    Rss: RssConfig
    Tags: TagConfig
    Search: SearchConfig
    Timeline: TimelineConfig
    TextOnlyVersion: TextOnlyConfig
    Collections: CollectionConfig
    IndieWeb: IndieWebConfig
}

[<CLIMutable>]
type ThemeConfig = {
    Name: string
    CssFramework: string
    ViewEngine: string
    CustomCss: string array
    CustomJs: string array
}

[<CLIMutable>]
type BuildConfig = {
    CleanOutput: bool
    CopyStaticFiles: bool
    EnableValidation: bool
    Parallel: bool
}

[<CLIMutable>]
type SiteConfiguration = {
    Site: SiteInfo
    Directories: DirectoryConfig
    ContentTypes: ContentTypeConfig array
    Plugins: PluginConfig
    Features: FeatureConfig
    Theme: ThemeConfig
    Build: BuildConfig
}

// =============================================================================
// Configuration Loading and Validation
// =============================================================================

/// Load site configuration from JSON file
let loadSiteConfiguration (configPath: string) : Result<SiteConfiguration, string> =
    try
        if not (File.Exists(configPath)) then
            Error $"Configuration file not found: {configPath}"
        else
            let json = File.ReadAllText(configPath)
            let options = JsonSerializerOptions()
            options.PropertyNameCaseInsensitive <- true
            options.ReadCommentHandling <- JsonCommentHandling.Skip
            let config = JsonSerializer.Deserialize<SiteConfiguration>(json, options)
            Ok config
    with
    | ex -> Error $"Failed to load configuration: {ex.Message}"

/// Validate configuration for required fields and dependencies
let validateConfiguration (config: SiteConfiguration) : Result<unit, string list> =
    let errors = ResizeArray<string>()
    
    // Validate site information
    if String.IsNullOrWhiteSpace(config.Site.Title) then
        errors.Add("Site title is required")
    if String.IsNullOrWhiteSpace(config.Site.BaseUrl) then
        errors.Add("Site base URL is required")
    
    // Validate directories exist or can be created
    let validateDirectory path name =
        if String.IsNullOrWhiteSpace(path) then
            errors.Add($"{name} directory path is required")
        elif not (Directory.Exists(path)) then
            try
                Directory.CreateDirectory(path) |> ignore
            with
            | ex -> errors.Add($"Cannot create {name} directory '{path}': {ex.Message}")
    
    validateDirectory config.Directories.Source "Source"
    validateDirectory config.Directories.Data "Data"
    
    // Validate content types have valid processors
    for contentType in config.ContentTypes do
        if contentType.Enabled && String.IsNullOrWhiteSpace(contentType.Processor) then
            errors.Add($"Content type '{contentType.Name}' requires a processor when enabled")
    
    // Validate theme configuration
    if String.IsNullOrWhiteSpace(config.Theme.ViewEngine) then
        errors.Add("Theme view engine is required")
    
    if errors.Count > 0 then
        Error (List.ofSeq errors)
    else
        Ok ()

/// Get enabled content types from configuration
let getEnabledContentTypes (config: SiteConfiguration) : ContentTypeConfig array =
    config.ContentTypes |> Array.filter (fun ct -> ct.Enabled)

/// Get enabled custom blocks from configuration  
let getEnabledCustomBlocks (config: SiteConfiguration) : CustomBlockConfig array =
    config.Plugins.CustomBlocks |> Array.filter (fun cb -> cb.Enabled)

/// Check if a feature is enabled
let isFeatureEnabled (config: SiteConfiguration) (featureName: string) : bool =
    match featureName.ToLowerInvariant() with
    | "rss" -> config.Features.Rss.Enabled
    | "tags" -> config.Features.Tags.Enabled
    | "search" -> config.Features.Search.Enabled
    | "timeline" -> config.Features.Timeline.Enabled
    | "textonlyversion" -> config.Features.TextOnlyVersion.Enabled
    | "collections" -> config.Features.Collections.Enabled
    | "indieweb" -> config.Features.IndieWeb.Enabled
    | _ -> false

/// Get output path for content type
let getContentTypeOutputPath (config: SiteConfiguration) (contentType: ContentTypeConfig) (slug: string) : string =
    let pattern = contentType.UrlPattern.Replace("{slug}", slug)
    Path.Combine(config.Directories.Output, pattern.TrimStart('/'))

/// Create default configuration for new sites
let createDefaultConfiguration () : SiteConfiguration =
    {
        Site = {
            Title = "My Site"
            Description = "A static site built with SSG.Core"
            BaseUrl = "https://example.com"
            Author = {
                Name = "Site Author"
                Email = "author@example.com"
                Url = "https://example.com"
            }
        }
        Directories = {
            Source = "_src"
            Output = "_public"
            Data = "Data"
            Assets = "_src/assets"
        }
        ContentTypes = [|
            {
                Name = "posts"
                SourceDirectory = "posts"
                Processor = "PostProcessor"
                Enabled = true
                UrlPattern = "/posts/{slug}/"
                FeedEnabled = true
                ArchiveEnabled = true
            }
        |]
        Plugins = {
            CustomBlocks = [||]
            MarkdownExtensions = [|"UsePipeTables"; "UseGenericAttributes"; "UseAutoLinks"|]
        }
        Features = {
            Rss = { Enabled = true; Unified = true; TagFeeds = false; ItemLimit = 20 }
            Tags = { Enabled = true; GeneratePages = true; CloudEnabled = false }
            Search = { Enabled = false; GenerateIndex = false; IncludeContent = false }
            Timeline = { Enabled = false; ProgressiveLoading = false; InitialItems = 50; ChunkSize = 25 }
            TextOnlyVersion = { Enabled = false; OutputPath = "text" }
            Collections = { Enabled = false; StarterPacks = false }
            IndieWeb = { Enabled = false; Webmentions = false; Microformats = false }
        }
        Theme = {
            Name = "default"
            CssFramework = "bootstrap"
            ViewEngine = "giraffe"
            CustomCss = [||]
            CustomJs = [||]
        }
        Build = {
            CleanOutput = true
            CopyStaticFiles = true
            EnableValidation = false
            Parallel = false
        }
    }