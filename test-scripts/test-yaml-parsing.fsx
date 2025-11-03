// Test YAML parsing of review blocks
#r "nuget: YamlDotNet"

open YamlDotNet.Serialization
open System.Collections.Generic

[<CLIMutable>]
type ReviewData = {
    [<YamlMember(Alias="item")>]
    item: string
    [<YamlMember(Alias="itemType")>]
    item_type: string option
    [<YamlMember(Alias="rating")>]
    rating: float
    [<YamlMember(Alias="scale")>]
    scale: float option
    [<YamlMember(Alias="summary")>]
    summary: string option
    [<YamlMember(Alias="itemUrl")>]
    item_url: string option
    [<YamlMember(Alias="imageUrl")>]
    image_url: string option
    [<YamlMember(Alias="additionalFields")>]
    additional_fields: Dictionary<string, obj> option
}

let yamlContent = """item: "Agency"
itemType: "book"
rating: 2.5
scale: 5.0
summary: "I really wanted to like this book but didn't."
itemUrl: "https://www.penguinrandomhouse.com/books/530536/agency-by-william-gibson/"
imageUrl: "https://images4.penguinrandomhouse.com/cover/9781101986943"
additionalFields:
  author: "William Gibson"
  isbn: "9781101986943"
"""

printfn "Testing YAML parsing..."
let deserializer = 
    DeserializerBuilder()
        .IgnoreUnmatchedProperties()
        .Build()

try
    let reviewData = deserializer.Deserialize<ReviewData>(yamlContent)
    printfn "✅ Parsed successfully"
    printfn "Item: %s" reviewData.item
    printfn "Rating: %.1f" reviewData.rating
    match reviewData.additional_fields with
    | Some fields ->
        printfn "Additional fields count: %d" fields.Count
        for kvp in fields do
            printfn "  %s: %A" kvp.Key kvp.Value
    | None -> printfn "No additional fields parsed"
with ex ->
    printfn "❌ Error: %s" ex.Message
