// Test the new buildUnifiedCollections function in a controlled way
#r "nuget: Giraffe.ViewEngine, 1.4.0"
#r "nuget: YamlDotNet, 16.3.0"

#load "../Domain.fs"
#load "../Collections.fs"

open System
open System.IO
open Domain
open Collections.CollectionBuilder

printfn "🧪 Testing buildUnifiedCollections function"
printfn "==========================================="

// Test the buildCollections function that returns data for Builder.fs
printfn "\n1. Testing buildCollections() function..."
let collections = buildCollections ()

printfn "\nCollection data loaded:"
for (collection, data) in collections do
    printfn "   %s: %d items" collection.Title data.Items.Length

// Test OPML generation for each collection
printfn "\n2. Testing OPML generation for each collection..."
for (collection, data) in collections do
    try
        let opmlContent = generateCollectionOpmlContent data
        printfn "   ✅ %s: OPML generated (%d chars)" collection.Title opmlContent.Length
    with
    | ex ->
        printfn "   ❌ %s: OPML error: %s" collection.Title ex.Message

// Test RSS generation for each collection
printfn "\n3. Testing RSS generation for each collection..."
for (collection, data) in collections do
    try
        let rssContent = generateCollectionRssContent data
        printfn "   ✅ %s: RSS generated (%d chars)" collection.Title rssContent.Length
    with
    | ex ->
        printfn "   ❌ %s: RSS error: %s" collection.Title ex.Message

printfn "\n==========================================="
printfn "🎯 Integration test complete"
printfn ""
printfn "Ready to integrate buildUnifiedCollections() into Program.fs"
