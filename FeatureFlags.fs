module FeatureFlags

open System

/// Content types that can be migrated to new processors
type ContentType = 
    | Snippets 
    | Wiki 
    | Presentations 
    | Books 
    | Posts 
    | Notes
    | Responses 
    | Albums

/// Feature flag configuration for a content type
type FeatureFlag = {
    ContentType: ContentType
    Enabled: bool
    EnvironmentVariable: string
}

/// Get the environment variable name for a content type
let getEnvironmentVariable contentType =
    match contentType with
    | Snippets -> "NEW_SNIPPETS"
    | Wiki -> "NEW_WIKI"
    | Presentations -> "NEW_PRESENTATIONS"
    | Books -> "NEW_BOOKS"
    | Posts -> "NEW_POSTS"
    | Notes -> "NEW_NOTES"
    | Responses -> "NEW_RESPONSES"
    | Albums -> "NEW_ALBUMS"

/// Parse boolean value from environment variable
let parseBooleanFlag (value: string) =
    match value with
    | null | "" -> false
    | v when v.ToLowerInvariant() = "true" -> true
    | v when v.ToLowerInvariant() = "false" -> false
    | v when v = "1" -> true
    | v when v = "0" -> false
    | _ -> false

/// Get feature flag status for a content type with migration-specific defaults
let isEnabled contentType =
    let envVar = getEnvironmentVariable contentType
    let value = Environment.GetEnvironmentVariable(envVar)
    
    // Handle explicit environment variable values
    match value with
    | null | "" -> 
        // Default values for migrated content types
        match contentType with
        | Snippets -> true       // Snippets migration complete - default to new processor
        | Wiki -> true           // Wiki migration complete - default to new processor
        | Presentations -> true  // Presentations migration complete - default to new processor
        | Books -> true          // Books migration complete - default to new processor
        | Notes -> true          // Notes migration complete - default to new processor
        | Responses -> true      // Responses migration complete - default to new processor
        | _ -> false             // Other content types default to old processors
    | _ -> parseBooleanFlag value

/// Get all feature flags with their current status
let getAllFlags () =
    [
        { ContentType = Snippets; Enabled = isEnabled Snippets; EnvironmentVariable = getEnvironmentVariable Snippets }
        { ContentType = Wiki; Enabled = isEnabled Wiki; EnvironmentVariable = getEnvironmentVariable Wiki }
        { ContentType = Presentations; Enabled = isEnabled Presentations; EnvironmentVariable = getEnvironmentVariable Presentations }
        { ContentType = Books; Enabled = isEnabled Books; EnvironmentVariable = getEnvironmentVariable Books }
        { ContentType = Posts; Enabled = isEnabled Posts; EnvironmentVariable = getEnvironmentVariable Posts }
        { ContentType = Notes; Enabled = isEnabled Notes; EnvironmentVariable = getEnvironmentVariable Notes }
        { ContentType = Responses; Enabled = isEnabled Responses; EnvironmentVariable = getEnvironmentVariable Responses }
        { ContentType = Albums; Enabled = isEnabled Albums; EnvironmentVariable = getEnvironmentVariable Albums }
    ]

/// Validate feature flag configuration
let validateConfiguration () =
    let flags = getAllFlags()
    let enabledFlags = flags |> List.filter (fun f -> f.Enabled)
    
    if enabledFlags.IsEmpty then
        Ok "All feature flags disabled - using existing processors"
    else
        let enabledTypes = enabledFlags |> List.map (fun f -> f.ContentType.ToString()) |> String.concat ", "
        Ok $"Feature flags enabled for: {enabledTypes}"

/// Get debug information about current feature flag status
let getDebugInfo () =
    let flags = getAllFlags()
    flags 
    |> List.map (fun f -> 
        let status = if f.Enabled then "ENABLED" else "disabled"
        $"{f.EnvironmentVariable}={status}")
    |> String.concat "; "

/// Print feature flag status to console (for debugging)
let printStatus () =
    printfn "Feature Flag Status:"
    let flags = getAllFlags()
    flags |> List.iter (fun f ->
        let status = if f.Enabled then "✅ ENABLED" else "❌ disabled"
        printfn $"  {f.EnvironmentVariable}: {status}")
    printfn ""
