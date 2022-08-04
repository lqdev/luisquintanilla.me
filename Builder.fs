module Builder

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

    let copyStaticFiles () =
        let directories = [
            "css"
            "css/bootstrap-icons-1.5.0"
            // "js"
            "images"
            "lib"
            "lib/boostrap"
            "lib/highlight"
            "lib/jquery"
            "lib/revealjs"
            "lib/revealjs/dist"
            "lib/revealjs/dist/theme"
            "lib/revealjs/dist/theme/fonts"
            "lib/revealjs/dist/theme/fonts/league-gothic"
            "lib/revealjs/dist/theme/fonts/source-sans-pro"
            "lib/revealjs/plugin"
            "lib/revealjs/plugin/markdown"
        ]

        directories
        |> List.map(fun dir -> Path.Join(srcDir,dir),Path.Join(outputDir,dir))
        |> List.iter(fun (s,d) -> 
            let saveDir = Directory.CreateDirectory(d)
            
            Directory.GetFiles(s)
            |> Array.iter(fun file -> File.Copy(Path.GetFullPath(file),Path.Join(saveDir.FullName,Path.GetFileName(file)),true)))
        
        // Copy favicon & avatar
        File.Copy(Path.Join(srcDir,"favicon.ico"),Path.Join(outputDir,"favicon.ico"),true)
        File.Copy(Path.Join(srcDir,"avatar.png"),Path.Join(outputDir,"avatar.png"),true)

    let buildHomePage (posts:Post array) = 
        let recentPosts = 
            posts 
            |> Array.sortByDescending(fun x-> DateTime.Parse(x.Metadata.Date))
            |> Array.take 5 

        let recentPostsContent = generatePartial (recentPostsView recentPosts)
        let homePage = generate (homeView recentPostsContent) "default" "Home - Luis Quintanilla"
        File.WriteAllText(Path.Join(outputDir,"index.html"),homePage)

    let buildAboutPage () = 

        let aboutContent = convertFileToHtml (Path.Join(srcDir,"about.md")) |> aboutView
        let aboutPage = generate aboutContent "default" "About - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"about")
        Directory.CreateDirectory(saveDir)
        File.WriteAllText(Path.Join(saveDir,"index.html"), aboutPage)

    let buildBlogrollPage () = 
        let blogRollContent = Path.Join(srcDir,"blogroll.md") |> convertFileToHtml |> blogRollView
        let blogRollPage = generate blogRollContent "default" "Blogroll - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"feed","blogroll")
        Directory.CreateDirectory(saveDir)        
        File.WriteAllText(Path.Join(saveDir,"index.html"), blogRollPage)

    let buildPodrollPage () = 
        let podRollContent = Path.Join(srcDir,"podroll.md") |> convertFileToHtml |> podRollView
        let podRollPage = generate podRollContent "default" "Podroll - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"feed","podroll")
        Directory.CreateDirectory(saveDir)         
        File.WriteAllText(Path.Join(saveDir,"index.html"), podRollPage)

    let buildIRLStackPage () = 
        let irlStackContent = Path.Join(srcDir,"irl-stack.md") |> convertFileToHtml |> irlStackView
        let irlStackPage = generate irlStackContent "default" "In Real Life Stack - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"irl-stack")
        Directory.CreateDirectory(saveDir)                
        File.WriteAllText(Path.Join(saveDir,"index.html"), irlStackPage)

    let buildColophonPage () = 
        let colophonContent = Path.Join(srcDir,"colophon.md") |> convertFileToHtml |> irlStackView
        let colophonPage = generate colophonContent "default" "Colophon - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"colophon")
        Directory.CreateDirectory(saveDir)                
        File.WriteAllText(Path.Join(saveDir,"index.html"), colophonPage)        

    let buildSubscribePage () = 
        let subscribeContent = Path.Join(srcDir,"subscribe.md") |> convertFileToHtml |> subscribeView
        let subscribePage = generate subscribeContent "default" "Subscribe - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"subscribe")
        Directory.CreateDirectory(saveDir)                
        File.WriteAllText(Path.Join(saveDir,"index.html"), subscribePage)        
    
    let buildContactPage () = 
        let contactContent = convertFileToHtml (Path.Join(srcDir,"contact.md")) |> contactView
        let contactPage = generate contactContent "default" "Contact - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"contact")
        Directory.CreateDirectory(saveDir) 
        File.WriteAllText(Path.Join(saveDir, "index.html"), contactPage)

    let buildOnlineRadioPage () = 
        let onlineRadioContent = convertFileToHtml (Path.Join(srcDir,"radio.md")) |> onlineRadioView
        let onlineRadioPage = generate onlineRadioContent "default" "Online Radio - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"radio")
        Directory.CreateDirectory(saveDir)
        File.WriteAllText(Path.Join(saveDir,"index.html"), onlineRadioPage)        

    let loadPosts () = 
        let postPaths = 
            Directory.GetFiles(Path.Join(srcDir,"posts"))
        
        let posts = postPaths |> Array.map(parsePost)
        
        posts

    let loadFeed () =
        let postPaths = 
            Directory.GetFiles(Path.Join(srcDir,"feed"))
        
        let posts = postPaths |> Array.map(parsePost)
        
        posts

    let loadPresentations () =
        let presentationPaths = 
            Directory.GetFiles(Path.Join(srcDir,"presentations"))
        
        let presentations = presentationPaths |> Array.map(parsePresentation)
        
        presentations

    let loadSnippets () = 
        let snippetPaths = 
            Directory.GetFiles(Path.Join(srcDir,"snippets"))
        
        let snippets = snippetPaths |> Array.map(parseSnippet)
        
        snippets

    let loadWikis () = 
        let wikiPaths = 
            Directory.GetFiles(Path.Join(srcDir,"wiki"))
        
        let wikis = wikiPaths |> Array.map(parseWiki)
        
        wikis        

    let loadLinks () = 
        let links =  
            File.ReadAllText(Path.Join("Data","links.json"))
            |> JsonSerializer.Deserialize<Link array>
            |> Array.sortByDescending(fun x -> DateTime.Parse(x.DateAdded))
    
        links

    let loadRedirects () = 
        let (redirects:RedirectDetails array) = 
            [|
                ("https://twitter.com/ljquintanilla","Twitter")
                ("https://github.com/lqdev","GitHub")
                ("https://www.linkedin.com/in/lquintanilla01", "LinkedIn")
                ("https://matrix.to/#/@lqdev:matrix.lqdev.tech", "Matrix")
                ("https://toot.lqdev.tech/@lqdev", "Mastodon")
                ("https://www.twitch.tv/lqdev1", "Twitch")
                ("https://www.youtube.com/channel/UCkA5fHdQ4cf3D1J19UNgV7A", "YouTube")
            |]

        redirects

    let buildBlogRssFeed (posts: Post array) = 
        let rssPage = 
            posts
            |> Array.sortByDescending(fun x -> DateTime.Parse(x.Metadata.Date))
            |> generateBlogRss
            |> string

        let saveDir = Path.Join(outputDir,"posts")
        File.WriteAllText(Path.Join(saveDir,"index.xml"), rssPage)  

    let buildFeedRssPage (posts: Post array) (saveFileName:string)= 
        let rssPage = 
            posts
            |> Array.sortByDescending(fun x -> DateTime.Parse(x.Metadata.Date))
            |> generateMainFeedRss
            |> string

        let saveDir = Path.Join(outputDir,"feed")            
        File.WriteAllText(Path.Join(saveDir,$"{saveFileName}.xml"), rssPage)  
    
    let buildPostPages (posts:Post array) = 
        let postPages = 
            posts
            |> Array.map(fun post -> 
                let postTitle = post.Metadata.Title
                let postContent = post.Content |> ConvertMdToHtml 
                let postView = postView postTitle postContent
                post.FileName,generate postView "defaultindex" $"{post.Metadata.Title} - Luis Quintanilla")
        
        let rootSaveDir = Path.Join(outputDir,"posts")
        // Directory.CreateDirectory(saveDir) |> ignore
        postPages
        |> Array.iter(fun (fileName,html) ->
            let saveDir = Path.Join(rootSaveDir,fileName)
            Directory.CreateDirectory(saveDir)
            // let saveFileName = sprintf "%s.html" fileName
            let savePath = Path.Join(saveDir,"index.html")
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
            let page = generate (postPaginationView currentPage len x) "defaultindex" $"Posts {idx} - Luis Quintanilla"
            let dir = Directory.CreateDirectory(Path.Join(outputDir,"posts", idx))
            let fileName = "index.html"
            File.WriteAllText(Path.Join(dir.FullName,fileName), page))
    
    let buildEventPage () = 
        let events =  
            File.ReadAllText(Path.Join("Data","events.json"))
            |> JsonSerializer.Deserialize<Event array>
            |> Array.sortByDescending(fun x -> DateTime.Parse(x.Date))

        let eventPage = generate (eventView events) "default" "Events - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"events")
        Directory.CreateDirectory(saveDir)
        File.WriteAllText(Path.Join(saveDir,"index.html"),eventPage)

    let filterFeedByPostType (posts: Post array) (postType: string) = 
        posts |> Array.filter(fun post -> post.Metadata.PostType = postType)

    let buildFeedPage (posts:Post array) (feedTitle:string) (saveFileName:string) =
        
        // Convert post markdown to HTML
        let parsedPosts = 
            posts 
            |> Array.map(fun post -> {post with Content = post.Content |> ConvertMdToHtml})
            |> Array.sortByDescending(fun post -> DateTime.Parse(post.Metadata.Date))

        // Generate aggregate feed
        let feedPage = generate (feedView parsedPosts) "defaultindex" feedTitle
        
        // Create directories
        let rootSaveDir = Path.Join(outputDir,"feed")
        // Directory.CreateDirectory(saveDir) |> ignore

        // Generate individual feed posts        
        parsedPosts
        |> Array.map(fun post -> 
            let postView = feedPostView post
            post.FileName,generate postView "defaultindex" post.Metadata.Title)
        |> Array.iter(fun (fileName,html) ->
            let saveDir = Path.Join(rootSaveDir,fileName)
            Directory.CreateDirectory(saveDir)
            let savePath = Path.Join(saveDir,"index.html")
            File.WriteAllText(savePath,html))        
    
        // Save feed
        File.WriteAllText(Path.Join(rootSaveDir, $"{saveFileName}.html"), feedPage)

    let buildPresentationsPage (presentations: Presentation array) = 
        let presentationPage = generate (presentationsView presentations) "defaultindex" "Presentations - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"presentations")
        Directory.CreateDirectory(saveDir)
        File.WriteAllText(Path.Join(saveDir,"index.html"),presentationPage)

    let buildPresentationPages (presentations:Presentation array) = 
        presentations
        |> Array.iter(fun presentation ->
            let rootSaveDir = Path.Join(outputDir,"presentations")
            let html = presentationPageView presentation
            let presentationView = generate  html "presentation" $"Presentation | {presentation.Metadata.Title} | Luis Quintanilla"
            let saveDir = Path.Join(rootSaveDir,$"{presentation.FileName}")
            Directory.CreateDirectory(saveDir)
            File.WriteAllText(Path.Join(saveDir,"index.html"),presentationView))

    let buildLinklogPage (links: Link array) = 

        let lingLogPage = generate (linkView links) "defaultindex" "Linklog | Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"feed","linklog")
        Directory.CreateDirectory(saveDir)
        File.WriteAllText(Path.Join(saveDir,"index.html"), lingLogPage)

    let buildSnippetPage(snippets:Snippet array) = 
        let snippetsPage = generate (snippetsView snippets) "defaultindex" "Snippets | Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"snippets")
        Directory.CreateDirectory(saveDir)
        File.WriteAllText(Path.Join(saveDir,"index.html"),snippetsPage)

    let buildSnippetPages (snippets:Snippet array) = 
        let rootSaveDir = Path.Join(outputDir,"snippets") 
        
        snippets
        |> Array.iter(fun snippet ->    
            let saveDir = Path.Join(rootSaveDir,snippet.FileName)
            Directory.CreateDirectory(saveDir)
            let html = 
                { snippet with Content=(snippet.Content |> ConvertMdToHtml) }
                |> snippetView
            let snippetView = generate  html "defaultindex" $"Snippet | {snippet.Metadata.Title} | Luis Quintanilla"
            let saveFileName = Path.Join(saveDir,"index.html")
            File.WriteAllText(saveFileName,snippetView))

    let buildWikiPage(snippets:Wiki array) = 
        let wikisPage = generate (wikisView snippets) "defaultindex" "Wiki | Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"wiki")
        Directory.CreateDirectory(saveDir)
        File.WriteAllText(Path.Join(saveDir,"index.html"),wikisPage)

    let buildWikiPages (wikis:Wiki array) = 
        let rootSaveDir = Path.Join(outputDir,"wiki") 
        
        wikis
        |> Array.iter(fun wiki ->    
            let saveDir = Path.Join(rootSaveDir,wiki.FileName)
            Directory.CreateDirectory(saveDir)
            let html = 
                { wiki with Content=(wiki.Content |> ConvertMdToHtml) }
                |> wikiView
            let wikiView = generate  html "defaultindex" $"Wiki | {wiki.Metadata.Title} | Luis Quintanilla"
            let saveFileName = Path.Join(saveDir,"index.html")
            File.WriteAllText(saveFileName,wikiView))


    let buildRedirectPages (redirectDetails: RedirectDetails array) =
        redirectDetails
        |> Array.iter(fun (url:string,title:string) -> 
            let dir = title.ToLower()
            let redirectPage = generateRedirect url title
            let saveDir = Path.Join(outputDir,dir)
            Directory.CreateDirectory(saveDir)
            File.WriteAllText(Path.Join(saveDir,"index.html"), redirectPage)
        )