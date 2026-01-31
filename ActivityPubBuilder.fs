module ActivityPubBuilder

open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open System.Security.Cryptography
open System.Text
open System.Diagnostics
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

/// ActivityPub Image attachment for media rendering
/// Research: Mastodon strips inline <img> tags and only renders from attachment array
[<CLIMutable>]
type ActivityPubImage = {
    [<JsonPropertyName("type")>]
    Type: string  // Always "Image"
    
    [<JsonPropertyName("mediaType")>]
    MediaType: string  // "image/jpeg", "image/png", etc.
    
    [<JsonPropertyName("url")>]
    Url: string
    
    [<JsonPropertyName("name")>]
    Name: string option  // Alt text/caption
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
    
    [<JsonPropertyName("attachment")>]
    Attachment: ActivityPubImage array option
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

/// ActivityPub Like activity for stars/favorites
/// Phase 5A: Research - Like is an activity, not wrapped in Create
[<CLIMutable>]
type ActivityPubLike = {
    [<JsonPropertyName("@context")>]
    Context: string
    
    [<JsonPropertyName("id")>]
    Id: string
    
    [<JsonPropertyName("type")>]
    Type: string  // Always "Like"
    
    [<JsonPropertyName("actor")>]
    Actor: string
    
    [<JsonPropertyName("published")>]
    Published: string
    
    [<JsonPropertyName("to")>]
    To: string array
    
    [<JsonPropertyName("cc")>]
    Cc: string array option
    
    [<JsonPropertyName("object")>]
    Object: string  // URL of the thing being liked (can be any URL)
}

/// ActivityPub Announce activity for reshares/boosts
/// Phase 5A: Research - Announce is an activity, not wrapped in Create
[<CLIMutable>]
type ActivityPubAnnounce = {
    [<JsonPropertyName("@context")>]
    Context: string
    
    [<JsonPropertyName("id")>]
    Id: string
    
    [<JsonPropertyName("type")>]
    Type: string  // Always "Announce"
    
    [<JsonPropertyName("actor")>]
    Actor: string
    
    [<JsonPropertyName("published")>]
    Published: string
    
    [<JsonPropertyName("to")>]
    To: string array
    
    [<JsonPropertyName("cc")>]
    Cc: string array option
    
    [<JsonPropertyName("object")>]
    Object: string  // URL of the thing being announced (can be any URL)
}

/// ActivityPub OrderedCollection for outbox
/// Research: MUST be OrderedCollection (not Collection) per spec
/// Phase 5A: Updated to support mixed activity types (Create, Like, Announce)
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
    OrderedItems: obj array  // Mixed types: ActivityPubCreate, ActivityPubLike, ActivityPubAnnounce
}

/// Configuration constants for ActivityPub generation
module Config =
    let baseUrl = "https://lqdev.me"
    let activityPubBase = sprintf "%s/api/activitypub" baseUrl
    let actorUri = sprintf "%s/actor" activityPubBase
    let followersCollection = sprintf "%s/followers" activityPubBase
    let outboxUri = sprintf "%s/outbox" activityPubBase
    let publicCollection = "https://www.w3.org/ns/activitystreams#Public"
    let activityStreamsContext = "https://www.w3.org/ns/activitystreams"
    // Phase 5A: Migrating from /notes/ to /activities/ for mixed activity types
    // Azure Function API endpoint for activity dereferencing (ensures correct Content-Type headers)
    // Static files still generated at /activitypub/activities/ for CDN caching
    let activitiesPath = "/api/activitypub/activities/"

/// Generate MD5 hash for stable Note IDs
/// Research: IDs must be stable across rebuilds, dereferenceable, globally unique
let generateHash (content: string) : string =
    use md5 = MD5.Create()
    let hash = md5.ComputeHash(Encoding.UTF8.GetBytes(content))
    Convert.ToHexString(hash).ToLowerInvariant()

/// Generate stable ActivityPub Activity ID from content
/// Phase 5A: Updated to use /activities/ path for all activity types
/// Research: Using content hash ensures stability and uniqueness
let generateActivityId (url: string) (content: string) : string =
    let hash = generateHash (url + content)
    sprintf "%s%s%s" Config.baseUrl Config.activitiesPath hash

