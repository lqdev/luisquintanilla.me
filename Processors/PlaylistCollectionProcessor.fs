module PlaylistCollectionProcessor

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
    
    let create() : ContentProcessor<PlaylistCollection> = {
        Parse = fun filePath ->
            match parsePlaylistCollectionFromFile filePath with
            | Ok parsedDoc -> 
                match parsedDoc.Metadata with
                | Some metadata -> 
                    let fileName = Path.GetFileNameWithoutExtension(filePath)
                    
                    Ok {
                        FileName = fileName
                        Metadata = metadata
                        Content = extractContentWithoutFrontMatter parsedDoc.RawMarkdown
                        MarkdownSource = Some (extractContentWithoutFrontMatter parsedDoc.RawMarkdown)
                    }
                | None -> Error (Diagnostics.ContentError.ParseFailure(filePath, "frontmatter", "no front-matter block found (expected a leading '---' fence)"))
            | Error e -> Error (Diagnostics.ofParseError filePath e)
        
        Render = fun playlistCollection ->
            // Return raw markdown content to be processed by Builder.fs through MarkdownService
            playlistCollection.Content
        
        OutputPath = fun playlistCollection ->
            sprintf "collections/playlists/%s/index.html" playlistCollection.FileName
        
        RenderCard = fun playlistCollection ->
            let title = playlistCollection.Metadata.Title
            let description = playlistCollection.Metadata.Description |> Option.defaultValue ""
            let url = sprintf "/collections/playlists/%s/" playlistCollection.FileName
            let date = playlistCollection.Metadata.Date
            
            let viewNode = 
                article [ _class "playlist-collection-card h-entry" ] [
                    h2 [] [ a [ _href url ] [ Text title ] ]
                    if not (String.IsNullOrEmpty(description)) then
                        p [ _class "content-preview" ] [ Text description ]
                    div [ _class "mt-2" ] [
                        a [ _href url; _class "btn btn-outline-primary btn-sm" ] [ Text "View Playlist →" ]
                    ]
                ]
            RenderView.AsString.xmlNode viewNode
        
        RenderRss = fun playlistCollection ->
            // Create RSS item for playlist collection
            let url = sprintf "https://www.lqdev.me/collections/playlists/%s/" playlistCollection.FileName
            let description = playlistCollection.Metadata.Description |> Option.defaultValue ""
            
            // Normalize URLs in description for RSS compatibility
            let normalizedDescription = normalizeUrlsForRss description "https://www.lqdev.me"
            
            // Create RSS category elements for tags
            let categories = 
                playlistCollection.Metadata.Tags
                |> Array.map (fun tag -> XElement(XName.Get "category", tag))
                |> Array.toList
            
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", playlistCollection.Metadata.Title),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" normalizedDescription),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url),
                    XElement(XName.Get "pubDate", playlistCollection.Metadata.Date))
            
            // Add categories if they exist
            if not (List.isEmpty categories) then
                item.Add(categories |> List.toArray)
            
            // Add source:markdown if available
            match generateSourceMarkdown playlistCollection.MarkdownSource with
            | Some sourceElement -> item.Add(sourceElement)
            | None -> ()
                
            Some item
    }
