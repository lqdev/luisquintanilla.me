module RssService

    open System
    open System.IO
    open System.Linq
    open System.Xml.Linq
    open Domain
    open MarkdownService

    let title = "Luis Quintanilla Blog"
    let link = "https://www.luisquintanilla.me"
    let description = "Luis Quintanilla's blog"
    // let lastPubDate = "02/09/2021"
    let language = "en"

    let blogEntryXml (entry:Post) =
        
        let url = $"https://www.luisquintanilla.me/posts/{entry.FileName}"
        let urlWithUtm = $"{url}?utm_medium=feed"
        
        let description = $"{entry.Metadata.Description}. See the full post at <a href=\"{urlWithUtm}\">{url}</a>"

        XElement(XName.Get "item",
            XElement(XName.Get "title", entry.Metadata.Title),
            XElement(XName.Get "description", description),
            XElement(XName.Get "link", urlWithUtm),
            XElement(XName.Get "guid", url),
            XElement(XName.Get "pubDate", entry.Metadata.Date))    

    let feedEntryXml (entry:Post) =

        let url = $"https://www.luisquintanilla.me/feed/{entry.FileName}"
        let urlWithUtm = $"{url}?utm_medium=feed"
        
        let content = entry.Content |> convertMdToHtml
        let cdata = $"<![CDATA[<p>See the original post at <a href=\"{urlWithUtm}\">{url}</a></p><br><br>{content}]]>"

        XElement(XName.Get "item",
            XElement(XName.Get "title", entry.Metadata.Title),
            XElement(XName.Get "description", cdata),            
            XElement(XName.Get "link", urlWithUtm),
            XElement(XName.Get "guid", url),
            XElement(XName.Get "pubDate", entry.Metadata.Date))

    let reponseFeedEntryXml (entry:Response) =

        let url = $"https://www.luisquintanilla.me/feed/{entry.FileName}"
        let urlWithUtm = $"{url}?utm_medium=feed"
        
        let content = entry.Content |> convertMdToHtml
        let cdata = $"See the original post at <a href=\"{urlWithUtm}\">{url}</a><br><br><![CDATA[{content}]]>"

        XElement(XName.Get "item",
            XElement(XName.Get "title", entry.Metadata.Title),
            XElement(XName.Get "description", cdata),            
            XElement(XName.Get "link", urlWithUtm),
            XElement(XName.Get "guid", url),
            XElement(XName.Get "pubDate", entry.Metadata.DatePublished))

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

    let generateReponseFeedRss (posts:Response array) =
        let latestPost = posts |> Array.sortByDescending(fun post -> DateTime.Parse(post.Metadata.DatePublished)) |> Array.head 
        let entries = posts |> Array.map(reponseFeedEntryXml)
        let channel = feedChannelXml "Luis Quintanilla Response Feed" "Response Feed" latestPost.Metadata.DatePublished
        
        channel.Descendants(XName.Get "channel").First().Add(entries)
        channel         