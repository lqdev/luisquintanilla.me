module AiMemexProcessor

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

    let private extractContentWithoutFrontMatter (rawMarkdown: string) : string =
        let lines = rawMarkdown.Split([|'\n'|], StringSplitOptions.None)
        if lines.Length > 0 && lines.[0].Trim() = "---" then
            let closingIndex = 
                lines 
                |> Array.skip 1
                |> Array.findIndex (fun line -> line.Trim() = "---")
            lines 
            |> Array.skip (closingIndex + 2)
            |> String.concat "\n"
        else
            rawMarkdown

    let create() : ContentProcessor<AiMemex> = {
        Parse = fun filePath ->
            match parseAiMemexFromFile filePath with
            | Ok parsedDoc -> 
                match parsedDoc.Metadata with
                | Some metadata -> 
                    Ok {
                        FileName = Path.GetFileNameWithoutExtension(filePath)
                        Metadata = metadata
                        Content = parsedDoc.TextContent
                        MarkdownSource = Some (extractContentWithoutFrontMatter parsedDoc.RawMarkdown)
                    }
                | None -> Error (Diagnostics.ContentError.ParseFailure(filePath, "frontmatter", "no front-matter block found (expected a leading '---' fence)"))
            | Error e -> Error (Diagnostics.ofParseError filePath e)
        
        Render = fun aiMemex ->
            let viewNode = article [] [ rawText aiMemex.Content ]
            RenderView.AsString.xmlNode viewNode
        
        OutputPath = fun aiMemex ->
            sprintf "resources/ai-memex/%s.html" aiMemex.FileName
        
        RenderCard = fun aiMemex ->
            let title = Html.escapeHtml aiMemex.Metadata.Title
            let excerpt = Html.escapeHtml (aiMemex.Content.Substring(0, min 150 aiMemex.Content.Length) + "...")
            let url = sprintf "/resources/ai-memex/%s/" aiMemex.FileName
            
            Html.element "article" (Html.attribute "class" "ai-memex-card")
                (Html.element "h2" "" (Html.element "a" (Html.attribute "href" url) title) +
                 Html.element "p" "" excerpt)
        
        RenderRss = fun aiMemex ->
            let url = sprintf "https://www.lqdev.me/resources/ai-memex/%s" aiMemex.FileName
            let categories = 
                if String.IsNullOrEmpty(aiMemex.Metadata.Tags) then []
                else aiMemex.Metadata.Tags.Split(',') 
                     |> Array.map (fun tag -> XElement(XName.Get "category", tag.Trim())) 
                     |> Array.toList
            
            // Normalize URLs in content for RSS compatibility
            let normalizedContent = normalizeUrlsForRss aiMemex.Content "https://www.lqdev.me"
            
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", aiMemex.Metadata.Title),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" normalizedContent),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url))
            
            // Add pubDate if published_date exists
            if not (String.IsNullOrEmpty(aiMemex.Metadata.PublishedDate)) then
                item.Add(XElement(XName.Get "pubDate", aiMemex.Metadata.PublishedDate))
            
            // Add categories if they exist
            if not (List.isEmpty categories) then
                item.Add(categories |> List.toArray)
            
            // Add source:markdown if available
            match generateSourceMarkdown aiMemex.MarkdownSource with
            | Some sourceElement -> item.Add(sourceElement)
            | None -> ()
                
            Some item
    }
