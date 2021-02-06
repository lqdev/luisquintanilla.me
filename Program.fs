// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open System.IO
open MarkdownService
open PartialViews
open ViewGenerator
open RssService

[<EntryPoint>]
let main argv =

    let filePaths = 
        Directory.GetFiles("_src/posts")
    
    let outputDir = "_public"
    
    let posts = filePaths |> Array.map(parseMarkdown) 

    let aboutContent = convertFileToHtml @"_src\about.md" |> aboutView

    // Generate Home / About
    let homePage = generate homeView "default" "Luis Quintanilla"
    let aboutPage = generate aboutContent "default" "Luis Quintanilla - About"
    
    // Generate Post Page
    let postPages = 
        posts
        |> Array.map(fun post -> 
            let postView = post.Content |> ConvertMdToHtml |> postView
            post.FileName,generate postView "default" post.Metadata.Title)

    // Generate rss
    let rssPage = 
        posts
        |> generateRss
        |> string


        
    // Write Home / About / RSS Pages
    File.WriteAllText(Path.Join("_public","index.html"),homePage)
    File.WriteAllText(Path.Join("_public","about.html"),aboutPage)
    File.WriteAllText(Path.Join("_public","feed.rss"), rssPage)  
    
    // Write Post Pages
    postPages
    |> Array.iter(fun (fileName,html) ->
        let saveDir = Path.Join(outputDir,"posts")
        let saveFileName = sprintf "%s.html" fileName
        let savePath = Path.Join(saveDir,saveFileName)
        File.WriteAllText(savePath,html))

    // Write Post pages
    let postsPerPage = 5
    let paginatedPosts = 
        posts
        |> Array.chunkBySize postsPerPage
        |> Array.iteri(fun i x -> 
            let len = posts |> Array.chunkBySize postsPerPage |> Array.length
            let nextPage = i + 2
            let idx = string (i + 1)
            let page = generate (postPaginationView nextPage len x) "default" idx
            let dir = Directory.CreateDirectory(Path.Join("_public","posts", idx))
            let fileName = sprintf "%s.html" idx
            File.WriteAllText(Path.Join(dir.FullName,fileName), page))

    0