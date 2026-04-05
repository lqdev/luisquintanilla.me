module RdfSerializer

open System
open System.IO
open VDS.RDF
open VDS.RDF.Writing
open KnowledgeGraph

let private schemaUri = Uri("https://schema.org/")
let private kbUri = Uri("https://www.lqdev.me/vocab/kb#")
let private baseUri = Uri("https://www.lqdev.me/")

let inline private assertTriple (g: IGraph) (s: INode) (p: INode) (o: INode) =
    g.Assert(Triple(s, p, o)) |> ignore

/// Build an RDF graph from the knowledge graph and serialize to Turtle + JSON-LD
let serializeGraph (graph: KnowledgeGraph) (outputDir: string) =
    let rdf = new Graph()

    rdf.NamespaceMap.AddNamespace("schema", schemaUri)
    rdf.NamespaceMap.AddNamespace("kb", kbUri)
    rdf.NamespaceMap.AddNamespace("rdf", Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#"))
    rdf.NamespaceMap.AddNamespace("rdfs", Uri("http://www.w3.org/2000/01/rdf-schema#"))
    rdf.NamespaceMap.AddNamespace("lqdev", baseUri)

    let rdfType = rdf.CreateUriNode("rdf:type")
    let schemaName = rdf.CreateUriNode("schema:name")
    let schemaUrl = rdf.CreateUriNode("schema:url")
    let schemaMentions = rdf.CreateUriNode("schema:mentions")
    let schemaKeywords = rdf.CreateUriNode("schema:keywords")
    let schemaSameAs = rdf.CreateUriNode("schema:sameAs")
    let kbEntryType = rdf.CreateUriNode("kb:entryType")
    let kbSourceProject = rdf.CreateUriNode("kb:sourceProject")
    let kbRelatedTo = rdf.CreateUriNode("kb:relatedTo")
    let assert3 s p o = assertTriple rdf s p o

    // Entry nodes
    for node in graph.Nodes do
        let entryUri = rdf.CreateUriNode(UriFactory.Create($"https://www.lqdev.me{node.Url}"))
        let articleType =
            match node.EntryType with
            | "pattern" -> "schema:TechArticle"
            | "research" -> "schema:ScholarlyArticle"
            | "blog-post" -> "schema:BlogPosting"
            | _ -> "schema:Article"
        assert3 entryUri rdfType (rdf.CreateUriNode(articleType))
        assert3 entryUri schemaName (rdf.CreateLiteralNode(node.Title))
        assert3 entryUri schemaUrl (rdf.CreateUriNode(UriFactory.Create($"https://www.lqdev.me{node.Url}")))
        assert3 entryUri kbEntryType (rdf.CreateLiteralNode(node.EntryType))

        if not (String.IsNullOrWhiteSpace(node.Description)) then
            assert3 entryUri (rdf.CreateUriNode("schema:description")) (rdf.CreateLiteralNode(node.Description))

        if not (String.IsNullOrWhiteSpace(node.SourceProject)) then
            assert3 entryUri kbSourceProject (rdf.CreateLiteralNode(node.SourceProject))

        for tag in node.Tags do
            assert3 entryUri schemaKeywords (rdf.CreateLiteralNode(tag))

    // Edges as kb:relatedTo
    for edge in graph.Edges do
        if edge.EdgeType <> EntityMention then
            let src = graph.Nodes |> Array.tryFind (fun n -> n.Id = edge.Source)
            let tgt = graph.Nodes |> Array.tryFind (fun n -> n.Id = edge.Target)
            match src, tgt with
            | Some s, Some t ->
                let srcUri = rdf.CreateUriNode(UriFactory.Create($"https://www.lqdev.me{s.Url}"))
                let tgtUri = rdf.CreateUriNode(UriFactory.Create($"https://www.lqdev.me{t.Url}"))
                assert3 srcUri kbRelatedTo tgtUri
            | _ -> ()

    // Entity nodes and mentions edges
    for entity in graph.EntityNodes do
        let entityUri = rdf.CreateUriNode(UriFactory.Create($"https://www.lqdev.me/entity/{entity.Id}"))
        let entityRdfType =
            match entity.EntityType with
            | "SoftwareApplication" -> "schema:SoftwareApplication"
            | "SoftwareSourceCode" -> "schema:SoftwareSourceCode"
            | "Person" -> "schema:Person"
            | "Organization" -> "schema:Organization"
            | "WebAPI" -> "schema:WebAPI"
            | "WebApplication" -> "schema:WebApplication"
            | "ProgrammingLanguage" -> "schema:ComputerLanguage"
            | "CreativeWork" -> "schema:CreativeWork"
            | "Article" -> "schema:Article"
            | _ -> "schema:Thing"
        assert3 entityUri rdfType (rdf.CreateUriNode(entityRdfType))
        assert3 entityUri schemaName (rdf.CreateLiteralNode(entity.Label))

        for sameAsUrl in entity.SameAs do
            if not (String.IsNullOrWhiteSpace(sameAsUrl)) then
                try assert3 entityUri schemaSameAs (rdf.CreateUriNode(UriFactory.Create(sameAsUrl)))
                with _ -> ()

        for slug in entity.MentionedIn do
            match graph.Nodes |> Array.tryFind (fun n -> n.Id = slug) with
            | Some n ->
                let entryUri = rdf.CreateUriNode(UriFactory.Create($"https://www.lqdev.me{n.Url}"))
                assert3 entryUri schemaMentions entityUri
            | None -> ()

    // Serialize to Turtle
    let outPath = Path.Combine(outputDir, "resources", "ai-memex")
    Directory.CreateDirectory(outPath) |> ignore

    let turtleWriter = CompressingTurtleWriter()
    turtleWriter.Save(rdf, Path.Combine(outPath, "graph.ttl"))

    // Serialize to JSON-LD via TripleStore wrapper
    let store = new TripleStore()
    store.Add(rdf) |> ignore
    let jsonLdWriter = JsonLdWriter()
    jsonLdWriter.Save(store, Path.Combine(outPath, "graph.jsonld"))

    eprintfn "  RDF serialized: %d triples -> graph.ttl + graph.jsonld" rdf.Triples.Count
    rdf.Triples.Count
