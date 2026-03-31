module TagService

    open Domain

    // Canonical tag map applied after all character normalization.
    // Uses exact-match lookup (not substring Replace) to safely consolidate
    // plural, gerund, hyphenated, and misspelled variants.
    let private canonicalTagMap =
        dict [
            // --- Plural → Singular ---
            "agents",               "agent"
            "algorithms",           "algorithm"
            "apps",                 "app"
            "audiobooks",           "audiobook"
            "blogs",                "blog"
            "books",                "book"
            "calendars",            "calendar"
            "embeddings",           "embedding"
            "evaluations",          "evaluation"
            "feeds",                "feed"
            "images",               "image"
            "llms",                 "llm"
            "models",               "model"
            "movies",               "movie"
            "nationalparks",        "nationalpark"
            "neuralnetworks",       "neuralnetwork"
            "newsletters",          "newsletter"
            "notebooks",            "notebook"
            "openstandards",        "openstandard"
            "phones",               "phone"
            "photos",               "photo"
            "podcasts",             "podcast"
            "programminglanguages", "programminglanguage"
            "protocols",            "protocol"
            "smartphones",          "smartphone"
            "standards",            "standard"
            "tokenizers",           "tokenizer"
            "transformers",         "transformer"
            "videos",               "video"
            "webmentions",          "webmention"
            "websites",             "website"
            // --- Gerund / verb-form → base form ---
            "selfhosting",          "selfhost"
            "self-hosting",         "selfhost"
            // --- Compound-word hyphenated → concatenated (dominant form wins) ---
            // These are reached naturally because spaces→hyphens runs first,
            // so "machine learning" → "machine-learning" → "machinelearning"
            "artificial-intelligence", "artificialintelligence"
            "computer-vision",      "computervision"
            "data-science",         "datascience"
            "dotnet-core",          "dotnetcore"
            "e-mail",               "email"
            "hugging-face",         "huggingface"
            "local-first",          "localfirst"
            "machine-learning",     "machinelearning"
            "open-source",          "opensource"
            "org-mode",             "orgmode"
            "raspberry-pi",         "raspberrypi"
            "social-media",         "socialmedia"
            "text-to-speech",       "texttospeech"
            // --- Typo fixes ---
            "messsaging",           "messaging"
        ]

    let processTagName (tag: string) = 
        if System.String.IsNullOrWhiteSpace(tag) then "untagged"
        else
            let processed = 
                tag.ToLower().Trim()
                // Technology-specific replacements (order matters!)
                |> fun s -> s.Replace(".net core", "dotnet")
                |> fun s -> s.Replace(".net framework", "dotnet-framework") 
                |> fun s -> s.Replace(".net", "dotnet")
                |> fun s -> s.Replace("asp.net", "aspnet")
                |> fun s -> s.Replace("c#", "csharp")
                |> fun s -> s.Replace("f#", "fsharp")
                |> fun s -> s.Replace("node.js", "nodejs")
                |> fun s -> s.Replace("next.js", "nextjs")
                |> fun s -> s.Replace("vue.js", "vuejs")
                // Special characters
                |> fun s -> s.Replace("#", "sharp")
                |> fun s -> s.Replace("++", "plus")
                |> fun s -> s.Replace("@", "at")
                |> fun s -> s.Replace("&", "and")
                |> fun s -> s.Replace("%", "percent")
                |> fun s -> s.Replace("$", "dollar")
                // Space normalization
                |> fun s -> System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ")
                |> fun s -> s.Replace(' ', '-')
                |> fun s -> s.Replace('_', '-')
                // URL-unsafe characters
                |> fun s -> s.Replace("(", "").Replace(")", "")
                |> fun s -> s.Replace("[", "").Replace("]", "")
                |> fun s -> s.Replace("{", "").Replace("}", "")
                |> fun s -> s.Replace(":", "").Replace(";", "")
                |> fun s -> s.Replace(",", "").Replace("'", "").Replace("\"", "")
                |> fun s -> s.Replace("?", "").Replace("!", "")
                // Remove double hyphens and clean up
                |> fun s -> s.Replace("--", "-")
                |> fun s -> s.Trim('-').Trim()
            
            if System.String.IsNullOrWhiteSpace(processed) || processed = "-" then "untagged"
            else
                match canonicalTagMap.TryGetValue(processed) with
                | true, canonical -> canonical
                | false, _        -> processed

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