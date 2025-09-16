module MediaTypes

open System

/// Media types supported by the IndieWeb content system
type MediaType =
    | Image
    | Video  
    | Audio
    | Document
    | Link
    | Unknown

/// Aspect ratios for media content
type AspectRatio =
    | Landscape
    | Portrait
    | Square
    | Cinematic
    | Unknown

/// Location information for media content
type Location = {
    Description: string option
    Latitude: float option
    Longitude: float option
    Timezone: string option
}

/// Media metadata structure for IndieWeb content
type MediaMetadata = {
    MediaType: MediaType
    Uri: string
    AltText: string option
    Caption: string option
    AspectRatio: AspectRatio
    Location: Location option
    Title: string option
    Duration: TimeSpan option
    FileSize: int64 option
    MimeType: string option
}

/// Helper functions for media type detection and manipulation
module MediaTypeHelpers = 
    
    let detectMediaTypeFromUri (uri: string) : MediaType =
        let lowerUri = uri.ToLower()
        if lowerUri.Contains(".jpg") || lowerUri.Contains(".jpeg") || 
           lowerUri.Contains(".png") || lowerUri.Contains(".gif") || 
           lowerUri.Contains(".webp") || lowerUri.Contains(".svg") ||
           lowerUri.Contains(".bmp") || lowerUri.Contains(".tiff") then
            MediaType.Image
        elif lowerUri.Contains(".mp4") || lowerUri.Contains(".webm") || 
             lowerUri.Contains(".avi") || lowerUri.Contains(".mov") ||
             lowerUri.Contains(".mkv") || lowerUri.Contains(".wmv") ||
             lowerUri.Contains(".flv") || lowerUri.Contains(".m4v") then
            MediaType.Video
        elif lowerUri.Contains(".mp3") || lowerUri.Contains(".wav") || 
             lowerUri.Contains(".ogg") || lowerUri.Contains(".m4a") ||
             lowerUri.Contains(".flac") || lowerUri.Contains(".aac") ||
             lowerUri.Contains(".wma") then
            MediaType.Audio
        elif lowerUri.Contains(".pdf") || lowerUri.Contains(".doc") || 
             lowerUri.Contains(".txt") then
            MediaType.Document
        elif lowerUri.StartsWith("http") then
            MediaType.Link
        else
            MediaType.Unknown

    let detectAspectRatioFromDimensions (width: int) (height: int) : AspectRatio =
        let ratio = float width / float height
        if abs(ratio - 1.0) < 0.1 then 
            AspectRatio.Square
        elif ratio > 2.0 then 
            AspectRatio.Cinematic
        elif ratio > 1.2 then 
            AspectRatio.Landscape
        elif ratio < 0.8 then 
            AspectRatio.Portrait
        else 
            AspectRatio.Unknown

    let createBasicMediaMetadata (uri: string) (altText: string option) : MediaMetadata =
        {
            MediaType = detectMediaTypeFromUri uri
            Uri = uri
            AltText = altText
            Caption = None
            AspectRatio = AspectRatio.Unknown
            Location = None
            Title = None
            Duration = None
            FileSize = None
            MimeType = None
        }

    let createLocationFromCoordinates (lat: float) (lng: float) (description: string option) : Location =
        {
            Description = description
            Latitude = Some lat
            Longitude = Some lng
            Timezone = None
        }

    let createLocationFromDescription (description: string) : Location =
        {
            Description = Some description
            Latitude = None
            Longitude = None
            Timezone = None
        }

/// Constants for common media configurations
module MediaConstants = 
    
    let CommonImageExtensions = [|".jpg"; ".jpeg"; ".png"; ".gif"; ".webp"; ".svg"; ".bmp"; ".tiff"|]
    let CommonVideoExtensions = [|".mp4"; ".webm"; ".avi"; ".mov"; ".mkv"; ".wmv"; ".flv"; ".m4v"|]
    let CommonAudioExtensions = [|".mp3"; ".wav"; ".ogg"; ".m4a"; ".flac"; ".aac"; ".wma"|]
    let CommonDocumentExtensions = [|".pdf"; ".doc"; ".docx"; ".txt"; ".md"|]
    
    let DefaultImageAspectRatio = AspectRatio.Landscape
    let DefaultVideoAspectRatio = AspectRatio.Landscape
    let DefaultUnknownAspectRatio = AspectRatio.Unknown
