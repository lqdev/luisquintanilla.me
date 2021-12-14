---
post_type: "article" 
title: "Sending Webmentions with F#"
description: "Discover Webmention endpoint URLs and send Webmentions using F#"
published_date: "12/13/2021 20:53"
tags: 
---

## Introduction

> This post is part of the [F# Advent 2021](https://sergeytihon.com/2021/10/18/f-advent-calendar-2021). 

Webmentions is a W3C specification that defines a standard way of implementing notifications between websites. It enables rich interactions between websites using standard protocols. According to the spec, "Webmention is a simple way to notify any URL when you mention it on your site. From the receiver's perspective, it's a way to request notifications when other sites mention it". 

If you're interested in learning more about the spec and overall use cases, see the following links:

- [Webmentions: Enabling better communication on the internet](https://boffosocko.com/2018/07/19/webmentions-enabling-better-communication-on-the-internet-2/)
- [Sending your first Webmention from scratch](https://aaronparecki.com/2018/06/30/11/your-first-webmention)
- [Webmentions W3C specification](https://www.w3.org/TR/webmention/)
- [Webmentions implementation tests](https://webmention.rocks/)

The Webmentions protocol has send and receive components. In this post, I'll go over a lightweight implementation for sending Webmentions using F# and test it using the [webmention.rocks](https://webmention.rocks/) website. Source code for this post can be found at the [fsadvent-2021-webmentions GitHub repository](https://github.com/lqdev/fsadvent-2021-webmentions).

## Sending Webmention workflow

Let's say you want to use webmentions to reply to or mention content from another site. The general workflow works as follows:

1. You create a document (source) that mentions content from another website (target). 
1. You perform discovery on the target website for the webmention endpoint URL. The URL might be in one of three places:

    1. HTTP response headers.
    2. `<link>` tag with `rel=webmention` attribute.
    3. `<a>` tag with `rel=webmention` attribute.

    The discovery is performed in order and each subsequent option works as a fallback of the other. 

1. You make an HTTP POST request notifying the source that you've mentioned their article on your site. The request body is in `x-www-form-urlencoded` form containing the `source` and `target` parameters where source is the URL of your article and the target is the URL of article being mentioned. 

For more details on this workflow, see the [Sending Webmentions section](https://www.w3.org/TR/webmention/#sending-webmentions) of the specification.

## Create a document

The document can be any publicly hosted HTML document as long as you have a source URL to provide during your notification request.

For example, you can have something like the following:

```html
<!doctype html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta http-equiv="X-UA-Compatible" content="IE=edge">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>F# Advent 2021 - Webmentions</title>
</head>
    <body>
        <a href="https://webmention.rocks/test/1">F# Advent 2021 - This is a great post</a>
    </body>
</html>
```

Using [microformats](http://microformats.org/wiki/h-entry) though, you can annotate your HTML to provide more context to the receiver. The same document with microformats providing the author, content, and source document being replied to might look like the following with microformats:

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta http-equiv="X-UA-Compatible" content="IE=edge">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>F# Advent 2021 - Webmentions</title>
</head>
<body>
    <div class="h-entry">
        <div class="u-author h-card">
            <a href="http://lqdev.me" class="u-url p-name">lqdev</a>
        </div>
        <p>In reply to: <a href="https://webmention.rocks/test/1" class="u-in-reply-to">@webmention.rocks</a></p>
        <p class="e-content">F# Advent 2021 - This is a great post</p>
    </div>
</body>
</html>
```

Receiving Webmentions is beyond the scope of this post, so I'll skip over them. Once you have a document and make it publicly accessible to the web, it's time to find where to send notifications to.

## Discover Webmention endpoint URL

To discover where to send the Webmention to, send an HTTP GET or HTTP HEAD request to the target URL. Once you get the response, the Webmention endpoint URL should be in one of three places:

- HTTP headers

    ```text
    GET /test/1 HTTP/1.1
    Host: webmention.rocks
    HTTP/1.1 200 OK
    Link: <https://webmebtion.rocks/test/1/webmention>; rel="webmention"
    ```


- `<link>` tag

    ```html
    <html>
        <head>
        ...
            <link href="http://webmention.rocks/test/1/webmention" rel="webmention" />
        ...
        </head>
    </html>
    ```
- `<a>` tag

    ```html
    <html>
        <body>
        ....
        <a href="http://webmention.rocks/test/1/webmention" rel="webmention">webmention</a>
        ...
        </body>
    </html>
    ```

The following examples show how you'd implement the discovery process for each of these scenarios in F#.

### Discover Webmention URL in header

The `discoverUrlInHeaderAsync` function uses the .NET HttpClient to make an HTTP HEAD request to get the headers from the target URL provided. Then, a search is performed for a "link" header containing the text "webmention". Once the header is found, it's sanitized to extract the Webmention endpoint URL of the target site.

```fsharp
let discoverUrlInHeaderAsync (url:string) =
    async {
        // Initialize HttpClient
        use client = new HttpClient()

        // Prepare request message
        let reqMessage = new HttpRequestMessage(new HttpMethod("HEAD"), url)
        
        // Send request
        let! response = client.SendAsync(reqMessage) |> Async.AwaitTask

        // Get request headers
        let responseHeaders = 
            [
                for header in response.Headers do
                    header.Key.ToLower(), header.Value
            ]

        // Look for webmention header
        try
            // Find "link" header that contains "webmention"
            let webmentionHeader =
                responseHeaders
                |> Seq.filter(fun (k,_) -> k = "link")
                |> Seq.map(fun (_,v) -> v |> Seq.filter(fun header -> header.Contains("webmention")))
                |> Seq.head
                |> List.ofSeq
                |> List.head

            // Get first part of "link" header
            let webmentionUrl = 
                webmentionHeader.Split(';')
                |> Array.head

            // Remove angle brackets from URL
            let sanitizedWebmentionUrl = 
                webmentionUrl
                    .Replace("<","")
                    .Replace(">","")
                    .Trim()

            return Some sanitizedWebmentionUrl                    

        with
            | _ -> return None
        
    }
```

### Discover Webmention URL endpoint in link tag

The next place to check for a Webmention endpoint URL is a `<link>` tag in the document. In this example, the `discoverUrlInLinkTagAsync` function uses `FSharp.Data` library to get the contents of the target URL and parse the HTML contents to find a link tag with a `rel=webmention` attribute value.

```fsharp
let discoverUrlInLinkTagAsync (url:string) = 
    async {
        // Load HTML document
        let! htmlDoc = HtmlDocument.AsyncLoad(url)

        // Get webmention URL from "<link>" tag
        try
            let webmentionUrl = 
                htmlDoc.CssSelect("link[rel='webmention']")
                |> List.map(fun link -> link.AttributeValue("href"))
                |> List.head

            return Some webmentionUrl
        with
            | _ -> return None
    }
```

### Discover Webmention endpoint URL in anchor tag

The last place to check for the Webmention endpoint URL is in an anchor tag in the contents of the target URL. In this example, the `discoverUrlInAnchorTagAsync` uses the `FSharp.Data` library to get the contents of the target URL and parse the HTML contents to find an anchor tag with a `rel=webmention` attribute value.

```fsharp
let discoverUrlInAnchorTagAsync (url:string) = 
    async {
        // Load HTML document
        let!  htmlDoc = HtmlDocument.AsyncLoad(url)

        // Get webmention URL from "<a>" tag
        try
            let webmentionUrl = 
                htmlDoc.CssSelect("a[rel='webmention'")
                |> List.map(fun a -> a.AttributeValue("href"))
                |> List.head

            return Some webmentionUrl
        with
            | _ -> return None
    }
```

Once the utility functions are in place to handle the different scenarios, create a new function to perform the discovery workflow. 

```fsharp
// Apply webmention URL discovery workflow
// 1. Check header
// 2. Check link tag
// 3. Check anchor tag
let discoverWebmentionUrlAsync (url:string) = 
    async {
        let! headerUrl = discoverUrlInHeaderAsync url
        let! linkUrl = discoverUrlInLinkTagAsync url
        let! anchorUrl = discoverUrlInAnchorTagAsync url

        // Aggregate results
        let discoveryResults = [headerUrl; linkUrl; anchorUrl]

        // Unwrap and take the first entry containing a value
        let webmentionUrl = 
            discoveryResults
            |> List.choose(fun url -> url)
            |> List.head

        return webmentionUrl
    }
```

The `discoverWebmentionUrlAsync` calls all of the discovery utility methods and chooses only the results that successfully extracted a Webmention endpoint URL. In the event of multiple endpoints, the first one is takes precedence. 

## Send Webmention

Now that you have a publicly accessible document and the Webmention URL endpoint to send your Webmention to, it's time to send it. 

```fsharp
let sendWebMentionAsync (url:string) (req:IDictionary<string,string>) = 
    async {
        use client = new HttpClient()
        let content = new FormUrlEncodedContent(req)
        let! response = client.PostAsync(url, content) |> Async.AwaitTask
        return response.IsSuccessStatusCode
    }
```

The `sendWebMentionAsync` function uses the .NET HttpClient to send an HTTP POST request to the Webmention endpoint URL you just extracted. 

Define your source and target URLs

```fsharp
let sourceUrl = new Uri("https://raw.githubusercontent.com/lqdev/fsadvent-2021-webmentions/main/reply.html")
let targetUrl = new Uri("https://webmention.rocks/test/1")
```

The source URL I used in this example is hosted on [GitHub](https://raw.githubusercontent.com/lqdev/fsadvent-2021-webmentions/main/reply.html) and the target URL is the first implementation test in the [webmention.rocks](https://webmention.rocks/test/1) website.

Then, create a function to run the entire workflow end-to-end.

```fsharp
let runWebmentionWorkflow () = 
    async {
        // Discover webmention endpoint URL of target URL
        let! discoveredUrl = 
            targetUrl.OriginalString
            |> discoverWebmentionUrlAsync

        // Construct URL depending on whether it's absolute or relative
        let authority = targetUrl.GetLeftPart(UriPartial.Authority)

        let constructedUrl = 
            match (discoveredUrl.Contains("http")) with
            | true -> discoveredUrl
            | false -> 
                let noQueryUrl = 
                    discoveredUrl.Split('?')
                    |> Array.head
                    
                $"{authority}{noQueryUrl}"

        // Prepare webmention request data
        let reqData = 
            dict [
                ("source", sourceUrl.OriginalString)
                ("target", targetUrl.OriginalString)
            ]

        // Send web mentions
        return! sendWebMentionAsync constructedUrl reqData
    }
```

Now that you have everything set up, you're ready to send your webmention! 

```fsharp
runWebmentionWorkflow ()
|> Async.RunSynchronously
```

If your post is successful, you should see it on the target URL.

![Display of Webmention post on webmention.rocks](https://user-images.githubusercontent.com/11130940/145916987-31d9cd76-50e3-4963-85f4-5e5edd73e4a5.png)

## Conclusion

In this post, I showed how you can use F# to implement sending webmentions using F#. This is not a full implementation as there are still scenarios I need to account for like receiving webmentions. There are community maintained [libraries](https://indieweb.org/Webmention-developer#Libraries), [plugins](https://indieweb.org/Webmention#Publishing_Software), and [services](https://indieweb.org/Webmention#Services) to simplify the process, but because the specification is built on open protocols and standards, it's possible to build your own implementation in F# and integrate it into your website. Happy coding!