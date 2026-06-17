module BlogArchiveBuilder

    open System
    open System.IO
    open System.IO.Compression
    open System.Text.Json
    open System.Text.Json.Nodes
    open System.Text.RegularExpressions
    open MarkdownService
    open ViewGenerator
    open Giraffe.ViewEngine
    open BuilderCommon

    type private ArchiveFeedItem = {
        Item: UnifiedFeeds.UnifiedFeedItem
        ContentHtml: string
    }

    type private ArchiveDownload = {
        FileName: string
        Label: string
        Description: string
        SizeBytes: int64
    }

    // Single source of truth lives in GenericBuilder.UnifiedFeeds; reuse it here
    // so BAR exports stay consistent with the JSON feed response stream.
    let private responseFeedTypes = UnifiedFeeds.responseStreamContentTypes
    let private maxUploadNameCollisions = 10000

    let private isRemoteUrl (value: string) =
        value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("//", StringComparison.OrdinalIgnoreCase)

    let private renderArchiveContent (item: UnifiedFeeds.UnifiedFeedItem) =
        match item.ContentType with
        | "posts"
        | "notes" -> convertMdToHtml item.Content
        | _ -> item.Content

    let private rewriteLocalImageSources (html: string) (registerUpload: string -> string option) =
        let imgPattern = @"(<img[^>]*\bsrc\s*=\s*[""'])([^""']+)([""'])"
        Regex.Replace(html, imgPattern, fun m ->
            let imgPrefix = m.Groups.[1].Value
            let imgSource = m.Groups.[2].Value
            let imgSuffix = m.Groups.[3].Value

            if isRemoteUrl imgSource then m.Value
            else
                match registerUpload imgSource with
                | Some archivedPath -> $"{imgPrefix}{archivedPath}{imgSuffix}"
                | None -> m.Value
        )

    let private toArchiveIsoDate (value: string) =
        if String.IsNullOrWhiteSpace(value) then DateTimeOffset.UtcNow.ToString("o")
        else
            try DateTimeOffset.Parse(value).ToString("o")
            with _ -> DateTimeOffset.UtcNow.ToString("o")

    let private formatArchiveDisplayDate (value: string) =
        if String.IsNullOrWhiteSpace(value) then DateTimeOffset.UtcNow.ToString("MMM dd, yyyy")
        else
            try DateTimeOffset.Parse(value).ToString("MMM dd, yyyy")
            with _ -> DateTimeOffset.UtcNow.ToString("MMM dd, yyyy")

    let private parseArchiveSortDate (value: string) =
        if String.IsNullOrWhiteSpace(value) then DateTimeOffset.MinValue
        else
            try DateTimeOffset.Parse(value)
            with _ -> DateTimeOffset.MinValue

    let private formatArchiveSize (sizeBytes: int64) =
        let oneKb = 1024.0
        let oneMb = oneKb * 1024.0
        if sizeBytes < int64 oneKb then $"{sizeBytes} B"
        elif sizeBytes < int64 oneMb then $"{(float sizeBytes / oneKb):F1} KB"
        else $"{(float sizeBytes / oneMb):F2} MB"

    let private buildArchiveLandingPage (downloads: ArchiveDownload list) =
        let archiveView =
            div [ _class "content-wrapper" ] [
                div [ _class "mr-auto" ] [
                    h2 [] [ Text "Blog Archive Format (.bar)" ]
                    p [] [
                        Text "Download portable blog backups in Blog Archive Format (BAR). "
                        Text "Each file contains an h-feed/h-entry index, JSON Feed v1.1 metadata, and bundled local image/media uploads when available."
                    ]
                    p [] [
                        Text "BAR files can be imported into platforms and tools that support blogarchive.org."
                    ]
                    ul [] [
                        for archive in downloads do
                            li [] [
                                a [ _href $"/archive/{archive.FileName}" ] [ Text archive.FileName ]
                                Text $" — {archive.Label} ({formatArchiveSize archive.SizeBytes})"
                                if not (String.IsNullOrWhiteSpace(archive.Description)) then
                                    Text $" — {archive.Description}"
                            ]
                    ]
                ]
            ]

        let archivePage = generate archiveView "defaultindex" "Archive Exports - Luis Quintanilla"
        writePageToDir (Path.Join(outputDir, "archive")) "index.html" archivePage

    let private generateBarFeedJson (archiveTitle: string) (homePageUrl: string) (entries: ArchiveFeedItem list) =
        let items = JsonArray()
        entries
        |> List.iter (fun entry ->
            let item = JsonObject()
            item["id"] <- entry.Item.Url
            item["url"] <- entry.Item.Url
            item["title"] <- entry.Item.Title
            item["content_html"] <- entry.ContentHtml
            item["date_published"] <- toArchiveIsoDate entry.Item.Date
            if not (isNull entry.Item.Tags) && entry.Item.Tags.Length > 0 then
                let tags = JsonArray()
                entry.Item.Tags |> Array.iter (fun tag -> tags.Add(tag))
                item["tags"] <- tags
            items.Add(item)
        )

        let author = JsonObject()
        author["name"] <- "Luis Quintanilla"
        author["url"] <- "https://www.lqdev.me"
        let authors = JsonArray()
        authors.Add(author)

        let feed = JsonObject()
        feed["version"] <- "https://jsonfeed.org/version/1.1"
        feed["title"] <- archiveTitle
        feed["home_page_url"] <- homePageUrl
        feed["feed_url"] <- "feed.json"
        feed["authors"] <- authors
        feed["items"] <- items

        feed.ToJsonString(JsonSerializerOptions(WriteIndented = true))

    let private buildBarIndexHtml (archiveTitle: string) (homePageUrl: string) (entries: ArchiveFeedItem list) =
        let content =
            html [] [
                head [] [
                    meta [ _charset "utf-8" ]
                    title [] [ Text archiveTitle ]
                ]
                body [] [
                    div [ _class "h-feed" ] [
                        h1 [ _class "p-name" ] [ Text archiveTitle ]
                        a [ _class "u-url"; _href homePageUrl ] []
                        for entry in entries do
                            article [ _class "h-entry" ] [
                                h2 [ _class "p-name" ] [ Text entry.Item.Title ]
                                a [ _class "u-url"; _href entry.Item.Url ] [ Text "Permalink" ]
                                time [ _class "dt-published"; _datetime (toArchiveIsoDate entry.Item.Date) ] [ Text (formatArchiveDisplayDate entry.Item.Date) ]
                                div [ _class "e-content" ] [
                                    rawText entry.ContentHtml
                                ]
                            ]
                    ]
                ]
            ]

        RenderView.AsString.htmlDocument content

    let private buildBarArchive (archiveName: string) (title: string) (homePageUrl: string) (items: UnifiedFeeds.UnifiedFeedItem list) =
        let archiveOutputDir = Path.Join(outputDir, "archive")
        Directory.CreateDirectory(archiveOutputDir) |> ignore

        let uploadPathBySource = Collections.Generic.Dictionary<string, string>()
        let usedUploadPaths = Collections.Generic.HashSet<string>()

        let registerUpload (imgSource: string) =
            let relativePathCandidate =
                let normalizedSource = imgSource.Trim().Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/')
                let canonicalUri = Uri($"https://www.lqdev.me/{normalizedSource.TrimStart('/')}")
                canonicalUri.AbsolutePath.TrimStart('/')

            if String.IsNullOrWhiteSpace(relativePathCandidate) then None
            else
                let pathSegments =
                    relativePathCandidate.Split('/', StringSplitOptions.RemoveEmptyEntries)
                    |> Array.toList

                let sourcePath =
                    pathSegments
                    |> List.fold (fun currentPath segment -> Path.Combine(currentPath, segment)) outputDir
                if File.Exists(sourcePath) then
                    if uploadPathBySource.ContainsKey(sourcePath) then
                        Some uploadPathBySource.[sourcePath]
                    else
                        let extension = Path.GetExtension(relativePathCandidate)
                        let baseName = Path.GetFileNameWithoutExtension(relativePathCandidate)
                        let mutable candidate = $"uploads/images/{baseName}{extension}"
                        let mutable duplicateCount = 1
                        while usedUploadPaths.Contains(candidate) && duplicateCount <= maxUploadNameCollisions do
                            candidate <- $"uploads/images/{baseName}-{duplicateCount}{extension}"
                            duplicateCount <- duplicateCount + 1

                        if duplicateCount > maxUploadNameCollisions then
                            failwith $"Unable to generate unique upload path for {relativePathCandidate}"

                        usedUploadPaths.Add(candidate) |> ignore
                        uploadPathBySource.[sourcePath] <- candidate
                        Some candidate
                else
                    None

        let archiveEntries =
            items
            |> List.sortByDescending (fun item -> parseArchiveSortDate item.Date)
            |> List.map (fun item ->
                let contentHtml = renderArchiveContent item |> fun html -> rewriteLocalImageSources html registerUpload
                { Item = item; ContentHtml = contentHtml }
            )

        let zipPath = Path.Join(archiveOutputDir, $"{archiveName}.bar")
        if File.Exists(zipPath) then File.Delete(zipPath)

        // ZIP DOS-time minimum is 1980-01-01; clamp to keep ZipArchiveEntry.LastWriteTime happy.
        let zipEpoch = DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero)
        let clampForZip (d: DateTimeOffset) = if d < zipEpoch then zipEpoch else d
        let mostRecentDate =
            archiveEntries
            |> List.tryHead
            |> Option.map (fun e -> parseArchiveSortDate e.Item.Date)
            |> Option.defaultValue zipEpoch
            |> clampForZip

        use archiveStream = File.Open(zipPath, FileMode.CreateNew, FileAccess.Write)
        use archive = new ZipArchive(archiveStream, ZipArchiveMode.Create)

        let indexEntry = archive.CreateEntry("index.html")
        indexEntry.LastWriteTime <- mostRecentDate
        do
            use indexWriter = new StreamWriter(indexEntry.Open())
            indexWriter.Write(buildBarIndexHtml title homePageUrl archiveEntries)

        let feedEntry = archive.CreateEntry("feed.json")
        feedEntry.LastWriteTime <- mostRecentDate
        do
            use feedWriter = new StreamWriter(feedEntry.Open())
            feedWriter.Write(generateBarFeedJson title homePageUrl archiveEntries)

        // Sort uploads by in-archive path so iteration order is deterministic
        // regardless of Dictionary insertion semantics.
        uploadPathBySource
        |> Seq.sortBy (fun kvp -> kvp.Value)
        |> Seq.iter (fun kvp ->
            let uploadEntry = archive.CreateEntry(kvp.Value)
            uploadEntry.LastWriteTime <- mostRecentDate
            use uploadEntryStream = uploadEntry.Open()
            use uploadSourceStream = File.OpenRead(kvp.Key)
            uploadSourceStream.CopyTo(uploadEntryStream)
        )

        FileInfo(zipPath).Length

    let buildBlogArchiveExports (feedDataSets: (string * (UnifiedFeeds.UnifiedFeedItem list)) list) =
        let postsItems = feedDataSets |> List.tryFind (fun (name, _) -> name = "posts") |> Option.map snd |> Option.defaultValue []
        let notesItems = feedDataSets |> List.tryFind (fun (name, _) -> name = "notes") |> Option.map snd |> Option.defaultValue []
        let responseItems =
            feedDataSets
            |> List.collect snd
            |> List.filter (fun item -> responseFeedTypes.Contains(item.ContentType))

        let allItems =
            [ postsItems; notesItems; responseItems ]
            |> List.concat
            |> List.sortByDescending (fun item -> parseArchiveSortDate item.Date)

        let downloads = [
            {
                FileName = "posts.bar"
                Label = "Posts"
                Description = "Long-form blog posts"
                SizeBytes = buildBarArchive "posts" "Luis Quintanilla — Posts" "https://www.lqdev.me/posts/" postsItems
            }
            {
                FileName = "notes.bar"
                Label = "Notes"
                Description = "Short-form notes and updates"
                SizeBytes = buildBarArchive "notes" "Luis Quintanilla — Notes" "https://www.lqdev.me/notes/" notesItems
            }
            {
                FileName = "responses.bar"
                Label = "Responses"
                Description = "Replies, stars, and reshares"
                SizeBytes = buildBarArchive "responses" "Luis Quintanilla — Responses" "https://www.lqdev.me/responses/" responseItems
            }
            {
                FileName = "all.bar"
                Label = "All"
                Description = "Combined posts, notes, and responses stream"
                SizeBytes = buildBarArchive "all" "Luis Quintanilla — All Streams" "https://www.lqdev.me/feed/" allItems
            }
        ]

        buildArchiveLandingPage downloads
        printfn "✅ Blog archive exports generated: posts.bar, notes.bar, responses.bar, all.bar"
