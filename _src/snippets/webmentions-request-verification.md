---
title: "Webmentions Request Verification"
language: "F#"
tags: f#,indieweb,webmentions,internet,web,social,interactive,script
---

## Description

Sample script that shows how to perform Webmention request verification per [Webmentions specification](https://www.w3.org/TR/webmention/#request-verification). 

## Usage

```bash
dotnet fsi request-verification.fsx
```

## Snippet

**request-verification.fsx**

```fsharp
// https://www.w3.org/TR/webmention/#request-verification

// 1. Send response with 202 Accepted to acknowledge successful request
// 2. DONE: Check that the protocol is http or https
// 3. DONE: Source URL is different than Target URL
// 4. DONE Check that Target URL is a valid resource

#r "nuget: Microsoft.AspNetCore.WebUtilities, 2.2.0"

open System
open System.Net
open System.Net.Http
open System.Collections.Generic
open Microsoft.AspNetCore.WebUtilities

type RequestVerificationResponse =
    | Ok of HttpRequestMessage
    | Error of string

// Parse Form URL Encoded string
let getFormContent (request:HttpRequestMessage) =
    async {
        let! content = request.Content.ReadAsStringAsync() |> Async.AwaitTask
        let query = QueryHelpers.ParseQuery(content)
        let source = query["source"] |> Seq.head
        let target = query["target"] |> Seq.head

        return source,target
    }

// Check protocol is HTTP or HTTPS
let checkProtocol (request: RequestVerificationResponse) =
    match request with 
    | Ok m -> 
        let source,target = 
            async {
                return! getFormContent(m)
            } |> Async.RunSynchronously

        let isProtocolValid = 
            match source.StartsWith("http"),target.StartsWith("http") with
            | true,true -> Ok m
            | true,false -> Error "Target invalid protocol"
            | false,true ->  Error "Source invalid protocol"
            | false,false -> Error "Source and Target invalid protocol"

        isProtocolValid
    | Error s -> Error $"{s}"

// Check the URLs are not the same
let checkUrlsSame (request:RequestVerificationResponse) = 
    match request with 
    | Ok m -> 
        let source,target = 
            async {
                return! getFormContent(m)
            } |> Async.RunSynchronously
        let check = 
            match source.Equals(target) with 
            | true -> Error "Urls are the same"
            | false ->  Ok m
        check
    | Error s -> Error s

// Helper functions
let uriIsMine (url:string) = 
    let uri = new Uri(url)
    uri.Host.Equals("lqdev.me") || uri.Host.Equals("www.luisquintanilla.me") || uri.Host.Equals("luisquintanilla.me")

let isValid (url:string) (msg:HttpResponseMessage) = 
    let isMine = uriIsMine url
    isMine & msg.IsSuccessStatusCode

// Check URL is a valid resource
// Valid means, the URL is one of my domains and returns a non-400 or 500 HTML status code
let checkUrlValidResource (request:RequestVerificationResponse) = 
    match request with 
    | Ok m -> 
        let res = 
            async {
                let! source,target = getFormContent(m)
                use client = new HttpClient()
                let reqMessage = new HttpRequestMessage(HttpMethod.Head, target)
                let! resp = client.SendAsync(reqMessage) |> Async.AwaitTask
                return isValid target resp
            } |> Async.RunSynchronously
        match res with 
        | true -> Ok m
        | false -> Error "Target is not a valid resource" 
    | Error s -> Error s

// Combine validation steps into single function
let validate = 
    checkProtocol >> checkUrlsSame >> checkUrlValidResource

// Test application
let buildSampleRequestMessages (content:IDictionary<string,string>) = 

    let reqMessage = new HttpRequestMessage()
    reqMessage.Content <- new FormUrlEncodedContent(content)

    let liftedReqMessage = Ok reqMessage
    liftedReqMessage

let sampleContent = [ 
    dict [
        ("source","http://lqdev.me")
        ("target","http://lqdev.me")
    ]
    dict [
        ("source","http://://lqdev.me")
        ("target","protocol://lqdev.me")
    ]    
    dict [
        ("source","http://lqdev.me")
        ("target","http://github.com/lqdev")
    ]
    dict [
        ("source","http://github.com/lqdev")
        ("target","http://lqdev.me")
    ]        
]

sampleContent
|> List.map(buildSampleRequestMessages)
|> List.map(validate)
```

## Sample Output

```text
[  
  Error "Urls are the same"; 
  Error "Target invalid protocol";
  Error "Target is not a valid resource";
  Ok
    Method: GET, RequestUri: '<null>', Version: 1.1, Content: System.Net.Http.FormUrlEncodedContent, Headers:
    {
        Content-Type: application/x-www-form-urlencoded
        Content-Length: 67
    }
    {
        Content = System.Net.Http.FormUrlEncodedContent;
        Headers = seq [];
        Method = GET;
        Options = seq [];
        Properties = seq [];
        RequestUri = null;
        Version = 1.1;
        VersionPolicy = RequestVersionOrLower;
    }
]
```