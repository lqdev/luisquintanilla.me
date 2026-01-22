# ActivityPub Implementation Plan for F# Website

> ⚠️ **ARCHIVED DOCUMENT**  
> This document is maintained for historical reference only.  
> For current implementation details, see: [ARCHITECTURE-OVERVIEW.md](../ARCHITECTURE-OVERVIEW.md)

> **📋 NOTE**: This is the **original implementation plan** for historical reference and technical details.  
> **For current implementation status**, see [`implementation-status.md`](../implementation-status.md)  
> **For API reference**, see [`/api/ACTIVITYPUB.md`](../../api/ACTIVITYPUB.md)

**Document Version**: 1.0  
**Date**: August 31, 2025  
**Status**: **Phases 1-2 Complete**, Phase 3-4 Planned  

## Overview

This document outlines the implementation plan for transforming the F# static website into a Fediverse node using ActivityPub, enabling federation with Mastodon, Pleroma, and other ActivityPub-compatible platforms while maintaining the operational benefits of static site generation.

## Research Foundation

### ActivityPub Protocol Requirements

ActivityPub operates through two interconnected protocols:
- **Client-to-Server**: User interactions with the server
- **Server-to-Server**: Federation between instances

**Core Required Endpoints**:
- `/.well-known/webfinger` - Discovery endpoint for account lookup
- `/actor` - Actor document with profile metadata and public keys
- `/outbox` - Public activity stream (similar to RSS but ActivityStreams format)
- `/inbox` - Receives federation requests (POST endpoint, requires dynamic processing)

**Authentication**: HTTP Signatures with RSA public/private key pairs for cryptographic verification of federation requests.

### Hybrid Implementation Pattern (Research-Backed)

Based on analysis of successful implementations (maho.dev, s3lph.me, "Almost Static ActivityPub"), the optimal approach is:

- **80-90% Static Files**: Actor documents, outbox, individual posts as ActivityPub objects
- **10-20% Dynamic Components**: Inbox processing, follow management, activity delivery
- **Serverless Functions**: Azure Functions for minimal dynamic requirements
- **Cost Profile**: $10-50/month even with thousands of followers vs. $100+ for managed Mastodon

## Current Architecture Advantages

### Exceptional Fit Factors

1. **Strong Type System**: F# records map perfectly to ActivityPub JSON-LD objects
2. **Unified Content Pipeline**: `GenericBuilder.fs` already processes all content types
3. **Feed Generation**: RSS system can be extended to generate ActivityPub objects
4. **IndieWeb Foundation**: Response types already map to ActivityPub activities
5. **Azure Integration**: Static Web Apps + Functions provide ideal hosting platform

### Existing Assets for ActivityPub

- **Domain Types**: `ITaggable` interface provides unified content processing
- **Content Processors**: Each content type has structured processors in `GenericBuilder.fs`
- **RSS Generation**: XML feed system can be parallel-extended for ActivityPub JSON
- **Unified Feeds**: `UnifiedFeeds` module already aggregates all content types
- **Response System**: IndieWeb response types map directly to ActivityPub activities

## Implementation Phases

### Phase 1: Static ActivityPub Foundation (Weeks 1-2)

**Objective**: Establish basic federation presence with static files only

#### 1.1 Domain Type Extensions

Add ActivityPub types to `Domain.fs`:

