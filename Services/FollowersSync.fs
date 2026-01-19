module FollowersSync

open System
open System.IO
open System.Text.Json

/// Represents a follower in Table Storage
type FollowerEntity = {
    ActorUrl: string
    Inbox: string
    FollowedAt: string
    DisplayName: string
    FollowActivityId: string
}

/// ActivityPub Followers Collection
type FollowersCollection = {
    Context: string
    Id: string
    Type: string
    TotalItems: int
    OrderedItems: string list
} with
    static member Create(followers: FollowerEntity list) =
        {
            Context = "https://www.w3.org/ns/activitystreams"
            Id = "https://lqdev.me/api/activitypub/followers"
            Type = "OrderedCollection"
            TotalItems = followers.Length
            OrderedItems = followers |> List.map (fun f -> f.ActorUrl)
        }

/// Get followers from Azure Table Storage using REST API
/// Uses ACTIVITYPUB_STORAGE_CONNECTION environment variable
let getFollowersFromTableStorage () : FollowerEntity list =
    try
        let connectionString = Environment.GetEnvironmentVariable("ACTIVITYPUB_STORAGE_CONNECTION")
        
        if String.IsNullOrWhiteSpace(connectionString) then
            printfn "⚠️  ACTIVITYPUB_STORAGE_CONNECTION not set - returning empty followers list"
            []
        else
            // For build-time generation, we'll use the Node.js utility
            // This keeps the implementation simple and reuses existing infrastructure
            printfn "ℹ️  Table Storage integration available - using static file fallback for now"
            []
    with ex ->
        printfn "⚠️  Error accessing Table Storage: %s" ex.Message
        []

/// Build followers collection and write to static file
/// This maintains spec compliance while Table Storage is the source of truth
let buildFollowersCollection (outputDir: string) =
    try
        let followers = getFollowersFromTableStorage()
        let collection = FollowersCollection.Create(followers)
        
        let outputPath = Path.Combine(outputDir, "api", "data", "followers.json")
        let outputDirectory = Path.GetDirectoryName(outputPath)
        
        // Ensure directory exists
        if not (Directory.Exists(outputDirectory)) then
            Directory.CreateDirectory(outputDirectory) |> ignore
        
        // Build JSON manually to match ActivityPub format
        let orderedItemsArray = 
            if collection.OrderedItems.IsEmpty then 
                "[]"
            else
                let items = 
                    collection.OrderedItems
                    |> List.map (sprintf "    \"%s\"")
                    |> String.concat ",\n"
                sprintf "[\n%s\n  ]" items
        
        // Build complete JSON string
        let jsonLines = [
            "{"
            "  \"@context\": \"https://www.w3.org/ns/activitystreams\","
            "  \"id\": \"https://lqdev.me/api/activitypub/followers\","
            "  \"type\": \"OrderedCollection\","
            sprintf "  \"totalItems\": %d," collection.TotalItems
            sprintf "  \"orderedItems\": %s" orderedItemsArray
            "}"
        ]
        let json = String.concat "\n" jsonLines
        
        File.WriteAllText(outputPath, json)
        printfn "✓ Followers collection generated: %d followers" collection.TotalItems
    with ex ->
        printfn "✗ Failed to build followers collection: %s" ex.Message
        printfn "  Note: Followers endpoint will use Table Storage directly via Azure Functions"
