module KnowledgeGraph

open System
open System.IO
open System.Text.Json
open System.Text.RegularExpressions
open Domain

// --- Types ---

type EdgeType = 
    | Explicit
    | Wikilink
    | TagOverlap
    | SameProject
    | SameEntryTypeTag

type GraphNode = {
    Id: string
    Title: string
    EntryType: string
    Tags: string array
    SourceProject: string
    Url: string
    Description: string
}

type GraphEdge = {
    Source: string
    Target: string
    EdgeType: EdgeType
    Weight: float
    Reason: string
}

type BacklinkData = {
    Slug: string
    Title: string
    Reason: string
}

type RelatedEntryData = {
    Slug: string
    Title: string
    EntryType: string
    Reason: string
    Score: float
}

type CrossContentItem = {
    Title: string
    Url: string
    ContentType: string
    Tags: string array
    OverlapReason: string
}

type KnowledgeGraph = {
    Nodes: GraphNode array
    Edges: GraphEdge array
    Backlinks: Map<string, BacklinkData array>
    RelatedEntries: Map<string, RelatedEntryData array>
}

// --- Helpers ---

let private splitCsv (s: string) =
    if String.IsNullOrWhiteSpace(s) then [||]
    else s.Split(',') |> Array.map (fun x -> x.Trim()) |> Array.filter (fun x -> x.Length > 0)

let private normalizeTags (tags: string array) =
    tags |> Array.map (fun t -> t.ToLowerInvariant().Trim()) |> Set.ofArray

let private jaccardSimilarity (a: Set<string>) (b: Set<string>) =
    if Set.isEmpty a && Set.isEmpty b then 0.0
    else
        let intersection = Set.intersect a b |> Set.count |> float
        let union = Set.union a b |> Set.count |> float
        intersection / union

// --- Node extraction ---

let toGraphNode (entry: AiMemex) : GraphNode =
    {
        Id = entry.FileName
        Title = entry.Metadata.Title
        EntryType = if String.IsNullOrEmpty(entry.Metadata.EntryType) then "unknown" else entry.Metadata.EntryType
        Tags = splitCsv entry.Metadata.Tags
        SourceProject = if isNull entry.Metadata.SourceProject then "" else entry.Metadata.SourceProject
        Url = $"/resources/ai-memex/{entry.FileName}/"
        Description = if isNull entry.Metadata.Description then "" else entry.Metadata.Description
    }

// --- Edge discovery layers ---

/// Layer 1: Explicit related_entries from frontmatter (weight 1.0)
let private extractExplicitEdges (entries: AiMemex array) : GraphEdge array =
    let slugSet = entries |> Array.map (fun e -> e.FileName) |> Set.ofArray
    entries
    |> Array.collect (fun entry ->
        let relatedSlugs = splitCsv entry.Metadata.RelatedEntries
        relatedSlugs
        |> Array.filter (fun slug -> Set.contains slug slugSet)
        |> Array.map (fun targetSlug ->
            {
                Source = entry.FileName
                Target = targetSlug
                EdgeType = Explicit
                Weight = 1.0
                Reason = "explicit link"
            }))

/// Layer 2: Wikilinks [[slug]] in content body (weight 0.9)
let private extractWikilinkEdges (entries: AiMemex array) : GraphEdge array =
    let slugSet = entries |> Array.map (fun e -> e.FileName) |> Set.ofArray
    let wikilinkPattern = @"\[\[([^\]|]+)(?:\|[^\]]+)?\]\]"
    entries
    |> Array.collect (fun entry ->
        let content = 
            match entry.MarkdownSource with
            | Some md -> md
            | None -> entry.Content
        let matches = Regex.Matches(content, wikilinkPattern)
        matches
        |> Seq.cast<Match>
        |> Seq.map (fun m -> m.Groups.[1].Value.Trim())
        |> Seq.filter (fun slug -> Set.contains slug slugSet && slug <> entry.FileName)
        |> Seq.distinct
        |> Seq.map (fun targetSlug ->
            {
                Source = entry.FileName
                Target = targetSlug
                EdgeType = Wikilink
                Weight = 0.9
                Reason = "wikilink reference"
            })
        |> Seq.toArray)

