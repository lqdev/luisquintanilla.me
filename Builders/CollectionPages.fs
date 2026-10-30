module CollectionPagesBuilder

    open System.IO
    open Domain
    open ViewGenerator
    open BuilderCommon

    let buildUnifiedCollections () = 
        // Get all collections and their data
        let collections = Collections.CollectionBuilder.buildCollections ()
        
        printfn "🔧 Building unified collections..."
        
        for (collection, data) in collections do
            try
                // Calculate paths
                let paths = Collections.CollectionProcessor.getCollectionPaths collection

                // Generate HTML page using the new system
                let htmlContent = Collections.CollectionProcessor.generateCollectionPage data
                let metadata =
                    pageMetadata
                        (Path.GetDirectoryName(paths.HtmlPath).Replace("\\", "/") |> fun path -> "/" + path.TrimStart('/') + "/")
                        collection.Description
                        "website"
                let htmlPage = generatePage metadata htmlContent "default" $"{collection.Title} - Luis Quintanilla"
                
                // Ensure output directory exists
                let outputPath = Path.Join(outputDir, Path.GetDirectoryName(paths.HtmlPath))
                Directory.CreateDirectory(outputPath) |> ignore
                
                // Write HTML file
                File.WriteAllText(Path.Join(outputDir, paths.HtmlPath), htmlPage)
                
                // Generate and write OPML file
                let opmlContent = Collections.CollectionBuilder.generateCollectionOpmlContent data
                File.WriteAllText(Path.Join(outputDir, paths.OpmlPath), opmlContent)
                
                // Generate and write RSS file
                let rssContent = Collections.CollectionBuilder.generateCollectionRssContent data
                File.WriteAllText(Path.Join(outputDir, paths.RssPath), rssContent)
                
                // Build a single processor instance; reused by GPX + Garmin GPX blocks below.
                let processor = Collections.CollectionProcessor.createCollectionProcessor collection

                // Generate and write GPX file (if applicable)
                match paths.GpxPath with
                | Some gpxRelativePath ->
                    match processor.GenerateGpxFile data with
                    | Some gpxContent ->
                        File.WriteAllText(Path.Join(outputDir, gpxRelativePath), gpxContent)
                        printfn "✅ Generated GPX file: %s" gpxRelativePath
                    | None ->
                        printfn "⚠️  No GPX content generated for %s" collection.Title
                | None -> 
                    () // No GPX file expected for this collection

                // Generate and write Garmin-compatible waypoint-only GPX file (if applicable)
                match paths.GarminGpxPath with
                | Some garminPath ->
                    match processor.GenerateGarminGpxFile data with
                    | Some gpxContent ->
                        File.WriteAllText(Path.Join(outputDir, garminPath), gpxContent)
                        printfn "✅ Generated Garmin GPX file: %s" garminPath
                    | None ->
                        printfn "⚠️  No Garmin GPX content generated for %s" collection.Title
                | None ->
                    () // No Garmin GPX file expected for this collection
                
                printfn "✅ Built collection: %s (%d items)" collection.Title (data.Items.Length)
                
            with
            | ex -> 
                printfn "❌ Error building collection %s: %s" collection.Title ex.Message

        let includedCollections = Collections.FollowingDirectory.getIncludedCollections ()
        let directoryModified =
            seq {
                yield OpmlService.sourceModified "Collections.fs"
                yield OpmlService.sourceModified "Builders/CollectionPages.fs"

                for collection in includedCollections do
                    yield OpmlService.sourceModified (Path.Join("Data", collection.DataFile))
            }
            |> Seq.max

        let metadata =
            {
                Title = "Luis Quintanilla Following"
                OwnerId = Constants.Urls.canonical
                DocsUrl = Some $"{Constants.Urls.canonical}/collections/"
                DateModified = Some directoryModified
            }

        let missingChildOpmls =
            includedCollections
            |> Array.choose (fun collection ->
                let opmlPath = Collections.CollectionProcessor.getCollectionPaths collection
                let generatedPath = Path.Join(outputDir, opmlPath.OpmlPath)

                if File.Exists(generatedPath) then
                    None
                else
                    Some generatedPath)

        if missingChildOpmls.Length > 0 then
            failwithf
                "Following directory cannot be generated because child OPML files are missing: %s"
                (String.concat ", " missingChildOpmls)

        let includeOutlines =
            includedCollections
            |> Array.map Collections.FollowingDirectory.getInclude
            |> Array.map IncludeOutline

        let followingContent =
            OpmlService.buildOpmlDocument metadata includeOutlines
            |> fun document -> document.ToString()

        let discoveryDir = Path.Join(outputDir, ".well-known")
        Directory.CreateDirectory(discoveryDir) |> ignore
        File.WriteAllText(Path.Join(discoveryDir, "following.opml"), followingContent)
        printfn "✅ Built following OPML directory (%d collections)" includeOutlines.Length
