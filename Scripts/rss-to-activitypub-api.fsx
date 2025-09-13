#r "nuget: FSharp.Data"
#r "nuget: System.Text.Json"

open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open System.Security.Cryptography
open System.Text
open FSharp.Data

// Enhanced script to output to API data directory with /activitypub namespace
let outputBaseDir = "api/data"
let websiteBaseUrl = "https://www.lqdev.me"

// Generate MD5 hash for content
let generateContentHash (content: string) =
    use md5 = MD5.Create()
    let hash = md5.ComputeHash(Encoding.UTF8.GetBytes(content))
    BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()

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
        summary = "AI whisperer wandering the shifting sands of the desert of the real . Semi-fluent in the language of machines, with an affinity for the F# dialect."
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
            |> Array.map (fun item ->
                // Generate hash from stable item identifiers for consistent IDs
                let contentForHash = $"{item.Title}{item.Link}{item.PubDate}"
                let noteHash = generateContentHash contentForHash
                let noteId = $"{websiteBaseUrl}/activitypub/notes/{noteHash}"
                let activityId = $"{websiteBaseUrl}/activitypub/outbox#{noteHash}"
                
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
                File.WriteAllText(Path.Combine(outputBaseDir, "notes", $"{noteHash}.json"), noteJson)
                
                // Create activity
                {|
                    ``@context`` = "https://www.w3.org/ns/activitystreams"
                    id = activityId
                    ``type`` = "Create"
                    actor = $"{websiteBaseUrl}/@lqdev"
                    published = item.PubDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    ``object`` = note
                |}, noteHash)
        
        // Create outbox collection
        let outbox = {|
            ``@context`` = "https://www.w3.org/ns/activitystreams"
            id = $"{websiteBaseUrl}/activitypub/outbox"
            ``type`` = "OrderedCollection"
            totalItems = activities.Length
            orderedItems = activities |> Array.map fst
        |}
        
        let options = JsonSerializerOptions(WriteIndented = true)
        options.Encoder <- System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        let outboxJson = JsonSerializer.Serialize(outbox, options)
        File.WriteAllText(Path.Combine(outputBaseDir, "outbox.json"), outboxJson)
        
        let hashes = activities |> Array.map snd
        let hashSample = hashes |> Array.take 3 |> String.concat ", "
        printfn $"✅ Generated outbox.json with {activities.Length} activities"
        printfn $"✅ Generated {activities.Length} individual note files with hashes: {hashSample}..."
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