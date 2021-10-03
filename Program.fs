// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open Builder

[<EntryPoint>]
let main argv =
   
    let srcDir = "_src"
    let outputDir = "_public"

    // Prep work
    cleanOutputDirectory outputDir
    copyStaticFiles ()
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
    buildAboutPage()
    buildContactPage()

    // Write Post / Archive Pages
    buildPostPages posts
    buildPostArchive posts

    // Build Feed
    buildFeedPage feedPosts "Luis Quintanilla - Everything Feed" "index"
    buildFeedPage notePosts "Luis Quintanilla - Note Feed" "notes"
    buildFeedPage videoPosts "Luis Quintanilla - Videos Feed" "videos"

    // Build RSS pages
    buildBlogRssFeed posts
    buildFeedRssPage feedPosts "index"
    buildFeedRssPage notePosts "notes"
    buildFeedRssPage videoPosts "videos"
    
    // Build event page
    buildEventPage ()

    0