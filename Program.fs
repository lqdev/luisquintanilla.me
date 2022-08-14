// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open Builder
open System.IO

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

    let notePosts = filterFeedByPostType feedPosts "note"
    let photoPosts = filterFeedByPostType feedPosts "photo"
    let videoPosts = filterFeedByPostType feedPosts "video"

    // Build static pages
    buildHomePage posts
    buildAboutPage ()
    buildContactPage ()
    buildBlogrollPage ()
    buildPodrollPage ()
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
    buildPodrollOpml podrollLinks

    // Build event page
    buildEventPage ()

    // Build linklog page
    buildLinklogPage links

    // Build presentation pages
    buildPresentationsPage presentations
    buildPresentationPages presentations

    // Redirects
    buildRedirectPages redirects

    // Build Snippet Pages
    buildSnippetPage snippets
    buildSnippetPages snippets

    // Build Wiki
    buildWikiPage wikis
    buildWikiPages wikis

    0