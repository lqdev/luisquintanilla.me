module BookmarkProcessor

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

    let create() : ContentProcessor<Bookmark> = {
        Parse = fun filePath ->
            match parseBookmarkFromFile filePath with
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
        
        Render = fun bookmark ->
            // Bookmark rendering with IndieWeb microformat support
            let viewNode = article [ _class "h-entry bookmark" ] [
                a [ _class "u-bookmark-of"; _href bookmark.Metadata.BookmarkOf ] [ str bookmark.Metadata.Title ]
                rawText bookmark.Content
            ]
            RenderView.AsString.xmlNode viewNode
        
        OutputPath = fun bookmark ->
            sprintf "bookmarks/%s.html" bookmark.FileName
        
        RenderCard = fun bookmark ->
            let title = Html.escapeHtml bookmark.Metadata.Title
            let bookmarkOf = Html.escapeHtml bookmark.Metadata.BookmarkOf
            let description = Html.escapeHtml bookmark.Metadata.Description
            let url = sprintf "/bookmarks/%s/" bookmark.FileName
            let date = bookmark.Metadata.DatePublished
            
            // IndieWeb microformat card for bookmarks
            Html.element "article" (Html.attribute "class" "bookmark-card h-entry")
                (Html.element "h2" "" (Html.element "a" (Html.attribute "href" url) title) +
                 Html.element "div" (Html.attribute "class" "bookmark-target") 
                    (sprintf "🔖 %s" (Html.element "a" (Html.attribute "href" bookmarkOf + Html.attribute "class" "u-bookmark-of") bookmarkOf)) +
                 Html.element "div" (Html.attribute "class" "bookmark-description") description)
        
        RenderRss = fun bookmark ->
            // Create RSS item for bookmark
            let url = sprintf "https://www.lqdev.me/bookmarks/%s" bookmark.FileName
            let description = sprintf "Bookmark: %s - %s" bookmark.Metadata.Description bookmark.Metadata.BookmarkOf
            
            // Normalize URLs in description for RSS compatibility
            let normalizedDescription = normalizeUrlsForRss description "https://www.lqdev.me"
            
            let item = 
                XElement(XName.Get "item",
                    XElement(XName.Get "title", bookmark.Metadata.Title),
                    XElement(XName.Get "description", sprintf "<![CDATA[%s]]>" normalizedDescription),
                    XElement(XName.Get "link", url),
                    XElement(XName.Get "guid", url),
                    XElement(XName.Get "pubDate", bookmark.Metadata.DatePublished))
            
            // Add source:markdown if available
            match generateSourceMarkdown bookmark.MarkdownSource with
            | Some sourceElement -> item.Add(sourceElement)
            | None -> ()
            
            Some item
    }
