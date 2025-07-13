#r "nuget: YamlDotNet"

open YamlDotNet.Serialization
open YamlDotNet.Serialization.NamingConventions

[<CLIMutable>]
type MediaItem = {
    media_type: string
    uri: string
    alt_text: string
    caption: string
    aspect: string
}

let testYaml = """- media_type: image
  uri: /images/fall-mountains/sunrise.jpg
  alt_text: Sunrise at mountain summit
  caption: Sunrise
  aspect: ""
- media_type: image
  uri: /images/fall-mountains/sunset.jpg
  alt_text: Sunset at lake
  caption: Sunset
  aspect: "" """

printfn "Testing YAML content:"
printfn "%s" testYaml
printfn ""

try
    let deserializer = 
        DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build()
    
    let result = deserializer.Deserialize<MediaItem list>(testYaml)
    printfn "Success! Parsed %d items:" (List.length result)
    result |> List.iteri (fun i item -> 
        printfn "  Item %d: %s -> %s" i item.media_type item.uri)
with
| ex -> 
    printfn "Error: %s" ex.Message
