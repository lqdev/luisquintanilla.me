module StarterPackSystem

open System
open System.IO
open System.Text.Json
open Giraffe.ViewEngine

// =============================================================================
// Domain Types - Composable and Extensible
// =============================================================================

/// Represents the metadata for a starter pack collection
type StarterPackMetadata = {
    Id: string
    Title: string
    Description: string
    Category: string option
    Tags: string list
    Author: string option
    Created: DateTime option
    Updated: DateTime option
    Featured: bool
    SortOrder: int option
}

/// Represents the configuration for a starter pack
type StarterPackConfig = {
    Metadata: StarterPackMetadata
    DataFile: string
    OutputPath: string
    UrlPath: string
    OpmlFilename: string option
}

/// Represents a complete starter pack with data
type StarterPack = {
    Config: StarterPackConfig
    Links: Outline array
}

/// Starter pack categories for organization
type StarterPackCategory =
    | Technology
    | AI
    | Development
    | Content
    | Community
    | Custom of string

/// Collection of all starter packs
type StarterPackRegistry = {
    Packs: Map<string, StarterPack>
    Categories: Map<string, StarterPackConfig list>
    Featured: StarterPackConfig list
}

// =============================================================================
// Configuration Loading and Validation
// =============================================================================

/// Load starter pack configurations from a central registry file
let loadStarterPackConfigs () : StarterPackConfig list =
    let configPath = Path.Join("Data", "starter-packs-registry.json")
    
    if File.Exists(configPath) then
        let json = File.ReadAllText(configPath)
        JsonSerializer.Deserialize<StarterPackConfig list>(json)
    else
        // Fallback to existing individual collections for migration
        [
            {
                Metadata = {
                    Id = "ai"
                    Title = "AI Starter Pack"
                    Description = "A curated collection of AI resources including blogs, podcasts, and research publications to stay on top of AI developments."
                    Category = Some "Technology"
                    Tags = ["ai"; "machine-learning"; "deep-learning"; "research"]
                    Author = Some "Luis Quintanilla"
                    Created = Some (DateTime(2024, 1, 1))
                    Updated = None
                    Featured = true
                    SortOrder = Some 1
                }
                DataFile = "ai-starter-pack.json"
                OutputPath = "collections/starter-packs/ai"
                UrlPath = "/collections/starter-packs/ai"
                OpmlFilename = Some "index.opml"
            }
            // Additional packs can be added here or migrated from registry file
        ]

/// Validate that data files exist for all configured starter packs
let validateStarterPackConfigs (configs: StarterPackConfig list) : Result<StarterPackConfig list, string> =
    let missingFiles = 
        configs 
        |> List.filter (fun config -> 
            let dataPath = Path.Join("Data", config.DataFile)
            not (File.Exists(dataPath)))
        |> List.map (fun config -> config.DataFile)
    
    if missingFiles.IsEmpty then
        Ok configs
    else
        Error $"Missing data files: {String.Join(", ", missingFiles)}"

// =============================================================================
// Data Loading and Processing
// =============================================================================

/// Load links for a specific starter pack
let loadStarterPackLinks (config: StarterPackConfig) : Result<Outline array, string> =
    try
        let dataPath = Path.Join("Data", config.DataFile)
        let json = File.ReadAllText(dataPath)
        let links = JsonSerializer.Deserialize<Outline array>(json)
        Ok links
    with
    | ex -> Error $"Failed to load {config.DataFile}: {ex.Message}"

/// Load all starter packs with their data
let loadAllStarterPacks () : Result<StarterPack list, string> =
    result {
        let! configs = loadStarterPackConfigs () |> Ok
        let! validatedConfigs = validateStarterPackConfigs configs
        
        let packs = 
            validatedConfigs
            |> List.map (fun config ->
                result {
                    let! links = loadStarterPackLinks config
                    return {
                        Config = config
                        Links = links
                    }
                })
        
        // Collect all results and return first error if any
        let errors = packs |> List.choose (function | Error e -> Some e | Ok _ -> None)
        if errors.IsEmpty then
            let successfulPacks = packs |> List.choose (function | Ok p -> Some p | Error _ -> None)
            return successfulPacks
        else
            return! Error (String.Join("; ", errors))
    }

/// Create a registry from loaded starter packs
let createStarterPackRegistry (packs: StarterPack list) : StarterPackRegistry =
    let packsMap = 
        packs 
        |> List.map (fun pack -> pack.Config.Metadata.Id, pack)
        |> Map.ofList
    
    let categoriesMap =
        packs
        |> List.groupBy (fun pack -> 
            pack.Config.Metadata.Category |> Option.defaultValue "Uncategorized")
        |> List.map (fun (category, categoryPacks) -> 
            category, categoryPacks |> List.map (fun p -> p.Config))
        |> Map.ofList
    
    let featured = 
        packs 
        |> List.filter (fun pack -> pack.Config.Metadata.Featured)
        |> List.map (fun pack -> pack.Config)
        |> List.sortBy (fun config -> config.Metadata.SortOrder |> Option.defaultValue 999)
    
    {
        Packs = packsMap
        Categories = categoriesMap
        Featured = featured
    }

// =============================================================================
// View Generation - Composable and Template-Based
// =============================================================================