/// Layer 3: Tag overlap via Jaccard similarity (weight scales with coefficient, threshold 0.3)
let private extractTagOverlapEdges (entries: AiMemex array) : GraphEdge array =
    let tagSets = 
        entries 
        |> Array.map (fun e -> e.FileName, normalizeTags (splitCsv e.Metadata.Tags))
    [|
        for i in 0 .. tagSets.Length - 2 do
            for j in i + 1 .. tagSets.Length - 1 do
                let (idA, tagsA) = tagSets.[i]
                let (idB, tagsB) = tagSets.[j]
                let jaccard = jaccardSimilarity tagsA tagsB
                if jaccard >= 0.3 then
                    let sharedTags = Set.intersect tagsA tagsB |> Set.toArray |> Array.sort
                    let reason = sprintf "shared tags: %s" (String.Join(", ", sharedTags))
                    yield {
                        Source = idA
                        Target = idB
                        EdgeType = TagOverlap
                        Weight = jaccard
                        Reason = reason
                    }
    |]

/// Layer 4: Same source_project (weight 0.2)
let private extractSameProjectEdges (entries: AiMemex array) : GraphEdge array =
    let withProject = 
        entries 
        |> Array.filter (fun e -> not (String.IsNullOrWhiteSpace(e.Metadata.SourceProject)))
    let groups = withProject |> Array.groupBy (fun e -> e.Metadata.SourceProject.Trim().ToLowerInvariant())
    groups
    |> Array.collect (fun (project, group) ->
        if group.Length < 2 then [||]
        else
            [|
                for i in 0 .. group.Length - 2 do
                    for j in i + 1 .. group.Length - 1 do
                        yield {
                            Source = group.[i].FileName
                            Target = group.[j].FileName
                            EdgeType = SameProject
                            Weight = 0.2
                            Reason = sprintf "same project: %s" project
                        }
            |])

/// Layer 5: Same entry_type with at least one shared tag (weight 0.1)
let private extractSameTypeTagEdges (entries: AiMemex array) : GraphEdge array =
    let byType = entries |> Array.groupBy (fun e -> e.Metadata.EntryType)
    byType
    |> Array.collect (fun (_, group) ->
        if group.Length < 2 then [||]
        else
            let tagSets = group |> Array.map (fun e -> e.FileName, normalizeTags (splitCsv e.Metadata.Tags))
            [|
                for i in 0 .. tagSets.Length - 2 do
                    for j in i + 1 .. tagSets.Length - 1 do
                        let (idA, tagsA) = tagSets.[i]
                        let (idB, tagsB) = tagSets.[j]
                        let shared = Set.intersect tagsA tagsB
                        if not (Set.isEmpty shared) then
                            yield {
                                Source = idA
                                Target = idB
                                EdgeType = SameEntryTypeTag
                                Weight = 0.1
                                Reason = sprintf "same type + shared tag: %s" (shared |> Set.toArray |> Array.head)
                            }
            |])

// --- Graph construction ---

/// Deduplicate edges: for each (source, target) pair, keep the highest-weight edge.
/// Also normalizes direction so (A,B) and (B,A) are treated as the same pair.
let private deduplicateEdges (edges: GraphEdge array) : GraphEdge array =
    edges
    |> Array.groupBy (fun e ->
        let a, b = if e.Source < e.Target then e.Source, e.Target else e.Target, e.Source
        (a, b))
    |> Array.map (fun (_, group) ->
        group |> Array.maxBy (fun e -> e.Weight))

/// Compute backlinks: entries that link TO this node via explicit or wikilink edges.
/// Excludes weak algorithmic connections (those are covered by Related Entries).
let private computeBacklinks (nodes: GraphNode array) (edges: GraphEdge array) : Map<string, BacklinkData array> =
    let titleMap = nodes |> Array.map (fun n -> n.Id, n.Title) |> Map.ofArray
    let mutable backlinks: Map<string, BacklinkData list> = Map.empty
    
    // Only include explicit links and wikilinks for backlinks
    let strongEdges = edges |> Array.filter (fun e -> e.EdgeType = Explicit || e.EdgeType = Wikilink)
    
    for edge in strongEdges do
        // Source links to target, so target gets a backlink from source
        let bl = { Slug = edge.Source; Title = Map.tryFind edge.Source titleMap |> Option.defaultValue edge.Source; Reason = edge.Reason }
        backlinks <- 
            match Map.tryFind edge.Target backlinks with
            | Some existing -> Map.add edge.Target (bl :: existing) backlinks
            | None -> Map.add edge.Target [bl] backlinks
    backlinks
    |> Map.map (fun _ bls ->
        bls 
        |> List.distinctBy (fun b -> b.Slug)
        |> List.toArray)

