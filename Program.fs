// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open System.IO
open Loaders
open Builder
open GenericBuilder
open WebmentionService
open Domain
open FeatureFlags
open MigrationUtils

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

    // Feature Flag Status
    printfn "=== Build Configuration ==="
    FeatureFlags.printStatus()
    match FeatureFlags.validateConfiguration() with
    | Ok message -> printfn $"✅ {message}"
    | Error error -> 
        printfn $"❌ Feature flag error: {error}"
        exit 1
    
    // Migration Progress
    MigrationUtils.printMigrationProgress()
    printfn ""

    // Build static pages
    buildHomePage posts feedNotes responses
    buildAboutPage ()
    buildContactPage ()
    buildStarterPackPage ()
    buildIRLStackPage ()
    buildColophonPage ()
    buildSubscribePage ()
    buildOnlineRadioPage ()

    // Write Post / Archive Pages - Using AST-based processor
    let _ = buildPosts()

    // Build Notes - Using AST-based processor (Notes Migration Complete)
    let _ = buildNotes()
    
    // Build RSS pages
    buildBlogRssFeed posts
    
    // Build responses (star,repost,reply,bookmarks) - AST-based processor
    let _ = buildResponses()
    ()
   
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
    printfn "Building presentations with AST-based processor"
    let _ = buildPresentations()
    ()

    // Build livestream pages
    buildLiveStreamPage ()
    buildLiveStreamsPage liveStreams
    buildLiveStreamPages liveStreams

    // Redirects (TODO: Function missing, need implementation)
    // buildRedirectPages redirects

    // Build Snippet Pages
    let _ = buildSnippets()
    ()

    // Build Wiki Pages  
    printfn "Building wiki pages with AST-based processor"
    let _ = buildWikis()
    ()

    // Build Books
    printfn "Building books with AST-based processor"
    let _ = buildBooks()
    ()

    // Build Albums with feature flag support
    if FeatureFlags.isEnabled FeatureFlags.Albums then
        printfn "Building albums with AST-based processor (NEW_ALBUMS=true)"
        let _ = buildAlbums()
        ()
    else
        printfn "Skipping albums - NEW_ALBUMS feature flag disabled (albums infrastructure commented out)"
        // Old album functions are commented out: buildAlbumPage, buildAlbumPages
        // Albums currently disabled until NEW_ALBUMS=true

    // Build reponses (TODO: Functions missing, need implementation)
    // buildResponsePage responses "Responses" "index"

    // Build tags page
    buildTagsPages posts feedNotes responses

    0
