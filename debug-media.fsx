#r "nuget: YamlDotNet"

open System

// Simulate the album images format
type AlbumImage = {
    ImagePath: string
    AltText: string
    Description: string
}

let testImages = [|
    { ImagePath = "/images/fall-mountains/sunrise.jpg"; AltText = "Sunrise at mountain summit"; Description = "Sunrise" }
    { ImagePath = "/images/fall-mountains/sunset.jpg"; AltText = "Sunset at lake"; Description = "Sunset" }
|]

// Generate the format I'm currently creating
let myFormat = 
    let mediaItems = 
        testImages
        |> Array.map (fun img -> 
            sprintf "- media_type: image\n  uri: %s\n  alt_text: %s\n  caption: %s\n  aspect: \"\"" 
                img.ImagePath img.AltText img.Description)
        |> String.concat "\n"
    let mediaBlock = sprintf ":::media\n%s\n:::media" mediaItems
    sprintf "# Test Album\n\n%s" mediaBlock

printfn "=== My Generated Format ==="
printfn "%s" myFormat
printfn ""

// What the sample expects
let expectedFormat = """# Test Album

:::media
- media_type: image
  uri: /images/fall-mountains/sunrise.jpg
  caption: Sunrise
  alt_text: Sunrise at mountain summit
  aspect: ""
- media_type: image
  uri: /images/fall-mountains/sunset.jpg
  caption: Sunset  
  alt_text: Sunset at lake
  aspect: ""
:::media"""

printfn "=== Expected Format ==="
printfn "%s" expectedFormat
