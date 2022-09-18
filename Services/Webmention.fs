module WebmentionService

    open System
    open System.Collections.Generic
    open System.Net.Http
    open Domain
    open FSharp.Data
    
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

    let postWebMentionAsync (url:string) (req:IDictionary<string,string>) = 
        async {
            use client = new HttpClient()
            let content = new FormUrlEncodedContent(req)
            let! response = client.PostAsync(url, content) |> Async.AwaitTask
            return response.IsSuccessStatusCode
        }

    let runWebmentionWorkflow (mentions: Webmention array) = 
        seq {
            for mention in mentions do
                yield async {
                    // Discover webmention endpoint URL of target URL
                    let! discoveredUrl = 
                        mention.TargetUrl.OriginalString
                        |> discoverWebmentionUrlAsync

                    // Construct URL depending on whether it's absolute or relative
                    let authority = mention.TargetUrl.GetLeftPart(UriPartial.Authority)

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
                            ("source", mention.SourceUrl.OriginalString)
                            ("target", mention.TargetUrl.OriginalString)
                        ]

                    // Send web mentions
                    return! postWebMentionAsync constructedUrl reqData
                }                 
        }

        let sendWebmentions (responses: Response array) = 
            responses
            |> Array.filter(fun x -> 
                let currentDateTime = DateTimeOffset(DateTime.Now)
                let updatedDateTime = DateTimeOffset(DateTime.Parse(x.Metadata.DateUpdated).AddMinutes(60))
                printfn $"Current: {currentDateTime}"
                printfn $"Updated: {updatedDateTime}"
                currentDateTime < updatedDateTime)
            |> Array.map(fun x -> { SourceUrl=new Uri($"http://lqdev.me/feed/{x.FileName}"); TargetUrl=new Uri(x.Metadata.TargetUrl) })
            |> runWebmentionWorkflow
            |> Async.Parallel
            |> Async.RunSynchronously
            |> ignore