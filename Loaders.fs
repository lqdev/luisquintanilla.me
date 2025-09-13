module Loaders

    open System.IO
    open System.Text.Json
    open Domain
    open MarkdownService
    open ASTParsing
    open GenericBuilder

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

    let loadFeed (srcDir: string) =
        // Load notes using AST-based system (same as Program.fs)
        let noteFiles = 
            Directory.GetFiles(Path.Join(srcDir, "feed"))
            |> Array.filter (fun f -> f.EndsWith(".md"))
            |> Array.toList
        let processor = GenericBuilder.NoteProcessor.create()
        let feedData = GenericBuilder.buildContentWithFeeds processor noteFiles
        feedData |> List.map (fun item -> item.Content) |> List.toArray

    let loadReponses (srcDir: string) =
        // Load responses using AST-based system (same as Program.fs)
        let responseFiles = 
            Directory.GetFiles(Path.Join(srcDir, "responses"))
            |> Array.filter (fun f -> f.EndsWith(".md"))
            |> Array.toList
        let processor = GenericBuilder.ResponseProcessor.create()
        let feedData = GenericBuilder.buildContentWithFeeds processor responseFiles
        feedData |> List.map (fun item -> item.Content) |> List.toArray