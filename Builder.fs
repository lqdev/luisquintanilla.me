module Builder

    open System
    open System.Globalization
    open System.IO
    open System.Text.Json
    open Domain
    open MarkdownService
    open TagService
    open RssService
    open OpmlService
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
            "js"
            "images"
            "lib"
            ".well-known"
        ]

        directories
        |> List.map(fun x -> 
            Directory.GetDirectories(Path.Join(srcDir,x),"*",SearchOption.AllDirectories)
            |> List.ofArray
            |> fun a -> a @ [x])
        |> List.collect(fun x -> 
                x
                |> List.map(fun y -> y.Replace("_src" + Path.DirectorySeparatorChar.ToString(),"")))
        |> List.map(fun (dir:string) -> Path.Join(srcDir,dir),Path.Join(outputDir,dir))
        |> List.iter(fun (s,d) -> 
            let saveDir = Directory.CreateDirectory(d)
            
            Directory.GetFiles(s)
            |> Array.iter(fun file -> File.Copy(Path.GetFullPath(file),Path.Join(saveDir.FullName,Path.GetFileName(file)),true)))
        
        // Copy favicon & avatar
        File.Copy(Path.Join(srcDir,"favicon.ico"),Path.Join(outputDir,"favicon.ico"),true)
        File.Copy(Path.Join(srcDir,"avatar.png"),Path.Join(outputDir,"avatar.png"),true)
        File.Copy(Path.Join(srcDir,"art-profile.png"),Path.Join(outputDir,"art-profile.png"),true)

        // Copy contact cards
        File.Copy(Path.Join(srcDir,"vcard.vcf"),Path.Join(outputDir,"vcard.vcf"),true)
        File.Copy(Path.Join(srcDir,"mecard.txt"),Path.Join(outputDir,"mecard.txt"),true)

    let buildHomePage (blogPosts:Post array) (feedPosts:Post array) (responsePosts:Response array)= 
        let recentBlog = 
            blogPosts 
            |> Array.sortByDescending(fun x-> DateTime.Parse(x.Metadata.Date))
            |> Array.head

        let recentFeedPost = 
            feedPosts
            |> Array.sortByDescending(fun x -> DateTime.Parse(x.Metadata.Date))
            |> Array.head

        let recentResponsePost = 
            responsePosts
            |> Array.sortByDescending(fun x -> DateTime.Parse(x.Metadata.DateUpdated))
            |> Array.head

        // let recentPostsContent = generatePartial (recentPostsView recentPosts)
        let homePage = generate (homeView recentBlog recentFeedPost recentResponsePost) "default" "Home - Luis Quintanilla"
        File.WriteAllText(Path.Join(outputDir,"index.html"),homePage)

    let buildAboutPage () = 

        let aboutContent = convertFileToHtml (Path.Join(srcDir,"about.md")) |> contentView
        let aboutPage = generate aboutContent "default" "About - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"about")
        Directory.CreateDirectory(saveDir) |> ignore
        File.WriteAllText(Path.Join(saveDir,"index.html"), aboutPage)

    let buildStarterPackPage () = 

        let starterContent = convertFileToHtml (Path.Join(srcDir,"starter-packs.md")) |> contentView
        let starterPage = generate starterContent "default" "Starter Packs - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"feed","starter")
        Directory.CreateDirectory(saveDir) |> ignore
        File.WriteAllText(Path.Join(saveDir,"index.html"), starterPage)

    let buildBlogrollPage (links:Outline array) = 
        let blogRollContent = 
            links
            |> blogRollView

        let blogRollPage = generate blogRollContent "default" "Blogroll - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"feed","blogroll")
        Directory.CreateDirectory(saveDir) |> ignore    
        File.WriteAllText(Path.Join(saveDir,"index.html"), blogRollPage)

    let buildPodrollPage (links:Outline array) = 
        let podrollContent = 
            links
            |> podRollView

        let podrollPage = generate podrollContent "default" "Podroll - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"feed","podroll")
        Directory.CreateDirectory(saveDir) |> ignore
        File.WriteAllText(Path.Join(saveDir,"index.html"), podrollPage)

    let buildForumsPage (links:Outline array) = 
        let forumContent = 
            links
            |> forumsView

        let forumsPage = generate forumContent "default" "Forums - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"feed","forums")
        Directory.CreateDirectory(saveDir) |> ignore
        File.WriteAllText(Path.Join(saveDir,"index.html"), forumsPage)

    let buildYouTubeChannelsPage (links:Outline array) = 
        let ytContent = 
            links
            |> youTubeFeedView

        let ytFeedPage = generate ytContent "default" "YouTube Channels - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"feed","youtube")
        Directory.CreateDirectory(saveDir) |> ignore
        File.WriteAllText(Path.Join(saveDir,"index.html"), ytFeedPage)

    let buildAIStarterPackPage (links:Outline array) = 
        let aiStarterPackContent = 
            links
            |> aiStarterPackFeedView

        let ytFeedPage = generate aiStarterPackContent "default" "AI Starter Pack - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"feed","starter","ai")
        Directory.CreateDirectory(saveDir) |> ignore
        File.WriteAllText(Path.Join(saveDir,"index.html"), ytFeedPage)

    let buildIRLStackPage () = 
        let irlStackContent = Path.Join(srcDir,"uses.md") |> convertFileToHtml |> contentView
        let irlStackPage = generate irlStackContent "default" "In Real Life Stack - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"uses")
        Directory.CreateDirectory(saveDir) |> ignore   
        File.WriteAllText(Path.Join(saveDir,"index.html"), irlStackPage)

    let buildColophonPage () = 
        let colophonContent = Path.Join(srcDir,"colophon.md") |> convertFileToHtml |> contentView
        let colophonPage = generate colophonContent "default" "Colophon - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"colophon")
        Directory.CreateDirectory(saveDir) |> ignore               
        File.WriteAllText(Path.Join(saveDir,"index.html"), colophonPage)        

    let buildSubscribePage () = 
        let subscribeContent = Path.Join(srcDir,"subscribe.md") |> convertFileToHtml |> contentView
        let subscribePage = generate subscribeContent "default" "Subscribe - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"subscribe")
        Directory.CreateDirectory(saveDir) |> ignore
        File.WriteAllText(Path.Join(saveDir,"index.html"), subscribePage)        
    
    let buildContactPage () = 
        let contactContent = convertFileToHtml (Path.Join(srcDir,"contact.md")) |> contentView
        let contactPage = generate contactContent "default" "Contact - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"contact")
        Directory.CreateDirectory(saveDir) |> ignore
        File.WriteAllText(Path.Join(saveDir, "index.html"), contactPage)

    let buildOnlineRadioPage () = 
        let onlineRadioContent = convertFileToHtml (Path.Join(srcDir,"radio.md")) |> contentView
        let onlineRadioPage = generate onlineRadioContent "default" "Online Radio - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"radio")
        Directory.CreateDirectory(saveDir) |> ignore

        // Copy playlist file
        File.Copy(Path.Join(srcDir,"OnlineRadioPlaylist.m3u"),Path.Join(saveDir,"OnlineRadioPlaylist.m3u"),true)
        
        // Write out page
        File.WriteAllText(Path.Join(saveDir,"index.html"), onlineRadioPage)        

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

    let buildResponseFeedRssPage (posts: Response array) (saveFileName:string) = 

        let rssPage = 
            posts
            |> Array.sortByDescending(fun x -> DateTime.Parse(x.Metadata.DatePublished))
            |> generateReponseFeedRss
            |> string

        let saveDir = Path.Join(outputDir, "feed", "responses")
        File.WriteAllText(Path.Join(saveDir,$"{saveFileName}.xml"), rssPage)

    let buildFeedsOpml (links:Outline array) = 
        let feed = buildOpmlFeed "Luis Quintanilla Feeds" "https://www.luisquintanilla.me" links
        let saveDir = Path.Join(outputDir,"feed")
        // File.WriteAllText(Path.Join(saveDir,"index.xml"), feed.ToString())
        File.WriteAllText(Path.Join(saveDir,"index.opml"), feed.ToString())

    let buildBlogrollOpml (links:Outline array) = 
        let feed = buildOpmlFeed "Luis Quintanilla Blogroll" "https://www.luisquintanilla.me" links
        let saveDir = Path.Join(outputDir,"feed","blogroll")
        File.WriteAllText(Path.Join(saveDir,"index.xml"), feed.ToString())
        File.WriteAllText(Path.Join(saveDir,"index.opml"), feed.ToString())

    let buildPodrollOpml (links:Outline array) = 
        let feed = buildOpmlFeed "Luis Quintanilla Podroll" "https://www.lqdev.me" links
        let saveDir = Path.Join(outputDir,"feed","podroll")
        File.WriteAllText(Path.Join(saveDir,"index.xml"), feed.ToString())
        File.WriteAllText(Path.Join(saveDir,"index.opml"), feed.ToString())

    let buildForumsOpml (links:Outline array) = 
        let feed = buildOpmlFeed "Luis Quintanilla Forums" "https://www.lqdev.me" links
        let saveDir = Path.Join(outputDir,"feed","forums")
        File.WriteAllText(Path.Join(saveDir,"index.xml"), feed.ToString())
        File.WriteAllText(Path.Join(saveDir,"index.opml"), feed.ToString())

    let buildYouTubeOpml (links:Outline array) = 
        let feed = buildOpmlFeed "Luis Quintanilla YouTube Channels" "https://www.lqdev.me" links
        let saveDir = Path.Join(outputDir,"feed","youtube")
        File.WriteAllText(Path.Join(saveDir,"index.xml"), feed.ToString())
        File.WriteAllText(Path.Join(saveDir,"index.opml"), feed.ToString())

    let buildAIStarterPackOpml (links:Outline array) = 
        let feed = buildOpmlFeed "Luis Quintanilla AI Starter Pack" "https://www.lqdev.me" links
        let saveDir = Path.Join(outputDir,"feed","starter","ai")
        File.WriteAllText(Path.Join(saveDir,"index.xml"), feed.ToString())
        File.WriteAllText(Path.Join(saveDir,"index.opml"), feed.ToString())

    let buildPostPages (posts:Post array) = 
        let postPages = 
            posts
            |> Array.map(fun post -> 
                let postTitle = post.Metadata.Title
                let postContent = post.Content |> convertMdToHtml 
                let postView = blogPostView postTitle postContent
                post.FileName,generate postView "defaultindex" $"{post.Metadata.Title} - Luis Quintanilla")
        
        let rootSaveDir = Path.Join(outputDir,"posts")
        // Directory.CreateDirectory(saveDir) |> ignore
        postPages
        |> Array.iter(fun (fileName,html) ->
            let saveDir = Path.Join(rootSaveDir,fileName)
            Directory.CreateDirectory(saveDir) |> ignore
            // let saveFileName = sprintf "%s.html" fileName
            let savePath = Path.Join(saveDir,"index.html")
            File.WriteAllText(savePath,html))

    let buildPostArchive (posts:Post array) = 
        let postsPerPage = 10

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
    
    let buildTagsPages (posts: Post array) (notes: Post array) (responses: Response array) = 

        let taggedPosts = 
            processTaggedPost posts
            |> Array.map(fun (t,p) -> 
                let sortedPosts = p |> Array.sortByDescending(fun x -> DateTime.Parse(x.Metadata.Date))
                t, sortedPosts)
            |> Array.sortBy(fst)

        let taggedNotes = 
            processTaggedPost notes
            |> Array.map(fun (t,p) -> 
                let sortedNotes = p |> Array.sortByDescending(fun x -> DateTime.Parse(x.Metadata.Date))
                t,sortedNotes)
            |> Array.sortBy(fst)

        let taggedResponses = 
            processTaggedResponse responses
            |> Array.map(fun (t,p) -> 
                let sortedResponses = p |> Array.sortByDescending(fun x -> DateTime.Parse(x.Metadata.DatePublished))
                t,sortedResponses)
            |> Array.sortBy(fst)

        let postTagNames = 
            taggedPosts |> Array.map(fst)

        let noteTagNames = 
            taggedNotes |> Array.map(fst)

        let responseTagNames = 
            taggedResponses |> Array.map(fst)

        let tagNames = 
            postTagNames 
            |> Array.append noteTagNames
            |> Array.append responseTagNames
            |> Array.distinct
            |> Array.sort  

        let mutable taggedPostDict = 
            tagNames
            |> Array.map(fun n -> (n, {Posts=[||];Notes=[||];Responses=[||]}))
            |> Map

        taggedPosts
        |> Array.iter(fun (t,c) -> 
            taggedPostDict <- taggedPostDict.Add(t,{taggedPostDict[t] with Posts=c}))

        taggedNotes
        |> Array.iter(fun (t,c) -> 
            taggedPostDict <- taggedPostDict.Add(t,{taggedPostDict[t] with Notes=c}))

        taggedResponses
        |> Array.iter(fun (t,c) -> 
            taggedPostDict <- taggedPostDict.Add(t,{taggedPostDict[t] with Responses=c}))

        let combinedTaggedPosts = 
            taggedPostDict
            |> Map.toArray

        let tagPage = generate (allTagsView tagNames) "default" "Tags - Luis Quintanilla"

        let saveDir = Path.Join(outputDir,"tags")
        File.WriteAllText(Path.Join(saveDir,"index.html"), tagPage)

        combinedTaggedPosts
        |> Array.iter(fun (tag,pc)-> 
            let individualTagSaveDir = Path.Join(saveDir,tag.Trim().Replace("\"",""))
            Directory.CreateDirectory(individualTagSaveDir) |> ignore
            let individualTagPage = generate (individualTagView tag pc.Posts pc.Notes pc.Responses) "default" $"{tag} - Tags - Luis Quintanilla"
            File.WriteAllText(Path.Join(individualTagSaveDir,"index.html"),individualTagPage)
        )

    let buildEventPage () = 
        let events =  
            File.ReadAllText(Path.Join("Data","events.json"))
            |> JsonSerializer.Deserialize<Event array>
            |> Array.sortByDescending(fun x -> DateTime.Parse(x.Date))

        let eventPage = generate (eventView events) "default" "Events - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"events")
        Directory.CreateDirectory(saveDir) |> ignore
        File.WriteAllText(Path.Join(saveDir,"index.html"),eventPage)

    let filterFeedByPostType (posts: Post array) (postType: string) = 
        posts |> Array.filter(fun post -> post.Metadata.PostType = postType)

    let buildFeedPage (posts:Post array) (feedTitle:string) (saveFileName:string) =
        
        // Convert post markdown to HTML
        let parsedPosts = 
            posts 
            |> Array.map(fun post -> {post with Content = post.Content |> convertMdToHtml})
            |> Array.sortByDescending(fun post -> DateTime.Parse(post.Metadata.Date))

        // Generate aggregate feed
        let feedPage = generate (feedView parsedPosts) "defaultindex" feedTitle
        
        // Create directories
        let rootSaveDir = Path.Join(outputDir,"feed")
        // Directory.CreateDirectory(saveDir) |> ignore

        // Generate individual feed posts
        let generatePost (post:Post) = 
            let postView = feedPostView post |> feedPostViewWithBacklink
            post.FileName,generate postView "defaultindex" post.Metadata.Title 

        let writePost (fileName:string, html:string) = 
            let saveDir = Path.Join(rootSaveDir,fileName)
            Directory.CreateDirectory(saveDir) |> ignore
            let savePath = Path.Join(saveDir,"index.html")
            File.WriteAllText(savePath,html)

        parsedPosts
        |> Array.map(generatePost)
        |> Array.iter(writePost)        
    
        // Save feed
        File.WriteAllText(Path.Join(rootSaveDir, $"{saveFileName}.html"), feedPage)

    let buildPresentationsPage (presentations: Presentation array) = 
        let presentationPage = generate (presentationsView presentations) "defaultindex" "Presentations - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"presentations")
        Directory.CreateDirectory(saveDir) |> ignore
        File.WriteAllText(Path.Join(saveDir,"index.html"),presentationPage)

    let buildPresentationPages (presentations:Presentation array) = 
        presentations
        |> Array.iter(fun presentation ->
            let rootSaveDir = Path.Join(outputDir,"presentations")
            let html = presentationPageView presentation
            let presentationView = generate  html "presentation" $"Presentation | {presentation.Metadata.Title} | Luis Quintanilla"
            let saveDir = Path.Join(rootSaveDir,$"{presentation.FileName}")
            Directory.CreateDirectory(saveDir) |> ignore
            File.WriteAllText(Path.Join(saveDir,"index.html"),presentationView))

    let buildLiveStreamPage () = 
        let title = "Live Stream - Luis Quintanilla"
        let page = generate (liveStreamView title) "default" title
        let saveDir = Path.Join(outputDir,"live")
        Directory.CreateDirectory(saveDir) |> ignore

        File.WriteAllText(Path.Join(saveDir,"index.html"), page)

    let buildLiveStreamsPage (streams: Livestream array) = 
        let liveStreamsPage = generate (liveStreamsView streams) "defaultindex" "Live Stream Recordings - Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"streams")
        Directory.CreateDirectory(saveDir) |> ignore
        File.WriteAllText(Path.Join(saveDir,"index.html"),liveStreamsPage)

    let buildLiveStreamPages (streams:Livestream array) = 
        streams
        |> Array.iter(fun stream ->
            let rootSaveDir = Path.Join(outputDir,"streams")
            let html = liveStreamPageView {stream with Content = stream.Content |> convertMdToHtml}
            let streamView = generate  html "defaultindex" $"Live Stream Recording | {stream.Metadata.Title} | Luis Quintanilla"
            let saveDir = Path.Join(rootSaveDir,$"{stream.FileName}")
            Directory.CreateDirectory(saveDir) |> ignore
            File.WriteAllText(Path.Join(saveDir,"index.html"),streamView))

    let buildSnippetPage(snippets:Snippet array) = 
        let snippetsPage = generate (snippetsView snippets) "defaultindex" "Snippets | Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"snippets")
        Directory.CreateDirectory(saveDir) |> ignore
        File.WriteAllText(Path.Join(saveDir,"index.html"),snippetsPage)

    let buildSnippetPages (snippets:Snippet array) = 
        let rootSaveDir = Path.Join(outputDir,"snippets") 
        
        snippets
        |> Array.iter(fun snippet ->    
            let saveDir = Path.Join(rootSaveDir,snippet.FileName)
            Directory.CreateDirectory(saveDir) |> ignore
            let html = 
                contentViewWithTitle snippet.Metadata.Title (snippet.Content |> convertMdToHtml) 
            let snippetView = generate  html "defaultindex" $"Snippet | {snippet.Metadata.Title} | Luis Quintanilla"
            let saveFileName = Path.Join(saveDir,"index.html")
            File.WriteAllText(saveFileName,snippetView))

    let buildWikiPage(wikis:Wiki array) = 
        let wikisPage = generate (wikisView wikis) "defaultindex" "Wiki | Luis Quintanilla"
        let saveDir = Path.Join(outputDir,"wiki")
        Directory.CreateDirectory(saveDir) |> ignore
        File.WriteAllText(Path.Join(saveDir,"index.html"),wikisPage)

    let buildWikiPages (wikis:Wiki array) = 
        let rootSaveDir = Path.Join(outputDir,"wiki") 
        
        wikis
        |> Array.iter(fun wiki ->    
            let saveDir = Path.Join(rootSaveDir,wiki.FileName)
            Directory.CreateDirectory(saveDir) |> ignore
            let html = 
                contentViewWithTitle wiki.Metadata.Title (wiki.Content |> convertMdToHtml)
            let wikiView = generate  html "defaultindex" $"Wiki | {wiki.Metadata.Title} | Luis Quintanilla"
            let saveFileName = Path.Join(saveDir,"index.html")
            File.WriteAllText(saveFileName,wikiView))


    let buildRedirectPages (redirectDetails: RedirectDetails array) =
        redirectDetails
        |> Array.iter(fun (target:string,source:string,title:string) -> 
            let redirectPage = generateRedirect target title
            let saveDir = Path.Join(outputDir,source)
            Directory.CreateDirectory(saveDir) |> ignore
            File.WriteAllText(Path.Join(saveDir,"index.html"), redirectPage)
        )

    let buildLibraryPage (books:Book array) = 
        let saveDir = Path.Join(outputDir,"library")

        Directory.CreateDirectory(saveDir) |> ignore

        let html = books |> libraryView
        
        let libraryPage = generate html "defaultindex" $"Library | Luis Quintanilla"

        File.WriteAllText(Path.Join(saveDir,"index.html"),libraryPage)


    let buildBookPages (books:Book array) = 
        let rootSaveDir = Path.Join(outputDir,"library")
        
        books
        |> Array.iter(fun book -> 
            let saveDir = Path.Join(rootSaveDir,book.FileName)
            Directory.CreateDirectory(saveDir) |> ignore
            let html = 
                contentViewWithTitle book.Metadata.Title (book.Content |> convertMdToHtml)
            let bookPage = generate  html "defaultindex" $"Book | {book.Metadata.Title} | Luis Quintanilla"
            File.WriteAllText(Path.Join(saveDir,"index.html"),bookPage))

    let buildAlbumPage (albums: Album array) = 
        let saveDir = Path.Join(outputDir,"albums")

        Directory.CreateDirectory(saveDir) |> ignore

        let html = albums |> albumsPageView
        
        let albumPage = generate html "defaultindex" $"Albums | Luis Quintanilla"

        File.WriteAllText(Path.Join(saveDir,"index.html"),albumPage)


    let buildAlbumPages (albums: Album array) = 

        let rootSaveDir = Path.Join(outputDir,"albums")

        albums
        |> Array.iter(fun album -> 
            let saveDir = Path.Join(rootSaveDir,album.FileName)
            Directory.CreateDirectory(saveDir) |> ignore
                        
            let albumPage = generate (albumPageView album.Metadata.Images) "default" $"Album | {album.Metadata.Title} | Luis Quintanilla"
            
            File.WriteAllText(Path.Join(saveDir,"index.html"),albumPage))

    let buildResponsePage (posts:Response array) (feedTitle:string) (saveFileName:string) =
        
        // Convert post markdown to HTML
        let parsedPosts = 
            posts 
            |> Array.map(fun post -> {post with Content = post.Content |> convertMdToHtml})
            |> Array.sortByDescending(fun post -> DateTime.Parse(post.Metadata.DatePublished))

        // Generate aggregate feed
        let responsePage = generate (responseView parsedPosts) "defaultindex" feedTitle
        
        // Create directories
        let rootSaveDir = Path.Join(outputDir,"feed")
        // Directory.CreateDirectory(saveDir) |> ignore

        // Generate individual feed posts         

        let generatePost (post:Response) = 
            let postView = responsePostView post |> reponsePostViewWithBacklink
            let postType = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(post.Metadata.ResponseType)
            post.FileName,generate postView "defaultindex" $"{postType}: {post.Metadata.Title}"

        let writePost (fileName:string,html:string) = 
            let saveDir = Path.Join(rootSaveDir,fileName)
            Directory.CreateDirectory(saveDir) |> ignore
            let savePath = Path.Join(saveDir,"index.html")
            File.WriteAllText(savePath,html)            

        parsedPosts
        |> Array.map(generatePost)
        |> Array.map(writePost)
        |> ignore

        // Save feed
        File.WriteAllText(Path.Join(rootSaveDir, "responses", $"{saveFileName}.html"), responsePage)
