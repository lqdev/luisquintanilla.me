// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open System.IO
open Loaders
open Builder
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
    let feedPosts = loadFeed (srcDir)
    let liveStreams = loadLiveStreams (srcDir)
    let feedLinks = loadFeedLinks (srcDir)
    let redirects = loadRedirects ()
    let books = loadBooks (srcDir)
    let albums = loadAlbums (srcDir)
    let responses = loadReponses (srcDir)
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
    buildHomePage posts feedPosts responses
    buildAboutPage ()
    buildContactPage ()
    buildStarterPackPage ()
    buildIRLStackPage ()
    buildColophonPage ()
    buildSubscribePage ()
    buildOnlineRadioPage ()

    // Write Post / Archive Pages
    if FeatureFlags.isEnabled FeatureFlags.Posts then
        // Use new AST-based processor
        let _ = buildPosts()
        ()
    else
        // Use legacy string-based processor
        buildPostPages posts
        buildPostArchive posts

    // Build Feeds
    buildFeedPage feedPosts "Main Feed - Luis Quintanilla" "index"

    // Build RSS pages
    buildBlogRssFeed posts
    buildFeedRssPage feedPosts "index"
    
    // Build responses (star,repost,reply,bookmarks)
    buildResponseFeedRssPage responses "index"
   
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

    // Build gallery pages (TODO: Functions missing, need implementation)
    // buildAlbumPage albums
    // buildAlbumPages albums

    // Build reponses (TODO: Functions missing, need implementation)
    // buildResponsePage responses "Responses" "index"

    // Build tags page
    buildTagsPages posts feedPosts responses

    0
