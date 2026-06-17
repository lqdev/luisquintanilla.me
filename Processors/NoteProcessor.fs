module NoteProcessor

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

    let create() : ContentProcessor<Post> = {
        Parse = fun filePath ->
            match parsePostFromFile filePath with
            | Ok parsedDoc -> 
                match parsedDoc.Metadata with
                | Some metadata -> 
                    let readingTime = ReadingTimeService.calculateReadingTime parsedDoc.TextContent
                    Ok {
                        FileName = Path.GetFileNameWithoutExtension(filePath)
                        Metadata = { metadata with ReadingTimeMinutes = readingTime }
                        Content = parsedDoc.TextContent  // Raw markdown content
                        MarkdownSource = Some parsedDoc.RawMarkdown
                    }
                | None -> Error (Diagnostics.ContentError.ParseFailure(filePath, "frontmatter", "no front-matter block found (expected a leading '---' fence)"))
            | Error e -> Error (Diagnostics.ofParseError filePath e)
        
        Render = fun note ->
            // Return ViewEngine node rendered to HTML string
            let viewNode = article [ _class "note" ] [ rawText note.Content ]
            RenderView.AsString.xmlNode viewNode
        
        OutputPath = fun note ->
            sprintf "notes/%s/index.html" note.FileName
        
        RenderCard = fun note ->
            let title = Html.escapeHtml note.Metadata.Title
            let description = 
                if isNull note.Metadata.Description then 
                    "No description available"
                else 
                    Html.escapeHtml note.Metadata.Description
            let url = sprintf "/notes/%s/" note.FileName
            let date = note.Metadata.Date
            
            // Include content excerpt like other processors
            let contentExcerpt = 
                if note.Content.Length > 200 then
                    note.Content.Substring(0, 200) + "..."
                else
                    note.Content
            
            // Use ViewEngine for consistency with Render function
            let viewNode = 
                article [ _class "note-card" ] [
                    h2 [] [ a [ _href url ] [ Text title ] ]
                    p [ _class "description" ] [ Text description ]
                    div [ _class "content-excerpt" ] [ Text contentExcerpt ]
                ]
            RenderView.AsString.xmlNode viewNode
        
        RenderRss = fun note ->
            // Create RSS item for note using existing pattern
            let url = sprintf "https://www.lqdev.me/notes/%s" note.FileName
            let categories = 
                if isNull note.Metadata.Tags then []
                else note.Metadata.Tags |> Array.map (fun tag -> XElement(XName.Get "category", tag)) |> Array.toList
            
            // Normalize URLs in content for RSS compatibility
            let normalizedContent = normalizeUrlsForRss note.Content "https://www.lqdev.me"
            
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", note.Metadata.Title),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" normalizedContent),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url),
                    XElement(XName.Get "pubDate", note.Metadata.Date))
            
            // Add categories if they exist
            if not (List.isEmpty categories) then
                item.Add(categories |> List.toArray)
            
            // Add source:markdown if available
            match generateSourceMarkdown note.MarkdownSource with
            | Some sourceElement -> item.Add(sourceElement)
            | None -> ()
            
            Some item
    }
