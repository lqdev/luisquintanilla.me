#!/usr/bin/env dotnet fsi

#load "WebSubOpmlTopics.fsx"

open System
open System.Collections.Generic
open System.IO
open System.Net
open System.Net.Http
open WebSubOpmlTopics

let arguments =
    fsi.CommandLineArgs
    |> Array.skip 1
    |> Array.filter ((<>) "--")

let commit =
    match arguments with
    | [||] -> false
    | [| "--commit" |] -> true
    | _ -> invalidArg "arguments" "Use no arguments for a dry run or --commit to notify the hub."

let projectRoot = Directory.GetCurrentDirectory()

for topic in topics do
    let relativePath = topic.Route.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)
    let generatedPath = Path.Combine(projectRoot, "_public", relativePath)

    if not (File.Exists(generatedPath)) then
        invalidOp $"Generated OPML topic was not found: {generatedPath}."

if not commit then
    printfn "Dry run: %d generated OPML topics would be notified." topics.Length
    topics |> Array.iter (fun topic -> printfn "  %s" topic.Url)
else
    let notification =
        task {
            let hubUrl = loadHubUrl ()
            let token = Environment.GetEnvironmentVariable("WEBSUB_PUBLISH_TOKEN")

            if String.IsNullOrWhiteSpace(token) || token.Length < 32 then
                invalidOp "WEBSUB_PUBLISH_TOKEN must contain at least 32 characters when using --commit."

            use client = new HttpClient()
            use request = new HttpRequestMessage(HttpMethod.Post, hubUrl)
            request.Headers.TryAddWithoutValidation("X-WebSub-Publish-Token", token) |> ignore

            let formValues =
                [ yield KeyValuePair("hub.mode", "publish")
                  for topic in topics do
                      yield KeyValuePair("hub.url", topic.Url) ]

            request.Content <- new FormUrlEncodedContent(formValues)
            use! response = client.SendAsync(request)

            if response.StatusCode <> HttpStatusCode.Accepted then
                invalidOp $"The WebSub hub rejected the publish request (HTTP {int response.StatusCode})."

            printfn "Hub responded %d; %d OPML topics queued." (int response.StatusCode) topics.Length
        }

    notification.GetAwaiter().GetResult()
