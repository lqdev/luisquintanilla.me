// Send Webmentions Script
// 
// This script loads response content and sends webmentions for recently updated responses.
// Uses inline NuGet references to avoid dependency issues in CI/CD environments.
// 
// Usage: dotnet fsi Scripts/send-webmentions.fsx

#r "nuget: lqdev.WebmentionFs, 0.0.7"
#r "nuget: YamlDotNet, 16.3.0"

open System
open System.IO
open System.Text.RegularExpressions
open WebmentionFs
open WebmentionFs.Services
open YamlDotNet.Serialization

// Response metadata structure matching Domain.fs
[<CLIMutable>]
type ResponseDetails = {
    title: string
    targeturl: string
    response_type: string
    dt_published: string
    dt_updated: string
    tags: string array
}

// Response with filename
type Response = {
    FileName: string
    Metadata: ResponseDetails
}

// Parse a response markdown file to extract metadata
let parseResponseFile (filePath: string) : Response option =
    try
        let content = File.ReadAllText(filePath)
        let fileName = Path.GetFileNameWithoutExtension(filePath)
        
        // Extract YAML frontmatter using regex
        let yamlPattern = @"^---\s*\n(.*?)\n---"
        let yamlMatch = Regex.Match(content, yamlPattern, RegexOptions.Singleline)
        
        if yamlMatch.Success then
            let yamlContent = yamlMatch.Groups.[1].Value
            let deserializer = DeserializerBuilder().Build()
            let metadata = deserializer.Deserialize<ResponseDetails>(yamlContent)
            Some { FileName = fileName; Metadata = metadata }
        else
            None
    with
    | ex -> 
        printfn $"Error parsing {filePath}: {ex.Message}"
        None

// Load all responses from the _src/responses directory
let loadResponses () =
    let responsesDir = Path.Combine("_src", "responses")
    if Directory.Exists(responsesDir) then
        Directory.GetFiles(responsesDir, "*.md")
        |> Array.choose parseResponseFile
    else
        printfn "Warning: _src/responses directory not found"
        [||]

// Send webmentions for recently updated responses
let sendWebmentions (responses: Response array) =
    // Filter responses updated within the last hour
    let recentResponses =
        responses
        |> Array.filter (fun x ->
            try
                // Get current time in EST (-05:00) to match the timezone used in post metadata
                let estTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")
                let currentDateTime = TimeZoneInfo.ConvertTime(DateTimeOffset.Now, estTimeZone)
                let updatedDateTime = DateTimeOffset.Parse(x.Metadata.dt_updated)
                // Send webmentions for responses updated within the last hour
                currentDateTime.Subtract(updatedDateTime).TotalHours < 1.0
            with
            | ex ->
                printfn $"Error parsing date for response: {ex.Message}"
                false)
    
    printfn $"Found {recentResponses.Length} recent responses to send webmentions for"
    
    if recentResponses.Length = 0 then
        printfn "No recent responses found. Exiting."
    else
        // Send webmentions for each response
        let mentions =
            recentResponses
            |> Array.map (fun x ->
                { Source = new Uri($"http://lqdev.me/feed/{x.FileName}")
                  Target = new Uri(x.Metadata.targeturl) })
        
        // Process each mention
        let tasks =
            mentions
            |> Array.map (fun mention ->
                async {
                    let ds = new UrlDiscoveryService()
                    let ws = new WebmentionSenderService(ds)
                    
                    printfn $"Sending: {mention}"
                    
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

// Main execution
let responses = loadResponses()
printfn $"Loaded {responses.Length} total responses"
sendWebmentions responses