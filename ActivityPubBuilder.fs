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

/// ActivityPub Link attachment for bookmark URLs
/// Phase 5B: Research - FEP-8967 standardizes Link attachments for preview generation
/// Mastodon 4.5+ uses Link attachments to signal which URLs should get preview cards
[<CLIMutable>]
type ActivityPubLink = {
    [<JsonPropertyName("type")>]
    Type: string  // Always "Link"
    
    [<JsonPropertyName("href")>]
    Href: string  // The bookmarked URL
    
    [<JsonPropertyName("name")>]
    Name: string option  // Title of the bookmarked resource
}

/// Phase 5C: Schema.org Rating object for reviewRating
/// Research: schema:Rating with ratingValue, bestRating, worstRating
[<CLIMutable>]
type SchemaRating = {
    [<JsonPropertyName("@type")>]
    Type: string  // Always "schema:Rating"
    
    [<JsonPropertyName("schema:ratingValue")>]
    RatingValue: float
    
    [<JsonPropertyName("schema:bestRating")>]
    BestRating: float
    
    [<JsonPropertyName("schema:worstRating")>]
    WorstRating: float
}

/// Phase 5C: Schema.org itemReviewed object
/// Research: Represents the item being reviewed (Book, Movie, Product, etc.)
[<CLIMutable>]
type SchemaItemReviewed = {
    [<JsonPropertyName("@type")>]
    Type: string  // "schema:Book", "schema:Movie", "schema:Product", etc.
    
    [<JsonPropertyName("schema:name")>]
    Name: string
    
    [<JsonPropertyName("schema:author")>]
    Author: string option
    
    [<JsonPropertyName("schema:isbn")>]
    Isbn: string option
    
    [<JsonPropertyName("schema:url")>]
    Url: string option
    
    [<JsonPropertyName("schema:image")>]
    Image: string option
}

/// Phase 5D: ActivityPub Media Object for native Image/Video/Audio
/// Research: Media-primary content uses Image/Video/Audio as top-level object type
/// instead of Note with attachment. This enables native rendering in Pixelfed-style clients.
[<CLIMutable>]
type ActivityPubMediaObject = {
    [<JsonPropertyName("@context")>]
    Context: string
    
    [<JsonPropertyName("id")>]
    Id: string
    
    [<JsonPropertyName("type")>]
    Type: string  // "Image", "Video", or "Audio"
    
    [<JsonPropertyName("attributedTo")>]
    AttributedTo: string
    
    [<JsonPropertyName("published")>]
    Published: string
    
    [<JsonPropertyName("url")>]
    Url: string  // Media file URL
    
    [<JsonPropertyName("mediaType")>]
    MediaType: string  // MIME type (e.g., "image/jpeg", "video/mp4")
    
    [<JsonPropertyName("name")>]
    Name: string option  // Caption
    
    [<JsonPropertyName("summary")>]
    Summary: string option  // Alt text for accessibility
    
    [<JsonPropertyName("to")>]
    To: string array
    
    [<JsonPropertyName("cc")>]
    Cc: string array option
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
    Attachment: obj array option  // Phase 5B: Supports both ActivityPubImage and ActivityPubLink
    
    // Phase 5C: Schema.org Review vocabulary extension
    // These properties are only set for review content types
    [<JsonPropertyName("schema:reviewRating")>]
    ReviewRating: SchemaRating option
    
    [<JsonPropertyName("schema:itemReviewed")>]
    ItemReviewed: SchemaItemReviewed option
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

/// ActivityPub OrderedCollection for outbox (root collection - no items, only pagination links)
/// Research: Per spec, root OrderedCollection should NOT contain orderedItems inline
/// Phase 5F: Updated to use proper pagination with first/last links
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
    
    [<JsonPropertyName("first")>]
    First: string  // URL to first page
    
    [<JsonPropertyName("last")>]
    Last: string  // URL to last page
}

