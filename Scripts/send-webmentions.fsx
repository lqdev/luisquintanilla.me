// Send Webmentions Script
// 
// This script reads webmention data from JSON files (generated during build)
// and sends webmentions using WebmentionFs.
// 
// Usage: dotnet fsi Scripts/send-webmentions.fsx

#r "nuget: lqdev.WebmentionFs, 0.0.7"

open System
open System.IO
open System.Text.Json
open WebmentionFs
open WebmentionFs.Services

// Webmention data structure matching JSON format
type WebmentionData = {
    Source: string
    Target: string
}

// Read webmentions from the JSON files produced during build:
//   webmentions.json         response posts (Scripts/identify-webmentions.fsx)
//   follow-webmentions.json  blogroll/podroll follows (Scripts/identify-follow-webmentions.fsx)
let webmentionFiles = [ "webmentions.json"; "follow-webmentions.json" ]

let loadFile (path: string) : WebmentionData array =
    if File.Exists(path) then
        let loaded = JsonSerializer.Deserialize<WebmentionData array>(File.ReadAllText(path))
        printfn $"Loaded {loaded.Length} webmentions from {path}"
        loaded
    else
        printfn $"{path} not found; skipping"
        [||]

let webmentions = webmentionFiles |> List.map loadFile |> Array.concat

printfn $"Loaded {webmentions.Length} webmentions to send"

if webmentions.Length = 0 then
    printfn "No webmentions to send. Exiting."
else
    // Convert to WebmentionFs UrlData format
    let mentions : UrlData array =
        webmentions
        |> Array.map(fun w -> 
            { Source = Uri(w.Source)
              Target = Uri(w.Target) })
    
    // Send each webmention
    let tasks =
        mentions
        |> Array.map (fun mention ->
            async {
                let ds = new UrlDiscoveryService()
                let ws = new WebmentionSenderService(ds)
                
                let sourceUri = mention.Source.AbsoluteUri
                let targetUri = mention.Target.AbsoluteUri
                printfn $"Sending: Source={sourceUri}, Target={targetUri}"
                
                try
                    let! result = ws.SendAsync(mention) |> Async.AwaitTask
                    match result with
                    | ValidationSuccess s -> 
                        printfn $"{s.RequestBody.Target} sent successfully to {s.Endpoint.OriginalString}"
                    | ValidationError e -> 
                        printfn $"Error: {e}"
                with
                | ex -> printfn $"Exception sending webmention: {ex.Message}"
            })
    
    tasks
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore
    
    printfn $"Completed sending {webmentions.Length} webmentions"