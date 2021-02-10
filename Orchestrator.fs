module Orchestrator

    open System
    open System.IO
    open System.Text.Json
    open Domain
    open MarkdownService
    open RssService
    open ViewGenerator
    open PartialViews
    
    let private srcDir = "_src"
    let private outputDir = "_public"

    let rec cleanOutputDirectory (outputDir:string) = 
        let dirInfo = DirectoryInfo(outputDir)

        dirInfo.GetFiles()
        |> Array.iter(fun x -> x.Delete())

        dirInfo.GetDirectories()
        |> Array.iter(fun x -> 
            cleanOutputDirectory x.FullName
            x.Delete())

    // let copyStaticFiles (src:string) (dest:string) = 
    let copyStaticFiles () =
        let directories = [   
            "css"
            "js"
            "images" 
        ]

        directories
        |> List.map(fun dir -> Path.Join(srcDir,dir),Path.Join(outputDir,dir))
        |> List.iter(fun (s,d) -> 
            let saveDir = Directory.CreateDirectory(d)
            
            Directory.GetFiles(s)
            |> Array.iter(fun file -> File.Copy(Path.GetFullPath(file),Path.Join(saveDir.FullName,Path.GetFileName(file)),true)))
        
        // let files = 
        //     Directory.GetFiles(src)
        //     |> Array.filter(fun x-> Path.GetExtension(x) = ".css")

        // files |> Array.iter(fun x -> File.Copy(Path.GetFullPath(x),Path.Join(dest,Path.GetFileName(x)),true))

    // let copyImages (src:string) (dest:string) = 
    //     let sourcePath = Path.Join(srcDir,src)
    //     let savePath = Directory.CreateDirectory(Path.Join(outputDir,dest))
        
    //     Directory.GetFiles(sourcePath)
    //     |> Array.iter(fun file -> 
    //         File.Copy(Path.GetFullPath(file),Path.Join(savePath.FullName,Path.GetFileName(file)),true))

    let buildHomePage (posts:Post array) = 
        let recentPosts = 
            posts 
            |> Array.sortByDescending(fun x-> DateTime.Parse(x.Metadata.Date))
            |> Array.take 5 

        let recentPostsContent = generatePartial (recentPostsView recentPosts)
        let homePage = generate (homeView recentPostsContent) "default" "Luis Quintanilla - Home"
        File.WriteAllText(Path.Join(outputDir,"index.html"),homePage)

    let buildAboutPage () = 

        let aboutContent = convertFileToHtml (Path.Join(srcDir,"about.md")) |> aboutView
        let aboutPage = generate aboutContent "default" "Luis Quintanilla - About"
        File.WriteAllText(Path.Join(outputDir,"about.html"), aboutPage)

    let loadPosts () = 
        let postPaths = 
            Directory.GetFiles(Path.Join(srcDir,"posts"))
        
        let posts = postPaths |> Array.map(parseMarkdown)
        
        posts

    let buildRssFeed (posts: Post array) = 
        let rssPage = 
            posts
            |> Array.sortByDescending(fun x -> x.Metadata.Date)
            |> generateRss
            |> string

        File.WriteAllText(Path.Join(outputDir,"feed.rss"), rssPage)  

    let buildPostPages (posts:Post array) = 
        let postPages = 
            posts
            |> Array.map(fun post -> 
                let postView = post.Content |> ConvertMdToHtml |> postView
                post.FileName,generate postView "default" post.Metadata.Title)
        
        let saveDir = Path.Join(outputDir,"posts")
        Directory.CreateDirectory(saveDir) |> ignore
        postPages
        |> Array.iter(fun (fileName,html) ->
            let saveFileName = sprintf "%s.html" fileName
            let savePath = Path.Join(saveDir,saveFileName)
            File.WriteAllText(savePath,html))

    let buildPostArchive (posts:Post array) = 
        let postsPerPage = 5

        posts
        |> Array.sortByDescending(fun x -> DateTime.Parse(x.Metadata.Date))
        |> Array.chunkBySize postsPerPage
        |> Array.iteri(fun i x -> 
            let len = posts |> Array.chunkBySize postsPerPage |> Array.length
            let currentPage = i + 1
            let idx = string currentPage
            let page = generate (postPaginationView currentPage len x) "default" (sprintf "Luis Quintanilla - Posts %s" idx)
            let dir = Directory.CreateDirectory(Path.Join(outputDir,"posts", idx))
            let fileName = "index.html"
            File.WriteAllText(Path.Join(dir.FullName,fileName), page))
    
    let buildEventPage () = 
        let events =  
            File.ReadAllText(Path.Join("Data","events.json"))
            |> JsonSerializer.Deserialize<Event array>
            |> Array.sortByDescending(fun x -> DateTime.Parse(x.Date))

        // let eventPage = generate (eventView events) "default" "Luis Quintanilla - Events"
        let eventPage = generate (underConstructionView ()) "default" "Luis Quintanilla - Events"
        File.WriteAllText(Path.Join(outputDir,"events.html"),eventPage)
        
        