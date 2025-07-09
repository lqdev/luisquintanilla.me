// Simple test to validate basic build with feature flags
#r "../bin/Debug/net9.0/PersonalSite.dll"

open System
open FeatureFlags

printfn "=== Simple Feature Flag Test ==="

// Test old system
Environment.SetEnvironmentVariable("NEW_SNIPPETS", "false")
printfn "OLD system - NEW_SNIPPETS=false, Feature flag: %b" (isEnabled ContentType.Snippets)

// Test new system  
Environment.SetEnvironmentVariable("NEW_SNIPPETS", "true")
printfn "NEW system - NEW_SNIPPETS=true, Feature flag: %b" (isEnabled ContentType.Snippets)

printfn "Feature flag test complete"
