/// RSS to ActivityPub Outbox Generator
/// Based on the ActivityPub implementation guide by Maho Pacheco
/// Converts RSS feeds to ActivityPub outbox and notes for static site integration

#r "nuget: System.Text.Json"

open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open System.Xml.Linq
open System.Collections.Generic
open System.Security.Cryptography
open System.Text

/// ActivityPub JSON naming policy to handle @context and camelCase
type ActivityPubNamingPolicy() =
    inherit JsonNamingPolicy()
    
    override _.ConvertName(name: string) =
        match name with
        | "Context" -> "@context"
        | _ when name.Length > 0 && Char.IsUpper(name.[0]) ->
            Char.ToLowerInvariant(name.[0]).ToString() + name.Substring(1)
        | _ -> name

/// ActivityPub tag representation
type ActivityPubTag = {
    Type: string
    Href: string
    Name: string
}

/// ActivityPub note representation
type ActivityPubNote = {
    Context: string
    Id: string
    Type: string
    Content: string
    Url: string
    AttributedTo: string
    To: string array
    Cc: string array
    Published: string
    Tag: ActivityPubTag array
    Hash: string  // For filename generation
}

/// ActivityPub Create activity
type ActivityPubCreate = {
    Context: string
    Id: string
    Type: string
    Actor: string
    Published: string
    To: string array
    Cc: string array
    Object: ActivityPubNote
}

/// ActivityPub outbox collection
type ActivityPubOutbox = {
    Context: string
    Id: string
    Type: string
    Summary: string
    TotalItems: int
    OrderedItems: ActivityPubCreate array
}

/// Configuration for outbox generation
type OutboxConfig = {
    RssPath: string
    StaticPath: string
    AuthorUsername: string
    SiteActorUri: string
    Domain: string option
    AuthorUri: string option
    ContentTemplate: string option
    NotesPath: string
    OutboxPath: string
}

/// Generate MD5 hash for unique note identification
let generateHash (content: string) : string =
    use md5 = MD5.Create()
    let hash = md5.ComputeHash(Encoding.UTF8.GetBytes(content))
    Convert.ToHexString(hash).ToLowerInvariant()

/// Extract author URI from username (@user@domain format)
let extractAuthorUri (authorUsername: string) =
    let parts = authorUsername.Split('@', StringSplitOptions.RemoveEmptyEntries)
    if parts.Length >= 2 then
        let username = parts.[0]
        let domain = parts.[1]
        sprintf "https://%s/users/%s" domain username
    else
        "https://example.com/users/unknown"

/// Convert RSS categories to ActivityPub hashtags and mentions
let convertTagsToActivityPub (tags: string array) (domain: string) (authorUsername: string) =
    let hashtags = 
        tags
        |> Array.map (fun tag -> {
            Type = "Hashtag"
            Href = sprintf "%s/tags/%s" domain (tag.Replace("#", "").ToLowerInvariant())
            Name = sprintf "#%s" (tag.Replace("#", ""))
        })
    
    // Add author mention
    let authorMention = {
        Type = "Mention"
        Href = extractAuthorUri authorUsername
        Name = authorUsername
    }
    
    Array.append [| authorMention |] hashtags

/// Parse RSS XML and extract items
let parseRssItems (rssPath: string) =
    let rssContent = File.ReadAllText(rssPath)
    let rssXml = XDocument.Parse(rssContent)
    let dcNamespace = XNamespace.Get("http://purl.org/dc/elements/1.1/")
    let contentNamespace = XNamespace.Get("http://purl.org/rss/1.0/modules/content/")
    
    // Extract channel description for outbox summary
    let summary = 
        rssXml.Descendants("channel")
        |> Seq.tryPick (fun channel -> 
            match channel.Element("description") with
            | null -> None
            | elem -> Some elem.Value)
        |> Option.defaultValue "Outbox for blog posts"
    
    // Extract RSS items
    let items = 
        rssXml.Descendants("item")
        |> Seq.map (fun item -> {|
            Title = 
                match item.Element("title") with
                | null -> 
                    match item.Element("description") with
                    | null -> "Untitled"
                    | desc -> desc.Value
                | title -> title.Value
            Link = 
                match item.Element("link") with
                | null -> ""
                | link -> link.Value
            Description = 
                match item.Element("description") with
                | null -> ""
                | desc -> desc.Value
            Content = 
                match item.Element(contentNamespace + "encoded") with
                | null -> 
                    match item.Element("description") with
                    | null -> ""
                    | desc -> desc.Value
                | content -> content.Value
            PubDate = 
                match item.Element("pubDate") with
                | null -> DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz")
                | date -> date.Value
            Author = 
                match item.Element(dcNamespace + "creator") with
                | null -> ""
                | author -> author.Value
            Tags = 
                item.Elements("category")
                |> Seq.map (fun cat -> cat.Value)
                |> Seq.toArray
        |})
        |> Seq.toArray
    
    (summary, items)

