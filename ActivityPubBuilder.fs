module ActivityPubBuilder

open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open System.Security.Cryptography
open System.Text
open GenericBuilder

/// ActivityPub domain types following W3C ActivityStreams vocabulary
/// Research-backed implementation for static site generation
/// Signatures applied at delivery time (Phase 4), not embedded in JSON

/// ActivityPub hashtag tag
[<CLIMutable>]
type ActivityPubHashtag = {
    [<JsonPropertyName("type")>]
    Type: string
    
    [<JsonPropertyName("href")>]
    Href: string
    
    [<JsonPropertyName("name")>]
    Name: string
}

/// ActivityPub source content (for markdown preservation)
[<CLIMutable>]
type ActivityPubSource = {
    [<JsonPropertyName("content")>]
    Content: string
    
    [<JsonPropertyName("mediaType")>]
    MediaType: string
}

/// ActivityPub Note object representing a blog post or status
/// Research: Required fields for Mastodon federation validated
[<CLIMutable>]
type ActivityPubNote = {
    // Required by spec
    [<JsonPropertyName("@context")>]
    Context: string
    
    [<JsonPropertyName("id")>]
    Id: string
    
    [<JsonPropertyName("type")>]
    Type: string
    
    [<JsonPropertyName("attributedTo")>]
    AttributedTo: string
    
    [<JsonPropertyName("published")>]
    Published: string
    
    [<JsonPropertyName("content")>]
    Content: string
    
    [<JsonPropertyName("to")>]
    To: string array
    
    // Highly recommended for UX
    [<JsonPropertyName("name")>]
    Name: string option
    
    [<JsonPropertyName("url")>]
    Url: string option
    
    [<JsonPropertyName("summary")>]
    Summary: string option
    
    [<JsonPropertyName("cc")>]
    Cc: string array option
    
    [<JsonPropertyName("tag")>]
    Tag: ActivityPubHashtag array option
    
    [<JsonPropertyName("source")>]
    Source: ActivityPubSource option
    
    [<JsonPropertyName("inReplyTo")>]
    InReplyTo: string option
    
    [<JsonPropertyName("sensitive")>]
    Sensitive: bool option
}

/// ActivityPub Create activity wrapping a Note
/// Research: Actor must match Note's attributedTo, addressing should match
[<CLIMutable>]
type ActivityPubCreate = {
    [<JsonPropertyName("@context")>]
    Context: string
    
    [<JsonPropertyName("id")>]
    Id: string
    
    [<JsonPropertyName("type")>]
    Type: string
    
    [<JsonPropertyName("actor")>]
    Actor: string
    
    [<JsonPropertyName("published")>]
    Published: string
    
    [<JsonPropertyName("to")>]
    To: string array
    
    [<JsonPropertyName("cc")>]
    Cc: string array option
    
    [<JsonPropertyName("object")>]
    Object: ActivityPubNote
}

/// ActivityPub OrderedCollection for outbox
/// Research: MUST be OrderedCollection (not Collection) per spec
[<CLIMutable>]
type ActivityPubOutbox = {
    [<JsonPropertyName("@context")>]
    Context: string
    
    [<JsonPropertyName("id")>]
    Id: string
    
    [<JsonPropertyName("type")>]
    Type: string
    
    [<JsonPropertyName("summary")>]
    Summary: string
    
    [<JsonPropertyName("totalItems")>]
    TotalItems: int
    
    [<JsonPropertyName("orderedItems")>]
    OrderedItems: ActivityPubCreate array
}

/// Configuration constants for ActivityPub generation
module Config =
    let actorUri = "https://lqdev.me/api/actor"
    let baseUrl = "https://lqdev.me"
    let followersCollection = "https://lqdev.me/api/followers"
    let publicCollection = "https://www.w3.org/ns/activitystreams#Public"
    let activityStreamsContext = "https://www.w3.org/ns/activitystreams"
    let outboxUri = "https://lqdev.me/api/outbox"
    let notesPath = "/activitypub/notes/"  // Static path for individual notes

/// Generate MD5 hash for stable Note IDs
/// Research: IDs must be stable across rebuilds, dereferenceable, globally unique
let generateHash (content: string) : string =
    use md5 = MD5.Create()
    let hash = md5.ComputeHash(Encoding.UTF8.GetBytes(content))
    Convert.ToHexString(hash).ToLowerInvariant()

/// Generate stable ActivityPub Note ID from content
/// Research: Using content hash ensures stability and uniqueness
let generateNoteId (url: string) (content: string) : string =
    let hash = generateHash (url + content)
    sprintf "%s%s%s" Config.baseUrl Config.notesPath hash

/// Generate Create activity ID from Note ID
/// Research: Fragment identifier pattern (#create) is acceptable
let generateActivityId (noteId: string) : string =
    sprintf "%s#create" noteId

/// Convert tags to ActivityPub hashtags
/// Research: Hashtags should include # symbol in name, href points to tag page
let convertTagsToHashtags (tags: string list) : ActivityPubHashtag array option =
    if tags.IsEmpty then None
    else
        tags
        |> List.map (fun tag ->
            let cleanTag = TagService.processTagName tag
            {
                Type = "Hashtag"
                Href = sprintf "%s/tags/%s" Config.baseUrl cleanTag
                Name = sprintf "#%s" cleanTag
            })
        |> List.toArray
        |> Some

