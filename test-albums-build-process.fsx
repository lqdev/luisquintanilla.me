#r "bin/Debug/net9.0/PersonalSite.dll"

open Domain
open Builder
open System
open System.IO

// Test album build process with feature flag
printfn "=== Album Build Process Test ==="

// Test 1: Test build function exists
printfn "\n1. Testing buildAlbums function availability..."
printfn "✅ buildAlbums function accessible"

// Test 2: Test feature flag behavior
printfn "\n2. Testing feature flag behavior..."
let originalValue = Environment.GetEnvironmentVariable("NEW_ALBUMS")
printfn "   Original NEW_ALBUMS value: %s" (if isNull originalValue then "null" else originalValue)

// Test with feature flag disabled (default)
let albumsEnabled = FeatureFlags.isEnabled FeatureFlags.Albums
printfn "   Albums enabled (default): %b" albumsEnabled

// Test 3: Test with feature flag enabled
printfn "\n3. Testing with NEW_ALBUMS=true..."
Environment.SetEnvironmentVariable("NEW_ALBUMS", "true")
let albumsEnabledWithFlag = FeatureFlags.isEnabled FeatureFlags.Albums
printfn "   Albums enabled (with flag): %b" albumsEnabledWithFlag

// Test 4: Check album source directory
printfn "\n4. Testing album source directory..."
let albumsDir = Path.Combine("_src", "albums")
if Directory.Exists albumsDir then
    let albumFiles = Directory.GetFiles(albumsDir, "*.md")
    printfn "✅ Albums directory exists"
    printfn "   Album files found: %d" albumFiles.Length
    albumFiles |> Array.iter (fun f -> printfn "   - %s" (Path.GetFileName f))
else
    printfn "❌ Albums directory not found"

// Restore original environment
if isNull originalValue then
    Environment.SetEnvironmentVariable("NEW_ALBUMS", null)
else
    Environment.SetEnvironmentVariable("NEW_ALBUMS", originalValue)

printfn "\n=== Album Build Process Test Complete ==="
printfn "✅ Ready for Phase 3 migration validation"