```fsharp
// ActivityPub domain types
[<CLIMutable>]
type ActivityPubContext = {
    [<JsonPropertyName("@context")>] Context: obj
}

[<CLIMutable>]
type ActivityPubActor = {
    [<JsonPropertyName("@context")>] Context: string array
    [<JsonPropertyName("id")>] Id: string
    [<JsonPropertyName("type")>] Type: string
    [<JsonPropertyName("name")>] Name: string
    [<JsonPropertyName("preferredUsername")>] PreferredUsername: string
    [<JsonPropertyName("summary")>] Summary: string
    [<JsonPropertyName("url")>] Url: string
    [<JsonPropertyName("icon")>] Icon: ActivityPubImage
    [<JsonPropertyName("image")>] Image: ActivityPubImage option
    [<JsonPropertyName("inbox")>] Inbox: string
    [<JsonPropertyName("outbox")>] Outbox: string
    [<JsonPropertyName("followers")>] Followers: string
    [<JsonPropertyName("following")>] Following: string
    [<JsonPropertyName("publicKey")>] PublicKey: ActivityPubPublicKey
    [<JsonPropertyName("endpoints")>] Endpoints: ActivityPubEndpoints option
}

[<CLIMutable>]
type ActivityPubNote = {
    [<JsonPropertyName("@context")>] Context: obj
    [<JsonPropertyName("id")>] Id: string
    [<JsonPropertyName("type")>] Type: string
    [<JsonPropertyName("content")>] Content: string
    [<JsonPropertyName("contentMap")>] ContentMap: Map<string, string> option
    [<JsonPropertyName("published")>] Published: string
    [<JsonPropertyName("updated")>] Updated: string option
    [<JsonPropertyName("attributedTo")>] AttributedTo: string
    [<JsonPropertyName("to")>] To: string array
    [<JsonPropertyName("cc")>] Cc: string array option
    [<JsonPropertyName("tag")>] Tag: ActivityPubTag array option
    [<JsonPropertyName("attachment")>] Attachment: ActivityPubAttachment array option
    [<JsonPropertyName("url")>] Url: string
    [<JsonPropertyName("sensitive")>] Sensitive: bool option
    [<JsonPropertyName("summary")>] Summary: string option
}

[<CLIMutable>]
type ActivityPubActivity = {
    [<JsonPropertyName("@context")>] Context: obj
    [<JsonPropertyName("id")>] Id: string
    [<JsonPropertyName("type")>] Type: string
    [<JsonPropertyName("actor")>] Actor: string
    [<JsonPropertyName("published")>] Published: string
    [<JsonPropertyName("to")>] To: string array
    [<JsonPropertyName("cc")>] Cc: string array option
    [<JsonPropertyName("object")>] Object: obj
}

[<CLIMutable>]
type ActivityPubCollection = {
    [<JsonPropertyName("@context")>] Context: obj
    [<JsonPropertyName("id")>] Id: string
    [<JsonPropertyName("type")>] Type: string
    [<JsonPropertyName("totalItems")>] TotalItems: int
    [<JsonPropertyName("first")>] First: string option
    [<JsonPropertyName("last")>] Last: string option
    [<JsonPropertyName("orderedItems")>] OrderedItems: obj array option
}
```

#### 1.2 ActivityPub Content Processors

Extend `GenericBuilder.fs` with ActivityPub generation:

```fsharp
module ActivityPubProcessor =
    
    let private actorId = "https://www.luisquintanilla.me/actor"
    let private baseUrl = "https://www.luisquintanilla.me"
    
    /// Convert existing content to ActivityPub Note objects
    let convertToNote (contentType: string) (title: string) (content: string) (date: string) (url: string) (tags: string array) : ActivityPubNote =
        let activityPubContext = [| 
            "https://www.w3.org/ns/activitystreams"
            "https://w3id.org/security/v1"
        |]
        
        let contentHtml = 
            // Convert markdown to HTML if needed
            match contentType with
            | "posts" | "notes" -> MarkdownService.convertMdToHtml content
            | _ -> content
        
        let tagObjects = 
            if isNull tags || tags.Length = 0 then None
            else 
                tags 
                |> Array.map (fun tag -> 
                    {| 
                        ``type`` = "Hashtag"
                        href = sprintf "%s/tags/%s" baseUrl (tag.ToLowerInvariant().Replace(" ", "-"))
                        name = sprintf "#%s" tag 
                    |})
                |> Some
        
        {
            Context = activityPubContext
            Id = url
            Type = if contentType = "posts" then "Article" else "Note"
            Content = contentHtml
            ContentMap = None
            Published = date
            Updated = None
            AttributedTo = actorId
            To = [| "https://www.w3.org/ns/activitystreams#Public" |]
            Cc = Some [| sprintf "%s/followers" actorId |]
            Tag = tagObjects
            Attachment = None
            Url = url
            Sensitive = None
            Summary = if title <> "" then Some title else None
        }
    
    /// Convert to Create Activity (for outbox)
    let convertToCreateActivity (note: ActivityPubNote) : ActivityPubActivity =
        let activityId = sprintf "%s/activities/%s" baseUrl (System.Guid.NewGuid().ToString("N"))
        
        {
            Context = [| "https://www.w3.org/ns/activitystreams" |]
            Id = activityId
            Type = "Create"
            Actor = actorId
            Published = note.Published
            To = note.To
            Cc = note.Cc
            Object = note
        }
    
    /// Generate static actor document
    let generateActorDocument (publicKeyPem: string) : ActivityPubActor =
        {
            Context = [| 
                "https://www.w3.org/ns/activitystreams"
                "https://w3id.org/security/v1"
            |]
            Id = actorId
            Type = "Person"
            Name = "Luis Quintanilla"
            PreferredUsername = "lqdev"
            Summary = "Software Engineer, AI enthusiast, and content creator. Posts about F#, machine learning, and technology."
            Url = baseUrl
            Icon = {| 
                ``type`` = "Image"
                mediaType = "image/png"
                url = sprintf "%s/avatar.png" baseUrl
            |}
            Image = None
            Inbox = sprintf "%s/inbox" baseUrl
            Outbox = sprintf "%s/outbox" baseUrl
            Followers = sprintf "%s/followers" baseUrl
            Following = sprintf "%s/following" baseUrl
            PublicKey = {|
                id = sprintf "%s#main-key" actorId
                owner = actorId
                publicKeyPem = publicKeyPem
            |}
            Endpoints = None
        }
```

