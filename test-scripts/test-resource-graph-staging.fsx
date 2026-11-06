// Focused wire-contract tests for the first guarded AT Resource Graph slice.
// Run from the repository root:
//   dotnet fsi test-scripts/test-resource-graph-staging.fsx

#r "nuget: Giraffe.ViewEngine, 1.4.0"
#r "nuget: YamlDotNet, 16.3.0"

#load "../Domain.fs"
#load "../Views/TravelViews.fs"
#load "../Collections.fs"
#load "../ResourceGraphStaging.fs"

open System
open System.IO
open System.Text.Json
open System.Text.Json.Nodes
open Domain
open Collections
open ResourceGraphStaging

let mutable passed = 0
let mutable failed = 0

let check name condition =
    if condition then
        passed <- passed + 1
        printfn "  PASS  %s" name
    else
        failed <- failed + 1
        printfn "  FAIL  %s" name

let fixturePath name =
    Path.Combine("test-scripts", "fixtures", name)

let normalizeJson (value: string) =
    JsonNode.Parse(value).ToJsonString()

let sampleCollection =
    { Id = "blogroll"
      Title = "Blogroll"
      Description = "Websites and blogs I follow regularly"
      CollectionType = MediumFocused "blogs"
      UrlPath = "/collections/blogroll/"
      DataFile = "blogroll.json"
      Tags = [| "blogs"; "reading"; "indieweb" |]
      LastUpdated = "2026-09-19"
      ItemCount = Some 2 }

let sampleData =
    { Metadata = sampleCollection
      Items =
        [| { Title = "First feed"
             Type = "rss"
             HtmlUrl = "https://example.test/first"
             XmlUrl = "https://example.test/feeds/first.xml"
             Description = None
             Tags = None
             Added = None }
           { Title = "Second feed"
             Type = "atom"
             HtmlUrl = "https://example.test/second"
             XmlUrl = "https://example.test/feeds/second.atom"
             Description = None
             Tags = None
             Added = None } |] }

printfn "Resource Graph staging wire-contract tests"
printfn "------------------------------------------"

check "Resource Graph staging is disabled by default" (not useResourceGraphStaging)

let projected = projectCollection sampleData
let expected = File.ReadAllText(fixturePath "resource-graph-bundle.json")
check "bundle fixture matches deterministic projection"
    (normalizeJson (projected.ToJsonString()) = normalizeJson expected)

check "draft bundle discriminator is preserved"
    (projected["$type"].GetValue<string>() = "me.lqdev.resourcegraph.temp.bundle")

let members = projected["members"] :?> JsonArray
check "members are inline and ordered" (members.Count = 2)
check "first membership position is zero" ((members.[0].["position"]).GetValue<int>() = 0)
check "second membership position is one" ((members.[1].["position"]).GetValue<int>() = 1)

let firstResource = members.[0].["resource"] :?> JsonObject
let secondResource = members.[1].["resource"] :?> JsonObject
check "podcast/YouTube-style inputs remain syndication refs"
    (firstResource["$type"].GetValue<string>().EndsWith("#syndicationRef")
     && secondResource["$type"].GetValue<string>().EndsWith("#syndicationRef"))
check "feed URLs are preserved exactly"
    (firstResource["uri"].GetValue<string>() = "https://example.test/feeds/first.xml"
     && secondResource["uri"].GetValue<string>() = "https://example.test/feeds/second.atom")
check "RSS and Atom formats remain explicit"
    (firstResource["format"].GetValue<string>() = "rss"
     && secondResource["format"].GetValue<string>() = "atom")
check "web-page URLs are not emitted as resource URIs"
    (not (firstResource.ContainsKey("htmlUrl")) && not (secondResource.ContainsKey("htmlUrl")))

let invalidUriFixture =
    File.ReadAllText(fixturePath "resource-graph-invalid-uris.json")
    |> JsonSerializer.Deserialize<JsonElement array>

let invalidUriData url =
    { sampleData with
        Items = [| { sampleData.Items.[0] with XmlUrl = url }; sampleData.Items.[1] |] }

for invalidUri in invalidUriFixture do
    let name = invalidUri.GetProperty("name").GetString()
    let url = invalidUri.GetProperty("url").GetString()
    let rejected =
        try
            projectCollection (invalidUriData url) |> ignore
            false
        with
        | :? ArgumentException -> true
        | _ -> false

    check (sprintf "rejects %s URL before serialization" name) rejected

let invalidTypeData =
    { sampleData with
        Items = [| { sampleData.Items.[0] with Type = "html" }; sampleData.Items.[1] |] }

let invalidTypeRejected =
    try
        projectCollection invalidTypeData |> ignore
        false
    with
    | :? ArgumentException -> true
    | _ -> false

check "unsupported non-feed types fail explicitly" invalidTypeRejected

