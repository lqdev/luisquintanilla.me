#r "../bin/Debug/net9.0/PersonalSite.dll"

open System
open System.IO
open Domain
open TagService

// Load all content using the correct functions
let posts = Loaders.loadPosts "_src"
let notes = Loaders.loadFeed "_src"  // Notes are loaded via loadFeed
let responses = Loaders.loadReponses "_src"

// Helper function to extract raw tags from content
let extractRawTags (posts: Post array) =
    posts
    |> Array.collect (fun post -> 
        if post.Metadata.Tags <> null then
            post.Metadata.Tags
        else [||])
    |> Array.distinct

let extractRawTagsFromResponses (responses: Response array) =
    responses
    |> Array.collect (fun response -> 
        if response.Metadata.Tags <> null then
            response.Metadata.Tags
        else [||])
    |> Array.distinct

// Get all raw tags from all content types
let allRawTags = 
    [|
        extractRawTags posts
        extractRawTags notes
        extractRawTagsFromResponses responses
    |]
    |> Array.concat
    |> Array.distinct
    |> Array.filter (fun tag -> not (String.IsNullOrWhiteSpace(tag)))
    |> Array.sort

printfn "=== RAW TAGS ANALYSIS ==="
printfn "Total unique raw tags: %d" allRawTags.Length
printfn ""

// Get processed tags using the existing processing function
let processedTagPairs = 
    allRawTags
    |> Array.map (fun rawTag -> (rawTag, processTagName rawTag))
    |> Array.distinctBy snd

printfn "=== PROCESSED TAGS ANALYSIS ==="
printfn "Total unique processed tags: %d" (processedTagPairs |> Array.map snd |> Array.distinct |> Array.length)
printfn ""

// Identify potential duplicates by finding raw tags that map to the same processed tag
let duplicateGroups =
    allRawTags
    |> Array.groupBy processTagName
    |> Array.filter (fun (processed, raws) -> raws.Length > 1)
    |> Array.sortBy fst

printfn "=== DUPLICATE TAG GROUPS (Already Handled by Processing) ==="
printfn "Groups where multiple raw tags map to same processed tag: %d" duplicateGroups.Length

for (processedTag, rawTags) in duplicateGroups do
    printfn "  %s <- [%s]" processedTag (String.Join("; ", rawTags))

printfn ""

// Look for potential merge candidates among processed tags
// Check for plural vs singular patterns, similar terms, etc.
let findSimilarTags (tags: string array) =
    let similarities = ResizeArray<string * string * string>()
    
    for i in 0..(tags.Length - 1) do
        for j in (i+1)..(tags.Length - 1) do
            let tag1 = tags.[i]
            let tag2 = tags.[j]
            
            // Check for plural patterns
            if tag1 + "s" = tag2 then
                similarities.Add(tag1, tag2, "plural")
            elif tag2 + "s" = tag1 then
                similarities.Add(tag2, tag1, "plural")
            elif tag1 + "ing" = tag2 then
                similarities.Add(tag1, tag2, "gerund")
            elif tag2 + "ing" = tag1 then
                similarities.Add(tag2, tag1, "gerund")
            // Check for common prefixes/suffixes
            elif tag1.Contains("-") && tag2.Contains("-") then
                let parts1 = tag1.Split('-')
                let parts2 = tag2.Split('-')
                if parts1.Length > 1 && parts2.Length > 1 then
                    if parts1.[0] = parts2.[0] || Array.last parts1 = Array.last parts2 then
                        similarities.Add(tag1, tag2, "related-compound")
            // Check for very similar strings (edit distance 1-2)
            elif abs(tag1.Length - tag2.Length) <= 2 then
                let commonChars = 
                    tag1.ToCharArray() 
                    |> Array.filter (fun c -> tag2.Contains(c))
                    |> Array.length
                let similarity = float commonChars / float (max tag1.Length tag2.Length)
                if similarity > 0.7 then
                    similarities.Add(tag1, tag2, "similar")
    
    similarities.ToArray()

let processedTags = processedTagPairs |> Array.map snd |> Array.distinct |> Array.sort
let similarTagGroups = findSimilarTags processedTags

printfn "=== POTENTIAL MERGE CANDIDATES ==="
printfn "Similar tag pairs found: %d" similarTagGroups.Length

for (tag1, tag2, reason) in similarTagGroups do
    printfn "  %s <-> %s (%s)" tag1 tag2 reason

printfn ""

// Count usage of tags to help prioritize merges
let getTagUsageCounts (posts: Post array) (notes: Post array) (responses: Response array) =
    let allTagUsages = ResizeArray<string>()
    
    // Add post tags
    for post in posts do
        if post.Metadata.Tags <> null then
            for tag in post.Metadata.Tags do
                allTagUsages.Add(processTagName tag)
    
    // Add note tags  
    for note in notes do
        if note.Metadata.Tags <> null then
            for tag in note.Metadata.Tags do
                allTagUsages.Add(processTagName tag)
                
    // Add response tags
    for response in responses do
        if response.Metadata.Tags <> null then
            for tag in response.Metadata.Tags do
                allTagUsages.Add(processTagName tag)
    
    allTagUsages.ToArray()
    |> Array.groupBy id
    |> Array.map (fun (tag, occurrences) -> (tag, occurrences.Length))
    |> Array.sortByDescending snd

let tagUsageCounts = getTagUsageCounts posts notes responses

printfn "=== TAG USAGE STATISTICS ==="
printfn "Top 20 most used tags:"
for (tag, count) in tagUsageCounts |> Array.take (min 20 tagUsageCounts.Length) do
    printfn "  %s: %d uses" tag count

printfn ""

// Analyze similar tags with usage data
printfn "=== MERGE RECOMMENDATIONS ==="
for (tag1, tag2, reason) in similarTagGroups do
    let count1 = tagUsageCounts |> Array.tryFind (fun (tag, _) -> tag = tag1) |> Option.map snd |> Option.defaultValue 0
    let count2 = tagUsageCounts |> Array.tryFind (fun (tag, _) -> tag = tag2) |> Option.map snd |> Option.defaultValue 0
    let total = count1 + count2
    
    if total >= 2 then // Only show recommendations for tags with some usage
        let primary = if count1 >= count2 then tag1 else tag2
        let secondary = if count1 >= count2 then tag2 else tag1
        let primaryCount = max count1 count2
        let secondaryCount = min count1 count2
        
        printfn "  MERGE: %s (%d) + %s (%d) -> %s (total: %d) [%s]" 
            secondary secondaryCount primary primaryCount primary total reason

printfn ""
printfn "=== ANALYSIS COMPLETE ==="