/// Parse date to ISO 8601 format
let parseDate (dateStr: string) =
    try
        // Try parsing common RSS date formats
        match DateTime.TryParse(dateStr) with
        | (true, date) -> date.ToString("yyyy-MM-ddTHH:mm:sszzz")
        | (false, _) ->
            // Fallback to current time
            DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz")
    with
    | _ -> DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz")

/// Generate ActivityPub note from RSS item
let generateNote (item: {| Title: string; Link: string; Description: string; Content: string; PubDate: string; Author: string; Tags: string array |}) 
                 (config: OutboxConfig) =
    let domain = config.Domain |> Option.defaultValue "https://example.com"
    
    // Generate content using template or default format
    let contentTemplate = 
        config.ContentTemplate 
        |> Option.defaultValue "{title}\n\n{tags}\n\n🔗 {link}"
    
    let tagsStr = 
        if item.Tags.Length > 0 then
            item.Tags 
            |> Array.map (fun tag -> sprintf "#%s" tag)
            |> String.concat " "
        else ""
    
    // For social media, just use the title and link - keep it simple
    let content = 
        contentTemplate
            .Replace("{title}", item.Title)
            .Replace("{content}", item.Content)
            .Replace("{description}", item.Description)
            .Replace("{link}", item.Link)
            .Replace("{author}", item.Author)
            .Replace("{tags}", tagsStr)
    
    // Generate unique hash for note ID
    let noteHash = generateHash (item.Title + item.Link + item.PubDate)
    let noteId = sprintf "%s/%s/%s" domain config.NotesPath noteHash
    
    // Convert tags to ActivityPub format
    let activityPubTags = convertTagsToActivityPub item.Tags domain config.AuthorUsername
    
    let note = {
        Context = "https://www.w3.org/ns/activitystreams"
        Id = noteId
        Type = "Note"
        Content = content
        Url = item.Link
        AttributedTo = config.SiteActorUri
        To = [| "https://www.w3.org/ns/activitystreams#Public" |]
        Cc = [||]
        Published = parseDate item.PubDate
        Tag = activityPubTags
        Hash = noteHash
    }
    
    note

/// Generate ActivityPub Create activity for a note
let generateCreateActivity (note: ActivityPubNote) (config: OutboxConfig) =
    let domain = config.Domain |> Option.defaultValue "https://example.com"
    let createId = sprintf "%s/%s/activities/%s" domain config.OutboxPath note.Hash
    
    {
        Context = "https://www.w3.org/ns/activitystreams"
        Id = createId
        Type = "Create"
        Actor = config.SiteActorUri
        Published = note.Published
        To = note.To
        Cc = note.Cc
        Object = note
    }

/// Generate ActivityPub outbox collection
let generateOutbox (createActivities: ActivityPubCreate array) (summary: string) (config: OutboxConfig) =
    let domain = config.Domain |> Option.defaultValue "https://example.com"
    let outboxUrl = sprintf "%s/%s" domain config.OutboxPath
    
    {
        Context = "https://www.w3.org/ns/activitystreams"
        Id = outboxUrl
        Type = "OrderedCollection"
        Summary = summary
        TotalItems = createActivities.Length
        OrderedItems = createActivities
    }

/// Save ActivityPub objects to files
let saveActivityPubFiles (notes: ActivityPubNote array) (outbox: ActivityPubOutbox) (config: OutboxConfig) =
    let staticPath = config.StaticPath
    let notesDir = Path.Combine(staticPath, config.NotesPath)
    let outboxDir = Path.Combine(staticPath, config.OutboxPath)
    
    // Create directories if they don't exist
    Directory.CreateDirectory(notesDir) |> ignore
    Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(staticPath, config.OutboxPath))) |> ignore
    
    // JSON serialization options
    let jsonOptions = JsonSerializerOptions()
    jsonOptions.PropertyNamingPolicy <- ActivityPubNamingPolicy()
    jsonOptions.WriteIndented <- true
    
    // Save individual notes
    for note in notes do
        let noteJson = JsonSerializer.Serialize(note, jsonOptions)
        let noteFile = Path.Combine(notesDir, note.Hash)
        File.WriteAllText(noteFile, noteJson)
    
    // Save outbox
    let outboxJson = JsonSerializer.Serialize(outbox, jsonOptions)
    let outboxFile = Path.Combine(staticPath, config.OutboxPath, "index.json")
    Directory.CreateDirectory(Path.Combine(staticPath, config.OutboxPath)) |> ignore
    File.WriteAllText(outboxFile, outboxJson)
    
    printfn "✅ Generated ActivityPub outbox with %d notes" notes.Length
    printfn "   📁 Notes: %s" notesDir
    printfn "   📁 Outbox: %s" outboxFile

