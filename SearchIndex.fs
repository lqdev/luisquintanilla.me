module SearchIndex

open System
open System.IO
open System.Text.Json
open System.Text.RegularExpressions
open Domain
open GenericBuilder.UnifiedFeeds

// Search item type optimized for client-side search
type SearchItem = {
    Id: string
    Title: string
    Content: string
    ContentType: string
    Url: string
    Date: string
    Tags: string[]
    Summary: string
    Keywords: string[]
}

// Content preprocessing for search optimization
module ContentProcessor =
    
    // Remove HTML tags and normalize whitespace
    let stripHtml (content: string) =
        if String.IsNullOrWhiteSpace(content) then ""
        else
            let htmlPattern = @"<[^>]*>"
            let noHtml = Regex.Replace(content, htmlPattern, " ")
            let normalized = Regex.Replace(noHtml, @"\s+", " ")
            normalized.Trim()
    
    // Basic stop words filter (expandable)
    let isStopWord (word: string) =
        let stopWords = [|
            "the"; "and"; "for"; "are"; "but"; "not"; "you"; "all"; "can"; "had"; 
            "her"; "was"; "one"; "our"; "out"; "day"; "get"; "has"; "him"; "his"; 
            "how"; "its"; "may"; "new"; "now"; "old"; "see"; "two"; "way"; "who"; 
            "boy"; "did"; "don"; "end"; "few"; "got"; "man"; "own"; "say"; "she"; 
            "too"; "use"; "that"; "with"; "have"; "this"; "will"; "been"; "each"; 
            "make"; "most"; "move"; "must"; "name"; "over"; "such"; "take"; "than"; 
            "them"; "well"; "were"; "when"; "very"; "what"; "your"; "into"; "just"; 
            "know"; "like"; "only"; "other"; "some"; "time"; "work"; "could"; "first"; 
            "after"; "being"; "where"; "every"; "great"; "might"; "right"; "still"; 
            "these"; "think"; "would"; "about"; "before"; "should"; "there"; "their"; 
            "through"; "between"; "because"; "without"; "something"
        |]
        Array.contains word stopWords
    
    // Extract keywords from content using simple frequency analysis
    let extractKeywords (content: string) (maxKeywords: int) =
        if String.IsNullOrWhiteSpace(content) then [||]
        else
            let allWords = 
                content.ToLower()
                    .Split([|' '; '\n'; '\r'; '\t'; '.'; ','; ';'; '!'; '?'; '('; ')'; '['; ']'; '{'; '}'; '"'; '\''; '-'; '_'|], 
                           StringSplitOptions.RemoveEmptyEntries)
                |> Array.filter (fun word -> word.Length > 3) // Filter short words
                |> Array.filter (fun word -> not (isStopWord word))
            let wordFrequency = allWords |> Array.countBy id
            let topWords = 
                wordFrequency
                |> Array.sortByDescending snd
                |> Array.take (min maxKeywords (Array.length wordFrequency))
                |> Array.map fst
            topWords
    
    // Generate content summary (first 200 chars, ending at word boundary)
    let generateSummary (content: string) (maxLength: int) =
        if String.IsNullOrWhiteSpace(content) || content.Length <= maxLength then content
        else
            let truncated = content.Substring(0, maxLength)
            let lastSpace = truncated.LastIndexOf(' ')
            if lastSpace > maxLength / 2 then
                truncated.Substring(0, lastSpace) + "..."
            else
                truncated + "..."