/// Generate Note object ID (uses #object fragment to differentiate from Create activity)
/// Research: Fragment pattern allows both Create and Note to share same base URL
/// The Create activity ID matches the fetchable URL, Note uses fragment
let generateObjectId (activityId: string) : string =
    sprintf "%s#object" activityId

/// Generate Create activity ID (same as Note ID base, no fragment)
/// Research: Activity ID must match the fetchable URL for Mastodon discoverability
/// Fix: Mastodon validates that fetched URL matches the 'id' field exactly
let generateCreateActivityId (noteId: string) : string =
    // Strip any fragment from Note ID to get the base fetchable URL
    if noteId.Contains("#") then
        noteId.Split('#').[0]
    else
        noteId  // Already a base URL

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

/// Detect media type from URL extension
/// Research: Proper MIME types required for ActivityPub image rendering
let detectMediaTypeFromUrl (url: string) : string =
    let extension = Path.GetExtension(url).ToLower()
    match extension with
    | ".jpg" | ".jpeg" -> "image/jpeg"
    | ".png" -> "image/png"
    | ".gif" -> "image/gif"
    | ".webp" -> "image/webp"
    | ".mp4" -> "video/mp4"
    | ".webm" -> "video/webm"
    | ".mp3" -> "audio/mpeg"
    | ".wav" -> "audio/wav"
    | ".ogg" -> "audio/ogg"
    | _ -> "image/jpeg"  // Default fallback

/// Extract media items from content with :::media blocks
/// Returns (cleaned content, media attachments array)
/// Research: Mastodon only renders images from attachment array, not inline HTML
let extractMediaAttachments (content: string) : (string * ActivityPubImage array option) =
    let mediaPattern = @":::media\s*([\s\S]*?):::(?:media)?"
    let matches = System.Text.RegularExpressions.Regex.Matches(content, mediaPattern)
    
    if matches.Count = 0 then
        (content, None)
    else
        // Extract ALL image data from each media block
        let images = 
            matches
            |> Seq.cast<System.Text.RegularExpressions.Match>
            |> Seq.collect (fun m ->
                let yamlContent = m.Groups.[1].Value
                
                // Find all URL matches in this media block (handles multiple images in albums)
                let urlMatches = System.Text.RegularExpressions.Regex.Matches(yamlContent, @"url:\s*[""']([^""']+)[""']")
                
                urlMatches
                |> Seq.cast<System.Text.RegularExpressions.Match>
                |> Seq.map (fun urlMatch ->
                    let url = urlMatch.Groups.[1].Value
                    
                    // Determine media type from file extension
                    let mediaType = detectMediaTypeFromUrl url
                    
                    // Try to find caption near this URL (within 200 characters after URL)
                    let urlPosition = urlMatch.Index
                    let searchStart = urlPosition
                    let searchEnd = min (searchStart + 200) yamlContent.Length
                    let contextAfterUrl = yamlContent.Substring(searchStart, searchEnd - searchStart)
                    
                    let captionMatch = System.Text.RegularExpressions.Regex.Match(contextAfterUrl, @"caption:\s*[""']([^""']+)[""']")
                    let altMatch = System.Text.RegularExpressions.Regex.Match(contextAfterUrl, @"alt:\s*[""']([^""']+)[""']")
                    
                    // Prefer caption, fall back to alt text
                    let caption = 
                        if captionMatch.Success then Some captionMatch.Groups.[1].Value
                        elif altMatch.Success then Some altMatch.Groups.[1].Value
                        else None
                    
                    // Determine ActivityPub Type based on media type
                    let activityPubType = 
                        if mediaType.StartsWith("image/") then "Image"
                        elif mediaType.StartsWith("video/") then "Video"
                        elif mediaType.StartsWith("audio/") then "Audio"
                        else "Document"
                    
                    {
                        Type = activityPubType
                        MediaType = mediaType
                        Url = url
                        Name = caption
                    }))
            |> Seq.toArray
        
        // Remove :::media blocks from content
        let cleanedContent = System.Text.RegularExpressions.Regex.Replace(content, mediaPattern, "")
        
        let attachments = if images.Length > 0 then Some images else None
        // Preserve intentional line breaks by trimming only trailing spaces/tabs
        (cleanedContent.TrimEnd([| ' '; '\t' |]), attachments)

