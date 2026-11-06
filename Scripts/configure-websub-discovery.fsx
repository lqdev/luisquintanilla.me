#!/usr/bin/env dotnet fsi

#load "WebSubOpmlTopics.fsx"

open System
open System.IO
open System.Text.Json
open System.Text.Json.Nodes
open WebSubOpmlTopics

let configPath =
    Path.Combine(Directory.GetCurrentDirectory(), "_public", "staticwebapp.config.json")

if not (File.Exists(configPath)) then
    invalidOp $"Generated Static Web Apps configuration was not found at {configPath}."

let root = JsonNode.Parse(File.ReadAllText(configPath))

if isNull root then
    invalidOp "Generated Static Web Apps configuration is empty."

let topicsByRoute = topics |> Array.map (fun topic -> topic.Route, topic) |> Map.ofArray
let hubUrl = (loadHubUrl ()).AbsoluteUri
let mutable configuredCount = 0

for routeNode in root["routes"].AsArray() do
    if not (isNull routeNode) then
        let route = routeNode["route"].GetValue<string>()

        match Map.tryFind route topicsByRoute with
        | Some topic ->
            let headers =
                if isNull routeNode["headers"] then
                    let headers = JsonObject()
                    routeNode["headers"] <- headers
                    headers
                else
                    routeNode["headers"].AsObject()

            headers["Link"] <-
                JsonValue.Create($"<{hubUrl}>; rel=\"hub\", <{topic.Url}>; rel=\"self\"")

            configuredCount <- configuredCount + 1
        | None ->
            ()

if configuredCount <> topics.Length then
    invalidOp $"Expected to configure {topics.Length} OPML routes, but configured {configuredCount}."

let options = JsonSerializerOptions(WriteIndented = true)
File.WriteAllText(configPath, root.ToJsonString(options))
printfn "Added WebSub hub discovery to %d OPML routes." configuredCount
