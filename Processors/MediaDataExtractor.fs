module MediaDataExtractor

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

    /// Extract media data from raw markdown content
    let extractAlbumMediaData (rawMarkdown: string) : AlbumMediaData =
        try
            // Parse the media block to extract the first media item
            let mediaBlockPattern = System.Text.RegularExpressions.Regex(@":::media\s*\n(.*?)\n:::(?:media)?", System.Text.RegularExpressions.RegexOptions.Singleline)
            let mediaMatches = mediaBlockPattern.Matches(rawMarkdown)
            
            if mediaMatches.Count > 0 then
                let firstMediaContent = mediaMatches.[0].Groups.[1].Value
                
                // Extract first URL and alt text from YAML-like structure
                let urlPattern = System.Text.RegularExpressions.Regex(@"uri:\s*[""']?([^""'\n]+)[""']?")
                let urlMatch = urlPattern.Match(firstMediaContent)
                let altPattern = System.Text.RegularExpressions.Regex(@"alt_text:\s*[""']?([^""'\n]+)[""']?")
                let altMatch = altPattern.Match(firstMediaContent)
                
                let firstImageUrl = if urlMatch.Success then Some (urlMatch.Groups.[1].Value.Trim()) else None
                let firstImageAlt = if altMatch.Success then Some (altMatch.Groups.[1].Value) else None
                
                {
                    FirstImageUrl = firstImageUrl
                    FirstImageAlt = firstImageAlt
                    MediaCount = mediaMatches.Count
                }
            else
                {
                    FirstImageUrl = None
                    FirstImageAlt = None
                    MediaCount = 0
                }
        with
        | _ -> 
            {
                FirstImageUrl = None
                FirstImageAlt = None
                MediaCount = 0
            }
