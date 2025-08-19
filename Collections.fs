module Collections

open System
open System.IO
open System.Text.Json
open Domain
open Giraffe.ViewEngine

// Collection Processor Pattern - Unified processing for all collection types
module CollectionProcessor =
    
    // Collection processor interface
    type CollectionProcessor = {
        LoadData: string -> Collection -> CollectionData
        GenerateHtmlPage: CollectionData -> XmlNode
        GenerateRssFeed: CollectionData -> string
        GenerateOpmlFile: CollectionData -> string
        GetOutputPaths: Collection -> CollectionPaths
    }
    
    // Calculate output paths for collection
    let getCollectionPaths (collection: Collection) : CollectionPaths =
        let baseDir = 
            match collection.CollectionType with
            | MediumFocused _ -> Path.Join("collections", collection.Id)
            | TopicFocused _ -> Path.Join("collections", "starter-packs", collection.Id)
            | Other _ -> Path.Join("collections", collection.Id)
        
        {
            HtmlPath = Path.Join(baseDir, "index.html")
            RssPath = Path.Join(baseDir, "index.rss") 
            OpmlPath = Path.Join(baseDir, "index.opml")
            DataPath = Path.Join("Data", collection.DataFile)
        }
    
    // Load collection data from JSON file
    let loadCollectionData (dataPath: string) (collection: Collection) : CollectionData =
        try
            let jsonContent = File.ReadAllText(dataPath)
            // Legacy support: load as Outline array first, then convert to CollectionItem array
            let outlines = JsonSerializer.Deserialize<Outline array>(jsonContent)
            let items = 
                outlines 
                |> Array.map (fun outline -> {
                    Title = outline.Title
                    Type = outline.Type
                    HtmlUrl = outline.HtmlUrl
                    XmlUrl = outline.XmlUrl
                    Description = None
                    Tags = None
                    Added = None
                })
            
            {
                Metadata = { collection with ItemCount = Some items.Length }
                Items = items
            }
        with
        | ex -> 
            printfn "Error loading collection data from %s: %s" dataPath ex.Message
            {
                Metadata = collection
                Items = [||]
            }
    
    // Generate HTML page for collection
    let generateCollectionPage (data: CollectionData) : XmlNode =
        let collection = data.Metadata
        let items = data.Items
        
        // Convert CollectionItem array to legacy Outline array for existing view compatibility
        let outlines = 
            items
            |> Array.map (fun item -> {
                Title = item.Title
                Type = item.Type
                HtmlUrl = item.HtmlUrl
                XmlUrl = item.XmlUrl
            })
        
        // Use consistent rollLinkView pattern from FeedViews
        let linkContent = 
            ul [] [
                for item in items do
                    li [] [
                        strong [] [
                            str $"{item.Title} - "
                        ]
                        a [ _href item.HtmlUrl ] [ Text "Website"]
                        str " / "
                        a [ _href item.XmlUrl ] [ Text "RSS Feed"]                    
                    ]
            ]
        
        div [ _class "mr-auto" ] [
            h2 [] [ Text collection.Title ]
            p [] [ Text collection.Description ]
            
            // Collection metadata
            if collection.ItemCount.IsSome then
                p [ _class "collection-meta text-muted" ] [
                    Text $"Contains {collection.ItemCount.Value} feeds"
                ]
            
            // OPML download information
            p [] [
                Text "You can subscribe to any of the individual feeds in your preferred RSS reader using the RSS feed links below. Want to subscribe to all of them? Use the "
                a [ _href (collection.UrlPath + "index.opml") ] [ Text "OPML file" ]
                Text " if your RSS reader supports "
                a [ _href "http://opml.org/" ] [ Text "OPML." ]
            ]
            
            // Feed list using established pattern
            linkContent
        ]
    
    // Generate RSS feed for collection (placeholder - could aggregate feeds)
    let generateCollectionRss (data: CollectionData) : string =
        // For now, collections don't have their own RSS content
        // This could be enhanced to aggregate latest posts from all feeds
        sprintf """<?xml version="1.0" encoding="UTF-8"?>
<rss version="2.0">
    <channel>
        <title>%s</title>
        <description>%s</description>
        <link>https://www.luisquintanilla.me%s</link>
    </channel>
</rss>""" data.Metadata.Title data.Metadata.Description data.Metadata.UrlPath
    
    // Generate OPML file for collection
    let generateCollectionOpml (data: CollectionData) : string =
        let collection = data.Metadata
        let items = data.Items
        
        let outlines = 
            items
            |> Array.map (fun item -> 
                sprintf """        <outline title="%s" type="%s" htmlUrl="%s" xmlUrl="%s" />"""
                    item.Title item.Type item.HtmlUrl item.XmlUrl)
            |> String.concat "\n"
        
        sprintf """<?xml version="1.0" encoding="UTF-8"?>
<opml version="2.0">
    <head>
        <title>%s</title>
        <ownerId>https://www.luisquintanilla.me</ownerId>
    </head>
    <body>
%s
    </body>
</opml>""" collection.Title outlines
    
    // Create unified collection processor
    let createCollectionProcessor (collection: Collection) : CollectionProcessor = 
        {
            LoadData = loadCollectionData
            GenerateHtmlPage = generateCollectionPage
            GenerateRssFeed = generateCollectionRss
            GenerateOpmlFile = generateCollectionOpml
            GetOutputPaths = getCollectionPaths
        }

