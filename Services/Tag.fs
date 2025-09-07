module TagService

    open Domain

    let processTagName (tag:string) = 
        let processed = 
            tag
                .Replace(".net","dotnet")
                .Replace(".","")
                .Replace(' ', '-')
                .Replace("c#","csharp")
                .ToLower()
                .Trim()
        
        // Return empty string as is - filtering happens at higher level
        processed

    let cleanTags (tags:string array) = 
        try
            let processedTags =
                tags 
                |> Array.map(processTagName)
                |> Array.filter(fun tag -> tag <> "" && tag <> null)
            
            // If no valid tags remain, assign "untagged"
            if processedTags.Length = 0 then [|"untagged"|] else processedTags
        with
            | _ -> [|"untagged"|]

    let cleanPostTags (post:Post) = 
        try
            let processedTags =
                post.Metadata.Tags 
                |> Array.map(processTagName)
                |> Array.filter(fun tag -> tag <> "" && tag <> null)
            
            // If no valid tags remain, assign "untagged"
            if processedTags.Length = 0 then [|"untagged"|] else processedTags
        with
            | _ -> [|"untagged"|]

    let cleanResponseTags (post:Response) = 
        try
            let processedTags =
                post.Metadata.Tags 
                |> Array.map(processTagName)
                |> Array.filter(fun tag -> tag <> "" && tag <> null)
            
            // If no valid tags remain, assign "untagged"
            if processedTags.Length = 0 then [|"untagged"|] else processedTags
        with
            | _ -> [|"untagged"|]

    let getTagsFromPost (post:Post) = 
        try
            let processedTags =
                post.Metadata.Tags 
                |> Array.map(fun x -> processTagName x, post)
                |> Array.filter(fun (tag, _) -> tag <> "" && tag <> null)
            
            // If no valid tags remain, assign "untagged"
            if processedTags.Length = 0 then [|"untagged", post|] else processedTags
        with 
            | _ -> [|"untagged",post|]

    let getTagsFromResponse (post:Response) = 
        try
            let processedTags =
                post.Metadata.Tags 
                |> Array.map(fun x -> processTagName x, post)
                |> Array.filter(fun (tag, _) -> tag <> "" && tag <> null)
            
            // If no valid tags remain, assign "untagged"
            if processedTags.Length = 0 then [|"untagged", post|] else processedTags
        with 
            | _ -> [|"untagged",post|]

    let processTaggedPost (unprocessedPosts: Post array) = 
        unprocessedPosts 
        |> Array.collect(getTagsFromPost)
        |> Set.ofArray
        |> Set.toArray
        |> Array.groupBy(fst)
        |> Array.map(fun x -> 
            let tag = fst x
            let post = snd x |> Array.map(snd)
            tag,post
        )

    let processTaggedResponse (unprocessedPosts: Response array) = 
        unprocessedPosts 
        |> Array.collect(getTagsFromResponse)
        |> Set.ofArray
        |> Set.toArray
        |> Array.groupBy(fst)
        |> Array.map(fun x -> 
            let tag = fst x
            let post = snd x |> Array.map(snd)
            tag,post
        )