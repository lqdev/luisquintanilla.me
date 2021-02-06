open System
open System.Linq
open System.Xml.Linq


let title = "Luis Quintanilla"
let link = "https://luisquintanilla.me"
let description = "Luis Quintanilla's personal website"
let lastPubDate = "02/05/2021"
let language = "en"

let entryXml (entry:Post) =
    XElement(XName.Get "item",
        XElement()
    ) 

let channelXml = 
    XElement(XName.Get "rss",
        XAttribute(XName.Get "version","2.0"),
        XElement(XName.Get "channel",
            XElement(XName.Get "title", title),
            XElement(XName.Get "link", link),
            XElement(XName.Get "description", description),
            XElement(XName.Get "lastPubDate", lastPubDate),
            XElement(XName.Get "language", language)))