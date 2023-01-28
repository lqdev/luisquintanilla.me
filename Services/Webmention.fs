module WebmentionService

    open System
    open Domain
    open WebmentionFs
    open WebmentionFs.Services

    let runWebmentionWorkflow (mentions: UrlData array) = 
        seq {
            for mention in mentions do 
                    yield async {
                        let ds = new UrlDiscoveryService()

                        let ws = new WebmentionSenderService(ds)

                        printfn $"Sending: {mention}"

                        let! result = ws.SendAsync(mention) |> Async.AwaitTask

                        return 
                            match result with 
                            | ValidationSuccess s -> printfn $"{s.RequestBody.Target} sent successfully to ${s.Endpoint.OriginalString}"
                            | ValidationError e -> printfn $"{e}"
                    }
        }

    let sendWebmentions (responses: Response array) =     
        responses
        |> Array.filter(fun x -> 
            let currentDateTime = DateTimeOffset(DateTime.Now)
            let updatedDateTime = DateTimeOffset(DateTime.Parse(x.Metadata.DateUpdated).AddMinutes(60))
            currentDateTime < updatedDateTime)
        |> Array.map(fun x -> { Source=new Uri($"http://lqdev.me/feed/{x.FileName}"); Target=new Uri(x.Metadata.TargetUrl) })
        |> runWebmentionWorkflow
        |> Async.Parallel
        |> Async.RunSynchronously
        |> ignore