let rejectedProjection =
    projectCollectionWithRejections
        { sampleData with
            Items =
                [| { sampleData.Items.[0] with Type = "html" }; sampleData.Items.[1] |] }

let rejectedProjectionMembers = rejectedProjection.Bundle["members"] :?> JsonArray
check "non-throwing projection reports rejected entries"
    (rejectedProjection.Rejections.Length = 1
     && rejectedProjection.Rejections.Head.Code = "unsupported_syndication_type")
check "accepted members retain source positions after rejection"
    (rejectedProjectionMembers.[0].["position"].GetValue<int>() = 1)

let hashA = generateSourceHash sampleCollection sampleData
let hashB = generateSourceHash sampleCollection sampleData
let changedData =
    { sampleData with
        Items = [| { sampleData.Items.[0] with Title = "Changed feed" }; sampleData.Items.[1] |] }
check "source hash is deterministic" (hashA = hashB)
check "source hash changes when a source item changes" (hashA <> generateSourceHash sampleCollection changedData)

check "cutoff rejects a pre-activation staging run"
    (not (isActivationEligible (resourceGraphActivationCutoff.AddTicks(-1L))))
check "cutoff accepts an activation-time staging run"
    (isActivationEligible resourceGraphActivationCutoff)

let lossFixture = File.ReadAllText(fixturePath "resource-graph-conventional-opml-loss.json")
check "conventional OPML loss report is explicit and fixture-backed"
    (normalizeJson ((conventionalOpmlLossReport ()).ToJsonString()) = normalizeJson lossFixture)

let stagedSources =
    [| for id, title in [| "blogroll", "Blogroll"; "podroll", "Podroll"; "youtube", "YouTube Channels" |] do
           let collection = { sampleCollection with Id = id; Title = title }
           collection, { sampleData with Metadata = collection } |]

let temporaryOutput =
    Path.Combine(Path.GetTempPath(), sprintf "lqdev-resource-graph-%s" (Guid.NewGuid().ToString("N")))

try
    buildResourceGraphStagingAt resourceGraphActivationCutoff stagedSources temporaryOutput
    let stagingPath = Path.Combine(temporaryOutput, "api", "data", "atproto", "resource-graph")
    check "opt-in staging writes the three bundle artifacts"
        ([| "blogroll.json"; "podroll.json"; "youtube.json" |]
         |> Array.forall (fun fileName -> File.Exists(Path.Combine(stagingPath, fileName))))
    check "staging writes a hash manifest"
        (File.Exists(Path.Combine(stagingPath, "manifest.json")))
    check "staging writes the explicit OPML loss report"
        (File.Exists(Path.Combine(stagingPath, "conventional-opml-loss-report.json")))
    check "staging writes an empty deterministic rejection report"
        (File.ReadAllText(Path.Combine(stagingPath, "rejections.json")).Trim() = "[]")
    let snapshotFiles =
        [| "blogroll.json"
           "podroll.json"
           "youtube.json"
           "manifest.json"
           "rejections.json" |]
    let firstSnapshot =
        snapshotFiles
        |> Array.map (fun fileName -> File.ReadAllBytes(Path.Combine(stagingPath, fileName)))
    buildResourceGraphStagingAt resourceGraphActivationCutoff stagedSources temporaryOutput
    let secondSnapshot =
        snapshotFiles
        |> Array.map (fun fileName -> File.ReadAllBytes(Path.Combine(stagingPath, fileName)))
    check "staging bundles, manifest, and rejections are byte-identical across repeated runs"
        (Array.forall2 (=) firstSnapshot secondSnapshot)
finally
    if Directory.Exists temporaryOutput then
        Directory.Delete(temporaryOutput, true)

let configuredCollections = CollectionConfig.getDefaultCollections ()
let selectedIds = configuredCollections |> Array.map (fun collection -> collection.Id) |> Set.ofArray
check "configured source collections include blogroll" (selectedIds.Contains "blogroll")
check "configured source collections include podroll" (selectedIds.Contains "podroll")
check "configured source collections include youtube" (selectedIds.Contains "youtube")

let realBlogroll =
    configuredCollections
    |> Array.find (fun collection -> collection.Id = "blogroll")
    |> CollectionBuilder.processCollectionData

let realYoutube =
    configuredCollections
    |> Array.find (fun collection -> collection.Id = "youtube")
    |> CollectionBuilder.processCollectionData

let realPodroll =
    configuredCollections
    |> Array.find (fun collection -> collection.Id = "podroll")
    |> CollectionBuilder.processCollectionData

let realBlogrollBundle = projectCollection realBlogroll
let realYoutubeBundle = projectCollection realYoutube
let realBlogrollFirstResource =
    (realBlogrollBundle["members"] :?> JsonArray).[0].["resource"] :?> JsonObject
let realYoutubeFirstResource =
    (realYoutubeBundle["members"] :?> JsonArray).[0].["resource"] :?> JsonObject