#### 1.3 Static Endpoint Generation

Add to `Program.fs` build pipeline:

```fsharp
// ActivityPub static endpoint generation
let buildActivityPubEndpoints (allUnifiedItems: UnifiedFeedItem list) =
    
    // Generate actor document
    let publicKey = loadOrGeneratePublicKey()  // Implement key management
    let actor = ActivityPubProcessor.generateActorDocument publicKey
    let actorJson = JsonSerializer.Serialize(actor, jsonOptions)
    File.WriteAllText(Path.Combine(outputDir, "actor", "index.json"), actorJson)
    
    // Generate webfinger
    let webfinger = {|
        subject = "acct:lqdev@luisquintanilla.me"
        links = [|
            {|
                rel = "self"
                ``type`` = "application/activity+json"
                href = "https://www.luisquintanilla.me/actor"
            |}
        |]
    |}
    let webfingerDir = Path.Combine(outputDir, ".well-known")
    Directory.CreateDirectory(webfingerDir) |> ignore
    File.WriteAllText(Path.Combine(webfingerDir, "webfinger"), JsonSerializer.Serialize(webfinger))
    
    // Generate outbox
    let recentItems = allUnifiedItems |> List.take (min 20 allUnifiedItems.Length)
    let activities = 
        recentItems
        |> List.map (fun item -> 
            let note = ActivityPubProcessor.convertToNote item.ContentType item.Title item.Content item.Date item.Url item.Tags
            ActivityPubProcessor.convertToCreateActivity note)
    
    let outbox = {|
        ``@context`` = "https://www.w3.org/ns/activitystreams"
        id = "https://www.luisquintanilla.me/outbox"
        ``type`` = "OrderedCollection"
        totalItems = activities.Length
        orderedItems = activities
    |}
    
    let outboxDir = Path.Combine(outputDir, "outbox")
    Directory.CreateDirectory(outboxDir) |> ignore
    File.WriteAllText(Path.Combine(outboxDir, "index.json"), JsonSerializer.Serialize(outbox, jsonOptions))
    
    printfn "✅ ActivityPub static endpoints generated: actor, webfinger, outbox"
```

### Phase 2: Azure Functions for Dynamic Components (Weeks 3-4)

**Objective**: Add minimal dynamic processing for federation interactions

#### 2.1 HTTP Signature Verification

Create `ActivityPubSecurity.fs`:

```fsharp
module ActivityPubSecurity

open System
open System.Security.Cryptography
open System.Text
open Microsoft.AspNetCore.Http

type HttpSignatureVerification = {
    IsValid: bool
    ActorId: string option
    Error: string option
}

let verifyHttpSignature (request: HttpRequest) : Async<HttpSignatureVerification> =
    async {
        try
            // Extract signature header
            let signatureHeader = request.Headers.["Signature"] |> Seq.tryHead
            
            match signatureHeader with
            | None -> 
                return { IsValid = false; ActorId = None; Error = Some "Missing Signature header" }
            | Some signature ->
                // Parse signature components
                let signatureParams = parseSignatureHeader signature
                
                // Fetch actor's public key
                let! actorDoc = fetchActorDocument signatureParams.KeyId
                
                // Verify signature
                let isValid = verifySignatureWithPublicKey request signatureParams actorDoc.PublicKey
                
                return { 
                    IsValid = isValid
                    ActorId = Some actorDoc.Id
                    Error = if isValid then None else Some "Signature verification failed"
                }
        with
        | ex -> 
            return { IsValid = false; ActorId = None; Error = Some ex.Message }
    }

let generateHttpSignature (privateKey: RSA) (request: HttpRequestMessage) : string =
    // Implementation for outgoing request signing
    // Used when delivering activities to follower inboxes
    ""
```

