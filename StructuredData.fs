module StructuredData

// =============================================================================
// schema.org JSON-LD structured data, driven entirely by `Constants` (single
// source of truth for site identity).
//
// The centerpiece is a shared `@graph` containing two stable-identity nodes that
// every page embeds:
//   - WebSite  @id = {canonical}/#website
//   - Person   @id = {canonical}/#person
//
// Per-page nodes (BlogPosting, ProfilePage, BreadcrumbList, ...) reference these
// by `@id` (e.g. {"@id":"https://lqdev.me/#person"}) so crawlers/LLMs merge the
// identity across the whole site. See docs and plan for the full design.
// =============================================================================

open System.Text.Json
open System.Text.Json.Nodes

/// Canonical origin with no trailing slash, e.g. "https://lqdev.me".
let canonical = Constants.Urls.canonical

/// Stable node identifiers for the shared identity graph.
let websiteId = canonical + "/#website"
let personId = canonical + "/#person"

/// A slim `{"@id": "..."}` reference node, for linking to shared identities.
let idRef (id: string) : JsonNode =
    let o = JsonObject()
    o.Add("@id", JsonValue.Create(id))
    o :> JsonNode

let private imageObject (idSuffix: string) (url: string) : JsonNode =
    let o = JsonObject()
    o.Add("@type", JsonValue.Create("ImageObject"))
    o.Add("@id", JsonValue.Create(canonical + idSuffix))
    o.Add("url", JsonValue.Create(url))
    o :> JsonNode

/// The canonical `Person` node for the site owner.
let personNode () : JsonNode =
    let o = JsonObject()
    o.Add("@type", JsonValue.Create("Person"))
    o.Add("@id", JsonValue.Create(personId))
    o.Add("url", JsonValue.Create(canonical + "/"))
    o.Add("name", JsonValue.Create(Constants.Author.name))
    o.Add("givenName", JsonValue.Create("Luis"))
    o.Add("familyName", JsonValue.Create("Quintanilla"))
    o.Add("description", JsonValue.Create(Constants.Author.bio))
    o.Add("jobTitle", JsonValue.Create(Constants.Author.jobTitle))
    o.Add("image", imageObject "/#person-image" Constants.Avatar.displayUrl)
    let sameAs = JsonArray()
    for link in Constants.Author.sameAs do
        sameAs.Add(JsonValue.Create(link))
    o.Add("sameAs", sameAs)
    o :> JsonNode

/// The canonical `WebSite` node.
let websiteNode () : JsonNode =
    let o = JsonObject()
    o.Add("@type", JsonValue.Create("WebSite"))
    o.Add("@id", JsonValue.Create(websiteId))
    o.Add("url", JsonValue.Create(canonical + "/"))
    o.Add("name", JsonValue.Create(Constants.Author.name))
    let alt = JsonArray()
    alt.Add(JsonValue.Create("lqdev.me"))
    alt.Add(JsonValue.Create(Constants.Site.title))
    o.Add("alternateName", alt)
    o.Add("description", JsonValue.Create(Constants.Pwa.description))
    o.Add("inLanguage", JsonValue.Create("en"))
    o.Add("publisher", idRef personId)
    o.Add("image", imageObject "/#website-image" Constants.Avatar.displayUrl)
    o :> JsonNode

/// Serialize a set of nodes as a `@graph` JSON-LD document string (compact).
let graphJson (nodes: JsonNode list) : string =
    let root = JsonObject()
    root.Add("@context", JsonValue.Create("https://schema.org"))
    let graph = JsonArray()
    for n in nodes do
        graph.Add(n)
    root.Add("@graph", graph)
    root.ToJsonString(JsonSerializerOptions(WriteIndented = false))

/// The site-wide identity graph (WebSite + Person) embedded on every page.
let siteIdentityGraphJson () : string =
    graphJson [ websiteNode (); personNode () ]

/// A `{"@type":"WebPage","url":...}` reference node for linking to an external
/// target (used by IndieWeb responses: inReplyTo / sharedContent / citation).
let webPageRef (url: string) : JsonNode =
    let o = JsonObject()
    o.Add("@type", JsonValue.Create("WebPage"))
    o.Add("url", JsonValue.Create(url))
    o :> JsonNode

