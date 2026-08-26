// Focused, network-free Part C contract tests.
// Run: dotnet fsi test-scripts/test-atproto-media.fsx

#r "nuget: YamlDotNet, 16.3.0"
#r "nuget: Giraffe.ViewEngine, 1.4.0"
#load "../Domain.fs"
#load "../Constants.fs"
#load "../ContentTypes.fs"
#load "../Services/Tag.fs"
#load "../Services/AtProtoMediaValidation.fs"
#load "../AtProtoBuilder.fs"

open System
open System.Text
open System.Text.Json.Nodes
open AtProtoBuilder
open AtProtoMediaValidation

let mutable passed = 0
let mutable failed = 0
let check name condition =
    if condition then passed <- passed + 1; printfn "  PASS  %s" name
    else failed <- failed + 1; printfn "  FAIL  %s" name

let album (content: string) (tags: string array) : Domain.Album =
    { FileName = "media-fixture"
      Metadata =
        { PostType = "media"
          Title = "Fixture"
          Date = "2026-08-02T12:00:00-05:00"
          Tags = tags
          Images = null }
      Content = content
      MarkdownSource = Some content }

let imageBlock alt aspect url =
    sprintf ":::media\n- url: \"%s\"\n  mediaType: \"image\"\n  alt: \"%s\"\n  aspectRatio: \"%s\"\n:::" url alt aspect

let one = album (imageBlock "" "landscape" "https://cdn.example/one.jpg") [| ".NET"; "C#"; "dotnet"; "untagged" |]
let descriptors = extractMediaItemsFromMarkdown one.Content |> normalizeMediaDescriptors one.Metadata.Title
check "AST-source media extraction finds one descriptor" (List.length descriptors = 1)
check "alt fallback uses title" (descriptors.Head.Alt = "Fixture")
check "landscape gets dimensions" (descriptors.Head.Width = 16 && descriptors.Head.Height = 9)
check "one image selects native image embed" (validateMediaDescriptors descriptors = Ok Image)

let many =
    [ 1 .. 5 ]
    |> List.map (fun n -> imageBlock "" "1:1" (sprintf "https://cdn.example/%d.png" n))
    |> String.concat "\n"
let manyDescriptors = extractMediaItemsFromMarkdown many |> normalizeMediaDescriptors "Gallery"
check "five images select gallery embed" (validateMediaDescriptors manyDescriptors = Ok Gallery)
let galleryRecord =
    buildMediaPostRecordJson (album many [||]) (DateTimeOffset(2026, 8, 2, 12, 0, 0, TimeSpan.FromHours -5.0)) Gallery manyDescriptors
check "gallery record carries all five image descriptors" (
    galleryRecord.["embed"].["$type"].GetValue<string>() = "app.bsky.embed.gallery" &&
    galleryRecord.["embed"].["items"].AsArray().Count = 5 &&
    galleryRecord.["embed"].["items"].AsArray().[0].["$type"].GetValue<string>() =
        "app.bsky.embed.gallery#image")

let mediaRecord = buildMediaPostRecordJson one (DateTimeOffset(2026, 8, 2, 12, 0, 0, TimeSpan.FromHours -5.0)) Image descriptors
let mediaType = mediaRecord.["embed"].["$type"].GetValue<string>()
check "record has app.bsky.feed.post discriminator" (mediaRecord.["$type"].GetValue<string>() = "app.bsky.feed.post")
check "record has app.bsky.embed.images" (mediaType = "app.bsky.embed.images")
check "tags are normalized and capped" (
    let tags = mediaRecord.["tags"].AsArray() |> Seq.map (fun x -> x.GetValue<string>()) |> Seq.toList
    tags = [ "dotnet"; "csharp" ])
check "canonical media URL appears in text" (mediaRecord.["text"].GetValue<string>().Contains(mediaUrl one.FileName))

let text = mediaRecord.["text"].GetValue<string>()
let hash = mediaRecord.["sourceHash"].GetValue<string>()
let changedDescriptor = { descriptors.Head with Alt = "different" }
check "sourceHash includes canonical URL/text/tags/media descriptors" (
    hash = generateMediaSourceHash (mediaUrl one.FileName) text [| "dotnet"; "csharp" |] descriptors &&
    hash <> generateMediaSourceHash (mediaUrl one.FileName) text [| "dotnet"; "csharp" |] [ changedDescriptor ])
let facet = mediaRecord.["facets"].AsArray().[0]
let start = facet.["index"].["byteStart"].GetValue<int>()
let finish = facet.["index"].["byteEnd"].GetValue<int>()
let url = mediaUrl one.FileName
check "media text is <=300 graphemes and <=3000 UTF-8 bytes" (
    (Globalization.StringInfo(text).LengthInTextElements <= 300) && Encoding.UTF8.GetByteCount(text) <= 3000)
check "facet uses UTF-8 byte offsets" (
    start = Encoding.UTF8.GetByteCount(text.Substring(0, text.IndexOf(url))) &&
    finish - start = Encoding.UTF8.GetByteCount(url))

let bodyWithMedia =
    album (sprintf "Before the gallery.\n\n%s\n\nAfter the gallery." (imageBlock "A photo" "1:1" "https://cdn.example/body.jpg")) [||]
let bodyText = buildMediaPostText bodyWithMedia
check "media fences are removed without dropping surrounding text" (
    bodyText.Contains("Before the gallery.") &&
    bodyText.Contains("After the gallery.") &&
    not (bodyText.Contains("mediaType")))