#### 2.2 Inbox Azure Function

Create `Functions/ActivityPubInbox.fs`:

```fsharp
module ActivityPubInbox

open Microsoft.Azure.Functions.Worker
open Microsoft.Azure.Functions.Worker.Http
open Microsoft.Extensions.Logging
open System.Net
open System.Text.Json
open ActivityPubSecurity

[<Function("ActivityPubInbox")>]
let inbox ([<HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "inbox")>] req: HttpRequestData) (logger: ILogger) =
    async {
        try
            // Verify HTTP signature
            let! verification = verifyHttpSignature req
            
            if not verification.IsValid then
                logger.LogWarning($"Invalid signature: {verification.Error}")
                return req.CreateResponse(HttpStatusCode.Unauthorized)
            
            // Parse activity
            let! body = req.ReadAsStringAsync() |> Async.AwaitTask
            let activity = JsonSerializer.Deserialize<ActivityPubActivity>(body)
            
            // Process activity based on type
            match activity.Type with
            | "Follow" -> 
                let! result = processFollowActivity activity verification.ActorId.Value
                return req.CreateResponse(HttpStatusCode.Accepted)
            
            | "Undo" ->
                let! result = processUndoActivity activity
                return req.CreateResponse(HttpStatusCode.Accepted)
            
            | "Create" ->
                let! result = processCreateActivity activity
                return req.CreateResponse(HttpStatusCode.Accepted)
            
            | _ ->
                logger.LogInformation($"Unhandled activity type: {activity.Type}")
                return req.CreateResponse(HttpStatusCode.Accepted)
                
        with
        | ex ->
            logger.LogError($"Error processing inbox request: {ex.Message}")
            return req.CreateResponse(HttpStatusCode.InternalServerError)
    }
    |> Async.StartAsTask

let processFollowActivity (activity: ActivityPubActivity) (actorId: string) : Async<unit> =
    async {
        // Store follower information
        let follower = {|
            Id = actorId
            FollowedAt = DateTime.UtcNow
            ActivityId = activity.Id
        |}
        
        // Save to storage (JSON file, Cosmos DB, etc.)
        do! saveFollower follower
        
        // Send Accept activity
        let acceptActivity = {|
            ``@context`` = "https://www.w3.org/ns/activitystreams"
            id = sprintf "https://www.luisquintanilla.me/activities/%s" (Guid.NewGuid().ToString("N"))
            ``type`` = "Accept"
            actor = "https://www.luisquintanilla.me/actor"
            ``object`` = activity
        |}
        
        // Deliver Accept activity to follower's inbox
        do! deliverActivity acceptActivity actorId
    }
```

#### 2.3 Activity Delivery System

Create `ActivityPubDelivery.fs`:

```fsharp
module ActivityPubDelivery

open System.Net.Http
open System.Text
open System.Text.Json

let deliverActivityToFollowers (activity: obj) (followers: string list) : Async<unit> =
    async {
        // Load private key for signing
        let privateKey = loadPrivateKey()
        
        // Deliver to each follower's inbox
        for follower in followers do
            try
                let! followerActor = fetchActorDocument follower
                let inboxUrl = followerActor.Inbox
                
                // Create HTTP request
                use client = new HttpClient()
                let json = JsonSerializer.Serialize(activity)
                let content = new StringContent(json, Encoding.UTF8, "application/activity+json")
                
                // Sign request
                let signature = generateHttpSignature privateKey content
                content.Headers.Add("Signature", signature)
                content.Headers.Add("Date", DateTime.UtcNow.ToString("R"))
                
                // Send request
                let! response = client.PostAsync(inboxUrl, content) |> Async.AwaitTask
                
                if response.IsSuccessStatusCode then
                    printfn $"✅ Delivered activity to {follower}"
                else
                    printfn $"❌ Failed to deliver to {follower}: {response.StatusCode}"
                    
            with
            | ex ->
                printfn $"❌ Error delivering to {follower}: {ex.Message}"
    }
```

