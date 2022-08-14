module OpmlService

    open System.IO
    open System.Linq
    open System.Text.Json
    open System.Xml.Linq
    open Domain

    let opmlFeed (head:XElement) = 
        XElement(XName.Get "opml",
            XAttribute(XName.Get "version", "2.0"),
                head,
                XElement(XName.Get "body"))

    let headElement (metadata:OpmlMetadata) = 
            XElement(XName.Get "head",
                XElement(XName.Get "title", metadata.Title),
                XElement(XName.Get "ownerId", metadata.OwnerId))

    let outlineElement (data:Outline) = 
        XElement(XName.Get "outline",
            XAttribute(XName.Get "title", data.Title),
            XAttribute(XName.Get "text", data.Title),        
            XAttribute(XName.Get "type", data.Type),
            XAttribute(XName.Get "htmlUrl", data.HtmlUrl),
            XAttribute(XName.Get "xmlUrl", data.XmlUrl))

    let buildOpmlFeed (title:string) (ownerId:string) (links:Outline array) = 
        
        let head = 
            {
                Title=title
                OwnerId=ownerId
            }
            |> headElement
        
        let outlines = links |> Array.map(outlineElement)
        
        let feed =  opmlFeed head 

        feed.Descendants(XName.Get "body").First().Add(outlines)
        feed