/// Main conversion function
let convertRssToActivityPub (config: OutboxConfig) =
    try
        printfn "🔄 Converting RSS to ActivityPub..."
        printfn "   📄 RSS Feed: %s" config.RssPath
        printfn "   📁 Output: %s" config.StaticPath
        printfn "   👤 Author: %s" config.AuthorUsername
        printfn "   🌐 Actor: %s" config.SiteActorUri
        
        // Parse RSS feed
        let (summary, rssItems) = parseRssItems config.RssPath
        printfn "   📊 Found %d RSS items" rssItems.Length
        
        // Convert RSS items to ActivityPub notes
        let notes = 
            rssItems
            |> Array.map (fun item -> generateNote item config)
        
        // Generate Create activities
        let createActivities = 
            notes
            |> Array.map (fun note -> generateCreateActivity note config)
        
        // Generate outbox
        let outbox = generateOutbox createActivities summary config
        
        // Save all files
        saveActivityPubFiles notes outbox config
        
        printfn "✅ RSS to ActivityPub conversion completed successfully!"
        
    with
    | ex -> 
        printfn "❌ Error during conversion: %s" ex.Message
        printfn "   Stack trace: %s" ex.StackTrace

/// Parse command line arguments
let parseArgs (args: string array) =
    let mutable rssPath = ""
    let mutable staticPath = ""
    let mutable authorUsername = "@lqdev@lqdev.me"
    let mutable siteActorUri = "https://lqdev.me/@lqdev"
    let mutable domain = "https://lqdev.me"
    let mutable authorUri = "https://lqdev.me/"
    let mutable contentTemplate = "{title}\n\n{tags}\n\n🔗 {link}"
    let mutable notesPath = "social/notes"
    let mutable outboxPath = "social/outbox"
    
    let rec parseLoop (argList: string list) =
        match argList with
        | "--rss-path" :: value :: rest ->
            rssPath <- value
            parseLoop rest
        | "--static-path" :: value :: rest ->
            staticPath <- value
            parseLoop rest
        | "--author-username" :: value :: rest ->
            authorUsername <- value
            parseLoop rest
        | "--site-actor-uri" :: value :: rest ->
            siteActorUri <- value
            parseLoop rest
        | "--domain" :: value :: rest ->
            domain <- value
            parseLoop rest
        | "--author-uri" :: value :: rest ->
            authorUri <- value
            parseLoop rest
        | "--content-template" :: value :: rest ->
            contentTemplate <- value
            parseLoop rest
        | "--notes-path" :: value :: rest ->
            notesPath <- value
            parseLoop rest
        | "--outbox-path" :: value :: rest ->
            outboxPath <- value
            parseLoop rest
        | "--help" :: _ | "-h" :: _ ->
            printfn "RSS to ActivityPub Converter"
            printfn ""
            printfn "Usage: dotnet fsi rss-to-activitypub.fsx [options]"
            printfn ""
            printfn "Options:"
            printfn "  --rss-path <path>         Path to RSS feed XML file"
            printfn "  --static-path <path>      Path to static site output directory"
            printfn "  --author-username <user>  Author username (e.g., @user@domain.com)"
            printfn "  --site-actor-uri <uri>    Site actor URI"
            printfn "  --domain <domain>         Site domain (e.g., https://example.com)"
            printfn "  --author-uri <uri>        Author profile URI"
            printfn "  --content-template <tmpl> Content template (default: {title}\\n\\n{tags}\\n\\n🔗 {link})"
            printfn "  --notes-path <path>       Relative path for notes (default: socialweb/notes)"
            printfn "  --outbox-path <path>      Relative path for outbox (default: socialweb/outbox)"
            printfn "  --help, -h                Show this help message"
            printfn ""
            printfn "Example:"
            printfn "  dotnet fsi rss-to-activitypub.fsx --rss-path ./_public/feed/feed.xml --static-path ./_public"
            exit 0
        | [] -> ()
        | unknown :: rest ->
            printfn "Unknown argument: %s" unknown
            parseLoop rest
    
    parseLoop (Array.toList args)
    
    // Use auto-discovery for paths if not provided
    let baseDir = 
        if Directory.Exists("_public") then "."
        elif Directory.Exists(Path.Combine("..", "_public")) then ".."
        else Environment.CurrentDirectory
    
    let finalRssPath = 
        if String.IsNullOrEmpty(rssPath) then
            Path.Combine(baseDir, "_public", "feed", "feed.xml")
        else rssPath
    
    let finalStaticPath = 
        if String.IsNullOrEmpty(staticPath) then
            Path.Combine(baseDir, "_public")
        else staticPath
    
    {
        RssPath = finalRssPath
        StaticPath = finalStaticPath
        AuthorUsername = authorUsername
        SiteActorUri = siteActorUri
        Domain = Some domain
        AuthorUri = Some authorUri
        ContentTemplate = Some contentTemplate
        NotesPath = notesPath
        OutboxPath = outboxPath
    }

