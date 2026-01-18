# Phase 3 Research Summary: ActivityPub Outbox Generation

**Research Date**: January 18, 2026  
**Research Tools**: Perplexity (comprehensive research) + DeepWiki (Mastodon implementation)  
**Purpose**: Validate implementation approach before Phase 3 development

---

## ✅ Key Research Findings

### 1. Static File Generation is Correct Approach ✅

**Research Validation**: Multiple production implementations (Maho.dev, Paul Kinlan) successfully use static file generation for outbox collections. **Signatures are applied at delivery time via HTTP headers, NOT embedded in JSON files.**

**Critical Insight**: "Static sites can generate and serve JSON-LD documents without running an active application server, though they typically delegate activity acceptance and signature verification to a serverless function or external inbox service."

### 2. Required vs Optional Fields (Mastodon-Verified)

**Absolutely Required for Federation**:
- `@context`: `"https://www.w3.org/ns/activitystreams"` (mandatory JSON-LD namespace)
- `type`: `"Note"` for short content, `"Article"` for blog posts
- `id`: Stable, dereferenceable HTTPS URI (must never change)
- `attributedTo`: Actor URI who created the content
- `content`: HTML-formatted content (properly escaped)
- `published`: RFC 3339 datetime format (e.g., `"2025-01-15T14:30:00Z"`)
- `to` or `cc`: Addressing properties for distribution

**Highly Recommended for Better UX**:
- `name`: Plain-text title (used by Mastodon for display)
- `url`: HTML permalink to actual blog post
- `summary`: HTML excerpt/preview
- `tag`: Array of hashtags and mentions
- `source`: Original markdown with `mediaType: "text/markdown"`

**Mastodon-Specific Requirements**:
- Content must be HTML (not plain text or markdown in `content` field)
- `to`/`cc` fields determine visibility (public collection for public posts)
- Response size limit: 1MB maximum
- Domain consistency: URI and actor URI must share same domain

### 3. Collection Structure (Specification-Backed)

**Outbox MUST be OrderedCollection** (not optional):
- Type: `"OrderedCollection"` (specification requirement)
- Ordering: Reverse chronological (most recent first)
- Items: Array of Create activities wrapping Note objects

**Pagination Structure**:
```json
{
  "@context": "https://www.w3.org/ns/activitystreams",
  "type": "OrderedCollection",
  "id": "https://lqdev.me/api/outbox",
  "totalItems": 42,
  "first": "https://lqdev.me/api/outbox?page=1",
  "orderedItems": []  // Or reference to first page
}
```

### 4. Create Activity Structure

