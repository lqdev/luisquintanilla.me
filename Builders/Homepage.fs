module HomepageBuilder

    open System
    open System.IO
    open Domain
    open Loaders
    open ViewGenerator
    open PartialViews
    open BuilderCommon

    let buildHomePage (blogPosts:Post array) (feedPosts:Post array) (responsePosts:Response array)= 
        let recentBlog = 
            blogPosts 
            |> Array.sortByDescending(fun x-> DateTimeOffset.Parse(x.Metadata.Date))
            |> Array.head

        let recentFeedPost = 
            feedPosts
            |> Array.sortByDescending(fun x -> DateTimeOffset.Parse(x.Metadata.Date))
            |> Array.head

        let recentResponsePost = 
            responsePosts
            |> Array.sortByDescending(fun x -> DateTimeOffset.Parse(x.Metadata.DateUpdated))
            |> Array.head

        // let recentPostsContent = generatePartial (recentPostsView recentPosts)
        let homePage = generate (homeView recentBlog recentFeedPost recentResponsePost) "default" "Home - Luis Quintanilla"
        File.WriteAllText(Path.Join(outputDir,"index.html"),homePage)

    // New timeline homepage for Phase 3 - Feed-as-Homepage Interface
    let buildTimelineHomePage (allUnifiedItems: (string * UnifiedFeeds.UnifiedFeedItem list) list) =
        // Load pinned posts configuration
        let pinnedPostsConfig = loadPinnedPosts()
        
        // Flatten all items
        let allItemsFlattened = 
            allUnifiedItems
            |> List.collect snd
            |> List.sortByDescending (fun item -> DateTimeOffset.Parse(item.Date))
        
        // Extract pinned items based on configuration (using filename matching)
        let pinnedItems = 
            pinnedPostsConfig
            |> Array.choose (fun (config: PinnedPost) ->
                allItemsFlattened 
                |> List.tryFind (fun item -> 
                    // Extract filename from URL: /posts/my-post/ -> my-post
                    let urlFileName = 
                        item.Url.TrimEnd('/').Split('/')
                        |> Array.last
                    urlFileName = config.FileName && 
                    item.ContentType = config.ContentType))
            |> Array.toList
        
        // Remove pinned items from chronological list
        let unpinnedItemsFlattened = 
            allItemsFlattened
            |> List.filter (fun item ->
                not (pinnedItems |> List.exists (fun pinned -> pinned.Url = item.Url)))
        
        // Take initial items from unpinned list
        let chronologicalInitialItems = 
            unpinnedItemsFlattened
            |> List.take (min 50 unpinnedItemsFlattened.Length)
        
        // Combine: pinned first, then chronological
        let initialItems = pinnedItems @ chronologicalInitialItems
        
        // Group remaining items by content type for type-aware progressive loading
        let remainingItemsByType = 
            allUnifiedItems
            |> List.map (fun (contentType, items) ->
                let sortedItems = items |> List.sortByDescending (fun item -> DateTimeOffset.Parse(item.Date))
                // Skip items that are already in the initial load (pinned + chronological)
                let remainingItems = 
                    sortedItems 
                    |> List.filter (fun item -> 
                        not (initialItems |> List.exists (fun initial -> initial.Url = item.Url)))
                (contentType, remainingItems)
            )
            |> List.filter (fun (_, items) -> not items.IsEmpty)
        
        // Create set of pinned URLs for efficient lookup
        let pinnedUrls = pinnedItems |> List.map (fun item -> item.Url) |> Set.ofList
        
        // Generate the timeline homepage with pinned posts support
        let timelineHomePage = generate (TimelineViews.timelineHomeViewStratified (initialItems |> List.toArray) remainingItemsByType pinnedUrls) "default" "Luis Quintanilla - Personal Website"
        File.WriteAllText(Path.Join(outputDir,"index.html"), timelineHomePage)
        
        let totalItems = allUnifiedItems |> List.sumBy (fun (_, items) -> items.Length)
        let pinnedCount = pinnedItems.Length
        printfn "✅ Timeline homepage created with %d pinned posts, %d chronological items from %d total items across all content types" pinnedCount chronologicalInitialItems.Length totalItems

    let buildUnifiedFeedPage (allUnifiedItems: (string * UnifiedFeeds.UnifiedFeedItem list) list) =
        // Flatten all feed items and sort chronologically
        let flattenedItems = 
            allUnifiedItems
            |> List.collect snd
            |> List.sortByDescending (fun item -> DateTimeOffset.Parse(item.Date))
            |> List.take (min 30 (allUnifiedItems |> List.collect snd |> List.length)) // Limit to 30 items
            |> List.toArray
        
        // Generate the unified feed page
        let unifiedFeedHtml = generate (enhancedSubscriptionHubView flattenedItems) "defaultindex" "Feeds & Content Discovery - Luis Quintanilla"
        let feedIndexDir = Path.Join(outputDir, "feed")
        Directory.CreateDirectory(feedIndexDir) |> ignore
        File.WriteAllText(Path.Join(feedIndexDir, "index.html"), unifiedFeedHtml)
        
        printfn "✅ Unified feed page created at /feed/index.html with %d items" flattenedItems.Length
