module EntityExtraction

open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open Microsoft.Extensions.AI

// --- Extraction result types ---

type ExtractedEntity = {
    [<JsonPropertyName("id")>] Id: string
    [<JsonPropertyName("type")>] EntityType: string
    [<JsonPropertyName("label")>] Label: string
    [<JsonPropertyName("sameAs")>] SameAs: string array
}

type Assertion = {
    [<JsonPropertyName("s")>] Subject: string
    [<JsonPropertyName("p")>] Predicate: string
    [<JsonPropertyName("o")>] Object: string
    [<JsonPropertyName("confidence")>] Confidence: float
}

type ExtractionResult = {
    [<JsonPropertyName("entities")>] Entities: ExtractedEntity array
    [<JsonPropertyName("assertions")>] Assertions: Assertion array
}

// --- Normalization ---

/// Strip schema: prefix from entity types for consistent downstream matching
let private normalizeEntityType (t: string) =
    if String.IsNullOrWhiteSpace(t) then "Thing"
    elif t.StartsWith("schema:", StringComparison.OrdinalIgnoreCase) then t.Substring(7)
    else t

let private normalizeResult (result: ExtractionResult) =
    { result with
        Entities =
            result.Entities
            |> Array.map (fun e -> { e with EntityType = normalizeEntityType e.EntityType }) }

// --- Cache ---

let private cacheDir = Path.Combine("graph", "cache")
let private promptVersion = "v1"

let private cachePath (slug: string) =
    Path.Combine(cacheDir, $"{slug}.{promptVersion}.json")

let private jsonOptions =
    let opts = JsonSerializerOptions(WriteIndented = true)
    opts.DefaultIgnoreCondition <- JsonIgnoreCondition.WhenWritingNull
    opts

let private loadCache (slug: string) : ExtractionResult option =
    let path = cachePath slug
    if File.Exists(path) then
        try
            let json = File.ReadAllText(path)
            JsonSerializer.Deserialize<ExtractionResult>(json, jsonOptions)
            |> Option.ofObj
            |> Option.map normalizeResult
        with _ -> None
    else None

let private saveCache (slug: string) (result: ExtractionResult) =
    Directory.CreateDirectory(cacheDir) |> ignore
    let json = JsonSerializer.Serialize(result, jsonOptions)
    File.WriteAllText(cachePath slug, json)

// --- Markdown chunking ---

/// Split markdown body into chunks by top-level headings, targeting ~750 tokens.
/// Returns (chunkIndex, chunkText) pairs.
let chunkMarkdown (bodyText: string) : (int * string) array =
    if String.IsNullOrWhiteSpace(bodyText) then [||]
    else
        let lines = bodyText.Split([| '\n' |])
        let mutable chunks = []
        let mutable currentChunk = System.Text.StringBuilder()
        let mutable chunkIndex = 0

        for line in lines do
            if line.StartsWith("## ") && currentChunk.Length > 100 then
                chunks <- (chunkIndex, currentChunk.ToString().Trim()) :: chunks
                chunkIndex <- chunkIndex + 1
                currentChunk <- System.Text.StringBuilder()
            currentChunk.AppendLine(line) |> ignore

        if currentChunk.Length > 0 then
            chunks <- (chunkIndex, currentChunk.ToString().Trim()) :: chunks

        chunks |> List.rev |> List.toArray

// --- IChatClient creation ---

/// Create an IChatClient targeting GitHub Models via MEAI.
/// Returns None if GITHUB_TOKEN is not set (graceful degradation).
let createChatClient () : IChatClient option =
    let token = Environment.GetEnvironmentVariable("GITHUB_TOKEN")
    if String.IsNullOrEmpty(token) then
        None
    else
        try
            let credential = System.ClientModel.ApiKeyCredential(token)
            let options = OpenAI.OpenAIClientOptions()
            options.Endpoint <- Uri("https://models.github.ai/inference")
            let openAiClient = OpenAI.OpenAIClient(credential, options)
            let chatClient =
                openAiClient
                    .GetChatClient("openai/gpt-4o-mini")
                    .AsIChatClient()
            Some chatClient
        with ex ->
            eprintfn "Warning: Failed to create LLM client: %s" ex.Message
            None

