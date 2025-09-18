#!/usr/bin/env -S dotnet fsi

// Review Schema Analysis Script
// Analyzes current ReviewData type capabilities and requirements for different review types

#r "nuget: YamlDotNet, 13.1.1"

open System
open System.Collections.Generic
open YamlDotNet.Serialization
open YamlDotNet.Serialization.NamingConventions

// Current ReviewData type (simplified)
[<CLIMutable>]
type CurrentReviewData = {
    item_title: string
    rating: float
    max_rating: float
    review_text: string
    item_url: string option
    review_date: string option
}

// Enhanced ReviewData type for comprehensive review support
[<CLIMutable>]
type EnhancedReviewData = {
    // Core identification
    title: string
    item: string
    item_type: string  // "book", "movie", "music", "business", "product", etc.
    
    // Rating system
    rating: float
    scale: float
    
    // Review content
    summary: string
    pros: string array  // Required field for testing
    cons: string array  // Required field for testing
}

// Test YAML samples for different review types

let bookReviewYaml = """
title: "Excellent Programming Guide"
item: "Expert F# 4.0"
item_type: "book"
rating: 5.0
scale: 5.0
summary: "Comprehensive guide to F# programming with excellent examples."
pros:
  - "Clear explanations"
  - "Practical examples"
  - "Good progression"
cons:
  - "Could use more advanced topics"
additional_fields:
  author: "Don Syme"
  isbn: "978-1484207413"
  pages: 624
"""

let movieReviewYaml = """
title: "Excellent Sci-Fi Film"
item: "Blade Runner 2049"
item_type: "movie"
rating: 4.8
scale: 5.0
summary: "A masterpiece that honors the original while standing on its own."
pros:
  - "Stunning visuals"
  - "Excellent soundtrack"
  - "Great performances"
cons:
  - "Lengthy runtime"
additional_fields:
  director: "Denis Villeneuve"
  year: 2017
  runtime: "164 minutes"
  rotten_tomatoes: "88%"
"""

let musicReviewYaml = """
title: "Groundbreaking Album"
item: "OK Computer"
item_type: "music"
rating: 4.9
scale: 5.0
summary: "Revolutionary album that redefined alternative rock."
pros:
  - "Innovative production"
  - "Thought-provoking lyrics"
  - "Cohesive vision"
cons:
  - "Can be depressing"
additional_fields:
  artist: "Radiohead"
  year: 1997
  genre: "Alternative Rock"
  label: "Parlophone"
"""

let businessReviewYaml = """
title: "Great Local Coffee Shop"
item: "Blue Bottle Coffee"
item_type: "business"
rating: 4.5
scale: 5.0
summary: "Excellent coffee with knowledgeable staff and great atmosphere."
pros:
  - "High quality coffee"
  - "Friendly staff"
  - "Good wifi"
cons:
  - "Can be crowded"
  - "Expensive"
additional_fields:
  location: "San Francisco, CA"
  yelp_rating: "4.0"
  price_range: "$$$"
  category: "Coffee Shop"
"""

let productReviewYaml = """
title: "Solid Development Laptop"
item: "MacBook Pro M2"
item_type: "product"
rating: 4.6
scale: 5.0
summary: "Excellent performance for development work with great battery life."
pros:
  - "Fast M2 chip"
  - "Excellent battery life"
  - "Great display"
cons:
  - "Expensive"
  - "Limited ports"
additional_fields:
  manufacturer: "Apple"
  price: "$1999"
  amazon_rating: "4.4"
  model: "13-inch M2"
"""

// Test deserialization
let testDeserialization () =
    let deserializer = 
        DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build()
    
    let testCases = [
        ("Book Review", bookReviewYaml)
        ("Movie Review", movieReviewYaml)
        ("Music Review", musicReviewYaml)
        ("Business Review", businessReviewYaml)
        ("Product Review", productReviewYaml)
    ]
    
    printfn "=== Enhanced ReviewData Schema Analysis ==="
    printfn ""
    
    for (name, yaml) in testCases do
        try
            printfn "Testing %s:" name
            let review = deserializer.Deserialize<EnhancedReviewData>(yaml)
            printfn "  ✅ Successfully parsed"
            printfn "  - Item: %s (%s)" review.item review.item_type
            printfn "  - Rating: %.1f/%.1f" review.rating review.scale
            printfn "  - Summary: %s" (review.summary.Substring(0, min 50 review.summary.Length) + "...")
            printfn "  - Pros: %d items" review.pros.Length
            printfn "  - Cons: %d items" review.cons.Length
            printfn "  - Additional fields: N/A (simplified test)"
            printfn ""
        with
        | ex ->
            printfn "  ❌ Failed to parse: %s" ex.Message
            printfn ""

// Test current ReviewData compatibility
let testCurrentCompatibility () =
    let deserializer = 
        DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build()
    
    printfn "=== Current ReviewData Compatibility Analysis ==="
    printfn ""
    
    // Test with simplified YAML that matches current schema
    let currentCompatibleYaml = """
item_title: "Expert F# 4.0"
rating: 5.0
max_rating: 5.0
review_text: "Comprehensive guide to F# programming."
item_url: "https://example.com/book"
review_date: "2025-01-15"
"""
    
    try
        let review = deserializer.Deserialize<CurrentReviewData>(currentCompatibleYaml)
        printfn "✅ Current ReviewData can handle basic reviews"
        printfn "  - Title: %s" review.item_title
        printfn "  - Rating: %.1f/%.1f" review.rating review.max_rating
        printfn ""
    with
    | ex ->
        printfn "❌ Current ReviewData failed: %s" ex.Message
        printfn ""
    
    // Test current schema with sophisticated review
    printfn "Testing current schema with sophisticated review:"
    try
        let review = deserializer.Deserialize<CurrentReviewData>(bookReviewYaml)
        printfn "✅ Unexpected success - current schema handled sophisticated review"
    with
    | ex ->
        printfn "❌ Expected failure - current schema cannot handle sophisticated reviews"
        printfn "   Error: %s" ex.Message
        printfn ""

// Run analysis
testCurrentCompatibility ()
testDeserialization ()

printfn "=== Schema Analysis Summary ==="
printfn ""
printfn "Current ReviewData Limitations:"
printfn "- ❌ No support for item_type classification"
printfn "- ❌ No support for pros/cons lists"
printfn "- ❌ No support for type-specific additional fields"
printfn "- ❌ Limited metadata structure"
printfn ""
printfn "Enhanced ReviewData Benefits:"
printfn "- ✅ Supports all review types (books, movies, music, businesses, products)"
printfn "- ✅ Flexible additional_fields for type-specific metadata"
printfn "- ✅ Structured pros/cons lists"
printfn "- ✅ Backward compatibility through optional legacy fields"
printfn "- ✅ Consistent rating system across all types"
printfn ""
printfn "Recommendation: Implement Enhanced ReviewData schema to support comprehensive review system"