/// A generic CreativeWork node object (NO `@context` — suitable for embedding in
/// a `@graph`) for an individual content item, linked to the shared identity
/// nodes by `@id`. `schemaType` is the schema.org type (e.g. "BlogPosting",
/// "SocialMediaPosting", "SoftwareSourceCode", "Article"). `pageUrl` is a
/// site-root-relative path (e.g. "/posts/x/"). Dates are ISO-8601 (yyyy-MM-dd);
/// pass "" to omit. `extra` adds type-specific top-level props.
let contentNodeObj
    (schemaType: string)
    (pageUrl: string)
    (title: string)
    (datePublished: string)
    (dateModified: string)
    (tags: string seq)
    (extra: (string * JsonNode) list)
    : JsonObject =
    let absUrl = canonical + pageUrl
    let o = JsonObject()
    o.Add("@type", JsonValue.Create(schemaType))
    o.Add("@id", JsonValue.Create(absUrl + "#" + schemaType.ToLowerInvariant()))
    o.Add("url", JsonValue.Create(absUrl))
    o.Add("headline", JsonValue.Create(title))
    o.Add("name", JsonValue.Create(title))
    o.Add("mainEntityOfPage", idRef absUrl)
    o.Add("author", idRef personId)
    o.Add("publisher", idRef personId)
    o.Add("isPartOf", idRef websiteId)
    if not (System.String.IsNullOrWhiteSpace datePublished) then
        o.Add("datePublished", JsonValue.Create(datePublished))
    let dm = if System.String.IsNullOrWhiteSpace dateModified then datePublished else dateModified
    if not (System.String.IsNullOrWhiteSpace dm) then
        o.Add("dateModified", JsonValue.Create(dm))
    let tagList =
        if isNull (box tags) then []
        else tags |> Seq.filter (System.String.IsNullOrWhiteSpace >> not) |> Seq.toList
    if not tagList.IsEmpty then
        o.Add("keywords", JsonValue.Create(System.String.Join(",", tagList)))
    for (k, v) in extra do
        o.Add(k, v)
    o.Add("image", imageObject "/#website-image" Constants.Avatar.displayUrl)
    o.Add("inLanguage", JsonValue.Create("en"))
    o

/// Standalone (single-object, self-contained) JSON-LD string for one content
/// node. Prefer `contentPageJson` for individual pages (it adds a breadcrumb).
let contentNodeJson
    (schemaType: string)
    (pageUrl: string)
    (title: string)
    (datePublished: string)
    (dateModified: string)
    (tags: string seq)
    (extra: (string * JsonNode) list)
    : string =
    let o = contentNodeObj schemaType pageUrl title datePublished dateModified tags extra
    o.Add("@context", JsonValue.Create("https://schema.org"))
    o.ToJsonString(JsonSerializerOptions(WriteIndented = false))

/// A `BreadcrumbList` node (NO `@context`). `crumbs` is an ordered list of
/// (name, url) pairs from the site root to the current page; root-relative URLs
/// are made absolute against the canonical origin.
let breadcrumbNode (crumbs: (string * string) list) : JsonNode =
    let o = JsonObject()
    o.Add("@type", JsonValue.Create("BreadcrumbList"))
    let items = JsonArray()
    crumbs
    |> List.iteri (fun i (name, url) ->
        let li = JsonObject()
        li.Add("@type", JsonValue.Create("ListItem"))
        li.Add("position", JsonValue.Create(i + 1))
        li.Add("name", JsonValue.Create(name))
        let absU = if url.StartsWith("http") then url else canonical + url
        li.Add("item", JsonValue.Create(absU))
        items.Add(li))
    o.Add("itemListElement", items)
    o :> JsonNode

/// The response-type -> semantic-link mapping for IndieWeb responses.
/// reply/rsvp -> inReplyTo, reshare/share -> sharedContent, else -> citation.
let private responseExtra (responseType: string) (targetUrl: string) : (string * JsonNode) list =
    if System.String.IsNullOrWhiteSpace targetUrl then []
    else
        let rel =
            match responseType with
            | "reply" | "rsvp" -> "inReplyTo"
            | "reshare" | "share" -> "sharedContent"
            | _ -> "citation"   // star (like), bookmark, other
        [ (rel, webPageRef targetUrl) ]

