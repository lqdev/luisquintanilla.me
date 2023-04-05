let tags = [|"a";"b";"c"|]

type Post = string
type Response = int

type TaggedCollection = { Posts:Post array; Notes:Post array; Responses:Response array }

let posts = [||]

let mutable tagDict = 
    tags
    |> Array.map(fun n -> (n, {Posts=[||];Notes=[||];Responses=[||]}))
    |> Map

tagDict

tagDict <- tagDict.Add("a", {tagDict["a"] with Posts=[|"Hello"|]})
tagDict <- tagDict.Add("a", {tagDict["a"] with Notes=[|"My longer note"|]})
tagDict <- tagDict.Add("a", {tagDict["a"] with Responses=[|1|]})

tagDict
|> Map.toArra