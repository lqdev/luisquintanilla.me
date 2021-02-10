module RssService

    open System
    open System.IO
    open System.Linq
    open System.Xml.Linq
    open Domain

    let title = "Luis Quintanilla"
    let link = "https://www.luisquintanilla.me"
    let description = "Luis Quintanilla's personal website"
    // let lastPubDate = "02/09/2021"
    let language = "en"

    let entryXml (entry:Post) =
        
        let filePath = sprintf "%s.html" entry.FileName
        let url = sprintf "https://www.luisquintanilla.me/posts/%s" filePath
        
        XElement(XName.Get "item",
            XElement(XName.Get "title", entry.Metadata.Title),
            XElement(XName.Get "link", url),
            XElement(XName.Get "guid", url),
            XElement(XName.Get "pubDate", (DateTime.Parse(entry.Metadata.Date).ToShortDateString())))    

    let channelXml (lastPubDate:string) = 
        XElement(XName.Get "rss",
            XAttribute(XName.Get "version","2.0"),
            XElement(XName.Get "channel",
                XElement(XName.Get "title", title),
                XElement(XName.Get "link", link),
                XElement(XName.Get "description", description),
                XElement(XName.Get "lastPubDate", lastPubDate),
                XElement(XName.Get "language", language)))
                
    let generateRss (posts:Post array) = 
        let latestPost = posts |> Array.sortByDescending(fun post -> post.Metadata.Date) |> Array.head 
        let entries = posts |> Array.map(entryXml)
        let channel = channelXml (DateTime.Parse(latestPost.Metadata.Date).ToShortDateString())
        
        channel.Descendants(XName.Get "channel").First().Add(entries)
        channel