// --- Extraction ---

let private emptyResult = { Entities = [||]; Assertions = [||] }

let private promptPath = Path.Combine("ontology", "extract_entities_v1.txt")

/// Load the system prompt, substituting the article URI placeholder.
let private loadSystemPrompt () : string =
    if File.Exists(promptPath) then
        File.ReadAllText(promptPath)
    else
        failwithf "Extraction prompt not found: %s" promptPath

/// Extract entities from a single chunk using MEAI structured output.
let private extractFromChunk
    (client: IChatClient)
    (systemPrompt: string)
    (articleUri: string)
    (chunkText: string)
    : ExtractionResult =
    let prompt = systemPrompt.Replace("<ARTICLE_URI>", articleUri)
    let messages = [
        ChatMessage(ChatRole.System, prompt)
        ChatMessage(ChatRole.User, chunkText)
    ]
    let options = ChatOptions()
    options.ResponseFormat <- ChatResponseFormat.ForJsonSchema<ExtractionResult>()

    let response =
        client.GetResponseAsync(messages, options)
        |> Async.AwaitTask
        |> Async.RunSynchronously

    let text = response.Text
    if String.IsNullOrWhiteSpace(text) then emptyResult
    else
        try
            JsonSerializer.Deserialize<ExtractionResult>(text, jsonOptions)
        with _ -> emptyResult

/// Merge multiple extraction results, deduplicating entities by id.
let private mergeResults (results: ExtractionResult array) : ExtractionResult =
    let entities =
        results
        |> Array.collect (fun r -> r.Entities)
        |> Array.distinctBy (fun e -> e.Id.ToLowerInvariant())
    let assertions =
        results
        |> Array.collect (fun r -> r.Assertions)
        |> Array.distinctBy (fun a -> (a.Subject, a.Predicate, a.Object))
    { Entities = entities; Assertions = assertions }

/// Extract entities from a Memex entry. Uses cache when available.
/// Without an IChatClient, returns cached results or empty.
let extractEntities
    (chatClient: IChatClient option)
    (slug: string)
    (bodyText: string)
    (baseUrl: string)
    : ExtractionResult =
    match loadCache slug with
    | Some cached -> cached
    | None ->
        match chatClient with
        | None -> emptyResult
        | Some client ->
            let systemPrompt = loadSystemPrompt ()
            let articleUri = $"{baseUrl}/resources/ai-memex/{slug}/"
            let chunks = chunkMarkdown bodyText

            let results =
                if chunks.Length = 0 then [| emptyResult |]
                else
                    chunks
                    |> Array.map (fun (_, chunkText) ->
                        try extractFromChunk client systemPrompt articleUri chunkText
                        with ex ->
                            eprintfn "Warning: extraction failed for %s: %s" slug ex.Message
                            emptyResult)

            let merged = mergeResults results |> normalizeResult
            if merged.Entities.Length > 0 then
                saveCache slug merged
            merged

/// Extract entities for all entries, logging progress.
let extractAll
    (chatClient: IChatClient option)
    (entries: (string * string) array)
    (baseUrl: string)
    : Map<string, ExtractionResult> =
    let total = entries.Length
    let mutable cacheHits = 0
    let mutable llmCalls = 0

    let results =
        entries
        |> Array.mapi (fun i (slug, body) ->
            let isCached = (loadCache slug).IsSome
            if isCached then cacheHits <- cacheHits + 1
            else if chatClient.IsSome then llmCalls <- llmCalls + 1

            if (i + 1) % 10 = 0 || i = total - 1 then
                eprintfn "  Entity extraction: %d/%d (cache: %d, LLM: %d)" (i + 1) total cacheHits llmCalls

            let result = extractEntities chatClient slug body baseUrl
            (slug, result))
        |> Map.ofArray

    eprintfn "  Entity extraction complete: %d entries, %d cached, %d LLM calls" total cacheHits llmCalls
    results
