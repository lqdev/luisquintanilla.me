module AlbumProcessor

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
    
    /// Convert AlbumImage to :::media block markdown syntax
    let private convertImageToMediaBlock (image: AlbumImage) : string =
        sprintf ":::media\nmedia_type: image\nuri: %s\nalt_text: %s\ncaption: %s\naspect: \"\"\n:::" 
            image.ImagePath image.AltText image.Description
    
    /// Convert album images to :::media blocks and combine with existing content
    let private convertAlbumToMarkdown (album: Album) (existingContent: string) : string =
        // For new :::media block format, just return the existing content
        // The AST parsing will handle :::media blocks automatically
        existingContent
    
    let create() : ContentProcessor<Album> = {
        Parse = fun filePath ->
            match parseAlbumFromFile filePath with
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
                        Content = extractContentWithoutFrontMatter parsedDoc.RawMarkdown  // Use raw markdown without frontmatter
                        MarkdownSource = Some (extractContentWithoutFrontMatter parsedDoc.RawMarkdown)
                    }
                | None -> Error (Diagnostics.ContentError.ParseFailure(filePath, "frontmatter", "no front-matter block found (expected a leading '---' fence)"))
            | Error e -> Error (Diagnostics.ofParseError filePath e)
        
        Render = fun album ->
            // Return raw markdown content to be processed by Builder.fs through MarkdownService
            // This allows :::media blocks to be converted to proper HTML by custom block processors
            album.Content
        
        OutputPath = fun album ->
            sprintf "media/%s/index.html" album.FileName
        
        RenderCard = fun album ->
            let title = album.Metadata.Title
            let url = sprintf "/media/%s/" album.FileName
            let date = album.Metadata.Date
            
            // Get media data from cache (extracted during parsing)
            let mediaData = 
                match mediaDataCache.TryGetValue(album.FileName) with
                | (true, data) -> data
                | _ -> { FirstImageUrl = None; FirstImageAlt = None; MediaCount = 0 }
            
            // Generate content preview using cached data
            let contentPreview = 
                match mediaData.FirstImageUrl with
                | Some imageUrl ->
                    let altText = mediaData.FirstImageAlt |> Option.defaultValue "Media preview"
                    sprintf """<img src="%s" alt="%s" class="img-fluid" />""" imageUrl altText
                | None -> "Photo album"
            
            let viewNode = 
                article [ _class "album-card h-entry" ] [
                    h2 [] [ a [ _href url ] [ Text title ] ]
                    div [ _class "content-preview" ] [ rawText contentPreview ]
                    div [ _class "mt-2" ] [
                        a [ _href url; _class "btn btn-outline-primary btn-sm" ] [ Text "Read More →" ]
                    ]
                ]
            RenderView.AsString.xmlNode viewNode
        
        RenderRss = fun album ->
            // Render the same authored markdown/media blocks used by the page.
            let url = sprintf "%s/media/%s" Constants.Urls.canonical album.FileName
            let normalizedDescription =
                MarkdownService.convertMdToHtml album.Content
                |> fun html -> normalizeRssHtmlUrls html url
            
            let categories = 
                if isNull album.Metadata.Tags then []
                else album.Metadata.Tags |> Array.map (fun tag -> XElement(XName.Get "category", tag)) |> Array.toList
            
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", album.Metadata.Title),
                    rssDescriptionElement normalizedDescription,
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url))

            match rssPubDateElement (sprintf "media/%s.md (published_date)" album.FileName) album.Metadata.Date with
            | Some pubDate -> item.Add(pubDate)
            | None -> ()

            let mediaElements = mediaRssContentElements album.Content url
            if not mediaElements.IsEmpty then
                item.Add(mediaElements |> List.toArray)
            
            // Add categories if they exist
            if not (List.isEmpty categories) then
                item.Add(categories |> List.toArray)
            
            // Add source:markdown if available
            match generateSourceMarkdown album.MarkdownSource with
            | Some sourceElement -> item.Add(sourceElement)
            | None -> ()
                
            Some item
    }
