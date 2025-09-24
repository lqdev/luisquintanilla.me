module SSG.Core.PluginRegistry

open System
open System.Collections.Generic
open System.IO
open System.Reflection
open System.Xml.Linq
open SSG.Core.Configuration

// =============================================================================
// Plugin Interface Definitions
// =============================================================================

/// Interface for content processors that can be registered as plugins
type IContentProcessor<'T> =
    abstract member Name: string
    abstract member Parse: string -> 'T option
    abstract member RenderHtml: 'T -> string
    abstract member RenderCard: 'T -> string  
    abstract member GenerateRssItem: 'T -> XElement option
    abstract member GetOutputPath: 'T -> string
    abstract member GetSlug: 'T -> string
    abstract member GetTags: 'T -> string array
    abstract member GetDate: 'T -> string

/// Interface for custom block processors
type ICustomBlockProcessor =
    abstract member Name: string
    abstract member ParseBlock: string -> obj list
    abstract member RenderHtml: obj -> string
    abstract member GetBlockType: unit -> Type

/// Interface for view plugins that can render different layouts
type IViewPlugin =
    abstract member Name: string
    abstract member RenderLayout: content:string -> metadata:Map<string, obj> -> string
    abstract member SupportedLayouts: string array

/// Interface for build pipeline plugins that can hook into the build process
type IBuildPlugin = 
    abstract member Name: string
    abstract member PreBuild: config:SiteConfiguration -> unit
    abstract member PostBuild: config:SiteConfiguration -> unit
    abstract member ProcessContent: contentType:string -> content:obj -> obj

// =============================================================================
// Plugin Registry Implementation
// =============================================================================