### Phase 3: Content Integration (Weeks 5-6)

**Objective**: Integrate ActivityPub with existing content publishing workflow

#### 3.1 Automatic Activity Generation

Modify `Program.fs` to generate activities for new content:

```fsharp
// Enhanced build pipeline with ActivityPub integration
let buildContentWithActivityPub() =
    
    // Build all content using existing system
    let postsFeedData = buildPosts()
    let notesFeedData = buildNotes()
    let responsesFeedData = buildResponses()
    // ... other content types
    
    // Generate ActivityPub static endpoints
    buildActivityPubEndpoints allUnifiedContent
    
    // Generate individual ActivityPub objects for each content item
    allUnifiedContent
    |> List.iter (fun item ->
        let note = ActivityPubProcessor.convertToNote item.ContentType item.Title item.Content item.Date item.Url item.Tags
        let noteJson = JsonSerializer.Serialize(note, jsonOptions)
        
        // Save individual ActivityPub object
        let activityPath = sprintf "_public/activities/%s.json" (extractIdFromUrl item.Url)
        ensureDirectoryExists (Path.GetDirectoryName(activityPath))
        File.WriteAllText(activityPath, noteJson)
    )
    
    printfn "✅ ActivityPub objects generated for all content"
```

#### 3.2 Response Type Mapping

Map existing IndieWeb response types to ActivityPub:

```fsharp
let mapResponseTypeToActivityPub (responseType: string) (targetUrl: string) : string =
    match responseType.ToLowerInvariant() with
    | "star" | "like" -> "Like"
    | "reshare" | "repost" -> "Announce" 
    | "reply" -> "Note"  // Reply is a Note with inReplyTo
    | "bookmark" -> "Note"  // Bookmark is a Note with bookmark context
    | _ -> "Note"

let convertResponseToActivity (response: Response) : ActivityPubActivity =
    let activityType = mapResponseTypeToActivityPub response.Metadata.ResponseType response.Metadata.TargetUrl
    let objectId = 
        match activityType with
        | "Like" | "Announce" -> response.Metadata.TargetUrl  // Point to external content
        | _ -> sprintf "https://www.luisquintanilla.me/responses/%s" response.FileName  // Point to our Note
    
    {
        Context = [| "https://www.w3.org/ns/activitystreams" |]
        Id = sprintf "https://www.luisquintanilla.me/activities/response-%s" response.FileName
        Type = activityType
        Actor = "https://www.luisquintanilla.me/actor"
        Published = response.Metadata.DatePublished
        To = [| "https://www.w3.org/ns/activitystreams#Public" |]
        Cc = Some [| "https://www.luisquintanilla.me/followers" |]
        Object = objectId
    }
```

### Phase 4: Advanced Features (Weeks 7-8)

**Objective**: Add advanced federation features and optimization

#### 4.1 Webmention Integration

Bridge ActivityPub replies to existing webmention system:

```fsharp
module ActivityPubWebmentionBridge =
    
    let convertActivityPubReplyToWebmention (activity: ActivityPubActivity) : Webmention option =
        match activity.Type with
        | "Create" when activity.Object is ActivityPubNote ->
            let note = activity.Object :?> ActivityPubNote
            match note.InReplyTo with
            | Some targetUrl when targetUrl.StartsWith("https://www.luisquintanilla.me") ->
                Some {
                    Source = note.Id
                    Target = targetUrl
                    Type = "reply"
                    Content = note.Content
                    Author = activity.Actor
                    Published = note.Published
                }
            | _ -> None
        | _ -> None
```

#### 4.2 Collection Management

Implement followers and following collections:

```fsharp
module ActivityPubCollections =
    
    let buildFollowersCollection (followers: string list) : ActivityPubCollection =
        {
            Context = [| "https://www.w3.org/ns/activitystreams" |]
            Id = "https://www.luisquintanilla.me/followers"
            Type = "Collection"
            TotalItems = followers.Length
            First = Some "https://www.luisquintanilla.me/followers?page=1"
            Last = None
            OrderedItems = Some (followers |> List.map (fun f -> f :> obj) |> List.toArray)
        }
    
    let updateFollowersCollection() =
        async {
            let! followers = loadFollowers()
            let collection = buildFollowersCollection followers
            let json = JsonSerializer.Serialize(collection, jsonOptions)
            File.WriteAllText("_public/followers/index.json", json)
        }
```

