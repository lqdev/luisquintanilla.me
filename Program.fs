// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open System.IO
open Loaders
open Builder
open GenericBuilder
open WebmentionService
open Domain
open PersonalSite.Redirects
open TextOnlyBuilder

[<EntryPoint>]
let main argv =
   
    let srcDir = "_src"
    let outputDir = "_public"

    // Prep work
    cleanOutputDirectory outputDir
    copyStaticFiles ()

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
    let posts = loadPosts(srcDir) 
    let feedNotes = 
        // Load notes using new AST-based system
        let noteFiles = 
            Directory.GetFiles(Path.Join(srcDir, "feed"))
            |> Array.filter (fun f -> f.EndsWith(".md"))
            |> Array.toList
        let processor = GenericBuilder.NoteProcessor.create()
        let feedData = GenericBuilder.buildContentWithFeeds processor noteFiles
        feedData |> List.map (fun item -> item.Content) |> List.toArray
    let liveStreams = loadLiveStreams (srcDir)
    let feedLinks = loadFeedLinks (srcDir)
    let redirects = loadRedirects ()
    let books = loadBooks (srcDir)
    let albums = loadAlbums (srcDir)
    let responses = 
        // Load responses using AST-based system
        let responseFiles = 
            Directory.GetFiles(Path.Join(srcDir, "responses"))
            |> Array.filter (fun f -> f.EndsWith(".md"))
            |> Array.toList
        let processor = GenericBuilder.ResponseProcessor.create()
        let feedData = GenericBuilder.buildContentWithFeeds processor responseFiles
        feedData |> List.map (fun item -> item.Content) |> List.toArray
    let blogrollLinks = loadBlogrollLinks ()
    let podrollLinks = loadPodrollLinks ()
    let forumLinks = loadForumsLinks ()
    let youTubeLinks = loadYouTubeLinks ()
    let aiStarterPackLinks = loadAIStarterPackLinks ()

    // Build static pages
    // buildHomePage posts feedNotes responses  // Traditional homepage - replaced by timeline
    buildAboutPage ()
    buildContactPage ()
    buildSearchPage ()
    buildStarterPackPage ()
    buildIRLStackPage ()
    buildColophonPage ()
    buildOnlineRadioPage ()

    // =============================================================================
    // Unified Feed System - Collect all feed data and generate unified feeds
    // =============================================================================
    
    // Collect feed data from all content types
    let postsFeedData = buildPosts()
    let notesFeedData = buildNotes()
    let responsesFeedData = buildResponses()
    
    // Generate bookmarks landing page from bookmark responses
    buildBookmarksLandingPage responsesFeedData
    
    let snippetsFeedData = buildSnippets()
    let wikisFeedData = buildWikis()
    let presentationsFeedData = buildPresentations()
    let booksFeedData = buildBooks()
    let mediaFeedData = buildMedia()
    
    // Convert to unified feed items
    let allUnifiedItems = [
        ("posts", GenericBuilder.UnifiedFeeds.convertPostsToUnified postsFeedData)
        ("notes", GenericBuilder.UnifiedFeeds.convertNotesToUnified notesFeedData)
        ("responses", GenericBuilder.UnifiedFeeds.convertResponsesToUnified responsesFeedData)
        ("bookmarks", GenericBuilder.UnifiedFeeds.convertResponseBookmarksToUnified responsesFeedData)
        ("snippets", GenericBuilder.UnifiedFeeds.convertSnippetsToUnified snippetsFeedData)
        ("wiki", GenericBuilder.UnifiedFeeds.convertWikisToUnified wikisFeedData)
        ("presentations", GenericBuilder.UnifiedFeeds.convertPresentationsToUnified presentationsFeedData)
        ("reviews", GenericBuilder.UnifiedFeeds.convertBooksToUnified booksFeedData)
        ("media", GenericBuilder.UnifiedFeeds.convertAlbumsToUnified mediaFeedData)
    ]
    
    // Generate unified feeds (fire-hose + type-specific)
    GenericBuilder.UnifiedFeeds.buildAllFeeds allUnifiedItems "_public"
    
    // Generate tag RSS feeds using unified feed data
    GenericBuilder.UnifiedFeeds.buildTagFeeds allUnifiedItems "_public"
    
    // Build Timeline Homepage (Feed-as-Homepage Phase 3)
    buildTimelineHomePage allUnifiedItems
    
    // Generate unified feed HTML page
    buildUnifiedFeedPage allUnifiedItems
   
    // Build roll pages
    buildFeedsOpml feedLinks
    
    buildBlogrollPage blogrollLinks
    buildBlogrollOpml blogrollLinks
    buildPodrollPage podrollLinks
    buildPodrollOpml podrollLinks
    buildForumsPage forumLinks
    buildForumsOpml forumLinks
    buildYouTubeChannelsPage youTubeLinks
    buildYouTubeOpml youTubeLinks
    buildAIStarterPackPage aiStarterPackLinks
    buildAIStarterPackOpml aiStarterPackLinks

    // Build event page
    buildEventPage ()

    // Build presentation pages
    let _ = buildPresentations()
    ()

    // Build livestream pages
    buildLiveStreamPage ()
    buildLiveStreamsPage liveStreams
    buildLiveStreamPages liveStreams

    // Build redirect pages for URL migration
    buildRedirectPages redirects

    // Build Snippet Pages
    let _ = buildSnippets()
    ()

    // Build Wiki Pages  
    let _ = buildWikis()
    ()

    // Build Books
    let _ = buildBooks()
    ()

    // Build Media
    let _ = buildMedia()
    ()

    // Build tags page
    buildTagsPages posts feedNotes responses

    // Generate redirect pages for legacy URLs
    PersonalSite.Redirects.createRedirectPages outputDir

    // Build legacy RSS feed aliases for backward compatibility (at the very end)
    buildLegacyRssFeedAliases ()

    // =============================================================================
    // Text-Only Site Generation - Phase 1 Implementation
    // =============================================================================
    
    // Prepare unified content for text-only site
    let allUnifiedContent = 
        allUnifiedItems
        |> List.collect snd
        |> List.sortByDescending (fun item -> item.Date)
    
    // Build text-only site
    TextOnlyBuilder.buildTextOnlySite outputDir allUnifiedContent

    // =============================================================================
    // Enhanced Content Discovery - Search Index Generation
    // =============================================================================
    
    // Generate search indexes for client-side search functionality
    let searchIndexStats = SearchIndex.buildSearchIndexes outputDir allUnifiedContent
    
    printfn $"✅ Search indexes generated: {searchIndexStats.SearchIndex.ItemCount} content items, {searchIndexStats.TagIndex.TagCount} tags"

    0