/// A page-level `@graph` for an individual content item: the content node plus a
/// `BreadcrumbList` (Home -> section -> current page). `sectionName`/`sectionUrl`
/// name the content-type landing page (e.g. "Posts", "/posts/").
let contentPageJson
    (schemaType: string)
    (sectionName: string)
    (sectionUrl: string)
    (pageUrl: string)
    (title: string)
    (datePublished: string)
    (dateModified: string)
    (tags: string seq)
    (extra: (string * JsonNode) list)
    : string =
    let node = contentNodeObj schemaType pageUrl title datePublished dateModified tags extra :> JsonNode
    let bc = breadcrumbNode [ ("Home", "/"); (sectionName, sectionUrl); (title, pageUrl) ]
    graphJson [ node; bc ]

/// Page-level `@graph` for a `BlogPosting` (post) individual page.
let blogPostingJson pageUrl title datePublished dateModified tags : string =
    contentPageJson "BlogPosting" "Posts" "/posts/" pageUrl title datePublished dateModified tags []

/// Page-level `@graph` for an IndieWeb response individual page: a
/// `SocialMediaPosting` (with inReplyTo/sharedContent/citation by type) plus a
/// breadcrumb. `sectionName`/`sectionUrl` differ for responses vs bookmarks.
let responsePageJson
    (sectionName: string)
    (sectionUrl: string)
    (pageUrl: string)
    (title: string)
    (datePublished: string)
    (tags: string seq)
    (responseType: string)
    (targetUrl: string)
    : string =
    contentPageJson "SocialMediaPosting" sectionName sectionUrl pageUrl title datePublished "" tags (responseExtra responseType targetUrl)

/// Standalone `SocialMediaPosting` JSON-LD (no breadcrumb) — retained for callers
/// that only want the content node.
let responsePostingJson
    (pageUrl: string)
    (title: string)
    (datePublished: string)
    (tags: string seq)
    (responseType: string)
    (targetUrl: string)
    : string =
    contentNodeJson "SocialMediaPosting" pageUrl title datePublished "" tags (responseExtra responseType targetUrl)

/// A `ProfilePage` `@graph` for `/about`: a ProfilePage whose `mainEntity`/`about`
/// is the shared `#person`, plus a Home -> About breadcrumb.
let profilePageJson (pageUrl: string) : string =
    let absUrl = canonical + pageUrl
    let o = JsonObject()
    o.Add("@type", JsonValue.Create("ProfilePage"))
    o.Add("@id", JsonValue.Create(absUrl + "#profilepage"))
    o.Add("url", JsonValue.Create(absUrl))
    o.Add("name", JsonValue.Create("About " + Constants.Author.name))
    o.Add("isPartOf", idRef websiteId)
    o.Add("mainEntity", idRef personId)
    o.Add("about", idRef personId)
    o.Add("inLanguage", JsonValue.Create("en"))
    let bc = breadcrumbNode [ ("Home", "/"); ("About", pageUrl) ]
    graphJson [ (o :> JsonNode); bc ]

/// A list/landing page `@graph`: a `CollectionPage` (or `Blog` for the posts
/// index) linked to the shared identity, plus a Home -> {title} breadcrumb.
/// `schemaType` is typically "CollectionPage" or "Blog".
let listPageJson (schemaType: string) (pageUrl: string) (title: string) : string =
    let absUrl = canonical + pageUrl
    let o = JsonObject()
    o.Add("@type", JsonValue.Create(schemaType))
    o.Add("@id", JsonValue.Create(absUrl + "#" + schemaType.ToLowerInvariant()))
    o.Add("url", JsonValue.Create(absUrl))
    o.Add("name", JsonValue.Create(title))
    o.Add("isPartOf", idRef websiteId)
    o.Add("author", idRef personId)
    o.Add("publisher", idRef personId)
    o.Add("inLanguage", JsonValue.Create("en"))
    let bc = breadcrumbNode [ ("Home", "/"); (title, pageUrl) ]
    graphJson [ (o :> JsonNode); bc ]

/// A `CollectionPage` `@graph` with an explicit breadcrumb trail — for list
/// pages that live deeper than one level (e.g. tag pages under /tags/).
let collectionPageJson (pageUrl: string) (title: string) (crumbs: (string * string) list) : string =
    let absUrl = canonical + pageUrl
    let o = JsonObject()
    o.Add("@type", JsonValue.Create("CollectionPage"))
    o.Add("@id", JsonValue.Create(absUrl + "#collectionpage"))
    o.Add("url", JsonValue.Create(absUrl))
    o.Add("name", JsonValue.Create(title))
    o.Add("isPartOf", idRef websiteId)
    o.Add("inLanguage", JsonValue.Create("en"))
    graphJson [ (o :> JsonNode); breadcrumbNode crumbs ]

