module PresentationProcessor

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

    let create() : ContentProcessor<Presentation> = {
        Parse = fun filePath ->
            match parsePresentationFromFile filePath with
            | Ok parsedDoc -> 
                match parsedDoc.Metadata with
                | Some metadata -> 
                    // For presentations, we need raw markdown content (not HTML) for reveal.js
                    // Extract the content without frontmatter from the already parsed raw markdown
                    let lines = parsedDoc.RawMarkdown.Split([|'\n'|], StringSplitOptions.None)
                    
                    let markdownContent = 
                        if lines.Length > 0 && lines.[0].Trim() = "---" then
                            let endIdx = 
                                lines 
                                |> Array.skip 1
                                |> Array.tryFindIndex (fun line -> line.Trim() = "---")
                            match endIdx with
                            | Some idx -> 
                                lines.[(idx + 2)..]
                                |> String.concat "\n"
                            | None -> parsedDoc.RawMarkdown
                        else parsedDoc.RawMarkdown
                    
                    Ok {
                        FileName = System.IO.Path.GetFileNameWithoutExtension(filePath)
                        Metadata = metadata
                        Content = markdownContent  // Store raw markdown for reveal.js
                        MarkdownSource = Some markdownContent
                    }
                | None -> Error (Diagnostics.ContentError.ParseFailure(filePath, "frontmatter", "no front-matter block found (expected a leading '---' fence)"))
            | Error e -> Error (Diagnostics.ofParseError filePath e)
        
        Render = fun presentation ->
            // Return raw content for reveal.js processing in presentationPageView
            presentation.Content
        
        OutputPath = fun presentation ->
            sprintf "presentations/%s.html" presentation.FileName
        
        RenderCard = fun presentation ->
            let title = Html.escapeHtml presentation.Metadata.Title
            let excerpt = Html.escapeHtml (presentation.Content.Substring(0, min 150 presentation.Content.Length) + "...")
            let url = sprintf "/resources/presentations/%s/" presentation.FileName
            
            // Add resources display
            let resourcesHtml = 
                if presentation.Metadata.Resources.Length > 0 then
                    let resourceLinks = 
                        presentation.Metadata.Resources
                        |> Array.map (fun resource -> 
                            Html.element "a" (Html.attribute "href" resource.Url) (Html.escapeHtml resource.Text))
                        |> String.concat " | "
                    Html.element "div" (Html.attribute "class" "resources") resourceLinks
                else ""
            
            Html.element "article" (Html.attribute "class" "presentation-card")
                (Html.element "h2" "" (Html.element "a" (Html.attribute "href" url) title) +
                 Html.element "p" "" excerpt +
                 resourcesHtml)
        
        RenderRss = fun presentation ->
            // Create RSS item for presentation
            let url = sprintf "https://www.lqdev.me/resources/presentations/%s" presentation.FileName
            let categories = 
                if String.IsNullOrEmpty(presentation.Metadata.Tags) then []
                else presentation.Metadata.Tags.Split(',') 
                     |> Array.map (fun tag -> XElement(XName.Get "category", tag.Trim())) 
                     |> Array.toList
            
            // Normalize URLs in content for RSS compatibility
            let normalizedContent = normalizeUrlsForRss presentation.Content "https://www.lqdev.me"
            
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", presentation.Metadata.Title),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" normalizedContent),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url))
            
            // Add pubDate if date exists
            if not (String.IsNullOrEmpty(presentation.Metadata.Date)) then
                item.Add(XElement(XName.Get "pubDate", presentation.Metadata.Date))
            
            // Add categories if they exist
            if not (List.isEmpty categories) then
                item.Add(categories |> List.toArray)
            
            // Add source:markdown if available
            match generateSourceMarkdown presentation.MarkdownSource with
            | Some sourceElement -> item.Add(sourceElement)
            | None -> ()
                
            Some item
    }
