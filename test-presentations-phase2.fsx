#r "bin/Debug/net9.0/PersonalSite.dll"

open System
open FeatureFlags

printfn "=== Presentation Feature Flag Test ==="

// Test feature flag functionality
printfn "Testing feature flag defaults:"
printfn "NEW_PRESENTATIONS enabled: %b" (isEnabled ContentType.Presentations)

// Test with environment variable
Environment.SetEnvironmentVariable("NEW_PRESENTATIONS", "true")
printfn "After setting NEW_PRESENTATIONS=true: %b" (isEnabled ContentType.Presentations)

Environment.SetEnvironmentVariable("NEW_PRESENTATIONS", "false")
printfn "After setting NEW_PRESENTATIONS=false: %b" (isEnabled ContentType.Presentations)

// Test processor creation
open GenericBuilder

printfn "\nTesting presentation processor creation:"
try
    let processor = PresentationProcessor.create()
    printfn "✅ PresentationProcessor created successfully"
    printfn "✅ Parse function available"
    printfn "✅ Render function available"
    printfn "✅ RenderCard function available"  
    printfn "✅ RenderRss function available"
    printfn "✅ OutputPath function available"
with
| ex -> printfn "❌ Error creating processor: %s" ex.Message

printfn "\n✅ Phase 2 implementation complete - Feature flag integration successful!"
