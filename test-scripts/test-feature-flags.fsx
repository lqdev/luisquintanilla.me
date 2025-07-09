#r "../bin/Debug/net9.0/PersonalSite.dll"

open FeatureFlags
open System

printfn "=== Feature Flag System Test ==="
printfn ""

// Test 1: Basic functionality with no environment variables
printfn "Test 1: Default state (no environment variables)"
printfn "Expected: All flags should be disabled"
FeatureFlags.printStatus()

// Test 2: Validation
printfn "Test 2: Configuration validation"
match FeatureFlags.validateConfiguration() with
| Ok message -> printfn $"✅ Validation passed: {message}"
| Error error -> printfn $"❌ Validation failed: {error}"
printfn ""

// Test 3: Individual flag checking
printfn "Test 3: Individual flag checking"
let testTypes = [
    ContentType.Snippets
    ContentType.Wiki
    ContentType.Presentations
    ContentType.Books
    ContentType.Posts
    ContentType.Responses
    ContentType.Albums
]

testTypes |> List.iter (fun contentType ->
    let enabled = FeatureFlags.isEnabled contentType
    let envVar = FeatureFlags.getEnvironmentVariable contentType
    let status = if enabled then "ENABLED" else "disabled"
    printfn $"  {envVar}: {status}")
printfn ""

// Test 4: Debug information
printfn "Test 4: Debug information"
let debugInfo = FeatureFlags.getDebugInfo()
printfn $"Debug info: {debugInfo}"
printfn ""

// Test 5: Set some environment variables and test
printfn "Test 5: Setting environment variables for testing"
Environment.SetEnvironmentVariable("NEW_SNIPPETS", "true")
Environment.SetEnvironmentVariable("NEW_WIKI", "1")
Environment.SetEnvironmentVariable("NEW_POSTS", "false")
Environment.SetEnvironmentVariable("NEW_INVALID", "maybe")

printfn "Set NEW_SNIPPETS=true, NEW_WIKI=1, NEW_POSTS=false, NEW_INVALID=maybe"
printfn "Expected: Snippets and Wiki enabled, Posts disabled, others disabled"
FeatureFlags.printStatus()

// Test 6: Validation with flags enabled
printfn "Test 6: Validation with some flags enabled"
match FeatureFlags.validateConfiguration() with
| Ok message -> printfn $"✅ Validation passed: {message}"
| Error error -> printfn $"❌ Validation failed: {error}"
printfn ""

// Test 7: Individual flag tests
printfn "Test 7: Individual flag verification"
[
    (ContentType.Snippets, true, "Should be enabled (NEW_SNIPPETS=true)")
    (ContentType.Wiki, true, "Should be enabled (NEW_WIKI=1)")
    (ContentType.Posts, false, "Should be disabled (NEW_POSTS=false)")
    (ContentType.Books, false, "Should be disabled (no environment variable)")
] |> List.iter (fun (contentType, expected, description) ->
    let actual = FeatureFlags.isEnabled contentType
    let result = if actual = expected then "✅ PASS" else "❌ FAIL"
    printfn $"  {result}: {description} - Actual: {actual}")

printfn ""
printfn "=== Feature Flag System Test Complete ==="

// Clean up environment variables for production
Environment.SetEnvironmentVariable("NEW_SNIPPETS", null)
Environment.SetEnvironmentVariable("NEW_WIKI", null)
Environment.SetEnvironmentVariable("NEW_POSTS", null)
Environment.SetEnvironmentVariable("NEW_INVALID", null)

printfn ""
printfn "Environment variables cleaned up for production use"
