#!/usr/bin/env -S dotnet fsi

// Simplified Review Schema Test
// Tests the updated ReviewData type based on @lqdev feedback

#r "nuget: YamlDotNet, 13.1.1"

open System
open System.Collections.Generic
open YamlDotNet.Serialization
open YamlDotNet.Serialization.NamingConventions

// Simplified ReviewData type matching the updated implementation
[<CLIMutable>]
type ReviewData = {
    // Core review fields
    [<YamlDotNet.Serialization.YamlMember(Alias="item")>]
    item: string  // Name of the item being reviewed
    [<YamlDotNet.Serialization.YamlMember(Alias="itemType")>]
    item_type: string option  // Type of review
    [<YamlDotNet.Serialization.YamlMember(Alias="rating")>]
    rating: float
    [<YamlDotNet.Serialization.YamlMember(Alias="scale")>]
    scale: float option
    [<YamlDotNet.Serialization.YamlMember(Alias="summary")>]
    summary: string option
    
    // Optional structured feedback
    [<YamlDotNet.Serialization.YamlMember(Alias="pros")>]
    pros: string array option
    [<YamlDotNet.Serialization.YamlMember(Alias="cons")>]
    cons: string array option
    
    // Optional metadata and links
    [<YamlDotNet.Serialization.YamlMember(Alias="itemUrl")>]
    item_url: string option
    [<YamlDotNet.Serialization.YamlMember(Alias="imageUrl")>]
    image_url: string option
    [<YamlDotNet.Serialization.YamlMember(Alias="additionalFields")>]
    additional_fields: Dictionary<string, obj> option
}
with
    member this.GetItemType() = 
        this.item_type |> Option.defaultValue "unknown"
    member this.GetScale() = 
        this.scale |> Option.defaultValue 5.0
    member this.GetSummary() = 
        this.summary |> Option.defaultValue ""

// Test examples for different review types
let bookReview = """item: "The Four Agreements"
itemType: "book"
rating: 4.4
scale: 5.0
summary: "Ancient Toltec wisdom offering powerful code of conduct."
pros:
  - "Clear, actionable principles"
  - "Practical daily application"
cons:
  - "Very brief treatment"
itemUrl: "https://openlibrary.org/works/OL27203W/The_Four_Agreements"
imageUrl: "https://covers.openlibrary.org/b/id/15101528-L.jpg"
additionalFields:
  author: "Don Miguel Ruiz"
  isbn: "9781878424945"
  genre: "Philosophy/Self-Help"
"""

let movieReview = """item: "Blade Runner 2049"
itemType: "movie"
rating: 4.8
scale: 5.0
summary: "A masterpiece that honors the original while standing on its own."
pros:
  - "Stunning visuals"
  - "Great performances"
cons:
  - "Lengthy runtime"
itemUrl: "https://www.imdb.com/title/tt1856101/"
imageUrl: "https://image.tmdb.org/t/p/w500/gajva2L0rPYkEWjzgFlBXCAVBE5.jpg"
additionalFields:
  director: "Denis Villeneuve"
  year: 2017
  rotten_tomatoes: "88%"
"""

let productReview = """item: "MacBook Pro M2"
itemType: "product"
rating: 4.6
scale: 5.0
summary: "Excellent performance for development work with great battery life."
pros:
  - "Fast M2 chip"
  - "Great battery life"
cons:
  - "Expensive"
  - "Limited ports"
itemUrl: "https://www.apple.com/macbook-pro-13/"
imageUrl: "https://store.storeimages.cdn-apple.com/4982/as-images.apple.com/is/mbp13-spacegray-select-202206"
additionalFields:
  manufacturer: "Apple"
  price: "$1999"
  model: "13-inch M2"
"""

// Test the simplified schema
let testSimplifiedSchema () =
    let deserializer = 
        DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build()
    
    let testCases = [
        ("Book Review", bookReview)
        ("Movie Review", movieReview) 
        ("Product Review", productReview)
    ]
    
    printfn "=== Simplified ReviewData Schema Test ==="
    printfn ""
    
    for (name, yaml) in testCases do
        try
            let review = deserializer.Deserialize<ReviewData>(yaml)
            printfn "✅ %s:" name
            printfn "   - Item: %s (%s)" review.item (review.GetItemType())
            printfn "   - Rating: %.1f/%.1f" review.rating (review.GetScale())
            printfn "   - Summary: %s" (review.GetSummary().Substring(0, min 40 (review.GetSummary().Length)) + "...")
            printfn "   - Pros: %d items" (match review.pros with Some p -> p.Length | None -> 0)
            printfn "   - Cons: %d items" (match review.cons with Some c -> c.Length | None -> 0)
            printfn "   - Has Item URL: %s" (match review.item_url with Some _ -> "Yes" | None -> "No")
            printfn "   - Has Image URL: %s" (match review.image_url with Some _ -> "Yes" | None -> "No")
            printfn "   - Additional Fields: %s" (match review.additional_fields with Some af -> $"{af.Count} items" | None -> "none")
            printfn ""
        with
        | ex ->
            printfn "❌ %s FAILED: %s" name ex.Message
            printfn ""

// Run tests
testSimplifiedSchema ()

printfn "=== Schema Simplification Benefits ==="
printfn ""
printfn "Removed Fields (based on @lqdev feedback):"
printfn "- ❌ title (use frontmatter title instead)"
printfn "- ❌ review_date (use frontmatter date_published instead)"
printfn "- ❌ Legacy compatibility fields (item_title, max_rating, review_text)"
printfn ""
printfn "Key Fields:"
printfn "- ✅ item: Name of what's being reviewed (required)"
printfn "- ✅ itemType: book/movie/music/business/product (optional)"
printfn "- ✅ rating: Numeric rating (required)"
printfn "- ✅ scale: Rating scale, defaults to 5.0 (optional)"
printfn "- ✅ summary: Brief review text (optional)"
printfn "- ✅ pros/cons: Structured feedback arrays (optional)"
printfn "- ✅ itemUrl: Link to item's website/page (optional)"
printfn "- ✅ imageUrl: Thumbnail/cover image for display (optional)"
printfn "- ✅ additionalFields: Type-specific metadata (optional)"
printfn ""
printfn "Clarifications:"
printfn "- 'item' = Name of what's being reviewed (e.g., book title, movie name)"
printfn "- 'title' = Use frontmatter title for the review post title"
printfn "- 'review_date' = Use frontmatter date_published for review date"
printfn "- 'imageUrl' = For book covers, movie posters, product images, etc."