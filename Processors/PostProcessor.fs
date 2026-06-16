module PostProcessor

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

    // Helper to extract markdown content without frontmatter
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

    let create() : ContentProcessor<Post> = {
        Parse = fun filePath ->
            match parsePostFromFile filePath with
            | Ok parsedDoc -> 
                match parsedDoc.Metadata with
                | Some metadata -> 
                    let contentWithoutFrontmatter = extractContentWithoutFrontMatter parsedDoc.RawMarkdown
                    let readingTime = ReadingTimeService.calculateReadingTime contentWithoutFrontmatter
                    Ok {
                        FileName = Path.GetFileNameWithoutExtension(filePath)
                        Metadata = { metadata with ReadingTimeMinutes = readingTime }
                        Content = contentWithoutFrontmatter  // Use raw markdown without frontmatter
                        MarkdownSource = Some contentWithoutFrontmatter
                    }
                | None -> Error (Diagnostics.ContentError.ParseFailure(filePath, "frontmatter", "no front-matter block found (expected a leading '---' fence)"))
            | Error e -> Error (Diagnostics.ofParseError filePath e)
        
        Render = fun post ->
            // Return raw markdown content to be processed by the calling code
            // The Builder will apply MarkdownService.convertMdToHtml to this content
            post.Content
        
        OutputPath = fun post ->
            sprintf "posts/%s/index.html" post.FileName
        
        RenderCard = fun post ->
            let title = Html.escapeHtml post.Metadata.Title
            let description = 
                if isNull post.Metadata.Description then 
                    "No description available"
                else 
                    Html.escapeHtml post.Metadata.Description
            let url = sprintf "/posts/%s/" post.FileName
            let date = post.Metadata.Date
            
            Html.element "article" (Html.attribute "class" "post-card")
                (Html.element "h2" "" (Html.element "a" (Html.attribute "href" url) title) +
                 Html.element "p" "" description)
        
        RenderRss = fun post ->
            // Create RSS item for post using existing pattern
            let url = sprintf "https://www.lqdev.me/posts/%s" post.FileName
            let categories = 
                if isNull post.Metadata.Tags then []
                else post.Metadata.Tags |> Array.map (fun tag -> XElement(XName.Get "category", tag)) |> Array.toList
            
            // Normalize URLs in content for RSS compatibility
            let normalizedContent = normalizeUrlsForRss post.Content "https://www.lqdev.me"
            
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", post.Metadata.Title),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" normalizedContent),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url),
                    XElement(XName.Get "pubDate", post.Metadata.Date))
            
            // Add categories if they exist
            if not (List.isEmpty categories) then
                item.Add(categories |> List.toArray)
            
            // Add source:markdown if available
            match generateSourceMarkdown post.MarkdownSource with
            | Some sourceElement -> item.Add(sourceElement)
            | None -> ()
            
            Some item
    }