/// Plugin registry for managing all registered plugins
type PluginRegistry() =
    let contentProcessors = Dictionary<string, obj>()
    let customBlockProcessors = Dictionary<string, ICustomBlockProcessor>()
    let viewPlugins = Dictionary<string, IViewPlugin>()
    let buildPlugins = List<IBuildPlugin>()
    
    /// Register a content processor for a specific content type
    member this.RegisterContentProcessor<'T>(processor: IContentProcessor<'T>) =
        contentProcessors.[processor.Name] <- processor :> obj
        
    /// Get content processor by name
    member this.GetContentProcessor<'T>(name: string) : IContentProcessor<'T> option =
        match contentProcessors.TryGetValue(name) with
        | true, processor -> Some (processor :?> IContentProcessor<'T>)
        | false, _ -> None
        
    /// Register a custom block processor
    member this.RegisterCustomBlockProcessor(processor: ICustomBlockProcessor) =
        customBlockProcessors.[processor.Name] <- processor
        
    /// Get custom block processor by name
    member this.GetCustomBlockProcessor(name: string) : ICustomBlockProcessor option =
        match customBlockProcessors.TryGetValue(name) with
        | true, processor -> Some processor
        | false, _ -> None
        
    /// Register a view plugin
    member this.RegisterViewPlugin(plugin: IViewPlugin) =
        viewPlugins.[plugin.Name] <- plugin
        
    /// Get view plugin by name
    member this.GetViewPlugin(name: string) : IViewPlugin option =
        match viewPlugins.TryGetValue(name) with
        | true, plugin -> Some plugin
        | false, _ -> None
        
    /// Register a build plugin
    member this.RegisterBuildPlugin(plugin: IBuildPlugin) =
        buildPlugins.Add(plugin)
        
    /// Get all build plugins
    member this.GetBuildPlugins() : IBuildPlugin list =
        List.ofSeq buildPlugins
        
    /// Get all registered content processor names
    member this.GetContentProcessorNames() : string list =
        contentProcessors.Keys |> List.ofSeq
        
    /// Get all registered custom block processor names
    member this.GetCustomBlockProcessorNames() : string list =
        customBlockProcessors.Keys |> List.ofSeq

// =============================================================================
// Plugin Discovery and Loading
// =============================================================================

/// Discover and load plugins from assemblies in specified directory
let discoverPlugins (pluginDirectory: string) (registry: PluginRegistry) : Result<unit, string list> =
    let errors = ResizeArray<string>()
    
    try
        if Directory.Exists(pluginDirectory) then
            let assemblyFiles = Directory.GetFiles(pluginDirectory, "*.dll")
            
            for assemblyFile in assemblyFiles do
                try
                    let assembly = Assembly.LoadFrom(assemblyFile)
                    
                    // Find content processor implementations
                    let contentProcessorTypes = 
                        assembly.GetTypes()
                        |> Array.filter (fun t -> 
                            t.GetInterfaces() 
                            |> Array.exists (fun i -> 
                                i.IsGenericType && 
                                i.GetGenericTypeDefinition() = typedefof<IContentProcessor<_>>))
                    
                    // Register content processors
                    for processorType in contentProcessorTypes do
                        try
                            let processor = Activator.CreateInstance(processorType)
                            // Note: This would need more sophisticated type handling in practice
                            // For now, assuming processors can be cast appropriately
                            match processor with
                            | :? obj as p -> 
                                let name = processorType.Name
                                printfn $"Discovered content processor: {name}"
                        with
                        | ex -> errors.Add($"Failed to instantiate content processor {processorType.Name}: {ex.Message}")
                    
                    // Find custom block processor implementations
                    let blockProcessorTypes = 
                        assembly.GetTypes()
                        |> Array.filter (fun t -> 
                            typeof<ICustomBlockProcessor>.IsAssignableFrom(t) && not t.IsAbstract)
                    
                    // Register custom block processors
                    for processorType in blockProcessorTypes do
                        try
                            let processor = Activator.CreateInstance(processorType) :?> ICustomBlockProcessor
                            registry.RegisterCustomBlockProcessor(processor)
                            printfn $"Registered custom block processor: {processor.Name}"
                        with
                        | ex -> errors.Add($"Failed to register custom block processor {processorType.Name}: {ex.Message}")
                    
                    // Find view plugin implementations
                    let viewPluginTypes = 
                        assembly.GetTypes()
                        |> Array.filter (fun t -> 
                            typeof<IViewPlugin>.IsAssignableFrom(t) && not t.IsAbstract)
                    
                    // Register view plugins
                    for pluginType in viewPluginTypes do
                        try
                            let plugin = Activator.CreateInstance(pluginType) :?> IViewPlugin
                            registry.RegisterViewPlugin(plugin)
                            printfn $"Registered view plugin: {plugin.Name}"
                        with
                        | ex -> errors.Add($"Failed to register view plugin {pluginType.Name}: {ex.Message}")
                    
                    // Find build plugin implementations
                    let buildPluginTypes = 
                        assembly.GetTypes()
                        |> Array.filter (fun t -> 
                            typeof<IBuildPlugin>.IsAssignableFrom(t) && not t.IsAbstract)
                    
                    // Register build plugins
                    for pluginType in buildPluginTypes do
                        try
                            let plugin = Activator.CreateInstance(pluginType) :?> IBuildPlugin
                            registry.RegisterBuildPlugin(plugin)
                            printfn $"Registered build plugin: {plugin.Name}"
                        with
                        | ex -> errors.Add($"Failed to register build plugin {pluginType.Name}: {ex.Message}")
                        
                with
                | ex -> errors.Add($"Failed to load assembly {assemblyFile}: {ex.Message}")
        else
            // Plugin directory doesn't exist - this is okay, just no plugins to load
            printfn $"Plugin directory {pluginDirectory} does not exist - no plugins loaded"
    with
    | ex -> errors.Add($"Failed to discover plugins: {ex.Message}")
    
    if errors.Count > 0 then
        Error (List.ofSeq errors)
    else
        Ok ()

// =============================================================================
// Configuration-Based Plugin Registration
// =============================================================================

/// Register plugins based on site configuration
let registerConfiguredPlugins (config: SiteConfiguration) (registry: PluginRegistry) : Result<unit, string list> =
    let errors = ResizeArray<string>()
    
    // Register content type processors based on configuration
    for contentType in getEnabledContentTypes config do
        // In the actual implementation, this would need to resolve the processor
        // from the configuration and register it appropriately
        printfn $"Content type '{contentType.Name}' configured with processor '{contentType.Processor}'"
    
    // Register custom blocks based on configuration
    for customBlock in getEnabledCustomBlocks config do
        printfn $"Custom block '{customBlock.Name}' configured with parser '{customBlock.Parser}'"
    
    if errors.Count > 0 then
        Error (List.ofSeq errors)
    else
        Ok ()

/// Initialize plugin registry with configuration and discovery
let initializePluginRegistry (config: SiteConfiguration) (pluginDirectory: string option) : Result<PluginRegistry, string list> =
    let registry = PluginRegistry()
    let errors = ResizeArray<string>()
    
    // Register configured plugins
    match registerConfiguredPlugins config registry with
    | Ok () -> ()
    | Error configErrors -> errors.AddRange(configErrors)
    
    // Discover additional plugins from directory
    match pluginDirectory with
    | Some dir when Directory.Exists(dir) ->
        match discoverPlugins dir registry with
        | Ok () -> ()
        | Error discoveryErrors -> errors.AddRange(discoveryErrors)
    | Some dir ->
        printfn $"Plugin directory {dir} does not exist - skipping plugin discovery"
    | None ->
        printfn "No plugin directory specified - skipping plugin discovery"
    
    if errors.Count > 0 then
        Error (List.ofSeq errors)
    else
        Ok registry

// =============================================================================
// Global Registry Instance
// =============================================================================

/// Global plugin registry instance (initialized during startup)
let mutable private globalRegistry: PluginRegistry option = None

/// Get the global plugin registry
let getGlobalRegistry() : PluginRegistry =
    match globalRegistry with
    | Some registry -> registry
    | None -> failwith "Plugin registry not initialized. Call initializeGlobalRegistry first."

/// Initialize the global plugin registry
let initializeGlobalRegistry (config: SiteConfiguration) (pluginDirectory: string option) : Result<unit, string list> =
    match initializePluginRegistry config pluginDirectory with
    | Ok registry -> 
        globalRegistry <- Some registry
        Ok ()
    | Error errors -> Error errors