check "blogroll source URL is copied from existing collection data"
    (realBlogrollFirstResource["uri"].GetValue<string>() = realBlogroll.Items.[0].XmlUrl)
check "YouTube channel feed remains a syndication reference"
    (realYoutubeFirstResource["$type"].GetValue<string>().EndsWith("#syndicationRef")
     && realYoutubeFirstResource["uri"].GetValue<string>() = realYoutube.Items.[0].XmlUrl)

let realSources =
    [| (configuredCollections |> Array.find (fun collection -> collection.Id = "blogroll"), realBlogroll)
       (configuredCollections |> Array.find (fun collection -> collection.Id = "podroll"), realPodroll)
       (configuredCollections |> Array.find (fun collection -> collection.Id = "youtube"), realYoutube) |]

let realHttpPodrollItems =
    realPodroll.Items
    |> Array.filter (fun item -> item.XmlUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))

let realTemporaryOutput =
    Path.Combine(Path.GetTempPath(), sprintf "lqdev-resource-graph-real-%s" (Guid.NewGuid().ToString("N")))

try
    buildResourceGraphStagingAt resourceGraphActivationCutoff realSources realTemporaryOutput
    let realStagingPath =
        Path.Combine(realTemporaryOutput, "api", "data", "atproto", "resource-graph")

    let realBundleFiles = [| "blogroll.json"; "podroll.json"; "youtube.json" |]
    check "real configured staging produces all three bundle files"
        (realBundleFiles
         |> Array.forall (fun fileName -> File.Exists(Path.Combine(realStagingPath, fileName))))

    let bundleUris fileName =
        let root =
            File.ReadAllText(Path.Combine(realStagingPath, fileName))
            |> JsonNode.Parse
            :?> JsonObject

        root["members"]
        :?> JsonArray
        |> Seq.map (fun memberNode ->
            let resource = memberNode.["resource"] :?> JsonObject
            resource["uri"].GetValue<string>())
        |> Seq.toArray

    check "real configured bundles contain no rejected HTTP URLs"
        (realBundleFiles
         |> Array.collect bundleUris
         |> Array.forall (fun uri ->
             uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))

    let rejectionReport =
        File.ReadAllText(Path.Combine(realStagingPath, "rejections.json"))
        |> JsonNode.Parse
        :?> JsonArray

    let podrollRejections =
        rejectionReport
        |> Seq.map (fun node -> node :?> JsonObject)
        |> Seq.filter (fun rejection ->
            rejection["collectionId"].GetValue<string>() = "podroll")
        |> Seq.toArray

    check "real HTTP podroll entries are explicitly rejected"
        (podrollRejections.Length = realHttpPodrollItems.Length
         && podrollRejections.Length > 0)
    check "real rejection report preserves original URL, type, and stable code"
        (podrollRejections
         |> Array.forall (fun rejection ->
             rejection["originalXmlUrl"].GetValue<string>().StartsWith("http://", StringComparison.OrdinalIgnoreCase)
             && rejection["type"].GetValue<string>() = "rss"
             && rejection["code"].GetValue<string>() = "non_https_syndication_uri"
             && rejection["reason"].GetValue<string>() = "XmlUrl must use absolute HTTPS."))

    let realManifest =
        File.ReadAllText(Path.Combine(realStagingPath, "manifest.json"))
        |> JsonNode.Parse
        :?> JsonObject

    let podrollManifest =
        realManifest["collections"]
        :?> JsonArray
        |> Seq.map (fun entry -> entry :?> JsonObject)
        |> Seq.find (fun entry -> entry["id"].GetValue<string>() = "podroll")

    check "manifest counts emitted and rejected podroll members"
        (podrollManifest["memberCount"].GetValue<int>() = realPodroll.Items.Length - realHttpPodrollItems.Length
         && podrollManifest["rejectedCount"].GetValue<int>() = realHttpPodrollItems.Length)

    let realSnapshotFiles = Array.append realBundleFiles [| "manifest.json"; "rejections.json" |]
    let firstRealSnapshot =
        realSnapshotFiles
        |> Array.map (fun fileName -> File.ReadAllBytes(Path.Combine(realStagingPath, fileName)))
    buildResourceGraphStagingAt resourceGraphActivationCutoff realSources realTemporaryOutput
    let secondRealSnapshot =
        realSnapshotFiles
        |> Array.map (fun fileName -> File.ReadAllBytes(Path.Combine(realStagingPath, fileName)))
    check "real bundles, manifest, and rejections are byte-identical across repeated runs"
        (Array.forall2 (=) firstRealSnapshot secondRealSnapshot)
finally
    if Directory.Exists realTemporaryOutput then
        Directory.Delete(realTemporaryOutput, true)

printfn "------------------------------------------"
printfn "Passed: %d   Failed: %d" passed failed
if failed > 0 then
    eprintfn "Resource Graph staging tests FAILED"
    exit 1
else
    printfn "All Resource Graph staging tests passed."