// Collection Configuration System
module CollectionConfig =
    
    // Default collections configuration
    let getDefaultCollections () : Collection array =
        [|
            // Medium-focused collections
            {
                Id = "blogroll"
                Title = "Blogroll"
                Description = "Websites and blogs I follow regularly"
                CollectionType = MediumFocused "blogs"
                UrlPath = "/collections/blogroll/"
                DataFile = "blogroll.json"
                Tags = [| "blogs"; "reading"; "indieweb" |]
                LastUpdated = DateTime.Now.ToString("yyyy-MM-dd")
                ItemCount = None
            }
            {
                Id = "podroll"
                Title = "Podroll"
                Description = "Podcasts I find interesting and listen to regularly"
                CollectionType = MediumFocused "podcasts"
                UrlPath = "/collections/podroll/"
                DataFile = "podroll.json"
                Tags = [| "podcasts"; "audio"; "listening" |]
                LastUpdated = DateTime.Now.ToString("yyyy-MM-dd")
                ItemCount = None
            }
            {
                Id = "youtube"
                Title = "YouTube Channels"
                Description = "YouTube channels I subscribe to and recommend"
                CollectionType = MediumFocused "youtube"
                UrlPath = "/collections/youtube/"
                DataFile = "youtube.json"
                Tags = [| "youtube"; "video"; "channels" |]
                LastUpdated = DateTime.Now.ToString("yyyy-MM-dd")
                ItemCount = None
            }
            {
                Id = "forums"
                Title = "Forums"
                Description = "Online forums and discussion communities I participate in"
                CollectionType = MediumFocused "forums"
                UrlPath = "/collections/forums/"
                DataFile = "forums.json"
                Tags = [| "forums"; "community"; "discussion" |]
                LastUpdated = DateTime.Now.ToString("yyyy-MM-dd")
                ItemCount = None
            }
            
            // Topic-focused collections
            {
                Id = "ai"
                Title = "AI Starter Pack"
                Description = "Everything you need to stay informed about AI developments"
                CollectionType = TopicFocused "ai"
                UrlPath = "/collections/starter-packs/ai/"
                DataFile = "ai-starter-pack.json"
                Tags = [| "ai"; "machine-learning"; "technology" |]
                LastUpdated = DateTime.Now.ToString("yyyy-MM-dd")
                ItemCount = None
            }
        |]
    
    // Load collections configuration (for future JSON-based config)
    let loadCollectionsConfig () : Collection array =
        // For now, use default collections
        // TODO: Implement JSON-based configuration loading
        getDefaultCollections ()
    
    // Get navigation structure based on collections
    let getNavigationStructure (collections: Collection array) : NavigationStructure =
        let contentTypes = 
            collections 
            |> Array.filter (fun c -> match c.CollectionType with MediumFocused _ -> true | _ -> false)
            |> Array.toList
        
        let topicGuides = 
            collections 
            |> Array.filter (fun c -> match c.CollectionType with TopicFocused _ -> true | _ -> false)
            |> Array.toList
        
        let otherCollections = 
            collections 
            |> Array.filter (fun c -> match c.CollectionType with Other _ -> true | _ -> false)
            |> Array.toList
        
        {
            ContentTypes = {
                Title = "Content Types"
                Description = "Browse content by format - blogs, podcasts, videos, and forums"
                Collections = contentTypes
                Icon = Some "grid"
            }
            TopicGuides = {
                Title = "Topic Guides"
                Description = "Curated resources organized by subject matter"
                Collections = topicGuides
                Icon = Some "book"
            }
            OtherCollections = {
                Title = "Other"
                Description = "Additional browsing and discovery options"
                Collections = otherCollections
                Icon = Some "more"
            }
        }