/// ActivityPub OrderedCollectionPage for paginated outbox
/// Research: Each page contains orderedItems + navigation (partOf, next, prev)
/// Phase 5F: Standard pagination with 50 items per page
[<CLIMutable>]
type ActivityPubOutboxPage = {
    [<JsonPropertyName("@context")>]
    Context: string
    
    [<JsonPropertyName("id")>]
    Id: string
    
    [<JsonPropertyName("type")>]
    Type: string
    
    [<JsonPropertyName("partOf")>]
    PartOf: string  // URL to root collection
    
    [<JsonPropertyName("next")>]
    Next: string option  // URL to next page (null for last page)
    
    [<JsonPropertyName("prev")>]
    Prev: string option  // URL to previous page (null for first page)
    
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
    // Phase 5C: Schema.org context for review metadata
    let schemaOrgContext = "https://schema.org/"
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
            let hashtag : ActivityPubHashtag = {
                Type = "Hashtag"
                Href = sprintf "%s/tags/%s" Config.baseUrl cleanTag
                Name = sprintf "#%s" cleanTag
            }
            hashtag)
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
/// Phase 5B: Returns obj array to support both Image and Link attachments
let extractMediaAttachments (content: string) : (string * obj array option) =
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
                    
                    let image : ActivityPubImage = {
                        Type = activityPubType
                        MediaType = mediaType
                        Url = url
                        Name = caption
                    }
                    box image))
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
    
    // Phase 5B: Add Link attachment for bookmarks
    // Research: FEP-8967 - Link attachments signal URLs for preview card generation
    let linkAttachment : obj option = 
        match item.ResponseType, item.TargetUrl with
        | Some "bookmark", Some targetUrl -> 
            let link : ActivityPubLink = {
                Type = "Link"
                Href = targetUrl
                Name = if String.IsNullOrWhiteSpace(item.Title) then None else Some item.Title
            }
            Some (box link)
        | _ -> None
    
    // Combine media attachments with link attachment if present
    let allAttachments = 
        match mediaAttachments, linkAttachment with
        | Some media, Some link -> Some (Array.append media [| link |])
        | Some media, None -> Some media
        | None, Some link -> Some [| link |]
        | None, None -> None
    
    // Phase 5C: Create Schema.org review properties for review content
    let (reviewRating, itemReviewed) =
        match item.ReviewData with
        | Some reviewData ->
            // Map item type to Schema.org type
            let schemaItemType = 
                match reviewData.ItemType.ToLowerInvariant() with
                | "book" -> "schema:Book"
                | "movie" | "film" -> "schema:Movie"
                | "music" | "album" -> "schema:MusicAlbum"
                | "product" -> "schema:Product"
                | "business" | "restaurant" | "place" -> "schema:LocalBusiness"
                | _ -> "schema:Thing"  // Generic fallback
            
            let rating : SchemaRating = {
                Type = "schema:Rating"
                RatingValue = reviewData.Rating
                BestRating = reviewData.Scale
                WorstRating = 1.0
            }
            
            let reviewed : SchemaItemReviewed = {
                Type = schemaItemType
                Name = reviewData.ItemName
                Author = reviewData.Author
                Isbn = reviewData.Isbn
                Url = reviewData.ItemUrl
                Image = reviewData.ImageUrl
            }
            
            (Some rating, Some reviewed)
        | None ->
            (None, None)
    
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
        Attachment = allAttachments  // Phase 5B: Combined media + link attachments
        // Phase 5C: Schema.org review properties
        ReviewRating = reviewRating
        ItemReviewed = itemReviewed
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

/// Phase 5D: Convert UnifiedFeedItem to native media object
/// Media-primary content (from media directory) uses Image/Video/Audio as top-level type
/// Research: Native media objects render properly in Pixelfed and media-focused clients
let convertToMediaObject (item: GenericBuilder.UnifiedFeeds.UnifiedFeedItem) : ActivityPubMediaObject option =
    match item.MediaData with
    | None -> None
    | Some mediaData ->
        let activityBaseId = generateActivityId item.Url item.Content
        let mediaId = generateObjectId activityBaseId  // Media gets #object fragment
        
        let publishedDate =
            try
                let dt = DateTimeOffset.Parse(item.Date)
                dt.ToString("yyyy-MM-dd'T'HH:mm:sszzz")
            with
            | _ -> item.Date
        
        Some {
            Context = Config.activityStreamsContext
            Id = mediaId
            Type = mediaData.ObjectType  // "Image", "Video", or "Audio"
            AttributedTo = Config.actorUri
            Published = publishedDate
            Url = mediaData.MediaUrl
            MediaType = mediaData.MediaType
            Name = mediaData.Caption  // Caption as name
            Summary = mediaData.AltText  // Alt text as summary for accessibility
            To = [| Config.publicCollection |]
            Cc = Some [| Config.followersCollection |]
        }

/// Phase 5D: Convert UnifiedFeedItem with MediaData to Create activity wrapping media object
let convertToCreateMediaActivity (item: GenericBuilder.UnifiedFeeds.UnifiedFeedItem) : obj option =
    match convertToMediaObject item with
    | None -> None
    | Some mediaObj ->
        // Create wrapper activity
        let activityBaseId = generateActivityId item.Url item.Content
        let createId = generateCreateActivityId activityBaseId  // Create uses base URL (fetchable)
        
        // Build Create activity as anonymous record to match existing pattern
        Some (box {|
            ``@context`` = Config.activityStreamsContext
            id = createId
            ``type`` = "Create"
            actor = Config.actorUri
            published = mediaObj.Published
            ``to`` = mediaObj.To
            cc = mediaObj.Cc
            ``object`` = mediaObj
        |})

/// Phase 5F: Items per page for outbox pagination
/// Research: Mastodon uses 20-50 items per page, 50 is a good balance
let itemsPerPage = 50

