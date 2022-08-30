---
title: "Analyze RSS feeds with FSharp.Data XML Type Provider"
language: "F#"
tags: xml,dotnet,fsharp,typeprovider,rss
---

## Description

Use the [FSharp.Data XML Type Provider](https://fsprojects.github.io/FSharp.Data/library/XmlProvider.html) to load and analyze RSS feeds. 

## Usage

```bash
dotnet fsi rss-parser.fsx 
```

## Snippet

### rss-parser.fsx

```fsharp
// Install NuGet packages
#r "nuget:FSharp.Data"

// Import NuGet packages
open System.Xml.Linq
open FSharp.Data

// Define Rss type using XML Type Provider
type Rss = XmlProvider<"http://luisquintanilla.me/posts/index.xml">

// Load RSS feed using Rss type
let blogFeed = Rss.Load("http://luisquintanilla.me/posts/index.xml")

// Get Feed Title
blogFeed.Channel.Title

// Get the 5 latest posts
blogFeed.Channel.Items
|> Array.sortByDescending(fun item -> item.PubDate)
|> Array.take 5

// Get the title and URL of 5 latest posts
blogFeed.Channel.Items
|> Array.sortByDescending(fun item -> item.PubDate)
|> Array.take 5
|> Array.map(fun item -> {|Title=item.Title;Url=item.Link|})
```