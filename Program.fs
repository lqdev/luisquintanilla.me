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

    // Write Home / About / RSS Pages    
    buildHomePage posts
    buildAboutPage()
    buildContactPage()

    // Write Post / Archive Pages
    buildPostPages posts
    buildPostArchive posts

    // Build Feed
    buildFeedPages feedPosts

    // Build RSS pages
    buildBlogRssFeed posts
    buildMainFeedRssPage feedPosts

    // Build event page
    buildEventPage ()

    0