let unicodeAlbum = album (String.replicate 180 "🙂 café ") [||]
let unicodeText = buildMediaPostText unicodeAlbum
let unicodeFacet = buildMediaLinkFacet unicodeText (mediaUrl unicodeAlbum.FileName)
let unicodeUrlStart = unicodeText.LastIndexOf(mediaUrl unicodeAlbum.FileName, StringComparison.Ordinal)
check "Unicode graphemes stay intact while facet uses UTF-8 bytes" (
    Globalization.StringInfo(unicodeText).LengthInTextElements <= 300 &&
    unicodeFacet.["index"].["byteStart"].GetValue<int>() =
        Encoding.UTF8.GetByteCount(unicodeText.Substring(0, unicodeUrlStart)))

let before = DateTimeOffset(2026, 7, 31, 23, 59, 0, TimeSpan.FromHours -5.0)
let after = DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.FromHours -5.0)
check "media cutoff is forward-only" (
    not (isMediaAfterActivationCutoff Image before) &&
    isMediaAfterActivationCutoff Image after)
check "media TID is content-type qualified" (
    deriveMediaTid after "same-slug" Image <> deriveTid after "same-slug")
check "media TID is stable across embed-kind changes" (
    deriveMediaTid after "same-slug" Image = deriveMediaTid after "same-slug" Gallery &&
    deriveMediaTid after "same-slug" Gallery = deriveMediaTid after "same-slug" Video)

let mixed = [
    { Url = "https://cdn.example/a.jpg"; MimeType = "image/jpeg"; Alt = "a"; Width = 1; Height = 1 }
    { Url = "https://cdn.example/a.mp4"; MimeType = "video/mp4"; Alt = "v"; Width = 16; Height = 9 } ]
check "mixed image/video is rejected" (validateMediaDescriptors mixed = Error MixedImageAndVideo)
let videos = [ mixed.[1]; { mixed.[1] with Url = "https://cdn.example/b.mp4" } ]
check "multiple videos are rejected" (validateMediaDescriptors videos = Error MultipleVideos)
let tooMany = [ 1 .. 11 ] |> List.map (fun n ->
    { Url = sprintf "https://cdn.example/%d.jpg" n; MimeType = "image/jpeg"; Alt = "x"; Width = 1; Height = 1 })
check "more than ten images are rejected" (validateMediaDescriptors tooMany = Error (TooManyImages 11))
let audio = [ { Url = "https://cdn.example/a.mp3"; MimeType = "audio/mpeg"; Alt = "a"; Width = 1; Height = 1 } ]
check "unsupported media types are rejected" (
    match validateMediaDescriptors audio with
    | Error (UnsupportedMediaType "audio/mpeg") -> true
    | _ -> false)
let blankUrl = [
    { Url = ""; MimeType = "image/jpeg"; Alt = "missing"; Width = 1; Height = 1 }
    { Url = "https://cdn.example/valid.jpg"; MimeType = "image/jpeg"; Alt = "valid"; Width = 1; Height = 1 } ]
check "blank media URLs are rejected instead of discarded" (
    validateMediaDescriptors blankUrl = Error BlankMediaUrl)

let putAscii (bytes: byte array) offset (value: string) =
    let encoded = Encoding.ASCII.GetBytes value
    Array.blit encoded 0 bytes offset encoded.Length
    bytes

let putUInt32Be (bytes: byte array) offset value =
    bytes.[offset] <- byte (value >>> 24)
    bytes.[offset + 1] <- byte (value >>> 16)
    bytes.[offset + 2] <- byte (value >>> 8)
    bytes.[offset + 3] <- byte value
    bytes

let png = Array.zeroCreate<byte> 24
png.[0] <- 0x89uy
putAscii png 1 "PNG" |> ignore
png.[4] <- 0x0Duy
png.[5] <- 0x0Auy
png.[6] <- 0x1Auy
png.[7] <- 0x0Auy
putAscii png 12 "IHDR" |> ignore
putUInt32Be png 16 640u |> ignore
putUInt32Be png 20 360u |> ignore
check "PNG signature and exact dimensions are validated" (imageDimensions "image/png" png = (640, 360))
let invalidPng =
    try imageDimensions "image/png" (Array.zeroCreate<byte> 24) |> ignore; false
    with ex -> ex.Message.Contains("not a PNG")
check "invalid image signatures are rejected" invalidPng
let oversizedImage =
    try imageDimensions "image/png" (Array.zeroCreate<byte> (maxImageBlobBytes + 1)) |> ignore; false
    with ex -> ex.Message.Contains("2,000,000")
check "oversized images are rejected before upload" oversizedImage
let mp4 = Array.zeroCreate<byte> 12
putAscii mp4 4 "ftyp" |> ignore
check "MP4 signature and MIME type are validated" (validateVideo "video/mp4" mp4; true)
let invalidVideo =
    try validateVideo "video/webm" mp4; false
    with ex -> ex.Message.Contains("only video/mp4")
check "unsupported video MIME types are rejected" invalidVideo

let collisionCaught =
    try
        assertNoNativeTidCollisions [
            ("app.bsky.feed.post", "3abc", "note:x")
            ("app.bsky.feed.post", "3abc", "media:x") ]
        false
    with ex -> ex.Message.Contains("native app.bsky.feed.post rkey collision")
check "cross-content native rkey collision is loud" collisionCaught

printfn "-----------------------------------"
printfn "Passed: %d   Failed: %d" passed failed
if failed > 0 then exit 1
