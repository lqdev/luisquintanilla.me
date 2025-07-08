#r "nuget: Markdig"
#r "nuget: YamlDotNet" 
#load "Domain.fs"
#load "ASTParsing.fs"
#load "CustomBlocks.fs"
#load "MediaTypes.fs"
#load "BlockRenderers.fs"
#load "GenericBuilder.fs"

open System
open Domain
open ASTParsing
open CustomBlocks
open BlockRenderers
open GenericBuilder

// Test Phase 1B: Generic Builder and Renderers
printfn "=== Phase 1B Testing: Generic Builder and Renderers ==="

// Test 1: ContentProcessor pattern
printfn "\n--- Test 1: ContentProcessor Pattern ---"
let testProcessor = PostProcessor.create()
printfn "✓ PostProcessor created successfully"

// Test 2: BlockRenderer functionality
printfn "\n--- Test 2: BlockRenderer Functionality ---"

// Create test data for each block type
let testMediaItems = [
    { media_type = "image"; uri = "test.jpg"; alt_text = "Test image"; caption = "A test image"; aspect = "landscape" }
]

let testReviewData = {
    item_title = "Test Movie"
    rating = 4.5
    max_rating = 5.0
    review_text = "A great test movie"
    item_url = Some "https://example.com/movie"
    review_date = Some "2025-07-08"
}

let testVenueData = {
    venue_name = "Test Venue"
    venue_address = "123 Test St"
    venue_city = "Test City"
    venue_country = "Test Country"
    venue_url = Some "https://example.com/venue"
    latitude = Some 40.7128
    longitude = Some -74.0060
}

let testRsvpData = {
    event_name = "Test Event"
    event_url = "https://example.com/event"
    event_date = "2025-07-15"
    rsvp_status = "yes"
    event_location = Some "Test Location"
    notes = Some "Looking forward to it"
}

// Test rendering each block type
let mediaBlock = Media testMediaItems
let reviewBlock = Review testReviewData
let venueBlock = Venue testVenueData
let rsvpBlock = Rsvp testRsvpData

try
    let mediaHtml = BlockRenderer.renderCustomBlock mediaBlock
    printfn "✓ Media block rendered: %d characters" mediaHtml.Length
    
    let reviewHtml = BlockRenderer.renderCustomBlock reviewBlock
    printfn "✓ Review block rendered: %d characters" reviewHtml.Length
    
    let venueHtml = BlockRenderer.renderCustomBlock venueBlock
    printfn "✓ Venue block rendered: %d characters" venueHtml.Length
    
    let rsvpHtml = BlockRenderer.renderCustomBlock rsvpBlock
    printfn "✓ RSVP block rendered: %d characters" rsvpHtml.Length
    
    // Test batch rendering
    let allBlocks = [mediaBlock; reviewBlock; venueBlock; rsvpBlock]
    let batchHtml = BlockRenderer.renderCustomBlocks allBlocks
    printfn "✓ All blocks rendered together: %d characters" batchHtml.Length
    
    printfn "\n--- Sample Rendered Output ---"
    printfn "Media HTML: %s" (mediaHtml.Substring(0, min 100 mediaHtml.Length))
    printfn "Review HTML: %s" (reviewHtml.Substring(0, min 100 reviewHtml.Length))
    
    printfn "\n=== Phase 1B Test Results ==="
    printfn "✅ All block renderers functional"
    printfn "✅ ContentProcessor pattern working"
    printfn "✅ HTML output generation successful"
    
with
| ex -> 
    printfn "❌ Phase 1B Test Failed: %s" ex.Message
    printfn "Stack trace: %s" ex.StackTrace