/// JSON serialization options for ActivityPub
let private jsonOptions =
    let options = JsonSerializerOptions()
    options.WriteIndented <- true
    options.DefaultIgnoreCondition <- JsonIgnoreCondition.WhenWritingNull
    options

/// Convert UnifiedFeedItem to ActivityPub Note
/// Research: HTML content required by Mastodon, name field improves display
let convertToNote (item: GenericBuilder.UnifiedFeeds.UnifiedFeedItem) : ActivityPubNote =
    let noteId = generateNoteId item.Url item.Content
    
    // Research: Use Article type for posts, Note for everything else
    let noteType = 
        if item.ContentType = "posts" then "Article"
        else "Note"
    
    // Research: Name field (plain text title) improves presentation in clients
    let name = 
        if String.IsNullOrWhiteSpace(item.Title) then None
        else Some item.Title
    
    // Convert date to RFC 3339 format for ActivityPub spec compliance
    // Research: Published field must use RFC 3339 datetime format
    let publishedDate =
        try
            let dt = DateTimeOffset.Parse(item.Date)
            dt.ToString("yyyy-MM-dd'T'HH:mm:sszzz")
        with
        | _ -> item.Date  // Fallback to original if parsing fails
    
    // Research: Public posts use Public in 'to', followers in 'cc'
    let toArray = [| Config.publicCollection |]
    let ccArray = Some [| Config.followersCollection |]
    
    {
        Context = Config.activityStreamsContext
        Id = noteId
        Type = noteType
        AttributedTo = Config.actorUri
        Published = publishedDate
        Content = item.Content  // Already HTML from processing
        Name = name
        Url = Some item.Url
        Summary = None  // Can add excerpt extraction later
        To = toArray
        Cc = ccArray
        Tag = convertTagsToHashtags (Array.toList item.Tags)
        Source = None  // Can add markdown source preservation later
        InReplyTo = None
        Sensitive = None
    }

/// Convert Note to Create activity
/// Research: Actor must match attributedTo, addressing should match Note
let convertToCreateActivity (note: ActivityPubNote) : ActivityPubCreate =
    let activityId = generateActivityId note.Id
    
    {
        Context = Config.activityStreamsContext
        Id = activityId
        Type = "Create"
        Actor = Config.actorUri
        Published = note.Published
        To = note.To
        Cc = note.Cc
        Object = note
    }

/// Generate OrderedCollection outbox from Create activities
/// Research: MUST be OrderedCollection per spec, reverse chronological order
let generateOutbox (activities: ActivityPubCreate list) : ActivityPubOutbox =
    {
        Context = Config.activityStreamsContext
        Id = Config.outboxUri
        Type = "OrderedCollection"
        Summary = "All content updates from Luis Quintanilla's website"
        TotalItems = activities.Length
        OrderedItems = activities |> List.toArray
    }

/// Build individual ActivityPub note files for static serving
/// Generates activitypub/notes/{hash}.json files
let buildNotes (unifiedItems: GenericBuilder.UnifiedFeeds.UnifiedFeedItem list) (outputDir: string) : unit =
    printfn "  🎭 Generating individual ActivityPub note files..."
    
    let notesDir = Path.Combine(outputDir, "activitypub", "notes")
    Directory.CreateDirectory(notesDir) |> ignore
    
    let notes = 
        unifiedItems
        |> List.map convertToNote
    
    for note in notes do
        // Extract hash from note ID (last segment of URL)
        let noteId = note.Id.Split('/') |> Array.last
        let notePath = Path.Combine(notesDir, sprintf "%s.json" noteId)
        let json = JsonSerializer.Serialize(note, jsonOptions)
        File.WriteAllText(notePath, json)
    
    printfn "  ✅ Generated %d ActivityPub note files" notes.Length

/// Build ActivityPub outbox from unified feed items
/// Generates api/data/outbox/index.json for static serving
let buildOutbox (unifiedItems: GenericBuilder.UnifiedFeeds.UnifiedFeedItem list) (outputDir: string) : unit =
    printfn "  🎭 Converting %d items to ActivityPub format..." unifiedItems.Length
    
    // Convert all items to Create activities (reverse chronological)
    // Fix: Parse dates to DateTimeOffset for proper chronological sorting
    let activities = 
        unifiedItems
        |> List.sortByDescending (fun item -> 
            let mutable parsed = DateTimeOffset.MinValue
            if DateTimeOffset.TryParse(item.Date, &parsed) then parsed
            else DateTimeOffset.MinValue)  // Fallback for parse errors
        |> List.map (convertToNote >> convertToCreateActivity)
    
    printfn "  🎭 Generated %d Create activities" activities.Length
    
    // Generate outbox collection
    let outbox = generateOutbox activities
    
    // Serialize to JSON
    let json = JsonSerializer.Serialize(outbox, jsonOptions)
    
    // Ensure directory exists
    let outboxDir = Path.Combine(outputDir, "api", "data", "outbox")
    Directory.CreateDirectory(outboxDir) |> ignore
    
    // Write outbox file
    let outboxPath = Path.Combine(outboxDir, "index.json")
    File.WriteAllText(outboxPath, json)
    
    printfn "  ✅ ActivityPub outbox: %s" outboxPath
    printfn "  ✅ Total items: %d, Total Create activities: %d" outbox.TotalItems activities.Length