#### 4.3 Performance Optimization

Add caching and delivery optimization:

```fsharp
module ActivityPubOptimization =
    
    // Cache actor documents to reduce HTTP requests
    let mutable actorCache = Map.empty<string, ActivityPubActor * DateTime>
    
    let fetchActorDocumentCached (actorId: string) : Async<ActivityPubActor> =
        async {
            match actorCache.TryGetValue(actorId) with
            | true, (actor, timestamp) when DateTime.UtcNow - timestamp < TimeSpan.FromHours(1) ->
                return actor
            | _ ->
                let! actor = fetchActorDocument actorId
                actorCache <- actorCache.Add(actorId, (actor, DateTime.UtcNow))
                return actor
        }
    
    // Batch activity delivery
    let batchDeliverActivities (activities: ActivityPubActivity list) : Async<unit> =
        async {
            let! followers = loadFollowers()
            
            // Group followers by domain for efficiency
            let followersByDomain = 
                followers
                |> List.groupBy (fun f -> Uri(f).Host)
            
            // Deliver to each domain in parallel
            let deliveryTasks = 
                followersByDomain
                |> List.map (fun (domain, domainFollowers) ->
                    deliverActivitiesToDomain activities domainFollowers)
            
            do! Async.Parallel deliveryTasks |> Async.Ignore
        }
```

## Architecture Integration

### Enhanced Domain.fs Structure

```fsharp
// Add ActivityPub interface to existing content types
type IActivityPubConvertible =
    abstract member ToActivityPubNote: unit -> ActivityPubNote
    abstract member ToCreateActivity: unit -> ActivityPubActivity

// Implement on existing types
type Post with
    interface IActivityPubConvertible with
        member this.ToActivityPubNote() = 
            ActivityPubProcessor.convertToNote "posts" this.Metadata.Title this.Content this.Metadata.Date 
                (sprintf "https://www.luisquintanilla.me/posts/%s" this.FileName) this.Metadata.Tags
        
        member this.ToCreateActivity() =
            ActivityPubProcessor.convertToCreateActivity (this :> IActivityPubConvertible).ToActivityPubNote()
```

### Build Pipeline Integration

```fsharp
// Add to Program.fs main function
[<EntryPoint>]
let main argv =
    // ... existing build steps ...
    
    // =============================================================================
    // ActivityPub Integration - Phase 3 Full Implementation
    // =============================================================================
    
    // Generate ActivityPub static endpoints
    buildActivityPubEndpoints allUnifiedContent
    
    // Generate individual ActivityPub objects for content discovery
    buildIndividualActivityPubObjects allUnifiedContent
    
    // Update follower collections (if followers exist)
    updateActivityPubCollections() |> Async.RunSynchronously
    
    printfn "✅ ActivityPub federation endpoints generated"
    
    // ... rest of build pipeline ...
```

## Testing Strategy

### Phase 1 Testing: Static Endpoints

1. **Webfinger Validation**:
   ```bash
   curl -H "Accept: application/jrd+json" \
        "https://www.luisquintanilla.me/.well-known/webfinger?resource=acct:lqdev@luisquintanilla.me"
   ```

2. **Actor Document Validation**:
   ```bash
   curl -H "Accept: application/activity+json" \
        "https://www.luisquintanilla.me/actor"
   ```

3. **Outbox Validation**:
   ```bash
   curl -H "Accept: application/activity+json" \
        "https://www.luisquintanilla.me/outbox"
   ```

### Phase 2 Testing: Federation

1. **Test Account Following**: Create test Mastodon account to follow your site
2. **Activity Delivery**: Verify posts appear in test account timeline
3. **Reply Processing**: Test incoming replies and mentions
4. **Signature Verification**: Validate HTTP signature implementation

### Phase 3 Testing: Content Integration

1. **New Post Federation**: Publish new content and verify federation delivery
2. **Response Activity**: Test likes, reposts, and replies from existing content
3. **Content Format**: Verify ActivityPub objects maintain content fidelity

## Deployment Strategy

### Azure Functions Configuration

```yaml
# functions/host.json
{
  "version": "2.0",
  "functionTimeout": "00:05:00",
  "extensions": {
    "http": {
      "routePrefix": "",
      "maxOutstandingRequests": 200,
      "maxConcurrentRequests": 100
    }
  }
}
```