/// Create configuration from environment variables or defaults
let createConfigFromEnvironment () =
    // Use relative paths from the repository root for CI/CD compatibility
    let baseDir = 
        if Directory.Exists("_public") then "."
        elif Directory.Exists(Path.Combine("..", "_public")) then ".."
        else Environment.CurrentDirectory
    
    {
        RssPath = 
            Environment.GetEnvironmentVariable("RSS_PATH") 
            |> Option.ofObj 
            |> Option.defaultValue (Path.Combine(baseDir, "_public", "feed", "feed.xml"))
        StaticPath = 
            Environment.GetEnvironmentVariable("STATIC_PATH") 
            |> Option.ofObj 
            |> Option.defaultValue (Path.Combine(baseDir, "_public"))
        AuthorUsername = 
            Environment.GetEnvironmentVariable("AUTHOR_USERNAME") 
            |> Option.ofObj 
            |> Option.defaultValue "@lqdev@lqdev.me"
        SiteActorUri = 
            Environment.GetEnvironmentVariable("SITE_ACTOR_URI") 
            |> Option.ofObj 
            |> Option.defaultValue "https://www.lqdev.me/@lqdev"
        Domain = 
            Environment.GetEnvironmentVariable("DOMAIN") 
            |> Option.ofObj
        AuthorUri = 
            Environment.GetEnvironmentVariable("AUTHOR_URI") 
            |> Option.ofObj
        ContentTemplate = 
            Environment.GetEnvironmentVariable("CONTENT_TEMPLATE") 
            |> Option.ofObj 
            |> Option.orElse (Some "{title}\n\n{tags}\n\n🔗 {link}")
        NotesPath = 
            Environment.GetEnvironmentVariable("NOTES_PATH") 
            |> Option.ofObj 
            |> Option.defaultValue "socialweb/notes"
        OutboxPath = 
            Environment.GetEnvironmentVariable("OUTBOX_PATH") 
            |> Option.ofObj 
            |> Option.defaultValue "socialweb/outbox"
    }

/// Example usage and testing
let runExample () =
    // Use relative paths from the repository root for CI/CD compatibility
    let baseDir = 
        if Directory.Exists("_public") then "."
        elif Directory.Exists(Path.Combine("..", "_public")) then ".."
        else Environment.CurrentDirectory
    
    let exampleConfig = {
        RssPath = Path.Combine(baseDir, "_public", "feed", "feed.xml")
        StaticPath = Path.Combine(baseDir, "_public")
        AuthorUsername = "@lqdev@lqdev.me"  // Update with your actual Mastodon handle
        SiteActorUri = "https://lqdev.me/@lqdev"
        Domain = Some "https://lqdev.me"
        AuthorUri = Some "https://lqdev.me/"  // Update with your actual profile
        ContentTemplate = Some "{title}\n\n{tags}\n\n🔗 {link}"
        NotesPath = "socialweb/notes"
        OutboxPath = "socialweb/outbox"
    }
    
    convertRssToActivityPub exampleConfig

/// Run with environment variable configuration (ideal for CI/CD)
let runWithEnvironmentConfig () =
    let config = createConfigFromEnvironment ()
    convertRssToActivityPub config

/// Quick test function to validate the implementation
let testConversion () =
    printfn "🧪 Testing RSS to ActivityPub conversion..."
    
    // Check if RSS feed exists using relative path discovery
    let baseDir = 
        if Directory.Exists("_public") then "."
        elif Directory.Exists(Path.Combine("..", "_public")) then ".."
        else Environment.CurrentDirectory
    
    let rssPath = Path.Combine(baseDir, "_public", "feed", "feed.xml")
    if File.Exists(rssPath) then
        printfn "✅ RSS feed found: %s" (Path.GetFullPath(rssPath))
        
        // Run example conversion
        runExample ()
    else
        printfn "❌ RSS feed not found at: %s" (Path.GetFullPath(rssPath))
        printfn "   Please build your site first to generate the RSS feed"
        printfn "   Current directory: %s" Environment.CurrentDirectory
        printfn "   Looking for _public directory relative to current location"

// Check command line arguments
let args = Environment.GetCommandLineArgs()
let scriptArgs = 
    if args.Length > 2 then
        args.[2..] // Skip "dotnet" and "fsi" args
    else
        [||]

if scriptArgs.Length > 0 then
    printfn "🚀 Running with command line arguments..."
    let config = parseArgs scriptArgs
    convertRssToActivityPub config
else
    // Run test with example configuration when no args provided
    testConversion ()