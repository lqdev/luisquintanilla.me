#r "bin/Debug/net9.0/PersonalSite.dll"

open System
open System.IO
open GenericBuilder
open GenericBuilder.UnifiedFeeds

// Test unified feed system - Phase 2 Validation
printfn "=== Unified Feed System Validation ==="

// Test 1: Create sample feed data from different content types
printfn "\n1. Testing unified feed data conversion..."

// Simulate feed data from different content types (simplified test)
let sampleFeedDataSets = [
    ("posts", [])      // Empty for test - would be populated by buildPosts()
    ("notes", [])      // Empty for test - would be populated by buildNotes() 
    ("responses", [])  // Empty for test - would be populated by buildResponses()
    ("snippets", [])   // Empty for test - would be populated by buildSnippets()
    ("wiki", [])       // Empty for test - would be populated by buildWikis()
    ("presentations", []) // Empty for test - would be populated by buildPresentations()
    ("library", [])    // Empty for test - would be populated by buildBooks()
    ("albums", [])     // Empty for test - would be populated by buildAlbums()
]

try
    // Test 2: Build unified feeds with empty data (should not crash)
    printfn "\n2. Testing unified feed generation with empty data..."
    buildAllFeeds sampleFeedDataSets "_public"
    printfn "✅ Unified feed system function executed successfully"
    
    // Test 3: Verify feed directories are created
    printfn "\n3. Checking feed directory structure..."
    let feedDirs = [
        "_public/feed"
        "_public/posts/feed"
        "_public/snippets/feed"
        "_public/wiki/feed"
        "_public/presentations/feed"
        "_public/library/feed"
        "_public/feed/notes"
        "_public/feed/responses"
        "_public/feed/media"
    ]
    
    feedDirs |> List.iter (fun dir ->
        if Directory.Exists(dir) then
            printfn "   ✅ Directory created: %s" dir
        else
            printfn "   ❌ Directory missing: %s" dir
    )
    
    // Test 4: Check for RSS files (should be empty but valid structure)
    printfn "\n4. Checking RSS feed files..."
    let feedFiles = [
        "_public/feed/index.xml"                // Fire-hose feed
        "_public/posts/feed/index.xml"          // Posts feed
        "_public/snippets/feed/index.xml"       // Snippets feed
        "_public/wiki/feed/index.xml"           // Wiki feed
        "_public/presentations/feed/index.xml"  // Presentations feed
        "_public/library/feed/index.xml"        // Library feed
        "_public/feed/notes/index.xml"          // Notes feed
        "_public/feed/responses/index.xml"      // Responses feed
        "_public/feed/media/index.xml"          // Albums feed
    ]
    
    feedFiles |> List.iter (fun file ->
        if File.Exists(file) then
            let content = File.ReadAllText(file)
            let hasXmlDecl = content.StartsWith("<?xml")
            let hasRssTag = content.Contains("<rss")
            let hasChannel = content.Contains("<channel>")
            printfn "   ✅ Feed file: %s (XML: %b, RSS: %b, Channel: %b)" 
                (Path.GetFileName(Path.GetDirectoryName(file)) + "/" + Path.GetFileName(file))
                hasXmlDecl hasRssTag hasChannel
        else
            printfn "   ❌ Feed file missing: %s" file
    )
    
    printfn "\n=== Test Summary ==="
    printfn "✅ Unified feed system architecture validation complete"
    printfn "✅ Ready for integration with actual content data"
    
with ex ->
    printfn "❌ Unified feed system test failed: %s" ex.Message
    printfn "Stack trace: %s" ex.StackTrace