/// Compute ranked related entries for each node (top 5 by combined weight)
let private computeRelatedEntries (nodes: GraphNode array) (edges: GraphEdge array) : Map<string, RelatedEntryData array> =
    let nodeMap = nodes |> Array.map (fun n -> n.Id, n) |> Map.ofArray
    let mutable scores: Map<string, Map<string, (float * string)>> = Map.empty
    
    for edge in edges do
        // Accumulate score from source's perspective toward target
        let updateScore (fromId: string) (toId: string) =
            let current = 
                scores 
                |> Map.tryFind fromId 
                |> Option.defaultValue Map.empty
            let existing = Map.tryFind toId current
            let newEntry = 
                match existing with
                | Some (s, _) when s >= edge.Weight -> existing.Value
                | _ -> (edge.Weight, edge.Reason)
            scores <- Map.add fromId (Map.add toId newEntry current) scores
        updateScore edge.Source edge.Target
        updateScore edge.Target edge.Source
    
    scores
    |> Map.map (fun _ relMap ->
        relMap
        |> Map.toArray
        |> Array.sortByDescending (fun (_, (score, _)) -> score)
        |> Array.truncate 5
        |> Array.choose (fun (slug, (score, reason)) ->
            match Map.tryFind slug nodeMap with
            | Some node -> 
                Some { 
                    Slug = slug
                    Title = node.Title
                    EntryType = node.EntryType
                    Reason = reason
                    Score = score
                }
            | None -> None))

/// Build the complete knowledge graph from AI Memex entries
let buildGraph (entries: AiMemex array) : KnowledgeGraph =
    let nodes = entries |> Array.map toGraphNode
    
    // Collect edges from all layers
    let allEdges =
        [|
            yield! extractExplicitEdges entries
            yield! extractWikilinkEdges entries
            yield! extractTagOverlapEdges entries
            yield! extractSameProjectEdges entries
            yield! extractSameTypeTagEdges entries
        |]
    
    let edges = deduplicateEdges allEdges
    let backlinks = computeBacklinks nodes edges
    let relatedEntries = computeRelatedEntries nodes edges
    
    { Nodes = nodes; Edges = edges; Backlinks = backlinks; RelatedEntries = relatedEntries }

// --- JSON-LD Generation ---

/// Map entry_type to Schema.org @type
let private schemaOrgType (entryType: string) =
    match entryType with
    | "pattern" -> "TechArticle"
    | "research" -> "ScholarlyArticle"
    | "reference" -> "TechArticle"
    | "project-report" -> "Article"
    | "blog-post" -> "BlogPosting"
    | _ -> "Article"

/// Generate JSON-LD for a single AI Memex entry page
let generateEntryJsonLd (entry: AiMemex) (graph: KnowledgeGraph) : string =
    let relatedLinks =
        match Map.tryFind entry.FileName graph.RelatedEntries with
        | Some related -> 
            related 
            |> Array.map (fun r -> sprintf "\"https://www.lqdev.me/resources/ai-memex/%s/\"" r.Slug)
        | None -> [||]

    let tags = splitCsv entry.Metadata.Tags
    let aboutItems = 
        tags 
        |> Array.map (fun tag -> sprintf """{"@type":"Thing","name":"%s"}""" (tag.Replace("\"", "\\\"")))
    
    let publishedDate = 
        try DateTimeOffset.Parse(entry.Metadata.PublishedDate).ToString("yyyy-MM-dd")
        with _ -> ""
    let modifiedDate = 
        if String.IsNullOrEmpty(entry.Metadata.LastUpdatedDate) then publishedDate
        else 
            try DateTimeOffset.Parse(entry.Metadata.LastUpdatedDate).ToString("yyyy-MM-dd")
            with _ -> publishedDate
    
    let relatedLinkJson =
        if relatedLinks.Length > 0 then
            sprintf ""","relatedLink":[%s]""" (String.Join(",", relatedLinks))
        else ""
    
    let aboutJson =
        if aboutItems.Length > 0 then
            sprintf ""","about":[%s]""" (String.Join(",", aboutItems))
        else ""
    
    let keywords = String.Join(",", tags)
    
    sprintf """{
  "@context":"https://schema.org",
  "@type":"%s",
  "@id":"https://www.lqdev.me/resources/ai-memex/%s/",
  "headline":"%s",
  "description":"%s",
  "author":{"@type":"SoftwareApplication","name":"GitHub Copilot","url":"https://github.com/features/copilot","applicationCategory":"AI Coding Assistant"},
  "publisher":{"@type":"Person","name":"Luis Quintanilla","url":"https://www.lqdev.me"},
  "datePublished":"%s",
  "dateModified":"%s",
  "keywords":"%s",
  "isPartOf":{"@type":"Collection","@id":"https://www.lqdev.me/resources/ai-memex/","name":"AI Memex"}%s%s,
  "mainEntityOfPage":{"@type":"WebPage","@id":"https://www.lqdev.me/resources/ai-memex/%s/"}
}"""     (schemaOrgType entry.Metadata.EntryType)
         entry.FileName
         (entry.Metadata.Title.Replace("\"", "\\\""))
         ((if isNull entry.Metadata.Description then "" else entry.Metadata.Description).Replace("\"", "\\\""))
         publishedDate
         modifiedDate
         keywords
         relatedLinkJson
         aboutJson
         entry.FileName

