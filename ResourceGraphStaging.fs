/// First guarded projection of the site's feed collections into the draft
/// AT Resource Graph bundle wire shape.
///
/// This adapter is intentionally local to lqdev.me. The standalone Resource
/// Graph project is not a package dependency; the JSON emitted here follows
/// its draft me.lqdev.resourcegraph.temp lexicon contract.
module ResourceGraphStaging

open System
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open Collections
open Domain

// ---------------------------------------------------------------------------
// Activation and staging configuration
// ---------------------------------------------------------------------------

/// Resource Graph staging is opt-in. Static builds and AT publication remain
/// unchanged until this flag is deliberately enabled.
let useResourceGraphStaging = false

/// Forward-only activation gate for the first staging slice. Collection source
/// files do not carry historical membership timestamps, so the cutoff gates
/// the snapshot itself rather than inventing dates for individual entries.
let resourceGraphActivationCutoff =
    DateTimeOffset(2026, 9, 19, 0, 0, 0, TimeSpan.FromHours -5.0)

let private stagedCollectionIds = [| "blogroll"; "podroll"; "youtube" |]
let private bundleLexicon = "me.lqdev.resourcegraph.temp.bundle"
let private syndicationRefLexicon = "me.lqdev.resourcegraph.temp.defs#syndicationRef"

/// Conventional OPML cannot carry these graph semantics. Keep the loss
/// contract explicit so a future exporter cannot silently discard them.
let conventionalOpmlLosses =
    [|
        "authorship"
        "membershipRelationships"
        "cids"
        "nestedBundles"
        "provenance"
        "liveFrozenSemantics"
    |]

let private jsonOptions = JsonSerializerOptions(WriteIndented = true)

let private nonNull (value: string) =
    if isNull value then "" else value

type ResourceGraphRejection =
    { CollectionId: string
      CollectionTitle: string
      ItemTitle: string
      OriginalXmlUrl: string
      Type: string
      Position: int
      Code: string
      Reason: string }

type ProjectedCollection =
    { Bundle: JsonObject
      EmittedMemberCount: int
      Rejections: ResourceGraphRejection list }

type private ProjectionError =
    { Code: string
      Reason: string }

let private lengthPrefixed (value: string) =
    let normalized = nonNull value
    sprintf "%d:%s" normalized.Length normalized

/// Stable MD5 source hash, matching the existing AT staging convention. The
/// hash is metadata for change detection and is not added to the draft bundle
/// record because it is not part of the approved lexicon.
let generateSourceHash (collection: Collection) (data: CollectionData) : string =
    let tags =
        if isNull collection.Tags then ""
        else collection.Tags |> Array.toList |> String.concat "\u001f"

    let itemValues (item: CollectionItem) =
        let optionalValue value = value |> Option.defaultValue ""
        let optionalTags =
            item.Tags
            |> Option.defaultValue [||]
            |> Array.toList
            |> String.concat "\u001f"

        [
            item.Title
            item.Type
            item.HtmlUrl
            item.XmlUrl
            optionalValue item.Description
            optionalTags
            optionalValue item.Added
        ]
        |> List.map lengthPrefixed
        |> String.concat "|"

    let input =
        [
            collection.Id
            collection.Title
            collection.Description
            collection.UrlPath
            collection.DataFile
            tags
            yield! data.Items |> Array.toList |> List.map itemValues
        ]
        |> List.map lengthPrefixed
        |> String.concat "\u001e"

    use md5 = MD5.Create()
    md5.ComputeHash(Encoding.UTF8.GetBytes input)
    |> Array.map (fun byte -> byte.ToString("x2"))
    |> String.concat ""

let private trySyndicationFormat (item: CollectionItem) =
    match (nonNull item.Type).Trim().ToLowerInvariant() with
    | "atom" -> Ok "atom"
    | "rss" -> Ok "rss"
    | value ->
        Error
            { Code = "unsupported_syndication_type"
              Reason =
                sprintf
                    "Type '%s' is not supported; expected rss or atom."
                    value }

let private tryValidateSyndicationUri (collection: Collection) (item: CollectionItem) =
    let uri = nonNull item.XmlUrl
    match Uri.TryCreate(uri, UriKind.Absolute) with
    | true, parsed when not (parsed.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)) ->
        Error
            { Code = "non_https_syndication_uri"
              Reason = "XmlUrl must use absolute HTTPS." }
    | true, parsed when String.IsNullOrWhiteSpace parsed.Host ->
        Error
            { Code = "invalid_syndication_uri"
              Reason = "XmlUrl must include an HTTPS host." }
    | true, parsed when not (String.IsNullOrEmpty parsed.UserInfo) ->
        Error
            { Code = "syndication_uri_credentials"
              Reason = "XmlUrl credentials are not allowed." }
    | true, _ -> Ok uri
    | false, _ ->
        Error
            { Code = "invalid_syndication_uri"
              Reason = "XmlUrl must be an absolute HTTPS URI." }

