// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open System.IO
open Loaders
open ContentTypePagesBuilder
open AssetsBuilder
open AiMemexPagesBuilder
open ResumePageBuilder
open CollectionPagesBuilder
open LegacyFeedsBuilder
open TagPagesBuilder
open LivestreamBuilder
open HomepageBuilder
open BlogArchiveBuilder
open RollsBuilder
open StaticPagesBuilder
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
    prepareDirectories outputDir

    // Data
    let liveStreams = loadLiveStreams (srcDir)
    let feedLinks = loadFeedLinks (srcDir)
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

    // Create RSVPs landing page from rsvp-type responses
    buildRsvpLandingPage responsesFeedData
    
    let snippetsFeedData = buildSnippets()
    let wikisFeedData = buildWikis()
    let aiMemexFeedData = loadAiMemexFeedData()
    let presentationsFeedData = buildPresentations()
    let reviewsFeedData = buildReviews()
    let mediaFeedData = buildMedia()
    let albumCollectionsFeedData = buildAlbumCollections()
    let playlistCollectionsFeedData = buildPlaylistCollections()
    let marketplaceFeedData = buildMarketplace()
    
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
    let reviewsUnified = UnifiedFeeds.convertReviewsToUnified reviewsFeedData
    let albumsUnified = UnifiedFeeds.convertAlbumsToUnified mediaFeedData
    let albumCollectionsUnified = UnifiedFeeds.convertAlbumCollectionsToUnified albumCollectionsFeedData
    let playlistCollectionsUnified = UnifiedFeeds.convertPlaylistCollectionsToUnified playlistCollectionsFeedData
    let marketplaceUnified = UnifiedFeeds.convertMarketplaceToUnified marketplaceFeedData
    
    // Content-type roster (B1): one declarative row per type describing its
    // unified-feed participation. The three membership lists below derive from
    // this single table (per-row flags) instead of being hand-maintained — so a
    // type can no longer be added to one feed list but forgotten in another
    // (the bug class behind pattern-content-type-taxonomy-mismatch). The roster
    // holds already-projected results in the existing build order, so output is
    // byte-identical. Row order = the all-feeds order; the filters preserve it.
    let contentRoster : ContentRegistry.ContentTypeRoster list = [
        { Identity = ContentTypes.ContentType.Posts;              Unified = postsUnified;              InTimeline = true;  InAllFeeds = true; InBlogArchive = true }
        { Identity = ContentTypes.ContentType.Notes;              Unified = notesUnified;              InTimeline = true;  InAllFeeds = true; InBlogArchive = true }
        { Identity = ContentTypes.ContentType.Responses;          Unified = responsesUnified;          InTimeline = true;  InAllFeeds = true; InBlogArchive = true }
        { Identity = ContentTypes.ContentType.Bookmarks;          Unified = bookmarksUnified;          InTimeline = true;  InAllFeeds = true; InBlogArchive = false }
        { Identity = ContentTypes.ContentType.Snippets;           Unified = snippetsUnified;           InTimeline = false; InAllFeeds = true; InBlogArchive = false }
        { Identity = ContentTypes.ContentType.Wiki;               Unified = wikisUnified;              InTimeline = false; InAllFeeds = true; InBlogArchive = false }
        { Identity = ContentTypes.ContentType.AiMemex;            Unified = aiMemexUnified;            InTimeline = false; InAllFeeds = true; InBlogArchive = false }
        { Identity = ContentTypes.ContentType.Presentations;      Unified = presentationsUnified;      InTimeline = false; InAllFeeds = true; InBlogArchive = false }
        { Identity = ContentTypes.ContentType.Reviews;            Unified = reviewsUnified;            InTimeline = true;  InAllFeeds = true; InBlogArchive = false }
        { Identity = ContentTypes.ContentType.Media;              Unified = albumsUnified;             InTimeline = true;  InAllFeeds = true; InBlogArchive = false }
        { Identity = ContentTypes.ContentType.AlbumCollection;    Unified = albumCollectionsUnified;   InTimeline = true;  InAllFeeds = true; InBlogArchive = false }
        { Identity = ContentTypes.ContentType.PlaylistCollection; Unified = playlistCollectionsUnified; InTimeline = false; InAllFeeds = true; InBlogArchive = false }
        { Identity = ContentTypes.ContentType.Marketplace;        Unified = marketplaceUnified;        InTimeline = false; InAllFeeds = true; InBlogArchive = false }
    ]

    // Convert to unified feed items - Timeline feed (main content)
    let timelineFeedItems = ContentRegistry.timeline contentRoster

    // All unified items for RSS feeds and search (includes resources content)
    let allUnifiedItems = ContentRegistry.allFeeds contentRoster

    // Blog Archive / JSON feed scope (posts + notes + responses)
    let blogArchiveFeedItems = ContentRegistry.blogArchive contentRoster
    
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
    let activityPubContent = allUnifiedContent |> List.filter (fun item -> item.ContentType <> ContentTypes.AiMemex && item.ContentType <> ContentTypes.Marketplace)
    ActivityPubBuilder.buildActivities activityPubContent "_public"
    ActivityPubBuilder.buildOutbox activityPubContent "_public"
    ActivityPubBuilder.queueRecentPostsForDelivery activityPubContent "_public"

    // =============================================================================
    // AT Protocol (ATmosphere) staging — site.standard.document records for Posts (Track A).
    // Gated behind AtProtoBuilder.useAtProtoSync (default off) so _public stays byte-identical
    // until the feature is deliberately enabled. The sync script (Scripts/sync-atproto.fsx)
    // consumes these staged records; nothing here writes to the network.
    // =============================================================================
    if AtProtoBuilder.useAtProtoSync then
        printfn "🌐 Building AT Protocol staging records..."
        AtProtoBuilder.buildAtProtoStaging (postsFeedData |> List.map (fun fd -> fd.Content)) "_public"

    // Track B/C — native app.bsky.feed.post staging.  Media has separate
    // image/gallery and video gates; video remains dormant until its upload
    // service rollout is explicitly enabled.  All enabled native records are
    // collision-checked together with Notes before any manifest is written.
    let mediaAlbums = mediaFeedData |> List.map (fun fd -> fd.Content)
    let atprotoBookmarks = bookmarksFeedData |> List.map (fun fd -> fd.Content)
    let atprotoResponses = responsesFeedData |> List.map (fun fd -> fd.Content)
    let nativeKeys =
        AtProtoBuilder.nativeStagingKeys
            (notesFeedData |> List.map (fun fd -> fd.Content))
            mediaAlbums
        @ AtProtoBuilder.responseNativeStagingKeys atprotoBookmarks atprotoResponses
    AtProtoBuilder.assertNoNativeTidCollisions nativeKeys

    // Track B — Notes -> native app.bsky.feed.post staging records. Gated behind
    // AtProtoBuilder.useAtProtoNotesSync (default off) and forward-only from notesActivationCutoff,
    // so _public stays byte-identical until deliberately activated. The sync script consumes these.
    if AtProtoBuilder.useAtProtoNotesSync then
        printfn "🌐 Building AT Protocol note staging records..."
        AtProtoBuilder.buildAtProtoNotesStaging (notesFeedData |> List.map (fun fd -> fd.Content)) "_public"

    // Track C — rich media -> native app.bsky.feed.post.
    if AtProtoBuilder.useAtProtoMediaSync
       || AtProtoBuilder.useAtProtoMediaImageSync
       || AtProtoBuilder.useAtProtoMediaGallerySync
       || AtProtoBuilder.useAtProtoMediaVideoSync then
        printfn "🌐 Building AT Protocol rich-media staging records..."
        AtProtoBuilder.buildAtProtoMediaStaging mediaAlbums "_public"

    // Response POSSE — bookmarks + reshares -> native Bluesky records (#2574 / ADR-0009 amendment).
    // Each mode is gated behind its own dormant flag (all false by default) and a forward-only
    // cutoff, so _public stays byte-identical until a mode is deliberately activated. Bookmarks and
    // ordinary-web reshares become app.bsky.feed.post link posts; ATProto-targeted reshares become
    // a repost (no commentary) or a quote-post (with commentary). The sync script consumes these.
    if AtProtoBuilder.useAtProtoBookmarkPostsSync then
        printfn "🌐 Building AT Protocol bookmark staging records..."
        AtProtoBuilder.buildAtProtoBookmarksStaging atprotoBookmarks "_public"

    if AtProtoBuilder.useAtProtoResharePostsSync
       || AtProtoBuilder.useAtProtoRepostsSync
       || AtProtoBuilder.useAtProtoQuotePostsSync then
        printfn "🌐 Building AT Protocol reshare staging records..."
        AtProtoBuilder.buildAtProtoResharesStaging atprotoResponses "_public"
    
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

    // Reviews are built once above via buildReviews(); no second pass is needed
    // because the review pipeline now produces the final pages directly.

    // Build tags page - unified tag system across all content types
    let notesFromFeedData = notesFeedData |> List.map (fun item -> item.Content) |> List.toArray
    // F3: derive posts/responses for tag pages from already-parsed FeedData instead of
    // re-parsing the same files (loadPosts / a second ResponseProcessor pass) at the top of main.
    let posts = postsFeedData |> List.map (fun item -> item.Content) |> List.toArray
    let responses = responsesFeedData |> List.map (fun item -> item.Content) |> List.toArray

    // Combine regular responses with bookmark responses for complete tag coverage
    let bookmarkResponses = bookmarksFeedData |> List.map (fun item -> item.Content) |> List.toArray
    let allResponses = Array.append responses bookmarkResponses

    let reviews = reviewsFeedData |> List.map (fun item -> item.Content) |> List.toArray

    let allTaggableContent = [
        ("posts", posts |> Array.map (fun p -> p :> ITaggable))
        ("notes", notesFromFeedData |> Array.map (fun n -> n :> ITaggable))
        ("responses", allResponses |> Array.map (fun r -> r :> ITaggable))
        ("snippets", snippetsFeedData |> List.map (fun item -> item.Content) |> List.toArray |> Array.map (fun s -> s :> ITaggable))
        ("wikis", wikisFeedData |> List.map (fun item -> item.Content) |> List.toArray |> Array.map (fun w -> w :> ITaggable))
        ("ai-memex", aiMemexFeedData |> List.map (fun item -> item.Content) |> List.toArray |> Array.map (fun a -> a :> ITaggable))
        ("presentations", presentationsFeedData |> List.map (fun item -> item.Content) |> List.toArray |> Array.map (fun p -> p :> ITaggable))
        ("reviews", reviews |> Array.map (fun r -> r :> ITaggable))
        ("media", mediaFeedData |> List.map (fun item -> item.Content) |> List.toArray |> Array.map (fun a -> a :> ITaggable))
        ("marketplace", marketplaceFeedData |> List.map (fun item -> item.Content) |> List.toArray |> Array.map (fun l -> l :> ITaggable))
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