### Static Web Apps Integration

Update `staticwebapp.config.json`:

```json
{
  "routes": [
    {
      "route": "/actor",
      "headers": {
        "Content-Type": "application/activity+json"
      }
    },
    {
      "route": "/.well-known/webfinger",
      "headers": {
        "Content-Type": "application/jrd+json"
      }
    },
    {
      "route": "/outbox",
      "headers": {
        "Content-Type": "application/activity+json"
      }
    },
    {
      "route": "/inbox",
      "methods": ["POST"],
      "rewrite": "/api/ActivityPubInbox"
    }
  ]
}
```

## Security Considerations

### Key Management

- **Private Key Storage**: Azure Key Vault for production private keys
- **Key Rotation**: Implement key rotation strategy for long-term security
- **Signature Validation**: Strict HTTP signature verification on all incoming requests

### Federation Security

- **Rate Limiting**: Implement rate limiting on inbox endpoint
- **Content Validation**: Validate all incoming ActivityPub content structure
- **Actor Verification**: Verify actor documents match claimed domains

## Success Metrics

### Phase 1 Success Criteria
- [ ] Webfinger discovery works from Mastodon search
- [ ] Actor document validates against ActivityPub specification
- [ ] Outbox provides valid ActivityStreams collection
- [ ] Site appears as followable user in federation

### Phase 2 Success Criteria
- [ ] Inbox processes follow requests successfully
- [ ] HTTP signature verification prevents spoofed requests
- [ ] Follow/unfollow workflow completes properly
- [ ] Follower count updates correctly

### Phase 3 Success Criteria
- [ ] New posts automatically federate to followers
- [ ] Response activities (likes, reposts) federate correctly
- [ ] Content maintains formatting and links in federation
- [ ] RSS feeds continue working alongside ActivityPub

### Long-term Success Metrics
- [ ] Follower count growth on federated platform
- [ ] Engagement metrics from federated interactions
- [ ] Reduced dependency on external social media platforms
- [ ] Hosting costs remain under $50/month

## Risk Mitigation

### Technical Risks

1. **HTTP Signature Complexity**: Use established libraries, extensive testing with multiple server implementations
2. **Federation Delivery Failures**: Implement robust retry logic and failure tracking
3. **Content Format Issues**: Validate ActivityPub objects against specification
4. **Performance Impact**: Monitor Azure Functions costs and execution times

### Operational Risks

1. **Key Loss**: Backup private keys securely, implement key rotation procedures
2. **Spam/Abuse**: Implement content filtering and rate limiting
3. **Federation Breaking Changes**: Monitor ActivityPub specification updates
4. **Hosting Costs**: Set up cost alerts and monitoring for Azure Functions

## Future Enhancements

### Advanced ActivityPub Features

- **Custom Emoji**: Implement custom emoji support for personality
- **Polls**: Add poll creation and federation capabilities
- **Media Attachments**: Enhanced media handling for images/videos
- **Collections**: Implement featured posts and curated collections

### IndieWeb Integration

- **Bridgy Fed**: Integration with existing IndieWeb bridges
- **Webmention Bridge**: Two-way sync between ActivityPub and webmentions
- **Micropub Support**: Allow posting via IndieWeb clients

### Analytics and Insights

- **Federation Analytics**: Track follower growth and engagement
- **Content Performance**: Analyze which content performs best in federation
- **Server Compatibility**: Track compatibility with different ActivityPub implementations

## Conclusion

This implementation plan leverages your existing F# architecture's strengths while adding minimal complexity through strategic use of static files and serverless functions. The phased approach allows for gradual deployment and testing, with each phase providing immediate value while building toward full federation capabilities.

The combination of F#'s type safety, your established content processing pipeline, and Azure's serverless platform creates an ideal foundation for efficient ActivityPub implementation that can scale to thousands of followers while maintaining operational simplicity and cost-effectiveness.

## Implementation Timeline

- **Phase 1** (Weeks 1-2): Static endpoints and basic federation presence
- **Phase 2** (Weeks 3-4): Dynamic inbox processing and follower management  
- **Phase 3** (Weeks 5-6): Content integration and automatic federation
- **Phase 4** (Weeks 7-8): Advanced features and optimization

**Total Estimated Effort**: 8 weeks for full implementation with testing and refinement.
