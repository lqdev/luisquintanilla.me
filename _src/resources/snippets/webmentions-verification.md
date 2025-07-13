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
    | TaggedMention of {| Replies: bool; Likes: bool; Reposts: bool|}
    | UntaggedMention
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

let getMentionUsingCssSelector (doc:HtmlDocument) (selector:string) (target:string) = 
    doc.CssSelect(selector)
    |> List.map(fun x -> x.AttributeValue("href"))
    |> List.filter(fun x -> x = target)    

let hasMention (mentions:string list) = 
    mentions |> List.isEmpty |> not

let verifyWebmentions (source:string) (target:string)= 
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
                    getMentionUsingCssSelector doc ".u-in-reply-to" target

                // Get links tagged as likes using microformats
                let likes = 
                    getMentionUsingCssSelector doc ".u-in-like-of" target

                // Get links tagged as repost using microformats
                let shares = 
                    getMentionUsingCssSelector doc ".u-in-repost-of" target

                // Get untagged mentions
                let mentions = 
                    getMentionUsingCssSelector doc "a" target

                // Collect all tagged mentions
                let knownInteractions = 
                    [replies;likes;shares] 
                    |> List.collect(id)

                // Choose tagged mentions before untagged mentions
                match knownInteractions.IsEmpty,mentions.IsEmpty with 
                | true,true -> Error "Target not mentioned"
                | true,false | false,false -> 
                    TaggedMention 
                        {|
                            Replies = hasMention replies 
                            Likes = hasMention likes
                            Reposts = hasMention shares
                        |}
                | false,true -> UntaggedMention 

            | false -> 
                Error "Unable to get source"
        return webmentions            
    }

verifyWebmentions source target
|> Async.RunSynchronously
```

## Sample Output

```text
Interactions { 
    Likes = false
    Replies = true
    Shares = false }
```