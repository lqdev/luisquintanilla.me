#r "../bin/Debug/net9.0/PersonalSite.dll"

open MigrationUtils
open FeatureFlags
open System

printfn "=== Migration Utils Test ==="
printfn ""

// Test 1: Migration progress tracking
printfn "Test 1: Migration progress tracking"
MigrationUtils.printMigrationProgress()

// Test 2: Generate migration guides
printfn "Test 2: Generate migration guides"
let contentTypes = [
    ContentType.Snippets
    ContentType.Wiki
    ContentType.Posts
]

contentTypes |> List.iter (fun contentType ->
    let fileName = MigrationUtils.saveMigrationGuide contentType
    printfn $"Generated guide for {contentType}")

printfn ""

// Test 3: Test with feature flags enabled
printfn "Test 3: Testing with feature flags enabled"
Environment.SetEnvironmentVariable("NEW_SNIPPETS", "true")
Environment.SetEnvironmentVariable("NEW_WIKI", "true")

printfn "Enabled NEW_SNIPPETS and NEW_WIKI flags"
MigrationUtils.printMigrationProgress()

// Test 4: Validation simulation
printfn "Test 4: Migration validation simulation"
let testValidation contentType =
    printfn $"Testing validation for {contentType}:"
    let result = MigrationUtils.validateMigration contentType
    printfn $"Validation result: {result}"
    printfn ""

[ContentType.Snippets; ContentType.Wiki; ContentType.Posts] 
|> List.iter testValidation

// Test 5: Migration status details
printfn "Test 5: Migration status details"
let progress = MigrationUtils.getMigrationProgress()
printfn $"Summary: {progress.EnabledFlags}/{progress.TotalContentTypes} content types have feature flags enabled"
printfn $"Progress: {progress.ValidatedMigrations}/{progress.TotalContentTypes} migrations validated"
printfn ""

// Cleanup
Environment.SetEnvironmentVariable("NEW_SNIPPETS", null)
Environment.SetEnvironmentVariable("NEW_WIKI", null)

printfn "=== Migration Utils Test Complete ==="
