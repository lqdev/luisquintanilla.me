// Identify Webmentions Script
// 
// This script runs during the build step to identify which webmentions need to be sent.
// It uses PersonalSite.dll to parse responses and filter recent updates.
// Outputs a JSON file with the webmentions to send.
// 
// Usage: dotnet fsi Scripts/identify-webmentions.fsx

#r "../bin/Debug/net10.0/PersonalSite.dll"

open System
open System.IO
open System.Text.Json

// Webmention data structure for JSON serialization
type WebmentionData = {
    Source: string
    Target: string
}

// Load responses using the new AST-based system (same pattern as Program.fs)
let responses = 
    let responseFiles = 
        Directory.GetFiles(Path.Join("_src", "responses"))
        |> Array.filter (fun f -> f.EndsWith(".md"))
        |> Array.toList
    let processor = GenericBuilder.ResponseProcessor.create()
    let feedData = GenericBuilder.buildContentWithFeeds processor responseFiles
    feedData |> List.map (fun item -> item.Content) |> List.toArray

// Filter for recent responses (updated within last hour)
let recentResponses =
    responses
    |> Array.filter(fun x -> 
        try
            // Get current time in EST (-05:00) to match the timezone used in post metadata
            let estTimeZone = 
                try
                    TimeZoneInfo.FindSystemTimeZoneById("America/New_York") // Linux/Mac (handles DST)
                with
                | _ -> TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time") // Windows fallback
            let currentDateTime = TimeZoneInfo.ConvertTime(DateTimeOffset.Now, estTimeZone)
            let updatedDateTime = DateTimeOffset.Parse(x.Metadata.DateUpdated)
            // Send webmentions for responses updated within the last hour
            currentDateTime.Subtract(updatedDateTime).TotalHours < 1.0
        with
        | ex ->
            printfn $"Error parsing date for {x.FileName}: {ex.Message}"
            false)

printfn $"Found {recentResponses.Length} responses updated within the last hour"

// Convert to webmention data format
let webmentions =
    recentResponses
    |> Array.map(fun x -> 
        { Source = $"http://lqdev.me/feed/{x.FileName}"
          Target = x.Metadata.TargetUrl })

// Serialize to JSON
let json = JsonSerializer.Serialize(webmentions, JsonSerializerOptions(WriteIndented = true))

// Write to output directory
let outputDir = "_public/api/data"
Directory.CreateDirectory(outputDir) |> ignore
let outputPath = Path.Combine(outputDir, "webmentions.json")
File.WriteAllText(outputPath, json)

printfn $"Wrote {webmentions.Length} webmentions to {outputPath}"
