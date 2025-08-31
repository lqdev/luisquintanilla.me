module Loaders

    open System.IO
    open System.Text.Json
    open Domain
    open MarkdownService
    open ASTParsing

    let loadPosts (srcDir: string) = 
        let postPaths = 
            Directory.GetFiles(Path.Join(srcDir,"posts"))
        
        let posts = postPaths |> Array.map(MarkdownService.parsePost)
        
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

    let loadBooks (srcDir: string) = 
        let bookPaths = 
            Directory.GetFiles(Path.Join(srcDir,"reviews", "library"))

        let books = bookPaths |> Array.map(parseBook)

        books

    let loadAlbums (srcDir: string) = 
        let albumPaths = 
            Directory.GetFiles(Path.Join(srcDir,"media"))

        let albums = albumPaths |> Array.map(parseAlbum)

        albums