let private formatProjectionError
    (collection: Collection)
    (item: CollectionItem)
    (error: ProjectionError)
    =
    sprintf
        "Resource Graph staging cannot project collection '%s' item '%s': %s"
        collection.Id
        item.Title
        error.Reason

let private validateSyndicationUri (collection: Collection) (item: CollectionItem) =
    match tryValidateSyndicationUri collection item with
    | Ok uri -> uri
    | Error error -> invalidArg "data" (formatProjectionError collection item error)

let private tryBuildSyndicationReference (collection: Collection) (item: CollectionItem) =
    match tryValidateSyndicationUri collection item, trySyndicationFormat item with
    | Ok uri, Ok format ->
        let reference = JsonObject()
        reference.Add("$type", JsonValue.Create syndicationRefLexicon)
        reference.Add("uri", JsonValue.Create uri)
        reference.Add("format", JsonValue.Create format)

        if not (String.IsNullOrWhiteSpace item.Title) then
            reference.Add("title", JsonValue.Create item.Title)

        Ok reference
    | Error error, _ -> Error error
    | _, Error error -> Error error

let private buildSyndicationReference (collection: Collection) (item: CollectionItem) =
    let reference =
        match tryBuildSyndicationReference collection item with
        | Ok value -> value
        | Error error -> invalidArg "data" (formatProjectionError collection item error)

    reference

let private rejectionFor
    (collection: Collection)
    (item: CollectionItem)
    (position: int)
    (error: ProjectionError)
    =
    { CollectionId = collection.Id
      CollectionTitle = collection.Title
      ItemTitle = item.Title
      OriginalXmlUrl = item.XmlUrl
      Type = item.Type
      Position = position
      Code = error.Code
      Reason = error.Reason }

/// Project a collection without throwing for rejected source entries. Accepted
/// members retain their original source positions, so skipped entries create
/// deterministic gaps rather than silently changing authored order.
let projectCollectionWithRejections (data: CollectionData) : ProjectedCollection =
    let collection = data.Metadata
    let members = JsonArray()
    let rejections = ResizeArray<ResourceGraphRejection>()

    data.Items
    |> Array.iteri (fun position item ->
        match tryBuildSyndicationReference collection item with
        | Ok resource ->
            let membership = JsonObject()
            membership.Add("position", JsonValue.Create position)
            membership.Add("resource", resource)
            members.Add membership
        | Error error ->
            rejections.Add(rejectionFor collection item position error))

    let bundle = JsonObject()
    bundle.Add("$type", JsonValue.Create bundleLexicon)
    bundle.Add("name", JsonValue.Create collection.Title)

    if not (String.IsNullOrWhiteSpace collection.Description) then
        bundle.Add("description", JsonValue.Create collection.Description)

    bundle.Add("members", members)

    { Bundle = bundle
      EmittedMemberCount = members.Count
      Rejections = rejections |> Seq.toList }

/// Strict pure projection used by wire-contract tests and callers that want
/// invalid source data to fail explicitly.
let projectCollection (data: CollectionData) : JsonObject =
    let projected = projectCollectionWithRejections data

    match projected.Rejections with
    | [] -> projected.Bundle
    | first :: _ ->
        invalidArg
            "data"
            (sprintf
                "Resource Graph staging rejected collection '%s' item '%s' (%s): %s"
                first.CollectionId
                first.ItemTitle
                first.Code
                first.Reason)

let private rejectionJson (rejection: ResourceGraphRejection) =
    let reference = JsonObject()
    reference.Add("collectionId", JsonValue.Create rejection.CollectionId)
    reference.Add("collectionTitle", JsonValue.Create rejection.CollectionTitle)
    reference.Add("itemTitle", JsonValue.Create rejection.ItemTitle)
    reference.Add("originalXmlUrl", JsonValue.Create rejection.OriginalXmlUrl)
    reference.Add("type", JsonValue.Create rejection.Type)
    reference.Add("position", JsonValue.Create rejection.Position)
    reference.Add("code", JsonValue.Create rejection.Code)
    reference.Add("reason", JsonValue.Create rejection.Reason)

    reference

let private requiredCollectionData
    (collections: (Collection * CollectionData) array)
    (id: string)
    : CollectionData =
    collections
    |> Array.tryFind (fun (collection, _) -> collection.Id = id)
    |> Option.map snd
    |> Option.defaultWith (fun () ->
        invalidArg
            "collections"
            (sprintf "Resource Graph staging source collection '%s' was not loaded." id))

