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

    // Write Home / About / RSS Pages    
    buildHomePage posts
    buildAboutPage()
    buildRssFeed posts
    
    // Write Post / Archive Pages
    buildPostPages posts
    buildPostArchive posts

    buildEventPage ()

    0