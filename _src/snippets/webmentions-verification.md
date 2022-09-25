---
title: "Webmentions Verification"
language: "F#"
tags: f#,indieweb,webmentions,internet,web,social,interactive,script
---

## Description

Sample script that shows how to perform Webmention verification per [Webmentions specification](https://www.w3.org/TR/webmention/#webmention-verification). 

## Usage

```bash
dotnet fsi webmention-verification.fsx
```

## Snippet

**webmention-verification.fsx**

```fsharp
// https://www.w3.org/TR/webmention/#webmention-verification

#r "nuget:FSharp.Data"
#r "nuget: Microsoft.AspNetCore.WebUtilities, 2.2.0"

open System
open System.Net
open System.Net.Http
open System.Net.Http.Headers
open System.Collections.Generic
open Microsoft.AspNetCore.WebUtilities
open FSharp.Data

type WebmentionVerificationResult = 
    | Interactions of {|Replies: string list; Likes: string list; Shares: string list|}
    | Mention of {| Mentions: string list|}
    | Error of string

let getFormContent (request:HttpRequestMessage) =
    async {
        let! content = request.Content.ReadAsStringAsync() |> Async.AwaitTask
        let query = QueryHelpers.ParseQuery(content)
        let source = query["source"] |> Seq.head
        let target = query["target"] |> Seq.head

        return source,target
    }


let cont =  
    dict [
        ("source","https://raw.githubusercontent.com/lqdev/fsadvent-2021-webmentions/main/reply.html")
        ("target","https://webmention.rocks/test/1")
    ]

let buildSampleRequestMessage (content:IDictionary<string,string>) = 

    let reqMessage = new HttpRequestMessage()
    reqMessage.Content <- new FormUrlEncodedContent(content)

    reqMessage

let req = buildSampleRequestMessage cont

// verification

let source,target = 
    req
    |> getFormContent
    |> Async.RunSynchronously

let getWebMentions (source:string) (target:string)= 
    async {
        use client = new HttpClient()
        let reqMessage = new HttpRequestMessage(new HttpMethod("Get"), source)
        reqMessage.Headers.Accept.Clear()
        
        // Only accept text/html content
        reqMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"))
        
        // Get document
        let! res = client.SendAsync(reqMessage) |> Async.AwaitTask
        
        // Verify webmention
        let webmentions = 
            match res.IsSuccessStatusCode with 
            | true ->
                // Get document contents
                let body = 
                    async {
                        return! res.Content.ReadAsStringAsync() |> Async.AwaitTask
                    } |> Async.RunSynchronously

                // Parse document
                let doc = HtmlDocument.Parse(body)

                // Get links tagged as replies using microformats
                let replies = 
                    doc.CssSelect(".u-in-reply-to")
                    |> List.map(fun x -> x.AttributeValue("href"))
                    |> List.filter(fun x -> x = target)

                // Get links tagged as likes using microformats
                let likes = 
                    doc.CssSelect(".u-in-like-of")
                    |> List.map(fun x -> x.AttributeValue("href"))
                    |> List.filter(fun x -> x = target)

                // Get links tagged as repost using microformats
                let shares = 
                    doc.CssSelect(".u-in-repost-of")
                    |> List.map(fun x -> x.AttributeValue("href"))
                    |> List.filter(fun x -> x = target)

                // Get untagged mentions
                let mentions = 
                    doc.CssSelect($"a")
                    |> List.map(fun x -> x.AttributeValue("href"))
                    |> List.filter(fun x -> x = target)

                // Collect all tagged interactions
                let knownInteractions = 
                    [replies;likes;shares] |> List.collect(id)

                // Choose tagged mentions before untagged mentions
                match knownInteractions.IsEmpty,mentions.IsEmpty with 
                | true,true -> Error "Target not mentioned"
                | true,false -> Interactions {|Replies=replies;Likes=likes;Shares=shares|}
                | false,true -> Mention {|Mentions=mentions|}
                | false, false -> Interactions {|Replies=replies;Likes=likes;Shares=shares|}

            | false -> 
                Error "Unable to get source"
        return webmentions            
    }

getWebMentions source target
|> Async.RunSynchronously
```

## Sample Output

```text
Interactions { Likes = []
                 Replies = ["https://webmention.rocks/test/1"]
                 Shares = [] }
```