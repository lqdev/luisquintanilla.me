module AtProtoMediaValidation

open System
open System.Text

let maxImageBlobBytes = 2_000_000
let maxVideoBlobBytes = 300_000_000

let private asciiEquals (bytes: byte[]) (offset: int) (value: string) =
    offset >= 0
    && offset + value.Length <= bytes.Length
    && Encoding.ASCII.GetString(bytes, offset, value.Length) = value

let private uint16Be (bytes: byte[]) offset =
    (int bytes.[offset] <<< 8) ||| int bytes.[offset + 1]

let private uint16Le (bytes: byte[]) offset =
    int bytes.[offset] ||| (int bytes.[offset + 1] <<< 8)

let private uint24Le (bytes: byte[]) offset =
    int bytes.[offset] ||| (int bytes.[offset + 1] <<< 8) ||| (int bytes.[offset + 2] <<< 16)

let private uint32Be (bytes: byte[]) offset =
    (uint32 bytes.[offset] <<< 24)
    ||| (uint32 bytes.[offset + 1] <<< 16)
    ||| (uint32 bytes.[offset + 2] <<< 8)
    ||| uint32 bytes.[offset + 3]

let private uint32Le (bytes: byte[]) offset =
    uint32 bytes.[offset]
    ||| (uint32 bytes.[offset + 1] <<< 8)
    ||| (uint32 bytes.[offset + 2] <<< 16)
    ||| (uint32 bytes.[offset + 3] <<< 24)

let private validateDimensions (mime: string) (width: int) (height: int) =
    if width <= 0 || height <= 0 then
        failwithf "media asset (%s) has invalid dimensions %dx%d" mime width height
    width, height

let private jpegDimensions (bytes: byte[]) =
    let rec findMarker position =
        if position + 1 >= bytes.Length then
            failwith "JPEG has no readable frame dimensions"
        elif bytes.[position] <> 0xFFuy then
            findMarker (position + 1)
        else
            let mutable markerPosition = position + 1
            while markerPosition < bytes.Length && bytes.[markerPosition] = 0xFFuy do
                markerPosition <- markerPosition + 1
            if markerPosition >= bytes.Length then
                failwith "JPEG has an incomplete marker"
            let marker = bytes.[markerPosition]
            if marker = 0xD8uy || marker = 0xD9uy || marker = 0x01uy then
                findMarker (markerPosition + 1)
            elif marker = 0xDAuy then
                failwith "JPEG has no frame dimensions before image data"
            elif markerPosition + 2 >= bytes.Length then
                failwith "JPEG has an incomplete segment length"
            else
                let segmentLength = uint16Be bytes (markerPosition + 1)
                let segmentEnd = markerPosition + 1 + segmentLength
                if segmentLength < 2 || segmentEnd > bytes.Length then
                    failwith "JPEG has an invalid segment length"
                let isFrame =
                    (marker >= 0xC0uy && marker <= 0xC3uy)
                    || (marker >= 0xC5uy && marker <= 0xC7uy)
                    || (marker >= 0xC9uy && marker <= 0xCBuy)
                    || (marker >= 0xCDuy && marker <= 0xCFuy)
                if isFrame && segmentLength >= 7 then
                    let height = uint16Be bytes (markerPosition + 4)
                    let width = uint16Be bytes (markerPosition + 6)
                    validateDimensions "image/jpeg" width height
                else
                    findMarker segmentEnd
    if bytes.Length < 4 || bytes.[0] <> 0xFFuy || bytes.[1] <> 0xD8uy then
        failwith "media asset is not a JPEG"
    findMarker 2

