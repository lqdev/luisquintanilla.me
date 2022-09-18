// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open System.IO
open Builder
open WebmentionService
open Domain

[<EntryPoint>]
let main argv =
   
    let srcDir = "_src"
    let outputDir = "_public"

    // Prep work
    cleanOutputDirectory outputDir
    copyStaticFiles ()

    //Create directories
    Path.Join(outputDir,"feed") |> Directory.CreateDirectory |> ignore
    Path.Join(outputDir,"posts") |> Directory.CreateDirectory |> ignore
    Path.Join(outputDir,"presentations") |> Directory.CreateDirectory |> ignore
    Path.Join(outputDir,"snippets") |> Directory.CreateDirectory |> ignore
    Path.Join(outputDir,"wiki") |> Directory.CreateDirectory |> ignore    
    // copyStaticFiles srcDir outputDir
    // copyImages "images" "images"

    // Data
    let posts = loadPosts() 
    let feedPosts = loadFeed ()
    let presentations = loadPresentations ()
    let links = loadLinks ()
    let blogrollLinks = loadBlogrollLinks ()
    let podrollLinks = loadPodrollLinks ()
    let redirects = loadRedirects ()
    let snippets = loadSnippets ()
    let wikis = loadWikis ()
    let books = loadBooks ()
    let albums = loadAlbums ()
    let responses = loadReponses ()

    let notePosts = filterFeedByPostType feedPosts "note"
    let photoPosts = filterFeedByPostType feedPosts "photo"
    let videoPosts = filterFeedByPostType feedPosts "video"


    // Build static pages
    buildHomePage posts
    buildAboutPage ()
    buildContactPage ()
    buildIRLStackPage ()
    buildColophonPage ()
    buildSubscribePage ()
    buildOnlineRadioPage ()

    // Write Post / Archive Pages
    buildPostPages posts
    buildPostArchive posts

    // Build Feeds
    buildFeedPage feedPosts "Main Feed - Luis Quintanilla" "index"
    buildFeedPage notePosts "Notes Feed - Luis Quintanilla" "notes"
    buildFeedPage photoPosts "Photos Feed - Luis Quintanilla" "photos"
    buildFeedPage videoPosts "Videos Feed - Luis Quintanilla" "videos"

    // Build RSS pages
    buildBlogRssFeed posts
    buildFeedRssPage feedPosts "index"
    buildFeedRssPage notePosts "notes"
    buildFeedRssPage notePosts "photos"
    buildFeedRssPage videoPosts "videos"
   
    // Build roll pages
    buildBlogrollPage blogrollLinks
    buildBlogrollOpml blogrollLinks
    buildPodrollPage podrollLinks
    buildPodrollOpml podrollLinks

    // Build event page
    buildEventPage ()

    // Build linklog page
    buildLinkblogPage links

    // Build presentation pages
    buildPresentationsPage presentations
    buildPresentationPages presentations

    // Redirects
    buildRedirectPages redirects

    // Build Snippet Pages
    buildSnippetPage snippets
    buildSnippetPages snippets

    // Build Wiki
    buildWikiPage (wikis |> Array.sortBy(fun x -> x.Metadata.Title))
    buildWikiPages wikis

    // Build books
    buildLibraryPage books
    buildBookPages books

    // Build gallery pages
    buildAlbumPage albums
    buildAlbumPages albums

    // Build reponses
    buildResponsePage responses "Responses" "responses"

    // Send webmentions
    let mentions = 
        responses
        |> Array.filter(fun x -> 
            let currentDateTime = DateTimeOffset(DateTime.Now).ToOffset(TimeSpan(-4,0,0))
            let updatedDateTime = DateTimeOffset(DateTime.SpecifyKind(DateTime.Parse(x.Metadata.DateUpdated).AddMinutes(60),DateTimeKind.Local)).ToOffset(TimeSpan(-4,0,0))
            printfn $"Current: {currentDateTime}"
            printfn $"Updated: {updatedDateTime}"
            currentDateTime < updatedDateTime)
        |> Array.map(fun x -> { SourceUrl=new Uri($"http://lqdev.me/feed/{x.FileName}"); TargetUrl=new Uri(x.Metadata.TargetUrl) })

    printfn "%A" mentions

    mentions
    |> runWebmentionWorkflow
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore

    0