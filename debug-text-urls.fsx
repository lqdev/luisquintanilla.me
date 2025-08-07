#load "Domain.fs"
#load "GenericBuilder.fs"

open GenericBuilder.UnifiedFeeds

let testData = loadUnifiedFeedData "_public"
let firstFew = testData |> List.take 5

for item in firstFew do
    printfn "URL: %s" item.Url
    printfn "ContentType: %s" item.ContentType
    printfn "Title: %s" item.Title
    printfn "---"
