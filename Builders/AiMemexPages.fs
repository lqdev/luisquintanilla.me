module AiMemexPagesBuilder

    open System
    open System.IO
    open Domain
    open MarkdownService
    open ViewGenerator
    open BuilderCommon

    let loadAiMemexFeedData() = 
        let aiMemexFiles = 
            let dir = Path.Join(srcDir, "resources", "ai-memex")
            if Directory.Exists(dir) then
                Directory.GetFiles(dir)
                |> Array.filter (fun f -> f.EndsWith(".md"))
                |> Array.toList
            else
                []
        
        let processor = GenericBuilder.AiMemexProcessor.create()
        GenericBuilder.buildContentWithFeeds processor aiMemexFiles

    // AST-based AI Memex page generation from pre-loaded feed data
    let buildAiMemexPages (feedData: GenericBuilder.FeedData<AiMemex> list) (crossContentItems: UnifiedFeeds.UnifiedFeedItem list) = 
        
        // Build knowledge graph from all entries
        let allEntries = feedData |> List.map (fun item -> item.Content) |> List.toArray
        let slugToTitle = allEntries |> Array.map (fun e -> e.FileName, e.Metadata.Title) |> Map.ofArray
        
        // Entity extraction (uses cache; no-op without GITHUB_TOKEN)
        let chatClient = EntityExtraction.createChatClient ()
        let entryPairs = allEntries |> Array.map (fun e -> (e.FileName, e.Content))
        let extractions =
            EntityExtraction.extractAll chatClient entryPairs "https://www.lqdev.me"
        
        let graph = KnowledgeGraph.buildGraph allEntries extractions
        
        // Save graph.json
        KnowledgeGraph.saveGraphJson outputDir graph |> ignore
        
        // Serialize RDF (Turtle + JSON-LD)
        RdfSerializer.serializeGraph graph outputDir |> ignore
        
        // Generate individual pages
        feedData
        |> List.iter (fun item ->
            let entry = item.Content
            let saveDir = Path.Join(outputDir, "resources", "ai-memex", entry.FileName)
            Directory.CreateDirectory(saveDir) |> ignore
            
            let entryTags = 
                if String.IsNullOrEmpty(entry.Metadata.Tags) then [||]
                else entry.Metadata.Tags.Split(',') |> Array.map (fun s -> s.Trim())
            
            // Resolve wikilinks in markdown source before rendering
            let contentToRender =
                match entry.MarkdownSource with
                | Some md ->
                    let (resolved, _) = KnowledgeGraph.resolveWikilinks slugToTitle md
                    resolved
                | None -> entry.Content
            
            // Get graph data for this entry
            let backlinks = 
                Map.tryFind entry.FileName graph.Backlinks 
                |> Option.defaultValue [||]
            let relatedEntries = 
                Map.tryFind entry.FileName graph.RelatedEntries 
                |> Option.defaultValue [||]
            let jsonLd = KnowledgeGraph.generateEntryJsonLd entry graph extractions
            
            // Find cross-content-type related items
            let crossContent =
                KnowledgeGraph.findCrossContentRelated entryTags entry.FileName crossContentItems
            
            // Get entity nodes for this entry
            let entryEntityNodes =
                graph.EntityNodes
                |> Array.filter (fun en -> en.MentionedIn |> Array.contains entry.FileName)
            
            let html = LayoutViews.aiMemexPageView entry.Metadata.Title (contentToRender |> convertMdToHtml) entry.Metadata.PublishedDate entry.Metadata.LastUpdatedDate entry.FileName entryTags entry.Metadata.EntryType entry.Metadata.Description entry.Metadata.RelatedSkill entry.Metadata.SourceProject backlinks relatedEntries jsonLd crossContent entryEntityNodes
            let page = generate html "defaultindex" $"{entry.Metadata.Title} | AI Memex | Luis Quintanilla"
            let saveFileName = Path.Join(saveDir, "index.html")
            File.WriteAllText(saveFileName, page))
        
        // Generate index page
        let entries = feedData |> List.map (fun item -> item.Content) |> List.toArray |> Array.sortByDescending(fun x -> 
            if not (String.IsNullOrEmpty(x.Metadata.PublishedDate)) then DateTimeOffset.Parse(x.Metadata.PublishedDate)
            elif not (String.IsNullOrEmpty(x.Metadata.LastUpdatedDate)) then DateTimeOffset.Parse(x.Metadata.LastUpdatedDate)
            else DateTimeOffset.MinValue)
        let collectionJsonLd = KnowledgeGraph.generateCollectionJsonLd entries
        let indexHtml = generate (CollectionViews.aiMemexView entries collectionJsonLd) "defaultindex" "AI Memex | Luis Quintanilla"
        let indexSaveDir = Path.Join(outputDir, "resources", "ai-memex")
        Directory.CreateDirectory(indexSaveDir) |> ignore
        File.WriteAllText(Path.Join(indexSaveDir, "index.html"), indexHtml)
        
        // Generate dedicated knowledge graph page
        let graphHtml = generate (CollectionViews.aiMemexGraphView entries.Length) "defaultindex" "Knowledge Graph | AI Memex | Luis Quintanilla"
        let graphSaveDir = Path.Join(outputDir, "resources", "ai-memex", "graph")
        Directory.CreateDirectory(graphSaveDir) |> ignore
        File.WriteAllText(Path.Join(graphSaveDir, "index.html"), graphHtml)
