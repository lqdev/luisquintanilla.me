module TagService

    open Domain

    let processTagName (tag:string) = 
        tag
            .Replace(".net","dotnet")
            .Replace(".","")
            .Replace(' ', '-')
            .Replace("c#","csharp")
            .ToLower()

    let cleanTags (tags:string array) = 
        try
            tags |> Array.map(processTagName)
        with
            | _ -> [|"untagged"|]

    let cleanPostTags (post:Post) = 
        try
            post.Metadata.Tags |> Array.map(processTagName)
        with
            | _ -> [|"untagged"|]

    let cleanResponseTags (post:Response) = 
        try
            post.Metadata.Tags |> Array.map(processTagName)
        with
            | _ -> [|"untagged"|]

    let getTagsFromPost (post:Post) = 
        try
            post.Metadata.Tags 
            |> Array.map(fun x -> 
                processTagName x, post)
        with 
            | _ -> [|"untagged",post|]

    let getTagsFromResponse (post:Response) = 
        try
            post.Metadata.Tags 
            |> Array.map(fun x -> 
                processTagName x, post)
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