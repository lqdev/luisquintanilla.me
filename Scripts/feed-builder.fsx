// This script loads data from podroll and youtube RSS feeds and generates feeds that form the startpage

#r "../bin/Debug/net8.0/PersonalSite.dll"
#r "nuget:FSharp.Data"

open System
open System.IO
open System.Text.Json
open FSharp.Data
open Domain;

let dataPath = Path.Join(__SOURCE_DIRECTORY__,"..","Data")
let getRssLinks (path) (filter:string array)= 
    File.ReadAllText(path)
    |> JsonSerializer.Deserialize<Outline array>
    |> Array.filter(fun v -> filter |> Array.exists(fun d -> d <> v.Title))
    |> Array.map(_.XmlUrl)

let podrollLinks = getRssLinks(Path.Join(dataPath,"podroll.json")) [|"Surveillance Report"|]

let youtubeLinks = getRssLinks(Path.Join(dataPath,"youtube.json")) [|String.Empty|]

let x = 
    podrollLinks[..10]

let documents = 
    x
    |> Array.map(HtmlDocument.Load)

let timeSpan = DateTime.Now - TimeSpan.FromDays(2)

// TODO Add Date Parser Function

let processPodcasts (doc:HtmlDocument) = 
    doc.Descendants ["item"]
    |> Seq.map(fun n -> 
        
        let title = 
            n.Descendants ["title"]
            |> Seq.map(_.InnerText())
            |> Seq.head
        
        let pubDate = 
            n.Descendants ["pubDate"]
            |> Seq.map(_.InnerText())
            |> Seq.head
            |> DateTime.TryParse
        
        let url = 
            n.Descendants ["enclosure"]
            |> Seq.map(fun v -> 
                v.TryGetAttribute("url")
                |> Option.map(fun d -> d.Value())
                |> Option.get)
            |> Seq.head    

        let result = 
            match pubDate with
            | true,d -> title, d, url
            | false,_ -> title,DateTime.Now, url
        
        result)
    |> Seq.filter(fun (_,b,_) -> b > timeSpan)
    |> Seq.map(fun (a,_,b) -> {|Title=a;Url=b|})
    

documents
|> Seq.collect(processPodcasts)



// This is an atom feed so they will always be processed the same
let processYouTube (doc:HtmlDocument) = 
    let processedFeed = 
        doc.Descendants ["entry"]
        |> Seq.map(
                fun n -> 
                    let id = 
                        n.Descendants ["yt:videoId"]
                        |> Seq.head
                        |> fun v -> v.InnerText()
                    let title = 
                        n.Descendants ["title"] 
                        |> Seq.head 
                        |> fun v -> v.InnerText()
                    let pubDate = 
                        n.Descendants ["updated"]
                        |> Seq.head
                        |> fun v -> v.InnerText()
                    id,title,DateTime.Parse(pubDate))
        |> Seq.filter(fun (_,_,pubDate) -> pubDate > timeSpan)
        |> Seq.map(fun (id, title,_) -> 
            {|
                Id= id
                Title= title 
                Thumbnail = $"https://i3.ytimg.com/vi/{id}/hqdefault.jpg"
                Url= $"https://www.youtube.com/watch?v={id}"|})
    processedFeed

let ytLinks = 
    youtubeLinks
    |> Seq.map(HtmlDocument.Load)
    |> Seq.collect(processYouTube)
