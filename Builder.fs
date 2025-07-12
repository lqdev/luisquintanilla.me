module Builder

    open System
    open System.Globalization
    open System.IO
    open System.Linq
    open System.Text.Json
    open System.Xml.Linq
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
        if Directory.Exists(outputDir) then
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

    // AST-based snippet processing using GenericBuilder infrastructure
    let buildSnippets() = 
        let snippetFiles = 
            Directory.GetFiles(Path.Join(srcDir, "snippets"))
            |> Array.filter (fun f -> f.EndsWith(".md"))
            |> Array.toList
        
        let processor = GenericBuilder.SnippetProcessor.create()
        let feedData = GenericBuilder.buildContentWithFeeds processor snippetFiles
        
        // Generate individual snippet pages
        feedData
        |> List.iter (fun item ->
            let snippet = item.Content
            let saveDir = Path.Join(outputDir, "snippets", snippet.FileName)
            Directory.CreateDirectory(saveDir) |> ignore
            
            let html = contentViewWithTitle snippet.Metadata.Title (snippet.Content |> convertMdToHtml)
            let snippetView = generate html "defaultindex" $"Snippet | {snippet.Metadata.Title} | Luis Quintanilla"
            let saveFileName = Path.Join(saveDir, "index.html")
            File.WriteAllText(saveFileName, snippetView))
        
        // Generate snippet index page using existing view for now
        let snippets = feedData |> List.map (fun item -> item.Content) |> List.toArray
        let snippetIndexHtml = generate (snippetsView snippets) "defaultindex" "Snippets | Luis Quintanilla"
        let indexSaveDir = Path.Join(outputDir, "snippets")
        Directory.CreateDirectory(indexSaveDir) |> ignore
        File.WriteAllText(Path.Join(indexSaveDir, "index.html"), snippetIndexHtml)
        
        // Return feed data for potential RSS generation
        feedData

    // AST-based wiki processing using GenericBuilder infrastructure  
    let buildWikis() = 
        let wikiFiles = 
            Directory.GetFiles(Path.Join(srcDir, "wiki"))
            |> Array.filter (fun f -> f.EndsWith(".md"))
            |> Array.toList
        
        let processor = GenericBuilder.WikiProcessor.create()
        let feedData = GenericBuilder.buildContentWithFeeds processor wikiFiles
        
        // Generate individual wiki pages
        feedData
        |> List.iter (fun item ->
            let wiki = item.Content
            let saveDir = Path.Join(outputDir, "wiki", wiki.FileName)
            Directory.CreateDirectory(saveDir) |> ignore
            
            let html = contentViewWithTitle wiki.Metadata.Title (wiki.Content |> convertMdToHtml)
            let wikiView = generate html "defaultindex" $"{wiki.Metadata.Title} | Wiki | Luis Quintanilla"
            let saveFileName = Path.Join(saveDir, "index.html")
            File.WriteAllText(saveFileName, wikiView))
        
        // Generate wiki index page using existing view
        let wikis = feedData |> List.map (fun item -> item.Content) |> List.toArray |> Array.sortBy(fun x -> x.Metadata.Title)
        let wikiIndexHtml = generate (wikisView wikis) "defaultindex" "Wiki | Luis Quintanilla"
        let indexSaveDir = Path.Join(outputDir, "wiki")
        Directory.CreateDirectory(indexSaveDir) |> ignore
        File.WriteAllText(Path.Join(indexSaveDir, "index.html"), wikiIndexHtml)
        
        // Return feed data for potential RSS generation
        feedData

    // AST-based presentation processing using GenericBuilder infrastructure
    let buildPresentations() = 
        let presentationFiles = 
            Directory.GetFiles(Path.Join(srcDir, "presentations"))
            |> Array.filter (fun f -> f.EndsWith(".md"))
            |> Array.toList
        
        let processor = GenericBuilder.PresentationProcessor.create()
        let feedData = GenericBuilder.buildContentWithFeeds processor presentationFiles
        
        // Generate individual presentation pages
        feedData
        |> List.iter (fun item ->
            let presentation = item.Content
            let saveDir = Path.Join(outputDir, "presentations", presentation.FileName)
            Directory.CreateDirectory(saveDir) |> ignore
            
            // Preserve Reveal.js integration - use existing presentation processing for now
            let html = contentViewWithTitle presentation.Metadata.Title (presentation.Content |> convertMdToHtml)
            let presentationView = generate html "defaultindex" $"{presentation.Metadata.Title} | Presentation | Luis Quintanilla"
            let saveFileName = Path.Join(saveDir, "index.html")
            File.WriteAllText(saveFileName, presentationView))
        
        // Generate presentation index page
        let presentations = feedData |> List.map (fun item -> item.Content) |> List.toArray
        let presentationIndexHtml = generate (presentationsView presentations) "defaultindex" "Presentations | Luis Quintanilla"
        let indexSaveDir = Path.Join(outputDir, "presentations")
        Directory.CreateDirectory(indexSaveDir) |> ignore
        File.WriteAllText(Path.Join(indexSaveDir, "index.html"), presentationIndexHtml)
        
        // Generate RSS feed
        let rssItems = feedData |> List.choose (fun item -> item.RssXml)
        if not (List.isEmpty rssItems) then
            // Create RSS channel for presentations
            let latestPresentation = 
                presentations 
                |> Array.filter (fun p -> not (String.IsNullOrEmpty(p.Metadata.Date)))
                |> Array.sortByDescending (fun p -> DateTime.Parse(p.Metadata.Date))
                |> Array.tryHead
            
            let lastPubDate = 
                match latestPresentation with
                | Some p -> p.Metadata.Date
                | None -> DateTime.Now.ToString("yyyy-MM-dd")
            
            let channel = 
                XElement(XName.Get "rss",
                    XAttribute(XName.Get "version", "2.0"),
                    XElement(XName.Get "channel",
                        XElement(XName.Get "title", "Luis Quintanilla Presentations"),
                        XElement(XName.Get "link", "https://www.luisquintanilla.me/presentations"),
                        XElement(XName.Get "description", "Presentations by Luis Quintanilla"),
                        XElement(XName.Get "lastPubDate", lastPubDate),
                        XElement(XName.Get "language", "en")))
            
            // Add RSS items to channel
            let channelElement = channel.Descendants(XName.Get "channel").First()
            channelElement.Add(rssItems |> List.toArray)
            
            // Save RSS feed
            let feedSaveDir = Path.Join(outputDir, "presentations", "feed")
            Directory.CreateDirectory(feedSaveDir) |> ignore
            let rssContent = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + Environment.NewLine + channel.ToString()
            File.WriteAllText(Path.Join(feedSaveDir, "index.xml"), rssContent)
        
        // Return feed data for potential RSS generation
        feedData

    // AST-based book processing using GenericBuilder infrastructure
    let buildBooks() = 
        let bookFiles = 
            Directory.GetFiles(Path.Join(srcDir, "library"))
            |> Array.filter (fun f -> f.EndsWith(".md"))
            |> Array.toList
        
        let processor = GenericBuilder.BookProcessor.create()
        let feedData = GenericBuilder.buildContentWithFeeds processor bookFiles
        
        // Generate individual book pages
        feedData
        |> List.iter (fun item ->
            let book = item.Content
            let saveDir = Path.Join(outputDir, "library", book.FileName)
            Directory.CreateDirectory(saveDir) |> ignore
            
            let html = contentViewWithTitle book.Metadata.Title (book.Content |> convertMdToHtml)
            let bookView = generate html "defaultindex" $"{book.Metadata.Title} | Library | Luis Quintanilla"
            let saveFileName = Path.Join(saveDir, "index.html")
            File.WriteAllText(saveFileName, bookView))
        
        // Generate library index page using existing libraryView
        let books = feedData |> List.map (fun item -> item.Content) |> List.toArray
        let libraryIndexHtml = generate (libraryView books) "defaultindex" "Library | Luis Quintanilla"
        let indexSaveDir = Path.Join(outputDir, "library")
        Directory.CreateDirectory(indexSaveDir) |> ignore
        File.WriteAllText(Path.Join(indexSaveDir, "index.html"), libraryIndexHtml)
        
        // Generate RSS feed for books
        let rssItems = feedData |> List.choose (fun item -> item.RssXml)
        if not (List.isEmpty rssItems) then
            // Create RSS channel for books
            let latestBook = 
                books 
                |> Array.filter (fun b -> not (String.IsNullOrEmpty(b.Metadata.DatePublished)))
                |> Array.sortByDescending (fun b -> DateTime.Parse(b.Metadata.DatePublished))
                |> Array.tryHead
            
            let lastPubDate = 
                match latestBook with
                | Some b -> b.Metadata.DatePublished
                | None -> DateTime.Now.ToString("yyyy-MM-dd")
            
            let channel = 
                XElement(XName.Get "rss",
                    XAttribute(XName.Get "version", "2.0"),
                    XElement(XName.Get "channel",
                        XElement(XName.Get "title", "Luis Quintanilla Library"),
                        XElement(XName.Get "link", "https://www.luisquintanilla.me/library"),
                        XElement(XName.Get "description", "Books read by Luis Quintanilla"),
                        XElement(XName.Get "lastPubDate", lastPubDate),
                        XElement(XName.Get "language", "en")))
            
            // Add RSS items to channel
            let channelElement = channel.Descendants(XName.Get "channel").First()
            channelElement.Add(rssItems |> List.toArray)
            
            // Save RSS feed
            let feedSaveDir = Path.Join(outputDir, "library", "feed")
            Directory.CreateDirectory(feedSaveDir) |> ignore
            let rssContent = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + Environment.NewLine + channel.ToString()
            File.WriteAllText(Path.Join(feedSaveDir, "index.xml"), rssContent)
        
        // Return feed data for potential RSS generation
        feedData

    // AST-based post processing using GenericBuilder infrastructure
    let buildPosts() = 
        let postFiles = 
            Directory.GetFiles(Path.Join(srcDir, "posts"))
            |> Array.filter (fun f -> f.EndsWith(".md"))
            |> Array.toList
        
        let processor = GenericBuilder.PostProcessor.create()
        let feedData = GenericBuilder.buildContentWithFeeds processor postFiles
        
        // Generate individual post pages
        feedData
        |> List.iter (fun item ->
            let post = item.Content
            let saveDir = Path.Join(outputDir, "posts", post.FileName)
            Directory.CreateDirectory(saveDir) |> ignore
            
            let html = blogPostView post.Metadata.Title (post.Content |> convertMdToHtml)
            let postView = generate html "defaultindex" $"{post.Metadata.Title} - Luis Quintanilla"
            let saveFileName = Path.Join(saveDir, "index.html")
            File.WriteAllText(saveFileName, postView))
        
        // Generate post archive pages (paginated)
        let posts = feedData |> List.map (fun item -> item.Content) |> List.toArray
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
        
        // Generate RSS feed for posts
        let rssItems = feedData |> List.choose (fun item -> item.RssXml)
        if not (List.isEmpty rssItems) then
            // Create RSS channel for posts using existing pattern
            let latestPost = 
                posts 
                |> Array.sortByDescending (fun p -> DateTime.Parse(p.Metadata.Date))
                |> Array.tryHead
            
            let lastPubDate = 
                match latestPost with
                | Some p -> p.Metadata.Date
                | None -> DateTime.Now.ToString("yyyy-MM-dd")
            
            let channel = 
                XElement(XName.Get "rss",
                    XAttribute(XName.Get "version", "2.0"),
                    XElement(XName.Get "channel",
                        XElement(XName.Get "title", "Luis Quintanilla Posts"),
                        XElement(XName.Get "link", "https://www.luisquintanilla.me/posts"),
                        XElement(XName.Get "description", "Blog posts by Luis Quintanilla"),
                        XElement(XName.Get "lastPubDate", lastPubDate),
                        XElement(XName.Get "language", "en")))
            
            // Add RSS items to channel
            let channelElement = channel.Descendants(XName.Get "channel").First()
            channelElement.Add(rssItems |> List.toArray)
            
            // Save RSS feed
            let feedSaveDir = Path.Join(outputDir, "posts", "feed")
            Directory.CreateDirectory(feedSaveDir) |> ignore
            let rssContent = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + Environment.NewLine + channel.ToString()
            File.WriteAllText(Path.Join(feedSaveDir, "index.xml"), rssContent)
        
        // Return feed data for potential RSS generation
        feedData

    // AST-based notes processing using GenericBuilder infrastructure
    let buildNotes() = 
        let noteFiles = 
            Directory.GetFiles(Path.Join(srcDir, "feed"))
            |> Array.filter (fun f -> f.EndsWith(".md"))
            |> Array.toList
        
        let processor = GenericBuilder.NoteProcessor.create()
        let feedData = GenericBuilder.buildContentWithFeeds processor noteFiles
        
        // Generate individual note pages
        feedData
        |> List.iter (fun item ->
            let note = item.Content
            let saveDir = Path.Join(outputDir, "feed", note.FileName)
            Directory.CreateDirectory(saveDir) |> ignore
            
            let html = feedPostView note |> feedPostViewWithBacklink
            let noteView = generate html "defaultindex" note.Metadata.Title
            let saveFileName = Path.Join(saveDir, "index.html")
            File.WriteAllText(saveFileName, noteView))
        
        // Generate notes index page using existing feedView
        let notes = feedData |> List.map (fun item -> item.Content) |> List.toArray
        let sortedNotes = notes |> Array.sortByDescending(fun (x: Post) -> DateTime.Parse(x.Metadata.Date))
        let notesIndexHtml = generate (feedView sortedNotes) "defaultindex" "Main Feed - Luis Quintanilla"
        let indexSaveDir = Path.Join(outputDir, "feed")
        Directory.CreateDirectory(indexSaveDir) |> ignore
        File.WriteAllText(Path.Join(indexSaveDir, "index.html"), notesIndexHtml)
        
        // Generate RSS feed for notes
        let rssItems = feedData |> List.choose (fun item -> item.RssXml)
        if not (List.isEmpty rssItems) then
            // Create RSS channel for notes using existing pattern
            let latestNote = 
                notes 
                |> Array.sortByDescending (fun (n: Post) -> DateTime.Parse(n.Metadata.Date))
                |> Array.tryHead
            
            let lastPubDate = 
                match latestNote with
                | Some n -> n.Metadata.Date
                | None -> DateTime.Now.ToString("yyyy-MM-dd")
            
            let channel = 
                XElement(XName.Get "rss",
                    XAttribute(XName.Get "version", "2.0"),
                    XElement(XName.Get "channel",
                        XElement(XName.Get "title", "Luis Quintanilla Main Feed"),
                        XElement(XName.Get "link", "https://www.luisquintanilla.me/feed"),
                        XElement(XName.Get "description", "Notes and updates by Luis Quintanilla"),
                        XElement(XName.Get "lastPubDate", lastPubDate),
                        XElement(XName.Get "language", "en")))
            
            // Add RSS items to channel
            let channelElement = channel.Descendants(XName.Get "channel").First()
            channelElement.Add(rssItems |> List.toArray)
            
            // Save RSS feed
            let feedSaveDir = Path.Join(outputDir, "feed")
            Directory.CreateDirectory(feedSaveDir) |> ignore
            let rssContent = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + Environment.NewLine + channel.ToString()
            File.WriteAllText(Path.Join(feedSaveDir, "index.xml"), rssContent)
        
        // Return feed data for potential RSS generation
        feedData

    // AST-based responses processing using GenericBuilder infrastructure
    let buildResponses() = 
        let responseFiles = 
            Directory.GetFiles(Path.Join(srcDir, "responses"))
            |> Array.filter (fun f -> f.EndsWith(".md"))
            |> Array.toList
        
        let processor = GenericBuilder.ResponseProcessor.create()
        let feedData = GenericBuilder.buildContentWithFeeds processor responseFiles
        
        // Generate individual response pages
        feedData
        |> List.iter (fun item ->
            let response = item.Content
            let saveDir = Path.Join(outputDir, "feed", response.FileName)
            Directory.CreateDirectory(saveDir) |> ignore
            
            let html = responsePostView response |> reponsePostViewWithBacklink
            let responseView = generate html "defaultindex" response.Metadata.Title
            let saveFileName = Path.Join(saveDir, "index.html")
            File.WriteAllText(saveFileName, responseView))
        
        // Generate responses index page
        let responses = feedData |> List.map (fun item -> item.Content) |> List.toArray
        let sortedResponses = responses |> Array.sortByDescending(fun (x: Response) -> DateTime.Parse(x.Metadata.DatePublished))
        
        // Generate RSS feed for responses
        let rssItems = feedData |> List.choose (fun item -> item.RssXml)
        if not (List.isEmpty rssItems) then
            let latestResponse = 
                responses 
                |> Array.sortByDescending (fun (r: Response) -> DateTime.Parse(r.Metadata.DatePublished))
                |> Array.tryHead
            
            let lastPubDate = 
                match latestResponse with
                | Some r -> r.Metadata.DatePublished
                | None -> DateTime.Now.ToString("yyyy-MM-dd")
            
            let channel = 
                XElement(XName.Get "rss",
                    XAttribute(XName.Get "version", "2.0"),
                    XElement(XName.Get "channel",
                        XElement(XName.Get "title", "Luis Quintanilla Responses"),
                        XElement(XName.Get "link", "https://www.luisquintanilla.me/feed/responses"),
                        XElement(XName.Get "description", "IndieWeb responses by Luis Quintanilla"),
                        XElement(XName.Get "lastPubDate", lastPubDate),
                        XElement(XName.Get "language", "en")))
            
            // Add RSS items to channel
            let channelElement = channel.Descendants(XName.Get "channel").First()
            channelElement.Add(rssItems |> List.toArray)
            
            // Save RSS feed in expected location
            let feedSaveDir = Path.Join(outputDir, "feed", "responses")
            Directory.CreateDirectory(feedSaveDir) |> ignore
            let rssContent = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + Environment.NewLine + channel.ToString()
            File.WriteAllText(Path.Join(feedSaveDir, "index.xml"), rssContent)
        
        // Return feed data for potential integration
        feedData