// Search index generation functions
module IndexGenerator =
    
    // Convert unified feed item to search item
    let convertToSearchItem (item: UnifiedFeedItem) =
        let cleanContent = ContentProcessor.stripHtml item.Content
        let summary = ContentProcessor.generateSummary cleanContent 200
        let keywords = ContentProcessor.extractKeywords cleanContent 10
        
        {
            Id = $"{item.ContentType}-{Path.GetFileNameWithoutExtension(item.Url)}"
            Title = item.Title
            Content = cleanContent
            ContentType = item.ContentType
            Url = item.Url
            Date = item.Date
            Tags = item.Tags
            Summary = summary
            Keywords = keywords
        }
    
    // Generate search index from unified content
    let generateSearchIndex (unifiedContent: UnifiedFeedItem list) =
        unifiedContent
        |> List.map convertToSearchItem
        |> List.sortByDescending (fun item -> item.Date)
    
    // Serialize search index to JSON with optimization
    let serializeToJson (searchItems: SearchItem list) =
        let options = JsonSerializerOptions()
        options.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
        options.WriteIndented <- false // Minimize file size
        JsonSerializer.Serialize(searchItems, options)
    
    // Generate and save search index file
    let saveSearchIndex (outputDir: string) (unifiedContent: UnifiedFeedItem list) =
        let searchIndex = generateSearchIndex unifiedContent
        let jsonContent = serializeToJson searchIndex
        
        // Create search directory if it doesn't exist
        let searchDir = Path.Combine(outputDir, "search")
        if not (Directory.Exists(searchDir)) then
            Directory.CreateDirectory(searchDir) |> ignore
        
        // Save main search index
        let indexPath = Path.Combine(searchDir, "index.json")
        File.WriteAllText(indexPath, jsonContent)
        
        // Generate stats
        let itemCount = List.length searchIndex
        let totalSize = jsonContent.Length
        let avgKeywords = 
            searchIndex 
            |> List.map (fun item -> item.Keywords.Length) 
            |> List.map float
            |> List.average
        
        // Return metadata for potential use in build reporting
        {| ItemCount = itemCount; FileSizeBytes = totalSize; AverageKeywords = avgKeywords |}

// Tag-based index generation for enhanced tag discovery
module TagIndexGenerator =
    
    // Generate tag frequency map for enhanced tag browsing
    let generateTagIndex (unifiedContent: UnifiedFeedItem list) =
        let tagFrequency = 
            unifiedContent
            |> List.collect (fun item -> item.Tags |> Array.toList)
            |> List.filter (fun tag -> not (String.IsNullOrWhiteSpace(tag)))
            |> List.groupBy id
            |> List.map (fun (tag, items) -> 
                {| 
                    Tag = tag
                    Count = List.length items
                    ContentTypes = items |> List.map (fun _ -> 
                        unifiedContent 
                        |> List.find (fun item -> Array.contains tag item.Tags)
                        |> fun item -> item.ContentType) 
                    |> List.distinct
                |})
            |> List.sortByDescending (fun tagInfo -> tagInfo.Count)
        
        tagFrequency
    
    // Save tag index for enhanced tag browsing
    let saveTagIndex (outputDir: string) (unifiedContent: UnifiedFeedItem list) =
        let tagIndex = generateTagIndex unifiedContent
        let jsonContent = 
            let options = JsonSerializerOptions()
            options.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
            options.WriteIndented <- false
            JsonSerializer.Serialize(tagIndex, options)
        
        let searchDir = Path.Combine(outputDir, "search")
        if not (Directory.Exists(searchDir)) then
            Directory.CreateDirectory(searchDir) |> ignore
        
        let tagIndexPath = Path.Combine(searchDir, "tags.json")
        File.WriteAllText(tagIndexPath, jsonContent)
        
        let tagCount = List.length tagIndex
        let totalSize = jsonContent.Length
        
        {| TagCount = tagCount; FileSizeBytes = totalSize |}

// Main orchestration function for search index generation
let buildSearchIndexes (outputDir: string) (unifiedContent: UnifiedFeedItem list) =
    
    // Generate main search index
    let searchStats = IndexGenerator.saveSearchIndex outputDir unifiedContent
    
    // Generate tag index for enhanced tag browsing
    let tagStats = TagIndexGenerator.saveTagIndex outputDir unifiedContent
    
    // Return combined statistics
    {| 
        SearchIndex = searchStats
        TagIndex = tagStats
        TotalIndexSize = searchStats.FileSizeBytes + tagStats.FileSizeBytes
    |}
