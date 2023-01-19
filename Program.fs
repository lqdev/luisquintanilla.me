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
    Path.Join(outputDir,"feed","responses") |> Directory.CreateDirectory |> ignore
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
    let blogrollLinks = loadBlogrollLinks ()
    let podrollLinks = loadPodrollLinks ()
    let redirects = loadRedirects ()
    let snippets = loadSnippets ()
    let wikis = loadWikis ()
    let books = loadBooks ()
    let albums = loadAlbums ()
    let responses = loadReponses ()

    // Build static pages
    buildHomePage posts feedPosts responses
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

    // Build RSS pages
    buildBlogRssFeed posts
    buildFeedRssPage feedPosts "index"
    
    // Build responses (star,repost,reply,bookmarks)
    buildResponseFeedRssPage responses "index"
   
    // Build roll pages
    buildBlogrollPage blogrollLinks
    buildBlogrollOpml blogrollLinks
    buildPodrollPage podrollLinks
    buildPodrollOpml podrollLinks

    // Build event page
    buildEventPage ()

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
    buildResponsePage responses "Responses" "index"

    // Send webmentions
    sendWebmentions responses

    0