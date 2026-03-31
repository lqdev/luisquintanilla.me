#r "../bin/Debug/net10.0/PersonalSite.dll"

open Domain
open TagService

// Load all posts
let posts = Loaders.loadPosts "_src"

// Process all tags and group them
let allTags =
    posts
    |> Array.collect (fun post ->
        try post.Metadata.Tags |> Array.map (fun x -> processTagName x, post)
        with _ -> [| "untagged", post |])
    |> Set.ofArray
    |> Set.toArray
    |> Array.groupBy fst
    |> Array.map (fun (tag, items) -> tag, items |> Array.map snd)
    |> Array.sortByDescending (fun (_, items) -> items.Length)

printfn "=== Tag Summary ==="
printfn "Unique tags: %d" allTags.Length
printfn ""
printfn "=== Top 50 Tags by Use ==="
allTags
|> Array.take (min 50 allTags.Length)
|> Array.iter (fun (tag, items) -> printfn "%4d  %s" items.Length tag)