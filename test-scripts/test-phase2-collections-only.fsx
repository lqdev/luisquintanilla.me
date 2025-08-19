// Simple test for Phase 2: Unified Collection Processing (Collections module only)
#r "nuget: Giraffe.ViewEngine, 1.4.0"
#r "nuget: YamlDotNet, 16.3.0"

#load "../Domain.fs"
#load "../Collections.fs"

open System
open System.IO
open Domain
open Collections.CollectionBuilder
open Collections.CollectionProcessor
open Collections.CollectionConfig

printfn "🧪 Testing Phase 2: Collections Module"
printfn "======================================"

// Test 1: Collection configuration loading
printfn "\n1. Testing collection configuration..."
let allCollections = getAllCollections ()
printfn "   ✅ Loaded %d collections" allCollections.Length

for collection in allCollections do
    printfn "   - %s (%A): %s" collection.Title collection.CollectionType collection.Description

// Test 2: Collection data loading (if data files exist)
printfn "\n2. Testing collection data loading..."
let mutable successCount = 0
let mutable errorCount = 0

for collection in allCollections do
    try
        let data = processCollectionData collection
        printfn "   ✅ %s: %d items" collection.Title data.Items.Length
        successCount <- successCount + 1
    with
    | ex -> 
        printfn "   ❌ %s: %s" collection.Title ex.Message
        errorCount <- errorCount + 1

printfn "\n   Summary: %d successful, %d errors" successCount errorCount

// Test 3: Path calculation for all collections
printfn "\n3. Testing path calculation..."
for collection in allCollections do
    let paths = getCollectionPaths collection
    printfn "   %s:" collection.Title
    printfn "     HTML: %s" paths.HtmlPath
    printfn "     Data: %s" paths.DataPath

// Test 4: Navigation structure
printfn "\n4. Testing navigation structure..."
let navStructure = getNavigationStructure allCollections

printfn "   Content Types (%d):" navStructure.ContentTypes.Collections.Length
for collection in navStructure.ContentTypes.Collections do
    printfn "     • %s" collection.Title

printfn "   Topic Guides (%d):" navStructure.TopicGuides.Collections.Length
for collection in navStructure.TopicGuides.Collections do
    printfn "     • %s" collection.Title

printfn "\n======================================"
printfn "🎯 Collections Module Testing Complete"
printfn ""
printfn "Next: Test integration with Builder.fs"
