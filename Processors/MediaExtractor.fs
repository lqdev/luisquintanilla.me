module MediaExtractor

    open Domain
    open ASTParsing
    open CustomBlocks
    open BlockRenderers
    open TagService
    open MarkdownService
    open ReadingTimeService
    open System.Xml.Linq
    open System
    open System.IO
    open System.Text.Json
    open System.Text.Json.Nodes
    open Giraffe.ViewEngine
    open Giraffe.ViewEngine.HtmlElements
    open Markdig
    open Markdig.Syntax
    open GenericBuilder

    open System.Text.RegularExpressions
    
    /// Detect MIME type from URL file extension
    let detectMimeType (url: string) : string =
        let ext = Path.GetExtension(url).ToLowerInvariant()
        match ext with
        | ".jpg" | ".jpeg" -> "image/jpeg"
        | ".png" -> "image/png"
        | ".gif" -> "image/gif"
        | ".webp" -> "image/webp"
        | ".mp4" -> "video/mp4"
        | ".webm" -> "video/webm"
        | ".mov" -> "video/quicktime"
        | ".mp3" -> "audio/mpeg"
        | ".wav" -> "audio/wav"
        | ".ogg" -> "audio/ogg"
        | ".m4a" -> "audio/mp4"
        | _ -> "application/octet-stream"
    
    /// Determine ActivityPub object type from MIME type
    let detectObjectType (mimeType: string) : string =
        if mimeType.StartsWith("image/") then "Image"
        elif mimeType.StartsWith("video/") then "Video"
        elif mimeType.StartsWith("audio/") then "Audio"
        else "Document"
    
    /// Extract first media item from :::media block for media-primary content
    /// Returns Some MediaAPData if content has a media block, None otherwise
    let extractPrimaryMedia (content: string) : MediaAPData option =
        // Defensive null check to prevent NullReferenceException
        if String.IsNullOrWhiteSpace(content) then None
        else
            let mediaPattern = @":::media\s*([\s\S]*?):::(?:media)?"
            let matches = Regex.Matches(content, mediaPattern)
            
            if matches.Count = 0 then None
            else
                let firstMediaContent = matches.[0].Groups.[1].Value
                
                // Extract URL
                let urlMatch = Regex.Match(firstMediaContent, @"url:\s*[""']([^""']+)[""']")
                if not urlMatch.Success then None
                else
                    let url = urlMatch.Groups.[1].Value
                    let mimeType = detectMimeType url
                    let objectType = detectObjectType mimeType
                    
                    // Extract caption and alt text
                    let captionMatch = Regex.Match(firstMediaContent, @"caption:\s*[""']([^""']+)[""']")
                    let altMatch = Regex.Match(firstMediaContent, @"alt:\s*[""']([^""']+)[""']")
                    
                    let caption = if captionMatch.Success then Some captionMatch.Groups.[1].Value else None
                    let altText = if altMatch.Success then Some altMatch.Groups.[1].Value else None
                    
                    Some {
                        MediaUrl = url
                        MediaType = mimeType
                        ObjectType = objectType
                        AltText = altText
                        Caption = caption
                    }
