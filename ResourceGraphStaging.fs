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

let private syndicationFormat (item: CollectionItem) =
    match (nonNull item.Type).Trim().ToLowerInvariant() with
    | "atom" -> "atom"
    | "rss" -> "rss"
    | value ->
        invalidArg
            "data"
            (sprintf
                "Resource Graph staging cannot project item '%s': unsupported syndication type '%s'. Expected rss or atom."
                item.Title
                value)

let private validateSyndicationUri (collection: Collection) (item: CollectionItem) =
    let uri = nonNull item.XmlUrl
    match Uri.TryCreate(uri, UriKind.Absolute) with
    | true, parsed when not (parsed.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)) ->
        invalidArg
            "data"
            (sprintf
                "Resource Graph staging cannot project collection '%s' item '%s': XmlUrl must use absolute HTTPS."
                collection.Id
                item.Title)
    | true, parsed when String.IsNullOrWhiteSpace parsed.Host ->
        invalidArg
            "data"
            (sprintf
                "Resource Graph staging cannot project collection '%s' item '%s': XmlUrl must include an HTTPS host."
                collection.Id
                item.Title)
    | true, parsed when not (String.IsNullOrEmpty parsed.UserInfo) ->
        invalidArg
            "data"
            (sprintf
                "Resource Graph staging cannot project collection '%s' item '%s': XmlUrl credentials are not allowed."
                collection.Id
                item.Title)
    | true, _ -> uri
    | false, _ ->
        invalidArg
            "data"
            (sprintf
                "Resource Graph staging cannot project collection '%s' item '%s': XmlUrl must be an absolute HTTPS URI."
                collection.Id
                item.Title)

let private buildSyndicationReference (collection: Collection) (item: CollectionItem) =
    let reference = JsonObject()
    reference.Add("$type", JsonValue.Create syndicationRefLexicon)
    reference.Add("uri", JsonValue.Create(validateSyndicationUri collection item))
    reference.Add("format", JsonValue.Create(syndicationFormat item))

    if not (String.IsNullOrWhiteSpace item.Title) then
        reference.Add("title", JsonValue.Create item.Title)

    reference

/// Project one existing collection into the draft bundle record. The source
/// item's XmlUrl is the only resource URI used; HtmlUrl is deliberately not
/// fetched or converted into a web-page resource.
let projectCollection (data: CollectionData) : JsonObject =
    let collection = data.Metadata
    let members = JsonArray()

    data.Items
    |> Array.iteri (fun position item ->
        let membership = JsonObject()
        membership.Add("position", JsonValue.Create position)
        membership.Add("resource", buildSyndicationReference collection item)
        members.Add membership)

    let bundle = JsonObject()
    bundle.Add("$type", JsonValue.Create bundleLexicon)
    bundle.Add("name", JsonValue.Create collection.Title)

    if not (String.IsNullOrWhiteSpace collection.Description) then
        bundle.Add("description", JsonValue.Create collection.Description)

    bundle.Add("members", members)
    bundle

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

let private sourceManifestEntry (data: CollectionData) =
    let entry = JsonObject()
    entry.Add("id", JsonValue.Create data.Metadata.Id)
    entry.Add("sourceHash", JsonValue.Create(generateSourceHash data.Metadata data))
    entry.Add("memberCount", JsonValue.Create data.Items.Length)
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

let private manifestJson (collections: CollectionData array) =
    let manifest = JsonObject()
    manifest.Add("format", JsonValue.Create "lqdev.me.resource-graph-staging.v1")
    manifest.Add("bundleLexicon", JsonValue.Create bundleLexicon)
    manifest.Add("activationCutoff", JsonValue.Create(resourceGraphActivationCutoff.ToString("O")))
    manifest.Add("stagingOnly", JsonValue.Create true)
    manifest.Add("publication", JsonValue.Create "none")

    let entries = JsonArray()
    collections |> Array.iter (sourceManifestEntry >> entries.Add)
    manifest.Add("collections", entries)
    manifest

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

    selected
    |> Array.iter (fun data ->
        let path = Path.Combine(stagingDir, sprintf "%s.json" data.Metadata.Id)
        writeIfChanged path ((projectCollection data).ToJsonString jsonOptions))

    writeIfChanged
        (Path.Combine(stagingDir, "manifest.json"))
        ((manifestJson selected).ToJsonString jsonOptions)

    writeIfChanged
        (Path.Combine(stagingDir, "conventional-opml-loss-report.json"))
        ((lossReportJson ()).ToJsonString jsonOptions)

    printfn
        "  ✅ Generated Resource Graph staging for %d collections (%d memberships)"
        selected.Length
        (selected |> Array.sumBy (fun data -> data.Items.Length))

/// Existing build entry point: default-off and intentionally separate from
/// Scripts/sync-atproto.fsx, whose --commit gate remains the only AT write path.
let buildResourceGraphStaging
    (collections: (Collection * CollectionData) array)
    (outputDir: string)
    : unit =
    if useResourceGraphStaging then
        buildResourceGraphStagingAt DateTimeOffset.UtcNow collections outputDir
