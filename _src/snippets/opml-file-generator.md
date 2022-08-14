---
title: "OPML File Generator"
language: "F#"
tags: dotnet,f#,script
---

## Description

Script to take information stored in a JSON file and converts it into OPML format. This works for RSS readers as well as podcast clients that support OPML import.

## Usage

```bash
dotnet fsi opml-generator.fsx "My Blogroll" "http://lqdev.me" "blogroll.json"
```

## Snippet

### opml-generator.fsx

```fsharp
open System.IO
open System.Linq
open System.Text.Json
open System.Xml.Linq

type OpmlMetadata = 
    {
        Title: string
        OwnerId: string
    }

type Outline = 
    {
        Title: string
        Type: string
        HtmlUrl: string
        XmlUrl: string
    }

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

let loadLinks (filePath:string) = 
    File.ReadAllText(filePath)
    |> fun x -> x |> JsonSerializer.Deserialize<Outline array>

let buildOpmlFeed (title:string) (ownerId:string) (filePath:string) = 
    let fileName = Path.GetFileNameWithoutExtension(filePath)
    
    let head = 
        {
            Title=title
            OwnerId=ownerId
        }
        |> headElement
    
    let links = filePath |> loadLinks |> Array.map(outlineElement) 
    
    let feed =  opmlFeed head 
    feed.Descendants(XName.Get "body").First().Add(links)
    File.WriteAllText($"{fileName}.opml", feed.ToString())

let args = fsi.CommandLineArgs
let title = args[1]
let ownerId = args.[2]
let dataPath = args.[3]

buildOpmlFeed title ownerId dataPath
```

### blogroll.json

```json
[
    {
        "Title": "Blogroll.org",
        "Type": "rss",
        "HtmlUrl": "https://blogroll.org/",
        "XmlUrl": "https://blogroll.org/feed/"
    },
    {
        "Title": "Cheapskate's Guide",
        "Type": "rss",
        "HtmlUrl": "https://cheapskatesguide.org/",
        "XmlUrl": "https://cheapskatesguide.org/cheapskates-guide-rss-feed.xml"
    },
    {
        "Title": "JWZ",
        "Type": "rss",
        "HtmlUrl": "https://www.jwz.org/blog/",
        "XmlUrl": "https://cdn.jwz.org/blog/feed/"
    }    
]
```