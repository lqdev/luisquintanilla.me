module MigrationUtils

open System
open System.IO
open FeatureFlags
open OutputComparison

/// Migration status for a content type
type MigrationStatus = {
    ContentType: ContentType
    FeatureFlagEnabled: bool
    ValidationPassed: bool option
    LastValidated: DateTime option
    OutputDirectory: string
}

/// Migration progress summary
type MigrationProgress = {
    TotalContentTypes: int
    EnabledFlags: int
    ValidatedMigrations: int
    ReadyForProduction: int
    Statuses: MigrationStatus list
}

/// Get output directory for a content type
let getOutputDirectory contentType =
    match contentType with
    | Snippets -> "snippets"
    | Wiki -> "wiki"
    | Presentations -> "presentations"
    | Books -> "library"
    | Posts -> "posts"
    | Responses -> "feed/responses"
    | Albums -> "albums"

/// Create migration status for a content type
let createMigrationStatus contentType =
    {
        ContentType = contentType
        FeatureFlagEnabled = FeatureFlags.isEnabled contentType
        ValidationPassed = None
        LastValidated = None
        OutputDirectory = getOutputDirectory contentType
    }

/// Get current migration progress
let getMigrationProgress () =
    let allContentTypes = [
        ContentType.Snippets
        ContentType.Wiki
        ContentType.Presentations
        ContentType.Books
        ContentType.Posts
        ContentType.Responses
        ContentType.Albums
    ]
    
    let statuses = allContentTypes |> List.map createMigrationStatus
    let enabledFlags = statuses |> List.filter (fun s -> s.FeatureFlagEnabled) |> List.length
    let validatedMigrations = statuses |> List.filter (fun s -> s.ValidationPassed = Some true) |> List.length
    let readyForProduction = validatedMigrations // Same as validated for now
    
    {
        TotalContentTypes = allContentTypes.Length
        EnabledFlags = enabledFlags
        ValidatedMigrations = validatedMigrations
        ReadyForProduction = readyForProduction
        Statuses = statuses
    }

/// Print migration progress summary
let printMigrationProgress () =
    let progress = getMigrationProgress()
    
    printfn "=== Migration Progress Summary ==="
    printfn $"Content Types: {progress.TotalContentTypes}"
    printfn $"Feature Flags Enabled: {progress.EnabledFlags}"
    printfn $"Validated Migrations: {progress.ValidatedMigrations}"
    printfn $"Ready for Production: {progress.ReadyForProduction}"
    printfn ""
    
    printfn "Content Type Status:"
    progress.Statuses |> List.iter (fun status ->
        let flagStatus = if status.FeatureFlagEnabled then "✅ ENABLED" else "❌ disabled"
        let validationStatus = 
            match status.ValidationPassed with
            | Some true -> "✅ VALIDATED"
            | Some false -> "❌ FAILED"
            | None -> "⏳ pending"
        printfn $"  {status.ContentType}: {flagStatus} | {validationStatus}")
    printfn ""

/// Run parallel execution for a content type (simulation for now)
let runParallelComparison contentType oldOutputDir newOutputDir =
    printfn $"=== Parallel Comparison: {contentType} ==="
    printfn $"Comparing old vs new output for {contentType}"
    printfn $"Old output: {oldOutputDir}"
    printfn $"New output: {newOutputDir}"
    
    // For now, this is a placeholder that validates the framework
    // In actual implementation, this would:
    // 1. Run old processor, capture output to oldOutputDir
    // 2. Run new processor, capture output to newOutputDir
    // 3. Compare outputs using OutputComparison module
    // 4. Return validation results
    
    if Directory.Exists(oldOutputDir) && Directory.Exists(newOutputDir) then
        let summary = OutputComparison.compareOutputs oldOutputDir newOutputDir
        let isValid = OutputComparison.printSummary summary
        let validationResult = if isValid then "✅ PASS" else "❌ FAIL"
        printfn $"Migration validation for {contentType}: {validationResult}"
        isValid
    else
        printfn $"⚠️  Output directories not found for comparison"
        printfn $"   This would be handled by actual processor execution"
        false

/// Validate a content type migration
let validateMigration contentType =
    let outputDir = getOutputDirectory contentType
    let oldOutputDir = $"_validation_old/{outputDir}"
    let newOutputDir = $"_validation_new/{outputDir}"
    
    if FeatureFlags.isEnabled contentType then
        runParallelComparison contentType oldOutputDir newOutputDir
    else
        printfn $"⏭️  Skipping {contentType} - feature flag disabled"
        false

/// Generate migration workflow documentation
let generateMigrationGuide contentType =
    let envVar = FeatureFlags.getEnvironmentVariable contentType
    let outputDir = getOutputDirectory contentType
    
    [
        $"# Migration Guide: {contentType}"
        ""
        "## Prerequisites"
        "- Core Infrastructure Phase 1 complete"
        "- Feature Flag Infrastructure implemented"
        "- New processor implementation ready"
        ""
        "## Migration Steps"
        ""
        "### 1. Enable Feature Flag"
        $"```bash"
        $"export {envVar}=true"
        $"```"
        ""
        "### 2. Run Parallel Validation"
        "```bash"
        "dotnet run"
        "```"
        ""
        "### 3. Validate Output"
        $"Check `_public/{outputDir}` for correct generation"
        ""
        "### 4. Compare Results"
        "Use OutputComparison module to validate identical output"
        ""
        "### 5. Production Deployment"
        $"Set {envVar}=true in production environment"
        ""
        "## Rollback Procedure"
        $"Set {envVar}=false to revert to old processor"
        ""
        "## Troubleshooting"
        "- Check feature flag validation messages"
        "- Review output comparison differences"
        "- Verify new processor implementation"
    ]

/// Save migration guide to file
let saveMigrationGuide contentType =
    let guide = generateMigrationGuide contentType
    let fileName = $"docs/migration-guide-{contentType.ToString().ToLower()}.md"
    
    // Ensure docs directory exists
    let docsDir = "docs"
    if not (Directory.Exists(docsDir)) then
        Directory.CreateDirectory(docsDir) |> ignore
    
    File.WriteAllLines(fileName, guide)
    printfn $"✅ Migration guide saved to {fileName}"
    fileName