**Required Fields**:
- `type`: `"Create"`
- `id`: Unique URI for this activity (distinct from Note ID)
- `actor`: Actor URI (must match Note's `attributedTo`)
- `object`: Full Note object or URI reference
- `published`: Same as Note's published date
- `to`/`cc`: Should match Note's addressing

**Best Practice**: Embed full Note object inline rather than just URI reference for better compatibility.

### 5. Stable ID Generation Pattern

**Critical Rule**: IDs must be:
- **Stable**: Never change even when content updates
- **Dereferenceable**: Return JSON when fetched with `Accept: application/activity+json`
- **Globally Unique**: UUID or content-based hash recommended
- **Domain Consistent**: Share domain with actor URI

**Recommended Pattern for F#**:
```fsharp
// Option 1: Content-based hash (from prototype)
let generateNoteId (url: string) (content: string) =
    let hash = generateHash content  // MD5 or SHA256
    sprintf "https://lqdev.me/api/notes/%s" hash

// Option 2: URL-based (simpler, more readable)
let generateNoteId (contentUrl: string) =
    let slug = extractSlugFromUrl contentUrl
    sprintf "https://lqdev.me/api/notes/%s" slug
```

### 6. Content Formatting Rules

**HTML is Default Format**:
- Content field expects properly escaped HTML
- Special characters: `<` → `&lt;`, `>` → `&gt;`, `&` → `&amp;`
- Most implementations strip `<script>`, `<form>`, inline styles
- Images can be in HTML or separate `attachment` field

**Markdown Support**:
```json
"source": {
  "content": "# Title\n\nMarkdown content",
  "mediaType": "text/markdown"
},
"content": "<h1>Title</h1>\n<p>Markdown content</p>"
```

### 7. Addressing for Public Posts

**Public Blog Posts Pattern**:
```json
"to": ["https://www.w3.org/ns/activitystreams#Public"],
"cc": ["https://lqdev.me/api/followers"]
```

**Followers-Only Pattern**:
```json
"to": ["https://lqdev.me/api/followers"],
"cc": []
```

---

## 🎯 Validated Implementation Architecture

### Phase 3a: Domain Types (ActivityPubBuilder.fs)

**Create F# records matching ActivityPub spec**:

```fsharp
module ActivityPubBuilder

open System
open System.Text.Json.Serialization

[<CLIMutable>]
type ActivityPubNote = {
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
    
    [<JsonPropertyName("name")>]
    Name: string option
    
    [<JsonPropertyName("url")>]
    Url: string option
    
    [<JsonPropertyName("summary")>]
    Summary: string option
    
    [<JsonPropertyName("to")>]
    To: string array
    
    [<JsonPropertyName("cc")>]
    Cc: string array option
    
    [<JsonPropertyName("tag")>]
    Tag: obj array option  // Hashtags and mentions
    
    [<JsonPropertyName("source")>]
    Source: obj option  // Markdown source
}

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

[<CLIMutable>]
type ActivityPubOutbox = {
    [<JsonPropertyName("@context")>]
    Context: string
    
    [<JsonPropertyName("id")>]
    Id: string
    
    [<JsonPropertyName("type")>]
    Type: string  // "OrderedCollection"
    
    [<JsonPropertyName("summary")>]
    Summary: string
    
    [<JsonPropertyName("totalItems")>]
    TotalItems: int
    
    [<JsonPropertyName("orderedItems")>]
    OrderedItems: ActivityPubCreate array
}
```

### Phase 3b: Conversion Functions

**Convert UnifiedFeedItem → ActivityPubNote**:
```fsharp
let convertToNote (item: UnifiedFeedItem) : ActivityPubNote =
    {
        Context = "https://www.w3.org/ns/activitystreams"
        Id = generateNoteId item.Url item.Content
        Type = if item.ContentType = "posts" then "Article" else "Note"
        AttributedTo = "https://lqdev.me/api/actor"
        Published = item.Date  // Already in RFC 3339 format
        Content = item.Content  // HTML from existing processing
        Name = if String.IsNullOrWhiteSpace(item.Title) then None else Some item.Title
        Url = Some item.Url
        Summary = None  // Can extract from Content if needed
        To = [| "https://www.w3.org/ns/activitystreams#Public" |]
        Cc = Some [| "https://lqdev.me/api/followers" |]
        Tag = convertTagsToHashtags item.Tags
        Source = None  // Can add markdown support later
    }

let convertToCreateActivity (note: ActivityPubNote) : ActivityPubCreate =
    {
        Context = "https://www.w3.org/ns/activitystreams"
        Id = note.Id + "#create"  // Fragment ID pattern
        Type = "Create"
        Actor = "https://lqdev.me/api/actor"
        Published = note.Published
        To = note.To
        Cc = note.Cc
        Object = note
    }
```

### Phase 3c: Collection Generation

**Generate OrderedCollection outbox**:
```fsharp
let generateOutbox (activities: ActivityPubCreate list) : ActivityPubOutbox =
    {
        Context = "https://www.w3.org/ns/activitystreams"
        Id = "https://lqdev.me/api/outbox"
        Type = "OrderedCollection"
        Summary = "All content updates from Luis Quintanilla's website"
        TotalItems = activities.Length
        OrderedItems = activities |> List.toArray
    }

let buildOutbox (unifiedItems: UnifiedFeedItem list) (outputDir: string) : unit =
    // Convert all items to Create activities
    let activities = 
        unifiedItems
        |> List.sortByDescending (fun item -> item.Date)
        |> List.map (convertToNote >> convertToCreateActivity)
    
    // Generate outbox collection
    let outbox = generateOutbox activities
    
    // Serialize and save
    let json = JsonSerializer.Serialize(outbox, jsonOptions)
    let outboxPath = Path.Combine(outputDir, "api", "data", "outbox", "index.json")
    Directory.CreateDirectory(Path.GetDirectoryName(outboxPath)) |> ignore
    File.WriteAllText(outboxPath, json)
```

### Phase 3d: Integration Points

**Add to Program.fs after RSS feed generation**:
```fsharp
// After line 137 (buildTagFeeds)
printfn "🎭 Building ActivityPub outbox..."
ActivityPubBuilder.buildOutbox allUnifiedItems "_public"
printfn "✅ ActivityPub outbox complete"
```

---

## ✅ Research-Validated Decisions

1. **✅ Use OrderedCollection** - Specification requires it for outboxes
2. **✅ Embed full Note objects** - Better compatibility than URI references
3. **✅ HTML content format** - Default expectation across all implementations
4. **✅ RFC 3339 timestamps** - Already implemented in our timezone pattern
5. **✅ Content-based or URL-based IDs** - Both patterns work, choose based on preference
6. **✅ Public addressing pattern** - Use `Public` in `to`, followers in `cc`
7. **✅ Static JSON generation** - Proven pattern, signing happens at delivery

---

## 🚀 Ready to Proceed

All architectural decisions validated against:
- ✅ W3C ActivityPub specification
- ✅ Production implementations (Maho.dev, Paul Kinlan)
- ✅ Mastodon parsing requirements
- ✅ Your existing F# architecture patterns

**Next Step**: Create ActivityPubBuilder.fs module with Phase 3a domain types.

**Confidence Level**: HIGH - Research confirms our planned approach aligns perfectly with specification requirements and production patterns.
