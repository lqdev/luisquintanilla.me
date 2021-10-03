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
    // copyStaticFiles srcDir outputDir
    // copyImages "images" "images"

    // Data
    let posts = loadPosts() 
    let feedPosts = loadFeed ()

    let notePosts = filterFeedByPostType feedPosts "note"
    let photoPosts = filterFeedByPostType feedPosts "photo"
    let videoPosts = filterFeedByPostType feedPosts "video"

    // Write Home / About / RSS Pages    
    buildHomePage posts
    buildAboutPage ()
    buildContactPage ()
    buildBlogrollPage ()

    // Write Post / Archive Pages
    buildPostPages posts
    buildPostArchive posts

    // Build Feed
    buildFeedPage feedPosts "Main Feed - Luis Quintanilla" "index"
    buildFeedPage notePosts "Notes Feed - Luis Quintanilla" "notes"
    buildFeedPage videoPosts "Videos Feed - Luis Quintanilla" "videos"

    // Build RSS pages
    buildBlogRssFeed posts
    buildFeedRssPage feedPosts "index"
    buildFeedRssPage notePosts "notes"
    buildFeedRssPage videoPosts "videos"
    
    // Build event page
    buildEventPage ()

    0