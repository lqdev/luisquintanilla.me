module ReviewSchema

open System

type ReviewFieldRole =
    | NoRole
    | SchemaAuthor
    | SchemaIsbn

type ReviewField = {
    Key: string
    Label: string
    Role: ReviewFieldRole
}

let private table : Map<string, ReviewField list> =
    [ "book",
        [ { Key = "author"; Label = "Author"; Role = SchemaAuthor }
          { Key = "isbn"; Label = "ISBN"; Role = SchemaIsbn }
          { Key = "genre"; Label = "Genre"; Role = NoRole } ]
      "movie",
        [ { Key = "director"; Label = "Director"; Role = NoRole }
          { Key = "year"; Label = "Year"; Role = NoRole }
          { Key = "genre"; Label = "Genre"; Role = NoRole } ]
      "music",
        [ { Key = "artist"; Label = "Artist"; Role = NoRole }
          { Key = "music_type"; Label = "Type"; Role = NoRole }
          { Key = "release_year"; Label = "Release Year"; Role = NoRole }
          { Key = "genre"; Label = "Genre"; Role = NoRole }
          { Key = "label"; Label = "Label"; Role = NoRole } ]
      "business",
        [ { Key = "business_type"; Label = "Type"; Role = NoRole }
          { Key = "location"; Label = "Location"; Role = NoRole }
          { Key = "price_range"; Label = "Price Range"; Role = NoRole } ]
      "product",
        [ { Key = "manufacturer"; Label = "Manufacturer"; Role = NoRole }
          { Key = "product_category"; Label = "Category"; Role = NoRole }
          { Key = "model_version"; Label = "Model"; Role = NoRole }
          { Key = "price"; Label = "Price"; Role = NoRole } ] ]
    |> Map.ofList

let fieldsFor (itemType: string) : ReviewField list =
    match table.TryFind (itemType.ToLowerInvariant()) with
    | Some fields -> fields
    | None -> []

let private normalizedFields (fields: Map<string, string>) : Map<string, string> =
    fields
    |> Map.toSeq
    |> Seq.map (fun (key, value) -> (key.ToLowerInvariant(), value))
    |> Map.ofSeq

let displayFields (itemType: string) (fields: Map<string, string>) : (string * string) list =
    let normalized = normalizedFields fields
    fieldsFor itemType
    |> List.choose (fun field ->
        match normalized.TryFind field.Key with
        | Some value when not (String.IsNullOrWhiteSpace value) -> Some (field.Label, value)
        | _ -> None)

let roleValue (itemType: string) (role: ReviewFieldRole) (fields: Map<string, string>) : string option =
    let normalized = normalizedFields fields
    fieldsFor itemType
    |> List.tryFind (fun field -> field.Role = role)
    |> Option.bind (fun field -> normalized.TryFind field.Key)
    |> Option.bind (fun value -> if String.IsNullOrWhiteSpace value then None else Some value)

let knownKeys (itemType: string) : Set<string> =
    fieldsFor itemType |> List.map (fun field -> field.Key) |> Set.ofList