/// Convert UnifiedFeedItem to ActivityPub Note
/// Phase 5A: For replies, includes inReplyTo field
/// Research: HTML content required by Mastodon, name field improves display
/// Fix: Note ID uses #object fragment so Create activity ID matches fetchable URL
let convertToNote (item: GenericBuilder.UnifiedFeeds.UnifiedFeedItem) : ActivityPubNote =
    let activityBaseId = generateActivityId item.Url item.Content
    let noteId = generateObjectId activityBaseId  // Note gets #object fragment
    
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
    
    // Extract media attachments and clean content
    // Research: Mastodon strips inline <img> tags, only renders from attachment array
    let (cleanedContent, mediaAttachments) = extractMediaAttachments item.Content
    
    // Phase 5A: Handle inReplyTo for replies
    let inReplyTo = 
        match item.ResponseType, item.TargetUrl with
        | Some "reply", Some targetUrl -> Some targetUrl
        | _ -> None
    
    {
        Context = Config.activityStreamsContext
        Id = noteId
        Type = noteType
        AttributedTo = Config.actorUri
        Published = publishedDate
        Content = cleanedContent  // Use cleaned content without :::media blocks
        Name = name
        Url = Some item.Url
        Summary = None  // Can add excerpt extraction later
        To = toArray
        Cc = ccArray
        Tag = convertTagsToHashtags (Array.toList item.Tags)
        Source = None  // Can add markdown source preservation later
        InReplyTo = inReplyTo  // Phase 5A: Set for replies
        Sensitive = None
        Attachment = mediaAttachments  // Add extracted media attachments
    }

/// Convert Note to Create activity
/// Research: Actor must match attributedTo, addressing should match Note
let convertToCreateActivity (note: ActivityPubNote) : ActivityPubCreate =
    let activityId = generateCreateActivityId note.Id
    
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

/// Convert UnifiedFeedItem to Like activity
/// Phase 5A: Stars become Like activities (not wrapped in Create)
/// Research: Like is an activity itself, object references the target URL
let convertToLikeActivity (item: GenericBuilder.UnifiedFeeds.UnifiedFeedItem) : ActivityPubLike =
    let activityId = generateActivityId item.Url item.Content
    let targetUrl = item.TargetUrl |> Option.defaultValue item.Url  // Fallback to item URL if no target
    
    let publishedDate =
        try
            let dt = DateTimeOffset.Parse(item.Date)
            dt.ToString("yyyy-MM-dd'T'HH:mm:sszzz")
        with
        | _ -> item.Date
    
    {
        Context = Config.activityStreamsContext
        Id = activityId
        Type = "Like"
        Actor = Config.actorUri
        Published = publishedDate
        To = [| Config.publicCollection |]
        Cc = Some [| Config.followersCollection |]
        Object = targetUrl  // URL being liked (can be any web URL)
    }

/// Convert UnifiedFeedItem to Announce activity
/// Phase 5A: Reshares become Announce activities (not wrapped in Create)
/// Research: Announce is an activity itself, object references the target URL
let convertToAnnounceActivity (item: GenericBuilder.UnifiedFeeds.UnifiedFeedItem) : ActivityPubAnnounce =
    let activityId = generateActivityId item.Url item.Content
    let targetUrl = item.TargetUrl |> Option.defaultValue item.Url  // Fallback to item URL if no target
    
    let publishedDate =
        try
            let dt = DateTimeOffset.Parse(item.Date)
            dt.ToString("yyyy-MM-dd'T'HH:mm:sszzz")
        with
        | _ -> item.Date
    
    {
        Context = Config.activityStreamsContext
        Id = activityId
        Type = "Announce"
        Actor = Config.actorUri
        Published = publishedDate
        To = [| Config.publicCollection |]
        Cc = Some [| Config.followersCollection |]
        Object = targetUrl  // URL being announced (can be any web URL)
    }

/// Generate OrderedCollection outbox from mixed activity types
/// Phase 5A: Updated to support Create, Like, and Announce activities
/// Research: MUST be OrderedCollection per spec, reverse chronological order
let generateOutbox (activities: obj list) : ActivityPubOutbox =
    {
        Context = Config.activityStreamsContext
        Id = Config.outboxUri
        Type = "OrderedCollection"
        Summary = "All content updates from Luis Quintanilla's website"
        TotalItems = activities.Length
        OrderedItems = activities |> List.toArray
    }

