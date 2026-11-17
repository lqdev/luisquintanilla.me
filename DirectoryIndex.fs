module DirectoryIndexBuilder

open System
open System.Text.Json
open System.Text.Json.Nodes
open Domain

type private DirectoryCandidate = {
    Entry: DirectoryEntry
    Source: DirectorySourceCollection
}

let private compareText (left: string) (right: string) =
    let insensitive = StringComparer.OrdinalIgnoreCase.Compare(left, right)
    if insensitive <> 0 then insensitive
    else StringComparer.Ordinal.Compare(left, right)

let private tryHttpUri (rawUrl: string) =
    if String.IsNullOrWhiteSpace(rawUrl) then
        None
    else
        match Uri.TryCreate(rawUrl.Trim(), UriKind.Absolute) with
        | true, uri when
            (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase)
             || uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            && String.IsNullOrEmpty(uri.UserInfo) ->
            Some uri
        | _ ->
            None

let private normalizeUrl (uri: Uri) =
    let normalized = UriBuilder(uri)
    normalized.Scheme <- uri.Scheme.ToLowerInvariant()
    normalized.Host <- uri.IdnHost.ToLowerInvariant()
    if uri.IsDefaultPort then
        normalized.Port <- -1
    normalized.Uri.AbsoluteUri

let private normalizeTags (tags: string array) =
    if isNull tags then
        [||]
    else
        tags
        |> Array.choose (fun tag ->
            if String.IsNullOrWhiteSpace(tag) then None
            else Some(tag.Trim()))
        |> Array.distinctBy (fun tag -> tag.ToUpperInvariant())
        |> Array.sortWith compareText

let private collectionPath (collection: Collection) =
    let path =
        if String.IsNullOrWhiteSpace(collection.UrlPath) then
            invalidOp $"Collection '{collection.Id}' has no URL path."
        else
            collection.UrlPath.Trim()

    let rootedPath = if path.StartsWith("/", StringComparison.Ordinal) then path else "/" + path
    if rootedPath.EndsWith("/", StringComparison.Ordinal) then rootedPath
    else rootedPath + "/"

let private sourceCollection (collection: Collection) =
    let url = collectionPath collection
    {
        Id = collection.Id
        Title = collection.Title
        Url = url
        OpmlUrl = url + "index.opml"
    }

let private createCandidate (collection: Collection) (item: CollectionItem) =
    let htmlUri = tryHttpUri item.HtmlUrl
    let xmlUri = tryHttpUri item.XmlUrl

    let homepageUri =
        match htmlUri, xmlUri with
        | Some uri, _ -> uri
        | None, Some uri -> uri
        | None, None ->
            let title = if String.IsNullOrWhiteSpace(item.Title) then "(untitled)" else item.Title
            invalidOp
                $"Following list '{collection.Title}' item '{title}' has no usable HTTP(S) homepage or feed URL. HtmlUrl='{item.HtmlUrl}', XmlUrl='{item.XmlUrl}'."

    let homepageUrl = normalizeUrl homepageUri
    let feedUrls =
        match xmlUri with
        | Some uri -> [| normalizeUrl uri |]
        | None -> [||]

    let itemTags =
        match item.Tags with
        | Some tags when not (isNull tags) -> tags
        | _ -> [||]

    let collectionTags = if isNull collection.Tags then [||] else collection.Tags
    let description =
        item.Description
        |> Option.bind (fun value ->
            if String.IsNullOrWhiteSpace(value) then None
            else Some(value.Trim()))

    let title =
        if String.IsNullOrWhiteSpace(item.Title) then homepageUri.Host
        else item.Title.Trim()

    {
        Entry = {
            Id = homepageUrl
            Title = title
            HomepageUrl = homepageUrl
            FeedUrls = feedUrls
            Description = description
            Tags = normalizeTags (Array.append itemTags collectionTags)
            Collections = [| sourceCollection collection |]
        }
        Source = sourceCollection collection
    }

let private mergeCandidates (candidates: DirectoryCandidate array) =
    candidates
    |> Array.groupBy (fun candidate -> candidate.Entry.Id)
    |> Array.map (fun (id, matching) ->
        let first = matching[0].Entry
        let descriptions =
            matching
            |> Array.choose (fun candidate -> candidate.Entry.Description)
        let sources =
            matching
            |> Array.map (fun candidate -> candidate.Source)
            |> Array.distinctBy (fun source -> source.Id)
            |> Array.sortWith (fun left right ->
                let byTitle = compareText left.Title right.Title
                if byTitle <> 0 then byTitle else StringComparer.Ordinal.Compare(left.Id, right.Id))
        let feeds =
            matching
            |> Array.collect (fun candidate -> candidate.Entry.FeedUrls)
            |> Array.distinct
            |> Array.sortWith (fun left right -> StringComparer.Ordinal.Compare(left, right))
        let tags =
            matching
            |> Array.collect (fun candidate -> candidate.Entry.Tags)
            |> normalizeTags

        {
            first with
                Id = id
                FeedUrls = feeds
                Description = Array.tryHead descriptions
                Tags = tags
                Collections = sources
        })
    |> Array.sortWith (fun left right ->
        let byTitle = compareText left.Title right.Title
        if byTitle <> 0 then byTitle
        else StringComparer.Ordinal.Compare(left.HomepageUrl, right.HomepageUrl))

let buildIndex
    (includedCollections: Collection array)
    (loadedCollections: (Collection * CollectionData) array) : DirectoryIndexDocument =

    let candidates =
        includedCollections
        |> Array.collect (fun included ->
            match loadedCollections |> Array.tryFind (fun (collection, _) -> collection.Id = included.Id) with
            | None ->
                invalidOp $"Following list '{included.Id}' was not loaded; directory index generation cannot continue."
            | Some (collection, data) ->
                data.Items
                |> Array.map (createCandidate collection))

    {
        Version = "1.0"
        Entries = mergeCandidates candidates
    }

let private stringNode (value: string) : JsonNode =
    JsonValue.Create(value) :> JsonNode

let private stringArrayNode (values: string array) =
    let array = JsonArray()
    for value in values do
        array.Add(stringNode value)
    array

let private sourceCollectionNode (source: DirectorySourceCollection) =
    let node = JsonObject()
    node["id"] <- stringNode source.Id
    node["title"] <- stringNode source.Title
    node["url"] <- stringNode source.Url
    node["opmlUrl"] <- stringNode source.OpmlUrl
    node :> JsonNode

let private entryNode (entry: DirectoryEntry) =
    let node = JsonObject()
    node["id"] <- stringNode entry.Id
    node["title"] <- stringNode entry.Title
    node["homepageUrl"] <- stringNode entry.HomepageUrl
    node["feedUrls"] <- stringArrayNode entry.FeedUrls
    match entry.Description with
    | Some description -> node["description"] <- stringNode description
    | None -> ()
    node["tags"] <- stringArrayNode entry.Tags
    let collections = JsonArray()
    for source in entry.Collections do
        collections.Add(sourceCollectionNode source)
    node["collections"] <- collections
    node :> JsonNode

let serialize (index: DirectoryIndexDocument) =
    let root = JsonObject()
    root["version"] <- stringNode index.Version
    let entries = JsonArray()
    for entry in index.Entries do
        entries.Add(entryNode entry)
    root["entries"] <- entries
    root.ToJsonString(JsonSerializerOptions(WriteIndented = false))
