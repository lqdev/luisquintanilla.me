module RssService

    open System
    open System.IO
    open System.Linq
    open System.Xml.Linq
    open Domain

    let title = "Luis Quintanilla Blog"
    let link = "https://www.luisquintanilla.me"
    let description = "Luis Quintanilla's blog"
    // let lastPubDate = "02/09/2021"
    let language = "en"

    let blogEntryXml (entry:Post) =
        
        let filePath = sprintf "%s.html" entry.FileName
        let url = $"https://www.luisquintanilla.me/posts/{filePath}"
        let urlWithUtm = $"{url}?utm_medium=feed"
        
        let description = $"{entry.Metadata.Description}. See the full post at <a href=\"{urlWithUtm}\">{url}</a>"

        XElement(XName.Get "item",
            XElement(XName.Get "title", entry.Metadata.Title),
            XElement(XName.Get "description", description),
            XElement(XName.Get "link", urlWithUtm),
            XElement(XName.Get "guid", url),
            XElement(XName.Get "pubDate", entry.Metadata.Date))    

    let feedEntryXml (entry:Post) =

        let filePath = sprintf "%s.html" entry.FileName
        let url = $"https://www.luisquintanilla.me/feed/{filePath}"
        let urlWithUtm = $"{url}?utm_medium=feed"
        
        XElement(XName.Get "item",
            XElement(XName.Get "title", entry.Metadata.Title),
            XElement(XName.Get "description", $"See the post at <a href=\"{urlWithUtm}\">{url}</a>"),            
            XElement(XName.Get "link", urlWithUtm),
            XElement(XName.Get "guid", url),
            XElement(XName.Get "pubDate", entry.Metadata.Date))

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
        let channel = blogChannelXml latestPost.Metadata.Date
        
        channel.Descendants(XName.Get "channel").First().Add(entries)
        channel

    let generateMainFeedRss (posts:Post array) =
        let latestPost = posts |> Array.sortByDescending(fun post -> DateTime.Parse(post.Metadata.Date)) |> Array.head 
        let entries = posts |> Array.map(feedEntryXml)
        let channel = feedChannelXml "Luis Quintanilla Feed" "Main Feed" latestPost.Metadata.Date
        
        channel.Descendants(XName.Get "channel").First().Add(entries)
        channel 