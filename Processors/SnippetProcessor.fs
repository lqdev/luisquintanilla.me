module SnippetProcessor

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

    let create() : ContentProcessor<Snippet> = {
        Parse = fun filePath ->
            match parseSnippetFromFile filePath with
            | Ok parsedDoc -> 
                match parsedDoc.Metadata with
                | Some metadata -> 
                    Ok {
                        FileName = Path.GetFileNameWithoutExtension(filePath)
                        Metadata = metadata
                        Content = parsedDoc.TextContent
                        MarkdownSource = Some parsedDoc.RawMarkdown
                    }
                | None -> Error (Diagnostics.ContentError.ParseFailure(filePath, "frontmatter", "no front-matter block found (expected a leading '---' fence)"))
            | Error e -> Error (Diagnostics.ofParseError filePath e)
        
        Render = fun snippet ->
            let viewNode = article [] [ rawText snippet.Content ]
            RenderView.AsString.xmlNode viewNode
        
        OutputPath = fun snippet ->
            sprintf "snippets/%s.html" snippet.FileName
        
        RenderCard = fun snippet ->
            let title = Html.escapeHtml snippet.Metadata.Title
            let excerpt = Html.escapeHtml (snippet.Content.Substring(0, min 150 snippet.Content.Length) + "...")
            let url = sprintf "/resources/snippets/%s/" snippet.FileName
            
            Html.element "article" (Html.attribute "class" "snippet-card")
                (Html.element "h2" "" (Html.element "a" (Html.attribute "href" url) title) +
                 Html.element "p" "" excerpt)
        
        RenderRss = fun snippet ->
            // Create RSS item for snippet similar to post
            let url = sprintf "https://www.lqdev.me/resources/snippets/%s" snippet.FileName
            let categories = 
                if String.IsNullOrEmpty(snippet.Metadata.Tags) then []
                else snippet.Metadata.Tags.Split(',') 
                     |> Array.map (fun tag -> XElement(XName.Get "category", tag.Trim())) 
                     |> Array.toList
            
            // Normalize URLs in content for RSS compatibility
            let normalizedContent = normalizeUrlsForRss snippet.Content "https://www.lqdev.me"
            
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", snippet.Metadata.Title),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" normalizedContent),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url))
            
            // Add pubDate if created_date exists
            if not (String.IsNullOrEmpty(snippet.Metadata.CreatedDate)) then
                item.Add(XElement(XName.Get "pubDate", snippet.Metadata.CreatedDate))
            
            // Add categories if they exist
            if not (List.isEmpty categories) then
                item.Add(categories |> List.toArray)
            
            // Add source:markdown if available
            match generateSourceMarkdown snippet.MarkdownSource with
            | Some sourceElement -> item.Add(sourceElement)
            | None -> ()
                
            Some item
    }