let private sourceManifestEntry
    (data: CollectionData)
    (projected: ProjectedCollection)
    =
    let entry = JsonObject()
    entry.Add("id", JsonValue.Create data.Metadata.Id)
    entry.Add("sourceHash", JsonValue.Create(generateSourceHash data.Metadata data))
    entry.Add("memberCount", JsonValue.Create projected.EmittedMemberCount)
    entry.Add("rejectedCount", JsonValue.Create projected.Rejections.Length)
    entry

let private lossReportJson () =
    let report = JsonObject()
    report.Add("profile", JsonValue.Create "conventional")
    report.Add("status", JsonValue.Create "losses-explicit")

    let losses = JsonArray()
    conventionalOpmlLosses |> Array.iter (fun loss -> losses.Add(JsonValue.Create loss))
    report.Add("losses", losses)
    report

/// JSON loss report used by the staging artifact and focused wire-contract
/// tests. Conventional OPML remains unchanged; this is an explicit warning
/// for any future graph-to-OPML projection.
let conventionalOpmlLossReport () =
    lossReportJson ()

let private manifestJson (collections: (CollectionData * ProjectedCollection) array) =
    let manifest = JsonObject()
    manifest.Add("format", JsonValue.Create "lqdev.me.resource-graph-staging.v1")
    manifest.Add("bundleLexicon", JsonValue.Create bundleLexicon)
    manifest.Add("activationCutoff", JsonValue.Create(resourceGraphActivationCutoff.ToString("O")))
    manifest.Add("stagingOnly", JsonValue.Create true)
    manifest.Add("publication", JsonValue.Create "none")

    let entries = JsonArray()
    collections
    |> Array.iter (fun (data, projected) ->
        entries.Add(sourceManifestEntry data projected))
    manifest.Add("collections", entries)
    manifest

let private rejectionsJson
    (collections: (CollectionData * ProjectedCollection) array)
    =
    let rejections = JsonArray()

    collections
    |> Array.iter (fun (_, projected) ->
        projected.Rejections
        |> List.iter (fun rejection -> rejections.Add(rejectionJson rejection)))

    rejections

let private writeIfChanged (path: string) (content: string) =
    if not (File.Exists path) || File.ReadAllText(path) <> content then
        File.WriteAllText(path, content)

let private isAfterActivationCutoff (now: DateTimeOffset) =
    now >= resourceGraphActivationCutoff

/// Pure forward-only gate used by the build and focused tests.
let isActivationEligible (now: DateTimeOffset) =
    isAfterActivationCutoff now

/// Generate the deterministic staging files for the selected collection
/// snapshot. This function performs only local file I/O and never publishes
/// or deletes AT records.
let buildResourceGraphStagingAt
    (now: DateTimeOffset)
    (collections: (Collection * CollectionData) array)
    (outputDir: string)
    : unit =
    if not (isAfterActivationCutoff now) then
        failwithf
            "Resource Graph staging is forward-only and is not active before %s."
            (resourceGraphActivationCutoff.ToString("O"))

    let selected =
        stagedCollectionIds
        |> Array.map (requiredCollectionData collections)

    let stagingDir = Path.Combine(outputDir, "api", "data", "atproto", "resource-graph")
    Directory.CreateDirectory stagingDir |> ignore

    let projected =
        selected
        |> Array.map (fun data -> data, projectCollectionWithRejections data)

    projected
    |> Array.iter (fun (data, result) ->
        let path = Path.Combine(stagingDir, sprintf "%s.json" data.Metadata.Id)
        writeIfChanged path (result.Bundle.ToJsonString jsonOptions))

    writeIfChanged
        (Path.Combine(stagingDir, "manifest.json"))
        ((manifestJson projected).ToJsonString jsonOptions)

    writeIfChanged
        (Path.Combine(stagingDir, "conventional-opml-loss-report.json"))
        ((lossReportJson ()).ToJsonString jsonOptions)

    writeIfChanged
        (Path.Combine(stagingDir, "rejections.json"))
        ((rejectionsJson projected).ToJsonString jsonOptions)

    let rejectedCount =
        projected
        |> Array.sumBy (fun (_, result) -> result.Rejections.Length)

    printfn
        "  ✅ Generated Resource Graph staging for %d collections (%d emitted memberships, %d rejected)"
        selected.Length
        (projected |> Array.sumBy (fun (_, result) -> result.EmittedMemberCount))
        rejectedCount

/// Existing build entry point: default-off and intentionally separate from
/// Scripts/sync-atproto.fsx, whose --commit gate remains the only AT write path.
let buildResourceGraphStaging
    (collections: (Collection * CollectionData) array)
    (outputDir: string)
    : unit =
    if useResourceGraphStaging then
        buildResourceGraphStagingAt DateTimeOffset.UtcNow collections outputDir
