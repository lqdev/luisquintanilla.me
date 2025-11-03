// Test YAML parsing with Map
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
    [<YamlMember(Alias="additionalFields")>]
    additional_fields: Map<string, string> option
}

let yamlContent = """item: "Agency"
itemType: "book"
rating: 2.5
additionalFields:
  author: "William Gibson"
  isbn: "9781101986943"
"""

printfn "Testing YAML parsing with Map..."
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
        printfn "Additional fields count: %d" (fields |> Map.count)
        for kvp in fields do
            printfn "  %s: %s" kvp.Key kvp.Value
    | None -> printfn "No additional fields parsed"
with ex ->
    printfn "❌ Error: %s" ex.Message
    printfn "Stack: %s" ex.StackTrace
