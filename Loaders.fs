module Loaders

    open System.IO
    open System.Text.Json
    open Domain
    open MarkdownService

    let loadPosts (srcDir: string) = 
        let postPaths = 
            Directory.GetFiles(Path.Join(srcDir,"posts"))
        
        let posts = postPaths |> Array.map(parsePost)
        
        posts

    let loadLiveStreams (srcDir: string) =
        let streamPaths = 
            Directory.GetFiles(Path.Join(srcDir,"streams"))
        
        let streams = streamPaths |> Array.map(parseLivestream)
        
        streams

    let loadSnippets (srcDir: string) = 
        let snippetPaths = 
            Directory.GetFiles(Path.Join(srcDir,"snippets"))
        
        let snippets = snippetPaths |> Array.map(parseSnippet)
        
        snippets

    let loadWikis (srcDir: string) = 
        let wikiPaths = 
            Directory.GetFiles(Path.Join(srcDir,"wiki"))
        
        let wikis = wikiPaths |> Array.map(parseWiki)
        
        wikis        

    let loadFeedLinks (srcDir: string) = 
        let links =  
            File.ReadAllText(Path.Join("Data","feeds.json"))
            |> JsonSerializer.Deserialize<Outline array>
    
        links

    let loadBlogrollLinks () = 
        let links =  
            File.ReadAllText(Path.Join("Data","blogroll.json"))
            |> JsonSerializer.Deserialize<Outline array>
    
        links

    let loadPodrollLinks () = 
        let links =  
            File.ReadAllText(Path.Join("Data","podroll.json"))
            |> JsonSerializer.Deserialize<Outline array>
    
        links

    let loadForumsLinks () = 
        let links =  
            File.ReadAllText(Path.Join("Data","forums.json"))
            |> JsonSerializer.Deserialize<Outline array>
    
        links

    let loadYouTubeLinks () = 
        let links =  
            File.ReadAllText(Path.Join("Data","youtube.json"))
            |> JsonSerializer.Deserialize<Outline array>
    
        links

    let loadAIStarterPackLinks () =
        let links =  
            File.ReadAllText(Path.Join("Data","ai-starter-pack.json"))
            |> JsonSerializer.Deserialize<Outline array>
    
        links

    let loadRedirects () = 
        let (redirects:RedirectDetails array) = 
            [|
                // Legacy post redirects
                ("/posts/client-credentials-authentication-csharp/","/2017/12/25/client-credentials-authentication-csharp","Client Credentials Auth")
                ("/posts/alternatives-to-whatsapp","/2021/01/09/alternatives-to-whatsapp/","Alternatives to WhatsApp")
                ("/posts/case-fsharp-machine-learning/","/2018/12/14/case-fsharp-machine-learning","The Case for Doing Machine Learning with F#")
                ("/posts/mlnet-classification-fsharp/","/2018/06/13/mlnet-classification-fsharp/","Classification with F# ML.NET Models")
                
                // URL Alignment Phase 2: Content migrations
                ("/snippets/", "/resources/snippets/", "Code Snippets")
                ("/wiki/", "/resources/wiki/", "Knowledge Base")  
                ("/presentations/", "/resources/presentations/", "Presentations")
                ("/library/", "/reviews/", "Book Reviews")
                
                // URL Alignment Phase 2: Feed relocations
                ("/feed/snippets.xml", "/resources/snippets/feed.xml", "Snippets Feed")
                ("/feed/wiki.xml", "/resources/wiki/feed.xml", "Wiki Feed")
                ("/feed/presentations.xml", "/resources/presentations/feed.xml", "Presentations Feed")
                ("/feed/library.xml", "/reviews/feed.xml", "Reviews Feed")
                ("/feed/albums.xml", "/media/feed.xml", "Media Feed")
            |]

        redirects

    let loadBooks (srcDir: string) = 
        let bookPaths = 
            Directory.GetFiles(Path.Join(srcDir,"reviews"))

        let books = bookPaths |> Array.map(parseBook)

        books

    let loadAlbums (srcDir: string) = 
        let albumPaths = 
            Directory.GetFiles(Path.Join(srcDir,"media"))

        let albums = albumPaths |> Array.map(parseAlbum)

        albums