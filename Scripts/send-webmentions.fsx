// Send Webmentions Script
// 
// This script loads response content using the new AST-based architecture
// and sends webmentions for recently updated responses (within the last hour).
// 
// Usage: dotnet fsi Scripts\send-webmentions.fsx

#r "../bin/Debug/net10.0/PersonalSite.dll"

open System
open System.IO

// Load responses using the new AST-based system (same pattern as Program.fs)
let responses = 
    let responseFiles = 
        Directory.GetFiles(Path.Join("_src", "responses"))
        |> Array.filter (fun f -> f.EndsWith(".md"))
        |> Array.toList
    let processor = GenericBuilder.ResponseProcessor.create()
    let feedData = GenericBuilder.buildContentWithFeeds processor responseFiles
    feedData |> List.map (fun item -> item.Content) |> List.toArray

// Send webmentions for recently updated responses (within last hour as per WebmentionService logic)
responses
|> WebmentionService.sendWebmentions