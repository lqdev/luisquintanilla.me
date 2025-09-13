#r "nuget: FSharp.Data"
#r "nuget: System.Text.Json"

open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open FSharp.Data

// Enhanced script to output to API data directory with /activitypub namespace
let outputBaseDir = "api/data"
let websiteBaseUrl = "https://www.lqdev.me"

// Ensure output directories exist
let ensureDirectoryExists path =
    if not (Directory.Exists(path)) then
        Directory.CreateDirectory(path) |> ignore

// Actor profile generation for api/data/actor.json
let generateActorProfile () =
    let actor = {|
        ``@context`` = [| "https://www.w3.org/ns/activitystreams"; "https://w3id.org/security/v1" |]
        id = $"{websiteBaseUrl}/@lqdev"
        ``type`` = "Person"
        preferredUsername = "lqdev"
        name = "Luis Quintanilla"
        summary = "Senior Program Manager at Microsoft working on .NET and AI. I enjoy building things with code."
        url = websiteBaseUrl
        icon = {|
            ``type`` = "Image"
            mediaType = "image/jpeg"
            url = $"{websiteBaseUrl}/avatar.png"
        |}
        image = {|
            ``type`` = "Image" 
            mediaType = "image/jpeg"
            url = $"{websiteBaseUrl}/avatar.png"
        |}
        inbox = $"{websiteBaseUrl}/activitypub/inbox"
        outbox = $"{websiteBaseUrl}/activitypub/outbox"
        followers = $"{websiteBaseUrl}/activitypub/followers"
        following = $"{websiteBaseUrl}/activitypub/following"
        publicKey = {|
            id = $"{websiteBaseUrl}/@lqdev#main-key"
            owner = $"{websiteBaseUrl}/@lqdev"
            publicKeyPem = "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA...\n-----END PUBLIC KEY-----"
        |}
    |}
    
    ensureDirectoryExists outputBaseDir
    let options = JsonSerializerOptions(WriteIndented = true)
    options.Encoder <- System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    let actorJson = JsonSerializer.Serialize(actor, options)
    File.WriteAllText(Path.Combine(outputBaseDir, "actor.json"), actorJson)
    printfn "✅ Generated actor.json"

// WebFinger generation for api/data/webfinger.json  
let generateWebFinger () =
    let webfinger = {|
        subject = "acct:lqdev@www.lqdev.me"
        links = [|
            {|
                rel = "self"
                ``type`` = "application/activity+json"
                href = $"{websiteBaseUrl}/@lqdev"
            |}
        |]
    |}
    
    let options = JsonSerializerOptions(WriteIndented = true)
    options.Encoder <- System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    let webfingerJson = JsonSerializer.Serialize(webfinger, options)
    File.WriteAllText(Path.Combine(outputBaseDir, "webfinger.json"), webfingerJson)
    printfn "✅ Generated webfinger.json"

// RSS to ActivityPub outbox conversion
let convertRssToActivityPub () =
    let rssUrl = $"{websiteBaseUrl}/feed/feed.xml"
    
    try
        let rss = XmlProvider<"https://www.lqdev.me/feed/feed.xml">.GetSample()
        
        let activities = 
            rss.Channel.Items
            |> Array.take 20
            |> Array.mapi (fun i item ->
                let noteId = $"{websiteBaseUrl}/activitypub/notes/{i:D3}"
                let activityId = $"{websiteBaseUrl}/activitypub/outbox#{i:D3}"
                
                // Create individual note
                let note = {|
                    ``@context`` = "https://www.w3.org/ns/activitystreams"
                    id = noteId
                    ``type`` = "Note"
                    attributedTo = $"{websiteBaseUrl}/@lqdev"
                    content = item.Description
                    name = item.Title
                    published = item.PubDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    url = item.Link
                    tag = [||] // TODO: Extract from content
                |}
                
                // Save individual note
                ensureDirectoryExists (Path.Combine(outputBaseDir, "notes"))
                let options = JsonSerializerOptions(WriteIndented = true)
                options.Encoder <- System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                let noteJson = JsonSerializer.Serialize(note, options)
                File.WriteAllText(Path.Combine(outputBaseDir, "notes", $"{i:D3}.json"), noteJson)
                
                // Create activity
                {|
                    ``@context`` = "https://www.w3.org/ns/activitystreams"
                    id = activityId
                    ``type`` = "Create"
                    actor = $"{websiteBaseUrl}/@lqdev"
                    published = item.PubDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    ``object`` = note
                |})
        
        // Create outbox collection
        let outbox = {|
            ``@context`` = "https://www.w3.org/ns/activitystreams"
            id = $"{websiteBaseUrl}/activitypub/outbox"
            ``type`` = "OrderedCollection"
            totalItems = activities.Length
            orderedItems = activities
        |}
        
        let options = JsonSerializerOptions(WriteIndented = true)
        options.Encoder <- System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        let outboxJson = JsonSerializer.Serialize(outbox, options)
        File.WriteAllText(Path.Combine(outputBaseDir, "outbox.json"), outboxJson)
        printfn $"✅ Generated outbox.json with {activities.Length} activities"
        printfn $"✅ Generated {activities.Length} individual note files"
    with
    | ex -> 
        printfn $"❌ Error processing RSS feed: {ex.Message}"
        printfn "Creating fallback data..."
        
        // Create fallback empty outbox
        let emptyOutbox = {|
            ``@context`` = "https://www.w3.org/ns/activitystreams"
            id = $"{websiteBaseUrl}/activitypub/outbox"
            ``type`` = "OrderedCollection"
            totalItems = 0
            orderedItems = [||]
        |}
        
        let options = JsonSerializerOptions(WriteIndented = true)
        options.Encoder <- System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        let outboxJson = JsonSerializer.Serialize(emptyOutbox, options)
        File.WriteAllText(Path.Combine(outputBaseDir, "outbox.json"), outboxJson)
        printfn "✅ Generated fallback empty outbox.json"

// Execute all generation functions
printfn "🚀 Starting ActivityPub data generation..."
generateActorProfile ()
generateWebFinger ()
convertRssToActivityPub ()

printfn "\n🎯 Phase 2 RSS Processing Complete!"
printfn $"📁 All files generated in: {outputBaseDir}"
printfn "🔗 Ready for Azure Functions to serve ActivityPub endpoints"