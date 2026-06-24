module TagPagesBuilder

    open System
    open System.IO
    open Domain
    open ViewGenerator
    open TagViews
    open BuilderCommon

    /// Enhanced unified tag processing function supporting all ITaggable content types
    let buildUnifiedTagsPages (contentArrays: (string * ITaggable array) list) = 
        // Process all tagged content using unified ITaggable interface
        let processTaggedContent (contentType: string) (items: ITaggable array) =
            items
            |> Array.filter (fun item -> item.Tags <> null && item.Tags.Length > 0)
            |> Array.collect (fun item -> 
                item.Tags |> Array.map (fun tag -> (TagService.processTagName tag, item)))
            |> Array.groupBy fst
            |> Array.map (fun (tag, items) -> 
                let sortedItems = items |> Array.map snd |> Array.sortByDescending (fun x -> DateTimeOffset.Parse(x.Date))
                (tag, contentType, sortedItems))

        // Process all content types
        let allTaggedContent = 
            contentArrays
            |> List.collect (fun (contentType, items) -> 
                processTaggedContent contentType items |> Array.toList)
            |> List.toArray

        // Group by tag name across all content types
        let groupedByTag = 
            allTaggedContent
            |> Array.groupBy (fun (tag, _, _) -> tag)
            |> Array.map (fun (tag, tagGroups) -> 
                let contentByType = 
                    tagGroups 
                    |> Array.map (fun (_, contentType, items) -> (contentType, items))
                    |> Array.groupBy fst
                    |> Array.map (fun (contentType, groups) -> 
                        let allItems = groups |> Array.collect snd
                        (contentType, allItems))
                (tag, contentByType))
            |> Array.sortBy fst

        // Create tag directory structure and generate pages
        let saveDir = Path.Join(outputDir, "tags")
        Directory.CreateDirectory(saveDir) |> ignore

        // Generate individual tag pages for unified content
        groupedByTag
        |> Array.iter (fun (tag, contentByType) -> 
            let sanitizedTag = sanitizeTagForPath tag
            let individualTagSaveDir = Path.Join(saveDir, sanitizedTag)
            Directory.CreateDirectory(individualTagSaveDir) |> ignore
            
            // Prepare content arrays with proper prefixes and display names
            let taggedContentForView = 
                contentByType
                |> Array.collect (fun (contentType, items) -> 
                    match contentType with
                    | "responses" -> 
                        // Handle bookmark responses separately
                        let regularResponses = items |> Array.filter (fun item -> 
                            match item with
                            | :? Response as r -> r.Metadata.ResponseType <> "bookmark"
                            | _ -> true)
                        let bookmarkResponses = items |> Array.filter (fun item -> 
                            match item with
                            | :? Response as r -> r.Metadata.ResponseType = "bookmark"
                            | _ -> false)
                        
                        // Create separate entries for regular responses and bookmarks
                        let responseEntries = 
                            if regularResponses.Length > 0 then [(regularResponses, "responses", "Responses")] else []
                        let bookmarkEntries = 
                            if bookmarkResponses.Length > 0 then [(bookmarkResponses, "bookmarks", "Bookmarks")] else []
                        
                        Array.ofList (responseEntries @ bookmarkEntries)
                    | _ ->
                        let (prefix, displayName) = 
                            match contentType with
                            | "posts" -> ("posts", "Blogs")
                            | "notes" -> ("notes", "Notes") 
                            | "snippets" -> ("resources/snippets", "Snippets")
                            | "wikis" -> ("resources/wiki", "Wiki")
                            | "ai-memex" -> ("resources/ai-memex", "AI Memex")
                            | "presentations" -> ("resources/presentations", "Presentations")
                            | "albums" -> ("media/albums", "Albums")
                            | "marketplace" -> ("marketplace", "Marketplace")
                            | _ -> (contentType, contentType)
                        [|(items, prefix, displayName)|]
                    )
                |> Array.toList
            
            let individualTagPage = generate (individualTagViewUnified tag taggedContentForView) "default" $"{tag} - Tags - Luis Quintanilla"
            File.WriteAllText(Path.Join(individualTagSaveDir, "index.html"), individualTagPage))

        // Generate main tags index page
        let allTagNames = groupedByTag |> Array.map fst
        let tagPage = generate (allTagsView allTagNames) "default" "Tags - Luis Quintanilla"
        File.WriteAllText(Path.Join(saveDir, "index.html"), tagPage)

        printfn "✅ Unified tags pages created for %d tags across %d content types" allTagNames.Length contentArrays.Length
