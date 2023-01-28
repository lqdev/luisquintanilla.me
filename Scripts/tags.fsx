#r "../bin/Debug/net7.0/PersonalSite.dll"

open Domain

let (posts:Post array) = Builder.loadPosts()

let processTagName (tag:string) = 
    tag
      .Replace(".net","dotnet")
      .Replace(".","")
      .Replace(' ', '-')
      .ToLower()

let tags = 
    posts 
    |> Array.collect(fun post -> 
        try
            post.Metadata.Tags 
            |> Array.map(fun x -> 
                processTagName x, post)
        with 
            | _ -> [|"untagged",post|]
    )
    |> Set.ofArray
    |> Set.toArray
    |> Array.groupBy(fst)
    |> Array.map(fun x -> 
        let tag = fst x
        let file = snd x |> Array.map(snd)
        tag,file
    )