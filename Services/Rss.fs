module RssService

    open System
    open System.IO
    open System.Linq
    open System.Xml.Linq
    open Domain

    let title = "Luis Quintanilla"
    let link = "https://luisquintanilla.me"
    let description = "Luis Quintanilla's personal website"
    let lastPubDate = "02/05/2021"
    let language = "en"

    let entryXml (entry:Post) =
        
        let filePath = sprintf "%s.html" entry.FileName
        let url = sprintf "https://luisquintanilla/posts/%s" filePath
        
        XElement(XName.Get "item",
            XElement(XName.Get "title", entry.Metadata.Title),
            XElement(XName.Get "link", url),
            XElement(XName.Get "guid", url),
            XElement(XName.Get "pubDate", entry.Metadata.Date))    

    let channelXml = 
        XElement(XName.Get "rss",
            XAttribute(XName.Get "version","2.0"),
            XElement(XName.Get "channel",
                XElement(XName.Get "title", title),
                XElement(XName.Get "link", link),
                XElement(XName.Get "description", description),
                XElement(XName.Get "lastPubDate", lastPubDate),
                XElement(XName.Get "language", language)))
                
    let generateRss (posts:Post array) = 
        let entries = posts |> Array.map(entryXml)
        channelXml.Descendants(XName.Get "channel").First().Add(entries)
        channelXml