/// Generate CollectionPage JSON-LD for the AI Memex index page
let generateCollectionJsonLd (entries: AiMemex array) : string =
    let byType = entries |> Array.groupBy (fun e -> e.Metadata.EntryType)
    let typeLabels = 
        [| ("pattern", "Patterns"); ("research", "Research"); ("reference", "References"); 
           ("project-report", "Project Reports"); ("blog-post", "Blog Posts") |]
        |> Map.ofArray
    
    let itemLists =
        byType
        |> Array.map (fun (entryType, items) ->
            let label = Map.tryFind entryType typeLabels |> Option.defaultValue entryType
            let listItems = 
                items 
                |> Array.mapi (fun i item ->
                    sprintf """{"@type":"ListItem","position":%d,"url":"https://www.lqdev.me/resources/ai-memex/%s/","name":"%s"}"""
                        (i + 1) item.FileName (item.Metadata.Title.Replace("\"", "\\\"")))
            sprintf """{"@type":"ItemList","name":"%s","numberOfItems":%d,"itemListElement":[%s]}"""
                label items.Length (String.Join(",", listItems)))

    sprintf """{
  "@context":"https://schema.org",
  "@type":"CollectionPage",
  "@id":"https://www.lqdev.me/resources/ai-memex/",
  "name":"AI Memex",
  "description":"A distributed knowledge capture system — patterns, research, references, and project outcomes authored by AI coding assistants.",
  "url":"https://www.lqdev.me/resources/ai-memex/",
  "collectionSize":%d,
  "hasPart":[%s]
}"""     entries.Length (String.Join(",", itemLists))

// --- graph.json serialization ---

/// Serialize the knowledge graph to graph.json for client-side visualization and AI tools
let serializeGraphJson (graph: KnowledgeGraph) : string =
    let edgeTypeString (et: EdgeType) =
        match et with
        | Explicit -> "explicit"
        | Wikilink -> "wikilink"
        | TagOverlap -> "tag-overlap"
        | SameProject -> "same-project"
        | SameEntryTypeTag -> "same-type-tag"
    
    let connectionCounts =
        let mutable counts: Map<string, int> = Map.empty
        for edge in graph.Edges do
            counts <- Map.add edge.Source (1 + (Map.tryFind edge.Source counts |> Option.defaultValue 0)) counts
            counts <- Map.add edge.Target (1 + (Map.tryFind edge.Target counts |> Option.defaultValue 0)) counts
        counts
    
    let nodeJsons =
        graph.Nodes
        |> Array.map (fun n ->
            let cc = Map.tryFind n.Id connectionCounts |> Option.defaultValue 0
            let tagsJson = n.Tags |> Array.map (fun t -> sprintf "\"%s\"" (t.Replace("\"", "\\\""))) |> fun a -> String.Join(",", a)
            sprintf """{"id":"%s","title":"%s","entryType":"%s","tags":[%s],"url":"%s","sourceProject":"%s","description":"%s","connectionCount":%d}"""
                n.Id
                (n.Title.Replace("\"", "\\\""))
                n.EntryType
                tagsJson
                n.Url
                (n.SourceProject.Replace("\"", "\\\""))
                (n.Description.Replace("\"", "\\\""))
                cc)
    
    let edgeJsons =
        graph.Edges
        |> Array.map (fun e ->
            sprintf """{"source":"%s","target":"%s","type":"%s","weight":%.2f,"reason":"%s"}"""
                e.Source e.Target (edgeTypeString e.EdgeType) e.Weight (e.Reason.Replace("\"", "\\\"")))
    
    // Build clusters by source project
    let clusters =
        graph.Nodes
        |> Array.filter (fun n -> not (String.IsNullOrWhiteSpace(n.SourceProject)))
        |> Array.groupBy (fun n -> n.SourceProject)
        |> Array.map (fun (project, nodes) ->
            let ids = nodes |> Array.map (fun n -> sprintf "\"%s\"" n.Id) |> fun a -> String.Join(",", a)
            sprintf "\"%s\":[%s]" project ids)
    
    let now = DateTimeOffset.UtcNow.ToString("o")
    let avgConn = 
        if graph.Nodes.Length = 0 then 0.0
        else (graph.Edges.Length |> float) * 2.0 / (graph.Nodes.Length |> float)
    
    sprintf """{
  "nodes":[%s],
  "edges":[%s],
  "clusters":{%s},
  "stats":{"nodeCount":%d,"edgeCount":%d,"avgConnections":%.1f,"generatedAt":"%s"}
}"""     (String.Join(",", nodeJsons))
         (String.Join(",", edgeJsons))
         (String.Join(",", clusters))
         graph.Nodes.Length
         graph.Edges.Length
         avgConn
         now