/// Generate the HTML view for a starter pack page
let generateStarterPackView (pack: StarterPack) =
    let metadata = pack.Config.Metadata
    let links = pack.Links
    
    // Use existing rollLinkView for consistency
    let linkContent = FeedViews.rollLinkView links
    
    div [ _class "mr-auto" ] [
        h2 [] [ Text metadata.Title ]
        p [] [ Text metadata.Description ]
        
        // Show tags if any
        if not metadata.Tags.IsEmpty then
            p [ _class "starter-pack-tags" ] [
                Text "Topics: "
                for i, tag in List.indexed metadata.Tags do
                    if i > 0 then Text ", "
                    span [ _class "tag" ] [ Text tag ]
            ]
        
        // OPML download link
        let opmlFilename = pack.Config.OpmlFilename |> Option.defaultValue "index.opml"
        p [] [
            Text "You can subscribe to any of the individual feeds using the RSS feed links below. Want to subscribe to all of them? Use the "
            a [ _href $"{pack.Config.UrlPath}/{opmlFilename}" ] [ Text "OPML file" ]
            Text " if your RSS reader supports "
            a [ _href "http://opml.org/" ] [ Text "OPML" ]
            Text "."
        ]
        
        linkContent
    ]

/// Generate OPML feed for a starter pack
let generateStarterPackOpml (pack: StarterPack) =
    let metadata = pack.Config.Metadata
    Builder.buildOpmlFeed metadata.Title "https://www.lqdev.me" pack.Links

/// Generate the main starter packs index page
let generateStarterPacksIndexView (registry: StarterPackRegistry) =
    div [ _class "mr-auto" ] [
        h1 [] [ Text "Starter Packs" ]
        p [] [
            Text "Welcome to my curated collection of RSS Starter Packs."
        ]
        p [] [
            Text "Inspired by "
            a [ _href "https://bsky.social/about/blog/06-26-2024-starter-packs" ] [ Text "BlueSky's Starter Pack feature" ]
            Text ", I've created a set of OPML bundles to help you discover and follow content across the open web. "
            Text "Whether it's a newsletter, blog, podcast, YouTube channel, Fediverse instance, or BlueSky profile—if it has an RSS feed and aligns with the topic, it's included."
        ]
        
        h2 [] [ Text "Featured Packs" ]
        ul [] [
            for pack in registry.Featured do
                li [] [
                    a [ _href pack.UrlPath ] [ Text pack.Metadata.Title ]
                    Text " - "
                    Text pack.Metadata.Description
                ]
        ]
        
        if registry.Categories.Count > 1 then
            h2 [] [ Text "By Category" ]
            for category in registry.Categories do
                h3 [] [ Text category.Key ]
                ul [] [
                    for pack in category.Value do
                        li [] [
                            a [ _href pack.UrlPath ] [ Text pack.Metadata.Title ]
                            Text " - "
                            Text pack.Metadata.Description
                        ]
                ]
    ]

// =============================================================================
// Build Pipeline - Automated and Consistent
// =============================================================================

/// Build a single starter pack (HTML page and OPML file)
let buildStarterPack (outputDir: string) (pack: StarterPack) =
    let config = pack.Config
    
    // Build HTML page
    let pageContent = generateStarterPackView pack
    let htmlPage = LayoutViews.generate pageContent "default" config.Metadata.Title
    
    let outputPath = Path.Join(outputDir, config.OutputPath)
    Directory.CreateDirectory(outputPath) |> ignore
    File.WriteAllText(Path.Join(outputPath, "index.html"), htmlPage)
    
    // Build OPML file
    let opmlFeed = generateStarterPackOpml pack
    let opmlFilename = config.OpmlFilename |> Option.defaultValue "index.opml"
    File.WriteAllText(Path.Join(outputPath, opmlFilename), opmlFeed.ToString())
    
    // Also create XML file for backward compatibility
    File.WriteAllText(Path.Join(outputPath, "index.xml"), opmlFeed.ToString())

/// Build the main starter packs index page
let buildStarterPacksIndex (outputDir: string) (registry: StarterPackRegistry) =
    let indexContent = generateStarterPacksIndexView registry
    let indexPage = LayoutViews.generate indexContent "default" "Starter Packs - Luis Quintanilla"
    
    let starterPacksDir = Path.Join(outputDir, "collections", "starter-packs")
    Directory.CreateDirectory(starterPacksDir) |> ignore
    File.WriteAllText(Path.Join(starterPacksDir, "index.html"), indexPage)

/// Build all starter packs using the unified pipeline
let buildAllStarterPacks (outputDir: string) =
    match loadAllStarterPacks () with
    | Ok packs ->
        let registry = createStarterPackRegistry packs
        
        // Build individual starter pack pages
        for pack in packs do
            buildStarterPack outputDir pack
        
        // Build main index page
        buildStarterPacksIndex outputDir registry
        
        printfn "✅ Built %d starter packs successfully" packs.Length
        Ok registry
    
    | Error error ->
        printfn "❌ Failed to build starter packs: %s" error
        Error error

// =============================================================================
// Migration and Extension Helpers
// =============================================================================

/// Create a new starter pack configuration template
let createStarterPackTemplate (id: string) (title: string) (description: string) =
    {
        Metadata = {
            Id = id
            Title = title
            Description = description
            Category = None
            Tags = []
            Author = Some "Luis Quintanilla"
            Created = Some DateTime.Now
            Updated = None
            Featured = false
            SortOrder = None
        }
        DataFile = $"{id}-starter-pack.json"
        OutputPath = $"collections/starter-packs/{id}"
        UrlPath = $"/collections/starter-packs/{id}"
        OpmlFilename = Some "index.opml"
    }

/// Generate the registry configuration file from existing packs
let generateRegistryConfig (configs: StarterPackConfig list) =
    let json = JsonSerializer.Serialize(configs, JsonSerializerOptions(WriteIndented = true))
    File.WriteAllText(Path.Join("Data", "starter-packs-registry.json"), json)
    printfn "✅ Generated starter packs registry configuration"