// Builder Integration Functions
module CollectionBuilder =
    
    open CollectionProcessor
    open CollectionConfig
    
    // Process a single collection data (returns content for Builder.fs to handle page generation)
    let processCollectionData (collection: Collection) : CollectionData =
        try
            let processor = createCollectionProcessor collection
            let paths = processor.GetOutputPaths collection
            let fullDataPath = paths.DataPath
            
            // Load collection data
            let data = processor.LoadData fullDataPath collection
            data
            
        with
        | ex -> 
            printfn "❌ Error loading collection data %s: %s" collection.Title ex.Message
            {
                Metadata = collection
                Items = [||]
            }
    
    // Get all collections configuration
    let getAllCollections () : Collection array =
        loadCollectionsConfig ()
    
    // Generate OPML content for a collection
    let generateCollectionOpmlContent (data: CollectionData) : string =
        let processor = createCollectionProcessor data.Metadata
        processor.GenerateOpmlFile data
    
    // Generate RSS content for a collection  
    let generateCollectionRssContent (data: CollectionData) : string =
        let processor = createCollectionProcessor data.Metadata
        processor.GenerateRssFeed data
    
    // Get all collections for unified processing (used by Builder.fs)
    let buildCollections () : (Collection * CollectionData) array =
        try
            let collections = loadCollectionsConfig ()
            
            printfn "🔧 Loading collections..."
            
            let collectionData = 
                collections
                |> Array.map (fun collection ->
                    let data = processCollectionData collection
                    printfn "✅ Loaded collection: %s (%d items)" collection.Title (data.Items.Length)
                    (collection, data))
            
            printfn "✅ Collections loading complete (%d collections)" collections.Length
            collectionData
            
        with
        | ex -> 
            printfn "❌ Error in collections processing: %s" ex.Message
            [||]

// Legacy Compatibility Functions
module LegacyCompatibility =
    
    // Convert legacy Outline array to CollectionItem array
    let outlinesToCollectionItems (outlines: Outline array) : CollectionItem array =
        outlines
        |> Array.map (fun outline -> {
            Title = outline.Title
            Type = outline.Type
            HtmlUrl = outline.HtmlUrl
            XmlUrl = outline.XmlUrl
            Description = None
            Tags = None
            Added = None
        })
    
    // Convert CollectionItem array back to Outline array for legacy functions
    let collectionItemsToOutlines (items: CollectionItem array) : Outline array =
        items
        |> Array.map (fun item -> {
            Title = item.Title
            Type = item.Type
            HtmlUrl = item.HtmlUrl
            XmlUrl = item.XmlUrl
        })