/// Phase 5A: Feature flag for enabling native activity types
/// Set to true to use Like/Announce, false to use Create+Note for all (legacy behavior)
let useNativeActivityTypes = true

/// Phase 5A: Convert UnifiedFeedItem to appropriate ActivityPub activity
/// Routes stars → Like, reshares → Announce, replies → Create+Note with inReplyTo, everything else → Create+Note
let convertToActivity (item: GenericBuilder.UnifiedFeeds.UnifiedFeedItem) : obj =
    if not useNativeActivityTypes then
        // Legacy behavior: Everything as Create+Note
        convertToNote item |> convertToCreateActivity |> box
    else
        // Phase 5A: Route by response type
        match item.ResponseType with
        | Some "star" -> 
            convertToLikeActivity item |> box
        | Some "reshare" | Some "share" -> 
            convertToAnnounceActivity item |> box
        | Some "reply" ->
            // Replies are Create+Note with inReplyTo field
            convertToNote item |> convertToCreateActivity |> box
        | _ ->
            // Everything else (posts, notes, bookmarks, etc.) is Create+Note
            convertToNote item |> convertToCreateActivity |> box

/// Build individual ActivityPub activity files for static serving
/// Phase 5A: Renamed from buildNotes, now generates to activitypub/activities/
/// Handles mixed activity types (Create, Like, Announce)
let buildActivities (unifiedItems: GenericBuilder.UnifiedFeeds.UnifiedFeedItem list) (outputDir: string) : unit =
    printfn "  🎭 Generating individual ActivityPub activity files..."
    
    let activitiesDir = Path.Combine(outputDir, "activitypub", "activities")
    Directory.CreateDirectory(activitiesDir) |> ignore
    
    let activities = 
        unifiedItems
        |> List.map convertToActivity
    
    for activity in activities do
        // Extract ID from activity (works for all activity types)
        let json = JsonSerializer.Serialize(activity, jsonOptions)
        let doc = System.Text.Json.JsonDocument.Parse(json)
        let id = doc.RootElement.GetProperty("id").GetString()
        let hash = id.Split('/') |> Array.last
        // Strip fragment from hash for filename (e.g., #create)
        let hashWithoutFragment = hash.Split('#') |> Array.head
        let activityPath = Path.Combine(activitiesDir, sprintf "%s.json" hashWithoutFragment)
        File.WriteAllText(activityPath, json)
    
    printfn "  ✅ Generated %d ActivityPub activity files" activities.Length

/// Build ActivityPub outbox from unified feed items
/// Phase 5A: Updated to generate mixed activity types
/// Generates api/data/outbox/index.json for static serving
let buildOutbox (unifiedItems: GenericBuilder.UnifiedFeeds.UnifiedFeedItem list) (outputDir: string) : unit =
    printfn "  🎭 Converting %d items to ActivityPub format..." unifiedItems.Length
    
    // Convert all items to appropriate activities (reverse chronological)
    // Fix: Parse dates to DateTimeOffset for proper chronological sorting
    let activities = 
        unifiedItems
        |> List.sortByDescending (fun item -> 
            let mutable parsed = DateTimeOffset.MinValue
            if DateTimeOffset.TryParse(item.Date, &parsed) then parsed
            else DateTimeOffset.MinValue)  // Fallback for parse errors
        |> List.map convertToActivity
    
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
    printfn "  ✅ Total items: %d, Total activities: %d" outbox.TotalItems activities.Length

/// Queue new posts for delivery to followers
/// Called during build to queue recent posts for ActivityPub delivery
let queueRecentPostsForDelivery (unifiedItems: GenericBuilder.UnifiedFeeds.UnifiedFeedItem list) (outputDir: string) : unit =
    // Only queue items from the last 24 hours to avoid re-delivering old content
    let cutoffDate = DateTimeOffset.UtcNow.AddDays(-1.0)
    
    let recentItems = 
        unifiedItems
        |> List.filter (fun item ->
            let mutable parsed = DateTimeOffset.MinValue
            if DateTimeOffset.TryParse(item.Date, &parsed) then
                parsed >= cutoffDate
            else
                false)
    
    if recentItems.IsEmpty then
        printfn "  ✓ No recent posts to queue for delivery"
    else
        printfn "  📮 Found %d recent posts for delivery (will be queued post-deployment)" recentItems.Length
        printfn "  ℹ️  Post queueing handled by GitHub Actions workflow after deployment"
