module ContentTypePagesBuilder

    open System
    open System.IO
    open Domain
    open MarkdownService
    open RelatedContentService
    open ViewGenerator
    open PartialViews
    open BuilderCommon

    // AST-based snippet processing using GenericBuilder infrastructure
    let buildSnippets() =
        let snippetTags (snippet: Snippet) =
            if String.IsNullOrEmpty(snippet.Metadata.Tags) then [||]
            else snippet.Metadata.Tags.Split(',') |> Array.map (fun s -> s.Trim())
        BuildDriver.buildContentType srcDir outputDir {
            Name = ContentTypes.Snippets
            SourceDir = [ "resources"; "snippets" ]
            OutputDir = [ "resources"; "snippets" ]
            Processor = SnippetProcessor.create()
            Slug = fun snippet -> snippet.FileName
            ItemView = fun snippet allSnippets ->
                let relatedSnippets = RelatedContentService.findRelatedContent snippet allSnippets 5
                snippetPageView snippet.Metadata.Title (snippet.Content |> convertMdToHtml) snippet.Metadata.CreatedDate snippet.FileName (snippetTags snippet) relatedSnippets
            ItemTitle = fun snippet -> $"Snippet | {snippet.Metadata.Title} | Luis Quintanilla"
            Layout = "defaultindex"
            Index = Some { View = snippetsView; Title = "Snippets | Luis Quintanilla"; Sort = None }
        }

    // AST-based wiki processing using GenericBuilder infrastructure  
    let buildWikis() =
        let wikiTags (wiki: Wiki) =
            if String.IsNullOrEmpty(wiki.Metadata.Tags) then [||]
            else wiki.Metadata.Tags.Split(',') |> Array.map (fun s -> s.Trim())
        BuildDriver.buildContentType srcDir outputDir {
            Name = ContentTypes.Wiki
            SourceDir = [ "resources"; "wiki" ]
            OutputDir = [ "resources"; "wiki" ]
            Processor = WikiProcessor.create()
            Slug = fun wiki -> wiki.FileName
            ItemView = fun wiki allWikis ->
                let relatedWikis = RelatedContentService.findRelatedContent wiki allWikis 5
                wikiPageView wiki.Metadata.Title (wiki.Content |> convertMdToHtml) wiki.Metadata.LastUpdatedDate wiki.FileName (wikiTags wiki) relatedWikis
            ItemTitle = fun wiki -> $"{wiki.Metadata.Title} | Wiki | Luis Quintanilla"
            Layout = "defaultindex"
            Index = Some { View = wikisView; Title = "Wiki | Luis Quintanilla"; Sort = Some (Array.sortBy (fun (x: Wiki) -> x.Metadata.Title)) }
        }

    // AST-based presentation processing using GenericBuilder infrastructure
    let buildPresentations() =
        BuildDriver.buildContentType srcDir outputDir {
            Name = ContentTypes.Presentations
            SourceDir = [ "resources"; "presentations" ]
            OutputDir = [ "resources"; "presentations" ]
            Processor = PresentationProcessor.create()
            Slug = fun presentation -> presentation.FileName
            ItemView = fun presentation _ -> LayoutViews.presentationPageView presentation
            ItemTitle = fun presentation -> $"{presentation.Metadata.Title} | Presentation | Luis Quintanilla"
            Layout = "defaultindex"
            Index = Some { View = presentationsView; Title = "Presentations | Luis Quintanilla"; Sort = None }
        }

    // AST-based review processing using GenericBuilder infrastructure.
    // Scans all review subdirectories (library, movies, music, business, products)
    // and builds a combined /reviews/ index plus individual pages.
    let buildReviews() =
        let processor = ReviewProcessor.create()

        let reviewFiles =
            let reviewsDir = Path.Join(srcDir, "reviews")
            if Directory.Exists(reviewsDir) then
                Directory.GetFiles(reviewsDir, "*.md", SearchOption.AllDirectories)
                |> Array.filter (fun f -> not (f.EndsWith(".gitkeep")))
                |> Array.toList
            else
                []

        let feedData = GenericBuilder.buildContentWithFeeds processor reviewFiles
        let reviews = feedData |> List.map (fun item -> item.Content) |> List.toArray

        // Individual pages
        feedData
        |> List.iter (fun item ->
            let review = item.Content
            let saveDir = Path.Join(outputDir, "reviews", review.FileName)
            Directory.CreateDirectory(saveDir) |> ignore
            let html = ViewGenerator.generate (reviewPageView review) "defaultindex" $"{review.Metadata.Title} | Reviews | Luis Quintanilla"
            File.WriteAllText(Path.Join(saveDir, "index.html"), html))

        // Combined /reviews/ index, sorted by date descending.
        let sortedReviews =
            reviews
            |> Array.sortByDescending (fun review ->
                let dateStr =
                    if String.IsNullOrWhiteSpace(review.Metadata.PublishedDate) then review.Metadata.DatePublished
                    else review.Metadata.PublishedDate
                try
                    DateTimeOffset.Parse(dateStr)
                with
                | _ -> DateTimeOffset.MinValue)

        let indexHtml = ViewGenerator.generate (reviewsView sortedReviews) "defaultindex" "Reviews | Luis Quintanilla"
        let indexSaveDir = Path.Join(outputDir, "reviews")
        Directory.CreateDirectory(indexSaveDir) |> ignore
        File.WriteAllText(Path.Join(indexSaveDir, "index.html"), indexHtml)

        printfn "✅ Reviews built: %d reviews across all types" reviews.Length

        feedData

    // AST-based post processing using GenericBuilder infrastructure
    let buildPosts() =
        BuildDriver.buildContentTypeWithHead
            (fun (post: Post) -> AtProtoBuilder.documentLinkHead post.Metadata.Date post.FileName)
            srcDir outputDir {
            Name = ContentTypes.Posts
            SourceDir = [ "posts" ]
            OutputDir = [ "posts" ]
            Processor = PostProcessor.create()
            Slug = fun post -> post.FileName
            ItemView = fun post allPosts ->
                let relatedPosts = RelatedContentService.findRelatedContent post allPosts 5
                blogPostView post.Metadata.Title (post.Content |> convertMdToHtml) post.Metadata.Date post.FileName post.Metadata.Tags post.Metadata.ReadingTimeMinutes relatedPosts
            ItemTitle = fun post -> $"{post.Metadata.Title} - Luis Quintanilla"
            Layout = "defaultindex"
            Index = Some { View = feedView; Title = "Posts - Luis Quintanilla"; Sort = Some (Array.sortByDescending (fun (x: Post) -> DateTimeOffset.Parse(x.Metadata.Date))) }
        }

    // AST-based notes processing using GenericBuilder infrastructure
    let buildNotes() =
        BuildDriver.buildContentType srcDir outputDir {
            Name = ContentTypes.Notes
            SourceDir = [ "notes" ]
            OutputDir = [ "notes" ]
            Processor = NoteProcessor.create()
            Slug = fun note -> note.FileName
            ItemView = fun note allNotes ->
                let relatedNotes = RelatedContentService.findRelatedContent note allNotes 5
                LayoutViews.notePostView note.Metadata.Title (note.Content |> convertMdToHtml) note.Metadata.Date note.FileName note.Metadata.Tags note.Metadata.ReadingTimeMinutes relatedNotes
            ItemTitle = fun note -> note.Metadata.Title
            Layout = "defaultindex"
            Index = Some { View = notesView; Title = "Notes - Luis Quintanilla"; Sort = Some (Array.sortByDescending (fun (x: Post) -> DateTimeOffset.Parse(x.Metadata.Date))) }
        }

    // AST-based responses processing using GenericBuilder infrastructure
    let buildResponses() =
        BuildDriver.buildContentType srcDir outputDir {
            Name = ContentTypes.Responses
            SourceDir = [ "responses" ]
            OutputDir = [ "responses" ]
            Processor = ResponseProcessor.create()
            Slug = fun response -> response.FileName
            ItemView = fun response _ ->
                LayoutViews.responsePostView response.Metadata.Title (response.Content |> convertMdToHtml) response.Metadata.DatePublished response.FileName response.Metadata.TargetUrl response.Metadata.Tags response.Metadata.ReadingTimeMinutes response.Metadata.ResponseType response.Metadata.RsvpStatus
            ItemTitle = fun response -> response.Metadata.Title
            Layout = "defaultindex"
            Index = Some { View = responseView; Title = "Responses - Luis Quintanilla"; Sort = Some (Array.sortByDescending (fun (x: Response) -> DateTimeOffset.Parse(x.Metadata.DatePublished))) }
        }

    // Generate bookmarks landing page from bookmark-type responses
    let buildBookmarksLandingPage (responsesFeedData: GenericBuilder.FeedData<Response> list) =
        // Filter for bookmark-type responses only
        let bookmarkResponses = 
            responsesFeedData 
            |> List.map (fun item -> item.Content)
            |> List.filter (fun response -> response.Metadata.ResponseType = "bookmark")
            |> List.sortByDescending (fun response -> DateTimeOffset.Parse(response.Metadata.DatePublished))
            |> List.toArray
        
        // Create the bookmarks landing page using bookmarkResponseView (which handles Response arrays but displays as bookmarks)
        let bookmarksLandingHtml = generate (bookmarkResponseView bookmarkResponses) "defaultindex" "Bookmarks - Luis Quintanilla"
        let bookmarksIndexDir = Path.Join(outputDir, "bookmarks")
        // Use helper to write file
        writePageToDir bookmarksIndexDir "index.html" bookmarksLandingHtml
        
        printfn "✅ Bookmarks landing page created with %d bookmark responses" bookmarkResponses.Length

    // Generate /rsvp landing page from rsvp-type responses (temporal facet of responses;
    // detail pages remain at /responses/{file}/, so no URLs move).
    let buildRsvpLandingPage (responsesFeedData: GenericBuilder.FeedData<Response> list) =
        let rsvpResponses =
            responsesFeedData
            |> List.map (fun item -> item.Content)
            |> List.filter (fun response -> response.Metadata.ResponseType = "rsvp")
            |> List.sortByDescending (fun response -> DateTimeOffset.Parse(response.Metadata.DatePublished))
            |> List.toArray

        let rsvpLandingHtml = generate (rsvpView rsvpResponses) "defaultindex" "RSVPs - Luis Quintanilla"
        let rsvpIndexDir = Path.Join(outputDir, "rsvp")
        writePageToDir rsvpIndexDir "index.html" rsvpLandingHtml

        printfn "✅ RSVPs landing page created with %d rsvp responses" rsvpResponses.Length

    // AST-based media processing using GenericBuilder infrastructure
    let buildMedia() =
        let processor = AlbumProcessor.create()
        BuildDriver.buildContentType srcDir outputDir {
            Name = ContentTypes.Media
            SourceDir = [ "media" ]
            OutputDir = [ "media" ]
            Processor = processor
            Slug = fun album -> album.FileName
            ItemView = fun album _ ->
                mediaPageView album.Metadata.Title (processor.Render album |> convertMdToHtml) album.Metadata.Date album.FileName album.Metadata.Tags
            ItemTitle = fun album -> $"{album.Metadata.Title} | Media | Luis Quintanilla"
            Layout = "defaultindex"
            Index = Some { View = albumsPageView; Title = "Media | Luis Quintanilla"; Sort = Some (Array.sortByDescending (fun x -> DateTimeOffset.Parse(x.Metadata.Date))) }
        }

    // AST-based marketplace processing using GenericBuilder infrastructure
    let buildMarketplace() =
        let processor = MarketplaceProcessor.create()
        BuildDriver.buildContentType srcDir outputDir {
            Name = ContentTypes.Marketplace
            SourceDir = [ "marketplace" ]
            OutputDir = [ "marketplace" ]
            Processor = processor
            Slug = fun listing -> listing.FileName
            ItemView = fun listing _ ->
                marketplaceListingView listing (processor.Render listing |> convertMdToHtml)
            ItemTitle = fun listing -> $"{listing.Metadata.Title} | Marketplace | Luis Quintanilla"
            Layout = "defaultindex"
            Index = Some { View = marketplaceIndexView; Title = "Marketplace | Luis Quintanilla"; Sort = Some (Array.sortByDescending (fun x -> DateTimeOffset.Parse(x.Metadata.Date))) }
        }

    let buildAlbumCollections() =
        let processor = AlbumCollectionProcessor.create()
        BuildDriver.buildContentType srcDir outputDir {
            Name = ContentTypes.AlbumCollection
            SourceDir = [ "albums" ]
            OutputDir = [ "collections"; "albums" ]
            Processor = processor
            Slug = fun albumCollection -> albumCollection.FileName
            ItemView = fun albumCollection _ ->
                albumCollectionDetailView albumCollection (processor.Render albumCollection |> convertMdToHtml)
            ItemTitle = fun albumCollection -> $"{albumCollection.Metadata.Title} | Albums | Luis Quintanilla"
            Layout = "defaultindex"
            Index = Some { View = albumCollectionsPageView; Title = "Albums | Luis Quintanilla"; Sort = None }
        }

    let buildPlaylistCollections() =
        let processor = PlaylistCollectionProcessor.create()
        BuildDriver.buildContentType srcDir outputDir {
            Name = ContentTypes.PlaylistCollection
            SourceDir = [ "playlists" ]
            OutputDir = [ "collections"; "playlists" ]
            Processor = processor
            Slug = fun playlistCollection -> playlistCollection.FileName
            ItemView = fun playlistCollection _ ->
                playlistCollectionDetailView playlistCollection (processor.Render playlistCollection |> convertMdToHtml)
            ItemTitle = fun playlistCollection -> $"{playlistCollection.Metadata.Title} | Playlists | Luis Quintanilla"
            Layout = "defaultindex"
            Index = Some { View = playlistCollectionsPageView; Title = "Playlists | Luis Quintanilla"; Sort = Some (Array.sortByDescending (fun x -> DateTimeOffset.Parse(x.Metadata.Date))) }
        }

    // AST-based bookmark processing using GenericBuilder infrastructure
    // Note: Bookmarks are Response objects with response_type: "bookmark"
    let buildBookmarks() =
        // Bookmarks are Response objects; they have no index of their own — the
        // /bookmarks/ landing page is built by buildBookmarksLandingPage from
        // bookmark-type responses. Hence Index = None.
        BuildDriver.buildContentType srcDir outputDir {
            Name = ContentTypes.Bookmarks
            SourceDir = [ "bookmarks" ]
            OutputDir = [ "bookmarks" ]
            Processor = ResponseProcessor.create()
            Slug = fun response -> response.FileName
            ItemView = fun response _ ->
                LayoutViews.responsePostView response.Metadata.Title (response.Content |> convertMdToHtml) response.Metadata.DatePublished response.FileName response.Metadata.TargetUrl response.Metadata.Tags response.Metadata.ReadingTimeMinutes response.Metadata.ResponseType response.Metadata.RsvpStatus
            ItemTitle = fun response -> response.Metadata.Title
            Layout = "defaultindex"
            Index = None
        }
