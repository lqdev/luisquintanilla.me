module WikiProcessor

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

    let create() : ContentProcessor<Wiki> = {
        Parse = fun filePath ->
            match parseWikiFromFile filePath with
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
        
        Render = fun wiki ->
            let viewNode = article [] [ rawText wiki.Content ]
            RenderView.AsString.xmlNode viewNode
        
        OutputPath = fun wiki ->
            sprintf "wiki/%s.html" wiki.FileName
        
        RenderCard = fun wiki ->
            let title = Html.escapeHtml wiki.Metadata.Title
            let excerpt = Html.escapeHtml (wiki.Content.Substring(0, min 150 wiki.Content.Length) + "...")
            let url = sprintf "/resources/wiki/%s/" wiki.FileName
            
            Html.element "article" (Html.attribute "class" "wiki-card")
                (Html.element "h2" "" (Html.element "a" (Html.attribute "href" url) title) +
                 Html.element "p" "" excerpt)
        
        RenderRss = fun wiki ->
            // Create RSS item for wiki similar to post
            let url = sprintf "https://www.lqdev.me/resources/wiki/%s" wiki.FileName
            let categories = 
                if String.IsNullOrEmpty(wiki.Metadata.Tags) then []
                else wiki.Metadata.Tags.Split(',') 
                     |> Array.map (fun tag -> XElement(XName.Get "category", tag.Trim())) 
                     |> Array.toList
            
            // Normalize URLs in content for RSS compatibility
            let normalizedContent = normalizeUrlsForRss wiki.Content "https://www.lqdev.me"
            
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", wiki.Metadata.Title),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" normalizedContent),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url))
            
            // Add pubDate if last_updated_date exists
            if not (String.IsNullOrEmpty(wiki.Metadata.LastUpdatedDate)) then
                item.Add(XElement(XName.Get "pubDate", wiki.Metadata.LastUpdatedDate))
            
            // Add categories if they exist
            if not (List.isEmpty categories) then
                item.Add(categories |> List.toArray)
            
            // Add source:markdown if available
            match generateSourceMarkdown wiki.MarkdownSource with
            | Some sourceElement -> item.Add(sourceElement)
            | None -> ()
                
            Some item
    }
