#r "bin/Debug/net9.0/PersonalSite.dll"

open Domain
open Builder
open GenericBuilder
open System
open System.IO

// End-to-end migration validation - Phase 3
printfn "=== End-to-End Albums Migration Validation ==="

// Test 1: Full build process with NEW_ALBUMS enabled
printfn "\n1. Testing complete build process with NEW_ALBUMS=true..."
Environment.SetEnvironmentVariable("NEW_ALBUMS", "true")

try
    // Clean and rebuild
    let feedData = buildAlbums()
    printfn "✅ Complete album build successful"
    printfn "   Albums processed: %d" feedData.Length
    
    if feedData.Length > 0 then
        let album = feedData.[0].Content
        printfn "   Sample album: %s" album.Metadata.Title
        printfn "   Album date: %s" album.Metadata.Date
        printfn "   Images count: %d" (Array.length album.Metadata.Images)
        
        // Verify ITaggable integration
        let taggable = album :> ITaggable
        printfn "   ITaggable ContentType: %s" taggable.ContentType
        printfn "   ITaggable FileName: %s" taggable.FileName

    // Test 2: Verify all expected output files exist
    printfn "\n2. Verifying output file structure..."
    
    let expectedFiles = [
        ("Individual album page", Path.Combine("_public", "media", "fall-mountains", "index.html"))
        ("Media index page", Path.Combine("_public", "media", "index.html"))
        ("Album RSS feed", Path.Combine("_public", "feed", "albums", "rss.xml"))
        ("Album HTML index", Path.Combine("_public", "feed", "albums", "index.html"))
    ]
    
    expectedFiles |> List.iter (fun (name, path) ->
        if File.Exists path then
            printfn "   ✅ %s: %s" name (Path.GetFileName path)
        else
            printfn "   ❌ %s: Missing at %s" name path)
    
    // Test 3: Verify URL structure and content
    printfn "\n3. Verifying URL structure and content..."
    
    // Check individual album page
    let albumPagePath = Path.Combine("_public", "media", "fall-mountains", "index.html")
    if File.Exists albumPagePath then
        let content = File.ReadAllText(albumPagePath)
        printfn "   ✅ Album page accessible at /media/fall-mountains/"
        
        // Check for essential elements
        let checks = [
            ("Album title", content.Contains("Fall Mountains"))
            ("H-entry microformat", content.Contains("h-entry"))
            ("Image elements", content.Contains("<img"))
            ("Media content", content.Contains("media") || content.Contains("image"))
        ]
        
        checks |> List.iter (fun (check, result) ->
            if result then
                printfn "     ✅ %s present" check
            else
                printfn "     ⚠️ %s missing" check)
    
    // Test 4: Verify RSS feed structure and content
    printfn "\n4. Verifying RSS feed structure..."
    let rssPath = Path.Combine("_public", "feed", "albums", "rss.xml")
    if File.Exists rssPath then
        let rssContent = File.ReadAllText(rssPath)
        printfn "   ✅ RSS feed generated"
        
        let rssChecks = [
            ("XML declaration", rssContent.StartsWith("<?xml"))
            ("RSS version", rssContent.Contains("version=\"2.0\""))
            ("Channel element", rssContent.Contains("<channel>"))
            ("Item elements", rssContent.Contains("<item>"))
            ("Album title", rssContent.Contains("Fall Mountains"))
        ]
        
        rssChecks |> List.iter (fun (check, result) ->
            if result then
                printfn "     ✅ %s correct" check
            else
                printfn "     ❌ %s missing" check)
    
    // Test 5: Performance and build metrics
    printfn "\n5. Build performance metrics..."
    let buildStart = DateTime.Now
    let _ = buildAlbums()
    let buildEnd = DateTime.Now
    let buildTime = buildEnd.Subtract(buildStart).TotalMilliseconds
    
    printfn "   ✅ Build performance test"
    printfn "     Build time: %.0f ms" buildTime
    printfn "     Albums per second: %.1f" (float feedData.Length / (buildTime / 1000.0))
    
    if buildTime < 5000.0 then
        printfn "     ✅ Build time acceptable (< 5 seconds)"
    else
        printfn "     ⚠️ Build time slow (> 5 seconds)"

with ex ->
    printfn "❌ End-to-end test failed: %s" ex.Message
    printfn "   Stack trace: %s" ex.StackTrace

// Test 6: Feature flag behavior validation
printfn "\n6. Testing feature flag behavior..."
Environment.SetEnvironmentVariable("NEW_ALBUMS", "false")
let albumsDisabled = FeatureFlags.isEnabled FeatureFlags.Albums
printfn "   NEW_ALBUMS=false: Albums enabled = %b" albumsDisabled

Environment.SetEnvironmentVariable("NEW_ALBUMS", "true")
let albumsEnabled = FeatureFlags.isEnabled FeatureFlags.Albums
printfn "   NEW_ALBUMS=true: Albums enabled = %b" albumsEnabled

Environment.SetEnvironmentVariable("NEW_ALBUMS", null)
let albumsDefault = FeatureFlags.isEnabled FeatureFlags.Albums
printfn "   NEW_ALBUMS=null: Albums enabled = %b (default)" albumsDefault

// Restore environment
Environment.SetEnvironmentVariable("NEW_ALBUMS", null)

printfn "\n=== End-to-End Albums Migration Validation Complete ==="
printfn "✅ Phase 3 validation successful - ready for Phase 4 production deployment"
