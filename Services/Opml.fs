module OpmlService

    open System
    open System.IO
    open System.Linq
    open System.Text.Json
    open System.Xml.Linq
    open Domain

    let private opmlDocument (head: XElement) (outlines: XElement array) =
        XElement(XName.Get "opml",
            XAttribute(XName.Get "version", "2.0"),
                head,
                XElement(XName.Get "body", outlines |> Array.map (fun outline -> outline :> obj)))

    let private headElement (metadata: OpmlMetadata) =
        let head = XElement(XName.Get "head")
        head.Add(XElement(XName.Get "title", metadata.Title))
        head.Add(XElement(XName.Get "ownerId", metadata.OwnerId))

        match metadata.DocsUrl with
        | Some docsUrl when not (String.IsNullOrWhiteSpace(docsUrl)) ->
            head.Add(XElement(XName.Get "docs", docsUrl))
        | _ ->
            ()

        head

    let private feedOutlineElement (data: Outline) =
        XElement(XName.Get "outline",
            XAttribute(XName.Get "title", data.Title),
            XAttribute(XName.Get "text", data.Title),        
            XAttribute(XName.Get "type", data.Type),
            XAttribute(XName.Get "htmlUrl", data.HtmlUrl),
            XAttribute(XName.Get "xmlUrl", data.XmlUrl))

    let private includeOutlineElement (data: OpmlInclude) =
        if String.IsNullOrWhiteSpace(data.Title) then
            invalidArg (nameof data.Title) "OPML include title cannot be blank."

        if String.IsNullOrWhiteSpace(data.Url) then
            invalidArg (nameof data.Url) "OPML include URL cannot be blank."

        XElement(XName.Get "outline",
            XAttribute(XName.Get "title", data.Title),
            XAttribute(XName.Get "text", data.Title),
            XAttribute(XName.Get "type", "include"),
            XAttribute(XName.Get "url", data.Url))

    let private outlineElement (outline: OpmlOutline) =
        match outline with
        | FeedOutline data -> feedOutlineElement data
        | IncludeOutline data -> includeOutlineElement data

    let buildOpmlDocument (metadata: OpmlMetadata) (outlines: OpmlOutline array) =
        let head = metadata |> headElement
        let outlineElements = outlines |> Array.map outlineElement
        opmlDocument head outlineElements

    let buildOpmlFeed (title: string) (ownerId: string) (links: Outline array) =
        let metadata =
            {
                Title = title
                OwnerId = ownerId
                DocsUrl = None
            }

        links
        |> Array.map FeedOutline
        |> buildOpmlDocument metadata
