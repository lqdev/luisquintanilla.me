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
                            | ValidationSuccess s -> printfn $"{s.RequestBody.Target} sent successfully to {s.Endpoint.OriginalString}"
                            | ValidationError e -> printfn $"{e}"
                    }
        }

    let sendWebmentions (responses: Response array) =     
        responses
        |> Array.filter(fun x -> 
            // Get current time in EST (-05:00) to match the timezone used in post metadata
            let estTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")
            let currentDateTime = TimeZoneInfo.ConvertTime(DateTimeOffset.Now, estTimeZone)
            let updatedDateTime = DateTimeOffset.Parse(x.Metadata.DateUpdated)
            // Send webmentions for responses updated within the last hour
            // Both times are now in EST for accurate comparison
            currentDateTime.Subtract(updatedDateTime).TotalHours < 1.0)
        |> Array.map(fun x -> { Source=new Uri($"http://lqdev.me/feed/{x.FileName}"); Target=new Uri(x.Metadata.TargetUrl) })
        |> runWebmentionWorkflow
        |> Async.Parallel
        |> Async.RunSynchronously
        |> ignore