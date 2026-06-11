// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open System.IO
open Loaders
open Builder
open GenericBuilder
open WebmentionService
open Domain
open TagService
open TextOnlyBuilder

[<EntryPoint>]
let main argv =
   
    let srcDir = "_src"
    let outputDir = "_public"

    // Prep work
    cleanOutputDirectory outputDir
    copyStaticFiles ()
    
    // Copy Azure Static Web Apps configuration
    let configSourcePath = "staticwebapp.config.json"
    let configTargetPath = Path.Join(outputDir, "staticwebapp.config.json")
    if File.Exists(configSourcePath) then
        File.Copy(configSourcePath, configTargetPath, true)
        printfn "✅ Azure Static Web Apps configuration copied to output directory"

    //Create directories
    Path.Join(outputDir,"feed") |> Directory.CreateDirectory |> ignore
    Path.Join(outputDir,"feed","responses") |> Directory.CreateDirectory |> ignore
    Path.Join(outputDir,"feed","starter") |> Directory.CreateDirectory |> ignore
    Path.Join(outputDir,"posts") |> Directory.CreateDirectory |> ignore
    Path.Join(outputDir,"presentations") |> Directory.CreateDirectory |> ignore
    Path.Join(outputDir,"snippets") |> Directory.CreateDirectory |> ignore
    Path.Join(outputDir,"wiki") |> Directory.CreateDirectory |> ignore 
    Path.Join(outputDir,"tags") |> Directory.CreateDirectory |> ignore 
    // copyStaticFiles srcDir outputDir
    // copyImages "images" "images"

    // Data
    let liveStreams = loadLiveStreams (srcDir)
    let feedLinks = loadFeedLinks (srcDir)
    let books = loadBooks (srcDir)
    let albums = loadAlbums (srcDir)

    // Build static pages
    // buildHomePage posts feedNotes responses  // Traditional homepage - replaced by timeline
    buildAboutPage ()
    buildCollectionsPage ()
    buildContactPage ()
    buildSearchPage ()
    buildStarterPackPage ()
    buildTravelGuidesPage ()
    buildIRLStackPage ()
    buildColophonPage ()
    buildToolsPage ()
    buildOnlineRadioPage ()
    buildResumePage ()

    // =============================================================================
    // Unified Feed System - Collect all feed data and generate unified feeds
    // =============================================================================
    
    // Collect feed data from all content types
    let postsFeedData = buildPosts()
    let notesFeedData = buildNotes()
    let responsesFeedData = buildResponses()
    let bookmarksFeedData = buildBookmarks()
    
    // Create bookmarks landing page using the bookmarks data
    buildBookmarksLandingPage bookmarksFeedData
    
    let snippetsFeedData = buildSnippets()
    let wikisFeedData = buildWikis()
    let aiMemexFeedData = loadAiMemexFeedData()
    let presentationsFeedData = buildPresentations()
    let booksFeedData = buildBooks()
    let mediaFeedData = buildMedia()
    let albumCollectionsFeedData = buildAlbumCollections()
    let playlistCollectionsFeedData = buildPlaylistCollections()
    
    // Convert each content type to unified feed items exactly once (F3: these pure
    // projections were previously re-run up to 3x across the feed lists below).
    let postsUnified = UnifiedFeeds.convertPostsToUnified postsFeedData
    let notesUnified = UnifiedFeeds.convertNotesToUnified notesFeedData
    let responsesUnified = UnifiedFeeds.convertResponsesToUnified responsesFeedData
    let bookmarksUnified = UnifiedFeeds.convertBookmarkResponsesToUnified bookmarksFeedData
    let snippetsUnified = UnifiedFeeds.convertSnippetsToUnified snippetsFeedData
    let wikisUnified = UnifiedFeeds.convertWikisToUnified wikisFeedData
    let aiMemexUnified = UnifiedFeeds.convertAiMemexToUnified aiMemexFeedData
    let presentationsUnified = UnifiedFeeds.convertPresentationsToUnified presentationsFeedData
    let booksUnified = UnifiedFeeds.convertBooksToUnified booksFeedData
    let albumsUnified = UnifiedFeeds.convertAlbumsToUnified mediaFeedData
    let albumCollectionsUnified = UnifiedFeeds.convertAlbumCollectionsToUnified albumCollectionsFeedData
    let playlistCollectionsUnified = UnifiedFeeds.convertPlaylistCollectionsToUnified playlistCollectionsFeedData
    
    // Convert to unified feed items - Timeline feed (main content)
    let timelineFeedItems = [
        (ContentTypes.Posts, postsUnified)
        (ContentTypes.Notes, notesUnified)
        (ContentTypes.Responses, responsesUnified)
        (ContentTypes.Bookmarks, bookmarksUnified)
        (ContentTypes.Reviews, booksUnified)
        (ContentTypes.Media, albumsUnified)
        (ContentTypes.AlbumCollection, albumCollectionsUnified)
    ]
    
    // All unified items for RSS feeds and search (includes resources content)
    let allUnifiedItems = [
        (ContentTypes.Posts, postsUnified)
        (ContentTypes.Notes, notesUnified)
        (ContentTypes.Responses, responsesUnified)
        (ContentTypes.Bookmarks, bookmarksUnified)
        (ContentTypes.Snippets, snippetsUnified)
        (ContentTypes.Wiki, wikisUnified)
        (ContentTypes.AiMemex, aiMemexUnified)
        (ContentTypes.Presentations, presentationsUnified)
        (ContentTypes.Reviews, booksUnified)
        (ContentTypes.Media, albumsUnified)
        (ContentTypes.AlbumCollection, albumCollectionsUnified)
        (ContentTypes.PlaylistCollection, playlistCollectionsUnified)
    ]

    // Blog Archive / JSON feed scope (posts + notes + responses)
    let blogArchiveFeedItems = [
        (ContentTypes.Posts, postsUnified)
        (ContentTypes.Notes, notesUnified)
        (ContentTypes.Responses, responsesUnified)
    ]
    
    // Prepare unified content for text-only site and search indexes
    // Normalize tags through canonical map so all consumers see consolidated tag names
    let allUnifiedContent = 
        allUnifiedItems
        |> List.collect snd
        |> List.sortByDescending (fun item -> item.Date)
        |> List.map (fun item ->
            if isNull item.Tags then item
            else { item with Tags = item.Tags |> Array.map TagService.processTagName |> Array.distinct })
    
    // Generate unified feeds (fire-hose + type-specific)
    UnifiedFeeds.buildAllFeeds allUnifiedItems "_public"

    // Generate JSON Feed v1.1 outputs for posts, notes, responses, and combined stream
    UnifiedFeeds.buildJsonFeeds blogArchiveFeedItems "_public"
    
    // Generate tag RSS feeds using unified feed data
    UnifiedFeeds.buildTagFeeds allUnifiedItems "_public"

    // Generate Blog Archive Format (.bar) exports and archive landing page
    buildBlogArchiveExports blogArchiveFeedItems

    // Phase 3: pre-render styled QR SVGs for every content page so the
    // per-page modal/disclosure can swap from runtime JS to a static asset.
    buildPerPageQRs "_public" allUnifiedContent
    
    // =============================================================================
    // ActivityPub Content Generation - Phase 3+ Implementation
    // Phase 5A: Now generates mixed activity types (Create, Like, Announce)
    // =============================================================================
    
    printfn "🎭 Building ActivityPub content..."
    let activityPubContent = allUnifiedContent |> List.filter (fun item -> item.ContentType <> ContentTypes.AiMemex)
    ActivityPubBuilder.buildActivities activityPubContent "_public"
    ActivityPubBuilder.buildOutbox activityPubContent "_public"
    ActivityPubBuilder.queueRecentPostsForDelivery activityPubContent "_public"
    
    // =============================================================================
    // ActivityPub Followers Collection - Phase 4A Implementation
    // =============================================================================
    
    printfn "🎭 Building ActivityPub followers collection..."
    FollowersSync.buildFollowersCollection "_public"
    
    // Build Timeline Homepage (Feed-as-Homepage Phase 3) - Use timeline-specific content
    buildTimelineHomePage timelineFeedItems
    
    // Generate unified feed HTML page - Use timeline content for main feed page
    buildUnifiedFeedPage timelineFeedItems
    
    // =============================================================================
    // Text-Only Site Generation - Phase 1 Implementation
    // =============================================================================
    
    // Build text-only site
    TextOnlyBuilder.buildTextOnlySite outputDir allUnifiedContent presentationsFeedData
   
    // Build roll pages
    buildFeedsOpml feedLinks
    
    // =============================================================================
    // Unified Collection System - Primary collection processing
    // =============================================================================
    buildUnifiedCollections ()

    // Build event page
    buildEventPage ()

    // Build livestream pages
    buildLiveStreamPage ()
    buildLiveStreamsPage liveStreams
    buildLiveStreamPages liveStreams

    // Build AI Memex Pages (with cross-content connections)
    buildAiMemexPages aiMemexFeedData allUnifiedContent

    // Build Read Later Page
    let readLaterLinks = loadReadLaterLinks()
    buildReadLaterPage readLaterLinks

    // Build Books — NOTE (F3): this second buildBooks() run is intentionally retained.
    // The StarRating SVG gradient IDs (BlockRenderers.fs:85) come from a *global* mutable
    // counter, so the shipped review pages' gradient IDs depend on this being the last render
    // pass. Removing it is cosmetic-only but breaks byte-identical output. Eliminating this
    // last duplicate requires making those gradient IDs page-local/deterministic first
    // (logged for the StarRating cleanup / F7-B2).
    let _ = buildBooks()
    ()

    // Build tags page - unified tag system across all content types
    let notesFromFeedData = notesFeedData |> List.map (fun item -> item.Content) |> List.toArray
    // F3: derive posts/responses for tag pages from already-parsed FeedData instead of
    // re-parsing the same files (loadPosts / a second ResponseProcessor pass) at the top of main.
    let posts = postsFeedData |> List.map (fun item -> item.Content) |> List.toArray
    let responses = responsesFeedData |> List.map (fun item -> item.Content) |> List.toArray

    // Combine regular responses with bookmark responses for complete tag coverage
    let bookmarkResponses = bookmarksFeedData |> List.map (fun item -> item.Content) |> List.toArray
    let allResponses = Array.append responses bookmarkResponses

    let allTaggableContent = [
        ("posts", posts |> Array.map (fun p -> p :> ITaggable))
        ("notes", notesFromFeedData |> Array.map (fun n -> n :> ITaggable))
        ("responses", allResponses |> Array.map (fun r -> r :> ITaggable))
        ("snippets", snippetsFeedData |> List.map (fun item -> item.Content) |> List.toArray |> Array.map (fun s -> s :> ITaggable))
        ("wikis", wikisFeedData |> List.map (fun item -> item.Content) |> List.toArray |> Array.map (fun w -> w :> ITaggable))
        ("ai-memex", aiMemexFeedData |> List.map (fun item -> item.Content) |> List.toArray |> Array.map (fun a -> a :> ITaggable))
        ("presentations", presentationsFeedData |> List.map (fun item -> item.Content) |> List.toArray |> Array.map (fun p -> p :> ITaggable))
        ("media", mediaFeedData |> List.map (fun item -> item.Content) |> List.toArray |> Array.map (fun a -> a :> ITaggable))
    ]
    buildUnifiedTagsPages allTaggableContent

    // Build legacy RSS feed aliases for backward compatibility (at the very end)
    buildLegacyRssFeedAliases ()

    // =============================================================================
    // Enhanced Content Discovery - Search Index Generation
    // =============================================================================
    
    // Generate search indexes for client-side search functionality
    let searchIndexStats = SearchIndex.buildSearchIndexes outputDir allUnifiedContent
    
    printfn $"✅ Search indexes generated: {searchIndexStats.SearchIndex.ItemCount} content items, {searchIndexStats.TagIndex.TagCount} tags"

    // F8 railway: report-loudly-keep-building. Individual error blocks were already
    // printed at parse time; here we summarise and gate the exit code. Default is
    // exit 0 (a bad file must not block publishing the rest); `--strict` /
    // STRICT_CONTENT=1 turns any content error into a non-zero exit for CI.
    let contentErrors = Diagnostics.errorCount ()
    if contentErrors > 0 then
        printfn "⚠ %d content error(s) reported above (files omitted from the site)." contentErrors
        if Diagnostics.isStrict argv then
            printfn "✗ Strict mode: failing the build (exit 1)."
            1
        else
            0
    else
        0