/// Validate a supported image and return its exact encoded dimensions.
let imageDimensions (mime: string) (bytes: byte[]) =
    if bytes.Length > maxImageBlobBytes then
        failwithf "image asset exceeds the 2,000,000-byte app.bsky.embed.images limit (%d bytes)" bytes.Length
    match mime.ToLowerInvariant() with
    | "image/png" ->
        if bytes.Length < 24
           || bytes.[0] <> 0x89uy
           || not (asciiEquals bytes 1 "PNG")
           || bytes.[4] <> 0x0Duy
           || bytes.[5] <> 0x0Auy
           || bytes.[6] <> 0x1Auy
           || bytes.[7] <> 0x0Auy
           || not (asciiEquals bytes 12 "IHDR") then
            failwith "media asset is not a PNG"
        let width = uint32Be bytes 16
        let height = uint32Be bytes 20
        if width > uint32 Int32.MaxValue || height > uint32 Int32.MaxValue then
            failwith "PNG dimensions exceed supported integer range"
        validateDimensions mime (int width) (int height)
    | "image/gif" ->
        if bytes.Length < 10
           || not (asciiEquals bytes 0 "GIF87a")
              && not (asciiEquals bytes 0 "GIF89a") then
            failwith "media asset is not a GIF"
        validateDimensions mime (uint16Le bytes 6) (uint16Le bytes 8)
    | "image/jpeg" -> jpegDimensions bytes
    | "image/webp" ->
        if bytes.Length < 16 || not (asciiEquals bytes 0 "RIFF") || not (asciiEquals bytes 8 "WEBP") then
            failwith "media asset is not a WebP"
        let mutable offset = 12
        let mutable dimensions : (int * int) option = None
        while dimensions.IsNone && offset + 8 <= bytes.Length do
            let chunk = Encoding.ASCII.GetString(bytes, offset, 4)
            let chunkLength = uint32Le bytes (offset + 4)
            if chunkLength > uint32 Int32.MaxValue then
                failwith "WebP chunk is too large"
            let dataOffset = offset + 8
            let dataLength = int chunkLength
            if dataOffset + dataLength > bytes.Length then
                failwith "WebP chunk exceeds the downloaded asset"
            match chunk with
            | "VP8X" when dataLength >= 10 ->
                dimensions <- Some (1 + uint24Le bytes (dataOffset + 4), 1 + uint24Le bytes (dataOffset + 7))
            | "VP8L" when dataLength >= 5 && bytes.[dataOffset] = 0x2Fuy ->
                let width = 1 + ((int bytes.[dataOffset + 1] ||| (int bytes.[dataOffset + 2] <<< 8)) &&& 0x3FFF)
                let height = 1 + (((int bytes.[dataOffset + 2] >>> 6) ||| (int bytes.[dataOffset + 3] <<< 2) ||| (int bytes.[dataOffset + 4] <<< 10)) &&& 0x3FFF)
                dimensions <- Some (width, height)
            | "VP8 " when
                dataLength >= 10
                && bytes.[dataOffset + 3] = 0x9Duy
                && bytes.[dataOffset + 4] = 0x01uy
                && bytes.[dataOffset + 5] = 0x2Auy ->
                let width = uint16Le bytes (dataOffset + 6) &&& 0x3FFF
                let height = uint16Le bytes (dataOffset + 8) &&& 0x3FFF
                dimensions <- Some (width, height)
            | _ -> ()
            offset <- dataOffset + dataLength + (dataLength &&& 1)
        dimensions
        |> Option.map (fun (width, height) -> validateDimensions mime width height)
        |> Option.defaultWith (fun () -> failwith "WebP has no readable frame dimensions")
    | _ -> failwithf "unsupported image MIME type '%s'" mime

/// Validate an MP4 container and its current app.bsky.embed.video byte limit.
let validateVideo (mime: string) (bytes: byte[]) =
    if mime.ToLowerInvariant() <> "video/mp4" then
        failwithf "unsupported video MIME type '%s'; only video/mp4 is accepted" mime
    if bytes.Length > maxVideoBlobBytes then
        failwithf "video asset exceeds the 300,000,000-byte app.bsky.embed.video limit (%d bytes)" bytes.Length
    if bytes.Length < 12 || not (asciiEquals bytes 4 "ftyp") then
        failwith "video asset is not an MP4 file (missing the ftyp box)"