/// Save graph.json to the output directory
let saveGraphJson (outputDir: string) (graph: KnowledgeGraph) =
    let graphDir = Path.Combine(outputDir, "resources", "ai-memex")
    if not (Directory.Exists(graphDir)) then
        Directory.CreateDirectory(graphDir) |> ignore
    let graphPath = Path.Combine(graphDir, "graph.json")
    let json = serializeGraphJson graph
    File.WriteAllText(graphPath, json)
    {| EdgeCount = graph.Edges.Length; NodeCount = graph.Nodes.Length |}

// --- Cross-content-type connections ---

/// Find related content from across the site (posts, wiki, snippets, etc.)
/// using tag overlap with a given Memex entry. Returns top 5 matches.
let findCrossContentRelated (entryTags: string array) (entrySlug: string) (unifiedItems: GenericBuilder.UnifiedFeeds.UnifiedFeedItem list) : CrossContentItem array =
    if entryTags.Length = 0 then [||]
    else
        let entryTagSet = entryTags |> Array.map (fun t -> t.ToLowerInvariant().Trim()) |> Set.ofArray
        unifiedItems
        |> List.filter (fun item ->
            item.ContentType <> "ai-memex" &&
            not (String.IsNullOrWhiteSpace(item.Title)) &&
            not (isNull item.Tags) &&
            item.Tags.Length > 0)
        |> List.choose (fun item ->
            let itemTagSet = item.Tags |> Array.map (fun t -> t.ToLowerInvariant().Trim()) |> Set.ofArray
            let shared = Set.intersect entryTagSet itemTagSet
            if shared.Count >= 2 then
                let reason = sprintf "shared tags: %s" (shared |> Set.toArray |> Array.sort |> fun a -> String.Join(", ", a))
                Some (item, shared.Count, reason)
            else None)
        |> List.sortByDescending (fun (_, count, _) -> count)
        |> List.truncate 5
        |> List.map (fun (item, _, reason) ->
            {
                Title = item.Title
                Url = item.Url
                ContentType = item.ContentType
                Tags = item.Tags
                OverlapReason = reason
            })
        |> List.toArray

// --- Wikilink resolution ---

/// Resolve [[slug]] and [[slug|text]] wikilinks to HTML anchor tags.
/// Returns (transformed content, list of referenced slugs).
let resolveWikilinks (slugToTitle: Map<string, string>) (content: string) : string * string list =
    let mutable referencedSlugs = []
    let pattern = @"\[\[([^\]|]+)(?:\|([^\]]+))?\]\]"
    let result = 
        Regex.Replace(content, pattern, fun (m: Match) ->
            let slug = m.Groups.[1].Value.Trim()
            let displayText = 
                if m.Groups.[2].Success then m.Groups.[2].Value.Trim()
                else ""
            match Map.tryFind slug slugToTitle with
            | Some title ->
                referencedSlugs <- slug :: referencedSlugs
                let text = if displayText = "" then title else displayText
                sprintf """<a href="/resources/ai-memex/%s/" class="memex-wikilink" title="%s">%s</a>""" 
                    slug (title.Replace("\"", "&quot;")) text
            | None ->
                sprintf """<span class="memex-wikilink-broken" title="Entry not found">%s</span>""" slug)
    (result, referencedSlugs |> List.distinct |> List.rev)