/// Phase 5F: Generate root OrderedCollection for outbox (no items, only pagination links)
/// Research: Root collection should only contain metadata + first/last links
let generateOutboxRoot (totalItems: int) (totalPages: int) : ActivityPubOutbox =
    {
        Context = Config.activityStreamsContext
        Id = Config.outboxUri
        Type = "OrderedCollection"
        Summary = "All content updates from Luis Quintanilla's website"
        TotalItems = totalItems
        First = sprintf "%s?page=1" Config.outboxUri
        Last = sprintf "%s?page=%d" Config.outboxUri totalPages
    }

/// Phase 5F: Generate OrderedCollectionPage for a specific page
/// Research: Each page contains orderedItems + navigation links
let generateOutboxPage (pageNum: int) (totalPages: int) (items: obj array) : ActivityPubOutboxPage =
    {
        Context = Config.activityStreamsContext
        Id = sprintf "%s?page=%d" Config.outboxUri pageNum
        Type = "OrderedCollectionPage"
        PartOf = Config.outboxUri
        Next = if pageNum < totalPages then Some (sprintf "%s?page=%d" Config.outboxUri (pageNum + 1)) else None
        Prev = if pageNum > 1 then Some (sprintf "%s?page=%d" Config.outboxUri (pageNum - 1)) else None
        OrderedItems = items
    }

/// Phase 5A: Feature flag for enabling native activity types
/// Set to true to use Like/Announce, false to use Create+Note for all (legacy behavior)
let useNativeActivityTypes = true

/// Phase 5D: Feature flag for enabling native media objects
/// Set to false: Use Note+attachment (Mastodon/Pixelfed compatible) as primary
/// Set to true: Use standalone Image/Video/Audio objects (breaks rendering in Mastodon)
/// Research: Mastodon only renders attachments on Note objects, not standalone media objects
let useNativeMediaObjects = false

/// Phase 5A: Convert UnifiedFeedItem to appropriate ActivityPub activity
/// Routes stars → Like, reshares → Announce, replies → Create+Note with inReplyTo
/// Phase 5D: Routes media-primary content → Create+Image/Video/Audio
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
            // Phase 5D: Check for media-primary content
            if useNativeMediaObjects && item.MediaData.IsSome then
                match convertToCreateMediaActivity item with
                | Some mediaActivity -> mediaActivity
                | None -> 
                    // Fallback to Note+attachment if media conversion fails
                    convertToNote item |> convertToCreateActivity |> box
            else
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
/// Phase 5F: Updated to generate paginated outbox structure
/// Generates:
///   - api/data/outbox/index.json (root OrderedCollection with first/last links)
///   - api/data/outbox/page-1.json, page-2.json, etc. (OrderedCollectionPage with items)
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
    
    // Count activity types for logging
    let activityCounts = 
        activities 
        |> List.groupBy (fun a -> 
            let json = JsonSerializer.Serialize(a, jsonOptions)
            let doc = System.Text.Json.JsonDocument.Parse(json)
            doc.RootElement.GetProperty("type").GetString())
        |> List.map (fun (t, items) -> (t, items.Length))
        |> List.sortByDescending snd
    let countStr = activityCounts |> List.map (fun (t, c) -> sprintf "%d %s" c t) |> String.concat ", "
    printfn "  🎭 Generated activities: %s" countStr
    
    // Ensure directory exists
    let outboxDir = Path.Combine(outputDir, "api", "data", "outbox")
    Directory.CreateDirectory(outboxDir) |> ignore
    
    // Phase 5F: Generate paginated structure
    let activitiesArray = activities |> List.toArray
    let totalItems = activitiesArray.Length
    let totalPages = max 1 ((totalItems + itemsPerPage - 1) / itemsPerPage)  // Ceiling division
    
    printfn "  📄 Generating %d pages (%d items per page)..." totalPages itemsPerPage
    
    // Generate each page
    for pageNum in 1 .. totalPages do
        let startIdx = (pageNum - 1) * itemsPerPage
        let endIdx = min (startIdx + itemsPerPage) totalItems
        let pageItems = activitiesArray.[startIdx .. endIdx - 1]
        
        let page = generateOutboxPage pageNum totalPages pageItems
        let pageJson = JsonSerializer.Serialize(page, jsonOptions)
        let pagePath = Path.Combine(outboxDir, sprintf "page-%d.json" pageNum)
        File.WriteAllText(pagePath, pageJson)
    
    // Generate root collection (no items, just pagination links)
    let outbox = generateOutboxRoot totalItems totalPages
    let json = JsonSerializer.Serialize(outbox, jsonOptions)
    let outboxPath = Path.Combine(outboxDir, "index.json")
    File.WriteAllText(outboxPath, json)
    
    printfn "  ✅ ActivityPub outbox: %s (%d pages)" outboxPath totalPages
    printfn "  ✅ Total items: %d across %d pages" totalItems totalPages

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
