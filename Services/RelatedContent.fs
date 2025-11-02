module RelatedContentService

open System
open Domain

/// Represents a related content item with relevance score
type RelatedItem<'T> = {
    Content: 'T
    RelevanceScore: int
}

/// Calculate tag overlap between two string arrays
let private calculateTagOverlap (tags1: string array) (tags2: string array) : int =
    if isNull tags1 || isNull tags2 then 0
    else
        let set1 = Set.ofArray (tags1 |> Array.map (fun t -> t.ToLowerInvariant().Trim()))
        let set2 = Set.ofArray (tags2 |> Array.map (fun t -> t.ToLowerInvariant().Trim()))
        Set.intersect set1 set2 |> Set.count

/// Find related content based on shared tags
/// - Excludes the current item
/// - Sorts by relevance (tag overlap count), then recency
/// - Returns top N items (default 5)
let findRelatedContent<'T when 'T :> ITaggable> 
    (currentItem: 'T) 
    (allItems: 'T array) 
    (maxResults: int) : 'T array =
    
    let currentTaggable = currentItem :> ITaggable
    let currentTags = 
        if isNull currentTaggable.Tags then [||]
        else currentTaggable.Tags
    
    let currentFileName = currentTaggable.FileName
    
    // Calculate relevance scores for all items except the current one
    allItems
    |> Array.filter (fun item -> 
        let itf = item :> ITaggable
        itf.FileName <> currentFileName)
    |> Array.map (fun item ->
        let itemTaggable = item :> ITaggable
        let itemTags = 
            if isNull itemTaggable.Tags then [||]
            else itemTaggable.Tags
        let score = calculateTagOverlap currentTags itemTags
        { Content = item; RelevanceScore = score })
    |> Array.filter (fun relItem -> relItem.RelevanceScore > 0)  // Only include items with at least one shared tag
    |> Array.sortByDescending (fun relItem -> 
        // Sort by relevance score first, then by date (more recent first)
        let itemTaggable = relItem.Content :> ITaggable
        let date = 
            try DateTimeOffset.Parse(itemTaggable.Date)
            with _ -> DateTimeOffset.MinValue
        (relItem.RelevanceScore, date))
    |> Array.truncate maxResults
    |> Array.map (fun relItem -> relItem.Content)
