module LinkPreview

open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open System.Collections.Generic
open LinkRegistry
open UnifiedFeeds

[<CLIMutable>]
type PreviewRecord =
    { Title: string
      Type: string
      Date: string
      Updated: string
      Summary: string
      Url: string }

[<CLIMutable>]
type PreviewBucket =
    { Version: int
      BucketCount: int
      Entries: Dictionary<string, PreviewRecord> }

type PreviewStats =
    { RecordCount: int
      BucketCount: int }

let private displayType (contentType: string) =
    match contentType with
    | "posts" -> "Post"
    | "notes" -> "Note"
    | "responses" -> "Response"
    | "reply" -> "Reply"
    | "reshare" -> "Reshare"
    | "star" -> "Like"
    | "rsvp" -> "RSVP"
    | "bookmark" -> "Bookmark"
    | "bookmarks" -> "Bookmark"
    | "snippets" -> "Snippet"
    | "wiki" -> "Wiki"
    | "presentations" -> "Presentation"
    | "reviews" -> "Review"
    | "media" -> "Media"
    | "streams" -> "Livestream"
    | "ai-memex" -> "AI Memex"
    | "album-collection" -> "Album collection"
    | "playlist-collection" -> "Playlist"
    | "marketplace" -> "Marketplace listing"
    | other -> other.Replace("-", " ")

let private makeRecord (descriptionsByPath: Map<string, string>) (page: ScannedPage) =
    let fallbackContent = SearchIndex.ContentProcessor.stripHtml page.Item.Content
    let authoredDescription =
        Map.tryFind page.PathKey descriptionsByPath
        |> Option.filter (String.IsNullOrWhiteSpace >> not)
    let bodyText =
        authoredDescription
        |> Option.defaultValue
            (if String.IsNullOrWhiteSpace(page.SummaryText) then fallbackContent else page.SummaryText)
    let summary = SearchIndex.ContentProcessor.generateSummary bodyText 200
    let normalizedSummary =
        if String.IsNullOrWhiteSpace(summary) then page.Item.Title
        else summary.Trim()
    { Title = page.Item.Title
      Type = displayType page.Item.ContentType
      Date = page.Item.Date
      Updated = page.Item.UpdatedDate |> Option.defaultWith (fun () -> null)
      Summary = normalizedSummary
      Url = Constants.Urls.canonical + page.PathKey + "/" }

let private jsonOptions =
    JsonSerializerOptions(
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    )

let buildPreviewBuckets
    (outputDir: string)
    (pages: ScannedPage list)
    (redirects: Map<string, string>)
    (descriptionsByPath: Map<string, string>)
    =
    let records = Dictionary<string, PreviewRecord>(StringComparer.Ordinal)
    for page in pages do
        records[page.PathKey] <- makeRecord descriptionsByPath page

    for KeyValue(aliasPath, targetPath) in redirects do
        let resolvedTarget = resolveRedirectPath redirects targetPath
        match records.TryGetValue(resolvedTarget) with
        | true, record -> records[aliasPath] <- record
        | false, _ -> ()

    let previewDir = Path.Combine(outputDir, "assets", "previews")
    Directory.CreateDirectory(previewDir) |> ignore

    for bucketIndex in 0 .. Constants.LinkPreview.bucketCount - 1 do
        let entries = Dictionary<string, PreviewRecord>(StringComparer.Ordinal)
        records
        |> Seq.filter (fun entry -> LinkClassification.bucketFor entry.Key = bucketIndex)
        |> Seq.sortBy (fun entry -> entry.Key)
        |> Seq.iter (fun entry -> entries[entry.Key] <- entry.Value)

        let bucket =
            { Version = 1
              BucketCount = Constants.LinkPreview.bucketCount
              Entries = entries }
        let fileName = sprintf "b%02d.json" bucketIndex
        File.WriteAllText(
            Path.Combine(previewDir, fileName),
            JsonSerializer.Serialize(bucket, jsonOptions)
        )

    { RecordCount = records.Count
      BucketCount = Constants.LinkPreview.bucketCount }
