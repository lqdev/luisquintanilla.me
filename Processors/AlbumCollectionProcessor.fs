module AlbumCollectionProcessor

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

    // Cache for media data extracted during parsing
    let private mediaDataCache = System.Collections.Concurrent.ConcurrentDictionary<string, AlbumMediaData>()
    
    /// Helper to extract markdown content without frontmatter
    let private extractContentWithoutFrontMatter (rawMarkdown: string) : string =
        let lines = rawMarkdown.Split([|'\n'|], StringSplitOptions.None)
        if lines.Length > 0 && lines.[0].Trim() = "---" then
            // Find the closing ---
            let closingIndex = 
                lines 
                |> Array.skip 1
                |> Array.findIndex (fun line -> line.Trim() = "---")
            // Return everything after the second ---
            lines 
            |> Array.skip (closingIndex + 2)
            |> String.concat "\n"
        else
            rawMarkdown
    
    let create() : ContentProcessor<AlbumCollection> = {
        Parse = fun filePath ->
            match parseAlbumCollectionFromFile filePath with
            | Ok parsedDoc -> 
                match parsedDoc.Metadata with
                | Some metadata -> 
                    let fileName = Path.GetFileNameWithoutExtension(filePath)
                    
                    // Extract media data from raw markdown during parsing
                    let mediaData = MediaDataExtractor.extractAlbumMediaData parsedDoc.RawMarkdown
                    
                    // Store media data in cache for later use in rendering
                    mediaDataCache.[fileName] <- mediaData
                    
                    Ok {
                        FileName = fileName
                        Metadata = metadata
                        Content = extractContentWithoutFrontMatter parsedDoc.RawMarkdown
                        MarkdownSource = Some (extractContentWithoutFrontMatter parsedDoc.RawMarkdown)
                    }
                | None -> Error (Diagnostics.ContentError.ParseFailure(filePath, "frontmatter", "no front-matter block found (expected a leading '---' fence)"))
            | Error e -> Error (Diagnostics.ofParseError filePath e)
        
        Render = fun albumCollection ->
            // Return raw markdown content to be processed by Builder.fs through MarkdownService
            // This allows :::media blocks to be converted to proper HTML by custom block processors
            albumCollection.Content
        
        OutputPath = fun albumCollection ->
            sprintf "collections/albums/%s/index.html" albumCollection.FileName
        
        RenderCard = fun albumCollection ->
            let title = albumCollection.Metadata.Title
            let description = albumCollection.Metadata.Description
            let url = sprintf "/collections/albums/%s/" albumCollection.FileName
            let date = albumCollection.Metadata.Date
            
            // Get media data from cache (extracted during parsing)
            let mediaData = 
                match mediaDataCache.TryGetValue(albumCollection.FileName) with
                | (true, data) -> data
                | _ -> { FirstImageUrl = None; FirstImageAlt = None; MediaCount = 0 }
            
            // Generate content preview using cached data
            let contentPreview = 
                match mediaData.FirstImageUrl with
                | Some imageUrl ->
                    let altText = mediaData.FirstImageAlt |> Option.defaultValue "Album preview"
                    sprintf """<div class="album-preview"><img src="%s" alt="%s" class="img-fluid" /><p class="mt-2">%s</p></div>""" imageUrl altText description
                | None -> sprintf "<p>%s</p>" description
            
            let viewNode = 
                article [ _class "album-collection-card h-entry" ] [
                    h2 [] [ a [ _href url ] [ Text title ] ]
                    div [ _class "content-preview" ] [ rawText contentPreview ]
                    if mediaData.MediaCount > 0 then
                        p [ _class "text-muted" ] [ Text (sprintf "%d items" mediaData.MediaCount) ]
                    div [ _class "mt-2" ] [
                        a [ _href url; _class "btn btn-outline-primary btn-sm" ] [ Text "View Album →" ]
                    ]
                ]
            RenderView.AsString.xmlNode viewNode
        
        RenderRss = fun albumCollection ->
            // Create RSS item for album collection
            let url = sprintf "https://www.lqdev.me/collections/albums/%s/" albumCollection.FileName
            let description = albumCollection.Metadata.Description
            
            // Get media count from cache
            let mediaData = 
                match mediaDataCache.TryGetValue(albumCollection.FileName) with
                | (true, data) -> data
                | _ -> { FirstImageUrl = None; FirstImageAlt = None; MediaCount = 0 }
            
            let enhancedDescription = 
                if mediaData.MediaCount > 0 then
                    sprintf "%s (%d items)" description mediaData.MediaCount
                else
                    description
            
            // Normalize URLs in description for RSS compatibility
            let normalizedDescription = normalizeUrlsForRss enhancedDescription "https://www.lqdev.me"
            
            // Create RSS category elements for tags
            let categories = 
                albumCollection.Metadata.Tags
                |> Array.map (fun tag -> XElement(XName.Get "category", tag))
                |> Array.toList
            
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", albumCollection.Metadata.Title),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" normalizedDescription),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url),
                    XElement(XName.Get "pubDate", albumCollection.Metadata.Date))
            
            // Add categories if they exist
            if not (List.isEmpty categories) then
                item.Add(categories |> List.toArray)
            
            // Add source:markdown if available
            match generateSourceMarkdown albumCollection.MarkdownSource with
            | Some sourceElement -> item.Add(sourceElement)
            | None -> ()
                
            Some item
    }
