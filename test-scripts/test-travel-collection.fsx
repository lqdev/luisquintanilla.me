#load "../Domain.fs"
#load "../Collections.fs"

open System.IO
open Collections.CollectionConfig
open Collections.CollectionProcessor

printfn "🧪 Testing Travel Collection and GPX Generation"
printfn "================================================="

// Test 1: Check if travel collection is configured
printfn "\n1. Testing travel collection configuration..."
let collections = getDefaultCollections ()
let travelCollection = collections |> Array.tryFind (fun c -> c.Id = "rome-favorites")

match travelCollection with
| Some collection ->
    printfn "   ✅ Found Rome Favorites collection"
    printfn "   Collection ID: %s" collection.Id
    printfn "   Title: %s" collection.Title
    printfn "   Data file: %s" collection.DataFile
    printfn "   Tags: %s" (String.concat ", " collection.Tags)
    
    // Test 2: Check GPX path generation
    printfn "\n2. Testing GPX path generation..."
    let paths = getCollectionPaths collection
    printfn "   GPX Path: %A" paths.GpxPath
    
    // Test 3: Test GPX generation
    printfn "\n3. Testing GPX generation..."
    try
        let processor = createCollectionProcessor collection
        let data = processor.LoadData paths.DataPath collection
        printfn "   ✅ Collection data loaded: %d items" data.Items.Length
        
        match processor.GenerateGpxFile data with
        | Some gpxContent ->
            printfn "   ✅ GPX generation successful (%d characters)" gpxContent.Length
            printfn "   GPX preview (first 200 chars):"
            printfn "   %s..." (gpxContent.Substring(0, min 200 gpxContent.Length))
        | None ->
            printfn "   ⚠️  No GPX content generated (expected for non-travel collections)"
    with
    | ex ->
        printfn "   ❌ GPX generation error: %s" ex.Message

| None ->
    printfn "   ❌ Rome Favorites collection not found"

printfn "\n================================================="
printfn "🎯 Travel Collection Test Complete"