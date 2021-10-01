module RssService

    open System
    open System.IO
    open System.Linq
    open System.Xml.Linq
    open Domain

    let title = "Luis Quintanilla"
    let link = "https://www.luisquintanilla.me"
    let description = "Luis Quintanilla's blog"
    // let lastPubDate = "02/09/2021"
    let language = "en"

    let blogEntryXml (entry:Post) =
        
        let filePath = sprintf "%s.html" entry.FileName
        let url = sprintf "https://www.luisquintanilla.me/posts/%s" filePath
        
        XElement(XName.Get "item",
            XElement(XName.Get "title", entry.Metadata.Title),
            XElement(XName.Get "description", entry.Metadata.Description),
            XElement(XName.Get "link", url),
            XElement(XName.Get "guid", url),
            XElement(XName.Get "pubDate", (DateTime.Parse(entry.Metadata.Date).ToShortDateString())))    

    let feedEntryXml (entry:Post) =

        let filePath = sprintf "%s.html" entry.FileName
        let url = sprintf "https://www.luisquintanilla.me/feed/%s" filePath
        
        XElement(XName.Get "item",
            XElement(XName.Get "title", entry.Metadata.Title),
            XElement(XName.Get "link", url),
            XElement(XName.Get "guid", url),
            XElement(XName.Get "pubDate", (DateTime.Parse(entry.Metadata.Date).ToShortDateString())))

    let blogChannelXml (lastPubDate:string) = 
        XElement(XName.Get "rss",
            XAttribute(XName.Get "version","2.0"),
            XElement(XName.Get "channel",
                XElement(XName.Get "title", title),
                XElement(XName.Get "link", link),
                XElement(XName.Get "description", description),
                XElement(XName.Get "lastPubDate", lastPubDate),
                XElement(XName.Get "language", language)))

    let feedChannelXml (title:string) (description:string) (lastPubDate:string) = 
        XElement(XName.Get "rss",
            XAttribute(XName.Get "version","2.0"),
            XElement(XName.Get "channel",
                XElement(XName.Get "title", title),
                XElement(XName.Get "link", link),
                XElement(XName.Get "description", description),
                XElement(XName.Get "lastPubDate", lastPubDate),
                XElement(XName.Get "language", language)))   
             
    let generateBlogRss (posts:Post array) = 
        let latestPost = posts |> Array.sortByDescending(fun post -> DateTime.Parse(post.Metadata.Date)) |> Array.head 
        let entries = posts |> Array.map(blogEntryXml)
        let channel = blogChannelXml (DateTime.Parse(latestPost.Metadata.Date).ToShortDateString())
        
        channel.Descendants(XName.Get "channel").First().Add(entries)
        channel

    let generateMainFeedRss (posts:Post array) =
        let latestPost = posts |> Array.sortByDescending(fun post -> DateTime.Parse(post.Metadata.Date)) |> Array.head 
        let entries = posts |> Array.map(feedEntryXml)
        let channel = feedChannelXml "Luis Quintanilla" "Main Feed" (DateTime.Parse(latestPost.Metadata.Date).ToShortDateString())
        
        channel.Descendants(XName.Get "channel").First().Add(entries)
        channel 